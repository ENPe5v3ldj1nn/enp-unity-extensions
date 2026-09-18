using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ENP.UnityExtensions.Runtime;
using Newtonsoft.Json.Linq;

namespace ENP.UnityExtensions.Editor
{
    public enum LanguageFileIssueSeverity
    {
        Warning,
        Error
    }

    public readonly struct LanguageFileIssue
    {
        public LanguageFileIssue(LanguageFileIssueSeverity severity, string message)
        {
            Severity = severity;
            Message = message;
        }

        public LanguageFileIssueSeverity Severity { get; }
        public string Message { get; }

        public override string ToString() => $"[{Severity}] {Message}";
    }

    // Checks every Resources language root against its English (EnglishUS) folder, which is the
    // source of truth: missing keys, mismatched value types and {n} placeholders, plural forms the
    // language needs, unparseable JSON, and literal keys used in code that English lacks.
    // A language root is any folder under a Resources folder that contains the EnglishUS folder,
    // so custom LanguageController.SetResourcesPath locations are found too.
    public static class LanguageFilesValidator
    {
        private const string AssetsFolder = "Assets";
        private const string ResourcesSegment = "Resources";

        private static readonly Regex PlaceholderRegex = new Regex(@"(?<!\{)\{(\d+)(?::[^}]*)?\}", RegexOptions.Compiled);

        private static readonly Regex CodeKeyRegex = new Regex(
            @"\b(?:LanguageController\.(?:Get|GetArray|GetPlural)|SetKey(?:WithParams)?|SetArrayKey(?:WithParams)?|SetPluralKey)\s*\(\s*""([^""]+)""",
            RegexOptions.Compiled);

        private static readonly long[] PluralProbeCounts = BuildPluralProbeCounts();

        public static IReadOnlyList<LanguageFileIssue> ValidateProject()
        {
            var issues = new List<LanguageFileIssue>();
            var roots = FindLanguageRoots();
            if (roots.Count == 0)
                return issues;

            // A key used in code only has to exist in one root's English — which root a given call
            // reads from depends on LanguageController.SetResourcesPath, which cannot be known here.
            var englishKeysInAnyRoot = new HashSet<string>(StringComparer.Ordinal);
            foreach (var root in roots)
                englishKeysInAnyRoot.UnionWith(ValidateRoot(root, issues));

            foreach (var key in CollectKeysFromCode().Where(key => !englishKeysInAnyRoot.Contains(key)).OrderBy(key => key, StringComparer.Ordinal))
                AddError(issues, $"Key '{key}' is used in code but missing from English.");

            return issues;
        }

        public static HashSet<string> CollectKeysFromCode()
        {
            var result = new HashSet<string>(StringComparer.Ordinal);
            foreach (var file in Directory.GetFiles(AssetsFolder, "*.cs", SearchOption.AllDirectories))
            {
                string text;
                try
                {
                    text = File.ReadAllText(file);
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogWarning($"[Language] Failed to scan {file}: {ex.Message}");
                    continue;
                }

                foreach (Match match in CodeKeyRegex.Matches(text))
                {
                    var key = match.Groups[1].Value;
                    if (!string.IsNullOrWhiteSpace(key))
                        result.Add(key);
                }
            }

            return result;
        }

        private static List<string> FindLanguageRoots()
        {
            var englishFolder = LanguageId.EnglishUS.ToFolderName();
            return Directory.GetDirectories(AssetsFolder, englishFolder, SearchOption.AllDirectories)
                .Select(path => Path.GetDirectoryName(path)?.Replace('\\', '/'))
                .Where(path => !string.IsNullOrEmpty(path) && IsInsideResources(path))
                .Distinct(StringComparer.Ordinal)
                .ToList();
        }

        private static bool IsInsideResources(string path)
        {
            return path.Split('/').Any(segment => string.Equals(segment, ResourcesSegment, StringComparison.OrdinalIgnoreCase));
        }

        private static IEnumerable<string> ValidateRoot(string root, List<LanguageFileIssue> issues)
        {
            var english = LoadFolder(Path.Combine(root, LanguageId.EnglishUS.ToFolderName()), root, issues);

            foreach (var folder in Directory.GetDirectories(root).OrderBy(path => path, StringComparer.Ordinal))
            {
                var folderName = Path.GetFileName(folder);
                if (string.Equals(folderName, LanguageId.EnglishUS.ToFolderName(), StringComparison.Ordinal))
                    continue;

                if (!TryGetLanguageForFolder(folderName, out var language))
                {
                    AddWarning(issues, $"{root}/{folderName}: folder does not match any LanguageId and will never be loaded.");
                    continue;
                }

                var translation = LoadFolder(folder, root, issues);
                ValidateTranslation($"{root}/{folderName}", language, english, translation, issues);
            }

            return english.Keys;
        }

        private static void ValidateTranslation(string label, LanguageId language, Dictionary<string, JToken> english,
            Dictionary<string, JToken> translation, List<LanguageFileIssue> issues)
        {
            var requiredForms = GetRequiredPluralForms(language);

            foreach (var entry in english)
            {
                if (!translation.TryGetValue(entry.Key, out var translated))
                {
                    AddError(issues, $"{label}: missing key '{entry.Key}'.");
                    continue;
                }

                if (translated.Type != entry.Value.Type)
                {
                    AddError(issues, $"{label}: '{entry.Key}' is {translated.Type} but English has {entry.Value.Type}.");
                    continue;
                }

                switch (entry.Value.Type)
                {
                    case JTokenType.String:
                        ComparePlaceholders(label, entry.Key, entry.Value.Value<string>(), translated.Value<string>(), issues);
                        break;
                    case JTokenType.Array:
                        CompareArrays(label, entry.Key, (JArray)entry.Value, (JArray)translated, issues);
                        break;
                    case JTokenType.Object:
                        ComparePlurals(label, entry.Key, (JObject)entry.Value, (JObject)translated, requiredForms, issues);
                        break;
                }
            }

            foreach (var key in translation.Keys.Where(key => !english.ContainsKey(key)).OrderBy(key => key, StringComparer.Ordinal))
                AddWarning(issues, $"{label}: key '{key}' is not in English and is never checked against a source.");
        }

