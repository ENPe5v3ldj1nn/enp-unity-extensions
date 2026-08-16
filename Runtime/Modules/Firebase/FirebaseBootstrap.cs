using System;
using Firebase;
using Firebase.Extensions;
using UnityEngine;

namespace ENP.UnityExtensions.Firebase
{
    public sealed class FirebaseBootstrap
    {
        private bool _isInitializing;

        public bool IsReady { get; private set; }

        public event Action Initialized;

        public void Initialize()
        {
            if (IsReady || _isInitializing)
                return;

            _isInitializing = true;

            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
            {
                _isInitializing = false;

                if (task.IsFaulted || task.IsCanceled)
                {
                    Debug.LogError($"[Firebase] Dependency check failed: {task.Exception}");
                    return;
                }

                if (task.Result != DependencyStatus.Available)
                {
                    Debug.LogError($"[Firebase] Dependencies are not available: {task.Result}");
                    return;
                }

                _ = FirebaseApp.DefaultInstance;
                IsReady = true;
                Initialized?.Invoke();
            });
        }
    }
}
