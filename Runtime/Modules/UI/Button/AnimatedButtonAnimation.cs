using DG.Tweening;
using UnityEngine;

namespace ENP.UnityExtensions.Runtime
{
    [AddComponentMenu("UI/Animated Button Animation")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class AnimatedButtonAnimation : MonoBehaviour
    {
        private AnimatedButton _button;
        private RectTransform _rectTransform;
        private Tween _scaleTween;
        private Vector3 _restScale;
        private bool _restScaleCaptured;

        private void Awake()
        {
            CacheReferences();
        }

        private void OnEnable()
        {
            CacheReferences();
            _button.Pressed += HandlePressed;
            _button.Released += HandleReleased;
        }

        private void OnDisable()
        {
            _button.Pressed -= HandlePressed;
            _button.Released -= HandleReleased;
            ResetScale();
        }

        private void OnValidate()
        {
            CacheReferences();
        }

        private void HandlePressed()
        {
            EnsureRestScale();
            SetScale(_restScale * _button.PressedScale);
        }

        private void HandleReleased()
        {
            EnsureRestScale();
            SetScale(_restScale);
        }

        // Captured lazily on first use rather than in Awake/OnEnable: those can run before layout,
        // spawn animations or other startup scripts have settled the transform into its true rest
        // pose, baking in a wrong value forever. By the time a press is physically possible, the UI
        // has necessarily already settled, so this is the first point we can trust.
        private void EnsureRestScale()
        {
            if (_restScaleCaptured)
                return;

            _restScaleCaptured = true;
            _restScale = _rectTransform.localScale;
        }

        private void SetScale(Vector3 targetScale)
        {
            KillTween();

            if (_button.UseAnimation)
            {
                _scaleTween = _rectTransform.DOScale(targetScale, _button.AnimationDuration);
                return;
            }

            _rectTransform.localScale = targetScale;
        }

        private void ResetScale()
        {
            if (!_restScaleCaptured)
                return;

            KillTween();
            _rectTransform.localScale = _restScale;
        }

        private void KillTween()
        {
            if (_scaleTween == null)
            {
                return;
            }

            _scaleTween.Kill();
            _scaleTween = null;
        }

        private void CacheReferences()
        {
            _button = GetComponent<AnimatedButton>();
            _rectTransform = GetComponent<RectTransform>();
            UnityEngine.Debug.Assert(_button != null, "AnimatedButtonAnimation requires an AnimatedButton.");
            UnityEngine.Debug.Assert(_rectTransform != null, "AnimatedButtonAnimation requires a RectTransform.");
        }
    }
}
