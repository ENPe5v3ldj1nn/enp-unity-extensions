using System;
using System.Collections.Generic;
using UnityEngine;

namespace ENP.UnityExtensions.Runtime
{
    internal sealed class Pool
    {
        private readonly AbstractPoolObject _prefab;
        private readonly Transform _container;
        private readonly Queue<AbstractPoolObject> _available = new();

        public Pool(AbstractPoolObject prefab, Transform container)
        {
            _prefab = prefab;
            _container = container;
        }

        public void Prewarm(int capacity)
        {
            for (int i = 0; i < capacity; i++)
                _available.Enqueue(CreateInstance());
        }

        public AbstractPoolObject Get()
        {
            if (_available.Count == 0)
            {
                Deb.Log($"Pool for prefab '{_prefab.name}' is exhausted (capacity reached).");
                throw new InvalidOperationException($"Pool for prefab '{_prefab.name}' is exhausted.");
            }

            var instance = _available.Dequeue();
            instance.gameObject.SetActive(true);
            instance.OnSpawn();
            return instance;
        }

        public void Release(AbstractPoolObject instance)
        {
            instance.OnDespawn();
            instance.gameObject.SetActive(false);
            instance.transform.SetParent(_container, false);
            _available.Enqueue(instance);
        }

        private AbstractPoolObject CreateInstance()
        {
            var instance = UnityEngine.Object.Instantiate(_prefab, _container);
            instance.gameObject.SetActive(false);
            instance.OwnerPool = this;
            return instance;
        }
    }
}
