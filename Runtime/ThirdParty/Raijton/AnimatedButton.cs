using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

namespace _main.AdditionalScripts
{
    public class AnimatedButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        public CanvasGroup _canvasGroup;
        public RectTransform _rectTransform;
        public bool InverseInput;
        public bool interactable = true;
    
        private readonly UnityEvent _onClick = new ();
        private bool _blockInput;
        private static WaitForSeconds _blockInputDelay = new WaitForSeconds(0.25f);

        private void OnValidate()
        {
            if (TryGetComponent<RectTransform>(out var component))
            {
                _rectTransform = component;
            }

            if (TryGetComponent<CanvasGroup>(out var canvasGroup))
            {
                _canvasGroup = canvasGroup;
            }
        }

        private void OnEnable()
        {
            _blockInput = false;
        }

        public void AddListener(UnityAction onClick)
        {
            _onClick.AddListener(onClick.Invoke);
        }

        public void RemoveListener()
        {
            _onClick.RemoveAllListeners();
        }

        public void ForceInvoke()
        {
            _onClick.Invoke();
        }
    
        private void Press()
        {
            _onClick.Invoke();
        }


        private void BlockInputTemporarily()
        {
            BlockInputTemp().Start();
        }

        IEnumerator BlockInputTemp()
        {
            _blockInput = true;
            yield return _blockInputDelay;
            _blockInput = false;
        }

        private bool CanClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || !interactable)
            {
                return false;
            }

            if (!InverseInput) //  && !InputManager.CanInput
            {
                return false;
            }

            if (_blockInput )
            {
                return false;
            }

            return true;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!CanClick(eventData)) return;
        
            transform.DOScale(Vector3.one * 0.95f, 0.2f);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!CanClick(eventData)) return;
        
            Press();
            BlockInputTemporarily();
        }
 
        public void OnPointerUp(PointerEventData eventData)
        {
            transform.DOScale(Vector3.one, 0.2f);
        }
    }
}