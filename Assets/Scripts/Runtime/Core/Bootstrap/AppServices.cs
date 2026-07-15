using Lizzo.PV.Data;
using System;

public sealed class AppServices
{
    public IAssetService Assets { get; }
    public IDataProvider Data { get; }public AppServices(IAssetService assets, IDataProvider data)
    {
        Assets = assets ?? throw new ArgumentNullException(nameof(assets));
        Data = data ?? throw new ArgumentNullException(nameof(data));}

    public void ReleaseAll()
    {
        Assets.ReleaseAll();
    }
}