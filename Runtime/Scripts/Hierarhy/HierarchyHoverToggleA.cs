#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace enp_unity_extensions.Runtime.Scripts.Hierarhy
{
    [InitializeOnLoad]
    internal static class HierarchyHoverToggleA
    {
        private static GameObject _hovered;

        static HierarchyHoverToggleA()
        {
            EditorApplication.hierarchyWindowItemOnGUI -= OnHierarchyGUI;
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
        }

        private static void OnHierarchyGUI(int instanceId, Rect selectionRect)
        {
            var evt = Event.current;
            if (evt == null) return;

            if (evt.type == EventType.Layout)
                _hovered = null;

            var go = EditorUtility.InstanceIDToObject(instanceId) as GameObject;
            if (!go) return;

            if (selectionRect.Contains(evt.mousePosition) &&
                (evt.type == EventType.Repaint || evt.type == EventType.MouseMove))
                _hovered = go;

            if (evt.type == EventType.KeyDown &&
                evt.keyCode == KeyCode.A &&
                evt.modifiers == EventModifiers.None &&
                _hovered)
            {
                ToggleHoveredTargets();
                evt.Use();
            }
        }

        private static void ToggleHoveredTargets()
        {
            if (!_hovered) return;

            var targets = Selection.gameObjects.Contains(_hovered)
                ? Selection.gameObjects
                : new[] { _hovered };

            bool enable = !targets.Any(t => t.activeSelf);

            Undo.RecordObjects(targets.Cast<Object>().ToArray(), "Toggle Active State");

            foreach (var go in targets)
            {
                go.SetActive(enable);
                EditorUtility.SetDirty(go);
            }

            EditorApplication.RepaintHierarchyWindow();
        }
    }
}
#endif
