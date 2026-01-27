using System;
using System.Collections.Generic;
using enp_unity_extensions.Runtime.Scripts.UI.Windows;
using UnityEngine;
using UnityEngine.Events;
using static enp_unity_extensions.Runtime.Scripts.UI.Windows.AnimatedWindowConstant;

namespace enp_unity_extensions.Runtime.Scripts.Controllers
{
    [AddComponentMenu("")]
    public abstract class AbstractUiController : MonoBehaviour
    {
        private static AbstractUiController _instance;
        private readonly Dictionary<Type, AnimatedWindow> _windowsMap = new();

        public static AnimatedWindow CurrentWindow
        {
            get => AnimatedWindowHistory.CurrentWindow;
            private set => AnimatedWindowHistory.CurrentWindow = value;
        }

        public static AnimatedWindow LastWindow
        {
            get => AnimatedWindowHistory.LastWindow;
            private set => AnimatedWindowHistory.LastWindow = value;
        }

        public static Type CurrentWindowType => CurrentWindow != null ? CurrentWindow.GetType() : null;
        public static Type LastWindowType => LastWindow != null ? LastWindow.GetType() : null;

        protected virtual void Initialize()
        {
            _instance = this;
            _windowsMap.Clear();
            SetupMap(_windowsMap);
            AnimatedWindowHistory.Reset();
        }

        protected abstract void SetupMap(Dictionary<Type, AnimatedWindow> windowsMap);

        protected void RegisterWindow(AnimatedWindow window)
        {
            _windowsMap[window.GetType()] = window;
        }

        protected void AutoRegisterWindows(Dictionary<Type, AnimatedWindow> windowsMap, bool includeInactive = true)
        {
            var windows = GetComponentsInChildren<AnimatedWindow>(includeInactive);
            for (int i = 0; i < windows.Length; i++)
            {
                var w = windows[i];
                var t = w.GetType();

                if (windowsMap.TryGetValue(t, out var existing) && existing != w)
                    throw new InvalidOperationException($"Duplicate window type registration: {t.Name}. Instances: {existing.name} and {w.name}");

                windowsMap[t] = w;
            }
        }

        public static AnimatedWindow ShowExclusive(Type windowType, UnityAction onClose = null)
        {
            return ShowExclusive(windowType, WindowDirection.Middle, onClose);
        }

        public static AnimatedWindow ShowExclusive(Type windowType, WindowDirection direction, UnityAction onClose = null)
        {
            var target = GetWindowInternal(windowType);
            OpenNext(target, direction, onClose);
            return target;
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

        public static Type ShowExclusiveType(Type windowType, UnityAction onClose = null)
        {
            ShowExclusive(windowType, WindowDirection.Middle, onClose);
            return windowType;
        }

        public static Type ShowExclusiveType(Type windowType, WindowDirection direction, UnityAction onClose = null)
        {
            ShowExclusive(windowType, direction, onClose);
            return windowType;
        }

        public static Type ShowExclusiveType<T>(UnityAction onClose = null) where T : AnimatedWindow
        {
            ShowExclusive<T>(WindowDirection.Middle, onClose);
            return typeof(T);
        }

        public static Type ShowExclusiveType<T>(WindowDirection direction, UnityAction onClose = null) where T : AnimatedWindow
        {
            ShowExclusive<T>(direction, onClose);
            return typeof(T);
        }

        public static void ShowLastWindow<T>(UnityAction onClose = null) where T : AnimatedWindow
        {
            ShowLastWindow<T>(WindowDirection.Middle, onClose);
        }

        public static void ShowLastWindow<T>(WindowDirection direction, UnityAction onClose = null) where T : AnimatedWindow
        {
            var target = LastWindow;
            if (target == null)
                return;

            if (target is not T)
                throw new InvalidOperationException($"LastWindow is {target.GetType().Name}, expected {typeof(T).Name}");

            OpenNext(target, direction, onClose);
        }

        public static T GetWindow<T>() where T : AnimatedWindow
        {
            return (T)GetWindowInternal(typeof(T));
        }

        private static AnimatedWindow GetWindowInternal(Type windowType)
        {
            if (windowType == null)
                throw new ArgumentNullException(nameof(windowType));

            if (_instance._windowsMap.TryGetValue(windowType, out var exact))
                return exact;

            AnimatedWindow candidate = null;
            Type candidateType = null;

            foreach (var kv in _instance._windowsMap)
            {
                if (!windowType.IsAssignableFrom(kv.Key))
                    continue;

                if (candidate != null)
                    throw new InvalidOperationException($"Multiple windows match requested type {windowType.Name}. Matches: {candidateType.Name}, {kv.Key.Name}");

                candidate = kv.Value;
                candidateType = kv.Key;
            }

            if (candidate == null)
                throw new KeyNotFoundException($"Window type {windowType.Name} not registered in {_instance.GetType().Name}.");

            return candidate;
        }

        protected static void OpenNext(AnimatedWindow window)
        {
            OpenNext(window, CloseMiddle, OpenMiddle);
        }

        protected static void OpenNext(AnimatedWindow window, WindowDirection direction, UnityAction onClose = null)
        {
            var (close, open) = ResolveDirection(direction);
            OpenNext(window, close, open, onClose);
        }

        protected static void OpenNext(AnimatedWindow window, AnimatedWindowConstant close, AnimatedWindowConstant open, UnityAction onClose = null)
        {
            var active = CurrentWindow;
            if (active == null)
            {
                onClose?.Invoke();
                window.Open(open);
                CurrentWindow = window;
                return;
            }

            if (active != window)
                LastWindow = active;

            active.Close(close, () =>
            {
                onClose?.Invoke();
                window.Open(open);
                CurrentWindow = window;
            });
        }

        private static (AnimatedWindowConstant close, AnimatedWindowConstant open) ResolveDirection(WindowDirection direction)
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
