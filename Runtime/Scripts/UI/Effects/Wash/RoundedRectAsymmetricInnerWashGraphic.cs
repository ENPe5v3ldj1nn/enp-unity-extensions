using System;
using UnityEngine;
using UnityEngine.UI;

namespace enp_unity_extensions.Runtime.Scripts.UI.Effects.Wash
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("UI/ENP/Rounded Rect Asymmetric Inner Wash")]
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RoundedRectAsymmetricInnerWashGraphic : MaskableGraphic
    {
        private const string ShaderName = "UI/ENP/RoundedRectAsymmetricInnerWash";

        private static readonly int _tintColorId = Shader.PropertyToID("_TintColor");
        private static readonly int _topColorId = Shader.PropertyToID("_TopColor");
        private static readonly int _bottomColorId = Shader.PropertyToID("_BottomColor");
        private static readonly int _intensityId = Shader.PropertyToID("_Intensity");
        private static readonly int _thicknessId = Shader.PropertyToID("_Thickness");
        private static readonly int _softnessId = Shader.PropertyToID("_Softness");
        private static readonly int _centerClearId = Shader.PropertyToID("_CenterClear");
        private static readonly int _roundnessId = Shader.PropertyToID("_Roundness");
        private static readonly int _topStrengthId = Shader.PropertyToID("_TopStrength");
        private static readonly int _bottomStrengthId = Shader.PropertyToID("_BottomStrength");
        private static readonly int _leftStrengthId = Shader.PropertyToID("_LeftStrength");
        private static readonly int _rightStrengthId = Shader.PropertyToID("_RightStrength");
        private static readonly int _rectSizeId = Shader.PropertyToID("_RectSize");
        private static readonly int _rectCenterId = Shader.PropertyToID("_RectCenter");

        [SerializeField] private AsymmetricInnerWashState _initialState;

        private Material _runtimeMaterial;
        private Shader _shader;
        private AsymmetricInnerWashState _state;
        private Vector2 _lastRectSize;
        private Vector2 _lastRectCenter;
        private bool _materialDirty;
        private bool _stateInitialized;

        public override Texture mainTexture => Texture2D.whiteTexture;

        public AsymmetricInnerWashState State => _state;

        protected override void Awake()
        {
            base.Awake();
            EnsureShader();
            InitializeStateIfNeeded();
            EnsureMaterial();
            ApplyVisualState(true);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            EnsureShader();
            InitializeStateIfNeeded();
            EnsureMaterial();
            SetAllDirty();
            ApplyVisualState(true);
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

        public void SetState(AsymmetricInnerWashState state)
        {
            _state = AsymmetricInnerWashState.Sanitize(state);
            _materialDirty = true;
            ApplyVisualState(false);
        }

        public void SetIntensity(float intensity)
        {
            _state.Intensity = Mathf.Clamp01(intensity);
            _materialDirty = true;
            ApplyVisualState(false);
        }

        public void SetThickness(float thickness)
        {
            _state.Thickness = Mathf.Clamp01(thickness);
            _materialDirty = true;
            ApplyVisualState(false);
        }

        public void SetSoftness(float softness)
        {
            _state.Softness = Mathf.Clamp01(softness);
            _materialDirty = true;
            ApplyVisualState(false);
        }

        public void SetCenterClear(float centerClear)
        {
            _state.CenterClear = Mathf.Clamp01(centerClear);
            _materialDirty = true;
            ApplyVisualState(false);
        }

        public void SetRoundness(float roundness)
        {
            _state.CornerRoundness = Mathf.Max(0f, roundness);
            _materialDirty = true;
            ApplyVisualState(false);
        }

        public void SetEdgeStrengths(float top, float bottom, float left, float right)
        {
            _state.TopStrength = Mathf.Clamp(top, 0f, 2f);
            _state.BottomStrength = Mathf.Clamp(bottom, 0f, 2f);
            _state.LeftStrength = Mathf.Clamp(left, 0f, 2f);
            _state.RightStrength = Mathf.Clamp(right, 0f, 2f);
            _materialDirty = true;
            ApplyVisualState(false);
        }

        public void SetColors(Color tintColor, Color topColor, Color bottomColor)
        {
            _state.TintColor = tintColor;
            _state.TopColor = topColor;
            _state.BottomColor = bottomColor;
            _materialDirty = true;
            ApplyVisualState(false);
        }

        [ContextMenu("Apply Neutral Ambient Preset")]
        private void ApplyNeutralAmbientPreset()
        {
            SetState(AsymmetricInnerWashState.CreateNeutralAmbient());
        }

        [ContextMenu("Apply Medium Ambient Preset")]
        private void ApplyMediumAmbientPreset()
        {
            SetState(AsymmetricInnerWashState.CreateMediumAmbient());
        }

        [ContextMenu("Apply Warm Strong Preset")]
        private void ApplyWarmStrongPreset()
        {
            SetState(AsymmetricInnerWashState.CreateWarmStrong());
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
            _materialDirty = true;
            SetVerticesDirty();
            ApplyVisualState(false);
        }

        private void Reset()
        {
            _initialState = AsymmetricInnerWashState.CreateNeutralAmbient();
            color = Color.white;
            raycastTarget = false;
            _state = AsymmetricInnerWashState.Sanitize(_initialState);
            _materialDirty = true;

            if (isActiveAndEnabled)
            {
                ApplyVisualState(true);
                SetVerticesDirty();
                SetMaterialDirty();
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
                throw new InvalidOperationException($"{nameof(RoundedRectAsymmetricInnerWashGraphic)} requires shader '{ShaderName}'.");
            }
        }

        private void InitializeStateIfNeeded()
        {
            if (_stateInitialized)
            {
                return;
            }

            if (_initialState.TintColor == default && _initialState.TopColor == default && _initialState.BottomColor == default)
            {
                _initialState = AsymmetricInnerWashState.CreateNeutralAmbient();
            }

            _state = AsymmetricInnerWashState.Sanitize(_initialState);
            _stateInitialized = true;
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
            _runtimeMaterial.name = "UI/ENP/RoundedRectAsymmetricInnerWash (Runtime)";
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

            _state = AsymmetricInnerWashState.Sanitize(_state);

            _runtimeMaterial.SetColor(_tintColorId, _state.TintColor);
            _runtimeMaterial.SetColor(_topColorId, _state.TopColor);
            _runtimeMaterial.SetColor(_bottomColorId, _state.BottomColor);
            _runtimeMaterial.SetFloat(_intensityId, _state.Intensity);
            _runtimeMaterial.SetFloat(_thicknessId, _state.Thickness);
            _runtimeMaterial.SetFloat(_softnessId, _state.Softness);
            _runtimeMaterial.SetFloat(_centerClearId, _state.CenterClear);
            _runtimeMaterial.SetFloat(_roundnessId, _state.CornerRoundness);
            _runtimeMaterial.SetFloat(_topStrengthId, _state.TopStrength);
            _runtimeMaterial.SetFloat(_bottomStrengthId, _state.BottomStrength);
            _runtimeMaterial.SetFloat(_leftStrengthId, _state.LeftStrength);
            _runtimeMaterial.SetFloat(_rightStrengthId, _state.RightStrength);

            _materialDirty = false;
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            _initialState = AsymmetricInnerWashState.Sanitize(_initialState);
            _state = _initialState;
            _materialDirty = true;

            if (isActiveAndEnabled)
            {
                ApplyVisualState(true);
                SetVerticesDirty();
                SetMaterialDirty();
            }
        }
    }
}
