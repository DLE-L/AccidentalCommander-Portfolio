using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Lizzo.PV.P0.Units;

namespace Lizzo.PV.EditorTests
{
    public sealed class BossVideoReadabilityTests
    {
        [Test]
        public void ChargeLane_PreservesRequestedWorldSizeUnderScaledBoss()
        {
            GameObject boss = new GameObject("Boss");
            GameObject lane = new GameObject("ChargePathWarning");
            try
            {
                boss.transform.localScale = Vector3.one * 0.7f;
                lane.transform.SetParent(boss.transform, false);
                SpriteRenderer renderer = lane.AddComponent<SpriteRenderer>();
                System.Type warningType = System.Type.GetType(
                    "Lizzo.PV.P0.Units.ChargePathWarning, Assembly-CSharp",
                    throwOnError: true);
                object warning = System.Activator.CreateInstance(warningType, nonPublic: true);
                warningType.GetMethod("Bind").Invoke(warning, new object[] { renderer });

                warningType.GetMethod("Show").Invoke(
                    warning,
                    new object[] { boss.transform, Vector2.zero, Vector2.right, 4.0f, 1.25f, Color.red, "test" });

                Vector3 worldScale = renderer.transform.lossyScale;
                Assert.That(Mathf.Abs(worldScale.x), Is.EqualTo(4.0f).Within(0.001f));
                Assert.That(Mathf.Abs(worldScale.y), Is.EqualTo(1.25f).Within(0.001f));
                Assert.That(renderer.color.r, Is.GreaterThan(0.8f));
                Assert.That(renderer.color.g, Is.LessThan(0.25f));
                Assert.That(renderer.color.a, Is.InRange(0.12f, 0.25f));

                Transform upperEdge = boss.transform.Find("ChargePathWarning_EdgeUpper");
                Transform lowerEdge = boss.transform.Find("ChargePathWarning_EdgeLower");
                Assert.That(upperEdge, Is.Not.Null);
                Assert.That(lowerEdge, Is.Not.Null);
                Assert.That(upperEdge.GetComponent<SpriteRenderer>().enabled, Is.True);
                Assert.That(lowerEdge.GetComponent<SpriteRenderer>().enabled, Is.True);
                Assert.That(Mathf.Abs(upperEdge.lossyScale.x), Is.EqualTo(4.0f).Within(0.001f));
                Assert.That(Mathf.Abs(lowerEdge.lossyScale.x), Is.EqualTo(4.0f).Within(0.001f));
                Assert.That(Mathf.Abs(upperEdge.lossyScale.y), Is.LessThan(0.14f));
                Assert.That(Mathf.Abs(lowerEdge.lossyScale.y), Is.LessThan(0.14f));
            }
            finally
            {
                Object.DestroyImmediate(boss);
            }
        }

        [Test]
        public void ChargeOrigin_UsesVisualGroundContactInsteadOfDirectionalBodyEdge()
        {
            GameObject boss = new GameObject("Boss");
            GameObject visual = new GameObject("Visual");
            try
            {
                visual.transform.SetParent(boss.transform, false);
                visual.transform.localPosition = new Vector3(2.0f, 3.0f, 0.0f);
                SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
                renderer.sprite = Sprite.Create(
                    Texture2D.whiteTexture,
                    new Rect(0.0f, 0.0f, 1.0f, 1.0f),
                    new Vector2(0.5f, 0.5f),
                    1.0f);
                HungryGiantBehaviour behaviour = boss.AddComponent<HungryGiantBehaviour>();
                SetField(behaviour, "_spriteRenderer", renderer);
                SetField(behaviour, "_chargeDirection", Vector2.right);

                Vector2 origin = (Vector2)Invoke(behaviour, "GetBossChargeOrigin");

                Assert.That(origin.x, Is.EqualTo(renderer.bounds.center.x).Within(0.001f));
                Assert.That(origin.y, Is.EqualTo(renderer.bounds.min.y).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(boss);
            }
        }

        [Test]
        public void AoeTelegraph_ActivatesAuthoredGroundRootAndBuildsWorldSpaceCircle()
        {
            GameObject boss = new GameObject("Boss");
            GameObject warning = new GameObject("BossAoeWarning");
            try
            {
                boss.transform.localScale = Vector3.one * 0.7f;
                warning.transform.SetParent(boss.transform, false);
                SpriteRenderer renderer = warning.AddComponent<SpriteRenderer>();
                HungryGiantBehaviour behaviour = boss.AddComponent<HungryGiantBehaviour>();
                SetField(behaviour, "_aoeWarningRenderer", renderer);
                warning.SetActive(false);

                Invoke(behaviour, "BeginBossAoeWarning", new Vector2(2.0f, -1.0f));

                LineRenderer circle = warning.GetComponent<LineRenderer>();
                Assert.That(warning.activeSelf, Is.True);
                Assert.That(circle, Is.Not.Null);
                Assert.That(circle.enabled, Is.True);
                Assert.That(circle.useWorldSpace, Is.True);
                Assert.That(circle.positionCount, Is.GreaterThanOrEqualTo(32));
            }
            finally
            {
                Object.DestroyImmediate(boss);
            }
        }

        [Test]
        public void ChargeMovement_StartsAttackPoseAtChargeExecution()
        {
            string sourcePath = Path.Combine(
                Application.dataPath,
                "_LizzoPV/Gameplay/Enemies/Runtime/HungryGiantBehaviour.Movement.cs");
            string source = File.ReadAllText(sourcePath);

            StringAssert.Contains("PlayBossAttackMotion(_chargeDirection, 0.35f)", source);
        }

        [Test]
        public void BossTelegraphPreparation_DoesNotStartAttackPose()
        {
            string aoeSource = File.ReadAllText(Path.Combine(
                Application.dataPath,
                "_LizzoPV/Gameplay/Enemies/Runtime/HungryGiantBehaviour.Aoe.cs"));
            string chargeSource = File.ReadAllText(Path.Combine(
                Application.dataPath,
                "_LizzoPV/Gameplay/Enemies/Runtime/HungryGiantBehaviour.Charge.cs"));
            string movementSource = File.ReadAllText(Path.Combine(
                Application.dataPath,
                "_LizzoPV/Gameplay/Enemies/Runtime/HungryGiantBehaviour.Movement.cs"));

            StringAssert.DoesNotContain(
                "PlayBossAttackMotion",
                GetMethodSource(aoeSource, "BeginBossAoeWarning"));
            StringAssert.DoesNotContain(
                "PlayBossAttackMotion",
                GetMethodSource(chargeSource, "BeginBossChargeWarning"));
            StringAssert.Contains("PlayBossAttackMotion(_aoeCenter", aoeSource);
            StringAssert.Contains("PlayBossAttackMotion(_chargeDirection, 0.35f)", movementSource);
        }

        private static string GetMethodSource(string source, string methodName)
        {
            int start = source.IndexOf(methodName, System.StringComparison.Ordinal);
            Assert.That(start, Is.GreaterThanOrEqualTo(0), methodName);
            int end = source.IndexOf("\n        private void ", start + methodName.Length, System.StringComparison.Ordinal);
            return end >= 0 ? source.Substring(start, end - start) : source.Substring(start);
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(target, value);
        }

        private static object Invoke(object target, string name, params object[] args)
        {
            MethodInfo method = target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, name);
            return method.Invoke(target, args);
        }
    }
}
