using System;
using UnityEngine;

namespace enp_unity_extensions.Runtime.Scripts.UI.Effects.LiquidEdge
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RoundedRectLiquidEdgeGraphic))]
    [AddComponentMenu("UI/ENP/Rounded Rect Liquid Edge Controller")]
    public sealed class RoundedRectLiquidEdgeController : MonoBehaviour
    {
        private const int SegmentCount = 11;

        [SerializeField] private LiquidEdgePreset _preset = LiquidEdgePreset.Living;
        [SerializeField] private bool _applyPresetOnEnable = true;
        [SerializeField] private int _seed = 17431;
        [SerializeField] private LiquidEdgeState _state = LiquidEdgeState.Living();

        private readonly float[] _baseStrengths = new float[SegmentCount];
        private readonly float[] _currentStrengths = new float[SegmentCount];
        private readonly float[] _targetDrift = new float[SegmentCount];
        private readonly float[] _currentDrift = new float[SegmentCount];
        private readonly float[] _driftVelocity = new float[SegmentCount];
        private readonly float[] _driftSpeed = new float[SegmentCount];
        private readonly float[] _driftAmplitude = new float[SegmentCount];
        private readonly float[] _noiseOffset = new float[SegmentCount];
        private readonly float[] _lobes = new float[SegmentCount];
        private readonly LiquidLobe[] _liquidLobes = new LiquidLobe[6];

        private RoundedRectLiquidEdgeGraphic _graphic;
        private System.Random _random;
        private float _lastTime;
        private bool _initialized;

        public LiquidEdgePreset Preset => _preset;
        public LiquidEdgeState State => _state;

        private void Awake()
        {
            InitializeIfNeeded();
        }

        private void OnEnable()
        {
            InitializeIfNeeded();

            if (_applyPresetOnEnable)
            {
                ApplyPreset(_preset);
            }
        }

        private void Update()
        {
            InitializeIfNeeded();
            _state = LiquidEdgeState.Sanitize(_state);
            float now = ResolveTime();
            float motionNow = now * _state.GlobalMotionSpeed;
            float deltaTime = Mathf.Clamp(now - _lastTime, 0f, 0.08f);
            float motionDeltaTime = deltaTime * _state.GlobalMotionSpeed;
            _lastTime = now;

            UpdateSegmentDrift(motionNow, motionDeltaTime);
            UpdateLiquidLobes(motionNow);
            ComposeRuntimeSegments();
            _graphic.SetRuntimeSegmentValues(_currentStrengths, _currentDrift, _lobes, motionNow + _seed * 0.013f);
        }

        public void ApplyPreset(LiquidEdgePreset preset)
        {
            _preset = preset;
            ApplyState(GetPresetState(preset), true);
        }

        public void ApplyState(LiquidEdgeState state, bool restartMotion)
        {
            InitializeIfNeeded();
            _state = LiquidEdgeState.Sanitize(state);
            _graphic.SetState(_state);

            float[] strengths = _state.SegmentStrengths.ToArray();

            for (int i = 0; i < SegmentCount; i++)
            {
                _baseStrengths[i] = strengths[i];
                _currentStrengths[i] = strengths[i];
            }

            if (restartMotion)
            {
                ResetMotion();
            }
        }

        public void SetGlobalIntensity(float value)
        {
            _state.GlobalIntensity = Mathf.Clamp(value, 0f, 2f);
            _graphic.SetGlobalIntensity(_state.GlobalIntensity);
        }

        public void SetBottomDominance(float value)
        {
            _state.BottomDominance = Mathf.Clamp(value, 0f, 2f);
            _graphic.SetBottomDominance(_state.BottomDominance);
        }

        public void SetSideSupportAmount(float value)
        {
            _state.SideSupportAmount = Mathf.Clamp(value, 0f, 2f);
            _graphic.SetSideSupportAmount(_state.SideSupportAmount);
        }

        public void SetTopSuppression(float value)
        {
            _state.TopSuppression = Mathf.Clamp01(value);
            _graphic.SetTopSuppression(_state.TopSuppression);
        }

        private void InitializeIfNeeded()
        {
            if (_initialized)
            {
                return;
            }

            _graphic = GetComponent<RoundedRectLiquidEdgeGraphic>();
            _random = new System.Random(_seed);
            _state = LiquidEdgeState.Sanitize(_state);
            _lastTime = ResolveTime();
            ResetMotion();
            _initialized = true;
        }

        private void ResetMotion()
        {
            _random = new System.Random(_seed);
            float[] strengths = _state.SegmentStrengths.ToArray();

            for (int i = 0; i < SegmentCount; i++)
            {
                _baseStrengths[i] = strengths[i];
                _currentStrengths[i] = strengths[i];
                _targetDrift[i] = 0f;
                _currentDrift[i] = 0f;
                _driftVelocity[i] = 0f;
                _driftSpeed[i] = Mathf.Lerp(_state.SegmentDriftSpeedRange.x, _state.SegmentDriftSpeedRange.y, Random01());
                _driftAmplitude[i] = Mathf.Lerp(IsBottomSegment(i) ? 0.82f : 0.42f, IsBottomSegment(i) ? 1.28f : 0.82f, Random01());
                _noiseOffset[i] = Random01() * 1000f;
                _lobes[i] = 0f;
            }

            int activeLobeCount = GetActiveLobeCount();

            for (int i = 0; i < _liquidLobes.Length; i++)
            {
                _liquidLobes[i] = i < activeLobeCount ? CreateLiquidLobe(i, activeLobeCount) : default;
            }
        }

        private void UpdateSegmentDrift(float now, float deltaTime)
        {
            for (int i = 0; i < SegmentCount; i++)
            {
                float organic = Mathf.PerlinNoise(_noiseOffset[i], now * _driftSpeed[i]) * 2f - 1f;
                float slowLocal = Mathf.PerlinNoise(_noiseOffset[i] + 31.7f, now * _driftSpeed[i] * 0.43f) * 2f - 1f;
                float neighborA = Mathf.PerlinNoise(_noiseOffset[PreviousSegment(i)], now * _driftSpeed[i] * 0.61f) * 2f - 1f;
                float neighborB = Mathf.PerlinNoise(_noiseOffset[NextSegment(i)], now * _driftSpeed[i] * 0.69f + 7.1f) * 2f - 1f;
                float neighborBlend = (neighborA + neighborB) * (IsBottomSegment(i) ? 0.12f : 0.18f);
                float localBlend = organic * 0.66f + slowLocal * 0.18f + neighborBlend;
                _targetDrift[i] = localBlend * _state.SegmentDriftAmplitude * _driftAmplitude[i];

                float smoothTime = Mathf.Lerp(0.9f, 2.6f, Mathf.InverseLerp(_state.SegmentDriftSpeedRange.y, _state.SegmentDriftSpeedRange.x, _driftSpeed[i]));
                _currentDrift[i] = Mathf.SmoothDamp(_currentDrift[i], _targetDrift[i], ref _driftVelocity[i], smoothTime, 4f, Mathf.Max(deltaTime, 0.0001f));
            }
        }

        private void UpdateLiquidLobes(float now)
        {
            Array.Clear(_lobes, 0, _lobes.Length);

            int activeLobeCount = GetActiveLobeCount();

            for (int i = 0; i < activeLobeCount; i++)
            {
                LiquidLobe lobe = _liquidLobes[i];
                float drift = Mathf.Sin(now * lobe.DriftSpeed + lobe.DriftPhase) * lobe.DriftAmount;
                drift += Mathf.Sin(now * lobe.SecondarySpeed + lobe.SecondaryPhase) * lobe.DriftAmount * 0.38f;
                float center = Mathf.Repeat(lobe.BaseCenter + drift, 1f);
                float pulse = 0.5f + 0.5f * Mathf.Sin(now * lobe.PulseSpeed + lobe.PulsePhase);
                pulse = Mathf.Lerp(0.58f, 1f, pulse);
                float amplitude = lobe.Amplitude * pulse;

                if (lobe.Side == LobeSide.Bottom)
                {
                    AddBottomLobe(center, lobe.Width, amplitude);
                }
                else
                {
                    AddSideLobe(lobe.Side, center, lobe.Width, amplitude);
                }
            }
        }

        private LiquidLobe CreateLiquidLobe(int index, int activeLobeCount)
        {
            int bottomLobeCount = GetBottomLobeCount(activeLobeCount);
            bool isBottom = index < bottomLobeCount;
            float frequencyScale = Mathf.Lerp(0.76f, 1.18f, Mathf.Clamp01(_state.HighlightZoneFrequency / 1.4f));
            float pulseScale = 1f / Mathf.Max(_state.HighlightZoneLifetime, 0.2f);

            if (isBottom)
            {
                float spacing = bottomLobeCount <= 1 ? 0f : 1f / bottomLobeCount;

                return new LiquidLobe
                {
                    Side = LobeSide.Bottom,
                    BaseCenter = Mathf.Repeat(index * spacing + Random01() * spacing * 0.62f, 1f),
                    Width = Mathf.Lerp(_state.HighlightZoneWidthRange.x, _state.HighlightZoneWidthRange.y, Random01()),
                    Amplitude = Mathf.Lerp(0.58f, 0.98f, Random01()),
                    DriftAmount = Mathf.Lerp(0.08f, 0.22f, Random01()),
                    DriftSpeed = Mathf.Lerp(0.26f, 0.72f, Random01()) * frequencyScale * _state.HighlightZoneDriftSpeed,
                    SecondarySpeed = Mathf.Lerp(0.14f, 0.38f, Random01()) * frequencyScale * _state.HighlightZoneDriftSpeed,
                    PulseSpeed = Mathf.Lerp(0.38f, 0.72f, Random01()) * frequencyScale * pulseScale,
                    DriftPhase = Random01() * Mathf.PI * 2f,
                    SecondaryPhase = Random01() * Mathf.PI * 2f,
                    PulsePhase = Random01() * Mathf.PI * 2f
                };
            }

            int sideIndex = index - bottomLobeCount;
            return new LiquidLobe
            {
                Side = sideIndex % 2 == 0 ? LobeSide.Left : LobeSide.Right,
                BaseCenter = Mathf.Lerp(0.08f, 0.42f, Random01()),
                Width = Mathf.Lerp(_state.HighlightZoneWidthRange.x * 0.85f, _state.HighlightZoneWidthRange.y, Random01()),
                Amplitude = Mathf.Lerp(0.2f, 0.4f, Random01()),
                DriftAmount = Mathf.Lerp(0.06f, 0.14f, Random01()),
                DriftSpeed = Mathf.Lerp(0.18f, 0.44f, Random01()) * frequencyScale * _state.HighlightZoneDriftSpeed,
                SecondarySpeed = Mathf.Lerp(0.1f, 0.28f, Random01()) * frequencyScale * _state.HighlightZoneDriftSpeed,
                PulseSpeed = Mathf.Lerp(0.24f, 0.46f, Random01()) * frequencyScale * pulseScale,
                DriftPhase = Random01() * Mathf.PI * 2f,
                SecondaryPhase = Random01() * Mathf.PI * 2f,
                PulsePhase = Random01() * Mathf.PI * 2f
            };
        }

        private void AddBottomLobe(float center, float width, float amplitude)
        {
            for (int i = 0; i < 5; i++)
            {
                float coord = i / 4f;
                float distance = CircularDistance01(coord, center);
                float value = Gaussian01(distance, width) * amplitude;
                AddLobe(6 + i, value);

                if (i > 0)
                {
                    AddLobe(6 + i - 1, value * 0.18f);
                }

                if (i < 4)
                {
                    AddLobe(6 + i + 1, value * 0.18f);
                }
            }
        }

        private void AddSideLobe(LobeSide side, float center, float width, float amplitude)
        {
            int offset = side == LobeSide.Left ? 0 : 3;

            for (int i = 0; i < 3; i++)
            {
                float coordFromBottom = 1f - i / 2f;
                float distance = Mathf.Abs(coordFromBottom - center);
                float topSuppression = i == 0 ? 0.18f : 1f;
                float value = Gaussian01(distance, width) * amplitude * topSuppression;
                AddLobe(offset + i, value);

                if (i > 0)
                {
                    AddLobe(offset + i - 1, value * 0.14f);
                }

                if (i < 2)
                {
                    AddLobe(offset + i + 1, value * 0.14f);
                }
            }
        }

        private void ComposeRuntimeSegments()
        {
            for (int i = 0; i < SegmentCount; i++)
            {
                float localLobe = _lobes[i] * _state.HighlightZoneIntensity;
                float shapeScale = (IsBottomSegment(i) ? 0.32f : 0.12f) * _state.HighlightZoneShapeInfluence;
                _currentStrengths[i] = Mathf.Clamp(_baseStrengths[i] + _currentDrift[i] * 0.42f + localLobe * shapeScale, 0f, 2f);
            }
        }

        private void AddLobe(int index, float value)
        {
            _lobes[index] = Mathf.Clamp01(_lobes[index] + value);
        }

        private static float Gaussian01(float distance, float width)
        {
            float normalized = distance / Mathf.Max(width, 0.0001f);
            return Mathf.Exp(-normalized * normalized * 2.2f);
        }

        private static float CircularDistance01(float a, float b)
        {
            float distance = Mathf.Abs(a - b);
            return Mathf.Min(distance, 1f - distance);
        }

        private int GetActiveLobeCount()
        {
            return Mathf.Clamp(_state.SimultaneousHighlightZoneCount, 1, _liquidLobes.Length);
        }

        private static int GetBottomLobeCount(int activeLobeCount)
        {
            return Mathf.Clamp(activeLobeCount - 2, 2, 4);
        }

        private static bool IsBottomSegment(int index)
        {
            return index >= 6;
        }

        private static int PreviousSegment(int index)
        {
            if (index >= 7)
            {
                return index - 1;
            }

            if (index == 6)
            {
                return 6;
            }

            if (index == 3)
            {
                return 3;
            }

            return Mathf.Max(0, index - 1);
        }

        private static int NextSegment(int index)
        {
            if (index >= 6)
            {
                return Mathf.Min(10, index + 1);
            }

            if (index == 2)
            {
                return 2;
            }

            if (index == 5)
            {
                return 5;
            }

            return index + 1;
        }

        private float ResolveTime()
        {
            if (_graphic != null && _graphic.UseUnscaledTime)
            {
                return Application.isPlaying ? Time.unscaledTime : Time.realtimeSinceStartup;
            }

            return Application.isPlaying ? Time.time : Time.realtimeSinceStartup;
        }

        private float Random01()
        {
            return (float)_random.NextDouble();
        }

        public static LiquidEdgeState GetPresetState(LiquidEdgePreset preset)
        {
            switch (preset)
            {
                case LiquidEdgePreset.Calm:
                    return LiquidEdgeState.Calm();
                case LiquidEdgePreset.Rich:
                    return LiquidEdgeState.Rich();
                case LiquidEdgePreset.Overdriven:
                    return LiquidEdgeState.Overdriven();
                default:
                    return LiquidEdgeState.Living();
            }
        }

        private enum LobeSide
        {
            Bottom,
            Left,
            Right
        }

        private struct LiquidLobe
        {
            public LobeSide Side;
            public float BaseCenter;
            public float Width;
            public float Amplitude;
            public float DriftAmount;
            public float DriftSpeed;
            public float SecondarySpeed;
            public float PulseSpeed;
            public float DriftPhase;
            public float SecondaryPhase;
            public float PulsePhase;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _seed = Mathf.Max(1, _seed);
            _state = LiquidEdgeState.Sanitize(_state);
            _initialized = false;

            if (isActiveAndEnabled)
            {
                InitializeIfNeeded();

                if (_applyPresetOnEnable && !Application.isPlaying)
                {
                    ApplyPreset(_preset);
                }
                else
                {
                    ApplyState(_state, true);
                }
            }
        }
#endif
    }
}
