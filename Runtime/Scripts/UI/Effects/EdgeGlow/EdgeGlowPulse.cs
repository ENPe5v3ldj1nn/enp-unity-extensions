using System;
using UnityEngine;

namespace enp_unity_extensions.Runtime.Scripts.UI.Effects.EdgeGlow
{
    [Serializable]
    public struct EdgeGlowPulse
    {
        public Color TopColor;
        public Color BottomColor;
        [Range(0f, 1f)] public float ColorBlend;
        [Min(0f)] public float IntensityBoost;
        public float ThicknessOffset;
        public float SoftnessOffset;
        public float CornerRoundnessOffset;
        public float CenterClearOffset;
        [Min(0f)] public float Attack;
        [Min(0f)] public float Hold;
        [Min(0f)] public float Decay;
        public bool IgnoreTimeScale;

        public float Duration => Attack + Hold + Decay;

        public static EdgeGlowPulse Sanitize(EdgeGlowPulse pulse)
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
