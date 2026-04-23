using UnityEngine;
using UnityEngine.UI;

namespace enp_unity_extensions.Runtime.Scripts.UI.Effects.LiquidEdge
{
    public sealed class LiquidEdgeDemoControls : MonoBehaviour
    {
        [SerializeField] private RoundedRectLiquidEdgeController _controller;
        [SerializeField] private Slider _globalIntensity;
        [SerializeField] private Slider _bottomDominance;
        [SerializeField] private Slider _sideSupport;
        [SerializeField] private Slider _topSuppression;
        [SerializeField] private Text _presetLabel;

        private void Awake()
        {
            BindSlider(_globalIntensity, OnGlobalIntensityChanged);
            BindSlider(_bottomDominance, OnBottomDominanceChanged);
            BindSlider(_sideSupport, OnSideSupportChanged);
            BindSlider(_topSuppression, OnTopSuppressionChanged);
            SyncFromController();
        }

        public void ApplyCalm()
        {
            ApplyPreset(LiquidEdgePreset.Calm);
        }

        public void ApplyLiving()
        {
            ApplyPreset(LiquidEdgePreset.Living);
        }

        public void ApplyRich()
        {
            ApplyPreset(LiquidEdgePreset.Rich);
        }

        public void ApplyOverdriven()
        {
            ApplyPreset(LiquidEdgePreset.Overdriven);
        }

        private void ApplyPreset(LiquidEdgePreset preset)
        {
            if (_controller == null)
            {
                return;
            }

            _controller.ApplyPreset(preset);
            SyncFromController();
        }

        private void SyncFromController()
        {
            if (_controller == null)
            {
                return;
            }

            LiquidEdgeState state = _controller.State;
            SetSliderWithoutNotify(_globalIntensity, state.GlobalIntensity);
            SetSliderWithoutNotify(_bottomDominance, state.BottomDominance);
            SetSliderWithoutNotify(_sideSupport, state.SideSupportAmount);
            SetSliderWithoutNotify(_topSuppression, state.TopSuppression);

            if (_presetLabel != null)
            {
                _presetLabel.text = _controller.Preset.ToString();
            }
        }

        private void OnGlobalIntensityChanged(float value)
        {
            if (_controller != null)
            {
                _controller.SetGlobalIntensity(value);
            }
        }

        private void OnBottomDominanceChanged(float value)
        {
            if (_controller != null)
            {
                _controller.SetBottomDominance(value);
            }
        }

        private void OnSideSupportChanged(float value)
        {
            if (_controller != null)
            {
                _controller.SetSideSupportAmount(value);
            }
        }

        private void OnTopSuppressionChanged(float value)
        {
            if (_controller != null)
            {
                _controller.SetTopSuppression(value);
            }
        }

        private static void BindSlider(Slider slider, UnityEngine.Events.UnityAction<float> callback)
        {
            if (slider == null)
            {
                return;
            }

            slider.onValueChanged.AddListener(callback);
        }

        private static void SetSliderWithoutNotify(Slider slider, float value)
        {
            if (slider == null)
            {
                return;
            }

            slider.SetValueWithoutNotify(value);
        }
    }
}
