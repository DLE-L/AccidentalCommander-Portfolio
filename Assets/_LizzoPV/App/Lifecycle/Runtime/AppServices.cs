using Lizzo.PV.Data;
using Lizzo.PV.Presentation;
using System;

public sealed class AppServices
{
    public IAssetService Assets { get; }
    public IDataProvider Data { get; }
    public AssetCatalogBundleRuntime AssetCatalogs { get; }
    public Lizzo.PV.Flow.RunLaunchState LaunchState { get; }
    public Lizzo.PV.Flow.CompanionUnlockProgress CompanionUnlockProgress { get; }
    public Lizzo.PV.Flow.AccountResourceWallet AccountWallet { get; }

    public AppServices(
        IAssetService assets,
        IDataProvider data,
        Lizzo.PV.Flow.AccountResourceWallet accountWallet = null)
    {
        Assets = assets ?? throw new ArgumentNullException(nameof(assets));
        Data = data ?? throw new ArgumentNullException(nameof(data));
        AssetCatalogs = new AssetCatalogBundleRuntime();
        LaunchState = new Lizzo.PV.Flow.RunLaunchState();
        AccountWallet = accountWallet ?? new Lizzo.PV.Flow.AccountResourceWallet(
            new Lizzo.PV.Flow.PlayerPrefsAccountResourceWalletStore());
        CompanionUnlockProgress = new Lizzo.PV.Flow.CompanionUnlockProgress(
            new Lizzo.PV.Flow.PlayerPrefsCompanionUnlockProgressStore(),
            Lizzo.PV.Flow.CompanionUnlockProgress.IsTestRuntime,
            () => Lizzo.PV.Flow.FirstRunProgress.IsTutorialCompleted);
    }

    public void ReleaseAll()
    {
        Assets.ReleaseAll();
    }
}
