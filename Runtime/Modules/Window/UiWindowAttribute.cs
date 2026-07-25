using System;

namespace ENP.UnityExtensions.Runtime
{
    /// <summary>
    /// Marks an <see cref="AnimatedWindow"/> type as a top-level window that
    /// <see cref="AbstractUiController"/> registers automatically during discovery.
    /// Opt-in: only windows whose concrete type carries this attribute are registered,
    /// so nested <see cref="AnimatedWindow"/> sub-views stay out of the lookup table.
    /// Not inherited — each concrete window declares its intent explicitly.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class UiWindowAttribute : Attribute
    {
    }
}
