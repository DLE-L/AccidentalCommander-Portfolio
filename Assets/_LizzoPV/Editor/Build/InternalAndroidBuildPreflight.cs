#if UNITY_EDITOR
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using Lizzo.PV.Build;
using Unity.Android.Types;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

namespace Lizzo.PV.EditorTools
{
    public static partial class InternalAndroidBuildUtility
    {
        private static bool TryCreateEditorBuildRequest(out BuildRequest request, out string failure)
        {
            request = default;
            if (Application.isBatchMode)
            {
                failure = "PREFLIGHT_EDITOR_ONLY";
                return false;
            }

            if (TryValidateEditorBuildPreflight(out failure) == false)
                return false;

            string revision = GetRevision();
            if (string.Equals(revision, "UNVERIFIED", StringComparison.Ordinal))
            {
                failure = "PREFLIGHT_UNVERIFIED_REVISION";
                return false;
            }

            DateTime buildDateUtc = DateTime.UtcNow;
            request = new BuildRequest(InternalBuildInfo.CreateBuildId(buildDateUtc, revision), buildDateUtc, revision);
            failure = string.Empty;
            return true;
        }

        private static bool TryCreateCliBuildRequest(string[] commandLineArgs, out BuildRequest request, out string failure)
        {
            request = default;
            if (TryGetSingleCommandLineValue(commandLineArgs, BuildIdArgument, out string buildId, out failure) == false ||
                TryGetSingleCommandLineValue(commandLineArgs, BuildDateUtcArgument, out string buildDateText, out failure) == false)
                return false;

            Match buildIdMatch = BuildIdPattern.Match(buildId);
            if (buildIdMatch.Success == false)
            {
                failure = $"INVALID_BUILD_ID expected HHmmss_revision: '{buildId}'";
                return false;
            }

            if (DateTime.TryParseExact(buildDateText, "yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out DateTime buildDateUtc) == false)
            {
                failure = $"INVALID_BUILD_DATE_UTC expected yyyy-MM-ddTHH:mm:ssZ: '{buildDateText}'";
                return false;
            }

            string revision = GetRevision();
            if (string.Equals(revision, "UNVERIFIED", StringComparison.Ordinal))
            {
                failure = "UNVERIFIED_REVISION current git revision is required for CLI build.";
                return false;
            }

            if (string.Equals(buildIdMatch.Groups["revision"].Value, revision, StringComparison.Ordinal) == false ||
                string.Equals(InternalBuildInfo.CreateBuildId(buildDateUtc, revision), buildId, StringComparison.Ordinal) == false)
            {
                failure = $"BUILD_ID_REVISION_MISMATCH requested={buildId}, current_revision={revision}, requested_date_utc={buildDateText}";
                return false;
            }

            request = new BuildRequest(buildId, buildDateUtc, revision);
            failure = string.Empty;
            return true;
        }

        private static bool TryGetSingleCommandLineValue(string[] commandLineArgs, string argumentName, out string value, out string failure)
        {
            value = string.Empty;
            failure = string.Empty;
            int matchCount = 0;
            for (int index = 0; index < commandLineArgs.Length; index++)
            {
                if (string.Equals(commandLineArgs[index], argumentName, StringComparison.Ordinal) == false)
                    continue;

                if (index + 1 >= commandLineArgs.Length || string.IsNullOrWhiteSpace(commandLineArgs[index + 1]))
                {
                    failure = $"MISSING_ARGUMENT_VALUE {argumentName}";
                    return false;
                }

                value = commandLineArgs[index + 1].Trim();
                matchCount++;
            }

            if (matchCount != 1)
            {
                failure = matchCount == 0 ? $"MISSING_REQUIRED_ARGUMENT {argumentName}" : $"DUPLICATE_ARGUMENT {argumentName}";
                return false;
            }

            return true;
        }

        private static bool TryValidateEditorBuildPreflight(out string failure)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                failure = "PREFLIGHT_PLAY_MODE_ACTIVE_OR_PENDING";
                return false;
            }

            if (EditorApplication.isCompiling)
            {
                failure = "PREFLIGHT_EDITOR_COMPILING";
                return false;
            }

            if (EditorApplication.isUpdating)
            {
                failure = "PREFLIGHT_EDITOR_UPDATING";
                return false;
            }

            if (BuildPipeline.isBuildingPlayer)
            {
                failure = "PREFLIGHT_BUILD_ALREADY_RUNNING";
                return false;
            }

            if (TryValidateCliPreflight(out failure) == false)
                return false;

            if (GetEnabledScenes().Length == 0)
            {
                failure = "PREFLIGHT_ENABLED_SCENES_MISSING";
                return false;
            }

            for (int index = 0; index < EditorBuildSettings.scenes.Length; index++)
            {
                EditorBuildSettingsScene scene = EditorBuildSettings.scenes[index];
                if (scene.enabled && File.Exists(ToFullPath(scene.path)) == false)
                {
                    failure = $"PREFLIGHT_ENABLED_SCENE_MISSING path={scene.path}";
                    return false;
                }
            }

            if (TryValidateAuthorizedAndroidDevice(out failure) == false)
                return false;

            if (TryValidateNoUnsavedScenes(out failure) == false)
                return false;

            failure = string.Empty;
            return true;
        }

