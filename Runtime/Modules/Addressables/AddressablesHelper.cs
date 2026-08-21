using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace ENP.UnityExtensions.Runtime
{
    public static class AddressablesHelper
    {
        public static async UniTask<T> LoadComponentAsync<T>(string address, CancellationToken cancellationToken = default) where T : Component
        {
            AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(address);
            GameObject result = await handle.ToUniTask(cancellationToken: cancellationToken);

            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"AddressablesHelper: failed to load asset at address \"{address}\".");
                Addressables.Release(handle);
                return null;
            }

            T component = result.GetComponent<T>();
            Addressables.Release(handle);
            return component;
        }
    }
}
