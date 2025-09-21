using TMPro;

namespace enp_unity_extensions.Scripts.Language
{
    public static class LanguageExtension
    {
        public static void SetKey(this TMP_Text targetTMP, string key)
        {
            var languageText = GetOrSetLanguageText(targetTMP);
            languageText.SetKey(key);
        }
        
        public static void SetKeyWithParams(this TMP_Text targetTMP, string key, params object[] args)
        {
            var languageText = GetOrSetLanguageText(targetTMP);
            languageText.SetKeyWithParams(key, args);
        }

        public static void RefreshLanguage(this TMP_Text targetTMP)
        {
            var languageText = GetOrSetLanguageText(targetTMP);
            languageText.Refresh();
        }

        private static LanguageText GetOrSetLanguageText(TMP_Text targetTMP)
        {
            if (targetTMP == null) throw new System.ArgumentNullException(nameof(targetTMP));
            if (!targetTMP.TryGetComponent(out LanguageText languageText))
                languageText = targetTMP.gameObject.AddComponent<LanguageText>();
            return languageText;
        }
    }
}