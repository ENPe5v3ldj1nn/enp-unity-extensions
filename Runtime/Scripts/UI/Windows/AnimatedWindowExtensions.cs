using UnityEngine.Events;
using static enp_unity_extensions.Runtime.Scripts.UI.Windows.AnimatedWindowConstant;

namespace enp_unity_extensions.Runtime.Scripts.UI.Windows
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
        
        public static void OpenNext(AnimatedWindow window, AnimatedWindowConstant close = CloseMiddle, AnimatedWindowConstant open = OpenMiddle, UnityAction onClose = null)
        {
            ActiveWindow.Close(close, () =>
            {
                onClose?.Invoke();
                window.Open(open);
            });
        }
    }
}