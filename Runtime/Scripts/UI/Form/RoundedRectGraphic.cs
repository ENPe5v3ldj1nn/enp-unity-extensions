using enp_unity_extensions.Scripts.UI.Form;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace enp_unity_extensions.Runtime.Scripts.UI.Form
{
    [ExecuteAlways]
    [AddComponentMenu("UI/Rounded Shape Graphic")]
    public sealed class RoundedShapeGraphic : MaskableGraphic, ICanvasRaycastFilter
    {
        [SerializeField] private RoundedShapeStyle _style;
        [SerializeField] private bool _preciseRaycast = false;

        private static Material _sharedMaterial;
        private int _lastStyleVersion = -1;

        public RoundedShapeStyle Style
        {
            get => _style;
            set
            {
                if (_style == value) return;
                _style = value;
                _lastStyleVersion = -1;
                SetMaterialDirty();
                SetVerticesDirty();
            }
        }

        public override Texture mainTexture
        {
            get
            {
                if (_style == null) return s_WhiteTexture;
                return _style.GetRampTexture();
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            EnsureCanvasChannels();
            EnsureMaterial();
            SyncStyleIfNeeded(true);
            SetVerticesDirty();
            SetMaterialDirty();
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            SetVerticesDirty();
        }

        protected override void OnValidate()
        {
            EnsureCanvasChannels();
            EnsureMaterial();
            SyncStyleIfNeeded(true);
            SetVerticesDirty();
            SetMaterialDirty();
        }

        void Update()
        {
            if (!isActiveAndEnabled) return;
            if (_style == null) return;
            SyncStyleIfNeeded(false);
        }

        void SyncStyleIfNeeded(bool force)
        {
            if (_style == null) return;
            if (!force && _lastStyleVersion == _style.Version) return;
            _lastStyleVersion = _style.Version;
            SetMaterialDirty();
            SetVerticesDirty();
        }

        void EnsureCanvasChannels()
        {
            if (canvas == null) return;
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

            var shape = st != null ? st.shape : RoundedShapeType.RoundedRect;

            var cornerRadius = st != null ? st.cornerRadius : 24f;
            var borderThickness = st != null ? st.borderThickness : 0f;

            var shadowEnabled = st != null && st.shadowEnabled;
            var shadowColor = st != null ? st.shadowColor : new Color(0f, 0f, 0f, 0f);
            var shadowOffset = st != null ? st.shadowOffset : Vector2.zero;
            var shadowBlur = st != null ? st._shadowBlur : 0f;
            var shadowSpread = st != null ? st._shadowSpread : 0f;

            var fillAngle = st != null ? st.fillGradientAngle : 90f;
            var borderAngle = st != null ? st.borderGradientAngle : 90f;

            var rad = Mathf.Min(Mathf.Max(0f, cornerRadius), Mathf.Min(halfW, halfH));
            var border = Mathf.Min(Mathf.Max(0f, borderThickness), Mathf.Min(halfW, halfH));
            var params0 = new Vector4(halfW, halfH, rad, border);

            var fa = fillAngle * Mathf.Deg2Rad;
            var ba = borderAngle * Mathf.Deg2Rad;

            var fillDir = new Vector2(Mathf.Cos(fa), Mathf.Sin(fa));
            if (fillDir.sqrMagnitude < 1e-6f) fillDir = Vector2.up;
            fillDir.Normalize();

            var borderDir = new Vector2(Mathf.Cos(ba), Mathf.Sin(ba));
            if (borderDir.sqrMagnitude < 1e-6f) borderDir = Vector2.up;
            borderDir.Normalize();

            var gradDirs = new Vector4(fillDir.x, fillDir.y, borderDir.x, borderDir.y);

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

            AddVert(vh, p0, vcol, (Vector2)p0 - center, flag, sp, params0, gradDirs, tangent);
            AddVert(vh, p1, vcol, (Vector2)p1 - center, flag, sp, params0, gradDirs, tangent);
            AddVert(vh, p2, vcol, (Vector2)p2 - center, flag, sp, params0, gradDirs, tangent);
            AddVert(vh, p3, vcol, (Vector2)p3 - center, flag, sp, params0, gradDirs, tangent);

            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(2, 3, 0);
        }

        static void AddVert(VertexHelper vh, Vector3 pos, Color32 col, Vector2 local, float flag, Vector4 shadowParams, Vector4 params0, Vector4 gradDirs, Vector4 tangent)
        {
            UIVertex v = UIVertex.simpleVert;
            v.position = pos;
            v.color = col;
            v.uv0 = shadowParams;
            v.uv1 = new Vector4(local.x, local.y, flag, 0f);
            v.uv2 = params0;
            v.uv3 = gradDirs;
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
            var shape = st != null ? st.shape : RoundedShapeType.RoundedRect;
            var cornerRadius = st != null ? st.cornerRadius : 24f;

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
