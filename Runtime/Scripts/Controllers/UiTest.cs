using System;
using System.Collections.Generic;
using enp_unity_extensions.Runtime.Scripts.UI.Windows;
using UnityEngine;

namespace enp_unity_extensions.Runtime.Scripts.Controllers
{
    public class UiTest : UiController<WindowId>
    {

        protected override void SetupMap(Dictionary<WindowId, AnimatedWindow> windowsMap)
        {

        }
    }

    public enum WindowId
    {
        MainMenu,
        Gameplay,
        Settings,
    }
}