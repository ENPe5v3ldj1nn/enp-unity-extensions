using System;
using UnityEngine;
using UnityEngine.Serialization;
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
        private static readonly int _bandTightnessId = Shader.PropertyToID("_BandTightness");
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

        [FormerlySerializedAs("_initialState")]
        [SerializeField] private AsymmetricInnerWashState _baseState;

        [SerializeField] private bool _useSegmentedPreview;
        [SerializeField] private bool _previewUsesAbsoluteStrengths = true;
        [SerializeField] private bool _usePreviewAccentColorOverride;
        [SerializeField] private Color _previewBottomAccentColor = new Color(0.66f, 0.78f, 1f, 1f);

        [SerializeField] private float[] _previewBottomStrengths = new float[5];
        [SerializeField] private float[] _previewLeftStrengths = new float[3];
        [SerializeField] private float[] _previewRightStrengths = new float[3];

        [SerializeField] private float[] _previewBottomAccents = new float[5];
        [SerializeField] private float[] _previewLeftAccents = new float[3];
        [SerializeField] private float[] _previewRightAccents = new float[3];

        private Material _runtimeMaterial;
        private Shader _shader;
        private AsymmetricInnerWashState _state;
        private Vector2 _lastRectSize;
        private Vector2 _lastRectCenter;
        private bool _stateInitialized;

        private bool _runtimeSegmentedActive;
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

        public AsymmetricInnerWashState BaseState => _baseState;
        public AsymmetricInnerWashState State => _state;
        public bool UseSegmentedPreview => _useSegmentedPreview;
        public bool RuntimeSegmentedActive => _runtimeSegmentedActive;

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
            _baseState = AsymmetricInnerWashState.Sanitize(state);
            _state = _baseState;
            _runtimeSegmentedActive = false;
            ApplyVisualState(true);
            SetMaterialDirty();
        }

        public void SetIntensity(float intensity)
        {
            InitializeStateIfNeeded();
            _baseState.Intensity = Mathf.Clamp01(intensity);

            if (!_runtimeSegmentedActive)
            {
                _state = _baseState;
            }

            ApplyVisualState(false);
            SetMaterialDirty();
        }

        public void SetThickness(float thickness)
        {
            InitializeStateIfNeeded();
            _baseState.Thickness = Mathf.Clamp01(thickness);

            if (!_runtimeSegmentedActive)
            {
                _state = _baseState;
            }

            ApplyVisualState(false);
            SetMaterialDirty();
        }

        public void SetSoftness(float softness)
        {
            InitializeStateIfNeeded();
            _baseState.Softness = Mathf.Clamp01(softness);

            if (!_runtimeSegmentedActive)
            {
                _state = _baseState;
            }

            ApplyVisualState(false);
            SetMaterialDirty();
        }

        public void SetBandTightness(float bandTightness)
        {
            InitializeStateIfNeeded();
            _baseState.BandTightness = Mathf.Clamp01(bandTightness);

            if (!_runtimeSegmentedActive)
            {
                _state = _baseState;
            }

            ApplyVisualState(false);
            SetMaterialDirty();
        }

        public void SetCenterClear(float centerClear)
        {
            InitializeStateIfNeeded();
            _baseState.CenterClear = Mathf.Clamp01(centerClear);

            if (!_runtimeSegmentedActive)
            {
                _state = _baseState;
            }

            ApplyVisualState(false);
            SetMaterialDirty();
        }

        public void SetRoundness(float roundness)
        {
            InitializeStateIfNeeded();
            _baseState.CornerRoundness = Mathf.Max(0f, roundness);

            if (!_runtimeSegmentedActive)
            {
                _state = _baseState;
            }

            ApplyVisualState(false);
            SetMaterialDirty();
        }

        public void SetEdgeStrengths(float top, float bottom, float left, float right)
        {
            InitializeStateIfNeeded();
            _baseState.TopStrength = Mathf.Clamp(top, 0f, 2f);
            _baseState.BottomStrength = Mathf.Clamp(bottom, 0f, 2f);
            _baseState.LeftStrength = Mathf.Clamp(left, 0f, 2f);
            _baseState.RightStrength = Mathf.Clamp(right, 0f, 2f);

            if (!_runtimeSegmentedActive)
            {
                _state = _baseState;
            }

            ApplyVisualState(false);
            SetMaterialDirty();
        }

        public void SetColors(Color tintColor, Color topColor, Color bottomColor)
        {
            InitializeStateIfNeeded();
            _baseState.TintColor = tintColor;
            _baseState.TopColor = topColor;
            _baseState.BottomColor = bottomColor;

            if (!_runtimeSegmentedActive)
            {
                _state = _baseState;
            }

            ApplyVisualState(false);
            SetMaterialDirty();
        }

        public void SetSegmentedPreviewEnabled(bool isEnabled)
        {
            InitializeStateIfNeeded();
            _useSegmentedPreview = isEnabled;

            if (!_runtimeSegmentedActive)
            {
                ApplyVisualState(false);
                SetMaterialDirty();
            }
        }

        public void SetSegmentedPreviewMode(bool usesAbsoluteStrengths)
        {
            InitializeStateIfNeeded();
            _previewUsesAbsoluteStrengths = usesAbsoluteStrengths;

            if (!_runtimeSegmentedActive)
            {
                ApplyVisualState(false);
                SetMaterialDirty();
            }
        }

        public void RestorePreview()
        {
            InitializeStateIfNeeded();
            _runtimeSegmentedActive = false;
            _state = _baseState;
            ApplyVisualState(false);
            SetMaterialDirty();
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
            ValidateArray(bottomStrengths, 5, nameof(bottomStrengths));
            ValidateArray(leftStrengths, 3, nameof(leftStrengths));
            ValidateArray(rightStrengths, 3, nameof(rightStrengths));
            ValidateArray(bottomAccents, 5, nameof(bottomAccents));
            ValidateArray(leftAccents, 3, nameof(leftAccents));
            ValidateArray(rightAccents, 3, nameof(rightAccents));

            InitializeStateIfNeeded();

            _state = _baseState;
            _state.BottomColor = bottomColor;
            _state.Intensity = Mathf.Clamp01(intensity);
            _state.TopStrength = 0f;
            _state.BottomStrength = 0f;
            _state.LeftStrength = 0f;
            _state.RightStrength = 0f;

            _runtimeBottomAccentColor = accentBottomColor;
            _runtimeBottomSegmentsA = new Vector4(
                Mathf.Clamp(bottomStrengths[0], 0f, 2f),
                Mathf.Clamp(bottomStrengths[1], 0f, 2f),
                Mathf.Clamp(bottomStrengths[2], 0f, 2f),
                Mathf.Clamp(bottomStrengths[3], 0f, 2f));

            _runtimeBottomSegmentsB = new Vector4(
                Mathf.Clamp(bottomStrengths[4], 0f, 2f),
                0f,
                0f,
                0f);

            _runtimeLeftSegments = new Vector4(
                Mathf.Clamp(leftStrengths[0], 0f, 2f),
                Mathf.Clamp(leftStrengths[1], 0f, 2f),
                Mathf.Clamp(leftStrengths[2], 0f, 2f),
                0f);

            _runtimeRightSegments = new Vector4(
                Mathf.Clamp(rightStrengths[0], 0f, 2f),
                Mathf.Clamp(rightStrengths[1], 0f, 2f),
                Mathf.Clamp(rightStrengths[2], 0f, 2f),
                0f);

            _runtimeBottomAccentA = new Vector4(
                Mathf.Clamp01(bottomAccents[0]),
                Mathf.Clamp01(bottomAccents[1]),
                Mathf.Clamp01(bottomAccents[2]),
                Mathf.Clamp01(bottomAccents[3]));

            _runtimeBottomAccentB = new Vector4(
                Mathf.Clamp01(bottomAccents[4]),
                0f,
                0f,
                0f);

            _runtimeLeftAccent = new Vector4(
                Mathf.Clamp01(leftAccents[0]),
                Mathf.Clamp01(leftAccents[1]),
                Mathf.Clamp01(leftAccents[2]),
                0f);

            _runtimeRightAccent = new Vector4(
                Mathf.Clamp01(rightAccents[0]),
                Mathf.Clamp01(rightAccents[1]),
                Mathf.Clamp01(rightAccents[2]),
                0f);

            _runtimeSegmentedActive = true;
            ApplyVisualState(false);
            SetMaterialDirty();
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            SetVerticesDirty();
            ApplyVisualState(false);
            SetMaterialDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            Rect rect = GetPixelAdjustedRect();

            vh.Clear();

            UIVertex vertex = UIVertex.simpleVert;
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
            _baseState = AsymmetricInnerWashState.CreateNeutralAmbient();
            _state = _baseState;
            _stateInitialized = true;

            _useSegmentedPreview = false;
            _previewUsesAbsoluteStrengths = true;
            _usePreviewAccentColorOverride = false;
            _previewBottomAccentColor = new Color(0.66f, 0.78f, 1f, 1f);

            EnsurePreviewArrays();
            FillStrengthArray(_previewBottomStrengths, 5, _baseState.BottomStrength);
            FillStrengthArray(_previewLeftStrengths, 3, _baseState.LeftStrength);
            FillStrengthArray(_previewRightStrengths, 3, _baseState.RightStrength);
            FillZeroArray(_previewBottomAccents, 5);
            FillZeroArray(_previewLeftAccents, 3);
            FillZeroArray(_previewRightAccents, 3);

            color = Color.white;
            raycastTarget = false;
            _runtimeSegmentedActive = false;

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

            if (_baseState.TintColor == default && _baseState.TopColor == default && _baseState.BottomColor == default)
            {
                _baseState = AsymmetricInnerWashState.CreateNeutralAmbient();
            }

            _baseState = AsymmetricInnerWashState.Sanitize(_baseState);
            _state = _baseState;
            EnsurePreviewArrays();
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
            InitializeStateIfNeeded();

            Rect rect = GetPixelAdjustedRect();
            Vector2 rectSize = rect.size;
            Vector2 rectCenter = rect.center;

            if (forceRectRefresh || rectSize != _lastRectSize || rectCenter != _lastRectCenter)
            {
                _runtimeMaterial.SetVector(_rectSizeId, new Vector4(rectSize.x, rectSize.y, 0f, 0f));
                _runtimeMaterial.SetVector(_rectCenterId, new Vector4(rectCenter.x, rectCenter.y, 0f, 0f));
                _lastRectSize = rectSize;
                _lastRectCenter = rectCenter;
            }

            if (!_runtimeSegmentedActive)
            {
                _state = _baseState;
            }

            _state = AsymmetricInnerWashState.Sanitize(_state);

            _runtimeMaterial.SetColor(_tintColorId, _state.TintColor);
            _runtimeMaterial.SetColor(_topColorId, _state.TopColor);
            _runtimeMaterial.SetColor(_bottomColorId, _state.BottomColor);
            _runtimeMaterial.SetFloat(_intensityId, _state.Intensity);
            _runtimeMaterial.SetFloat(_thicknessId, _state.Thickness);
            _runtimeMaterial.SetFloat(_softnessId, _state.Softness);
            _runtimeMaterial.SetFloat(_bandTightnessId, _state.BandTightness);
            _runtimeMaterial.SetFloat(_centerClearId, _state.CenterClear);
            _runtimeMaterial.SetFloat(_roundnessId, _state.CornerRoundness);
            _runtimeMaterial.SetFloat(_topStrengthId, _state.TopStrength);
            _runtimeMaterial.SetFloat(_bottomStrengthId, _state.BottomStrength);
            _runtimeMaterial.SetFloat(_leftStrengthId, _state.LeftStrength);
            _runtimeMaterial.SetFloat(_rightStrengthId, _state.RightStrength);

            if (_runtimeSegmentedActive)
            {
                ApplyPackedSegmentData(
                    _runtimeBottomAccentColor,
                    _runtimeBottomSegmentsA,
                    _runtimeBottomSegmentsB,
                    _runtimeLeftSegments,
                    _runtimeRightSegments,
                    _runtimeBottomAccentA,
                    _runtimeBottomAccentB,
                    _runtimeLeftAccent,
                    _runtimeRightAccent);
            }
            else if (_useSegmentedPreview)
            {
                Color accentColor = _usePreviewAccentColorOverride ? _previewBottomAccentColor : _state.BottomColor;

                Vector4 bottomSegmentsA = new Vector4(
                    GetPreviewStrength(_previewBottomStrengths, 0, _baseState.BottomStrength),
                    GetPreviewStrength(_previewBottomStrengths, 1, _baseState.BottomStrength),
                    GetPreviewStrength(_previewBottomStrengths, 2, _baseState.BottomStrength),
                    GetPreviewStrength(_previewBottomStrengths, 3, _baseState.BottomStrength));

                Vector4 bottomSegmentsB = new Vector4(
                    GetPreviewStrength(_previewBottomStrengths, 4, _baseState.BottomStrength),
                    0f,
                    0f,
                    0f);

                Vector4 leftSegments = new Vector4(
                    GetPreviewStrength(_previewLeftStrengths, 0, _baseState.LeftStrength),
                    GetPreviewStrength(_previewLeftStrengths, 1, _baseState.LeftStrength),
                    GetPreviewStrength(_previewLeftStrengths, 2, _baseState.LeftStrength),
                    0f);

                Vector4 rightSegments = new Vector4(
                    GetPreviewStrength(_previewRightStrengths, 0, _baseState.RightStrength),
                    GetPreviewStrength(_previewRightStrengths, 1, _baseState.RightStrength),
                    GetPreviewStrength(_previewRightStrengths, 2, _baseState.RightStrength),
                    0f);

                Vector4 bottomAccentA = new Vector4(
                    GetPreviewAccent(_previewBottomAccents, 0),
                    GetPreviewAccent(_previewBottomAccents, 1),
                    GetPreviewAccent(_previewBottomAccents, 2),
                    GetPreviewAccent(_previewBottomAccents, 3));

                Vector4 bottomAccentB = new Vector4(
                    GetPreviewAccent(_previewBottomAccents, 4),
                    0f,
                    0f,
                    0f);

                Vector4 leftAccent = new Vector4(
                    GetPreviewAccent(_previewLeftAccents, 0),
                    GetPreviewAccent(_previewLeftAccents, 1),
                    GetPreviewAccent(_previewLeftAccents, 2),
                    0f);

                Vector4 rightAccent = new Vector4(
                    GetPreviewAccent(_previewRightAccents, 0),
                    GetPreviewAccent(_previewRightAccents, 1),
                    GetPreviewAccent(_previewRightAccents, 2),
                    0f);

                ApplyPackedSegmentData(
                    accentColor,
                    bottomSegmentsA,
                    bottomSegmentsB,
                    leftSegments,
                    rightSegments,
                    bottomAccentA,
                    bottomAccentB,
                    leftAccent,
                    rightAccent);
            }
            else
            {
                ClearSegmentData(_state.BottomColor);
            }
        }

        private void ApplyPackedSegmentData(
            Color accentBottomColor,
            Vector4 bottomSegmentsA,
            Vector4 bottomSegmentsB,
            Vector4 leftSegments,
            Vector4 rightSegments,
            Vector4 bottomAccentA,
            Vector4 bottomAccentB,
            Vector4 leftAccent,
            Vector4 rightAccent)
        {
            _runtimeMaterial.SetColor(_bottomAccentColorId, accentBottomColor);
            _runtimeMaterial.SetVector(_bottomSegmentsAId, bottomSegmentsA);
            _runtimeMaterial.SetVector(_bottomSegmentsBId, bottomSegmentsB);
            _runtimeMaterial.SetVector(_leftSegmentsId, leftSegments);
            _runtimeMaterial.SetVector(_rightSegmentsId, rightSegments);
            _runtimeMaterial.SetVector(_bottomAccentAId, bottomAccentA);
            _runtimeMaterial.SetVector(_bottomAccentBId, bottomAccentB);
            _runtimeMaterial.SetVector(_leftAccentId, leftAccent);
            _runtimeMaterial.SetVector(_rightAccentId, rightAccent);
        }

        private void ClearSegmentData(Color accentBottomColor)
        {
            _runtimeMaterial.SetColor(_bottomAccentColorId, accentBottomColor);
            _runtimeMaterial.SetVector(_bottomSegmentsAId, Vector4.zero);
            _runtimeMaterial.SetVector(_bottomSegmentsBId, Vector4.zero);
            _runtimeMaterial.SetVector(_leftSegmentsId, Vector4.zero);
            _runtimeMaterial.SetVector(_rightSegmentsId, Vector4.zero);
            _runtimeMaterial.SetVector(_bottomAccentAId, Vector4.zero);
            _runtimeMaterial.SetVector(_bottomAccentBId, Vector4.zero);
            _runtimeMaterial.SetVector(_leftAccentId, Vector4.zero);
            _runtimeMaterial.SetVector(_rightAccentId, Vector4.zero);
        }

        private float GetPreviewStrength(float[] values, int index, float baseStrength)
        {
            float raw = values[index];

            if (_previewUsesAbsoluteStrengths)
            {
                return Mathf.Clamp(raw, 0f, 2f);
            }

            return Mathf.Clamp(baseStrength + raw, 0f, 2f);
        }

        private static float GetPreviewAccent(float[] values, int index)
        {
            return Mathf.Clamp01(values[index]);
        }

        private void EnsurePreviewArrays()
        {
            EnsureStrengthArray(ref _previewBottomStrengths, 5, _baseState.BottomStrength);
            EnsureStrengthArray(ref _previewLeftStrengths, 3, _baseState.LeftStrength);
            EnsureStrengthArray(ref _previewRightStrengths, 3, _baseState.RightStrength);

            EnsureAccentArray(ref _previewBottomAccents, 5);
            EnsureAccentArray(ref _previewLeftAccents, 3);
            EnsureAccentArray(ref _previewRightAccents, 3);
        }

        private static void EnsureStrengthArray(ref float[] values, int expectedLength, float defaultValue)
        {
            if (values == null || values.Length != expectedLength)
            {
                values = new float[expectedLength];
                FillStrengthArray(values, expectedLength, defaultValue);
                return;
            }

            for (int i = 0; i < expectedLength; i++)
            {
                values[i] = Mathf.Clamp(values[i], -2f, 2f);
            }
        }

        private static void EnsureAccentArray(ref float[] values, int expectedLength)
        {
            if (values == null || values.Length != expectedLength)
            {
                values = new float[expectedLength];
                FillZeroArray(values, expectedLength);
                return;
            }

            for (int i = 0; i < expectedLength; i++)
            {
                values[i] = Mathf.Clamp01(values[i]);
            }
        }

        private static void FillStrengthArray(float[] values, int count, float value)
        {
            float clamped = Mathf.Clamp(value, -2f, 2f);

            for (int i = 0; i < count; i++)
            {
                values[i] = clamped;
            }
        }

        private static void FillZeroArray(float[] values, int count)
        {
            for (int i = 0; i < count; i++)
            {
                values[i] = 0f;
            }
        }

        private static void ValidateArray(float[] values, int expectedLength, string paramName)
        {
            if (values == null || values.Length != expectedLength)
            {
                throw new ArgumentException(paramName);
            }
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();

            _baseState = AsymmetricInnerWashState.Sanitize(_baseState);

            if (_baseState.TintColor == default && _baseState.TopColor == default && _baseState.BottomColor == default)
            {
                _baseState = AsymmetricInnerWashState.CreateNeutralAmbient();
            }

            EnsurePreviewArrays();

            if (!_runtimeSegmentedActive)
            {
                _state = _baseState;
            }

            _stateInitialized = true;

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