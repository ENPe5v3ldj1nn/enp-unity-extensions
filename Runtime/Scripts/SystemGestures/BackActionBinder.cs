using UnityEngine;
using UnityEngine.Events;

namespace enp_unity_extensions.Runtime.Scripts.SystemGestures
{
    [DisallowMultipleComponent]
    public class BackActionBinder : MonoBehaviour
    {
        [SerializeField] private UnityEvent _onBack;

        private UnityAction _runtimeCallback;
        public static BackActionBinder Attach(GameObject target, UnityAction onBack)
        {
            if (target == null)
                return null;

            var binder = target.GetComponent<BackActionBinder>();
            if (binder == null)
                binder = target.AddComponent<BackActionBinder>();

            binder.SetCallback(onBack);
            return binder;
        }

        public void SetCallback(UnityAction onBack)
        {
            _runtimeCallback = onBack;
        }

        private void OnEnable()
        {
            var callback = ResolveCallback();
            if (callback != null)
                SystemGesturesController.Register(gameObject, callback);
        }

        private void OnDisable()
        {
            SystemGesturesController.Unregister(gameObject);
        }

        private void OnDestroy()
        {
            SystemGesturesController.Unregister(gameObject);
        }

        private UnityAction ResolveCallback()
        {
            if (_runtimeCallback != null)
                return _runtimeCallback;

            if (_onBack != null)
                return InvokeSerialized;

            return null;
        }

        private void InvokeSerialized()
        {
            _onBack?.Invoke();
        }
    }
}
