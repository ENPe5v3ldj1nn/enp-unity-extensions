using System;
using UnityEngine;

namespace enp_unity_extensions.Runtime.Scripts.UI.Effects.LiquidEdge
{
    [Serializable]
    public struct LiquidEdgeState
    {
        public Color BaseColorA;
        public Color BaseColorB;
        public Color AccentColor;
        [Range(0f, 2f)] public float GlobalIntensity;
        [Min(0f)] public float Thickness;
        [Range(0f, 1f)] public float Softness;
        [Range(0f, 1f)] public float CenterClear;
        [Min(0f)] public float CornerRoundness;
        [Range(0f, 1f)] public float AmbientMotionIntensity;
        [Range(0f, 1f)] public float SegmentDriftAmplitude;
        public Vector2 SegmentDriftSpeedRange;
        [Min(0f)] public float GlobalMotionSpeed;
        [Range(1, 6)] public int SimultaneousHighlightZoneCount;
        public Vector2 HighlightZoneWidthRange;
        [Min(0f)] public float HighlightZoneDriftSpeed;
        [Range(0f, 2f)] public float HighlightZoneShapeInfluence;
        [Range(0f, 2f)] public float HighlightZoneIntensityInfluence;
        [Range(0f, 2f)] public float HighlightZoneColorInfluence;
        [Range(0f, 2f)] public float HighlightZoneIntensity;
        [Min(0.05f)] public float HighlightZoneLifetime;
        [Min(0f)] public float HighlightZoneFrequency;
        [Range(0f, 1f)] public float HighlightZoneColorShiftAmount;
        [Range(0f, 2f)] public float BottomDominance;
        [Range(0f, 2f)] public float SideSupportAmount;
        [Range(0f, 1f)] public float TopSuppression;
        public LiquidEdgeSegmentStrengths SegmentStrengths;

        public static LiquidEdgeState Calm()
        {
            return new LiquidEdgeState
            {
                BaseColorA = new Color(0.08f, 0.58f, 0.92f, 1f),
                BaseColorB = new Color(0.23f, 0.2f, 0.86f, 1f),
                AccentColor = new Color(0.78f, 0.94f, 1f, 1f),
                GlobalIntensity = 0.58f,
                Thickness = 44f,
                Softness = 0.62f,
                CenterClear = 0.82f,
                CornerRoundness = 38f,
                AmbientMotionIntensity = 0.12f,
                SegmentDriftAmplitude = 0.18f,
                SegmentDriftSpeedRange = new Vector2(0.08f, 0.18f),
                GlobalMotionSpeed = 0.9f,
                SimultaneousHighlightZoneCount = 4,
                HighlightZoneWidthRange = new Vector2(0.18f, 0.32f),
                HighlightZoneDriftSpeed = 0.8f,
                HighlightZoneShapeInfluence = 0.55f,
                HighlightZoneIntensityInfluence = 0.62f,
                HighlightZoneColorInfluence = 0.45f,
                HighlightZoneIntensity = 0.52f,
                HighlightZoneLifetime = 1.9f,
                HighlightZoneFrequency = 0.26f,
                HighlightZoneColorShiftAmount = 0.24f,
                BottomDominance = 0.9f,
                SideSupportAmount = 0.26f,
                TopSuppression = 0.96f,
                SegmentStrengths = LiquidEdgeSegmentStrengths.Create(0.18f, 0.28f, 0.44f)
            };
        }

