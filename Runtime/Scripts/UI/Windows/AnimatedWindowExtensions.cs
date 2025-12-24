using UnityEngine.Events;
using static enp_unity_extensions.Runtime.Scripts.UI.Windows.AnimatedWindowConstant;

namespace enp_unity_extensions.Runtime.Scripts.UI.Windows
{
    public static class AnimatedWindowExtensions
    {
        public static void Open(this AnimatedWindow window, AnimatedWindowConstant animName)
        {
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
