using UnityEngine;

namespace ENP.UnityExtensions.Runtime
{
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    [AddComponentMenu("2D/Rounded Shape 2D Renderer")]
    public sealed class RoundedShape2DRenderer : MonoBehaviour
    {
        [SerializeField] private RoundedShapeStyle _style;
        [SerializeField] private Color _color = Color.white;
        [SerializeField] private Vector2 _size = new Vector2(1f, 1f);
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
        [SerializeField, Min(0f)] private float _customCornerRadius = 0.24f;
        [SerializeField, Min(0f)] private float _customBorderThickness;
        [SerializeField] private bool _customShadowEnabled;
        [SerializeField] private Color _customShadowColor = new Color(0f, 0f, 0f, 0.35f);
        [SerializeField] private Vector2 _customShadowOffset = new Vector2(0f, -0.06f);
        [SerializeField, Min(0f)] private float _customShadowBlur = 0.12f;
        [SerializeField, Min(0f)] private float _customShadowSpread;

        private static Material _sharedMaterial;
        private int _lastStyleVersion = -1;
        private Texture2D _customRamp;
        private ulong _customRampHash;

        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private Mesh _mesh;

        public RoundedShapeStyle Style
        {
            get => _style;
            set
            {
                if (_style == value) return;
                _style = value;
                _lastStyleVersion = -1;
                SyncAnglesFromStyleIfNeeded();
                BuildMesh();
            }
        }

        public Color Color
        {
            get => _color;
            set
            {
                if (_color == value) return;
                _color = value;
                BuildMesh();
            }
        }

