using UnityEngine.Events;

namespace ENP.UnityExtensions.Runtime
{
    /// <summary>
    /// Instance-facing window navigation API. Inject this (e.g. via a DI container) instead of
    /// reaching for the static <see cref="AbstractUiController"/> facade. Implemented by
    /// <see cref="AbstractUiController"/> / the ready-made <see cref="WindowController"/>.
    /// </summary>
    public interface IWindowService
    {
        AnimatedWindow CurrentWindow { get; }
        AnimatedWindow LastWindow { get; }

        /// <summary>Resolves a registered window without opening it. Optionally disambiguate by GameObject name.</summary>
        T GetWindow<T>(string name = null) where T : AnimatedWindow;

        /// <summary>
        /// Closes the current window and opens <typeparamref name="T"/> with the given transition.
        /// Pass <paramref name="name"/> (matched against GameObject name) to pick a specific instance
        /// when several windows share the type.
        /// </summary>
        T ShowExclusive<T>(WindowTransition transition, UnityAction onClose = null, string name = null) where T : AnimatedWindow;

        /// <summary>Re-opens the previously shown window, if any.</summary>
        void ShowLastWindow(WindowTransition transition, UnityAction onClose = null);
    }
}
