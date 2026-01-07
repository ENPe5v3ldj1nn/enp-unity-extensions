using System.IO;
using enp_unity_extensions.Runtime.Scripts.UI.Form;
using UnityEditor;
using UnityEngine;

namespace enp_unity_extensions.Editor.Form
{
    [CustomEditor(typeof(RoundedShapeGraphic))]
    public sealed class RoundedShapeGraphicEditor : UnityEditor.Editor
    {
        SerializedProperty style;
        SerializedProperty preciseRaycast;
        SerializedProperty raycastTarget;
        SerializedProperty maskable;

        void OnEnable()
        {
            style = FindFirst("style", "_style", "m_Style");
            preciseRaycast = FindFirst("preciseRaycast", "_preciseRaycast", "m_PreciseRaycast");
            raycastTarget = serializedObject.FindProperty("m_RaycastTarget");
            maskable = serializedObject.FindProperty("m_Maskable");
        }

        SerializedProperty FindFirst(params string[] names)
        {
            for (int i = 0; i < names.Length; i++)
            {
                var p = serializedObject.FindProperty(names[i]);
                if (p != null) return p;
            }
            return null;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (style == null)
            {
                EditorGUILayout.HelpBox("Cannot find serialized field for style. Ensure RoundedShapeGraphic has a [SerializeField] field named 'style' (or update the editor to match your field name).", MessageType.Error);
                DrawDefaultInspector();
                serializedObject.ApplyModifiedProperties();
                return;
            }

            EditorGUILayout.PropertyField(style);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Create Style"))
                {
                    var folder = "Assets/UIStyles";
                    if (!AssetDatabase.IsValidFolder(folder))
                        AssetDatabase.CreateFolder("Assets", "UIStyles");

                    var path = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(folder, "RoundedShapeStyle.asset"));
                    var asset = ScriptableObject.CreateInstance<RoundedShapeStyle>();
                    AssetDatabase.CreateAsset(asset, path);
                    AssetDatabase.SaveAssets();
                    style.objectReferenceValue = asset;
                    EditorGUIUtility.PingObject(asset);
                }

                if (GUILayout.Button("Ping Style"))
                {
                    if (style.objectReferenceValue != null)
                        EditorGUIUtility.PingObject(style.objectReferenceValue);
                }
            }

            EditorGUILayout.Space(8);

            if (raycastTarget != null)
                EditorGUILayout.PropertyField(raycastTarget, new GUIContent("Raycast Target"));

            if (preciseRaycast != null)
                EditorGUILayout.PropertyField(preciseRaycast);

            EditorGUILayout.Space(8);

            if (maskable != null)
                EditorGUILayout.PropertyField(maskable);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
