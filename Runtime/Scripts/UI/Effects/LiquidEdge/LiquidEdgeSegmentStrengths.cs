using System;
using UnityEngine;

namespace enp_unity_extensions.Runtime.Scripts.UI.Effects.LiquidEdge
{
    [Serializable]
    public struct LiquidEdgeSegmentStrengths
    {
        [Range(0f, 2f)] public float LeftTop;
        [Range(0f, 2f)] public float LeftMid;
        [Range(0f, 2f)] public float LeftBottom;
        [Range(0f, 2f)] public float RightTop;
        [Range(0f, 2f)] public float RightMid;
        [Range(0f, 2f)] public float RightBottom;
        [Range(0f, 2f)] public float BottomFarLeft;
        [Range(0f, 2f)] public float BottomLeft;
        [Range(0f, 2f)] public float BottomCenter;
        [Range(0f, 2f)] public float BottomRight;
        [Range(0f, 2f)] public float BottomFarRight;

        public float[] ToArray()
        {
            return new[]
            {
                LeftTop, LeftMid, LeftBottom,
                RightTop, RightMid, RightBottom,
                BottomFarLeft, BottomLeft, BottomCenter, BottomRight, BottomFarRight
            };
        }

        public static LiquidEdgeSegmentStrengths Create(float sideTop, float sideMid, float bottom)
        {
            return new LiquidEdgeSegmentStrengths
            {
                LeftTop = sideTop,
                LeftMid = sideMid,
                LeftBottom = sideMid * 1.15f,
                RightTop = sideTop,
                RightMid = sideMid,
                RightBottom = sideMid * 1.15f,
                BottomFarLeft = bottom * 0.78f,
                BottomLeft = bottom * 0.96f,
                BottomCenter = bottom * 1.12f,
                BottomRight = bottom,
                BottomFarRight = bottom * 0.82f
            };
        }

        public static LiquidEdgeSegmentStrengths Sanitize(LiquidEdgeSegmentStrengths strengths)
        {
            strengths.LeftTop = Mathf.Clamp(strengths.LeftTop, 0f, 2f);
            strengths.LeftMid = Mathf.Clamp(strengths.LeftMid, 0f, 2f);
            strengths.LeftBottom = Mathf.Clamp(strengths.LeftBottom, 0f, 2f);
            strengths.RightTop = Mathf.Clamp(strengths.RightTop, 0f, 2f);
            strengths.RightMid = Mathf.Clamp(strengths.RightMid, 0f, 2f);
            strengths.RightBottom = Mathf.Clamp(strengths.RightBottom, 0f, 2f);
            strengths.BottomFarLeft = Mathf.Clamp(strengths.BottomFarLeft, 0f, 2f);
            strengths.BottomLeft = Mathf.Clamp(strengths.BottomLeft, 0f, 2f);
            strengths.BottomCenter = Mathf.Clamp(strengths.BottomCenter, 0f, 2f);
            strengths.BottomRight = Mathf.Clamp(strengths.BottomRight, 0f, 2f);
            strengths.BottomFarRight = Mathf.Clamp(strengths.BottomFarRight, 0f, 2f);
            return strengths;
        }
    }
}
