using System;
using UnityEngine;

namespace enp_unity_extensions.Runtime.Scripts.UI.Effects.Wash
{
    public sealed class AsymmetricInnerWashMotionGenerator
    {
        public const int BottomSegmentCount = 5;
        public const int SideSegmentCount = 3;

        private static readonly float[] _bottomCoords = new float[] { 0.10f, 0.30f, 0.50f, 0.70f, 0.90f };
        private static readonly float[] _sideCoords = new float[] { 0.16f, 0.50f, 0.84f };

        private readonly float[] _bottomStrengths = new float[BottomSegmentCount];
        private readonly float[] _leftStrengths = new float[SideSegmentCount];
        private readonly float[] _rightStrengths = new float[SideSegmentCount];

        private readonly float[] _bottomAccents = new float[BottomSegmentCount];
        private readonly float[] _leftAccents = new float[SideSegmentCount];
        private readonly float[] _rightAccents = new float[SideSegmentCount];

        private readonly float[] _bottomRaw = new float[BottomSegmentCount];
        private readonly float[] _leftRaw = new float[SideSegmentCount];
        private readonly float[] _rightRaw = new float[SideSegmentCount];

        private readonly SegmentState[] _bottomSegments = new SegmentState[BottomSegmentCount];
        private readonly SegmentState[] _leftSegments = new SegmentState[SideSegmentCount];
        private readonly SegmentState[] _rightSegments = new SegmentState[SideSegmentCount];

        private AccentZone _bottomAccentA;
        private AccentZone _bottomAccentB;
        private AccentZone _leftAccent;
        private AccentZone _rightAccent;

        private float _bottomAccentCooldownA;
        private float _bottomAccentCooldownB;
        private float _leftAccentCooldown;
        private float _rightAccentCooldown;

        private uint _rngState;
        private float _time;
        private float _intensity;
        private Color _bottomColor;
        private Color _accentBottomColor;

        public float[] BottomStrengths => _bottomStrengths;
        public float[] LeftStrengths => _leftStrengths;
        public float[] RightStrengths => _rightStrengths;

        public float[] BottomAccents => _bottomAccents;
        public float[] LeftAccents => _leftAccents;
        public float[] RightAccents => _rightAccents;

        public float Intensity => _intensity;
        public Color BottomColor => _bottomColor;
        public Color AccentBottomColor => _accentBottomColor;

        public AsymmetricInnerWashMotionGenerator(int seed = 1)
        {
            Reset(seed);
        }

        public void Reset(int seed)
        {
            _rngState = (uint)(seed == 0 ? 1 : seed);
            _time = 0f;
            _intensity = 0f;
            _bottomColor = Color.white;
            _accentBottomColor = Color.white;

            ClearArray(_bottomStrengths, BottomSegmentCount);
            ClearArray(_leftStrengths, SideSegmentCount);
            ClearArray(_rightStrengths, SideSegmentCount);
            ClearArray(_bottomAccents, BottomSegmentCount);
            ClearArray(_leftAccents, SideSegmentCount);
            ClearArray(_rightAccents, SideSegmentCount);
            ClearArray(_bottomRaw, BottomSegmentCount);
            ClearArray(_leftRaw, SideSegmentCount);
            ClearArray(_rightRaw, SideSegmentCount);

            InitializeSegments(_bottomSegments, _bottomCoords, BottomSegmentCount, 0.17f, 0.31f, 0.88f, 1.14f, 0.10f);
            InitializeSegments(_leftSegments, _sideCoords, SideSegmentCount, 0.14f, 0.25f, 0.84f, 1.05f, 0.08f);
            InitializeSegments(_rightSegments, _sideCoords, SideSegmentCount, 0.14f, 0.25f, 0.84f, 1.05f, 0.08f);

            _bottomAccentA = default;
            _bottomAccentB = default;
            _leftAccent = default;
            _rightAccent = default;

            _bottomAccentCooldownA = 0.35f + Next01() * 0.75f;
            _bottomAccentCooldownB = 0.85f + Next01() * 1.15f;
            _leftAccentCooldown = 0.90f + Next01() * 1.30f;
            _rightAccentCooldown = 1.10f + Next01() * 1.30f;
        }

