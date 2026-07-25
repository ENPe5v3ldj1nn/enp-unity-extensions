using UnityEngine;

namespace ENP.UnityExtensions.Runtime
{
    /// <summary>
    /// Ready-made window controller — no subclassing required. Drop it on your UI root, set
    /// <c>Windows Root</c>, and mark screens with <c>[UiWindow]</c>; they self-register on start.
    ///
    /// Boot timing:
    ///  • Non-DI: leave <c>Initialize On Awake</c> on — it boots itself.
    ///  • DI: turn it off and let the container adapter call <see cref="Boot"/> at build time,
    ///    so the service is registered before anything resolves it.
    /// </summary>
    [AddComponentMenu("ENP/UI/Window Controller")]
    public class WindowController : AbstractUiController
    {
        [Tooltip("Boot on Awake. Turn OFF when a DI container boots the controller instead.")]
        [SerializeField] private bool _initializeOnAwake = true;

        [Header("Startup")]
        [Tooltip("Window opened right after boot. Leave empty to open none (open manually instead).")]
        [SerializeField] private AnimatedWindow _startupWindow;
        [SerializeField] private WindowTransition _startupTransition = WindowTransition.Fade;

        private bool _booted;

        private void Awake()
        {
            if (_initializeOnAwake)
                Boot();
        }

        /// <summary>Discovers and registers windows. Safe to call once; extra calls are ignored.</summary>
        public void Boot()
        {
            if (_booted)
                return;

            _booted = true;
            Initialize();
            Show(_startupWindow, _startupTransition);
        }
    }
}
