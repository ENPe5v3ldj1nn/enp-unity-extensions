using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ENP.UnityExtensions.Runtime
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("UI/Rounded Slider")]
    [RequireComponent(typeof(RectTransform))]
    public sealed class RoundedSlider : MonoBehaviour,
        IPointerDownHandler,
        IBeginDragHandler,
        IDragHandler,
        ICanvasElement
    {
        [Header("Targets")]
        [SerializeField] private RectTransform _background;
        [SerializeField] private RectTransform _foreground;
        [SerializeField] private RectTransform _handle;

        [Header("Value")]
        [SerializeField] private float _minValue;
        [SerializeField] private float _maxValue = 1f;
        [SerializeField] private float _value;
        [SerializeField] private bool _wholeNumbers;
        [SerializeField] private bool _interactable = true;

        [Header("Layout")]
        [SerializeField] private Slider.Direction _direction = Slider.Direction.LeftToRight;
        [SerializeField, Min(0f)] private float _leftPadding;
        [SerializeField, Min(0f)] private float _rightPadding;
        [SerializeField, Min(0f)] private float _topPadding;
        [SerializeField, Min(0f)] private float _bottomPadding;

        [Header("Handle")]
        [SerializeField] private bool _driveHandle = true;
        [SerializeField, Min(0f)] private float _handleOffset;

        [Header("Events")]
        [SerializeField] private UnityEvent<float> _onValueChanged = new UnityEvent<float>();

        private RectTransform _selfRect;
        private RoundedSliderFillGraphic _backgroundGraphic;
        private RoundedSliderFillGraphic _foregroundGraphic;
        private bool _isDirty = true;

        public float minValue
        {
            get => _minValue;
            set
            {
                if (Mathf.Approximately(_minValue, value))
                    return;

                _minValue = value;
                if (_maxValue < _minValue)
                    _maxValue = _minValue;

                SetValue(_value, notify: false);
            }
        }

        public float maxValue
        {
            get => _maxValue;
            set
            {
                if (Mathf.Approximately(_maxValue, value))
                    return;

                _maxValue = value;
                if (_maxValue < _minValue)
                    _minValue = _maxValue;

                SetValue(_value, notify: false);
            }
        }

        public float value
        {
            get => _value;
            set => SetValue(value);
        }

        public float normalizedValue
        {
            get => Mathf.Approximately(_maxValue, _minValue)
                ? 0f
                : Mathf.InverseLerp(_minValue, _maxValue, _value);
            set => SetNormalizedValue(value);
        }

        public bool wholeNumbers
        {
            get => _wholeNumbers;
            set
            {
                if (_wholeNumbers == value)
                    return;

                _wholeNumbers = value;
                SetValue(_value, notify: false);
            }
        }

        public bool interactable
        {
            get => _interactable;
            set => _interactable = value;
        }

        public Slider.Direction direction
        {
            get => _direction;
            set
            {
                if (_direction == value)
                    return;

                _direction = value;
                SetDirty();
            }
        }

        public UnityEvent<float> onValueChanged => _onValueChanged;

        private void Awake()
        {
            EnsureBackgroundGraphic();
            EnsureForegroundGraphic();
            CacheReferences();
            SetValue(_value, notify: false);
        }

        private void OnEnable()
        {
            EnsureBackgroundGraphic();
            EnsureForegroundGraphic();
            CacheReferences();
            SetDirty();
        }

        private void OnDisable()
        {
            _isDirty = true;
        }

        private void OnValidate()
        {
            EnsureBackgroundGraphic();
            EnsureForegroundGraphic();
            CacheReferences();

            _leftPadding = Mathf.Max(0f, _leftPadding);
            _rightPadding = Mathf.Max(0f, _rightPadding);
            _topPadding = Mathf.Max(0f, _topPadding);
            _bottomPadding = Mathf.Max(0f, _bottomPadding);
            _handleOffset = Mathf.Max(0f, _handleOffset);

            if (_maxValue < _minValue)
                _maxValue = _minValue;

            _value = ClampValue(_value);
            SetDirty();
        }

        private void OnRectTransformDimensionsChange()
        {
            SetDirty();
        }

        private void Update()
        {
            if (_isDirty)
                RefreshVisuals();
        }

        public void SetValueWithoutNotify(float newValue)
        {
            SetValue(newValue, notify: false);
        }

        public void SetNormalizedValueWithoutNotify(float normalized)
        {
            SetNormalizedValue(normalized, notify: false);
        }

        public void SetValue(float newValue)
        {
            SetValue(newValue, notify: true);
        }

        public void SetNormalizedValue(float normalized)
        {
            SetNormalizedValue(normalized, notify: true);
        }

        public void RefreshVisuals()
        {
            _isDirty = false;

            if (_background == null)
                return;

            if (_foreground == null)
                return;

            ClampAndNormalizeLayout();

            var backgroundRect = _background.rect;
            var backgroundWidth = backgroundRect.width;
            var backgroundHeight = backgroundRect.height;
            var normalized = normalizedValue;

            if (_backgroundGraphic != null)
            {
                _backgroundGraphic.direction = _direction;
                _backgroundGraphic.fillAmount = 1f;
                _backgroundGraphic.roundFullCaps = true;
            }

            if (_foregroundGraphic != null)
            {
                _foregroundGraphic.direction = _direction;
                _foregroundGraphic.fillAmount = normalized;
                _foregroundGraphic.roundFullCaps = true;
            }

            switch (_direction)
            {
                case Slider.Direction.LeftToRight:
                    ApplyHorizontal(normalized, backgroundWidth, backgroundHeight, leftToRight: true);
                    break;
                case Slider.Direction.RightToLeft:
                    ApplyHorizontal(normalized, backgroundWidth, backgroundHeight, leftToRight: false);
                    break;
                case Slider.Direction.BottomToTop:
                    ApplyVertical(normalized, backgroundWidth, backgroundHeight, bottomToTop: true);
                    break;
                case Slider.Direction.TopToBottom:
                    ApplyVertical(normalized, backgroundWidth, backgroundHeight, bottomToTop: false);
                    break;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!_interactable)
                return;

            UpdateValueFromPointer(eventData);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!_interactable)
                return;

            UpdateValueFromPointer(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_interactable)
                return;

            UpdateValueFromPointer(eventData);
        }

        public void Rebuild(CanvasUpdate executing)
        {
            if (executing == CanvasUpdate.PostLayout)
                RefreshVisuals();
        }

        public void LayoutComplete()
        {
        }

        public void GraphicUpdateComplete()
        {
        }

        public bool IsDestroyed()
        {
            return this == null;
        }

        private void CacheReferences()
        {
            if (_selfRect == null)
                _selfRect = transform as RectTransform;

            if (_background != null)
                _backgroundGraphic = _background.GetComponent<RoundedSliderFillGraphic>();

            if (_foreground != null)
                _foregroundGraphic = _foreground.GetComponent<RoundedSliderFillGraphic>();

            if (_backgroundGraphic != null)
                _backgroundGraphic.raycastTarget = true;

            if (_foregroundGraphic != null)
                _foregroundGraphic.raycastTarget = false;

            var handleGraphic = _handle != null ? _handle.GetComponent<Graphic>() : null;
            if (handleGraphic != null)
                handleGraphic.raycastTarget = false;
        }

        private void EnsureBackgroundGraphic()
        {
            if (_background == null)
                return;

            var legacyImage = _background.GetComponent<Image>();
            if (legacyImage != null)
                legacyImage.enabled = false;

            _backgroundGraphic = _background.GetComponent<RoundedSliderFillGraphic>();
            if (_backgroundGraphic == null)
                _backgroundGraphic = _background.gameObject.AddComponent<RoundedSliderFillGraphic>();
        }

        private void EnsureForegroundGraphic()
        {
            if (_foreground == null)
                return;

            var legacyImage = _foreground.GetComponent<Image>();
            if (legacyImage != null)
                legacyImage.enabled = false;

            _foregroundGraphic = _foreground.GetComponent<RoundedSliderFillGraphic>();
            if (_foregroundGraphic == null)
                _foregroundGraphic = _foreground.gameObject.AddComponent<RoundedSliderFillGraphic>();
        }

        private void SetDirty()
        {
            _isDirty = true;

            if (isActiveAndEnabled)
                CanvasUpdateRegistry.RegisterCanvasElementForLayoutRebuild(this);
        }

        private void ClampAndNormalizeLayout()
        {
            _minValue = float.IsNaN(_minValue) ? 0f : _minValue;
            _maxValue = float.IsNaN(_maxValue) ? 1f : _maxValue;
            if (_maxValue < _minValue)
                _maxValue = _minValue;
            _value = ClampValue(_value);
        }

        private float ClampValue(float input)
        {
            var clamped = Mathf.Clamp(input, _minValue, _maxValue);
            return _wholeNumbers ? Mathf.Round(clamped) : clamped;
        }

        private void SetValue(float newValue, bool notify)
        {
            var clamped = ClampValue(newValue);
            if (Mathf.Approximately(clamped, _value))
            {
                SetDirty();
                return;
            }

            _value = clamped;
            SetDirty();

            if (isActiveAndEnabled)
                RefreshVisuals();

            if (notify)
                _onValueChanged?.Invoke(_value);
        }

        private void SetNormalizedValue(float normalized, bool notify)
        {
            normalized = Mathf.Clamp01(normalized);
            var valueRange = _maxValue - _minValue;
            var newValue = _minValue + valueRange * normalized;
            SetValue(newValue, notify);
        }

        private void ApplyHorizontal(float normalized, float backgroundWidth, float backgroundHeight, bool leftToRight)
        {
            var usableWidth = Mathf.Max(0f, backgroundWidth - _leftPadding - _rightPadding);
            var fillWidth = usableWidth * normalized;
            var fillHeight = Mathf.Max(0f, backgroundHeight - _topPadding - _bottomPadding);
            var handleX = leftToRight ? _leftPadding + fillWidth : backgroundWidth - _rightPadding - fillWidth;
            DriveHandleHorizontal(handleX, fillHeight);
        }

        private void ApplyVertical(float normalized, float backgroundWidth, float backgroundHeight, bool bottomToTop)
        {
            var fillWidth = Mathf.Max(0f, backgroundWidth - _leftPadding - _rightPadding);
            var usableHeight = Mathf.Max(0f, backgroundHeight - _topPadding - _bottomPadding);
            var fillHeight = usableHeight * normalized;
            var handleY = bottomToTop ? _bottomPadding + fillHeight : backgroundHeight - _topPadding - fillHeight;
            DriveHandleVertical(handleY, fillWidth);
        }

        private void DriveHandleHorizontal(float edgeX, float fillHeight)
        {
            if (!_driveHandle || _handle == null)
                return;

            var handleRect = _handle.rect;
            var handleWidth = handleRect.width;
            var handleHeight = handleRect.height > 0f ? handleRect.height : Mathf.Max(0f, fillHeight);
            var centerY = (_bottomPadding - _topPadding) * 0.5f;
            var left = edgeX - handleWidth * 0.5f + _handleOffset;
            SetRect(_handle, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(handleWidth, handleHeight),
                new Vector2(left, centerY));
        }

        private void DriveHandleVertical(float edgeY, float fillWidth)
        {
            if (!_driveHandle || _handle == null)
                return;

            var handleRect = _handle.rect;
            var handleWidth = handleRect.width > 0f ? handleRect.width : Mathf.Max(0f, fillWidth);
            var handleHeight = handleRect.height;
            var centerX = (_leftPadding - _rightPadding) * 0.5f;
            var bottom = edgeY - handleHeight * 0.5f + _handleOffset;
            SetRect(_handle, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(handleWidth, handleHeight),
                new Vector2(centerX, bottom));
        }

        private void SetRect(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta, Vector2 anchoredPosition)
        {
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.pivot = anchorMin == anchorMax ? anchorMin : rectTransform.pivot;
            rectTransform.sizeDelta = sizeDelta;
            rectTransform.anchoredPosition = anchoredPosition;
        }

        private void UpdateValueFromPointer(PointerEventData eventData)
        {
            if (_background == null)
                return;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_background, eventData.position,
                    eventData.pressEventCamera, out var localPoint))
            {
                return;
            }

            var rect = _background.rect;
            var x = localPoint.x + rect.width * _background.pivot.x;
            var y = localPoint.y + rect.height * _background.pivot.y;

            float normalized;
            switch (_direction)
            {
                case Slider.Direction.LeftToRight:
                    normalized = Mathf.Clamp01((x - _leftPadding) / Mathf.Max(0.0001f, rect.width - _leftPadding - _rightPadding));
                    break;
                case Slider.Direction.RightToLeft:
                    normalized = Mathf.Clamp01(1f - (x - _leftPadding) / Mathf.Max(0.0001f, rect.width - _leftPadding - _rightPadding));
                    break;
                case Slider.Direction.BottomToTop:
                    normalized = Mathf.Clamp01((y - _bottomPadding) / Mathf.Max(0.0001f, rect.height - _topPadding - _bottomPadding));
                    break;
                case Slider.Direction.TopToBottom:
                    normalized = Mathf.Clamp01(1f - (y - _bottomPadding) / Mathf.Max(0.0001f, rect.height - _topPadding - _bottomPadding));
                    break;
                default:
                    normalized = 0f;
                    break;
            }

            SetNormalizedValue(normalized);
        }
    }
}
