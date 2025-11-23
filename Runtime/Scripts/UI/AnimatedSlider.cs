using DG.Tweening;
using UniRx;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace enp_unity_extensions.Scripts.UI
{
    public class AnimatedSlider : MonoBehaviour
    {
        [SerializeField] private Slider _slider;
        [SerializeField] private Image _background;
        [SerializeField] private Image _handle;
        [SerializeField] private UnityEngine.UI.Button _onClick;
        
        [Header("Colors")]
        [Space(20)]
        [Header("Active colors")]
        [SerializeField] private Color _activeColorBackground = new Color(1,1,1,1);
        [SerializeField] private Color _activeColorHandle = new Color(1,1,1,1);
        [Header("InActive colors")]
        [SerializeField] private Color _deActiveColorBackground = new Color(1,1,1,1);
        [SerializeField] private Color _deActiveColorHandle = new Color(1,1,1,1);
        
        private float _duration = 0.3f;
        private bool _isFast = false;
        
        public void Initialize(ReactiveProperty<bool> onClick, UnityAction<bool> onClickOut, float duration = 0.3f, bool isFast = false)
        {
            _onClick.onClick.AddListener(() =>
            {
                onClick.Value = !onClick.Value;
                onClickOut?.Invoke(onClick.Value);
            });
            
            OnValueChanged(onClick.Value);
            onClick.Subscribe(OnValueChanged).AddTo(this);
            
            _duration = duration;
            _isFast = isFast;
        }
        
        public void SetFast(bool isFast)
        {
            _isFast = isFast;
        }
        
        private void SetSlider(bool isActive)
        {
            if (isActive)
            {
                _background.DOColor(_activeColorBackground, _duration);

                _handle.DOColor(_activeColorHandle, _duration);
                _slider.DOValue(1, _duration);
            }
            else
            {
                _background.DOColor(_deActiveColorBackground, _duration);
                _handle.DOColor(_deActiveColorHandle, _duration);
                _slider.DOValue(0, _duration);
            }
        }

        private void SetSliderFast(bool isActive)
        {
            if (isActive)
            {
                _background.color = _activeColorBackground;

                _handle.color = _activeColorHandle;
                _slider.value = 1;
            }
            else
            {
                _background.color = _deActiveColorBackground;
                _handle.color = _deActiveColorHandle;
                _slider.value = 0;
            }
        }

        private void OnValueChanged(bool isActive)
        {
            if (_isFast)
            {
                SetFast(isActive);
            }
            else
            {
                SetSlider(isActive);
            }
        }
    }
}