using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Lizzo.PV.Presentation;
using NUnit.Framework;
using TMPro;
using UnityEngine;

namespace Lizzo.PV.EditorTests
{
    public sealed class SharedUiThemeProfileTests
    {
        [Test]
        public void TypographyRoles_MatchCurrentM1Contract()
        {
            string[] roles = Enum.GetNames(typeof(TypographyRole));

            CollectionAssert.AreEquivalent(
                new[]
                {
                    "DisplayTitle",
                    "ContentTitle",
                    "ButtonLabel",
                    "StatusTitle",
                    "Body",
                    "Metadata",
                    "EffectNumber",
                    "HudResourceNumber",
                    "Timer",
                },
                roles);
            Assert.That(roles, Does.Not.Contain("Medium"));
        }

        [Test]
        public void TypographyProfile_RequiresEveryRoleExactlyOnce()
        {
            TypographyProfileSO profile = Create<TypographyProfileSO>();
            TMP_FontAsset font = Create<TMP_FontAsset>();
            try
            {
                profile.SetForEditor(CreateTypographyBindings(font));
                Assert.That(profile.TryValidate(out string validIssue), Is.True, validIssue);
                Assert.That(profile.TryGet(TypographyRole.Timer, out TypographyProfileBinding timer), Is.True);
                Assert.That(timer.FontAsset, Is.SameAs(font));

                profile.SetForEditor(new[]
                {
                    new TypographyProfileBinding(TypographyRole.DisplayTitle, font),
                    new TypographyProfileBinding(TypographyRole.DisplayTitle, font),
                });
                Assert.That(profile.TryValidate(out string duplicateIssue), Is.False);
                Assert.That(duplicateIssue, Does.Contain("Duplicate"));

                profile.SetForEditor(CreateTypographyBindings(font).Where(binding => binding.Role != TypographyRole.Timer));
                Assert.That(profile.TryValidate(out string missingIssue), Is.False);
                Assert.That(missingIssue, Does.Contain(nameof(TypographyRole.Timer)));
            }
            finally
            {
                Destroy(profile, font);
            }
        }

        [Test]
        public void ColorPalette_UsesSemanticKeysWithoutFreezingUnapprovedRgbRoles()
        {
            var role = new ColorRole("world.damage.enemy");
            ColorPaletteSO palette = Create<ColorPaletteSO>();
            try
            {
                Assert.That(role.IsNone, Is.False);
                Assert.That(ColorRole.None.IsNone, Is.True);
                Assert.That(typeof(ColorRole).GetMethod("op_Implicit"), Is.Null);

                palette.SetForEditor(new[]
                {
                    new ColorPaletteEntry(role, Color.red),
                    new ColorPaletteEntry(new ColorRole("world.heal"), Color.green),
                });
                Assert.That(palette.TryValidate(out string validIssue), Is.True, validIssue);
                Assert.That(palette.TryGet(role, out Color color), Is.True);
                Assert.That(color, Is.EqualTo(Color.red));

                palette.SetForEditor(new[]
                {
                    new ColorPaletteEntry(role, Color.red),
                    new ColorPaletteEntry(role, Color.blue),
                });
                Assert.That(palette.TryValidate(out string duplicateIssue), Is.False);
                Assert.That(duplicateIssue, Does.Contain("Duplicate"));
            }
            finally
            {
                Destroy(palette);
            }
        }

        [TestCase(ControlStyleRole.PrimaryButton)]
        [TestCase(ControlStyleRole.DestructiveButton)]
        [TestCase(ControlStyleRole.IconButton)]
        [TestCase(ControlStyleRole.ChoiceCard)]
        public void ControlStyleProfile_ValidatesRoleSpecificRequiredStates(ControlStyleRole role)
        {
            ControlStyleProfileSO profile = CreateStyle(role);
            try
            {
                Assert.That(profile.TryValidate(role, out string issue), Is.True, issue);

                bool selectedExpected = role == ControlStyleRole.IconButton || role == ControlStyleRole.ChoiceCard;
                bool lockedExpected = role == ControlStyleRole.IconButton;
                bool processingExpected = role != ControlStyleRole.ChoiceCard;
                Assert.That(profile.TryGet(ControlVisualState.Selected, out _), Is.EqualTo(selectedExpected));
                Assert.That(profile.TryGet(ControlVisualState.Locked, out _), Is.EqualTo(lockedExpected));
                Assert.That(profile.TryGet(ControlVisualState.Processing, out _), Is.EqualTo(processingExpected));
            }
            finally
            {
                Destroy(profile);
            }
        }

        [Test]
        public void ControlStyleSet_RejectsDuplicateAndMissingRoles()
        {
            var owned = new List<UnityEngine.Object>();
            ControlStyleProfileSetSO set = Own(owned, Create<ControlStyleProfileSetSO>());
            try
            {
                set.SetForEditor(CreateStyleBindings(owned));
                Assert.That(set.TryValidate(out string validIssue), Is.True, validIssue);
                Assert.That(set.TryGet(ControlStyleRole.IconButton, out ControlStyleProfileSO icon), Is.True);
                Assert.That(icon, Is.Not.Null);

                ControlStyleProfileSO primary = CreateStyle(ControlStyleRole.PrimaryButton);
                Own(owned, primary);
                set.SetForEditor(new[]
                {
                    new ControlStyleProfileBinding(ControlStyleRole.PrimaryButton, primary),
                    new ControlStyleProfileBinding(ControlStyleRole.PrimaryButton, primary),
                });
                Assert.That(set.TryValidate(out string duplicateIssue), Is.False);
                Assert.That(duplicateIssue, Does.Contain("Duplicate"));
            }
            finally
            {
                Destroy(owned.ToArray());
            }
        }

