using enp_unity_extensions.Editor.Language;

namespace enp_unity_extensions.Editor.LanguageSettings
{
    internal interface ILanguageSettingsTab
    {
        string Title { get; }
        void OnEnable(LanguageSettingsWindow host);
        void OnDisable();
        void OnGUI();
    }
}
