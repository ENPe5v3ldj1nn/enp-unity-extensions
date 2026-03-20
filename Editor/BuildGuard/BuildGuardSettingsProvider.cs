using System.Collections.Generic;
using UnityEditor;

namespace BuildGuard.Editor
{
    public static class BuildGuardSettingsProvider
    {
        [SettingsProvider]
        public static SettingsProvider CreateProvider()
        {
            return new SettingsProvider("Project/Build Guard", SettingsScope.Project)
            {
                label = "Build Guard",
                guiHandler = _ =>
                {
                    var settings = BuildGuardSettings.instance;

                    EditorGUI.BeginChangeCheck();
                    var askEveryBuild = EditorGUILayout.Toggle("Ask Every Build", settings.AskEveryBuild);
                    var restoreStateAfterBuild = EditorGUILayout.Toggle("Restore State After Build", settings.RestoreStateAfterBuild);
                    var defaultInteractiveMode = (BuildMode)EditorGUILayout.EnumPopup("Default Interactive Mode", settings.DefaultInteractiveMode);
                    var batchModeBuildMode = (BuildMode)EditorGUILayout.EnumPopup("Batch Mode Build Mode", settings.BatchModeBuildMode);
                    var releaseDefineSymbol = EditorGUILayout.TextField("Release Define Symbol", settings.ReleaseDefineSymbol);

                    using (new EditorGUI.DisabledScope(true))
                        EditorGUILayout.EnumPopup("Last Selected Mode", settings.LastSelectedMode);

                    if (!EditorGUI.EndChangeCheck())
                        return;

                    settings.AskEveryBuild = askEveryBuild;
                    settings.RestoreStateAfterBuild = restoreStateAfterBuild;
                    settings.DefaultInteractiveMode = defaultInteractiveMode;
                    settings.BatchModeBuildMode = batchModeBuildMode;
                    settings.ReleaseDefineSymbol = releaseDefineSymbol;
                },
                keywords = new HashSet<string>(new[] { "Build", "Guard", "Release", "Development" })
            };
        }
    }
}
