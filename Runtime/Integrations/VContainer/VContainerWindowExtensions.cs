using VContainer;
using VContainer.Unity;

namespace ENP.UnityExtensions.Runtime
{
    /// <summary>
    /// VContainer sugar for the window system. Lives in an optional assembly that only compiles
    /// when the VContainer package is present, so the core plugin stays DI-agnostic.
    /// </summary>
    public static class VContainerWindowExtensions
    {
        /// <summary>
        /// Boots the given <see cref="WindowController"/> (discovers [UiWindow] windows) and
        /// registers it as <see cref="IWindowService"/> so consumers can inject it. Put the
        /// controller's own Initialize-on-Awake OFF and call this from your LifetimeScope instead.
        /// </summary>
        public static RegistrationBuilder RegisterWindowController(this IContainerBuilder builder, WindowController controller)
        {
            controller.Boot();
            return builder.RegisterComponent(controller).As<IWindowService>();
        }
    }
}
