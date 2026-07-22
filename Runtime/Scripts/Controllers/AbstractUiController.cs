using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using static ENP.UnityExtensions.Runtime.AnimatedWindowAnimation;

namespace ENP.UnityExtensions.Runtime
{
    [AddComponentMenu("")]
    public abstract class AbstractUiController : MonoBehaviour
    {
        private static AbstractUiController _instance;
        private static CancellationTokenSource _transitionCts;

        // Built once on Initialize (no runtime additions), then queried by a fast linear scan.
        // An array of tuples (not a Dictionary) so several windows of the same type can coexist,
        // disambiguated by gameObject name.
        private (Type type, AnimatedWindow window)[] _windows;
        private List<(Type type, AnimatedWindow window)> _building;

        public static AnimatedWindow CurrentWindow
        {
            get => WindowHistory.CurrentWindow;
            private set => WindowHistory.CurrentWindow = value;
        }

        public static AnimatedWindow LastWindow
        {
            get => WindowHistory.LastWindow;
            private set => WindowHistory.LastWindow = value;
        }

        public static Type CurrentWindowType => CurrentWindow != null ? CurrentWindow.GetType() : null;
        public static Type LastWindowType => LastWindow != null ? LastWindow.GetType() : null;

        protected virtual void Initialize()
        {
            _instance = this;
            _building = new List<(Type, AnimatedWindow)>();
            SetupMap(_building);
            _windows = _building.ToArray();
            _building = null;
            WindowHistory.Reset();
        }

        protected abstract void SetupMap(List<(Type type, AnimatedWindow window)> windows);

        protected void RegisterWindow(AnimatedWindow window)
        {
            _building.Add((window.GetType(), window));
        }

        protected void CloseAll()
        {
            for (int i = 0; i < _windows.Length; i++)
                _windows[i].window.gameObject.SetActive(false);
        }

        public static T ShowExclusive<T>(UnityAction onClose = null) where T : AnimatedWindow
        {
            return ShowExclusive<T>(WindowDirection.Middle, onClose);
        }

        public static T ShowExclusive<T>(WindowDirection direction, UnityAction onClose = null) where T : AnimatedWindow
        {
            var target = GetWindow<T>();
            OpenNext(target, direction, onClose);
            return target;
        }

        public static T ShowExclusive<T>(string name, WindowDirection direction = WindowDirection.Middle, UnityAction onClose = null) where T : AnimatedWindow
        {
            var target = GetWindow<T>(name);
            OpenNext(target, direction, onClose);
            return target;
        }

        public static void ShowLastWindow(UnityAction onClose = null)
        {
            ShowLastWindow(WindowDirection.Middle, onClose);
        }

        public static void ShowLastWindow(WindowDirection direction, UnityAction onClose = null)
        {
            var target = LastWindow;
            if (target == null)
                return;
            
            OpenNext(target, direction, onClose);
        }

        public static T GetWindow<T>(string name = null) where T : AnimatedWindow
        {
            return (T)GetWindowInternal(typeof(T), name);
        }

        private static AnimatedWindow GetWindowInternal(Type windowType, string name = null)
        {
            if (windowType == null)
                throw new ArgumentNullException(nameof(windowType));

            var windows = _instance._windows;

            AnimatedWindow candidate = null;
            Type candidateType = null;

            for (int i = 0; i < windows.Length; i++)
            {
                var (type, window) = windows[i];
                
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

                if (candidate != null)
                    throw new InvalidOperationException($"Multiple windows match requested type {windowType.Name}. Matches: {candidateType.Name}, {type.Name}");

                candidate = window;
                candidateType = type;
            }

            if (candidate == null)
            {
                if (name != null)
                    throw new KeyNotFoundException($"Window of type {windowType.Name} with name '{name}' not registered in {_instance.GetType().Name}.");

                throw new KeyNotFoundException($"Window type {windowType.Name} not registered in {_instance.GetType().Name}.");
            }

            return candidate;
        }

        protected static void OpenNext(AnimatedWindow window, WindowDirection direction, UnityAction onClose = null)
        {
            var (close, open) = ResolveDirection(direction);
            OpenNext(window, close, open, onClose);
        }

        protected static void OpenNext(AnimatedWindow window, AnimatedWindowAnimation close, AnimatedWindowAnimation open, UnityAction onClose = null)
        {
            _transitionCts?.Cancel();
            _transitionCts?.Dispose();
            _transitionCts = new CancellationTokenSource();

            OpenNextAsync(window, close, open, onClose, _transitionCts.Token).Forget();
        }

        private static async UniTaskVoid OpenNextAsync(AnimatedWindow window, AnimatedWindowAnimation close,
            AnimatedWindowAnimation open, UnityAction onClose, CancellationToken token)
        {
            var active = CurrentWindow;

            if (active != null && active != window)
                LastWindow = active;

            if (active != null)
                await active.CloseAsync(close, token);

            if (token.IsCancellationRequested)
                return;

            onClose?.Invoke();
            CurrentWindow = window;
            await window.OpenAsync(open, token);
        }

        private static (AnimatedWindowAnimation close, AnimatedWindowAnimation open) ResolveDirection(WindowDirection direction)
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
