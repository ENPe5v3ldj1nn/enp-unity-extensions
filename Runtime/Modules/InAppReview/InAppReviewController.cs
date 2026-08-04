using Cysharp.Threading.Tasks;
using UnityEngine;

#if UNITY_IOS
using UnityEngine.iOS;
#elif UNITY_ANDROID && PLAY_REVIEW
using Google.Play.Review;
#endif

public static class InAppReviewController
{
    private const int MaxInAppReviewAttemptsBeforeDirectOpen = 3;
    private static int s_rateAndReviewAttempts;

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
        s_rateAndReviewAttempts = 0;

#if UNITY_ANDROID
        s_androidStoreUrl = androidStoreUrl;

#if PLAY_REVIEW
        if (s_reviewManager == null)
        {
            s_reviewManager = new ReviewManager();
            // Підготовка PlayReviewInfo у фоні через UniTask
            InitReviewAsync(false).Forget();
        }
#else
        UnityEngine.Debug.LogWarning("[InAppReviewController] Google Play In-App Review plugin (com.google.play.review) not found. Falling back to store URL.");
#endif
#endif

#if UNITY_IOS
        s_iosStoreUrl = iosStoreUrl;
#endif
    }
    
    public static void RateAndReview()
    {
        s_rateAndReviewAttempts++;
        if (s_rateAndReviewAttempts > MaxInAppReviewAttemptsBeforeDirectOpen)
        {
            DirectlyOpen();
            return;
        }

#if UNITY_IOS
        Device.RequestStoreReview();
#elif UNITY_ANDROID && PLAY_REVIEW
        if (s_reviewManager == null)
        {
            s_reviewManager = new ReviewManager();
        }

        // Запуск через UniTask
        LaunchReviewAsync().Forget();
#elif UNITY_ANDROID
        UnityEngine.Debug.LogWarning("[InAppReviewController] Google Play In-App Review plugin (com.google.play.review) not found. Opening store page instead.");
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
    private static async UniTask InitReviewAsync(bool forceDirectOpenOnError)
    {
        var requestFlowOperation = s_reviewManager.RequestReviewFlow();
        await UniTask.WaitUntil(() => requestFlowOperation.IsDone);

        if (requestFlowOperation.Error != ReviewErrorCode.NoError)
        {
            s_playReviewInfo = null;

            if (forceDirectOpenOnError)
            {
                DirectlyOpen();
            }

            return;
        }

        s_playReviewInfo = requestFlowOperation.GetResult();
    }

    /// <summary>
    /// Показує in-app review, за потреби сам ініціалізує PlayReviewInfo.
    /// </summary>
    private static async UniTaskVoid LaunchReviewAsync()
    {
        // Якщо ще не маємо PlayReviewInfo – догружаємо його тут
        if (s_playReviewInfo == null)
        {
            // Тут ми ініціалізуємо з прапорцем:
            // якщо щось піде не так – просто відкриємо сторінку стору
            await InitReviewAsync(true);

            if (s_playReviewInfo == null)
            {
                return; // InitReviewAsync уже зробив DirectlyOpen() у разі помилки
            }
        }

        var launchFlowOperation = s_reviewManager.LaunchReviewFlow(s_playReviewInfo);
        await UniTask.WaitUntil(() => launchFlowOperation.IsDone);

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
