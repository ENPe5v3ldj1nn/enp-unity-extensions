using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using static ENP.UnityExtensions.Runtime.WindowAnimation;

namespace ENP.UnityExtensions.Runtime
{
    /// <summary>
    /// Window stack controller. Auto-discovers <c>[UiWindow]</c> windows under a serialized root
    /// and drives exclusive open/close transitions.
    ///
    /// Two ways to consume it:
    ///  • DI / testable: inject <see cref="IWindowService"/> (this type implements it).
    ///  • Convenience: the static facade (<see cref="ShowExclusive{T}(UnityAction)"/> etc.) forwards
    ///    to the most recently initialized controller. Handy for projects without a container.
    ///
    /// Use the ready-made <see cref="WindowController"/> for drop-in usage, or subclass this for
    /// custom behaviour.
    /// </summary>
    [AddComponentMenu("")]
    public abstract class AbstractUiController : MonoBehaviour, IWindowService
    {
        [Tooltip("Root under which [UiWindow] windows are auto-discovered. " +
                 "Leave empty to scan this controller's own children.")]
        [SerializeField] private Transform _windowsRoot;

        // Static facade target: the last controller that ran Initialize(). Optional convenience for
        // non-DI projects; DI consumers inject IWindowService and never touch this.
        private static AbstractUiController _instance;

        private CancellationTokenSource _transitionCts;

        // Built once on Initialize (no runtime additions), then queried by a fast linear scan.
        // An array of tuples (not a Dictionary) so several windows of the same type can coexist,
        // disambiguated by gameObject name.
        private (Type type, AnimatedWindow window)[] _windows;
        private List<(Type type, AnimatedWindow window)> _building;

        // ---------------------------------------------------------------- IWindowService (instance)

        AnimatedWindow IWindowService.CurrentWindow => WindowHistory.CurrentWindow;
        AnimatedWindow IWindowService.LastWindow => WindowHistory.LastWindow;
        Type IWindowService.CurrentWindowType => WindowHistory.CurrentWindow != null ? WindowHistory.CurrentWindow.GetType() : null;
        Type IWindowService.LastWindowType => WindowHistory.LastWindow != null ? WindowHistory.LastWindow.GetType() : null;

        T IWindowService.GetWindow<T>(string name) => GetWindowImpl<T>(name);
        T IWindowService.ShowExclusive<T>(UnityAction onClose) => ShowExclusiveImpl<T>(WindowDirection.Middle, onClose);
        T IWindowService.ShowExclusive<T>(WindowDirection direction, UnityAction onClose) => ShowExclusiveImpl<T>(direction, onClose);
        T IWindowService.ShowExclusive<T>(string name, WindowDirection direction, UnityAction onClose) => ShowExclusiveImpl<T>(name, direction, onClose);
        void IWindowService.ShowLastWindow(UnityAction onClose) => ShowLastImpl(WindowDirection.Middle, onClose);
        void IWindowService.ShowLastWindow(WindowDirection direction, UnityAction onClose) => ShowLastImpl(direction, onClose);

        // ---------------------------------------------------------------- Static facade (optional)

        public static AnimatedWindow CurrentWindow => WindowHistory.CurrentWindow;
        public static AnimatedWindow LastWindow => WindowHistory.LastWindow;
        public static Type CurrentWindowType => CurrentWindow != null ? CurrentWindow.GetType() : null;
        public static Type LastWindowType => LastWindow != null ? LastWindow.GetType() : null;

        public static T GetWindow<T>(string name = null) where T : AnimatedWindow => Active.GetWindowImpl<T>(name);

        public static T ShowExclusive<T>(UnityAction onClose = null) where T : AnimatedWindow =>
            Active.ShowExclusiveImpl<T>(WindowDirection.Middle, onClose);

        public static T ShowExclusive<T>(WindowDirection direction, UnityAction onClose = null) where T : AnimatedWindow =>
            Active.ShowExclusiveImpl<T>(direction, onClose);

        public static T ShowExclusive<T>(string name, WindowDirection direction = WindowDirection.Middle, UnityAction onClose = null) where T : AnimatedWindow =>
            Active.ShowExclusiveImpl<T>(name, direction, onClose);

        public static void ShowLastWindow(UnityAction onClose = null) => Active.ShowLastImpl(WindowDirection.Middle, onClose);
        public static void ShowLastWindow(WindowDirection direction, UnityAction onClose = null) => Active.ShowLastImpl(direction, onClose);

        private static AbstractUiController Active =>
            _instance != null
                ? _instance
                : throw new InvalidOperationException(
                    "No AbstractUiController is initialized. Call Initialize() on your controller " +
                    "(WindowController does this on Awake, or a DI adapter does it) before using the static window API.");

        // ---------------------------------------------------------------- init / discovery

        protected virtual void Initialize()
        {
            _instance = this;
            _building = new List<(Type, AnimatedWindow)>();
            DiscoverWindows();
            SetupMap(_building);
            _windows = _building.ToArray();
            _building = null;
            WindowHistory.Reset();
        }

