using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace ENP.UnityExtensions.Runtime
{
    public static class LanguageController
    {
        public static event Action<LanguageId> LanguageChanged;

        public static LanguageId CurrentLanguageId { get; private set; }
        public static string CurrentLanguageFolder { get; private set; }
        public static int Version { get; private set; }

        public static readonly Dictionary<string, string> Data = new Dictionary<string, string>();
        public static readonly Dictionary<string, string[]> Arrays = new Dictionary<string, string[]>();
        public static readonly Dictionary<string, Dictionary<PluralCategory, string>> Plurals = new Dictionary<string, Dictionary<PluralCategory, string>>();

        public static bool IsCanLog { get; set; } = true;

        private static string _resourcesBasePath = "Languages";
        private static readonly string _fallbackLangFolder = LanguageId.EnglishUS.ToFolderName();
        private static bool _hasLanguage;

        static LanguageController()
        {
            CurrentLanguageId = LanguageId.EnglishUS;
            CurrentLanguageFolder = _fallbackLangFolder;
        }

        public static void SetResourcesPath(string basePath)
        {
            _resourcesBasePath = string.IsNullOrEmpty(basePath) ? "" : basePath.TrimEnd('/');
        }

        public static void SetLanguage(LanguageId id)
        {
            var folder = id.ToFolderName();

            if (_hasLanguage && string.Equals(folder, CurrentLanguageFolder, StringComparison.OrdinalIgnoreCase))
            {
                CurrentLanguageId = id;
                LanguageChanged?.Invoke(id);
                return;
            }

            CurrentLanguageId = id;
            CurrentLanguageFolder = folder;
            Reload();
            _hasLanguage = true;
            LanguageChanged?.Invoke(id);
        }

        public static bool TryGetDeviceLanguage(out LanguageId id)
        {
            var locale = GetDeviceLocaleCode();
            return LanguageIdExtensions.TryFromLocaleCode(locale, out id);
        }

        public static LanguageId ResolveSelectedLanguage(LanguageId storedLanguage, bool wasLaunchedBefore, IReadOnlyList<LanguageId> availableLanguages)
        {
            if (!wasLaunchedBefore && TryGetDeviceLanguage(out var deviceLanguage) &&
                TryResolveAvailable(deviceLanguage, availableLanguages, out var resolvedDevice))
                return resolvedDevice;

            return TryResolveAvailable(storedLanguage, availableLanguages, out var resolvedStored)
                ? resolvedStored
                : GetFallbackLanguage(availableLanguages);
        }

        // Exact match first, then another regional variant of the same language (a pt-PT device gets
        // pt-BR, fr-CA gets fr-FR) — any variant of the player's language beats the English fallback.
        public static bool TryResolveAvailable(LanguageId requested, IReadOnlyList<LanguageId> availableLanguages, out LanguageId resolved)
        {
            if (Contains(availableLanguages, requested))
            {
                resolved = requested;
                return true;
            }

            if (availableLanguages != null)
            {
                var primary = requested.ToPrimaryCode();
                for (var i = 0; i < availableLanguages.Count; i++)
                {
                    if (!string.Equals(availableLanguages[i].ToPrimaryCode(), primary, StringComparison.Ordinal))
                        continue;

                    resolved = availableLanguages[i];
                    return true;
                }
            }

            resolved = requested;
            return false;
        }

        public static LanguageId GetFallbackLanguage(IReadOnlyList<LanguageId> availableLanguages)
        {
            if (Contains(availableLanguages, LanguageId.EnglishUS))
                return LanguageId.EnglishUS;

            if (availableLanguages != null && availableLanguages.Count > 0)
                return availableLanguages[0];

            return LanguageId.EnglishUS;
        }

        public static string Get(string key)
        {
            if (string.IsNullOrEmpty(key)) return string.Empty;
            return Data.TryGetValue(key, out var v) ? v : string.Empty;
        }

        public static string[] GetArray(string key)
        {
            if (string.IsNullOrEmpty(key)) return Array.Empty<string>();
            return Arrays.TryGetValue(key, out var arr) && arr != null && arr.Length > 0 ? arr : Array.Empty<string>();
        }

        // Picks the plural form for count in the current language; falls back to "other", then to any
        // form present, so a translation that only defines "other" still renders.
        public static string GetPlural(string key, long count)
        {
            if (string.IsNullOrEmpty(key)) return string.Empty;
            if (!Plurals.TryGetValue(key, out var forms) || forms.Count == 0) return string.Empty;

            if (forms.TryGetValue(PluralRules.Select(CurrentLanguageId, count), out var form)) return form;
            if (forms.TryGetValue(PluralCategory.Other, out var other)) return other;

            foreach (var any in forms.Values)
                return any;

            return string.Empty;
        }

        public static void Reload()
        {
            Data.Clear();
            Arrays.Clear();
            Plurals.Clear();
            LoadFolder(_fallbackLangFolder);

            if (!string.Equals(CurrentLanguageFolder, _fallbackLangFolder, StringComparison.OrdinalIgnoreCase))
                LoadFolder(CurrentLanguageFolder);

            Version++;
        }

        private static string GetDeviceLocaleCode()
        {
            try
            {
                var ui = CultureInfo.CurrentUICulture?.Name;
                if (!string.IsNullOrWhiteSpace(ui)) return ui;
            }
            catch
            {
            }

            try
            {
                var c = CultureInfo.CurrentCulture?.Name;
                if (!string.IsNullOrWhiteSpace(c)) return c;
            }
            catch
            {
            }

            return null;
        }

        private static bool Contains(IReadOnlyList<LanguageId> list, LanguageId value)
        {
            if (list == null) return false;
            for (var i = 0; i < list.Count; i++)
            {
                if (list[i] == value) return true;
            }
            return false;
        }

        private static void LoadFolder(string langFolder)
        {
            var path = string.IsNullOrEmpty(_resourcesBasePath) ? langFolder : $"{_resourcesBasePath}/{langFolder}";
            var assets = Resources.LoadAll<TextAsset>(path);
            if (assets == null || assets.Length == 0) return;

            for (var i = 0; i < assets.Length; i++)
            {
                var ta = assets[i];
                if (ta == null) continue;

                try
                {
                    var root = JObject.Parse(ta.text);
                    foreach (var prop in root.Properties())
                    {
                        var key = prop.Name;
                        if (string.IsNullOrWhiteSpace(key)) continue;

                        var token = prop.Value;
                        if (token.Type == JTokenType.String)
                        {
                            Data[key] = token.Value<string>() ?? string.Empty;
                            continue;
                        }

                        if (token.Type == JTokenType.Array && token is JArray arrayToken)
                        {
                            var list = new List<string>(arrayToken.Count);
                            foreach (var item in arrayToken)
                            {
                                if (item.Type != JTokenType.String) continue;

                                var value = item.Value<string>();
                                if (string.IsNullOrWhiteSpace(value)) continue;

                                list.Add(value.Trim());
                            }

                            if (list.Count > 0)
                                Arrays[key] = list.ToArray();

                            continue;
                        }

                        if (token.Type == JTokenType.Object && token is JObject pluralToken)
                        {
                            var forms = ParsePluralForms(pluralToken);
                            if (forms.Count > 0)
                                Plurals[key] = forms;
                            else
                                LogError($"[Language] Plural key '{key}' in '{ta.name}' has no recognised forms (zero/one/two/few/many/other).");

                            continue;
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogError($"[Language] Failed to parse '{ta.name}' in '{path}': {ex.Message}");
                }
            }
        }

        private static Dictionary<PluralCategory, string> ParsePluralForms(JObject pluralToken)
        {
            var forms = new Dictionary<PluralCategory, string>();
            foreach (var form in pluralToken.Properties())
            {
                if (form.Value.Type != JTokenType.String) continue;
                if (!TryParsePluralCategory(form.Name, out var category)) continue;

                forms[category] = form.Value.Value<string>() ?? string.Empty;
            }

            return forms;
        }

        private static bool TryParsePluralCategory(string name, out PluralCategory category)
        {
            foreach (PluralCategory candidate in Enum.GetValues(typeof(PluralCategory)))
            {
                if (!string.Equals(candidate.ToKey(), name, StringComparison.OrdinalIgnoreCase)) continue;

                category = candidate;
                return true;
            }

            category = PluralCategory.Other;
            return false;
        }

        private static void LogError(string message)
        {
            if (!IsCanLog) return;
            UnityEngine.Debug.LogError(message);
        }
    }
}