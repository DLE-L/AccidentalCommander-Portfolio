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
        public void BattleApothecary_LegacyHealAndBouncePathIsRetiredForVulnerabilityFlask()
        {
            LocalDataProvider data = CreateProjectProvider();
            Assert.That(new CompanionRangedSupportCombatResolver(data).TryResolve("field_herbalist", 1.0f, out _), Is.False);
            Assert.That(new CompanionTargetAreaCombatResolver(data).TryResolve(
                "field_herbalist",
                1.0f,
                out CompanionTargetAreaCombatSetup baseSetup), Is.True);
            Assert.That(baseSetup.AppliedStatusKind, Is.EqualTo(CompanionEnemyStatusKind.Vulnerable));
            Assert.That(data.GetCompanionRoster("field_herbalist").PromotionContractStage,
                Is.EqualTo(CompanionCombatContractStage.RuntimeConnected));
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
            Assert.That(state.IncomingDamageMultiplier, Is.EqualTo(0.50f));
            Assert.That(state.Advance(1.5f), Is.False);
            Assert.That(state.IncomingDamageMultiplier, Is.EqualTo(1.0f));
            state.ResetForOwnerDown(1.5f);
            Assert.That(state.IsActive, Is.False);
        }

        static LocalDataProvider CreateProjectProvider()
        {
            TestAssetService assets = new TestAssetService();
            TextAsset gameData = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_LizzoPV/Gameplay/Run/Data/GameData.xml");
            Assert.That(gameData, Is.Not.Null);
            assets.Register("PlayerData.xml", gameData);
            LocalDataProvider provider = new LocalDataProvider(assets);
            Assert.That(provider.InitializeAsync().GetAwaiter().GetResult().Succeeded, Is.True);
            return provider;
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
