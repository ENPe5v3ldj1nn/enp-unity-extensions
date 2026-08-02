using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ENP.UnityExtensions.Runtime
{
    /// <summary>
    /// Alternative to <see cref="ContentSizeFitter"/> that fits the RectTransform to its content
    /// exactly once instead of on every layout rebuild. Fits on the first OnEnable, and again on
    /// <see cref="IWindowVisibilityAware.OnWindowShown"/> if hosted under an AnimatedWindow with
    /// Canvas hide mode (where OnEnable won't refire). Call <see cref="ForceRecalculate"/> when the
    /// content genuinely changes after the initial fit.
    /// In the Unity Editor (UNITY_EDITOR) this instead behaves exactly like the stock
    /// <see cref="ContentSizeFitter"/> — recalculating continuously via the layout rebuild system —
    /// so authoring/tweaking content feels normal. Builds always use the fit-once behavior above.
    /// </summary>
    [AddComponentMenu("Layout/Fit Once Content Size Fitter")]
    [RequireComponent(typeof(RectTransform))]
#if UNITY_EDITOR
    public sealed class FitOnceContentSizeFitter : UIBehaviour, IWindowVisibilityAware, ILayoutSelfController
#else
    public sealed class FitOnceContentSizeFitter : UIBehaviour, IWindowVisibilityAware
#endif
    {
        [SerializeField] private ContentSizeFitter.FitMode _horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        [SerializeField] private ContentSizeFitter.FitMode _verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        private RectTransform _rectTransform;
        private bool _sized;

        public ContentSizeFitter.FitMode HorizontalFit { get => _horizontalFit; set => _horizontalFit = value; }
        public ContentSizeFitter.FitMode VerticalFit { get => _verticalFit; set => _verticalFit = value; }

        public void ForceRecalculate()
        {
            _sized = false;
            TryFit();
        }

        void IWindowVisibilityAware.OnWindowShown() => TryFit();

        void IWindowVisibilityAware.OnWindowHidden() { }

        protected override void OnEnable()
        {
            base.OnEnable();
#if UNITY_EDITOR
            SetDirty();
#else
            TryFit();
#endif
        }

        private void TryFit()
        {
            if (_sized) return;
            _sized = true;
            Recalculate();
        }

        private void Recalculate()
        {
            if (_rectTransform == null) _rectTransform = (RectTransform)transform;

            SetSize(RectTransform.Axis.Horizontal, _horizontalFit);
            SetSize(RectTransform.Axis.Vertical, _verticalFit);

            if (_rectTransform.parent is RectTransform parentRect)
                LayoutRebuilder.MarkLayoutForRebuild(parentRect);
        }

        private void SetSize(RectTransform.Axis axis, ContentSizeFitter.FitMode fitMode)
        {
            if (fitMode == ContentSizeFitter.FitMode.Unconstrained) return;

            float size = fitMode == ContentSizeFitter.FitMode.MinSize
                ? LayoutUtility.GetMinSize(_rectTransform, (int)axis)
                : LayoutUtility.GetPreferredSize(_rectTransform, (int)axis);

            _rectTransform.SetSizeWithCurrentAnchors(axis, size);
        }

#if UNITY_EDITOR
        // Editor-only: mirrors ContentSizeFitter's own hookup into the layout rebuild system,
        // so the component recalculates continuously while authoring, instead of only once.
        public void SetLayoutHorizontal()
        {
            if (_rectTransform == null) _rectTransform = (RectTransform)transform;
            SetSize(RectTransform.Axis.Horizontal, _horizontalFit);
        }

        public void SetLayoutVertical()
        {
            if (_rectTransform == null) _rectTransform = (RectTransform)transform;
            SetSize(RectTransform.Axis.Vertical, _verticalFit);
        }

        protected override void OnDisable()
        {
            SetDirty();
            base.OnDisable();
        }

        protected override void OnRectTransformDimensionsChange()
        {
            SetDirty();
        }

        protected override void OnBeforeTransformParentChanged()
        {
            SetDirty();
        }

        private void SetDirty()
        {
            if (!IsActive()) return;
            if (_rectTransform == null) _rectTransform = (RectTransform)transform;
            LayoutRebuilder.MarkLayoutForRebuild(_rectTransform);
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            SetDirty();
        }
#endif
    }
}
