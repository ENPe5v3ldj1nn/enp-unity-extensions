using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace ENP.UnityExtensions.Editor
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
        private IReadOnlyList<LanguageFileIssue> _validationIssues;
        private Vector2 _validationScroll;

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
                if (GUILayout.Button("Validate all language files", GUILayout.Width(220)))
                {
                    RunValidation();
                }
                GUILayout.FlexibleSpace();
            }

            DrawValidationIssues();

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

        // Same check the Build Guard runs before every build (LanguageFilesBuildGuardAdapter).
        private void RunValidation()
        {
            _validationIssues = LanguageFilesValidator.ValidateProject();
            var errors = _validationIssues.Count(issue => issue.Severity == LanguageFileIssueSeverity.Error);
            var warnings = _validationIssues.Count - errors;

            if (_validationIssues.Count == 0)
                _host.SetStatus("Validation complete: no issues.", MessageType.Info);
            else
                _host.SetStatus($"Validation complete: {errors} error(s), {warnings} warning(s).", errors > 0 ? MessageType.Error : MessageType.Warning);
        }

        private void DrawValidationIssues()
        {
            if (_validationIssues == null || _validationIssues.Count == 0)
                return;

            GUILayout.Space(6f);
            EditorGUILayout.LabelField("Validation issues:", EditorStyles.boldLabel);
            _validationScroll = EditorGUILayout.BeginScrollView(_validationScroll, GUILayout.Height(200));
            foreach (var issue in _validationIssues)
            {
                var type = issue.Severity == LanguageFileIssueSeverity.Error ? MessageType.Error : MessageType.Warning;
                EditorGUILayout.HelpBox(issue.Message, type);
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
            return LanguageFilesValidator.CollectKeysFromCode();
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
                    UnityEngine.Debug.LogWarning($"[Language] Failed to parse {jsonPath}: {ex.Message}");
                }
            }

            return result;
        }
    }
}
