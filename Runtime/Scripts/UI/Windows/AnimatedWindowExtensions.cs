using UnityEngine.Events;

namespace ENP.UnityExtensions.Runtime
{
    public static class AnimatedWindowExtensions
    {
        /// <summary>
        /// Starts the close animation but does not wait for it — invokes onComplete immediately.
        /// Use when the caller must proceed without waiting for the exit tween.
        /// </summary>
        public static void CloseFast(this AnimatedWindow window, AnimatedWindowAnimation animName, UnityAction onComplete)
        {
            window.Close(animName, null);
            onComplete?.Invoke();
        }
    }
}
