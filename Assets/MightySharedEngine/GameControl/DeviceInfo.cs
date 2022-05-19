using UnityEngine;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

public sealed class DeviceInfo
{
    public enum QualityLevel
    {
        Low,
        Medium,
        High
    }

    QualityLevel quality;
    public QualityLevel debugQualityLevelOverride = QualityLevel.High;

    private static DeviceInfo sharedInstance;
    public static DeviceInfo sharedDeviceInfo {
        get {
            if (sharedInstance == null) {
                sharedInstance = new DeviceInfo();
            }
            return sharedInstance;
        }
    }

    public static QualityLevel deviceQuality {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            return DeviceInfo.sharedDeviceInfo.quality;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool isTouchDevice()
    {
        return Input.touchSupported || Input.stylusTouchSupported;
    }

    public static bool isTablet()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        return false;
#else
        float dpi = Screen.dpi;

        if (Mathf.Approximately(dpi, 0f)) {
            // Unity can't determine the screen's DPI so we'll try to use a fallback
#if UNITY_IOS && !UNITY_EDITOR
            // Is it an iPad?
            return UnityEngine.iOS.Device.generation.ToString().ToLower().Contains("ipad");
#else
            // No reliable fallback on other platforms so let's assume it's not a tablet
            return false;
#endif
        }

        float screenDiagonalPixels = Mathf.Sqrt((Screen.width * Screen.width) + (Screen.height * Screen.height));
        float screenDiagonalInches = screenDiagonalPixels / dpi;

        // If the screen has a diagonal size of 7 inches or more, we'll consider it a tablet
        return (screenDiagonalInches >= 7f);
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool isTVDevice()
    {
        return (isFireTV() || isAndroidTV() || sharedDeviceInfo.isAppleTV);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool isEditor()
    {
#if UNITY_EDITOR
        return true;
#else
        return false;
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool isFireTV()
    {
        return (SystemInfo.deviceModel.Contains("Amazon AFT"));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool isKindle2ndGenCheck()
    {
        return SystemInfo.deviceModel.Equals("KFJWA")
            || SystemInfo.deviceModel.Equals("KFJWI")
            || SystemInfo.deviceModel.Equals("KFTT")
            || SystemInfo.deviceModel.Equals("KFOT");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool isWeakDeviceCheck()
    {
        return SystemInfo.deviceModel == "Amazon AFTM" || SystemInfo.deviceModel == "Amazon KFOT" || SystemInfo.deviceModel == "Amazon Kindle Fire";
    }

#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")]
    extern static void GetScreenSafeInsets(out float left, out float right, out float top, out float bottom);
#endif

#if UNITY_IOS && !UNITY_EDITOR
    // TODO: We should unify all safe area content to work off these values
    // At the moment these are only serving to fix iPhone X notch and rounded corners
    // But we should also generalise this to solve for other scenarios, eg. TV screen space for Apple TV / Android TV which are currently using custom hacks

    static float insetLeft, insetRight, insetTop, insetBottom;
    static DeviceOrientation cachedInsetsOrientation = DeviceOrientation.Unknown;
    static bool cachedInsetSize = false; // We need to make sure we cache these at least once since the simulator initially reports DeviceOrientation.Unknown as the current orientation
    
    static void CacheInsets()
    {
        if (cachedInsetsOrientation != Input.deviceOrientation || !cachedInsetSize) {
            GetScreenSafeInsets(out insetLeft, out insetRight, out insetTop, out insetBottom);
            cachedInsetsOrientation = Input.deviceOrientation;
            cachedInsetSize = true;
        }
    }

    public static float SafeScreenInsetLeft { get { CacheInsets(); return insetLeft; } }
    public static float SafeScreenInsetRight { get { CacheInsets(); return insetRight; } }
    public static float SafeScreenInsetTop { get { CacheInsets(); return insetTop; } }
    public static float SafeScreenInsetBottom { get { CacheInsets(); return insetBottom; } }
    public static float SafeScreenWidth { get { CacheInsets(); return Screen.width - insetLeft - insetRight; } }
    public static float SafeScreenHeight { get { CacheInsets(); return Screen.height - insetTop - insetBottom; } }
#elif UNITY_EDITOR
    public static float SafeScreenInsetLeft { get { return (Screen.width > Screen.height) ? 132f : 0f; } }
    public static float SafeScreenInsetRight { get { return (Screen.width > Screen.height) ? 132f : 0f; } }
    public static float SafeScreenInsetTop { get { return (Screen.width > Screen.height) ? 0f : 132f; } }
    public static float SafeScreenInsetBottom { get { return (Screen.width > Screen.height) ? 102f : 102f; } }
    public static float SafeScreenWidth { get { return Screen.width; } }
    public static float SafeScreenHeight { get { return Screen.height; } }
#else
    public static float SafeScreenInsetLeft { get { return 0f; } }
    public static float SafeScreenInsetRight { get { return 0f; } }
    public static float SafeScreenInsetTop { get { return 0f; } }
    public static float SafeScreenInsetBottom { get { return 0f; } }
    public static float SafeScreenWidth { get { return Screen.width; } }
    public static float SafeScreenHeight { get { return Screen.height; } }
#endif

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool gamepadIsPresent()
    {
        return Input.GetJoystickNames().Length > 0;
    }

    public static bool isLowQualityAndroid()
    {
        if (!sharedDeviceInfo.isAmazon && !sharedDeviceInfo.isGooglePlay)
            return false;

        if (sharedDeviceInfo.quality != DeviceInfo.QualityLevel.Low)
            return false;

        return true;
    }

    public static bool isIPad()
    {
#if UNITY_IOS && !UNITY_EDITOR
        UnityEngine.iOS.DeviceGeneration device = UnityEngine.iOS.Device.generation;

        if (device == UnityEngine.iOS.DeviceGeneration.iPad2Gen
            || device == UnityEngine.iOS.DeviceGeneration.iPad3Gen
            || device == UnityEngine.iOS.DeviceGeneration.iPad4Gen
            || device == UnityEngine.iOS.DeviceGeneration.iPadAir1
            || device == UnityEngine.iOS.DeviceGeneration.iPadAir2
            || device == UnityEngine.iOS.DeviceGeneration.iPadMini1Gen
            || device == UnityEngine.iOS.DeviceGeneration.iPadMini2Gen
            || device == UnityEngine.iOS.DeviceGeneration.iPadMini3Gen
            || device == UnityEngine.iOS.DeviceGeneration.iPadMini4Gen
            || device == UnityEngine.iOS.DeviceGeneration.iPadPro10Inch1Gen
            || device == UnityEngine.iOS.DeviceGeneration.iPadPro10Inch2Gen
            || device == UnityEngine.iOS.DeviceGeneration.iPadPro1Gen
            || device == UnityEngine.iOS.DeviceGeneration.iPadPro2Gen
            || device == UnityEngine.iOS.DeviceGeneration.iPadUnknown) {
            return true;
        }

        Resolution current = Screen.currentResolution;
        float aspect = (float)current.width / (float)current.height;
        if (Mathf.Approximately(aspect, 3f / 4f)) {
            return true;
        }
#endif
        return false;
    }

    // // // // 
    // this is all kinda barf all the way down from here on

    /// <summary>
    /// Detects the Android TV, falls back if it gets a null return from the android cases
    /// </summary>
    /// <returns></returns>
    public static bool isAndroidTV()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try {
            AndroidJavaClass unityPlayerJavaClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            if (unityPlayerJavaClass != null) {
                AndroidJavaObject androidActivity = unityPlayerJavaClass.GetStatic<AndroidJavaObject>("currentActivity");
                if (androidActivity != null) {
                    AndroidJavaClass contextJavaClass = new AndroidJavaClass("android.content.Context");
                    if (contextJavaClass != null) {
                        AndroidJavaObject modeServiceConst = contextJavaClass.GetStatic<AndroidJavaObject>("UI_MODE_SERVICE");
                        if (modeServiceConst != null) {
                            AndroidJavaObject uiModeManager = androidActivity.Call<AndroidJavaObject>("getSystemService", modeServiceConst);
                            int currentModeType = uiModeManager.Call<int>("getCurrentModeType");
                            AndroidJavaClass configurationAndroidClass = new AndroidJavaClass("android.content.res.Configuration");
                            int modeTypeTelevisionConst = configurationAndroidClass.GetStatic<int>("UI_MODE_TYPE_TELEVISION");

                            if (modeTypeTelevisionConst == currentModeType) {
                                return true;
                            } else {
                                return false;
                            }
                        }
                    }
                }
            }
        } catch {
            Debug.LogWarning("Failed Android TV check - is this an old version of Android?");
        }

        // Old fallback if the above native Java code fails
        bool tv = (SystemInfo.deviceModel.Contains("Android TV") || SystemInfo.deviceModel == "Google ADT-1");

        if (tv) {
            Debug.Log("This is an AndroidTV device (FALLBACK)");
            return true;
        }
#endif

        return false;
    }

    // -- // -- // ---// -- // -- // ---// -- // -- // ---// -- // -- // ---// -- // -- // ---// -- // -- // ---// -- // -- // ---// -- // -- // ---// -- // -- // ---// -- // -- // ---
    // -- // -- // ---// -- // -- // ---// -- // -- // ---// -- // -- // ---// -- // -- // ---// -- // -- // ---// -- // -- // ---// -- // -- // ---// -- // -- // ---// -- // -- // ---

    DeviceInfo()
    {
        isKindle2ndGen = isKindle2ndGenCheck();
        isWeakDevice = isWeakDeviceCheck();

        quality = getQualityLevel(SystemInfo.graphicsDeviceName);

        setupPlatform();
    }

    public bool isIOS = false;
    public bool isGooglePlay = false;
    public bool isAmazon = false;
    public bool isAppleTV = false;
    public bool isKindle2ndGen = false;
    public bool isWeakDevice = false;

    private void setupPlatform()
    {
        string platform = EnvironmentController.stringForKey("marketplaceforlinking");

        if (isEditor()) {
            isIOS = (platform == "ios");
            isGooglePlay = (platform == "google");
            isAmazon = (platform == "amazon");
            isAppleTV = (platform == "tvos");

            quality = debugQualityLevelOverride;

            return;
        }

        if (Application.platform == RuntimePlatform.IPhonePlayer) {
            isIOS = true;
            return;
        }

        if (platform == "google") {
            isGooglePlay = true;
            return;
        }

        isAmazon = (Application.platform == RuntimePlatform.Android && platform == "amazon");
        isAppleTV = (Application.platform == RuntimePlatform.tvOS);
    }

#if UNITY_ANDROID && !UNITY_EDITOR

    private QualityLevel getQualityLevel(string graphicsName)
    {
        graphicsName = graphicsName.ToLower();
        graphicsName = graphicsName.Replace(" ", "");
        Debug.Log("[GRAPHICS] chipset found: " + graphicsName);

        if (graphicsName.Contains("adreno")) {
            string digits = onlyDigits(graphicsName);

            int level = MathfExtensions.parseInt(digits);
            
            if (level == 320) return QualityLevel.Low; 
            if (level >= 300) return QualityLevel.High;
            if (level >= 200) return QualityLevel.Medium;

            return QualityLevel.Low;
        }

        if (graphicsName.Contains("mali")) {
            string digits = onlyDigits(graphicsName);

            int level = MathfExtensions.parseInt(digits);

            if (level <= 400) return QualityLevel.Low;
            if (level <= 600) return QualityLevel.Medium;
        }
        
        if (graphicsName.Contains("tegra")) {
            if (graphicsName.Contains("2")) return QualityLevel.Medium;
        }

        if (graphicsName.Contains("vivente")) {
            string digits = onlyDigits(graphicsName);
            int level = MathfExtensions.parseInt(digits);
            if (level <= 2000) return QualityLevel.Low;
            if (level <= 7000) return QualityLevel.Medium;
        }

        if (graphicsName.Contains("powervr")) {
            if (graphicsName.Contains("VXD370".ToLower())) return QualityLevel.Low;
            if (graphicsName.Contains("SGX540".ToLower())) return QualityLevel.Low;

            if (graphicsName.Contains("SGX544MP2".ToLower())) return QualityLevel.Medium;
            if (graphicsName.Contains("SGX544MP3".ToLower())) return QualityLevel.Medium;
        }

        if (isWeakDevice) return QualityLevel.Low;

        // Everything defaults to HIGH otherwise
        return QualityLevel.High;
    }

    private string onlyDigits(string source)
    {
        System.Text.StringBuilder destination = new System.Text.StringBuilder();
        for (int i = 0; i < source.Length; i++) {
            if (System.Char.IsDigit(source[i])) destination.Append(source[i]);
        }
        return destination.ToString();
    }

#elif UNITY_IOS && !UNITY_EDITOR

    private QualityLevel getQualityLevel(string graphicsName)
    {
        UnityEngine.iOS.DeviceGeneration device = UnityEngine.iOS.Device.generation;

        switch (device) {
            case UnityEngine.iOS.DeviceGeneration.iPad2Gen:
            case UnityEngine.iOS.DeviceGeneration.iPad3Gen:
            case UnityEngine.iOS.DeviceGeneration.iPadMini1Gen:
            case UnityEngine.iOS.DeviceGeneration.iPhone4S:
            case UnityEngine.iOS.DeviceGeneration.iPhone5:
            case UnityEngine.iOS.DeviceGeneration.iPodTouch4Gen:
                return QualityLevel.Low;

            case UnityEngine.iOS.DeviceGeneration.iPad4Gen:
            case UnityEngine.iOS.DeviceGeneration.iPadAir1:
            case UnityEngine.iOS.DeviceGeneration.iPadMini2Gen:
            case UnityEngine.iOS.DeviceGeneration.iPadMini3Gen:
            case UnityEngine.iOS.DeviceGeneration.iPhone5C:
            case UnityEngine.iOS.DeviceGeneration.iPhone5S:
            case UnityEngine.iOS.DeviceGeneration.iPhoneSE1Gen:
            case UnityEngine.iOS.DeviceGeneration.iPodTouch5Gen:
                return QualityLevel.Medium;

            case UnityEngine.iOS.DeviceGeneration.iPadAir2:
            case UnityEngine.iOS.DeviceGeneration.iPadMini4Gen:
            case UnityEngine.iOS.DeviceGeneration.iPadPro1Gen:
            case UnityEngine.iOS.DeviceGeneration.iPadPro2Gen:
            case UnityEngine.iOS.DeviceGeneration.iPadPro10Inch1Gen:
            case UnityEngine.iOS.DeviceGeneration.iPadPro10Inch2Gen:
            case UnityEngine.iOS.DeviceGeneration.iPhone6:
            case UnityEngine.iOS.DeviceGeneration.iPhone6Plus:
            case UnityEngine.iOS.DeviceGeneration.iPhone6S:
            case UnityEngine.iOS.DeviceGeneration.iPhone6SPlus:
            case UnityEngine.iOS.DeviceGeneration.iPhone7:
            case UnityEngine.iOS.DeviceGeneration.iPhone7Plus:
            case UnityEngine.iOS.DeviceGeneration.iPhone8:
            case UnityEngine.iOS.DeviceGeneration.iPhone8Plus:
            case UnityEngine.iOS.DeviceGeneration.iPhoneX:
            case UnityEngine.iOS.DeviceGeneration.iPadUnknown:
            case UnityEngine.iOS.DeviceGeneration.iPhoneUnknown:
            case UnityEngine.iOS.DeviceGeneration.iPodTouchUnknown:
                return QualityLevel.High;

            default:
                return QualityLevel.High;
        }
    }

#elif UNITY_WP8 && !UNITY_EDITOR

    private QualityLevel getQualityLevel(string graphicsName)
    {
        // TODO: Add Windows phone settings
        return QualityLevel.High;
    }

#else

    private QualityLevel getQualityLevel(string graphicsName)
    {
        return QualityLevel.High;
    }

#endif
}
