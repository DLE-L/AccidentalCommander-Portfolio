using System.Collections.Generic;
using Lizzo.PV.Gameplay.Spawning;
using NUnit.Framework;
using UnityEngine;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class SpawnPositionResolverTests
    {
        readonly List<Camera> _disabledMainCameras = new List<Camera>();
        GameObject _cameraObject;

        [SetUp]
        public void SetUp()
        {
            foreach (Camera camera in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                if (camera.enabled && camera.CompareTag("MainCamera"))
                {
                    camera.enabled = false;
                    _disabledMainCameras.Add(camera);
                }
            }

            _cameraObject = new GameObject("SpawnPositionResolverTestMainCamera");
            _cameraObject.tag = "MainCamera";
        }

        [TearDown]
        public void TearDown()
        {
            if (_cameraObject != null)
                Object.DestroyImmediate(_cameraObject);

            foreach (Camera camera in _disabledMainCameras)
            {
                if (camera != null)
                    camera.enabled = true;
            }

            _disabledMainCameras.Clear();
        }

        [Test]
        public void ResolveWithDirection_UsesOrthographicCardinalCameraEdgeAndMargin()
        {
            Camera camera = CreateOrthographicMainCamera(10.0f, 2.0f);

            Vector2 position = SpawnPositionResolver.ResolveOutsideCamera(
                new Vector2(3.0f, -2.0f),
                Vector2.right,
                1.5f);

            Assert.That(position.x, Is.EqualTo(24.5f).Within(0.0001f));
            Assert.That(position.y, Is.EqualTo(-2.0f).Within(0.0001f));
            Assert.That(camera.orthographic, Is.True);
        }

        [Test]
        public void ResolveWithDirection_NormalizesDiagonalBeforeCameraEdgeCalculation()
        {
            CreateOrthographicMainCamera(10.0f, 2.0f);

            Vector2 position = SpawnPositionResolver.ResolveOutsideCamera(
                Vector2.zero,
                new Vector2(1.0f, 1.0f),
                0.0f);

            Assert.That(position.x, Is.EqualTo(10.0f).Within(0.0001f));
            Assert.That(position.y, Is.EqualTo(10.0f).Within(0.0001f));
        }

        [Test]
        public void ResolveWithDirection_UsesRightForZeroDirectionAndClampsMargin()
        {
            CreateOrthographicMainCamera(10.0f, 2.0f);

            Vector2 position = SpawnPositionResolver.ResolveOutsideCamera(
                new Vector2(3.0f, 4.0f),
                Vector2.zero,
                -1.0f);

            Assert.That(position.x, Is.EqualTo(23.0f).Within(0.0001f));
            Assert.That(position.y, Is.EqualTo(4.0f).Within(0.0001f));
        }

        [Test]
        public void ResolveWithDirection_UsesFallbackDistanceForNonOrthographicMainCamera()
        {
            Camera camera = _cameraObject.AddComponent<Camera>();
            camera.orthographic = false;

            Vector2 position = SpawnPositionResolver.ResolveOutsideCamera(
                Vector2.zero,
                Vector2.up,
                2.0f);

            Assert.That(position.x, Is.EqualTo(0.0f).Within(0.0001f));
            Assert.That(position.y, Is.EqualTo(7.0f).Within(0.0001f));
        }

        Camera CreateOrthographicMainCamera(float orthographicSize, float aspect)
        {
            Camera camera = _cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = orthographicSize;
            camera.aspect = aspect;
            return camera;
        }
    }
}