        public Vector2 Size
        {
            get => _size;
            set
            {
                if (_size == value) return;
                _size = value;
                BuildMesh();
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
            InvalidateCustomRamp();
            BuildMesh();
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
            BuildMesh();
        }

        public void ResetBaseAnglesToStyle()
        {
            if (_style == null)
                return;

            if (_useStyleBaseAngles) return;
            _customFillGradientAngle = _style.FillGradientAngle;
            _customBorderGradientAngle = _style.BorderGradientAngle;
            _useStyleBaseAngles = true;
            BuildMesh();
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
            if (changed) BuildMesh();
        }

        private Gradient ResolveFillGradient()
        {
            return _useStyleGradients && _style != null ? _style.FillGradient : EnsureGradient(_customFillGradient);
        }

        private Gradient ResolveBorderGradient()
        {
            return _useStyleGradients && _style != null ? _style.BorderGradient : EnsureGradient(_customBorderGradient);
        }

        private Texture GetRampTexture()
        {
            if (_useStyleGradients && _style != null)
                return _style.GetRampTexture();

            var fillGradient = ResolveFillGradient();
            var borderGradient = ResolveBorderGradient();
            return GetCustomRampTexture(fillGradient, borderGradient);
        }

        private Texture GetCustomRampTexture(Gradient fillGradient, Gradient borderGradient)
        {
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
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
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

        private void OnEnable()
        {
            _meshFilter = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();

            if (_mesh == null)
            {
                _mesh = new Mesh { name = "RoundedShape2D", hideFlags = HideFlags.DontSave };
            }
            _meshFilter.sharedMesh = _mesh;

            EnsureMaterial();
            EnsureCustomGradients();
            SyncStyleIfNeeded(true);
            BuildMesh();
        }

        private void OnDisable()
        {
            DestroyCustomRamp();
        }

        private void OnDestroy()
        {
            DestroyCustomRamp();
            if (_mesh != null)
            {
                if (Application.isPlaying) Destroy(_mesh);
                else DestroyImmediate(_mesh);
                _mesh = null;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _customCornerRadius = Mathf.Max(0f, _customCornerRadius);
            _customBorderThickness = Mathf.Max(0f, _customBorderThickness);
            _customShadowBlur = Mathf.Max(0f, _customShadowBlur);
            _customShadowSpread = Mathf.Max(0f, _customShadowSpread);
            EnsureCustomGradients();

            if (_meshFilter == null) _meshFilter = GetComponent<MeshFilter>();
            if (_meshRenderer == null) _meshRenderer = GetComponent<MeshRenderer>();
            EnsureMaterial();
            SyncStyleIfNeeded(true);
            BuildMesh();
        }
#endif

        private void Update()
        {
            if (_style == null)
                return;
            SyncStyleIfNeeded(false);
        }

        private void SyncStyleIfNeeded(bool force)
        {
            if (_style == null)
                return;

            if (!force && _lastStyleVersion == _style.Version) return;
            _lastStyleVersion = _style.Version;
            SyncAnglesFromStyleIfNeeded();
            BuildMesh();
        }

        private void SyncAnglesFromStyleIfNeeded()
        {
            if (!_useStyleBaseAngles || _style == null) return;
            _customFillGradientAngle = _style.FillGradientAngle;
            _customBorderGradientAngle = _style.BorderGradientAngle;
        }

        private void EnsureMaterial()
        {
            if (_sharedMaterial == null)
            {
                var shader = Shader.Find("Sprite2D/RoundedShapeSDF");
                if (shader == null)
                {
                    Debug.LogError("Shader 'Sprite2D/RoundedShapeSDF' not found.");
                    return;
                }
                _sharedMaterial = new Material(shader);
                _sharedMaterial.hideFlags = HideFlags.HideAndDontSave;
            }
            if (_meshRenderer != null && _meshRenderer.sharedMaterial != _sharedMaterial)
                _meshRenderer.sharedMaterial = _sharedMaterial;
        }

        private void BuildMesh()
        {
            if (_mesh == null || _meshRenderer == null) return;

            var halfW = _size.x * 0.5f;
            var halfH = _size.y * 0.5f;
            if (halfW <= 0f || halfH <= 0f)
            {
                _mesh.Clear();
                return;
            }

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

            var p0 = new Vector3(-halfW - padX, -halfH - padY, 0f);
            var p1 = new Vector3(-halfW - padX, halfH + padY, 0f);
            var p2 = new Vector3(halfW + padX, halfH + padY, 0f);
            var p3 = new Vector3(halfW + padX, -halfH - padY, 0f);

            var sp = new Vector4(shadowOffset.x, shadowOffset.y, shadowBlur, shadowSpread);
            var flag = shape == RoundedShapeType.Ellipse ? 1f : 0f;

            var vertices = new[] { p0, p1, p2, p3 };
            var colors = new[] { _color, _color, _color, _color };
            var uv0 = new[] { sp, sp, sp, sp };
            var uv1 = new[]
            {
                new Vector4(p0.x, p0.y, flag, 0f),
                new Vector4(p1.x, p1.y, flag, 0f),
                new Vector4(p2.x, p2.y, flag, 0f),
                new Vector4(p3.x, p3.y, flag, 0f)
            };
            var uv2 = new[] { params0, params0, params0, params0 };
            var uv3 = new[] { gradientData, gradientData, gradientData, gradientData };
            var tangents = new[] { tangent, tangent, tangent, tangent };
            var triangles = new[] { 0, 1, 2, 2, 3, 0 };

            _mesh.Clear();
            _mesh.SetVertices(vertices);
            _mesh.SetColors(colors);
            _mesh.SetUVs(0, uv0);
            _mesh.SetUVs(1, uv1);
            _mesh.SetUVs(2, uv2);
            _mesh.SetUVs(3, uv3);
            _mesh.SetTangents(tangents);
            _mesh.SetTriangles(triangles, 0);
            _mesh.RecalculateBounds();

            _meshRenderer.GetPropertyBlock(_propertyBlock ??= new MaterialPropertyBlock());
            _propertyBlock.SetTexture("_MainTex", GetRampTexture());
            _meshRenderer.SetPropertyBlock(_propertyBlock);
        }

        private MaterialPropertyBlock _propertyBlock;
    }
}
