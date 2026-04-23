using System;
using UnityEngine;

namespace enp_unity_extensions.Runtime.Scripts.UI.Effects.LiquidEdge
{
    [Serializable]
    public struct PatchLiquidEdgeState
    {
        public Color BaseColorA;
        public Color BaseColorB;
        public Color AccentColor;
        [Range(0f, 2f)] public float GlobalIntensity;
        [Range(0f, 1f)] public float CenterClear;
        [Min(0f)] public float CornerRoundness;
        [Range(0f, 1f)] public float AmbientBaseLayerIntensity;
        [Min(0f)] public float GlobalMotionSpeed;
        [Range(0f, 2f)] public float InnerDepth;
        public Vector2 PatchWidthRange;
        [Range(0, 4)] public int BottomPatchCount;
        [Range(0, 2)] public int SidePatchCountPerSide;
        [Range(0, 1)] public int TopPatchCount;
        public Vector2 PatchDriftSpeedRange;
        [Range(0f, 2f)] public float PatchAmplitude;
        [Range(0f, 2f)] public float PatchShapeInfluence;
        [Range(0f, 2f)] public float PatchColorInfluence;
        [Range(0f, 2f)] public float BottomDominance;
        [Range(0f, 2f)] public float SideSupport;
        [Range(0f, 1f)] public float TopSuppression;
        [Range(0f, 1f)] public float BottomToSideBleed;
        [Range(0f, 1f)] public float CornerBleedAttenuation;
        [Range(0f, 2f)] public float BottomThicknessMultiplier;
        [Range(0f, 2f)] public float SideThicknessMultiplier;

        public static PatchLiquidEdgeState Calm()
        {
            return new PatchLiquidEdgeState
            {
                BaseColorA = Hex(0x39, 0xA7, 0xFF),
                BaseColorB = Hex(0x69, 0x5B, 0xFF),
                AccentColor = Hex(0xD7, 0xEE, 0xFF),
                GlobalIntensity = 0.72f,
                CenterClear = 0.64f,
                CornerRoundness = 18f,
                AmbientBaseLayerIntensity = 0.08f,
                GlobalMotionSpeed = 0.75f,
                InnerDepth = 0.34f,
                PatchWidthRange = new Vector2(0.2f, 0.38f),
                BottomPatchCount = 2,
                SidePatchCountPerSide = 1,
                TopPatchCount = 0,
                PatchDriftSpeedRange = new Vector2(0.18f, 0.38f),
                PatchAmplitude = 0.72f,
                PatchShapeInfluence = 0.72f,
                PatchColorInfluence = 0.52f,
                BottomDominance = 1.05f,
                SideSupport = 0.28f,
                TopSuppression = 0.98f,
                BottomToSideBleed = 0.2f,
                CornerBleedAttenuation = 0.62f,
                BottomThicknessMultiplier = 0.92f,
                SideThicknessMultiplier = 0.52f
            };
        }

        public static PatchLiquidEdgeState Living()
        {
            return new PatchLiquidEdgeState
            {
                BaseColorA = Hex(0x39, 0xA7, 0xFF),
                BaseColorB = Hex(0x69, 0x5B, 0xFF),
                AccentColor = Hex(0xD7, 0xEE, 0xFF),
                GlobalIntensity = 0.86f,
                CenterClear = 0.58f,
                CornerRoundness = 16f,
                AmbientBaseLayerIntensity = 0.12f,
                GlobalMotionSpeed = 1.25f,
                InnerDepth = 0.36f,
                PatchWidthRange = new Vector2(0.14f, 0.28f),
                BottomPatchCount = 4,
                SidePatchCountPerSide = 1,
                TopPatchCount = 0,
                PatchDriftSpeedRange = new Vector2(0.34f, 0.72f),
                PatchAmplitude = 0.96f,
                PatchShapeInfluence = 0.8f,
                PatchColorInfluence = 0.88f,
                BottomDominance = 1.14f,
                SideSupport = 0.22f,
                TopSuppression = 0.95f,
                BottomToSideBleed = 0.14f,
                CornerBleedAttenuation = 0.78f,
                BottomThicknessMultiplier = 0.84f,
                SideThicknessMultiplier = 0.46f
            };
        }

        public static PatchLiquidEdgeState Rich()
        {
            PatchLiquidEdgeState state = Living();
            state.GlobalIntensity = 1.12f;
            state.AmbientBaseLayerIntensity = 0.16f;
            state.GlobalMotionSpeed = 1.45f;
            state.InnerDepth = 0.48f;
            state.PatchWidthRange = new Vector2(0.24f, 0.52f);
            state.SidePatchCountPerSide = 2;
            state.PatchDriftSpeedRange = new Vector2(0.38f, 0.86f);
            state.PatchAmplitude = 1.22f;
            state.PatchShapeInfluence = 1.22f;
            state.PatchColorInfluence = 1f;
            state.BottomDominance = 1.28f;
            state.SideSupport = 0.32f;
            state.TopSuppression = 0.96f;
            state.BottomToSideBleed = 0.18f;
            state.CornerBleedAttenuation = 0.72f;
            state.BottomThicknessMultiplier = 0.92f;
            state.SideThicknessMultiplier = 0.5f;
            return state;
        }

