using System.IO;
using Lizzo.PV.Gameplay;
using Lizzo.PV.Gameplay.RunTraits;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Lizzo.PV.EditorTests
{
    public sealed class RunTraitHudStatusTests
    {
        const string TraitStatusItemPrefabPath = "Assets/_LizzoPV/Gameplay/UI/Prefabs/HUD/TraitStatusItem.prefab";
        const string GameplayScenePath = "Assets/_LizzoPV/Scenes/Gameplay.unity";
        const string TraitIconFolderPath = "Assets/_LizzoPV/Gameplay/RunTraits/Art/Icons";
        const string PauseEntryPlateSpriteGuid = "10ade63a7fb164f3dbf95a1ec3ffebfb";

        static readonly string[] TraitIconFileNames =
        {
            "trait_dangerous_march.png",
            "trait_elite_few.png",
            "trait_emergency_rally.png",
            "trait_fuse_link.png",
            "trait_moment_of_completion.png",
            "trait_promotion_shout.png",
        };

        GameObject _root;
        RunTraitRunState _runTraits;
        RunTraitEffectCoordinator _effects;
        TraitStatusRailController _rail;
        TraitStatusItemView[] _items;
        RunTraitPresentationCatalog _traitPresentationCatalog;
        Sprite[] _icons;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("TraitStatusRail", typeof(RectTransform));
            CreatePresentationCatalog();
            _runTraits = new RunTraitRunState();
            _effects = new RunTraitEffectCoordinator(_runTraits);
            _rail = _root.AddComponent<TraitStatusRailController>();
            _items = new[] { CreateItem("Item0"), CreateItem("Item1"), CreateItem("Item2") };
            SetItems(_rail, _items);
        }

        [TearDown]
        public void TearDown()
        {
            _effects.Dispose();
            _runTraits.Dispose();
            Object.DestroyImmediate(_traitPresentationCatalog);
            for (int index = 0; index < _icons.Length; index++)
            {
                Object.DestroyImmediate(_icons[index].texture);
                Object.DestroyImmediate(_icons[index]);
            }
            Object.DestroyImmediate(_root);
        }

        [Test]
        public void SelectedTraitsKeepOrderAtThreeAndOnlyTimedEffectsShowDuration()
        {
            Assert.IsTrue(_runTraits.TrySelect(RunTraitIds.FuseLink));
            Assert.IsTrue(_runTraits.TrySelect(RunTraitIds.PromotionShout));
            Assert.IsTrue(_runTraits.TrySelect(RunTraitIds.EmergencyRally));
            Assert.IsFalse(_runTraits.TrySelect(RunTraitIds.EliteFew));
            Assert.IsTrue(_rail.Bind(_runTraits, _effects, _traitPresentationCatalog));

            _rail.Refresh(10.0f);
            Assert.AreSame(_icons[0], _items[0].Icon);
            Assert.AreSame(_icons[2], _items[1].Icon);
            Assert.AreSame(_icons[3], _items[2].Icon);
            Assert.IsFalse(_items[0].IsDurationVisible);
            Assert.IsFalse(_effects.TryGetActiveDurationRatio(RunTraitIds.FuseLink, 10.0f, out _));
            Assert.IsFalse(_effects.TryGetActiveDurationRatio(RunTraitIds.MomentOfCompletion, 10.0f, out _));
            Assert.IsFalse(_effects.TryGetActiveDurationRatio(RunTraitIds.DangerousMarch, 10.0f, out _));
            Assert.IsFalse(_effects.TryGetActiveDurationRatio(RunTraitIds.EliteFew, 10.0f, out _));

            _effects.ReportPromotionCommitted(10.0f);
            _rail.Refresh(10.0f);
            Assert.IsTrue(_items[1].IsDurationVisible);
            Assert.AreEqual(1.0f, _items[1].DurationRatio, 0.0001f);
            _rail.Refresh(12.5f);
            Assert.AreEqual(0.5f, _items[1].DurationRatio, 0.0001f);
            _rail.Refresh(15.0f);
            Assert.IsFalse(_items[1].IsDurationVisible);

            Assert.IsTrue(_effects.TryActivateEmergencyRally(10, 100, null, 20.0f));
            _rail.Refresh(20.0f);
            Assert.IsTrue(_items[2].IsDurationVisible);
            Assert.AreEqual(1.0f, _items[2].DurationRatio, 0.0001f);
            _rail.Refresh(22.0f);
            Assert.AreEqual(0.5f, _items[2].DurationRatio, 0.0001f);
            _rail.Refresh(24.0f);
            Assert.IsFalse(_items[2].IsDurationVisible);
        }

        [Test]
        public void TraitIconPngsAreBinaryAlphaCenteredAndUseThirtyPixelDensity()
        {
            string absoluteFolder = Path.Combine(
                Directory.GetParent(Application.dataPath).FullName,
                TraitIconFolderPath.Replace('/', Path.DirectorySeparatorChar));
            string[] pngPaths = Directory.GetFiles(absoluteFolder, "*.png", SearchOption.TopDirectoryOnly);
            Assert.AreEqual(TraitIconFileNames.Length, pngPaths.Length);

            for (int iconIndex = 0; iconIndex < TraitIconFileNames.Length; iconIndex++)
            {
                string fileName = TraitIconFileNames[iconIndex];
                string absolutePath = Path.Combine(absoluteFolder, fileName);
                Assert.IsTrue(File.Exists(absolutePath), fileName);

                Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                try
                {
                    Assert.IsTrue(texture.LoadImage(File.ReadAllBytes(absolutePath), false), fileName);
                    Assert.AreEqual(512, texture.width, fileName);
                    Assert.AreEqual(512, texture.height, fileName);

                    Color32[] pixels = texture.GetPixels32();
                    int minX = texture.width;
                    int minY = texture.height;
                    int maxX = -1;
                    int maxY = -1;
                    int transparentPixelCount = 0;
                    int visiblePixelCount = 0;
                    for (int y = 0; y < texture.height; y++)
                    {
                        for (int x = 0; x < texture.width; x++)
                        {
                            byte alpha = pixels[y * texture.width + x].a;
                            if (alpha == 0)
                            {
                                transparentPixelCount++;
                                continue;
                            }

                            visiblePixelCount++;
                            minX = Mathf.Min(minX, x);
                            minY = Mathf.Min(minY, y);
                            maxX = Mathf.Max(maxX, x);
                            maxY = Mathf.Max(maxY, y);
                        }
                    }

                    Assert.Greater(transparentPixelCount, 0, fileName);
                    Assert.Greater(visiblePixelCount, 0, fileName);
                    Assert.GreaterOrEqual(maxX, minX, fileName);
                    Assert.GreaterOrEqual(maxY, minY, fileName);
                    int opaqueWidth = maxX - minX + 1;
                    int opaqueHeight = maxY - minY + 1;
                    Assert.GreaterOrEqual(Mathf.Max(opaqueWidth, opaqueHeight), texture.width * 0.80f, fileName);
                    Assert.LessOrEqual(Mathf.Max(opaqueWidth, opaqueHeight), texture.width * 0.95f, fileName);
                    Assert.LessOrEqual(Mathf.Abs(minX - (texture.width - 1 - maxX)), 1, fileName);
                    Assert.LessOrEqual(Mathf.Abs(minY - (texture.height - 1 - maxY)), 1, fileName);
                }
                finally
                {
                    Object.DestroyImmediate(texture);
                }
            }
        }

        [Test]
        public void TraitStatusItemPrefabIsExactIconOnlyOneHundredFourPixelContract()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(TraitStatusItemPrefabPath);
            try
            {
                Assert.AreEqual("TraitStatusItem", root.name);
                Assert.AreEqual(new Vector2(104.0f, 104.0f), root.GetComponent<RectTransform>().sizeDelta);
                Assert.IsNotNull(root.GetComponent<TraitStatusItemView>());
                Assert.IsNull(root.transform.Find("Content/Label"));
                Assert.AreEqual(0, root.GetComponentsInChildren<TMP_Text>(true).Length);

                Transform visual = root.transform.Find("Visual");
                Assert.IsNotNull(visual);
                Assert.AreEqual(0, visual.childCount);
                Assert.AreEqual(0, visual.GetComponentsInChildren<Graphic>(true).Length);
                AssertStretchFill(visual as RectTransform);

                RectTransform content = root.transform.Find("Content") as RectTransform;
                RectTransform state = root.transform.Find("State") as RectTransform;
                Assert.IsNotNull(content);
                Assert.IsNotNull(state);
                AssertStretchFill(content);
                AssertStretchFill(state);

                RectTransform icon = root.transform.Find("Content/Icon") as RectTransform;
                Assert.IsNotNull(icon);
                AssertCenteredSize(icon, new Vector2(96.0f, 96.0f));

                RectTransform durationTransform = root.transform.Find("State/DurationRadial") as RectTransform;
                Assert.IsNotNull(durationTransform);
                AssertCenteredSize(durationTransform, new Vector2(96.0f, 96.0f));
                Image durationRadial = durationTransform.GetComponent<Image>();
                Assert.IsNotNull(durationRadial);
                Assert.IsFalse(durationRadial.enabled);
                Assert.IsNull(durationRadial.sprite);
                Assert.AreEqual(0.0f, durationRadial.fillAmount);

                Graphic[] graphics = root.GetComponentsInChildren<Graphic>(true);
                Assert.AreEqual(2, graphics.Length);
                for (int index = 0; index < graphics.Length; index++)
                    Assert.IsFalse(graphics[index].raycastTarget, graphics[index].transform.name);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        [Test]
        public void TraitStatusRailIsCenteredBelowProgressBarWithoutSharedPlateOrDividers()
        {
            Scene scene = SceneManager.GetSceneByPath(GameplayScenePath);
            bool openedForTest = scene.IsValid() == false || scene.isLoaded == false;
            if (openedForTest)
                scene = EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Additive);

            try
            {
                GameObject gameplayUiRoot = FindSceneRoot(scene, "GameplayUIRoot");
                Assert.IsNotNull(gameplayUiRoot);
                Transform topbarContent = gameplayUiRoot.transform.Find("HUDLayer/HUD/Content/TopStatus/Content");
                Transform progressContent = gameplayUiRoot.transform.Find("HUDLayer/HUD/Content/ProgressStatus/Content");
                Assert.IsNotNull(topbarContent);
                Assert.IsNotNull(progressContent);
                Transform timer = topbarContent.Find("SurvivalTimer");
                RectTransform rail = progressContent.Find("TraitStatusRail") as RectTransform;
                Assert.IsNotNull(timer);
                Assert.IsNotNull(rail);
                Assert.IsNull(topbarContent.Find("TraitStatusRail"));
                Assert.AreEqual(new Vector2(312.0f, 104.0f), rail.sizeDelta);
                Assert.AreEqual(new Vector2(0.5f, 1.0f), rail.anchorMin);
                Assert.AreEqual(rail.anchorMin, rail.anchorMax);
                Assert.AreEqual(new Vector2(0.5f, 1.0f), rail.pivot);
                Assert.AreEqual(new Vector2(0.0f, -270.0f), rail.anchoredPosition);

                Bounds railBounds = CalculateRectBounds(progressContent as RectTransform, rail);
                Bounds experienceBounds = CalculateGraphicBounds(progressContent as RectTransform, progressContent.Find("Experience"));
                Bounds levelBounds = CalculateGraphicBounds(progressContent as RectTransform, progressContent.Find("LevelValue"));
                Assert.AreEqual(0.0f, railBounds.center.x, 0.01f);
                Assert.LessOrEqual(railBounds.max.y, experienceBounds.min.y);
                Assert.LessOrEqual(railBounds.max.y, levelBounds.min.y);

                Transform visual = rail.Find("Visual");
                Transform content = rail.Find("Content");
                Assert.IsNotNull(visual);
                Assert.IsNotNull(content);
                Assert.AreEqual(0, visual.GetSiblingIndex());
                Assert.AreEqual(1, content.GetSiblingIndex());
                AssertStretchFill(visual as RectTransform);
                AssertStretchFill(content as RectTransform);

                Image plate = visual.Find("Plate")?.GetComponent<Image>();
                Assert.IsNotNull(plate);
                Assert.IsFalse(plate.gameObject.activeSelf);

                Assert.IsFalse(visual.Find("Divider_01").gameObject.activeSelf);
                Assert.IsFalse(visual.Find("Divider_02").gameObject.activeSelf);

                Assert.AreEqual(3, visual.GetComponentsInChildren<Graphic>(true).Length);
                Assert.AreEqual(3, content.childCount);

                TraitStatusRailController controller = rail.GetComponent<TraitStatusRailController>();
                Assert.IsNotNull(controller);
                SerializedProperty items = new SerializedObject(controller).FindProperty("_items");
                Assert.AreEqual(3, items.arraySize);
                for (int index = 0; index < content.childCount; index++)
                {
                    RectTransform item = content.GetChild(index) as RectTransform;
                    Assert.IsNotNull(item);
                    Assert.AreEqual("TraitStatusItem", item.name);
                    Assert.AreEqual(new Vector2(104.0f, 104.0f), item.sizeDelta);
                    Assert.AreEqual(new Vector2(0.0f, 0.5f), item.anchorMin);
                    Assert.AreEqual(item.anchorMin, item.anchorMax);
                    Assert.AreEqual(new Vector2(0.0f, 0.5f), item.pivot);
                    Assert.AreEqual(new Vector2(index * 104.0f, 0.0f), item.anchoredPosition);
                    Assert.AreSame(item.GetComponent<TraitStatusItemView>(), items.GetArrayElementAtIndex(index).objectReferenceValue);
                }

                GameObject emptyRail = Object.Instantiate(rail.gameObject);
                try
                {
                    TraitStatusItemView[] emptyItems = emptyRail.GetComponentsInChildren<TraitStatusItemView>(true);
                    Assert.AreEqual(3, emptyItems.Length);
                    for (int index = 0; index < emptyItems.Length; index++)
                        emptyItems[index].SetInactive();

                    Image emptyPlate = emptyRail.transform.Find("Visual/Plate")?.GetComponent<Image>();
                    Assert.IsNotNull(emptyPlate);
                    Assert.IsFalse(emptyPlate.gameObject.activeSelf);
                }
                finally
                {
                    Object.DestroyImmediate(emptyRail);
                }

                Image pausePlate = topbarContent.Find("PauseEntry/Visual")?.GetComponent<Image>();
                Assert.IsNotNull(pausePlate);
                Assert.AreEqual(PauseEntryPlateSpriteGuid, AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(pausePlate.sprite)));
                Assert.AreSame(pausePlate.sprite, plate.sprite);
                Assert.AreEqual(Color.white, pausePlate.color);
                Assert.AreEqual(Image.Type.Sliced, pausePlate.type);

                AssertPreservedTopbarRect(topbarContent, "PauseEntry", new Vector2(-484.0f, -60.0f), new Vector2(84.0f, 83.0f));
                AssertPreservedTopbarRect(topbarContent, "SpeedEntry", new Vector2(474.0f, -60.0f), new Vector2(104.0f, 77.0f));
                AssertPreservedTopbarRect(topbarContent, "SurvivalTimer/Visual/Icon", new Vector2(-379.0f, -56.80005f), new Vector2(60.0f, 60.0f));
                AssertPreservedTopbarRect(topbarContent, "SurvivalTimer/Content/ValueText", new Vector2(-269.0f, -60.0f), new Vector2(236.0f, 56.0f));
                AssertPreservedTopbarRect(topbarContent, "KillCounter/Visual/Background", new Vector2(337.5f, -60.0f), new Vector2(145.0f, 64.0f));
                AssertPreservedTopbarRect(topbarContent, "KillCounter/Visual/Icon", new Vector2(294.0f, -60.0f), new Vector2(48.0f, 48.0f));
                AssertPreservedTopbarRect(topbarContent, "KillCounter/Content/ValueText", new Vector2(361.5f, -60.0f), new Vector2(77.0f, 48.0f));

                Bounds timerBounds = CalculateGraphicBounds(topbarContent as RectTransform, timer);
                Bounds pauseBounds = CalculateGraphicBounds(topbarContent as RectTransform, topbarContent.Find("PauseEntry"));
                Bounds killBounds = CalculateGraphicBounds(topbarContent as RectTransform, topbarContent.Find("KillCounter"));
                Bounds speedBounds = CalculateGraphicBounds(topbarContent as RectTransform, topbarContent.Find("SpeedEntry"));
                Assert.LessOrEqual(pauseBounds.max.x, timerBounds.min.x);
                Assert.LessOrEqual(killBounds.max.x, speedBounds.min.x);
            }
            finally
            {
                if (openedForTest)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void PresentationCatalogMapsAllSixCanonicalTraitIdsToDistinctIcons()
        {
            string[] traitIds =
            {
                RunTraitIds.FuseLink,
                RunTraitIds.MomentOfCompletion,
                RunTraitIds.PromotionShout,
                RunTraitIds.EmergencyRally,
                RunTraitIds.DangerousMarch,
                RunTraitIds.EliteFew,
            };

            Assert.IsTrue(_traitPresentationCatalog.TryValidate());
            for (int index = 0; index < traitIds.Length; index++)
            {
                Assert.IsTrue(_traitPresentationCatalog.TryResolve(traitIds[index], out Sprite icon));
                Assert.AreSame(_icons[index], icon);
                for (int otherIndex = index + 1; otherIndex < traitIds.Length; otherIndex++)
                    Assert.AreNotSame(icon, _icons[otherIndex]);
            }
        }

        void CreatePresentationCatalog()
        {
            _icons = new Sprite[6];
            for (int index = 0; index < _icons.Length; index++)
            {
                Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                texture.SetPixel(0, 0, new Color(index / 6.0f, 1.0f, 1.0f));
                texture.Apply();
                _icons[index] = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
            }

            _traitPresentationCatalog = ScriptableObject.CreateInstance<RunTraitPresentationCatalog>();
            string[] traitIds =
            {
                RunTraitIds.FuseLink,
                RunTraitIds.MomentOfCompletion,
                RunTraitIds.PromotionShout,
                RunTraitIds.EmergencyRally,
                RunTraitIds.DangerousMarch,
                RunTraitIds.EliteFew,
            };
            SerializedObject serializedTraits = new SerializedObject(_traitPresentationCatalog);
            SerializedProperty entries = serializedTraits.FindProperty("_entries");
            entries.arraySize = traitIds.Length;
            for (int index = 0; index < traitIds.Length; index++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(index);
                entry.FindPropertyRelative("_traitId").stringValue = traitIds[index];
                entry.FindPropertyRelative("_icon").objectReferenceValue = _icons[index];
            }
            serializedTraits.ApplyModifiedPropertiesWithoutUndo();

        }

        TraitStatusItemView CreateItem(string name)
        {
            GameObject item = new GameObject(name, typeof(RectTransform));
            item.transform.SetParent(_root.transform, false);
            TraitStatusItemView view = item.AddComponent<TraitStatusItemView>();

            GameObject iconObject = new GameObject("Icon", typeof(RectTransform));
            iconObject.transform.SetParent(item.transform, false);
            Image icon = iconObject.AddComponent<Image>();
            icon.raycastTarget = false;

            GameObject radialObject = new GameObject("DurationRadial", typeof(RectTransform));
            radialObject.transform.SetParent(item.transform, false);
            Image radial = radialObject.AddComponent<Image>();
            radial.raycastTarget = false;

            SetReference(view, "_icon", icon);
            SetReference(view, "_durationRadial", radial);
            return view;
        }

        static void SetItems(TraitStatusRailController rail, TraitStatusItemView[] items)
        {
            UnityEditor.SerializedObject serialized = new UnityEditor.SerializedObject(rail);
            UnityEditor.SerializedProperty property = serialized.FindProperty("_items");
            property.arraySize = items.Length;
            for (int index = 0; index < items.Length; index++)
                property.GetArrayElementAtIndex(index).objectReferenceValue = items[index];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetReference(Object target, string fieldName, Object value)
        {
            UnityEditor.SerializedObject serialized = new UnityEditor.SerializedObject(target);
            serialized.FindProperty(fieldName).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void AssertStretchFill(RectTransform rect)
        {
            Assert.IsNotNull(rect);
            Assert.AreEqual(Vector2.zero, rect.anchorMin);
            Assert.AreEqual(Vector2.one, rect.anchorMax);
            Assert.AreEqual(Vector2.zero, rect.sizeDelta);
        }

        static void AssertCenteredSize(RectTransform rect, Vector2 size)
        {
            Assert.AreEqual(new Vector2(0.5f, 0.5f), rect.anchorMin);
            Assert.AreEqual(rect.anchorMin, rect.anchorMax);
            Assert.AreEqual(new Vector2(0.5f, 0.5f), rect.pivot);
            Assert.AreEqual(Vector2.zero, rect.anchoredPosition);
            Assert.AreEqual(size, rect.sizeDelta);
        }

        static void AssertDivider(Transform visual, string name, float x)
        {
            Image divider = visual.Find(name)?.GetComponent<Image>();
            Assert.IsNotNull(divider, name);
            Assert.IsTrue(divider.gameObject.activeSelf, name);
            Assert.IsTrue(divider.enabled, name);
            Assert.IsNull(divider.sprite, name);
            Assert.AreEqual(Color.black, divider.color, name);
            Assert.AreEqual(Image.Type.Simple, divider.type, name);
            Assert.IsFalse(divider.raycastTarget, name);

            RectTransform rect = divider.rectTransform;
            Assert.AreEqual(new Vector2(0.0f, 0.5f), rect.anchorMin, name);
            Assert.AreEqual(rect.anchorMin, rect.anchorMax, name);
            Assert.AreEqual(new Vector2(0.5f, 0.5f), rect.pivot, name);
            Assert.AreEqual(new Vector2(x, 0.0f), rect.anchoredPosition, name);
            Assert.AreEqual(new Vector2(2.0f, 104.0f), rect.sizeDelta, name);
        }

        static GameObject FindSceneRoot(Scene scene, string rootName)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int index = 0; index < roots.Length; index++)
            {
                if (roots[index].name == rootName)
                    return roots[index];
            }

            return null;
        }

        static void AssertPreservedTopbarRect(
            Transform topbarContent,
            string relativePath,
            Vector2 expectedPosition,
            Vector2 expectedSize)
        {
            RectTransform rect = topbarContent.Find(relativePath) as RectTransform;
            Assert.IsNotNull(rect, relativePath);
            Assert.AreEqual(expectedPosition.x, rect.anchoredPosition.x, 0.001f, relativePath);
            Assert.AreEqual(expectedPosition.y, rect.anchoredPosition.y, 0.001f, relativePath);
            Assert.AreEqual(expectedSize, rect.sizeDelta, relativePath);
        }

        static Bounds CalculateGraphicBounds(RectTransform relativeTo, Transform root)
        {
            Graphic[] graphics = root.GetComponentsInChildren<Graphic>(true);
            Assert.Greater(graphics.Length, 0, root.name);

            Bounds bounds = default;
            bool initialized = false;
            Vector3[] corners = new Vector3[4];
            for (int graphicIndex = 0; graphicIndex < graphics.Length; graphicIndex++)
            {
                graphics[graphicIndex].rectTransform.GetWorldCorners(corners);
                for (int cornerIndex = 0; cornerIndex < corners.Length; cornerIndex++)
                {
                    Vector3 point = relativeTo.InverseTransformPoint(corners[cornerIndex]);
                    if (initialized)
                        bounds.Encapsulate(point);
                    else
                    {
                        bounds = new Bounds(point, Vector3.zero);
                        initialized = true;
                    }
                }
            }

            return bounds;
        }

        static Bounds CalculateRectBounds(RectTransform relativeTo, RectTransform rect)
        {
            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            Bounds bounds = new Bounds(relativeTo.InverseTransformPoint(corners[0]), Vector3.zero);
            for (int index = 1; index < corners.Length; index++)
                bounds.Encapsulate(relativeTo.InverseTransformPoint(corners[index]));
            return bounds;
        }
    }
}
