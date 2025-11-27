using System;
using System.Collections.Generic;
using enp_unity_extensions.Runtime.Scripts.UI.Windows;
using UnityEngine;
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
        
        public static void ShowExclusiveById(TWindowId id)
        {
            var target = _instance._windowsMap[id];
            OpenNext(target);
        }

        public static void ShowExclusiveById(TWindowId id, WindowDirection direction)
        {
            var target = _instance._windowsMap[id];
            var (close, open) = ResolveDirection(direction);
            OpenNext(target, close, open);
        }

        public static T ShowExclusiveByType<T>(TWindowId id) where T : Component
        {
            var target = _instance._windowsMap[id];
            OpenNext(target);

            if (target is T typed)
                return typed;

            throw new InvalidOperationException($"WindowId {id} → {target.GetType().Name}, очікували {typeof(T).Name}");
        }

        public static T ShowExclusiveByType<T>(TWindowId id, WindowDirection direction) where T : Component
        {
            var target = _instance._windowsMap[id];
            var (close, open) = ResolveDirection(direction);
            OpenNext(target, close, open);

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
                WindowDirection.Left => (CloseLeft, OpenLeft),
                WindowDirection.Right => (CloseRight, OpenRight),
                WindowDirection.Middle => (CloseMiddle, OpenMiddle),
                WindowDirection.SmoothLeft => (CloseSmoothLeft, OpenSmoothLeft),
                WindowDirection.SmoothRight => (CloseSmoothRight, OpenSmoothRight),
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
