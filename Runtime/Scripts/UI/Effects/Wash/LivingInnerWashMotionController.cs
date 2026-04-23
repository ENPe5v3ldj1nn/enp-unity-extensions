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
        private int _lastSeed;
        private double _lastTime;
        private bool _initialized;
        private bool _timeInitialized;

        public LivingInnerWashState State => _state;
        public bool MotionEnabled
        {
            get => _motionEnabled;
            set => _motionEnabled = value;
        }

        private static void ApplyDefaultConfiguration(LivingInnerWashMotionController controller)
        {
            controller._state = LivingInnerWashState.Default();
            controller._motionEnabled = DefaultMotionEnabled;
            controller._useUnscaledTime = DefaultUseUnscaledTime;
            controller._globalMotionSpeed = DefaultGlobalMotionSpeed;
            controller._intensityBreathAmplitude = DefaultIntensityBreathAmplitude;
            controller._thicknessBreathAmplitude = DefaultThicknessBreathAmplitude;
            controller._bottomSegmentMotionAmplitude = DefaultBottomSegmentMotionAmplitude;
            controller._sideSegmentMotionAmplitude = DefaultSideSegmentMotionAmplitude;
            controller._accentMotionAmplitude = DefaultAccentMotionAmplitude;
            controller._accentMotionSpeed = DefaultAccentMotionSpeed;
            controller._segmentFrequencyMin = DefaultSegmentFrequencyMin;
            controller._segmentFrequencyMax = DefaultSegmentFrequencyMax;
            controller._segmentSmoothing = DefaultSegmentSmoothing;
            controller._multiZoneBlend = DefaultMultiZoneBlend;
            controller._seed = DefaultSeed;
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

            _timeInitialized = false;
        }

        private void Update()
        {
            InitializeIfNeeded(false);

            if (_graphic == null)
            {
                return;
            }

            if (!_motionEnabled)
            {
                _graphic.SetState(_state);
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

            float time = (float)now * Mathf.Max(0f, _globalMotionSpeed);
            Tick(time, deltaTime);

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
            CaptureBaseValues();
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
                CaptureBaseValues();
                CopyBaseToCurrent();
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

        private void Tick(float time, float deltaTime)
        {
            CaptureBaseValues();

            float intensityBreath = EvaluateBreath(time, 0.23f, 0.71f, 0.17f);
            float thicknessBreath = EvaluateBreath(time, 0.19f, 0.47f, 0.29f);
            float intensity = Mathf.Clamp01(_state.Intensity + intensityBreath * _intensityBreathAmplitude);
            float thickness = Mathf.Clamp01(_state.Thickness + thicknessBreath * _thicknessBreathAmplitude);

            EvaluateGroup(time, _baseBottom, _bottomTarget, _baseBottomAccent, _bottomAccentTarget, _bottomAccentProfile, 0, BottomCount, _bottomSegmentMotionAmplitude, _accentMotionAmplitude, 1f);
            EvaluateGroup(time, _baseLeft, _leftTarget, _baseLeftAccent, _leftAccentTarget, _sideAccentProfile, BottomCount, SideCount, _sideSegmentMotionAmplitude, _accentMotionAmplitude * 0.44f, 0.52f);
            EvaluateGroup(time, _baseRight, _rightTarget, _baseRightAccent, _rightAccentTarget, _sideAccentProfile, BottomCount + SideCount, SideCount, _sideSegmentMotionAmplitude, _accentMotionAmplitude * 0.44f, 0.52f);

            SmoothArray(_bottom, _bottomTarget, BottomCount, deltaTime);
            SmoothArray(_left, _leftTarget, SideCount, deltaTime);
            SmoothArray(_right, _rightTarget, SideCount, deltaTime);
            SmoothArray(_bottomAccent, _bottomAccentTarget, BottomCount, deltaTime);
            SmoothArray(_leftAccent, _leftAccentTarget, SideCount, deltaTime);
            SmoothArray(_rightAccent, _rightAccentTarget, SideCount, deltaTime);

            float accentLift = MaxValue(_bottomAccent, BottomCount) * 0.16f;
            Color bottomColor = Color.LerpUnclamped(_state.BottomColor, Color.white, accentLift * 0.32f);
            Color accentColor = Color.LerpUnclamped(_state.AccentColor, Color.white, accentLift);

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
                float blended = Mathf.Lerp(local, (previous + local + next) / 3f, _multiZoneBlend);

                float accentWave = EvaluateAccentWave(time, wave);
                float previousAccent = i > 0 ? EvaluateAccentWave(time, _waves[waveOffset + i - 1]) : accentWave;
                float nextAccent = i < count - 1 ? EvaluateAccentWave(time, _waves[waveOffset + i + 1]) : accentWave;
                float accentBlended = Mathf.Lerp(accentWave, Mathf.Max(accentWave, (previousAccent + accentWave + nextAccent) / 3f), _multiZoneBlend * 0.72f);
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

        private float EvaluateAccentWave(float time, SegmentWave wave)
        {
            float a = Mathf.Sin(time * wave.AccentFrequency * Mathf.Max(0f, _accentMotionSpeed) + wave.AccentPhase) * 0.5f + 0.5f;
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

        private void SmoothArray(float[] current, float[] target, int count, float deltaTime)
        {
            float response = Mathf.Lerp(4.5f, 18f, _segmentSmoothing);
            float t = 1f - Mathf.Exp(-response * Mathf.Max(0f, deltaTime));

            for (int i = 0; i < count; i++)
            {
                current[i] = Mathf.Lerp(current[i], target[i], t);
            }
        }

        private void CaptureBaseValues()
        {
            _state = LivingInnerWashState.Sanitize(_state);
            _state.BottomSegments.ToArray(_baseBottom);
            _state.LeftSegments.ToArray(_baseLeft);
            _state.RightSegments.ToArray(_baseRight);
            _state.BottomAccents.ToArray(_baseBottomAccent);
            _state.LeftAccents.ToArray(_baseLeftAccent);
            _state.RightAccents.ToArray(_baseRightAccent);
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
