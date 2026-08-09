using System.Collections.Generic;

public sealed class AssetPreloadResult
{
    public int TotalCount { get; internal set; }
    public int SuccessCount { get; internal set; }
    public List<string> FailedAddresses { get; } = new List<string>();
    public bool Succeeded => TotalCount > 0 && FailedAddresses.Count == 0 && SuccessCount == TotalCount;
}