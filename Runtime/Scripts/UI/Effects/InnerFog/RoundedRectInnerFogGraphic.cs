using System;
using UnityEngine;
using UnityEngine.UI;

namespace enp_unity_extensions.Runtime.Scripts.UI.Effects.InnerFog
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("UI/ENP/Rounded Rect Inner Fog")]
    public sealed class RoundedRectInnerFogGraphic : MaskableGraphic
    {
        private const string ShaderName = "UI/ENP/RoundedRectInnerFog";

        private static readonly int _topColorId = Shader.PropertyToID("_TopColor");
        private static readonly int _bottomColorId = Shader.PropertyToID("_BottomColor");
        private static readonly int _intensityId = Shader.PropertyToID("_Intensity");
        private static readonly int _coverageId = Shader.PropertyToID("_Coverage");
        private static readonly int _contrastId = Shader.PropertyToID("_Contrast");
        private static readonly int _softnessId = Shader.PropertyToID("_Softness");
        private static readonly int _noiseScaleId = Shader.PropertyToID("_NoiseScale");
        private static readonly int _warpAmountId = Shader.PropertyToID("_WarpAmount");
        private static readonly int _warpSpeedId = Shader.PropertyToID("_WarpSpeed");
        private static readonly int _centerProtectionId = Shader.PropertyToID("_CenterProtection");
        private static readonly int _edgeBoostId = Shader.PropertyToID("_EdgeBoost");
        private static readonly int _roundnessId = Shader.PropertyToID("_Roundness");
        private static readonly int _animationTimeId = Shader.PropertyToID("_AnimationTime");
        private static readonly int _rectSizeId = Shader.PropertyToID("_RectSize");
        private static readonly int _rectCenterId = Shader.PropertyToID("_RectCenter");

        [SerializeField] private InnerFogState _initialState = default;
        [SerializeField] private InnerFogState _neutralState = default;
        [SerializeField] private bool _useUnscaledTime = true;

        private Material _runtimeMaterial;
        private Shader _shader;

        private InnerFogState _baseState;
        private InnerFogState _transitionFromState;
        private InnerFogState _transitionToState;
        private bool _baseTransitionActive;
        private float _baseTransitionElapsed;
        private float _baseTransitionDuration;

        private InnerFogPulse _pulse;
        private bool _pulseActive;
        private float _pulseElapsed;
        private float _pulseDuration;

        private float _animationTime;
        private Vector2 _lastRectSize;
        private Vector2 _lastRectCenter;
        private bool _materialStateDirty;
        private bool _stateInitialized;

        public override Texture mainTexture => Texture2D.whiteTexture;

        public InnerFogState BaseState => _baseState;

        protected override void Awake()
        {
            base.Awake();
            EnsureShader();
            InitializeStatesIfNeeded();
            EnsureMaterial();
            ApplyVisualState(true);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            EnsureShader();
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

        public void SetState(InnerFogState state)
        {
            _baseTransitionActive = false;
            _baseState = InnerFogState.Sanitize(state);
            _transitionFromState = _baseState;
            _transitionToState = _baseState;
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

        public void SetShape(float coverage, float contrast, float softness, float noiseScale, float centerProtection,
            float edgeBoost, float cornerRoundness)
        {
            _baseTransitionActive = false;
            _baseState.Coverage = Mathf.Clamp01(coverage);
            _baseState.Contrast = Mathf.Clamp01(contrast);
            _baseState.Softness = Mathf.Clamp01(softness);
            _baseState.NoiseScale = Mathf.Max(0.001f, noiseScale);
            _baseState.CenterProtection = Mathf.Clamp01(centerProtection);
            _baseState.EdgeBoost = Mathf.Clamp01(edgeBoost);
            _baseState.CornerRoundness = Mathf.Max(0f, cornerRoundness);
            _materialStateDirty = true;
            ApplyVisualState(false);
        }

        public void SetWarp(float warpAmount, float warpSpeed)
        {
            _baseTransitionActive = false;
            _baseState.WarpAmount = Mathf.Clamp01(warpAmount);
            _baseState.WarpSpeed = Mathf.Max(0f, warpSpeed);
            _materialStateDirty = true;
            ApplyVisualState(false);
        }

        public void AnimateState(InnerFogState targetState, float duration)
        {
            targetState = InnerFogState.Sanitize(targetState);

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

        public void PlayPulse(InnerFogPulse pulse)
        {
            _pulse = InnerFogPulse.Sanitize(pulse);
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

        private void Reset()
        {
            _initialState = InnerFogState.CreateDefault();
            _neutralState = InnerFogState.CreateNeutral();
            color = Color.white;
            raycastTarget = false;
            SyncPreviewStateFromInitial();
            _animationTime = 0f;
            _materialStateDirty = true;

            if (isActiveAndEnabled)
            {
                ApplyVisualState(true);
                SetVerticesDirty();
                SetMaterialDirty();
            }
        }

        private void Update()
        {
            var shouldAnimateTime = ShouldAnimateTime();

            if (!_materialStateDirty && !_baseTransitionActive && !_pulseActive && !shouldAnimateTime)
            {
                return;
            }

            var deltaTime = _useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            var changed = false;

            if (_baseTransitionActive)
            {
                _baseTransitionElapsed += deltaTime;

                var t = _baseTransitionDuration <= 0f
                    ? 1f
                    : Mathf.Clamp01(_baseTransitionElapsed / _baseTransitionDuration);

                _baseState = InnerFogState.Lerp(_transitionFromState, _transitionToState, t);
                changed = true;

                if (t >= 1f)
                {
                    _baseTransitionActive = false;
                }
            }

            if (_pulseActive)
            {
                var pulseDeltaTime = _pulse.IgnoreTimeScale ? Time.unscaledDeltaTime : Time.deltaTime;
                _pulseElapsed += pulseDeltaTime;
                changed = true;

                if (_pulseElapsed >= _pulseDuration)
                {
                    _pulseElapsed = _pulseDuration;
                    _pulseActive = false;
                }
            }

            if (shouldAnimateTime)
            {
                _animationTime += deltaTime;
                changed = true;
            }

            if (changed || _materialStateDirty)
            {
                ApplyVisualState(false);
            }
        }

        private void EnsureShader()
        {
            if (_shader != null)
            {
                return;
            }

            _shader = Shader.Find(ShaderName);

            if (_shader == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(RoundedRectInnerFogGraphic)} requires shader '{ShaderName}'.");
            }
        }

        private void InitializeStatesIfNeeded()
        {
            if (_stateInitialized)
            {
                return;
            }

            if (InnerFogState.IsDefault(_initialState))
            {
                _initialState = InnerFogState.CreateDefault();
            }

            if (InnerFogState.IsDefault(_neutralState))
            {
                _neutralState = InnerFogState.CreateNeutral();
            }

            SyncPreviewStateFromInitial();
        }

        private void SyncPreviewStateFromInitial()
        {
            _initialState = InnerFogState.Sanitize(_initialState);
            _neutralState = InnerFogState.Sanitize(_neutralState);
            _baseState = _initialState;
            _transitionFromState = _baseState;
            _transitionToState = _baseState;
            _baseTransitionActive = false;
            _pulseActive = false;
            _pulseElapsed = 0f;
            _pulseDuration = 0f;
            _stateInitialized = true;
        }

        private bool ShouldAnimateTime()
        {
            if (_baseTransitionActive || _pulseActive)
            {
                return true;
            }

            return _baseState.Intensity > 0.0001f && _baseState.WarpSpeed > 0.0001f;
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

            EnsureShader();
            _runtimeMaterial = new Material(_shader);
            _runtimeMaterial.name = "UI/ENP/RoundedRectInnerFog (Runtime)";
            _runtimeMaterial.hideFlags = HideFlags.HideAndDontSave;
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
                effectiveState = InnerFogState.Compose(_baseState, _pulse, pulseWeight);
            }

            effectiveState = InnerFogState.Sanitize(effectiveState);

            _runtimeMaterial.SetColor(_topColorId, effectiveState.TopColor);
            _runtimeMaterial.SetColor(_bottomColorId, effectiveState.BottomColor);
            _runtimeMaterial.SetFloat(_intensityId, effectiveState.Intensity);
            _runtimeMaterial.SetFloat(_coverageId, effectiveState.Coverage);
            _runtimeMaterial.SetFloat(_contrastId, effectiveState.Contrast);
            _runtimeMaterial.SetFloat(_softnessId, effectiveState.Softness);
            _runtimeMaterial.SetFloat(_noiseScaleId, effectiveState.NoiseScale);
            _runtimeMaterial.SetFloat(_warpAmountId, effectiveState.WarpAmount);
            _runtimeMaterial.SetFloat(_warpSpeedId, effectiveState.WarpSpeed);
            _runtimeMaterial.SetFloat(_centerProtectionId, effectiveState.CenterProtection);
            _runtimeMaterial.SetFloat(_edgeBoostId, effectiveState.EdgeBoost);
            _runtimeMaterial.SetFloat(_roundnessId, effectiveState.CornerRoundness);
            _runtimeMaterial.SetFloat(_animationTimeId, _animationTime);

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

            if (InnerFogState.IsDefault(_initialState))
            {
                _initialState = InnerFogState.CreateDefault();
            }

            if (InnerFogState.IsDefault(_neutralState))
            {
                _neutralState = InnerFogState.CreateNeutral();
            }

            SyncPreviewStateFromInitial();
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