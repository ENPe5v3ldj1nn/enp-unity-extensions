using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ENP.UnityExtensions.Runtime
{
    [ExecuteAlways]
    [AddComponentMenu("UI/Rounded Slider Fill Graphic")]
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RoundedSliderFillGraphic : MaskableGraphic
    {
        [SerializeField, Range(0f, 1f)] private float _fillAmount = 1f;
        [SerializeField] private Slider.Direction _direction = Slider.Direction.LeftToRight;
        [SerializeField, Range(0f, 1f)] private float _roundness = 1f;
        [SerializeField, HideInInspector, Min(3)] private int _cornerSegments = 24;
        [SerializeField, HideInInspector] private bool _roundFullCaps = true;

        private static readonly List<Vector2> s_Points = new(64);

        public float fillAmount
        {
            get => _fillAmount;
            set
            {
                var clamped = Mathf.Clamp01(value);
                if (Mathf.Approximately(_fillAmount, clamped))
                    return;

                _fillAmount = clamped;
                SetVerticesDirty();
            }
        }

        public Slider.Direction direction
        {
            get => _direction;
            set
            {
                if (_direction == value)
                    return;

                _direction = value;
                SetVerticesDirty();
            }
        }

        public float roundness
        {
            get => _roundness;
            set
            {
                var clamped = Mathf.Clamp01(value);
                if (Mathf.Approximately(_roundness, clamped))
                    return;

                _roundness = clamped;
                SetVerticesDirty();
            }
        }

        public int cornerSegments
        {
            get => _cornerSegments;
            set
            {
                var clamped = Mathf.Max(3, value);
                if (_cornerSegments == clamped)
                    return;

                _cornerSegments = clamped;
                SetVerticesDirty();
            }
        }

        public bool roundFullCaps
        {
            get => _roundFullCaps;
            set
            {
                if (_roundFullCaps == value)
                    return;

                _roundFullCaps = value;
                SetVerticesDirty();
            }
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            _fillAmount = Mathf.Clamp01(_fillAmount);
            _roundness = Mathf.Clamp01(_roundness);
            _cornerSegments = Mathf.Max(3, _cornerSegments);
        }

        public override Texture mainTexture => s_WhiteTexture;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            var rect = rectTransform.rect;
            var width = rect.width;
            var height = rect.height;
            if (width <= 0f || height <= 0f || _fillAmount <= 0f)
                return;

            switch (_direction)
            {
                case Slider.Direction.LeftToRight:
                    BuildHorizontal(vh, rect.xMin, rect.center.y, width, width * _fillAmount, height, leftToRight: true);
                    break;
                case Slider.Direction.RightToLeft:
                    BuildHorizontal(vh, rect.xMin, rect.center.y, width, width * _fillAmount, height, leftToRight: false);
                    break;
                case Slider.Direction.BottomToTop:
                    BuildVertical(vh, rect.center.x, rect.yMin, height, height * _fillAmount, width, bottomToTop: true);
                    break;
                case Slider.Direction.TopToBottom:
                    BuildVertical(vh, rect.center.x, rect.yMin, height, height * _fillAmount, width, bottomToTop: false);
                    break;
            }
        }

        private void BuildHorizontal(VertexHelper vh, float originX, float originY, float totalWidth, float fillWidth, float height, bool leftToRight)
        {
            if (fillWidth <= 0f)
                return;

            BuildLeadingRoundedShape(vh, originX, originY, totalWidth, fillWidth, height, roundedLeft: leftToRight, roundBothEnds: _roundFullCaps && fillWidth >= rectTransform.rect.width - 0.001f);
        }

        private void BuildVertical(VertexHelper vh, float originX, float originY, float totalHeight, float fillHeight, float width, bool bottomToTop)
        {
            if (fillHeight <= 0f)
                return;

            BuildLeadingRoundedShapeVertical(vh, originX, originY, totalHeight, width, fillHeight, roundedBottom: bottomToTop, roundBothEnds: _roundFullCaps && fillHeight >= rectTransform.rect.height - 0.001f);
        }

        private void BuildLeadingRoundedShape(VertexHelper vh, float originX, float originY, float totalWidth, float fillWidth, float height, bool roundedLeft, bool roundBothEnds)
        {
            var halfHeight = height * 0.5f;

            s_Points.Clear();

            if (fillWidth <= height)
            {
                var diameter = Mathf.Min(height, totalWidth);
                var radius = diameter * 0.5f;
                var center = new Vector2(roundedLeft ? originX + radius : originX + totalWidth - radius, originY);
                AddCircle(s_Points, center, radius, _cornerSegments);
                EmitFan(vh, s_Points, center, color);
                return;
            }
            else if (_roundFullCaps || roundBothEnds)
            {
                var radius = ResolveRadius(fillWidth, height);
                AddRoundedRectHorizontal(s_Points, originX, originY, fillWidth, height, radius, _cornerSegments);
            }
            else if (roundedLeft)
            {
                var radius = ResolveRadius(fillWidth, height);
                AddLeadingRoundRightFlat(s_Points, originX, originY, fillWidth, halfHeight, radius, _cornerSegments);
            }
            else
            {
                var radius = ResolveRadius(fillWidth, height);
                AddLeadingRoundLeftFlat(s_Points, originX, originY, fillWidth, halfHeight, radius, _cornerSegments);
            }

            EmitFan(vh, s_Points, new Vector2(originX + fillWidth * 0.5f, originY), color);
        }

        private void BuildLeadingRoundedShapeVertical(VertexHelper vh, float originX, float originY, float totalHeight, float width, float fillHeight, bool roundedBottom, bool roundBothEnds)
        {
            var halfWidth = width * 0.5f;

            s_Points.Clear();

            if (fillHeight <= width)
            {
                var diameter = Mathf.Min(width, totalHeight);
                var radius = diameter * 0.5f;
                var center = new Vector2(originX, roundedBottom ? originY + radius : originY + totalHeight - radius);
                AddCircle(s_Points, center, radius, _cornerSegments);
                EmitFan(vh, s_Points, center, color);
                return;
            }
            else if (_roundFullCaps || roundBothEnds)
            {
                var radius = ResolveRadius(fillHeight, width);
                AddRoundedRectVertical(s_Points, originX, originY, width, fillHeight, radius, _cornerSegments);
            }
            else if (roundedBottom)
            {
                var radius = ResolveRadius(fillHeight, width);
                AddLeadingRoundTopFlat(s_Points, originX, originY, fillHeight, halfWidth, radius, _cornerSegments);
            }
            else
            {
                var radius = ResolveRadius(fillHeight, width);
                AddLeadingRoundBottomFlat(s_Points, originX, originY, fillHeight, halfWidth, radius, _cornerSegments);
            }

            EmitFan(vh, s_Points, new Vector2(originX, originY + fillHeight * 0.5f), color);
        }

        private float ResolveRadius(float fillLength, float thickness)
        {
            var baseRadius = thickness * 0.5f * _roundness;
            return Mathf.Max(0f, Mathf.Min(baseRadius, thickness * 0.5f, fillLength * 0.5f));
        }

        private static void AddRoundedRectHorizontal(List<Vector2> points, float originX, float originY, float width, float height, float radius, int segments)
        {
            var halfHeight = height * 0.5f;
            var left = originX + radius;
            var right = originX + width - radius;
            var bottom = originY - halfHeight;
            var top = originY + halfHeight;

            points.Add(new Vector2(left, bottom));
            AppendArc(points, new Vector2(left, originY), radius, 270f, 90f, segments);
            points.Add(new Vector2(left, top));
            points.Add(new Vector2(right, top));
            AppendArc(points, new Vector2(right, originY), radius, 90f, -90f, segments);
            points.Add(new Vector2(right, bottom));
        }

        private static void AddRoundedRectVertical(List<Vector2> points, float originX, float originY, float width, float height, float radius, int segments)
        {
            var halfWidth = width * 0.5f;
            var bottom = originY + radius;
            var top = originY + height - radius;
            var left = originX - halfWidth;
            var right = originX + halfWidth;

            points.Add(new Vector2(left, bottom));
            AppendArc(points, new Vector2(originX, bottom), radius, 180f, 0f, segments);
            points.Add(new Vector2(right, bottom));
            points.Add(new Vector2(right, top));
            AppendArc(points, new Vector2(originX, top), radius, 0f, 180f, segments);
            points.Add(new Vector2(left, top));
        }

        private static void AddCircle(List<Vector2> points, Vector2 center, float radius, int segments)
        {
            segments = Mathf.Max(8, segments);
            for (var i = 0; i < segments; i++)
            {
                var angle = (i / (float)segments) * Mathf.PI * 2f;
                points.Add(new Vector2(center.x + Mathf.Cos(angle) * radius, center.y + Mathf.Sin(angle) * radius));
            }
        }

        private static void AddLeadingRoundRightFlat(List<Vector2> points, float originX, float originY, float width, float halfHeight, float radius, int segments)
        {
            var left = originX + radius;
            var right = originX + width;
            var bottom = originY - halfHeight;
            var top = originY + halfHeight;

            points.Add(new Vector2(right, bottom));
            points.Add(new Vector2(right, top));
            points.Add(new Vector2(left, top));
            AppendArc(points, new Vector2(left, originY), radius, 90f, 270f, segments);
            points.Add(new Vector2(left, bottom));
        }

        private static void AddLeadingRoundLeftFlat(List<Vector2> points, float originX, float originY, float width, float halfHeight, float radius, int segments)
        {
            var left = originX;
            var right = originX + width - radius;
            var bottom = originY - halfHeight;
            var top = originY + halfHeight;

            points.Add(new Vector2(left, bottom));
            points.Add(new Vector2(left, top));
            points.Add(new Vector2(right, top));
            AppendArc(points, new Vector2(right, originY), radius, 90f, -90f, segments);
            points.Add(new Vector2(right, bottom));
        }

        private static void AddLeadingRoundTopFlat(List<Vector2> points, float originX, float originY, float fillHeight, float halfWidth, float radius, int segments)
        {
            var top = originY + fillHeight;
            var left = originX - halfWidth;
            var right = originX + halfWidth;

            points.Add(new Vector2(left, top));
            points.Add(new Vector2(right, top));
            points.Add(new Vector2(right, originY + radius));
            AppendArc(points, new Vector2(originX, originY + radius), radius, 0f, 180f, segments);
            points.Add(new Vector2(left, originY + radius));
        }

        private static void AddLeadingRoundBottomFlat(List<Vector2> points, float originX, float originY, float fillHeight, float halfWidth, float radius, int segments)
        {
            var bottom = originY;
            var top = originY + fillHeight - radius;
            var left = originX - halfWidth;
            var right = originX + halfWidth;

            points.Add(new Vector2(left, bottom));
            points.Add(new Vector2(right, bottom));
            points.Add(new Vector2(right, top));
            AppendArc(points, new Vector2(originX, top), radius, 0f, -180f, segments);
            points.Add(new Vector2(left, top));
        }

        private static void AppendArc(List<Vector2> points, Vector2 center, float radius, float startDegrees, float endDegrees, int segments)
        {
            segments = Mathf.Max(3, segments);
            var step = (endDegrees - startDegrees) / segments;

            for (var i = 0; i <= segments; i++)
            {
                var angle = (startDegrees + step * i) * Mathf.Deg2Rad;
                points.Add(new Vector2(center.x + Mathf.Cos(angle) * radius, center.y + Mathf.Sin(angle) * radius));
            }
        }

        private static void EmitFan(VertexHelper vh, List<Vector2> points, Vector2 center, Color32 vertexColor)
        {
            if (points.Count < 3)
                return;

            var startIndex = vh.currentVertCount;
            vh.AddVert(center, vertexColor, Vector2.zero);

            for (var i = 0; i < points.Count; i++)
                vh.AddVert(points[i], vertexColor, Vector2.zero);

            for (var i = 1; i < points.Count; i++)
                vh.AddTriangle(startIndex, startIndex + i, startIndex + i + 1);

            vh.AddTriangle(startIndex, startIndex + points.Count, startIndex + 1);
        }
    }
}
