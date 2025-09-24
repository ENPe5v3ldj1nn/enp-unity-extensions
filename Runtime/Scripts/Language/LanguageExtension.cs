using TMPro;
using UnityEngine;

namespace enp_unity_extensions.Scripts.Language
{
    public static class LanguageExtension
    {
        public static void SetKey(this TMP_Text text, string key)
        {
            if (text == null) return;
            var v = LanguageController.Get(key);
            text.text = string.IsNullOrEmpty(v) ? $"<{key}>" : v;
        }

        public static void SetKeyWithParams(this TMP_Text text, string key, params object[] args)
        {
            if (text == null) return;
            var v = LanguageController.Get(key);
            if (string.IsNullOrEmpty(v))
            {
                text.text = $"<{key}>";
                return;
            }
            text.text = args is { Length: > 0 } ? string.Format(v, args) : v;
        }

        public static void SetArrayKey(this TMP_Text text, string key)
        {
            if (text == null) return;
            var arr = LanguageController.GetArray(key);
            if (arr.Length == 0)
            {
                text.text = $"<{key}>";
                return;
            }
            text.text = arr[RandomIndex(arr.Length)];
        }

        public static void SetArrayKey(this TMP_Text text, string key, int index)
        {
            if (text == null) return;
            var arr = LanguageController.GetArray(key);
            if (arr.Length == 0 || index < 0 || index >= arr.Length)
            {
                text.text = $"<{key}[{index}]>";
                return;
            }
            text.text = arr[index];
        }

        public static void SetArrayKeyWithParams(this TMP_Text text, string key, params object[] args)
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

        public static void SetArrayKeyWithParams(this TMP_Text text, string key, int index, params object[] args)
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
