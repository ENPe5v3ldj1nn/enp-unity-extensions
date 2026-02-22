using UnityEngine;

namespace enp_unity_extensions.Runtime.Scripts.UI.Form
{
    [CreateAssetMenu(menuName = "UI/Rounded Shape Style", fileName = "RoundedShapeStyle")]
    public sealed class RoundedShapeStyle : ScriptableObject
    {
        [SerializeField] private RoundedShapeType _shape = RoundedShapeType.RoundedRect;
        [SerializeField] private Gradient _fillGradient = DefaultWhiteGradient();
        [SerializeField, Range(0f, 360f)] private float _fillGradientAngle = 90f;
        [SerializeField] private Gradient _borderGradient = DefaultWhiteGradient();
        [SerializeField, Range(0f, 360f)] private float _borderGradientAngle = 90f;
        [SerializeField, Min(0f)] private float _cornerRadius = 24f;
        [SerializeField, Min(0f)] private float _borderThickness = 0f;
        [SerializeField] private bool _shadowEnabled;
        [SerializeField] private Color _shadowColor = new Color(0f, 0f, 0f, 0.35f);
        [SerializeField] private Vector2 _shadowOffset = new Vector2(0f, -6f);
        [SerializeField, Min(0f)] private float _shadowBlur = 12f;
        [SerializeField, Min(0f)] private float _shadowSpread;

        [SerializeField, HideInInspector] private int _version = 1;

        [System.NonSerialized] private Texture2D _ramp;
        [System.NonSerialized] private int _rampVersion = -1;
        [System.NonSerialized] private ulong _rampHash;

        public RoundedShapeType Shape => _shape;
        public Gradient FillGradient => _fillGradient;
        public float FillGradientAngle => _fillGradientAngle;
        public Gradient BorderGradient => _borderGradient;
        public float BorderGradientAngle => _borderGradientAngle;
        public float CornerRadius => _cornerRadius;
        public float BorderThickness => _borderThickness;
        public bool ShadowEnabled => _shadowEnabled;
        public Color ShadowColor => _shadowColor;
        public Vector2 ShadowOffset => _shadowOffset;
        public float ShadowBlur => _shadowBlur;
        public float ShadowSpread => _shadowSpread;
        public int Version => _version;

        private void OnValidate()
        {
            _cornerRadius = Mathf.Max(0f, _cornerRadius);
            _borderThickness = Mathf.Max(0f, _borderThickness);
            _shadowBlur = Mathf.Max(0f, _shadowBlur);
            _shadowSpread = Mathf.Max(0f, _shadowSpread);
            _version++;
            if (_version < 1) _version = 1;
            _rampVersion = -1;
            _rampHash = 0;
        }

        private void OnDisable()
        {
            if (_ramp == null) return;
            if (Application.isPlaying) Destroy(_ramp);
            else DestroyImmediate(_ramp);
            _ramp = null;
            _rampVersion = -1;
            _rampHash = 0;
        }

        public Texture2D GetRampTexture()
        {
            #if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                var hash = ComputeHash();
                if (_ramp != null && _rampHash == hash) return _ramp;
                _rampHash = hash;
                _rampVersion = _version;
                _ramp = CreateRampTexture(_fillGradient, _borderGradient);
                return _ramp;
            }
            #endif

            if (_ramp != null && _rampVersion == _version) return _ramp;
            _rampVersion = _version;
            _ramp = CreateRampTexture(_fillGradient, _borderGradient);
            return _ramp;
        }

        private ulong ComputeHash()
        {
            var hash = 1469598103934665603UL;
            hash = (hash ^ HashGradient(_fillGradient)) * 1099511628211UL;
            hash = (hash ^ HashGradient(_borderGradient)) * 1099511628211UL;
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

        public void MarkDirty()
        {
            // In edit mode the style is validated via OnValidate and the ramp is also hash-checked.
            // Bumping a serialized version here would unnecessarily dirty the asset.
            if (Application.isPlaying)
            {
                _version++;
                if (_version < 1) _version = 1;
            }
            _rampVersion = -1;
            _rampHash = 0;
        }

        private static Texture2D CreateRampTexture(Gradient fill, Gradient border)
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

        private static Gradient DefaultWhiteGradient()
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
            );
            return gradient;
        }
    }
}
