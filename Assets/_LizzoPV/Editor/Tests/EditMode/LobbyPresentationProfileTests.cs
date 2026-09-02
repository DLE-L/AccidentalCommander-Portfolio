using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Lizzo.PV.Lobby;
using Lizzo.PV.Presentation;
using NUnit.Framework;
using TMPro;
using UnityEngine;

namespace Lizzo.PV.EditorTests
{
    public sealed class LobbyPresentationProfileTests
    {
        [Test]
        public void LobbySection_PreservesSerializedOrderAndUsesCurrentNames()
        {
            CollectionAssert.AreEqual(
                new[] { "Shop", "Legion", "Departure", "Commander", "Challenge" },
                Enum.GetNames(typeof(LobbySection)));
            Assert.That((int)LobbySection.Shop, Is.EqualTo(0));
            Assert.That((int)LobbySection.Departure, Is.EqualTo(2));
            Assert.That((int)LobbySection.Commander, Is.EqualTo(3));
        }

        [Test]
        public void LobbyShell_ValidatesFiveTabsWithDepartureSelected()
        {
            LobbyShellPresentationProfileSO profile = Create<LobbyShellPresentationProfileSO>();
            try
            {
                profile.SetForEditor(
                    new AudioAssetId(1),
                    CreateTabBindings(),
                    SpriteAssetId.None,
                    new SpriteAssetId(20),
                    new LocalizationKey("ui.lobby.locked_feature"),
                    new AudioAssetId(21),
                    new MotionAssetId(22),
                    new MotionAssetId(23),
                    new AudioAssetId(24));

                Assert.That(profile.TryValidate(out string issue), Is.True, issue);
                Assert.That(profile.LockedToastSpriteId.IsNone, Is.True);
                Assert.That(profile.TryGetTab(LobbySection.Departure, out LobbyTabPresentationBinding departure), Is.True);
                Assert.That(departure.InitialState, Is.EqualTo(LobbyTabInitialState.Selected));
                Assert.That(departure.IconButtonStyleRole, Is.EqualTo(ControlStyleRole.IconButton));
            }
            finally
            {
                Destroy(profile);
            }
        }

        [Test]
        public void LobbyShell_RejectsDuplicateMissingAndWrongSelectedTab()
        {
            LobbyShellPresentationProfileSO profile = Create<LobbyShellPresentationProfileSO>();
            try
            {
                List<LobbyTabPresentationBinding> duplicate = CreateTabBindings().ToList();
                duplicate[4] = duplicate[0];
                SetShell(profile, duplicate);
                Assert.That(profile.TryValidate(out string duplicateIssue), Is.False);
                Assert.That(duplicateIssue, Does.Contain("Duplicate"));

                List<LobbyTabPresentationBinding> wrongSelected = CreateTabBindings().ToList();
                wrongSelected[1] = new LobbyTabPresentationBinding(
                    LobbySection.Legion,
                    new SpriteAssetId(11),
                    LobbyTabInitialState.Selected,
                    new MotionAssetId(31));
                wrongSelected[2] = new LobbyTabPresentationBinding(
                    LobbySection.Departure,
                    new SpriteAssetId(12),
                    LobbyTabInitialState.Locked,
                    new MotionAssetId(32));
                SetShell(profile, wrongSelected);
                Assert.That(profile.TryValidate(out string selectedIssue), Is.False);
                Assert.That(selectedIssue, Does.Contain("Departure"));
            }
            finally
            {
                Destroy(profile);
            }
        }

        [Test]
        public void DepartureProfile_AllowsOptionalShadowAndLoadingIndicator()
        {
            DepartureScreenPresentationSO profile = Create<DepartureScreenPresentationSO>();
            try
            {
                profile.SetForEditor(
                    new SpriteAssetId(1),
                    SpriteAssetId.None,
                    new MotionAssetId(2),
                    new AudioAssetId(3),
                    new AudioAssetId(4),
                    new MotionAssetId(5),
                    new MotionAssetId(6),
                    MotionAssetId.None);

                Assert.That(profile.TryValidate(out string issue), Is.True, issue);
                Assert.That(profile.CommanderShadowSpriteId.IsNone, Is.True);
                Assert.That(profile.LoadingIndicatorMotionId.IsNone, Is.True);
                Assert.That(profile.DepartureButtonStyleRole, Is.EqualTo(ControlStyleRole.PrimaryButton));
                Assert.That(
                    typeof(DepartureScreenPresentationSO)
                        .GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                        .Any(member => member.Name.Contains("CommanderId", StringComparison.Ordinal)),
                    Is.False);
            }
            finally
            {
                Destroy(profile);
            }
        }

