using UnityEditor;

namespace enp_unity_extensions.Editor.Language
{
    internal class LanguageSettingsTranslationTab : ILanguageSettingsTab
    {
        public string Title => "Translation Editor";

        private LanguageSettingsWindow _host;
        private readonly TranslationEditorView _view = new TranslationEditorView();

        public void OnEnable(LanguageSettingsWindow host)
        {
            _host = host;
            _view.OnEnable(_host);
            _view.SyncResourcesPath(_host.ResourcesPath);
        }

        public void OnDisable()
        {
            _view.OnDisable();
        }

        public void OnGUI()
        {
            _view.SyncResourcesPath(_host.ResourcesPath);
            _view.OnGUI();
            _host.SetResourcesPath(_view.ResourcesPath);
        }
    }
}
