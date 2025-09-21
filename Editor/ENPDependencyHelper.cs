// Auto-checker for optional dependencies (DOTween, UniRx)
#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ENP.EditorTools
{
    [InitializeOnLoad]
    public static class ENPDependencyHelper
    {
        const string MenuPath = "ENP/Check Dependencies";
        const string PrefsKeySilenced = "ENP_DependencyHelper_Silenced";

        static ENPDependencyHelper()
        {
            EditorApplication.update += DelayedCheck;
        }

        [MenuItem(MenuPath, priority = 10)]
        public static void ManualCheck() => Check(showDialogEvenIfAllOk: true);

        static void DelayedCheck()
        {
            EditorApplication.update -= DelayedCheck;
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            if (EditorPrefs.GetBool(PrefsKeySilenced, false)) return;
            Check(showDialogEvenIfAllOk: false);
        }

        static void Check(bool showDialogEvenIfAllOk)
        {
            bool hasDoTween = TypeExists("DG.Tweening.DOTween") || AssemblyExists("DOTween");
            bool hasUniRx  = TypeExists("UniRx.Unit") || AssemblyExists("UniRx");

            if (hasDoTween && hasUniRx)
            {
                if (showDialogEvenIfAllOk)
                    EditorUtility.DisplayDialog("ENP Dependencies", "All good! DOTween and UniRx are present.", "OK");
                return;
            }

            string msg = "Some optional dependencies are missing:\n";
            if (!hasDoTween) msg += "• DOTween (Demigiant)\n";
            if (!hasUniRx)  msg += "• UniRx\n";
            msg += "\nInstall them now?\n(You can run this later from ENP/Check Dependencies)";

            int choice = EditorUtility.DisplayDialogComplex(
                "ENP Dependencies",
                msg,
                "Get DOTween",
                "Ignore",
                "Get UniRx"
            );

            if (choice == 0) // Get DOTween
            {
                Application.OpenURL("https://assetstore.unity.com/packages/tools/animation/dotween-hotween-v2-27676");
            }
            else if (choice == 2) // Get UniRx
            {
                Application.OpenURL("https://openupm.com/packages/com.neuecc.unirx/");
            }
            else
            {
                bool silence = EditorUtility.DisplayDialog("ENP Dependencies", "Hide future dependency prompts for this project?", "Yes, hide", "No");
                if (silence) EditorPrefs.SetBool(PrefsKeySilenced, true);
            }
        }

        static bool TypeExists(string fullName)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    if (asm.GetType(fullName, throwOnError: false) != null)
                        return true;
                }
                catch { /* ignore */ }
            }
            return false;
        }

        static bool AssemblyExists(string name)
        {
            return AppDomain.CurrentDomain.GetAssemblies().Any(a =>
            {
                try { return a.GetName().Name.Equals(name, StringComparison.OrdinalIgnoreCase); }
                catch { return false; }
            });
        }
    }
}
#endif
