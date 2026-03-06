using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using enp_unity_extensions.Editor.LanguageSettings;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace enp_unity_extensions.Editor.Language
{
    internal class TranslationEditorView
    {
        private class LanguageEntry
        {
            public SystemLanguage Language;
            public string Folder;
            public List<string> Files = new List<string>();
            public string Value = string.Empty;
            public bool Enabled = true;
        }

        private class TemplateFile
        {
            public string FileName = string.Empty;
            public List<string> Keys = new List<string>();
        }

        private const string WindowTitle = "Translation Editor";
        private const string PrefsBasePathKey = "ENP.LanguageSettings.ResourcesPath";
        private const string DefaultResourcesPath = "Languages";

        private string _resourcesPath;
        private string _key = string.Empty;
        private Vector2 _scroll;
        private readonly List<LanguageEntry> _entries = new List<LanguageEntry>();
        private string _statusMessage = string.Empty;
        private MessageType _statusType = MessageType.Info;
        private readonly List<string> _sourceFiles = new List<string>();
        private string _selectedSourceFile = string.Empty;
        private readonly List<string> _availableKeys = new List<string>();
        private int _selectedKeyIndex = -1;
        private string _loadedKey = string.Empty;
        private string _pendingSelectKey = string.Empty;
        private readonly List<TemplateFile> _templateFiles = new List<TemplateFile>();
        private string _newSourceFileName = string.Empty;
        private EditorWindow _host;
        public string ResourcesPath => _resourcesPath;

        public void OnEnable(EditorWindow host)
        {
            _host = host;
            _resourcesPath = LanguageSettingsPathUtility.Sanitize(EditorPrefs.GetString(PrefsBasePathKey, DefaultResourcesPath));
            RefreshLanguages();
        }

        public void OnDisable()
        {
        }

        public void SyncResourcesPath(string path)
        {
            var sanitized = LanguageSettingsPathUtility.Sanitize(path);
            if (string.Equals(sanitized, _resourcesPath, StringComparison.Ordinal))
            {
                return;
            }

            _resourcesPath = sanitized;
            EditorPrefs.SetString(PrefsBasePathKey, _resourcesPath);
            RefreshLanguages();
        }

        public void OnGUI()
        {
            EditorGUILayout.LabelField("Base translations path (asset path under a Resources folder)", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            var trimmed = LanguageSettingsPathUtility.Sanitize(EditorGUILayout.TextField("Resources Path", _resourcesPath));
            if (EditorGUI.EndChangeCheck())
            {
                _resourcesPath = trimmed;
                EditorPrefs.SetString(PrefsBasePathKey, _resourcesPath);
                RefreshLanguages();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel("Absolute path");
                EditorGUILayout.SelectableLabel(GetAbsoluteResourcesPath(), EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Refresh", GUILayout.Width(120)))
                {
                    RefreshLanguages();
                }
                GUILayout.FlexibleSpace();
            }

            GUILayout.Space(6f);
            EditorGUILayout.LabelField("Key source file (first language folder)", EditorStyles.boldLabel);
            if (_sourceFiles.Count == 0)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PrefixLabel("Source file");
                    _newSourceFileName = EditorGUILayout.TextField(_newSourceFileName, GUILayout.Width(180f));
                    if (GUILayout.Button("Add", GUILayout.Width(60f)))
                    {
                        AddSourceFile();
                    }
                }
                EditorGUILayout.HelpBox("No JSON files found. Enter a name and press Add to create one for all languages.", MessageType.Info);
            }
            else
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    var sourceIndex = Mathf.Max(0, _sourceFiles.IndexOf(_selectedSourceFile));
                    var newIndex = EditorGUILayout.Popup("Source file", sourceIndex, _sourceFiles.ToArray());
                    var chosen = _sourceFiles[Mathf.Clamp(newIndex, 0, _sourceFiles.Count - 1)];
                    if (!string.Equals(chosen, _selectedSourceFile, StringComparison.Ordinal))
                    {
                        _selectedSourceFile = chosen;
                        LoadKeysFromSourceFile();
                    }

                    _newSourceFileName = EditorGUILayout.TextField(_newSourceFileName, GUILayout.Width(140f));

                    if (GUILayout.Button("Add", GUILayout.Width(60f)))
                    {
                        AddSourceFile();
                    }

                    if (GUILayout.Button("Delete", GUILayout.Width(70f)))
                    {
                        DeleteSourceFile();
                    }
                }
            }

            GUILayout.Space(6f);
            _key = EditorGUILayout.TextField("Key", _key);
            if (_availableKeys.Count > 0)
            {
                var keyNames = _availableKeys.ToArray();
                var currentIndex = Mathf.Clamp(_selectedKeyIndex, 0, keyNames.Length - 1);
                EditorGUI.BeginChangeCheck();
                var selected = EditorGUILayout.Popup("Existing key", currentIndex, keyNames);
                if (EditorGUI.EndChangeCheck())
                {
                    _selectedKeyIndex = selected;
                    _key = keyNames[_selectedKeyIndex];
                    _loadedKey = _key;
                    PopulateValuesForKey(_key);
                }
            }

            GUILayout.Space(4f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("New Key", GUILayout.Width(90f)))
                {
                    ClearCurrentKey();
                }

                if (GUILayout.Button("Save Key", GUILayout.Height(24f)))
                {
                    SaveKey();
                }

                using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(_key) && string.IsNullOrWhiteSpace(_loadedKey)))
                {
                    if (GUILayout.Button("Delete Key", GUILayout.Width(100f)))
                    {
                        DeleteKey();
                    }
                }
            }

            GUILayout.Space(8f);
            EditorGUILayout.LabelField("Languages", EditorStyles.boldLabel);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            if (_entries.Count == 0)
            {
                EditorGUILayout.HelpBox("No language folders found. Set the correct resources path or create language folders first.", MessageType.Info);
            }
            else
            {
                foreach (var entry in _entries)
                {
                    DrawLanguageEntry(entry);
                    GUILayout.Space(6f);
                }
            }
            EditorGUILayout.EndScrollView();

            if (!string.IsNullOrEmpty(_statusMessage))
            {
                EditorGUILayout.HelpBox(_statusMessage, _statusType);
            }
        }

        private void DrawLanguageEntry(LanguageEntry entry)
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                entry.Enabled = EditorGUILayout.ToggleLeft($"{entry.Language} ({entry.Folder})", entry.Enabled, GUILayout.Width(200f));

                EditorGUILayout.LabelField("Value");
                entry.Value = EditorGUILayout.TextArea(entry.Value, GUILayout.MinHeight(40f));
            }
        }

        private void RefreshLanguages()
        {
            _entries.Clear();
            var basePath = GetAbsoluteResourcesPath();
            if (!AssetDatabase.IsValidFolder(basePath))
            {
                SetStatus($"Translations path not found: {basePath}", MessageType.Warning);
                return;
            }

            foreach (var folder in AssetDatabase.GetSubFolders(basePath))
            {
                var folderName = Path.GetFileName(folder);
                if (string.IsNullOrEmpty(folderName)) continue;
                if (!TryGetLanguageForFolder(folderName, out var language)) continue;

                var entry = new LanguageEntry
                {
                    Language = language,
                    Folder = folderName
                };

                entry.Files.AddRange(GetJsonFilesInFolder(folder));

                _entries.Add(entry);
            }

            _entries.Sort((a, b) => string.CompareOrdinal(a.Folder, b.Folder));
            SetStatus($"Found {_entries.Count} language folders.", MessageType.Info);
            BuildTemplateFiles(basePath);
            EnsureTemplateFilesExist(basePath);
            RefreshKeySourceOptions();
        }

        private IEnumerable<string> GetJsonFilesInFolder(string folderAssetPath)
        {
            var result = new List<string>();
            try
            {
                var full = Path.GetFullPath(folderAssetPath);
                if (!Directory.Exists(full)) return result;
                foreach (var file in Directory.GetFiles(full, "*.json", SearchOption.TopDirectoryOnly))
                {
                    result.Add(Path.GetFileName(file));
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Language] Failed to read files in {folderAssetPath}: {ex.Message}");
            }
            return result;
        }

        private void SaveKey()
        {
            if (string.IsNullOrWhiteSpace(_key))
            {
                SetStatus("Key cannot be empty.", MessageType.Error);
                return;
            }

            var targetKey = _key.Trim();
            var isRenaming = !string.IsNullOrEmpty(_loadedKey) && !string.Equals(_loadedKey, targetKey, StringComparison.Ordinal);

            var activeEntries = _entries.Where(e => e.Enabled).ToList();
            if (activeEntries.Count == 0)
            {
                SetStatus("Select at least one language to save.", MessageType.Warning);
                return;
            }

            var basePath = GetAbsoluteResourcesPath();
            if (!AssetDatabase.IsValidFolder(basePath))
            {
                SetStatus($"Translations path not found: {basePath}", MessageType.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(_selectedSourceFile))
            {
                SetStatus("Select a source file or create one before saving.", MessageType.Error);
                return;
            }

            foreach (var entry in activeEntries)
            {
                var targetFileName = _selectedSourceFile;
                var folderAssetPath = $"{basePath}/{entry.Folder}";
                var folderFsPath = Path.GetFullPath(folderAssetPath);
                var targetPath = Path.Combine(folderFsPath, targetFileName);

                try
                {
                    JObject json;
                    if (File.Exists(targetPath))
                    {
                        var content = File.ReadAllText(targetPath);
                        json = string.IsNullOrWhiteSpace(content) ? new JObject() : JObject.Parse(content);
                    }
                    else
                    {
                        json = new JObject();
                    }

                    json[targetKey] = entry.Value ?? string.Empty;
                    if (isRenaming && json.Property(_loadedKey) != null)
                    {
                        json.Remove(_loadedKey);
                    }
                    Directory.CreateDirectory(Path.GetDirectoryName(targetPath) ?? folderFsPath);
                    File.WriteAllText(targetPath, json.ToString());
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Language] Failed to write '{targetFileName}' for {entry.Language}: {ex.Message}");
                    SetStatus($"Failed to save for {entry.Language}: {ex.Message}", MessageType.Error);
                    return;
                }
            }

            AssetDatabase.Refresh();
            _loadedKey = targetKey;
            _pendingSelectKey = targetKey;
            SetStatus($"Saved key '{targetKey}' for {activeEntries.Count} language(s).", MessageType.Info);
            RefreshLanguages();
        }

        private void AddSourceFile()
        {
            if (_entries.Count == 0)
            {
                SetStatus("No language folders available to create a file.", MessageType.Warning);
                return;
            }

            var name = string.IsNullOrWhiteSpace(_newSourceFileName) ? _selectedSourceFile : _newSourceFileName;
            if (string.IsNullOrWhiteSpace(name))
            {
                SetStatus("Enter a file name to add.", MessageType.Error);
                return;
            }

            name = SanitizeFileName(name.Trim());
            if (!name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                name += ".json";
            }

            if (_sourceFiles.Contains(name))
            {
                _selectedSourceFile = name;
                LoadKeysFromSourceFile();
                SetStatus($"File '{name}' already exists. Selected.", MessageType.Info);
                return;
            }

            var basePath = GetAbsoluteResourcesPath();
            CreateFileAcrossLanguages(basePath, name, Enumerable.Empty<string>());
            AssetDatabase.Refresh();
            RefreshLanguages();
            _selectedSourceFile = name;
            LoadKeysFromSourceFile();
            SetStatus($"Created file '{name}' for all languages.", MessageType.Info);
        }

        private void DeleteSourceFile()
        {
            if (string.IsNullOrEmpty(_selectedSourceFile))
            {
                SetStatus("No source file selected to delete.", MessageType.Warning);
                return;
            }

            var confirm = EditorUtility.DisplayDialog(
                "Delete translation file?",
                $"Delete '{_selectedSourceFile}' from all language folders?",
                "Delete",
                "Cancel");

            if (!confirm) return;

            var basePath = GetAbsoluteResourcesPath();
            foreach (var entry in _entries)
            {
                var folderFsPath = Path.GetFullPath($"{basePath}/{entry.Folder}");
                var targetPath = Path.Combine(folderFsPath, _selectedSourceFile);
                try
                {
                    if (File.Exists(targetPath))
                    {
                        File.Delete(targetPath);
                    }
                    entry.Files.Remove(_selectedSourceFile);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[Language] Failed to delete '{targetPath}': {ex.Message}");
                }
            }

            _sourceFiles.Remove(_selectedSourceFile);
            _selectedSourceFile = _sourceFiles.Count > 0 ? _sourceFiles[0] : string.Empty;
            _availableKeys.Clear();
            _selectedKeyIndex = -1;
            _loadedKey = string.Empty;
            _pendingSelectKey = string.Empty;
            AssetDatabase.Refresh();
            if (!string.IsNullOrEmpty(_selectedSourceFile))
            {
                LoadKeysFromSourceFile();
            }
            SetStatus("File deleted across all languages.", MessageType.Info);
        }

        private void DeleteKey()
        {
            var targetKey = string.IsNullOrWhiteSpace(_key) ? _loadedKey : _key.Trim();
            if (string.IsNullOrWhiteSpace(targetKey))
            {
                SetStatus("No key selected to delete.", MessageType.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(_selectedSourceFile))
            {
                SetStatus("Select a source file before deleting a key.", MessageType.Error);
                return;
            }

            var basePath = GetAbsoluteResourcesPath();
            if (!AssetDatabase.IsValidFolder(basePath))
            {
                SetStatus($"Translations path not found: {basePath}", MessageType.Error);
                return;
            }

            var deleted = false;
            foreach (var entry in _entries)
            {
                var folderFsPath = Path.GetFullPath($"{basePath}/{entry.Folder}");
                var targetPath = Path.Combine(folderFsPath, _selectedSourceFile);
                if (!File.Exists(targetPath)) continue;
                try
                {
                    var content = File.ReadAllText(targetPath);
                    var json = string.IsNullOrWhiteSpace(content) ? new JObject() : JObject.Parse(content);
                    if (json.Property(targetKey) != null)
                    {
                        json.Remove(targetKey);
                        File.WriteAllText(targetPath, json.ToString());
                        deleted = true;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[Language] Failed to delete key '{targetKey}' in '{targetPath}': {ex.Message}");
                }
            }

            AssetDatabase.Refresh();
            _pendingSelectKey = string.Empty;
            _loadedKey = string.Empty;
            _key = string.Empty;
            foreach (var entry in _entries)
            {
                entry.Value = string.Empty;
            }

            RefreshLanguages();

            if (deleted)
            {
                SetStatus($"Deleted key '{targetKey}' from all languages.", MessageType.Info);
            }
            else
            {
                SetStatus($"Key '{targetKey}' not found in selected file.", MessageType.Warning);
            }
        }

        private void CreateFileAcrossLanguages(string basePath, string fileName, IEnumerable<string> keys)
        {
            var obj = new JObject();
            if (keys != null)
            {
                foreach (var key in keys)
                {
                    obj[key] = string.Empty;
                }
            }

            foreach (var entry in _entries)
            {
                var folderFsPath = Path.GetFullPath($"{basePath}/{entry.Folder}");
                var targetPath = Path.Combine(folderFsPath, fileName);
                try
                {
                    Directory.CreateDirectory(folderFsPath);
                    if (!File.Exists(targetPath))
                    {
                        File.WriteAllText(targetPath, obj.ToString());
                    }
                    if (!entry.Files.Contains(fileName))
                    {
                        entry.Files.Add(fileName);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[Language] Failed to create file '{targetPath}': {ex.Message}");
                }
            }
        }

        private void RefreshKeySourceOptions()
        {
            _sourceFiles.Clear();
            _availableKeys.Clear();
            _selectedKeyIndex = -1;
            _selectedSourceFile = string.Empty;
            _loadedKey = string.Empty;
            _newSourceFileName = string.Empty;

            if (_entries.Count == 0) return;
            var first = _entries[0];
            _sourceFiles.AddRange(first.Files);
            if (_sourceFiles.Count == 0) return;

            if (string.IsNullOrEmpty(_selectedSourceFile) || !_sourceFiles.Contains(_selectedSourceFile))
            {
                _selectedSourceFile = _sourceFiles[0];
            }

            LoadKeysFromSourceFile();
        }

        private void LoadKeysFromSourceFile()
        {
            _availableKeys.Clear();
            _selectedKeyIndex = -1;
            if (string.IsNullOrEmpty(_selectedSourceFile) || _entries.Count == 0) return;

            var first = _entries[0];
            var basePath = GetAbsoluteResourcesPath();
            var folderFsPath = Path.GetFullPath($"{basePath}/{first.Folder}");
            var targetPath = Path.Combine(folderFsPath, _selectedSourceFile);
            if (!File.Exists(targetPath))
            {
                SetStatus($"Source file not found: {targetPath}", MessageType.Warning);
                return;
            }

            try
            {
                var json = File.ReadAllText(targetPath);
                var root = string.IsNullOrWhiteSpace(json) ? new JObject() : JObject.Parse(json);
                foreach (var prop in root.Properties())
                {
                    _availableKeys.Add(prop.Name);
                }

                _availableKeys.Sort(StringComparer.Ordinal);

                if (_availableKeys.Count > 0)
                {
                    var desiredKey = !string.IsNullOrEmpty(_pendingSelectKey)
                        ? _pendingSelectKey
                        : (!string.IsNullOrEmpty(_loadedKey) ? _loadedKey : _availableKeys[0]);

                    if (_availableKeys.Contains(desiredKey))
                    {
                        _selectedKeyIndex = _availableKeys.IndexOf(desiredKey);
                        _key = desiredKey;
                        _loadedKey = desiredKey;
                    }
                    else
                    {
                        _selectedKeyIndex = 0;
                        _key = _availableKeys[0];
                        _loadedKey = _key;
                    }

                    _pendingSelectKey = string.Empty;
                    PopulateValuesForKey(_key);
                }
                else
                {
                    _pendingSelectKey = string.Empty;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Language] Failed to load keys from {targetPath}: {ex.Message}");
                SetStatus($"Failed to load keys: {ex.Message}", MessageType.Error);
            }
        }

        private void PopulateValuesForKey(string key)
        {
            if (string.IsNullOrEmpty(key)) return;
            var basePath = GetAbsoluteResourcesPath();

            foreach (var entry in _entries)
            {
                var folderFsPath = Path.GetFullPath($"{basePath}/{entry.Folder}");
                var candidates = new List<string>();
                if (!string.IsNullOrEmpty(_selectedSourceFile))
                {
                    candidates.Add(_selectedSourceFile);
                }

                string value = string.Empty;
                foreach (var file in candidates)
                {
                    var targetPath = Path.Combine(folderFsPath, file);
                    if (!File.Exists(targetPath)) continue;
                    try
                    {
                        var json = File.ReadAllText(targetPath);
                        var root = string.IsNullOrWhiteSpace(json) ? new JObject() : JObject.Parse(json);
                        var token = root[key];
                        if (token != null)
                        {
                            value = token.Type == JTokenType.String ? token.Value<string>() : token.ToString();
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[Language] Failed to read {targetPath}: {ex.Message}");
                    }
                }

                entry.Value = value ?? string.Empty;
            }
        }

        private void ClearCurrentKey()
        {
            _key = string.Empty;
            _loadedKey = string.Empty;
            _pendingSelectKey = string.Empty;
            _selectedKeyIndex = -1;
            foreach (var entry in _entries)
            {
                entry.Value = string.Empty;
            }
        }

        private void BuildTemplateFiles(string basePath)
        {
            _templateFiles.Clear();
            if (_entries.Count == 0) return;
            var templateEntry = _entries.FirstOrDefault(e => e.Files.Count > 0);
            if (templateEntry == null) return;

            var folderFsPath = Path.GetFullPath($"{basePath}/{templateEntry.Folder}");
            foreach (var file in templateEntry.Files)
            {
                var targetPath = Path.Combine(folderFsPath, file);
                if (!File.Exists(targetPath)) continue;
                try
                {
                    var json = File.ReadAllText(targetPath);
                    var root = string.IsNullOrWhiteSpace(json) ? new JObject() : JObject.Parse(json);
                    var keys = root.Properties().Select(p => p.Name).ToList();
                    _templateFiles.Add(new TemplateFile { FileName = file, Keys = keys });
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[Language] Failed to read template file {targetPath}: {ex.Message}");
                }
            }
        }

        private void EnsureTemplateFilesExist(string basePath)
        {
            if (_templateFiles.Count == 0 || _entries.Count == 0) return;

            foreach (var entry in _entries)
            {
                var folderFsPath = Path.GetFullPath($"{basePath}/{entry.Folder}");
                foreach (var template in _templateFiles)
                {
                    var targetPath = Path.Combine(folderFsPath, template.FileName);
                    if (File.Exists(targetPath)) continue;

                    try
                    {
                        Directory.CreateDirectory(folderFsPath);
                        var obj = new JObject();
                        foreach (var key in template.Keys)
                        {
                            obj[key] = string.Empty;
                        }
                        File.WriteAllText(targetPath, obj.ToString());
                        if (!entry.Files.Contains(template.FileName))
                        {
                            entry.Files.Add(template.FileName);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[Language] Failed to create template file {targetPath}: {ex.Message}");
                    }
                }
            }
        }

        private static string SanitizeFileName(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var sanitized = new string(name.Where(c => !invalid.Contains(c)).ToArray());
            return string.IsNullOrWhiteSpace(sanitized) ? "translations.json" : sanitized;
        }

        private string GetAbsoluteResourcesPath()
        {
            return LanguageSettingsPathUtility.ToAssetPath(_resourcesPath);
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

        private void SetStatus(string message, MessageType type)
        {
            _statusMessage = message;
            _statusType = type;
            _host?.Repaint();
        }
    }
}
