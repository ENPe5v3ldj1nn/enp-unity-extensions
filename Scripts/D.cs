using UnityEngine;

namespace enp_unity_extensions.Scripts
{
    public static class D
    {
        public static bool IsCanLog = true;
        
        public static void Log(object message)
        {
            if (!IsCanLog)
                return;
            
            Debug.Log(message);
        }
    }
}