using UnityEngine;
using System.Runtime.InteropServices;

public sealed class MiOSLocalNotifications : MonoBehaviour
{
    public static void resetLocalNotificationBadge()
    {
        MDebug.LogBlue("[MiOSLocalNotifications] resetLocalNotificationBadge.");

#if UNITY_IOS && !UNITY_EDITOR
        resetNotifications();
#endif
    }

#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void resetNotifications();
#endif
}
