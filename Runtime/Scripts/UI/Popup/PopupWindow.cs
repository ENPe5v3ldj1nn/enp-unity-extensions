using enp_unity_extensions.Scripts.UI.Windows;
using static enp_unity_extensions.Scripts.UI.Windows.AnimatedWindowConstant;

namespace enp_unity_extensions.Runtime.Scripts.UI.Popup
{
    public class PopupWindow : AnimatedWindow
    {
        public void Close(AnimatedWindowConstant closeAnim = CloseMiddle)
        {
            PopupController.Close(this, closeAnim);
        }
    }
}