        private static void ComparePlaceholders(string label, string key, string source, string translated, List<LanguageFileIssue> issues)
        {
            var expected = GetPlaceholders(source);
            var actual = GetPlaceholders(translated);
            if (!expected.SetEquals(actual))
                AddError(issues, $"{label}: '{key}' placeholders {Format(actual)} differ from English {Format(expected)}.");
        }

        // Arrays can be random pools (any length is fine) or index-paired lists (must match) —
        // the validator cannot tell which, so a length difference is only a warning.
        private static void CompareArrays(string label, string key, JArray source, JArray translated, List<LanguageFileIssue> issues)
        {
            if (source.Count != translated.Count)
                AddWarning(issues, $"{label}: '{key}' has {translated.Count} items, English has {source.Count}.");

            var count = Math.Min(source.Count, translated.Count);
            for (var i = 0; i < count; i++)
            {
                if (source[i].Type == JTokenType.String && translated[i].Type == JTokenType.String)
                    ComparePlaceholders(label, $"{key}[{i}]", source[i].Value<string>(), translated[i].Value<string>(), issues);
            }
        }

        private static void ComparePlurals(string label, string key, JObject source, JObject translated,
            HashSet<string> requiredForms, List<LanguageFileIssue> issues)
        {
            var otherKey = PluralCategory.Other.ToKey();
            var sourceOther = source.Value<string>(otherKey);
            var hasOther = translated.Property(otherKey) != null;
            if (!hasOther)
                AddError(issues, $"{label}: plural '{key}' has no \"other\" form.");

            foreach (var form in requiredForms.Where(form => form != otherKey && translated.Property(form) == null))
            {
                var consequence = hasOther ? "\"other\" will be used instead" : "another form will be used instead";
                AddWarning(issues, $"{label}: plural '{key}' has no \"{form}\" form; {consequence}.");
            }

            if (sourceOther == null)
                return;

            foreach (var form in translated.Properties().Where(p => p.Value.Type == JTokenType.String))
                ComparePlaceholders(label, $"{key}.{form.Name}", sourceOther, form.Value.Value<string>(), issues);
        }

        private static Dictionary<string, JToken> LoadFolder(string folder, string root, List<LanguageFileIssue> issues)
        {
            var result = new Dictionary<string, JToken>(StringComparer.Ordinal);
            if (!Directory.Exists(folder))
            {
                AddError(issues, $"{root}: folder '{Path.GetFileName(folder)}' not found.");
                return result;
            }

            foreach (var file in Directory.GetFiles(folder, "*.json", SearchOption.TopDirectoryOnly).OrderBy(path => path, StringComparer.Ordinal))
            {
                JObject json;
                try
                {
                    json = JObject.Parse(File.ReadAllText(file));
                }
                catch (Exception ex)
                {
                    AddError(issues, $"{file.Replace('\\', '/')}: invalid JSON ({ex.Message}).");
                    continue;
                }

                foreach (var property in json.Properties())
                    result[property.Name] = property.Value;
            }

            return result;
        }

        private static HashSet<int> GetPlaceholders(string text)
        {
            var result = new HashSet<int>();
            if (string.IsNullOrEmpty(text))
                return result;

            foreach (Match match in PlaceholderRegex.Matches(text))
                result.Add(int.Parse(match.Groups[1].Value));

            return result;
        }

        private static HashSet<string> GetRequiredPluralForms(LanguageId language)
        {
            var result = new HashSet<string>(StringComparer.Ordinal);
            foreach (var count in PluralProbeCounts)
                result.Add(PluralRules.Select(language, count).ToKey());

            return result;
        }

        private static long[] BuildPluralProbeCounts()
        {
            var counts = new List<long>();
            for (long i = 0; i <= 200; i++)
                counts.Add(i);

            counts.Add(1000000);
            return counts.ToArray();
        }

        private static bool TryGetLanguageForFolder(string folderName, out LanguageId language)
        {
            foreach (LanguageId candidate in Enum.GetValues(typeof(LanguageId)))
            {
                if (!string.Equals(candidate.ToFolderName(), folderName, StringComparison.Ordinal))
                    continue;

                language = candidate;
                return true;
            }

            language = LanguageId.EnglishUS;
            return false;
        }

        private static string Format(HashSet<int> placeholders)
        {
            return placeholders.Count == 0 ? "(none)" : string.Join(", ", placeholders.OrderBy(i => i).Select(i => "{" + i + "}"));
        }

        private static void AddError(List<LanguageFileIssue> issues, string message)
        {
            issues.Add(new LanguageFileIssue(LanguageFileIssueSeverity.Error, message));
        }

        private static void AddWarning(List<LanguageFileIssue> issues, string message)
        {
            issues.Add(new LanguageFileIssue(LanguageFileIssueSeverity.Warning, message));
        }
    }
}
