using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ENP.UnityExtensions.Editor.TimeScaleToolbar
{
    [InitializeOnLoad]
    internal static class TimeScaleToolbarInjector
    {
        private const string RootContainerName = "ENP.UnityExtensions.TimeScaleToolbar.Root";
        private const string PlayModeZoneName = "ToolbarZonePlayMode";

        private static readonly Type ToolbarType = Type.GetType("UnityEditor.Toolbar, UnityEditor");
        private static readonly FieldInfo ToolbarRootField = ToolbarType != null
            ? ToolbarType.GetField("m_Root", BindingFlags.NonPublic | BindingFlags.Instance)
            : null;

        private static bool _attachmentScheduled;
        private static VisualElement _host;

        static TimeScaleToolbarInjector()
        {
            EditorApplication.delayCall += TryAttach;
            AssemblyReloadEvents.afterAssemblyReload += HandleAfterAssemblyReload;
        }

        private static void HandleAfterAssemblyReload()
        {
            ScheduleAttach();
        }

        private static void ScheduleAttach()
        {
            if (_attachmentScheduled)
            {
                return;
            }

            _attachmentScheduled = true;
            EditorApplication.delayCall += TryAttach;
        }

        private static void TryAttach()
        {
            _attachmentScheduled = false;

            if (_host != null && _host.panel != null)
            {
                return;
            }

            if (ToolbarType == null || ToolbarRootField == null)
            {
                return;
            }

            var toolbars = Resources.FindObjectsOfTypeAll(ToolbarType);
            if (toolbars == null || toolbars.Length == 0)
            {
                ScheduleAttach();
                return;
            }

            for (var i = 0; i < toolbars.Length; i++)
            {
                var toolbar = toolbars[i];
                var root = ToolbarRootField.GetValue(toolbar) as VisualElement;
                if (root == null)
                {
                    continue;
                }

                var playModeZone = root.Q<VisualElement>(PlayModeZoneName);
                if (playModeZone == null)
                {
                    continue;
                }

                var host = playModeZone.Q<VisualElement>(RootContainerName);
                if (host == null)
                {
                    host = new VisualElement
                    {
                        name = RootContainerName
                    };

                    host.style.flexDirection = FlexDirection.Row;
                    host.style.alignItems = Align.Center;
                    host.style.flexShrink = 0f;
                    host.style.marginLeft = 4f;
                    host.style.marginRight = 4f;

                    playModeZone.Add(host);
                }

                if (host.childCount == 0)
                {
                    host.Add(new TimeScaleToolbarElementView());
                }

                _host = host;
                return;
            }

            ScheduleAttach();
        }
    }
}
