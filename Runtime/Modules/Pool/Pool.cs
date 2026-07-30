using System;
using System.Collections.Generic;
using UnityEngine;

namespace ENP.UnityExtensions.Runtime
{
    internal sealed class Pool
    {
        private readonly Component _prefab;
        private readonly Transform _container;
        private readonly Dictionary<Component, Pool> _ownerRegistry;
        private readonly Queue<Component> _available = new();

        public Pool(Component prefab, Transform container, Dictionary<Component, Pool> ownerRegistry)
        {
            _prefab = prefab;
            _container = container;
            _ownerRegistry = ownerRegistry;
        }

        public void Prewarm(int capacity)
        {
            for (int i = 0; i < capacity; i++)
                _available.Enqueue(CreateInstance());
        }

        public Component Get()
        {
            if (_available.Count == 0)
            {
                Deb.Log($"Pool for prefab '{_prefab.name}' is exhausted (capacity reached).");
                throw new InvalidOperationException($"Pool for prefab '{_prefab.name}' is exhausted.");
            }

            var instance = _available.Dequeue();
            instance.gameObject.SetActive(true);
            ((IPoolable)instance).OnSpawn();
            return instance;
        }

        public void Release(Component instance)
        {
            ((IPoolable)instance).OnDespawn();
            instance.gameObject.SetActive(false);
            instance.transform.SetParent(_container, false);
            _available.Enqueue(instance);
        }

        private Component CreateInstance()
        {
            var instance = UnityEngine.Object.Instantiate(_prefab, _container);
            instance.gameObject.SetActive(false);
            _ownerRegistry[instance] = this;
            return instance;
        }
    }
}
