using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using enp_unity_extensions.Editor.Language;
using enp_unity_extensions.Scripts.Language;
using UnityEditor;
using UnityEngine;

namespace enp_unity_extensions.Editor.LanguageSettings
{
    internal class LanguageSettingsFoldersTab : ILanguageSettingsTab
    {
        public string Title => "Translation Folders";

        private LanguageSettingsWindow _host;
        private SystemLanguage _newFolderLanguage = SystemLanguage.English;
        private SystemLanguage _selectedLanguageForSet = SystemLanguage.English;
        private Vector2 _foldersScroll;
        private readonly List<string> _folders = new List<string>();

        public void OnEnable(LanguageSettingsWindow host)
        {
            _host = host;
            RefreshFolders();
            SyncSelectedLanguage(LanguageController.CurrentLanguage);
        }

        public void OnDisable()
        {
        }

        public void OnGUI()
        {
            EditorGUILayout.LabelField("Base translations path (inside a Resources folder)", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            var absolutePath = LanguageSettingsPathUtility.Sanitize(EditorGUILayout.TextField("Absolute path", _host.GetAbsoluteResourcesPath()));
            if (EditorGUI.EndChangeCheck())
            {
                if (TrySetAbsolutePath(absolutePath))
                {
                    RefreshFolders();
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Refresh list", GUILayout.Width(140)))
                {
                    RefreshFolders();
                }
                GUILayout.FlexibleSpace();
            }

            GUILayout.Space(6f);
            EditorGUILayout.LabelField("Existing folders", EditorStyles.boldLabel);

            _foldersScroll = EditorGUILayout.BeginScrollView(_foldersScroll, GUILayout.Height(160));
            var foldersSnapshot = _folders.ToArray();
            if (foldersSnapshot.Length == 0)
            {
                EditorGUILayout.LabelField("No folders found.");
            }
            else
            {
                foreach (var folder in foldersSnapshot)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField(folder, GUILayout.MinWidth(100));
                        if (GUILayout.Button("Delete", GUILayout.Width(80)))
                        {
                            TryDeleteFolder(folder);
                        }
                    }
                }
            }
            EditorGUILayout.EndScrollView();

            GUILayout.Space(8f);
            EditorGUILayout.LabelField("Create folder", EditorStyles.boldLabel);
            var availableLanguages = GetAvailableLanguages().ToList();
            if (availableLanguages.Count == 0)
            {
                EditorGUILayout.HelpBox("All language folders already exist.", MessageType.Info);
            }
            else
            {
                if (!availableLanguages.Contains(_newFolderLanguage))
                {
                    _newFolderLanguage = availableLanguages[0];
                }

                var options = availableLanguages.Select(l => l.ToString()).ToArray();
                var currentIndex = Mathf.Max(0, availableLanguages.IndexOf(_newFolderLanguage));
                var selectedIndex = EditorGUILayout.Popup("Language", currentIndex, options);
                _newFolderLanguage = availableLanguages[Mathf.Clamp(selectedIndex, 0, availableLanguages.Count - 1)];

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Create", GUILayout.Width(100)))
                    {
                        CreateFolder(_newFolderLanguage);
                    }
                    GUILayout.FlexibleSpace();
                }
            }

            GUILayout.Space(8f);
            EditorGUILayout.LabelField("Set runtime language", EditorStyles.boldLabel);
            var switchableLanguages = GetLanguagesWithFolders().ToList();
            if (switchableLanguages.Count == 0)
            {
                EditorGUILayout.HelpBox("No language folders detected to set. Create one first.", MessageType.Info);
            }
            else
            {
                if (!switchableLanguages.Contains(_selectedLanguageForSet))
                {
                    _selectedLanguageForSet = switchableLanguages[0];
                }

                var switchableOptions = switchableLanguages.Select(l => l.ToString()).ToArray();
                var switchableIndex = Mathf.Max(0, switchableLanguages.IndexOf(_selectedLanguageForSet));
                EditorGUI.BeginChangeCheck();
                var switchableSelected = EditorGUILayout.Popup("Language", switchableIndex, switchableOptions);
                var newLanguage = switchableLanguages[Mathf.Clamp(switchableSelected, 0, switchableLanguages.Count - 1)];
                if (EditorGUI.EndChangeCheck())
                {
                    _selectedLanguageForSet = newLanguage;
                    ApplyLanguageSelection(_selectedLanguageForSet);
                }
                else
                {
                    _selectedLanguageForSet = newLanguage;
                }
            }
        }