        public void Update(float deltaTime, float streak01, AsymmetricInnerWashState baseState)
        {
            if (deltaTime < 0f)
            {
                deltaTime = 0f;
            }

            streak01 = Mathf.Clamp01(streak01);
            baseState = AsymmetricInnerWashState.Sanitize(baseState);

            _time += deltaTime;

            UpdateSideStrengths(
                _bottomSegments,
                _bottomRaw,
                _bottomStrengths,
                BottomSegmentCount,
                baseState.BottomStrength,
                streak01,
                deltaTime,
                1.00f,
                0.046f,
                0.155f,
                0.040f,
                0.38f,
                0.085f);

            UpdateSideStrengths(
                _leftSegments,
                _leftRaw,
                _leftStrengths,
                SideSegmentCount,
                baseState.LeftStrength,
                streak01,
                deltaTime,
                0.62f,
                0.031f,
                0.102f,
                0.020f,
                0.30f,
                0.073f);

            UpdateSideStrengths(
                _rightSegments,
                _rightRaw,
                _rightStrengths,
                SideSegmentCount,
                baseState.RightStrength,
                streak01,
                deltaTime,
                0.62f,
                0.031f,
                0.102f,
                0.020f,
                0.30f,
                0.073f);

            UpdateAccentSchedulers(deltaTime, streak01);

            ClearArray(_bottomAccents, BottomSegmentCount);
            ClearArray(_leftAccents, SideSegmentCount);
            ClearArray(_rightAccents, SideSegmentCount);

            float bottomAccentPeak = 0f;
            float bottomAccentWarmth = 0f;
            float leftAccentPeak = 0f;
            float rightAccentPeak = 0f;

            ApplyAccentZone(
                _bottomAccentA,
                _bottomCoords,
                BottomSegmentCount,
                _bottomAccents,
                _bottomStrengths,
                0.36f + streak01 * 0.20f,
                ref bottomAccentPeak,
                ref bottomAccentWarmth);

            ApplyAccentZone(
                _bottomAccentB,
                _bottomCoords,
                BottomSegmentCount,
                _bottomAccents,
                _bottomStrengths,
                0.36f + streak01 * 0.20f,
                ref bottomAccentPeak,
                ref bottomAccentWarmth);

            ApplyAccentZone(
                _leftAccent,
                _sideCoords,
                SideSegmentCount,
                _leftAccents,
                _leftStrengths,
                0.20f + streak01 * 0.12f,
                ref leftAccentPeak,
                ref bottomAccentWarmth);

            ApplyAccentZone(
                _rightAccent,
                _sideCoords,
                SideSegmentCount,
                _rightAccents,
                _rightStrengths,
                0.20f + streak01 * 0.12f,
                ref rightAccentPeak,
                ref bottomAccentWarmth);

            float ambientLift = (SampleSignedNoise(41.7f, _time * 0.058f) * 0.5f + 0.5f) * Mathf.Lerp(0.008f, 0.028f, streak01);
            _bottomColor = Color.LerpUnclamped(baseState.BottomColor, Color.LerpUnclamped(baseState.BottomColor, Color.white, 0.16f), ambientLift);

            float accentColorAmount = Mathf.Clamp01(bottomAccentPeak * Mathf.Lerp(0.85f, 1.30f, streak01));
            float accentWarmth = Mathf.Clamp01(bottomAccentWarmth * Mathf.Lerp(0.90f, 1.35f, streak01));
            _accentBottomColor = BuildAccentBottomColor(_bottomColor, accentColorAmount, accentWarmth);

            float ambientPulse = SampleSignedNoise(17.2f, _time * 0.081f) * 0.5f + 0.5f;
            float accentEnergy = Mathf.Max(bottomAccentPeak, Mathf.Max(leftAccentPeak, rightAccentPeak) * 0.72f);
            _intensity = Mathf.Clamp01(baseState.Intensity + ambientPulse * Mathf.Lerp(0.010f, 0.040f, streak01) + accentEnergy * 0.085f);
        }

