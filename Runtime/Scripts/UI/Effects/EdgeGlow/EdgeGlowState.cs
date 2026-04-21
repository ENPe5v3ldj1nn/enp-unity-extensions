using System;
using UnityEngine;

namespace enp_unity_extensions.Runtime.Scripts.UI.Effects.EdgeGlow
{
    [Serializable]
    public struct EdgeGlowState
    {
        public Color TopColor;
        public Color BottomColor;
        [Range(0f, 1f)] public float Intensity;
        [Min(0f)] public float Thickness;
        [Range(0f, 1f)] public float Softness;
        [Min(0f)] public float CornerRoundness;
        [Range(0f, 1f)] public float CenterClear;

        public static EdgeGlowState CreateDefault()
        {
            return new EdgeGlowState
            {
                TopColor = new Color(0.25f, 0.95f, 1f, 1f),
                BottomColor = new Color(0.1f, 0.35f, 1f, 1f),
                Intensity = 0.85f,
                Thickness = 72f,
                Softness = 0.2f,
                CornerRoundness = 24f,
                CenterClear = 0.68f
            };
        }

        public static EdgeGlowState CreateNeutral()
        {
            return new EdgeGlowState
            {
                TopColor = new Color(1f, 1f, 1f, 0f),
                BottomColor = new Color(1f, 1f, 1f, 0f),
                Intensity = 0f,
                Thickness = 72f,
                Softness = 0.2f,
                CornerRoundness = 24f,
                CenterClear = 0.68f
            };
        }

        public static EdgeGlowState Sanitize(EdgeGlowState state)
        {
            state.Intensity = Mathf.Clamp01(state.Intensity);
            state.Thickness = Mathf.Max(0f, state.Thickness);
            state.Softness = Mathf.Clamp01(state.Softness);
            state.CornerRoundness = Mathf.Max(0f, state.CornerRoundness);
            state.CenterClear = Mathf.Clamp01(state.CenterClear);
            return state;
        }

        public static EdgeGlowState Lerp(EdgeGlowState from, EdgeGlowState to, float t)
        {
            t = Mathf.Clamp01(t);

            return new EdgeGlowState
            {
                TopColor = Color.LerpUnclamped(from.TopColor, to.TopColor, t),
                BottomColor = Color.LerpUnclamped(from.BottomColor, to.BottomColor, t),
                Intensity = Mathf.LerpUnclamped(from.Intensity, to.Intensity, t),
                Thickness = Mathf.LerpUnclamped(from.Thickness, to.Thickness, t),
                Softness = Mathf.LerpUnclamped(from.Softness, to.Softness, t),
                CornerRoundness = Mathf.LerpUnclamped(from.CornerRoundness, to.CornerRoundness, t),
                CenterClear = Mathf.LerpUnclamped(from.CenterClear, to.CenterClear, t)
            };
        }

        public static EdgeGlowState Compose(EdgeGlowState baseState, EdgeGlowPulse pulse, float weight)
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

            return result;
        }
    }
}
