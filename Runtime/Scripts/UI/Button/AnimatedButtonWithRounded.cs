using enp_unity_extensions.Runtime.Scripts.UI.Form;
using UnityEngine;

namespace enp_unity_extensions.Scripts.UI.Button
{
    public class AnimatedButtonWithRounded : AnimatedButton
    {
        [SerializeField] private RoundedShapeGraphic _roundedShapeGraphic;
        [Header("Active colors")]
        [SerializeField] private Gradient _activeColorMain = new Gradient();
        [SerializeField] private Gradient _activeColorBorder = new Gradient();
        
        [Header("InActive colors")]
        [SerializeField] private Gradient _deActiveColorMain = new Gradient();
        [SerializeField] private Gradient _deActiveColorBorder = new Gradient();


        private bool _isActive;
        private float _animTime = 0.5f;

        public void SetAnimTIme(float animTime)
        {
            _animTime = animTime;
        }
        
        public void SetActive()
        {
            _roundedShapeGraphic.SetGradientOverrides(_activeColorMain, _activeColorBorder);
            _isActive = true;
        }

        public void SetInactive()
        {
            _roundedShapeGraphic.SetGradientOverrides(_deActiveColorMain, _deActiveColorBorder);
            _isActive = false;
        }
    }
}