        public void ApplyTo(RoundedRectAsymmetricInnerWashGraphic graphic)
        {
            graphic.SetSegmentedRuntimeAnimatedValues(
                _bottomColor,
                _accentBottomColor,
                _intensity,
                _bottomStrengths,
                _leftStrengths,
                _rightStrengths,
                _bottomAccents,
                _leftAccents,
                _rightAccents);
        }

        private void UpdateSideStrengths(
            SegmentState[] segments,
            float[] rawValues,
            float[] strengths,
            int count,
            float baseStrength,
            float streak01,
            float deltaTime,
            float sideScale,
            float ambientBaseAmplitude,
            float driftBaseAmplitude,
            float microBaseAmplitude,
            float coupling,
            float ambientSpeed)
        {
            float ambientAmplitude = ambientBaseAmplitude * (0.85f + streak01 * 0.55f) * sideScale;
            float driftAmplitude = driftBaseAmplitude * (0.75f + streak01 * 0.95f) * sideScale;
            float microAmplitude = microBaseAmplitude * streak01 * sideScale;

            for (int i = 0; i < count; i++)
            {
                SegmentState segment = segments[i];

                float driftTarget = SampleSignedNoise(segment._noiseOffsetA * 0.17f, _time * (0.045f + segment._speed * 0.05f)) * 0.22f;
                segment._drift = Mathf.SmoothDamp(segment._drift, driftTarget, ref segment._driftVelocity, 1.35f, Mathf.Infinity, deltaTime);

                float ambient0 = SampleSignedNoise(segment._noiseOffsetA + segment._coord * 1.37f, _time * ambientSpeed);
                float ambient1 = SampleSignedNoise(segment._noiseOffsetB + segment._coord * 2.11f, _time * (ambientSpeed * 0.57f + 0.009f));
                float ambient = ambient0 * 0.68f + ambient1 * 0.32f;

                float local0 = SampleSignedNoise(segment._noiseOffsetA + segment._drift * 2.8f + 7.3f, _time * segment._speed + segment._phase);
                float local1 = SampleSignedNoise(segment._noiseOffsetB + segment._coord * 3.6f + segment._drift * 3.2f, _time * (segment._speed * 0.63f + 0.041f) + segment._phase * 0.61f);
                float local = local0 * 0.72f + local1 * 0.28f;

                float micro = SampleSignedNoise(segment._noiseOffsetA * 1.73f + segment._coord * 5.4f, _time * (segment._speed * 1.31f + 0.11f) + segment._phase * 1.17f);

                rawValues[i] = ambient * ambientAmplitude
                    + local * driftAmplitude * segment._amplitude
                    + micro * microAmplitude
                    + segment._bias * 0.08f;

                segments[i] = segment;
            }

            for (int i = 0; i < count; i++)
            {
                float sum = rawValues[i];
                float weight = 1f;

                if (i > 0)
                {
                    sum += rawValues[i - 1] * coupling;
                    weight += coupling;
                }

                if (i < count - 1)
                {
                    sum += rawValues[i + 1] * coupling;
                    weight += coupling;
                }

                float coupled = Mathf.Lerp(rawValues[i], sum / weight, 0.65f);
                coupled = Mathf.Clamp(coupled, -0.42f * sideScale, 0.52f * sideScale + 0.08f * streak01);

                float value = baseStrength * (1f + coupled);
                strengths[i] = Mathf.Clamp(value, 0f, 2f);
            }
        }

