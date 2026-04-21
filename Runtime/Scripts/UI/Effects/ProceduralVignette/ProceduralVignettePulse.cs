using System;
using UnityEngine;

namespace enp_unity_extensions.Runtime.Scripts.UI.Effects.ProceduralVignette
{
    [Serializable]
    public struct ProceduralVignettePulse
    {
        public Color TopColor;
        public Color BottomColor;
        [Range(0f, 1f)] public float ColorBlend;
        [Min(0f)] public float IntensityBoost;
        public float ThicknessOffset;
        public float SoftnessOffset;
        public float CornerRoundnessOffset;
        public float CenterClearOffset;
        public float WarpAmountOffset;
        public float WarpScaleOffset;
        public float WarpSpeedOffset;
        public float NoiseAmountOffset;
        public float NoiseScaleOffset;
        public float NoiseSpeedOffset;
        public float TopEdgeStrengthOffset;
        public float BottomEdgeStrengthOffset;
        public float LeftEdgeStrengthOffset;
        public float RightEdgeStrengthOffset;
        [Min(0f)] public float Attack;
        [Min(0f)] public float Hold;
        [Min(0f)] public float Decay;
        public bool IgnoreTimeScale;

        public float Duration => Attack + Hold + Decay;

        public static ProceduralVignettePulse Sanitize(ProceduralVignettePulse pulse)
        {
            pulse.ColorBlend = Mathf.Clamp01(pulse.ColorBlend);
            pulse.IntensityBoost = Mathf.Max(0f, pulse.IntensityBoost);
            pulse.SoftnessOffset = Mathf.Clamp(pulse.SoftnessOffset, -1f, 1f);
            pulse.Attack = Mathf.Max(0f, pulse.Attack);
            pulse.Hold = Mathf.Max(0f, pulse.Hold);
            pulse.Decay = Mathf.Max(0f, pulse.Decay);
            return pulse;
        }
    }
}
