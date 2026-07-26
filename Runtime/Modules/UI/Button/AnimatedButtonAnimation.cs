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
            // Captured once, before any interaction is possible, so it always reflects the
            // authored rest pose. Re-capturing on every OnEnable is what let the scale compound:
            // if the component got re-enabled while a press tween hadn't finished, that shrunk
            // value would get baked in as the new "rest" and each press would shrink it further.
            _restScale = _rectTransform.localScale;
            Debug.Log($"[BTNANIM {name}] Awake restScale={_restScale} f={Time.frameCount}");
        }

        private void OnEnable()
        {
            CacheReferences();
            Debug.Log($"[BTNANIM {name}] OnEnable restScale={_restScale} currentScale={_rectTransform.localScale} f={Time.frameCount}");
            _button.Pressed += HandlePressed;
            _button.Released += HandleReleased;
        }

        private void OnDisable()
        {
            Debug.Log($"[BTNANIM {name}] OnDisable currentScale={_rectTransform.localScale} f={Time.frameCount}");
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
            Debug.Log($"[BTNANIM {name}] HandlePressed restScale={_restScale} currentScale={_rectTransform.localScale} target={_restScale * _button.PressedScale} f={Time.frameCount}");
            SetScale(_restScale * _button.PressedScale);
        }

        private void HandleReleased()
        {
            Debug.Log($"[BTNANIM {name}] HandleReleased restScale={_restScale} currentScale={_rectTransform.localScale} f={Time.frameCount}");
            SetScale(_restScale);
        }

        private void SetScale(Vector3 targetScale)
        {
            KillTween();

            if (_button.UseAnimation)
            {
                _scaleTween = _rectTransform.DOScale(targetScale, _button.AnimationDuration);
                _scaleTween.OnComplete(() => Debug.Log($"[BTNANIM {name}] tween complete finalScale={_rectTransform.localScale} f={Time.frameCount}"));
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
