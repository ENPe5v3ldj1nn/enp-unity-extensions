namespace enp_unity_extensions.Runtime.Scripts.UI.Windows
{
    public static class AnimatedWindowHistory
    {
        public static AnimatedWindow CurrentWindow { get; internal set; }
        public static AnimatedWindow LastWindow { get; internal set; }

        internal static void Reset()
        {
            CurrentWindow = null;
            LastWindow = null;
        }
    }
}
