using System;
using UnityEngine;
using TMPro;

namespace enp_unity_extensions.Scripts.Language
{
    public class LanguageText : MonoBehaviour
    {
        public TMP_Text Text => _text;
        [SerializeField] private TMP_Text _text;

        private string _key;
        private object[] _formatArgs;
        
        public void SetKey(string key)
        {
            _key = key;
            _formatArgs = null;
            UpdateText();
        }

        public void SetKeyWithParams(string key, params object[] args)
        {
            _key = key;
            _formatArgs = args;
            UpdateText(_formatArgs);
        }

        public void UpdateText()
        {
            if (string.IsNullOrEmpty(_key))
            {
                _text.text = $"<missing:key>";
                return;
            }

            if (LanguageController.LanguageDictionary.TryGetValue(_key, out string value))
            {
                _text.text = _formatArgs != null && _formatArgs.Length > 0
                    ? string.Format(value, _formatArgs)
                    : value;
            }
            else
            {
                _text.text = $"<missing:{_key}>";
            }
        }
        
        public void UpdateText(params object[] args)
        {
            if (string.IsNullOrEmpty(_key))
            {
                _text.text = $"<missing:key>";
                return;
            }

            _formatArgs = args;
            if (LanguageController.LanguageDictionary.TryGetValue(_key, out string value))
            {
                _text.text = string.Format(value, args);
            }
            else
            {
                _text.text = $"<missing:{_key}>";
            }
        }

        private void OnEnable()
        {
            LanguageController.OnLanguageChanged += LanguageControllerOnLanguageChanged;
            if (_formatArgs != null && _formatArgs.Length > 0)
                UpdateText(_formatArgs);
            else
                UpdateText();
        }

        private void OnDisable()
        {
            LanguageController.OnLanguageChanged -= LanguageControllerOnLanguageChanged;
        }

        private void LanguageControllerOnLanguageChanged(SystemLanguage language)
        {
            if (_formatArgs != null && _formatArgs.Length > 0)
                UpdateText(_formatArgs);
            else
                UpdateText();
        }
    }
}