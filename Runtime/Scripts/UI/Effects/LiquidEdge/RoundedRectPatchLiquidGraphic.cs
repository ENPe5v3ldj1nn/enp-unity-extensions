using System;
using UnityEngine;
using UnityEngine.UI;

namespace enp_unity_extensions.Runtime.Scripts.UI.Effects.LiquidEdge
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("UI/ENP/Rounded Rect Patch Liquid")]
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RoundedRectPatchLiquidGraphic : MaskableGraphic
    {
        private const string ShaderName = "UI/ENP/RoundedRectPatchLiquid";
        public const int MaxPatches = 9;

        private static readonly int BaseColorAId = Shader.PropertyToID("_BaseColorA");
        private static readonly int BaseColorBId = Shader.PropertyToID("_BaseColorB");
        private static readonly int AccentColorId = Shader.PropertyToID("_AccentColor");
        private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        private static readonly int CenterClearId = Shader.PropertyToID("_CenterClear");
        private static readonly int RoundnessId = Shader.PropertyToID("_Roundness");
        private static readonly int AmbientIntensityId = Shader.PropertyToID("_AmbientIntensity");
        private static readonly int PatchShapeInfluenceId = Shader.PropertyToID("_PatchShapeInfluence");
        private static readonly int PatchColorInfluenceId = Shader.PropertyToID("_PatchColorInfluence");
        private static readonly int BottomDominanceId = Shader.PropertyToID("_BottomDominance");
        private static readonly int SideSupportId = Shader.PropertyToID("_SideSupport");
        private static readonly int TopSuppressionId = Shader.PropertyToID("_TopSuppression");
        private static readonly int BottomToSideBleedId = Shader.PropertyToID("_BottomToSideBleed");
        private static readonly int CornerBleedAttenuationId = Shader.PropertyToID("_CornerBleedAttenuation");
        private static readonly int BottomThicknessMultiplierId = Shader.PropertyToID("_BottomThicknessMultiplier");
        private static readonly int SideThicknessMultiplierId = Shader.PropertyToID("_SideThicknessMultiplier");
        private static readonly int TimeSeedId = Shader.PropertyToID("_TimeSeed");
        private static readonly int PatchCountId = Shader.PropertyToID("_PatchCount");
        private static readonly int RectSizeId = Shader.PropertyToID("_RectSize");
        private static readonly int RectCenterId = Shader.PropertyToID("_RectCenter");
        private static readonly int PatchAId = Shader.PropertyToID("_PatchA");
        private static readonly int PatchBId = Shader.PropertyToID("_PatchB");

        [SerializeField] private PatchLiquidEdgeState _state = PatchLiquidEdgeState.Living();
        [SerializeField] private bool _useUnscaledTime = true;

        private readonly Vector4[] _emptyPatchA = new Vector4[MaxPatches];
        private readonly Vector4[] _emptyPatchB = new Vector4[MaxPatches];
        private Material _runtimeMaterial;
        private Vector2 _lastRectSize;
        private Vector2 _lastRectCenter;
        private bool _stateInitialized;

        public override Texture mainTexture => Texture2D.whiteTexture;
        public PatchLiquidEdgeState State => _state;
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

        public void SetState(PatchLiquidEdgeState state)
        {
            _state = PatchLiquidEdgeState.Sanitize(state);
            _stateInitialized = true;
            ApplyState(false);
            SetMaterialDirty();
        }

        public void SetGlobalIntensity(float value)
        {
            InitializeStateIfNeeded();
            _state.GlobalIntensity = Mathf.Clamp(value, 0f, 2f);
            ApplyState(false, false);
        }

        public void SetRuntimePatches(Vector4[] patchA, Vector4[] patchB, int patchCount, float timeSeed)
        {
            if (patchA == null || patchA.Length < MaxPatches) throw new ArgumentException(nameof(patchA));
            if (patchB == null || patchB.Length < MaxPatches) throw new ArgumentException(nameof(patchB));

            InitializeStateIfNeeded();
            EnsureMaterial();
            ApplyState(false, false);
            _runtimeMaterial.SetFloat(TimeSeedId, timeSeed);
            _runtimeMaterial.SetFloat(PatchCountId, Mathf.Clamp(patchCount, 0, MaxPatches));
            _runtimeMaterial.SetVectorArray(PatchAId, patchA);
            _runtimeMaterial.SetVectorArray(PatchBId, patchB);
        }

#if UNITY_EDITOR
        protected override void Reset()
        {
            base.Reset();
            _state = PatchLiquidEdgeState.Living();
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
#endif

        private void InitializeStateIfNeeded()
        {
            if (_stateInitialized)
            {
                return;
            }

            if (_state.BaseColorA == default && _state.BaseColorB == default && _state.AccentColor == default)
            {
                _state = PatchLiquidEdgeState.Living();
            }

            _state = PatchLiquidEdgeState.Sanitize(_state);
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

        private void ApplyState(bool forceRectRefresh, bool clearPatches = true)
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

            _state = PatchLiquidEdgeState.Sanitize(_state);
            _runtimeMaterial.SetColor(BaseColorAId, _state.BaseColorA);
            _runtimeMaterial.SetColor(BaseColorBId, _state.BaseColorB);
            _runtimeMaterial.SetColor(AccentColorId, _state.AccentColor);
            _runtimeMaterial.SetFloat(IntensityId, _state.GlobalIntensity);
            _runtimeMaterial.SetFloat(CenterClearId, _state.CenterClear);
            _runtimeMaterial.SetFloat(RoundnessId, _state.CornerRoundness);
            _runtimeMaterial.SetFloat(AmbientIntensityId, _state.AmbientBaseLayerIntensity);
            _runtimeMaterial.SetFloat(PatchShapeInfluenceId, _state.PatchShapeInfluence);
            _runtimeMaterial.SetFloat(PatchColorInfluenceId, _state.PatchColorInfluence);
            _runtimeMaterial.SetFloat(BottomDominanceId, _state.BottomDominance);
            _runtimeMaterial.SetFloat(SideSupportId, _state.SideSupport);
            _runtimeMaterial.SetFloat(TopSuppressionId, _state.TopSuppression);
            _runtimeMaterial.SetFloat(BottomToSideBleedId, _state.BottomToSideBleed);
            _runtimeMaterial.SetFloat(CornerBleedAttenuationId, _state.CornerBleedAttenuation);
            _runtimeMaterial.SetFloat(BottomThicknessMultiplierId, _state.BottomThicknessMultiplier);
            _runtimeMaterial.SetFloat(SideThicknessMultiplierId, _state.SideThicknessMultiplier);

            if (clearPatches)
            {
                _runtimeMaterial.SetFloat(PatchCountId, 0f);
                _runtimeMaterial.SetVectorArray(PatchAId, _emptyPatchA);
                _runtimeMaterial.SetVectorArray(PatchBId, _emptyPatchB);
            }
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            _state = PatchLiquidEdgeState.Sanitize(_state);
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
