using UnityEngine;

namespace enp_unity_extensions.Runtime.Scripts.UI.Effects.LiquidEdge
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RoundedRectPatchLiquidGraphic))]
    [AddComponentMenu("UI/ENP/Rounded Rect Patch Liquid Controller")]
    public sealed class RoundedRectPatchLiquidController : MonoBehaviour
    {
        private const int MaxPatches = RoundedRectPatchLiquidGraphic.MaxPatches;

        [SerializeField] private LiquidEdgePreset _preset = LiquidEdgePreset.Living;
        [SerializeField] private bool _applyPresetOnEnable = true;
        [SerializeField] private int _seed = 24191;
        [SerializeField] private PatchLiquidEdgeState _state = PatchLiquidEdgeState.Living();

        private readonly Vector4[] _patchA = new Vector4[MaxPatches];
        private readonly Vector4[] _patchB = new Vector4[MaxPatches];
        private readonly PatchInstance[] _patches = new PatchInstance[MaxPatches];

        private RoundedRectPatchLiquidGraphic _graphic;
        private System.Random _random;
        private float _lastTime;
        private int _patchCount;
        private bool _initialized;

        public LiquidEdgePreset Preset => _preset;
        public PatchLiquidEdgeState State => _state;

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
            _state = PatchLiquidEdgeState.Sanitize(_state);

            float now = ResolveTime();
            float motionTime = now * _state.GlobalMotionSpeed;
            _lastTime = now;

            PackPatches(motionTime);
            _graphic.SetRuntimePatches(_patchA, _patchB, _patchCount, motionTime + _seed * 0.013f);
        }

        public void ApplyPreset(LiquidEdgePreset preset)
        {
            _preset = preset;
            ApplyState(GetPresetState(preset), true);
        }

        public void ApplyState(PatchLiquidEdgeState state, bool restartMotion)
        {
            InitializeIfNeeded();
            _state = PatchLiquidEdgeState.Sanitize(state);
            _graphic.SetState(_state);

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

        private void InitializeIfNeeded()
        {
            if (_initialized)
            {
                return;
            }

            _graphic = GetComponent<RoundedRectPatchLiquidGraphic>();
            _random = new System.Random(_seed);
            _state = PatchLiquidEdgeState.Sanitize(_state);
            _lastTime = ResolveTime();
            ResetMotion();
            _initialized = true;
        }

        private void ResetMotion()
        {
            _random = new System.Random(_seed);
            _patchCount = 0;

            for (int i = 0; i < MaxPatches; i++)
            {
                _patches[i] = default;
                _patchA[i] = Vector4.zero;
                _patchB[i] = Vector4.zero;
            }

            AddRegionPatches(PatchSide.Bottom, _state.BottomPatchCount);
            AddRegionPatches(PatchSide.Left, _state.SidePatchCountPerSide);
            AddRegionPatches(PatchSide.Right, _state.SidePatchCountPerSide);
            AddRegionPatches(PatchSide.Top, _state.TopPatchCount);
        }

        private void AddRegionPatches(PatchSide side, int count)
        {
            count = Mathf.Max(0, count);

            for (int i = 0; i < count && _patchCount < MaxPatches; i++)
            {
                float spacing = count <= 1 ? 0.5f : 1f / count;
                float baseCenter = count <= 1 ? RandomRange(0.22f, 0.78f) : Mathf.Repeat(i * spacing + RandomRange(0.08f, 0.72f) * spacing, 1f);
                float sideScale = GetSideScale(side);
                float topScale = side == PatchSide.Top ? 1f - _state.TopSuppression : 1f;
                float widthScale = side == PatchSide.Bottom ? 0.82f : 0.62f;
                float thicknessScale = side == PatchSide.Bottom ? _state.BottomThicknessMultiplier : _state.SideThicknessMultiplier;

                _patches[_patchCount++] = new PatchInstance
                {
                    Side = side,
                    BaseCenter = baseCenter,
                    Width = RandomRange(_state.PatchWidthRange.x, _state.PatchWidthRange.y) * widthScale,
                    Depth = _state.InnerDepth * RandomRange(side == PatchSide.Bottom ? 0.82f : 0.5f, side == PatchSide.Bottom ? 1.04f : 0.72f) * thicknessScale,
                    Intensity = RandomRange(0.72f, 1.08f) * sideScale * topScale,
                    ColorMix = RandomRange(0.42f, 0.86f) * sideScale,
                    Shape = RandomRange(0.72f, 1.15f) * sideScale,
                    DriftAmount = RandomRange(side == PatchSide.Bottom ? 0.08f : 0.04f, side == PatchSide.Bottom ? 0.22f : 0.12f),
                    DriftSpeed = RandomRange(_state.PatchDriftSpeedRange.x, _state.PatchDriftSpeedRange.y),
                    SecondarySpeed = RandomRange(_state.PatchDriftSpeedRange.x * 0.32f, _state.PatchDriftSpeedRange.y * 0.58f),
                    PulseSpeed = RandomRange(_state.PatchDriftSpeedRange.x * 0.64f, _state.PatchDriftSpeedRange.y * 1.12f),
                    Phase = RandomRange(0f, Mathf.PI * 2f),
                    SecondaryPhase = RandomRange(0f, Mathf.PI * 2f),
                    PulsePhase = RandomRange(0f, Mathf.PI * 2f)
                };
            }
        }

        private void PackPatches(float time)
        {
            for (int i = 0; i < MaxPatches; i++)
            {
                _patchA[i] = Vector4.zero;
                _patchB[i] = Vector4.zero;
            }

            for (int i = 0; i < _patchCount; i++)
            {
                PatchInstance patch = _patches[i];
                float drift = Mathf.Sin(time * patch.DriftSpeed + patch.Phase) * patch.DriftAmount;
                drift += Mathf.Sin(time * patch.SecondarySpeed + patch.SecondaryPhase) * patch.DriftAmount * 0.46f;
                float center = patch.Side == PatchSide.Bottom || patch.Side == PatchSide.Top
                    ? Mathf.Repeat(patch.BaseCenter + drift, 1f)
                    : Mathf.Clamp01(patch.BaseCenter + drift);

                float pulse = 0.5f + 0.5f * Mathf.Sin(time * patch.PulseSpeed + patch.PulsePhase);
                pulse = Mathf.Lerp(0.72f, 1.12f, pulse);
                float softShape = 0.5f + 0.5f * Mathf.Sin(time * patch.PulseSpeed * 0.63f + patch.SecondaryPhase);
                float depth = patch.Depth * Mathf.Lerp(0.82f, 1.18f, softShape) * _state.PatchShapeInfluence;
                float width = patch.Width * Mathf.Lerp(0.88f, 1.14f, softShape);

                _patchA[i] = new Vector4((float)patch.Side, center, width, depth);
                _patchB[i] = new Vector4(
                    patch.Intensity * pulse * _state.PatchAmplitude,
                    patch.ColorMix * _state.PatchColorInfluence,
                    patch.Shape * _state.PatchShapeInfluence,
                    0f);
            }
        }

        private float GetSideScale(PatchSide side)
        {
            switch (side)
            {
                case PatchSide.Bottom:
                    return _state.BottomDominance;
                case PatchSide.Left:
                case PatchSide.Right:
                    return _state.SideSupport;
                default:
                    return 1f - _state.TopSuppression;
            }
        }

        private float ResolveTime()
        {
            return _graphic != null && _graphic.UseUnscaledTime
                ? (Application.isPlaying ? Time.unscaledTime : Time.realtimeSinceStartup)
                : (Application.isPlaying ? Time.time : Time.realtimeSinceStartup);
        }

        private float RandomRange(float min, float max)
        {
            return Mathf.Lerp(min, max, (float)_random.NextDouble());
        }

        public static PatchLiquidEdgeState GetPresetState(LiquidEdgePreset preset)
        {
            switch (preset)
            {
                case LiquidEdgePreset.Calm:
                    return PatchLiquidEdgeState.Calm();
                case LiquidEdgePreset.Rich:
                    return PatchLiquidEdgeState.Rich();
                case LiquidEdgePreset.Overdriven:
                    return PatchLiquidEdgeState.Overdriven();
                default:
                    return PatchLiquidEdgeState.Living();
            }
        }

        private enum PatchSide
        {
            Bottom = 0,
            Left = 1,
            Right = 2,
            Top = 3
        }

        private struct PatchInstance
        {
            public PatchSide Side;
            public float BaseCenter;
            public float Width;
            public float Depth;
            public float Intensity;
            public float ColorMix;
            public float Shape;
            public float DriftAmount;
            public float DriftSpeed;
            public float SecondarySpeed;
            public float PulseSpeed;
            public float Phase;
            public float SecondaryPhase;
            public float PulsePhase;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _seed = Mathf.Max(1, _seed);
            _state = PatchLiquidEdgeState.Sanitize(_state);
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
