using System;
using UnityEngine;

namespace enp_unity_extensions.Runtime.Scripts.UI.Effects.InnerFog
{
    [Serializable]
    public struct InnerFogState
    {
        public Color TopColor;
        public Color BottomColor;
        [Range(0f, 1f)] public float Intensity;
        [Range(0f, 1f)] public float Coverage;
        [Range(0f, 1f)] public float Contrast;
        [Range(0f, 1f)] public float Softness;
        [Min(0f)] public float NoiseScale;
        [Range(0f, 1f)] public float WarpAmount;
        [Min(0f)] public float WarpSpeed;
        [Range(0f, 1f)] public float CenterProtection;
        [Range(0f, 1f)] public float EdgeBoost;
        [Min(0f)] public float CornerRoundness;

        public static InnerFogState CreateDefault()
        {
            return new InnerFogState
            {
                TopColor = new Color(0.95f, 0.98f, 1f, 1f),
                BottomColor = new Color(0.62f, 0.78f, 1f, 1f),
                Intensity = 0.24f,
                Coverage = 0.52f,
                Contrast = 0.5f,
                Softness = 0.72f,
                NoiseScale = 4f,
                WarpAmount = 0.22f,
                WarpSpeed = 0.35f,
                CenterProtection = 0.4f,
                EdgeBoost = 0.2f,
                CornerRoundness = 24f
            };
        }

        public static InnerFogState CreateNeutral()
        {
            var state = CreateDefault();
            state.Intensity = 0f;
            return state;
        }

        public static InnerFogState Lerp(InnerFogState from, InnerFogState to, float t)
        {
            t = Mathf.Clamp01(t);

            return new InnerFogState
            {
                TopColor = Color.LerpUnclamped(from.TopColor, to.TopColor, t),
                BottomColor = Color.LerpUnclamped(from.BottomColor, to.BottomColor, t),
                Intensity = Mathf.LerpUnclamped(from.Intensity, to.Intensity, t),
                Coverage = Mathf.LerpUnclamped(from.Coverage, to.Coverage, t),
                Contrast = Mathf.LerpUnclamped(from.Contrast, to.Contrast, t),
                Softness = Mathf.LerpUnclamped(from.Softness, to.Softness, t),
                NoiseScale = Mathf.LerpUnclamped(from.NoiseScale, to.NoiseScale, t),
                WarpAmount = Mathf.LerpUnclamped(from.WarpAmount, to.WarpAmount, t),
                WarpSpeed = Mathf.LerpUnclamped(from.WarpSpeed, to.WarpSpeed, t),
                CenterProtection = Mathf.LerpUnclamped(from.CenterProtection, to.CenterProtection, t),
                EdgeBoost = Mathf.LerpUnclamped(from.EdgeBoost, to.EdgeBoost, t),
                CornerRoundness = Mathf.LerpUnclamped(from.CornerRoundness, to.CornerRoundness, t)
            };
        }

        public static InnerFogState Compose(InnerFogState baseState, InnerFogPulse pulse, float weight)
        {
            weight = Mathf.Clamp01(weight);
            var colorBlend = Mathf.Clamp01(pulse.ColorBlend * weight);

            return Sanitize(new InnerFogState
            {
                TopColor = Color.LerpUnclamped(baseState.TopColor, pulse.TopColor, colorBlend),
                BottomColor = Color.LerpUnclamped(baseState.BottomColor, pulse.BottomColor, colorBlend),
                Intensity = baseState.Intensity + pulse.IntensityBoost * weight,
                Coverage = baseState.Coverage + pulse.CoverageOffset * weight,
                Contrast = baseState.Contrast + pulse.ContrastOffset * weight,
                Softness = baseState.Softness + pulse.SoftnessOffset * weight,
                NoiseScale = baseState.NoiseScale + pulse.NoiseScaleOffset * weight,
                WarpAmount = baseState.WarpAmount + pulse.WarpAmountOffset * weight,
                WarpSpeed = baseState.WarpSpeed + pulse.WarpSpeedOffset * weight,
                CenterProtection = baseState.CenterProtection + pulse.CenterProtectionOffset * weight,
                EdgeBoost = baseState.EdgeBoost + pulse.EdgeBoostOffset * weight,
                CornerRoundness = baseState.CornerRoundness + pulse.CornerRoundnessOffset * weight
            });
        }

        public static InnerFogState Sanitize(InnerFogState state)
        {
            state.Intensity = Mathf.Clamp01(state.Intensity);
            state.Coverage = Mathf.Clamp01(state.Coverage);
            state.Contrast = Mathf.Clamp01(state.Contrast);
            state.Softness = Mathf.Clamp01(state.Softness);
            state.NoiseScale = Mathf.Max(0.001f, state.NoiseScale);
            state.WarpAmount = Mathf.Clamp01(state.WarpAmount);
            state.WarpSpeed = Mathf.Max(0f, state.WarpSpeed);
            state.CenterProtection = Mathf.Clamp01(state.CenterProtection);
            state.EdgeBoost = Mathf.Clamp01(state.EdgeBoost);
            state.CornerRoundness = Mathf.Max(0f, state.CornerRoundness);
            return state;
        }

        public static bool IsDefault(InnerFogState state)
        {
            return state.TopColor == default
                   && state.BottomColor == default
                   && Mathf.Approximately(state.Intensity, 0f)
                   && Mathf.Approximately(state.Coverage, 0f)
                   && Mathf.Approximately(state.Contrast, 0f)
                   && Mathf.Approximately(state.Softness, 0f)
                   && Mathf.Approximately(state.NoiseScale, 0f)
                   && Mathf.Approximately(state.WarpAmount, 0f)
                   && Mathf.Approximately(state.WarpSpeed, 0f)
                   && Mathf.Approximately(state.CenterProtection, 0f)
                   && Mathf.Approximately(state.EdgeBoost, 0f)
                   && Mathf.Approximately(state.CornerRoundness, 0f);
        }
    }
}
