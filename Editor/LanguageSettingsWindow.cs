using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace enp_unity_extensions.Editor.Language
{
    public class LanguageSettingsWindow : EditorWindow
    {
        private enum Tab
        {
            Folders,
            KeysAudit
        }

        private const string WindowTitle = "Language Settings";
        private const string PrefsBasePathKey = "ENP.LanguageSettings.ResourcesPath";
        private const string DefaultResourcesPath = "Languages";

        private Tab _tab;
        private string _resourcesPath;
        private SystemLanguage _newFolderLanguage = SystemLanguage.English;
        private Vector2 _foldersScroll;
        private readonly List<string> _folders = new List<string>();
        private readonly Dictionary<string, List<string>> _missingByKey = new Dictionary<string, List<string>>();
        private readonly List<string> _auditLanguages = new List<string>();
        private int _auditUsedKeys;
        private int _auditLanguagesProcessed;
        private string _statusMessage = string.Empty;
        private MessageType _statusType = MessageType.Info;

        [MenuItem("Tools/Language Settings")]
        public static void ShowWindow()
        {
            var window = GetWindow<LanguageSettingsWindow>(false, WindowTitle, true);
            window.minSize = new Vector2(360f, 280f);
            window.Show();
        }

        private void OnEnable()
        {
            _resourcesPath = EditorPrefs.GetString(PrefsBasePathKey, DefaultResourcesPath);
            RefreshFolders();
        }

        private void OnGUI()
        {
            _tab = (Tab)GUILayout.Toolbar((int)_tab, new[] { "Translation Folders", "Audit Folders" });
            GUILayout.Space(4f);

            switch (_tab)
            {
                case Tab.Folders:
                    DrawFoldersTab();
                    break;
                case Tab.KeysAudit:
                    DrawKeysAuditTab();
                    break;
            }

            if (!string.IsNullOrEmpty(_statusMessage))
            {
                EditorGUILayout.HelpBox(_statusMessage, _statusType);
            }
        }

        private void DrawFoldersTab()
        {
            EditorGUILayout.LabelField("Base translations path (relative to Assets/Resources)", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            var trimmed = EditorGUILayout.TextField("Resources Path", _resourcesPath).Trim().Trim('/', '\\');
            if (EditorGUI.EndChangeCheck())
            {
                _resourcesPath = trimmed;
                EditorPrefs.SetString(PrefsBasePathKey, _resourcesPath);
                RefreshFolders();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel("Absolute path");
                EditorGUILayout.SelectableLabel(GetAbsoluteResourcesPath(), EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
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
                return;
            }

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

        private void DrawKeysAuditTab()
        {
            EditorGUILayout.LabelField("Audit keys used in code vs translations", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Base translations path", GetAbsoluteResourcesPath());
            GUILayout.Space(4f);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Scan code and translations", GUILayout.Width(220)))
                {
                    RunAudit();
                }
                GUILayout.FlexibleSpace();
            }

            GUILayout.Space(8f);
            if (_auditLanguagesProcessed > 0)
            {
                EditorGUILayout.LabelField($"Languages checked: {_auditLanguagesProcessed}");
                EditorGUILayout.LabelField($"Keys used in code: {_auditUsedKeys}");
            }

            GUILayout.Space(6f);
            if (_missingByKey.Count == 0)
            {
                EditorGUILayout.HelpBox("No missing keys detected.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField("Missing keys by language:", EditorStyles.boldLabel);
            _foldersScroll = EditorGUILayout.BeginScrollView(_foldersScroll, GUILayout.Height(200));
            foreach (var kvp in _missingByKey.OrderBy(k => k.Key))
            {
                var languages = string.Join(", ", kvp.Value);
                EditorGUILayout.LabelField(kvp.Key, EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField($"Missing in: {languages}");
                EditorGUI.indentLevel--;
                GUILayout.Space(4f);
            }
            EditorGUILayout.EndScrollView();
        }

        private void RefreshFolders()
        {
            _folders.Clear();
            var basePath = GetAbsoluteResourcesPath();
            if (!AssetDatabase.IsValidFolder(basePath))
            {
                SetStatus($"Path not found: {basePath}", MessageType.Info);
                return;
            }

            foreach (var sub in AssetDatabase.GetSubFolders(basePath))
            {
                _folders.Add(Path.GetFileName(sub));
            }
            _folders.Sort(StringComparer.OrdinalIgnoreCase);
            SetStatus($"Found {_folders.Count} folders in {basePath}", MessageType.Info);
        }

        private void CreateFolder(SystemLanguage language)
        {
            var folderName = GetFolderNameFor(language);
            if (string.IsNullOrEmpty(folderName))
            {
                SetStatus($"Unsupported language value: {language}", MessageType.Error);
                return;
            }

            var basePath = GetAbsoluteResourcesPath();
            if (!EnsureBasePathExists(basePath))
            {
                SetStatus($"Could not create base path: {basePath}", MessageType.Error);
                return;
            }

            var target = $"{basePath}/{folderName}";
            if (AssetDatabase.IsValidFolder(target))
            {
                SetStatus($"Folder '{folderName}' already exists at {target}.", MessageType.Warning);
                return;
            }

            AssetDatabase.CreateFolder(basePath, folderName);
            AssetDatabase.Refresh();
            SetStatus($"Created folder: {target}", MessageType.Info);
            RefreshFolders();
        }

        private void TryDeleteFolder(string folderName)
        {
            var basePath = GetAbsoluteResourcesPath();
            var target = $"{basePath}/{folderName}";
            if (!AssetDatabase.IsValidFolder(target))
            {
                SetStatus($"Folder '{folderName}' not found in {basePath}.", MessageType.Warning);
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
                SetStatus($"Folder '{folderName}' deleted.", MessageType.Info);
            }
            else
            {
                SetStatus($"Failed to delete '{folderName}'.", MessageType.Error);
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

        private string GetAbsoluteResourcesPath()
        {
            var relative = string.IsNullOrWhiteSpace(_resourcesPath) ? string.Empty : _resourcesPath.Trim().Trim('/', '\\');
            return string.IsNullOrEmpty(relative) ? "Assets/Resources" : $"Assets/Resources/{relative}";
        }

        private void RunAudit()
        {
            _missingByKey.Clear();
            _auditLanguages.Clear();
            _auditUsedKeys = 0;
            _auditLanguagesProcessed = 0;

            var usedKeys = CollectKeysFromCode();
            _auditUsedKeys = usedKeys.Count;

            var basePath = GetAbsoluteResourcesPath();
            if (!AssetDatabase.IsValidFolder(basePath))
            {
                SetStatus($"Translations path not found: {basePath}", MessageType.Warning);
                return;
            }

            var languageFolders = AssetDatabase.GetSubFolders(basePath);
            foreach (var folder in languageFolders)
            {
                var langName = Path.GetFileName(folder);
                if (string.IsNullOrEmpty(langName)) continue;
                _auditLanguages.Add(langName);
                var langKeys = CollectKeysFromLanguageFolder(folder);
                foreach (var key in usedKeys)
                {
                    if (langKeys.Contains(key)) continue;
                    if (!_missingByKey.TryGetValue(key, out var list))
                    {
                        list = new List<string>();
                        _missingByKey[key] = list;
                    }
                    list.Add(langName);
                }
                _auditLanguagesProcessed++;
            }

            if (_missingByKey.Count == 0)
            {
                SetStatus("Audit complete: all used keys found in translations.", MessageType.Info);
            }
            else
            {
                SetStatus($"Audit complete: {_missingByKey.Count} missing key(s) detected.", MessageType.Warning);
            }
        }

        private static HashSet<string> CollectKeysFromCode()
        {
            var result = new HashSet<string>(StringComparer.Ordinal);
            var regex = new Regex(@"\b(?:LanguageController\.Get(?:Array)?|SetKey(?:WithParams)?|SetArrayKey(?:WithParams)?)\s*\(\s*""([^""]+)""", RegexOptions.Compiled);
            var files = Directory.GetFiles("Assets", "*.cs", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                try
                {
                    var text = File.ReadAllText(file);
                    foreach (Match match in regex.Matches(text))
                    {
                        var key = match.Groups[1].Value;
                        if (!string.IsNullOrWhiteSpace(key))
                        {
                            result.Add(key);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[Language] Failed to scan {file}: {ex.Message}");
                }
            }
            return result;
        }

        private static HashSet<string> CollectKeysFromLanguageFolder(string folderAssetPath)
        {
            var result = new HashSet<string>(StringComparer.Ordinal);
            var folderFsPath = Path.GetFullPath(folderAssetPath);
            if (!Directory.Exists(folderFsPath)) return result;

            var jsonFiles = Directory.GetFiles(folderFsPath, "*.json", SearchOption.TopDirectoryOnly);
            foreach (var jsonPath in jsonFiles)
            {
                try
                {
                    var json = File.ReadAllText(jsonPath);
                    var root = JObject.Parse(json);
                    foreach (var prop in root.Properties())
                    {
                        result.Add(prop.Name);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[Language] Failed to parse {jsonPath}: {ex.Message}");
                }
            }

            return result;
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

        private void SetStatus(string message, MessageType type)
        {
            _statusMessage = message;
            _statusType = type;
            Repaint();
        }
    }
}
