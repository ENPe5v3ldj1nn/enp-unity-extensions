using enp_unity_extensions.Scripts.Language;
using UnityEngine;

namespace enp_unity_extensions.Scripts.UI.Button
{
    public class AnimatedButtonWithText : AnimatedButton
    {
        [SerializeField] private LanguageText _languageText;
        
        public void SetKey(string key)
        {
            _languageText.SetKey(key);
        }

        public void SetKeyWithParams(string key, params object[] args)
        {
            _languageText.SetKeyWithParams(key, args);
        }

        public void UpdateText()
        {
            _languageText.Refresh();
        }
    }
}