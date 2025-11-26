using enp_unity_extensions.Scripts.UI.Windows;
using UnityEngine;
using static enp_unity_extensions.Scripts.UI.Windows.AnimatedWindowConstant;

namespace enp_unity_extensions.Runtime.Scripts.UI.Popup
{
    public class PopupWindow : AnimatedWindow
    {
        private void OnValidate()
        {
            var className = nameof(PopupWindow);

            if (gameObject.name != className)
            {
                gameObject.name = className;
            }
        }

        public void Close(AnimatedWindowConstant closeAnim = CloseMiddle)
        {
            PopupController.Close(this, closeAnim);
        }
    }
}
