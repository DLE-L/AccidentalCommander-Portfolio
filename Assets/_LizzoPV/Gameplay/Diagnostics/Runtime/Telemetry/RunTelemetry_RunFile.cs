
namespace Lizzo.PV.Gameplay.Telemetry
{
    public static partial class RunTelemetry
    {
        public static string CurrentRunLogPath => RunTelemetryWriter.CurrentRunLogPath;
        public static string CurrentRunId => RunTelemetryWriter.CurrentRunId;

        public static void FlushRunLog(string reason)
        {
            RunTelemetryWriter.Flush(reason);
        }

    }
}