        // Auto-registers every AnimatedWindow marked with [UiWindow] under the windows root.
        // Opt-in via attribute so nested sub-views aren't picked up. Inactive windows are
        // included because screens usually start disabled.
        private void DiscoverWindows()
        {
            var root = _windowsRoot != null ? _windowsRoot : transform;
            var found = root.GetComponentsInChildren<AnimatedWindow>(includeInactive: true);
            for (int i = 0; i < found.Length; i++)
            {
                var window = found[i];
                if (Attribute.IsDefined(window.GetType(), typeof(UiWindowAttribute), inherit: false))
                    RegisterWindow(window);
            }
        }

        // Optional manual registration hook. Auto-discovery covers the common case;
        // override only to add windows that live outside the root or aren't attribute-marked.
        protected virtual void SetupMap(List<(Type type, AnimatedWindow window)> windows)
        {
        }

        protected void RegisterWindow(AnimatedWindow window)
        {
            // Idempotent: discovery and a manual SetupMap override may reference the same
            // instance, so skip anything already registered.
            for (int i = 0; i < _building.Count; i++)
            {
                if (ReferenceEquals(_building[i].window, window))
                    return;
            }

            _building.Add((window.GetType(), window));
        }

        protected void CloseAll()
        {
            for (int i = 0; i < _windows.Length; i++)
                _windows[i].window.gameObject.SetActive(false);
        }

        // ---------------------------------------------------------------- instance implementations

        private T ShowExclusiveImpl<T>(WindowDirection direction, UnityAction onClose) where T : AnimatedWindow
        {
            var target = GetWindowImpl<T>(null);
            OpenNext(target, direction, onClose);
            return target;
        }

        private T ShowExclusiveImpl<T>(string name, WindowDirection direction, UnityAction onClose) where T : AnimatedWindow
        {
            var target = GetWindowImpl<T>(name);
            OpenNext(target, direction, onClose);
            return target;
        }

        private void ShowLastImpl(WindowDirection direction, UnityAction onClose)
        {
            var target = WindowHistory.LastWindow;
            if (target == null)
                return;

            OpenNext(target, direction, onClose);
        }

        private T GetWindowImpl<T>(string name) where T : AnimatedWindow => (T)GetWindowInternal(typeof(T), name);

        private AnimatedWindow GetWindowInternal(Type windowType, string name)
        {
            if (windowType == null)
                throw new ArgumentNullException(nameof(windowType));

            AnimatedWindow candidate = null;
            Type candidateType = null;

            for (int i = 0; i < _windows.Length; i++)
            {
                var (type, window) = _windows[i];

                // When a name is provided it disambiguates directly — return the first match.
                if (name != null)
                {
                    if (name == window.gameObject.name)
                        return window;

                    continue;
                }

                // Exact type wins immediately over assignable subtypes.
                if (type == windowType)
                    return window;

                // Only assignable subtypes are ambiguity candidates; unrelated windows are ignored
                // so lookup doesn't fail just because other window types are registered first.
                if (!windowType.IsAssignableFrom(type))
                    continue;

                if (candidate != null)
                    throw new InvalidOperationException($"Multiple windows match requested type {windowType.Name}. Matches: {candidateType.Name}, {type.Name}");

                candidate = window;
                candidateType = type;
            }

            if (candidate == null)
            {
                if (name != null)
                    throw new KeyNotFoundException($"Window of type {windowType.Name} with name '{name}' not registered in {GetType().Name}.");

                throw new KeyNotFoundException($"Window type {windowType.Name} not registered in {GetType().Name}.");
            }

            return candidate;
        }

        protected void OpenNext(AnimatedWindow window, WindowDirection direction, UnityAction onClose = null)
        {
            var (close, open) = ResolveDirection(direction);
            OpenNext(window, close, open, onClose);
        }

        protected void OpenNext(AnimatedWindow window, WindowAnimation close, WindowAnimation open, UnityAction onClose = null)
        {
            _transitionCts?.Cancel();
            _transitionCts?.Dispose();
            _transitionCts = new CancellationTokenSource();

            OpenNextAsync(window, close, open, onClose, _transitionCts.Token).Forget();
        }

        private static async UniTaskVoid OpenNextAsync(AnimatedWindow window, WindowAnimation close,
            WindowAnimation open, UnityAction onClose, CancellationToken token)
        {
            var active = WindowHistory.CurrentWindow;

            if (active != null && active != window)
                WindowHistory.LastWindow = active;

            if (active != null)
                await active.CloseAsync(close, token);

            if (token.IsCancellationRequested)
                return;

            onClose?.Invoke();
            WindowHistory.CurrentWindow = window;
            await window.OpenAsync(open, token);
        }

        private static (WindowAnimation close, WindowAnimation open) ResolveDirection(WindowDirection direction)
        {
            return direction switch
            {
                WindowDirection.Left => (CloseRight, OpenLeft),
                WindowDirection.Right => (CloseLeft, OpenRight),
                WindowDirection.Middle => (CloseMiddle, OpenMiddle),
                WindowDirection.SmoothLeft => (CloseSmoothRight, OpenSmoothLeft),
                WindowDirection.SmoothRight => (CloseSmoothLeft, OpenSmoothRight),
                WindowDirection.PopupCard => (ClosePopupCard, OpenPopupCard),
                _ => (CloseMiddle, OpenMiddle)
            };
        }
    }

    public enum WindowDirection
    {
        Middle,
        Left,
        Right,
        SmoothLeft,
        SmoothRight,
        PopupCard
    }
}
