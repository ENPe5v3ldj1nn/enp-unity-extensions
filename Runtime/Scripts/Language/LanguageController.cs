using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace enp_unity_extensions.Scripts.Language
{
    public static class LanguageController
    {
        public static event Action<SystemLanguage> LanguageChanged;
        public static SystemLanguage CurrentLanguage { get; private set; }
        public static readonly Dictionary<string, string> Data = new Dictionary<string, string>();
        public static readonly Dictionary<string, string[]> Arrays = new Dictionary<string, string[]>();
        public static bool IsCanLog { get; set; } = true;

        private static string _resourcesBasePath = "Languages";
        private const string FallbackLangFolder = "english";

        public static void SetResourcesPath(string basePath)
        {
            _resourcesBasePath = string.IsNullOrEmpty(basePath) ? "" : basePath.TrimEnd('/');
        }

        public static void SetLanguage(SystemLanguage language)
        {
            CurrentLanguage = language;
            Reload();
            LanguageChanged?.Invoke(CurrentLanguage);
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
            LoadFolder(FallbackLangFolder);
            LoadFolder(GetLanguageFolderName(CurrentLanguage));
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
                            {
                                LogWarning($"[Language] Key '{key}' overwritten by '{ta.name}' in '{path}'.");
                            }

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
                                {
                                    LogWarning($"[Language] Array key '{key}' overwritten by '{ta.name}' in '{path}'.");
                                }

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

        private static string GetLanguageFolderName(SystemLanguage language)
        {
            switch (language)
            {
                case SystemLanguage.Afrikaans: return "afrikaans";
                case SystemLanguage.Arabic: return "arabic";
                case SystemLanguage.Basque: return "basque";
                case SystemLanguage.Belarusian: return "belarusian";
                case SystemLanguage.Bulgarian: return "bulgarian";
                case SystemLanguage.Catalan: return "catalan";
                case SystemLanguage.Chinese: return "chinese";
                case SystemLanguage.ChineseSimplified: return "chinese_simplified";
                case SystemLanguage.ChineseTraditional: return "chinese_traditional";
                case SystemLanguage.Czech: return "czech";
                case SystemLanguage.Danish: return "danish";
                case SystemLanguage.Dutch: return "dutch";
                case SystemLanguage.English: return "english";
                case SystemLanguage.Estonian: return "estonian";
                case SystemLanguage.Faroese: return "faroese";
                case SystemLanguage.Finnish: return "finnish";
                case SystemLanguage.French: return "french";
                case SystemLanguage.German: return "german";
                case SystemLanguage.Greek: return "greek";
                case SystemLanguage.Hebrew: return "hebrew";
                case SystemLanguage.Hungarian: return "hungarian";
                case SystemLanguage.Icelandic: return "icelandic";
                case SystemLanguage.Indonesian: return "indonesian";
                case SystemLanguage.Italian: return "italian";
                case SystemLanguage.Japanese: return "japanese";
                case SystemLanguage.Korean: return "korean";
                case SystemLanguage.Latvian: return "latvian";
                case SystemLanguage.Lithuanian: return "lithuanian";
                case SystemLanguage.Norwegian: return "norwegian";
                case SystemLanguage.Polish: return "polish";
                case SystemLanguage.Portuguese: return "portuguese";
                case SystemLanguage.Romanian: return "romanian";
                case SystemLanguage.Russian: return "russian";
                case SystemLanguage.SerboCroatian: return "serbo_croatian";
                case SystemLanguage.Slovak: return "slovak";
                case SystemLanguage.Slovenian: return "slovenian";
                case SystemLanguage.Spanish: return "spanish";
                case SystemLanguage.Swedish: return "swedish";
                case SystemLanguage.Thai: return "thai";
                case SystemLanguage.Turkish: return "turkish";
                case SystemLanguage.Ukrainian: return "ukrainian";
                case SystemLanguage.Vietnamese: return "vietnamese";
                case SystemLanguage.Hindi: return "hindi";
                default: return "english";
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
