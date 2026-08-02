using UnityEngine;

namespace ENP.UnityExtensions.Runtime
{
    [ExecuteAlways]
    [RequireComponent(typeof(SpriteRenderer))]
    [AddComponentMenu("2D/Effects/Sprite Gradient")]
    public sealed class SpriteGradient : MonoBehaviour
    {
        [SerializeField] private Gradient _gradient = DefaultWhiteGradient();
        [SerializeField, Range(-180f, 180f)] private float _angle;
        [SerializeField] private Vector2 _offset = Vector2.zero;
        [SerializeField] private bool _ignoreRatio = true;

        private static readonly int GRADIENT_TEX_ID = Shader.PropertyToID("_GradientTex");
        private static readonly int GRADIENT_ANGLE_ID = Shader.PropertyToID("_GradientAngle");
        private static readonly int GRADIENT_OFFSET_ID = Shader.PropertyToID("_GradientOffset");
        private static readonly int GRADIENT_IGNORE_RATIO_ID = Shader.PropertyToID("_GradientIgnoreRatio");
        private static readonly int SPRITE_BOUNDS_SIZE_ID = Shader.PropertyToID("_SpriteBoundsSize");

        private static Material _sharedMaterial;

        private SpriteRenderer _spriteRenderer;
        private MaterialPropertyBlock _propertyBlock;
        private Texture2D _rampTexture;
        private ulong _rampHash;

        public Gradient Gradient { get => _gradient; set { _gradient = EnsureGradient(value); MarkDirty(); } }
        public float Angle { get => _angle; set { _angle = value; MarkDirty(); } }
        public Vector2 Offset { get => _offset; set { _offset = value; MarkDirty(); } }
        public bool IgnoreRatio { get => _ignoreRatio; set { _ignoreRatio = value; MarkDirty(); } }

        private void OnEnable()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            EnsureGradient();
            EnsureMaterial();
            Apply();
        }

        private void OnDisable()
        {
            DestroyRampTexture();
            if (_spriteRenderer != null) _spriteRenderer.SetPropertyBlock(null);
        }

        private void OnDestroy()
        {
            DestroyRampTexture();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_spriteRenderer == null) _spriteRenderer = GetComponent<SpriteRenderer>();
            EnsureGradient();
            EnsureMaterial();
            Apply();
        }
#endif

        private void MarkDirty()
        {
            if (!isActiveAndEnabled) return;
            Apply();
        }

        private void EnsureMaterial()
        {
            if (_sharedMaterial == null)
            {
                var shader = Shader.Find("Sprite2D/SpriteGradient");
                if (shader == null)
                {
                    Debug.LogError("Shader 'Sprite2D/SpriteGradient' not found.");
                    return;
                }
                _sharedMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            }
            if (_spriteRenderer != null && _spriteRenderer.sharedMaterial != _sharedMaterial)
                _spriteRenderer.sharedMaterial = _sharedMaterial;
        }

        private void Apply()
        {
            if (_spriteRenderer == null) return;

            var bounds = _spriteRenderer.sprite != null ? _spriteRenderer.sprite.bounds.size : Vector3.one;

            _propertyBlock ??= new MaterialPropertyBlock();
            _spriteRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetTexture(GRADIENT_TEX_ID, GetRampTexture());
            _propertyBlock.SetFloat(GRADIENT_ANGLE_ID, _angle);
            _propertyBlock.SetVector(GRADIENT_OFFSET_ID, _offset);
            _propertyBlock.SetFloat(GRADIENT_IGNORE_RATIO_ID, _ignoreRatio ? 1f : 0f);
            _propertyBlock.SetVector(SPRITE_BOUNDS_SIZE_ID, new Vector4(bounds.x, bounds.y, 0f, 0f));
            _spriteRenderer.SetPropertyBlock(_propertyBlock);
        }

        private Texture2D GetRampTexture()
        {
            var hash = HashGradient(_gradient);
            if (_rampTexture != null && _rampHash == hash) return _rampTexture;
            DestroyRampTexture();
            _rampTexture = CreateRampTexture(_gradient);
            _rampHash = hash;
            return _rampTexture;
        }

        private void DestroyRampTexture()
        {
            if (_rampTexture == null) return;
            if (Application.isPlaying) Destroy(_rampTexture);
            else DestroyImmediate(_rampTexture);
            _rampTexture = null;
        }

        private void EnsureGradient()
        {
            if (_gradient == null) _gradient = DefaultWhiteGradient();
        }

        private static Gradient EnsureGradient(Gradient gradient) => gradient ?? DefaultWhiteGradient();

        private static Gradient DefaultWhiteGradient()
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
            );
            return gradient;
        }

        private static Texture2D CreateRampTexture(Gradient gradient)
        {
            const int width = 256;
            var texture = new Texture2D(width, 1, TextureFormat.RGBA32, false, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };

            var pixels = new Color32[width];
            for (int x = 0; x < width; x++)
            {
                var t = x / (float)(width - 1);
                pixels[x] = gradient != null ? (Color32)gradient.Evaluate(t) : (Color32)Color.white;
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
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
                    hash = (hash ^ Quant01(colors[i].time)) * 1099511628211UL;
                }
                hash = (hash ^ (ulong)alphas.Length) * 1099511628211UL;
                for (int i = 0; i < alphas.Length; i++)
                {
                    hash = (hash ^ Quant01(alphas[i].alpha)) * 1099511628211UL;
                    hash = (hash ^ Quant01(alphas[i].time)) * 1099511628211UL;
                }
                hash = (hash ^ (ulong)gradient.mode.GetHashCode()) * 1099511628211UL;
                return hash;
            }
        }

        private static ulong Quant01(float value) => (ulong)Mathf.Clamp(Mathf.RoundToInt(value * 65535f), 0, 65535);
    }
}
