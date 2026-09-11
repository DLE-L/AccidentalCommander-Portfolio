using System;
using System.Reflection;
using Lizzo.PV.Gameplay.Presentation;
using Lizzo.PV.Legion;
using Lizzo.PV.Legion.RunCore;
using Lizzo.PV.Legion.RunCore.Presentation;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Lizzo.PV.EditorTests
{
    internal sealed class PooledCompanionEffects : IDisposable
    {
        private readonly GameObject _root = new GameObject("CompanionEffectPoolTest");
        internal ObjectPoolService Storage { get; }
        internal CompanionEffectPool Effects { get; }
        internal CompanionRuntimePresentationSet Set { get; }
        internal PooledCompanionEffects()
        {
            Storage = new ObjectPoolService(_root.transform);
            Effects = new CompanionEffectPool(new PrefabFactory(new TestAssetService(), Storage));
            Set = AssetDatabase.LoadAssetAtPath<CompanionRuntimePresentationSet>(
                "Assets/_LizzoPV/Gameplay/Legion/Presentation/Data/CompanionRuntimePresentationSet.asset");
        }
        internal T Find<T>() where T : Component => _root.GetComponentInChildren<T>(true);
        public void Dispose()
        {
            Effects.Dispose();
            // Destroy test-owned hierarchy immediately; production Clear schedules destruction in Play Mode.
            Object.DestroyImmediate(_root);
        }
    }

    public sealed class CompanionEffectPoolTests
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private static void Tick(object view, string method) => view.GetType().GetMethod(method, PrivateInstance).Invoke(view, null);
        private static void SetField(object target, string field, object value) => target.GetType().GetField(field, PrivateInstance).SetValue(target, value);
        private static object Field(object target, string field) => target.GetType().GetField(field, PrivateInstance).GetValue(target);

        [Test]
        public void ReturningThenArea_ReusesInstanceAndResetsFlightRendererAndClock()
        {
            using var f = new PooledCompanionEffects();
            var flight = new ReturningAttackFlight(Vector3.zero, Vector3.right * 4f, .4f);
            var play = typeof(CompanionTravelingPayloadView).GetMethod("TryPlayFlight", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(play.Invoke(null, new object[] { f.Set, f.Effects, "dmg_skeleton_scythe_throw_v1", flight, 1f }), Is.True);
            var first = f.Find<CompanionTravelingPayloadView>();
            int id = first.GetInstanceID();
            first.transform.localScale = Vector3.one * 99f;
            first.GetComponent<SpriteRenderer>().color = Color.red;
            flight.Complete();
            Tick(first, "LateUpdate");
            Assert.That(f.Effects.ActiveCount, Is.Zero);
            Assert.That(f.Storage.ActiveCount, Is.Zero);
            Assert.That(first.gameObject.activeSelf, Is.False);
            Assert.That(first.GetComponent<SpriteRenderer>().sprite, Is.Null);

            Vector3 source = new Vector3(3, 4, 0);
            Assert.That(CompanionTravelingPayloadView.TryPlayArea(f.Set, f.Effects, AttackDelivery.Area,
                "bombardier_payload", source, source + Vector3.up * 4f, .2f, .5f), Is.True);
            var reused = f.Find<CompanionTravelingPayloadView>();
            Assert.That(reused.GetInstanceID(), Is.EqualTo(id));
            Assert.That(Field(reused, "_flight"), Is.Null);
            Assert.That(Field(reused, "_elapsed"), Is.EqualTo(0f));
            Assert.That(reused.transform.position, Is.EqualTo(source));
            Assert.That(reused.GetComponent<SpriteRenderer>().enabled, Is.True);
            Assert.That(f.Set.Projectiles.TryGetVisual("bombardier_payload", out var visual), Is.True);
            Assert.That(reused.GetComponent<SpriteRenderer>().sprite, Is.SameAs(visual.BodySprite));
            Assert.That(reused.transform.localScale, Is.EqualTo(Vector3.Scale(visual.Scale, new Vector3(1.85f, 1.85f, 1))));
            Assert.That(reused.GetComponent<SpriteRenderer>().color, Is.EqualTo(new Color(visual.Tint.r*.5f, visual.Tint.g*.5f, visual.Tint.b*.5f, visual.Tint.a)));
            SetField(reused, "_elapsed", 1f);
            Tick(reused, "Update");
            reused.Release();
            Assert.That(f.Effects.ActiveCount, Is.Zero);
            Assert.That(f.Storage.ActiveCount, Is.Zero);
        }

        [Test]
        public void BurstCompletion_ReusesInstanceAndStartsAtFirstBurstAgain()
        {
            using var f = new PooledCompanionEffects();
            CompanionBurstVfxSequence.Play(f.Effects, f.Set.BurstPrefab, "pool_test_burst", Vector3.zero, Vector3.up, 7.4f);
            var first = f.Find<CompanionBurstVfxSequence>();
            int id = first.GetInstanceID();
            Assert.That(Field(first, "_nextBurstIndex"), Is.EqualTo(1));
            SetField(first, "_nextBurstAt", Time.unscaledTime - 1f);
            Tick(first, "Update");
            Assert.That(f.Storage.ActiveCount, Is.Zero);
            Assert.That(Field(first, "_effectId"), Is.Null);
            CompanionBurstVfxSequence.Play(f.Effects, f.Set.BurstPrefab, "pool_test_burst", Vector3.right * 3, Vector3.left, 2f, .6f);
            var reused = f.Find<CompanionBurstVfxSequence>();
            Assert.That(reused.GetInstanceID(), Is.EqualTo(id));
            Assert.That(Field(reused, "_nextBurstIndex"), Is.EqualTo(1));
            Assert.That(Field(reused, "_origin"), Is.EqualTo(Vector3.right * 3));
            Assert.That(Field(reused, "_intensityMultiplier"), Is.EqualTo(.6f));
            Assert.That((float)Field(reused, "_nextBurstAt"), Is.GreaterThan(Time.unscaledTime));
        }

        [Test]
        public void ResetAndDispose_ReturnMixedEffectsExactlyOnceAndRejectLaterRent()
        {
            using var f = new PooledCompanionEffects();
            CompanionTravelingPayloadView.TryPlayArea(f.Set, f.Effects, AttackDelivery.Area,
                "bombardier_payload", Vector3.zero, Vector3.right, 10f, 1f);
            CompanionBurstVfxSequence.Play(f.Effects, f.Set.BurstPrefab, "pool_test_burst", Vector3.zero, Vector3.up, 2f);
            var payload = f.Find<CompanionTravelingPayloadView>();
            var burst = f.Find<CompanionBurstVfxSequence>();
            Assert.That(f.Effects.ActiveCount, Is.EqualTo(2));
            f.Effects.Reset();
            f.Effects.Reset();
            payload.Release();
            burst.Release();
            Assert.That(f.Storage.ActiveCount, Is.Zero);
            Assert.That(f.Effects.ActiveCount, Is.Zero);
            Assert.That(payload.gameObject.activeSelf || burst.gameObject.activeSelf, Is.False);
            f.Effects.Dispose();
            Assert.Throws<ObjectDisposedException>(() => f.Effects.Rent(f.Set.TravelingPayloadPrefab));
        }

        [Test]
        public void Stop_BlocksLatePresentationUntilScenarioReset()
        {
            using var f = new PooledCompanionEffects();
            Assert.That(CompanionTravelingPayloadView.TryPlayArea(f.Set, f.Effects, AttackDelivery.Area,
                "bombardier_payload", Vector3.zero, Vector3.right, 10f, 1f), Is.True);
            f.Effects.Stop();
            Assert.That(f.Storage.ActiveCount, Is.Zero);
            Assert.That(CompanionTravelingPayloadView.TryPlayArea(f.Set, f.Effects, AttackDelivery.Area,
                "bombardier_payload", Vector3.zero, Vector3.right, 10f, 1f), Is.False);
            CompanionBurstVfxSequence.Play(f.Effects, f.Set.BurstPrefab, "pool_test_burst", Vector3.zero, Vector3.up, 2f);
            Assert.That(f.Effects.ActiveCount, Is.Zero);
            f.Effects.Reset();
            Assert.That(CompanionTravelingPayloadView.TryPlayArea(f.Set, f.Effects, AttackDelivery.Area,
                "bombardier_payload", Vector3.zero, Vector3.right, 10f, 1f), Is.True);
        }
    }
}
