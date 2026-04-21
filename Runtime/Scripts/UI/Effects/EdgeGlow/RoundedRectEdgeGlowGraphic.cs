using System;
using UnityEngine;
using UnityEngine.UI;

namespace enp_unity_extensions.Runtime.Scripts.UI.Effects.EdgeGlow
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("UI/ENP/Rounded Rect Edge Glow")]
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RoundedRectEdgeGlowGraphic : MaskableGraphic
    {
        private const string ShaderName = "UI/ENP/RoundedRectEdgeGlow";

        private static readonly int _topColorId = Shader.PropertyToID("_TopColor");
        private static readonly int _bottomColorId = Shader.PropertyToID("_BottomColor");
        private static readonly int _intensityId = Shader.PropertyToID("_Intensity");
        private static readonly int _thicknessId = Shader.PropertyToID("_Thickness");
        private static readonly int _softnessId = Shader.PropertyToID("_Softness");
        private static readonly int _roundnessId = Shader.PropertyToID("_Roundness");
        private static readonly int _centerClearId = Shader.PropertyToID("_CenterClear");
        private static readonly int _rectSizeId = Shader.PropertyToID("_RectSize");
        private static readonly int _rectCenterId = Shader.PropertyToID("_RectCenter");

        [SerializeField] private EdgeGlowState _initialState = default;
        [SerializeField] private EdgeGlowState _neutralState = default;
        [SerializeField] private bool _useUnscaledTime = true;

        private Material _runtimeMaterial;
        private EdgeGlowState _baseState;
        private EdgeGlowState _transitionFromState;
        private EdgeGlowState _transitionToState;
        private EdgeGlowPulse _pulse;
        private Vector2 _lastRectSize;
        private Vector2 _lastRectCenter;
        private bool _baseTransitionActive;
        private bool _pulseActive;
        private bool _stateInitialized;
        private bool _materialStateDirty;
        private float _baseTransitionElapsed;
        private float _baseTransitionDuration;
        private float _pulseElapsed;
        private float _pulseDuration;

        public override Texture mainTexture => Texture2D.whiteTexture;

        public EdgeGlowState BaseState => _baseState;

        public bool IsPulseActive => _pulseActive;

        protected override void Awake()
        {
            base.Awake();
            InitializeStatesIfNeeded();
            EnsureMaterial();
            ApplyVisualState(true);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            InitializeStatesIfNeeded();
            EnsureMaterial();
            SetAllDirty();
            ApplyVisualState(true);
        }

        protected override void OnDisable()
        {
            _baseTransitionActive = false;
            _pulseActive = false;
            _pulseElapsed = 0f;
            _pulseDuration = 0f;
            _materialStateDirty = true;
            base.OnDisable();
        }

        protected override void OnDestroy()
        {
            material = null;

            if (_runtimeMaterial != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(_runtimeMaterial);
                }
                else
                {
                    DestroyImmediate(_runtimeMaterial);
                }

                _runtimeMaterial = null;
            }

            base.OnDestroy();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            var rect = GetPixelAdjustedRect();

            vh.Clear();

            var vertex = UIVertex.simpleVert;
            vertex.color = color;

            vertex.position = new Vector2(rect.xMin, rect.yMin);
            vertex.uv0 = new Vector2(0f, 0f);
            vh.AddVert(vertex);

            vertex.position = new Vector2(rect.xMin, rect.yMax);
            vertex.uv0 = new Vector2(0f, 1f);
            vh.AddVert(vertex);

            vertex.position = new Vector2(rect.xMax, rect.yMax);
            vertex.uv0 = new Vector2(1f, 1f);
            vh.AddVert(vertex);

            vertex.position = new Vector2(rect.xMax, rect.yMin);
            vertex.uv0 = new Vector2(1f, 0f);
            vh.AddVert(vertex);

            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(2, 3, 0);
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            _materialStateDirty = true;
            SetVerticesDirty();
            ApplyVisualState(false);
        }

        public void SetState(EdgeGlowState state)
        {
            _baseTransitionActive = false;
            _baseState = EdgeGlowState.Sanitize(state);
            _materialStateDirty = true;
            ApplyVisualState(false);
        }

        public void SetGradient(Color topColor, Color bottomColor)
        {
            _baseTransitionActive = false;
            _baseState.TopColor = topColor;
            _baseState.BottomColor = bottomColor;
            _materialStateDirty = true;
            ApplyVisualState(false);
        }

        public void SetIntensity(float intensity)
        {
            _baseTransitionActive = false;
            _baseState.Intensity = Mathf.Clamp01(intensity);
            _materialStateDirty = true;
            ApplyVisualState(false);
        }

        public void SetShape(float thickness, float softness, float cornerRoundness, float centerClear)
        {
            _baseTransitionActive = false;
            _baseState.Thickness = Mathf.Max(0f, thickness);
            _baseState.Softness = Mathf.Clamp01(softness);
            _baseState.CornerRoundness = Mathf.Max(0f, cornerRoundness);
            _baseState.CenterClear = Mathf.Clamp01(centerClear);
            _materialStateDirty = true;
            ApplyVisualState(false);
        }

        public void AnimateState(EdgeGlowState targetState, float duration)
        {
            targetState = EdgeGlowState.Sanitize(targetState);

            if (duration <= 0f)
            {
                SetState(targetState);
                return;
            }

            _transitionFromState = _baseState;
            _transitionToState = targetState;
            _baseTransitionDuration = duration;
            _baseTransitionElapsed = 0f;
            _baseTransitionActive = true;
            _materialStateDirty = true;
            ApplyVisualState(false);
        }

        public void PlayPulse(EdgeGlowPulse pulse)
        {
            _pulse = EdgeGlowPulse.Sanitize(pulse);
            _pulseElapsed = 0f;
            _pulseDuration = _pulse.Duration;
            _pulseActive = _pulseDuration > 0f;
            _materialStateDirty = true;
            ApplyVisualState(false);
        }

        public void StopPulse()
        {
            _pulseActive = false;
            _pulseElapsed = 0f;
            _pulseDuration = 0f;
            _materialStateDirty = true;
            ApplyVisualState(false);
        }

        public void ResetToNeutral(float duration = 0f)
        {
            StopPulse();
            AnimateState(_neutralState, duration);
        }

        private void Reset()
        {
            _initialState = EdgeGlowState.CreateDefault();
            _neutralState = EdgeGlowState.CreateNeutral();
            color = Color.white;
            raycastTarget = false;
            _stateInitialized = false;
            _materialStateDirty = true;
        }

        private void Update()
        {
            if (!_materialStateDirty && !_baseTransitionActive && !_pulseActive)
            {
                return;
            }

            var changed = false;

            if (_baseTransitionActive && Application.isPlaying)
            {
                var deltaTime = _useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                _baseTransitionElapsed += deltaTime;

                var t = _baseTransitionDuration <= 0f
                    ? 1f
                    : Mathf.Clamp01(_baseTransitionElapsed / _baseTransitionDuration);

                _baseState = EdgeGlowState.Lerp(_transitionFromState, _transitionToState, t);
                changed = true;

                if (t >= 1f)
                {
                    _baseTransitionActive = false;
                }
            }

            if (_pulseActive && Application.isPlaying)
            {
                var deltaTime = _pulse.IgnoreTimeScale ? Time.unscaledDeltaTime : Time.deltaTime;
                _pulseElapsed += deltaTime;
                changed = true;

                if (_pulseElapsed >= _pulseDuration)
                {
                    _pulseElapsed = _pulseDuration;
                    _pulseActive = false;
                }
            }

            if (changed || _materialStateDirty || !Application.isPlaying)
            {
                ApplyVisualState(false);
            }
        }

        private void InitializeStatesIfNeeded()
        {
            if (_stateInitialized)
            {
                return;
            }

            if (IsZeroState(_initialState))
            {
                _initialState = EdgeGlowState.CreateDefault();
            }

            if (IsZeroState(_neutralState))
            {
                _neutralState = EdgeGlowState.CreateNeutral();
            }

            _baseState = EdgeGlowState.Sanitize(_initialState);
            _neutralState = EdgeGlowState.Sanitize(_neutralState);
            _stateInitialized = true;
            _materialStateDirty = true;
        }

        private static bool IsZeroState(EdgeGlowState state)
        {
            return state.Intensity <= 0f
                   && state.Thickness <= 0f
                   && state.Softness <= 0f
                   && state.CornerRoundness <= 0f
                   && state.CenterClear <= 0f
                   && state.TopColor == default
                   && state.BottomColor == default;
        }

        private void EnsureMaterial()
        {
            if (_runtimeMaterial != null)
            {
                if (material != _runtimeMaterial)
                {
                    material = _runtimeMaterial;
                }

                return;
            }

            var shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                throw new InvalidOperationException($"Shader '{ShaderName}' not found.");
            }

            _runtimeMaterial = new Material(shader)
            {
                name = ShaderName + " (Runtime)",
                hideFlags = HideFlags.HideAndDontSave
            };

            material = _runtimeMaterial;
        }

        private void ApplyVisualState(bool forceRectRefresh)
        {
            if (!IsActive())
            {
                return;
            }

            EnsureMaterial();

            var rect = GetPixelAdjustedRect();
            var rectSize = rect.size;
            var rectCenter = rect.center;

            if (forceRectRefresh || rectSize != _lastRectSize || rectCenter != _lastRectCenter)
            {
                _runtimeMaterial.SetVector(_rectSizeId, new Vector4(rectSize.x, rectSize.y, 0f, 0f));
                _runtimeMaterial.SetVector(_rectCenterId, new Vector4(rectCenter.x, rectCenter.y, 0f, 0f));
                _lastRectSize = rectSize;
                _lastRectCenter = rectCenter;
            }

            var effectiveState = _baseState;

            if (_pulseActive)
            {
                var pulseWeight = EvaluatePulseWeight();
                effectiveState = EdgeGlowState.Compose(_baseState, _pulse, pulseWeight);
            }

            effectiveState = EdgeGlowState.Sanitize(effectiveState);

            _runtimeMaterial.SetColor(_topColorId, effectiveState.TopColor);
            _runtimeMaterial.SetColor(_bottomColorId, effectiveState.BottomColor);
            _runtimeMaterial.SetFloat(_intensityId, effectiveState.Intensity);
            _runtimeMaterial.SetFloat(_thicknessId, effectiveState.Thickness);
            _runtimeMaterial.SetFloat(_softnessId, effectiveState.Softness);
            _runtimeMaterial.SetFloat(_roundnessId, effectiveState.CornerRoundness);
            _runtimeMaterial.SetFloat(_centerClearId, effectiveState.CenterClear);

            _materialStateDirty = false;
        }

        private float EvaluatePulseWeight()
        {
            var time = _pulseElapsed;

            if (_pulse.Attack > 0f && time < _pulse.Attack)
            {
                return time / _pulse.Attack;
            }

            time -= _pulse.Attack;

            if (time < _pulse.Hold)
            {
                return 1f;
            }

            time -= _pulse.Hold;

            if (_pulse.Decay <= 0f)
            {
                return 0f;
            }

            return 1f - Mathf.Clamp01(time / _pulse.Decay);
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            _initialState = EdgeGlowState.Sanitize(_initialState);
            _neutralState = EdgeGlowState.Sanitize(_neutralState);
            _materialStateDirty = true;

            if (isActiveAndEnabled)
            {
                ApplyVisualState(true);
                SetVerticesDirty();
                SetMaterialDirty();
            }
        }
#endif
    }
}