        [Test]
        public void LobbySet_ComposesProfilesBundlesAndThemeWithoutOwningBootstrap()
        {
            var owned = new List<UnityEngine.Object>();
            try
            {
                LobbyShellPresentationProfileSO shell = Own(owned, Create<LobbyShellPresentationProfileSO>());
                SetShell(shell, CreateTabBindings());

                DepartureScreenPresentationSO departure = Own(owned, Create<DepartureScreenPresentationSO>());
                departure.SetForEditor(
                    new SpriteAssetId(40),
                    SpriteAssetId.None,
                    new MotionAssetId(41),
                    new AudioAssetId(42),
                    new AudioAssetId(43),
                    new MotionAssetId(44),
                    new MotionAssetId(45),
                    MotionAssetId.None);

                UiThemeProfileSO theme = CreateValidTheme(owned);
                AssetCatalogBundleSO shared = Own(owned, Create<AssetCatalogBundleSO>());
                AssetCatalogBundleSO lobby = Own(owned, Create<AssetCatalogBundleSO>());
                LobbyPresentationSetSO set = Own(owned, Create<LobbyPresentationSetSO>());
                set.SetForEditor(shell, departure, theme, shared, lobby);

                Assert.That(set.TryValidate(out string issue), Is.True, issue);
                Assert.That(
                    typeof(LobbyPresentationSetSO)
                        .GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                        .Any(member => member.Name.Contains("Bootstrap", StringComparison.Ordinal)),
                    Is.False);
            }
            finally
            {
                Destroy(owned.ToArray());
            }
        }

        private static IEnumerable<LobbyTabPresentationBinding> CreateTabBindings()
        {
            int index = 0;
            foreach (LobbySection section in Enum.GetValues(typeof(LobbySection)))
            {
                yield return new LobbyTabPresentationBinding(
                    section,
                    new SpriteAssetId(10 + index),
                    section == LobbySection.Departure ? LobbyTabInitialState.Selected : LobbyTabInitialState.Locked,
                    new MotionAssetId(30 + index));
                index++;
            }
        }

        private static void SetShell(
            LobbyShellPresentationProfileSO profile,
            IEnumerable<LobbyTabPresentationBinding> bindings)
        {
            profile.SetForEditor(
                new AudioAssetId(1),
                bindings,
                SpriteAssetId.None,
                new SpriteAssetId(20),
                new LocalizationKey("ui.lobby.locked_feature"),
                new AudioAssetId(21),
                new MotionAssetId(22),
                new MotionAssetId(23),
                new AudioAssetId(24));
        }

        private static UiThemeProfileSO CreateValidTheme(List<UnityEngine.Object> owned)
        {
            TMP_FontAsset font = Own(owned, Create<TMP_FontAsset>());
            TypographyProfileSO typography = Own(owned, Create<TypographyProfileSO>());
            typography.SetForEditor(
                Enum.GetValues(typeof(TypographyRole))
                    .Cast<TypographyRole>()
                    .Select(role => new TypographyProfileBinding(role, font)));

            ColorPaletteSO palette = Own(owned, Create<ColorPaletteSO>());
            palette.SetForEditor(Array.Empty<ColorPaletteEntry>());

            ControlStyleProfileSetSO styles = Own(owned, Create<ControlStyleProfileSetSO>());
            var bindings = new List<ControlStyleProfileBinding>();
            foreach (ControlStyleRole role in Enum.GetValues(typeof(ControlStyleRole)))
            {
                ControlStyleProfileSO profile = Own(owned, Create<ControlStyleProfileSO>());
                profile.SetForEditor(
                    new SpriteAssetId(101),
                    new SpriteAssetId(102),
                    role == ControlStyleRole.IconButton || role == ControlStyleRole.ChoiceCard
                        ? new SpriteAssetId(103)
                        : SpriteAssetId.None,
                    new SpriteAssetId(104),
                    role == ControlStyleRole.IconButton ? new SpriteAssetId(105) : SpriteAssetId.None,
                    role != ControlStyleRole.ChoiceCard ? new SpriteAssetId(106) : SpriteAssetId.None);
                bindings.Add(new ControlStyleProfileBinding(role, profile));
            }

            styles.SetForEditor(bindings);
            UiThemeProfileSO theme = Own(owned, Create<UiThemeProfileSO>());
            theme.SetForEditor(typography, palette, styles);
            return theme;
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
