using System;
using UnityEngine;
using TMPro;

namespace enp_unity_extensions.Scripts.Language
{
    public class LanguageText : MonoBehaviour
    {
        [SerializeField] private TMP_Text _text;

        private string _key;
        private object[] _formatArgs;
        

        public void SetKeyWithParams(string key)
        {
            _key = key;
            _formatArgs = null;
            UpdateText();
        }

        public void SetKeyWithParams(string key, params object[] args)
        {
            _key = key;
            _formatArgs = args;
            UpdateText();
        }



        private void UpdateText()
        {
            if (string.IsNullOrEmpty(_key))
            {
                throw new Exception("Key is empty");
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

        private void OnEnable()
        {
            LanguageController.OnLanguageChanged += LanguageControllerOnLanguageChanged;
            UpdateText();
        }

        private void OnDisable()
        {
            LanguageController.OnLanguageChanged -= LanguageControllerOnLanguageChanged;
        }

        private void LanguageControllerOnLanguageChanged(Language language)
        {
            UpdateText();
        }
    }
}