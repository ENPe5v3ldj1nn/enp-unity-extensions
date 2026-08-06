using VContainer;

namespace ENP.UnityExtensions.Runtime
{
    /// <summary>
    /// VContainer sugar for <see cref="TmpFontRegistrationService"/>. Lives in an optional assembly
    /// that only compiles when the VContainer package is present, so the core plugin stays DI-agnostic.
    /// </summary>
    public static class VContainerFontExtensions
    {
        public static RegistrationBuilder RegisterTmpFontRegistrationService(this IContainerBuilder builder, string addressPrefix)
        {
            var service = new TmpFontRegistrationService(addressPrefix);
            service.Initialize();
            return builder.RegisterInstance(service);
        }
    }
}
