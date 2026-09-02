using System;
using System.Linq;
using System.Reflection;
using Lizzo.PV.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace Lizzo.PV.EditorTests
{
    public sealed class LoadingPresentationProfileTests
    {
        [Test]
        public void LocalizationKey_IsTypedAndHasNoImplicitStringConversion()
        {
            var key = new LocalizationKey("ui.loading.retry");

            Assert.That(key.IsNone, Is.False);
            Assert.That(key.Value, Is.EqualTo("ui.loading.retry"));
            Assert.That(LocalizationKey.None.IsNone, Is.True);
            Assert.That(typeof(LocalizationKey).GetMethod("op_Implicit"), Is.Null);
        }

        [Test]
        public void StartProfile_ValidatesRequiredIdsAndDoesNotOwnLobbyBgm()
        {
            StartLoadingPresentationProfileSO profile = ScriptableObject.CreateInstance<StartLoadingPresentationProfileSO>();
            try
            {
                profile.SetForEditor(
                    new SpriteAssetId(1),
                    new SpriteAssetId(2),
                    new SpriteAssetId(3),
                    new AudioAssetId(4),
                    new MotionAssetId(5),
                    new MotionAssetId(6),
                    new MotionAssetId(7),
                    0.5f,
                    8f,
                    0.1f);

                Assert.That(profile.TryValidate(out string issue), Is.True, issue);
                Assert.That(profile.CompleteSfxId.Value, Is.EqualTo(4));
                Assert.That(
                    typeof(StartLoadingPresentationProfileSO)
                        .GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                        .Any(member => member.Name.Contains("LobbyBgm", StringComparison.Ordinal)),
                    Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void StartProfile_RejectsNoneAndInvalidTiming()
        {
            StartLoadingPresentationProfileSO profile = ScriptableObject.CreateInstance<StartLoadingPresentationProfileSO>();
            try
            {
                profile.SetForEditor(
                    SpriteAssetId.None,
                    new SpriteAssetId(2),
                    new SpriteAssetId(3),
                    new AudioAssetId(4),
                    new MotionAssetId(5),
                    new MotionAssetId(6),
                    new MotionAssetId(7),
                    0f,
                    1f,
                    0f);

                Assert.That(profile.TryValidate(out string missingIdIssue), Is.False);
                Assert.That(missingIdIssue, Does.Contain(nameof(profile.BackgroundSpriteId)));

                profile.SetForEditor(
                    new SpriteAssetId(1),
                    new SpriteAssetId(2),
                    new SpriteAssetId(3),
                    new AudioAssetId(4),
                    new MotionAssetId(5),
                    new MotionAssetId(6),
                    new MotionAssetId(7),
                    0f,
                    0f,
                    0f);

                Assert.That(profile.TryValidate(out string timingIssue), Is.False);
                Assert.That(timingIssue, Does.Contain("ProgressSmoothing"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void TransitionProfile_KeepsEnterAndReadyCuesDistinct()
        {
            TransitionLoadingPresentationProfileSO profile =
                ScriptableObject.CreateInstance<TransitionLoadingPresentationProfileSO>();
            try
            {
                profile.SetForEditor(
                    new SpriteAssetId(11),
                    new SpriteAssetId(12),
                    new SpriteAssetId(13),
                    new AudioAssetId(14),
                    new AudioAssetId(15),
                    new MotionAssetId(16),
                    new MotionAssetId(17),
                    new MotionAssetId(18),
                    0.5f,
                    10f,
                    0.1f);

                Assert.That(profile.TryValidate(out string issue), Is.True, issue);
                Assert.That(profile.TransitionEnterSfxId.Value, Is.EqualTo(14));
                Assert.That(profile.TransitionReadySfxId.Value, Is.EqualTo(15));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void ErrorProfile_HasFixedPrimaryRetryRoleOptionalPanelAndRequiredCues()
        {
            LoadingErrorPresentationProfileSO profile =
                ScriptableObject.CreateInstance<LoadingErrorPresentationProfileSO>();
            try
            {
                profile.SetForEditor(
                    SpriteAssetId.None,
                    new AudioAssetId(22),
                    new AudioAssetId(23),
                    new MotionAssetId(24),
                    new MotionAssetId(25),
                    new MotionAssetId(26));

                Assert.That(profile.TryValidate(out string issue), Is.True, issue);
                Assert.That(profile.RetryButtonStyleRole, Is.EqualTo(ControlStyleRole.PrimaryButton));
                Assert.That(profile.ErrorPanelSpriteId.IsNone, Is.True);

                profile.SetForEditor(
                    new SpriteAssetId(21),
                    AudioAssetId.None,
                    new AudioAssetId(23),
                    new MotionAssetId(24),
                    new MotionAssetId(25),
                    new MotionAssetId(26));
                Assert.That(profile.TryValidate(out string missingCueIssue), Is.False);
                Assert.That(missingCueIssue, Does.Contain(nameof(profile.LoadErrorSfxId)));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
            }
        }
    }
}
