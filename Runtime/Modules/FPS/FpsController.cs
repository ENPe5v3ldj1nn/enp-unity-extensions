using UnityEngine;

namespace _main.Scripts.Controllers
{
    public static class FpsController
    {
        public static void SetRefreshRateRatio()
        {
            var maxHzFromUnity = 0;

            foreach (var r in Screen.resolutions)
                maxHzFromUnity = Mathf.Max(maxHzFromUnity, Mathf.RoundToInt((float)r.refreshRateRatio.value));

            if (maxHzFromUnity == 0)
                maxHzFromUnity = Mathf.RoundToInt((float)Screen.currentResolution.refreshRateRatio.value); // фолбек

            var panelMax = maxHzFromUnity > 0 ? maxHzFromUnity : 60;
            if (panelMax < 45) panelMax = 60; // анти-LTPO фолбек

            QualitySettings.vSyncCount = 0; // targetFrameRate має пріоритет
            var desired = 120;
            Application.targetFrameRate = Mathf.Min(desired, panelMax);

            // Debug.Log($"FPS target set: {Application.targetFrameRate} (panel max {panelMax})");
        }
    }
}