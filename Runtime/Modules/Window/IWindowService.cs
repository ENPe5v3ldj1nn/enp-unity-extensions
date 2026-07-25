using System;
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
        Type CurrentWindowType { get; }
        Type LastWindowType { get; }

        T GetWindow<T>(string name = null) where T : AnimatedWindow;

        T ShowExclusive<T>(UnityAction onClose = null) where T : AnimatedWindow;
        T ShowExclusive<T>(WindowDirection direction, UnityAction onClose = null) where T : AnimatedWindow;
        T ShowExclusive<T>(string name, WindowDirection direction = WindowDirection.Middle, UnityAction onClose = null) where T : AnimatedWindow;

        void ShowLastWindow(UnityAction onClose = null);
        void ShowLastWindow(WindowDirection direction, UnityAction onClose = null);
    }
}
