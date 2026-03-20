using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace BuildGuard.Editor
{
    public static class BuildGuardBuildInterceptor
    {
        private const string _settingsAssetPath = "ProjectSettings/BuildGuardSettings.asset";

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            BuildPlayerWindow.RegisterBuildPlayerHandler(OnBuildPlayer);
        }

        private static void OnBuildPlayer(BuildPlayerOptions options)
        {
            var settings = BuildGuardSettings.instance;
            var mode = ResolveBuildMode(settings);
            if (!mode.HasValue)
                return;

            settings.LastSelectedMode = mode.Value;

            var buildOptions = options;
            if (mode.Value == BuildMode.Release)
                buildOptions.extraScriptingDefines = AppendDefine(buildOptions.extraScriptingDefines, settings.ReleaseDefineSymbol);

            var context = new BuildGuardContext(mode.Value, buildOptions, settings);
            var adapters = BuildGuardAdapterDiscovery.CreateAdapters();
            var appliedAdapters = new List<IBuildGuardProjectAdapter>(adapters.Count);
            var hasPrimaryException = false;
            Exception restoreException = null;

            try
            {
                foreach (var adapter in adapters)
                    adapter.Validate(context);

                foreach (var adapter in adapters)
                {
                    adapter.Apply(context);
                    appliedAdapters.Add(adapter);
                }

                BuildPlayerWindow.DefaultBuildMethods.BuildPlayer(buildOptions);
            }
            catch
            {
                hasPrimaryException = true;
                throw;
            }
            finally
            {
                if (settings.RestoreStateAfterBuild)
                {
                    try
                    {
                        for (var i = appliedAdapters.Count - 1; i >= 0; i--)
                            appliedAdapters[i].Restore(context);
                    }
                    catch (Exception ex)
                    {
                        if (hasPrimaryException)
                            Debug.LogException(ex);
                        else
                            restoreException = ex;
                    }
                }
            }

            if (restoreException != null)
                throw restoreException;
        }

        private static BuildMode? ResolveBuildMode(BuildGuardSettings settings)
        {
            if (Application.isBatchMode)
                return settings.BatchModeBuildMode;

            if (!settings.AskEveryBuild)
                return File.Exists(_settingsAssetPath) ? settings.LastSelectedMode : settings.DefaultInteractiveMode;

            var dialogResult = EditorUtility.DisplayDialogComplex(
                "Build Guard",
                "Build as Release?",
                "Release",
                "Cancel",
                "Development");

            if (dialogResult == 1)
                return null;

            return dialogResult == 0 ? BuildMode.Release : BuildMode.Development;
        }

        private static string[] AppendDefine(string[] defines, string define)
        {
            if (string.IsNullOrWhiteSpace(define))
                throw new BuildFailedException("Build Guard release define symbol must not be empty.");

            if (defines == null || defines.Length == 0)
                return new[] { define };

            if (defines.Contains(define, StringComparer.Ordinal))
                return defines;

            var result = new string[defines.Length + 1];
            Array.Copy(defines, result, defines.Length);
            result[defines.Length] = define;
            return result;
        }
    }
}
