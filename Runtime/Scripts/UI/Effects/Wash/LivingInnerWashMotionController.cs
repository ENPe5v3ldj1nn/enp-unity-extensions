using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace enp_unity_extensions.Runtime.Scripts.UI.Effects.Wash
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RoundedRectLivingInnerWashGraphic))]
    public sealed class LivingInnerWashMotionController : MonoBehaviour
    {
        [System.Serializable]
        public struct MotionTuning
        {
            public bool MotionEnabled;
            public bool UseUnscaledTime;
            public float GlobalMotionSpeed;
            public float IntensityBreathAmplitude;
            public float ThicknessBreathAmplitude;
            public float BottomSegmentMotionAmplitude;
            public float SideSegmentMotionAmplitude;
            public float AccentMotionAmplitude;
            public float AccentMotionSpeed;
            public float SegmentFrequencyMin;
            public float SegmentFrequencyMax;
            public float SegmentSmoothing;
            public float MultiZoneBlend;
            public int Seed;

            public static MotionTuning Sanitize(MotionTuning tuning)
            {
                tuning.GlobalMotionSpeed = Mathf.Max(0f, tuning.GlobalMotionSpeed);
                tuning.IntensityBreathAmplitude = Mathf.Clamp(tuning.IntensityBreathAmplitude, 0f, 0.2f);
                tuning.ThicknessBreathAmplitude = Mathf.Clamp(tuning.ThicknessBreathAmplitude, 0f, 0.2f);
                tuning.BottomSegmentMotionAmplitude = Mathf.Clamp01(tuning.BottomSegmentMotionAmplitude);
                tuning.SideSegmentMotionAmplitude = Mathf.Clamp01(tuning.SideSegmentMotionAmplitude);
                tuning.AccentMotionAmplitude = Mathf.Clamp01(tuning.AccentMotionAmplitude);
                tuning.AccentMotionSpeed = Mathf.Max(0f, tuning.AccentMotionSpeed);
                tuning.SegmentFrequencyMin = Mathf.Max(0.01f, tuning.SegmentFrequencyMin);
                tuning.SegmentFrequencyMax = Mathf.Max(tuning.SegmentFrequencyMin, tuning.SegmentFrequencyMax);
                tuning.SegmentSmoothing = Mathf.Clamp01(tuning.SegmentSmoothing);
                tuning.MultiZoneBlend = Mathf.Clamp01(tuning.MultiZoneBlend);
                tuning.Seed = Mathf.Max(1, tuning.Seed);
                return tuning;
            }
        }

        private const int BottomCount = 5;
        private const int SideCount = 3;
        private const int TotalCount = 11;
        private const bool DefaultMotionEnabled = true;
        private const bool DefaultUseUnscaledTime = true;
        private const float DefaultGlobalMotionSpeed = 7f;
        private const float DefaultIntensityBreathAmplitude = 0.04f;
        private const float DefaultThicknessBreathAmplitude = 0.08f;
        private const float DefaultBottomSegmentMotionAmplitude = 0.3f;
        private const float DefaultSideSegmentMotionAmplitude = 0.11f;
        private const float DefaultAccentMotionAmplitude = 0.18f;
        private const float DefaultAccentMotionSpeed = 1.6f;
        private const float DefaultSegmentFrequencyMin = 0.22f;
        private const float DefaultSegmentFrequencyMax = 0.72f;
        private const float DefaultSegmentSmoothing = 0.82f;
        private const float DefaultMultiZoneBlend = 0.62f;
        private const int DefaultSeed = 4059;

        [SerializeField] private LivingInnerWashState _state = LivingInnerWashState.Default();
        [SerializeField] private bool _motionEnabled = DefaultMotionEnabled;
        [SerializeField] private bool _useUnscaledTime = DefaultUseUnscaledTime;
        [SerializeField] [Min(0f)] private float _globalMotionSpeed = DefaultGlobalMotionSpeed;
        [SerializeField] [Range(0f, 0.2f)] private float _intensityBreathAmplitude = DefaultIntensityBreathAmplitude;
        [SerializeField] [Range(0f, 0.2f)] private float _thicknessBreathAmplitude = DefaultThicknessBreathAmplitude;
        [SerializeField] [Range(0f, 1f)] private float _bottomSegmentMotionAmplitude = DefaultBottomSegmentMotionAmplitude;
        [SerializeField] [Range(0f, 1f)] private float _sideSegmentMotionAmplitude = DefaultSideSegmentMotionAmplitude;
        [SerializeField] [Range(0f, 1f)] private float _accentMotionAmplitude = DefaultAccentMotionAmplitude;
        [SerializeField] [Min(0f)] private float _accentMotionSpeed = DefaultAccentMotionSpeed;
        [SerializeField] [Min(0.01f)] private float _segmentFrequencyMin = DefaultSegmentFrequencyMin;
        [SerializeField] [Min(0.01f)] private float _segmentFrequencyMax = DefaultSegmentFrequencyMax;
        [SerializeField] [Range(0f, 1f)] private float _segmentSmoothing = DefaultSegmentSmoothing;
        [SerializeField] [Range(0f, 1f)] private float _multiZoneBlend = DefaultMultiZoneBlend;
        [SerializeField] private int _seed = DefaultSeed;

        private readonly float[] _baseBottom = new float[BottomCount];
        private readonly float[] _baseLeft = new float[SideCount];
        private readonly float[] _baseRight = new float[SideCount];
        private readonly float[] _baseBottomAccent = new float[BottomCount];
        private readonly float[] _baseLeftAccent = new float[SideCount];
        private readonly float[] _baseRightAccent = new float[SideCount];

        private readonly float[] _bottom = new float[BottomCount];
        private readonly float[] _left = new float[SideCount];
        private readonly float[] _right = new float[SideCount];
        private readonly float[] _bottomAccent = new float[BottomCount];
        private readonly float[] _leftAccent = new float[SideCount];
        private readonly float[] _rightAccent = new float[SideCount];

        private readonly float[] _bottomTarget = new float[BottomCount];
        private readonly float[] _leftTarget = new float[SideCount];
        private readonly float[] _rightTarget = new float[SideCount];
        private readonly float[] _bottomAccentTarget = new float[BottomCount];
        private readonly float[] _leftAccentTarget = new float[SideCount];
        private readonly float[] _rightAccentTarget = new float[SideCount];

        private readonly SegmentWave[] _waves = new SegmentWave[TotalCount];
        private readonly float[] _bottomAccentProfile = new float[BottomCount] { 0.22f, 1f, 1.12f, 1f, 0.22f };
        private readonly float[] _sideAccentProfile = new float[SideCount] { 0.08f, 0.66f, 0.18f };

        private RoundedRectLivingInnerWashGraphic _graphic;
        private LivingInnerWashState _runtimeOverrideState;
        private MotionTuning _runtimeOverrideTuning;
        private int _lastSeed;
        private double _lastTime;
        private float _motionTime;
        private bool _initialized;
        private bool _timeInitialized;
        private bool _runtimeOverrideActive;

        public LivingInnerWashState State => _state;
        public MotionTuning CurrentTuning => new MotionTuning
        {
            MotionEnabled = _motionEnabled,
            UseUnscaledTime = _useUnscaledTime,
            GlobalMotionSpeed = _globalMotionSpeed,
            IntensityBreathAmplitude = _intensityBreathAmplitude,
            ThicknessBreathAmplitude = _thicknessBreathAmplitude,
            BottomSegmentMotionAmplitude = _bottomSegmentMotionAmplitude,
            SideSegmentMotionAmplitude = _sideSegmentMotionAmplitude,
            AccentMotionAmplitude = _accentMotionAmplitude,
            AccentMotionSpeed = _accentMotionSpeed,
            SegmentFrequencyMin = _segmentFrequencyMin,
            SegmentFrequencyMax = _segmentFrequencyMax,
            SegmentSmoothing = _segmentSmoothing,
            MultiZoneBlend = _multiZoneBlend,
            Seed = _seed
        };
        public bool MotionEnabled
        {
            get => _motionEnabled;
            set => _motionEnabled = value;
        }

        public void ApplyDebugConfiguration(LivingInnerWashState state, MotionTuning tuning)
        {
            _runtimeOverrideState = LivingInnerWashState.Sanitize(state);
            _runtimeOverrideTuning = MotionTuning.Sanitize(tuning);
            _runtimeOverrideActive = true;
        }

        public void ClearDebugConfiguration()
        {
            _runtimeOverrideActive = false;

            if (_graphic != null)
            {
                _graphic.SetState(_state);
            }
        }

        private static void ApplyDefaultConfiguration(LivingInnerWashMotionController controller)
        {
            controller._state = LivingInnerWashState.Default();
            controller.ApplyTuning(new MotionTuning
            {
                MotionEnabled = DefaultMotionEnabled,
                UseUnscaledTime = DefaultUseUnscaledTime,
                GlobalMotionSpeed = DefaultGlobalMotionSpeed,
                IntensityBreathAmplitude = DefaultIntensityBreathAmplitude,
                ThicknessBreathAmplitude = DefaultThicknessBreathAmplitude,
                BottomSegmentMotionAmplitude = DefaultBottomSegmentMotionAmplitude,
                SideSegmentMotionAmplitude = DefaultSideSegmentMotionAmplitude,
                AccentMotionAmplitude = DefaultAccentMotionAmplitude,
                AccentMotionSpeed = DefaultAccentMotionSpeed,
                SegmentFrequencyMin = DefaultSegmentFrequencyMin,
                SegmentFrequencyMax = DefaultSegmentFrequencyMax,
                SegmentSmoothing = DefaultSegmentSmoothing,
                MultiZoneBlend = DefaultMultiZoneBlend,
                Seed = DefaultSeed
            });
        }

        private void ApplyTuning(MotionTuning tuning)
        {
            tuning = MotionTuning.Sanitize(tuning);
            bool seedChanged = _seed != tuning.Seed;
            _motionEnabled = tuning.MotionEnabled;
            _useUnscaledTime = tuning.UseUnscaledTime;
            _globalMotionSpeed = tuning.GlobalMotionSpeed;
            _intensityBreathAmplitude = tuning.IntensityBreathAmplitude;
            _thicknessBreathAmplitude = tuning.ThicknessBreathAmplitude;
            _bottomSegmentMotionAmplitude = tuning.BottomSegmentMotionAmplitude;
            _sideSegmentMotionAmplitude = tuning.SideSegmentMotionAmplitude;
            _accentMotionAmplitude = tuning.AccentMotionAmplitude;
            _accentMotionSpeed = tuning.AccentMotionSpeed;
            _segmentFrequencyMin = tuning.SegmentFrequencyMin;
            _segmentFrequencyMax = tuning.SegmentFrequencyMax;
            _segmentSmoothing = tuning.SegmentSmoothing;
            _multiZoneBlend = tuning.MultiZoneBlend;
            _seed = tuning.Seed;

            if (_initialized && seedChanged)
            {
                InitializeWaves();
                _lastSeed = _seed;
            }
        }

        private void Awake()
        {
            InitializeIfNeeded(true);
        }

        private void OnEnable()
        {
            InitializeIfNeeded(true);
            ResetTime();
        }

        private void OnDisable()
        {
            if (_graphic != null)
            {
                _graphic.RestoreBase();
            }

            _runtimeOverrideActive = false;
            _motionTime = 0f;
            _timeInitialized = false;
        }

        private void Update()
        {
            InitializeIfNeeded(false);

            if (_graphic == null)
            {
                return;
            }

            LivingInnerWashState activeState = GetActiveState();
            MotionTuning activeTuning = GetActiveTuning();

            if (!activeTuning.MotionEnabled)
            {
                _graphic.SetState(activeState);
                return;
            }

            double now = GetCurrentTime();

            if (!_timeInitialized)
            {
                _lastTime = now;
                _timeInitialized = true;
            }

            float deltaTime = Mathf.Clamp((float)(now - _lastTime), 0f, 0.05f);
            _lastTime = now;

            _motionTime += deltaTime * Mathf.Max(0f, activeTuning.GlobalMotionSpeed);
            Tick(activeState, activeTuning, _motionTime, deltaTime);

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorApplication.QueuePlayerLoopUpdate();
                SceneView.RepaintAll();
            }
#endif
        }

        public void SetState(LivingInnerWashState state)
        {
            _state = LivingInnerWashState.Sanitize(state);
            _runtimeOverrideActive = false;
            CaptureBaseValues(_state);
            CopyBaseToCurrent();

            if (_graphic != null)
            {
                _graphic.SetState(_state);
            }
        }

        private void InitializeIfNeeded(bool forceWaveReset)
        {
            bool shouldApplyState = !_initialized;

            if (_graphic == null)
            {
                _graphic = GetComponent<RoundedRectLivingInnerWashGraphic>();
            }

            _state = LivingInnerWashState.Sanitize(_state);

            if (!_initialized)
            {
                CaptureBaseValues(_state);
                CopyBaseToCurrent();
                _motionTime = 0f;
                _initialized = true;
            }

            if (forceWaveReset || _lastSeed != _seed)
            {
                InitializeWaves();
                _lastSeed = _seed;
            }

            if (_graphic != null)
            {
                if (shouldApplyState || forceWaveReset)
                {
                    _graphic.SetState(_state);
                }
            }
        }

        private void Tick(LivingInnerWashState activeState, MotionTuning activeTuning, float time, float deltaTime)
        {
            CaptureBaseValues(activeState);

            float intensityBreath = EvaluateBreath(time, 0.23f, 0.71f, 0.17f);
            float thicknessBreath = EvaluateBreath(time, 0.19f, 0.47f, 0.29f);
            float intensity = Mathf.Clamp01(activeState.Intensity + intensityBreath * activeTuning.IntensityBreathAmplitude);
            float thickness = Mathf.Clamp01(activeState.Thickness + thicknessBreath * activeTuning.ThicknessBreathAmplitude);

            EvaluateGroup(time, activeTuning, _baseBottom, _bottomTarget, _baseBottomAccent, _bottomAccentTarget, _bottomAccentProfile, 0, BottomCount, activeTuning.BottomSegmentMotionAmplitude, activeTuning.AccentMotionAmplitude, 1f);
            EvaluateGroup(time, activeTuning, _baseLeft, _leftTarget, _baseLeftAccent, _leftAccentTarget, _sideAccentProfile, BottomCount, SideCount, activeTuning.SideSegmentMotionAmplitude, activeTuning.AccentMotionAmplitude * 0.44f, 0.52f);
            EvaluateGroup(time, activeTuning, _baseRight, _rightTarget, _baseRightAccent, _rightAccentTarget, _sideAccentProfile, BottomCount + SideCount, SideCount, activeTuning.SideSegmentMotionAmplitude, activeTuning.AccentMotionAmplitude * 0.44f, 0.52f);

            SmoothArray(activeTuning, _bottom, _bottomTarget, BottomCount, deltaTime);
            SmoothArray(activeTuning, _left, _leftTarget, SideCount, deltaTime);
            SmoothArray(activeTuning, _right, _rightTarget, SideCount, deltaTime);
            SmoothArray(activeTuning, _bottomAccent, _bottomAccentTarget, BottomCount, deltaTime);
            SmoothArray(activeTuning, _leftAccent, _leftAccentTarget, SideCount, deltaTime);
            SmoothArray(activeTuning, _rightAccent, _rightAccentTarget, SideCount, deltaTime);

            float accentLift = MaxValue(_bottomAccent, BottomCount) * 0.16f;
            Color bottomColor = Color.LerpUnclamped(activeState.BottomColor, Color.white, accentLift * 0.32f);
            Color accentColor = Color.LerpUnclamped(activeState.AccentColor, Color.white, accentLift);

            _graphic.SetAnimatedValues(
                bottomColor,
                accentColor,
                intensity,
                thickness,
                _bottom,
                _left,
                _right,
                _bottomAccent,
                _leftAccent,
                _rightAccent);
        }

        private void EvaluateGroup(
            float time,
            MotionTuning activeTuning,
            float[] baseValues,
            float[] targetValues,
            float[] baseAccents,
            float[] targetAccents,
            float[] accentProfile,
            int waveOffset,
            int count,
            float segmentAmplitude,
            float accentAmplitude,
            float groupScale)
        {
            for (int i = 0; i < count; i++)
            {
                SegmentWave wave = _waves[waveOffset + i];
                float local = EvaluateWave(time, wave);
                float previous = i > 0 ? EvaluateWave(time, _waves[waveOffset + i - 1]) : local;
                float next = i < count - 1 ? EvaluateWave(time, _waves[waveOffset + i + 1]) : local;
                float blended = Mathf.Lerp(local, (previous + local + next) / 3f, activeTuning.MultiZoneBlend);

                float accentWave = EvaluateAccentWave(time, activeTuning, wave);
                float previousAccent = i > 0 ? EvaluateAccentWave(time, activeTuning, _waves[waveOffset + i - 1]) : accentWave;
                float nextAccent = i < count - 1 ? EvaluateAccentWave(time, activeTuning, _waves[waveOffset + i + 1]) : accentWave;
                float accentBlended = Mathf.Lerp(accentWave, Mathf.Max(accentWave, (previousAccent + accentWave + nextAccent) / 3f), activeTuning.MultiZoneBlend * 0.72f);
                float accentWeight = accentProfile[i];
                float accentValue = baseAccents[i] + Mathf.Max(0f, accentBlended) * accentAmplitude * groupScale * accentWeight;
                float segmentValue = baseValues[i] * (1f + blended * segmentAmplitude * groupScale);
                segmentValue += Mathf.Max(0f, accentBlended) * accentAmplitude * groupScale * accentWeight * 0.16f;
                targetValues[i] = Mathf.Clamp(segmentValue, 0f, 2f);
                targetAccents[i] = Mathf.Clamp01(accentValue);
            }
        }

        private float EvaluateWave(float time, SegmentWave wave)
        {
            float a = Mathf.Sin(time * wave.FrequencyA + wave.PhaseA);
            float b = Mathf.Sin(time * wave.FrequencyB + wave.PhaseB);
            float c = Mathf.Sin(time * wave.FrequencyC + wave.PhaseC);
            return (a * 0.54f + b * 0.31f + c * 0.15f) * wave.Amplitude + wave.Bias;
        }

        private float EvaluateAccentWave(float time, MotionTuning activeTuning, SegmentWave wave)
        {
            float a = Mathf.Sin(time * wave.AccentFrequency * Mathf.Max(0f, activeTuning.AccentMotionSpeed) + wave.AccentPhase) * 0.5f + 0.5f;
            float b = Mathf.Sin(time * wave.FrequencyB * 0.67f + wave.PhaseC) * 0.5f + 0.5f;
            float value = Mathf.Lerp(a, a * b, 0.42f);
            return value * value * (3f - 2f * value);
        }

        private static float EvaluateBreath(float time, float frequencyA, float frequencyB, float phase)
        {
            float a = Mathf.Sin(time * frequencyA + phase);
            float b = Mathf.Sin(time * frequencyB + phase * 3.7f);
            return a * 0.68f + b * 0.32f;
        }

        private void SmoothArray(MotionTuning activeTuning, float[] current, float[] target, int count, float deltaTime)
        {
            float response = Mathf.Lerp(4.5f, 18f, activeTuning.SegmentSmoothing);
            float t = 1f - Mathf.Exp(-response * Mathf.Max(0f, deltaTime));

            for (int i = 0; i < count; i++)
            {
                current[i] = Mathf.Lerp(current[i], target[i], t);
            }
        }

        private void CaptureBaseValues(LivingInnerWashState state)
        {
            state = LivingInnerWashState.Sanitize(state);
            state.BottomSegments.ToArray(_baseBottom);
            state.LeftSegments.ToArray(_baseLeft);
            state.RightSegments.ToArray(_baseRight);
            state.BottomAccents.ToArray(_baseBottomAccent);
            state.LeftAccents.ToArray(_baseLeftAccent);
            state.RightAccents.ToArray(_baseRightAccent);
        }

        private void CopyBaseToCurrent()
        {
            CopyArray(_baseBottom, _bottom, BottomCount);
            CopyArray(_baseLeft, _left, SideCount);
            CopyArray(_baseRight, _right, SideCount);
            CopyArray(_baseBottomAccent, _bottomAccent, BottomCount);
            CopyArray(_baseLeftAccent, _leftAccent, SideCount);
            CopyArray(_baseRightAccent, _rightAccent, SideCount);
        }

        private void InitializeWaves()
        {
            uint state = (uint)Mathf.Max(1, _seed);
            float minFrequency = Mathf.Min(_segmentFrequencyMin, _segmentFrequencyMax);
            float maxFrequency = Mathf.Max(_segmentFrequencyMin, _segmentFrequencyMax);

            for (int i = 0; i < TotalCount; i++)
            {
                float sideScale = i < BottomCount ? 1f : 0.58f;
                _waves[i] = new SegmentWave
                {
                    FrequencyA = Lerp(minFrequency, maxFrequency, Next01(ref state)),
                    FrequencyB = Lerp(minFrequency * 0.55f, maxFrequency * 0.82f, Next01(ref state)),
                    FrequencyC = Lerp(minFrequency * 0.28f, maxFrequency * 0.48f, Next01(ref state)),
                    AccentFrequency = Lerp(minFrequency * 0.72f, maxFrequency * 1.15f, Next01(ref state)),
                    PhaseA = Next01(ref state) * Mathf.PI * 2f,
                    PhaseB = Next01(ref state) * Mathf.PI * 2f,
                    PhaseC = Next01(ref state) * Mathf.PI * 2f,
                    AccentPhase = Next01(ref state) * Mathf.PI * 2f,
                    Amplitude = Lerp(0.72f, 1.12f, Next01(ref state)) * sideScale,
                    Bias = Lerp(-0.05f, 0.05f, Next01(ref state)) * sideScale
                };
            }
        }

        private LivingInnerWashState GetActiveState()
        {
            return _runtimeOverrideActive ? _runtimeOverrideState : _state;
        }

        private MotionTuning GetActiveTuning()
        {
            return _runtimeOverrideActive ? _runtimeOverrideTuning : CurrentTuning;
        }

        private double GetCurrentTime()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                return EditorApplication.timeSinceStartup;
            }
