using System;
using UnityEngine;

namespace Lizzo.PV.Build
{
    [Serializable]
    public sealed class InternalBuildInfo
    {
        public const string RuntimeResourcePath = "InternalTest/BuildInfo";
        public const string RuntimePayloadAssetPath = "Assets/Resources/InternalTest/BuildInfo.json";

        public string buildId;
        public string productName;
        public string packageId;
        public string versionName;
        public int versionCode;
        public string buildDateUtc;
        public string unityVersion;
        public string revision;

        public static InternalBuildInfo Create(
            string buildId,
            string productName,
            string packageId,
            string versionName,
            int versionCode,
            DateTime buildDateUtc,
            string unityVersion,
            string revision)
        {
            return new InternalBuildInfo
            {
                buildId = buildId ?? string.Empty,
                productName = productName ?? string.Empty,
                packageId = packageId ?? string.Empty,
                versionName = versionName ?? string.Empty,
                versionCode = versionCode,
                buildDateUtc = buildDateUtc.ToString("O"),
                unityVersion = unityVersion ?? string.Empty,
                revision = revision ?? string.Empty,
            };
        }

        public static string CreateBuildId(DateTime buildDateUtc, string revision)
        {
            return $"{buildDateUtc:HHmmss}_{revision}";
        }

        public static bool TryLoadRuntime(out InternalBuildInfo buildInfo)
        {
            TextAsset payload = Resources.Load<TextAsset>(RuntimeResourcePath);
            if (payload == null || string.IsNullOrWhiteSpace(payload.text))
            {
                buildInfo = null;
                return false;
            }

            buildInfo = JsonUtility.FromJson<InternalBuildInfo>(payload.text);
            return buildInfo != null && string.IsNullOrWhiteSpace(buildInfo.buildId) == false;
        }

        public string ToRuntimePayloadJson()
        {
            return JsonUtility.ToJson(this, true);
        }

        public string CreateBuildInfoJson(string buildResult, string apkFileName, string apkSha256)
        {
            return JsonUtility.ToJson(new ManifestData
            {
                buildId = buildId,
                productName = productName,
                packageId = packageId,
                versionName = versionName,
                versionCode = versionCode,
                buildDateUtc = buildDateUtc,
                unityVersion = unityVersion,
                revision = revision,
                buildResult = buildResult,
                apkFileName = apkFileName,
                apkSha256 = apkSha256,
            }, true);
        }

        public string[] ToTelemetryParameters()
        {
            return new[]
            {
                $"build_id={buildId}",
                $"version_name={versionName}",
                $"version_code={versionCode}",
                $"build_date_utc={buildDateUtc}",
                $"unity_version={unityVersion}",
                $"revision={revision}",
            };
        }

        [Serializable]
        private sealed class ManifestData
        {
            public string buildId;
            public string productName;
            public string packageId;
            public string versionName;
            public int versionCode;
            public string buildDateUtc;
            public string unityVersion;
            public string revision;
            public string buildResult;
            public string apkFileName;
            public string apkSha256;
        }
    }
}
