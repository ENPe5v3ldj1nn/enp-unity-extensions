using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;

namespace enp_unity_extensions.Scripts.Language
{
    public static class LanguageController
    {
        public static event UnityAction<SystemLanguage> OnLanguageChanged;
        public static Dictionary<string, string> LanguageDictionary { get; private set; } = new();
        public static SystemLanguage CurrentLanguage { get; private set; }

        private static string _resourcesBasePath = "Languages";

        public static void SetLanguage(SystemLanguage language)
        {
            CurrentLanguage = language;
            var folderName = GetLanguageFolderName(language);
            var fullPath = string.IsNullOrEmpty(_resourcesBasePath) ? folderName : $"{_resourcesBasePath}/{folderName}";

            var assets = Resources.LoadAll<TextAsset>(fullPath);
            if (assets == null || assets.Length == 0)
            {
                Debug.LogError($"No localization files found in Resources/{fullPath}");
                LanguageDictionary = new Dictionary<string, string>();
                OnLanguageChanged?.Invoke(language);
                return;
            }

            var merged = new Dictionary<string, string>();
            var owners = new Dictionary<string, string>();

            foreach (var ta in assets)
            {
                Dictionary<string, string> fileData = null;
                try
                {
                    fileData = JsonConvert.DeserializeObject<Dictionary<string, string>>(ta.text);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to parse localization file {ta.name} in {fullPath}: {e.Message}");
                }

                if (fileData == null) continue;

                foreach (var kv in fileData)
                {
                    if (merged.ContainsKey(kv.Key))
                    {
                        var prevOwner = owners[kv.Key];
                        Debug.LogWarning($"Duplicate localization key '{kv.Key}' in files: {prevOwner} and {ta.name}");
                    }

                    merged[kv.Key] = kv.Value;
                    owners[kv.Key] = ta.name;
                }
            }

            LanguageDictionary = merged;
            OnLanguageChanged?.Invoke(language);
        }

        public static void SetResourcesPath(string path)
        {
            _resourcesBasePath = string.IsNullOrEmpty(path) ? "" : path.TrimEnd('/');
        }
        
        private static string GetLanguageFolderName(SystemLanguage language)
        {
            return language switch
            {
                SystemLanguage.Afrikaans => "afrikaans",
                SystemLanguage.Arabic => "arabic",
                SystemLanguage.Basque => "basque",
                SystemLanguage.Belarusian => "belarusian",
                SystemLanguage.Bulgarian => "bulgarian",
                SystemLanguage.Catalan => "catalan",
                SystemLanguage.Chinese => "chinese",
                SystemLanguage.ChineseSimplified => "chinese_simplified",
                SystemLanguage.ChineseTraditional => "chinese_traditional",
                SystemLanguage.Czech => "czech",
                SystemLanguage.Danish => "danish",
                SystemLanguage.Dutch => "dutch",
                SystemLanguage.English => "english",
                SystemLanguage.Estonian => "estonian",
                SystemLanguage.Faroese => "faroese",
                SystemLanguage.Finnish => "finnish",
                SystemLanguage.French => "french",
                SystemLanguage.German => "german",
                SystemLanguage.Greek => "greek",
                SystemLanguage.Hebrew => "hebrew",
                SystemLanguage.Hungarian => "hungarian",
                SystemLanguage.Icelandic => "icelandic",
                SystemLanguage.Indonesian => "indonesian",
                SystemLanguage.Italian => "italian",
                SystemLanguage.Japanese => "japanese",
                SystemLanguage.Korean => "korean",
                SystemLanguage.Latvian => "latvian",
                SystemLanguage.Lithuanian => "lithuanian",
                SystemLanguage.Norwegian => "norwegian",
                SystemLanguage.Polish => "polish",
                SystemLanguage.Portuguese => "portuguese",
                SystemLanguage.Romanian => "romanian",
                SystemLanguage.Russian => "russian",
                SystemLanguage.SerboCroatian => "serbo_croatian",
                SystemLanguage.Slovak => "slovak",
                SystemLanguage.Slovenian => "slovenian",
                SystemLanguage.Spanish => "spanish",
                SystemLanguage.Swedish => "swedish",
                SystemLanguage.Thai => "thai",
                SystemLanguage.Turkish => "turkish",
                SystemLanguage.Ukrainian => "ukrainian",
                SystemLanguage.Vietnamese => "vietnamese",
                SystemLanguage.Hindi => "hindi",
                _ => "english"
            };
        }
    }
}
