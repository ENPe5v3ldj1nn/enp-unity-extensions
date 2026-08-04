using UnityEngine;

namespace ENP.UnityExtensions.Runtime
{
    [CreateAssetMenu(menuName = "ENP/Pool Config")]
    public class PoolConfig : ScriptableObject
    {
        [SerializeField] private Component _object;
        [SerializeField] private int _capacity;

        public Component Object => _object;
        public int Capacity => _capacity;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_object == null || _object is IPoolable)
            {
                return;
            }

            var poolable = _object.GetComponent(typeof(IPoolable)) as Component;
            if (poolable != null)
            {
                _object = poolable;
                return;
            }

            Debug.LogError($"{_object.name} must implement {nameof(IPoolable)}.", this);
            _object = null;
        }
#endif
    }
}
