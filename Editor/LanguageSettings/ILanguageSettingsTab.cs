namespace ENP.UnityExtensions.Editor
{
    internal interface ILanguageSettingsTab
    {
        string Title { get; }
        void OnEnable(LanguageSettingsWindow host);
        void OnDisable();
        void OnGUI();
    }
}
