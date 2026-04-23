using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace enp_unity_extensions.Runtime.Scripts.UI.Effects.Wash
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RoundedRectAsymmetricInnerWashGraphic))]
    public sealed class AsymmetricInnerWashExecuteAlwaysPreview : MonoBehaviour
    {
        [SerializeField] private bool _previewInEditMode = true;
        [SerializeField] private bool _autoAnimateStreak = true;
        [SerializeField] [Range(0f, 1f)] private float _manualStreak01 = 0.5f;
        [SerializeField] [Min(0.01f)] private float _streakCycleDuration = 4.5f;
        [SerializeField] [Range(0.1f, 4f)] private float _streakCurve = 1.35f;
        [SerializeField] [Range(0f, 1f)] private float _streakMin = 0.18f;
        [SerializeField] [Range(0f, 1f)] private float _streakMax = 0.82f;
        [SerializeField] private bool _useGraphicBaseState = true;
        [SerializeField] private AsymmetricInnerWashState _overrideBaseState;
        [SerializeField] private int _seed = 12345;
        [SerializeField] private bool _resetMotionOnEnable = true;
        [SerializeField] private bool _restoreGraphicPreviewOnDisable = true;

        private RoundedRectAsymmetricInnerWashGraphic _graphic;
        private AsymmetricInnerWashMotionGenerator _motionGenerator;
        private int _lastSeed;
        private double _lastTime;
        private bool _timeInitialized;
        private bool _pendingReset;
        private bool _pendingEditorApply;
        private bool _previewWasApplied;

        public float CurrentPreviewStreak01 => _autoAnimateStreak
            ? EvaluateAnimatedStreak(GetCurrentTime())
            : _manualStreak01;

        private void Awake()
        {
            EnsureOverrideStateDefaults();
            TryCacheReferences();
            EnsureGenerator(true);
        }

        private void OnEnable()
        {
            EnsureOverrideStateDefaults();
            TryCacheReferences();
            EnsureGenerator(_resetMotionOnEnable);
            ResetTime();
            _pendingReset = false;
            _pendingEditorApply = true;
            _previewWasApplied = false;

#if UNITY_EDITOR
            EditorApplication.update -= EditorTick;
            EditorApplication.update += EditorTick;
#endif
        }

        private void OnDisable()
        {
#if UNITY_EDITOR
            EditorApplication.update -= EditorTick;
#endif

            RestoreGraphicIfNeeded();
            _timeInitialized = false;
            _pendingReset = false;
            _pendingEditorApply = false;
            _previewWasApplied = false;
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            Tick(GetCurrentTime());
        }

#if UNITY_EDITOR
        private void EditorTick()
        {
            if (Application.isPlaying)
            {
                return;
            }

            if (!isActiveAndEnabled)
            {
                return;
            }

            if (!TryCacheReferences())
            {
                return;
            }

            if (!_previewInEditMode)
            {
                RestoreGraphicIfNeeded();
                return;
            }

            if (_pendingReset)
            {
                EnsureGenerator(true);
                ResetTime();
                _pendingReset = false;
            }

            Tick(EditorApplication.timeSinceStartup);
            _pendingEditorApply = false;
            EditorApplication.QueuePlayerLoopUpdate();
        }
#endif

        private void Tick(double currentTime)
        {
            if (!TryCacheReferences())
            {
                return;
            }

            EnsureGenerator(false);

            if (!_timeInitialized)
            {
                _lastTime = currentTime;
                _timeInitialized = true;
            }

            float deltaTime = (float)(currentTime - _lastTime);
            _lastTime = currentTime;

            if (deltaTime < 0f)
            {
                deltaTime = 0f;
            }
            else if (deltaTime > 0.05f)
            {
                deltaTime = 0.05f;
            }

            AsymmetricInnerWashState baseState = GetEffectiveBaseState();
            float streak01 = _autoAnimateStreak
                ? EvaluateAnimatedStreak(currentTime)
                : Mathf.Clamp01(_manualStreak01);

            _motionGenerator.Update(deltaTime, streak01, baseState);
            _motionGenerator.ApplyTo(_graphic);
            _previewWasApplied = true;
        }

        private AsymmetricInnerWashState GetEffectiveBaseState()
        {
            if (_useGraphicBaseState && _graphic != null)
            {
                return AsymmetricInnerWashState.Sanitize(_graphic.BaseState);
            }

            AsymmetricInnerWashState state = AsymmetricInnerWashState.Sanitize(_overrideBaseState);

            if (IsVisualStateEmpty(state))
            {
                if (_graphic != null)
                {
                    return AsymmetricInnerWashState.Sanitize(_graphic.BaseState);
                }

                return AsymmetricInnerWashState.CreateNeutralAmbient();
            }

            return state;
        }

        private static bool IsVisualStateEmpty(AsymmetricInnerWashState state)
        {
            return state.TintColor == default
                && state.TopColor == default
                && state.BottomColor == default;
        }

        private void EnsureOverrideStateDefaults()
        {
            if (IsVisualStateEmpty(_overrideBaseState))
            {
                _overrideBaseState = AsymmetricInnerWashState.CreateNeutralAmbient();
            }
            else
            {
                _overrideBaseState = AsymmetricInnerWashState.Sanitize(_overrideBaseState);
            }
        }

        private void RestoreGraphicIfNeeded()
        {
            if (_restoreGraphicPreviewOnDisable && _graphic != null && _previewWasApplied)
            {
                _graphic.RestorePreview();
            }
        }

        private float EvaluateAnimatedStreak(double currentTime)
        {
            float minValue = Mathf.Clamp01(_streakMin);
            float maxValue = Mathf.Clamp01(_streakMax);

            if (maxValue < minValue)
            {
                float temp = minValue;
                minValue = maxValue;
                maxValue = temp;
            }

            float duration = Mathf.Max(0.01f, _streakCycleDuration);
            float phase = (float)(currentTime / duration);
            float wave = Mathf.Sin(phase * Mathf.PI * 2f) * 0.5f + 0.5f;
            float curved = Mathf.Pow(wave, Mathf.Max(0.1f, _streakCurve));
            return Mathf.LerpUnclamped(minValue, maxValue, curved);
        }

        private bool TryCacheReferences()
        {
            if (_graphic == null)
            {
                _graphic = GetComponent<RoundedRectAsymmetricInnerWashGraphic>();
            }

            return _graphic != null;
        }

        private void EnsureGenerator(bool forceReset)
        {
            if (_motionGenerator == null || _lastSeed != _seed)
            {
                _motionGenerator = new AsymmetricInnerWashMotionGenerator(_seed);
                _lastSeed = _seed;
                forceReset = false;
            }

            if (forceReset)
            {
                _motionGenerator.Reset(_seed);
            }
        }

        private void ResetTime()
        {
            _lastTime = GetCurrentTime();
            _timeInitialized = false;
        }

        private double GetCurrentTime()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                return EditorApplication.timeSinceStartup;
            }
