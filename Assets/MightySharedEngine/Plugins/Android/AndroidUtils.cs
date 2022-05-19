using UnityEngine;
using System;

public static class AndroidUtils
{
#if UNITY_ANDROID && !UNITY_EDITOR

    public const string UnityPlayerClassName = "com.unity3d.player.UnityPlayer";

    public static bool CanRotate()
    {
        try {
            AndroidJavaClass unityPlayerClass = new AndroidJavaClass(UnityPlayerClassName);
            AndroidJavaObject activity = unityPlayerClass.GetStatic<AndroidJavaObject>("currentActivity");

            if (activity == null) {
                Debug.LogError("[AndroidUtils] CanRotate - Error: Unable to get the current activity");
                return true;
            }

            AndroidJavaObject resolver = activity.Call<AndroidJavaObject>("getContentResolver");
            AndroidJavaClass settings = new AndroidJavaClass("android.provider.Settings$System");
            int canRotate = settings.CallStatic<int>("getInt", resolver, "accelerometer_rotation");

            return canRotate != 0;
        } catch (Exception e) {
            Debug.LogError($"[AndroidUtils] CanRotate - Error: {e?.ToString()}");
            return true;
        }
    }

    public static bool HasReadPermission()
    {
        try {
            AndroidJavaClass unityPlayerClass = new AndroidJavaClass(UnityPlayerClassName);
            AndroidJavaObject activity = unityPlayerClass.GetStatic<AndroidJavaObject>("currentActivity");

            if (activity == null) {
                Debug.LogError("[AndroidUtils] HasReadPermission - Error: Unable to get the current activity");
                return false;
            }

            return activity.Call<int>("checkCallingOrSelfPermission", "android.permission.READ_EXTERNAL_STORAGE") == 0;
        } catch (Exception e) {
            Debug.LogError($"[AndroidUtils] HasReadPermission - Error: {e?.ToString()}");
            return false;
        }
    }

    public static bool HasWritePermission()
    {
        try {
            AndroidJavaClass unityPlayerClass = new AndroidJavaClass(UnityPlayerClassName);
            AndroidJavaObject activity = unityPlayerClass.GetStatic<AndroidJavaObject>("currentActivity");

            if (activity == null) {
                Debug.LogError("[AndroidUtils] HasWritePermission - Error: Unable to get the current activity");
                return false;
            }

            return activity.Call<int>("checkCallingOrSelfPermission", "android.permission.WRITE_EXTERNAL_STORAGE") == 0;
        } catch (Exception e) {
            Debug.LogError($"[AndroidUtils] HasWritePermission - Error: {e?.ToString()}");
            return false;
        }
    }

    public static bool HasCameraPermission()
    {
        try {
            AndroidJavaClass unityPlayerClass = new AndroidJavaClass(UnityPlayerClassName);
            AndroidJavaObject activity = unityPlayerClass.GetStatic<AndroidJavaObject>("currentActivity");

            if (activity == null) {
                Debug.LogError("[AndroidUtils] HasCameraPermission - Error: Unable to get the current activity");
                return false;
            }

            return activity.Call<int>("checkCallingOrSelfPermission", "android.permission.CAMERA") == 0;
        } catch (Exception e) {
            Debug.LogError($"[AndroidUtils] HasCameraPermission - Error: {e?.ToString()}");
            return false;
        }
    }

    public static void RequestReadPermission()
    {
        try {
            using (AndroidJavaClass unityPlayerClass = new AndroidJavaClass(UnityPlayerClassName)) {
                AndroidJavaObject activity = unityPlayerClass.GetStatic<AndroidJavaObject>("currentActivity");
                AndroidJavaClass permissionGranter = new AndroidJavaClass("com.mightygames.giftlibrary.PermissionGranter");
                permissionGranter.CallStatic("grantPermission", activity, "android.permission.READ_EXTERNAL_STORAGE");
            }
        } catch (System.Exception e) {
            Debug.LogError($"[AndroidUtils] RequestWritePermission - Error: {e?.ToString()}");
        }
    }

