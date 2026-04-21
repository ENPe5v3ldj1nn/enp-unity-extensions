using System;
using UnityEngine;

namespace enp_unity_extensions.Runtime.Scripts.UI.Effects.ProceduralVignette
{
    [Serializable]
    public struct ProceduralVignetteState
    {
        public Color TopColor;
        public Color BottomColor;
        [Range(0f, 1f)] public float Intensity;
        [Min(0f)] public float Thickness;
        [Range(0f, 1f)] public float Softness;
        [Min(0f)] public float CornerRoundness;
        [Range(0f, 1f)] public float CenterClear;
        [Min(0f)] public float WarpAmount;
        [Min(0f)] public float WarpScale;
        [Min(0f)] public float WarpSpeed;
        [Min(0f)] public float NoiseAmount;
        [Min(0f)] public float NoiseScale;
        [Min(0f)] public float NoiseSpeed;
        [Min(0f)] public float TopEdgeStrength;
        [Min(0f)] public float BottomEdgeStrength;
        [Min(0f)] public float LeftEdgeStrength;
        [Min(0f)] public float RightEdgeStrength;

        public static ProceduralVignetteState CreateDefault()
        {
            return new ProceduralVignetteState
            {
                TopColor = new Color(0.3f, 0.95f, 1f, 1f),
                BottomColor = new Color(0.08f, 0.32f, 1f, 1f),
                Intensity = 0.32f,
                Thickness = 52f,
                Softness = 0.58f,
                CornerRoundness = 24f,
                CenterClear = 0.74f,
                WarpAmount = 5f,
                WarpScale = 0.024f,
                WarpSpeed = 0.45f,
                NoiseAmount = 3.5f,
                NoiseScale = 0.055f,
                NoiseSpeed = 0.7f,
                TopEdgeStrength = 1f,
                BottomEdgeStrength = 1f,
                LeftEdgeStrength = 1f,
                RightEdgeStrength = 1f
            };
        }

        public static ProceduralVignetteState CreateNeutral()
        {
            return new ProceduralVignetteState
            {
                TopColor = new Color(1f, 1f, 1f, 0f),
                BottomColor = new Color(1f, 1f, 1f, 0f),
                Intensity = 0f,
                Thickness = 52f,
                Softness = 0.58f,
                CornerRoundness = 24f,
                CenterClear = 0.74f,
                WarpAmount = 5f,
                WarpScale = 0.024f,
                WarpSpeed = 0.45f,
                NoiseAmount = 3.5f,
                NoiseScale = 0.055f,
                NoiseSpeed = 0.7f,
                TopEdgeStrength = 1f,
                BottomEdgeStrength = 1f,
                LeftEdgeStrength = 1f,
                RightEdgeStrength = 1f
            };
        }

        public static ProceduralVignetteState Sanitize(ProceduralVignetteState state)
        {
            state.Intensity = Mathf.Clamp01(state.Intensity);
            state.Thickness = Mathf.Max(0f, state.Thickness);
            state.Softness = Mathf.Clamp01(state.Softness);
            state.CornerRoundness = Mathf.Max(0f, state.CornerRoundness);
            state.CenterClear = Mathf.Clamp01(state.CenterClear);
            state.WarpAmount = Mathf.Max(0f, state.WarpAmount);
            state.WarpScale = Mathf.Max(0f, state.WarpScale);
            state.WarpSpeed = Mathf.Max(0f, state.WarpSpeed);
            state.NoiseAmount = Mathf.Max(0f, state.NoiseAmount);
            state.NoiseScale = Mathf.Max(0f, state.NoiseScale);
            state.NoiseSpeed = Mathf.Max(0f, state.NoiseSpeed);
            state.TopEdgeStrength = Mathf.Max(0f, state.TopEdgeStrength);
            state.BottomEdgeStrength = Mathf.Max(0f, state.BottomEdgeStrength);
            state.LeftEdgeStrength = Mathf.Max(0f, state.LeftEdgeStrength);
            state.RightEdgeStrength = Mathf.Max(0f, state.RightEdgeStrength);
            return state;
        }

