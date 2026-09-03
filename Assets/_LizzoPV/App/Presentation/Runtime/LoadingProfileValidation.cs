namespace Lizzo.PV.Presentation
{
    internal static class LoadingProfileValidation
    {
        public static bool Require(SpriteAssetId id, string fieldName, out string issue) =>
            Require(id.Value, "Sprite", fieldName, out issue);

        public static bool Require(AudioAssetId id, string fieldName, out string issue) =>
            Require(id.Value, "Audio", fieldName, out issue);

        public static bool Require(MotionAssetId id, string fieldName, out string issue) =>
            Require(id.Value, "Motion", fieldName, out issue);

        public static bool RequireTiming(
            float minimumVisibleSeconds,
            float progressSmoothing,
            float exitDelaySeconds,
            out string issue)
        {
            if (minimumVisibleSeconds < 0f)
            {
                issue = "MinimumVisibleSeconds cannot be negative.";
                return false;
            }

            if (progressSmoothing <= 0f)
            {
                issue = "ProgressSmoothing must be greater than zero.";
                return false;
            }

            if (exitDelaySeconds < 0f)
            {
                issue = "ExitDelaySeconds cannot be negative.";
                return false;
            }

            issue = string.Empty;
            return true;
        }

        static bool Require(int value, string assetKind, string fieldName, out string issue)
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
