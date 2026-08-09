using IDisposable = System.IDisposable;
using System.Collections.Generic;
using System.Reflection;
using Lizzo.PV.Combat;
using Lizzo.PV.Combat.Fields;
using Lizzo.PV.Combat.Projectiles;
using Lizzo.PV.Combat.Summons;
using Lizzo.PV.Data;
using Lizzo.PV.Flow;
using Lizzo.PV.Legion;
using Lizzo.PV.Legion.Combat.Attacks;
using Lizzo.PV.P0.Presentation;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class CompanionOwnedSupportCombatTests
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
        public void BattleApothecary_BounceSetupAndSelectorPreserveBoundedOwnedTargetContract()
        {
            CompanionRangedSupportCombatSetup setup = ResolveHerbalistSetup()
                .WithPromotedBattleApothecaryHeal()
                .WithPromotedBattleApothecaryBounce();

            Assert.IsTrue(setup.HasPrimaryProjectileBounce);
            Assert.AreEqual("field_herbalist", setup.PrimaryProjectileBounce.SourceId);
            Assert.AreEqual(1.8f, setup.PrimaryProjectileBounce.Radius);
            Assert.AreEqual(1, setup.PrimaryProjectileBounce.MaxTargets);
            Assert.AreEqual(0.60f, setup.PrimaryProjectileBounce.DamageRatio);
            Assert.AreEqual(7, setup.PrimaryProjectileBounce.ResolveDamage(12));

            List<ProjectileBounceTargetCandidate> candidates = new List<ProjectileBounceTargetCandidate>
            {
                new ProjectileBounceTargetCandidate(null, new Vector3(1.0f, 0.0f), 20, true),
                new ProjectileBounceTargetCandidate(null, new Vector3(1.0f, 0.0f), 10, true),
                new ProjectileBounceTargetCandidate(null, new Vector3(0.2f, 0.0f), 3, true),
                new ProjectileBounceTargetCandidate(null, new Vector3(0.3f, 0.0f), 4, true),
                new ProjectileBounceTargetCandidate(null, new Vector3(1.81f, 0.0f), 5, true),
                new ProjectileBounceTargetCandidate(null, new Vector3(0.5f, 0.0f), 6, false),
            }
            ;

            Assert.IsTrue(ProjectileBounceTargetSelector.TrySelect(candidates, Vector3.zero, 3, 4, 1.8f, out ProjectileBounceTargetCandidate result));
            Assert.AreEqual(10, result.InstanceId);
        }

        [Test]
        public void BattleApothecary_BaseAndPromotedRoutingKeepProxyOwnershipExplicit()
        {
            CompanionRangedSupportCombatSetup baseSetup = ResolveHerbalistSetup();
            Assert.IsFalse(baseSetup.HasPrimaryProjectileBounce);

            GameObject owner = CreateObject("BattleApothecaryBounceRouting", Vector3.zero);
            AllyCombat combat = owner.AddComponent<AllyCombat>();
            combat.SetCanonicalRangedSupportInfo(baseSetup.WithPromotedBattleApothecaryHeal());
            Assert.IsFalse(combat.HasPromotedProjectileBounce);

            combat.SetPromotedProjectileBounce(baseSetup
                .WithPromotedBattleApothecaryHeal()
                .WithPromotedBattleApothecaryBounce()
                .PrimaryProjectileBounce);
            Assert.IsTrue(combat.HasPromotedProjectileBounce);
        }

        [Test]
        public void ClericHeal_RepairsCommanderAndThenLowestCompanionUntilPartyIsFull()
        {
            using ServiceTestFixture fixture = CreateClericFixture(out ClericVisualScope scope);
            using (scope)
            {
                PlayerController player = CreatePlayer(fixture, 50);
                Assert.IsTrue(ClericHealAttack.TryResolve(fixture.Run.Party, 10));
                Assert.AreEqual(60, player.Hp);

                player = CreatePlayer(fixture, 80);
                CompanionRuntime companion = AddCompanion(fixture, "shield_guard", 40);
                int castCount = 0;
                while (ClericHealAttack.TryResolve(fixture.Run.Party, 10))
                    castCount++;

                Assert.AreEqual(8, castCount);
                Assert.AreEqual(player.MaxHp, player.Hp);
                Assert.AreEqual(companion.MaxHp, companion.Hp);
            }
        }

        [Test]
        public void ClericHeal_FullPartyDoesNotResolveAHeal()
        {
            using ServiceTestFixture fixture = CreateClericFixture(out ClericVisualScope scope);
            using (scope)
            {
                PlayerController player = CreatePlayer(fixture, 100);
                CompanionRuntime companion = AddCompanion(fixture, "shield_guard", 80);
                SetProperty(companion, "Hp", companion.MaxHp);

                Assert.IsFalse(ClericHealAttack.TryResolve(fixture.Run.Party, 10));
                Assert.AreEqual(player.MaxHp, player.Hp);
                Assert.AreEqual(companion.MaxHp, companion.Hp);
            }
        }

        [Test]
        public void ClericHeal_DownedCompanionIsRecoveredBeforeNormalTarget()
        {
            using ServiceTestFixture fixture = CreateClericFixture(out ClericVisualScope scope);
            using (scope)
            {
                PlayerController player = CreatePlayer(fixture, 50);
                CompanionRuntime downed = AddCompanion(fixture, "shield_guard", 0, true);
                CompanionRuntime damaged = AddCompanion(fixture, "sword_soldier", 20);

                Assert.IsTrue(ClericHealAttack.TryResolve(fixture.Run.Party, 10));
                Assert.IsFalse(downed.IsDown);
                Assert.Greater(downed.Hp, 0);
                Assert.AreEqual(50, player.Hp);
                Assert.AreEqual(20, damaged.Hp);
            }
        }

        [Test]
        public void ClericHeal_NoReviveUsesLowestLivingTargetWithinRange()
        {
            using ServiceTestFixture fixture = CreateClericFixture(out ClericVisualScope scope);
            using (scope)
            {
                PlayerController player = CreatePlayer(fixture, 50);
                player.transform.position = Vector3.zero;
                CompanionRuntime downed = AddCompanion(fixture, "shield_guard", 0, true);
                downed.transform.position = new Vector3(0.0f, 1.0f, 0.0f);
                CompanionRuntime damaged = AddCompanion(fixture, "sword_soldier", 20);
                damaged.transform.position = new Vector3(1.0f, 0.0f, 0.0f);
                CompanionRuntime outsideRange = AddCompanion(fixture, "cleric", 10);
                outsideRange.transform.position = new Vector3(5.0f, 0.0f, 0.0f);

                Assert.IsTrue(ClericHealAttack.TryResolveNoRevive(fixture.Run.Party, Vector3.zero, 8, 4.0f));
                Assert.IsTrue(downed.IsDown);
                Assert.AreEqual(0, downed.Hp);
                Assert.AreEqual(28, damaged.Hp);
                Assert.AreEqual(50, player.Hp);
                Assert.AreEqual(10, outsideRange.Hp);
            }
        }

        [TestCase(0.70f, 14, 34, 42)]
        [TestCase(0.60f, 6, 26, 36)]
        public void ClericHeal_PromotedNoReviveHealsTwoLivingTargetsWithConfiguredSecondRatio(
            float secondTargetRatio,
            int healAmount,
            int expectedLowestHp,
            int expectedSecondHp)
        {
            using ServiceTestFixture fixture = CreateClericFixture(out ClericVisualScope scope);
            using (scope)
            {
                CreatePlayer(fixture, 100).transform.position = Vector3.zero;
                CompanionRuntime lowest = AddCompanion(fixture, "shield_guard", 20);
                lowest.transform.position = new Vector3(1.0f, 0.0f, 0.0f);
                CompanionRuntime secondLowest = AddCompanion(fixture, "sword_soldier", 32);
                secondLowest.transform.position = new Vector3(2.0f, 0.0f, 0.0f);
                CompanionRuntime thirdLowest = AddCompanion(fixture, "cleric", 48);
                thirdLowest.transform.position = new Vector3(3.0f, 0.0f, 0.0f);
                CompanionRuntime downed = AddCompanion(fixture, "shield_guard", 0, true);
                downed.transform.position = new Vector3(1.0f, 1.0f, 0.0f);
                CompanionRuntime outsideRange = AddCompanion(fixture, "sword_soldier", 10);
                outsideRange.transform.position = new Vector3(5.0f, 0.0f, 0.0f);
                List<ClericHealAttack.SupportHealTarget> targets = new List<ClericHealAttack.SupportHealTarget>(2);

                Assert.IsTrue(ClericHealAttack.TryResolveNoRevive(
                    fixture.Run.Party, Vector3.zero, healAmount, 4.0f, 2, secondTargetRatio, targets));
                Assert.AreEqual(2, targets.Count);
                Assert.AreEqual(expectedLowestHp, lowest.Hp);
                Assert.AreEqual(expectedSecondHp, secondLowest.Hp);
                Assert.AreEqual(48, thirdLowest.Hp);
                Assert.IsTrue(downed.IsDown);
                Assert.AreEqual(0, downed.Hp);
                Assert.AreEqual(10, outsideRange.Hp);
            }
        }

        [Test]
        public void Necromancer_MapsCurseAndPersonalSkeletonContracts()
        {
            LocalDataProvider data = CreateProjectProvider();
            Assert.IsTrue(data.InitializeAsync().GetAwaiter().GetResult().Succeeded);
            CompanionProjectileCombatResolver projectileResolver = new CompanionProjectileCombatResolver(data);
            Assert.IsTrue(projectileResolver.TryResolve("necromancer", 1.0f, out CompanionProjectileCombatSetup projectile));
            Assert.AreEqual("necromancer", projectile.SourceId);
            Assert.AreEqual(8, projectile.Damage);
            Assert.AreEqual(3.0f, projectile.Period);
            Assert.AreEqual(5.0f, projectile.Range);
            Assert.AreEqual(1, projectile.MaxTargets);
            Assert.AreEqual(0.15f, projectile.NoTargetRetrySeconds);

            CompanionPersonalSummonResolver summonResolver = new CompanionPersonalSummonResolver(data);
            Assert.IsTrue(summonResolver.TryResolve("necromancer", out CompanionPersonalSummonSetup summon));
            AssertPersonalSkeletonSetup(summon);
        }

        [Test]
        public void Necromancer_ThresholdRequiresReleaseBeforeNextPersonalSummon()
        {
            CountableKillThresholdState state = new CountableKillThresholdState();
            state.Configure(15, 1);
            Assert.IsFalse(state.RecordKill(false));
            for (int i = 0;
            i < 14;
            i++)
                Assert.IsFalse(state.RecordKill(true));
            Assert.IsTrue(state.RecordKill(true));
            Assert.AreEqual(1, state.ActiveCount);
            Assert.IsFalse(state.RecordKill(true));
            Assert.IsTrue(state.ReleaseOne());
            for (int i = 0;
            i < 14;
            i++)
                Assert.IsFalse(state.RecordKill(true));
            Assert.IsTrue(state.RecordKill(true));
            state.Reset();
            Assert.AreEqual(0, state.ActiveCount);
            Assert.AreEqual(0, state.PendingCountableKills);
        }

        [Test]
        public void Necromancer_RejectsOtherOwnersAndPreservesFallbackPersonalIdentity()
        {
            LocalDataProvider projectData = CreateProjectProvider();
            Assert.IsTrue(projectData.InitializeAsync().GetAwaiter().GetResult().Succeeded);
            CompanionPersonalSummonResolver resolver = new CompanionPersonalSummonResolver(projectData);
            Assert.IsFalse(resolver.TryResolve("skeleton_bomber", out _));
            Assert.IsTrue(resolver.TryResolve("necromancer", out CompanionPersonalSummonSetup summon));
            Assert.AreNotEqual(summon.SummonId, summon.DistinctFromSummonId);

            LogAssert.Expect(LogType.Error, "[LocalDataProvider] Local data asset was not available. address=PlayerData.xml");
            LocalDataProvider fallback = new LocalDataProvider(new TestAssetService());
            Assert.IsTrue(fallback.InitializeAsync().GetAwaiter().GetResult().Succeeded);
            Assert.AreEqual(1, fallback.CompanionSummons.Count);
            Assert.AreEqual("UNIT_PERSONAL_SKELETON_01", fallback.GetCompanionSummon("UNIT_PERSONAL_SKELETON_01").Id);
            Assert.IsTrue(new CompanionPersonalSummonResolver(fallback).TryResolve("necromancer", out _));
        }

        [Test]
        public void FalconOwnedProxy_MapsAndPreservesPrimaryProjectileBridge()
        {
            LocalDataProvider provider = CreateProjectProvider();
            Assert.IsTrue(provider.InitializeAsync().GetAwaiter().GetResult().Succeeded);
            CompanionOwnedProxyCombatResolver proxyResolver = new CompanionOwnedProxyCombatResolver(provider);
            Assert.IsTrue(proxyResolver.TryResolve("falcon_archer", out CompanionOwnedProxyCombatSetup proxy));
            Assert.AreEqual(6, proxy.Damage);
            Assert.AreEqual(5.5f, proxy.Range);
            Assert.AreEqual(1, proxy.MaxTargets);
            Assert.AreEqual(4, proxy.TriggerCount);
            Assert.IsFalse(proxyResolver.TryResolve("archer", out _));
            Assert.IsFalse(proxyResolver.TryResolve("cleric", out _));

            CompanionProjectileCombatResolver projectileResolver = new CompanionProjectileCombatResolver(provider);
            Assert.IsTrue(projectileResolver.TryResolve("falcon_archer", 1.0f, out CompanionProjectileCombatSetup projectile));
            GameObject owner = CreateObject("FalconCanonicalProxyBridge", Vector3.zero);
            AllyCombat combat = owner.AddComponent<AllyCombat>();
            combat.SetCanonicalProjectileWithProxyInfo(projectile, proxy);
            Assert.IsTrue(combat.HasOwnedProxyAssist);
            Assert.AreEqual(AllyAttackStyle.TargetedProjectile, combat.AttackStyle);
            Assert.AreEqual("falcon_archer", combat.CombatSourceId);
            Assert.AreEqual(9, combat.Damage);
            Assert.AreEqual(5.5f, combat.AttackRange);
        }

        [Test]
        public void FalconOwnedProxy_TriggersEveryFourthSuccessfulActionAndResets()
        {
            SuccessfulActionCounter counter = new SuccessfulActionCounter();
            counter.Configure(4);
            Assert.IsFalse(counter.RecordSuccess());
            Assert.IsFalse(counter.RecordSuccess());
            Assert.IsFalse(counter.RecordSuccess());
            Assert.AreEqual(3, counter.CurrentCount);
            Assert.IsTrue(counter.RecordSuccess());
            Assert.AreEqual(0, counter.CurrentCount);
            Assert.IsFalse(counter.RecordSuccess());
            counter.Reset();
            Assert.AreEqual(0, counter.CurrentCount);
        }

        [Test]
        public void PersonalSkeletonPrefab_PreservesNonCompanionRuntimeContract()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_LizzoPV/Gameplay/Legion/Prefabs/Characters/Supports/PersonalSkeletonSummon.prefab");
            Assert.IsNotNull(prefab);
            PersonalSummonRuntime runtime = prefab.GetComponent<PersonalSummonRuntime>();
            Assert.IsNotNull(runtime);
            Assert.IsNotNull(runtime.GetComponent<Rigidbody2D>());
            Assert.IsNotNull(runtime.CombatCollider);
            Assert.IsFalse(runtime.IsCompanionOwned);
            Assert.IsFalse(runtime.HasRosterIdentity);
            Assert.IsFalse(runtime.HasFamilyTag);
            Assert.AreEqual("Visual", prefab.transform.GetChild(0).name);
        }

        [Test]
        public void PersonalSummonModule_SpawnsAtOwnerCapsAndUsesNearestCadenceTarget()
        {
            GameObject owner = CreateObject("Owner", Vector3.zero);
            RecordingEnemy farther = CreateEnemy("Far", new Vector3(0.9f, 0.0f, 0.0f));
            RecordingEnemy nearer = CreateEnemy("Near", new Vector3(0.5f, 0.0f, 0.0f));
            RecordingFactory factory = new RecordingFactory(LoadPersonalSkeletonPrefab());
            CompanionPersonalSummonModule module = CreatePersonalModule(factory, new RecordingTargetSource(farther, nearer));
            PersonalSummonSpawnRequest request = CreateRequest(owner.transform);

            Assert.IsTrue(module.TrySpawn(request, 0.0f));
            Assert.IsFalse(module.TrySpawn(request, 0.0f));
            Assert.AreEqual(1, module.ActiveCount);
            Assert.AreEqual(owner.transform.position, factory.LastSpawn.transform.position);
            module.Tick(0.0f, 0.2f);
            Assert.AreEqual(1, nearer.HitCount);
            Assert.AreEqual(0, farther.HitCount);
            Assert.AreEqual(4, nearer.LastRequest.Damage);
            Assert.AreEqual("necromancer:UNIT_PERSONAL_SKELETON_01", nearer.LastRequest.SourceId);
            module.Tick(1.29f, 0.2f);
            Assert.AreEqual(1, nearer.HitCount);
            module.Tick(1.3f, 0.2f);
            Assert.AreEqual(2, nearer.HitCount);
        }

        [Test]
        public void PersonalSummonModule_ReleaseOnDeathAndFailedSpawnLeavesNoActor()
        {
            GameObject owner = CreateObject("Owner", Vector3.zero);
            RecordingEnemy target = CreateEnemy("Target", Vector3.zero);
            RecordingFactory factory = new RecordingFactory(LoadPersonalSkeletonPrefab());
            CompanionPersonalSummonModule module = CreatePersonalModule(factory, new RecordingTargetSource(target));
            Assert.IsTrue(module.TrySpawn(CreateRequest(owner.transform), 0.0f));
            PersonalSummonRuntime runtime = factory.LastSpawn.GetComponent<PersonalSummonRuntime>();
            Assert.IsTrue(new CombatImmediateHitModule().TryApply(CombatImmediateHitRequest.CreateEnemyContact(
                "test_enemy", runtime, Vector3.zero, Vector3.right, 18, "contact", RetroVfxKind.PlayerDamaged)));
            module.Tick(0.1f, 0.1f);
            Assert.AreEqual(0, module.ActiveCount);
            Assert.AreEqual(1, factory.ReleaseCount);
            factory.ReturnNull = true;
            Assert.IsFalse(module.TrySpawn(CreateRequest(owner.transform), 1.0f));
            Assert.AreEqual(0, module.ActiveCount);
            module.Reset();
            Assert.AreEqual(0, module.ActiveCount);
        }

        [Test]
        public void PersonalSummonModule_UsesStableOwnerKeyAcrossPromotionAndCap()
        {
            GameObject firstOrigin = CreateObject("FirstOrigin", Vector3.zero);
            GameObject promotedOrigin = CreateObject("PromotedOrigin", new Vector3(2.0f, 0.0f, 0.0f));
            RecordingFactory factory = new RecordingFactory(LoadPersonalSkeletonPrefab());
            CompanionPersonalSummonModule module = CreatePersonalModule(factory, new RecordingTargetSource());
            PersonalSummonSpawnRequest baseRequest = CreateRequest("squad_03", firstOrigin.transform, 1);
            PersonalSummonSpawnRequest promotedRequest = CreateRequest("squad_03", promotedOrigin.transform, 2);

            Assert.IsTrue(module.TrySpawn(baseRequest, 0.0f));
            Assert.AreEqual(1, module.GetActiveCount("squad_03", "necromancer:UNIT_PERSONAL_SKELETON_01"));
            Assert.IsTrue(module.TrySpawn(promotedRequest, 0.0f));
            Assert.AreEqual(promotedOrigin.transform.position, factory.LastSpawn.transform.position);
            Assert.AreEqual(2, module.GetActiveCount("squad_03", "necromancer:UNIT_PERSONAL_SKELETON_01"));
            Assert.IsFalse(module.TrySpawn(promotedRequest, 0.0f));
        }

        [Test]
        public void RunServices_OwnsOnePersonalSummonModuleAndRegistryDoesNot()
        {
            using ServiceTestFixture fixture = new ServiceTestFixture();
            Assert.IsNotNull(fixture.Run.PersonalSummonModule);
            Assert.IsNull(typeof(RuntimeObjectRegistry).GetProperty("PersonalSummonModule"));
        }

        [Test]
        public void WolfOwnedProxy_MapsCanonicalProfileAndSelectsNearestEligibleTarget()
        {
            LocalDataProvider provider = CreateProjectProvider();
            Assert.IsTrue(provider.InitializeAsync().GetAwaiter().GetResult().Succeeded);
            CompanionWolfOwnedProxyCombatResolver resolver = new CompanionWolfOwnedProxyCombatResolver(provider);
            Assert.IsTrue(resolver.TryResolve("wolf_tamer", out CompanionWolfOwnedProxyCombatSetup setup));
            Assert.AreEqual("wolf_tamer", setup.SourceId);
            Assert.AreEqual(10, setup.Damage);
            Assert.AreEqual(4.0f, setup.Period);
            Assert.AreEqual(4.0f, setup.SearchRange);
            Assert.AreEqual(0.8f, setup.Duration);
            Assert.AreEqual(1, setup.MaxTargets);
            Assert.AreEqual(1, setup.MaxActive);
            Assert.AreEqual(0.15f, setup.NoTargetRetrySeconds);
            Assert.IsFalse(resolver.TryResolve("falcon_archer", out _));

            List<WolfOwnedProxyTargetCandidate> candidates = new List<WolfOwnedProxyTargetCandidate>
            {
                new WolfOwnedProxyTargetCandidate(30, new Vector3(3.0f, 0.0f), true),
                new WolfOwnedProxyTargetCandidate(20, new Vector3(2.0f, 0.0f), true),
                new WolfOwnedProxyTargetCandidate(10, new Vector3(2.0f, 0.0f), true),
                new WolfOwnedProxyTargetCandidate(1, new Vector3(1.0f, 0.0f), false),
            }
            ;
            Assert.IsTrue(WolfOwnedProxyTargetSelector.TrySelectNearest(Vector3.zero, 4.0f, candidates, out WolfOwnedProxyTargetCandidate target));
            Assert.AreEqual(10, target.InstanceId);
            Assert.IsFalse(WolfOwnedProxyTargetSelector.TrySelectNearest(Vector3.zero, 1.0f, candidates, out _));
        }

        [Test]
        public void WolfOwnedProxy_StateTransitionsDashHitReturnAndReset()
        {
            WolfOwnedProxyState state = new WolfOwnedProxyState();
            Vector3 owner = Vector3.zero;
            Vector3 target = new Vector3(4.0f, 0.0f);
            Assert.IsTrue(state.TryBegin(owner, target, 0.0f, 0.8f));
            Assert.IsFalse(state.TryBegin(owner, target, 0.0f, 0.8f));
            Assert.AreEqual(WolfOwnedProxyPhase.Dash, state.Phase);
            Assert.IsTrue(state.Advance(0.2f, out Vector3 dashPosition, out bool shouldDamage));
            Assert.IsFalse(shouldDamage);
            Assert.AreEqual(2.0f, dashPosition.x);
            Assert.IsTrue(state.Advance(0.4f, out Vector3 impactPosition, out shouldDamage));
            Assert.IsTrue(shouldDamage);
            Assert.AreEqual(4.0f, impactPosition.x);
            Assert.AreEqual(WolfOwnedProxyPhase.Return, state.Phase);
            Assert.IsTrue(state.Advance(0.6f, out Vector3 returnPosition, out shouldDamage));
            Assert.IsFalse(shouldDamage);
            Assert.AreEqual(2.0f, returnPosition.x);
            Assert.IsFalse(state.Advance(0.8f, out Vector3 finishedPosition, out shouldDamage));
            Assert.AreEqual(owner, finishedPosition);
            Assert.AreEqual(WolfOwnedProxyPhase.Inactive, state.Phase);
            Assert.IsTrue(state.TryBegin(owner, target, 1.0f, 0.8f));
            state.Reset();
            Assert.IsFalse(state.IsActive);
            Assert.AreEqual(WolfOwnedProxyPhase.Inactive, state.Phase);
        }

        [Test]
        public void WolfOwnedProxy_PromotedTwoHitAndBaseOneHitStatesRemainDistinct()
        {
            WolfOwnedProxyState promoted = new WolfOwnedProxyState();
            Assert.IsTrue(promoted.TryBegin(Vector3.zero, Vector3.right, 42, 0.0f, 0.8f, 2));
            promoted.Advance(0.5f, out _, out bool firstHit);
            Assert.IsTrue(firstHit);
            Assert.AreEqual(42, promoted.LockedTargetInstanceId);
            Assert.AreEqual(1, promoted.PendingHitCount);
            Assert.IsFalse(promoted.TryConsumeLockedTargetHit(false));
            Assert.AreEqual(WolfOwnedProxyPhase.Return, promoted.Phase);
            promoted.Reset();
            Assert.AreEqual(0, promoted.PendingHitCount);

            WolfOwnedProxyState baseState = new WolfOwnedProxyState();
            Assert.IsTrue(baseState.TryBegin(Vector3.zero, Vector3.right, 7, 0.0f, 0.8f, 1));
            baseState.Advance(0.5f, out _, out bool hit);
            Assert.IsTrue(hit);
            Assert.AreEqual(0, baseState.PendingHitCount);
            Assert.AreEqual(WolfOwnedProxyPhase.Return, baseState.Phase);
        }

        [Test]
        public void Wraith_MapsPrimaryDefenseAndMitigationCadence()
        {
            LocalDataProvider provider = CreateProjectProvider();
            Assert.IsTrue(provider.InitializeAsync().GetAwaiter().GetResult().Succeeded);
            Assert.IsTrue(new CompanionMeleeCombatResolver(provider).TryResolveWraithMeleeDefense(1.0f, out CompanionWraithMeleeDefenseSetup setup));
            Assert.AreEqual(AllyAttackStyle.ForwardSlash, setup.Melee.AttackStyle);
            Assert.AreEqual(14, setup.Melee.Damage);
            Assert.AreEqual(1.4f, setup.Melee.Period);
            Assert.AreEqual(1.2f, setup.Melee.Range);
            Assert.AreEqual(60.0f, setup.Melee.Angle);
            Assert.AreEqual(3, setup.Melee.MaxTargets);
            Assert.AreEqual(0.15f, setup.Melee.NoTargetRetrySeconds);
            Assert.AreEqual(0.60f, setup.PersonalDefense.IncomingDamageMultiplier);
            Assert.AreEqual(5.0f, setup.PersonalDefense.Period);
            Assert.AreEqual(1.2f, setup.PersonalDefense.Duration);

            PersonalDamageMitigationState state = new PersonalDamageMitigationState();
            state.Configure(new PersonalDamageMitigationSetup(0.60f, 5.0f, 1.2f), 0.0f);
            Assert.IsTrue(state.Advance(0.0f));
            Assert.AreEqual(6, state.ApplyToSelf(10));
            Assert.IsFalse(state.Advance(1.2f));
            Assert.AreEqual(10, state.ApplyToSelf(10));
            Assert.IsTrue(state.Advance(5.0f));
            state.ResetForOwnerDown(6.0f);
            Assert.IsFalse(state.IsActive);
            Assert.IsTrue(state.Advance(6.0f));
            Assert.AreEqual(6, state.ApplyToSelf(10));
        }

        [Test]
        public void StableSlotSummon_UsesRosterKeyDiscardsCapFullAndRequiresRelease()
        {
            using StableSlotFixture fixture = CreateStableSlotFixture(out GameObject origin);
            CountableKillAttribution attribution = CreateCompanionAttribution(101);
            for (int i = 0;
            i < 14;
            i++)
                Assert.IsFalse(fixture.Party.TryAdvanceNecromancerPersonalSummon(attribution, "squad_00", false, origin.transform));
            Assert.IsTrue(fixture.Party.TryAdvanceNecromancerPersonalSummon(attribution, "squad_00", false, origin.transform));
            Assert.AreEqual(1, fixture.Module.SpawnRequests.Count);
            Assert.AreEqual("squad_00", fixture.Module.SpawnRequests[0].OwnerKey);
            Assert.AreSame(origin.transform, fixture.Module.SpawnRequests[0].SpawnOrigin);
            fixture.Module.SetActive("squad_00", "necromancer:UNIT_PERSONAL_SKELETON_01", 1);
            for (int i = 0;
            i < 15;
            i++)
                Assert.IsFalse(fixture.Party.TryAdvanceNecromancerPersonalSummon(attribution, "squad_00", false, origin.transform));
            Assert.IsTrue(fixture.Party.TryGetNecromancerKillState("squad_00", out CountableKillThresholdState state));
            Assert.AreEqual(0, state.PendingCountableKills);
            fixture.Module.SetActive("squad_00", "necromancer:UNIT_PERSONAL_SKELETON_01", 0);
            for (int i = 0;
            i < 14;
            i++)
                Assert.IsFalse(fixture.Party.TryAdvanceNecromancerPersonalSummon(attribution, "squad_00", false, origin.transform));
            Assert.IsTrue(fixture.Party.TryAdvanceNecromancerPersonalSummon(attribution, "squad_00", false, origin.transform));
            Assert.AreEqual(2, fixture.Module.SpawnRequests.Count);
        }

        [Test]
        public void StableSlotSummon_PromotionPreservesStateAndIsolatesOwners()
        {
            using StableSlotFixture fixture = CreateStableSlotFixture(out GameObject origin);
            CountableKillAttribution attribution = CreateCompanionAttribution(202);
            for (int i = 0;
            i < 10;
            i++)
                fixture.Party.TryAdvanceNecromancerPersonalSummon(attribution, "squad_01", false, origin.transform);
            for (int i = 0;
            i < 5;
            i++)
                Assert.IsFalse(fixture.Party.TryAdvanceNecromancerPersonalSummon(attribution, "squad_02", true, origin.transform));
            Assert.IsTrue(fixture.Party.TryGetNecromancerKillState("squad_01", out CountableKillThresholdState first));
            Assert.AreEqual(10, first.PendingCountableKills);
            for (int i = 0;
            i < 4;
            i++)
                Assert.IsFalse(fixture.Party.TryAdvanceNecromancerPersonalSummon(attribution, "squad_01", true, origin.transform));
            Assert.IsTrue(fixture.Party.TryAdvanceNecromancerPersonalSummon(attribution, "squad_01", true, origin.transform));
            Assert.AreEqual(2, fixture.Module.SpawnRequests[0].ActiveCap);
            Assert.AreEqual("squad_01", fixture.Module.SpawnRequests[0].OwnerKey);
            Assert.IsTrue(fixture.Party.TryGetNecromancerKillState("squad_02", out CountableKillThresholdState second));
            Assert.AreEqual(5, second.PendingCountableKills);
        }

        [Test]
        public void StableSlotSummon_ResetClearsStateAndRejectsExcludedAttribution()
        {
            using StableSlotFixture fixture = CreateStableSlotFixture(out GameObject origin);
            CountableKillAttribution excluded = new CountableKillAttribution(1, "necromancer", CombatKillSourceCategory.PersonalSummon);
            Assert.IsFalse(fixture.Party.TryAdvanceNecromancerPersonalSummon(excluded, "squad_03", false, origin.transform));
            Assert.IsFalse(fixture.Party.TryGetNecromancerKillState("squad_03", out _));
            fixture.Party.TryAdvanceNecromancerPersonalSummon(CreateCompanionAttribution(1), "squad_03", false, origin.transform);
            Assert.IsTrue(fixture.Party.TryGetNecromancerKillState("squad_03", out _));
            fixture.Party.ResetRunState();
            Assert.IsFalse(fixture.Party.TryGetNecromancerKillState("squad_03", out _));
        }

        private CompanionRangedSupportCombatSetup ResolveHerbalistSetup()
        {
            LocalDataProvider provider = CreateProjectProvider();
            Assert.IsTrue(provider.InitializeAsync().GetAwaiter().GetResult().Succeeded);
            CompanionRangedSupportCombatResolver resolver = new CompanionRangedSupportCombatResolver(provider);
            Assert.IsTrue(resolver.TryResolve("field_herbalist", 1.0f, out CompanionRangedSupportCombatSetup setup));
            return setup;
        }

        private LocalDataProvider CreateProjectProvider()
        {
            TestAssetService assets = new TestAssetService();
            TextAsset gameData = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_LizzoPV/Gameplay/Run/Data/GameData.xml");
            Assert.IsNotNull(gameData);
            assets.Register("PlayerData.xml", gameData);
            return new LocalDataProvider(assets);
        }

        private ServiceTestFixture CreateClericFixture(out ClericVisualScope scope)
        {
            ServiceTestFixture fixture = new ServiceTestFixture();
            fixture.Data.InitializeAsync().GetAwaiter().GetResult();
            ClericHealTestVisualFactory factory = new ClericHealTestVisualFactory();
            AttackVisual.Configure(factory);
            FloatingDamageText.Configure(factory);
            RetroVfx.Configure(fixture.App.Assets, fixture.Run.Factory);
            LogAssert.ignoreFailingMessages = true;
            scope = new ClericVisualScope(factory);
            return fixture;
        }

        private PlayerController CreatePlayer(ServiceTestFixture fixture, int hp)
        {
            PlayerController player = CreateObject("TestCommander", Vector3.zero).AddComponent<PlayerController>();
            player.MaxHp = 100;
            player.Hp = hp;
            fixture.Run.Registry.RegisterPlayer(player);
            return player;
        }

        private CompanionRuntime AddCompanion(ServiceTestFixture fixture, string unitId, int hp, bool isDown = false)
        {
            GameObject companionObject = CreateObject(unitId, Vector3.zero);
            companionObject.AddComponent<Rigidbody2D>();
            CircleCollider2D bodyCollider = companionObject.AddComponent<CircleCollider2D>();
            CircleCollider2D combatCollider = companionObject.AddComponent<CircleCollider2D>();
            combatCollider.isTrigger = true;
            companionObject.AddComponent<AllyCombat>();
            companionObject.AddComponent<HitFlash>();
            companionObject.AddComponent<CompanionHealthBar>();
            CreateCompanionUi(companionObject);
            CompanionRuntime companion = companionObject.AddComponent<CompanionRuntime>();
            SetPrivateField(companion, "_bodyCollider", bodyCollider);
            SetPrivateField(companion, "_combatCollider", combatCollider);
            companion.Configure(fixture.Run.Party, fixture.Data.GetUnit(unitId), unitId, false);
            SetProperty(companion, "Hp", hp);
            SetProperty(companion, "IsDown", isDown);
            List<CompanionRuntime> companions = (List<CompanionRuntime>)typeof(PartyService)
                .GetField("Companions", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(fixture.Run.Party);
            companions.Add(companion);
            return companion;
        }

        private GameObject CreateObject(string name, Vector3 position)
        {
            GameObject value = new GameObject(name);
            value.transform.position = position;
            _objects.Add(value);
            return value;
        }

        private RecordingEnemy CreateEnemy(string name, Vector3 position)
        {
            return CreateObject(name, position).AddComponent<RecordingEnemy>();
        }

        private GameObject LoadPersonalSkeletonPrefab()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_LizzoPV/Gameplay/Legion/Prefabs/Characters/Supports/PersonalSkeletonSummon.prefab");
            Assert.IsNotNull(prefab);
            return prefab;
        }

        private CompanionPersonalSummonModule CreatePersonalModule(RecordingFactory factory, RecordingTargetSource source)
        {
            return new CompanionPersonalSummonModule(factory, null, source, new CombatImmediateHitModule());
        }

        private PersonalSummonSpawnRequest CreateRequest(Transform owner)
        {
            return CreateRequest("1", owner, 1);
        }

        private PersonalSummonSpawnRequest CreateRequest(string ownerKey, Transform owner, int activeCap)
        {
            CompanionSummonData data = new CompanionSummonData
            {
                Id = "UNIT_PERSONAL_SKELETON_01", OwnerUnitId = "necromancer", BaseActiveCap = 1, PromotedActiveCap = 2,
                Hp = 18, Damage = 4, AttackInterval = 1.3f, Range = 1.0f, MoveSpeed = 2.7f, AiScanInterval = 0.2f,
            }
            ;
            return new PersonalSummonSpawnRequest(ownerKey, "necromancer:UNIT_PERSONAL_SKELETON_01", owner,
                "Lizzo/Characters/Supports/UNIT_PERSONAL_SKELETON_01", new CompanionPersonalSummonSetup(data), activeCap);
        }

        private StableSlotFixture CreateStableSlotFixture(out GameObject origin)
        {
            OwnedSupportPresentationSet supports = AssetDatabase.LoadAssetAtPath<OwnedSupportPresentationSet>(
                "Assets/_LizzoPV/Gameplay/Presentation/Data/OwnedSupportPresentationSet.asset");
            Assert.IsNotNull(supports);
            PresentationCatalog catalog = ScriptableObject.CreateInstance<PresentationCatalog>();
            catalog.SetPresentationSetsForEditor(null, null, null, null, null, supports);
            GameObject providerRoot = CreateObject("StableSlotSummonCatalog", Vector3.zero);
            providerRoot.SetActive(false);
            PresentationCatalogProvider provider = providerRoot.AddComponent<PresentationCatalogProvider>();
            SerializedObject serialized = new SerializedObject(provider);
            serialized.FindProperty("_catalog").objectReferenceValue = catalog;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            origin = CreateObject("NecromancerOrigin", Vector3.zero);
            return new StableSlotFixture(catalog, providerRoot, provider, origin);
        }

        private static CountableKillAttribution CreateCompanionAttribution(int ownerInstanceId)
        {
            return new CountableKillAttribution(ownerInstanceId, "necromancer", CombatKillSourceCategory.CompanionOwnedAction);
        }

        private static void AssertPersonalSkeletonSetup(CompanionPersonalSummonSetup summon)
        {
            Assert.AreEqual("UNIT_PERSONAL_SKELETON_01", summon.SummonId);
            Assert.AreEqual("necromancer", summon.OwnerUnitId);
            Assert.AreEqual(15, summon.CountableKillThreshold);
            Assert.AreEqual(1, summon.BaseActiveCap);
            Assert.AreEqual(2, summon.PromotedActiveCap);
            Assert.AreEqual(18, summon.Hp);
            Assert.AreEqual(4, summon.Damage);
            Assert.AreEqual(1.3f, summon.AttackInterval);
            Assert.AreEqual(1.0f, summon.Range);
            Assert.AreEqual(2.7f, summon.MoveSpeed);
            Assert.AreEqual(0.2f, summon.AiScanInterval);
            Assert.AreEqual(CombatTargetRule.Nearest, summon.TargetRule);
            Assert.AreEqual("UNIT_SYNERGY_SKELETON_01", summon.DistinctFromSummonId);
            StringAssert.Contains("companion_tag=false", summon.Tags);
            StringAssert.Contains("no_family_tag", summon.Tags);
        }

        private static void CreateCompanionUi(GameObject companionObject)
        {
            Transform ui = new GameObject("UI").transform;
            ui.SetParent(companionObject.transform, false);
            Transform hpBarAnchor = new GameObject("HpBarAnchor").transform;
            hpBarAnchor.SetParent(ui, false);
            GameObject downMarker = new GameObject("P0_DownMarker");
            downMarker.transform.SetParent(hpBarAnchor, false);
            downMarker.AddComponent<TextMeshPro>();
            GameObject healthBar = new GameObject("P0_CompanionHPBar");
            healthBar.transform.SetParent(hpBarAnchor, false);
            CreateBarSprite(healthBar.transform, "Back");
            CreateBarSprite(healthBar.transform, "Fill");
            GameObject hpText = new GameObject("Text");
            hpText.transform.SetParent(healthBar.transform, false);
            hpText.AddComponent<TextMeshPro>();
        }

        private static void CreateBarSprite(Transform parent, string name)
        {
            GameObject part = new GameObject(name);
            part.transform.SetParent(parent, false);
            SpriteRenderer renderer = part.AddComponent<SpriteRenderer>();
            renderer.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0.0f, 0.0f, 1.0f, 1.0f), new Vector2(0.5f, 0.5f));
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            typeof(CompanionRuntime).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }

        private static void SetProperty(object target, string propertyName, object value)
        {
            PropertyInfo property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            property.GetSetMethod(true).Invoke(target, new[] {
                value }
            );
        }

        private sealed class ClericVisualScope : IDisposable
        {
            private readonly ClericHealTestVisualFactory _factory;
            public ClericVisualScope(ClericHealTestVisualFactory factory) => _factory = factory;
            public void Dispose()
            {
                LogAssert.ignoreFailingMessages = false;
                AttackVisual.ClearServices();
                FloatingDamageText.ClearServices();
                RetroVfx.ClearServices();
                _factory.Clear();
            }
        }

        private sealed class RecordingFactory : IPrefabFactory
        {
            private readonly GameObject _prefab;
            public GameObject LastSpawn {
                get;
                private set;
            }
            public int ReleaseCount {
                get;
                private set;
            }
            public bool ReturnNull {
                get;
                set;
            }
            public RecordingFactory(GameObject prefab) => _prefab = prefab;
            public GameObject Spawn(string address, Transform parent = null, bool pooled = false)
            {
                if (ReturnNull)
                    return null;
                LastSpawn = Object.Instantiate(_prefab, parent);
                return LastSpawn;
            }
            public GameObject Rent(GameObject prefab, string poolKey, Transform parent = null) => Spawn(poolKey, parent);
            public void Release(GameObject instance)
            {
                ReleaseCount++;
                Object.DestroyImmediate(instance);
            }
            public void Clear() {
            }
        }

        private sealed class RecordingTargetSource : ICompanionPersonalSummonTargetSource
        {
            private readonly RecordingEnemy[] _targets;
            public RecordingTargetSource(params RecordingEnemy[] targets) => _targets = targets;
            public void CollectTargets(List<PersonalSummonTarget> destination)
            {
                for (int i = 0;
                i < _targets.Length;
                i++)
                {
                    RecordingEnemy target = _targets[i];
                    if (target != null)
                        destination.Add(new PersonalSummonTarget(target, target.transform.position, target.GetInstanceID()));
                }
            }
        }

        private sealed class RecordingEnemy : MonoBehaviour, ICombatImmediateHitTarget
        {
            public int HitCount {
                get;
                private set;
            }
            public CombatImmediateHitRequest LastRequest {
                get;
                private set;
            }
            CombatImmediateHitFaction ICombatImmediateHitTarget.Faction => CombatImmediateHitFaction.Enemy;
            bool ICombatImmediateHitTarget.IsAlive => true;
            void ICombatImmediateHitTarget.ReceiveImmediateHit(in CombatImmediateHitRequest request)
            {
                HitCount++;
                LastRequest = request;
            }
        }

        private sealed class StableSlotFixture : IDisposable
        {
            private readonly GameObject _providerRoot;
            private readonly PresentationCatalog _catalog;
            private readonly GameObject _origin;
            private readonly PresentationCatalogProvider _previousProvider;
            private static readonly FieldInfo ActiveProviderField = typeof(PresentationCatalogProvider).GetField(
                "_active",
                BindingFlags.Static | BindingFlags.NonPublic);
            public readonly RecordingPersonalSummonModule Module = new RecordingPersonalSummonModule();
            public readonly PartyService Party;

            public StableSlotFixture(PresentationCatalog catalog, GameObject providerRoot, PresentationCatalogProvider provider, GameObject origin)
            {
                _catalog = catalog;
                _providerRoot = providerRoot;
                _origin = origin;
                _previousProvider = ActiveProviderField.GetValue(null) as PresentationCatalogProvider;
                ActiveProviderField.SetValue(null, provider);
                FakeDataProvider data = new FakeDataProvider();
                data.SetCompanionCombatProfile(new CompanionCombatProfileData
                {
                    UnitId = "necromancer", BaseHp = 50, MoveSpeed = 2.5f, BasicSkillId = "skill_curse_bolt", BasicEffectId = "dmg_curse_bolt_v1",
                    SecondarySkillId = "skill_personal_thrall",
                    SecondaryRuleId = "personal_thrall_countable_kills",
                    PromotionProfileId = "dark_ritualist",
                    NoTargetRetrySeconds = 0.15f,
                }
                );
                data.SetCompanionSummon(new CompanionSummonData
                {
                    Id = "UNIT_PERSONAL_SKELETON_01", OwnerUnitId = "necromancer", SkillId = "skill_personal_thrall", CountableKillThreshold = 15,
                    BaseActiveCap = 1, PromotedActiveCap = 2, Hp = 18, Damage = 4, AttackInterval = 1.3f, Range = 1.0f, MoveSpeed = 2.7f, AiScanInterval = 0.2f,
                    LifetimeRuleId = "battle_end_or_hp0", TargetRule = CombatTargetRule.Nearest, Tags = "summon_object,companion_tag=false,no_family_tag",
                    BossRuleId = "normal_target",
                    StackRuleId = "separate_owner_cap",
                    ResetRuleId = "battle_end",
                    RemoteConfigKey = "rc_personal_skeleton_stats",
                    DistinctFromSummonId = "UNIT_SYNERGY_SKELETON_01",
                }
                );
                data.InitializeAsync().GetAwaiter().GetResult();
                RecordingFactory factory = new RecordingFactory(null);
                Party = new PartyService(data, new RuntimeObjectRegistry(factory), factory, new NoProjectileModule(), new NoImmediateHitModule(),
                    new NoFieldModule(), new RunState(), Module);
            }

            public void Dispose()
            {
                Party.Dispose();
                ActiveProviderField.SetValue(null, _previousProvider);
                Object.DestroyImmediate(_origin);
                Object.DestroyImmediate(_providerRoot);
                Object.DestroyImmediate(_catalog);
            }
        }

        private sealed class RecordingPersonalSummonModule : ICompanionPersonalSummonModule
        {
            private readonly Dictionary<string, int> _active = new Dictionary<string, int>();
            public readonly List<PersonalSummonSpawnRequest> SpawnRequests = new List<PersonalSummonSpawnRequest>();
            public int ActiveCount {
                get;
                private set;
            }
            public bool TrySpawn(in PersonalSummonSpawnRequest request, float currentTime) {
                SpawnRequests.Add(request);
                return true;
            }
            public int GetActiveCount(string ownerKey, string sourceId) => _active.TryGetValue(ownerKey + ":" + sourceId, out int count) ? count : 0;
            public void SetActive(string ownerKey, string sourceId, int count) {
                _active[ownerKey + ":" + sourceId] = count;
            }
            public bool Release(PersonalSummonRuntime runtime) => false;
            public void Tick(float currentTime, float deltaTime) {
            }
            public void Reset() {
                _active.Clear();
            }
            public void Dispose() {
                _active.Clear();
            }
        }

        private sealed class NoProjectileModule : ICombatProjectileModule {
            public bool TrySpawn(in CombatProjectileRequest request) => false;
        }
        private sealed class NoImmediateHitModule : ICombatImmediateHitModule {
            public bool TryApply(in CombatImmediateHitRequest request) => false;
        }
        private sealed class NoFieldModule : ICombatPersistentFieldModule
        {
            public int ActiveFieldCount => 0;
            public bool TrySpawn(in CombatPersistentFieldRequest request, float currentTime) => false;
            public void Tick(float currentTime) {
            }
            public void Reset() {
            }
            public void Dispose() {
            }
        }
    }
}
