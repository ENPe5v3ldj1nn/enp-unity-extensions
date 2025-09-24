using System.Collections.Generic;
using TMPro;
using UnityEngine;

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
        private TMP_Text _text;
        private Queue<string> _bag;

        private void Awake()
        {
            _text = GetComponent<TMP_Text>();
        }

        private void OnEnable()
        {
            LanguageController.OnLanguageChanged += OnLanguageChanged;
            Refresh();
        }

        private void OnDisable()
        {
            LanguageController.OnLanguageChanged -= OnLanguageChanged;
        }

        public void SetKey(string key)
        {
            _key = key;
            _source = LanguageTextSource.String;
            Refresh();
        }

        public void SetArrayKey(string key, bool noRepeat = false)
        {
            _key = key;
            _source = noRepeat ? LanguageTextSource.ArrayNoRepeat : LanguageTextSource.ArrayRandom;
            Refresh();
        }

        public void SetArrayKey(string key, int index)
        {
            _key = key;
            _arrayIndex = index;
            _source = LanguageTextSource.ArrayByIndex;
            Refresh();
        }

        public void SetAuto(string key)
        {
            _key = key;
            _source = LanguageTextSource.Auto;
            Refresh();
        }

        public void Refresh(params object[] formatArgs)
        {
            if (_text == null || string.IsNullOrEmpty(_key)) return;

            if (_source == LanguageTextSource.String)
            {
                var v = LanguageController.Get(_key);
                _text.text = string.IsNullOrEmpty(v) ? $"<{_key}>" : (formatArgs is { Length: > 0 } ? string.Format(v, formatArgs) : v);
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
                _text.text = formatArgs is { Length: > 0 } ? string.Format(pick, formatArgs) : pick;
                return;
            }

            if (_source == LanguageTextSource.ArrayRandom || _source == LanguageTextSource.ArrayNoRepeat || _source == LanguageTextSource.Auto)
            {
                var arr = LanguageController.GetArray(_key);
                if (_source == LanguageTextSource.Auto && arr.Length == 0)
                {
                    var v = LanguageController.Get(_key);
                    _text.text = string.IsNullOrEmpty(v) ? $"<{_key}>" : (formatArgs is { Length: > 0 } ? string.Format(v, formatArgs) : v);
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
                            var tmp = shuffled[i];
                            shuffled[i] = shuffled[j];
                            shuffled[j] = tmp;
                        }
                        _bag = new Queue<string>(shuffled);
                    }
                    pick = _bag.Dequeue();
                }
                else
                {
                    pick = arr[Random.Range(0, arr.Length)];
                }

                _text.text = formatArgs is { Length: > 0 } ? string.Format(pick, formatArgs) : pick;
            }
        }

        private void OnLanguageChanged(SystemLanguage _)
        {
            _bag = null;
            Refresh();
        }
    }
}
