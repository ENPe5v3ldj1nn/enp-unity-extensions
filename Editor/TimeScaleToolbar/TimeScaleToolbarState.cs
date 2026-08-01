using UnityEditor;
using UnityEngine;

namespace ENP.UnityExtensions.Editor.TimeScaleToolbar
{
    internal static class TimeScaleToolbarState
    {
        private const float MinScale = 0f;
        private const float MaxScale = 5f;
        private const float DefaultScale = 1f;
        private const float FixedDeltaTimeRestoreFallback = 0.02f;
        private const string CurrentScaleKey = "ENP.UnityExtensions.TimeScaleToolbar.CurrentScale";
        private const string DefaultFixedDeltaTimeKey = "ENP.UnityExtensions.TimeScaleToolbar.DefaultFixedDeltaTime";
        private const string HasDefaultFixedDeltaTimeKey = "ENP.UnityExtensions.TimeScaleToolbar.HasDefaultFixedDeltaTime";

        public static event System.Action<float> ScaleChanged;

        public static float CurrentScale { get; private set; } = DefaultScale;
        public static bool IsPlaying { get; private set; }
        public static bool HasCachedDefaultFixedDeltaTime { get; private set; }
        public static float CachedDefaultFixedDeltaTime { get; private set; } = FixedDeltaTimeRestoreFallback;

        static TimeScaleToolbarState()
        {
            CurrentScale = Mathf.Clamp(EditorPrefs.GetFloat(CurrentScaleKey, DefaultScale), MinScale, MaxScale);
            HasCachedDefaultFixedDeltaTime = SessionState.GetBool(HasDefaultFixedDeltaTimeKey, false);
            CachedDefaultFixedDeltaTime = SessionState.GetFloat(DefaultFixedDeltaTimeKey, FixedDeltaTimeRestoreFallback);
            IsPlaying = EditorApplication.isPlaying;

            if (IsPlaying)
            {
                ApplyToRuntime(CurrentScale);
            }
        }

        public static void HandlePlayModeStateChanged(PlayModeStateChange stateChange)
        {
            switch (stateChange)
            {
                case PlayModeStateChange.ExitingEditMode:
                    IsPlaying = true;
                    CacheDefaultFixedDeltaTimeIfNeeded();
                    ApplyToRuntime(CurrentScale);
                    break;

                case PlayModeStateChange.EnteredPlayMode:
                    IsPlaying = true;
                    CacheDefaultFixedDeltaTimeIfNeeded();
                    ApplyToRuntime(CurrentScale);
                    NotifyScaleChanged();
                    break;

                case PlayModeStateChange.ExitingPlayMode:
                    RestoreDefaults();
                    break;

                case PlayModeStateChange.EnteredEditMode:
                    IsPlaying = false;
                    NotifyScaleChanged();
                    break;
            }
        }

        public static void SetScale(float scale)
        {
            var clampedScale = Mathf.Clamp(scale, MinScale, MaxScale);
            if (Mathf.Approximately(CurrentScale, clampedScale))
            {
                return;
            }

            CurrentScale = clampedScale;
            EditorPrefs.SetFloat(CurrentScaleKey, CurrentScale);

            if (IsPlaying)
            {
                ApplyToRuntime(CurrentScale);
            }

            NotifyScaleChanged();
        }

        public static void RestoreDefaults()
        {
            CurrentScale = DefaultScale;
            EditorPrefs.SetFloat(CurrentScaleKey, CurrentScale);

            Time.timeScale = DefaultScale;
            Time.fixedDeltaTime = HasCachedDefaultFixedDeltaTime ? CachedDefaultFixedDeltaTime : FixedDeltaTimeRestoreFallback;

            IsPlaying = false;
            NotifyScaleChanged();
        }

        private static void CacheDefaultFixedDeltaTimeIfNeeded()
        {
            if (HasCachedDefaultFixedDeltaTime)
            {
                return;
            }

            CachedDefaultFixedDeltaTime = Time.fixedDeltaTime;
            HasCachedDefaultFixedDeltaTime = true;
            SessionState.SetBool(HasDefaultFixedDeltaTimeKey, true);
            SessionState.SetFloat(DefaultFixedDeltaTimeKey, CachedDefaultFixedDeltaTime);
        }

        private static void ApplyToRuntime(float scale)
        {
            Time.timeScale = scale;
            Time.fixedDeltaTime = HasCachedDefaultFixedDeltaTime ? CachedDefaultFixedDeltaTime * scale : FixedDeltaTimeRestoreFallback * scale;
        }

        private static void NotifyScaleChanged()
        {
            ScaleChanged?.Invoke(CurrentScale);
        }
    }
}
