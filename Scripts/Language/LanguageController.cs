using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine.Events;
using UnityEngine;

namespace enp_unity_extensions.Scripts.Language
{
    public static class LanguageController
    {
        public static event UnityAction<SystemLanguage> OnLanguageChanged;

        public static Dictionary<string, string> LanguageDictionary { get; private set; } = new();

        public static SystemLanguage CurrentLanguage { get; private set; }

        private const string ResourcesPath = "Languages/";

        public static void SetLanguage(SystemLanguage language)
        {
            CurrentLanguage = language;

            string fileName = GetLanguageFileName(CurrentLanguage);

            TextAsset jsonFile = Resources.Load<TextAsset>(ResourcesPath + fileName);
            if (jsonFile == null)
            {
                Debug.LogError($"Localization file not found: {fileName}.json");
                return;
            }

            LanguageDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonFile.text);
            OnLanguageChanged?.Invoke(language);
        }

        private static string GetLanguageFileName(SystemLanguage language)
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
                _ => "english" // fallback
            };
        }
    }
}