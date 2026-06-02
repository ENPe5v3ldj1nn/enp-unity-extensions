using UnityEngine;

public static class DebugLog
{
    public static bool IsCanLog = true;
        
    public static void Log(object message)
    {
        if (!IsCanLog)
            return;
            
        Debug.Log(message);
    }
}
