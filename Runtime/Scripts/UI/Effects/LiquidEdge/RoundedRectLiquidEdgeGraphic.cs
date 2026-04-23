using System;
using UnityEngine;
using UnityEngine.UI;

namespace enp_unity_extensions.Runtime.Scripts.UI.Effects.LiquidEdge
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("UI/ENP/Rounded Rect Liquid Edge")]
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RoundedRectLiquidEdgeGraphic : MaskableGraphic
    {
        private const string ShaderName = "UI/ENP/RoundedRectLiquidEdge";

        private static readonly int BaseColorAId = Shader.PropertyToID("_BaseColorA");
        private static readonly int BaseColorBId = Shader.PropertyToID("_BaseColorB");
        private static readonly int AccentColorId = Shader.PropertyToID("_AccentColor");
        private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        private static readonly int ThicknessId = Shader.PropertyToID("_Thickness");
        private static readonly int SoftnessId = Shader.PropertyToID("_Softness");
        private static readonly int CenterClearId = Shader.PropertyToID("_CenterClear");
        private static readonly int RoundnessId = Shader.PropertyToID("_Roundness");
        private static readonly int AmbientMotionIntensityId = Shader.PropertyToID("_AmbientMotionIntensity");
        private static readonly int HighlightZoneIntensityId = Shader.PropertyToID("_HighlightZoneIntensity");
        private static readonly int HighlightColorShiftId = Shader.PropertyToID("_HighlightColorShift");
        private static readonly int HighlightShapeInfluenceId = Shader.PropertyToID("_HighlightShapeInfluence");
        private static readonly int HighlightIntensityInfluenceId = Shader.PropertyToID("_HighlightIntensityInfluence");
        private static readonly int HighlightColorInfluenceId = Shader.PropertyToID("_HighlightColorInfluence");
        private static readonly int BottomDominanceId = Shader.PropertyToID("_BottomDominance");
        private static readonly int SideSupportAmountId = Shader.PropertyToID("_SideSupportAmount");
        private static readonly int TopSuppressionId = Shader.PropertyToID("_TopSuppression");
        private static readonly int TimeSeedId = Shader.PropertyToID("_TimeSeed");
        private static readonly int RectSizeId = Shader.PropertyToID("_RectSize");
        private static readonly int RectCenterId = Shader.PropertyToID("_RectCenter");
        private static readonly int BottomStrengthAId = Shader.PropertyToID("_BottomStrengthA");
        private static readonly int BottomStrengthBId = Shader.PropertyToID("_BottomStrengthB");
        private static readonly int LeftStrengthId = Shader.PropertyToID("_LeftStrength");
        private static readonly int RightStrengthId = Shader.PropertyToID("_RightStrength");
        private static readonly int BottomDriftAId = Shader.PropertyToID("_BottomDriftA");
        private static readonly int BottomDriftBId = Shader.PropertyToID("_BottomDriftB");
        private static readonly int LeftDriftId = Shader.PropertyToID("_LeftDrift");
        private static readonly int RightDriftId = Shader.PropertyToID("_RightDrift");
        private static readonly int BottomAccentAId = Shader.PropertyToID("_BottomAccentA");
        private static readonly int BottomAccentBId = Shader.PropertyToID("_BottomAccentB");
        private static readonly int LeftAccentId = Shader.PropertyToID("_LeftAccent");
        private static readonly int RightAccentId = Shader.PropertyToID("_RightAccent");

        [SerializeField] private LiquidEdgeState _state = LiquidEdgeState.Living();
        [SerializeField] private bool _useUnscaledTime = true;

        private readonly float[] _zero3 = new float[3];
        private readonly float[] _zero5 = new float[5];
        private Material _runtimeMaterial;
        private Vector2 _lastRectSize;
        private Vector2 _lastRectCenter;
        private bool _stateInitialized;

        public override Texture mainTexture => Texture2D.whiteTexture;
        public LiquidEdgeState State => _state;
        public bool UseUnscaledTime => _useUnscaledTime;

        protected override void Awake()
        {
            base.Awake();
            InitializeStateIfNeeded();
            EnsureMaterial();
            ApplyState(true);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            InitializeStateIfNeeded();
            EnsureMaterial();
            SetAllDirty();
            ApplyState(true);
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
            ApplyState(true);
        }

        public void SetState(LiquidEdgeState state)
        {
            _state = LiquidEdgeState.Sanitize(state);
            _stateInitialized = true;
            ApplyState(false);
            SetMaterialDirty();
        }

        public void SetGlobalIntensity(float value)
        {
            InitializeStateIfNeeded();
            _state.GlobalIntensity = Mathf.Clamp(value, 0f, 2f);
            ApplyState(false);
        }

        public void SetBottomDominance(float value)
        {
            InitializeStateIfNeeded();
            _state.BottomDominance = Mathf.Clamp(value, 0f, 2f);
            ApplyState(false);
        }

        public void SetSideSupportAmount(float value)
        {
            InitializeStateIfNeeded();
            _state.SideSupportAmount = Mathf.Clamp(value, 0f, 2f);
            ApplyState(false);
        }

        public void SetTopSuppression(float value)
        {
            InitializeStateIfNeeded();
            _state.TopSuppression = Mathf.Clamp01(value);
            ApplyState(false);
        }

        public void SetRuntimeSegmentValues(float[] strengths, float[] drifts, float[] accents, float timeSeed)
        {
            if (strengths == null || strengths.Length != 11) throw new ArgumentException(nameof(strengths));
            if (drifts == null || drifts.Length != 11) throw new ArgumentException(nameof(drifts));
            if (accents == null || accents.Length != 11) throw new ArgumentException(nameof(accents));

            InitializeStateIfNeeded();
            EnsureMaterial();
            ApplyState(false, false);
            ApplySegmentData(strengths, drifts, accents, timeSeed);
        }

        public void ClearRuntimeMotion()
        {
            InitializeStateIfNeeded();
            EnsureMaterial();
            ApplyState(false);
            ApplySegmentData(_state.SegmentStrengths.ToArray(), CreateZeroSegments(), CreateZeroSegments(), 0f);
            SetMaterialDirty();
        }

        protected override void Reset()
        {
            base.Reset();
            _state = LiquidEdgeState.Living();
            _stateInitialized = true;
            color = Color.white;
            raycastTarget = false;

            if (isActiveAndEnabled)
            {
                ApplyState(true);
                SetVerticesDirty();
                SetMaterialDirty();
            }
        }

        private void InitializeStateIfNeeded()
        {
            if (_stateInitialized)
            {
                return;
            }

            if (_state.BaseColorA == default && _state.BaseColorB == default && _state.AccentColor == default)
            {
                _state = LiquidEdgeState.Living();
            }

            _state = LiquidEdgeState.Sanitize(_state);
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

            Shader shader = Shader.Find(ShaderName);

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

        private void ApplyState(bool forceRectRefresh, bool applyStaticSegments = true)
        {
            if (!IsActive())
            {
                return;
            }

            InitializeStateIfNeeded();
            EnsureMaterial();

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

            _state = LiquidEdgeState.Sanitize(_state);
            _runtimeMaterial.SetColor(BaseColorAId, _state.BaseColorA);
            _runtimeMaterial.SetColor(BaseColorBId, _state.BaseColorB);
            _runtimeMaterial.SetColor(AccentColorId, _state.AccentColor);
            _runtimeMaterial.SetFloat(IntensityId, _state.GlobalIntensity);
            _runtimeMaterial.SetFloat(ThicknessId, _state.Thickness);
            _runtimeMaterial.SetFloat(SoftnessId, _state.Softness);
            _runtimeMaterial.SetFloat(CenterClearId, _state.CenterClear);
            _runtimeMaterial.SetFloat(RoundnessId, _state.CornerRoundness);
            _runtimeMaterial.SetFloat(AmbientMotionIntensityId, _state.AmbientMotionIntensity);
            _runtimeMaterial.SetFloat(HighlightZoneIntensityId, _state.HighlightZoneIntensity);
            _runtimeMaterial.SetFloat(HighlightColorShiftId, _state.HighlightZoneColorShiftAmount);
            _runtimeMaterial.SetFloat(HighlightShapeInfluenceId, _state.HighlightZoneShapeInfluence);
            _runtimeMaterial.SetFloat(HighlightIntensityInfluenceId, _state.HighlightZoneIntensityInfluence);
            _runtimeMaterial.SetFloat(HighlightColorInfluenceId, _state.HighlightZoneColorInfluence);
            _runtimeMaterial.SetFloat(BottomDominanceId, _state.BottomDominance);
            _runtimeMaterial.SetFloat(SideSupportAmountId, _state.SideSupportAmount);
            _runtimeMaterial.SetFloat(TopSuppressionId, _state.TopSuppression);
            if (applyStaticSegments)
            {
                ApplySegmentData(_state.SegmentStrengths.ToArray(), CreateZeroSegments(), CreateZeroSegments(), 0f);
            }
        }

        private void ApplySegmentData(float[] strengths, float[] drifts, float[] accents, float timeSeed)
        {
            PackSegments(strengths, out Vector4 bottomA, out Vector4 bottomB, out Vector4 left, out Vector4 right);
            PackSegments(drifts, out Vector4 bottomDriftA, out Vector4 bottomDriftB, out Vector4 leftDrift, out Vector4 rightDrift);
            PackSegments(accents, out Vector4 bottomAccentA, out Vector4 bottomAccentB, out Vector4 leftAccent, out Vector4 rightAccent);

            _runtimeMaterial.SetFloat(TimeSeedId, timeSeed);
            _runtimeMaterial.SetVector(BottomStrengthAId, bottomA);
            _runtimeMaterial.SetVector(BottomStrengthBId, bottomB);
            _runtimeMaterial.SetVector(LeftStrengthId, left);
            _runtimeMaterial.SetVector(RightStrengthId, right);
            _runtimeMaterial.SetVector(BottomDriftAId, bottomDriftA);
            _runtimeMaterial.SetVector(BottomDriftBId, bottomDriftB);
            _runtimeMaterial.SetVector(LeftDriftId, leftDrift);
            _runtimeMaterial.SetVector(RightDriftId, rightDrift);
            _runtimeMaterial.SetVector(BottomAccentAId, bottomAccentA);
            _runtimeMaterial.SetVector(BottomAccentBId, bottomAccentB);
            _runtimeMaterial.SetVector(LeftAccentId, leftAccent);
            _runtimeMaterial.SetVector(RightAccentId, rightAccent);
        }

        private static void PackSegments(float[] values, out Vector4 bottomA, out Vector4 bottomB, out Vector4 left, out Vector4 right)
        {
            left = new Vector4(values[0], values[1], values[2], 0f);
            right = new Vector4(values[3], values[4], values[5], 0f);
            bottomA = new Vector4(values[6], values[7], values[8], values[9]);
            bottomB = new Vector4(values[10], 0f, 0f, 0f);
        }

        private float[] CreateZeroSegments()
        {
            return new[]
            {
                _zero3[0], _zero3[1], _zero3[2],
                _zero3[0], _zero3[1], _zero3[2],
                _zero5[0], _zero5[1], _zero5[2], _zero5[3], _zero5[4]
            };
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            _state = LiquidEdgeState.Sanitize(_state);
            _stateInitialized = true;

            if (isActiveAndEnabled)
            {
                ApplyState(true);
                SetVerticesDirty();
                SetMaterialDirty();
            }
        }
#endif
    }
}
