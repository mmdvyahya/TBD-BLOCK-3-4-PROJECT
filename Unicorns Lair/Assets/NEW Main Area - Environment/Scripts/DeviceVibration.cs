using UnityEngine;

public static class DeviceVibration
{
    public static void Vibrate()
    {
#if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
#else
        Debug.Log("Vibration triggered. Works on mobile device.");
#endif
    }
}