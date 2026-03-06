using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace enp_unity_extensions.Scripts.UI.Button
{
    public class AnimatedButtonWithGradient : AnimatedButton
    {
        [SerializeField] private UIGradient _backgroundGradient;
        [SerializeField] private UIGradient _outlineGradient;

        [Header("Active colors")]
        [SerializeField] private Color _activeBackgroundColor1 = Color.white;
        [SerializeField] private Color _activeBackgroundColor2 = Color.white;
        [SerializeField] private Color _activeOutlineColor1 = Color.white;
        [SerializeField] private Color _activeOutlineColor2 = Color.white;

        [Header("InActive colors")]
        [SerializeField] private Color _deActiveBackgroundColor1 = Color.white;
        [SerializeField] private Color _deActiveBackgroundColor2 = Color.white;
        [SerializeField] private Color _deActiveOutlineColor1 = Color.white;
        [SerializeField] private Color _deActiveOutlineColor2 = Color.white;

        private Graphic _backgroundGraphic;
        private Graphic _outlineGraphic;

        private Tweener _backgroundColor1Tween;
        private Tweener _backgroundColor2Tween;
        private Tweener _outlineColor1Tween;
        private Tweener _outlineColor2Tween;

        private float _animTime = 0.5f;

        private void Awake()
        {
            CacheGraphics();
        }

        private void OnValidate()
        {
            CacheGraphics();
        }

        private void OnDisable()
        {
            KillTweens();
        }

        public void SetAnimTIme(float animTime)
        {
            _animTime = animTime;
        }

        public void SetActive()
        {
            AnimateGradients(_activeBackgroundColor1, _activeBackgroundColor2, _activeOutlineColor1, _activeOutlineColor2);
        }

        public void SetInactive()
        {
            AnimateGradients(_deActiveBackgroundColor1, _deActiveBackgroundColor2, _deActiveOutlineColor1, _deActiveOutlineColor2);
        }

        private void AnimateGradients(Color backgroundColor1, Color backgroundColor2, Color outlineColor1, Color outlineColor2)
        {
            AnimateGradient(_backgroundGradient, ref _backgroundColor1Tween, ref _backgroundColor2Tween, _backgroundGraphic, backgroundColor1, backgroundColor2);
            AnimateGradient(_outlineGradient, ref _outlineColor1Tween, ref _outlineColor2Tween, _outlineGraphic, outlineColor1, outlineColor2);
        }

        private void AnimateGradient(UIGradient gradient, ref Tweener color1Tween, ref Tweener color2Tween, Graphic graphic, Color target1, Color target2)
        {
            if (gradient == null)
            {
                return;
            }

            color1Tween?.Kill();
            color2Tween?.Kill();

            color1Tween = DOTween.To(() => gradient.m_color1, value =>
            {
                gradient.m_color1 = value;
                graphic?.SetVerticesDirty();
            }, target1, _animTime);

            color2Tween = DOTween.To(() => gradient.m_color2, value =>
            {
                gradient.m_color2 = value;
                graphic?.SetVerticesDirty();
            }, target2, _animTime);
        }

        private void KillTweens()
        {
            _backgroundColor1Tween?.Kill();
            _backgroundColor2Tween?.Kill();
            _outlineColor1Tween?.Kill();
            _outlineColor2Tween?.Kill();
        }

        private void CacheGraphics()
        {
            if (_backgroundGradient != null)
            {
                _backgroundGraphic = _backgroundGradient.GetComponent<Graphic>();
            }

            if (_outlineGradient != null)
            {
                _outlineGraphic = _outlineGradient.GetComponent<Graphic>();
            }
        }
    }
}
