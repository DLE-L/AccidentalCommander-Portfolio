namespace Lizzo.PV.Presentation
{
    internal static class GameplayOverlayProfileValidation
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
