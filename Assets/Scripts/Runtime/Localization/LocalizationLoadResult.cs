using System.Collections.Generic;

namespace Lizzo.PV.Localization
{
    public sealed class LocalizationLoadResult
    {
        public bool Succeeded { get; internal set; }
        public string CurrentLocale { get; internal set; }
        public bool UsedFallback { get; internal set; }
        public string ParseError { get; internal set; }
        public IReadOnlyList<string> MissingKeys => _missingKeys;
        public IReadOnlyList<string> InvalidLocales => _invalidLocales;

        readonly List<string> _missingKeys = new List<string>();
        readonly List<string> _invalidLocales = new List<string>();

        internal void AddMissingKey(string key)
        {
            if (string.IsNullOrEmpty(key) == false && _missingKeys.Contains(key) == false)
                _missingKeys.Add(key);
        }

        internal void AddInvalidLocale(string locale)
        {
            if (string.IsNullOrEmpty(locale) == false && _invalidLocales.Contains(locale) == false)
                _invalidLocales.Add(locale);
        }
    }
}