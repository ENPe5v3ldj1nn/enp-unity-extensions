using System;
using System.Linq;

namespace enp_unity_extensions.Editor.LanguageSettings
{
    internal static class LanguageSettingsPathUtility
    {
        private const string AssetsPrefix = "Assets";
        private const string ResourcesSegment = "Resources";

        internal static string Sanitize(string path)
        {
            return string.IsNullOrWhiteSpace(path) ? string.Empty : path.Replace('\\', '/').Trim().Trim('/');
        }

        internal static string ToAssetPath(string path)
        {
            var sanitized = Sanitize(path);
            if (string.IsNullOrEmpty(sanitized))
            {
                return $"{AssetsPrefix}/{ResourcesSegment}";
            }

            var assetsIndex = sanitized.IndexOf($"{AssetsPrefix}/", StringComparison.OrdinalIgnoreCase);
            if (assetsIndex > 0)
            {
                sanitized = sanitized.Substring(assetsIndex);
            }

            if (sanitized.StartsWith($"{AssetsPrefix}/", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(sanitized, AssetsPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return sanitized.TrimEnd('/');
            }

            return $"{AssetsPrefix}/{ResourcesSegment}/{sanitized}";
        }

        internal static string ToResourcesRelativePath(string path)
        {
            var sanitized = Sanitize(path);
            if (string.IsNullOrEmpty(sanitized))
            {
                return string.Empty;
            }

            var segments = sanitized.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            var resourcesIndex = Array.FindIndex(segments, s => string.Equals(s, ResourcesSegment, StringComparison.OrdinalIgnoreCase));
            if (resourcesIndex >= 0)
            {
                var relative = string.Join("/", segments.Skip(resourcesIndex + 1));
                return relative;
            }

            return sanitized;
        }

        internal static bool IsAssetPathInsideResources(string path)
        {
            var sanitized = Sanitize(path);
            if (string.IsNullOrEmpty(sanitized))
            {
                return true;
            }

            var startsWithAssets = sanitized.StartsWith($"{AssetsPrefix}/", StringComparison.OrdinalIgnoreCase) ||
                                   string.Equals(sanitized, AssetsPrefix, StringComparison.OrdinalIgnoreCase);
            if (startsWithAssets)
            {
                var segments = sanitized.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                return segments.Any(s => string.Equals(s, ResourcesSegment, StringComparison.OrdinalIgnoreCase));
            }

            // Relative paths will be placed under Assets/Resources automatically.
            return true;
        }
    }
}