        private void UpdateAccentSchedulers(float deltaTime, float streak01)
        {
            UpdateAccent(
                ref _bottomAccentA,
                ref _bottomAccentCooldownA,
                deltaTime,
                streak01,
                _bottomCoords,
                BottomSegmentCount,
                0.45f,
                1.10f,
                0.95f,
                1.60f,
                0.18f,
                0.34f,
                0.16f,
                0.30f,
                0.18f,
                0.58f,
                0.08f);

            UpdateAccent(
                ref _bottomAccentB,
                ref _bottomAccentCooldownB,
                deltaTime,
                streak01,
                _bottomCoords,
                BottomSegmentCount,
                0.70f,
                1.45f,
                1.05f,
                1.85f,
                0.20f,
                0.36f,
                0.14f,
                0.28f,
                0.14f,
                0.52f,
                0.08f);

            UpdateAccent(
                ref _leftAccent,
                ref _leftAccentCooldown,
                deltaTime,
                streak01,
                _sideCoords,
                SideSegmentCount,
                0.95f,
                1.75f,
                0.80f,
                1.35f,
                0.26f,
                0.46f,
                0.09f,
                0.18f,
                0.08f,
                0.26f,
                0.05f);

            UpdateAccent(
                ref _rightAccent,
                ref _rightAccentCooldown,
                deltaTime,
                streak01,
                _sideCoords,
                SideSegmentCount,
                0.95f,
                1.75f,
                0.80f,
                1.35f,
                0.26f,
                0.46f,
                0.09f,
                0.18f,
                0.08f,
                0.26f,
                0.05f);
        }

        private void UpdateAccent(
            ref AccentZone zone,
            ref float cooldown,
            float deltaTime,
            float streak01,
            float[] anchors,
            int anchorCount,
            float minDelay,
            float maxDelay,
            float minDuration,
            float maxDuration,
            float minWidth,
            float maxWidth,
            float minStrength,
            float maxStrength,
            float minWarmth,
            float maxWarmth,
            float edgePadding)
        {
            if (zone._isActive)
            {
                zone._age += deltaTime;

                if (zone._age >= zone._duration)
                {
                    zone._isActive = false;
                    zone._age = zone._duration;
                }
            }

            cooldown -= deltaTime;

            if (!zone._isActive && cooldown <= 0f)
            {
                SpawnAccent(
                    ref zone,
                    ref cooldown,
                    streak01,
                    anchors,
                    anchorCount,
                    minDelay,
                    maxDelay,
                    minDuration,
                    maxDuration,
                    minWidth,
                    maxWidth,
                    minStrength,
                    maxStrength,
                    minWarmth,
                    maxWarmth,
                    edgePadding);
            }
        }

        private void SpawnAccent(
            ref AccentZone zone,
            ref float cooldown,
            float streak01,
            float[] anchors,
            int anchorCount,
            float minDelay,
            float maxDelay,
            float minDuration,
            float maxDuration,
            float minWidth,
            float maxWidth,
            float minStrength,
            float maxStrength,
            float minWarmth,
            float maxWarmth,
            float edgePadding)
        {
            float width = Mathf.Lerp(minWidth, maxWidth, Next01());
            float duration = Mathf.Lerp(minDuration, maxDuration, Next01());
            float strength = Mathf.Lerp(minStrength, maxStrength, Next01()) * (0.85f + streak01 * 0.75f);
            float warmth = Mathf.Lerp(minWarmth, maxWarmth, Next01()) * (0.75f + streak01 * 0.90f);

            float center;
            if (Next01() < 0.65f)
            {
                int anchorIndex = Mathf.Min(anchorCount - 1, (int)(Next01() * anchorCount));
                float offset = (Next01() - 0.5f) * width * 0.55f;
                center = Mathf.Clamp01(anchors[anchorIndex] + offset);
            }
            else
            {
                center = Mathf.Lerp(edgePadding, 1f - edgePadding, Next01());
            }

            zone._isActive = true;
            zone._center = center;
            zone._width = width;
            zone._strength = strength;
            zone._warmth = warmth;
            zone._age = 0f;
            zone._duration = duration;

            float delay = Mathf.Lerp(minDelay, maxDelay, Next01());
            cooldown = duration + Mathf.Lerp(delay * 0.85f, delay, 0.5f + streak01 * 0.5f);
        }

