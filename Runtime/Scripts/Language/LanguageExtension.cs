using System.Runtime.CompilerServices;
using enp_unity_extensions.Scripts.Language;
using TMPro;

namespace enp_unity_extensions.Runtime.Scripts.Language
{
    public static class LanguageExtension
    {
        private sealed class KeyCacheEntry
        {
            public string Key;
            public string Value;
            public int Version;
        }

        private static readonly ConditionalWeakTable<TMP_Text, KeyCacheEntry> KeyCache = new();

        public static void SetKey(this TMP_Text text, string key, params object[] args)
        {
            if (!text) 
                return;
            
            var cache = KeyCache.GetOrCreateValue(text);
            if (cache.Key != key || cache.Version != LanguageController.Version)
            {
                cache.Key = key;
                cache.Version = LanguageController.Version;
                cache.Value = LanguageController.Get(key);
            }

            var v = cache.Value;
            if (string.IsNullOrEmpty(v))
            {
                text.text = $"<{key}>";
                return;
            }
            text.text = args is { Length: > 0 } ? string.Format(v, args) : v;
        }

        public static void UpdateValue(this TMP_Text text, params object[] args)
        {
            if (!text)
                return;
            if (!KeyCache.TryGetValue(text, out var cache) || string.IsNullOrEmpty(cache.Key))
                return;

            if (cache.Version != LanguageController.Version)
            {
                cache.Version = LanguageController.Version;
                cache.Value = LanguageController.Get(cache.Key);
            }

            var v = cache.Value;
            if (string.IsNullOrEmpty(v))
            {
                text.text = $"<{cache.Key}>";
                return;
            }

            text.text = args is { Length: > 0 } ? string.Format(v, args) : v;
        }

        public static void SetArrayKey(this TMP_Text text, string key, params object[] args)
        {
            if (text == null) return;
            var arr = LanguageController.GetArray(key);
            if (arr.Length == 0)
            {
                text.text = $"<{key}>";
                return;
            }
            var pick = arr[RandomIndex(arr.Length)];
            text.text = args is { Length: > 0 } ? string.Format(pick, args) : pick;
        }

        public static void SetArrayKey(this TMP_Text text, string key, int index, params object[] args)
        {
            if (text == null) return;
            var arr = LanguageController.GetArray(key);
            if (arr.Length == 0 || index < 0 || index >= arr.Length)
            {
                text.text = $"<{key}[{index}]>";
                return;
            }
            var pick = arr[index];
            text.text = args is { Length: > 0 } ? string.Format(pick, args) : pick;
        }

        private static int RandomIndex(int length)
        {
            return UnityEngine.Random.Range(0, length);
        }
    }
}
