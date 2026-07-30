using System.Collections.Generic;
using System.Reflection;
using Lizzo.PV.Legion;
using Lizzo.PV.P0.Presentation;
using Lizzo.PV.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class PauseCompanionPresentationResolverTests
    {
        static readonly FieldInfo ActiveProvider = typeof(PresentationCatalogProvider)
            .GetField("_active", BindingFlags.Static | BindingFlags.NonPublic);

        PresentationCatalogProvider _previousProvider;
        PresentationCatalog _catalog;
        GameObject _providerRoot;

        [SetUp]
        public void SetUp()
        {
            _previousProvider = ActiveProvider.GetValue(null) as PresentationCatalogProvider;
            UnitPresentationSet units = AssetDatabase.LoadAssetAtPath<UnitPresentationSet>(
                "Assets/_LizzoPV/Data/Presentation/UnitPresentationSet.asset");
            Assert.That(units, Is.Not.Null);

            _catalog = ScriptableObject.CreateInstance<PresentationCatalog>();
            _catalog.SetPresentationSetsForEditor(null, null, null, null, units);
            _providerRoot = new GameObject("PauseCompanionPresentationCatalog");
            _providerRoot.SetActive(false);
            PresentationCatalogProvider provider = _providerRoot.AddComponent<PresentationCatalogProvider>();
            SerializedObject serialized = new SerializedObject(provider);
            serialized.FindProperty("_catalog").objectReferenceValue = _catalog;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            ActiveProvider.SetValue(null, provider);
        }

        [TearDown]
        public void TearDown()
        {
            if (_providerRoot != null)
                Object.DestroyImmediate(_providerRoot);
            if (_catalog != null)
                Object.DestroyImmediate(_catalog);
            ActiveProvider.SetValue(null, _previousProvider);
        }

        [Test]
        public void ActiveCanonicalSquadUsesBaseUnitPortraitAndInactiveSlotsStayEmpty()
        {
            Assert.That(PresentationCatalogProvider.TryGetUnit("shield_guard", out UnitPresentationSet.Entry shieldGuard), Is.True);
            Assert.That(shieldGuard.Portrait, Is.Not.Null);

            List<PauseCompanionPresentation> presentations = new List<PauseCompanionPresentation>();
            PauseCompanionPresentationResolver.Fill(CreateCanonicalSquadSnapshot(), presentations, 7, null);

            Assert.That(presentations, Has.Count.EqualTo(7));
            Assert.That(presentations[0].Icon, Is.SameAs(shieldGuard.Portrait));
            Assert.That(presentations[0].CurrentCount, Is.EqualTo(2));
            for (int i = 1; i < presentations.Count; i++)
            {
                Assert.That(presentations[i].Icon, Is.Null);
                Assert.That(presentations[i].CurrentCount, Is.Zero);
            }
        }

        [Test]
        public void ActiveSquadWithoutUnitPresentationLogsBaseUnitAndRosterSlot()
        {
            List<PauseCompanionPresentation> presentations = new List<PauseCompanionPresentation>();
            SquadSlotState[] states = CreateCanonicalSquadSnapshot();
            states[0] = new SquadSlotState("squad_00", "missing_unit", string.Empty, 1, 3, false, "missing_unit");

            LogAssert.Expect(
                LogType.Error,
                "[GameplayUIController] Missing UnitPresentationSet entry for active companion: missing_unit (roster slot: squad_00)");
            PauseCompanionPresentationResolver.Fill(states, presentations, 7, null);

            Assert.That(presentations[0].Icon, Is.Null);
            Assert.That(presentations[0].CurrentCount, Is.EqualTo(1));
        }

        static SquadSlotState[] CreateCanonicalSquadSnapshot()
        {
            SquadSlotState[] states = new SquadSlotState[7];
            states[0] = new SquadSlotState("squad_00", "shield_guard", string.Empty, 2, 3, false, "shield_guard");
            for (int i = 1; i < states.Length; i++)
                states[i] = new SquadSlotState($"squad_{i:00}", string.Empty, string.Empty, 0, 3, false, string.Empty);
            return states;
        }
    }
}
