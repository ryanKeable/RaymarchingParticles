using UnityEngine;
using System.Runtime.InteropServices;

public sealed class MiOSUtilities : MonoBehaviour
{
    public static bool isIosMusicPlaying()
    {
#if (UNITY_IOS || UNITY_TVOS) && !UNITY_EDITOR
        bool result = isMusicPlaying();
        MDebug.LogBlue("[MiOSUtilities] isIosMusicPlaying: " + result);
#else
        bool result = false;
#endif

        return result;
    }

#if (UNITY_IOS || UNITY_TVOS) && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern bool isMusicPlaying();
#endif

    public static bool tryRequestAppStoreReview()
    {
#if (UNITY_IOS || UNITY_TVOS) && !UNITY_EDITOR
        bool result = requestAppStoreReview();
        MDebug.LogBlue("[MiOSUtilities] tryRequestAppStoreReview: " + result);
#else
        bool result = false;
#endif
        return result;
    }

#if (UNITY_IOS || UNITY_TVOS) && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern bool requestAppStoreReview();
#endif

    public static bool canRequestAppStoreReview()
    {
#if (UNITY_IOS || UNITY_TVOS) && !UNITY_EDITOR
        bool result = supportsAppStoreReview();
        MDebug.LogBlue("[MiOSUtilities] canRequestAppStoreReview: " + result);
#else
        bool result = false;
#endif
        return result;
    }

#if (UNITY_IOS || UNITY_TVOS) && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern bool supportsAppStoreReview();
#endif

    public static bool canOpenURL(string url)
    {
#if (UNITY_IOS || UNITY_TVOS) && !UNITY_EDITOR
        bool result = supportsURL(url);
        MDebug.LogBlue($"[MiOSUtilities] canOpenURL {url}: {result}");
#else
        bool result = false;
#endif
        return result;
    }

    public static bool canOpenDodgyURL()
    {
#if (UNITY_IOS || UNITY_TVOS) && !UNITY_EDITOR
        bool result = supportsURL("cydia://home");
        MDebug.LogBlue("[MiOSUtilities] canOpenDodgyURL: " + result);
#else
        bool result = false;
#endif
        return result;
    }

#if (UNITY_IOS || UNITY_TVOS) && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern bool supportsURL(string url);
#endif

#if (UNITY_IOS || UNITY_TVOS) && !UNITY_EDITOR
    [DllImport("__Internal")]
    public static extern string getMobileCountryCode();

    [DllImport("__Internal")]
    public static extern string getDeviceLocaleCode();

    [DllImport("__Internal")]
    public static extern long GetFreeStorageSpace();

    [DllImport("__Internal")]
    public static extern long GetFreeStorageSpaceMB();
#else
    public static string getMobileCountryCode() { return null; }

    public static string getDeviceLocaleCode() { return null; }

    public static long GetFreeStorageSpace() { return -1L; }

    public static long GetFreeStorageSpaceMB() { return -1L; }
#endif
}
