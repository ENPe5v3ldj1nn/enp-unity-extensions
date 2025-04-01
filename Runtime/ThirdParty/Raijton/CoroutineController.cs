using System.Collections;
using _main.AdditionalScripts._main.AdditionalScripts;
using UnityEngine;

namespace _main.AdditionalScripts
{
    using UnityEngine;

    namespace _main.AdditionalScripts
    {
        public class CoroutineController : MonoBehaviour
        {
            private static CoroutineController _instance;

            public static CoroutineController Instance
            {
                get
                {
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("[CoroutineController]");
                        _instance = go.AddComponent<CoroutineController>();
                        Object.DontDestroyOnLoad(go);
                    }

                    return _instance;
                }
                private set => _instance = value;
            }
        }
    }

    
    public static class CoroutineExtension
    {
        public static void Start(this IEnumerator method)
        {
            CoroutineController.Instance.StartCoroutine(method);
        }
        
        public static void Start(this IEnumerator method, out Coroutine coroutine)
        {
            coroutine = CoroutineController.Instance.StartCoroutine(method);
        }

        public static void Start(this IEnumerator method, out Coroutine coroutine, MonoBehaviour behaviour)
        {
            coroutine = behaviour.StartCoroutine(method);
        }

        public static void Stop(this IEnumerator method)
        {
            CoroutineController.Instance.StopCoroutine(method);
        }

        public static void Stop(this IEnumerator method, Coroutine coroutine)
        {
            if (coroutine != null)
            {
                CoroutineController.Instance.StopCoroutine(method);
            }
        }

        public static void Stop(this IEnumerator method, Coroutine coroutine, MonoBehaviour behaviour)
        {
            if (coroutine != null)
            {
                behaviour.StopCoroutine(method);
            }
        }
    }
}