using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace enp_unity_extensions.Runtime.Scripts.Language
{
    public static class LanguageController
    {
        public static event Action<LanguageId> LanguageChanged;

        public static LanguageId CurrentLanguageId { get; private set; }
        public static string CurrentLanguageFolder { get; private set; }
        public static int Version { get; private set; }

        public static readonly Dictionary<string, string> Data = new Dictionary<string, string>();
        public static readonly Dictionary<string, string[]> Arrays = new Dictionary<string, string[]>();

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
            var fallback = Contains(availableLanguages, storedLanguage)
                ? storedLanguage
                : GetFallbackLanguage(availableLanguages);

            if (!wasLaunchedBefore)
            {
                if (TryGetDeviceLanguage(out var deviceLanguage) && Contains(availableLanguages, deviceLanguage))
                    return deviceLanguage;
            }

            return fallback;
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

        public static void Reload()
        {
            Data.Clear();
            Arrays.Clear();
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
                    }
                }
                catch (Exception ex)
                {
                    LogError($"[Language] Failed to parse '{ta.name}' in '{path}': {ex.Message}");
                }
            }
        }

        private static void LogError(string message)
        {
            if (!IsCanLog) return;
            Debug.LogError(message);
        }
    }
}