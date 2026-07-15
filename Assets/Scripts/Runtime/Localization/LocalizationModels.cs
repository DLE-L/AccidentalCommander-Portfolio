using System;

namespace Lizzo.PV.Localization
{
    [Serializable]
    public sealed class LocalizationCatalogData
    {
        public string defaultLocale;
        public LocalizationLocaleData[] locales;
    }

    [Serializable]
    public sealed class LocalizationLocaleData
    {
        public string code;
        public string address;
        public string[] systemLanguages;
        public string fontAddress;
    }

    [Serializable]
    public sealed class LocalizationTableData
    {
        public string locale;
        public LocalizationEntryData[] entries;
    }

    [Serializable]
    public sealed class LocalizationEntryData
    {
        public string key;
        public string value;
    }
}