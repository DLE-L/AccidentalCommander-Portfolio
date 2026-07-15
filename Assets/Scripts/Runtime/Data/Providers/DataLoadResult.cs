using System.Collections.Generic;

namespace Lizzo.PV.Data
{
    public sealed class DataLoadResult
    {
        public bool Succeeded { get; internal set; }
        public string Source { get; internal set; } = string.Empty;
        public bool UsedFallback { get; internal set; }
        public string ParseError { get; internal set; } = string.Empty;
        public List<string> MissingRequiredIds { get; } = new List<string>();
    }
}