        private static bool TryValidateCliPreflight(out string failure)
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                failure = "PREFLIGHT_ACTIVE_TARGET_NOT_ANDROID";
                return false;
            }

            if (PlayerSettings.productName != ProductName ||
                PlayerSettings.companyName != CompanyName ||
                PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android) != AndroidBundleId ||
                PlayerSettings.bundleVersion != Version ||
                PlayerSettings.Android.bundleVersionCode != VersionCode ||
                PlayerSettings.defaultInterfaceOrientation != UIOrientation.Portrait ||
                PlayerSettings.allowedAutorotateToPortrait == false ||
                PlayerSettings.allowedAutorotateToPortraitUpsideDown ||
                PlayerSettings.allowedAutorotateToLandscapeLeft ||
                PlayerSettings.allowedAutorotateToLandscapeRight)
            {
                failure = "PREFLIGHT_ANDROID_IDENTITY_OR_ORIENTATION_MISMATCH";
                return false;
            }

            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null || string.IsNullOrWhiteSpace(settings.activeProfileId))
            {
                failure = "PREFLIGHT_ADDRESSABLES_SETTINGS_OR_PROFILE_MISSING";
                return false;
            }

            string androidOutput = Path.Combine(Addressables.BuildPath, BuildTarget.Android.ToString());
            if (Directory.Exists(androidOutput) == false)
            {
                failure = $"PREFLIGHT_ANDROID_ADDRESSABLES_OUTPUT_MISSING path={androidOutput}";
                return false;
            }

            if (IsSourceWorktreeClean(out string worktreeFailure) == false)
            {
                failure = worktreeFailure;
                return false;
            }

            failure = string.Empty;
            return true;
        }

        private static bool TryValidateNoUnsavedScenes(out string failure)
        {
            for (int index = 0; index < EditorSceneManager.sceneCount; index++)
            {
                Scene scene = EditorSceneManager.GetSceneAt(index);
                if (scene.isDirty)
                {
                    failure = $"PREFLIGHT_UNSAVED_SCENE_CHANGES path={scene.path}";
                    return false;
                }
            }

            failure = string.Empty;
            return true;
        }

        private static bool IsSourceWorktreeClean(out string failure)
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo("git", "status --porcelain --untracked-files=all")
                {
                    WorkingDirectory = ProjectRoot,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
                if (TryRunProcess(startInfo, ProcessTimeoutMilliseconds, out int exitCode, out string output, out string processError, out string processFailure) == false || exitCode != 0)
                {
                    string detail = string.IsNullOrWhiteSpace(processFailure) ? processError.Trim() : processFailure;
                    failure = string.IsNullOrWhiteSpace(detail)
                        ? "PREFLIGHT_GIT_STATUS_UNAVAILABLE"
                        : $"PREFLIGHT_GIT_STATUS_UNAVAILABLE: {detail}";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(output) == false)
                {
                    failure = "PREFLIGHT_WORKTREE_NOT_CLEAN";
                    return false;
                }
            }
            catch (Exception exception)
            {
                failure = $"PREFLIGHT_GIT_STATUS_UNAVAILABLE: {exception.Message}";
                return false;
            }

            failure = string.Empty;
            return true;
        }

        private readonly struct BuildRequest
        {
            public readonly string BuildId;
            public readonly DateTime BuildDateUtc;
            public readonly string Revision;

            public BuildRequest(string buildId, DateTime buildDateUtc, string revision)
            {
                BuildId = buildId;
                BuildDateUtc = buildDateUtc;
                Revision = revision;
            }
        }

    }
}
#endif
