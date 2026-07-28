using System;
using UnityEngine;
using static ENP.UnityExtensions.Runtime.WindowTransition;

namespace ENP.UnityExtensions.Runtime
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

        public void Close(WindowTransition closeAnim)
        {
            PopupController.Close(this, closeAnim);
        }

        public abstract void OnOpen(params object[] args);
        
    }
}
