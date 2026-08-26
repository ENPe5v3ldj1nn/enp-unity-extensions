#if UNITY_IOS

using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

namespace ENP.UnityExtensions.Editor
{
    // Links the system frameworks the plugin's own native iOS code needs, since Unity does not
    // auto-link a framework for a plain .mm file or a DllImport. Both required symbols come from
    // Runtime code that always compiles into an iOS build regardless of which optional modules a
    // consuming project uses (NeuroDashTrackingAuthorizationBridge.mm, VibrationController.TriggerIOS).
    internal static class EnpNativeFrameworkLinker
    {
        private const string APP_TRACKING_TRANSPARENCY_FRAMEWORK = "AppTrackingTransparency.framework";
        private const string AUDIO_TOOLBOX_FRAMEWORK = "AudioToolbox.framework";

        private static readonly (string Framework, bool Weak)[] RequiredFrameworks =
        {
            (APP_TRACKING_TRANSPARENCY_FRAMEWORK, true),
            (AUDIO_TOOLBOX_FRAMEWORK, false)
        };

        [PostProcessBuild]
        private static void OnPostProcessBuild(BuildTarget buildTarget, string buildPath)
        {
            string projectPath = PBXProject.GetPBXProjectPath(buildPath);
            PBXProject project = new PBXProject();
            project.ReadFromFile(projectPath);

            string unityFrameworkTargetGuid = project.GetUnityFrameworkTargetGuid();
            foreach (var (framework, weak) in RequiredFrameworks)
            {
                if (!project.ContainsFramework(unityFrameworkTargetGuid, framework))
                {
                    project.AddFrameworkToProject(unityFrameworkTargetGuid, framework, weak);
                }
            }

            project.WriteToFile(projectPath);
        }
    }
}

#endif