#endif
            return _useUnscaledTime ? Time.unscaledTimeAsDouble : Time.timeAsDouble;
        }

        private void ResetTime()
        {
            _lastTime = GetCurrentTime();
            _motionTime = 0f;
            _timeInitialized = false;
        }

        private static void CopyArray(float[] source, float[] target, int count)
        {
            for (int i = 0; i < count; i++)
            {
                target[i] = source[i];
            }
        }

        private static float MaxValue(float[] values, int count)
        {
            float max = 0f;

            for (int i = 0; i < count; i++)
            {
                if (values[i] > max)
                {
                    max = values[i];
                }
            }

            return max;
        }

        private static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * t;
        }

        private static float Next01(ref uint state)
        {
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;
            return (state & 0x00FFFFFF) / 16777215f;
        }

        private struct SegmentWave
        {
            public float FrequencyA;
            public float FrequencyB;
            public float FrequencyC;
            public float AccentFrequency;
            public float PhaseA;
            public float PhaseB;
            public float PhaseC;
            public float AccentPhase;
            public float Amplitude;
            public float Bias;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _state = LivingInnerWashState.Sanitize(_state);
            _globalMotionSpeed = Mathf.Max(0f, _globalMotionSpeed);
            _accentMotionSpeed = Mathf.Max(0f, _accentMotionSpeed);
            _segmentFrequencyMin = Mathf.Max(0.01f, _segmentFrequencyMin);
            _segmentFrequencyMax = Mathf.Max(0.01f, _segmentFrequencyMax);
            _segmentSmoothing = Mathf.Clamp01(_segmentSmoothing);
            _multiZoneBlend = Mathf.Clamp01(_multiZoneBlend);
            _seed = Mathf.Max(1, _seed);
            _initialized = false;

            if (isActiveAndEnabled)
            {
                InitializeIfNeeded(true);
                ResetTime();
            }
        }

        private void Reset()
        {
            ApplyDefaultConfiguration(this);
            _graphic = GetComponent<RoundedRectLivingInnerWashGraphic>();
            _initialized = false;
            InitializeIfNeeded(true);
            ResetTime();
        }
#endif
    }
}
