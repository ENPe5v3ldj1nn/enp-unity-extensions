using System;
using UnityEngine;

namespace ENP.ProceduralInnerWash.Runtime
{
    [Serializable]
    public struct AsymmetricInnerWashState
    {
        public Color TintColor;
        public Color TopColor;
        public Color BottomColor;
        [Range(0f, 1f)] public float Intensity;
        [Range(0f, 1f)] public float Thickness;
        [Range(0f, 1f)] public float Softness;
        [Range(0f, 1f)] public float CenterClear;
        [Min(0f)] public float CornerRoundness;
        [Range(0f, 2f)] public float TopStrength;
        [Range(0f, 2f)] public float BottomStrength;
        [Range(0f, 2f)] public float LeftStrength;
        [Range(0f, 2f)] public float RightStrength;

        public static AsymmetricInnerWashState CreateNeutralAmbient()
        {
            return new AsymmetricInnerWashState
            {
                TintColor = Color.white,
                TopColor = new Color(0.85f, 0.92f, 1f, 1f),
                BottomColor = new Color(0.50f, 0.66f, 0.92f, 1f),
                Intensity = 0.14f,
                Thickness = 0.18f,
                Softness = 0.72f,
                CenterClear = 0.68f,
                CornerRoundness = 26f,
                TopStrength = 0.08f,
                BottomStrength = 0.45f,
                LeftStrength = 0.32f,
                RightStrength = 0.32f
            };
        }

        public static AsymmetricInnerWashState CreateMediumAmbient()
        {
            return new AsymmetricInnerWashState
            {
                TintColor = Color.white,
                TopColor = new Color(0.92f, 0.96f, 1f, 1f),
                BottomColor = new Color(0.58f, 0.72f, 0.95f, 1f),
                Intensity = 0.28f,
                Thickness = 0.27f,
                Softness = 0.76f,
                CenterClear = 0.58f,
                CornerRoundness = 30f,
                TopStrength = 0.14f,
                BottomStrength = 0.78f,
                LeftStrength = 0.58f,
                RightStrength = 0.58f
            };
        }

        public static AsymmetricInnerWashState CreateWarmStrong()
        {
            return new AsymmetricInnerWashState
            {
                TintColor = new Color(1f, 0.98f, 0.94f, 1f),
                TopColor = new Color(0.96f, 0.84f, 0.72f, 1f),
                BottomColor = new Color(1f, 0.54f, 0.28f, 1f),
                Intensity = 0.48f,
                Thickness = 0.34f,
                Softness = 0.74f,
                CenterClear = 0.50f,
                CornerRoundness = 34f,
                TopStrength = 0.18f,
                BottomStrength = 1.00f,
                LeftStrength = 0.72f,
                RightStrength = 0.72f
            };
        }

        public static AsymmetricInnerWashState Sanitize(AsymmetricInnerWashState state)
        {
            if (state.TintColor == default)
            {
                state.TintColor = Color.white;
            }

            state.Intensity = Mathf.Clamp01(state.Intensity);
            state.Thickness = Mathf.Clamp01(state.Thickness);
            state.Softness = Mathf.Clamp01(state.Softness);
            state.CenterClear = Mathf.Clamp01(state.CenterClear);
            state.CornerRoundness = Mathf.Max(0f, state.CornerRoundness);
            state.TopStrength = Mathf.Clamp(state.TopStrength, 0f, 2f);
            state.BottomStrength = Mathf.Clamp(state.BottomStrength, 0f, 2f);
            state.LeftStrength = Mathf.Clamp(state.LeftStrength, 0f, 2f);
            state.RightStrength = Mathf.Clamp(state.RightStrength, 0f, 2f);
            return state;
        }

        public static AsymmetricInnerWashState Lerp(AsymmetricInnerWashState from, AsymmetricInnerWashState to, float t)
        {
            t = Mathf.Clamp01(t);

            return Sanitize(new AsymmetricInnerWashState
            {
                TintColor = Color.LerpUnclamped(from.TintColor, to.TintColor, t),
                TopColor = Color.LerpUnclamped(from.TopColor, to.TopColor, t),
                BottomColor = Color.LerpUnclamped(from.BottomColor, to.BottomColor, t),
                Intensity = Mathf.LerpUnclamped(from.Intensity, to.Intensity, t),
                Thickness = Mathf.LerpUnclamped(from.Thickness, to.Thickness, t),
                Softness = Mathf.LerpUnclamped(from.Softness, to.Softness, t),
                CenterClear = Mathf.LerpUnclamped(from.CenterClear, to.CenterClear, t),
                CornerRoundness = Mathf.LerpUnclamped(from.CornerRoundness, to.CornerRoundness, t),
                TopStrength = Mathf.LerpUnclamped(from.TopStrength, to.TopStrength, t),
                BottomStrength = Mathf.LerpUnclamped(from.BottomStrength, to.BottomStrength, t),
                LeftStrength = Mathf.LerpUnclamped(from.LeftStrength, to.LeftStrength, t),
                RightStrength = Mathf.LerpUnclamped(from.RightStrength, to.RightStrength, t)
            });
        }
    }
}
