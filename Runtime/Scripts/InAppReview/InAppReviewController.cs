using System.Collections;
using UnityEngine;

#if UNITY_IOS
using UnityEngine.iOS;
#elif UNITY_ANDROID && PLAY_REVIEW
using Google.Play.Review;
#endif

public static class InAppReviewController
{
#if UNITY_ANDROID && PLAY_REVIEW
    private static ReviewManager s_reviewManager;
    private static PlayReviewInfo s_playReviewInfo;
#endif

#if UNITY_ANDROID
    private static string s_androidStoreUrl;
#endif

#if UNITY_IOS
    private static string s_iosStoreUrl;
#endif
    
    public static void Initialize(string androidStoreUrl, string iosStoreUrl)
    {
#if UNITY_ANDROID
        s_androidStoreUrl = androidStoreUrl;

#if PLAY_REVIEW
        if (s_reviewManager == null)
        {
            s_reviewManager = new ReviewManager();
            // Підготовка PlayReviewInfo у фоні через CoroutineController
            CoroutineController.Instance.StartCoroutine(InitReviewCoroutine(false));
        }
#else
        Debug.LogWarning("[InAppReviewController] Google Play In-App Review plugin (com.google.play.review) not found. Falling back to store URL.");
#endif
#endif

#if UNITY_IOS
        s_iosStoreUrl = iosStoreUrl;
#endif
    }
    
    public static void RateAndReview()
    {
#if UNITY_IOS
        Device.RequestStoreReview();
#elif UNITY_ANDROID && PLAY_REVIEW
        if (s_reviewManager == null)
        {
            s_reviewManager = new ReviewManager();
        }

        // Запуск корутини тільки через CoroutineController
        CoroutineController.Instance.StartCoroutine(LaunchReviewCoroutine());
#elif UNITY_ANDROID
        Debug.LogWarning("[InAppReviewController] Google Play In-App Review plugin (com.google.play.review) not found. Opening store page instead.");
        DirectlyOpen();
#else
        Debug.Log("[InAppReviewController] In-app review не підтримується на цій платформі.");
#endif
    }

#if UNITY_ANDROID && PLAY_REVIEW
    /// <summary>
    /// Запитує PlayReviewInfo один раз і кешує його.
    /// Якщо forceDirectOpenOnError = true і сталася помилка – відкриває сторінку стору.
    /// </summary>
    private static IEnumerator InitReviewCoroutine(bool forceDirectOpenOnError)
    {
        var requestFlowOperation = s_reviewManager.RequestReviewFlow();
        yield return requestFlowOperation;

        if (requestFlowOperation.Error != ReviewErrorCode.NoError)
        {
            s_playReviewInfo = null;

            if (forceDirectOpenOnError)
            {
                DirectlyOpen();
            }

            yield break;
        }

        s_playReviewInfo = requestFlowOperation.GetResult();
    }

    /// <summary>
    /// Показує in-app review, за потреби сам ініціалізує PlayReviewInfo.
    /// </summary>
    private static IEnumerator LaunchReviewCoroutine()
    {
        // Якщо ще не маємо PlayReviewInfo – догружаємо його тут
        if (s_playReviewInfo == null)
        {
            // Тут ми ініціалізуємо з прапорцем:
            // якщо щось піде не так – просто відкриємо сторінку стору
            yield return InitReviewCoroutine(true);

            if (s_playReviewInfo == null)
            {
                yield break; // InitReviewCoroutine уже зробив DirectlyOpen() у разі помилки
            }
        }

        var launchFlowOperation = s_reviewManager.LaunchReviewFlow(s_playReviewInfo);
        yield return launchFlowOperation;

        // Після показу діалогу PlayReviewInfo більше не валідне
        s_playReviewInfo = null;

        if (launchFlowOperation.Error != ReviewErrorCode.NoError)
        {
            DirectlyOpen();
        }
    }
#endif

    /// <summary>
    /// Пряме відкриття сторінки застосунку в сторі.
    /// URL-и передаються через Initialize(androidStoreUrl, iosStoreUrl).
    /// </summary>
    private static void DirectlyOpen()
    {
#if UNITY_ANDROID
        var url = string.IsNullOrEmpty(s_androidStoreUrl)
            ? $"https://play.google.com/store/apps/details?id={Application.identifier}"
            : s_androidStoreUrl;

        Application.OpenURL(url);
#elif UNITY_IOS
        if (string.IsNullOrEmpty(s_iosStoreUrl))
        {
            Debug.LogWarning("[InAppReviewController] iOS store URL is not set. Call Initialize(androidUrl, iosUrl) first.");
            return;
        }

        Application.OpenURL(s_iosStoreUrl);
#endif
    }
}
