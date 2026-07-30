using System;
using System.Collections.Generic;
using UnityEngine;

namespace ENP.UnityExtensions.Runtime
{
    public class PoolController : MonoBehaviour, IPoolService
    {
        [SerializeField] private PoolConfig[] _configs;

        private readonly Dictionary<Component, Pool> _poolsByPrefab = new();
        private readonly Dictionary<Component, Pool> _poolsByInstance = new();

        private void Awake()
        {
            for (int i = 0; i < _configs.Length; i++)
            {
                var config = _configs[i];
                var pool = new Pool(config.Object, transform, _poolsByInstance);
                pool.Prewarm(config.Capacity);
                _poolsByPrefab[config.Object] = pool;
            }
        }

        public T Get<T>(T prefab) where T : Component, IPoolable => (T)FindPool(prefab).Get();

        public void Release(Component instance)
        {
            if (!_poolsByInstance.TryGetValue(instance, out var pool))
                throw new InvalidOperationException($"Instance '{instance.name}' does not belong to any pool.");

            pool.Release(instance);
        }

        private Pool FindPool(Component prefab)
        {
            if (!_poolsByPrefab.TryGetValue(prefab, out var pool))
                throw new KeyNotFoundException($"Prefab '{prefab.name}' is not registered in {GetType().Name}.");

            return pool;
        }
    }
}
