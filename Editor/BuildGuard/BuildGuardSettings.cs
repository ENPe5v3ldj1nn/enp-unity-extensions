using UnityEditor;
using UnityEngine;

namespace BuildGuard.Editor
{
    [FilePath("ProjectSettings/BuildGuardSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public sealed class BuildGuardSettings : ScriptableSingleton<BuildGuardSettings>
    {
        [SerializeField] private bool _askEveryBuild = true;
        [SerializeField] private bool _restoreStateAfterBuild = true;
        [SerializeField] private BuildMode _defaultInteractiveMode = BuildMode.Development;
        [SerializeField] private BuildMode _batchModeBuildMode = BuildMode.Release;
        [SerializeField] private string _releaseDefineSymbol = "APP_BUILD_RELEASE";
        [SerializeField] private BuildMode _lastSelectedMode = BuildMode.Development;

        public bool AskEveryBuild
        {
            get => _askEveryBuild;
            set
            {
                _askEveryBuild = value;
                Save(true);
            }
        }

        public bool RestoreStateAfterBuild
        {
            get => _restoreStateAfterBuild;
            set
            {
                _restoreStateAfterBuild = value;
                Save(true);
            }
        }

        public BuildMode DefaultInteractiveMode
        {
            get => _defaultInteractiveMode;
            set
            {
                _defaultInteractiveMode = value;
                Save(true);
            }
        }

        public BuildMode BatchModeBuildMode
        {
            get => _batchModeBuildMode;
            set
            {
                _batchModeBuildMode = value;
                Save(true);
            }
        }

        public string ReleaseDefineSymbol
        {
            get => _releaseDefineSymbol;
            set
            {
                _releaseDefineSymbol = value;
                Save(true);
            }
        }

        public BuildMode LastSelectedMode
        {
            get => _lastSelectedMode;
            set
            {
                _lastSelectedMode = value;
                Save(true);
            }
        }
    }
}