    public static void RequestWritePermission()
    {
        try {
            using (AndroidJavaClass unityPlayerClass = new AndroidJavaClass(UnityPlayerClassName)) {
                AndroidJavaObject activity = unityPlayerClass.GetStatic<AndroidJavaObject>("currentActivity");
                AndroidJavaClass permissionGranter = new AndroidJavaClass("com.mightygames.giftlibrary.PermissionGranter");
                permissionGranter.CallStatic("grantPermission", activity, "android.permission.WRITE_EXTERNAL_STORAGE");
            }
        } catch (System.Exception e) {
            Debug.LogError($"[AndroidUtils] RequestWritePermission - Error: {e?.ToString()}");
        }
    }

    public static void RequestCameraPermission()
    {
        try {
            using (AndroidJavaClass unityPlayerClass = new AndroidJavaClass(UnityPlayerClassName)) {
                AndroidJavaObject activity = unityPlayerClass.GetStatic<AndroidJavaObject>("currentActivity");
                AndroidJavaClass permissionGranter = new AndroidJavaClass("com.mightygames.giftlibrary.PermissionGranter");
                permissionGranter.CallStatic("grantPermission", activity, "android.permission.CAMERA");
            }
        } catch (System.Exception e) {
            Debug.LogError($"[AndroidUtils] RequestCameraPermission - Error: {e?.ToString()}");
        }
    }

    public static void StartAndroidSettings()
    {
        try {
            AndroidJavaClass unityPlayerClass = new AndroidJavaClass(UnityPlayerClassName);
            AndroidJavaObject activity = unityPlayerClass.GetStatic<AndroidJavaObject>("currentActivity");

            if (activity == null) {
                Debug.LogError("[AndroidUtils] StartAndroidSettings - Error: Unable to get the current activity");
                return;
            }

            AndroidJavaClass uriClass = new AndroidJavaClass("android.net.Uri");
            string package = "package:" + activity.Call<string>("getPackageName");
            AndroidJavaObject uriObject = uriClass.CallStatic<AndroidJavaObject>("parse", package);
            AndroidJavaObject intent = new AndroidJavaObject("android.content.Intent", "android.settings.APPLICATION_DETAILS_SETTINGS", uriObject);
            activity.Call("startActivityForResult", intent, 0);
        } catch (System.Exception e) {
            Debug.LogError($"[AndroidUtils] StartAndroidSettings - Error: {e?.ToString()}");
        }
    }

    public static string GetInternalDataPath()
    {
        try {
            string path = "";

            IntPtr context = AndroidJNI.FindClass("android/content/ContextWrapper");
            IntPtr getFilesDir = AndroidJNIHelper.GetMethodID(context, "getFilesDir", "()Ljava/io/File;");

            using (AndroidJavaClass unityPlayerClass = new AndroidJavaClass(UnityPlayerClassName)) {
                using (AndroidJavaObject activity = unityPlayerClass.GetStatic<AndroidJavaObject>("currentActivity")) {
                    IntPtr file = AndroidJNI.CallObjectMethod(activity.GetRawObject(), getFilesDir, new jvalue[0]);
                    IntPtr objFile = AndroidJNI.FindClass("java/io/File");
                    IntPtr getAbsolutePath = AndroidJNIHelper.GetMethodID(objFile, "getAbsolutePath", "()Ljava/lang/String;");

                    path = AndroidJNI.CallStringMethod(file, getAbsolutePath, new jvalue[0]);

                    if (path == null) return Application.persistentDataPath;
                }
            }
            return path;
        } catch (Exception e) {
            Debug.LogError($"[AndroidUtils] GetInternalDataPath - Error: {e?.ToString()}");
            return Application.persistentDataPath;
        }
    }

    public static int GetRunningSDK()
    {
        try {
            using (AndroidJavaClass buildVersion = new AndroidJavaClass("android.os.Build$VERSION")) {
                int sdkVersion = buildVersion.GetStatic<int>("SDK_INT");
                return sdkVersion;
            }
        } catch (System.Exception e) {
            Debug.LogError($"[AndroidUtils] GetRunningSDK - Error: {e?.ToString()}");
            return 0;
        }
    }