        private void ApplyAccentZone(
            AccentZone zone,
            float[] coords,
            int count,
            float[] accents,
            float[] strengths,
            float strengthBoost,
            ref float peak,
            ref float warmthAccumulator)
        {
            if (!zone._isActive || zone._duration <= 0f)
            {
                return;
            }

            float life = Mathf.Clamp01(zone._age / zone._duration);
            float envelope = Mathf.Sin(life * Mathf.PI);
            envelope *= envelope;

            if (envelope <= 0f)
            {
                return;
            }

            float zoneValue = zone._strength * envelope;

            for (int i = 0; i < count; i++)
            {
                float spatial = EvaluateZone(coords[i], zone._center, zone._width);
                float value = zoneValue * spatial;

                if (value <= 0f)
                {
                    continue;
                }

                if (value > accents[i])
                {
                    accents[i] = value;
                }

                strengths[i] = Mathf.Clamp(strengths[i] + value * strengthBoost, 0f, 2f);

                if (value > peak)
                {
                    peak = value;
                }

                float warmthValue = value * zone._warmth;
                if (warmthValue > warmthAccumulator)
                {
                    warmthAccumulator = warmthValue;
                }
            }
        }

        private void InitializeSegments(
            SegmentState[] segments,
            float[] coords,
            int count,
            float minSpeed,
            float maxSpeed,
            float minAmplitude,
            float maxAmplitude,
            float biasRange)
        {
            for (int i = 0; i < count; i++)
            {
                segments[i] = new SegmentState
                {
                    _coord = coords[i],
                    _speed = Mathf.Lerp(minSpeed, maxSpeed, Next01()),
                    _phase = Next01() * 64f,
                    _amplitude = Mathf.Lerp(minAmplitude, maxAmplitude, Next01()),
                    _bias = Mathf.Lerp(-biasRange, biasRange, Next01()),
                    _drift = 0f,
                    _driftVelocity = 0f,
                    _noiseOffsetA = Next01() * 100f + 3.1f,
                    _noiseOffsetB = Next01() * 100f + 37.9f
                };
            }
        }

        private static float EvaluateZone(float coord, float center, float width)
        {
            float safeWidth = Mathf.Max(width, 0.0001f);
            float distance = Mathf.Abs(coord - center) / safeWidth;

            if (distance >= 1f)
            {
                return 0f;
            }

            float t = 1f - distance;
            return t * t * (3f - 2f * t);
        }

        private static Color BuildAccentBottomColor(Color baseColor, float accentAmount, float warmth)
        {
            Color brightened = Color.LerpUnclamped(baseColor, Color.white, 0.10f + accentAmount * 0.24f);

            float r = Mathf.Clamp01(brightened.r + 0.05f + warmth * 0.12f);
            float g = Mathf.Clamp01(brightened.g + 0.02f + warmth * 0.05f);
            float b = Mathf.Clamp01(brightened.b - warmth * 0.06f);

            Color warm = new Color(r, g, b, baseColor.a);
            Color result = Color.LerpUnclamped(brightened, warm, 0.25f + warmth * 0.45f);
            result.a = baseColor.a;
            return result;
        }

        private static float SampleSignedNoise(float x, float y)
        {
            return Mathf.PerlinNoise(x, y) * 2f - 1f;
        }

        private float Next01()
        {
            _rngState ^= _rngState << 13;
            _rngState ^= _rngState >> 17;
            _rngState ^= _rngState << 5;
            return (_rngState & 0x00FFFFFF) / 16777215f;
        }

        private static void ClearArray(float[] values, int count)
        {
            for (int i = 0; i < count; i++)
            {
                values[i] = 0f;
            }
        }

        private struct SegmentState
        {
            public float _coord;
            public float _speed;
            public float _phase;
            public float _amplitude;
            public float _bias;
            public float _drift;
            public float _driftVelocity;
            public float _noiseOffsetA;
            public float _noiseOffsetB;
        }

        private struct AccentZone
        {
            public bool _isActive;
            public float _center;
            public float _width;
            public float _strength;
            public float _warmth;
            public float _age;
            public float _duration;
        }
    }
}