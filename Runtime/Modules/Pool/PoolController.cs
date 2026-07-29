using System;
using System.Collections.Generic;
using UnityEngine;

namespace ENP.UnityExtensions.Runtime
{
    public class PoolController : MonoBehaviour, IPoolService
    {
        [SerializeField] private PoolConfig[] _configs;

        private readonly Dictionary<AbstractPoolObject, Pool> _pools = new();

        private void Awake()
        {
            for (int i = 0; i < _configs.Length; i++)
            {
                var config = _configs[i];
                var pool = new Pool(config.Object, transform);
                pool.Prewarm(config.Capacity);
                _pools[config.Object] = pool;
            }
        }

        public AbstractPoolObject Get(AbstractPoolObject prefab) => FindPool(prefab).Get();

        public void Release(AbstractPoolObject instance)
        {
            if (instance.OwnerPool == null)
                throw new InvalidOperationException($"Instance '{instance.name}' does not belong to any pool.");

            instance.OwnerPool.Release(instance);
        }

        private Pool FindPool(AbstractPoolObject prefab)
        {
            if (!_pools.TryGetValue(prefab, out var pool))
                throw new KeyNotFoundException($"Prefab '{prefab.name}' is not registered in {GetType().Name}.");

            return pool;
        }
    }
}
