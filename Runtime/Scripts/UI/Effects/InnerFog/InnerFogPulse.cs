using System;
using UnityEngine;

namespace enp_unity_extensions.Runtime.Scripts.UI.Effects.InnerFog
{
    [Serializable]
    public struct InnerFogPulse
    {
        public Color TopColor;
        public Color BottomColor;
        [Range(0f, 1f)] public float ColorBlend;
        [Min(0f)] public float IntensityBoost;
        public float CoverageOffset;
        public float ContrastOffset;
        public float SoftnessOffset;
        public float NoiseScaleOffset;
        public float WarpAmountOffset;
        public float WarpSpeedOffset;
        public float CenterProtectionOffset;
        public float EdgeBoostOffset;
        public float CornerRoundnessOffset;
        [Min(0f)] public float Attack;
        [Min(0f)] public float Hold;
        [Min(0f)] public float Decay;
        public bool IgnoreTimeScale;

        public float Duration => Attack + Hold + Decay;

        public static InnerFogPulse Sanitize(InnerFogPulse pulse)
        {
            pulse.ColorBlend = Mathf.Clamp01(pulse.ColorBlend);
            pulse.IntensityBoost = Mathf.Max(0f, pulse.IntensityBoost);
            pulse.Attack = Mathf.Max(0f, pulse.Attack);
            pulse.Hold = Mathf.Max(0f, pulse.Hold);
            pulse.Decay = Mathf.Max(0f, pulse.Decay);
            return pulse;
        }
    }
}
