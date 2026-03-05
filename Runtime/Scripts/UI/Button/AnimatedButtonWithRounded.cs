using enp_unity_extensions.Runtime.Scripts.UI.Form;
using UnityEngine;

namespace enp_unity_extensions.Runtime.Scripts.UI.Button
{
    public class AnimatedButtonWithRounded : AnimatedButton
    {
        [SerializeField] private RoundedShapeGraphic _roundedShapeGraphic;
        [Header("Active colors")]
        [SerializeField] private UnityEngine.Gradient _activeColorMain = new UnityEngine.Gradient();
        [SerializeField] private UnityEngine.Gradient _activeColorBorder = new UnityEngine.Gradient();
        
        [Header("InActive colors")]
        [SerializeField] private UnityEngine.Gradient _deActiveColorMain = new UnityEngine.Gradient();
        [SerializeField] private UnityEngine.Gradient _deActiveColorBorder = new UnityEngine.Gradient();


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