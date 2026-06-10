using UnityEngine;
using UnityEngine.UI;

namespace ENP.UnityExtensions.Runtime.Scripts.UI
{
    /// <summary>
    /// Drives a slider-like bar by resizing a rounded fill instead of cropping it.
    /// This keeps the rounded cap visible while the value changes.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("UI/Rounded Slider View")]
    [RequireComponent(typeof(Slider))]
    public sealed class RoundedSliderView : MonoBehaviour
    {
        [SerializeField] private Slider _slider;
        [SerializeField] private RectTransform _track;
        [SerializeField] private RectTransform _fill;
        [SerializeField] private RectTransform _handle;

        [Header("Padding")]
        [SerializeField, Min(0f)] private float _leftPadding;
        [SerializeField, Min(0f)] private float _rightPadding;
        [SerializeField, Min(0f)] private float _topPadding;
        [SerializeField, Min(0f)] private float _bottomPadding;

        [Header("Handle")]
        [SerializeField] private bool _driveHandle = true;
        [SerializeField, Min(0f)] private float _handleOffset;

        private bool _isSubscribed;

        private void Reset()
        {
            _slider = GetComponent<Slider>();
            _track = transform as RectTransform;
        }

        private void Awake()
        {
            CacheRequiredReferences();
        }

        private void OnEnable()
        {
            CacheRequiredReferences();
            Subscribe();
            RefreshVisuals();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void OnValidate()
        {
            CacheRequiredReferences();
            ClampPadding();

            if (!isActiveAndEnabled)
                return;

            RefreshVisuals();
        }

        private void OnRectTransformDimensionsChange()
        {
            if (isActiveAndEnabled)
                RefreshVisuals();
        }

        private void CacheRequiredReferences()
        {
            if (_slider == null)
                _slider = GetComponent<Slider>();

            if (_track == null)
                _track = transform as RectTransform;

            Debug.Assert(_slider != null, "RoundedSliderView requires a Slider reference.");
            Debug.Assert(_track != null, "RoundedSliderView requires a RectTransform track.");
            Debug.Assert(_fill != null, "RoundedSliderView requires a fill RectTransform.");
        }

        private void ClampPadding()
        {
            _leftPadding = Mathf.Max(0f, _leftPadding);
            _rightPadding = Mathf.Max(0f, _rightPadding);
            _topPadding = Mathf.Max(0f, _topPadding);
            _bottomPadding = Mathf.Max(0f, _bottomPadding);
            _handleOffset = Mathf.Max(0f, _handleOffset);
        }

        private void Subscribe()
        {
            if (_isSubscribed || _slider == null)
                return;

            _slider.onValueChanged.AddListener(OnSliderValueChanged);
            _isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_isSubscribed || _slider == null)
                return;

            _slider.onValueChanged.RemoveListener(OnSliderValueChanged);
            _isSubscribed = false;
        }

        private void OnSliderValueChanged(float _)
        {
            RefreshVisuals();
        }

        public void RefreshVisuals()
        {
            if (_slider == null || _track == null || _fill == null)
                return;

            var direction = _slider.direction;
            var value = Mathf.InverseLerp(_slider.minValue, _slider.maxValue, _slider.value);

            switch (direction)
            {
                case Slider.Direction.LeftToRight:
                    ApplyHorizontal(value, leftToRight: true);
                    break;
                case Slider.Direction.RightToLeft:
                    ApplyHorizontal(value, leftToRight: false);
                    break;
                case Slider.Direction.BottomToTop:
                    ApplyVertical(value, bottomToTop: true);
                    break;
                case Slider.Direction.TopToBottom:
                    ApplyVertical(value, bottomToTop: false);
                    break;
            }
        }

        private void ApplyHorizontal(float value, bool leftToRight)
        {
            var trackRect = _track.rect;
            var trackWidth = trackRect.width;
            var usableWidth = Mathf.Max(0f, trackWidth - _leftPadding - _rightPadding);
            var fillWidth = usableWidth * value;
            var handleWidth = _handle != null ? _handle.rect.width : 0f;

            if (leftToRight)
            {
                ResizeFillHorizontal(_leftPadding, fillWidth);
                DriveHandleHorizontal(_leftPadding + fillWidth, handleWidth);
            }
            else
            {
                var fillLeft = _leftPadding + (usableWidth - fillWidth);
                ResizeFillHorizontal(fillLeft, fillWidth);
                DriveHandleHorizontal(fillLeft, handleWidth);
            }
        }

        private void ApplyVertical(float value, bool bottomToTop)
        {
            var trackRect = _track.rect;
            var trackHeight = trackRect.height;
            var usableHeight = Mathf.Max(0f, trackHeight - _topPadding - _bottomPadding);
            var fillHeight = usableHeight * value;
            var handleHeight = _handle != null ? _handle.rect.height : 0f;

            if (bottomToTop)
            {
                ResizeFillVertical(_bottomPadding, fillHeight);
                DriveHandleVertical(_bottomPadding + fillHeight, handleHeight);
            }
            else
            {
                var fillBottom = _bottomPadding + (usableHeight - fillHeight);
                ResizeFillVertical(fillBottom, fillHeight);
                DriveHandleVertical(fillBottom, handleHeight);
            }
        }

        private void ResizeFillHorizontal(float leftInset, float width)
        {
            _fill.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, leftInset, width);
        }

        private void ResizeFillVertical(float bottomInset, float height)
        {
            _fill.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Bottom, bottomInset, height);
        }

        private void DriveHandleHorizontal(float fillEdge, float handleWidth)
        {
            if (!_driveHandle || _handle == null)
                return;

            var inset = fillEdge - handleWidth * 0.5f + _handleOffset;
            _handle.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, inset, handleWidth);
        }

        private void DriveHandleVertical(float fillEdge, float handleHeight)
        {
            if (!_driveHandle || _handle == null)
                return;

            var inset = fillEdge - handleHeight * 0.5f + _handleOffset;
            _handle.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Bottom, inset, handleHeight);
        }
    }
}
