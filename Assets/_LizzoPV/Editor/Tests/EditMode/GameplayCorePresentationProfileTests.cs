using Lizzo.PV.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace Lizzo.PV.EditorTests
{
    public sealed class GameplayCorePresentationProfileTests
    {
        [Test]
        public void AudioProfile_RequiresBothBgmAndAllowsOptionalAmbience()
        {
            GameplayAudioPresentationProfileSO profile = Create<GameplayAudioPresentationProfileSO>();
            try
            {
                profile.SetForEditor(
                    new AudioAssetId(1),
                    new AudioAssetId(2),
                    AudioAssetId.None,
                    0.5f,
                    0.25f);

                Assert.That(profile.TryValidate(out string issue), Is.True, issue);
                Assert.That(profile.StageAmbienceLoopSfxId.IsNone, Is.True);

                profile.SetForEditor(AudioAssetId.None, new AudioAssetId(2), AudioAssetId.None, 0f, 0f);
                Assert.That(profile.TryValidate(out string missingIssue), Is.False);
                Assert.That(missingIssue, Does.Contain(nameof(profile.GameplayBgmId)));
            }
            finally
            {
                Destroy(profile);
            }
        }

        [Test]
        public void AudioProfile_RejectsNegativeFadeTiming()
        {
            GameplayAudioPresentationProfileSO profile = Create<GameplayAudioPresentationProfileSO>();
            try
            {
                profile.SetForEditor(new AudioAssetId(1), new AudioAssetId(2), AudioAssetId.None, -0.1f, 0f);
                Assert.That(profile.TryValidate(out string issue), Is.False);
                Assert.That(issue, Does.Contain("CrossFade"));
            }
            finally
            {
                Destroy(profile);
            }
        }

        [Test]
        public void HudProfile_RequiresVisualAndTransitionSlotsWithIconButtonRole()
        {
            GameplayHudPresentationProfileSO profile = Create<GameplayHudPresentationProfileSO>();
            try
            {
                SetHud(profile, 1);
                Assert.That(profile.TryValidate(out string issue), Is.True, issue);
                Assert.That(profile.IconButtonStyleRole, Is.EqualTo(ControlStyleRole.IconButton));

                SetHud(profile, 0);
                Assert.That(profile.TryValidate(out string missingIssue), Is.False);
                Assert.That(missingIssue, Does.Contain(nameof(profile.KillIconSpriteId)));
            }
            finally
            {
                Destroy(profile);
            }
        }

        [Test]
        public void InputProfile_RequiresThreeSpritesAndTwoMotions()
        {
            GameplayInputPresentationProfileSO profile = Create<GameplayInputPresentationProfileSO>();
            try
            {
                profile.SetForEditor(
                    new SpriteAssetId(1),
                    new SpriteAssetId(2),
                    new SpriteAssetId(3),
                    new MotionAssetId(4),
                    new MotionAssetId(5));
                Assert.That(profile.TryValidate(out string issue), Is.True, issue);

                profile.SetForEditor(
                    new SpriteAssetId(1),
                    new SpriteAssetId(2),
                    new SpriteAssetId(3),
                    new MotionAssetId(4),
                    MotionAssetId.None);
                Assert.That(profile.TryValidate(out string missingIssue), Is.False);
                Assert.That(missingIssue, Does.Contain(nameof(profile.ResetMotionId)));
            }
            finally
            {
                Destroy(profile);
            }
        }

        private static void SetHud(GameplayHudPresentationProfileSO profile, int firstSpriteId)
        {
            profile.SetForEditor(
                new SpriteAssetId(firstSpriteId),
                new SpriteAssetId(2),
                new SpriteAssetId(3),
                new SpriteAssetId(4),
                new SpriteAssetId(5),
                new SpriteAssetId(6),
                new SpriteAssetId(7),
                new SpriteAssetId(8),
                new SpriteAssetId(9),
                new AudioAssetId(10),
                new MotionAssetId(11),
                new MotionAssetId(12),
                new MotionAssetId(13));
        }

        private static T Create<T>() where T : ScriptableObject
        {
            return ScriptableObject.CreateInstance<T>();
        }

        private static void Destroy(UnityEngine.Object value)
        {
            if (value != null)
            {
                UnityEngine.Object.DestroyImmediate(value);
            }
        }
    }
}
