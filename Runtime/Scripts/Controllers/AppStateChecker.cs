using System;
using UnityEngine;
using UnityEngine.Events;

namespace enp_unity_extensions.Runtime.Scripts.Controllers
{
    [DisallowMultipleComponent]
    public class AppStateChecker : MonoBehaviour
    {
        private static AppStateChecker _instance;

        private static AppStateChecker Instance
        {
            get
            {
                if (_instance != null)
                {
                    return _instance;
                }

                _instance = FindAnyObjectByType<AppStateChecker>();
                if (_instance != null)
                {
                    return _instance;
                }

                var host = new GameObject(nameof(AppStateChecker));
                _instance = host.AddComponent<AppStateChecker>();
                return _instance;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureInstance()
        {
            _ = Instance;
        }

        private event UnityAction<bool> _applicationPause;
        private event UnityAction<bool> _applicationFocus;

        public static event UnityAction<bool> ApplicationPause
        {
            add => Instance._applicationPause += value;
            remove
            {
                if (_instance != null)
                {
                    _instance._applicationPause -= value;
                }
            }
        }

        public static event UnityAction<bool> ApplicationFocus
        {
            add => Instance._applicationFocus += value;
            remove
            {
                if (_instance != null)
                {
                    _instance._applicationFocus -= value;
                }
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (_instance != this)
            {
                return;
            }

            _applicationPause = null;
            _applicationFocus = null;
            _instance = null;
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            _applicationPause?.Invoke(pauseStatus);
        }
        
        private void OnApplicationFocus(bool focusStatus)
        {
            _applicationFocus?.Invoke(focusStatus);
        }
    }
}
