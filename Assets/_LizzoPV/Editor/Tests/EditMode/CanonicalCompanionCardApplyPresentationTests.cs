using System.Collections.Generic;
using System.Reflection;
using Lizzo.PV.Data;
using Lizzo.PV.Legion;
using Lizzo.PV.P0.Cards;
using Lizzo.PV.P0.Presentation;
using Lizzo.PV.P0.Visuals;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Lizzo.PV.EditorTests
{
    public sealed class CanonicalCompanionCardApplyPresentationTests
    {
        [Test]
        public void ProjectCatalog_ProvidesAllCanonicalTab92RowsAndBadgeKeys()
        {
            LocalDataProvider provider = CreateProjectProvider();
            Assert.IsTrue(provider.InitializeAsync().GetAwaiter().GetResult().Succeeded);

            Assert.AreEqual(12, provider.CompanionCardLocalizations.Count);
            for (int i = 0; i < provider.CompanionRoster.Count; i++)
            {
                CompanionRosterData roster = provider.CompanionRoster[i];
                CompanionCardLocalizationData row = provider.GetCompanionCardLocalization(roster.UnitId);
                Assert.IsNotNull(row, roster.UnitId);
                Assert.AreEqual(roster.RecruitTitleKey, row.RecruitTitleKey);
                Assert.AreEqual(roster.RecruitDescKey, row.RecruitDescKey);
                Assert.AreEqual("ui.card.badge.recruit", row.RecruitBadgeKey);
                Assert.AreEqual("ui.card.badge.reinforce", row.ReinforceBadgeKey);
                Assert.AreEqual("ui.card.badge.promote", row.PromoteBadgeKey);
                Assert.IsFalse(string.IsNullOrWhiteSpace(row.RecruitTitleKo));
                Assert.IsFalse(string.IsNullOrWhiteSpace(row.RecruitTitleEn));
                Assert.IsFalse(string.IsNullOrWhiteSpace(row.ReinforceTitleKo));
                Assert.IsFalse(string.IsNullOrWhiteSpace(row.ReinforceTitleEn));
                Assert.IsFalse(string.IsNullOrWhiteSpace(row.PromotionTitleKo));
                Assert.IsFalse(string.IsNullOrWhiteSpace(row.PromotionTitleEn));
            }
            CompanionCardLocalizationData shield = provider.GetCompanionCardLocalization("shield_guard");
            Assert.AreEqual("card.recruit.shield_guard.title", shield.RecruitTitleKey);
            Assert.AreEqual("방패병 소집", shield.RecruitTitleKo);
            Assert.AreEqual("Recruit Shield Guard", shield.RecruitTitleEn);
            Assert.AreEqual("방패 분대 병력이 1명 늘어납니다. {after_count}/3", shield.ReinforceDescKo);
            Assert.AreEqual("ui.card.badge.recruit", shield.RecruitBadgeKey);
            Assert.AreEqual("ui.card.badge.reinforce", shield.ReinforceBadgeKey);
            Assert.AreEqual("ui.card.badge.promote", shield.PromoteBadgeKey);

            CompanionCardLocalizationData skeleton = provider.GetCompanionCardLocalization("skeleton_bomber");
            Assert.AreEqual("해골 폭탄병 소집", skeleton.RecruitTitleKo);
            Assert.AreEqual("Promote to Bone Artillery!", skeleton.PromotionTitleEn);
            Assert.AreEqual("망자단 · 폭발단", skeleton.SynergyHintKo);
            Assert.IsNull(provider.GetCompanionCardLocalization("archer"));
            Assert.IsNull(provider.GetCompanionCardLocalization("shield_captain"));
        }

        [Test]
        public void Resolver_UsesCanonicalProgressLocalizationAndBasePortrait()
        {
            LocalDataProvider provider = CreateProjectProvider();
            Assert.IsTrue(provider.InitializeAsync().GetAwaiter().GetResult().Succeeded);
            UnitPresentationSet units = AssetDatabase.LoadAssetAtPath<UnitPresentationSet>("Assets/_LizzoPV/Data/Presentation/UnitPresentationSet.asset");
            Assert.IsNotNull(units);
            CanonicalCompanionCardPresentationResolver resolver = new CanonicalCompanionCardPresentationResolver(provider, units);
            ProgressView progress = new ProgressView();
            CardData card = new CardData(CardKind.RecruitArcher, "legacy", "legacy", CardHighlight.New, "falcon_archer");

            progress.Set("falcon_archer", 0, 1);
            Assert.IsTrue(resolver.TryResolve(card, progress, CompanionCardLanguage.Korean, out CanonicalCompanionCardPresentation recruit));
            Assert.AreEqual(CanonicalCompanionCardMode.Recruit, recruit.Mode);
            Assert.AreEqual("매사냥꾼 소집", recruit.Title);
            Assert.AreEqual("동료 소집", recruit.Badge);
            Assert.AreEqual("원거리 · 야수", recruit.RoleBadge);
            Assert.AreEqual("사격대 · 야수단", recruit.SynergyHint);
            Assert.AreEqual("falcon_archer", recruit.PortraitUnitId);
            Assert.IsNotNull(recruit.Portrait);

            progress.Set("falcon_archer", 1, 2);
            Assert.IsTrue(resolver.TryResolve(card, progress, CompanionCardLanguage.English, out CanonicalCompanionCardPresentation reinforce));
            Assert.AreEqual(CanonicalCompanionCardMode.Reinforce, reinforce.Mode);
            Assert.AreEqual("Reinforce Falcon Archer", reinforce.Title);
            Assert.AreEqual("Adds 1 soldier to the falcon squad. 2/3", reinforce.Description);
            Assert.AreEqual("Reinforce", reinforce.Badge);

            progress.Set("falcon_archer", 2, 3);
            Assert.IsTrue(resolver.TryResolve(card, progress, CompanionCardLanguage.Korean, out CanonicalCompanionCardPresentation promote));
            Assert.AreEqual(CanonicalCompanionCardMode.Promote, promote.Mode);
            Assert.AreEqual("매사냥 대장 진급!", promote.Title);
            Assert.AreEqual("부대 진급", promote.Badge);
            Assert.AreEqual("falcon_archer", promote.PortraitUnitId);
        }

        [Test]
        public void Resolver_RejectsMissingCanonicalAuthoringAndDoesNotUseLegacyIdentity()
        {
            LocalDataProvider provider = CreateProjectProvider();
            Assert.IsTrue(provider.InitializeAsync().GetAwaiter().GetResult().Succeeded);
            UnitPresentationSet units = AssetDatabase.LoadAssetAtPath<UnitPresentationSet>("Assets/_LizzoPV/Data/Presentation/UnitPresentationSet.asset");
            CanonicalCompanionCardPresentationResolver resolver = new CanonicalCompanionCardPresentationResolver(provider, units);
            ProgressView progress = new ProgressView();

            Assert.IsFalse(resolver.TryResolve(new CardData(CardKind.RecruitArcher, "legacy", "legacy", CardHighlight.New, "archer"), progress, CompanionCardLanguage.Korean, out _));
            Assert.IsFalse(resolver.TryResolve(new CardData(CardKind.RecruitArcher, "legacy", "legacy", CardHighlight.New, "falcon_captain"), progress, CompanionCardLanguage.Korean, out _));
        }

        [Test]
        public void CanonicalApply_UsesFalconCanonicalPathAndRejectsLegacyIdentity()
        {
            using CanonicalFalconCardFixture fixture = new CanonicalFalconCardFixture();
            FixedCardPool.Configure(fixture.Run.Registry, fixture.Run.Party);
            CardEffectRuntime.Configure(fixture.Run.Registry, fixture.Run.Party);

            CardData canonical = new CardData(CardKind.RecruitArcher, "legacy", "legacy", CardHighlight.New, "falcon_archer");
            Assert.IsTrue(FixedCardPool.TryApplyCard(canonical));
            Assert.AreEqual(1, fixture.Run.Party.ActiveCompanionSlotCount);
            Assert.AreEqual(1, fixture.Run.Party.ActiveCompanionCount);
            CompanionRuntime runtime = fixture.Factory.LiveInstances[0].GetComponent<CompanionRuntime>();
            Assert.AreEqual("falcon_archer", runtime.BaseUnitId);
            Assert.IsFalse(string.IsNullOrEmpty(runtime.RosterSlotId));
            StringAssert.Contains("ranged_family", runtime.FamilyTags);
            StringAssert.Contains("beast_family", runtime.FamilyTags);

            CardData legacyLeak = new CardData(CardKind.RecruitArcher, "legacy", "legacy", CardHighlight.New, "archer");
            Assert.IsFalse(FixedCardPool.TryApplyCard(legacyLeak));
            Assert.AreEqual(1, fixture.Run.Party.ActiveCompanionSlotCount);
            FixedCardPool.ClearServices();
            CardEffectRuntime.ClearServices();
        }

        sealed class CanonicalFalconCardFixture : System.IDisposable
        {
            static readonly FieldInfo ActiveProvider = typeof(PresentationCatalogProvider).GetField("_active", BindingFlags.Static | BindingFlags.NonPublic);

            readonly GameObject _root = new GameObject("CanonicalFalconCardFixture");
            readonly PresentationCatalogProvider _previousProvider;
            readonly PresentationCatalog _catalog;
            readonly GameObject _providerRoot;
            readonly TestAssetService _assets = new TestAssetService();

            public readonly LocalDataProvider Data;
            public readonly CanonicalFalconCardFactory Factory = new CanonicalFalconCardFactory();
            public readonly AppServices App;
            public readonly RunServices Run;

            public CanonicalFalconCardFixture()
            {
                _previousProvider = ActiveProvider.GetValue(null) as PresentationCatalogProvider;
                UnitPresentationSet units = AssetDatabase.LoadAssetAtPath<UnitPresentationSet>("Assets/_LizzoPV/Data/Presentation/UnitPresentationSet.asset");
                OwnedSupportPresentationSet supports = AssetDatabase.LoadAssetAtPath<OwnedSupportPresentationSet>("Assets/_LizzoPV/Data/Presentation/OwnedSupportPresentationSet.asset");
                _catalog = ScriptableObject.CreateInstance<PresentationCatalog>();
                _catalog.SetPresentationSetsForEditor(null, null, null, null, units, supports);
                _providerRoot = new GameObject("CanonicalFalconCardCatalog");
                _providerRoot.SetActive(false);
                PresentationCatalogProvider provider = _providerRoot.AddComponent<PresentationCatalogProvider>();
                SerializedObject serialized = new SerializedObject(provider);
                serialized.FindProperty("_catalog").objectReferenceValue = _catalog;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                ActiveProvider.SetValue(null, provider);

                _assets.Register("PlayerData.xml", AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_LizzoPV/Data/Runtime/GameData.xml"));
                Data = new LocalDataProvider(_assets);
                Assert.That(Data.InitializeAsync().GetAwaiter().GetResult().Succeeded, Is.True);
                App = new AppServices(_assets, Data);
                Run = new RunServices(App, new Lizzo.PV.Flow.RunState(), new RuntimeObjectRegistry(Factory), new ObjectPoolService(new GameObject("CanonicalFalconCardPool").transform), Factory);
                RetroSfx.Configure(_assets);
                RetroVfx.Configure(_assets, Factory);
                AttackVisual.Configure(Factory);
                FloatingDamageText.Configure(Factory);
                PlayerController player = _root.AddComponent<PlayerController>();
                player.MaxHp = 100;
                player.Hp = 100;
                Run.Registry.RegisterPlayer(player);
            }

            public void Dispose()
            {
                Run.Dispose();
                FloatingDamageText.ClearServices();
                AttackVisual.ClearServices();
                RetroVfx.ClearServices();
                RetroSfx.ClearServices();
                App.ReleaseAll();
                Factory.Clear();
                if (_providerRoot != null) Object.DestroyImmediate(_providerRoot);
                if (_catalog != null) Object.DestroyImmediate(_catalog);
                ActiveProvider.SetValue(null, _previousProvider);
                Object.DestroyImmediate(_root);
            }
        }

        sealed class CanonicalFalconCardFactory : IPrefabFactory
        {
            public readonly List<GameObject> LiveInstances = new List<GameObject>();

            public GameObject Spawn(string address, Transform parent = null, bool pooled = false)
            {
                if (address == "FloatingDamageText.prefab")
                {
                    GameObject label = new GameObject(address);
                    if (parent != null)
                        label.transform.SetParent(parent, false);

                    label.AddComponent<TextMeshPro>();
                    label.AddComponent<FloatingDamageText>();
                    LiveInstances.Add(label);
                    return label;
                }

                string unitId = address.Substring(address.LastIndexOf('/') + 1);
                UnitPresentationSet set = AssetDatabase.LoadAssetAtPath<UnitPresentationSet>("Assets/_LizzoPV/Data/Presentation/UnitPresentationSet.asset");
                if (!set.TryGetEntry(unitId, out UnitPresentationSet.Entry entry))
                    return null;

                GameObject instance = Object.Instantiate(entry.Prefab, parent);
                LiveInstances.Add(instance);
                return instance;
            }

            public GameObject Rent(GameObject prefab, string poolKey, Transform parent = null)
            {
                return Spawn(poolKey, parent, pooled: true);
            }

            public void Release(GameObject instance)
            {
                LiveInstances.Remove(instance);
                if (instance != null)
                    Object.DestroyImmediate(instance);
            }

            public void Clear()
            {
                for (int i = LiveInstances.Count - 1; i >= 0; i--)
                    Release(LiveInstances[i]);
            }
        }

        static LocalDataProvider CreateProjectProvider()
        {
            TestAssetService assets = new TestAssetService();
            assets.Register("PlayerData.xml", AssetDatabase.LoadAssetAtPath<UnityEngine.TextAsset>("Assets/_LizzoPV/Data/Runtime/GameData.xml"));
            return new LocalDataProvider(assets);
        }

        sealed class ProgressView : ICanonicalCompanionCardProgressView
        {
            readonly Dictionary<string, int[]> _values = new Dictionary<string, int[]>();
            public void Set(string unitId, int currentCount, int previewCount) => _values[unitId] = new[] { currentCount, previewCount };
            public bool TryGetCanonicalCompanionProgress(string baseUnitId, out int currentCount, out int previewCount)
            {
                if (_values.TryGetValue(baseUnitId, out int[] value))
                {
                    currentCount = value[0]; previewCount = value[1]; return true;
                }
                currentCount = 0; previewCount = 1; return true;
            }
        }
    }
}
