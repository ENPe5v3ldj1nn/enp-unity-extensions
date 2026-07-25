using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using static ENP.UnityExtensions.Runtime.WindowAnimation;

namespace ENP.UnityExtensions.Runtime
{
    [AddComponentMenu("")]
    public abstract class AbstractUiController : MonoBehaviour, IWindowService
    {
        [Tooltip("Root under which [UiWindow] windows are auto-discovered. " +
                 "Leave empty to scan this controller's own children.")]
        [SerializeField] private Transform _windowsRoot;

        private static AbstractUiController _instance;

        private CancellationTokenSource _transitionCts;
        private (Type type, AnimatedWindow window)[] _windows;

        AnimatedWindow IWindowService.CurrentWindow => WindowHistory.CurrentWindow;
        AnimatedWindow IWindowService.LastWindow => WindowHistory.LastWindow;

        T IWindowService.GetWindow<T>(string name) => GetWindowImpl<T>(name);
        T IWindowService.ShowExclusive<T>(WindowDirection direction, UnityAction onClose) => ShowExclusiveImpl<T>(direction, onClose);
        void IWindowService.ShowLastWindow(WindowDirection direction, UnityAction onClose) => ShowLastImpl(direction, onClose);

        public static AnimatedWindow CurrentWindow => WindowHistory.CurrentWindow;
        public static AnimatedWindow LastWindow => WindowHistory.LastWindow;

        public static T GetWindow<T>(string name = null) where T : AnimatedWindow => Active.GetWindowImpl<T>(name);

        public static T ShowExclusive<T>(WindowDirection direction, UnityAction onClose = null) where T : AnimatedWindow =>
            Active.ShowExclusiveImpl<T>(direction, onClose);

        public static void ShowLastWindow(WindowDirection direction, UnityAction onClose = null) => Active.ShowLastImpl(direction, onClose);

        private static AbstractUiController Active =>
            _instance != null
                ? _instance
                : throw new InvalidOperationException(
                    "No AbstractUiController is initialized. Boot your WindowController before using the static window API.");

        protected virtual void Initialize()
        {
            _instance = this;
            _windows = DiscoverWindows();
            WindowHistory.Reset();
            CloseAll();
        }

        private (Type type, AnimatedWindow window)[] DiscoverWindows()
        {
            var root = _windowsRoot != null ? _windowsRoot : transform;
            var found = root.GetComponentsInChildren<AnimatedWindow>(includeInactive: true);
            var list = new List<(Type, AnimatedWindow)>(found.Length);

            for (int i = 0; i < found.Length; i++)
            {
                var window = found[i];
                if (Attribute.IsDefined(window.GetType(), typeof(UiWindowAttribute), inherit: false))
                    list.Add((window.GetType(), window));
            }

            return list.ToArray();
        }

        protected void CloseAll()
        {
            for (int i = 0; i < _windows.Length; i++)
                _windows[i].window.gameObject.SetActive(false);
        }

        private T ShowExclusiveImpl<T>(WindowDirection direction, UnityAction onClose) where T : AnimatedWindow
        {
            var target = GetWindowImpl<T>(null);
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

                if (name != null)
                {
                    if (name == window.gameObject.name)
                        return window;

                    continue;
                }

                if (type == windowType)
                    return window;

                if (!windowType.IsAssignableFrom(type))
                    continue;

                if (candidate != null)
                    throw new InvalidOperationException($"Multiple windows match requested type {windowType.Name}. Matches: {candidateType.Name}, {type.Name}");

                candidate = window;
                candidateType = type;
            }

            if (candidate == null)
                throw new KeyNotFoundException($"Window type {windowType.Name} not registered in {GetType().Name}.");

            return candidate;
        }

        private void OpenNext(AnimatedWindow window, WindowDirection direction, UnityAction onClose = null)
        {
            _transitionCts?.Cancel();
            _transitionCts?.Dispose();
            _transitionCts = new CancellationTokenSource();

            var (close, open) = ResolveDirection(direction);
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
