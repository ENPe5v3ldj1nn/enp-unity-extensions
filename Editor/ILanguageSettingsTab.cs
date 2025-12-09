namespace enp_unity_extensions.Editor.Language
{
    internal interface ILanguageSettingsTab
    {
        string Title { get; }
        void OnEnable(LanguageSettingsWindow host);
        void OnDisable();
        void OnGUI();
    }
}
