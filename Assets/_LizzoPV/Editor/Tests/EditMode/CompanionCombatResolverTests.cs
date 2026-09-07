using System.Collections.Generic;
using Lizzo.PV.Combat;
using Lizzo.PV.Combat.Fields;
using Lizzo.PV.Data;
using Lizzo.PV.Flow;
using Lizzo.PV.Legion;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class CompanionCombatResolverTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            for (int i = _objects.Count - 1;
            i >= 0;
            i--)
            {
                if (_objects[i] != null)
                    Object.DestroyImmediate(_objects[i]);
            }

            _objects.Clear();
        }

        [Test]
        public void MeleeResolver_MapsCanonicalProfiles()
        {
            LocalDataProvider provider = CreateProjectProvider();
            Assert.IsTrue(provider.InitializeAsync().GetAwaiter().GetResult().Succeeded);
            CompanionMeleeCombatResolver resolver = new CompanionMeleeCombatResolver(provider);

            for (int i = 0;
            i < MeleeProfiles.Length;
            i++)
            {
                MeleeProfileCase expected = MeleeProfiles[i];
                Assert.IsTrue(resolver.TryResolve(expected.SourceId, 1.0f, out CompanionMeleeCombatSetup setup));
                Assert.AreEqual(expected.AttackStyle, setup.AttackStyle);
                Assert.AreEqual(expected.Damage, setup.Damage);
                Assert.AreEqual(expected.Period, setup.Period);
                Assert.AreEqual(expected.Range, setup.Range);
                Assert.AreEqual(expected.Angle, setup.Angle);
                Assert.AreEqual(expected.Knockback, setup.Knockback);
                Assert.AreEqual(expected.MaxTargets, setup.MaxTargets);
                Assert.AreEqual(expected.Retry, setup.NoTargetRetrySeconds);
            }
        }

        [Test]
        public void MeleeResolver_RejectsUnsupportedProfiles()
        {
            LocalDataProvider provider = CreateProjectProvider();
            Assert.IsTrue(provider.InitializeAsync().GetAwaiter().GetResult().Succeeded);
            CompanionMeleeCombatResolver resolver = new CompanionMeleeCombatResolver(provider);

            Assert.IsFalse(resolver.TryResolve("cleric", 1.0f, out _));
            Assert.IsFalse(resolver.TryResolve("archer", 1.0f, out _));
        }

        [Test]
        public void ProjectileResolver_MapsFalconProfileAndRejectsOtherFamilies()
        {
            LocalDataProvider provider = CreateProjectProvider();
            Assert.IsTrue(provider.InitializeAsync().GetAwaiter().GetResult().Succeeded);
            CompanionProjectileCombatResolver resolver = new CompanionProjectileCombatResolver(provider);

            Assert.IsTrue(resolver.TryResolve("falcon_archer", 1.0f, out CompanionProjectileCombatSetup setup));
            Assert.AreEqual("falcon_archer", setup.SourceId);
            Assert.AreEqual(AllyAttackStyle.TargetedProjectile, setup.AttackStyle);
            Assert.AreEqual(9, setup.Damage);
            Assert.AreEqual(0.9f, setup.Period);
            Assert.AreEqual(5.5f, setup.Range);
            Assert.AreEqual(3, setup.MaxTargets);
            Assert.IsTrue(setup.IsStraightPiercing);
            Assert.AreEqual(0.15f, setup.NoTargetRetrySeconds);
            Assert.IsFalse(resolver.TryResolve("archer", 1.0f, out _));
            Assert.IsFalse(resolver.TryResolve("cleric", 1.0f, out _));
        }

        [Test]
        public void PersistentFieldResolver_MapsFireMageAndRejectsOtherFamilies()
        {
            LocalDataProvider provider = CreateProjectProvider();
            Assert.IsTrue(provider.InitializeAsync().GetAwaiter().GetResult().Succeeded);
            CompanionPersistentFieldCombatResolver resolver = new CompanionPersistentFieldCombatResolver(provider);

            Assert.IsTrue(resolver.TryResolve("fire_mage", 1.0f, out CompanionPersistentFieldCombatSetup setup));
            Assert.AreEqual("fire_mage", setup.SourceId);
            Assert.AreEqual("dot_fire_field_v1", setup.EffectId);
            Assert.AreEqual(5, setup.Damage);
            Assert.AreEqual(3.2f, setup.Period);
            Assert.AreEqual(4.8f, setup.Range);
            Assert.AreEqual(1.6f, setup.Radius);
            Assert.AreEqual(1.0f, setup.TickInterval);
            Assert.AreEqual(3.0f, setup.Duration);
            Assert.AreEqual(8, setup.MaxTargets);
            Assert.AreEqual(2, setup.MaxActiveFields);
            Assert.AreEqual(0.15f, setup.NoTargetRetrySeconds);
            Assert.IsFalse(resolver.TryResolve("bombardier", 1.0f, out _));
        }

        [Test]
        public void TargetAreaResolver_MapsCanonicalBombardierAndHerbalistProfiles()
        {
            LocalDataProvider provider = CreateProjectProvider();
            Assert.IsTrue(provider.InitializeAsync().GetAwaiter().GetResult().Succeeded);
            CompanionTargetAreaCombatResolver resolver = new CompanionTargetAreaCombatResolver(provider);

            for (int i = 0;
            i < TargetAreaProfiles.Length;
            i++)
            {
                TargetAreaProfileCase expected = TargetAreaProfiles[i];
                Assert.IsTrue(resolver.TryResolve(expected.SourceId, 1.0f, out CompanionTargetAreaCombatSetup setup));
                AssertTargetAreaSetup(setup, expected.SourceId, expected.Damage, expected.Period, expected.Range, expected.Radius, expected.MaxTargets,
                    expected.CastDelay, expected.Retry);
            }

            Assert.IsFalse(resolver.TryResolve("fire_mage", 1.0f, out _));
            Assert.IsFalse(resolver.TryResolve("skeleton_scythe_thrower", 1.0f, out _));
        }

        [Test]
        public void RangedSupportResolver_MapsReturningLightClericOnly()
        {
            LocalDataProvider provider = CreateProjectProvider();
            Assert.IsTrue(provider.InitializeAsync().GetAwaiter().GetResult().Succeeded);
            CompanionRangedSupportCombatResolver resolver = new CompanionRangedSupportCombatResolver(provider);

            for (int i = 0;
            i < RangedSupportProfiles.Length;
            i++)
            {
                RangedSupportProfileCase expected = RangedSupportProfiles[i];
                Assert.IsTrue(resolver.TryResolve(expected.SourceId, 1.0f, out CompanionRangedSupportCombatSetup setup));
                AssertRangedSupportSetup(setup, expected.SourceId, expected.PrimaryDamage, expected.PrimaryPeriod, expected.PrimaryRange,
                    expected.SecondaryHeal, expected.SecondaryPeriod, expected.SecondaryRange);
            }

            Assert.IsFalse(resolver.TryResolve("falcon_archer", 1.0f, out _));
            Assert.IsFalse(resolver.TryResolve("field_herbalist", 1.0f, out _));
        }

        [Test]
        public void ResolvedSetups_PreservePublicAllyCombatBridgesAndCadence()
        {
            LocalDataProvider provider = CreateProjectProvider();
            Assert.IsTrue(provider.InitializeAsync().GetAwaiter().GetResult().Succeeded);

            CompanionMeleeCombatResolver meleeResolver = new CompanionMeleeCombatResolver(provider);
            Assert.IsTrue(meleeResolver.TryResolve("shield_guard", 1.0f, out CompanionMeleeCombatSetup meleeSetup));
            GameObject meleeObject = CreateObject("ShieldCombat");
            AllyCombat meleeCombat = meleeObject.AddComponent<AllyCombat>();
            meleeCombat.SetCanonicalMeleeInfo(meleeSetup);
            Assert.AreEqual(3, meleeCombat.MaxForwardTargetCount);
            Assert.AreEqual(0.15f, meleeCombat.NoTargetRetrySeconds);
            Assert.IsTrue(meleeCombat.CanAcceptForwardTarget(2));
            Assert.IsFalse(meleeCombat.CanAcceptForwardTarget(3));
            Assert.AreEqual(0.15f, meleeCombat.ResolveNextAttackDelay(false));
            Assert.AreEqual(1.4f, meleeCombat.ResolveNextAttackDelay(true));

            CompanionProjectileCombatResolver projectileResolver = new CompanionProjectileCombatResolver(provider);
            Assert.IsTrue(projectileResolver.TryResolve("falcon_archer", 1.0f, out CompanionProjectileCombatSetup projectileSetup));
            GameObject projectileObject = CreateObject("FalconCombat");
            AllyCombat projectileCombat = projectileObject.AddComponent<AllyCombat>();
            projectileCombat.SetCanonicalProjectileInfo(projectileSetup);
            Assert.AreEqual(AllyAttackStyle.TargetedProjectile, projectileCombat.AttackStyle);
            Assert.AreEqual(9, projectileCombat.Damage);
            Assert.AreEqual(0.9f, projectileCombat.AttackPeriod);
            Assert.AreEqual(5.5f, projectileCombat.AttackRange);
            Assert.AreEqual(3, projectileCombat.MaxProjectileTargetCount);
            Assert.IsTrue(projectileCombat.UsesStraightPiercingProjectile);
            Assert.AreEqual("falcon_archer", projectileCombat.CombatSourceId);
            Assert.AreEqual(0.9f, projectileCombat.ResolveNextAttackDelay(true));

            CompanionPersistentFieldCombatResolver fieldResolver = new CompanionPersistentFieldCombatResolver(provider);
            Assert.IsTrue(fieldResolver.TryResolve("fire_mage", 1.0f, out CompanionPersistentFieldCombatSetup fieldSetup));
            GameObject fieldObject = CreateObject("FireMageCombat");
            AllyCombat fieldCombat = fieldObject.AddComponent<AllyCombat>();
            fieldCombat.SetCanonicalPersistentFieldInfo(fieldSetup);
            Assert.AreEqual(AllyAttackStyle.TargetedField, fieldCombat.AttackStyle);
            Assert.AreEqual(5, fieldCombat.Damage);
            Assert.AreEqual(3.2f, fieldCombat.AttackPeriod);
            Assert.AreEqual(4.8f, fieldCombat.AttackRange);
            Assert.AreEqual("fire_mage", fieldCombat.CombatSourceId);
            Assert.AreEqual("dot_fire_field_v1", fieldCombat.PersistentFieldSetup.EffectId);

            CompanionTargetAreaCombatResolver herbalResolver = new CompanionTargetAreaCombatResolver(provider);
            Assert.IsTrue(herbalResolver.TryResolve("field_herbalist", 1.0f, out CompanionTargetAreaCombatSetup herbalSetup));
            GameObject herbalObject = CreateObject("HerbalistCombat");
            AllyCombat herbalCombat = herbalObject.AddComponent<AllyCombat>();
            herbalCombat.SetCanonicalTargetAreaInfo(herbalSetup);
            Assert.AreEqual(AllyAttackStyle.TargetedArea, herbalCombat.AttackStyle);
            Assert.AreEqual(8, herbalCombat.Damage);
            Assert.AreEqual(1.4f, herbalCombat.AttackPeriod);
            Assert.AreEqual(5.0f, herbalCombat.AttackRange);
            Assert.AreEqual("field_herbalist", herbalCombat.CombatSourceId);
        }

        [Test]
        public void ChainSelector_OrdersNearestThenNearestUnhitWithDeterministicCap()
        {
            List<ChainTargetCandidate> source = new List<ChainTargetCandidate>
            {
                new ChainTargetCandidate(null, new Vector3(1, 0), 30),
                new ChainTargetCandidate(null, new Vector3(1, 0), 10),
                new ChainTargetCandidate(null, new Vector3(2.5f, 0), 20),
                new ChainTargetCandidate(null, new Vector3(4.2f, 0), 40),
            }
            ;
            List<ChainTargetCandidate> results = new List<ChainTargetCandidate>();

            ChainTargetSelector.Collect(source, Vector3.zero, 5.0f, 1.8f, 3, results);

            Assert.AreEqual(3, results.Count);
            Assert.AreEqual(10, results[0].InstanceId);
            Assert.AreEqual(30, results[1].InstanceId);
            Assert.AreEqual(20, results[2].InstanceId);
        }

        [Test]
        public void TargetAreaCastState_UsesRetryDelayAndConsumesOneLockedImpactExactlyOnce()
        {
            TargetAreaCastState state = new TargetAreaCastState();
            state.Configure(new CompanionTargetAreaCombatSetup("bombardier", 16, 2.2f, 5.0f, 1.6f, 6, 0.5f, 0.15f), 0.0f, 0.0f);

            Assert.IsTrue(state.IsReadyForTarget(0.0f));
            state.RecordNoTarget(0.0f);
            Assert.AreEqual(0.15f, state.NextTargetDueTime);
            Assert.IsFalse(state.IsReadyForTarget(0.14f));
            Assert.IsTrue(state.IsReadyForTarget(0.15f));

            Vector3 impactPoint = new Vector3(2.0f, 3.0f, 0.0f);
            Assert.IsTrue(state.TryBeginCast(0.15f, impactPoint));
            Assert.IsFalse(state.TryConsumeImpact(0.64f, out _));
            Assert.IsTrue(state.TryConsumeImpact(0.65f, out Vector3 consumedPoint));
            Assert.AreEqual(impactPoint, consumedPoint);
            Assert.IsFalse(state.TryConsumeImpact(0.65f, out _));
            Assert.AreEqual(2.85f, state.NextTargetDueTime);
        }

        [Test]
        public void TargetAreaImpactCollector_OrdersByDistanceThenInstanceIdAndCapsTargets()
        {
            List<TargetAreaImpactCandidate> source = new List<TargetAreaImpactCandidate>
            {
                new TargetAreaImpactCandidate(null, new Vector3(1.0f, 0.0f), 30),
                new TargetAreaImpactCandidate(null, new Vector3(-1.0f, 0.0f), 10),
                new TargetAreaImpactCandidate(null, new Vector3(2.0f, 0.0f), 20),
                new TargetAreaImpactCandidate(null, new Vector3(4.0f, 0.0f), 40),
            }
            ;
            List<TargetAreaImpactCandidate> results = new List<TargetAreaImpactCandidate>();

            TargetAreaImpactCollector.Collect(source, Vector3.zero, 2.0f, 3, results);

            Assert.AreEqual(3, results.Count);
            Assert.AreEqual(10, results[0].InstanceId);
            Assert.AreEqual(30, results[1].InstanceId);
            Assert.AreEqual(20, results[2].InstanceId);
        }

        [Test]
        public void SupportAbilitySchedules_AdvancePrimaryAndSecondaryIndependently()
        {
            CombatAbilitySchedule primary = new CombatAbilitySchedule();
            CombatAbilitySchedule secondary = new CombatAbilitySchedule();
            primary.Configure(1.4f, 0.15f, 0.0f, 0.0f);
            secondary.Configure(6.0f, 0.15f, 0.0f, 0.0f);
            primary.RecordResolution(0.0f, true);
            secondary.RecordResolution(0.0f, false);

            Assert.AreEqual(1.4f, primary.NextDueTime);
            Assert.AreEqual(0.15f, secondary.NextDueTime);
            Assert.IsFalse(primary.IsDue(0.15f));
            Assert.IsTrue(secondary.IsDue(0.15f));
        }

        [Test]
        public void ImmediateHit_ValidAllyAndEnemyRequestsDispatchExactlyOnce()
        {
            RecordingTarget enemy = CreateTarget("Enemy", CombatImmediateHitFaction.Enemy);
            CombatImmediateHitModule module = new CombatImmediateHitModule();
            CombatImmediateHitRequest allyRequest = CombatImmediateHitRequest.CreateAllyDirectTarget(
                "cleric", enemy, Vector3.left, Vector3.right, 12, AttackVisualKind.SingleHit, true);

            Assert.IsTrue(module.TryApply(allyRequest));
            Assert.AreEqual(1, enemy.DispatchCount);
            Assert.AreEqual(CombatImmediateHitMode.AllyDirectTarget, enemy.LastRequest.Mode);
            Assert.AreEqual("cleric", enemy.LastRequest.SourceId);
            Assert.AreEqual(12, enemy.LastRequest.Damage);
            Assert.AreEqual(AttackVisualKind.SingleHit, enemy.LastRequest.AllyFeedback);

            RecordingTarget ally = CreateTarget("Ally", CombatImmediateHitFaction.Ally);
            CombatImmediateHitRequest enemyRequest = CombatImmediateHitRequest.CreateEnemyContact(
                "small_goblin", ally, Vector3.zero, Vector3.up, 4, "contact_attack", RetroVfxKind.PlayerDamaged);

            Assert.IsTrue(module.TryApply(enemyRequest));
            Assert.AreEqual(1, ally.DispatchCount);
            Assert.AreEqual(CombatImmediateHitMode.EnemyContact, ally.LastRequest.Mode);
            Assert.AreEqual("contact_attack", ally.LastRequest.EnemyPatternId);
            Assert.AreEqual(4, ally.LastRequest.Damage);
            Assert.AreEqual(RetroVfxKind.PlayerDamaged, ally.LastRequest.EnemyFeedback);
        }

        [Test]
        public void ImmediateHit_InvalidOrResultLockedRequestsAreRejected()
        {
            CombatImmediateHitModule module = new CombatImmediateHitModule();
            RecordingTarget ally = CreateTarget("Ally", CombatImmediateHitFaction.Ally);
            RecordingTarget enemy = CreateTarget("Enemy", CombatImmediateHitFaction.Enemy);
            enemy.IsAlive = false;

            Assert.IsFalse(module.TryApply(CombatImmediateHitRequest.CreateAllyDirectTarget(
                "ally", ally, Vector3.zero, Vector3.zero, 1, AttackVisualKind.SingleHit, false)));
            Assert.IsFalse(module.TryApply(CombatImmediateHitRequest.CreateAllyDirectTarget(
                "", enemy, Vector3.zero, Vector3.zero, 1, AttackVisualKind.SingleHit, false)));
            Assert.IsFalse(module.TryApply(CombatImmediateHitRequest.CreateAllyDirectTarget(
                "ally", enemy, Vector3.zero, Vector3.zero, 1, AttackVisualKind.SingleHit, false)));
            Assert.AreEqual(0, ally.DispatchCount);
            Assert.AreEqual(0, enemy.DispatchCount);

            GameObject pauseObject = CreateObject("ResultLock");
            RunPauseController pause = pauseObject.AddComponent<RunPauseController>();
            pause.Initialize();
            pause.MarkRunEnded();
            RecordingTarget lockedTarget = CreateTarget("LockedEnemy", CombatImmediateHitFaction.Enemy);

            Assert.IsFalse(module.TryApply(CombatImmediateHitRequest.CreateAllyDirectTarget(
                "ally", lockedTarget, Vector3.zero, Vector3.zero, 1, AttackVisualKind.SingleHit, false)));
            Assert.AreEqual(0, lockedTarget.DispatchCount);
        }

        [Test]
        public void ImmediateHit_TargetRejectionDoesNotRaiseAppliedEvent()
        {
            RecordingTarget enemy = CreateTarget("RejectingEnemy", CombatImmediateHitFaction.Enemy);
            enemy.AcceptsHit = false;
            CombatImmediateHitModule module = new CombatImmediateHitModule();
            int appliedCount = 0;
            module.Applied += _ => appliedCount++;

            bool applied = module.TryApply(CombatImmediateHitRequest.CreateAllyDirectTarget(
                "cleric",
                enemy,
                Vector3.left,
                Vector3.right,
                12,
                AttackVisualKind.SingleHit,
                true));

            Assert.IsFalse(applied);
            Assert.AreEqual(1, enemy.DispatchCount);
            Assert.AreEqual(0, appliedCount);
        }

        [Test]
        public void PersistentField_TicksTargetsByPublicCadenceAndResetClearsFields()
        {
            RecordingTarget target = CreateTarget("Target", new Vector3(1.0f, 0.0f, 0.0f));
            RecordingTargetSource source = new RecordingTargetSource(target);
            CombatPersistentFieldModule module = new CombatPersistentFieldModule(source, new CombatImmediateHitModule());

            Assert.IsTrue(module.TrySpawn(CreateRequest(10), 0.0f));
            module.Tick(0.99f);
            Assert.AreEqual(0, target.DispatchCount);
            module.Tick(1.0f);
            module.Tick(1.0f);
            module.Tick(2.0f);
            module.Tick(3.0f);

            Assert.AreEqual(3, target.DispatchCount);
            Assert.AreEqual("fire_mage", target.LastRequest.SourceId);
            Assert.AreEqual(5, target.LastRequest.Damage);
            Assert.IsFalse(target.LastRequest.SpawnAllyFeedback);
            module.Reset();
            Assert.AreEqual(0, module.ActiveFieldCount);
        }

        [Test]
        public void PersistentField_ThirdSameOwnerFieldReplacesOldestWithoutAffectingOtherOwner()
        {
            RecordingTargetSource source = new RecordingTargetSource();
            CombatPersistentFieldModule module = new CombatPersistentFieldModule(source, new CombatImmediateHitModule());

            Assert.IsTrue(module.TrySpawn(CreateRequest(10), 0.0f));
            Assert.IsTrue(module.TrySpawn(CreateRequest(10), 0.1f));
            Assert.IsTrue(module.TrySpawn(CreateRequest(20), 0.2f));
            Assert.IsTrue(module.TrySpawn(CreateRequest(10), 0.3f));

            Assert.AreEqual(3, module.ActiveFieldCount);
            Assert.AreEqual(2, module.GetActiveFieldCount(10, "fire_mage"));
            Assert.AreEqual(1, module.GetActiveFieldCount(20, "fire_mage"));
        }

        [Test]
        public void PersistentField_IgnitionDamagesAndExtendsActiveFieldsWithoutSpawningNewFields()
        {
            RecordingTarget target = CreateTarget("IgnitionTarget", new Vector3(0.5f, 0.0f, 0.0f));
            CombatPersistentFieldModule module = new CombatPersistentFieldModule(
                new RecordingTargetSource(target),
                new CombatImmediateHitModule());
            Assert.IsTrue(module.TrySpawn(CreateRequest(10), 0.0f));

            CombatPersistentFieldIgnitionRequest ignition = CombatPersistentFieldIgnitionRequest.CreateAllyIgnition(
                "fire_sage_active_field_ignition",
                "promotion_fire_sage_ignition_v1",
                "fire_mage",
                Vector3.zero,
                range: 5.0f,
                damage: 1,
                durationExtension: 1.0f,
                maxFields: 2);
            Assert.IsTrue(module.TryIgnite(ignition, 0.5f, out int ignited));
            Assert.AreEqual(1, ignited);
            Assert.AreEqual(1, module.ActiveFieldCount);
            Assert.AreEqual(1, target.DispatchCount);
            Assert.AreEqual("fire_sage_active_field_ignition", target.LastRequest.SourceId);

            module.Tick(3.5f);
            Assert.AreEqual(1, module.ActiveFieldCount);
            module.Tick(4.1f);
            Assert.AreEqual(0, module.ActiveFieldCount);
        }

        private LocalDataProvider CreateProjectProvider()
        {
            TestAssetService assets = new TestAssetService();
            TextAsset gameData = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_LizzoPV/Gameplay/Run/Data/GameData.xml");
            Assert.IsNotNull(gameData);
            assets.Register("PlayerData.xml", gameData);
            return new LocalDataProvider(assets);
        }

        private GameObject CreateObject(string name)
        {
            GameObject gameObject = new GameObject(name);
            _objects.Add(gameObject);
            return gameObject;
        }

        private RecordingTarget CreateTarget(string name, CombatImmediateHitFaction faction)
        {
            RecordingTarget target = CreateObject(name).AddComponent<RecordingTarget>();
            target.Faction = faction;
            return target;
        }

        private RecordingTarget CreateTarget(string name, Vector3 position)
        {
            RecordingTarget target = CreateObject(name).AddComponent<RecordingTarget>();
            target.transform.position = position;
            target.Faction = CombatImmediateHitFaction.Enemy;
            return target;
        }

        private static void AssertTargetAreaSetup(
            CompanionTargetAreaCombatSetup setup,
            string sourceId,
            int damage,
            float period,
            float range,
            float radius,
            int maxTargets,
            float castDelay,
            float retry)
        {
            Assert.AreEqual(sourceId, setup.SourceId);
            Assert.AreEqual(damage, setup.Damage);
            Assert.AreEqual(period, setup.Period);
            Assert.AreEqual(range, setup.Range);
            Assert.AreEqual(radius, setup.Radius);
            Assert.AreEqual(maxTargets, setup.MaxTargets);
            Assert.AreEqual(castDelay, setup.CastDelay);
            Assert.AreEqual(retry, setup.NoTargetRetrySeconds);
        }

        private static void AssertRangedSupportSetup(
            CompanionRangedSupportCombatSetup setup,
            string sourceId,
            int primaryDamage,
            float primaryPeriod,
            float primaryRange,
            int secondaryHeal,
            float secondaryPeriod,
            float secondaryRange)
        {
            Assert.AreEqual(sourceId, setup.Primary.SourceId);
            Assert.AreEqual(AllyAttackStyle.TargetedProjectile, setup.Primary.AttackStyle);
            Assert.AreEqual(primaryDamage, setup.Primary.Damage);
            Assert.AreEqual(primaryPeriod, setup.Primary.Period);
            Assert.AreEqual(primaryRange, setup.Primary.Range);
            Assert.AreEqual(1, setup.Primary.MaxTargets);
            Assert.AreEqual(0.15f, setup.Primary.NoTargetRetrySeconds);
            Assert.AreEqual(secondaryHeal, setup.SecondaryHealAmount);
            Assert.AreEqual(secondaryPeriod, setup.SecondaryPeriod);
            Assert.AreEqual(secondaryRange, setup.SecondaryRange);
            Assert.AreEqual(1, setup.SecondaryMaxTargets);
            Assert.AreEqual(0.15f, setup.SecondaryNoTargetRetrySeconds);
        }

        private static readonly MeleeProfileCase[] MeleeProfiles =
        {
            new MeleeProfileCase("shield_guard", AllyAttackStyle.ForwardPush, 6, 1.4f, 1.2f, 60.0f, 0.5f, 3, 0.15f),
            new MeleeProfileCase("sword_soldier", AllyAttackStyle.ForwardSlash, 12, 1.0f, 1.6f, 60.0f, 0.0f, 5, 0.15f),
        }
        ;

        private static readonly TargetAreaProfileCase[] TargetAreaProfiles =
        {
            new TargetAreaProfileCase("bombardier", 16, 2.2f, 5.0f, 1.6f, 6, 0.5f, 0.15f),
            new TargetAreaProfileCase("field_herbalist", 8, 1.4f, 5.0f, 1.2f, 4, 0.25f, 0.15f),
        }
        ;

        private static readonly RangedSupportProfileCase[] RangedSupportProfiles =
        {
            new RangedSupportProfileCase("cleric", 5, 1.6f, 4.5f, 8, 4.0f, 4.0f),
        }
        ;

        private static CombatPersistentFieldRequest CreateRequest(int ownerId, int maxTargets = 8)
        {
            return CombatPersistentFieldRequest.CreateAllyDamage(
                "fire_mage",
                ownerId,
                Vector3.zero,
                damage: 5,
                radius: 1.6f,
                tickInterval: 1.0f,
                duration: 3.0f,
                maxTargets: maxTargets,
                maxActiveFields: 2);
        }

        private readonly struct MeleeProfileCase
        {
            public MeleeProfileCase(string sourceId, AllyAttackStyle attackStyle, int damage, float period, float range, float angle, float knockback,
                int maxTargets, float retry)
            {
                SourceId = sourceId;
                AttackStyle = attackStyle;
                Damage = damage;
                Period = period;
                Range = range;
                Angle = angle;
                Knockback = knockback;
                MaxTargets = maxTargets;
                Retry = retry;
            }

            public string SourceId {
                get;
            }
            public AllyAttackStyle AttackStyle {
                get;
            }
            public int Damage {
                get;
            }
            public float Period {
                get;
            }
            public float Range {
                get;
            }
            public float Angle {
                get;
            }
            public float Knockback {
                get;
            }
            public int MaxTargets {
                get;
            }
            public float Retry {
                get;
            }
        }

        private readonly struct TargetAreaProfileCase
        {
            public TargetAreaProfileCase(string sourceId, int damage, float period, float range, float radius, int maxTargets, float castDelay, float retry)
            {
                SourceId = sourceId;
                Damage = damage;
                Period = period;
                Range = range;
                Radius = radius;
                MaxTargets = maxTargets;
                CastDelay = castDelay;
                Retry = retry;
            }

            public string SourceId {
                get;
            }
            public int Damage {
                get;
            }
            public float Period {
                get;
            }
            public float Range {
                get;
            }
            public float Radius {
                get;
            }
            public int MaxTargets {
                get;
            }
            public float CastDelay {
                get;
            }
            public float Retry {
                get;
            }
        }

        private readonly struct RangedSupportProfileCase
        {
            public RangedSupportProfileCase(string sourceId, int primaryDamage, float primaryPeriod, float primaryRange, int secondaryHeal,
                float secondaryPeriod, float secondaryRange)
            {
                SourceId = sourceId;
                PrimaryDamage = primaryDamage;
                PrimaryPeriod = primaryPeriod;
                PrimaryRange = primaryRange;
                SecondaryHeal = secondaryHeal;
                SecondaryPeriod = secondaryPeriod;
                SecondaryRange = secondaryRange;
            }

            public string SourceId {
                get;
            }
            public int PrimaryDamage {
                get;
            }
            public float PrimaryPeriod {
                get;
            }
            public float PrimaryRange {
                get;
            }
            public int SecondaryHeal {
                get;
            }
            public float SecondaryPeriod {
                get;
            }
            public float SecondaryRange {
                get;
            }
        }

        private sealed class RecordingTargetSource : ICombatPersistentFieldTargetSource
        {
            private readonly RecordingTarget[] _targets;

            public RecordingTargetSource(params RecordingTarget[] targets)
            {
                _targets = targets;
            }

            public void CollectTargets(Vector3 center, List<CombatPersistentFieldTarget> destination)
            {
                for (int i = 0;
                i < _targets.Length;
                i++)
                {
                    RecordingTarget target = _targets[i];
                    if (target != null)
                        destination.Add(new CombatPersistentFieldTarget(target, target.transform.position, target.GetInstanceID()));
                }
            }
        }

        private sealed class RecordingTarget : MonoBehaviour, ICombatImmediateHitTarget
        {
            public CombatImmediateHitFaction Faction = CombatImmediateHitFaction.Enemy;
            public bool IsAlive = true;
            public bool AcceptsHit = true;
            public int DispatchCount {
                get;
                private set;
            }
            public CombatImmediateHitRequest LastRequest {
                get;
                private set;
            }

            CombatImmediateHitFaction ICombatImmediateHitTarget.Faction => Faction;
            bool ICombatImmediateHitTarget.IsAlive => IsAlive;

            bool ICombatImmediateHitTarget.TryReceiveImmediateHit(in CombatImmediateHitRequest request)
            {
                DispatchCount++;
                LastRequest = request;
                return AcceptsHit;
            }
        }
    }
}