        public void SyncSelectedLanguage(SystemLanguage language)
        {
            if (_folders.Count == 0)
            {
                RefreshFolders();
            }

            var available = GetLanguagesWithFolders().ToList();
            if (available.Count == 0) return;

            _selectedLanguageForSet = available.Contains(language) ? language : available[0];
        }

        private void RefreshFolders()
        {
            _folders.Clear();
            var basePath = _host.GetAbsoluteResourcesPath();
            if (!AssetDatabase.IsValidFolder(basePath))
            {
                _host.SetStatus($"Path not found: {basePath}", MessageType.Info);
                return;
            }

            foreach (var sub in AssetDatabase.GetSubFolders(basePath))
            {
                _folders.Add(Path.GetFileName(sub));
            }
            _folders.Sort(StringComparer.OrdinalIgnoreCase);
            _host.SetStatus($"Found {_folders.Count} folders in {basePath}", MessageType.Info);
        }

        private void CreateFolder(SystemLanguage language)
        {
            var folderName = GetFolderNameFor(language);
            if (string.IsNullOrEmpty(folderName))
            {
                _host.SetStatus($"Unsupported language value: {language}", MessageType.Error);
                return;
            }

            var basePath = _host.GetAbsoluteResourcesPath();
            if (!EnsureBasePathExists(basePath))
            {
                _host.SetStatus($"Could not create base path: {basePath}", MessageType.Error);
                return;
            }

            var target = $"{basePath}/{folderName}";
            if (AssetDatabase.IsValidFolder(target))
            {
                _host.SetStatus($"Folder '{folderName}' already exists at {target}.", MessageType.Warning);
                return;
            }

            AssetDatabase.CreateFolder(basePath, folderName);
            AssetDatabase.Refresh();
            _host.SetStatus($"Created folder: {target}", MessageType.Info);
            RefreshFolders();
        }

        private void TryDeleteFolder(string folderName)
        {
            var basePath = _host.GetAbsoluteResourcesPath();
            var target = $"{basePath}/{folderName}";
            if (!AssetDatabase.IsValidFolder(target))
            {
                _host.SetStatus($"Folder '{folderName}' not found in {basePath}.", MessageType.Warning);
                RefreshFolders();
                return;
            }

            var confirm = EditorUtility.DisplayDialog(
                "Delete translation folder?",
                $"Delete '{folderName}' and all of its contents?\nPath: {target}",
                "Delete",
                "Cancel");

            if (!confirm) return;

            if (AssetDatabase.DeleteAsset(target))
            {
                AssetDatabase.Refresh();
                _host.SetStatus($"Folder '{folderName}' deleted.", MessageType.Info);
            }
            else
            {
                _host.SetStatus($"Failed to delete '{folderName}'.", MessageType.Error);
            }

            RefreshFolders();
        }

        private bool EnsureBasePathExists(string basePath)
        {
            if (AssetDatabase.IsValidFolder(basePath)) return true;

            var parts = basePath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return false;

            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current = next;
            }

