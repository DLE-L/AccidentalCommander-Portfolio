using System;
using UnityEditor;

namespace Lizzo.PV.EditorTools.Capture
{
    internal sealed class GameViewCaptureSetup
    {
        internal GameViewCaptureSetup(GameViewRestorePlan restorePlan)
        {
            RestorePlan = restorePlan;
        }

        internal GameViewRestorePlan RestorePlan { get; }
    }

    internal sealed class GameViewRestorePlan
    {
        readonly int _previousIndex;
        readonly int _temporaryIndex;
        readonly bool _hasTemporarySize;

        internal GameViewRestorePlan(int previousIndex, int temporaryIndex, bool hasTemporarySize, string groupName = null)
        {
            _previousIndex = previousIndex;
            _temporaryIndex = temporaryIndex;
            _hasTemporarySize = hasTemporarySize;
            GroupName = groupName;
        }

        internal bool IsRestored { get; private set; }
        internal string GroupName { get; }

        internal void Restore(Action<int> selectIndex, Action<int> removeTemporarySize)
        {
            if (IsRestored)
                return;
            if (_hasTemporarySize)
                removeTemporarySize(_temporaryIndex);
            selectIndex(_previousIndex);
            IsRestored = true;
        }
    }

    internal sealed class CaptureSession
    {
        internal CaptureSession(string outputPath, int requestedWidth, int requestedHeight, float timeoutSeconds, EditorWindow gameView, GameViewRestorePlan restorePlan)
        {
            OutputPath = outputPath;
            RequestedWidth = requestedWidth;
            RequestedHeight = requestedHeight;
            TimeoutSeconds = timeoutSeconds;
            GameView = gameView;
            RestorePlan = restorePlan;
            StartedAt = EditorApplication.timeSinceStartup;
        }

        internal string OutputPath { get; }
        internal int RequestedWidth { get; }
        internal int RequestedHeight { get; }
        internal float TimeoutSeconds { get; }
        internal EditorWindow GameView { get; }
        internal GameViewRestorePlan RestorePlan { get; }
        internal double StartedAt { get; }
        internal int ReadyFrames { get; set; }
        internal bool CaptureRequested { get; set; }

        internal CaptureStatusResponse BuildResponse()
        {
            return CaptureStatusResponse.Pending(OutputPath, RequestedWidth, RequestedHeight);
        }

        internal void Restore(EditorWindow ignored)
        {
            RestorePlan.Restore(_ => { }, _ => { });
        }
    }
}
