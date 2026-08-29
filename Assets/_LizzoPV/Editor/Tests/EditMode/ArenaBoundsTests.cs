using Lizzo.PV.Gameplay.World;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class ArenaBoundsTests
    {
        private GameObject _root;
        private ArenaBounds _bounds;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("ArenaBoundsTests");
            _bounds = _root.AddComponent<ArenaBounds>();
            _bounds.Configure(Vector2.one * 100.0f);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
        }

        [TestCase(6.0f)]
        [TestCase(8.5f)]
        public void ClampCameraCenter_PortraitZoomKeepsViewInsideOuterPadding(float orthographicSize)
        {
            const float portraitAspect = 9.0f / 16.0f;
            Vector2 clamped = _bounds.ClampCameraCenter(new Vector2(100.0f, 100.0f), orthographicSize, portraitAspect);

            Assert.That(clamped.x, Is.EqualTo(50.0f - ArenaBounds.CameraOuterPadding - orthographicSize * portraitAspect).Within(0.0001f));
            Assert.That(clamped.y, Is.EqualTo(50.0f - ArenaBounds.CameraOuterPadding - orthographicSize).Within(0.0001f));
        }

        [TestCase(-100.0f, -100.0f, -48.5f, -48.5f)]
        [TestCase(100.0f, -100.0f, 48.5f, -48.5f)]
        [TestCase(100.0f, 100.0f, 48.5f, 48.5f)]
        [TestCase(-100.0f, 100.0f, -48.5f, 48.5f)]
        public void ClampPartyAnchor_UsesInsetAtEveryArenaCorner(float x, float y, float expectedX, float expectedY)
        {
            Vector2 clamped = _bounds.ClampPartyAnchor(new Vector2(x, y));

            Assert.That(clamped.x, Is.EqualTo(expectedX).Within(0.0001f));
            Assert.That(clamped.y, Is.EqualTo(expectedY).Within(0.0001f));
        }

        [TestCase(-100.0f, -100.0f, -48.5f, -48.5f)]
        [TestCase(100.0f, -100.0f, 48.5f, -48.5f)]
        [TestCase(100.0f, 100.0f, 48.5f, 48.5f)]
        [TestCase(-100.0f, 100.0f, -48.5f, 48.5f)]
        public void ClampFriendlyActor_UsesInsetAtEveryArenaCorner(float x, float y, float expectedX, float expectedY)
        {
            Vector2 clamped = _bounds.ClampFriendlyActor(new Vector2(x, y));

            Assert.That(clamped.x, Is.EqualTo(expectedX).Within(0.0001f));
            Assert.That(clamped.y, Is.EqualTo(expectedY).Within(0.0001f));
        }

        [Test]
        public void CompactArena_PartyAnchorCanTraverseBeyondInitialPortraitCameraWhileRemainingVisible()
        {
            const float initialOrthographicSize = 6.0f;
            const float portraitAspect = 1080.0f / 2340.0f;
            _bounds.Configure(Vector2.one * 20.0f);

            Vector2 partyAnchor = _bounds.ClampPartyAnchor(new Vector2(100.0f, 100.0f));
            Vector2 cameraCenter = _bounds.ClampCameraCenter(
                partyAnchor,
                initialOrthographicSize,
                portraitAspect);

            Assert.That(partyAnchor, Is.EqualTo(Vector2.one * 8.5f));
            Assert.That(partyAnchor.y, Is.GreaterThan(initialOrthographicSize));
            Assert.That(
                Mathf.Abs(partyAnchor.y - cameraCenter.y),
                Is.LessThanOrEqualTo(initialOrthographicSize));
        }

        [TestCase(-1.0f, -1.0f)]
        [TestCase(1.0f, -1.0f)]
        [TestCase(1.0f, 1.0f)]
        [TestCase(-1.0f, 1.0f)]
        public void PartyAnchorAndFriendlyActors_AreIndividuallyClampedInsideFollowedCamera(float xSign, float ySign)
        {
            const float maxFriendlyFootprint = 2.2f * 0.85f + 0.82f + 0.5f;
            const float orthographicSize = 8.5f;
            const float portraitAspect = 9.0f / 16.0f;
            Vector2 partyAnchor = _bounds.ClampPartyAnchor(new Vector2(xSign * 100.0f, ySign * 100.0f));
            Vector2 cameraCenter = _bounds.ClampCameraCenter(partyAnchor, orthographicSize, portraitAspect);
            Vector2 visibleMapEdge = new Vector2(
                cameraCenter.x + xSign * orthographicSize * portraitAspect,
                cameraCenter.y + ySign * orthographicSize);
            Vector2 friendlyActor = _bounds.ClampFriendlyActor(
                partyAnchor + new Vector2(xSign, ySign) * maxFriendlyFootprint);

            Assert.That(Mathf.Abs(partyAnchor.x), Is.LessThanOrEqualTo(Mathf.Abs(visibleMapEdge.x) + 0.0001f));
            Assert.That(Mathf.Abs(partyAnchor.y), Is.LessThanOrEqualTo(Mathf.Abs(visibleMapEdge.y) + 0.0001f));
            Assert.That(Mathf.Abs(friendlyActor.x), Is.LessThanOrEqualTo(Mathf.Abs(visibleMapEdge.x) + 0.0001f));
            Assert.That(Mathf.Abs(friendlyActor.y), Is.LessThanOrEqualTo(Mathf.Abs(visibleMapEdge.y) + 0.0001f));
        }

        [Test]
        public void ResolveOutsideCamera_NearCornerKeepsNormalDirectionalAndRingRequestsInsideAndOutsideView()
        {
            const float orthographicSize = 8.5f;
            const float portraitAspect = 9.0f / 16.0f;
            Vector2 cameraCenter = _bounds.ClampCameraCenter(new Vector2(100.0f, 100.0f), orthographicSize, portraitAspect);
            Vector2[] requests =
            {
                Vector2.right,
                Vector2.up,
                Vector2.one.normalized,
                Vector2.left,
                Vector2.down,
                new Vector2(-1.0f, -1.0f).normalized,
                new Vector2(1.0f, -1.0f).normalized,
                new Vector2(-1.0f, 1.0f).normalized,
            };

            foreach (Vector2 direction in requests)
            {
                Vector2 spawn = _bounds.ResolveOutsideCamera(cameraCenter, orthographicSize, portraitAspect, direction, 2.4f);

                Assert.That(_bounds.Contains(spawn), Is.True, $"direction={direction}");
                Assert.That(_bounds.IsOutsideCameraView(spawn, cameraCenter, orthographicSize, portraitAspect), Is.True, $"direction={direction}");
            }
        }

        [TestCase(-100.0f, -100.0f)]
        [TestCase(100.0f, -100.0f)]
        [TestCase(100.0f, 100.0f)]
        [TestCase(-100.0f, 100.0f)]
        public void ResolveBossPlacement_ClampsArenaAndSpawnInsideAtEveryCorner(float x, float y)
        {
            Vector2 center = _bounds.ResolveBossArenaCenter(new Vector2(x, y), 8.4f, 12.0f, 4.2f);
            Vector2 spawn = _bounds.ResolveBossSpawnPosition(center, 4.2f);

            Assert.That(_bounds.ContainsRect(center, new Vector2(8.4f, 12.0f)), Is.True);
            Assert.That(_bounds.Contains(spawn), Is.True);
        }

        [Test]
        public void AuthoredPrefabs_UseExactCompactArenaGeometryAndResolvedArenaBounds()
        {
            GameObject mapRoot = PrefabUtility.LoadPrefabContents("Assets/_LizzoPV/Gameplay/World/Prefabs/@Map.prefab");
            GameObject groundRoot = PrefabUtility.LoadPrefabContents("Assets/_LizzoPV/Gameplay/World/Prefabs/ArenaGround.prefab");
            try
            {
                ArenaBounds arenaBounds = mapRoot.GetComponent<ArenaBounds>();
                Transform groundInstance = mapRoot.transform.Find("ArenaGround");
                SpriteRenderer background = groundRoot.transform.Find("Background").GetComponent<SpriteRenderer>();

                Assert.That(arenaBounds, Is.Not.Null);
                Assert.That(arenaBounds.Size, Is.EqualTo(Vector2.one * 100.0f));
                Assert.That(background.size, Is.EqualTo(Vector2.one * 100.0f));
                Assert.That(mapRoot.transform.childCount, Is.EqualTo(1));
                Assert.That(groundInstance, Is.Not.Null);
                Assert.That(groundInstance.localPosition, Is.EqualTo(Vector3.zero));
                Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(mapRoot), Is.EqualTo(0));
                Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(groundRoot), Is.EqualTo(0));
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(mapRoot);
                PrefabUtility.UnloadPrefabContents(groundRoot);
            }
        }


        [Test]
        public void ConfiguredCompactArena_ResizesEveryRenderedGroundSurfaceToLogicalSize()
        {
            GameObject mapRoot = PrefabUtility.LoadPrefabContents("Assets/_LizzoPV/Gameplay/World/Prefabs/@Map.prefab");
            try
            {
                ArenaBounds arenaBounds = mapRoot.GetComponent<ArenaBounds>();
                Transform ground = mapRoot.transform.Find("ArenaGround");
                SpriteRenderer[] surfaces = ground.GetComponentsInChildren<SpriteRenderer>(true);

                arenaBounds.Configure(Vector2.one * 20.0f);

                Assert.That(surfaces, Has.Length.EqualTo(2));
                for (int index = 0; index < surfaces.Length; index++)
                    Assert.That(surfaces[index].size, Is.EqualTo(Vector2.one * 20.0f));
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(mapRoot);
            }
        }
    }
}
