namespace Lizzo.PV.Presentation
{
    internal static class GameplayCoreProfileValidation
    {
        public static bool Require(SpriteAssetId id, string fieldName, out string issue) => Require(id.Value, "Sprite", fieldName, out issue);
        public static bool Require(AudioAssetId id, string fieldName, out string issue) => Require(id.Value, "Audio", fieldName, out issue);
        public static bool Require(MotionAssetId id, string fieldName, out string issue) => Require(id.Value, "Motion", fieldName, out issue);

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