        public static PatchLiquidEdgeState Overdriven()
        {
            PatchLiquidEdgeState state = Rich();
            state.GlobalIntensity = 1.32f;
            state.AmbientBaseLayerIntensity = 0.2f;
            state.GlobalMotionSpeed = 1.8f;
            state.InnerDepth = 0.54f;
            state.PatchWidthRange = new Vector2(0.2f, 0.48f);
            state.TopPatchCount = 1;
            state.PatchDriftSpeedRange = new Vector2(0.5f, 1.08f);
            state.PatchAmplitude = 1.45f;
            state.PatchShapeInfluence = 1.38f;
            state.PatchColorInfluence = 1.18f;
            state.BottomDominance = 1.42f;
            state.SideSupport = 0.4f;
            state.TopSuppression = 0.92f;
            state.BottomToSideBleed = 0.24f;
            state.CornerBleedAttenuation = 0.64f;
            state.BottomThicknessMultiplier = 1f;
            state.SideThicknessMultiplier = 0.56f;
            return state;
        }

        public static PatchLiquidEdgeState Sanitize(PatchLiquidEdgeState state)
        {
            state.GlobalIntensity = Mathf.Clamp(state.GlobalIntensity, 0f, 2f);
            state.CenterClear = Mathf.Clamp01(state.CenterClear);
            state.CornerRoundness = Mathf.Max(0f, state.CornerRoundness);
            state.AmbientBaseLayerIntensity = Mathf.Clamp01(state.AmbientBaseLayerIntensity);
            state.GlobalMotionSpeed = state.GlobalMotionSpeed <= 0f ? 1f : Mathf.Max(0f, state.GlobalMotionSpeed);
            state.InnerDepth = Mathf.Clamp(state.InnerDepth, 0.04f, 2f);
            if (state.PatchWidthRange == default)
            {
                state.PatchWidthRange = new Vector2(0.22f, 0.46f);
            }

            state.PatchWidthRange.x = Mathf.Clamp(state.PatchWidthRange.x, 0.04f, 1.5f);
            state.PatchWidthRange.y = Mathf.Clamp(Mathf.Max(state.PatchWidthRange.x, state.PatchWidthRange.y), 0.04f, 1.5f);
            state.BottomPatchCount = Mathf.Clamp(state.BottomPatchCount <= 0 ? 3 : state.BottomPatchCount, 0, 4);
            state.SidePatchCountPerSide = Mathf.Clamp(state.SidePatchCountPerSide, 0, 2);
            state.TopPatchCount = Mathf.Clamp(state.TopPatchCount, 0, 1);
            if (state.PatchDriftSpeedRange == default)
            {
                state.PatchDriftSpeedRange = new Vector2(0.34f, 0.72f);
            }

            state.PatchDriftSpeedRange.x = Mathf.Max(0.01f, state.PatchDriftSpeedRange.x);
            state.PatchDriftSpeedRange.y = Mathf.Max(state.PatchDriftSpeedRange.x, state.PatchDriftSpeedRange.y);
            state.PatchAmplitude = Mathf.Clamp(state.PatchAmplitude <= 0f ? 1f : state.PatchAmplitude, 0f, 2f);
            state.PatchShapeInfluence = Mathf.Clamp(state.PatchShapeInfluence <= 0f ? 1f : state.PatchShapeInfluence, 0f, 2f);
            state.PatchColorInfluence = Mathf.Clamp(state.PatchColorInfluence <= 0f ? 1f : state.PatchColorInfluence, 0f, 2f);
            state.BottomDominance = Mathf.Clamp(state.BottomDominance <= 0f ? 1f : state.BottomDominance, 0f, 2f);
            state.SideSupport = Mathf.Clamp(state.SideSupport, 0f, 2f);
            state.TopSuppression = Mathf.Clamp01(state.TopSuppression);
            state.BottomToSideBleed = Mathf.Clamp01(state.BottomToSideBleed);
            state.CornerBleedAttenuation = Mathf.Clamp01(state.CornerBleedAttenuation <= 0f ? 0.72f : state.CornerBleedAttenuation);
            state.BottomThicknessMultiplier = Mathf.Clamp(state.BottomThicknessMultiplier <= 0f ? 1f : state.BottomThicknessMultiplier, 0f, 2f);
            state.SideThicknessMultiplier = Mathf.Clamp(state.SideThicknessMultiplier <= 0f ? 0.5f : state.SideThicknessMultiplier, 0f, 2f);
            return state;
        }

        private static Color Hex(byte r, byte g, byte b)
        {
            return new Color(r / 255f, g / 255f, b / 255f, 1f);
        }
    }
}
