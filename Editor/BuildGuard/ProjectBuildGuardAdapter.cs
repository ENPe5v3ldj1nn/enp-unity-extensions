using BuildGuard.Editor;
using enp_unity_extensions.Runtime.Scripts.Language;
using UnityEditor;
using UnityEditor.Build;

public sealed class ProjectBuildGuardAdapter : IBuildGuardProjectAdapter
{
    private const string _dotweenSettingsPath = "Assets/Resources/DOTweenSettings.asset";

    private bool _dIsCanLog;
    private bool _languageIsCanLog;
    private bool _dotweenDebugMode;
    private bool _dotweenDebugStoreTargetId;
    private bool _snapshotCaptured;

    public int Order => 0;

    public void Validate(BuildGuardContext context)
    {
        var dotweenSettings = AssetDatabase.LoadMainAssetAtPath(_dotweenSettingsPath);
        if (dotweenSettings == null)
            throw new BuildFailedException($"Required DOTween settings asset was not found at '{_dotweenSettingsPath}'.");

        var serializedObject = new SerializedObject(dotweenSettings);
        if (serializedObject.FindProperty("debugMode") == null)
            throw new BuildFailedException("DOTween settings asset does not expose 'debugMode'.");

        if (serializedObject.FindProperty("debugStoreTargetId") == null)
            throw new BuildFailedException("DOTween settings asset does not expose 'debugStoreTargetId'.");
    }

    public void Apply(BuildGuardContext context)
    {
        CaptureSnapshot();

        if (context.Mode != BuildMode.Release)
            return;

        D.IsCanLog = false;
        LanguageController.IsCanLog = false;

        var dotweenSettings = AssetDatabase.LoadMainAssetAtPath(_dotweenSettingsPath);
        var serializedObject = new SerializedObject(dotweenSettings);
        SetBoolean(serializedObject.FindProperty("debugMode"), false);
        SetBoolean(serializedObject.FindProperty("debugStoreTargetId"), false);
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(dotweenSettings);
        AssetDatabase.SaveAssets();
    }

    public void Restore(BuildGuardContext context)
    {
        D.IsCanLog = _dIsCanLog;
        LanguageController.IsCanLog = _languageIsCanLog;

        var dotweenSettings = AssetDatabase.LoadMainAssetAtPath(_dotweenSettingsPath);
        var serializedObject = new SerializedObject(dotweenSettings);
        SetBoolean(serializedObject.FindProperty("debugMode"), _dotweenDebugMode);
        SetBoolean(serializedObject.FindProperty("debugStoreTargetId"), _dotweenDebugStoreTargetId);
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(dotweenSettings);
        AssetDatabase.SaveAssets();
    }

    private void CaptureSnapshot()
    {
        if (_snapshotCaptured)
            return;

        _dIsCanLog = D.IsCanLog;
        _languageIsCanLog = LanguageController.IsCanLog;

        var dotweenSettings = AssetDatabase.LoadMainAssetAtPath(_dotweenSettingsPath);
        var serializedObject = new SerializedObject(dotweenSettings);
        _dotweenDebugMode = GetBoolean(serializedObject.FindProperty("debugMode"));
        _dotweenDebugStoreTargetId = GetBoolean(serializedObject.FindProperty("debugStoreTargetId"));
        _snapshotCaptured = true;
    }

    private static bool GetBoolean(SerializedProperty property)
    {
        return property.propertyType == SerializedPropertyType.Boolean ? property.boolValue : property.intValue != 0;
    }

    private static void SetBoolean(SerializedProperty property, bool value)
    {
        if (property.propertyType == SerializedPropertyType.Boolean)
        {
            property.boolValue = value;
            return;
        }

        property.intValue = value ? 1 : 0;
    }
}
