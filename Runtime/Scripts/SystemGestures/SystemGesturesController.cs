using System.Collections.Generic;
using enp_unity_extensions.Runtime.Scripts.UI.Windows;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace enp_unity_extensions.Runtime.Scripts.SystemGestures
{
    public class SystemGesturesController : MonoBehaviour
    {
        private static readonly List<BackHandler> BackHandlers = new();
        private static SystemGesturesController _instance;

        [SerializeField] private float _debounceSeconds = 0.35f;

        private InputAction _backAction;
        private float _nextAllowedTime;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            SetupBackAction();
            Input.backButtonLeavesApp = false;
        }

        private void OnEnable()
        {
            _backAction?.Enable();
        }

        private void OnDisable()
        {
            _backAction?.Disable();
        }

        private void OnDestroy()
        {
            _backAction?.Dispose();
            if (_instance == this)
                _instance = null;
        }

        private void SetupBackAction()
        {
            _backAction = new InputAction("Back", InputActionType.Button);
            _backAction.AddBinding("<Keyboard>/escape");
            _backAction.AddBinding("*/{Back}");
            _backAction.AddBinding("<Gamepad>/start");
            _backAction.AddBinding("<Gamepad>/select");
            _backAction.performed += _ => TryHandleBack();
            _backAction.Enable();
        }

        private void Update()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape))
                TryHandleBack();
#endif
        }

        private void TryHandleBack()
        {
            if (Time.unscaledTime < _nextAllowedTime)
                return;

            CleanupDeadHandlers();
            var activeWindow = AnimatedWindowExtensions.ActiveWindow;

            for (var i = BackHandlers.Count - 1; i >= 0; i--)
            {
                var handler = BackHandlers[i];
                var target = handler.Target;

                if (target == null || !target.activeInHierarchy)
                    continue;

                if (activeWindow != null)
                {
                    var activeTransform = activeWindow.transform;
                    var targetTransform = target.transform;

                    if (targetTransform != activeTransform && !targetTransform.IsChildOf(activeTransform))
                        continue;
                }

                _nextAllowedTime = Time.unscaledTime + _debounceSeconds;
                handler.Invoke();
                return;
            }
        }

        private static void CleanupDeadHandlers()
        {
            for (var i = BackHandlers.Count - 1; i >= 0; i--)
            {
                if (BackHandlers[i].Target == null)
                    BackHandlers.RemoveAt(i);
            }
        }

        internal static void Register(GameObject target, UnityAction action)
        {
            if (target == null || action == null)
                return;

            Unregister(target);
            BackHandlers.Add(new BackHandler(target, action));
        }

        internal static void Unregister(GameObject target)
        {
            for (var i = BackHandlers.Count - 1; i >= 0; i--)
            {
                if (BackHandlers[i].Target == target)
                    BackHandlers.RemoveAt(i);
            }
        }

        private readonly struct BackHandler
        {
            public readonly GameObject Target;
            private readonly UnityAction _callback;

            public BackHandler(GameObject target, UnityAction callback)
            {
                Target = target;
                _callback = callback;
            }

            public void Invoke()
            {
                _callback?.Invoke();
            }
        }
    }
}
