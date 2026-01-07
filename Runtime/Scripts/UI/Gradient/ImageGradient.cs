using UnityEngine;
using UnityEngine.UI;

namespace Enp.UI.MyGradient
{
    [AddComponentMenu("UI/Effects/Image Gradient")]
    [RequireComponent(typeof(Graphic))]
    public class ImageGradient : BaseMeshEffect
    {
        [SerializeField] private Gradient gradient = new Gradient();
        [SerializeField, Range(-180f, 180f)] private float angle = 0f;
        [SerializeField] private Vector2 offset = Vector2.zero;
        [SerializeField] private bool ignoreRatio = true;
        [SerializeField, Range(1, 64)] private int segments = 16;

        public Gradient Gradient { get => gradient; set { gradient = value; SetDirty(); } }
        public float Angle { get => angle; set { angle = value; SetDirty(); } }
        public Vector2 Offset { get => offset; set { offset = value; SetDirty(); } }
        public bool IgnoreRatio { get => ignoreRatio; set { ignoreRatio = value; SetDirty(); } }
        public int Segments { get => segments; set { segments = Mathf.Clamp(value, 1, 64); SetDirty(); } }

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!isActiveAndEnabled || graphic == null) return;

            int count = vh.currentVertCount;
            if (count == 0) return;

            var img = graphic as Image;
            bool canRebuildGrid = img != null && img.sprite == null;

            if (!canRebuildGrid)
            {
                ApplyPerVertex(vh);
                return;
            }

            RebuildGrid(vh);
        }

        private void RebuildGrid(VertexHelper vh)
        {
            Rect rect = graphic.rectTransform.rect;

            Vector2 dir = GetRotationDir(angle);
            if (!ignoreRatio)
            {
                float ratio = rect.height / rect.width;
                dir = new Vector2(dir.x * ratio, dir.y).normalized;
            }

            float width = rect.width;
            float height = rect.height;

            float absCos = Mathf.Abs(dir.x);
            float absSin = Mathf.Abs(dir.y);
            float denom = width * absCos + height * absSin;
            if (denom < 1e-6f) denom = 1f;
            float invDenom = 1f / denom;

            Vector2 center = rect.center + offset;

            int s = Mathf.Clamp(segments, 1, 64);
            int vertPerLine = s + 1;

            vh.Clear();

            Color baseColor = graphic.color;
            float xMin = rect.xMin;
            float yMin = rect.yMin;

            UIVertex v = UIVertex.simpleVert;

            for (int y = 0; y <= s; y++)
            {
                float fy = (float)y / s;
                float py = Mathf.Lerp(yMin, yMin + height, fy);

                for (int x = 0; x <= s; x++)
                {
                    float fx = (float)x / s;
                    float px = Mathf.Lerp(xMin, xMin + width, fx);

                    float dx = px - center.x;
                    float dy = py - center.y;

                    float t = (dx * dir.x + dy * dir.y) * invDenom + 0.5f;
                    Color gc = gradient.Evaluate(Mathf.Clamp01(t));

                    v.position = new Vector3(px, py, 0f);
                    v.uv0 = new Vector2(fx, fy);
                    v.color = (Color32)(baseColor * gc);

                    vh.AddVert(v);
                }
            }

            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    int i0 = y * vertPerLine + x;
                    int i1 = i0 + 1;
                    int i2 = i0 + vertPerLine;
                    int i3 = i2 + 1;

                    vh.AddTriangle(i0, i2, i1);
                    vh.AddTriangle(i1, i2, i3);
                }
            }
        }

        private void ApplyPerVertex(VertexHelper vh)
        {
            int count = vh.currentVertCount;
            if (count == 0) return;

            Rect rect = graphic.rectTransform.rect;
            Vector2 dir = GetRotationDir(angle);

            if (!ignoreRatio)
            {
                float ratio = rect.height / rect.width;
                dir = new Vector2(dir.x * ratio, dir.y).normalized;
            }

            float width = rect.width;
            float height = rect.height;

            float absCos = Mathf.Abs(dir.x);
            float absSin = Mathf.Abs(dir.y);
            float denom = width * absCos + height * absSin;
            if (denom < 1e-6f) denom = 1f;
            float invDenom = 1f / denom;

            Vector2 center = rect.center + offset;

            UIVertex vertex = default;
            for (int i = 0; i < count; i++)
            {
                vh.PopulateUIVertex(ref vertex, i);

                float dx = vertex.position.x - center.x;
                float dy = vertex.position.y - center.y;

                float t = (dx * dir.x + dy * dir.y) * invDenom + 0.5f;

                vertex.color *= gradient.Evaluate(Mathf.Clamp01(t));
                vh.SetUIVertex(vertex, i);
            }
        }

        private static Vector2 GetRotationDir(float angle)
        {
            float rad = angle * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        }

        private void SetDirty()
        {
            if (graphic != null) graphic.SetVerticesDirty();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            segments = Mathf.Clamp(segments, 1, 64);
            SetDirty();
        }
#endif
    }
}
