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

        private SerializedProperty _useStyleGradientsProperty;
        private SerializedProperty _customFillGradientProperty;
        private SerializedProperty _customBorderGradientProperty;

        private SerializedProperty _useStyleBaseAnglesProperty;
        private SerializedProperty _customFillGradientAngleProperty;
        private SerializedProperty _customBorderGradientAngleProperty;

        private SerializedProperty _useStyleShapePropertiesProperty;
        private SerializedProperty _customShapeProperty;
        private SerializedProperty _customCornerRadiusProperty;
        private SerializedProperty _customBorderThicknessProperty;
        private SerializedProperty _customShadowEnabledProperty;
        private SerializedProperty _customShadowColorProperty;
        private SerializedProperty _customShadowOffsetProperty;
        private SerializedProperty _customShadowBlurProperty;
        private SerializedProperty _customShadowSpreadProperty;

        private void OnEnable()
        {
            _styleProperty = FindFirst("style", "_style", "m_Style");
            _preciseRaycastProperty = FindFirst("preciseRaycast", "_preciseRaycast", "m_PreciseRaycast");
            _raycastTargetProperty = serializedObject.FindProperty("m_RaycastTarget");
            _maskableProperty = serializedObject.FindProperty("m_Maskable");
            _fillGradientAngleSpeedProperty = serializedObject.FindProperty("_fillGradientAngleSpeed");
            _borderGradientAngleSpeedProperty = serializedObject.FindProperty("_borderGradientAngleSpeed");

            _useStyleGradientsProperty = serializedObject.FindProperty("_useStyleGradients");
            _customFillGradientProperty = serializedObject.FindProperty("_customFillGradient");
            _customBorderGradientProperty = serializedObject.FindProperty("_customBorderGradient");

            _useStyleBaseAnglesProperty = serializedObject.FindProperty("_useStyleBaseAngles");
            _customFillGradientAngleProperty = serializedObject.FindProperty("_customFillGradientAngle");
            _customBorderGradientAngleProperty = serializedObject.FindProperty("_customBorderGradientAngle");

            _useStyleShapePropertiesProperty = serializedObject.FindProperty("_useStyleShapeProperties");
            _customShapeProperty = serializedObject.FindProperty("_customShape");
            _customCornerRadiusProperty = serializedObject.FindProperty("_customCornerRadius");
            _customBorderThicknessProperty = serializedObject.FindProperty("_customBorderThickness");
            _customShadowEnabledProperty = serializedObject.FindProperty("_customShadowEnabled");
            _customShadowColorProperty = serializedObject.FindProperty("_customShadowColor");
            _customShadowOffsetProperty = serializedObject.FindProperty("_customShadowOffset");
            _customShadowBlurProperty = serializedObject.FindProperty("_customShadowBlur");
            _customShadowSpreadProperty = serializedObject.FindProperty("_customShadowSpread");
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
                EditorGUILayout.HelpBox("Cannot find serialized field for style.", MessageType.Error);
                DrawDefaultInspector();
                serializedObject.ApplyModifiedProperties();
                return;
            }

            EditorGUILayout.PropertyField(_styleProperty);

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

                if (GUILayout.Button("Clear"))
                {
                    _styleProperty.objectReferenceValue = null;
                }
            }

            var hasStyle = _styleProperty.objectReferenceValue != null;
            var style = hasStyle ? (RoundedShapeStyle)_styleProperty.objectReferenceValue : null;

            EditorGUILayout.Space(8);

            if (_raycastTargetProperty != null)
                EditorGUILayout.PropertyField(_raycastTargetProperty, new GUIContent("Raycast Target"));

            if (_preciseRaycastProperty != null)
                EditorGUILayout.PropertyField(_preciseRaycastProperty);

            if (_fillGradientAngleSpeedProperty != null)
                EditorGUILayout.PropertyField(_fillGradientAngleSpeedProperty, new GUIContent("Fill Gradient Angle Speed (deg/sec)"));

            if (_borderGradientAngleSpeedProperty != null)
                EditorGUILayout.PropertyField(_borderGradientAngleSpeedProperty, new GUIContent("Border Gradient Angle Speed (deg/sec)"));

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Gradients", EditorStyles.boldLabel);

            if (_useStyleGradientsProperty != null)
                EditorGUILayout.PropertyField(_useStyleGradientsProperty, new GUIContent("Use Style Gradients"));

            var useStyleGradients = _useStyleGradientsProperty != null && _useStyleGradientsProperty.boolValue;
            if (!hasStyle) useStyleGradients = false;

            if (!useStyleGradients)
            {
                EditorGUI.indentLevel++;
                if (_customFillGradientProperty != null)
                    EditorGUILayout.PropertyField(_customFillGradientProperty, new GUIContent("Fill Gradient"));
                if (_customBorderGradientProperty != null)
                    EditorGUILayout.PropertyField(_customBorderGradientProperty, new GUIContent("Border Gradient"));
                EditorGUI.indentLevel--;
            }
            else
            {
                if (style != null)
                {
                    EditorGUILayout.LabelField("Fill Gradient: Style", EditorStyles.miniLabel);
                    EditorGUILayout.LabelField("Border Gradient: Style", EditorStyles.miniLabel);
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.enabled = hasStyle;
                if (GUILayout.Button("Reset Gradients To Style"))
                {
                    if (_useStyleGradientsProperty != null)
                        _useStyleGradientsProperty.boolValue = true;
                }
                GUI.enabled = true;

                if (GUILayout.Button("Use Custom Gradients"))
                {
                    if (_useStyleGradientsProperty != null)
                        _useStyleGradientsProperty.boolValue = false;
                }
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Gradient Angles", EditorStyles.boldLabel);

            var useStyleAngles = _useStyleBaseAnglesProperty != null && _useStyleBaseAnglesProperty.boolValue;

            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.enabled = hasStyle;
                if (_useStyleBaseAnglesProperty != null)
                    EditorGUILayout.PropertyField(_useStyleBaseAnglesProperty, new GUIContent("Use Style Base Angles"));
                GUI.enabled = true;

                if (GUILayout.Button("Use Custom Angles"))
                {
                    if (_useStyleBaseAnglesProperty != null)
                        _useStyleBaseAnglesProperty.boolValue = false;
                }

                GUI.enabled = hasStyle;
                if (GUILayout.Button("Reset Angles To Style"))
                {
                    if (_useStyleBaseAnglesProperty != null)
                        _useStyleBaseAnglesProperty.boolValue = true;

                    if (style != null)
                    {
                        if (_customFillGradientAngleProperty != null)
                            _customFillGradientAngleProperty.floatValue = style.FillGradientAngle;
                        if (_customBorderGradientAngleProperty != null)
                            _customBorderGradientAngleProperty.floatValue = style.BorderGradientAngle;
                    }
                }
                GUI.enabled = true;
            }

            if (hasStyle && useStyleAngles && style != null)
            {
                EditorGUILayout.LabelField($"Fill Angle (Style): {style.FillGradientAngle:0.##}°", EditorStyles.miniLabel);
                EditorGUILayout.LabelField($"Border Angle (Style): {style.BorderGradientAngle:0.##}°", EditorStyles.miniLabel);
            }

            var showCustomAngles = !hasStyle || !useStyleAngles;
            if (showCustomAngles)
            {
                EditorGUI.indentLevel++;
                if (_customFillGradientAngleProperty != null)
                    _customFillGradientAngleProperty.floatValue = EditorGUILayout.Slider(new GUIContent("Fill Gradient Angle"), NormalizeAngle(_customFillGradientAngleProperty.floatValue), 0f, 360f);
                if (_customBorderGradientAngleProperty != null)
                    _customBorderGradientAngleProperty.floatValue = EditorGUILayout.Slider(new GUIContent("Border Gradient Angle"), NormalizeAngle(_customBorderGradientAngleProperty.floatValue), 0f, 360f);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Shape Properties", EditorStyles.boldLabel);

            if (_useStyleShapePropertiesProperty != null)
            {
                EditorGUILayout.PropertyField(_useStyleShapePropertiesProperty, new GUIContent("Use Style Shape Properties"));
                if (!_useStyleShapePropertiesProperty.boolValue)
                {
                    EditorGUI.indentLevel++;
                    if (_customShapeProperty != null)
                        EditorGUILayout.PropertyField(_customShapeProperty);
                    if (_customCornerRadiusProperty != null)
                        EditorGUILayout.PropertyField(_customCornerRadiusProperty);
                    if (_customBorderThicknessProperty != null)
                        EditorGUILayout.PropertyField(_customBorderThicknessProperty);
                    if (_customShadowEnabledProperty != null)
                        EditorGUILayout.PropertyField(_customShadowEnabledProperty);
                    if (_customShadowEnabledProperty != null && _customShadowEnabledProperty.boolValue)
                    {
                        if (_customShadowColorProperty != null)
                            EditorGUILayout.PropertyField(_customShadowColorProperty);
                        if (_customShadowOffsetProperty != null)
                            EditorGUILayout.PropertyField(_customShadowOffsetProperty);
                        if (_customShadowBlurProperty != null)
                            EditorGUILayout.PropertyField(_customShadowBlurProperty);
                        if (_customShadowSpreadProperty != null)
                            EditorGUILayout.PropertyField(_customShadowSpreadProperty);
                    }
                    EditorGUI.indentLevel--;
                }
            }

            EditorGUILayout.Space(8);

            if (_maskableProperty != null)
                EditorGUILayout.PropertyField(_maskableProperty);

            serializedObject.ApplyModifiedProperties();
        }

        private static float NormalizeAngle(float angle)
        {
            angle %= 360f;
            if (angle < 0f) angle += 360f;
            return angle;
        }
    }
}