        public static ProceduralVignetteState Lerp(ProceduralVignetteState from, ProceduralVignetteState to, float t)
        {
            t = Mathf.Clamp01(t);

            return new ProceduralVignetteState
            {
                TopColor = Color.LerpUnclamped(from.TopColor, to.TopColor, t),
                BottomColor = Color.LerpUnclamped(from.BottomColor, to.BottomColor, t),
                Intensity = Mathf.LerpUnclamped(from.Intensity, to.Intensity, t),
                Thickness = Mathf.LerpUnclamped(from.Thickness, to.Thickness, t),
                Softness = Mathf.LerpUnclamped(from.Softness, to.Softness, t),
                CornerRoundness = Mathf.LerpUnclamped(from.CornerRoundness, to.CornerRoundness, t),
                CenterClear = Mathf.LerpUnclamped(from.CenterClear, to.CenterClear, t),
                WarpAmount = Mathf.LerpUnclamped(from.WarpAmount, to.WarpAmount, t),
                WarpScale = Mathf.LerpUnclamped(from.WarpScale, to.WarpScale, t),
                WarpSpeed = Mathf.LerpUnclamped(from.WarpSpeed, to.WarpSpeed, t),
                NoiseAmount = Mathf.LerpUnclamped(from.NoiseAmount, to.NoiseAmount, t),
                NoiseScale = Mathf.LerpUnclamped(from.NoiseScale, to.NoiseScale, t),
                NoiseSpeed = Mathf.LerpUnclamped(from.NoiseSpeed, to.NoiseSpeed, t),
                TopEdgeStrength = Mathf.LerpUnclamped(from.TopEdgeStrength, to.TopEdgeStrength, t),
                BottomEdgeStrength = Mathf.LerpUnclamped(from.BottomEdgeStrength, to.BottomEdgeStrength, t),
                LeftEdgeStrength = Mathf.LerpUnclamped(from.LeftEdgeStrength, to.LeftEdgeStrength, t),
                RightEdgeStrength = Mathf.LerpUnclamped(from.RightEdgeStrength, to.RightEdgeStrength, t)
            };
        }

        public static ProceduralVignetteState Compose(ProceduralVignetteState baseState, ProceduralVignettePulse pulse, float weight)
        {
            weight = Mathf.Clamp01(weight);

            var result = baseState;
            var colorBlend = Mathf.Clamp01(pulse.ColorBlend * weight);

            result.TopColor = Color.LerpUnclamped(baseState.TopColor, pulse.TopColor, colorBlend);
            result.BottomColor = Color.LerpUnclamped(baseState.BottomColor, pulse.BottomColor, colorBlend);
            result.Intensity = Mathf.Clamp01(baseState.Intensity + pulse.IntensityBoost * weight);
            result.Thickness = Mathf.Max(0f, baseState.Thickness + pulse.ThicknessOffset * weight);
            result.Softness = Mathf.Clamp01(baseState.Softness + pulse.SoftnessOffset * weight);
            result.CornerRoundness = Mathf.Max(0f, baseState.CornerRoundness + pulse.CornerRoundnessOffset * weight);
            result.CenterClear = Mathf.Clamp01(baseState.CenterClear + pulse.CenterClearOffset * weight);
            result.WarpAmount = Mathf.Max(0f, baseState.WarpAmount + pulse.WarpAmountOffset * weight);
            result.WarpScale = Mathf.Max(0f, baseState.WarpScale + pulse.WarpScaleOffset * weight);
            result.WarpSpeed = Mathf.Max(0f, baseState.WarpSpeed + pulse.WarpSpeedOffset * weight);
            result.NoiseAmount = Mathf.Max(0f, baseState.NoiseAmount + pulse.NoiseAmountOffset * weight);
            result.NoiseScale = Mathf.Max(0f, baseState.NoiseScale + pulse.NoiseScaleOffset * weight);
            result.NoiseSpeed = Mathf.Max(0f, baseState.NoiseSpeed + pulse.NoiseSpeedOffset * weight);
            result.TopEdgeStrength = Mathf.Max(0f, baseState.TopEdgeStrength + pulse.TopEdgeStrengthOffset * weight);
            result.BottomEdgeStrength = Mathf.Max(0f, baseState.BottomEdgeStrength + pulse.BottomEdgeStrengthOffset * weight);
            result.LeftEdgeStrength = Mathf.Max(0f, baseState.LeftEdgeStrength + pulse.LeftEdgeStrengthOffset * weight);
            result.RightEdgeStrength = Mathf.Max(0f, baseState.RightEdgeStrength + pulse.RightEdgeStrengthOffset * weight);

            return result;
        }
    }
}