    public static bool IsAppInstalled(string applicationId)
    {
        try {
            AndroidJavaClass unityPlayerClass = new AndroidJavaClass(UnityPlayerClassName);
            AndroidJavaObject activity = unityPlayerClass.GetStatic<AndroidJavaObject>("currentActivity");

            AndroidJavaClass packageManagerClass = new AndroidJavaClass("android.content.pm.PackageManager");
            int getActivitiesConstant = packageManagerClass.GetStatic<int>("GET_ACTIVITIES");

            AndroidJavaObject packageManager = activity.Call<AndroidJavaObject>("getPackageManager");
            AndroidJavaObject packageInfo = packageManager.Call<AndroidJavaObject>("getPackageInfo", applicationId, getActivitiesConstant);

            // If we've gotten to this point without an exception, then that means that the app is installed
            return true;
        } catch (System.Exception e) {
            Debug.LogError($"[AndroidUtils] IsAppInstalled - Error: {e?.ToString()}");
            return false;
        }
    }

    public static long GetFreeStorageSpace()
    {
        try {
            AndroidJavaClass environment = new AndroidJavaClass("android.os.Environment");
            AndroidJavaObject dataDirectory = environment.CallStatic<AndroidJavaObject>("getDataDirectory");
            return dataDirectory.Call<long>("getUsableSpace");
        } catch (System.Exception e) {
            Debug.LogError($"[AndroidUtils] GetFreeStorageSpace - Error: {e?.ToString()}");
            return -1L;
        }
    }

    public static long GetFreeStorageSpaceMB()
    {
        long freeSpace = GetFreeStorageSpace();
        if (freeSpace < 0L) {
            return -1L;
        } else {
            return freeSpace / 1048576L;
        }
    }

    public static string GetNetworkCountryIso()
    {
        try {
            AndroidJavaClass unityPlayerClass = new AndroidJavaClass(UnityPlayerClassName);
            AndroidJavaObject activity = unityPlayerClass.GetStatic<AndroidJavaObject>("currentActivity");

            AndroidJavaClass contextClass = new AndroidJavaClass("android.content.Context");
            string telephonyService = contextClass.GetStatic<string>("TELEPHONY_SERVICE");
            AndroidJavaObject telephonyManager = activity.Call<AndroidJavaObject>("getSystemService", telephonyService);
            return telephonyManager.Call<string>("getNetworkCountryIso");
        } catch (System.Exception e) {
            Debug.LogError($"[AndroidUtils] GetNetworkCountryIso - Error: {e?.ToString()}");
            return null;
        }
    }

    public static string GetDeviceLocaleCode()
    {
        try
        {
            using (AndroidJavaClass localeClass = new AndroidJavaClass("java.util.Locale"))
            {
                AndroidJavaObject defaultLocale = localeClass.CallStatic<AndroidJavaObject>("getDefault");
                if (defaultLocale == null) return null;
                return defaultLocale.Call<string>("getCountry");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[AndroidUtils] GetDeviceLocaleCode - Error: {e?.ToString()}");
            return null;
        }
    }

#else

    public static bool CanRotate() { return true; }
    public static bool HasReadPermission() { return true; }
    public static bool HasWritePermission() { return true; }
    public static bool HasCameraPermission() { return true; }
    public static void RequestReadPermission() { }
    public static void RequestWritePermission() { }
    public static void RequestCameraPermission() { }
    public static void StartAndroidSettings() { }
    public static string GetInternalDataPath() { return Application.persistentDataPath; }
    public static int GetRunningSDK() { return 0; }
    public static bool IsAppInstalled(string applicationId) { return false; }
    public static long GetFreeStorageSpace() { return -1L; }
    public static long GetFreeStorageSpaceMB() { return -1L; }
    public static string GetNetworkCountryIso() { return null; }
    public static string GetDeviceLocaleCode() { return null; }

#endif
}
