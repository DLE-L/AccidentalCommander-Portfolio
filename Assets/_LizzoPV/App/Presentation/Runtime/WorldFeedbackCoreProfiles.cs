using System;
using UnityEngine;

namespace Lizzo.PV.Presentation
{
    [CreateAssetMenu(menuName = "Lizzo PV/Presentation/World/Commander Feedback Profile", fileName = "CommanderWorldFeedbackProfile")]
    public sealed class CommanderWorldFeedbackProfileSO : ScriptableObject
    {
        [SerializeField] private VfxAssetId _healVfxId;
        [SerializeField] private AudioAssetId _healSfxId;
        [SerializeField] private SpriteAssetId _lowHealthOverlaySpriteId;
        [SerializeField] private MotionAssetId _lowHealthEnterMotionId;
        [SerializeField] private MotionAssetId _lowHealthLoopMotionId;
        [SerializeField] private MotionAssetId _lowHealthExitMotionId;
        [SerializeField] private AudioAssetId _lowHealthEnterSfxId;
        [SerializeField] private AudioAssetId _lowHealthLoopSfxId;
        [SerializeField] private AudioAssetId _lowHealthExitSfxId;

        public VfxAssetId HealVfxId => _healVfxId;
        public AudioAssetId HealSfxId => _healSfxId;
        public SpriteAssetId LowHealthOverlaySpriteId => _lowHealthOverlaySpriteId;
        public MotionAssetId LowHealthEnterMotionId => _lowHealthEnterMotionId;
        public MotionAssetId LowHealthLoopMotionId => _lowHealthLoopMotionId;
        public MotionAssetId LowHealthExitMotionId => _lowHealthExitMotionId;
        public AudioAssetId LowHealthEnterSfxId => _lowHealthEnterSfxId;
        public AudioAssetId LowHealthLoopSfxId => _lowHealthLoopSfxId;
        public AudioAssetId LowHealthExitSfxId => _lowHealthExitSfxId;

        public bool TryValidate(out string issue)
        {
            return WorldFeedbackCoreValidation.Require(_healVfxId, nameof(HealVfxId), out issue)
                   && WorldFeedbackCoreValidation.Require(_healSfxId, nameof(HealSfxId), out issue)
                   && WorldFeedbackCoreValidation.Require(_lowHealthEnterMotionId, nameof(LowHealthEnterMotionId), out issue)
                   && WorldFeedbackCoreValidation.Require(_lowHealthLoopMotionId, nameof(LowHealthLoopMotionId), out issue)
                   && WorldFeedbackCoreValidation.Require(_lowHealthExitMotionId, nameof(LowHealthExitMotionId), out issue)
                   && WorldFeedbackCoreValidation.Require(_lowHealthEnterSfxId, nameof(LowHealthEnterSfxId), out issue)
                   && WorldFeedbackCoreValidation.Require(_lowHealthLoopSfxId, nameof(LowHealthLoopSfxId), out issue)
                   && WorldFeedbackCoreValidation.Require(_lowHealthExitSfxId, nameof(LowHealthExitSfxId), out issue);
        }

#if UNITY_EDITOR
        public void SetForEditor(
            VfxAssetId healVfxId,
            AudioAssetId healSfxId,
            SpriteAssetId lowHealthOverlaySpriteId,
            MotionAssetId lowHealthEnterMotionId,
            MotionAssetId lowHealthLoopMotionId,
            MotionAssetId lowHealthExitMotionId,
            AudioAssetId lowHealthEnterSfxId,
            AudioAssetId lowHealthLoopSfxId,
            AudioAssetId lowHealthExitSfxId)
        {
            _healVfxId = healVfxId;
            _healSfxId = healSfxId;
            _lowHealthOverlaySpriteId = lowHealthOverlaySpriteId;
            _lowHealthEnterMotionId = lowHealthEnterMotionId;
            _lowHealthLoopMotionId = lowHealthLoopMotionId;
            _lowHealthExitMotionId = lowHealthExitMotionId;
            _lowHealthEnterSfxId = lowHealthEnterSfxId;
            _lowHealthLoopSfxId = lowHealthLoopSfxId;
            _lowHealthExitSfxId = lowHealthExitSfxId;
        }
#endif
    }

    [Serializable]
    public struct CompanionLifecycleFeedbackBinding
    {
        [SerializeField] private CompanionId _companionId;
        [SerializeField] private CompanionLifecycleFeedbackProfileSO _profile;

        public CompanionLifecycleFeedbackBinding(CompanionId companionId, CompanionLifecycleFeedbackProfileSO profile)
        {
            _companionId = companionId;
            _profile = profile;
        }

        public CompanionId CompanionId => _companionId;
        public CompanionLifecycleFeedbackProfileSO Profile => _profile;
    }

    [Serializable]
    public struct CombatImpactFeedbackBinding
    {
        [SerializeField] private CombatImpactKind _impactKind;
        [SerializeField] private CombatImpactFeedbackProfileSO _profile;

        public CombatImpactFeedbackBinding(CombatImpactKind impactKind, CombatImpactFeedbackProfileSO profile)
        {
            _impactKind = impactKind;
            _profile = profile;
        }

        public CombatImpactKind ImpactKind => _impactKind;
        public CombatImpactFeedbackProfileSO Profile => _profile;
    }

    internal static class WorldFeedbackCoreValidation
    {
        public static bool Require(SpriteAssetId id, string fieldName, out string issue) =>
            Require(id.Value, "Sprite", fieldName, out issue);

        public static bool Require(AudioAssetId id, string fieldName, out string issue) =>
            Require(id.Value, "Audio", fieldName, out issue);

        public static bool Require(VfxAssetId id, string fieldName, out string issue) =>
            Require(id.Value, "VFX", fieldName, out issue);

        public static bool Require(MotionAssetId id, string fieldName, out string issue) =>
            Require(id.Value, "Motion", fieldName, out issue);

        public static bool Require(ColorRole role, string fieldName, out string issue)
        {
            if (role.IsNone)
            {
                issue = $"Required ColorRole {fieldName} cannot be empty.";
                return false;
            }

            issue = string.Empty;
            return true;
        }

        private static bool Require(int value, string assetKind, string fieldName, out string issue)
        {
            if (value <= 0)
            {
                issue = $"Required {assetKind} Asset ID {fieldName} must be positive.";
                return false;
            }

            issue = string.Empty;
            return true;
        }
    }
}
