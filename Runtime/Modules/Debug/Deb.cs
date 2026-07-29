public static class Deb
{
    public static bool IsCanLog = true;
        
    public static void Log(object message)
    {
        if (!IsCanLog)
            return;
            
        UnityEngine.Debug.Log(message);
    }
}
