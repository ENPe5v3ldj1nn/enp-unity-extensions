using UnityEngine;
using UnityEngine.UI;

namespace enp_unity_extensions.Runtime.Scripts.UI.Effects.LiquidEdge
{
    public sealed class PatchLiquidDemoControls : MonoBehaviour
    {
        [SerializeField] private RoundedRectPatchLiquidController _controller;
        [SerializeField] private Slider _globalIntensity;
        [SerializeField] private Slider _motionSpeed;
        [SerializeField] private Slider _shapeInfluence;
        [SerializeField] private Slider _colorInfluence;
        [SerializeField] private Text _presetLabel;

        private void Awake()
        {
            BindSlider(_globalIntensity, OnGlobalIntensityChanged);
            BindSlider(_motionSpeed, OnMotionSpeedChanged);
            BindSlider(_shapeInfluence, OnShapeInfluenceChanged);
            BindSlider(_colorInfluence, OnColorInfluenceChanged);
            SyncFromController();
        }

        public void ApplyCalm() => ApplyPreset(LiquidEdgePreset.Calm);
        public void ApplyLiving() => ApplyPreset(LiquidEdgePreset.Living);
        public void ApplyRich() => ApplyPreset(LiquidEdgePreset.Rich);
        public void ApplyOverdriven() => ApplyPreset(LiquidEdgePreset.Overdriven);

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

            PatchLiquidEdgeState state = _controller.State;
            SetSliderWithoutNotify(_globalIntensity, state.GlobalIntensity);
            SetSliderWithoutNotify(_motionSpeed, state.GlobalMotionSpeed);
            SetSliderWithoutNotify(_shapeInfluence, state.PatchShapeInfluence);
            SetSliderWithoutNotify(_colorInfluence, state.PatchColorInfluence);

            if (_presetLabel != null)
            {
                _presetLabel.text = _controller.Preset.ToString();
            }
        }

        private void OnGlobalIntensityChanged(float value)
        {
            if (_controller == null) return;
            PatchLiquidEdgeState state = _controller.State;
            state.GlobalIntensity = value;
            _controller.ApplyState(state, false);
        }

        private void OnMotionSpeedChanged(float value)
        {
            if (_controller == null) return;
            PatchLiquidEdgeState state = _controller.State;
            state.GlobalMotionSpeed = value;
            _controller.ApplyState(state, false);
        }

        private void OnShapeInfluenceChanged(float value)
        {
            if (_controller == null) return;
            PatchLiquidEdgeState state = _controller.State;
            state.PatchShapeInfluence = value;
            _controller.ApplyState(state, false);
        }

        private void OnColorInfluenceChanged(float value)
        {
            if (_controller == null) return;
            PatchLiquidEdgeState state = _controller.State;
            state.PatchColorInfluence = value;
            _controller.ApplyState(state, false);
        }

        private static void BindSlider(Slider slider, UnityEngine.Events.UnityAction<float> callback)
        {
            if (slider != null)
            {
                slider.onValueChanged.AddListener(callback);
            }
        }

        private static void SetSliderWithoutNotify(Slider slider, float value)
        {
            if (slider != null)
            {
                slider.SetValueWithoutNotify(value);
            }
        }
    }
}
