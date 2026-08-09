namespace Lizzo.PV.Flow
{
    public static class ForegroundScreenAwakePolicy
    {
        public static bool ShouldKeepAwake(string scenePath, bool hasFocus, bool isPaused, bool isQuitting)
        {
            if (!hasFocus || isPaused || isQuitting)
                return false;

            return scenePath == GameFlowRoutes.LobbyScenePath
                || scenePath == GameFlowRoutes.GameplayScenePath;
        }
    }
}
