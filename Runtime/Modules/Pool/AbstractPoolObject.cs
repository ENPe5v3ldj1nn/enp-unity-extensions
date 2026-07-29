using UnityEngine;

namespace ENP.UnityExtensions.Runtime
{
    public abstract class AbstractPoolObject : MonoBehaviour
    {
        internal Pool OwnerPool { get; set; }

        public abstract void OnSpawn();
        public abstract void OnDespawn();
    }
}
