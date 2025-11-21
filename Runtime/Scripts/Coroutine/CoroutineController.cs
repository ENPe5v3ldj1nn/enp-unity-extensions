using System.Collections;
using UnityEngine;

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


public static class CoroutineExtension
{
    // --- START ---

    public static void Start(this IEnumerator method)
    {
        CoroutineController.Instance.StartCoroutine(method);
    }

    public static void Start(this IEnumerator method, MonoBehaviour behaviour)
    {
        behaviour.StartCoroutine(method);
    }

    public static void Start(this IEnumerator method, out Coroutine coroutine)
    {
        coroutine = CoroutineController.Instance.StartCoroutine(method);
    }

    public static void Start(this IEnumerator method, out Coroutine coroutine, MonoBehaviour behaviour)
    {
        coroutine = behaviour.StartCoroutine(method);
    }

    // --- STOP BY IENUMERATOR ---

    public static void Stop(this IEnumerator method)
    {
        if (method != null)
        {
            CoroutineController.Instance.StopCoroutine(method);
        }
    }

    public static void Stop(this IEnumerator method, MonoBehaviour behaviour)
    {
        if (method != null)
        {
            behaviour.StopCoroutine(method);
        }
    }

    public static void Stop(this IEnumerator method, Coroutine coroutine)
    {
        if (coroutine != null)
        {
            CoroutineController.Instance.StopCoroutine(coroutine);
        }
    }

    public static void Stop(this IEnumerator method, Coroutine coroutine, MonoBehaviour behaviour)
    {
        if (coroutine != null)
        {
            behaviour.StopCoroutine(coroutine);
        }
    }

    // --- STOP BY COROUTINE (зручно для s_coroutine.Stop();) ---

    public static void Stop(this Coroutine coroutine)
    {
        if (coroutine != null)
        {
            CoroutineController.Instance.StopCoroutine(coroutine);
        }
    }

    public static void Stop(this Coroutine coroutine, MonoBehaviour behaviour)
    {
        if (coroutine != null)
        {
            behaviour.StopCoroutine(coroutine);
        }
    }
}