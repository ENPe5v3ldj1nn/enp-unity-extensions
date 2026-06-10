using ENP.UnityExtensions.Runtime.Scripts.UI;
using UnityEditor;
using UnityEngine;

namespace ENP.UnityExtensions.Editor.UI
{
    [CustomEditor(typeof(RoundedSlider))]
    public sealed class RoundedSliderEditor : UnityEditor.Editor
    {
        private SerializedProperty _backgroundProperty;
        private SerializedProperty _foregroundProperty;
        private SerializedProperty _handleProperty;
        private SerializedProperty _minValueProperty;
        private SerializedProperty _maxValueProperty;
        private SerializedProperty _valueProperty;
        private SerializedProperty _wholeNumbersProperty;
        private SerializedProperty _interactableProperty;
        private SerializedProperty _directionProperty;
        private SerializedProperty _leftPaddingProperty;
        private SerializedProperty _rightPaddingProperty;
        private SerializedProperty _topPaddingProperty;
        private SerializedProperty _bottomPaddingProperty;
        private SerializedProperty _driveHandleProperty;
        private SerializedProperty _handleOffsetProperty;
        private SerializedProperty _onValueChangedProperty;

        private void OnEnable()
        {
            _backgroundProperty = serializedObject.FindProperty("_background");
            _foregroundProperty = serializedObject.FindProperty("_foreground");
            _handleProperty = serializedObject.FindProperty("_handle");
            _minValueProperty = serializedObject.FindProperty("_minValue");
            _maxValueProperty = serializedObject.FindProperty("_maxValue");
            _valueProperty = serializedObject.FindProperty("_value");
            _wholeNumbersProperty = serializedObject.FindProperty("_wholeNumbers");
            _interactableProperty = serializedObject.FindProperty("_interactable");
            _directionProperty = serializedObject.FindProperty("_direction");
            _leftPaddingProperty = serializedObject.FindProperty("_leftPadding");
            _rightPaddingProperty = serializedObject.FindProperty("_rightPadding");
            _topPaddingProperty = serializedObject.FindProperty("_topPadding");
            _bottomPaddingProperty = serializedObject.FindProperty("_bottomPadding");
            _driveHandleProperty = serializedObject.FindProperty("_driveHandle");
            _handleOffsetProperty = serializedObject.FindProperty("_handleOffset");
            _onValueChangedProperty = serializedObject.FindProperty("_onValueChanged");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_backgroundProperty);
            EditorGUILayout.PropertyField(_foregroundProperty);
            EditorGUILayout.PropertyField(_handleProperty);

            EditorGUILayout.Space(4f);
            EditorGUILayout.PropertyField(_interactableProperty);
            EditorGUILayout.PropertyField(_directionProperty);
            EditorGUILayout.PropertyField(_wholeNumbersProperty);
            EditorGUILayout.PropertyField(_minValueProperty);
            EditorGUILayout.PropertyField(_maxValueProperty);

            DrawValueSlider();

            EditorGUILayout.Space(4f);
            EditorGUILayout.PropertyField(_leftPaddingProperty);
            EditorGUILayout.PropertyField(_rightPaddingProperty);
            EditorGUILayout.PropertyField(_topPaddingProperty);
            EditorGUILayout.PropertyField(_bottomPaddingProperty);
            EditorGUILayout.PropertyField(_driveHandleProperty);
            EditorGUILayout.PropertyField(_handleOffsetProperty);

            EditorGUILayout.Space(4f);
            EditorGUILayout.PropertyField(_onValueChangedProperty);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawValueSlider()
        {
            var minValue = _minValueProperty.floatValue;
            var maxValue = _maxValueProperty.floatValue;
            if (maxValue < minValue)
                maxValue = minValue;

            EditorGUI.BeginChangeCheck();
            var value = EditorGUILayout.Slider(new GUIContent("Value"), _valueProperty.floatValue, minValue, maxValue);
            if (EditorGUI.EndChangeCheck())
            {
                if (_wholeNumbersProperty.boolValue)
                    value = Mathf.Round(value);

                _valueProperty.floatValue = Mathf.Clamp(value, minValue, maxValue);
            }
        }
    }
}
