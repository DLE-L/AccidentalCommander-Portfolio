using UnityEngine;

namespace Lizzo.PV.Presentation
{
    [CreateAssetMenu(menuName = "Lizzo PV/Presentation/Theme/Control Style Profile", fileName = "ControlStyleProfile")]
    public sealed class ControlStyleProfileSO : ScriptableObject
    {
        [SerializeField] private SpriteAssetId _normalSpriteId;
        [SerializeField] private SpriteAssetId _pressedSpriteId;
        [SerializeField] private SpriteAssetId _selectedSpriteId;
        [SerializeField] private SpriteAssetId _disabledSpriteId;
        [SerializeField] private SpriteAssetId _lockedSpriteId;
        [SerializeField] private SpriteAssetId _processingSpriteId;

        public SpriteAssetId NormalSpriteId => _normalSpriteId;
        public SpriteAssetId PressedSpriteId => _pressedSpriteId;
        public SpriteAssetId SelectedSpriteId => _selectedSpriteId;
        public SpriteAssetId DisabledSpriteId => _disabledSpriteId;
        public SpriteAssetId LockedSpriteId => _lockedSpriteId;
        public SpriteAssetId ProcessingSpriteId => _processingSpriteId;

        public bool TryGet(ControlVisualState state, out SpriteAssetId spriteId)
        {
            switch (state)
            {
                case ControlVisualState.Normal: spriteId = _normalSpriteId; break;
                case ControlVisualState.Pressed: spriteId = _pressedSpriteId; break;
                case ControlVisualState.Selected: spriteId = _selectedSpriteId; break;
                case ControlVisualState.Disabled: spriteId = _disabledSpriteId; break;
                case ControlVisualState.Locked: spriteId = _lockedSpriteId; break;
                case ControlVisualState.Processing: spriteId = _processingSpriteId; break;
                default:
                    spriteId = SpriteAssetId.None;
                    return false;
            }

            return !spriteId.IsNone;
        }

        public bool TryValidate(ControlStyleRole role, out string issue)
        {
            if (!Require(_normalSpriteId, nameof(NormalSpriteId), out issue)
                || !Require(_pressedSpriteId, nameof(PressedSpriteId), out issue)
                || !Require(_disabledSpriteId, nameof(DisabledSpriteId), out issue))
            {
                return false;
            }

            switch (role)
            {
                case ControlStyleRole.PrimaryButton:
                case ControlStyleRole.DestructiveButton:
                    return Require(_processingSpriteId, nameof(ProcessingSpriteId), out issue)
                           && RequireNone(_selectedSpriteId, nameof(SelectedSpriteId), role, out issue)
                           && RequireNone(_lockedSpriteId, nameof(LockedSpriteId), role, out issue);
                case ControlStyleRole.IconButton:
                    return Require(_selectedSpriteId, nameof(SelectedSpriteId), out issue)
                           && Require(_lockedSpriteId, nameof(LockedSpriteId), out issue)
                           && Require(_processingSpriteId, nameof(ProcessingSpriteId), out issue);
                case ControlStyleRole.ChoiceCard:
                    return Require(_selectedSpriteId, nameof(SelectedSpriteId), out issue)
                           && RequireNone(_lockedSpriteId, nameof(LockedSpriteId), role, out issue)
                           && RequireNone(_processingSpriteId, nameof(ProcessingSpriteId), role, out issue);
                default:
                    issue = $"Unsupported ControlStyleRole: {role}.";
                    return false;
            }
        }

#if UNITY_EDITOR
        public void SetForEditor(
            SpriteAssetId normalSpriteId,
            SpriteAssetId pressedSpriteId,
            SpriteAssetId selectedSpriteId,
            SpriteAssetId disabledSpriteId,
            SpriteAssetId lockedSpriteId,
            SpriteAssetId processingSpriteId)
        {
            _normalSpriteId = normalSpriteId;
            _pressedSpriteId = pressedSpriteId;
            _selectedSpriteId = selectedSpriteId;
            _disabledSpriteId = disabledSpriteId;
            _lockedSpriteId = lockedSpriteId;
            _processingSpriteId = processingSpriteId;
        }
#endif

        private static bool Require(SpriteAssetId id, string fieldName, out string issue)
        {
            if (id.IsNone)
            {
                issue = $"Required Control Style Sprite Asset ID {fieldName} must be positive.";
                return false;
            }

            issue = string.Empty;
            return true;
        }

        private static bool RequireNone(SpriteAssetId id, string fieldName, ControlStyleRole role, out string issue)
        {
            if (!id.IsNone)
            {
                issue = $"ControlStyleRole {role} does not allow {fieldName}.";
                return false;
            }

            issue = string.Empty;
            return true;
        }
    }
}
