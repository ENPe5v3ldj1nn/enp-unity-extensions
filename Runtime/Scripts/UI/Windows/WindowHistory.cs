namespace ENP.UnityExtensions.Runtime.Scripts.UI.Windows
{
    public static class WindowHistory
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
