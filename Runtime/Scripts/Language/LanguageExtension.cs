using System.Runtime.CompilerServices;
using System.Text;
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
            public string Missing;
            public StringBuilder Sb;
        }

        private static readonly ConditionalWeakTable<TMP_Text, KeyCacheEntry> KeyCache = new();

        public static void SetKey(this TMP_Text text, string key)
        {
            SetKeyInternal(text, key, 0, null, null, null, null);
        }

        public static void SetKey(this TMP_Text text, string key, string arg0)
        {
            SetKeyInternal(text, key, 1, arg0, null, null, null);
        }

        public static void SetKey(this TMP_Text text, string key, string arg0, string arg1)
        {
            SetKeyInternal(text, key, 2, arg0, arg1, null, null);
        }

        public static void SetKey(this TMP_Text text, string key, string arg0, string arg1, string arg2)
        {
            SetKeyInternal(text, key, 3, arg0, arg1, arg2, null);
        }

        public static void SetKey(this TMP_Text text, string key, string arg0, string arg1, string arg2, string arg3)
        {
            SetKeyInternal(text, key, 4, arg0, arg1, arg2, arg3);
        }

        public static void SetArrayKey(this TMP_Text text, string key)
        {
            SetArrayKeyInternal(text, key, -1, 0, null, null, null, null);
        }

        public static void SetArrayKey(this TMP_Text text, string key, string arg0)
        {
            SetArrayKeyInternal(text, key, -1, 1, arg0, null, null, null);
        }

        public static void SetArrayKey(this TMP_Text text, string key, string arg0, string arg1)
        {
            SetArrayKeyInternal(text, key, -1, 2, arg0, arg1, null, null);
        }

        public static void SetArrayKey(this TMP_Text text, string key, string arg0, string arg1, string arg2)
        {
            SetArrayKeyInternal(text, key, -1, 3, arg0, arg1, arg2, null);
        }

        public static void SetArrayKey(this TMP_Text text, string key, string arg0, string arg1, string arg2, string arg3)
        {
            SetArrayKeyInternal(text, key, -1, 4, arg0, arg1, arg2, arg3);
        }

        public static void SetArrayKey(this TMP_Text text, string key, int index)
        {
            SetArrayKeyInternal(text, key, index, 0, null, null, null, null);
        }

        public static void SetArrayKey(this TMP_Text text, string key, int index, string arg0)
        {
            SetArrayKeyInternal(text, key, index, 1, arg0, null, null, null);
        }

        public static void SetArrayKey(this TMP_Text text, string key, int index, string arg0, string arg1)
        {
            SetArrayKeyInternal(text, key, index, 2, arg0, arg1, null, null);
        }

        public static void SetArrayKey(this TMP_Text text, string key, int index, string arg0, string arg1, string arg2)
        {
            SetArrayKeyInternal(text, key, index, 3, arg0, arg1, arg2, null);
        }

        public static void SetArrayKey(this TMP_Text text, string key, int index, string arg0, string arg1, string arg2, string arg3)
        {
            SetArrayKeyInternal(text, key, index, 4, arg0, arg1, arg2, arg3);
        }

        private static void SetKeyInternal(TMP_Text text, string key, int argCount, string arg0, string arg1, string arg2, string arg3)
        {
            var cache = KeyCache.GetOrCreateValue(text);
            if (cache.Key != key || cache.Version != LanguageController.Version)
            {
                cache.Key = key;
                cache.Version = LanguageController.Version;
                cache.Value = LanguageController.Get(key);
                cache.Missing = null;
            }

            var template = cache.Value;
            if (string.IsNullOrEmpty(template))
            {
                cache.Missing ??= "<" + (key ?? string.Empty) + ">";
                text.SetText(cache.Missing);
                return;
            }

            if (argCount == 0)
            {
                text.SetText(template);
                return;
            }

            var sb = cache.Sb;
            if (sb == null)
            {
                sb = new StringBuilder(template.Length + 16 * argCount);
                cache.Sb = sb;
            }
            else
            {
                sb.Clear();
            }

            AppendFormatted(sb, template, argCount, arg0, arg1, arg2, arg3);
            text.SetText(sb);
        }

        private static void SetArrayKeyInternal(TMP_Text text, string key, int index, int argCount, string arg0, string arg1, string arg2, string arg3)
        {
            var arr = LanguageController.GetArray(key);
            if (arr.Length == 0)
            {
                text.SetText("<" + (key ?? string.Empty) + ">");
                return;
            }

            string pick;
            if (index < 0)
            {
                pick = arr[RandomIndex(arr.Length)];
            }
            else
            {
                if ((uint)index >= (uint)arr.Length)
                {
                    text.SetText("<" + (key ?? string.Empty) + "[" + index + "]>");
                    return;
                }

                pick = arr[index];
            }

            if (argCount == 0)
            {
                text.SetText(pick);
                return;
            }

            var cache = KeyCache.GetOrCreateValue(text);
            var sb = cache.Sb;
            if (sb == null)
            {
                sb = new StringBuilder(pick.Length + 16 * argCount);
                cache.Sb = sb;
            }
            else
            {
                sb.Clear();
            }

            AppendFormatted(sb, pick, argCount, arg0, arg1, arg2, arg3);
            text.SetText(sb);
        }

        private static void AppendFormatted(StringBuilder sb, string template, int argCount, string arg0, string arg1, string arg2, string arg3)
        {
            for (var i = 0; i < template.Length; i++)
            {
                var c = template[i];

                if (c == '{')
                {
                    if (i + 1 < template.Length && template[i + 1] == '{')
                    {
                        sb.Append('{');
                        i++;
                        continue;
                    }

                    var j = i + 1;
                    if (j >= template.Length)
                    {
                        sb.Append('{');
                        continue;
                    }

                    var index = 0;
                    var hasDigit = false;
                    while (j < template.Length)
                    {
                        var d = template[j];
                        if ((uint)(d - '0') <= 9u)
                        {
                            hasDigit = true;
                            index = index * 10 + (d - '0');
                            j++;
                            continue;
                        }
                        break;
                    }

                    if (!hasDigit)
                    {
                        sb.Append('{');
                        continue;
                    }

                    if (j < template.Length && template[j] == ':')
                    {
                        j++;
                        while (j < template.Length && template[j] != '}')
                            j++;
                    }

                    if (j < template.Length && template[j] == '}')
                    {
                        AppendArg(sb, index, argCount, arg0, arg1, arg2, arg3);
                        i = j;
                        continue;
                    }

                    sb.Append('{');
                    continue;
                }

                if (c == '}')
                {
                    if (i + 1 < template.Length && template[i + 1] == '}')
                    {
                        sb.Append('}');
                        i++;
                        continue;
                    }

                    sb.Append('}');
                    continue;
                }

                sb.Append(c);
            }
        }

        private static void AppendArg(StringBuilder sb, int index, int argCount, string arg0, string arg1, string arg2, string arg3)
        {
            if ((uint)index >= (uint)argCount)
                return;

            switch (index)
            {
                case 0:
                    if (!string.IsNullOrEmpty(arg0)) sb.Append(arg0);
                    return;
                case 1:
                    if (!string.IsNullOrEmpty(arg1)) sb.Append(arg1);
                    return;
                case 2:
                    if (!string.IsNullOrEmpty(arg2)) sb.Append(arg2);
                    return;
                case 3:
                    if (!string.IsNullOrEmpty(arg3)) sb.Append(arg3);
                    return;
            }
        }

        private static int RandomIndex(int length)
        {
            return UnityEngine.Random.Range(0, length);
        }
    }
}
