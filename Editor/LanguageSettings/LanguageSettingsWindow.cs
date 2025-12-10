using System;
using System.Collections.Generic;
using enp_unity_extensions.Scripts.Language;
using UnityEditor;
using UnityEngine;

namespace enp_unity_extensions.Editor.LanguageSettings
{
    public class LanguageSettingsWindow : EditorWindow
    {
        private const string WindowTitle = "Language Settings";
        internal const string PrefsBasePathKey = "ENP.LanguageSettings.ResourcesPath";
        internal const string DefaultResourcesPath = "Languages";

        private readonly List<ILanguageSettingsTab> _tabs = new List<ILanguageSettingsTab>();
        private readonly LanguageSettingsFoldersTab _foldersTab = new LanguageSettingsFoldersTab();
        private readonly LanguageSettingsKeysAuditTab _keysAuditTab = new LanguageSettingsKeysAuditTab();
        private readonly LanguageSettingsTranslationTab _translationTab = new LanguageSettingsTranslationTab();
        private int _tabIndex;
        internal string ResourcesPath { get; private set; }
        private string _statusMessage = string.Empty;
        private MessageType _statusType = MessageType.Info;

        [MenuItem("Tools/Language Settings")]
        public static void ShowWindow()
        {
            var window = GetWindow<LanguageSettingsWindow>(false, WindowTitle, true);
            window.minSize = new Vector2(420f, 320f);
            window.Show();
        }

        private void OnEnable()
        {
            ResourcesPath = EditorPrefs.GetString(PrefsBasePathKey, DefaultResourcesPath);

            _tabs.Clear();
            _tabs.Add(_foldersTab);
            _tabs.Add(_keysAuditTab);
            _tabs.Add(_translationTab);

            foreach (var tab in _tabs)
            {
                tab.OnEnable(this);
            }

            LanguageController.LanguageChanged += HandleLanguageChanged;
        }

        private void OnDisable()
        {
            foreach (var tab in _tabs)
            {
                tab.OnDisable();
            }

            LanguageController.LanguageChanged -= HandleLanguageChanged;
        }

        private void OnGUI()
        {
            if (_tabs.Count == 0)
            {
                EditorGUILayout.HelpBox("No tabs registered.", MessageType.Info);
                return;
            }

            var titles = _tabs.ConvertAll(t => t.Title).ToArray();
            _tabIndex = GUILayout.Toolbar(_tabIndex, titles);
            GUILayout.Space(4f);

            _tabs[_tabIndex].OnGUI();

            if (!string.IsNullOrEmpty(_statusMessage))
            {
                EditorGUILayout.HelpBox(_statusMessage, _statusType);
            }
        }

        private void HandleLanguageChanged(SystemLanguage language)
        {
            _foldersTab.SyncSelectedLanguage(language);
            Repaint();
        }

        internal string GetAbsoluteResourcesPath()
        {
            var relative = string.IsNullOrWhiteSpace(ResourcesPath) ? string.Empty : ResourcesPath.Trim().Trim('/', '\\');
            return string.IsNullOrEmpty(relative) ? "Assets/Resources" : $"Assets/Resources/{relative}";
        }

        internal void SetResourcesPath(string path)
        {
            var trimmed = string.IsNullOrWhiteSpace(path) ? string.Empty : path.Trim().Trim('/', '\\');
            if (string.Equals(trimmed, ResourcesPath, StringComparison.Ordinal)) return;

            ResourcesPath = trimmed;
            EditorPrefs.SetString(PrefsBasePathKey, ResourcesPath);
            Repaint();
        }

        internal void SetStatus(string message, MessageType type)
        {
            _statusMessage = message;
            _statusType = type;
            Repaint();
        }
    }
}