            return AssetDatabase.IsValidFolder(basePath);
        }

        private bool TrySetAbsolutePath(string absolutePath)
        {
            if (string.IsNullOrWhiteSpace(absolutePath))
            {
                _host.SetResourcesPath(string.Empty);
                return true;
            }

            var assetPath = LanguageSettingsPathUtility.ToAssetPath(absolutePath);
            var isAssetPath = assetPath.StartsWith("Assets", StringComparison.OrdinalIgnoreCase);
            var insideResources = LanguageSettingsPathUtility.IsAssetPathInsideResources(assetPath);
            if (isAssetPath && !insideResources)
            {
                _host.SetStatus("Path must be inside a Resources folder (e.g. Assets/.../Resources/...)", MessageType.Error);
                return false;
            }

            _host.SetResourcesPath(assetPath);
            return true;
        }

        private IEnumerable<SystemLanguage> GetAvailableLanguages()
        {
            var existing = new HashSet<string>(_folders, StringComparer.OrdinalIgnoreCase);
            foreach (SystemLanguage language in Enum.GetValues(typeof(SystemLanguage)))
            {
                var folder = GetFolderNameFor(language);
                if (string.IsNullOrEmpty(folder)) continue;
                if (existing.Contains(folder)) continue;
                yield return language;
            }
        }

        private IEnumerable<SystemLanguage> GetLanguagesWithFolders()
        {
            var seen = new HashSet<SystemLanguage>();
            foreach (var folder in _folders)
            {
                if (!TryGetLanguageForFolder(folder, out var language)) continue;
                if (seen.Add(language))
                {
                    yield return language;
                }
            }
        }

        private static bool TryGetLanguageForFolder(string folderName, out SystemLanguage language)
        {
            foreach (SystemLanguage lang in Enum.GetValues(typeof(SystemLanguage)))
            {
                if (string.Equals(GetFolderNameFor(lang), folderName, StringComparison.OrdinalIgnoreCase))
                {
                    language = lang;
                    return true;
                }
            }

            language = default;
            return false;
        }

        private static string GetFolderNameFor(SystemLanguage language)
        {
            switch (language)
            {
                case SystemLanguage.Afrikaans: return "afrikaans";
                case SystemLanguage.Arabic: return "arabic";
                case SystemLanguage.Basque: return "basque";
                case SystemLanguage.Belarusian: return "belarusian";
                case SystemLanguage.Bulgarian: return "bulgarian";
                case SystemLanguage.Catalan: return "catalan";
                case SystemLanguage.Chinese: return "chinese";
                case SystemLanguage.ChineseSimplified: return "chinese_simplified";
                case SystemLanguage.ChineseTraditional: return "chinese_traditional";
                case SystemLanguage.Czech: return "czech";
                case SystemLanguage.Danish: return "danish";
                case SystemLanguage.Dutch: return "dutch";
                case SystemLanguage.English: return "english";
                case SystemLanguage.Estonian: return "estonian";
                case SystemLanguage.Faroese: return "faroese";
                case SystemLanguage.Finnish: return "finnish";
                case SystemLanguage.French: return "french";
                case SystemLanguage.German: return "german";
                case SystemLanguage.Greek: return "greek";
                case SystemLanguage.Hebrew: return "hebrew";
                case SystemLanguage.Hungarian: return "hungarian";
                case SystemLanguage.Icelandic: return "icelandic";
                case SystemLanguage.Indonesian: return "indonesian";
                case SystemLanguage.Italian: return "italian";
                case SystemLanguage.Japanese: return "japanese";
                case SystemLanguage.Korean: return "korean";
                case SystemLanguage.Latvian: return "latvian";
                case SystemLanguage.Lithuanian: return "lithuanian";
                case SystemLanguage.Norwegian: return "norwegian";
                case SystemLanguage.Polish: return "polish";
                case SystemLanguage.Portuguese: return "portuguese";
                case SystemLanguage.Romanian: return "romanian";
                case SystemLanguage.Russian: return "russian";
                case SystemLanguage.SerboCroatian: return "serbo_croatian";
                case SystemLanguage.Slovak: return "slovak";
                case SystemLanguage.Slovenian: return "slovenian";
                case SystemLanguage.Spanish: return "spanish";
                case SystemLanguage.Swedish: return "swedish";
                case SystemLanguage.Thai: return "thai";
                case SystemLanguage.Turkish: return "turkish";
                case SystemLanguage.Ukrainian: return "ukrainian";
                case SystemLanguage.Vietnamese: return "vietnamese";
                case SystemLanguage.Hindi: return "hindi";
                default: return string.Empty;
            }
        }

        private void ApplyLanguageSelection(SystemLanguage language)
        {
            try
            {
                var relativePath = _host.GetResourcesRelativePath();
                LanguageController.SetResourcesPath(relativePath);
                LanguageController.SetLanguage(language);
                _host.SetStatus($"Language set to {language}.", MessageType.Info);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Language] Failed to set language: {ex.Message}");
                _host.SetStatus($"Failed to set language: {ex.Message}", MessageType.Error);
            }
        }
    }
}
