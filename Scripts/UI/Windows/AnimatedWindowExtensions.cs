using UnityEngine.Events;

namespace enp_unity_extensions.Scripts.UI.Windows
{
    public static class AnimatedWindowExtensions
    {
        public static AnimatedWindow ActiveWindow { get; private set; }
        
        public static void Open(this AnimatedWindow window, string animName)
        {
            ActiveWindow = window;
            window.Open(animName);
        }

        public static void Close(this AnimatedWindow window, string animName, UnityAction onComplete)
        {
            window.Close(animName, onComplete);
        }

        public static void CloseFast(this AnimatedWindow window, string animName, UnityAction onComplete)
        {
            window.Close(animName, null);
            onComplete?.Invoke();
        }
    }
}