        public static LiquidEdgeState Living()
        {
            return new LiquidEdgeState
            {
                BaseColorA = new Color(0.2235294f, 0.654902f, 1f, 1f),
                BaseColorB = new Color(0.4117647f, 0.3568628f, 1f, 1f),
                AccentColor = new Color(0.8431373f, 0.9333334f, 1f, 1f),
                GlobalIntensity = 1f,
                Thickness = 220f,
                Softness = 0.3f,
                CenterClear = 0.18f,
                CornerRoundness = 16f,
                AmbientMotionIntensity = 0.22f,
                SegmentDriftAmplitude = 0.42f,
                SegmentDriftSpeedRange = new Vector2(0.45f, 1.1f),
                GlobalMotionSpeed = 1.65f,
                SimultaneousHighlightZoneCount = 6,
                HighlightZoneWidthRange = new Vector2(0.22f, 0.42f),
                HighlightZoneDriftSpeed = 1.25f,
                HighlightZoneShapeInfluence = 1.2f,
                HighlightZoneIntensityInfluence = 1.12f,
                HighlightZoneColorInfluence = 0.9f,
                HighlightZoneIntensity = 1.15f,
                HighlightZoneLifetime = 1.8f,
                HighlightZoneFrequency = 1.6f,
                HighlightZoneColorShiftAmount = 0.12f,
                BottomDominance = 1.35f,
                SideSupportAmount = 0.42f,
                TopSuppression = 0.95f,
                SegmentStrengths = LiquidEdgeSegmentStrengths.Create(0.18f, 0.34f, 0.82f)
            };
        }

        public static LiquidEdgeState Rich()
        {
            return new LiquidEdgeState
            {
                BaseColorA = new Color(0.0f, 0.84f, 0.95f, 1f),
                BaseColorB = new Color(0.64f, 0.18f, 0.86f, 1f),
                AccentColor = new Color(1f, 0.62f, 0.24f, 1f),
                GlobalIntensity = 1.12f,
                Thickness = 76f,
                Softness = 0.5f,
                CenterClear = 0.74f,
                CornerRoundness = 46f,
                AmbientMotionIntensity = 0.3f,
                SegmentDriftAmplitude = 0.52f,
                SegmentDriftSpeedRange = new Vector2(0.16f, 0.48f),
                GlobalMotionSpeed = 1.25f,
                SimultaneousHighlightZoneCount = 6,
                HighlightZoneWidthRange = new Vector2(0.18f, 0.34f),
                HighlightZoneDriftSpeed = 1.05f,
                HighlightZoneShapeInfluence = 1.2f,
                HighlightZoneIntensityInfluence = 1.24f,
                HighlightZoneColorInfluence = 1.2f,
                HighlightZoneIntensity = 1.36f,
                HighlightZoneLifetime = 1.12f,
                HighlightZoneFrequency = 0.88f,
                HighlightZoneColorShiftAmount = 0.56f,
                BottomDominance = 1.34f,
                SideSupportAmount = 0.64f,
                TopSuppression = 0.92f,
                SegmentStrengths = LiquidEdgeSegmentStrengths.Create(0.32f, 0.52f, 0.86f)
            };
        }

        public static LiquidEdgeState Overdriven()
        {
            return new LiquidEdgeState
            {
                BaseColorA = new Color(0f, 0.92f, 1f, 1f),
                BaseColorB = new Color(0.85f, 0.18f, 0.72f, 1f),
                AccentColor = new Color(1f, 0.86f, 0.2f, 1f),
                GlobalIntensity = 1.42f,
                Thickness = 92f,
                Softness = 0.42f,
                CenterClear = 0.7f,
                CornerRoundness = 48f,
                AmbientMotionIntensity = 0.38f,
                SegmentDriftAmplitude = 0.72f,
                SegmentDriftSpeedRange = new Vector2(0.2f, 0.62f),
                GlobalMotionSpeed = 1.55f,
                SimultaneousHighlightZoneCount = 6,
                HighlightZoneWidthRange = new Vector2(0.16f, 0.3f),
                HighlightZoneDriftSpeed = 1.35f,
                HighlightZoneShapeInfluence = 1.35f,
                HighlightZoneIntensityInfluence = 1.4f,
                HighlightZoneColorInfluence = 1.45f,
                HighlightZoneIntensity = 1.72f,
                HighlightZoneLifetime = 0.95f,
                HighlightZoneFrequency = 1.2f,
                HighlightZoneColorShiftAmount = 0.72f,
                BottomDominance = 1.52f,
                SideSupportAmount = 0.76f,
                TopSuppression = 0.9f,
                SegmentStrengths = LiquidEdgeSegmentStrengths.Create(0.38f, 0.66f, 1f)
            };
        }

