using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace ENP.UnityExtensions.Editor
{
    // Safety net for the "Build Guard" rule in README.md: BuildGuardSettings.ReleaseDefineSymbol
    // (default APP_BUILD_RELEASE) must only ever be injected per-build via extraScriptingDefines,
    // never persisted in Player Settings. If it ends up there anyway (hand edit, merge, copy-paste
    // from another platform's define list), this strips it back out and warns.
    internal static class BuildGuardScriptingDefineGuard
    {
        [InitializeOnLoadMethod]
        private static void OnEditorLoad()
        {
            RemoveReleaseDefineFromPlayerSettings();
        }

        internal static void RemoveReleaseDefineFromPlayerSettings()
        {
            var releaseDefine = BuildGuardSettings.instance.ReleaseDefineSymbol;
            if (string.IsNullOrWhiteSpace(releaseDefine))
                return;

            foreach (BuildTargetGroup group in Enum.GetValues(typeof(BuildTargetGroup)))
            {
                if (group == BuildTargetGroup.Unknown)
                    continue;

                NamedBuildTarget namedTarget;
                try
                {
                    namedTarget = NamedBuildTarget.FromBuildTargetGroup(group);
                }
                catch (ArgumentException)
                {
                    continue;
                }

                RemoveFromTarget(namedTarget, releaseDefine);
            }
        }

        private static void RemoveFromTarget(NamedBuildTarget namedTarget, string releaseDefine)
        {
            string[] defines;
            try
            {
                defines = PlayerSettings.GetScriptingDefineSymbols(namedTarget)
                    .Split(';', StringSplitOptions.RemoveEmptyEntries);
            }
            catch (Exception)
            {
                return;
            }

            if (Array.IndexOf(defines, releaseDefine) < 0)
                return;

            var filtered = Array.FindAll(defines, define => define != releaseDefine);
            PlayerSettings.SetScriptingDefineSymbols(namedTarget, string.Join(";", filtered));

            Debug.LogWarning(
                $"[Build Guard] '{releaseDefine}' was found in Player Settings scripting define " +
                $"symbols for '{namedTarget.TargetName}' and has been removed automatically. This " +
                "symbol must only be injected per-build by Build Guard, never persisted - see " +
                "README.md \"Build Guard\" section.");
        }
    }
}
