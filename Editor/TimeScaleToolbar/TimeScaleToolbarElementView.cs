using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ENP.UnityExtensions.Editor.TimeScaleToolbar
{
    internal sealed class TimeScaleToolbarElementView : VisualElement
    {
        internal const string ElementId = "ENP.UnityExtensions.TimeScaleToolbar.Element";
        private const float SliderWidth = 120f;

        private readonly Slider _slider;
        private readonly Label _valueLabel;
        private readonly Button _quarterButton;
        private readonly Button _halfButton;
        private readonly Button _oneButton;
        private readonly Button _twoButton;
        private readonly Button _fiveButton;

        public TimeScaleToolbarElementView()
        {
            style.flexDirection = FlexDirection.Row;
            style.alignItems = Align.Center;
            style.marginLeft = 4f;
            style.marginRight = 4f;

            _slider = new Slider(0f, 5f)
            {
                name = "TimeScaleSlider",
                style =
                {
                    width = SliderWidth,
                    marginRight = 4f,
                    flexShrink = 0f
                }
            };
            _slider.RegisterValueChangedCallback(HandleSliderChanged);

            _valueLabel = new Label
            {
                name = "TimeScaleValueLabel",
                style =
                {
                    minWidth = 40f,
                    unityTextAlign = TextAnchor.MiddleLeft,
                    marginRight = 6f
                }
            };

            _quarterButton = CreatePresetButton("0.25x", 0.25f);
            _halfButton = CreatePresetButton("0.5x", 0.5f);
            _oneButton = CreatePresetButton("1x", 1f);
            _twoButton = CreatePresetButton("2x", 2f);
            _fiveButton = CreatePresetButton("5x", 5f);

            Add(_slider);
            Add(_valueLabel);
            Add(_quarterButton);
            Add(_halfButton);
            Add(_oneButton);
            Add(_twoButton);
            Add(_fiveButton);

            TimeScaleToolbarState.ScaleChanged += HandleScaleChanged;
            RegisterCallback<DetachFromPanelEvent>(HandleDetachedFromPanel);
            Refresh(TimeScaleToolbarState.CurrentScale);
        }

        private Button CreatePresetButton(string label, float scale)
        {
            var button = new Button(() => TimeScaleToolbarState.SetScale(scale))
            {
                text = label,
                style =
                {
                    marginLeft = 2f,
                    marginRight = 2f,
                    paddingLeft = 6f,
                    paddingRight = 6f,
                    flexShrink = 0f
                }
            };

            return button;
        }

        private void HandleSliderChanged(ChangeEvent<float> evt)
        {
            TimeScaleToolbarState.SetScale(evt.newValue);
        }

        private void HandleScaleChanged(float scale)
        {
            Refresh(scale);
        }

        private void HandleDetachedFromPanel(DetachFromPanelEvent evt)
        {
            TimeScaleToolbarState.ScaleChanged -= HandleScaleChanged;
            _slider.UnregisterValueChangedCallback(HandleSliderChanged);
        }

        private void Refresh(float scale)
        {
            _slider.SetValueWithoutNotify(scale);
            _slider.SetEnabled(true);
            _valueLabel.text = $"{scale:0.00}x";
            SetButtonsEnabled(true);
        }

        private void SetButtonsEnabled(bool enabled)
        {
            _quarterButton.SetEnabled(enabled);
            _halfButton.SetEnabled(enabled);
            _oneButton.SetEnabled(enabled);
            _twoButton.SetEnabled(enabled);
            _fiveButton.SetEnabled(enabled);
        }
    }
}
