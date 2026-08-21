using System;
using UnityEngine;

namespace ENP.UnityExtensions.Runtime
{
    [AddComponentMenu("UI/Round Anim Button")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AnimatedButton))]
    public sealed class RoundAnimButton : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private AnimatedButton _button;
        [SerializeField] private RoundedShapeGraphic _graphic;

        [Header("Style")]
        [SerializeField] private RoundedShapeStyle _normalStyle;
        [SerializeField] private RoundedShapeStyle _selectedStyle;

        private bool _isSelected;

        public event Action Pressed
        {
            add => _button.Pressed += value;
            remove => _button.Pressed -= value;
        }

        public event Action Released
        {
            add => _button.Released += value;
            remove => _button.Released -= value;
        }

        public AnimatedButton Button => _button;
        public RoundedShapeGraphic Graphic => _graphic;

        public bool Interactable
        {
            get => _button.Interactable;
            set => _button.SetInteractable(value);
        }

        public bool IsSelected => _isSelected;

        private void Awake()
        {
            ApplyStyle();
        }

        private void OnValidate()
        {
            ApplyStyle();

            if (_button == null)
            {
                _button = GetComponent<AnimatedButton>();
            }
        }

        public void AddListener(Action onClick)
        {
            _button.AddListener(onClick);
        }

        public void RemoveListener(Action onClick)
        {
            _button.RemoveListener(onClick);
        }

        public void RemoveListener()
        {
            _button.RemoveListener();
        }

        public void SetSelected(bool selected)
        {
            if (_isSelected == selected) return;
            _isSelected = selected;
            ApplyStyle();
        }

        private void ApplyStyle()
        {
            _graphic.Style = _isSelected ? _selectedStyle : _normalStyle;
        }
    }
}
