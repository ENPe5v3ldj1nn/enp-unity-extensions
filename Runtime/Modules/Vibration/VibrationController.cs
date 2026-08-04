using System.Runtime.InteropServices;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ENP.UnityExtensions.Runtime
{
    /// <summary>
    /// Легкий, повністю самодостатній контролер вібрації/haptics без зовнішніх залежностей.
    /// Портується copy-paste в інший проект за умови того самого патерну модуля.
    /// </summary>
    public static class VibrationController
    {
        private const string StoragePath = "Settings";
        private const string StorageFileName = "vibration_disabled.json";

        // Пресети Trigger(VibrationStrength): (тривалість мс, інтенсивність 0..1)
        private const int LightDurationMs = 20;
        private const float LightIntensity = 0.4f;
        private const int MediumDurationMs = 35;
        private const float MediumIntensity = 0.7f;
        private const int HeavyDurationMs = 60;
        private const float HeavyIntensity = 1f;

        public enum VibrationStrength
        {
            Light,
            Medium,
            Heavy
        }

        private static bool s_enabled = true;
        private static bool s_initialized;

#if UNITY_ANDROID && !UNITY_EDITOR
        private static AndroidJavaObject s_androidVibrator;
#endif

        public static bool IsEnabled => s_enabled;

        /// <summary>
        /// Необов'язковий виклик. Якщо initialEnabled не задано – підвантажує збережений стан.
        /// Всі Trigger-методи коректно працюють і без виклику Initialize.
        /// </summary>
        public static void Initialize(bool? initialEnabled = null)
        {
            s_enabled = initialEnabled ?? !Storage.Load<bool>(StoragePath, StorageFileName);
            s_initialized = true;
        }

        public static void SetEnabled(bool enabled)
        {
            s_enabled = enabled;
            Storage.Save(StoragePath, StorageFileName, !enabled);
        }

        /// <summary>
        /// Основний легкий виклик з пресетом сили.
        /// </summary>
        public static void Trigger(VibrationStrength strength = VibrationStrength.Medium)
        {
            switch (strength)
            {
                case VibrationStrength.Light:
                    Trigger(LightDurationMs, LightIntensity);
                    break;
                case VibrationStrength.Heavy:
                    Trigger(HeavyDurationMs, HeavyIntensity);
                    break;
                default:
                    Trigger(MediumDurationMs, MediumIntensity);
                    break;
            }
        }

        /// <summary>
        /// Розширений виклик з явним контролем тривалості (мс) та інтенсивності (0..1).
        /// Ніколи не кидає винятків – кожна платформна гілка деградує тихо.
        /// </summary>
        public static void Trigger(int durationMs, float intensity01 = 1f)
        {
            if (!EnsureEnabled())
                return;

            var clampedIntensity = Mathf.Clamp01(intensity01);

#if UNITY_ANDROID && !UNITY_EDITOR
            TriggerAndroid(durationMs, clampedIntensity);
#elif UNITY_IOS && !UNITY_EDITOR
            TriggerIOS();
#else
            TriggerFallback();
#endif
        }

        /// <summary>
        /// Вібрація геймпада (Unity.InputSystem). Окремий опційний виклик,
        /// не пов'язаний з ручними haptics пристрою.
        /// </summary>
        public static void TriggerGamepadRumble(float lowFrequency = 0.5f, float highFrequency = 0.5f, float durationSeconds = 0.15f)
        {
            if (!EnsureEnabled())
                return;

            var gamepad = Gamepad.current;
            if (gamepad == null)
                return;

            gamepad.SetMotorSpeeds(lowFrequency, highFrequency);
            StopGamepadRumbleAsync(gamepad, durationSeconds).Forget();
        }

        private static bool EnsureEnabled()
        {
            if (!s_initialized)
            {
                Initialize();
            }

            return s_enabled;
        }

        private static async UniTaskVoid StopGamepadRumbleAsync(Gamepad gamepad, float durationSeconds)
        {
            await UniTask.Delay(System.TimeSpan.FromSeconds(durationSeconds), cancellationToken: CancellationToken.None);
            gamepad?.SetMotorSpeeds(0f, 0f);
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static void TriggerAndroid(int durationMs, float intensity01)
        {
            var vibrator = GetAndroidVibrator();
            if (vibrator == null)
                return;

            if (AndroidSdkInt() >= 26)
            {
                var amplitude = Mathf.Clamp(Mathf.RoundToInt(intensity01 * 255), 1, 255);
                using var vibrationEffectClass = new AndroidJavaClass("android.os.VibrationEffect");
                using var effect = vibrationEffectClass.CallStatic<AndroidJavaObject>("createOneShot", (long)durationMs, amplitude);
                vibrator.Call("vibrate", effect);
            }
            else
            {
                vibrator.Call("vibrate", (long)durationMs);
            }
        }

        private static AndroidJavaObject GetAndroidVibrator()
        {
            if (s_androidVibrator != null)
                return s_androidVibrator;

            using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            s_androidVibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");

            return s_androidVibrator;
        }

        private static int AndroidSdkInt()
        {
            using var version = new AndroidJavaClass("android.os.Build$VERSION");
            return version.GetStatic<int>("SDK_INT");
        }
#endif

#if UNITY_IOS && !UNITY_EDITOR
        // Точка розширення: за потреби замінити на нативний .mm Taptic Engine плагін
        // без зміни публічного API (Trigger(durationMs, intensity01)).
        private const int SystemSoundIdVibrate = 4095;

        [DllImport("AudioToolbox")]
        private static extern void AudioServicesPlaySystemSound(int soundId);

        private static void TriggerIOS()
        {
            // Грубий фолбек: тривалість та інтенсивність ігноруються стандартним API.
            AudioServicesPlaySystemSound(SystemSoundIdVibrate);
        }
#endif

        private static void TriggerFallback()
        {
            if (SystemInfo.supportsVibration)
            {
                Handheld.Vibrate();
                return;
            }

            Debug.Log("[VibrationController] Вібрація не підтримується на цій платформі.");
        }
    }
}
