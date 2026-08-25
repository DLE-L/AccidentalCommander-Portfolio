#if UNITY_EDITOR
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;

namespace Lizzo.PV.EditorTools
{
    public static partial class InternalAndroidBuildUtility
    {
        private static bool TryValidateAuthorizedAndroidDevice(out string failure)
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo(ResolveAdbPath(), "devices -l")
                {
                    WorkingDirectory = ProjectRoot,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
                if (TryRunProcess(startInfo, ProcessTimeoutMilliseconds, out int exitCode, out string output, out string error, out string processFailure) == false)
                {
                    failure = processFailure.IndexOf("PROCESS_TIMEOUT", StringComparison.Ordinal) >= 0
                        ? $"PREFLIGHT_ANDROID_ADB_TIMEOUT timeout_ms={ProcessTimeoutMilliseconds}"
                        : $"PREFLIGHT_ANDROID_ADB_UNAVAILABLE: {processFailure}";
                    return false;
                }

                if (exitCode != 0)
                {
                    failure = $"PREFLIGHT_ANDROID_ADB_FAILED: {error.Trim()}";
                    return false;
                }

                string authorizedModel = string.Empty;
                int authorizedDeviceCount = 0;
                string[] lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                for (int index = 1; index < lines.Length; index++)
                {
                    string[] fields = lines[index].Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (fields.Length < 2 || string.Equals(fields[1], "device", StringComparison.Ordinal) == false)
                        continue;

                    authorizedDeviceCount++;
                    for (int fieldIndex = 2; fieldIndex < fields.Length; fieldIndex++)
                    {
                        if (fields[fieldIndex].StartsWith("model:", StringComparison.Ordinal))
                        {
                            authorizedModel = fields[fieldIndex].Substring("model:".Length);
                            break;
                        }
                    }
                }

                if (authorizedDeviceCount != 1 || authorizedModel.IndexOf("V500", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    failure = $"PREFLIGHT_AUTHORIZED_LG_V50_REQUIRED devices={authorizedDeviceCount}, model={authorizedModel}";
                    return false;
                }
            }
            catch (Exception exception)
            {
                failure = $"PREFLIGHT_ANDROID_ADB_UNAVAILABLE: {exception.Message}";
                return false;
            }

            failure = string.Empty;
            return true;
        }

        private static string ResolveAdbPath()
        {
            string unitySdkAdbPath = Path.Combine(
                EditorApplication.applicationContentsPath,
                "PlaybackEngines",
                "AndroidPlayer",
                "SDK",
                "platform-tools",
                "adb.exe");
            if (File.Exists(unitySdkAdbPath))
                return unitySdkAdbPath;

            string sdkRoot = Environment.GetEnvironmentVariable("ANDROID_HOME");
            if (string.IsNullOrWhiteSpace(sdkRoot))
                sdkRoot = Environment.GetEnvironmentVariable("ANDROID_SDK_ROOT");

            if (string.IsNullOrWhiteSpace(sdkRoot) == false)
            {
                string sdkAdbPath = Path.Combine(sdkRoot, "platform-tools", "adb.exe");
                if (File.Exists(sdkAdbPath))
                    return sdkAdbPath;
            }

            return "adb";
        }

        private static bool TryRunProcess(
            ProcessStartInfo startInfo,
            int timeoutMilliseconds,
            out int exitCode,
            out string output,
            out string error,
            out string failure)
        {
            exitCode = -1;
            output = string.Empty;
            error = string.Empty;
            failure = string.Empty;

            try
            {
                using Process process = Process.Start(startInfo);
                if (process == null)
                {
                    failure = "PROCESS_UNAVAILABLE";
                    return false;
                }

                Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
                Task<string> errorTask = process.StandardError.ReadToEndAsync();
                if (process.WaitForExit(timeoutMilliseconds) == false)
                {
                    TryTerminateProcess(process);
                    failure = $"PROCESS_TIMEOUT command={startInfo.FileName} timeout_ms={timeoutMilliseconds}";
                    return false;
                }

                outputTask.Wait(ProcessTerminationTimeoutMilliseconds);
                errorTask.Wait(ProcessTerminationTimeoutMilliseconds);
                output = GetCompletedProcessOutput(outputTask);
                error = GetCompletedProcessOutput(errorTask);
                exitCode = process.ExitCode;
                return true;
            }
            catch (Exception exception)
            {
                failure = exception.Message;
                return false;
            }
        }

        private static void TryTerminateProcess(Process process)
        {
            try
            {
                if (process.HasExited == false)
                    process.Kill();
            }
            catch (Exception)
            {
                // The timeout result remains the actionable failure; do not touch any other process.
            }

            try
            {
                process.WaitForExit(ProcessTerminationTimeoutMilliseconds);
            }
            catch (Exception)
            {
                // The bounded cleanup wait is best effort.
            }
        }

        private static string GetCompletedProcessOutput(Task<string> outputTask)
        {
            return outputTask.Status == TaskStatus.RanToCompletion ? outputTask.Result : string.Empty;
        }

    }
}
#endif
