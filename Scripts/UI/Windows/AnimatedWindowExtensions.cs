using UnityEngine.Events;

namespace enp_unity_extensions.Scripts.UI.Windows
{
    public static class AnimatedWindowExtensions
    {
        public static AnimatedWindow ActiveWindow { get; private set; }
        
        public static void Open(this AnimatedWindow window, AnimatedWindowConstant animName)
        {
            ActiveWindow = window;
            window.Open(animName.ToString());
        }

        public static void Close(this AnimatedWindow window, AnimatedWindowConstant animName, UnityAction onComplete)
        {
            window.Close(animName.ToString(), onComplete);
        }

        public static void CloseFast(this AnimatedWindow window, AnimatedWindowConstant animName, UnityAction onComplete)
        {
            window.Close(animName.ToString(), null);
            onComplete?.Invoke();
        }
    }
}