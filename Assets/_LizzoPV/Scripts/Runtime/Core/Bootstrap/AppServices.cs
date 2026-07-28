using Lizzo.PV.Data;
using System;

public sealed class AppServices
{
    public IAssetService Assets { get; }
    public IDataProvider Data { get; }
    public Lizzo.PV.Flow.RunLaunchState LaunchState { get; }
    public Lizzo.PV.Flow.CompanionUnlockProgress CompanionUnlockProgress { get; }

    public AppServices(IAssetService assets, IDataProvider data)
    {
        Assets = assets ?? throw new ArgumentNullException(nameof(assets));
        Data = data ?? throw new ArgumentNullException(nameof(data));
        LaunchState = new Lizzo.PV.Flow.RunLaunchState();
        CompanionUnlockProgress = new Lizzo.PV.Flow.CompanionUnlockProgress(new Lizzo.PV.Flow.PlayerPrefsCompanionUnlockProgressStore());
    }

    public void ReleaseAll()
    {
        Assets.ReleaseAll();
    }
}
