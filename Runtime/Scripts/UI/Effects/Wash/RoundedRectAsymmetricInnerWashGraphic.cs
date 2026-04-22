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

        private static readonly int _bottomSegmentsAId = Shader.PropertyToID("_BottomSegmentsA");
        private static readonly int _bottomSegmentsBId = Shader.PropertyToID("_BottomSegmentsB");
        private static readonly int _leftSegmentsId = Shader.PropertyToID("_LeftSegments");
        private static readonly int _rightSegmentsId = Shader.PropertyToID("_RightSegments");
        private static readonly int _bottomAccentAId = Shader.PropertyToID("_BottomAccentA");
        private static readonly int _bottomAccentBId = Shader.PropertyToID("_BottomAccentB");
        private static readonly int _leftAccentId = Shader.PropertyToID("_LeftAccent");
        private static readonly int _rightAccentId = Shader.PropertyToID("_RightAccent");
        private static readonly int _bottomAccentColorId = Shader.PropertyToID("_BottomAccentColor");

        [SerializeField] private AsymmetricInnerWashState _initialState;

        private Material _runtimeMaterial;
        private Shader _shader;
        private AsymmetricInnerWashState _state;
        private Vector2 _lastRectSize;
        private Vector2 _lastRectCenter;
        private bool _materialDirty;
        private bool _stateInitialized;

        private bool _segmentedRuntimeActive;
        private Color _runtimeBottomAccentColor;
        private Vector4 _runtimeBottomSegmentsA;
        private Vector4 _runtimeBottomSegmentsB;
        private Vector4 _runtimeLeftSegments;
        private Vector4 _runtimeRightSegments;
        private Vector4 _runtimeBottomAccentA;
        private Vector4 _runtimeBottomAccentB;
        private Vector4 _runtimeLeftAccent;
        private Vector4 _runtimeRightAccent;

        public override Texture mainTexture => Texture2D.whiteTexture;

        public AsymmetricInnerWashState State => _state;
        public AsymmetricInnerWashState ConfiguredInitialState => AsymmetricInnerWashState.Sanitize(_initialState);

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
                    Destroy(_runtimeMaterial);
                else
                    DestroyImmediate(_runtimeMaterial);

                _runtimeMaterial = null;
            }

            base.OnDestroy();
        }

        public void SetState(AsymmetricInnerWashState state)
        {
            InitializeStateIfNeeded();
            _segmentedRuntimeActive = false;
            _state = AsymmetricInnerWashState.Sanitize(state);
            _materialDirty = true;
            ApplyVisualState(true);
            SetMaterialDirty();
        }

        public void SetIntensity(float intensity)
        {
            InitializeStateIfNeeded();
            _segmentedRuntimeActive = false;
            _state.Intensity = Mathf.Clamp01(intensity);
            _materialDirty = true;
            ApplyVisualState(false);
            SetMaterialDirty();
        }

        public void SetThickness(float thickness)
        {
            InitializeStateIfNeeded();
            _segmentedRuntimeActive = false;
            _state.Thickness = Mathf.Clamp01(thickness);
            _materialDirty = true;
            ApplyVisualState(false);
            SetMaterialDirty();
        }

        public void SetSoftness(float softness)
        {
            InitializeStateIfNeeded();
            _segmentedRuntimeActive = false;
            _state.Softness = Mathf.Clamp01(softness);
            _materialDirty = true;
            ApplyVisualState(false);
            SetMaterialDirty();
        }

        public void SetCenterClear(float centerClear)
        {
            InitializeStateIfNeeded();
            _segmentedRuntimeActive = false;
            _state.CenterClear = Mathf.Clamp01(centerClear);
            _materialDirty = true;
            ApplyVisualState(false);
            SetMaterialDirty();
        }

        public void SetRoundness(float roundness)
        {
            InitializeStateIfNeeded();
            _segmentedRuntimeActive = false;
            _state.CornerRoundness = Mathf.Max(0f, roundness);
            _materialDirty = true;
            ApplyVisualState(false);
            SetMaterialDirty();
        }

        public void SetEdgeStrengths(float top, float bottom, float left, float right)
        {
            InitializeStateIfNeeded();
            _segmentedRuntimeActive = false;
            _state.TopStrength = Mathf.Clamp(top, 0f, 2f);
            _state.BottomStrength = Mathf.Clamp(bottom, 0f, 2f);
            _state.LeftStrength = Mathf.Clamp(left, 0f, 2f);
            _state.RightStrength = Mathf.Clamp(right, 0f, 2f);
            _materialDirty = true;
            ApplyVisualState(false);
            SetMaterialDirty();
        }

        public void SetColors(Color tintColor, Color topColor, Color bottomColor)
        {
            InitializeStateIfNeeded();
            _segmentedRuntimeActive = false;
            _state.TintColor = tintColor;
            _state.TopColor = topColor;
            _state.BottomColor = bottomColor;
            _materialDirty = true;
            ApplyVisualState(false);
            SetMaterialDirty();
        }

        public void SetRuntimeAnimatedValues(Color bottomColor, float intensity, float bottomStrength, float leftStrength, float rightStrength)
        {
            InitializeStateIfNeeded();
            _segmentedRuntimeActive = false;
            _state.BottomColor = bottomColor;
            _state.Intensity = Mathf.Clamp01(intensity);
            _state.TopStrength = 0f;
            _state.BottomStrength = Mathf.Clamp(bottomStrength, 0f, 2f);
            _state.LeftStrength = Mathf.Clamp(leftStrength, 0f, 2f);
            _state.RightStrength = Mathf.Clamp(rightStrength, 0f, 2f);
            _materialDirty = true;
            ApplyVisualState(false);
        }

        public void SetSegmentedRuntimeAnimatedValues(
            Color bottomColor,
            Color accentBottomColor,
            float intensity,
            float[] bottomStrengths,
            float[] leftStrengths,
            float[] rightStrengths,
            float[] bottomAccents,
            float[] leftAccents,
            float[] rightAccents)
        {
            if (bottomStrengths == null || bottomStrengths.Length != 5)
                throw new ArgumentException(nameof(bottomStrengths));

            if (leftStrengths == null || leftStrengths.Length != 3)
                throw new ArgumentException(nameof(leftStrengths));

            if (rightStrengths == null || rightStrengths.Length != 3)
                throw new ArgumentException(nameof(rightStrengths));

            if (bottomAccents == null || bottomAccents.Length != 5)
                throw new ArgumentException(nameof(bottomAccents));

            if (leftAccents == null || leftAccents.Length != 3)
                throw new ArgumentException(nameof(leftAccents));

            if (rightAccents == null || rightAccents.Length != 3)
                throw new ArgumentException(nameof(rightAccents));

            InitializeStateIfNeeded();

            _state.BottomColor = bottomColor;
            _state.Intensity = Mathf.Clamp01(intensity);
            _state.TopStrength = 0f;
            _state.BottomStrength = 0f;
            _state.LeftStrength = 0f;
            _state.RightStrength = 0f;

            _runtimeBottomAccentColor = accentBottomColor;
            _runtimeBottomSegmentsA = new Vector4(bottomStrengths[0], bottomStrengths[1], bottomStrengths[2], bottomStrengths[3]);
            _runtimeBottomSegmentsB = new Vector4(bottomStrengths[4], 0f, 0f, 0f);
            _runtimeLeftSegments = new Vector4(leftStrengths[0], leftStrengths[1], leftStrengths[2], 0f);
            _runtimeRightSegments = new Vector4(rightStrengths[0], rightStrengths[1], rightStrengths[2], 0f);
            _runtimeBottomAccentA = new Vector4(bottomAccents[0], bottomAccents[1], bottomAccents[2], bottomAccents[3]);
            _runtimeBottomAccentB = new Vector4(bottomAccents[4], 0f, 0f, 0f);
            _runtimeLeftAccent = new Vector4(leftAccents[0], leftAccents[1], leftAccents[2], 0f);
            _runtimeRightAccent = new Vector4(rightAccents[0], rightAccents[1], rightAccents[2], 0f);

            _segmentedRuntimeActive = true;
            _materialDirty = true;
            ApplyVisualState(false);
            SetMaterialDirty();
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            _materialDirty = true;
            SetVerticesDirty();
            ApplyVisualState(false);
            SetMaterialDirty();
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

        private void Reset()
        {
            _initialState = AsymmetricInnerWashState.CreateNeutralAmbient();
            color = Color.white;
            raycastTarget = false;
            _state = AsymmetricInnerWashState.Sanitize(_initialState);
            _stateInitialized = true;
            _segmentedRuntimeActive = false;
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
                return;

            _shader = Shader.Find(ShaderName);

            if (_shader == null)
                throw new InvalidOperationException($"{nameof(RoundedRectAsymmetricInnerWashGraphic)} requires shader '{ShaderName}'.");
        }

        private void InitializeStateIfNeeded()
        {
            if (_stateInitialized)
                return;

            if (_initialState.TintColor == default && _initialState.TopColor == default && _initialState.BottomColor == default)
                _initialState = AsymmetricInnerWashState.CreateNeutralAmbient();

            _state = AsymmetricInnerWashState.Sanitize(_initialState);
            _stateInitialized = true;
        }

        private void EnsureMaterial()
        {
            if (_runtimeMaterial != null)
            {
                if (material != _runtimeMaterial)
                    material = _runtimeMaterial;

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
                return;

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

            if (_segmentedRuntimeActive)
            {
                _runtimeMaterial.SetColor(_bottomAccentColorId, _runtimeBottomAccentColor);
                _runtimeMaterial.SetVector(_bottomSegmentsAId, _runtimeBottomSegmentsA);
                _runtimeMaterial.SetVector(_bottomSegmentsBId, _runtimeBottomSegmentsB);
                _runtimeMaterial.SetVector(_leftSegmentsId, _runtimeLeftSegments);
                _runtimeMaterial.SetVector(_rightSegmentsId, _runtimeRightSegments);
                _runtimeMaterial.SetVector(_bottomAccentAId, _runtimeBottomAccentA);
                _runtimeMaterial.SetVector(_bottomAccentBId, _runtimeBottomAccentB);
                _runtimeMaterial.SetVector(_leftAccentId, _runtimeLeftAccent);
                _runtimeMaterial.SetVector(_rightAccentId, _runtimeRightAccent);
            }
            else
            {
                _runtimeMaterial.SetColor(_bottomAccentColorId, _state.BottomColor);
                _runtimeMaterial.SetVector(_bottomSegmentsAId, Vector4.zero);
                _runtimeMaterial.SetVector(_bottomSegmentsBId, Vector4.zero);
                _runtimeMaterial.SetVector(_leftSegmentsId, Vector4.zero);
                _runtimeMaterial.SetVector(_rightSegmentsId, Vector4.zero);
                _runtimeMaterial.SetVector(_bottomAccentAId, Vector4.zero);
                _runtimeMaterial.SetVector(_bottomAccentBId, Vector4.zero);
                _runtimeMaterial.SetVector(_leftAccentId, Vector4.zero);
                _runtimeMaterial.SetVector(_rightAccentId, Vector4.zero);
            }

            _materialDirty = false;
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            _initialState = AsymmetricInnerWashState.Sanitize(_initialState);
            _state = _initialState;
            _stateInitialized = true;
            _segmentedRuntimeActive = false;
            _materialDirty = true;

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