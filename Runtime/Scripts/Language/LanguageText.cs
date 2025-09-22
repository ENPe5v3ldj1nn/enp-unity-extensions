using System;
using TMPro;
using UnityEngine;

namespace enp_unity_extensions.Scripts.Language
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TMP_Text))]
    public class LanguageText : MonoBehaviour
    {
        private TMP_Text _text;
        private string _key;
        private object[] _formatArgs;

        public void SetTmpText(TMP_Text target)
        {
            _text = target;
        }

        public void SetKey(string key)
        {
            _key = key;
            _formatArgs = null;
            Apply();
        }

        public void SetKeyWithParams(string key, params object[] args)
        {
            _key = key;
            _formatArgs = args;
            Apply();
        }

        public void Refresh()
        {
            Apply();
        }

        private void Apply()
        {
            if (string.IsNullOrEmpty(_key))
            {
                _text.text = "<missing:key>";
                return;
            }

            if (LanguageController.LanguageDictionary != null &&
                LanguageController.LanguageDictionary.TryGetValue(_key, out var value))
            {
                if (_formatArgs != null && _formatArgs.Length > 0)
                    _text.text = string.Format(value, _formatArgs);
                else
                    _text.text = value;
            }
            else
            {
                _text.text = $"<missing:{_key}>";
            }
        }

        private void OnEnable()
        {
            LanguageController.OnLanguageChanged += OnLanguageChanged;
            Apply();
        }

        private void OnDisable()
        {
            LanguageController.OnLanguageChanged -= OnLanguageChanged;
        }

        private void OnLanguageChanged(SystemLanguage _)
        {
            Apply();
        }
    }
}
