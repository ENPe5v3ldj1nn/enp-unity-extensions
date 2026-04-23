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
                TintColor = Hex(0xF4, 0xF7, 0xFF),
                TopColor = Hex(0xEA, 0xF2, 0xFF),
                BottomColor = Hex(0xA8, 0xC4, 0xFF),
                AccentColor = Hex(0xF3, 0xB6, 0x3F),
                Intensity = 0.816f,
                Thickness = 0.859f,
                Softness = 0.615f,
                BandTightness = 0.49f,
                CenterClear = 0f,
                CornerRoundness = 80f,
                TopStrength = 0f,
                BottomStrength = 0f,
                LeftStrength = 0f,
                RightStrength = 0f,
                BottomSegments = new LivingInnerWashFiveSegments(0.60f, 0.68f, 0.78f, 0.68f, 0.60f),
                LeftSegments = new LivingInnerWashThreeSegments(0.22f, 0.28f, 0.10f),
                RightSegments = new LivingInnerWashThreeSegments(0.22f, 0.28f, 0.10f),
                BottomAccents = new LivingInnerWashFiveSegments(0.08f, 0.14f, 0.18f, 0.14f, 0.08f),
                LeftAccents = new LivingInnerWashThreeSegments(0.02f, 0.05f, 0.03f),
                RightAccents = new LivingInnerWashThreeSegments(0.02f, 0.05f, 0.03f)
            };
        }

        public static LivingInnerWashState Sanitize(LivingInnerWashState state)
        {
            LivingInnerWashState defaults = Default();

            if (state.TintColor == default)
            {
                state.TintColor = defaults.TintColor;
            }

            if (state.TopColor == default)
            {
                state.TopColor = defaults.TopColor;
            }

            if (state.BottomColor == default)
            {
                state.BottomColor = defaults.BottomColor;
            }

            if (state.AccentColor == default)
            {
                state.AccentColor = defaults.AccentColor;
            }

            state.Intensity = Mathf.Clamp01(state.Intensity);
            state.Thickness = Mathf.Clamp01(state.Thickness <= 0f ? defaults.Thickness : state.Thickness);
            state.Softness = Mathf.Clamp01(state.Softness <= 0f ? defaults.Softness : state.Softness);
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

        private static Color Hex(byte r, byte g, byte b)
        {
            return new Color(r / 255f, g / 255f, b / 255f, 1f);
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
