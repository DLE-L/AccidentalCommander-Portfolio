using System;
using System.Threading;
using Lizzo.PV.Legion.RunCore;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;

namespace Lizzo.PV.EditorTests
{
    public sealed class CompanionActionLoadoutTests
    {
        private FakeDataProvider _data;
        [SetUp] public void SetUp()
        {
            _data = new FakeDataProvider();
            _data.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
        }

        [Test]
        public void Lists_AllowOrderedMultipleActionsAndIndependentPromotedComposition()
        {
            var profile = _data.GetCompanionCombatProfile("cleric");
            profile.BaseActionEffectIds = "heal_cleric_v1,dmg_cleric_bolt_v1,heal_cleric_v1";
            profile.PromotedActionEffectIds = "dmg_cleric_bolt_v1";
            var catalog = new CompanionRuntimeDefinitionCatalog(_data);
            Assert.That(catalog.TryGetDefinition("cleric", out var cleric), Is.True);
            Assert.That(cleric.BaseActionSet.Steps.Count, Is.EqualTo(3));
            Assert.That(cleric.BaseActionSet.Steps[0].EffectId, Is.EqualTo("heal_cleric_v1"));
            Assert.That(cleric.BaseActionSet.Steps[1].EffectId, Is.EqualTo("dmg_cleric_bolt_v1"));
            Assert.That(cleric.BaseActionSet.Steps[2].EffectId, Is.EqualTo("heal_cleric_v1"));
            Assert.That(cleric.PromotedActionSet.Steps.Count, Is.EqualTo(1));
            Assert.That(cleric.PromotedActionSet.Steps[0].Magnitude,
                Is.EqualTo(_data.GetCombatEffect("dmg_cleric_bolt_v1").BaseValue * _data.GetCompanionPromotion("light_guide").EffectMultiplier));
        }

        [Test]
        public void ExplicitClericLists_PreserveAttackThenHeal()
        {
            var catalog = new CompanionRuntimeDefinitionCatalog(_data);
            catalog.TryGetDefinition("cleric", out var cleric);
            Assert.That(cleric.BaseActionSet.Steps.Count, Is.EqualTo(2));
            Assert.That(cleric.PromotedActionSet.Steps.Count, Is.EqualTo(2));
            Assert.That(cleric.BaseActionSet.Steps[1].EffectId, Is.EqualTo("heal_cleric_v1"));
        }

        [TestCase(false, null)]
        [TestCase(false, "")]
        [TestCase(false, " ")]
        [TestCase(true, null)]
        [TestCase(true, "")]
        [TestCase(true, " ")]
        public void MissingLists_FailInsteadOfRestoringLegacyComposition(bool promoted, string ids)
        {
            var profile = _data.GetCompanionCombatProfile("cleric");
            if (promoted) profile.PromotedActionEffectIds = ids;
            else profile.BaseActionEffectIds = ids;
            Assert.Throws<InvalidOperationException>(() => new CompanionRuntimeDefinitionCatalog(_data));
        }

        [TestCase("missing_effect")]
        [TestCase("dmg_sword_slash_v1")]
        [TestCase("dmg_cleric_bolt_v1,")]
        public void InvalidExplicitLists_FailWithoutSilentlyDroppingActions(string ids)
        {
            _data.GetCompanionCombatProfile("cleric").BaseActionEffectIds = ids;
            Assert.Throws<InvalidOperationException>(() => new CompanionRuntimeDefinitionCatalog(_data));
        }

        [Test]
        public void XmlFallbackAndFixture_UseTheSameTwelveExplicitLoadouts()
        {
            UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Error,
                new System.Text.RegularExpressions.Regex("\\[LocalDataProvider\\] Local data asset was not available."));
            var fallback = new Lizzo.PV.Data.LocalDataProvider(new TestAssetService());
            Assert.That(fallback.InitializeAsync().GetAwaiter().GetResult().Succeeded, Is.True);
            var xml = System.Xml.Linq.XDocument.Load(System.IO.Path.Combine(UnityEngine.Application.dataPath,
                "_LizzoPV/Gameplay/Run/Data/GameData.xml"));
            int count = 0;
            foreach (var row in xml.Descendants("CompanionCombatProfileData"))
            {
                string id = (string)row.Attribute("unitId");
                foreach (var provider in new Lizzo.PV.Data.IDataProvider[] { fallback, _data })
                {
                    var profile = provider.GetCompanionCombatProfile(id);
                    Assert.That(profile.BaseActionEffectIds, Is.EqualTo((string)row.Attribute("baseActionEffectIds")), id);
                    Assert.That(profile.PromotedActionEffectIds, Is.EqualTo((string)row.Attribute("promotedActionEffectIds")), id);
                }
                count++;
            }
            Assert.That(count, Is.EqualTo(12));
            Assert.DoesNotThrow(() => new CompanionRuntimeDefinitionCatalog(fallback));
        }
    }
}
