using VContainer;
using VContainer.Unity;

namespace ENP.UnityExtensions.Runtime
{
    public static class VContainerPoolExtensions
    {
        public static RegistrationBuilder RegisterPoolController(this IContainerBuilder builder, PoolController controller) =>
            builder.RegisterComponent(controller).As<IPoolService>();
    }
}
