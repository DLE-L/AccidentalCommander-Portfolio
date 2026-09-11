using Lizzo.PV.Combat;
using Lizzo.PV.Gameplay.Units;
using System.Collections.Generic;
using System.Reflection;
using Lizzo.PV.Data;
using Lizzo.PV.Gameplay.CardOffer;
using Lizzo.PV.Legion;
using Lizzo.PV.Legion.Presentation;
using Lizzo.PV.Legion.RunCore;
using Lizzo.PV.Legion.RunCore.Presentation;
using Lizzo.PV.Gameplay.CardOffer;
using Lizzo.PV.Gameplay.Presentation;
using Lizzo.PV.Gameplay.Visuals;
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
            for (int i = 0;
            i < provider.CompanionRoster.Count;
            i++)
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

            CompanionCardLocalizationData skeleton = provider.GetCompanionCardLocalization("skeleton_scythe_thrower");
            Assert.AreEqual("해골 낫 투척병 소집", skeleton.RecruitTitleKo);
            Assert.AreEqual("가장 많은 적을 맞힐 직선으로 낫을 던져 왕복 피해를 줍니다.", skeleton.RecruitDescKo);
            Assert.AreEqual("Promote to Skeleton Reaper!", skeleton.PromotionTitleEn);
            Assert.AreEqual("원거리 · 왕복", skeleton.RoleBadgeKo);
            Assert.AreEqual("번개 · 늑대 연계", skeleton.SynergyHintKo);
            Assert.IsNull(provider.GetCompanionCardLocalization("archer"));
            Assert.IsNull(provider.GetCompanionCardLocalization("shield_captain"));
        }

        [Test]
        public void Resolver_UsesCanonicalProgressLocalizationAndBasePortrait()
        {
            LocalDataProvider provider = CreateProjectProvider();
            Assert.IsTrue(provider.InitializeAsync().GetAwaiter().GetResult().Succeeded);
            UnitPresentationSet units = AssetDatabase.LoadAssetAtPath<UnitPresentationSet>("Assets/_LizzoPV/Gameplay/Legion/Presentation/Data/UnitPresentationSet.asset");
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
            UnitPresentationSet units = AssetDatabase.LoadAssetAtPath<UnitPresentationSet>("Assets/_LizzoPV/Gameplay/Legion/Presentation/Data/UnitPresentationSet.asset");
            CanonicalCompanionCardPresentationResolver resolver = new CanonicalCompanionCardPresentationResolver(provider, units);
            ProgressView progress = new ProgressView();

            Assert.IsFalse(resolver.TryResolve(new CardData(CardKind.RecruitArcher, "legacy", "legacy", CardHighlight.New, "archer"), progress,
                CompanionCardLanguage.Korean, out _));
            Assert.IsFalse(resolver.TryResolve(new CardData(CardKind.RecruitArcher, "legacy", "legacy", CardHighlight.New, "falcon_captain"), progress,
                CompanionCardLanguage.Korean, out _));
        }

        [Test]
        public void CanonicalApply_RoutesFalconToCurrentInputAndRejectsLegacyIdentity()
        {
            using CanonicalFalconCardFixture fixture = new CanonicalFalconCardFixture();
            RecordingCompanionCardInput input = new RecordingCompanionCardInput();
            using CardOfferRuntime cardOffers = new CardOfferRuntime();
            cardOffers.Configure(
                fixture.Run.Registry,
                fixture.Run.Party,
                companionCardInput: input);

            CardData canonical = new CardData(CardKind.RecruitArcher, "legacy", "legacy", CardHighlight.New, "falcon_archer");
            Assert.IsTrue(cardOffers.TryApplyCard(canonical));
            Assert.AreEqual("falcon_archer", input.LastCompanionId);
            Assert.AreEqual(1, input.AcceptedCount);
            Assert.AreEqual(0, fixture.Run.Party.ActiveCompanionSlotCount);

            CardData legacyLeak = new CardData(CardKind.RecruitArcher, "legacy", "legacy", CardHighlight.New, "archer");
            Assert.IsFalse(cardOffers.TryApplyCard(legacyLeak));
            Assert.AreEqual(1, input.AcceptedCount);
        }

        sealed class RecordingCompanionCardInput : ICompanionCardInput
        {
            public int AcceptedCount { get; private set; }
            public string LastCompanionId { get; private set; }

            public CompanionRosterCommandResult SubmitCard(long sequence, string canonicalCompanionId)
            {
                if (canonicalCompanionId != "falcon_archer")
                {
                    return new CompanionRosterCommandResult(
                        false,
                        CompanionRosterRejection.InvalidCompanionId,
                        string.Empty,
                        -1);
                }

                AcceptedCount++;
                LastCompanionId = canonicalCompanionId;
                return new CompanionRosterCommandResult(
                    true,
                    CompanionRosterRejection.None,
                    "squad-test",
                    0);
            }
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
                UnitPresentationSet units = AssetDatabase.LoadAssetAtPath<UnitPresentationSet>("Assets/_LizzoPV/Gameplay/Legion/Presentation/Data/UnitPresentationSet.asset");
                OwnedSupportPresentationSet supports =
                    AssetDatabase.LoadAssetAtPath<OwnedSupportPresentationSet>(
                        "Assets/_LizzoPV/Gameplay/Legion/Data/Presentation/OwnedSupportPresentationSet.asset");
                CompanionRuntimePresentationSet companionRuntime =
                    AssetDatabase.LoadAssetAtPath<CompanionRuntimePresentationSet>(
                        "Assets/_LizzoPV/Gameplay/Legion/Presentation/Data/CompanionRuntimePresentationSet.asset");
                _catalog = ScriptableObject.CreateInstance<PresentationCatalog>();
                _catalog.SetPresentationSetsForEditor(null, units, supports, companionRuntime: companionRuntime);
                _providerRoot = new GameObject("CanonicalFalconPresentationCatalog");
                _providerRoot.SetActive(false);
                PresentationCatalogProvider provider = _providerRoot.AddComponent<PresentationCatalogProvider>();
                SerializedObject serialized = new SerializedObject(provider);
                serialized.FindProperty("_catalog").objectReferenceValue = _catalog;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                ActiveProvider.SetValue(null, provider);

                _assets.Register("PlayerData.xml", AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_LizzoPV/Gameplay/Run/Data/GameData.xml"));
                Data = new LocalDataProvider(_assets);
                Assert.That(Data.InitializeAsync().GetAwaiter().GetResult().Succeeded, Is.True);
                App = new AppServices(_assets, Data);
                Run = new RunServices(App, new Lizzo.PV.Flow.RunState(), new RuntimeObjectRegistry(Factory),
                    new ObjectPoolService(new GameObject("CanonicalFalconCardPool").transform), Factory);
                RetroVfx.Configure(_assets, Factory);
                FloatingDamageText.Configure(Factory);
                CommanderActor player = _root.AddComponent<CommanderActor>();
                player.RestoreHealth(player.Hp, 100);
                player.RestoreHealth(100);
                Run.Registry.RegisterPlayer(player);
            }

            public void Dispose()
            {
                Run.Dispose();
                FloatingDamageText.ClearServices();
                RetroVfx.ClearServices();
                RetroSfx.StopAndReset();
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
                PresentationCatalog catalog = AssetDatabase.LoadAssetAtPath<PresentationCatalog>(
                    "Assets/_LizzoPV/Gameplay/Presentation/Data/PresentationCatalog.asset");
                if (catalog == null
                    || catalog.CompanionRuntime == null
                    || !catalog.CompanionRuntime.TryGetSquadRoot(unitId, out CompanionSquadRoot prefab))
                    return null;

                GameObject instance = Object.Instantiate(prefab.gameObject, parent);
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
                for (int i = LiveInstances.Count - 1;
                i >= 0;
                i--)
                    Release(LiveInstances[i]);
            }
        }

        static LocalDataProvider CreateProjectProvider()
        {
            TestAssetService assets = new TestAssetService();
            assets.Register("PlayerData.xml", AssetDatabase.LoadAssetAtPath<UnityEngine.TextAsset>("Assets/_LizzoPV/Gameplay/Run/Data/GameData.xml"));
            return new LocalDataProvider(assets);
        }

        sealed class ProgressView : ICanonicalCompanionCardProgressView
        {
            readonly Dictionary<string, int[]> _values = new Dictionary<string, int[]>();
            public void Set(string unitId, int currentCount, int previewCount) => _values[unitId] = new[] {
                currentCount, previewCount }
            ;
            public bool TryGetCanonicalCompanionProgress(string baseUnitId, out int currentCount, out int previewCount)
            {
                if (_values.TryGetValue(baseUnitId, out int[] value))
                {
                    currentCount = value[0];
                    previewCount = value[1];
                    return true;
                }
                currentCount = 0;
                previewCount = 1;
                return true;
            }
        }
    }
}
