using System.Collections.Generic;
using UnityEngine;

namespace Lizzo.PV.Presentation
{
    [CreateAssetMenu(menuName = "Lizzo PV/Presentation/Lobby/Departure Screen Profile", fileName = "DepartureScreenPresentation")]
    public sealed class DepartureScreenPresentationSO : ScriptableObject
    {
        [SerializeField] private SpriteAssetId _lobbyBackgroundSpriteId;
        [SerializeField] private SpriteAssetId _commanderShadowSpriteId;
        [SerializeField] private MotionAssetId _commanderDisplayEnterMotionId;
        [SerializeField] private AudioAssetId _departureAcceptedSfxId;
        [SerializeField] private AudioAssetId _departureFailedSfxId;
        [SerializeField] private MotionAssetId _departureAcceptedMotionId;
        [SerializeField] private MotionAssetId _departureFailedMotionId;
        [SerializeField] private MotionAssetId _loadingIndicatorMotionId;
        [SerializeField] private List<CommanderVisualBinding> _commanderVisualBindings = new List<CommanderVisualBinding>();

        public SpriteAssetId LobbyBackgroundSpriteId => _lobbyBackgroundSpriteId;
        public SpriteAssetId CommanderShadowSpriteId => _commanderShadowSpriteId;
        public MotionAssetId CommanderDisplayEnterMotionId => _commanderDisplayEnterMotionId;
        public ControlStyleRole DepartureButtonStyleRole => ControlStyleRole.PrimaryButton;
        public AudioAssetId DepartureAcceptedSfxId => _departureAcceptedSfxId;
        public AudioAssetId DepartureFailedSfxId => _departureFailedSfxId;
        public MotionAssetId DepartureAcceptedMotionId => _departureAcceptedMotionId;
        public MotionAssetId DepartureFailedMotionId => _departureFailedMotionId;
        public MotionAssetId LoadingIndicatorMotionId => _loadingIndicatorMotionId;
        public IReadOnlyList<CommanderVisualBinding> CommanderVisualBindings => _commanderVisualBindings;

        public bool TryGetCommanderVisual(string gameDataId, out CommanderVisualBinding binding)
        {
            for (int i = 0; i < _commanderVisualBindings.Count; i++)
            {
                if (string.Equals(_commanderVisualBindings[i].GameDataId, gameDataId, System.StringComparison.Ordinal))
                {
                    binding = _commanderVisualBindings[i];
                    return true;
                }
            }

            binding = default;
            return false;
        }

        public bool TryValidate(out string issue)
        {
            if (!(LobbyProfileValidation.Require(_lobbyBackgroundSpriteId, nameof(LobbyBackgroundSpriteId), out issue)
                  && LobbyProfileValidation.Require(_commanderDisplayEnterMotionId, nameof(CommanderDisplayEnterMotionId), out issue)
                  && LobbyProfileValidation.Require(_departureAcceptedSfxId, nameof(DepartureAcceptedSfxId), out issue)
                  && LobbyProfileValidation.Require(_departureFailedSfxId, nameof(DepartureFailedSfxId), out issue)
                  && LobbyProfileValidation.Require(_departureAcceptedMotionId, nameof(DepartureAcceptedMotionId), out issue)
                  && LobbyProfileValidation.Require(_departureFailedMotionId, nameof(DepartureFailedMotionId), out issue)))
            {
                return false;
            }

            if (_commanderVisualBindings == null || _commanderVisualBindings.Count == 0)
            {
                issue = "At least one Commander GameDataId visual binding is required.";
                return false;
            }

            var seen = new HashSet<string>(System.StringComparer.Ordinal);
            for (int i = 0; i < _commanderVisualBindings.Count; i++)
            {
                CommanderVisualBinding binding = _commanderVisualBindings[i];
                if (string.IsNullOrWhiteSpace(binding.GameDataId))
                {
                    issue = $"CommanderVisualBindings[{i}] requires GameDataId.";
                    return false;
                }

                if (!seen.Add(binding.GameDataId))
                {
                    issue = $"Duplicate Commander GameDataId visual binding: {binding.GameDataId}.";
                    return false;
                }

                if (!LobbyProfileValidation.Require(binding.PortraitSpriteId, $"CommanderVisualBindings[{binding.GameDataId}].PortraitSpriteId", out issue))
                    return false;
            }

            issue = string.Empty;
            return true;
        }

#if UNITY_EDITOR
        public void SetForEditor(SpriteAssetId lobbyBackgroundSpriteId, SpriteAssetId commanderShadowSpriteId, MotionAssetId commanderDisplayEnterMotionId, AudioAssetId departureAcceptedSfxId, AudioAssetId departureFailedSfxId, MotionAssetId departureAcceptedMotionId, MotionAssetId departureFailedMotionId, MotionAssetId loadingIndicatorMotionId, IEnumerable<CommanderVisualBinding> commanderVisualBindings = null)
        {
            _lobbyBackgroundSpriteId = lobbyBackgroundSpriteId;
            _commanderShadowSpriteId = commanderShadowSpriteId;
            _commanderDisplayEnterMotionId = commanderDisplayEnterMotionId;
            _departureAcceptedSfxId = departureAcceptedSfxId;
            _departureFailedSfxId = departureFailedSfxId;
            _departureAcceptedMotionId = departureAcceptedMotionId;
            _departureFailedMotionId = departureFailedMotionId;
            _loadingIndicatorMotionId = loadingIndicatorMotionId;
            _commanderVisualBindings = commanderVisualBindings == null
                ? new List<CommanderVisualBinding>()
                : new List<CommanderVisualBinding>(commanderVisualBindings);
        }
#endif
    }
}
