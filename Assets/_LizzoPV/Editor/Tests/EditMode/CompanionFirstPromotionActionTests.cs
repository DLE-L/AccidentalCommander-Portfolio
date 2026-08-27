using System.Collections.Generic;
using Lizzo.PV.Data;
using Lizzo.PV.Legion;
using Lizzo.PV.Legion.Combat;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class CompanionFirstPromotionActionTests
    {
        [Test]
        public void Resolver_ExposesFirstFourPromotionActionsAsPlaceholderRuntimeContracts()
        {
            LocalDataProvider data = CreateProjectProvider();
            CompanionFirstPromotionCombatResolver resolver = new CompanionFirstPromotionCombatResolver(data);

            Assert.That(resolver.TryResolve(out CompanionFirstPromotionCombatSetup setup), Is.True);
            Assert.That(setup.Shield.SourceId, Is.EqualTo("shield_captain_shockwave"));
            Assert.That(setup.Shield.Damage, Is.EqualTo(1));
            Assert.That(setup.Shield.Cooldown, Is.GreaterThan(1.0f));
            Assert.That(setup.Shield.Radius, Is.GreaterThan(1.0f));
            Assert.That(setup.Shield.PushDistance, Is.GreaterThan(0.0f));

            Assert.That(setup.Sword.SourceId, Is.EqualTo("sword_captain_crescent"));
            Assert.That(setup.Sword.TriggerCount, Is.EqualTo(3));
            Assert.That(setup.Sword.Damage, Is.EqualTo(1));
            Assert.That(setup.Sword.ProjectileSpeed, Is.LessThan(10.0f));
            Assert.That(setup.Sword.Width, Is.GreaterThan(0.5f));
            Assert.That(setup.Sword.MaxTargets, Is.GreaterThan(1));

            Assert.That(setup.Light.SourceId, Is.EqualTo("light_guide_sanctuary"));
            Assert.That(setup.Light.TriggerCount, Is.EqualTo(3));
            Assert.That(setup.Light.AttackIntervalDivisor, Is.GreaterThan(1.0f));
            Assert.That(setup.Light.Duration, Is.GreaterThan(0.0f));
            Assert.That(setup.Light.Radius, Is.GreaterThan(0.0f));

            Assert.That(setup.Falcon.SourceId, Is.EqualTo("falcon_captain_dive"));
            Assert.That(setup.Falcon.TriggerCount, Is.EqualTo(3));
            Assert.That(setup.Falcon.Damage, Is.EqualTo(1));

            string[] connected = { "shield_guard", "sword_soldier", "cleric", "falcon_archer" };
            for (int index = 0; index < connected.Length; index++)
            {
                CompanionRosterData roster = data.GetCompanionRoster(connected[index]);
                Assert.That(roster.PromotionContractStage, Is.EqualTo(CompanionCombatContractStage.RuntimeConnected));
                Assert.That(roster.TuningState, Is.EqualTo(CompanionTuningState.Placeholder));
            }
        }

        [Test]
        public void Catalog_RejectsMissingRuntimePromotionEffectReference()
        {
            TextAsset source = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_LizzoPV/Gameplay/Run/Data/GameData.xml");
            string invalidXml = source.text.Replace(
                "promotionEffectRef=\"dmg_shield_captain_shockwave_v1\"",
                "promotionEffectRef=\"missing_shield_promotion_effect\"");
            TestAssetService assets = new TestAssetService();
            assets.Register("PlayerData.xml", new TextAsset(invalidXml));
            LocalDataProvider provider = new LocalDataProvider(assets);

            LogAssert.Expect(
                LogType.Error,
                "[LocalDataProvider] Required data missing: companion_promotion_effect:missing:shield_guard:missing_shield_promotion_effect, combat_effect:orphan:dmg_shield_captain_shockwave_v1");
            DataLoadResult result = provider.InitializeAsync().GetAwaiter().GetResult();

            Assert.That(result.Succeeded, Is.False);
            CollectionAssert.Contains(
                result.MissingRequiredIds,
                "companion_promotion_effect:missing:shield_guard:missing_shield_promotion_effect");
        }

        [Test]
        public void LineageCounters_UseActionAndReturningLightEventsWithOverflow()
        {
            CompanionFirstPromotionTriggerState state = new CompanionFirstPromotionTriggerState(3, 3, 3);

            Assert.That(state.Record("sword_soldier", CanonicalCompanionActionKind.BasicAttack, 2), Is.EqualTo(0));
            Assert.That(state.Record("sword_soldier", CanonicalCompanionActionKind.BasicAttack, 2), Is.EqualTo(1));
            Assert.That(state.SwordCurrentCount, Is.EqualTo(1));
            Assert.That(state.Record("cleric", CanonicalCompanionActionKind.BasicAttack), Is.EqualTo(0));
            Assert.That(state.Record("cleric", CanonicalCompanionActionKind.ReturningLightResolved, 3), Is.EqualTo(1));
            Assert.That(state.Record("falcon_archer", CanonicalCompanionActionKind.BasicAttack, 3), Is.EqualTo(1));

            state.Reset();
            Assert.That(state.SwordCurrentCount, Is.Zero);
            Assert.That(state.LightCurrentCount, Is.Zero);
            Assert.That(state.FalconCurrentCount, Is.Zero);
        }

        [Test]
        public void Sanctuary_UsesFixedCommanderOriginAndZoneMembership()
        {
            CompanionSanctuaryRuntimeState state = new CompanionSanctuaryRuntimeState();
            CompanionSanctuarySetup setup = new CompanionSanctuarySetup(
                "light_guide_sanctuary", 3, 2.5f, 4.0f, 1.25f);

            state.Begin(new Vector3(4.0f, 2.0f), 10.0f, setup);
            Assert.That(state.GetAttackIntervalDivisor(new Vector3(6.0f, 2.0f), 12.0f), Is.EqualTo(1.25f));
            Assert.That(state.GetAttackIntervalDivisor(new Vector3(7.0f, 2.0f), 12.0f), Is.EqualTo(1.0f));
            Assert.That(state.GetAttackIntervalDivisor(new Vector3(4.0f, 2.0f), 14.0f), Is.EqualTo(1.0f));
            state.Reset();
            Assert.That(state.IsActive(10.0f), Is.False);
        }

        [Test]
        public void FalconDiveSelector_PrioritizesBossThenEliteThenHighestHpDeterministically()
        {
            List<CompanionPromotionTargetCandidate> candidates = new List<CompanionPromotionTargetCandidate>
            {
                new CompanionPromotionTargetCandidate(null, Vector3.zero, 40, 40, false, false),
                new CompanionPromotionTargetCandidate(null, Vector3.zero, 30, 999, false, false),
                new CompanionPromotionTargetCandidate(null, Vector3.zero, 20, 10, false, true),
                new CompanionPromotionTargetCandidate(null, Vector3.zero, 10, 1, true, false),
            };

            Assert.That(CompanionFirstPromotionTargetSelector.TrySelectFalconDive(candidates, out CompanionPromotionTargetCandidate selected), Is.True);
            Assert.That(selected.InstanceId, Is.EqualTo(10));

            candidates.RemoveAt(3);
            Assert.That(CompanionFirstPromotionTargetSelector.TrySelectFalconDive(candidates, out selected), Is.True);
            Assert.That(selected.InstanceId, Is.EqualTo(20));

            candidates.RemoveAt(2);
            Assert.That(CompanionFirstPromotionTargetSelector.TrySelectFalconDive(candidates, out selected), Is.True);
            Assert.That(selected.InstanceId, Is.EqualTo(30));
        }

        private static LocalDataProvider CreateProjectProvider()
        {
            TestAssetService assets = new TestAssetService();
            TextAsset gameData = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_LizzoPV/Gameplay/Run/Data/GameData.xml");
            Assert.That(gameData, Is.Not.Null);
            assets.Register("PlayerData.xml", gameData);
            LocalDataProvider provider = new LocalDataProvider(assets);
            Assert.That(provider.InitializeAsync().GetAwaiter().GetResult().Succeeded, Is.True);
            return provider;
        }
    }
}
