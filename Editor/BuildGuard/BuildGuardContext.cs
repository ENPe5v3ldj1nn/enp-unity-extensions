namespace BuildGuard.Editor
{
    public sealed class BuildGuardContext
    {
        public BuildGuardContext(BuildMode mode, UnityEditor.BuildPlayerOptions options, BuildGuardSettings settings)
        {
            Mode = mode;
            Options = options;
            Settings = settings;
        }

        public BuildMode Mode { get; }
        public UnityEditor.BuildPlayerOptions Options { get; }
        public BuildGuardSettings Settings { get; }
    }
}
