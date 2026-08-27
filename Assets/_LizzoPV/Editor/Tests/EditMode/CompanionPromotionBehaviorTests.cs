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
        public void BeastCommander_PreservesSingleHitExecutionChainAsItsBaseAction()
        {
            CompanionWolfOwnedProxyCombatSetup baseAction = new CompanionWolfOwnedProxyCombatSetup(
                "wolf_tamer", 10, 4.0f, 4.0f, 0.8f, 1, 1, 0.15f);
            Assert.That(baseAction.HitCount, Is.EqualTo(1));
            Assert.That(baseAction.PerHitDamageRatio, Is.EqualTo(1.0f));

            WolfOwnedProxyState state = new WolfOwnedProxyState();
            Assert.That(state.TryBegin(Vector3.zero, Vector3.right, 42, 0.0f, 0.8f, baseAction.HitCount), Is.True);
            Assert.That(state.TryBegin(Vector3.zero, Vector3.right, 43, 0.0f, 0.8f, baseAction.HitCount), Is.False);
            Assert.That(state.Advance(0.4f, out _), Is.True);
            Assert.That(state.TryConsumeLockedTargetHit(true), Is.True);
            Assert.That(state.Phase, Is.EqualTo(WolfOwnedProxyPhase.Return));
            state.Reset();
            Assert.That(state.IsActive, Is.False);
        }

        [Test]
        public void SkeletonReaper_PreservesReturningScytheAsItsBaseAction()
        {
            LocalDataProvider data = CreateProjectProvider();
            Assert.That(new CompanionReturningAttackCombatResolver(data).TryResolve(
                "skeleton_bomber", 1.0f, out CompanionReturningAttackCombatSetup setup), Is.True);
            Assert.That(setup.MaxTargetsPerPass, Is.GreaterThan(1));
            Assert.That(setup.Width, Is.GreaterThan(0.0f));
        }

        [Test]
        public void DarkRitualist_ReusesOwnedUndeadStatsWithoutChangingSquadOwnership()
        {
            LocalDataProvider data = CreateProjectProvider();
            Assert.That(new CompanionPersonalSummonResolver(data).TryResolve("necromancer", out CompanionPersonalSummonSetup summon), Is.True);
            Assert.That(summon.SummonId, Is.EqualTo("UNIT_PERSONAL_SKELETON_01"));
            Assert.That(summon.Tags, Does.Contain("companion_tag=false"));
            Assert.That(summon.Tags, Does.Contain("no_family_tag"));
        }

        [Test]
        public void WraithGuardian_PreservesBaseSlashGeometryAndWeakening()
        {
            CompanionWraithMeleeDefenseSetup setup = ResolveWraithSetup();
            Assert.That(setup.Melee.Range, Is.EqualTo(1.2f));
            Assert.That(setup.Melee.Angle, Is.EqualTo(60.0f));
            Assert.That(setup.Melee.AppliedStatusKind, Is.EqualTo(CompanionEnemyStatusKind.Weakening));
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
