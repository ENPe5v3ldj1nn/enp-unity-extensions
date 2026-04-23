using System;
using UnityEngine;

namespace enp_unity_extensions.Runtime.Scripts.UI.Effects.Wash
{
    [Serializable]
    public struct ThreeSegmentValues
    {
        [SerializeField] private float _segment0;
        [SerializeField] private float _segment1;
        [SerializeField] private float _segment2;

        public void SetAll(float value)
        {
            _segment0 = value;
            _segment1 = value;
            _segment2 = value;
        }

        public void Clamp(float min, float max)
        {
            _segment0 = Mathf.Clamp(_segment0, min, max);
            _segment1 = Mathf.Clamp(_segment1, min, max);
            _segment2 = Mathf.Clamp(_segment2, min, max);
        }

        public Vector4 ToStrengthVector(bool absoluteValues, float baseStrength)
        {
            return new Vector4(
                ResolveStrength(_segment0, absoluteValues, baseStrength),
                ResolveStrength(_segment1, absoluteValues, baseStrength),
                ResolveStrength(_segment2, absoluteValues, baseStrength),
                0f);
        }

        public Vector4 ToAccentVector()
        {
            return new Vector4(
                Mathf.Clamp01(_segment0),
                Mathf.Clamp01(_segment1),
                Mathf.Clamp01(_segment2),
                0f);
        }

        private static float ResolveStrength(float rawValue, bool absoluteValues, float baseStrength)
        {
            if (absoluteValues)
            {
                return Mathf.Clamp(rawValue, 0f, 2f);
            }

            return Mathf.Clamp(baseStrength + rawValue, 0f, 2f);
        }
    }

    [Serializable]
    public struct FiveSegmentValues
    {
        [SerializeField] private float _segment0;
        [SerializeField] private float _segment1;
        [SerializeField] private float _segment2;
        [SerializeField] private float _segment3;
        [SerializeField] private float _segment4;

        public void SetAll(float value)
        {
            _segment0 = value;
            _segment1 = value;
            _segment2 = value;
            _segment3 = value;
            _segment4 = value;
        }

        public void Clamp(float min, float max)
        {
            _segment0 = Mathf.Clamp(_segment0, min, max);
            _segment1 = Mathf.Clamp(_segment1, min, max);
            _segment2 = Mathf.Clamp(_segment2, min, max);
            _segment3 = Mathf.Clamp(_segment3, min, max);
            _segment4 = Mathf.Clamp(_segment4, min, max);
        }

        public Vector4 ToStrengthVectorA(bool absoluteValues, float baseStrength)
        {
            return new Vector4(
                ResolveStrength(_segment0, absoluteValues, baseStrength),
                ResolveStrength(_segment1, absoluteValues, baseStrength),
                ResolveStrength(_segment2, absoluteValues, baseStrength),
                ResolveStrength(_segment3, absoluteValues, baseStrength));
        }

        public Vector4 ToStrengthVectorB(bool absoluteValues, float baseStrength)
        {
            return new Vector4(
                ResolveStrength(_segment4, absoluteValues, baseStrength),
                0f,
                0f,
                0f);
        }

        public Vector4 ToAccentVectorA()
        {
            return new Vector4(
                Mathf.Clamp01(_segment0),
                Mathf.Clamp01(_segment1),
                Mathf.Clamp01(_segment2),
                Mathf.Clamp01(_segment3));
        }

        public Vector4 ToAccentVectorB()
        {
            return new Vector4(
                Mathf.Clamp01(_segment4),
                0f,
                0f,
                0f);
        }

        private static float ResolveStrength(float rawValue, bool absoluteValues, float baseStrength)
        {
            if (absoluteValues)
            {
                return Mathf.Clamp(rawValue, 0f, 2f);
            }

            return Mathf.Clamp(baseStrength + rawValue, 0f, 2f);
        }
    }
}
