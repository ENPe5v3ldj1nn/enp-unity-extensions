using System.Linq;
using System.Text;
using UnityEditor.Build;
using UnityEngine;

namespace ENP.UnityExtensions.Editor
{
    // Runs LanguageFilesValidator on every Build Guard build. Release fails on any error, because a
    // missing key ships as a visible "<key>" marker; Development only logs, so a half-translated
    // language does not block a test build. Projects without language folders are unaffected.
    public sealed class LanguageFilesBuildGuardAdapter : IBuildGuardProjectAdapter
    {
        private const int MAX_LISTED_ISSUES = 25;

        public int Order => -100;

        public void Validate(BuildGuardContext context)
        {
            var issues = LanguageFilesValidator.ValidateProject();
            if (issues.Count == 0)
                return;

            var errors = issues.Where(issue => issue.Severity == LanguageFileIssueSeverity.Error).ToList();
            foreach (var issue in issues)
            {
                var message = $"[Language] {issue.Message}";
                if (issue.Severity == LanguageFileIssueSeverity.Error)
                    Debug.LogError(message);
                else
                    Debug.LogWarning(message);
            }

            if (context.Mode != BuildMode.Release || errors.Count == 0)
                return;

            var summary = new StringBuilder();
            summary.AppendLine($"Language files have {errors.Count} error(s); Release build aborted.");
            foreach (var error in errors.Take(MAX_LISTED_ISSUES))
                summary.AppendLine(error.Message);

            if (errors.Count > MAX_LISTED_ISSUES)
                summary.AppendLine($"...and {errors.Count - MAX_LISTED_ISSUES} more (see Console).");

            throw new BuildFailedException(summary.ToString());
        }

        public void Apply(BuildGuardContext context)
        {
        }

        public void Restore(BuildGuardContext context)
        {
        }
    }
}
