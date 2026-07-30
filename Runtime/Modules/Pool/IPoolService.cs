using UnityEngine;

namespace ENP.UnityExtensions.Runtime
{
    public interface IPoolService
    {
        T Get<T>(T prefab) where T : Component, IPoolable;
        void Release(Component instance);
    }
}
