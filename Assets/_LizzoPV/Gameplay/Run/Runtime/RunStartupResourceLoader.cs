using System.Threading;
using Cysharp.Threading.Tasks;
using Lizzo.PV.Data;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Lizzo.PV.Gameplay.Run
{
    internal static class RunStartupResourceLoader
    {
        internal static async UniTask<bool> PrepareAsync(
            AppServices app,
            CancellationToken cancellationToken)
        {
            AssetPreloadResult preload = await app.Assets.PreloadLabelAsync<Object>(
                "PreLoad",
                cancellationToken);
            if (!preload.Succeeded)
            {
                Debug.LogError($"[GameScene] PreLoad failed. total={preload.TotalCount}, success={preload.SuccessCount}, failed={preload.FailedAddresses.Count}");
                return false;
            }

            if (!await ValidateRequiredResourcesAsync(app.Assets, cancellationToken))
                return false;

            DataLoadResult dataResult = await app.Data.InitializeAsync(cancellationToken);
            if (!dataResult.Succeeded)
            {
                Debug.LogError($"[GameScene] Data provider initialization failed. missing={dataResult.MissingRequiredIds.Count}");
                return false;
            }

            return true;
        }

        private static async UniTask<bool> ValidateRequiredResourcesAsync(
            IAssetService assets,
            CancellationToken cancellationToken)
        {
            bool valid = true;
            valid &= await assets.LoadAsync<TextAsset>("PlayerData.xml", cancellationToken) != null;
            valid &= await assets.LoadAsync<GameObject>("Map_01.prefab", cancellationToken) != null;
            valid &= await assets.LoadAsync<GameObject>("P0/Units/Commander/Commander.prefab", cancellationToken) != null;
            valid &= await assets.LoadAsync<GameObject>("BossArenaAuthoring.prefab", cancellationToken) != null;
            if (!valid)
                Debug.LogError("[GameScene] One or more required startup resources are missing or have the wrong type.");
            return valid;
        }
    }
}
