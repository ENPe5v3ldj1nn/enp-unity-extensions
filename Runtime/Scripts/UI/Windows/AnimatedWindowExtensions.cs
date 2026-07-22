using UnityEngine.Events;
using static ENP.UnityExtensions.Runtime.AnimatedWindowAnimation;

namespace ENP.UnityExtensions.Runtime
{
    public static class AnimatedWindowExtensions
    {
        public static void Open(this AnimatedWindow window, AnimatedWindowAnimation animName)
        {
            window.Open(animName.ToString());
        }

        public static void Close(this AnimatedWindow window, AnimatedWindowAnimation animName, UnityAction onComplete)
        {
            window.Close(animName.ToString(), onComplete);
        }

        public static void CloseFast(this AnimatedWindow window, AnimatedWindowAnimation animName, UnityAction onComplete)
        {
            window.Close(animName.ToString(), null);
            onComplete?.Invoke();
        }
    }
}
