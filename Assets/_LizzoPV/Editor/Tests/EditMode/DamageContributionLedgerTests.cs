using System.Collections.Generic;
using System.Reflection;
using Lizzo.PV.Combat;
using Lizzo.PV.Legion;
using Lizzo.PV.Legion.Synergy;
using Lizzo.PV.P0.Units;
using Lizzo.PV.P0.Visuals;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class DamageContributionLedgerTests
    {
        [Test]
        public void CapturesCanonicalSynergyBucketsInActivationOrder()
        {
            using ServiceTestFixture fixture = new ServiceTestFixture();
            using DamageContributionLedger ledger = new DamageContributionLedger(fixture.Data);

            string[] expectedIds =
            {
                SynergyActivationIds.GuardShockwave,
                SynergyActivationIds.ArcherRain,
                SynergyActivationIds.MagicChain,
                SynergyActivationIds.ExplosionChain,
                SynergyActivationIds.BeastHunt,
                SynergyActivationIds.UndeadSummon,
                SynergyActivationIds.HealingBond,
                SynergyActivationIds.MixedCommand,
            };

            for (int index = 0; index < expectedIds.Length; index++)
                Assert.That(ledger.RecordAppliedDamage(expectedIds[index], index + 1), Is.True);

            DamageContributionSnapshot snapshot = ledger.CaptureSnapshot();
            Assert.That(snapshot.SynergyEntries, Has.Count.EqualTo(expectedIds.Length));
            for (int index = 0; index < expectedIds.Length; index++)
            {
                Assert.That(snapshot.SynergyEntries[index].Id, Is.EqualTo(expectedIds[index]));
                Assert.That(snapshot.SynergyEntries[index].Damage, Is.EqualTo(index + 1));
            }

            Assert.That(snapshot.CompanionEntries, Has.All.Property(nameof(DamageContributionEntry.Damage)).EqualTo(0));
        }

        [Test]
        public void ResolvesPromotedAndPersonalSummonSourcesToOneCanonicalCompanionBucket()
        {
            using ServiceTestFixture fixture = new ServiceTestFixture();
            using DamageContributionLedger ledger = new DamageContributionLedger(fixture.Data);

            Assert.That(ledger.RecordAppliedDamage("shield_guard", 4), Is.True);
            Assert.That(ledger.RecordAppliedDamage("shield_captain", 5), Is.True);
            Assert.That(ledger.RecordAppliedDamage("necromancer:UNIT_PERSONAL_SKELETON_01", 6), Is.True);
            Assert.That(ledger.RecordAppliedDamage("necromancer:UNIT_PERSONAL_SKELETON_01", 115, 1.15f), Is.True);
            Assert.That(ledger.RecordAppliedDamage(SynergyActivationIds.GuardShockwave, 7), Is.True);
            Assert.That(ledger.RecordAppliedDamage(SynergyActivationIds.GuardShockwave, 115, 1.15f), Is.True);
            Assert.That(ledger.RecordAppliedDamage("commander_01", 100), Is.False);
            Assert.That(ledger.RecordAppliedDamage("enemy:small_goblin", 100), Is.False);
            Assert.That(ledger.RecordAppliedDamage("unknown", 100), Is.False);
            Assert.That(ledger.RecordAppliedDamage("necromancer:", 100), Is.False);
            Assert.That(ledger.RecordAppliedDamage("shield_guard", 0), Is.False);
            Assert.That(ledger.RecordAppliedDamage("shield_guard", -1), Is.False);

            DamageContributionSnapshot snapshot = ledger.CaptureSnapshot();
            Assert.That(Find(snapshot.CompanionEntries, "shield_guard").Damage, Is.EqualTo(9));
            Assert.That(Find(snapshot.CompanionEntries, "necromancer").Damage, Is.EqualTo(121));
            Assert.That(Find(snapshot.SynergyEntries, SynergyActivationIds.GuardShockwave).Damage, Is.EqualTo(122));
            Assert.That(Find(snapshot.SynergyEntries, SynergyActivationIds.MixedCommand).AttributedBonusDamage, Is.EqualTo(0));
            Assert.That(Sum(snapshot), Is.EqualTo(252));
        }

        [Test]
        public void SnapshotRemainsImmutableAfterResetAndDispose()
        {
            FakeDataProvider data = new FakeDataProvider();
            data.InitializeAsync().GetAwaiter().GetResult();
            using DamageContributionLedger ledger = new DamageContributionLedger(data);
            Assert.That(ledger.RecordAppliedDamage("shield_guard", 12), Is.True);
            DamageContributionSnapshot snapshot = ledger.CaptureSnapshot();

            ledger.Reset();
            ledger.Dispose();

            Assert.That(Find(snapshot.CompanionEntries, "shield_guard").Damage, Is.EqualTo(12));
            Assert.That(Find(snapshot.SynergyEntries, SynergyActivationIds.GuardShockwave).PreventedDamage, Is.EqualTo(0));
            Assert.That(ledger.RecordAppliedDamage("shield_guard", 3), Is.False);
        }

        [Test]
        public void WinnerUsesActiveCanonicalOrderAndAllowsActiveZeroScore()
        {
            using ServiceTestFixture fixture = new ServiceTestFixture();
            using DamageContributionLedger ledger = new DamageContributionLedger(fixture.Data);
            ledger.RecordAppliedDamage(SynergyActivationIds.ExplosionChain, 100);
            ledger.RecordAppliedDamage(SynergyActivationIds.ArcherRain, 5);

            DamageContributionSnapshot noWinner = ledger.CaptureSnapshot();
            Assert.That(noWinner.BestActiveSynergy.HasValue, Is.False);

            SynergyActivationSnapshot[] active =
            {
                new SynergyActivationSnapshot(SynergyActivationIds.GuardShockwave, true, null),
                new SynergyActivationSnapshot(SynergyActivationIds.ArcherRain, true, null),
            };
            DamageContributionSnapshot winner = ledger.CaptureSnapshot(active);
            Assert.That(winner.BestActiveSynergy.HasValue, Is.True);
            Assert.That(winner.BestActiveSynergy.Value.Id, Is.EqualTo(SynergyActivationIds.ArcherRain));

            ledger.Reset();
            DamageContributionSnapshot zeroWinner = ledger.CaptureSnapshot(active);
            Assert.That(zeroWinner.BestActiveSynergy.Value.Id, Is.EqualTo(SynergyActivationIds.GuardShockwave));
        }

        [Test]
        public void ScoresSupportAndMixedContributionAndSelectsBestActiveSynergyDeterministically()
        {
            using ServiceTestFixture fixture = new ServiceTestFixture();
            using DamageContributionLedger ledger = new DamageContributionLedger(fixture.Data);

            Assert.That(ledger.RecordAppliedDamage("shield_guard", 100), Is.True);
            Assert.That(ledger.RecordAppliedDamage("shield_guard", 115, 1.15f), Is.True);
            Assert.That(ledger.RecordPreventedDamage(SynergyActivationIds.GuardShockwave, 9), Is.True);
            Assert.That(ledger.RecordPreventedDamage(SynergyActivationIds.HealingBond, 1), Is.True);

            SynergyActivationSnapshot[] active =
            {
                new SynergyActivationSnapshot(SynergyActivationIds.GuardShockwave, true, "slot_00"),
                new SynergyActivationSnapshot(SynergyActivationIds.MixedCommand, true, null),
            };
            DamageContributionSnapshot snapshot = ledger.CaptureSnapshot(active);
            DamageContributionEntry guard = Find(snapshot.SynergyEntries, SynergyActivationIds.GuardShockwave);
            DamageContributionEntry mixed = Find(snapshot.SynergyEntries, SynergyActivationIds.MixedCommand);

            Assert.That(guard.DirectDamage, Is.EqualTo(0));
            Assert.That(guard.PreventedDamage, Is.EqualTo(9));
            Assert.That(guard.TotalScore, Is.EqualTo(9));
            Assert.That(mixed.AttributedBonusDamage, Is.EqualTo(15));
            Assert.That(snapshot.BestActiveSynergy.HasValue, Is.True);
            Assert.That(snapshot.BestActiveSynergy.Value.Id, Is.EqualTo(SynergyActivationIds.MixedCommand));
        }

        [Test]
        public void AllocatesOverlappingPreventionByShapleyAndPreservesCanonicalTieOrder()
        {
            DamagePreventionAllocation allocation = DamageContributionLedger.CalculatePreventionAllocation(10, 7, 8, 5);

            Assert.That(allocation.Total, Is.EqualTo(5));
            Assert.That(allocation.GuardShockwave, Is.EqualTo(3));
            Assert.That(allocation.HealingBond, Is.EqualTo(2));

            DamagePreventionAllocation tie = DamageContributionLedger.CalculatePreventionAllocation(9, 8, 8, 7);
            Assert.That(tie.Total, Is.EqualTo(2));
            Assert.That(tie.GuardShockwave, Is.EqualTo(1));
            Assert.That(tie.HealingBond, Is.EqualTo(1));

            DamagePreventionAllocation fractionalTie = DamageContributionLedger.CalculatePreventionAllocation(7, 6, 6, 4);
            Assert.That(fractionalTie.Total, Is.EqualTo(3));
            Assert.That(fractionalTie.GuardShockwave, Is.EqualTo(2));
            Assert.That(fractionalTie.HealingBond, Is.EqualTo(1));
        }

        [Test]
        public void MonsterControllerRecordsAppliedHpLossAfterOverkillCapping()
        {
            using ServiceTestFixture fixture = new ServiceTestFixture();
            FloatingDamageText.Configure(new NullPrefabFactory());
            MonsterController target = CreateTarget(fixture, 10);
            GameObject targetObject = target.gameObject;
            try
            {
                target.transform.position = new Vector3(17.0f, 23.0f, 0.0f);
                LogAssert.Expect(LogType.Error, "[FloatingDamageText] Authored prefab is not cached: FloatingDamageText.prefab");
                target.OnDamagedFromPosition(Vector3.zero, 4, "shield_captain");
                target.OnDamagedFromPosition(Vector3.zero, 9, "shield_captain");

                Assert.That(Find(fixture.Run.DamageContributions.CaptureSnapshot().CompanionEntries, "shield_guard").Damage, Is.EqualTo(10));
            }
            finally
            {
                Object.DestroyImmediate(targetObject);
                FloatingDamageText.ClearServices();
            }
        }

        static DamageContributionEntry Find(IReadOnlyList<DamageContributionEntry> entries, string id)
        {
            for (int index = 0; index < entries.Count; index++)
                if (entries[index].Id == id)
                    return entries[index];

            Assert.Fail($"Missing contribution bucket '{id}'.");
            return default;
        }

        static int Sum(DamageContributionSnapshot snapshot)
        {
            int total = 0;
            for (int index = 0; index < snapshot.SynergyEntries.Count; index++)
                total += snapshot.SynergyEntries[index].Damage;
            for (int index = 0; index < snapshot.CompanionEntries.Count; index++)
                total += snapshot.CompanionEntries[index].Damage;
            return total;
        }

        static MonsterController CreateTarget(ServiceTestFixture fixture, int hp)
        {
            GameObject instance = new GameObject("DamageContributionTarget");
            instance.SetActive(false);
            instance.AddComponent<Rigidbody2D>();
            CircleCollider2D body = instance.AddComponent<CircleCollider2D>();
            body.isTrigger = false;
            CircleCollider2D combat = instance.AddComponent<CircleCollider2D>();
            combat.isTrigger = true;
            UnitColliderRefs refs = instance.AddComponent<UnitColliderRefs>();
            typeof(UnitColliderRefs).GetField("_bodyCollider", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(refs, body);
            typeof(UnitColliderRefs).GetField("_combatCollider", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(refs, combat);
            EnemyHealthBar health = instance.AddComponent<EnemyHealthBar>();
            HitFlash flash = instance.AddComponent<HitFlash>();
            instance.AddComponent<UnitVisualDriver>();
            instance.AddComponent<PatternEnemyVisual>();
            MonsterController target = instance.AddComponent<MonsterController>();
            typeof(MonsterController).GetField("_healthBar", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, health);
            typeof(MonsterController).GetField("_hitFlash", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, flash);
            instance.SetActive(true);
            target.Initialize(fixture.Run);
            target.ResetForSpawn();
            target.MaxHp = hp;
            target.Hp = hp;
            fixture.Run.Registry.RegisterEnemy(target);
            return target;
        }

        sealed class NullPrefabFactory : IPrefabFactory
        {
            public GameObject Spawn(string address, Transform parent = null, bool pooled = false) => null;
            public GameObject Rent(GameObject prefab, string poolKey, Transform parent = null) => null;
            public void Release(GameObject instance) { }
            public void Clear() { }
        }
    }
}
