using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Lizzo.PV.Data;
using Lizzo.PV.Legion;
using Lizzo.PV.P0.Config;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lizzo.PV.Tests.EditMode
{
    /// <summary>
    /// Current twelve-promotion mapping, formation, growth, and distinct combat behavior contracts.
    /// </summary>
    public sealed class CompanionPromotionBehaviorTests
    {
        static readonly PromotionCase[] Promotions =
        {
            new PromotionCase("shield_guard", "shield_captain"),
            new PromotionCase("sword_soldier", "sword_captain"),
            new PromotionCase("cleric", "light_guide"),
            new PromotionCase("falcon_archer", "falcon_captain"),
            new PromotionCase("field_herbalist", "battle_apothecary"),
            new PromotionCase("bombardier", "powder_captain"),
            new PromotionCase("fire_mage", "fire_sage"),
            new PromotionCase("lightning_mage", "storm_mage"),
            new PromotionCase("wolf_tamer", "beast_commander"),
            new PromotionCase("wraith_knight", "wraith_guardian"),
            new PromotionCase("necromancer", "dark_ritualist"),
            new PromotionCase("skeleton_bomber", "bone_artillery"),
        }
        ;

        [Test]
        public void PromotionMatrix_ResolvesAllTwelveCanonicalPromotions()
        {
            LocalDataProvider provider = CreateProjectProvider();
            for (int i = 0;
            i < Promotions.Length;
            i++)
            {
                PromotionCase promotion = Promotions[i];
                Assert.That(CompanionRuntimeSpec.TryCreate(provider, promotion.BaseId, false, out CompanionRuntimeSpec baseSpec), Is.True, promotion.BaseId);
                Assert.That(
                    CompanionRuntimeSpec.TryCreate(
                        provider,
                        promotion.BaseId,
                        true,
                        out CompanionRuntimeSpec promotedSpec),
                    Is.True,
                    promotion.PromotedId);
                Assert.That(baseSpec.BaseUnitId, Is.EqualTo(promotion.BaseId));
                Assert.That(baseSpec.PresentedUnitId, Is.EqualTo(promotion.BaseId));
                Assert.That(baseSpec.IsPromoted, Is.False);
                Assert.That(promotedSpec.BaseUnitId, Is.EqualTo(promotion.BaseId));
                Assert.That(promotedSpec.PresentedUnitId, Is.EqualTo(promotion.PromotedId));
                Assert.That(promotedSpec.IsPromoted, Is.True);
                Assert.That(promotedSpec.DisplayName, Is.Not.Empty);
            }
        }

        [Test]
        public void FormationWorldOffset_UsesApprovedSpacingForRadialSeparation()
        {
            RuntimeObjectRegistry registry = (RuntimeObjectRegistry)FormatterServices.GetUninitializedObject(typeof(RuntimeObjectRegistry));
            PartyService party = (PartyService)FormatterServices.GetUninitializedObject(typeof(PartyService));
            System.Type formationType = typeof(PartyService).Assembly.GetType("Lizzo.PV.Legion.FormationService");
            object formation = System.Activator.CreateInstance(
                formationType,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new object[] {
                    registry, party }
                ,
                null);
            MethodInfo resolveWorldOffset = formationType.GetMethod("ResolveWorldOffset", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Vector3 worldOffset = (Vector3)resolveWorldOffset.Invoke(formation, new object[] {
                Vector3.right, "front_left_01" }
            );

            Assert.That(RemoteConfig.FormationSpacing, Is.EqualTo(0.85f).Within(0.0001f));
            Assert.That(worldOffset.x, Is.EqualTo(0.85f).Within(0.0001f));
            Assert.That(worldOffset.magnitude, Is.GreaterThan(0.60f));
        }

        [Test]
        public void BattleApothecary_ChangesSecondaryHealAndPreservesPrimaryCadence()
        {
            CompanionRangedSupportCombatSetup baseSetup = ResolveHerbalistSetup();
            CompanionRangedSupportCombatSetup promoted = baseSetup.WithPromotedBattleApothecaryHeal();
            Assert.That(promoted.Primary.SourceId, Is.EqualTo("field_herbalist"));
            Assert.That(promoted.Primary.Damage, Is.EqualTo(8));
            Assert.That(promoted.Primary.Period, Is.EqualTo(1.4f));
            Assert.That(promoted.SecondaryHealAmount, Is.EqualTo(4));
            Assert.That(promoted.SecondaryMaxTargets, Is.EqualTo(2));
            Assert.That(promoted.SecondarySecondTargetRatio, Is.EqualTo(0.60f));
            Assert.That(promoted.SecondaryPeriod, Is.EqualTo(5.0f));
            Assert.That(promoted.SecondaryNoTargetRetrySeconds, Is.EqualTo(0.15f));

            GameObject owner = new GameObject("BattleApothecaryPromotion");
            try
            {
                AllyCombat combat = owner.AddComponent<AllyCombat>();
                combat.SetCanonicalRangedSupportInfo(promoted);
                combat.ApplyGrowthScale(new CompanionGrowthScale(1.55f, 2.0f, 0.90f, 3));
                Assert.That(combat.SecondaryHealAmount, Is.EqualTo(6));
                Assert.That(combat.SecondaryHealMaxTargets, Is.EqualTo(2));
                Assert.That(combat.SecondaryHealPeriod, Is.EqualTo(5.0f));
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void LightGuide_ChangesOnlyTheSecondHealTargetContract()
        {
            CompanionRangedSupportCombatSetup baseSetup = new CompanionRangedSupportCombatSetup(
                new CompanionProjectileCombatSetup("cleric", AllyAttackStyle.TargetedProjectile, 5, 1.6f, 4.5f, 1, 0.15f),
                8, 4.0f, 4.0f, 1, 1.0f, 0.15f);
            CompanionRangedSupportCombatSetup promoted = baseSetup.WithPromotedLightGuideHeal();
            Assert.That(baseSetup.SecondaryMaxTargets, Is.EqualTo(1));
            Assert.That(promoted.SecondaryMaxTargets, Is.EqualTo(2));
            Assert.That(promoted.SecondarySecondTargetRatio, Is.EqualTo(0.70f));
            Assert.That(promoted.SecondaryPeriod, Is.EqualTo(baseSetup.SecondaryPeriod));
            Assert.That(promoted.SecondaryRange, Is.EqualTo(baseSetup.SecondaryRange));

            GameObject owner = new GameObject("LightGuidePromotion");
            try
            {
                AllyCombat combat = owner.AddComponent<AllyCombat>();
                combat.SetCanonicalRangedSupportInfo(promoted);
                combat.ApplyGrowthScale(new CompanionGrowthScale(1.8f, 2.1f, 0.9f, 3));
                Assert.That(combat.SecondaryHealAmount, Is.EqualTo(14));
                Assert.That(Mathf.RoundToInt(combat.SecondaryHealAmount * combat.SecondaryHealSecondTargetRatio), Is.EqualTo(10));
                Assert.That(combat.SecondaryHealMaxTargets, Is.EqualTo(2));
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void BeastCommander_UsesTwoLockedWolfHitsAndReset()
        {
            CompanionWolfOwnedProxyCombatSetup promoted = new CompanionWolfOwnedProxyCombatSetup(
                    "wolf_tamer", 10, 4.0f, 4.0f, 0.8f, 1, 1, 0.15f)
                .WithPromotedBeastCommanderHits();
            Assert.That(promoted.HitCount, Is.EqualTo(2));
            Assert.That(promoted.PerHitDamageRatio, Is.EqualTo(0.70f));

            WolfOwnedProxyState state = new WolfOwnedProxyState();
            Assert.That(state.TryBegin(Vector3.zero, Vector3.right, 42, 0.0f, 0.8f, promoted.HitCount), Is.True);
            Assert.That(state.TryBegin(Vector3.zero, Vector3.right, 43, 0.0f, 0.8f, promoted.HitCount), Is.False);
            Assert.That(state.Advance(0.4f, out _), Is.True);
            Assert.That(state.TryConsumeLockedTargetHit(true), Is.True);
            Assert.That(state.TryConsumeLockedTargetHit(true), Is.True);
            Assert.That(state.Phase, Is.EqualTo(WolfOwnedProxyPhase.Return));
            state.Reset();
            Assert.That(state.IsActive, Is.False);
        }

        [Test]
        public void BoneArtillery_UsesRadiusOrderedFollowUpAndSingleImpact()
        {
            CompanionTargetAreaCombatSetup baseSetup = new CompanionTargetAreaCombatSetup(
                "skeleton_bomber", 15, 2.4f, 4.8f, 1.5f, 6, 0.0f, 0.15f);
            CompanionTargetAreaCombatSetup scaled = baseSetup.WithGrowthScale(new CompanionGrowthScale(1.70f, 2.10f, 1.10f, 3));
            PromotedTargetAreaFollowUpSetup followUp = scaled.CreatePromotedBoneArtilleryFollowUp();
            Assert.That(followUp.Radius, Is.EqualTo(2.0f));
            Assert.That(followUp.MaxTargets, Is.EqualTo(1));
            Assert.That(followUp.DamageRatio, Is.EqualTo(0.60f));

            List<TargetAreaImpactCandidate> source = new List<TargetAreaImpactCandidate>
            {
                new TargetAreaImpactCandidate(null, new Vector3(0.1f, 0.0f), 99),
                new TargetAreaImpactCandidate(null, new Vector3(1.0f, 0.0f), 30),
                new TargetAreaImpactCandidate(null, new Vector3(-1.0f, 0.0f), 10),
                new TargetAreaImpactCandidate(null, new Vector3(2.1f, 0.0f), 40),
            }
            ;
            Assert.That(PromotedTargetAreaFollowUpSelector.TrySelect(source, Vector3.zero, 99, 2.0f, out TargetAreaImpactCandidate selected), Is.True);
            Assert.That(selected.InstanceId, Is.EqualTo(10));
            TargetAreaCastState state = new TargetAreaCastState();
            state.Configure(baseSetup, 0.0f, 0.0f);
            Assert.That(state.TryBeginCast(0.0f, Vector3.one, 99), Is.True);
            Assert.That(state.TryConsumeImpact(0.0f, out _), Is.True);
            Assert.That(state.TryConsumeImpact(0.0f, out _), Is.False);
        }

        [Test]
        public void DarkRitualist_UsesPromotedSkeletonCapAndKillThreshold()
        {
            LocalDataProvider data = CreateProjectProvider();
            Assert.That(new CompanionPersonalSummonResolver(data).TryResolve("necromancer", out CompanionPersonalSummonSetup summon), Is.True);
            Assert.That(summon.ResolveActiveCap(false), Is.EqualTo(1));
            Assert.That(summon.ResolveActiveCap(true), Is.EqualTo(2));
            Assert.That(summon.CountableKillThreshold, Is.EqualTo(15));

            CountableKillThresholdState state = new CountableKillThresholdState();
            state.Configure(summon.CountableKillThreshold, summon.ResolveActiveCap(true));
            Assert.That(RecordThreshold(state, 15), Is.True);
            Assert.That(RecordThreshold(state, 15), Is.True);
            Assert.That(state.ActiveCount, Is.EqualTo(2));
            state.Reset();
            Assert.That(state.ActiveCount, Is.EqualTo(0));
            Assert.That(state.PendingCountableKills, Is.EqualTo(0));
        }

        [Test]
        public void FalconCaptain_UsesBurstRatioAndSuccessfulAssistCycle()
        {
            PromotedProjectileBurst burst = new PromotedProjectileBurst(2, 0.65f);
            Assert.That(burst.ShotCount, Is.EqualTo(2));
            Assert.That(burst.ResolveShotDamage(17), Is.EqualTo(11));
            SuccessfulActionCounter counter = new SuccessfulActionCounter();
            counter.Configure(3);
            Assert.That(counter.RecordSuccess(), Is.False);
            Assert.That(counter.RecordSuccess(), Is.False);
            Assert.That(counter.RecordSuccess(), Is.True);
            counter.Reset();
            Assert.That(counter.CurrentCount, Is.EqualTo(0));
        }

        [Test]
        public void FireSage_PersistentFieldPromotionIsIdempotentAndGrowthScalesOnce()
        {
            LocalDataProvider provider = CreateProjectProvider();
            Assert.That(
                new CompanionPersistentFieldCombatResolver(provider).TryResolve(
                    "fire_mage",
                    1.0f,
                    out CompanionPersistentFieldCombatSetup baseSetup),
                Is.True);
            CompanionPersistentFieldCombatSetup promoted = baseSetup.WithPromotedFireSageField();
            CompanionPersistentFieldCombatSetup promotedAgain = promoted.WithPromotedFireSageField();
            Assert.That(promoted.Radius, Is.EqualTo(1.8f));
            Assert.That(promoted.Duration, Is.EqualTo(4.0f));
            Assert.That(promotedAgain.Radius, Is.EqualTo(promoted.Radius));
            Assert.That(promotedAgain.Duration, Is.EqualTo(promoted.Duration));

            GameObject owner = new GameObject("FireSagePromotion");
            try
            {
                AllyCombat combat = owner.AddComponent<AllyCombat>();
                combat.SetCanonicalPersistentFieldInfo(promoted);
                combat.ApplyGrowthScale(new CompanionGrowthScale(1.60f, 2.10f, 1.05f, 3));
                Assert.That(combat.PersistentFieldSetup.Radius, Is.EqualTo(1.8f));
                Assert.That(combat.PersistentFieldSetup.Duration, Is.EqualTo(4.0f));
                Assert.That(combat.PersistentFieldSetup.Period, Is.EqualTo(3.36f).Within(0.0001f));
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void PowderCaptain_CollectsDeterministicImpactsAndRetriesDelayedCast()
        {
            CompanionTargetAreaCombatSetup promoted = new CompanionTargetAreaCombatSetup(
                "bombardier", 28, 2.42f, 5.0f, 1.6f, 6, 0.5f, 0.15f).WithPromotedPowderCaptainImpact();
            List<TargetAreaImpactCandidate> source = new List<TargetAreaImpactCandidate>
            {
                new TargetAreaImpactCandidate(null, new Vector3(1.0f, 0.0f), 30, TargetAreaImpactTargetClass.Normal),
                new TargetAreaImpactCandidate(null, new Vector3(-1.0f, 0.0f), 10, TargetAreaImpactTargetClass.Elite),
                new TargetAreaImpactCandidate(null, new Vector3(1.5f, 0.0f), 20, TargetAreaImpactTargetClass.Boss),
                new TargetAreaImpactCandidate(null, new Vector3(2.1f, 0.0f), 40, TargetAreaImpactTargetClass.Normal),
            }
            ;
            List<TargetAreaImpactCandidate> results = new List<TargetAreaImpactCandidate>();
            TargetAreaImpactCollector.Collect(source, Vector3.zero, promoted.Radius, promoted.MaxTargets, results);
            Assert.That(results.Count, Is.EqualTo(3));
            Assert.That(results[0].InstanceId, Is.EqualTo(10));
            Assert.That(TargetAreaPushRequest.Create(promoted, results[1], Vector3.zero).Distance, Is.EqualTo(0.4f));

            TargetAreaCastState state = new TargetAreaCastState();
            state.Configure(promoted, 0.0f, 0.0f);
            state.RecordNoTarget(0.0f);
            Assert.That(state.NextTargetDueTime, Is.EqualTo(0.15f));
            Assert.That(state.TryBeginCast(0.15f, Vector3.one), Is.True);
            Assert.That(state.TryConsumeImpact(0.65f, out _), Is.True);
            Assert.That(state.TryConsumeImpact(0.65f, out _), Is.False);
        }

        [Test]
        public void ShieldCaptain_ProtectionWindowActivatesExpiresAndResets()
        {
            CompanionMeleeCombatSetup baseSetup = new CompanionMeleeCombatSetup(
                AllyAttackStyle.ForwardPush, 12, 1.14f, 1.2f, 60.0f, 0.5f, 3, 0.15f);
            CompanionMeleeCombatSetup promoted = baseSetup.WithPromotedShieldCaptainGeometry();
            Assert.That(promoted.Range, Is.EqualTo(1.8f));
            Assert.That(promoted.Angle, Is.EqualTo(90.0f));
            Assert.That(promoted.MaxTargets, Is.EqualTo(3));

            CompanionProtectionWindow window = new CompanionProtectionWindow(
                new CompanionProtectionWindowSetup("shield_captain_promotion_protection", 0.90f, 1.5f));
            Assert.That(window.TryActivateOnce(2.0f), Is.True);
            Assert.That(window.IsActive(3.49f), Is.True);
            Assert.That(window.ApplyToCompanionDamage(10, 3.49f), Is.EqualTo(9));
            Assert.That(window.IsActive(3.5f), Is.False);
            window.Reset();
            Assert.That(window.TryActivateOnce(4.0f), Is.True);
            Assert.That(window.SourceKey, Is.Not.EqualTo("guard_squad"));
        }

        [Test]
        public void StormMage_ChainPromotionChangesCapacityAndRetryCadence()
        {
            CompanionChainCombatSetup baseSetup = ResolveChainSetup();
            CompanionChainCombatSetup promoted = baseSetup.WithPromotedStormMageChain();
            Assert.That(baseSetup.MaxTargets, Is.EqualTo(3));
            Assert.That(promoted.MaxTargets, Is.EqualTo(5));
            Assert.That(promoted.Damage, Is.EqualTo(baseSetup.Damage));
            Assert.That(promoted.Period, Is.EqualTo(baseSetup.Period));

            List<ChainTargetCandidate> source = new List<ChainTargetCandidate>
            {
                new ChainTargetCandidate(null, new Vector3(1.0f, 0.0f), 20),
                new ChainTargetCandidate(null, new Vector3(1.0f, 0.0f), 10),
                new ChainTargetCandidate(null, new Vector3(2.0f, 0.0f), 30),
                new ChainTargetCandidate(null, new Vector3(3.0f, 0.0f), 40),
                new ChainTargetCandidate(null, new Vector3(4.0f, 0.0f), 50),
                new ChainTargetCandidate(null, new Vector3(5.0f, 0.0f), 60),
            }
            ;
            List<ChainTargetCandidate> results = new List<ChainTargetCandidate>(5);
            ChainTargetSelector.Collect(source, Vector3.zero, 5.0f, 1.8f, 5, results);
            CollectionAssert.AreEqual(new[] {
                10, 20, 30, 40, 50 }
            , new[] {
                results[0].InstanceId, results[1].InstanceId, results[2].InstanceId, results[3].InstanceId, results[4].InstanceId }
            );
        }

        [Test]
        public void SwordCaptain_SequenceCompletesTwoPassesAndResets()
        {
            PromotedMultiHitSequence sequence = new PromotedMultiHitSequence(2, 0.70f);
            Assert.That(Mathf.RoundToInt(20 * sequence.DamageRatio), Is.EqualTo(14));
            sequence.BeginCast();
            Assert.That(sequence.TryRecordResolvedPass(), Is.True);
            Assert.That(sequence.IsComplete, Is.False);
            Assert.That(sequence.TryRecordResolvedPass(), Is.True);
            Assert.That(sequence.IsComplete, Is.True);
            Assert.That(sequence.TryRecordResolvedPass(), Is.False);
            sequence.Reset();
            Assert.That(sequence.CompletedPassCount, Is.EqualTo(0));
            Assert.That(sequence.IsComplete, Is.False);
        }

        [Test]
        public void WraithGuardian_UsesPersonalMitigationAndPromotedGeometry()
        {
            CompanionWraithMeleeDefenseSetup promoted = ResolveWraithSetup()
                .WithPromotedWraithGuardianGeometry()
                .WithPromotedWraithGuardianDefense();
            Assert.That(promoted.Melee.Range, Is.EqualTo(1.4f));
            Assert.That(promoted.Melee.Angle, Is.EqualTo(75.0f));
            Assert.That(promoted.PersonalDefense.IncomingDamageMultiplier, Is.EqualTo(0.50f));
            Assert.That(promoted.PersonalDefense.Duration, Is.EqualTo(1.5f));

            PersonalDamageMitigationState state = new PersonalDamageMitigationState();
            state.Configure(promoted.PersonalDefense, 0.0f);
            Assert.That(state.Advance(0.0f), Is.True);
            Assert.That(state.ApplyToSelf(10), Is.EqualTo(5));
            Assert.That(state.Advance(1.5f), Is.False);
            Assert.That(state.ApplyToSelf(10), Is.EqualTo(10));
            state.ResetForOwnerDown(1.5f);
            Assert.That(state.IsActive, Is.False);
        }

        static LocalDataProvider CreateProjectProvider()
        {
            TestAssetService assets = new TestAssetService();
            TextAsset gameData = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_LizzoPV/Gameplay/RunData/Data/GameData.xml");
            Assert.That(gameData, Is.Not.Null);
            assets.Register("PlayerData.xml", gameData);
            LocalDataProvider provider = new LocalDataProvider(assets);
            Assert.That(provider.InitializeAsync().GetAwaiter().GetResult().Succeeded, Is.True);
            return provider;
        }

        static CompanionRangedSupportCombatSetup ResolveHerbalistSetup()
        {
            CompanionRangedSupportCombatResolver resolver = new CompanionRangedSupportCombatResolver(CreateProjectProvider());
            Assert.That(resolver.TryResolve("field_herbalist", 1.0f, out CompanionRangedSupportCombatSetup setup), Is.True);
            return setup;
        }

        static CompanionChainCombatSetup ResolveChainSetup()
        {
            CompanionChainCombatResolver resolver = new CompanionChainCombatResolver(CreateProjectProvider());
            Assert.That(resolver.TryResolve("lightning_mage", 1.0f, out CompanionChainCombatSetup setup), Is.True);
            return setup;
        }

        static CompanionWraithMeleeDefenseSetup ResolveWraithSetup()
        {
            CompanionMeleeCombatResolver resolver = new CompanionMeleeCombatResolver(CreateProjectProvider());
            Assert.That(resolver.TryResolveWraithMeleeDefense(1.0f, out CompanionWraithMeleeDefenseSetup setup), Is.True);
            return setup;
        }

        static bool RecordThreshold(CountableKillThresholdState state, int count)
        {
            bool requested = false;
            for (int i = 0;
            i < count;
            i++)
                requested |= state.RecordKill(true);
            return requested;
        }

        readonly struct PromotionCase
        {
            public readonly string BaseId;
            public readonly string PromotedId;

            public PromotionCase(string baseId, string promotedId)
            {
                BaseId = baseId;
                PromotedId = promotedId;
            }
        }
    }
}
