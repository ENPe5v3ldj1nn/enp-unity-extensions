using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ENP.UnityExtensions.Runtime
{
    [AddComponentMenu("")]
    public abstract class AbstractUiController : MonoBehaviour, IWindowService
    {
        [Tooltip("Root under which [UiWindow] windows are auto-discovered. " +
                 "Leave empty to scan this controller's own children.")]
        [SerializeField] private Transform _windowsRoot;

        [Tooltip("Default animation config for windows that don't override it. " +
                 "Assigned to WindowConfig.Default on Initialize.")]
        [SerializeField] private WindowConfig _windowConfig;

        [Tooltip("Block raycasts on the whole UI while a transition is in flight, so a tap can't " +
                 "land on the outgoing or incoming window mid-animation, or double-fire a switch.")]
        [SerializeField] private bool _blockInputDuringTransition = true;

        private static AbstractUiController _instance;

        private CancellationTokenSource _transitionCts;
        private (Type type, AnimatedWindow window)[] _windows;

        private Canvas _blockerCanvas;
        private GraphicRaycaster _blockerRaycaster;
        private int _blockCount;

        AnimatedWindow IWindowService.CurrentWindow => WindowHistory.CurrentWindow;
        AnimatedWindow IWindowService.LastWindow => WindowHistory.LastWindow;

        T IWindowService.GetWindow<T>(string name) => GetWindowImpl<T>(name);
        T IWindowService.ShowExclusive<T>(WindowTransition transition, UnityAction onClose, string name) => ShowExclusiveImpl<T>(transition, onClose, name);
        void IWindowService.ShowLastWindow(WindowTransition transition, UnityAction onClose) => ShowLastImpl(transition, onClose);

        public static AnimatedWindow CurrentWindow => WindowHistory.CurrentWindow;
        public static AnimatedWindow LastWindow => WindowHistory.LastWindow;

        public static T GetWindow<T>(string name = null) where T : AnimatedWindow => Active.GetWindowImpl<T>(name);

        public static T ShowExclusive<T>(WindowTransition transition, UnityAction onClose = null, string name = null) where T : AnimatedWindow =>
            Active.ShowExclusiveImpl<T>(transition, onClose, name);

        public static void ShowLastWindow(WindowTransition transition, UnityAction onClose = null) => Active.ShowLastImpl(transition, onClose);

        private static AbstractUiController Active =>
            _instance != null
                ? _instance
                : throw new InvalidOperationException(
                    "No AbstractUiController is initialized. Boot your WindowController before using the static window API.");

        protected virtual void Initialize()
        {
            _instance = this;

            if (_windowConfig != null)
                WindowConfig.Default = _windowConfig;

            if (_blockInputDuringTransition)
                CreateBlocker();

            _windows = DiscoverWindows();

            for (int i = 0; i < _windows.Length; i++)
                _windows[i].window.InitializeWindow();

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
                _windows[i].window.HideImmediate();
        }

        protected void Show(AnimatedWindow window, WindowTransition transition, UnityAction onClose = null)
        {
            if (window != null)
                OpenNext(window, transition, onClose);
        }

        private T ShowExclusiveImpl<T>(WindowTransition transition, UnityAction onClose, string name) where T : AnimatedWindow
        {
            var target = GetWindowImpl<T>(name);
            OpenNext(target, transition, onClose);
            return target;
        }

        private void ShowLastImpl(WindowTransition transition, UnityAction onClose)
        {
            var target = WindowHistory.LastWindow;
            if (target == null)
                return;

            OpenNext(target, transition, onClose);
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

        private void OpenNext(AnimatedWindow window, WindowTransition transition, UnityAction onClose = null)
        {
            _transitionCts?.Cancel();
            _transitionCts?.Dispose();
            _transitionCts = new CancellationTokenSource();

            OpenNextAsync(window, transition, onClose, _transitionCts.Token).Forget();
        }

        private async UniTaskVoid OpenNextAsync(AnimatedWindow window, WindowTransition transition,
            UnityAction onClose, CancellationToken token)
        {
            SetInputBlocked(true);
            try
            {
                var active = WindowHistory.CurrentWindow;

                if (active != null && active != window)
                    WindowHistory.LastWindow = active;

                if (active != null)
                    await active.CloseAsync(transition, token);

                if (token.IsCancellationRequested)
                    return;

                onClose?.Invoke();
                WindowHistory.CurrentWindow = window;
                await window.OpenAsync(transition, token);
            }
            finally
            {
                SetInputBlocked(false);
            }
        }

        // Runtime full-screen raycast blocker, kept at the highest sort order so it sits above every
        // window regardless of their individual canvases. Ref-counted: an in-flight transition being
        // cancelled by a newer one must not let the old task's cleanup unblock input the new task needs.
        private void CreateBlocker()
        {
            var go = new GameObject("WindowInputBlocker", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            go.transform.SetParent(transform, false);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = go.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0f);
            image.raycastTarget = true;

            _blockerCanvas = go.GetComponent<Canvas>();
            _blockerCanvas.overrideSorting = true;
            _blockerCanvas.sortingOrder = short.MaxValue;
            _blockerCanvas.enabled = false;

            _blockerRaycaster = go.GetComponent<GraphicRaycaster>();
        }

        private void SetInputBlocked(bool blocked)
        {
            if (_blockerCanvas == null)
                return;

            _blockCount += blocked ? 1 : -1;
            var active = _blockCount > 0;
            _blockerCanvas.enabled = active;
            _blockerRaycaster.enabled = active;
        }
    }
}
