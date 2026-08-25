using System;
using System.IO;
using UnityEngine;

namespace Lizzo.PV.EditorTools.Capture
{
    public static partial class GameplayOverlayCaptureCommand
    {
        internal static string ValidateRequest(
            string output,
            int width,
            int height,
            float timeoutSeconds,
            string projectRoot,
            bool isPlaying,
            bool hasGameView,
            bool hasActiveSession)
        {
            if (!isPlaying)
                return "Gameplay overlay capture requires Play Mode.";
            if (hasActiveSession)
                return "A Gameplay overlay capture session is already active.";
            if (!hasGameView)
                return "No GameView is open; open a Game View before starting capture.";
            if (width <= 0 || height <= 0)
                return "width and height must be positive.";
            if (width > MaxDimension || height > MaxDimension)
                return $"width and height must be <= {MaxDimension}.";
            if (timeoutSeconds <= 0.0f || timeoutSeconds > 60.0f)
                return "timeoutSeconds must be > 0 and <= 60.";

            try
            {
                string normalizedProjectRoot = NormalizePath(projectRoot);
                string tempRoot = NormalizePath(Path.Combine(normalizedProjectRoot, CaptureRootName));
                string normalizedOutput = NormalizePath(output);
                if (!IsUnderRoot(normalizedOutput, tempRoot))
                    return "Output must be confined to the project Temp folder.";
                if (!string.Equals(Path.GetExtension(normalizedOutput), ".png", StringComparison.OrdinalIgnoreCase))
                    return "Output must use the .png extension.";
            }
            catch (Exception exception)
            {
                return "Invalid output path: " + exception.Message;
            }

            return null;
        }

        static string ResolveOutputPath(string output, string projectRoot)
        {
            if (!string.IsNullOrWhiteSpace(output))
                return Path.IsPathRooted(output) ? Path.GetFullPath(output) : Path.GetFullPath(Path.Combine(projectRoot, output));

            string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff", System.Globalization.CultureInfo.InvariantCulture);
            return Path.Combine(projectRoot, CaptureRootName, DefaultCaptureFolder, "gameplay_overlay_" + stamp + ".png");
        }

        internal static string EnsureOutputDirectory(string outputPath)
        {
            string parent = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(parent))
                Directory.CreateDirectory(parent);
            return parent;
        }

        static string GetProjectRoot()
        {
            return Path.GetDirectoryName(Application.dataPath);
        }

        static string NormalizePath(string path)
        {
            return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        static bool IsUnderRoot(string path, string root)
        {
            return string.Equals(path, root, StringComparison.OrdinalIgnoreCase) ||
                   path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        }
    }
}
