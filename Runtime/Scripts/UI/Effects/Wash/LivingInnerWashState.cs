using System;
using UnityEngine;

namespace enp_unity_extensions.Runtime.Scripts.UI.Effects.Wash
{
    [Serializable]
    public struct LivingInnerWashState
    {
        public Color TintColor;
        public Color TopColor;
        public Color BottomColor;
        public Color AccentColor;
        [Range(0f, 1f)] public float Intensity;
        [Range(0f, 1f)] public float Thickness;
        [Range(0f, 1f)] public float Softness;
        [Range(0f, 1f)] public float BandTightness;
        [Range(0f, 1f)] public float CenterClear;
        [Min(0f)] public float CornerRoundness;
        [Range(0f, 2f)] public float TopStrength;
        [Range(0f, 2f)] public float BottomStrength;
        [Range(0f, 2f)] public float LeftStrength;
        [Range(0f, 2f)] public float RightStrength;
        public LivingInnerWashFiveSegments BottomSegments;
        public LivingInnerWashThreeSegments LeftSegments;
        public LivingInnerWashThreeSegments RightSegments;
        public LivingInnerWashFiveSegments BottomAccents;
        public LivingInnerWashThreeSegments LeftAccents;
        public LivingInnerWashThreeSegments RightAccents;

        public static LivingInnerWashState Default()
        {
            return new LivingInnerWashState
            {
                TintColor = Color.white,
                TopColor = new Color(0.94f, 0.97f, 1f, 1f),
                BottomColor = new Color(0.58f, 0.72f, 0.98f, 1f),
                AccentColor = new Color(0.78f, 0.88f, 1f, 1f),
                Intensity = 0.86f,
                Thickness = 0.62f,
                Softness = 0.9f,
                BandTightness = 0f,
                CenterClear = 0f,
                CornerRoundness = 0f,
                TopStrength = 0f,
                BottomStrength = 0f,
                LeftStrength = 0f,
                RightStrength = 0f,
                BottomSegments = new LivingInnerWashFiveSegments(0.45f, 0.6f, 0.9f, 0.6f, 0.45f),
                LeftSegments = new LivingInnerWashThreeSegments(0.18f, 0.26f, 0.22f),
                RightSegments = new LivingInnerWashThreeSegments(0.18f, 0.26f, 0.22f),
                BottomAccents = new LivingInnerWashFiveSegments(0.025f, 0.12f, 0.16f, 0.12f, 0.025f),
                LeftAccents = new LivingInnerWashThreeSegments(0.005f, 0.045f, 0.015f),
                RightAccents = new LivingInnerWashThreeSegments(0.005f, 0.045f, 0.015f)
            };
        }

        public static LivingInnerWashState Sanitize(LivingInnerWashState state)
        {
            if (state.TintColor == default)
            {
                state.TintColor = Color.white;
            }

            if (state.TopColor == default)
            {
                state.TopColor = new Color(0.94f, 0.97f, 1f, 1f);
            }

            if (state.BottomColor == default)
            {
                state.BottomColor = new Color(0.58f, 0.72f, 0.98f, 1f);
            }

            if (state.AccentColor == default)
            {
                state.AccentColor = new Color(0.78f, 0.88f, 1f, 1f);
            }

            state.Intensity = Mathf.Clamp01(state.Intensity);
            state.Thickness = Mathf.Clamp01(state.Thickness <= 0f ? 0.62f : state.Thickness);
            state.Softness = Mathf.Clamp01(state.Softness <= 0f ? 0.9f : state.Softness);
            state.BandTightness = Mathf.Clamp01(state.BandTightness);
            state.CenterClear = Mathf.Clamp01(state.CenterClear);
            state.CornerRoundness = Mathf.Max(0f, state.CornerRoundness);
            state.TopStrength = Mathf.Clamp(state.TopStrength, 0f, 2f);
            state.BottomStrength = Mathf.Clamp(state.BottomStrength, 0f, 2f);
            state.LeftStrength = Mathf.Clamp(state.LeftStrength, 0f, 2f);
            state.RightStrength = Mathf.Clamp(state.RightStrength, 0f, 2f);
            state.BottomSegments.Clamp(0f, 2f);
            state.LeftSegments.Clamp(0f, 2f);
            state.RightSegments.Clamp(0f, 2f);
            state.BottomAccents.Clamp(0f, 1f);
            state.LeftAccents.Clamp(0f, 1f);
            state.RightAccents.Clamp(0f, 1f);
            return state;
        }
    }

    [Serializable]
    public struct LivingInnerWashThreeSegments
    {
        public float Top;
        public float Mid;
        public float Bottom;

        public LivingInnerWashThreeSegments(float top, float mid, float bottom)
        {
            Top = top;
            Mid = mid;
            Bottom = bottom;
        }

        public void Clamp(float min, float max)
        {
            Top = Mathf.Clamp(Top, min, max);
            Mid = Mathf.Clamp(Mid, min, max);
            Bottom = Mathf.Clamp(Bottom, min, max);
        }

        public void ToArray(float[] values)
        {
            values[0] = Top;
            values[1] = Mid;
            values[2] = Bottom;
        }
    }

    [Serializable]
    public struct LivingInnerWashFiveSegments
    {
        public float FarLeft;
        public float Left;
        public float Center;
        public float Right;
        public float FarRight;

        public LivingInnerWashFiveSegments(float farLeft, float left, float center, float right, float farRight)
        {
            FarLeft = farLeft;
            Left = left;
            Center = center;
            Right = right;
            FarRight = farRight;
        }

        public void Clamp(float min, float max)
        {
            FarLeft = Mathf.Clamp(FarLeft, min, max);
            Left = Mathf.Clamp(Left, min, max);
            Center = Mathf.Clamp(Center, min, max);
            Right = Mathf.Clamp(Right, min, max);
            FarRight = Mathf.Clamp(FarRight, min, max);
        }

        public void ToArray(float[] values)
        {
            values[0] = FarLeft;
            values[1] = Left;
            values[2] = Center;
            values[3] = Right;
            values[4] = FarRight;
        }
    }
}
