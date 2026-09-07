using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Lizzo.PV.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace Lizzo.PV.EditorTests
{
    public sealed class GameplayOverlayPresentationProfileTests
    {
        [Test]
        public void CardOffer_AllowsOptionalFrameSpritesAndHasNoRecommendedState()
        {
            CardOfferPresentationProfileSO profile = Create<CardOfferPresentationProfileSO>();
            try
            {
                SetCardOffer(profile, 0.2f);

                Assert.That(profile.TryValidate(out string issue), Is.True, issue);
                Assert.That(profile.DimmerSpriteId.IsNone, Is.True);
                Assert.That(profile.HeaderSpriteId.IsNone, Is.True);
                Assert.That(profile.ChoiceCardStyleRole, Is.EqualTo(ControlStyleRole.ChoiceCard));
                Assert.That(
                    typeof(CardOfferPresentationProfileSO)
                        .GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                        .Any(member => member.Name.Contains("Recommended", StringComparison.Ordinal)),
                    Is.False);

                SetCardOffer(profile, 0f);
                Assert.That(profile.TryValidate(out string timingIssue), Is.False);
                Assert.That(timingIssue, Does.Contain("AcceptedDisplaySeconds"));
            }
            finally
            {
                Destroy(profile);
            }
        }

        [Test]
        public void NotificationProfile_AllowsOnlyCombatPhaseOptionalCueSlots()
        {
            GameplayNotificationPresentationProfileSO profile = Create<GameplayNotificationPresentationProfileSO>();
            try
            {
                profile.SetForEditor(
                    new CombatPhasePresentation(
                        SpriteAssetId.None,
                        new LocalizationKey("ui.combat.phase"),
                        AudioAssetId.None,
                        new MotionAssetId(1),
                        MotionAssetId.None,
                        new MotionAssetId(2),
                        1f),
                    new EliteAlertPresentation(
                        new SpriteAssetId(3),
                        new LocalizationKey("ui.combat.elite"),
                        new AudioAssetId(4),
                        new MotionAssetId(5),
                        new MotionAssetId(6),
                        new MotionAssetId(7),
                        1f),
                    new BossWarningPresentation(
                        new SpriteAssetId(8),
                        new SpriteAssetId(9),
                        new LocalizationKey("ui.combat.boss"),
                        new AudioAssetId(10),
                        new MotionAssetId(11),
                        new MotionAssetId(12),
                        new MotionAssetId(13),
                        1f),
                    0f);

                Assert.That(profile.TryValidate(out string issue), Is.True, issue);
                Assert.That(profile.CombatPhasePresentation.FrameSpriteId.IsNone, Is.True);
                Assert.That(profile.CombatPhasePresentation.AlertSfxId.IsNone, Is.True);
                Assert.That(profile.CombatPhasePresentation.PulseMotionId.IsNone, Is.True);
            }
            finally
            {
                Destroy(profile);
            }
        }

        [Test]
        public void PauseProfile_UsesFixedTitleAndButtonRolesWithOptionalDimmer()
        {
            PausePresentationProfileSO profile = Create<PausePresentationProfileSO>();
            try
            {
                profile.SetForEditor(
                    SpriteAssetId.None,
                    new SpriteAssetId(1),
                    new LocalizationKey("ui.pause.synergy.empty"),
                    new AudioAssetId(2),
                    new AudioAssetId(3),
                    new AudioAssetId(4),
                    new MotionAssetId(5),
                    new MotionAssetId(6),
                    new MotionAssetId(7),
                    new MotionAssetId(8),
                    new MotionAssetId(9));

                Assert.That(profile.TryValidate(out string issue), Is.True, issue);
                Assert.That(profile.TitleLocalizationKey.Value, Is.EqualTo("ui.pause.title"));
                Assert.That(profile.ResumeButtonStyleRole, Is.EqualTo(ControlStyleRole.PrimaryButton));
                Assert.That(profile.AbandonButtonStyleRole, Is.EqualTo(ControlStyleRole.DestructiveButton));
            }
            finally
            {
                Destroy(profile);
            }
        }

        [Test]
        public void RunResultSet_ReusesOneOutcomeProfileTypeAndAllowsOptionalResultBgm()
        {
            var owned = new List<UnityEngine.Object>();
            try
            {
                RunResultSharedPresentationProfileSO shared = Own(owned, Create<RunResultSharedPresentationProfileSO>());
                shared.SetForEditor(
                    SpriteAssetId.None,
                    new LocalizationKey("ui.result.main"),
                    new AudioAssetId(1),
                    new AudioAssetId(2),
                    new MotionAssetId(3),
                    new MotionAssetId(4),
                    new MotionAssetId(5),
                    0.05f);

                RunResultPresentationProfileSO victory = CreateOutcome(owned, "victory", 10);
                RunResultPresentationProfileSO failure = CreateOutcome(owned, "failure", 20);
                RunResultPresentationProfileSO abandoned = CreateOutcome(owned, "abandoned", 30);
                RunResultPresentationSetSO set = Own(owned, Create<RunResultPresentationSetSO>());
                set.SetForEditor(shared, victory, failure, abandoned);

                Assert.That(set.TryValidate(out string issue), Is.True, issue);
                Assert.That(set.VictoryProfile.GetType(), Is.EqualTo(typeof(RunResultPresentationProfileSO)));
                Assert.That(set.FailureProfile.GetType(), Is.EqualTo(typeof(RunResultPresentationProfileSO)));
                Assert.That(set.AbandonedProfile.GetType(), Is.EqualTo(typeof(RunResultPresentationProfileSO)));
                Assert.That(victory.ResultBgmId.IsNone, Is.True);
            }
            finally
            {
                Destroy(owned.ToArray());
            }
        }

        [Test]
        public void GameplaySet_DeclaresRoleProfilesWithoutBootstrapOrWorldFeedbackState()
        {
            GameplayPresentationSetSO set = Create<GameplayPresentationSetSO>();
            try
            {
                Assert.That(set.TryValidate(out string issue), Is.False);
                Assert.That(issue, Does.Contain("requires all role Profiles"));

                MemberInfo[] members = typeof(GameplayPresentationSetSO)
                    .GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                Assert.That(members.Any(member => member.Name.Contains("Bootstrap", StringComparison.Ordinal)), Is.False);
                Assert.That(members.Any(member => member.Name.Contains("WorldFeedback", StringComparison.Ordinal)), Is.False);
                Assert.That(typeof(GameplayPresentationSetSO).GetProperty(nameof(set.CardOfferProfile)), Is.Not.Null);
                Assert.That(typeof(GameplayPresentationSetSO).GetProperty(nameof(set.RunResultSet)), Is.Not.Null);
            }
            finally
            {
                Destroy(set);
            }
        }

        private static void SetCardOffer(CardOfferPresentationProfileSO profile, float acceptedDisplaySeconds)
        {
            profile.SetForEditor(
                SpriteAssetId.None,
                SpriteAssetId.None,
                new LocalizationKey("ui.card_offer.title"),
                new SpriteAssetId(1),
                new SpriteAssetId(2),
                new SpriteAssetId(3),
                new AudioAssetId(4),
                new AudioAssetId(5),
                new AudioAssetId(6),
                new MotionAssetId(7),
                new MotionAssetId(8),
                new MotionAssetId(9),
                new MotionAssetId(10),
                acceptedDisplaySeconds);
        }

        private static RunResultPresentationProfileSO CreateOutcome(
            List<UnityEngine.Object> owned,
            string keySuffix,
            int firstId)
        {
            RunResultPresentationProfileSO profile = Own(owned, Create<RunResultPresentationProfileSO>());
            profile.SetForEditor(
                new LocalizationKey($"ui.result.{keySuffix}"),
                new SpriteAssetId(firstId),
                new SpriteAssetId(firstId + 1),
                new SpriteAssetId(firstId + 2),
                new ColorRole($"result.{keySuffix}"),
                new AudioAssetId(firstId + 3),
                AudioAssetId.None,
                new MotionAssetId(firstId + 4),
                0f);
            return profile;
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
