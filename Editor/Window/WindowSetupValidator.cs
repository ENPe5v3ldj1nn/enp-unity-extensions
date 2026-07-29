using System.Collections.Generic;
using System.Linq;
using System.Text;
using ENP.UnityExtensions.Runtime;
using UnityEditor;
using UnityEngine;

namespace ENP.UnityExtensions.Editor
{
    internal static class WindowSetupValidator
    {
        [MenuItem("ENP/UI/Validate Windows In Scene")]
        private static void Validate()
        {
            var windows = Object.FindObjectsByType<AnimatedWindow>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (windows.Length == 0)
            {
                UnityEngine.Debug.Log("[WindowSetupValidator] No AnimatedWindow found in the open scene(s).");
                return;
            }

            var issues = 0;
            var report = new StringBuilder();
            report.AppendLine($"[WindowSetupValidator] Checked {windows.Length} AnimatedWindow(s).");

            foreach (var window in windows)
                issues += ValidateWindow(window, report);

            issues += ValidateDuplicateNames(windows, report);

            if (issues == 0)
                UnityEngine.Debug.Log(report.ToString());
            else
                UnityEngine.Debug.LogWarning(report.ToString());
        }

        private static int ValidateWindow(AnimatedWindow window, StringBuilder report)
        {
            var so = new SerializedObject(window);
            var hideMode = (WindowHideMode)so.FindProperty("_hideMode").enumValueIndex;
            var canvas = so.FindProperty("_canvas").objectReferenceValue;
            var raycaster = so.FindProperty("_raycaster").objectReferenceValue;
            var config = so.FindProperty("_config").objectReferenceValue;

            var path = GetPath(window.transform);
            var issues = 0;

            if (hideMode == WindowHideMode.Canvas)
            {
                if (canvas == null)
                {
                    report.AppendLine($"  - {path}: Hide Mode is Canvas but no Canvas is assigned/present on this GameObject. It will silently fall back to GameObject toggling (no perf benefit).");
                    issues++;
                }
                else if (raycaster == null)
                {
                    report.AppendLine($"  - {path}: Has a Canvas but no GraphicRaycaster. Input will keep hitting this window's UI while it's supposed to be hidden.");
                    issues++;
                }
            }

            if (config == null && WindowConfig.Default == null)
            {
                report.AppendLine($"  - {path}: No per-window WindowConfig and WindowConfig.Default isn't set (yet). Make sure a controller assigns a default before this window opens.");
            }

            if (!System.Attribute.IsDefined(window.GetType(), typeof(UiWindowAttribute), inherit: false))
                report.AppendLine($"  - {path}: Type '{window.GetType().Name}' has no [UiWindow]. Fine for a nested sub-view; won't be auto-registered as a top-level window otherwise.");

            return issues;
        }

        private static int ValidateDuplicateNames(IEnumerable<AnimatedWindow> windows, StringBuilder report)
        {
            var issues = 0;
            var byType = windows.GroupBy(w => w.GetType());

            foreach (var group in byType)
            {
                var names = group.Select(w => w.gameObject.name).ToList();
                var duplicates = names.GroupBy(n => n).Where(g => g.Count() > 1).Select(g => g.Key).Distinct();

                foreach (var dup in duplicates)
                {
                    report.AppendLine($"  - Multiple '{group.Key.Name}' windows are named '{dup}'. Name-based ShowExclusive/GetWindow lookup can't disambiguate them.");
                    issues++;
                }
            }

            return issues;
        }

        private static string GetPath(Transform t)
        {
            var path = t.name;
            while (t.parent != null)
            {
                t = t.parent;
                path = t.name + "/" + path;
            }

            return path;
        }
    }
}
