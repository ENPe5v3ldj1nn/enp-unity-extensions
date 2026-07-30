using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace ENP.UnityExtensions.Runtime
{
    public enum WindowHideMode
    {
        // Toggles the window's own Canvas (+ GraphicRaycaster) instead of the GameObject:
        // no OnEnable/OnDisable churn and no canvas rebuild on show. GameObject stays alive,
        // so pause per-frame work in OnHidden. Falls back to GameObject when no Canvas exists.
        Canvas,
        // Toggles the CanvasGroup (alpha/raycasts) instead of the GameObject: GameObject also
        // stays alive here, avoiding a SetActive(true) -> stale-layout race on first open.
        GameObject
    }

    [RequireComponent(typeof(CanvasGroup), typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster))]
    public class AnimatedWindow : MonoBehaviour
    {
        [Tooltip("Optional per-window override. When empty, WindowConfig.Default is used.")]
        [SerializeField] private WindowConfig _config;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private RectTransform _rect;

        [Header("Hiding")]
        [SerializeField] private WindowHideMode _hideMode = WindowHideMode.Canvas;
        [SerializeField] private Canvas _canvas;
        [SerializeField] private GraphicRaycaster _raycaster;

        [Header("Optimization")]
        [Tooltip("Enable when the window's IWindowVisibilityAware children never change after the first show (no runtime instantiation/destruction). Caches the GetComponentsInChildren scan instead of repeating it on every show/hide.")]
        [SerializeField] private bool _staticHierarchy;

        private Vector2 _basePosition;
        private Vector3 _baseScale;
        private float _baseRotationZ;
        private bool _baseCaptured;
        private bool _initialized;
        private Sequence _activeSequence;
        private int _opGeneration;
        private IWindowVisibilityAware[] _cachedAware;

        private bool UseCanvasHiding => _hideMode == WindowHideMode.Canvas && _canvas != null;

        private void OnValidate()
        {
            if (_canvasGroup == null)
                _canvasGroup = GetComponent<CanvasGroup>();
            if (_rect == null)
                _rect = GetComponent<RectTransform>();
            if (_canvas == null)
                _canvas = GetComponent<Canvas>();
            if (_raycaster == null)
                _raycaster = GetComponent<GraphicRaycaster>();
        }

        public UniTask OpenAsync(WindowTransition transition, SlideDirection direction = SlideDirection.Right, CancellationToken token = default)
        {
            return PlayAsync(transition, enter: true, direction, token);
        }

        public UniTask CloseAsync(WindowTransition transition, SlideDirection direction = SlideDirection.Right, CancellationToken token = default)
        {
            return PlayAsync(transition, enter: false, direction, token);
        }

        public void HideImmediate()
        {
            KillActiveSequence();
            SetVisible(false);
        }

        // Called once by the controller after discovery, before the first show.
        internal void InitializeWindow()
        {
            if (_initialized)
                return;

            _initialized = true;
            OnInitialize();
        }

        protected virtual void OnInitialize() { }
        protected virtual void OnShown() { }
        protected virtual void OnHidden() { }

        private async UniTask PlayAsync(WindowTransition transition, bool enter, SlideDirection direction, CancellationToken token)
        {
            CaptureBase();
            KillActiveSequence();

            var config = _config != null ? _config : WindowConfig.Default;
            if (config == null)
                throw new InvalidOperationException(
                    $"{name}: no WindowConfig assigned and WindowConfig.Default is not set.");

            var recipe = enter ? config.Get(transition).enter : config.Get(transition).exit;
            var id = ++_opGeneration;

            var hasScale = recipe.scale != Vector3.zero;
            var hasRotation = Mathf.Abs(recipe.rotation) > 0.01f;

            var hiddenScale = hasScale ? recipe.scale : _baseScale;
            var hiddenRotZ = _baseRotationZ + recipe.rotation;

            var dirSign = direction == SlideDirection.Left ? -1f : 1f;
            var resolvedOffset = new Vector2(recipe.offset.x * dirSign * _rect.rect.width, recipe.offset.y * _rect.rect.height);
            var fromPos = enter ? _basePosition + resolvedOffset : _basePosition;
            var toPos = enter ? _basePosition : _basePosition + resolvedOffset;
            var fromScale = enter ? hiddenScale : _baseScale;
            var toScale = enter ? _baseScale : hiddenScale;
            var fromRotZ = enter ? hiddenRotZ : _baseRotationZ;
            var toRotZ = enter ? _baseRotationZ : hiddenRotZ;
            var fromAlpha = enter ? recipe.alpha : 1f;
            var toAlpha = enter ? 1f : recipe.alpha;

            // Apply the start state while still hidden, then reveal — avoids a one-frame flash.
            _rect.anchoredPosition = fromPos;
            if (hasScale) _rect.localScale = fromScale;
            if (hasRotation) _rect.localEulerAngles = new Vector3(0f, 0f, fromRotZ);
            _canvasGroup.alpha = fromAlpha;
            _canvasGroup.blocksRaycasts = enter;

            if (enter)
            {
                SetVisible(true);
                OnShown();
            }

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
            {
                SetVisible(false);
                OnHidden();
            }
        }

        private void SetVisible(bool visible)
        {
            if (UseCanvasHiding)
            {
                if (visible && !gameObject.activeSelf)
                    gameObject.SetActive(true);

                // GameObject stays active in this mode, so nothing ever fires OnEnable/OnDisable
                // again after the first real activation. Notify IWindowVisibilityAware children
                // explicitly so they can do what they'd normally do there (e.g. a button mid-press
                // releasing before the window hides, a scroll view resetting its drag state on show).
                if (!visible)
                    NotifyVisibilityAware(false);

                _canvas.enabled = visible;
                if (_raycaster != null)
                    _raycaster.enabled = visible;

                if (visible)
                    NotifyVisibilityAware(true);
            }
            else
            {
                if (!gameObject.activeSelf)
                    gameObject.SetActive(true);

                if (!visible)
                    NotifyVisibilityAware(false);

                _canvasGroup.alpha = visible ? 1f : 0f;
                _canvasGroup.blocksRaycasts = visible;
                _canvasGroup.interactable = visible;

                if (visible)
                    NotifyVisibilityAware(true);
            }
        }

        private void NotifyVisibilityAware(bool visible)
        {
            IWindowVisibilityAware[] aware;
            if (_staticHierarchy)
            {
                if (_cachedAware == null)
                    _cachedAware = GetComponentsInChildren<IWindowVisibilityAware>(includeInactive: false);
                aware = _cachedAware;
            }
            else
            {
                aware = GetComponentsInChildren<IWindowVisibilityAware>(includeInactive: false);
            }

            for (int i = 0; i < aware.Length; i++)
            {
                if (visible)
                    aware[i].OnWindowShown();
                else
                    aware[i].OnWindowHidden();
            }
        }

        private static Tween WithEase(Tween tween, in WindowConfig.Recipe recipe)
        {
            return tween.SetEase(recipe.ease);
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
