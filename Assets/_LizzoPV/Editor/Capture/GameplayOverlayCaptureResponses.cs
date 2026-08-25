using System;
using Newtonsoft.Json;

namespace Lizzo.PV.EditorTools.Capture
{
    [Serializable]
    public sealed class CaptureStatusResponse
    {
        [JsonProperty("status")] public string Status { get; set; }
        [JsonProperty("success")] public bool Success { get; set; }
        [JsonProperty("outputPath")] public string OutputPath { get; set; }
        [JsonProperty("requestedWidth")] public int RequestedWidth { get; set; }
        [JsonProperty("requestedHeight")] public int RequestedHeight { get; set; }
        [JsonProperty("actualWidth")] public int ActualWidth { get; set; }
        [JsonProperty("actualHeight")] public int ActualHeight { get; set; }
        [JsonProperty("restorationState")] public string RestorationState { get; set; }
        [JsonProperty("error")] public string Error { get; set; }
        [JsonProperty("message")] public string Message { get; set; }

        internal static CaptureStatusResponse Idle() => new CaptureStatusResponse { Status = "idle", RestorationState = "inactive" };

        internal static CaptureStatusResponse Pending(string outputPath, int width, int height) => new CaptureStatusResponse
        {
            Status = "pending",
            OutputPath = outputPath,
            RequestedWidth = width,
            RequestedHeight = height,
            RestorationState = "pending",
            Message = "Waiting for the requested Game View framebuffer."
        };

        internal static CaptureStatusResponse Pass(string outputPath, int width, int height, string message) => new CaptureStatusResponse
        {
            Status = "pass",
            Success = true,
            OutputPath = outputPath,
            RequestedWidth = width,
            RequestedHeight = height,
            ActualWidth = width,
            ActualHeight = height,
            RestorationState = "pending",
            Message = message
        };

        internal static CaptureStatusResponse Fail(string error, string outputPath, int width, int height) => new CaptureStatusResponse
        {
            Status = "fail",
            Success = false,
            OutputPath = outputPath,
            RequestedWidth = width,
            RequestedHeight = height,
            RestorationState = "pending",
            Error = error
        };
    }

    [Serializable]
    public sealed class CardOfferProofResponse
    {
        [JsonProperty("presented")] public bool Presented { get; set; }
        [JsonProperty("scenePath")] public string ScenePath { get; set; }
        [JsonProperty("message")] public string Message { get; set; }
    }
}
