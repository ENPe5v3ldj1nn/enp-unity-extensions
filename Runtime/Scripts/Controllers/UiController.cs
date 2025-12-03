using System;
using System.Collections.Generic;
using enp_unity_extensions.Runtime.Scripts.UI.Windows;
using UnityEngine;
using UnityEngine.Events;
using static enp_unity_extensions.Runtime.Scripts.UI.Windows.AnimatedWindowConstant;

namespace enp_unity_extensions.Runtime.Scripts.Controllers
{
    public abstract class UiController<TWindowId> : MonoBehaviour
        where TWindowId : struct, Enum
    {
        private static UiController<TWindowId> _instance;
        private readonly Dictionary<TWindowId, AnimatedWindow> _windowsMap = new();

        protected virtual void Initialize()
        {
            _instance = this;
            _windowsMap.Clear();
            SetupMap(_windowsMap);
        }

        protected abstract void SetupMap(Dictionary<TWindowId, AnimatedWindow> windowsMap);
        
        public static void ShowExclusiveById(TWindowId id, UnityAction onClose = null)
        {
            var target = _instance._windowsMap[id];
            OpenNext(target, WindowDirection.Middle, onClose);
        }

        public static void ShowExclusiveById(TWindowId id, WindowDirection direction, UnityAction onClose = null)
        {
            var target = _instance._windowsMap[id];
            OpenNext(target, direction, onClose);
        }

        public static T ShowExclusiveByType<T>(TWindowId id, UnityAction onClose = null) where T : Component
        {
            var target = _instance._windowsMap[id];
            OpenNext(target, WindowDirection.Middle, onClose);

            if (target is T typed)
                return typed;

            throw new InvalidOperationException($"WindowId {id} → {target.GetType().Name}, очікували {typeof(T).Name}");
        }

        public static T ShowExclusiveByType<T>(TWindowId id, WindowDirection direction, UnityAction onClose = null) where T : Component
        {
            var target = _instance._windowsMap[id];
            OpenNext(target, direction, onClose);

            if (target is T typed)
                return typed;

            throw new InvalidOperationException($"WindowId {id} → {target.GetType().Name}, очікували {typeof(T).Name}");
        }

        protected static void OpenNext(AnimatedWindow window)
        {
            OpenNext(window, CloseMiddle, OpenMiddle);
        }

        protected static void OpenNext(AnimatedWindow window, WindowDirection direction, UnityEngine.Events.UnityAction onClose = null)
        {
            var (close, open) = ResolveDirection(direction);
            OpenNext(window, close, open, onClose);
        }

        protected static void OpenNext(AnimatedWindow window,
            AnimatedWindowConstant close = CloseMiddle,
            AnimatedWindowConstant open = OpenMiddle,
            UnityEngine.Events.UnityAction onClose = null)
        {
            var active = AnimatedWindowExtensions.ActiveWindow;
            if (active == null)
            {
                onClose?.Invoke();
                window.Open(open);
                return;
            }

            active.Close(close, () =>
            {
                onClose?.Invoke();
                window.Open(open);
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
