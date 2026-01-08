using System.IO;
using enp_unity_extensions.Runtime.Scripts.UI.Form;
using UnityEditor;
using UnityEngine;

namespace enp_unity_extensions.Editor.Form
{
    [CustomEditor(typeof(RoundedShapeGraphic))]
    public sealed class RoundedShapeGraphicEditor : UnityEditor.Editor
    {
        private SerializedProperty _styleProperty;
        private SerializedProperty _preciseRaycastProperty;
        private SerializedProperty _raycastTargetProperty;
        private SerializedProperty _maskableProperty;
        private SerializedProperty _fillGradientAngleSpeedProperty;
        private SerializedProperty _borderGradientAngleSpeedProperty;
        private SerializedProperty _colorProperty;

        private void OnEnable()
        {
            _styleProperty = FindFirst("style", "_style", "m_Style");
            _preciseRaycastProperty = FindFirst("preciseRaycast", "_preciseRaycast", "m_PreciseRaycast");
            _raycastTargetProperty = serializedObject.FindProperty("m_RaycastTarget");
            _maskableProperty = serializedObject.FindProperty("m_Maskable");
            _fillGradientAngleSpeedProperty = serializedObject.FindProperty("_fillGradientAngleSpeed");
            _borderGradientAngleSpeedProperty = serializedObject.FindProperty("_borderGradientAngleSpeed");
            _colorProperty = serializedObject.FindProperty("m_Color");
        }

        private SerializedProperty FindFirst(params string[] names)
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

            if (_styleProperty == null)
            {
                EditorGUILayout.HelpBox("Cannot find serialized field for style. Ensure RoundedShapeGraphic has a [SerializeField] field named 'style' (or update the editor to match your field name).", MessageType.Error);
                DrawDefaultInspector();
                serializedObject.ApplyModifiedProperties();
                return;
            }

            EditorGUILayout.PropertyField(_styleProperty);
            if (_colorProperty != null)
                EditorGUILayout.PropertyField(_colorProperty, new GUIContent("Color"));

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
                    _styleProperty.objectReferenceValue = asset;
                    EditorGUIUtility.PingObject(asset);
                }

                if (GUILayout.Button("Ping Style"))
                {
                    if (_styleProperty.objectReferenceValue != null)
                        EditorGUIUtility.PingObject(_styleProperty.objectReferenceValue);
                }
            }

            EditorGUILayout.Space(8);

            if (_raycastTargetProperty != null)
                EditorGUILayout.PropertyField(_raycastTargetProperty, new GUIContent("Raycast Target"));

            if (_preciseRaycastProperty != null)
                EditorGUILayout.PropertyField(_preciseRaycastProperty);     

            if (_fillGradientAngleSpeedProperty != null)
                EditorGUILayout.PropertyField(_fillGradientAngleSpeedProperty, new GUIContent("Fill Gradient Angle Speed (deg/sec)"));

            if (_borderGradientAngleSpeedProperty != null)
                EditorGUILayout.PropertyField(_borderGradientAngleSpeedProperty, new GUIContent("Border Gradient Angle Speed (deg/sec)"));

            EditorGUILayout.Space(8);

            if (_maskableProperty != null)
                EditorGUILayout.PropertyField(_maskableProperty);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
