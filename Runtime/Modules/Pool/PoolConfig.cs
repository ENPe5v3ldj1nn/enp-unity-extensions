using UnityEngine;

namespace ENP.UnityExtensions.Runtime
{
    [CreateAssetMenu(menuName = "ENP/Pool Config")]
    public class PoolConfig : ScriptableObject
    {
        [SerializeField] private AbstractPoolObject _object;
        [SerializeField] private int _capacity;
   
        public AbstractPoolObject Object => _object;
        public int Capacity => _capacity;
    }
}
