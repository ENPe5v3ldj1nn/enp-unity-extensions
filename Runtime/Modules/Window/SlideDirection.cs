namespace ENP.UnityExtensions.Runtime
{
    /// <summary>
    /// Mirrors a directional transition (Slide/Smooth). Recipes in <see cref="WindowConfig"/> are
    /// authored once for <see cref="Right"/>; <see cref="Left"/> flips the recipe's horizontal offset.
    /// </summary>
    public enum SlideDirection
    {
        Right,
        Left
    }
}
