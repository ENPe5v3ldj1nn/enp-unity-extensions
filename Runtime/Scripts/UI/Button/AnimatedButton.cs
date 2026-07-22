using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

namespace ENP.UnityExtensions.Runtime
{
    [AddComponentMenu("UI/Animated Button")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(AnimatedButtonAnimation))]
    public sealed class AnimatedButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        [Header("Visual")]
        [FormerlySerializedAs("IsUseAnimation")]
        [SerializeField] private bool _useAnimation = true;
        [FormerlySerializedAs("_animValue")]
        [SerializeField, Range(0.01f, 1f)] private float _pressedScale = 0.95f;
        [SerializeField, Min(0f)] private float _animationDuration = 0.2f;
        [FormerlySerializedAs("Interactable")]
        [SerializeField] private bool _interactable = true;
        [SerializeField, Range(0f, 1f)] private float _disabledAlpha = 0.6f;

        [Header("Input")]
        [SerializeField, Min(0f)] private float _clickBlockDuration = 0.25f;

        [Header("Optional")]
        [FormerlySerializedAs("text")]
        [SerializeField] private TMP_Text _text;

        [Header("Events")]
        [SerializeField] private UnityEvent _onClick = new();

        private CanvasGroup _canvasGroup;
        private bool _isPointerDown;
        private float _blockInputUntilTime;

        public event Action Pressed;
        public event Action Released;

        public bool Interactable
        {
            get => _interactable;
            set => SetInteractable(value);
        }

        public bool UseAnimation
        {
            get => _useAnimation;
            set => _useAnimation = value;
        }

        public float PressedScale
        {
            get => _pressedScale;
            set => _pressedScale = value;
        }

        public float AnimationDuration
        {
            get => _animationDuration;
            set => _animationDuration = value;
        }

        public float DisabledAlpha
        {
            get => _disabledAlpha;
            set
            {
                _disabledAlpha = value;
                ApplyInteractableVisual();
            }
        }

        public float ClickBlockDuration
        {
            get => _clickBlockDuration;
            set => _clickBlockDuration = value;
        }

        public TMP_Text Text => _text;

        private void Awake()
        {
            CacheReferences();
            ApplyInteractableVisual();
        }

        private void OnEnable()
        {
            _isPointerDown = false;
            _blockInputUntilTime = 0f;
            ApplyInteractableVisual();
        }

        private void OnValidate()
        {
            CacheReferences();
            ApplyInteractableVisual();
        }

        public void AddListener(UnityAction onClick)
        {
            _onClick.AddListener(onClick);
        }

        public void RemoveListener()
        {
            _onClick.RemoveAllListeners();
        }

        public void RemoveListener(UnityAction onClick)
        {
            _onClick.RemoveListener(onClick);
        }

        public void ForceInvoke()
        {
            _onClick.Invoke();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!CanInteract(eventData))
            {
                return;
            }

            _isPointerDown = true;
            Pressed?.Invoke();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || !_isPointerDown)
            {
                return;
            }

            _isPointerDown = false;
            Released?.Invoke();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!CanClick(eventData))
            {
                return;
            }

            _onClick.Invoke();
            _blockInputUntilTime = Time.unscaledTime + _clickBlockDuration;
        }

        public void SetInteractable(bool value)
        {
            if (_interactable == value)
            {
                ApplyInteractableVisual();
                return;
            }

            _interactable = value;

            if (!_interactable && _isPointerDown)
            {
                _isPointerDown = false;
                Released?.Invoke();
            }

            ApplyInteractableVisual();
        }

        public void CacheReferences()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            Debug.Assert(_canvasGroup != null, "AnimatedButton requires a CanvasGroup.");
        }

        private bool CanInteract(PointerEventData eventData)
        {
            return eventData.button == PointerEventData.InputButton.Left && _interactable && !IsInputBlocked();
        }

        private bool CanClick(PointerEventData eventData)
        {
            return eventData.button == PointerEventData.InputButton.Left && _interactable && !IsInputBlocked();
        }

        private bool IsInputBlocked()
        {
            return Time.unscaledTime < _blockInputUntilTime;
        }

        private void ApplyInteractableVisual()
        {
            _canvasGroup.interactable = _interactable;
            _canvasGroup.blocksRaycasts = _interactable;
            _canvasGroup.alpha = _interactable ? 1f : Mathf.Clamp01(_disabledAlpha);
        }
    }
}
