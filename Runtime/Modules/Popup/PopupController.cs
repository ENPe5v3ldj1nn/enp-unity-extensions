using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ENP.UnityExtensions.Runtime
{
    public class PopupController : MonoBehaviour
    {
        [Tooltip("Boot on Awake. Turn OFF when a DI container calls Initialize() instead.")]
        [SerializeField] private bool _initializeOnAwake = true;
        [SerializeField] private Canvas _canvas;
        [SerializeField] private Image _background;

        public static bool PopupIsActive => Instance._windowStack.Count > 0;
        public static float CanvasScale => Instance._canvas.scaleFactor;

        private readonly Stack<PopupWindow> _windowStack = new Stack<PopupWindow>();
        private static float AnimSpeed = 0.45f;
        private static float BackgroundFadeMin = 1;
        private static float BackgroundFadeMax = 0;
        private static readonly string PopupPath = "Popups/";

        private static PopupController _instance;
        private static PopupController Instance
        {
            get
            {
                Debug.Assert(_instance != null, "PopupController.Initialize() must be called before use.");
                return _instance;
            }
        }

        private void Awake()
        {
            if (_initializeOnAwake)
                Initialize();
        }

        /// <summary>Registers this controller as the active singleton. Call once, before first use.</summary>
        public void Initialize()
        {
            _instance = this;
        }

        public static void Setup(float animSpeed, float fadeMin, float fadeMax)
        {
            AnimSpeed = animSpeed;
            BackgroundFadeMin = fadeMin;
            BackgroundFadeMax = fadeMax;
        }

        public static async UniTask<T> Open<T>(WindowTransition openAnim, params object[] args) where T : PopupWindow
        {
            Instance.gameObject.SetActive(true);
            Instance._canvas.gameObject.SetActive(true);

            if (Instance._windowStack.Count == 0)
            {
                Instance._background.DOKill();
                Instance._background.DOFade(BackgroundFadeMax, AnimSpeed);
            }

            return await SetPopup<T>(typeof(T).Name, openAnim, args);
        }

        public static void Close(PopupWindow popup, WindowTransition closeAnim, UnityAction onClose = null)
        {
            if (Instance._windowStack.Count == 0)
            {
                onClose?.Invoke();
                return;
            }

            var popupToClose = popup ?? Instance._windowStack.Peek();

            if (!Instance._windowStack.Contains(popupToClose))
            {
                Deb.Log("PopupController: popup to close is not registered in the stack.");
                onClose?.Invoke();
                return;
            }

            if (!ReferenceEquals(Instance._windowStack.Peek(), popupToClose))
            {
                Deb.Log("PopupController: only the top-most popup can be closed.");
                onClose?.Invoke();
                return;
            }

            if (Instance._windowStack.Count == 1)
            {
                Instance._background.DOKill();
                Instance._background.DOFade(BackgroundFadeMin, AnimSpeed)
                    .OnComplete(() => Instance.gameObject.SetActive(false));
            }

            void OnComplete()
            {
                Instance._windowStack.Pop();
                Addressables.ReleaseInstance(popupToClose.gameObject);

                if (Instance._windowStack.Count == 0)
                {
                    Instance._canvas.gameObject.SetActive(false);
                }
                else
                {
                    Instance._windowStack.Peek().transform.SetAsLastSibling();
                }

                onClose?.Invoke();
            }
    
            popupToClose.CloseAsync(closeAnim).ContinueWith(OnComplete).Forget();
        }
    
        private static async UniTask<T> SetPopup<T>(string name, WindowTransition openAnim, params object[] args) where T : PopupWindow
        {
            var instance = await Addressables.InstantiateAsync(PopupPath + name, Instance._canvas.transform).ToUniTask();
            var popup = instance.GetComponent<T>();
            Instance._windowStack.Push(popup);
            popup.transform.SetAsLastSibling();
            popup.OpenAsync(openAnim).Forget();
            popup.OnOpen(args);
            return popup;
        }
    }
}
