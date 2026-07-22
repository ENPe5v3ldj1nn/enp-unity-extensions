using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

namespace ENP.UnityExtensions.Runtime
{
    [RequireComponent(typeof(CanvasGroup), typeof(RectTransform))]
    public class AnimatedWindow : MonoBehaviour
    {
        [SerializeField] private WindowAnimationConfig _config;
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

        // --- Async API (primary) ---

        public UniTask OpenAsync(AnimatedWindowAnimation anim, CancellationToken token = default)
        {
            return PlayAsync(anim, deactivateOnEnd: false, token);
        }

        public UniTask CloseAsync(AnimatedWindowAnimation anim, CancellationToken token = default)
        {
            return PlayAsync(anim, deactivateOnEnd: true, token);
        }

        // --- Backward-compatible fire-and-forget API ---

        public void Open(AnimatedWindowAnimation anim)
        {
            OpenAsync(anim).Forget();
        }

        public void Close(AnimatedWindowAnimation anim, UnityAction onComplete)
        {
            CloseThenInvoke(anim, onComplete).Forget();
        }

        public void Open(string animName)
        {
            Open(Parse(animName));
        }

        public void Close(string animName, UnityAction onComplete)
        {
            Close(Parse(animName), onComplete);
        }

        private async UniTaskVoid CloseThenInvoke(AnimatedWindowAnimation anim, UnityAction onComplete)
        {
            await CloseAsync(anim);
            onComplete?.Invoke();
        }

        private async UniTask PlayAsync(AnimatedWindowAnimation anim, bool deactivateOnEnd, CancellationToken token)
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

            // Superseded by a newer op or cancelled — leave state to the winning op.
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

        private static AnimatedWindowAnimation Parse(string animName)
        {
            return (AnimatedWindowAnimation)Enum.Parse(typeof(AnimatedWindowAnimation), animName);
        }
    }
}
