using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Lizzo.PV.UI;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class UI_BossWarningOverlayTests
    {
        GameObject _root;
        UI_BossWarningOverlay _overlay;
        Image[] _accentImages;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("BossWarningOverlayTestRoot");
            _overlay = _root.AddComponent<UI_BossWarningOverlay>();
            CanvasGroup canvasGroup = _root.AddComponent<CanvasGroup>();

            GameObject warningTextObject = new GameObject("WarningText", typeof(RectTransform), typeof(TextMeshProUGUI));
            warningTextObject.transform.SetParent(_root.transform, false);

            _accentImages = new Image[3];
            float[] authoredAlphas = { 0.20f, 0.35f, 0.80f };
            for (int i = 0; i < _accentImages.Length; i++)
            {
                GameObject imageObject = new GameObject("Accent" + i, typeof(RectTransform), typeof(Image));
                imageObject.transform.SetParent(_root.transform, false);
                _accentImages[i] = imageObject.GetComponent<Image>();
                _accentImages[i].color = new Color(0.15f, 0.25f, 0.35f, authoredAlphas[i]);
            }

            SerializedObject serializedOverlay = new SerializedObject(_overlay);
            serializedOverlay.FindProperty("_root").objectReferenceValue = _root;
            serializedOverlay.FindProperty("_canvasGroup").objectReferenceValue = canvasGroup;
            serializedOverlay.FindProperty("_warningText").objectReferenceValue = warningTextObject.GetComponent<TMP_Text>();
            SerializedProperty accentProperty = serializedOverlay.FindProperty("_accentImages");
            accentProperty.arraySize = _accentImages.Length;
            for (int i = 0; i < _accentImages.Length; i++)
                accentProperty.GetArrayElementAtIndex(i).objectReferenceValue = _accentImages[i];
            serializedOverlay.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
        }

        [Test]
        public void InitThenShowAppliesAccentRgbAndPreservesEveryAuthoredAlphaAcrossRepeatedShows()
        {
            float[] authoredAlphas = { 0.20f, 0.35f, 0.80f };
            Color firstAccent = new Color(0.90f, 0.10f, 0.20f, 1.0f);
            Color secondAccent = new Color(0.10f, 0.80f, 0.30f, 1.0f);

            Assert.IsTrue(_overlay.Init());

            _overlay.Show("5", firstAccent, 1.05f, true);
            AssertAccentColors(firstAccent, authoredAlphas);

            _overlay.Show("4", secondAccent, 1.05f, true);
            AssertAccentColors(secondAccent, authoredAlphas);
        }

        void AssertAccentColors(Color expectedAccent, float[] authoredAlphas)
        {
            for (int i = 0; i < _accentImages.Length; i++)
            {
                Color actual = _accentImages[i].color;
                Assert.That(actual.r, Is.EqualTo(expectedAccent.r).Within(0.0001f));
                Assert.That(actual.g, Is.EqualTo(expectedAccent.g).Within(0.0001f));
                Assert.That(actual.b, Is.EqualTo(expectedAccent.b).Within(0.0001f));
                Assert.That(actual.a, Is.EqualTo(authoredAlphas[i]).Within(0.0001f));
            }
        }
    }
}
