using System.Collections.Generic;
using Unity.Plastic.Newtonsoft.Json;
using UnityEngine.Events;
using UnityEngine;

namespace enp_unity_extensions.Scripts.Language
{
    public static class LanguageController
    {
        public static event UnityAction<Language> OnLanguageChanged;

        public static Dictionary<string, string> LanguageDictionary { get; private set; } = new();

        public static Language CurrentLanguage { get; private set; }

        private const string ResourcesPath = "Languages/";

        public static void SetLanguage(Language language)
        {
            CurrentLanguage = language;

            string fileName = language switch
            {
                Language.English => "en",
                Language.Ukrainian => "uk",
                _ => "en"
            };

            TextAsset jsonFile = Resources.Load<TextAsset>(ResourcesPath + fileName);
            if (jsonFile == null)
            {
                Debug.LogError($"Localization file not found: {fileName}.json");
                return;
            }

            LanguageDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonFile.text);
            OnLanguageChanged?.Invoke(language);
        }
    }

    public enum Language
    {
        English,
        Ukrainian,
    }
}