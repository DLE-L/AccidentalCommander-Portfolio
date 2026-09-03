using System.Reflection;
using Lizzo.PV.Legion;
using Lizzo.PV.P0.Units;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class BossWorldFeedbackRegressionTests
    {
        private const string HungryGiantPrefabPath =
            "Assets/_LizzoPV/Gameplay/Enemies/Prefabs/Units/HungryGiant.prefab";

        [Test]
        public void HitFlash_RepeatedHitPreservesOriginalTint()
        {
            GameObject root = new GameObject("HitFlashRegression");
            try
            {
                SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
                Color original = new Color(0.45f, 0.08f, 0.08f, 1f);
                renderer.color = original;
                HitFlash flash = root.AddComponent<HitFlash>();

                flash.Play();
                flash.Play();

                Color stored = (Color)typeof(HitFlash)
                    .GetField("_baseColor", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(flash);
                Assert.That(stored, Is.EqualTo(original));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void HungryGiantPrefab_HasVisibleAuthoredAttackTelegraphs()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(HungryGiantPrefabPath);
            try
            {
                HungryGiantBehaviour boss = root.GetComponent<HungryGiantBehaviour>();
                Assert.That(boss, Is.Not.Null);
                SerializedObject serialized = new SerializedObject(boss);
                SpriteRenderer charge = serialized.FindProperty("_chargePathRenderer").objectReferenceValue
                    as SpriteRenderer;
                SpriteRenderer aoe = serialized.FindProperty("_aoeWarningRenderer").objectReferenceValue
                    as SpriteRenderer;

                Assert.That(charge, Is.Not.Null);
                Assert.That(aoe, Is.Not.Null);
                Assert.That(charge.sprite, Is.Not.Null, "Charge telegraph needs a visible temporary sprite.");
                Assert.That(aoe.sprite, Is.Not.Null, "AOE telegraph needs a visible temporary sprite.");

                aoe.gameObject.SetActive(false);
                typeof(HungryGiantBehaviour)
                    .GetMethod("BeginBossAoeWarning", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(boss, new object[] { Vector2.zero });
                Assert.That(aoe.gameObject.activeSelf, Is.True);
                Assert.That(aoe.enabled, Is.True);
                Assert.That(aoe.bounds.size.x, Is.EqualTo(1.65f * 1.64f).Within(0.03f));
                Assert.That(aoe.bounds.size.y, Is.EqualTo(1.65f * 1.64f).Within(0.03f));

                typeof(HungryGiantBehaviour)
                    .GetMethod("HideBossAoeWarning", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(boss, null);
                object chargeWarning = typeof(HungryGiantBehaviour)
                    .GetField("_chargePathWarning", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(boss);
                chargeWarning.GetType()
                    .GetMethod("Bind", BindingFlags.Instance | BindingFlags.Public)
                    .Invoke(chargeWarning, new object[] { charge });
                typeof(HungryGiantBehaviour)
                    .GetMethod("BeginBossChargeWarning", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(boss, new object[] { Vector2.right });
                typeof(HungryGiantBehaviour)
                    .GetField("_chargeWarningElapsed", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(boss, 0.5f);
                typeof(HungryGiantBehaviour)
                    .GetMethod("ShowBossChargePath", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(boss, null);
                Assert.That(charge.gameObject.activeSelf, Is.True);
                Assert.That(charge.enabled, Is.True);
                Assert.That(charge.bounds.size.y, Is.EqualTo(1.25f).Within(0.03f));
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