        [Test]
        public void LoadingSet_ValidatesComposedProfilesAndKeepsBootstrapAsConsumer()
        {
            var owned = new List<UnityEngine.Object>();
            try
            {
                TMP_FontAsset font = Own(owned, Create<TMP_FontAsset>());
                TypographyProfileSO typography = Own(owned, Create<TypographyProfileSO>());
                typography.SetForEditor(CreateTypographyBindings(font));

                ColorPaletteSO palette = Own(owned, Create<ColorPaletteSO>());
                palette.SetForEditor(Array.Empty<ColorPaletteEntry>());

                ControlStyleProfileSetSO styles = Own(owned, Create<ControlStyleProfileSetSO>());
                styles.SetForEditor(CreateStyleBindings(owned));

                UiThemeProfileSO theme = Own(owned, Create<UiThemeProfileSO>());
                theme.SetForEditor(typography, palette, styles);

                StartLoadingPresentationProfileSO start = Own(owned, Create<StartLoadingPresentationProfileSO>());
                start.SetForEditor(
                    SpriteId(1), SpriteId(2), SpriteId(3), new AudioAssetId(4),
                    new MotionAssetId(5), new MotionAssetId(6), new MotionAssetId(7), 0f, 1f, 0f);

                TransitionLoadingPresentationProfileSO transition = Own(owned, Create<TransitionLoadingPresentationProfileSO>());
                transition.SetForEditor(
                    SpriteId(8), SpriteId(9), SpriteId(10), new AudioAssetId(11), new AudioAssetId(12),
                    new MotionAssetId(13), new MotionAssetId(14), new MotionAssetId(15), 0f, 1f, 0f);

                LoadingErrorPresentationProfileSO error = Own(owned, Create<LoadingErrorPresentationProfileSO>());
                error.SetForEditor(
                    SpriteAssetId.None,
                    new AudioAssetId(16),
                    new AudioAssetId(17),
                    new MotionAssetId(18),
                    new MotionAssetId(19),
                    new MotionAssetId(20));
                Assert.That(error.TryValidate(out string errorIssue), Is.True, errorIssue);

                AssetCatalogBundleSO core = Own(owned, Create<AssetCatalogBundleSO>());
                AssetCatalogBundleSO shared = Own(owned, Create<AssetCatalogBundleSO>());
                LoadingPresentationSetSO loadingSet = Own(owned, Create<LoadingPresentationSetSO>());
                loadingSet.SetForEditor(start, transition, error, theme, core, shared);

                Assert.That(loadingSet.TryValidate(out string issue), Is.True, issue);
                Assert.That(
                    typeof(LoadingPresentationSetSO)
                        .GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                        .Any(member => member.Name.Contains("Bootstrap", StringComparison.Ordinal)),
                    Is.False);
            }
            finally
            {
                Destroy(owned.ToArray());
            }
        }

        [Test]
        public void ControlEnums_DoNotContainRecommended()
        {
            Assert.That(Enum.GetNames(typeof(ControlVisualState)), Does.Not.Contain("Recommended"));
            Assert.That(Enum.GetNames(typeof(ControlStyleRole)), Does.Not.Contain("Recommended"));
        }

        private static IEnumerable<TypographyProfileBinding> CreateTypographyBindings(TMP_FontAsset font)
        {
            foreach (TypographyRole role in Enum.GetValues(typeof(TypographyRole)))
            {
                yield return new TypographyProfileBinding(role, font);
            }
        }

        private static IEnumerable<ControlStyleProfileBinding> CreateStyleBindings(List<UnityEngine.Object> owned)
        {
            foreach (ControlStyleRole role in Enum.GetValues(typeof(ControlStyleRole)))
            {
                ControlStyleProfileSO profile = CreateStyle(role);
                Own(owned, profile);
                yield return new ControlStyleProfileBinding(role, profile);
            }
        }

        private static ControlStyleProfileSO CreateStyle(ControlStyleRole role)
        {
            ControlStyleProfileSO profile = Create<ControlStyleProfileSO>();
            profile.SetForEditor(
                SpriteId(101),
                SpriteId(102),
                role == ControlStyleRole.IconButton || role == ControlStyleRole.ChoiceCard ? SpriteId(103) : SpriteAssetId.None,
                SpriteId(104),
                role == ControlStyleRole.IconButton ? SpriteId(105) : SpriteAssetId.None,
                role != ControlStyleRole.ChoiceCard ? SpriteId(106) : SpriteAssetId.None);
            return profile;
        }

        private static SpriteAssetId SpriteId(int value)
        {
            return new SpriteAssetId(value);
        }

        private static T Create<T>() where T : ScriptableObject
        {
            return ScriptableObject.CreateInstance<T>();
        }

        private static T Own<T>(List<UnityEngine.Object> owned, T value) where T : UnityEngine.Object
        {
            owned.Add(value);
            return value;
        }

        private static void Destroy(params UnityEngine.Object[] objects)
        {
            for (int i = objects.Length - 1; i >= 0; i--)
            {
                if (objects[i] != null)
                {
                    UnityEngine.Object.DestroyImmediate(objects[i]);
                }
            }
        }
    }
}
