using UnityEngine;

namespace enp_unity_extensions.Runtime.Scripts.UI.Form
{
    [CreateAssetMenu(menuName = "UI/Rounded Shape Style", fileName = "RoundedShapeStyle")]
    public sealed class RoundedShapeStyle : ScriptableObject
    {
        public RoundedShapeType Shape = RoundedShapeType.RoundedRect;

        public Gradient FillGradient = DefaultWhiteGradient();
        [Range(0f, 360f)] public float FillGradientAngle = 90f;

        public Gradient BorderGradient = DefaultWhiteGradient();
        [Range(0f, 360f)] public float BorderGradientAngle = 90f;

        [Min(0f)] public float CornerRadius = 24f;
        [Min(0f)] public float BorderThickness = 0f;

        public bool ShadowEnabled = false;
        public Color ShadowColor = new Color(0f, 0f, 0f, 0.35f);
        public Vector2 ShadowOffset = new Vector2(0f, -6f);
        [Min(0f)] public float ShadowBlur = 12f;
        [Min(0f)] public float ShadowSpread = 0f;

        [SerializeField, HideInInspector] private int _version = 1;

        [System.NonSerialized] private Texture2D _ramp;
        [System.NonSerialized] private ulong _rampHash;

        public int Version => _version;

        private void OnValidate()
        {
            CornerRadius = Mathf.Max(0f, CornerRadius);
            BorderThickness = Mathf.Max(0f, BorderThickness);
            ShadowBlur = Mathf.Max(0f, ShadowBlur);
            ShadowSpread = Mathf.Max(0f, ShadowSpread);
            _version++;
            if (_version < 1) _version = 1;
            _rampHash = 0;
        }

        private void OnDisable()
        {
            if (_ramp == null) return;
            if (Application.isPlaying) Destroy(_ramp);
            else DestroyImmediate(_ramp);
            _ramp = null;
            _rampHash = 0;
        }

        public Texture2D GetRampTexture()
        {
            var h = ComputeHash();
            if (_ramp != null && _rampHash == h) return _ramp;
            _rampHash = h;
            _ramp = CreateRampTexture(FillGradient, BorderGradient);
            return _ramp;
        }

        private ulong ComputeHash()
        {
            ulong h = 1469598103934665603UL;
            h = (h ^ HashGradient(FillGradient)) * 1099511628211UL;
            h = (h ^ HashGradient(BorderGradient)) * 1099511628211UL;
            h = (h ^ (ulong)Mathf.RoundToInt(FillGradientAngle * 1000f)) * 1099511628211UL;
            h = (h ^ (ulong)Mathf.RoundToInt(BorderGradientAngle * 1000f)) * 1099511628211UL;
            h = (h ^ (ulong)Shape) * 1099511628211UL;
            h = (h ^ (ulong)Mathf.RoundToInt(CornerRadius * 1000f)) * 1099511628211UL;
            h = (h ^ (ulong)Mathf.RoundToInt(BorderThickness * 1000f)) * 1099511628211UL;
            h = (h ^ (ulong)ShadowEnabled.GetHashCode()) * 1099511628211UL;
            h = (h ^ Quant01(ShadowColor.r)) * 1099511628211UL;
            h = (h ^ Quant01(ShadowColor.g)) * 1099511628211UL;
            h = (h ^ Quant01(ShadowColor.b)) * 1099511628211UL;
            h = (h ^ Quant01(ShadowColor.a)) * 1099511628211UL;
            h = (h ^ Quant01(ShadowOffset.x * 0.01f)) * 1099511628211UL;
            h = (h ^ Quant01(ShadowOffset.y * 0.01f)) * 1099511628211UL;
            h = (h ^ (ulong)Mathf.RoundToInt(ShadowBlur * 1000f)) * 1099511628211UL;
            h = (h ^ (ulong)Mathf.RoundToInt(ShadowSpread * 1000f)) * 1099511628211UL;
            return h;
        }

        private static ulong HashGradient(Gradient g)
        {
            if (g == null) return 0UL;
            unchecked
            {
                ulong h = 1469598103934665603UL;
                var c = g.colorKeys;
                var a = g.alphaKeys;

                h = (h ^ (ulong)c.Length) * 1099511628211UL;
                for (int i = 0; i < c.Length; i++)
                {
                    h = (h ^ Quant01(c[i].color.r)) * 1099511628211UL;
                    h = (h ^ Quant01(c[i].color.g)) * 1099511628211UL;
                    h = (h ^ Quant01(c[i].color.b)) * 1099511628211UL;
                    h = (h ^ QuantT(c[i].time)) * 1099511628211UL;
                }

                h = (h ^ (ulong)a.Length) * 1099511628211UL;
                for (int i = 0; i < a.Length; i++)
                {
                    h = (h ^ Quant01(a[i].alpha)) * 1099511628211UL;
                    h = (h ^ QuantT(a[i].time)) * 1099511628211UL;
                }

                h = (h ^ (ulong)g.mode.GetHashCode()) * 1099511628211UL;
                return h;
            }
        }

        private static ulong Quant01(float v) => (ulong)Mathf.Clamp(Mathf.RoundToInt(v * 65535f), 0, 65535);
        private static ulong QuantT(float v) => (ulong)Mathf.Clamp(Mathf.RoundToInt(v * 65535f), 0, 65535);

        private static Texture2D CreateRampTexture(Gradient fill, Gradient border)
        {
            const int w = 256;
            const int h = 2;

            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false, true);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            tex.hideFlags = HideFlags.HideAndDontSave;

            var pixels = new Color32[w * h];

            for (int x = 0; x < w; x++)
            {
                var t = x / (float)(w - 1);
                var cf = fill != null ? fill.Evaluate(t) : Color.white;
                var cb = border != null ? border.Evaluate(t) : Color.white;
                pixels[x + 0 * w] = (Color32)cf;
                pixels[x + 1 * w] = (Color32)cb;
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            return tex;
        }

        private static Gradient DefaultWhiteGradient()
        {
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
            );
            return g;
        }
    }
}