        public static LiquidEdgeState Sanitize(LiquidEdgeState state)
        {
            state.GlobalIntensity = Mathf.Clamp(state.GlobalIntensity, 0f, 2f);
            state.Thickness = Mathf.Max(0f, state.Thickness);
            state.Softness = Mathf.Clamp01(state.Softness);
            state.CenterClear = Mathf.Clamp01(state.CenterClear);
            state.CornerRoundness = Mathf.Max(0f, state.CornerRoundness);
            state.AmbientMotionIntensity = Mathf.Clamp01(state.AmbientMotionIntensity);
            state.SegmentDriftAmplitude = Mathf.Clamp01(state.SegmentDriftAmplitude);
            state.SegmentDriftSpeedRange.x = Mathf.Max(0.01f, state.SegmentDriftSpeedRange.x);
            state.SegmentDriftSpeedRange.y = Mathf.Max(state.SegmentDriftSpeedRange.x, state.SegmentDriftSpeedRange.y);
            state.GlobalMotionSpeed = state.GlobalMotionSpeed <= 0f ? 1f : Mathf.Max(0f, state.GlobalMotionSpeed);
            state.SimultaneousHighlightZoneCount = Mathf.Clamp(state.SimultaneousHighlightZoneCount <= 0 ? 5 : state.SimultaneousHighlightZoneCount, 1, 6);
            if (state.HighlightZoneWidthRange == default)
            {
                state.HighlightZoneWidthRange = new Vector2(0.18f, 0.36f);
            }

            state.HighlightZoneWidthRange.x = Mathf.Clamp(state.HighlightZoneWidthRange.x, 0.04f, 1f);
            state.HighlightZoneWidthRange.y = Mathf.Clamp(Mathf.Max(state.HighlightZoneWidthRange.x, state.HighlightZoneWidthRange.y), 0.04f, 1f);
            state.HighlightZoneDriftSpeed = state.HighlightZoneDriftSpeed <= 0f ? 1f : Mathf.Max(0f, state.HighlightZoneDriftSpeed);
            state.HighlightZoneShapeInfluence = state.HighlightZoneShapeInfluence <= 0f ? 1f : Mathf.Clamp(state.HighlightZoneShapeInfluence, 0f, 2f);
            state.HighlightZoneIntensityInfluence = state.HighlightZoneIntensityInfluence <= 0f ? 1f : Mathf.Clamp(state.HighlightZoneIntensityInfluence, 0f, 2f);
            state.HighlightZoneColorInfluence = state.HighlightZoneColorInfluence <= 0f ? 1f : Mathf.Clamp(state.HighlightZoneColorInfluence, 0f, 2f);
            state.HighlightZoneIntensity = Mathf.Clamp(state.HighlightZoneIntensity, 0f, 2f);
            state.HighlightZoneLifetime = Mathf.Max(0.05f, state.HighlightZoneLifetime);
            state.HighlightZoneFrequency = Mathf.Max(0f, state.HighlightZoneFrequency);
            state.HighlightZoneColorShiftAmount = Mathf.Clamp01(state.HighlightZoneColorShiftAmount);
            state.BottomDominance = Mathf.Clamp(state.BottomDominance, 0f, 2f);
            state.SideSupportAmount = Mathf.Clamp(state.SideSupportAmount, 0f, 2f);
            state.TopSuppression = Mathf.Clamp01(state.TopSuppression);
            state.SegmentStrengths = LiquidEdgeSegmentStrengths.Sanitize(state.SegmentStrengths);
            return state;
        }
    }
}
