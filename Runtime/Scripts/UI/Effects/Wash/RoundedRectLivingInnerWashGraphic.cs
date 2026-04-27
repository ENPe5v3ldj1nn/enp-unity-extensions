using System;
using UnityEngine;
using UnityEngine.UI;

namespace enp_unity_extensions.Runtime.Scripts.UI.Effects.Wash
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("UI/ENP/Rounded Rect Living Inner Wash")]
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RoundedRectLivingInnerWashGraphic : MaskableGraphic
    {
        private const string ShaderName = "UI/ENP/RoundedRectAsymmetricInnerWash";

        private static readonly int TintColorId = Shader.PropertyToID("_TintColor");
        private static readonly int TopColorId = Shader.PropertyToID("_TopColor");
        private static readonly int BottomColorId = Shader.PropertyToID("_BottomColor");
        private static readonly int BottomAccentColorId = Shader.PropertyToID("_BottomAccentColor");
        private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        private static readonly int ThicknessId = Shader.PropertyToID("_Thickness");
        private static readonly int SoftnessId = Shader.PropertyToID("_Softness");
        private static readonly int BandTightnessId = Shader.PropertyToID("_BandTightness");
        private static readonly int CenterClearId = Shader.PropertyToID("_CenterClear");
        private static readonly int RoundnessId = Shader.PropertyToID("_Roundness");
        private static readonly int TopStrengthId = Shader.PropertyToID("_TopStrength");
        private static readonly int BottomStrengthId = Shader.PropertyToID("_BottomStrength");
        private static readonly int LeftStrengthId = Shader.PropertyToID("_LeftStrength");
        private static readonly int RightStrengthId = Shader.PropertyToID("_RightStrength");
        private static readonly int RectSizeId = Shader.PropertyToID("_RectSize");
        private static readonly int RectCenterId = Shader.PropertyToID("_RectCenter");
        private static readonly int BottomSegmentsAId = Shader.PropertyToID("_BottomSegmentsA");
        private static readonly int BottomSegmentsBId = Shader.PropertyToID("_BottomSegmentsB");
        private static readonly int LeftSegmentsId = Shader.PropertyToID("_LeftSegments");
        private static readonly int RightSegmentsId = Shader.PropertyToID("_RightSegments");
        private static readonly int BottomAccentAId = Shader.PropertyToID("_BottomAccentA");
        private static readonly int BottomAccentBId = Shader.PropertyToID("_BottomAccentB");
        private static readonly int LeftAccentId = Shader.PropertyToID("_LeftAccent");
        private static readonly int RightAccentId = Shader.PropertyToID("_RightAccent");

        [SerializeField, HideInInspector] private LivingInnerWashState _state = LivingInnerWashState.Default();

        private readonly float[] _bottomSegments = new float[5];
        private readonly float[] _leftSegments = new float[3];
        private readonly float[] _rightSegments = new float[3];
        private readonly float[] _bottomAccents = new float[5];
        private readonly float[] _leftAccents = new float[3];
        private readonly float[] _rightAccents = new float[3];

        private Material _runtimeMaterial;
        private Shader _shader;
        private Vector2 _lastRectSize;
        private Vector2 _lastRectCenter;
        private bool _stateInitialized;
        private bool _animatedValuesActive;

        public override Texture mainTexture => Texture2D.whiteTexture;
        public LivingInnerWashState State => _state;

        protected override void Awake()
        {
            base.Awake();
            InitializeStateIfNeeded();
            EnsureMaterial();
            ApplyVisualState(true);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
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

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            SetVerticesDirty();
            ApplyVisualState(true);
        }

        public void SetState(LivingInnerWashState state)
        {
            _state = LivingInnerWashState.Sanitize(state);
            _stateInitialized = true;
            _animatedValuesActive = false;
            ApplyVisualState(true);
            SetMaterialDirty();
        }

        public void SetAnimatedValues(
            Color bottomColor,
            Color accentColor,
            float intensity,
            float thickness,
            float[] bottomSegments,
            float[] leftSegments,
            float[] rightSegments,
            float[] bottomAccents,
            float[] leftAccents,
            float[] rightAccents)
        {
            ValidateArray(bottomSegments, 5, nameof(bottomSegments));
            ValidateArray(leftSegments, 3, nameof(leftSegments));
            ValidateArray(rightSegments, 3, nameof(rightSegments));
            ValidateArray(bottomAccents, 5, nameof(bottomAccents));
            ValidateArray(leftAccents, 3, nameof(leftAccents));
            ValidateArray(rightAccents, 3, nameof(rightAccents));

            InitializeStateIfNeeded();
            EnsureMaterial();
            ApplyStaticProperties(false, bottomColor, accentColor, Mathf.Clamp01(intensity), Mathf.Clamp01(thickness));
            ApplyPackedSegmentData(bottomSegments, leftSegments, rightSegments, bottomAccents, leftAccents, rightAccents);
            _animatedValuesActive = true;
            SetMaterialDirty();
        }

        public void RestoreBase()
        {
            _animatedValuesActive = false;
            ApplyVisualState(false);
            SetMaterialDirty();
        }
#if UNITY_EDITOR
        protected override void Reset()
        {
            base.Reset();
            _state = LivingInnerWashState.Default();
            _stateInitialized = true;
            color = Color.white;
            raycastTarget = false;
            _animatedValuesActive = false;

            if (isActiveAndEnabled)
            {
                ApplyVisualState(true);
                SetVerticesDirty();
                SetMaterialDirty();
            }
        }
#endif
        private void InitializeStateIfNeeded()
        {
            if (_stateInitialized)
            {
                return;
            }

            if (_state.TintColor == default && _state.TopColor == default && _state.BottomColor == default)
            {
                _state = LivingInnerWashState.Default();
            }

            _state = LivingInnerWashState.Sanitize(_state);
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

            if (_shader == null)
            {
                _shader = Shader.Find(ShaderName);
            }

            if (_shader == null)
            {
                throw new InvalidOperationException($"{nameof(RoundedRectLivingInnerWashGraphic)} requires shader '{ShaderName}'.");
            }

            _runtimeMaterial = new Material(_shader)
            {
                name = "UI/ENP/RoundedRectLivingInnerWash (Runtime)",
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

            InitializeStateIfNeeded();
            EnsureMaterial();
            ApplyRect(forceRectRefresh);

            if (_animatedValuesActive)
            {
                return;
            }

            _state = LivingInnerWashState.Sanitize(_state);
            _state.BottomSegments.ToArray(_bottomSegments);
            _state.LeftSegments.ToArray(_leftSegments);
            _state.RightSegments.ToArray(_rightSegments);
            _state.BottomAccents.ToArray(_bottomAccents);
            _state.LeftAccents.ToArray(_leftAccents);
            _state.RightAccents.ToArray(_rightAccents);

            ApplyStaticProperties(false, _state.BottomColor, _state.AccentColor, _state.Intensity, _state.Thickness);
            ApplyPackedSegmentData(_bottomSegments, _leftSegments, _rightSegments, _bottomAccents, _leftAccents, _rightAccents);
        }

        private void ApplyRect(bool forceRectRefresh)
        {
            Rect rect = GetPixelAdjustedRect();
            Vector2 rectSize = rect.size;
            Vector2 rectCenter = rect.center;

            if (forceRectRefresh || rectSize != _lastRectSize || rectCenter != _lastRectCenter)
            {
                _runtimeMaterial.SetVector(RectSizeId, new Vector4(rectSize.x, rectSize.y, 0f, 0f));
                _runtimeMaterial.SetVector(RectCenterId, new Vector4(rectCenter.x, rectCenter.y, 0f, 0f));
                _lastRectSize = rectSize;
                _lastRectCenter = rectCenter;
            }
        }

        private void ApplyStaticProperties(bool forceRectRefresh, Color bottomColor, Color accentColor, float intensity, float thickness)
        {
            ApplyRect(forceRectRefresh);
            _runtimeMaterial.SetColor(TintColorId, _state.TintColor);
            _runtimeMaterial.SetColor(TopColorId, _state.TopColor);
            _runtimeMaterial.SetColor(BottomColorId, bottomColor);
            _runtimeMaterial.SetColor(BottomAccentColorId, accentColor);
            _runtimeMaterial.SetFloat(IntensityId, intensity);
            _runtimeMaterial.SetFloat(ThicknessId, thickness);
            _runtimeMaterial.SetFloat(SoftnessId, _state.Softness);
            _runtimeMaterial.SetFloat(BandTightnessId, _state.BandTightness);
            _runtimeMaterial.SetFloat(CenterClearId, _state.CenterClear);
            _runtimeMaterial.SetFloat(RoundnessId, _state.CornerRoundness);
            _runtimeMaterial.SetFloat(TopStrengthId, _state.TopStrength);
            _runtimeMaterial.SetFloat(BottomStrengthId, _state.BottomStrength);
            _runtimeMaterial.SetFloat(LeftStrengthId, _state.LeftStrength);
            _runtimeMaterial.SetFloat(RightStrengthId, _state.RightStrength);
        }

        private void ApplyPackedSegmentData(
            float[] bottomSegments,
            float[] leftSegments,
            float[] rightSegments,
            float[] bottomAccents,
            float[] leftAccents,
            float[] rightAccents)
        {
            _runtimeMaterial.SetVector(BottomSegmentsAId, new Vector4(bottomSegments[0], bottomSegments[1], bottomSegments[2], bottomSegments[3]));
            _runtimeMaterial.SetVector(BottomSegmentsBId, new Vector4(bottomSegments[4], 0f, 0f, 0f));
            _runtimeMaterial.SetVector(LeftSegmentsId, new Vector4(leftSegments[0], leftSegments[1], leftSegments[2], 0f));
            _runtimeMaterial.SetVector(RightSegmentsId, new Vector4(rightSegments[0], rightSegments[1], rightSegments[2], 0f));
            _runtimeMaterial.SetVector(BottomAccentAId, new Vector4(bottomAccents[0], bottomAccents[1], bottomAccents[2], bottomAccents[3]));
            _runtimeMaterial.SetVector(BottomAccentBId, new Vector4(bottomAccents[4], 0f, 0f, 0f));
            _runtimeMaterial.SetVector(LeftAccentId, new Vector4(leftAccents[0], leftAccents[1], leftAccents[2], 0f));
            _runtimeMaterial.SetVector(RightAccentId, new Vector4(rightAccents[0], rightAccents[1], rightAccents[2], 0f));
        }

        private static void ValidateArray(float[] values, int expectedLength, string parameterName)
        {
            if (values == null || values.Length != expectedLength)
            {
                throw new ArgumentException(parameterName);
            }
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            _state = LivingInnerWashState.Sanitize(_state);
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
