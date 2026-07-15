using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace Lizzo.PV.Localization
{
    public sealed class LocalLocalizationProvider : ILocalizationProvider
    {
        const string CATALOG_ADDRESS = "LocalizationCatalog.json";
        const string LOCALE_PREFS_KEY = "localization.locale";

        readonly IAssetService _assets;
        readonly Dictionary<string, Dictionary<string, string>> _tables = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
        readonly HashSet<string> _reportedErrors = new HashSet<string>(StringComparer.Ordinal);
        readonly List<string> _availableLocales = new List<string>();

        LocalizationCatalogData _catalog;
        LocalizationLocaleData _defaultDefinition;
        string _currentLocale;
        TMP_FontAsset _currentFont;
        bool _initialized;

        public LocalLocalizationProvider(IAssetService assets)
        {
            _assets = assets ?? throw new ArgumentNullException(nameof(assets));
        }

        public bool IsInitialized => _initialized;
        public string CurrentLocale => _currentLocale;
        public IReadOnlyList<string> AvailableLocales => _availableLocales;
        public TMP_FontAsset CurrentFont => _currentFont;
        public event Action<string> LocaleChanged;

        public async UniTask<LocalizationLoadResult> InitializeAsync(CancellationToken cancellationToken = default)
        {
            if (_initialized)
            {
                return new LocalizationLoadResult
                {
                    Succeeded = true,
                    CurrentLocale = _currentLocale
                };
            }

            var result = new LocalizationLoadResult();
            TextAsset catalogAsset = await LoadTextAssetAsync(CATALOG_ADDRESS, cancellationToken);
            if (catalogAsset == null)
            {
                result.ParseError = "Localization catalog asset is missing.";
                Debug.LogError("[Localization] " + result.ParseError + " address=" + CATALOG_ADDRESS);
                return result;
            }

            try
            {
                _catalog = JsonUtility.FromJson<LocalizationCatalogData>(catalogAsset.text);
                ValidateCatalog(result);
            }
            catch (Exception exception)
            {
                result.ParseError = exception.Message;
                Debug.LogError("[Localization] Catalog parse failed: " + exception);
                return result;
            }

            if (_defaultDefinition == null)
            {
                result.ParseError = "Localization catalog has no valid default locale.";
                Debug.LogError("[Localization] " + result.ParseError);
                return result;
            }

            Dictionary<string, string> defaultTable = await LoadTableAsync(_defaultDefinition, cancellationToken);
            if (defaultTable == null)
            {
                result.ParseError = "Default locale table could not be loaded.";
                Debug.LogError("[Localization] " + result.ParseError + " locale=" + _defaultDefinition.code);
                return result;
            }

            _tables[_defaultDefinition.code] = defaultTable;
            string requestedLocale = ResolveRequestedLocale();
            string resolvedLocale = _defaultDefinition.code;
            if (requestedLocale != _defaultDefinition.code)
            {
                LocalizationLocaleData requestedDefinition = FindLocale(requestedLocale);
                if (requestedDefinition != null)
                {
                    Dictionary<string, string> requestedTable = await LoadTableAsync(requestedDefinition, cancellationToken);
                    if (requestedTable != null)
                    {
                        _tables[requestedDefinition.code] = requestedTable;
                        resolvedLocale = requestedDefinition.code;ValidateAgainstDefault(requestedDefinition.code, requestedTable, result);
                    }
                    else
                    {
                        result.UsedFallback = true;
                        Debug.LogError("[Localization] Requested locale failed; using default locale. locale=" + requestedLocale);
                    }
                }
                else
                {
                    result.UsedFallback = true;
                    Debug.LogError("[Localization] Requested locale is not in catalog; using default locale. locale=" + requestedLocale);
                }
            }

            _currentLocale = resolvedLocale;
            _currentFont = await LoadFontAsync(FindLocale(_currentLocale), cancellationToken);
            _initialized = true;
            result.Succeeded = true;
            result.CurrentLocale = _currentLocale;
            PlayerPrefs.SetString(LOCALE_PREFS_KEY, _currentLocale);
            PlayerPrefs.Save();
            return result;
        }

        public async UniTask<bool> SetLocaleAsync(string localeCode, CancellationToken cancellationToken = default)
        {
            EnsureInitialized();
            LocalizationLocaleData definition = FindLocale(localeCode);
            if (definition == null)
            {
                LogErrorOnce("invalid_locale:" + localeCode, "[Localization] Locale is not configured: " + localeCode);
                return false;
            }

            if (definition.code == _currentLocale)
                return true;

            Dictionary<string, string> table = await LoadTableAsync(definition, cancellationToken);
            if (table == null)
                return false;

            _tables[definition.code] = table;
            _currentLocale = definition.code;
            _currentFont = await LoadFontAsync(definition, cancellationToken);
            PlayerPrefs.SetString(LOCALE_PREFS_KEY, _currentLocale);
            PlayerPrefs.Save();
            LocaleChanged?.Invoke(_currentLocale);
            return true;
        }

        public string Get(string key)
        {
            EnsureInitialized();
            if (string.IsNullOrEmpty(key))
            {
                LogErrorOnce("empty_key", "[Localization] Empty localization key requested.");
                return "[missing:key]";
            }

            if (TryGetFromTable(_currentLocale, key, out string value))
                return value;

            if (_defaultDefinition != null && _defaultDefinition.code != _currentLocale && TryGetFromTable(_defaultDefinition.code, key, out value))
            {
                LogErrorOnce("fallback_key:" + _currentLocale + ":" + key, "[Localization] Missing key in current locale; using default. locale=" + _currentLocale + " key=" + key);
                return value;
            }

            LogErrorOnce("missing_key:" + key, "[Localization] Missing localization key: " + key);
            return "[missing:" + key + "]";
        }

        public string Format(string key, params object[] args)
        {
            string template = Get(key);
            try
            {
                CultureInfo culture = CultureInfo.InvariantCulture;
                if (string.IsNullOrEmpty(_currentLocale) == false)
                {
                    try
                    {
                        culture = CultureInfo.GetCultureInfo(_currentLocale);
                    }
                    catch (CultureNotFoundException)
                    {
                    }
                }

                return string.Format(culture, template, args ?? Array.Empty<object>());
            }
            catch (FormatException exception)
            {
                LogErrorOnce("format:" + key, "[Localization] Format failed for key=" + key + " error=" + exception.Message);
                return "[format:" + key + "]";
            }
        }

        async UniTask<TextAsset> LoadTextAssetAsync(string address, CancellationToken cancellationToken)
        {
            try
            {
                return await _assets.LoadAsync<TextAsset>(address, cancellationToken);
            }
            catch (Exception exception)
            {
                Debug.LogError("[Localization] TextAsset load failed. address=" + address + " error=" + exception.Message);
                return null;
            }
        }

        async UniTask<Dictionary<string, string>> LoadTableAsync(LocalizationLocaleData definition, CancellationToken cancellationToken)
        {
            if (definition == null || string.IsNullOrEmpty(definition.address))
                return null;

            TextAsset asset = await LoadTextAssetAsync(definition.address, cancellationToken);
            if (asset == null)
                return null;

            try
            {
                LocalizationTableData table = JsonUtility.FromJson<LocalizationTableData>(asset.text);
                if (table == null || string.IsNullOrEmpty(table.locale) || table.locale != definition.code)
                    throw new InvalidOperationException("Locale table code does not match catalog.");

                var values = new Dictionary<string, string>(StringComparer.Ordinal);
                LocalizationEntryData[] entries = table.entries ?? Array.Empty<LocalizationEntryData>();
                for (int i = 0; i < entries.Length; i++)
                {
                    LocalizationEntryData entry = entries[i];
                    if (entry == null || string.IsNullOrEmpty(entry.key))
                        throw new InvalidOperationException("Locale table contains an empty key.");
                    if (values.ContainsKey(entry.key))
                        throw new InvalidOperationException("Locale table contains duplicate key: " + entry.key);
                    values.Add(entry.key, entry.value ?? string.Empty);
                }

                return values;
            }
            catch (Exception exception)
            {
                Debug.LogError("[Localization] Locale table parse failed. locale=" + definition.code + " error=" + exception.Message);
                return null;
            }
        }

        void ValidateCatalog(LocalizationLoadResult result)
        {
            _availableLocales.Clear();
            _defaultDefinition = null;
            if (_catalog == null || _catalog.locales == null || _catalog.locales.Length == 0)
                return;

            for (int i = 0; i < _catalog.locales.Length; i++)
            {
                LocalizationLocaleData definition = _catalog.locales[i];
                if (definition == null || string.IsNullOrEmpty(definition.code) || string.IsNullOrEmpty(definition.address))
                {
                    result.AddInvalidLocale("index:" + i);
                    continue;
                }

                if (_availableLocales.Contains(definition.code))
                {
                    result.AddInvalidLocale(definition.code);
                    continue;
                }

                _availableLocales.Add(definition.code);
                if (definition.code == _catalog.defaultLocale)
                    _defaultDefinition = definition;
            }
        }

        void ValidateAgainstDefault(string localeCode, Dictionary<string, string> table, LocalizationLoadResult result)
        {
            if (_defaultDefinition == null || !_tables.TryGetValue(_defaultDefinition.code, out Dictionary<string, string> defaultTable))
                return;

            foreach (string key in defaultTable.Keys)
            {
                if (table.ContainsKey(key) == false)
                    result.AddMissingKey(localeCode + ":" + key);
            }

            if (result.MissingKeys.Count > 0)
                Debug.LogError("[Localization] Locale is missing default keys. locale=" + localeCode + " count=" + result.MissingKeys.Count);
        }

        LocalizationLocaleData FindLocale(string localeCode)
        {
            if (_catalog == null || _catalog.locales == null)
                return null;

            for (int i = 0; i < _catalog.locales.Length; i++)
            {
                LocalizationLocaleData definition = _catalog.locales[i];
                if (definition != null && definition.code == localeCode)
                    return definition;
            }

            return null;
        }

        string ResolveRequestedLocale()
        {
            string savedLocale = PlayerPrefs.GetString(LOCALE_PREFS_KEY, string.Empty);
            if (FindLocale(savedLocale) != null)
                return savedLocale;

            string systemLanguage = Application.systemLanguage.ToString();
            if (_catalog != null && _catalog.locales != null)
            {
                for (int i = 0; i < _catalog.locales.Length; i++)
                {
                    LocalizationLocaleData definition = _catalog.locales[i];
                    if (definition == null || definition.systemLanguages == null)
                        continue;

                    for (int j = 0; j < definition.systemLanguages.Length; j++)
                    {
                        if (definition.systemLanguages[j] == systemLanguage)
                            return definition.code;
                    }
                }
            }

            return _defaultDefinition == null ? string.Empty : _defaultDefinition.code;
        }

        async UniTask<TMP_FontAsset> LoadFontAsync(LocalizationLocaleData definition, CancellationToken cancellationToken)
        {
            if (definition == null || string.IsNullOrEmpty(definition.fontAddress))
                return null;

            try
            {
                return await _assets.LoadAsync<TMP_FontAsset>(definition.fontAddress, cancellationToken);
            }
            catch (Exception exception)
            {
                Debug.LogError("[Localization] Font load failed. locale=" + definition.code + " error=" + exception.Message);
                return null;
            }
        }

        bool TryGetFromTable(string localeCode, string key, out string value)
        {
            if (string.IsNullOrEmpty(localeCode) == false && _tables.TryGetValue(localeCode, out Dictionary<string, string> table) && table.TryGetValue(key, out value))
                return true;

            value = null;
            return false;
        }

        void EnsureInitialized()
        {
            if (!_initialized)
                throw new InvalidOperationException("[Localization] Provider is not initialized.");
        }

        void LogErrorOnce(string id, string message)
        {
            if (_reportedErrors.Add(id))
                Debug.LogError(message);
        }
    }
}