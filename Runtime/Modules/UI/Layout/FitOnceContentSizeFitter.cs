using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ENP.UnityExtensions.Runtime
{
    /// <summary>
    /// Alternative to <see cref="ContentSizeFitter"/> that fits the RectTransform to its content on
    /// specific triggers (OnEnable, <see cref="IWindowVisibilityAware.OnWindowShown"/>, or an explicit
    /// <see cref="ForceRecalculate"/> call) instead of on every layout rebuild. AnimatedWindow's
    /// Canvas/GameObject hide modes keep the window's GameObject alive after the first activation, so
    /// OnEnable won't refire on later shows — OnWindowShown is what re-fits then, and both paths
    /// always recalculate (no "already sized" guard), so each real show gets a correct fit even if an
    /// earlier one ran before layout/content had settled.
    /// In the Unity Editor's Edit Mode this instead behaves exactly like the stock
    /// <see cref="ContentSizeFitter"/> — recalculating continuously via the layout rebuild system —
    /// so authoring/tweaking content feels normal. Play Mode in the Editor and actual builds both
    /// use the trigger-based behavior above, so testing in the Editor matches what ships.
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
        private RectTransform TargetRect => _rectTransform != null ? _rectTransform : _rectTransform = (RectTransform)transform;

        public ContentSizeFitter.FitMode HorizontalFit { get => _horizontalFit; set => _horizontalFit = value; }
        public ContentSizeFitter.FitMode VerticalFit { get => _verticalFit; set => _verticalFit = value; }

        public void ForceRecalculate() => Recalculate();

        void IWindowVisibilityAware.OnWindowShown() => Recalculate();

        void IWindowVisibilityAware.OnWindowHidden() { }

        protected override void OnEnable()
        {
            base.OnEnable();
#if UNITY_EDITOR
            // Edit Mode keeps the continuous authoring behavior; Play Mode in the Editor should
            // behave exactly like a build (trigger-based fit) so testing in the Editor matches what ships.
            if (Application.isPlaying)
            {
                Recalculate();
            }
            else
            {
                SetDirty();
            }
#else
            Recalculate();
#endif
        }

        private void Recalculate()
        {
            SetSize(RectTransform.Axis.Horizontal, _horizontalFit);
            SetSize(RectTransform.Axis.Vertical, _verticalFit);

            if (TargetRect.parent is RectTransform parentRect)
                LayoutRebuilder.MarkLayoutForRebuild(parentRect);
        }

        private void SetSize(RectTransform.Axis axis, ContentSizeFitter.FitMode fitMode)
        {
            if (fitMode == ContentSizeFitter.FitMode.Unconstrained) return;

            float size = fitMode == ContentSizeFitter.FitMode.MinSize
                ? LayoutUtility.GetMinSize(TargetRect, (int)axis)
                : LayoutUtility.GetPreferredSize(TargetRect, (int)axis);

            TargetRect.SetSizeWithCurrentAnchors(axis, size);
        }

#if UNITY_EDITOR
        // Editor-only: mirrors ContentSizeFitter's own hookup into the layout rebuild system,
        // so the component recalculates continuously while authoring, instead of only once.
        public void SetLayoutHorizontal()
        {
            // Play Mode in the Editor should stay fit-once, not participate in continuous layout
            // rebuilds — Unity's layout system still calls this via ILayoutSelfController otherwise.
            if (Application.isPlaying) return;
            SetSize(RectTransform.Axis.Horizontal, _horizontalFit);
        }

        public void SetLayoutVertical()
        {
            if (Application.isPlaying) return;
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
            // Guards OnDisable/OnRectTransformDimensionsChange/OnBeforeTransformParentChanged/
            // OnValidate all at once — none of them should trigger continuous rebuilds in Play Mode.
            if (Application.isPlaying) return;
            if (!IsActive()) return;
            LayoutRebuilder.MarkLayoutForRebuild(TargetRect);
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            SetDirty();
        }
#endif
    }
}
