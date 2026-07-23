using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;

namespace Lizzo.PV.Localization
{
    public interface ILocalizationProvider
    {
        bool IsInitialized { get; }
        string CurrentLocale { get; }
        IReadOnlyList<string> AvailableLocales { get; }
        TMP_FontAsset CurrentFont { get; }
        event Action<string> LocaleChanged;

        UniTask<LocalizationLoadResult> InitializeAsync(CancellationToken cancellationToken = default);
        UniTask<bool> SetLocaleAsync(string localeCode, CancellationToken cancellationToken = default);
        string Get(string key);
        string Format(string key, params object[] args);
    }
}