using System;
using enp_unity_extensions.Runtime.Scripts.UI.Windows;
using UnityEngine;
using static enp_unity_extensions.Runtime.Scripts.UI.Windows.AnimatedWindowConstant;

namespace enp_unity_extensions.Runtime.Scripts.UI.Popup
{
    public abstract class PopupWindow : AnimatedWindow
    {
        private void OnValidate()
        {
            var className = GetType().Name;

            if (gameObject.name != className)
            {
                gameObject.name = className;
            }
        }

        public void Close(AnimatedWindowConstant closeAnim = CloseMiddle)
        {
            PopupController.Close(this, closeAnim);
        }

        public abstract void OnOpen(params object[] args);
        
    }
}
