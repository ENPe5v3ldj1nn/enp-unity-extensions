namespace ENP.UnityExtensions.Runtime
{
    /// <summary>
    /// Implemented by a child component that needs to react when its hosting
    /// <see cref="AnimatedWindow"/> becomes visible/hidden. Under Canvas hide mode the window's
    /// GameObject stays active, so Unity's own OnEnable/OnDisable never fire again after the
    /// first real activation — this is the reliable per-show/per-hide hook to use instead.
    /// </summary>
    public interface IWindowVisibilityAware
    {
        void OnWindowShown();
        void OnWindowHidden();
    }
}
