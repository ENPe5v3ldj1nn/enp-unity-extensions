using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace ENP.UnityExtensions.Runtime
{
    [RequireComponent(typeof(CanvasGroup), typeof(RectTransform))]
    public class AnimatedWindow : MonoBehaviour
    {
        [Tooltip("Optional per-window override. When empty, WindowConfig.Default is used.")]
        [SerializeField] private WindowConfig _config;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private RectTransform _rect;

        private Vector2 _basePosition;
        private Vector3 _baseScale;
        private float _baseRotationZ;
        private bool _baseCaptured;
        private Sequence _activeSequence;
        private int _opGeneration;

        private void OnValidate()
        {
            if (_canvasGroup == null)
                _canvasGroup = GetComponent<CanvasGroup>();
            if (_rect == null)
                _rect = GetComponent<RectTransform>();
        }

        public UniTask OpenAsync(WindowTransition transition, CancellationToken token = default)
        {
            return PlayAsync(transition, enter: true, token);
        }

        public UniTask CloseAsync(WindowTransition transition, CancellationToken token = default)
        {
            return PlayAsync(transition, enter: false, token);
        }

        private async UniTask PlayAsync(WindowTransition transition, bool enter, CancellationToken token)
        {
            CaptureBase();
            KillActiveSequence();

            var config = _config != null ? _config : WindowConfig.Default;
            if (config == null)
                throw new InvalidOperationException(
                    $"{name}: no WindowConfig assigned and WindowConfig.Default is not set.");

            var recipe = enter ? config.Get(transition).enter : config.Get(transition).exit;
            var id = ++_opGeneration;

            gameObject.SetActive(true);

            var hasScale = recipe.scale != Vector3.zero;
            var hasRotation = Mathf.Abs(recipe.rotation) > 0.01f;

            var hiddenScale = hasScale ? recipe.scale : _baseScale;
            var hiddenRotZ = _baseRotationZ + recipe.rotation;

            var fromPos = enter ? _basePosition + recipe.offset : _basePosition;
            var toPos = enter ? _basePosition : _basePosition + recipe.offset;
            var fromScale = enter ? hiddenScale : _baseScale;
            var toScale = enter ? _baseScale : hiddenScale;
            var fromRotZ = enter ? hiddenRotZ : _baseRotationZ;
            var toRotZ = enter ? _baseRotationZ : hiddenRotZ;
            var fromAlpha = enter ? recipe.alpha : 1f;
            var toAlpha = enter ? 1f : recipe.alpha;

            _rect.anchoredPosition = fromPos;
            if (hasScale) _rect.localScale = fromScale;
            if (hasRotation) _rect.localEulerAngles = new Vector3(0f, 0f, fromRotZ);
            _canvasGroup.alpha = fromAlpha;
            _canvasGroup.blocksRaycasts = enter;

            var sequence = DOTween.Sequence();
            if (recipe.delay > 0f)
                sequence.SetDelay(recipe.delay);

            sequence.Join(WithEase(_rect.DOAnchorPos(toPos, recipe.duration), recipe));
            sequence.Join(WithEase(_canvasGroup.DOFade(toAlpha, recipe.duration), recipe));
            if (hasScale)
                sequence.Join(WithEase(_rect.DOScale(toScale, recipe.duration), recipe));
            if (hasRotation)
                sequence.Join(WithEase(_rect.DOLocalRotate(new Vector3(0f, 0f, toRotZ), recipe.duration), recipe));

            _activeSequence = sequence;

            var completion = new UniTaskCompletionSource();
            sequence.OnComplete(() => completion.TrySetResult());
            sequence.OnKill(() => completion.TrySetResult());

            await using (token.Register(() => sequence.Kill()))
                await completion.Task;

            if (id != _opGeneration || token.IsCancellationRequested)
                return;

            _activeSequence = null;

            if (!enter)
                gameObject.SetActive(false);
        }

        private static Tween WithEase(Tween tween, in WindowConfig.Recipe recipe)
        {
            return recipe.curve != null && recipe.curve.length > 0
                ? tween.SetEase(recipe.curve)
                : tween.SetEase(recipe.ease);
        }

        private void CaptureBase()
        {
            if (_baseCaptured)
                return;

            _basePosition = _rect.anchoredPosition;
            _baseScale = _rect.localScale;
            _baseRotationZ = _rect.localEulerAngles.z;
            _baseCaptured = true;
        }

        private void KillActiveSequence()
        {
            if (_activeSequence == null)
                return;

            _activeSequence.Kill();
            _activeSequence = null;
        }

        private void OnDestroy()
        {
            KillActiveSequence();
        }
    }
}
