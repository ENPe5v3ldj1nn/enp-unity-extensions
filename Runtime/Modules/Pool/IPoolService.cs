namespace ENP.UnityExtensions.Runtime
{
    public interface IPoolService
    {
        AbstractPoolObject Get(AbstractPoolObject prefab);
        void Release(AbstractPoolObject instance);
    }
}
