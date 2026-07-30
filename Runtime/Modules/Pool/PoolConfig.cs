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
            if (_object != null && _object is not IPoolable)
            {
                Debug.LogError($"{_object.name} must implement {nameof(IPoolable)}.", this);
                _object = null;
            }
        }
#endif
    }
}
