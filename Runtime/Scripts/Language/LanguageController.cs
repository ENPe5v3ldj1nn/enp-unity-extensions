using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace enp_unity_extensions.Scripts.Language
{
    public static class LanguageController
    {
        public static event Action<string> LanguageChanged;
        public static event Action<LanguageId> LanguageIdChanged;

        public static LanguageId? CurrentLanguageId { get; private set; }
        public static string CurrentLanguageFolder { get; private set; }
        public static int Version { get; private set; }

        public static readonly Dictionary<string, string> Data = new Dictionary<string, string>();
        public static readonly Dictionary<string, string[]> Arrays = new Dictionary<string, string[]>();

        public static bool IsCanLog { get; set; } = true;

        private static string _resourcesBasePath = "Languages";
        private static readonly string _fallbackLangFolder = LanguageId.EnglishUS.ToFolderName();
        private static bool _hasLanguage;
        private static readonly Dictionary<Type, Delegate> _folderResolvers = new Dictionary<Type, Delegate>(8);

        static LanguageController()
        {
            CurrentLanguageId = LanguageId.EnglishUS;
            CurrentLanguageFolder = _fallbackLangFolder;
        }

        public static void SetResourcesPath(string basePath)
        {
            _resourcesBasePath = string.IsNullOrEmpty(basePath) ? "" : basePath.TrimEnd('/');
        }

        public static void RegisterFolderResolver<TEnum>(Func<TEnum, string> resolver) where TEnum : struct, Enum
        {
            if (resolver == null) throw new ArgumentNullException(nameof(resolver));
            _folderResolvers[typeof(TEnum)] = resolver;
        }

        public static void SetLanguage(LanguageId id)
        {
            var folder = id.ToFolderName();

            if (_hasLanguage && string.Equals(folder, CurrentLanguageFolder, StringComparison.OrdinalIgnoreCase))
            {
                CurrentLanguageId = id;
                LanguageIdChanged?.Invoke(id);
                LanguageChanged?.Invoke(CurrentLanguageFolder);
                return;
            }

            CurrentLanguageId = id;
            CurrentLanguageFolder = folder;
            Reload();
            _hasLanguage = true;
            LanguageIdChanged?.Invoke(id);
            LanguageChanged?.Invoke(CurrentLanguageFolder);
        }

        public static void SetLanguage<TEnum>(TEnum language) where TEnum : struct, Enum
        {
            if (!_folderResolvers.TryGetValue(typeof(TEnum), out var del))
                throw new InvalidOperationException($"Folder resolver is not registered for enum type '{typeof(TEnum).FullName}'.");

            var resolver = (Func<TEnum, string>)del;
            var folder = resolver(language);
            if (string.IsNullOrWhiteSpace(folder))
                throw new InvalidOperationException($"Folder resolver returned empty folder for enum '{typeof(TEnum).FullName}' value '{language}'.");

            if (_hasLanguage && string.Equals(folder, CurrentLanguageFolder, StringComparison.OrdinalIgnoreCase))
            {
                CurrentLanguageId = null;
                LanguageChanged?.Invoke(CurrentLanguageFolder);
                return;
            }

            CurrentLanguageId = null;
            CurrentLanguageFolder = folder;
            Reload();
            _hasLanguage = true;
            LanguageChanged?.Invoke(CurrentLanguageFolder);
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

        private static void LoadFolder(string langFolder)
        {
            var path = string.IsNullOrEmpty(_resourcesBasePath) ? langFolder : $"{_resourcesBasePath}/{langFolder}";
            var assets = Resources.LoadAll<TextAsset>(path);
            if (assets == null || assets.Length == 0) return;

            foreach (var ta in assets)
            {
                try
                {
                    if (ta == null)
                    {
                        LogWarning($"[Language] Null TextAsset found in '{path}', skipping.");
                        continue;
                    }

                    var root = JObject.Parse(ta.text);
                    foreach (var prop in root.Properties())
                    {
                        var key = prop.Name;
                        if (string.IsNullOrWhiteSpace(key))
                        {
                            LogWarning($"[Language] Empty key in '{ta.name}' ({path}), skipping.");
                            continue;
                        }

                        var token = prop.Value;
                        if (token.Type == JTokenType.String)
                        {
                            if (Data.ContainsKey(key))
                                LogWarning($"[Language] Key '{key}' overwritten by '{ta.name}' in '{path}'.");

                            Data[key] = token.Value<string>() ?? string.Empty;
                        }
                        else if (token.Type == JTokenType.Array && token is JArray arrayToken)
                        {
                            var list = new List<string>(arrayToken.Count);
                            foreach (var item in arrayToken)
                            {
                                if (item.Type != JTokenType.String)
                                {
                                    LogWarning($"[Language] Key '{key}' in '{ta.name}' ({path}) has non-string array item, skipping.");
                                    continue;
                                }

                                var value = item.Value<string>();
                                if (string.IsNullOrWhiteSpace(value))
                                {
                                    LogWarning($"[Language] Key '{key}' in '{ta.name}' ({path}) has empty array item, skipping.");
                                    continue;
                                }

                                list.Add(value.Trim());
                            }

                            if (list.Count > 0)
                            {
                                if (Arrays.ContainsKey(key))
                                    LogWarning($"[Language] Array key '{key}' overwritten by '{ta.name}' in '{path}'.");

                                Arrays[key] = list.ToArray();
                            }
                            else
                            {
                                LogWarning($"[Language] Array key '{key}' in '{ta.name}' ({path}) has no valid items, skipping.");
                            }
                        }
                        else
                        {
                            LogWarning($"[Language] Key '{key}' in '{ta.name}' ({path}) has unsupported token type '{token.Type}', skipping.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogError($"[Language] Failed to parse '{ta?.name ?? "<null>"}' in '{path}': {ex.Message}");
                }
            }
        }

        private static void LogWarning(string message)
        {
            if (!IsCanLog) return;
            Debug.LogWarning(message);
        }

        private static void LogError(string message)
        {
            if (!IsCanLog) return;
            Debug.LogError(message);
        }
    }
}