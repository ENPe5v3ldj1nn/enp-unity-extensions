using UnityEngine;
using UnityEngine.UI;

namespace enp_unity_extensions.Runtime.Scripts.UI.Form
{
    [ExecuteAlways]
    [AddComponentMenu("UI/Rounded Shape Graphic")]
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RoundedShapeGraphic : MaskableGraphic, ICanvasRaycastFilter
    {
        [SerializeField] private RoundedShapeStyle _style;
        [SerializeField] private bool _preciseRaycast = false;
        [SerializeField] private float _fillGradientAngleSpeed;
        [SerializeField] private float _borderGradientAngleSpeed;
        [SerializeField] private bool _useStyleBaseAngles = true;
        [SerializeField] private float _customFillGradientAngle = 90f;
        [SerializeField] private float _customBorderGradientAngle = 90f;
        [SerializeField] private bool _useStyleShapeProperties = true;
        [SerializeField] private bool _useStyleGradients = true;
        [SerializeField] private Gradient _customFillGradient = DefaultWhiteGradient();
        [SerializeField] private Gradient _customBorderGradient = DefaultWhiteGradient();
        [SerializeField] private RoundedShapeType _customShape = RoundedShapeType.RoundedRect;
        [SerializeField, Min(0f)] private float _customCornerRadius = 24f;
        [SerializeField, Min(0f)] private float _customBorderThickness;
        [SerializeField] private bool _customShadowEnabled;
        [SerializeField] private Color _customShadowColor = new Color(0f, 0f, 0f, 0.35f);
        [SerializeField] private Vector2 _customShadowOffset = new Vector2(0f, -6f);
        [SerializeField, Min(0f)] private float _customShadowBlur = 12f;
        [SerializeField, Min(0f)] private float _customShadowSpread;

        private static Material _sharedMaterial;
        private int _lastStyleVersion = -1;
        private Texture2D _customRamp;
        private ulong _customRampHash;

        public RoundedShapeStyle Style
        {
            get => _style;
            set
            {
                if (_style == value) return;
                _style = value;
                _lastStyleVersion = -1;
                SyncAnglesFromStyleIfNeeded();
                SetMaterialDirty();
                SetVerticesDirty();
            }
        }

        public Gradient FillGradient => ResolveFillGradient();
        public Gradient BorderGradient => ResolveBorderGradient();

        public bool UseStyleGradients
        {
            get => _useStyleGradients;
            set
            {
                if (_useStyleGradients == value) return;
                _useStyleGradients = value;
                MarkGradientTextureDirty();
            }
        }

        public void SetGradientOverrides(Gradient fillGradient, Gradient borderGradient)
        {
            _customFillGradient = EnsureGradient(fillGradient);
            _customBorderGradient = EnsureGradient(borderGradient);
            _useStyleGradients = false;
            MarkGradientTextureDirty();
        }

        public void ResetGradientsToStyle()
        {
            if (_style == null) return;
            _useStyleGradients = true;
            MarkGradientTextureDirty();
        }

        public void MarkGradientTextureDirty()
        {
            // IMPORTANT: do not mutate the style asset from a component instance.
            // Style cache invalidation is handled by the style itself (OnValidate / MarkDirty).
            InvalidateCustomRamp();
            SetVerticesDirty();
            SetMaterialDirty();
        }

        public float FillGradientAngle => _useStyleBaseAngles ? (_style?.FillGradientAngle ?? _customFillGradientAngle) : _customFillGradientAngle;
        public float BorderGradientAngle => _useStyleBaseAngles ? (_style?.BorderGradientAngle ?? _customBorderGradientAngle) : _customBorderGradientAngle;
        public float FillGradientAngleSpeed => _fillGradientAngleSpeed;
        public float BorderGradientAngleSpeed => _borderGradientAngleSpeed;

        public void SetBaseAngles(float fillAngle, float borderAngle)
        {
            var changed = _useStyleBaseAngles || !Mathf.Approximately(_customFillGradientAngle, fillAngle) || !Mathf.Approximately(_customBorderGradientAngle, borderAngle);
            if (!changed) return;
            _customFillGradientAngle = fillAngle;
            _customBorderGradientAngle = borderAngle;
            _useStyleBaseAngles = false;
            SetVerticesDirty();
        }

        public void ResetBaseAnglesToStyle()
        {
            if (_style == null)
                return;
            
            if (_useStyleBaseAngles) return;
            _customFillGradientAngle = _style.FillGradientAngle;
            _customBorderGradientAngle = _style.BorderGradientAngle;
            _useStyleBaseAngles = true;
            SetVerticesDirty();
        }

        public void SetGradientSpeeds(float fillSpeed, float borderSpeed)
        {
            var changed = false;
            if (!Mathf.Approximately(_fillGradientAngleSpeed, fillSpeed))
            {
                _fillGradientAngleSpeed = fillSpeed;
                changed = true;
            }
            if (!Mathf.Approximately(_borderGradientAngleSpeed, borderSpeed))
            {
                _borderGradientAngleSpeed = borderSpeed;
                changed = true;
            }
            if (changed) SetVerticesDirty();
        }

        public override Texture mainTexture
        {
            get
            {
                if (_useStyleGradients && _style != null)
                {
                    return _style.GetRampTexture();
                }
                var fillGradient = ResolveFillGradient();
                var borderGradient = ResolveBorderGradient();
                return GetCustomRampTexture(fillGradient, borderGradient);
            }
        }

        private Gradient ResolveFillGradient()
        {
            return _useStyleGradients && _style != null ? _style.FillGradient : EnsureGradient(_customFillGradient);
        }

        private Gradient ResolveBorderGradient()
        {
            return _useStyleGradients && _style != null ? _style.BorderGradient : EnsureGradient(_customBorderGradient);
        }

        private Texture GetCustomRampTexture(Gradient fillGradient, Gradient borderGradient)
        {
            if (fillGradient == null && borderGradient == null)
            {
                return s_WhiteTexture;
            }
            var hash = ComputeGradientHash(fillGradient, borderGradient);
            if (_customRamp != null && _customRampHash == hash) return _customRamp;
            DestroyCustomRamp();
            _customRamp = CreateRampTexture(fillGradient, borderGradient);
            _customRampHash = hash;
            return _customRamp;
        }

        private void InvalidateCustomRamp()
        {
            _customRampHash = 0;
            DestroyCustomRamp();
        }

        private void DestroyCustomRamp()
        {
            if (_customRamp == null) return;
            if (Application.isPlaying) Destroy(_customRamp);
            else DestroyImmediate(_customRamp);
            _customRamp = null;
        }

        private static Gradient EnsureGradient(Gradient gradient)
        {
            return gradient ?? DefaultWhiteGradient();
        }

        private void EnsureCustomGradients()
        {
            if (_customFillGradient == null)
                _customFillGradient = DefaultWhiteGradient();
            if (_customBorderGradient == null)
                _customBorderGradient = DefaultWhiteGradient();
        }

        private static Gradient DefaultWhiteGradient()
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
            );
            return gradient;
        }

        private Texture2D CreateRampTexture(Gradient fill, Gradient border)
        {
            const int width = 256;
            const int height = 2;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            texture.hideFlags = HideFlags.HideAndDontSave;
            var pixels = new Color32[width * height];
            for (int x = 0; x < width; x++)
            {
                var t = x / (float)(width - 1);
                var fillColor = fill != null ? fill.Evaluate(t) : Color.white;
                var borderColor = border != null ? border.Evaluate(t) : Color.white;
                pixels[x + 0 * width] = (Color32)fillColor;
                pixels[x + 1 * width] = (Color32)borderColor;
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private ulong ComputeGradientHash(Gradient fill, Gradient border)
        {
            var hash = 1469598103934665603UL;
            hash = (hash ^ HashGradient(fill)) * 1099511628211UL;
            hash = (hash ^ HashGradient(border)) * 1099511628211UL;
            return hash;
        }

        private static ulong HashGradient(Gradient gradient)
        {
            if (gradient == null) return 0UL;
            unchecked
            {
                ulong hash = 1469598103934665603UL;
                var colors = gradient.colorKeys;
                var alphas = gradient.alphaKeys;
                hash = (hash ^ (ulong)colors.Length) * 1099511628211UL;
                for (int i = 0; i < colors.Length; i++)
                {
                    hash = (hash ^ Quant01(colors[i].color.r)) * 1099511628211UL;
                    hash = (hash ^ Quant01(colors[i].color.g)) * 1099511628211UL;
                    hash = (hash ^ Quant01(colors[i].color.b)) * 1099511628211UL;
                    hash = (hash ^ QuantT(colors[i].time)) * 1099511628211UL;
                }
                hash = (hash ^ (ulong)alphas.Length) * 1099511628211UL;
                for (int i = 0; i < alphas.Length; i++)
                {
                    hash = (hash ^ Quant01(alphas[i].alpha)) * 1099511628211UL;
                    hash = (hash ^ QuantT(alphas[i].time)) * 1099511628211UL;
                }
                hash = (hash ^ (ulong)gradient.mode.GetHashCode()) * 1099511628211UL;
                return hash;
            }
        }

        private static ulong Quant01(float value) => (ulong)Mathf.Clamp(Mathf.RoundToInt(value * 65535f), 0, 65535);
        private static ulong QuantT(float value) => (ulong)Mathf.Clamp(Mathf.RoundToInt(value * 65535f), 0, 65535);

        protected override void OnEnable()
        {
            base.OnEnable();
            EnsureCanvasChannels();
            EnsureMaterial();
            EnsureCustomGradients();
            SyncStyleIfNeeded(true);
            SetVerticesDirty();
            SetMaterialDirty();
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            SetVerticesDirty();
        }

        #if UNITY_EDITOR
        protected override void OnValidate()
        {
            EnsureCanvasChannels();
            EnsureMaterial();
            SyncStyleIfNeeded(true);
            SetVerticesDirty();
            SetMaterialDirty();
            _customCornerRadius = Mathf.Max(0f, _customCornerRadius);
            _customBorderThickness = Mathf.Max(0f, _customBorderThickness);     
            _customShadowBlur = Mathf.Max(0f, _customShadowBlur);
            _customShadowSpread = Mathf.Max(0f, _customShadowSpread);
            EnsureCustomGradients();
        }
        #endif

        protected override void OnDisable()
        {
            base.OnDisable();
            DestroyCustomRamp();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            DestroyCustomRamp();
        }

        void Update()
        {
            if (!isActiveAndEnabled) return;
            if (_style == null)
                return;
            SyncStyleIfNeeded(false);
        }

        void SyncStyleIfNeeded(bool force)
        {
            if (_style == null)
                return;

            if (!force && _lastStyleVersion == _style.Version) return;     
            _lastStyleVersion = _style.Version;
            SyncAnglesFromStyleIfNeeded();
            SetMaterialDirty();
            SetVerticesDirty();
        }

        private void SyncAnglesFromStyleIfNeeded()
        {
            // If the user opted into custom angles, don't clobber them when the style changes.
            if (!_useStyleBaseAngles || _style == null) return;
            _customFillGradientAngle = _style.FillGradientAngle;
            _customBorderGradientAngle = _style.BorderGradientAngle;
        }

        void EnsureCanvasChannels()
        {
            if (canvas == null)
                return;
            
            canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1;
            canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord2;
            canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord3;
            canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.Tangent;
        }

        void EnsureMaterial()
        {
            if (_sharedMaterial == null)
            {
                var shader = Shader.Find("UI/RoundedShapeSDF");
                if (shader == null)
                {
                    Debug.LogError("Shader 'UI/RoundedShapeSDF' not found.");
                    return;
                }
                _sharedMaterial = new Material(shader);
                _sharedMaterial.hideFlags = HideFlags.HideAndDontSave;
            }
            if (material != _sharedMaterial) material = _sharedMaterial;
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            var r = rectTransform.rect;
            var w = r.width;
            var h = r.height;
            if (w <= 0f || h <= 0f) return;

            var center = r.center;
            var halfW = w * 0.5f;
            var halfH = h * 0.5f;

            var st = _style;
            var useStyleShape = _useStyleShapeProperties && st != null;

            var shape = useStyleShape ? st.Shape : _customShape;

            var cornerRadius = useStyleShape ? st.CornerRadius : _customCornerRadius;
            var borderThickness = useStyleShape ? st.BorderThickness : _customBorderThickness;

            var shadowEnabled = useStyleShape ? st.ShadowEnabled : _customShadowEnabled;
            var shadowColor = useStyleShape ? st.ShadowColor : _customShadowColor;
            var shadowOffset = useStyleShape ? st.ShadowOffset : _customShadowOffset;     
            var shadowBlur = useStyleShape ? st.ShadowBlur : _customShadowBlur;
            var shadowSpread = useStyleShape ? st.ShadowSpread : _customShadowSpread;

            var fillAngle = FillGradientAngle;
            var borderAngle = BorderGradientAngle;
            var fillSpeed = FillGradientAngleSpeed;
            var borderSpeed = BorderGradientAngleSpeed;

            var rad = Mathf.Min(Mathf.Max(0f, cornerRadius), Mathf.Min(halfW, halfH));
            var border = Mathf.Min(Mathf.Max(0f, borderThickness), Mathf.Min(halfW, halfH));
            var params0 = new Vector4(halfW, halfH, rad, border);
            var gradientData = new Vector4(fillAngle, fillSpeed, borderAngle, borderSpeed);

            var shCol = shadowEnabled ? shadowColor : new Color(0f, 0f, 0f, 0f);
            var tangent = new Vector4(shCol.r, shCol.g, shCol.b, shCol.a);

            var padX = 0f;
            var padY = 0f;
            if (shadowEnabled && shCol.a > 0.0001f)
            {
                padX = Mathf.Abs(shadowOffset.x) + shadowBlur + shadowSpread;
                padY = Mathf.Abs(shadowOffset.y) + shadowBlur + shadowSpread;
            }

            var p0 = new Vector3(r.xMin - padX, r.yMin - padY, 0f);
            var p1 = new Vector3(r.xMin - padX, r.yMax + padY, 0f);
            var p2 = new Vector3(r.xMax + padX, r.yMax + padY, 0f);
            var p3 = new Vector3(r.xMax + padX, r.yMin - padY, 0f);

            var sp = new Vector4(shadowOffset.x, shadowOffset.y, shadowBlur, shadowSpread);
            var flag = shape == RoundedShapeType.Ellipse ? 1f : 0f;

            var vcol = (Color32)color;

            AddVert(vh, p0, vcol, (Vector2)p0 - center, flag, sp, params0, gradientData, tangent);
            AddVert(vh, p1, vcol, (Vector2)p1 - center, flag, sp, params0, gradientData, tangent);
            AddVert(vh, p2, vcol, (Vector2)p2 - center, flag, sp, params0, gradientData, tangent);
            AddVert(vh, p3, vcol, (Vector2)p3 - center, flag, sp, params0, gradientData, tangent);

            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(2, 3, 0);
        }

        static void AddVert(VertexHelper vh, Vector3 pos, Color32 col, Vector2 local, float flag, Vector4 shadowParams, Vector4 params0, Vector4 gradientData, Vector4 tangent)
        {
            UIVertex v = UIVertex.simpleVert;
            v.position = pos;
            v.color = col;
            v.uv0 = shadowParams;
            v.uv1 = new Vector4(local.x, local.y, flag, 0f);
            v.uv2 = params0;
            v.uv3 = gradientData;
            v.tangent = tangent;
            vh.AddVert(v);
        }

        public bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera)
        {
            if (!_preciseRaycast) return true;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, sp, eventCamera, out var lp))
                return false;

            var r = rectTransform.rect;
            var center = r.center;
            var p = lp - center;

            var halfW = r.width * 0.5f;
            var halfH = r.height * 0.5f;
            if (halfW <= 0f || halfH <= 0f) return false;

            var st = _style;
            var useStyleShape = _useStyleShapeProperties && st != null;
            var shape = useStyleShape ? st.Shape : _customShape;
            var cornerRadius = useStyleShape ? st.CornerRadius : _customCornerRadius;

            if (shape == RoundedShapeType.Ellipse)
                return SdEllipseApprox(p, new Vector2(halfW, halfH)) <= 0f;

            var rad = Mathf.Min(Mathf.Max(0f, cornerRadius), Mathf.Min(halfW, halfH));
            return SdRoundRect(p, new Vector2(halfW, halfH), rad) <= 0f;
        }

        static float SdRoundRect(Vector2 p, Vector2 halfSize, float radius)
        {
            var q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - (halfSize - new Vector2(radius, radius));
            var mx = Mathf.Max(q.x, 0f);
            var my = Mathf.Max(q.y, 0f);
            var len = Mathf.Sqrt(mx * mx + my * my);
            var m = Mathf.Min(Mathf.Max(q.x, q.y), 0f);
            return len + m - radius;
        }

        static float SdEllipseApprox(Vector2 p, Vector2 halfSize)
        {
            var qx = p.x / Mathf.Max(halfSize.x, 1e-4f);
            var qy = p.y / Mathf.Max(halfSize.y, 1e-4f);
            var l = Mathf.Sqrt(qx * qx + qy * qy);
            return (l - 1f) * Mathf.Min(halfSize.x, halfSize.y);
        }
    }
}
