using System.Collections.Generic;
using DG.Tweening;
using enp_unity_extensions.Scripts.UI.Windows;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using static enp_unity_extensions.Scripts.UI.Windows.AnimatedWindowConstant;

namespace enp_unity_extensions.Runtime.Scripts.UI.Popup
{
    public class PopupController : MonoBehaviour
    {
        [SerializeField] private Canvas _canvas;
        [SerializeField] private Image _background;
    
        public static bool PopupIsActive => Instance._windowStack.Count > 0;
        public static float CanvasScale => Instance._canvas.scaleFactor;
    
        private readonly Stack<PopupWindow> _windowStack = new Stack<PopupWindow>();
        private static float AnimSpeed = 0.45f;
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

        public static void SetAnimSpeed(float animSpeed)
        {
            AnimSpeed = animSpeed;
        }
    
        public static T Open<T>(AnimatedWindowConstant openAnim = OpenMiddle) where T : PopupWindow
        {
            Instance.gameObject.SetActive(true);
            Instance._canvas.gameObject.SetActive(true);

            if (Instance._windowStack.Count == 0)
            {
                Instance._background.DOKill();
                Instance._background.DOFade(1, AnimSpeed);
            }
    
            return SetPopup<T>(typeof(T).Name, openAnim);
        }
    
        public static void Close(AnimatedWindowConstant closeAnim = CloseMiddle, UnityAction onClose = null)
        {
            Close(null, closeAnim, onClose);
        }

        public static void Close(PopupWindow popup, AnimatedWindowConstant closeAnim = CloseMiddle, UnityAction onClose = null)
        {
            if (Instance._windowStack.Count == 0)
            {
                onClose?.Invoke();
                return;
            }

            var popupToClose = popup ?? Instance._windowStack.Peek();

            if (!Instance._windowStack.Contains(popupToClose))
            {
                D.Log("PopupController: popup to close is not registered in the stack.");
                onClose?.Invoke();
                return;
            }

            if (!ReferenceEquals(Instance._windowStack.Peek(), popupToClose))
            {
                D.Log("PopupController: only the top-most popup can be closed.");
                onClose?.Invoke();
                return;
            }

            if (Instance._windowStack.Count == 1)
            {
                Instance._background.DOKill();
                Instance._background.DOFade(0, AnimSpeed)
                    .OnComplete(() => Instance.gameObject.SetActive(false));
            }

            void OnComplete()
            {
                Instance._windowStack.Pop();
                Destroy(popupToClose.gameObject);

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
    
            popupToClose.Close(closeAnim.ToString(), OnComplete);
        }
    
        private static T SetPopup<T>(string name, AnimatedWindowConstant openAnim) where T : PopupWindow
        {
            var popupFromResource = Resources.Load<T>(PopupPath + name);
            var popup = Instantiate(popupFromResource, Instance._canvas.transform);
            Instance._windowStack.Push(popup);
            popup.transform.SetAsLastSibling();
            popup.Open(openAnim.ToString());
            return popup;
        }
    }
}
