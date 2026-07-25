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

        private void Awake()
        {
            CacheReferences();
        }

        private void OnEnable()
        {
            CacheReferences();
            _restScale = _rectTransform.localScale;
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
            SetScale(_restScale * _button.PressedScale);
        }

        private void HandleReleased()
        {
            SetScale(_restScale);
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
            Debug.Assert(_button != null, "AnimatedButtonAnimation requires an AnimatedButton.");
            Debug.Assert(_rectTransform != null, "AnimatedButtonAnimation requires a RectTransform.");
        }
    }
}
