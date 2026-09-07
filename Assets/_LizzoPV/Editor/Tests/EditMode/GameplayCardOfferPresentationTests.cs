using System;
using System.Reflection;
using Lizzo.PV.Gameplay.CardOffer;
using Lizzo.PV.Gameplay.CardOffer;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lizzo.PV.EditorTests
{
    public sealed class GameplayCardOfferPresentationTests
    {
        [Test]
        public void NonCompanionCard_DoesNotRecreateRemovedGeneratedIcon()
        {
            CardData card = new CardData(
                CardKind.BasicAttackUp,
                "공격 강화",
                "공격력을 강화합니다",
                CardHighlight.None);
            Sprite first = ResolvePortrait(card);
            Sprite second = ResolvePortrait(card);

            Assert.That(first, Is.Null);
            Assert.That(second, Is.Null);
        }

        [Test]
        public void CanonicalPassive_UsesCurrentToNextDescription()
        {
            CardData card = new CardData(
                CardKind.PassiveScoutingBanner,
                "장거리 훈련",
                "사거리 1.00 → 1.15",
                CardHighlight.None,
                canonicalPassiveId: "passive_scouting_banner");
            Type resolverType = typeof(GameplayCardOfferItemView).Assembly.GetType(
                "Lizzo.PV.UI.SkillCardPresentationResolver",
                throwOnError: true);
            using CardOfferRuntime cardOffers = new CardOfferRuntime();
            object model = resolverType.GetMethod("Resolve", BindingFlags.Public | BindingFlags.Static)
                .Invoke(null, new object[] { card, null, cardOffers });

            Assert.That(model.GetType().GetProperty("Description").GetValue(model), Is.EqualTo(card.Description));
        }

        [Test]
        public void Present_BindsContentProgressAndVisualStates()
        {
            using CardItemFixture fixture = new CardItemFixture();
            Sprite portrait = Sprite.Create(
                Texture2D.whiteTexture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f));

            try
            {
                GameplayCardOfferItemPresentation presentation = new GameplayCardOfferItemPresentation(
                    "guard_recruit",
                    "근위병",
                    "방패로 적을 밀어냅니다",
                    "방패 밀치기",
                    "동료 모집",
                    portrait,
                    "근위대 준비",
                    showProgress: true,
                    progressCount: 2,
                    recommended: true);

                Assert.That(fixture.View.Present(presentation), Is.True);
                Assert.That(fixture.Title.text, Is.EqualTo("근위병"));
                Assert.That(fixture.Description.text, Is.EqualTo("방패로 적을 밀어냅니다"));
                Assert.That(fixture.Value.text, Is.EqualTo("방패 밀치기"));
                Assert.That(fixture.Status.text, Is.EqualTo("동료 모집"));
                Assert.That(fixture.StatusBadge.activeSelf, Is.True);
                Assert.That(fixture.Portrait.sprite, Is.SameAs(portrait));
                Assert.That(fixture.RelationLabel.text, Is.EqualTo("근위대 준비"));
                Assert.That(fixture.Relation.activeSelf, Is.True);
                Assert.That(fixture.ProgressOn[0].activeSelf, Is.True);
                Assert.That(fixture.ProgressOn[1].activeSelf, Is.True);
                Assert.That(fixture.ProgressOff[2].activeSelf, Is.True);
                Assert.That(fixture.Recommended.activeSelf, Is.False);
                Assert.That(fixture.Button.interactable, Is.True);

                fixture.View.SetState(selected: true, disabled: false, recommended: false);
                Assert.That(fixture.Selected.activeSelf, Is.True);
                Assert.That(fixture.Disabled.activeSelf, Is.False);
                Assert.That(fixture.Recommended.activeSelf, Is.False);
                Assert.That(fixture.CanvasGroup.alpha, Is.EqualTo(1f));

                fixture.View.SetState(selected: false, disabled: true, recommended: false);
                Assert.That(fixture.Selected.activeSelf, Is.False);
                Assert.That(fixture.Disabled.activeSelf, Is.True);
                Assert.That(fixture.CanvasGroup.alpha, Is.EqualTo(0.22f).Within(0.0001f));
                Assert.That(fixture.Button.interactable, Is.False);
                Assert.That(fixture.CanvasGroup.blocksRaycasts, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(portrait);
            }
        }

        [Test]
        public void Clear_RemovesStaleContentAndRestoresEmptyInteractionState()
        {
            using CardItemFixture fixture = new CardItemFixture();
            Assert.That(fixture.View.Present(new GameplayCardOfferItemPresentation(
                "passive_attack",
                "공격 강화",
                "피해량 증가",
                "+15%",
                string.Empty,
                null,
                string.Empty,
                showProgress: true,
                progressCount: 3,
                recommended: false)), Is.True);

            fixture.View.Clear();

            Assert.That(fixture.Title.text, Is.Empty);
            Assert.That(fixture.Description.text, Is.Empty);
            Assert.That(fixture.Value.text, Is.Empty);
            Assert.That(fixture.StatusBadge.activeSelf, Is.False);
            Assert.That(fixture.Relation.activeSelf, Is.False);
            Assert.That(fixture.Portrait.enabled, Is.False);
            Assert.That(fixture.ProgressRoots[0].activeSelf, Is.False);
            Assert.That(fixture.ProgressRoots[1].activeSelf, Is.False);
            Assert.That(fixture.ProgressRoots[2].activeSelf, Is.False);
            Assert.That(fixture.Button.interactable, Is.False);
            Assert.That(fixture.CanvasGroup.blocksRaycasts, Is.False);
        }

        [Test]
        public void OfferView_PresentsTwoActiveItemsAndLeavesTheThirdInactiveWithoutRaycasts()
        {
            using CardOfferFixture fixture = new CardOfferFixture();
            fixture.View.ClearOffer();

            Assert.That(fixture.Items[0].Root.activeSelf, Is.False);
            Assert.That(fixture.Items[1].Root.activeSelf, Is.False);
            Assert.That(fixture.Items[2].Root.activeSelf, Is.False);

            Assert.That(fixture.View.PresentOfferSlot(0, CreatePresentation("trait_one")), Is.True);
            Assert.That(fixture.View.PresentOfferSlot(1, CreatePresentation("trait_two")), Is.True);

            Assert.That(fixture.Items[0].Root.activeSelf, Is.True);
            Assert.That(fixture.Items[1].Root.activeSelf, Is.True);
            Assert.That(fixture.Items[2].Root.activeSelf, Is.False);
            Assert.That(fixture.Items[2].Button.interactable, Is.False);
            Assert.That(fixture.Items[2].CanvasGroup.blocksRaycasts, Is.False);

            Assert.That(fixture.View.PresentOfferSlot(2, CreatePresentation("trait_three")), Is.True);
            Assert.That(fixture.Items[2].Root.activeSelf, Is.True);
        }

        private static GameplayCardOfferItemPresentation CreatePresentation(string cardId)
        {
            return new GameplayCardOfferItemPresentation(
                cardId,
                cardId,
                "description",
                "value",
                string.Empty,
                null,
                string.Empty,
                showProgress: false,
                progressCount: 0,
                recommended: false);
        }

        private static Sprite ResolvePortrait(CardData card)
        {
            Type resolverType = typeof(GameplayCardOfferItemView).Assembly.GetType(
                "Lizzo.PV.UI.SkillCardPresentationResolver",
                throwOnError: true);
            MethodInfo resolve = resolverType.GetMethod(
                "Resolve",
                BindingFlags.Public | BindingFlags.Static);
            Assert.That(resolve, Is.Not.Null);
            using CardOfferRuntime cardOffers = new CardOfferRuntime();
            object model = resolve.Invoke(null, new object[] { card, null, cardOffers });
            PropertyInfo portrait = model.GetType().GetProperty("Portrait");
            Assert.That(portrait, Is.Not.Null);
            return (Sprite)portrait.GetValue(model);
        }

        private sealed class CardOfferFixture : IDisposable
        {
            public readonly GameObject Root;
            public readonly GameplayCardOfferView View;
            public readonly CardItemFixture[] Items = new CardItemFixture[3];

            public CardOfferFixture()
            {
                Root = CreateRect("CardOffer").gameObject;
                Root.SetActive(false);
                View = Root.AddComponent<GameplayCardOfferView>();
                RectTransform header = CreateRect("Header", Root.transform);
                RectTransform row = CreateRect("CardRow", Root.transform);
                RectTransform blocker = CreateRect("ModalInputBlocker", Root.transform);
                for (int index = 0; index < Items.Length; index++)
                {
                    Items[index] = new CardItemFixture();
                    Items[index].Root.transform.SetParent(row, false);
                }

                SetField(View, "_header", header);
                SetField(View, "_cardRow", row);
                SetField(View, "_modalInputBlocker", blocker);
                SetField(View, "_cardItem01", Items[0].View);
                SetField(View, "_cardItem02", Items[1].View);
                SetField(View, "_cardItem03", Items[2].View);
                Root.SetActive(true);
            }

            public void Dispose()
            {
                UnityEngine.Object.DestroyImmediate(Root);
            }

            private static RectTransform CreateRect(string name, Transform parent = null)
            {
                GameObject item = new GameObject(name, typeof(RectTransform));
                if (parent != null)
                    item.transform.SetParent(parent, false);
                return item.GetComponent<RectTransform>();
            }

            private static void SetField(object target, string fieldName, object value)
            {
                FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
                if (field == null)
                    throw new MissingFieldException(target.GetType().Name, fieldName);
                field.SetValue(target, value);
            }
        }

        private sealed class CardItemFixture : IDisposable
        {
            public readonly GameObject Root;
            public readonly GameplayCardOfferItemView View;
            public readonly Button Button;
            public readonly CanvasGroup CanvasGroup;
            public readonly TMP_Text Title;
            public readonly TMP_Text Description;
            public readonly TMP_Text Value;
            public readonly TMP_Text Status;
            public readonly TMP_Text RelationLabel;
            public readonly Image Portrait;
            public readonly Image RelationIcon;
            public readonly GameObject StatusBadge;
            public readonly GameObject Relation;
            public readonly GameObject Selected;
            public readonly GameObject Disabled;
            public readonly GameObject Recommended;
            public readonly GameObject[] ProgressRoots = new GameObject[3];
            public readonly GameObject[] ProgressOff = new GameObject[3];
            public readonly GameObject[] ProgressOn = new GameObject[3];

            public CardItemFixture()
            {
                Root = CreateRect("CardItem").gameObject;
                Image rootGraphic = Root.AddComponent<Image>();
                rootGraphic.raycastTarget = true;
                Button = Root.AddComponent<Button>();
                Button.targetGraphic = rootGraphic;
                CanvasGroup = Root.AddComponent<CanvasGroup>();
                View = Root.AddComponent<GameplayCardOfferItemView>();

                RectTransform visual = CreateRect("Visual", Root.transform);
                RectTransform content = CreateRect("Content", Root.transform);
                StatusBadge = CreateRect("StatusBadge", content).gameObject;
                Status = CreateText("StatusText", StatusBadge.transform);
                Portrait = CreateRect("Portrait", content).gameObject.AddComponent<Image>();
                Portrait.raycastTarget = false;
                Title = CreateText("TitleText", content);
                Description = CreateText("DescriptionText", content);
                Value = CreateText("ValueText", content);
                Relation = CreateRect("RelationSynergy", content).gameObject;
                RelationIcon = CreateRect("Icon", Relation.transform).gameObject.AddComponent<Image>();
                RelationIcon.raycastTarget = false;
                RelationLabel = CreateText("LabelText", Relation.transform);

                RectTransform progress = CreateRect("ProgressSlots", content);
                for (int index = 0; index < ProgressRoots.Length; index++)
                {
                    ProgressRoots[index] = CreateRect("Slot" + index, progress).gameObject;
                    ProgressOff[index] = CreateRect("Off", ProgressRoots[index].transform).gameObject;
                    ProgressOn[index] = CreateRect("On", ProgressRoots[index].transform).gameObject;
                }

                RectTransform states = CreateRect("StateVisuals", visual);
                Selected = CreateRect("Selected", states).gameObject;
                Disabled = CreateRect("Disabled", states).gameObject;
                Recommended = CreateRect("Recommended", states).gameObject;

                SetField(View, "_slotIndex", 0);
                SetField(View, "_button", Button);
                SetField(View, "_canvasGroup", CanvasGroup);
                SetField(View, "_visual", visual);
                SetField(View, "_content", content);
                SetField(View, "_statusBadge", StatusBadge);
                SetField(View, "_statusText", Status);
                SetField(View, "_portrait", Portrait);
                SetField(View, "_titleText", Title);
                SetField(View, "_descriptionText", Description);
                SetField(View, "_valueText", Value);
                SetField(View, "_relationSynergy", Relation);
                SetField(View, "_relationIcon", RelationIcon);
                SetField(View, "_relationLabelText", RelationLabel);
                SetField(View, "_progressSlotRoots", ProgressRoots);
                SetField(View, "_progressOffVisuals", ProgressOff);
                SetField(View, "_progressOnVisuals", ProgressOn);
                SetField(View, "_selectedState", Selected);
                SetField(View, "_disabledState", Disabled);
                SetField(View, "_recommendedState", Recommended);
                Assert.That(View.Configure(), Is.True);
            }

            public void Dispose()
            {
                UnityEngine.Object.DestroyImmediate(Root);
            }

            private static RectTransform CreateRect(string name, Transform parent = null)
            {
                GameObject item = new GameObject(name, typeof(RectTransform));
                if (parent != null)
                    item.transform.SetParent(parent, false);
                return item.GetComponent<RectTransform>();
            }

            private static TMP_Text CreateText(string name, Transform parent)
            {
                TextMeshProUGUI text = CreateRect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
                text.raycastTarget = false;
                return text;
            }

            private static void SetField(object target, string fieldName, object value)
            {
                FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
                if (field == null)
                    throw new MissingFieldException(target.GetType().Name, fieldName);
                field.SetValue(target, value);
            }
        }
    }
}