#endif
            return Time.unscaledTimeAsDouble;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _manualStreak01 = Mathf.Clamp01(_manualStreak01);
            _streakCycleDuration = Mathf.Max(0.01f, _streakCycleDuration);
            _streakCurve = Mathf.Clamp(_streakCurve, 0.1f, 4f);
            _streakMin = Mathf.Clamp01(_streakMin);
            _streakMax = Mathf.Clamp01(_streakMax);

            if (_streakMax < _streakMin)
            {
                float temp = _streakMin;
                _streakMin = _streakMax;
                _streakMax = temp;
            }

            EnsureOverrideStateDefaults();

            _pendingReset = true;
            _pendingEditorApply = true;

            if (!Application.isPlaying)
            {
                if (!_previewInEditMode)
                {
                    if (TryCacheReferences())
                    {
                        RestoreGraphicIfNeeded();
                    }
                }

                EditorApplication.QueuePlayerLoopUpdate();
            }
        }

        private void Reset()
        {
            _graphic = GetComponent<RoundedRectAsymmetricInnerWashGraphic>();
            _overrideBaseState = AsymmetricInnerWashState.CreateNeutralAmbient();
            _useGraphicBaseState = true;
            _pendingReset = true;
            _pendingEditorApply = true;
        }
#endif
    }
}