#if UNITY_EDITOR
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEngine;

namespace Lizzo.PV.EditorTools
{
    internal static class EditorBuildRunCoordinator
    {
        internal const string LockFileName = ".editor_build_run.lock";
        internal const string StatusFileName = ".editor_build_run_status.json";

        internal static bool TryAcquire(string outputRoot, string operationId, out Ownership ownership, out string failure)
        {
            ownership = null;
            failure = string.Empty;
            Directory.CreateDirectory(outputRoot);
            string lockPath = Path.Combine(outputRoot, LockFileName);

            FileStream stream = null;
            try
            {
                stream = new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 4096, FileOptions.WriteThrough);
                ownership = new Ownership(stream, operationId);
                ownership.WriteOwnerMetadata();
                return true;
            }
            catch (IOException)
            {
                ownership?.Dispose();
                stream?.Dispose();
                failure = ReadStatusSummary(outputRoot);
                return false;
            }
            catch (UnauthorizedAccessException exception)
            {
                ownership?.Dispose();
                stream?.Dispose();
                failure = $"SINGLE_FLIGHT_ACQUIRE_FAILED: {exception.Message}";
                return false;
            }
        }

        internal static bool TryReadStatus(string outputRoot, out EditorBuildRunStatus status)
        {
            status = null;
            string statusPath = Path.Combine(outputRoot, StatusFileName);
            if (File.Exists(statusPath) == false)
                return false;

            try
            {
                status = JsonUtility.FromJson<EditorBuildRunStatus>(File.ReadAllText(statusPath, Encoding.UTF8));
                return status != null;
            }
            catch (Exception)
            {
                return false;
            }
        }

        internal static void WriteStatus(string outputRoot, EditorBuildRunStatus status)
        {
            Directory.CreateDirectory(outputRoot);
            string statusPath = Path.Combine(outputRoot, StatusFileName);
            string temporaryPath = $"{statusPath}.{status.operationId}.tmp";
            try
            {
                File.WriteAllText(temporaryPath, JsonUtility.ToJson(status, true), new UTF8Encoding(false));
                if (File.Exists(statusPath))
                    File.Replace(temporaryPath, statusPath, null);
                else
                    File.Move(temporaryPath, statusPath);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }
        }

        private static string ReadStatusSummary(string outputRoot)
        {
            if (TryReadStatus(outputRoot, out EditorBuildRunStatus status))
            {
                string buildId = string.IsNullOrWhiteSpace(status.buildId) ? "pending" : status.buildId;
                return $"ALREADY_IN_PROGRESS state={status.state}, build_id={buildId}, operation_id={status.operationId}";
            }

            return "ALREADY_IN_PROGRESS status=unreadable";
        }

        internal sealed class Ownership : IDisposable
        {
            private FileStream _stream;

            internal Ownership(FileStream stream, string operationId)
            {
                _stream = stream;
                OperationId = operationId;
            }

            internal string OperationId { get; }

            internal void WriteOwnerMetadata()
            {
                EditorBuildRunOwner owner = new EditorBuildRunOwner
                {
                    operationId = OperationId,
                    processId = Process.GetCurrentProcess().Id,
                    acquiredAtUtc = DateTime.UtcNow.ToString("O"),
                };
                byte[] bytes = new UTF8Encoding(false).GetBytes(JsonUtility.ToJson(owner, true));
                _stream.SetLength(0);
                _stream.Write(bytes, 0, bytes.Length);
                _stream.Flush(true);
            }

            public void Dispose()
            {
                FileStream stream = _stream;
                _stream = null;
                if (stream != null)
                    stream.Dispose();
            }
        }

        [Serializable]
        internal sealed class EditorBuildRunStatus
        {
            public string state;
            public string operationId;
            public int ownerProcessId;
            public string startedAtUtc;
            public string buildId;
            public string result;
            public string packagePath;
            public string error;
        }

        [Serializable]
        private sealed class EditorBuildRunOwner
        {
            public string operationId;
            public int processId;
            public string acquiredAtUtc;
        }
    }
}
#endif
