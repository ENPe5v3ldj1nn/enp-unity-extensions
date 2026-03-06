using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace enp_unity_extensions.Scripts.UI.Button
{
    public class AnimatedButtonWithOutline : AnimatedButton
    {
        [SerializeField] private Image _background;
        [SerializeField] private Image _backgroundOutline;

        [Header("Active colors")]
        [SerializeField] private Color _activeColorMain;
        [SerializeField] private Color _activeColorOutline;
        [Header("InActive colors")]
        [SerializeField] private Color _deActiveColorMain;
        [SerializeField] private Color _deActiveColorOutline;
        
        private bool _isActive;
        private float _animTime = 0.5f;

        public void SetAnimTIme(float animTime)
        {
            _animTime = animTime;
        }

        public void SetActive()
        {
            _background.DOKill();
            _backgroundOutline.DOKill();
            
            _background.DOColor(_activeColorMain, _animTime);
            _backgroundOutline.DOColor(_activeColorOutline, _animTime);
            
            _isActive = true;
        }

        public void SetInactive()
        {
            _background.DOKill();
            _backgroundOutline.DOKill();
            
            _background.DOColor(_deActiveColorMain, _animTime);
            _backgroundOutline.DOColor(_deActiveColorOutline, _animTime);
            
            _isActive = false;
        }
    }
}