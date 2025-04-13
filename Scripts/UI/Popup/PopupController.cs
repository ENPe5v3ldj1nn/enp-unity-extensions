using enp_unity_extensions.Scripts.UI.Windows;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using DG.Tweening;
using static enp_unity_extensions.Scripts.UI.Windows.AnimatedWindowConstant;

namespace enp_unity_extensions.Scripts.UI.Popup
{
    public class PopupController : MonoBehaviour
    {
        [SerializeField] private Canvas _canvas;
        [SerializeField] private Image _background;
    
        public static bool PopupIsActive => Instance._activeWindow != null;
        public static float CanvasScale => Instance._canvas.scaleFactor;
    
        private PopupWindow _activeWindow;
        private const float AnimSpeed = 0.45f;
        private static readonly string PopupPath = "Popups/";
    
        private static PopupController _instance;
        private static PopupController Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<PopupController>();

                    if (_instance == null)
                    {
                        D.Log("PopupController not found in scene!");
                    }
                }

                return _instance;
            }
        }

    
        public static T Open<T>(AnimatedWindowConstant openAnim = OpenMiddle) where T : PopupWindow
        {
            Instance.gameObject.SetActive(true);
            Instance._canvas.gameObject.SetActive(true);
            Instance._background.DOFade(1, AnimSpeed);
            
            if (Instance._activeWindow != null)
            {
                Destroy(Instance._activeWindow.gameObject);
                Instance._activeWindow = null;
            }
    
            return SetPopup<T>(typeof(T).Name, openAnim);
        }
    
        public static void Close(AnimatedWindowConstant closeAnim = CloseMiddle, UnityAction onClose = null)
        {
            void OnComplete()
            {
                Destroy(Instance._activeWindow.gameObject);
                Instance._activeWindow = null;
                Instance._canvas.gameObject.SetActive(false);
                onClose?.Invoke();
            }
    
            Instance._activeWindow.Close(closeAnim.ToString(), OnComplete);
            Instance._background.DOFade(0, AnimSpeed)
                .OnComplete(() => Instance.gameObject.SetActive(false));
        }
    
        private static T SetPopup<T>(string name, AnimatedWindowConstant openAnim) where T : PopupWindow
        {
            var popupFromResource = Resources.Load<T>(PopupPath + name);
            var popup = Instantiate(popupFromResource, Instance._canvas.transform);
            Instance._activeWindow = popup;
            popup.Open(openAnim.ToString());
            return popup;
        }
    }
}
