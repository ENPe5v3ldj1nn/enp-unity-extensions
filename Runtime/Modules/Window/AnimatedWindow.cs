using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace ENP.UnityExtensions.Runtime
{
    [RequireComponent(typeof(CanvasGroup), typeof(RectTransform))]
    public class AnimatedWindow : MonoBehaviour
    {
        [SerializeField] private WindowConfig _config;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private RectTransform _rect;

        private Vector2 _basePosition;
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

        public UniTask OpenAsync(WindowAnimation anim, CancellationToken token = default)
        {
            return PlayAsync(anim, deactivateOnEnd: false, token);
        }

        public UniTask CloseAsync(WindowAnimation anim, CancellationToken token = default)
        {
            return PlayAsync(anim, deactivateOnEnd: true, token);
        }

        private async UniTask PlayAsync(WindowAnimation anim, bool deactivateOnEnd, CancellationToken token)
        {
            CaptureBase();
            KillActiveSequence();

            var recipe = _config.Get(anim);
            var id = ++_opGeneration;

            gameObject.SetActive(true);
            _rect.anchoredPosition = _basePosition + recipe.startOffset;
            _canvasGroup.alpha = recipe.fromAlpha;
            _canvasGroup.blocksRaycasts = !deactivateOnEnd;

            var sequence = DOTween.Sequence();
            sequence.Join(_rect.DOAnchorPos(_basePosition + recipe.endOffset, recipe.duration).SetEase(recipe.ease));
            sequence.Join(_canvasGroup.DOFade(recipe.toAlpha, recipe.duration));
            _activeSequence = sequence;

            var completion = new UniTaskCompletionSource();
            sequence.OnComplete(() => completion.TrySetResult());
            sequence.OnKill(() => completion.TrySetResult());

            await using (token.Register(() => sequence.Kill()))
                await completion.Task;

            if (id != _opGeneration || token.IsCancellationRequested)
                return;

            _activeSequence = null;

            if (deactivateOnEnd)
                gameObject.SetActive(false);
        }

        private void CaptureBase()
        {
            if (_baseCaptured)
                return;

            _basePosition = _rect.anchoredPosition;
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
