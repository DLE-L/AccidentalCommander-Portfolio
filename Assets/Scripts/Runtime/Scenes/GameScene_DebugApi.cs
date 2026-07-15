using UnityEngine;

public partial class GameScene
{
    public void DebugAddExperience(int amount)
    {
        if (IsRunLoaded)
            _runState.AddExperience(amount);
    }

    public void DebugSetRunLevel(int level)
    {
        if (!IsRunLoaded)
            return;

        int safeLevel = Mathf.Max(1, level);
        _runState.SetLevelForDebug(safeLevel, Mathf.Max(1, _services.App.Data.GetLevelExp(safeLevel)));
        RefreshExpUi();
    }

    public void DebugForceLevelUp()
    {
        if (IsRunLoaded)
            ShowLevelUpPopupAndAdvance();
    }

    public void DebugSetRunElapsedSeconds(float seconds)
    {
        if (IsRunLoaded)
            _runState.SetElapsedSecondsForDebug(seconds);
    }

    public void DebugSetSpawnStopped(bool stopped)
    {
        if (_stageSpawner != null)
            _stageSpawner.Stopped = stopped;
    }
}
