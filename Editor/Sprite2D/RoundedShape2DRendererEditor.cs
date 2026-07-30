using ENP.UnityExtensions.Runtime;
using UnityEditor;
using UnityEngine;

namespace ENP.UnityExtensions.Editor
{
    [CustomEditor(typeof(RoundedShape2DRenderer))]
    [CanEditMultipleObjects]
    public sealed class RoundedShape2DRendererEditor : UnityEditor.Editor
    {
        private SerializedProperty _colorProperty;
        private SerializedProperty _sizeProperty;
        private SerializedProperty _styleProperty;
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
            _colorProperty = serializedObject.FindProperty("_color");
            _sizeProperty = serializedObject.FindProperty("_size");
            _styleProperty = serializedObject.FindProperty("_style");
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

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_colorProperty, new GUIContent("Color"));
            EditorGUILayout.PropertyField(_sizeProperty, new GUIContent("Size"));

            EditorGUILayout.PropertyField(_styleProperty, new GUIContent("Style (Optional)"));

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Create Style"))
                {
                    var folder = "Assets/2DShapeStyles";
                    if (!AssetDatabase.IsValidFolder(folder))
                        AssetDatabase.CreateFolder("Assets", "2DShapeStyles");

                    var path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/RoundedShapeStyle.asset");
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

            var hasStyle = !_styleProperty.hasMultipleDifferentValues && _styleProperty.objectReferenceValue != null;
            var style = hasStyle ? (RoundedShapeStyle)_styleProperty.objectReferenceValue : null;

            EditorGUILayout.Space(8);

            EditorGUILayout.PropertyField(_fillGradientAngleSpeedProperty, new GUIContent("Fill Gradient Angle Speed (deg/sec)"));
            EditorGUILayout.PropertyField(_borderGradientAngleSpeedProperty, new GUIContent("Border Gradient Angle Speed (deg/sec)"));

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Gradients", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(!hasStyle))
                EditorGUILayout.PropertyField(_useStyleGradientsProperty, new GUIContent("Use Style Gradients"));
            var useStyleGradients = hasStyle && _useStyleGradientsProperty.boolValue;

            if (!useStyleGradients)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_customFillGradientProperty, new GUIContent("Fill Gradient"));
                EditorGUILayout.PropertyField(_customBorderGradientProperty, new GUIContent("Border Gradient"));
                EditorGUI.indentLevel--;
            }
            else if (style != null)
            {
                EditorGUILayout.LabelField("Fill Gradient: Style", EditorStyles.miniLabel);
                EditorGUILayout.LabelField("Border Gradient: Style", EditorStyles.miniLabel);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.enabled = hasStyle;
                if (GUILayout.Button("Reset Gradients To Style"))
                    _useStyleGradientsProperty.boolValue = true;
                GUI.enabled = true;

                if (GUILayout.Button("Use Custom Gradients"))
                    _useStyleGradientsProperty.boolValue = false;
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Gradient Angles", EditorStyles.boldLabel);

            var useStyleAngles = hasStyle && _useStyleBaseAnglesProperty.boolValue;

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(!hasStyle))
                    EditorGUILayout.PropertyField(_useStyleBaseAnglesProperty, new GUIContent("Use Style Base Angles"));

                if (GUILayout.Button("Use Custom Angles"))
                    _useStyleBaseAnglesProperty.boolValue = false;

                GUI.enabled = hasStyle;
                if (GUILayout.Button("Reset Angles To Style"))
                {
                    _useStyleBaseAnglesProperty.boolValue = true;
                    if (style != null)
                    {
                        _customFillGradientAngleProperty.floatValue = style.FillGradientAngle;
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
                _customFillGradientAngleProperty.floatValue = NormalizeAngle(_customFillGradientAngleProperty.floatValue);
                EditorGUILayout.Slider(_customFillGradientAngleProperty, 0f, 360f, new GUIContent("Fill Gradient Angle"));
                _customBorderGradientAngleProperty.floatValue = NormalizeAngle(_customBorderGradientAngleProperty.floatValue);
                EditorGUILayout.Slider(_customBorderGradientAngleProperty, 0f, 360f, new GUIContent("Border Gradient Angle"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Shape Properties", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(!hasStyle))
                EditorGUILayout.PropertyField(_useStyleShapePropertiesProperty, new GUIContent("Use Style Shape Properties"));

            var useStyleShape = hasStyle && !_useStyleShapePropertiesProperty.hasMultipleDifferentValues && _useStyleShapePropertiesProperty.boolValue;

            if (useStyleShape && style != null)
            {
                EditorGUILayout.LabelField($"Shape (Style): {style.Shape}", EditorStyles.miniLabel);
                EditorGUILayout.LabelField($"Corner Radius (Style): {style.CornerRadius:0.##}", EditorStyles.miniLabel);
                EditorGUILayout.LabelField($"Border Thickness (Style): {style.BorderThickness:0.##}", EditorStyles.miniLabel);
                EditorGUILayout.LabelField(style.ShadowEnabled ? "Shadow: Style (Enabled)" : "Shadow: Style (Disabled)", EditorStyles.miniLabel);
            }

            if (!useStyleShape)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_customShapeProperty);
                EditorGUILayout.PropertyField(_customCornerRadiusProperty);
                EditorGUILayout.PropertyField(_customBorderThicknessProperty);
                EditorGUILayout.PropertyField(_customShadowEnabledProperty);
                if (_customShadowEnabledProperty.boolValue)
                {
                    EditorGUILayout.PropertyField(_customShadowColorProperty);
                    EditorGUILayout.PropertyField(_customShadowOffsetProperty);
                    EditorGUILayout.PropertyField(_customShadowBlurProperty);
                    EditorGUILayout.PropertyField(_customShadowSpreadProperty);
                }
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Sorting", EditorStyles.boldLabel);
            DrawSortingLayerFields();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawSortingLayerFields()
        {
            var renderer = (target as RoundedShape2DRenderer)?.GetComponent<MeshRenderer>();
            if (renderer == null) return;

            var rendererSo = new SerializedObject(renderer);
            rendererSo.Update();

            var sortingLayerProperty = rendererSo.FindProperty("m_SortingLayerID");
            var sortingOrderProperty = rendererSo.FindProperty("m_SortingOrder");

            if (sortingLayerProperty != null)
                DrawSortingLayerPopup(sortingLayerProperty);
            if (sortingOrderProperty != null)
                EditorGUILayout.PropertyField(sortingOrderProperty, new GUIContent("Order in Layer"));

            rendererSo.ApplyModifiedProperties();
        }

        private static void DrawSortingLayerPopup(SerializedProperty sortingLayerIdProperty)
        {
            var layers = SortingLayer.layers;
            var names = new string[layers.Length];
            var currentIndex = 0;
            for (int i = 0; i < layers.Length; i++)
            {
                names[i] = layers[i].name;
                if (layers[i].id == sortingLayerIdProperty.intValue)
                    currentIndex = i;
            }

            var selectedIndex = EditorGUILayout.Popup(new GUIContent("Sorting Layer"), currentIndex, names);
            sortingLayerIdProperty.intValue = layers[selectedIndex].id;
        }

        private static float NormalizeAngle(float angle)
        {
            angle %= 360f;
            if (angle < 0f) angle += 360f;
            return angle;
        }
    }
}
