using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

namespace enp_unity_extensions.Scripts.Language
{
    public enum LanguageTextSource
    {
        Auto,
        String,
        ArrayRandom,
        ArrayNoRepeat,
        ArrayByIndex
    }

    [RequireComponent(typeof(TMP_Text))]
    public class LanguageText : MonoBehaviour
    {
        [SerializeField] private LanguageTextSource _source = LanguageTextSource.Auto;
        [SerializeField] private string _key;
        [SerializeField] private int _arrayIndex;
        private bool _initialized;
        [ThreadStatic] private static StringBuilder _formatBuilder;
        private TMP_Text _text;
        private Queue<string> _bag;

        private void Awake()
        {
            _text = GetComponent<TMP_Text>();
            Initialize();
        }

        private void OnDestroy()
        {
            if (_initialized)
            {
                LanguageController.LanguageChanged -= LanguageChanged;
            }
        }

        private void Initialize()
        {
            if (_initialized) return;
            if (_text == null)
            {
                _text = GetComponent<TMP_Text>();
            }
            LanguageController.LanguageChanged += LanguageChanged;
            _initialized = true;
        }

        public void SetKey(string key)
        {
            _key = key;
            _source = LanguageTextSource.String;
        }

        public void SetArrayKey(string key, bool noRepeat = false)
        {
            _key = key;
            _source = noRepeat ? LanguageTextSource.ArrayNoRepeat : LanguageTextSource.ArrayRandom;
        }

        public void SetArrayKey(string key, int index)
        {
            _key = key;
            _arrayIndex = index;
            _source = LanguageTextSource.ArrayByIndex;
        }

        public void SetAuto(string key)
        {
            _key = key;
            _source = LanguageTextSource.Auto;
        }

        public void SetKeyAndRefresh(string key, params object[] formatArgs)
        {
            SetKey(key);
            Refresh(formatArgs);
        }

        public void SetArrayKeyAndRefresh(string key, bool noRepeat = false, params object[] formatArgs)
        {
            SetArrayKey(key, noRepeat);
            Refresh(formatArgs);
        }

        public void SetArrayKeyAndRefresh(string key, int index, params object[] formatArgs)
        {
            SetArrayKey(key, index);
            Refresh(formatArgs);
        }

        public void SetAutoAndRefresh(string key, params object[] formatArgs)
        {
            SetAuto(key);
            Refresh(formatArgs);
        }

        public void Refresh(params object[] formatArgs)
        {
            if (!_initialized) Initialize();
            if (_text == null || string.IsNullOrEmpty(_key)) return;

            if (_source == LanguageTextSource.String)
            {
                var v = LanguageController.Get(_key);
                _text.text = string.IsNullOrEmpty(v) ? $"<{_key}>" : FormatValue(v, formatArgs);
                return;
            }

            if (_source == LanguageTextSource.ArrayByIndex)
            {
                var arr = LanguageController.GetArray(_key);
                if (arr.Length == 0 || _arrayIndex < 0 || _arrayIndex >= arr.Length)
                {
                    _text.text = $"<{_key}[{_arrayIndex}]>";
                    return;
                }
                var pick = arr[_arrayIndex];
                _text.text = FormatValue(pick, formatArgs);
                return;
            }

            if (_source == LanguageTextSource.ArrayRandom || _source == LanguageTextSource.ArrayNoRepeat || _source == LanguageTextSource.Auto)
            {
                var arr = LanguageController.GetArray(_key);
                if (_source == LanguageTextSource.Auto && arr.Length == 0)
                {
                    var v = LanguageController.Get(_key);
                    _text.text = string.IsNullOrEmpty(v) ? $"<{_key}>" : FormatValue(v, formatArgs);
                    return;
                }

                if (arr.Length == 0)
                {
                    _text.text = $"<{_key}>";
                    return;
                }

                string pick;
                if (_source == LanguageTextSource.ArrayNoRepeat)
                {
                    if (_bag == null || _bag.Count == 0)
                    {
                        var shuffled = new List<string>(arr);
                        for (int i = 0; i < shuffled.Count; i++)
                        {
                            var j = Random.Range(i, shuffled.Count);
                            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
                        }
                        _bag = new Queue<string>(shuffled);
                    }
                    pick = _bag.Dequeue();
                }
                else
                {
                    pick = arr[Random.Range(0, arr.Length)];
                }

                _text.text = FormatValue(pick, formatArgs);
            }
        }

        private void LanguageChanged(SystemLanguage _)
        {
            _bag = null;
            Refresh();
        }

        private static string FormatValue(string value, object[] formatArgs)
        {
            if (string.IsNullOrEmpty(value)) return value;
            if (formatArgs == null || formatArgs.Length == 0) return value;
            var sb = _formatBuilder ??= new StringBuilder(value.Length + 16);
            sb.Clear();
            sb.AppendFormat(CultureInfo.InvariantCulture, value, formatArgs);
            return sb.ToString();
        }
    }
}
