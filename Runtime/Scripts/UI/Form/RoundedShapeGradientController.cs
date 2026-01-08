using UnityEngine;

namespace enp_unity_extensions.Runtime.Scripts.UI.Form
{
    [RequireComponent(typeof(RoundedShapeGraphic))]
    public sealed class RoundedShapeGradientController : MonoBehaviour
    {
        [SerializeField] private RoundedShapeGraphic _target;

        public RoundedShapeGraphic Target => _target != null ? _target : (_target = GetComponent<RoundedShapeGraphic>());

        public float FillBaseAngle => Target != null ? Target.FillGradientAngle : 0f;
        public float BorderBaseAngle => Target != null ? Target.BorderGradientAngle : 0f;
        public float FillSpeedDegPerSec => Target != null ? Target.FillGradientAngleSpeed : 0f;
        public float BorderSpeedDegPerSec => Target != null ? Target.BorderGradientAngleSpeed : 0f;

        public void SetBaseAngles(float fillAngle, float borderAngle)
        {
            Target?.SetBaseAngles(fillAngle, borderAngle);
        }

        public void ResetBaseAngles()
        {
            Target?.ResetBaseAnglesToStyle();
        }

        public void SetSpeeds(float fillSpeed, float borderSpeed)
        {
            Target?.SetGradientSpeeds(fillSpeed, borderSpeed);
        }
    }
}
