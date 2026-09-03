using System;
using System.Collections.Generic;
using Lizzo.PV.Lobby;
using UnityEngine;

namespace Lizzo.PV.Presentation
{
    public enum LobbyTabInitialState
    {
        Locked,
        Selected,
    }

    [Serializable]
    public struct LobbyTabPresentationBinding
    {
        [SerializeField] private LobbySection _section;
        [SerializeField] private SpriteAssetId _iconSpriteId;
        [SerializeField] private LobbyTabInitialState _initialState;
        [SerializeField] private MotionAssetId _selectedMotionId;

        public LobbyTabPresentationBinding(LobbySection section, SpriteAssetId iconSpriteId, LobbyTabInitialState initialState, MotionAssetId selectedMotionId)
        {
            _section = section;
            _iconSpriteId = iconSpriteId;
            _initialState = initialState;
            _selectedMotionId = selectedMotionId;
        }

        public LobbySection Section => _section;
        public SpriteAssetId IconSpriteId => _iconSpriteId;
        public ControlStyleRole IconButtonStyleRole => ControlStyleRole.IconButton;
        public LobbyTabInitialState InitialState => _initialState;
        public MotionAssetId SelectedMotionId => _selectedMotionId;
    }

    [Serializable]
    public struct CommanderVisualBinding
    {
        [SerializeField] private string _gameDataId;
        [SerializeField] private SpriteAssetId _portraitSpriteId;

        public CommanderVisualBinding(string gameDataId, SpriteAssetId portraitSpriteId)
        {
            _gameDataId = gameDataId;
            _portraitSpriteId = portraitSpriteId;
        }

        public string GameDataId => _gameDataId;
        public SpriteAssetId PortraitSpriteId => _portraitSpriteId;
    }

    internal static class LobbyProfileValidation
    {
        public static bool Require(SpriteAssetId id, string fieldName, out string issue) => Require(id.Value, "Sprite", fieldName, out issue);
        public static bool Require(AudioAssetId id, string fieldName, out string issue) => Require(id.Value, "Audio", fieldName, out issue);
        public static bool Require(MotionAssetId id, string fieldName, out string issue) => Require(id.Value, "Motion", fieldName, out issue);

        public static bool Require(LocalizationKey key, string fieldName, out string issue)
        {
            if (key.IsNone)
            {
                issue = $"Required LocalizationKey {fieldName} cannot be empty.";
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
