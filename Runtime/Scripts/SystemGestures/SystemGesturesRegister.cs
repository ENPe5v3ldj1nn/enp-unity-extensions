using System;
using UnityEngine;
using UnityEngine.Events;

namespace enp_unity_extensions.Runtime.Scripts.SystemGestures
{
    public static class SystemGesturesRegister
    {
        public static IDisposable RegisterBackAction(this GameObject target, UnityAction onBackAction)
        {
            SystemGesturesController.Register(target, onBackAction);
            return new BackRegistration(target);
        }

        public static void UnregisterBackAction(this GameObject target)
        {
            SystemGesturesController.Unregister(target);
        }

        [Obsolete("Use RegisterBackAction instead")]
        public static void RegistrateBackAction(this GameObject target, UnityAction onBackAction)
        {
            RegisterBackAction(target, onBackAction);
        }

        private readonly struct BackRegistration : IDisposable
        {
            private readonly GameObject _target;

            public BackRegistration(GameObject target)
            {
                _target = target;
            }

            public void Dispose()
            {
                SystemGesturesController.Unregister(_target);
            }
        }
    }
}
