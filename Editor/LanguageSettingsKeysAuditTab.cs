using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace enp_unity_extensions.Editor.Language
{
    internal class LanguageSettingsKeysAuditTab : ILanguageSettingsTab
    {
        public string Title => "Audit Folders";

        private LanguageSettingsWindow _host;
        private readonly Dictionary<string, List<string>> _missingByKey = new Dictionary<string, List<string>>();
        private readonly List<string> _auditLanguages = new List<string>();
        private int _auditUsedKeys;
        private int _auditLanguagesProcessed;
        private Vector2 _scroll;

        public void OnEnable(LanguageSettingsWindow host)
        {
            _host = host;
            _missingByKey.Clear();
            _auditLanguages.Clear();
            _auditUsedKeys = 0;
            _auditLanguagesProcessed = 0;
        }

        public void OnDisable()
        {
        }

        public void OnGUI()
        {
            EditorGUILayout.LabelField("Audit keys used in code vs translations", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Base translations path", _host.GetAbsoluteResourcesPath());
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
            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.Height(200));
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

        private void RunAudit()
        {
            _missingByKey.Clear();
            _auditLanguages.Clear();
            _auditUsedKeys = 0;
            _auditLanguagesProcessed = 0;

            var usedKeys = CollectKeysFromCode();
            _auditUsedKeys = usedKeys.Count;

            var basePath = _host.GetAbsoluteResourcesPath();
            if (!AssetDatabase.IsValidFolder(basePath))
            {
                _host.SetStatus($"Translations path not found: {basePath}", MessageType.Warning);
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
                _host.SetStatus("Audit complete: all used keys found in translations.", MessageType.Info);
            }
            else
            {
                _host.SetStatus($"Audit complete: {_missingByKey.Count} missing key(s) detected.", MessageType.Warning);
            }
        }

        private static HashSet<string> CollectKeysFromCode()
        {
            var result = new HashSet<string>(System.StringComparer.Ordinal);
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
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[Language] Failed to scan {file}: {ex.Message}");
                }
            }
            return result;
        }

        private static HashSet<string> CollectKeysFromLanguageFolder(string folderAssetPath)
        {
            var result = new HashSet<string>(System.StringComparer.Ordinal);
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
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[Language] Failed to parse {jsonPath}: {ex.Message}");
                }
            }

            return result;
        }
    }
}
