using System;
using System.IO;
using System.Reflection;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Lizzo.PV.EditorTools.Capture
{
    /// <summary>
    /// Captures the live Play Mode Game View framebuffer so ScreenSpaceOverlay UI is included.
    /// The command changes only the selected Game View size and restores it before becoming
    /// terminal. It never changes Canvas, Camera, Scene, or runtime authoring state.
    /// </summary>
    public static partial class GameplayOverlayCaptureCommand
    {
        const int MaxDimension = 4096;
        const float DefaultTimeoutSeconds = 10.0f;
        const string CaptureRootName = "Temp";
        const string DefaultCaptureFolder = "GameplayOverlayCapture";

        static CaptureSession s_session;
        static CaptureStatusResponse s_lastStatus = CaptureStatusResponse.Idle();

        [CliCommand(
            "lizzo_capture_gameplay_overlay_start",
            "Start an exact-resolution Play Mode Game View framebuffer capture including ScreenSpaceOverlay UI.",
            MainThreadRequired = true)]
        public static CaptureStatusResponse StartCapture(
            [CliArg("output", "PNG output path, confined to the project Temp folder.")] string output = "",
            [CliArg("width", "Requested output width in pixels.")] int width = 0,
            [CliArg("height", "Requested output height in pixels.")] int height = 0,
            [CliArg("timeoutSeconds", "Maximum wait for the requested Game View size and PNG.")] float timeoutSeconds = DefaultTimeoutSeconds)
        {
            string projectRoot = GetProjectRoot();
            string resolvedOutput = ResolveOutputPath(output, projectRoot);
            string validationError = ValidateRequest(
                resolvedOutput,
                width,
                height,
                timeoutSeconds,
                projectRoot,
                EditorApplication.isPlaying,
                FindGameView() != null,
                s_session != null);
            if (validationError != null)
                return CaptureStatusResponse.Fail(validationError, resolvedOutput, width, height);

            EditorWindow gameView = FindGameView();
            GameViewCaptureSetup setup = null;
            try
            {
                setup = PrepareGameView(gameView, width, height);
                s_session = new CaptureSession(resolvedOutput, width, height, timeoutSeconds, gameView, setup.RestorePlan);
                s_lastStatus = s_session.BuildResponse();
                EditorApplication.update -= Update;
                EditorApplication.update += Update;
                return s_lastStatus;
            }
            catch (Exception exception)
            {
                if (setup != null)
                    TryRestore(setup.RestorePlan, gameView, out _);
                return CaptureStatusResponse.Fail(
                    "Could not prepare the requested Game View size: " + exception.Message,
                    resolvedOutput,
                    width,
                    height);
            }
        }

        [CliCommand(
            "lizzo_capture_gameplay_overlay_status",
            "Return the pending/pass/fail status of the live Gameplay overlay capture.",
            MainThreadRequired = true)]
        public static CaptureStatusResponse GetStatus()
        {
            return s_session == null ? s_lastStatus : s_session.BuildResponse();
        }

        /// <summary>
        /// Uses the existing public Gameplay debug seam to present a real three-card offer
        /// for the one required capture proof. This is Editor-only and does not ship in runtime.
        /// </summary>
        [CliCommand(
            "lizzo_gameplay_overlay_present_card_offer",
            "Present one real Gameplay CardOffer through the existing public playtest seam.",
            MainThreadRequired = true)]
        public static CardOfferProofResponse PresentCardOffer()
        {
            if (!EditorApplication.isPlaying)
                throw new InvalidOperationException("CardOffer proof requires Play Mode.");

            GameScene gameScene = UnityEngine.Object.FindFirstObjectByType<GameScene>();
            Scene activeScene = SceneManager.GetActiveScene();
            if (gameScene == null || !gameScene.IsRunLoaded || activeScene.path != "Assets/_LizzoPV/Scenes/Gameplay.unity")
                throw new InvalidOperationException("Gameplay must be loaded and ready before presenting CardOffer.");

            gameScene.DebugForceLevelUp();
            return new CardOfferProofResponse
            {
                Presented = true,
                ScenePath = activeScene.path,
                Message = "Presented one real Gameplay CardOffer through GameScene.DebugForceLevelUp."
            };
        }

        internal static bool TryReserveSessionForTests()
        {
            if (s_session != null)
                return false;

            s_session = new CaptureSession("test", 1, 1, 1.0f, null, new GameViewRestorePlan(0, -1, false));
            return true;
        }

        internal static void ResetForTests()
        {
            if (s_session != null)
                s_session.Restore(null);
            s_session = null;
            s_lastStatus = CaptureStatusResponse.Idle();
            EditorApplication.update -= Update;
        }

        static void Update()
        {
            if (s_session == null)
            {
                EditorApplication.update -= Update;
                return;
            }

            try
            {
                if (!EditorApplication.isPlaying)
                {
                    Complete(CaptureStatusResponse.Fail("Play Mode ended before capture completed.", s_session.OutputPath, s_session.RequestedWidth, s_session.RequestedHeight));
                    return;
                }

                if (EditorApplication.timeSinceStartup - s_session.StartedAt > s_session.TimeoutSeconds)
                {
                    Complete(CaptureStatusResponse.Fail("Timed out waiting for the exact Game View framebuffer or PNG.", s_session.OutputPath, s_session.RequestedWidth, s_session.RequestedHeight));
                    return;
                }

                if (!s_session.CaptureRequested)
                {
                    Vector2 renderSize = GetTargetRenderSize(s_session.GameView);
                    if (Mathf.RoundToInt(renderSize.x) != s_session.RequestedWidth ||
                        Mathf.RoundToInt(renderSize.y) != s_session.RequestedHeight)
                    {
                        s_session.GameView.Repaint();
                        return;
                    }

                    if (s_session.ReadyFrames++ < 1)
                    {
                        s_session.GameView.Repaint();
                        return;
                    }

                    EnsureOutputDirectory(s_session.OutputPath);
                    ScreenCapture.CaptureScreenshot(s_session.OutputPath);
                    s_session.CaptureRequested = true;
                    return;
                }

                if (!File.Exists(s_session.OutputPath))
                    return;

                byte[] png = File.ReadAllBytes(s_session.OutputPath);
                Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                try
                {
                    if (!texture.LoadImage(png, true))
                    {
                        Complete(CaptureStatusResponse.Fail("The captured file is not a readable PNG.", s_session.OutputPath, s_session.RequestedWidth, s_session.RequestedHeight));
                        return;
                    }

                    if (texture.width != s_session.RequestedWidth || texture.height != s_session.RequestedHeight)
                    {
                        Complete(CaptureStatusResponse.Fail(
                            $"Captured PNG dimensions were {texture.width}x{texture.height}, expected {s_session.RequestedWidth}x{s_session.RequestedHeight}.",
                            s_session.OutputPath,
                            s_session.RequestedWidth,
                            s_session.RequestedHeight));
                        return;
                    }
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(texture);
                }

                Complete(CaptureStatusResponse.Pass(
                    s_session.OutputPath,
                    s_session.RequestedWidth,
                    s_session.RequestedHeight,
                    "Captured the actual Play Mode Game View framebuffer including ScreenSpaceOverlay UI."));
            }
            catch (Exception exception)
            {
                Complete(CaptureStatusResponse.Fail("Capture failed: " + exception.Message, s_session.OutputPath, s_session.RequestedWidth, s_session.RequestedHeight));
            }
        }

        static void Complete(CaptureStatusResponse terminal)
        {
            CaptureSession session = s_session;
            if (session == null)
                return;

            string restorationError;
            if (!TryRestore(session.RestorePlan, session.GameView, out restorationError))
            {
                terminal.Status = "fail";
                terminal.Success = false;
                terminal.Error = string.IsNullOrWhiteSpace(terminal.Error)
                    ? "Game View restoration failed: " + restorationError
                    : terminal.Error + " Restoration also failed: " + restorationError;
                terminal.RestorationState = "failed";
            }
            else
            {
                terminal.RestorationState = "restored";
            }

            s_lastStatus = terminal;
            s_session = null;
            EditorApplication.update -= Update;
        }

    }

}
