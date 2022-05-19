using UnityEngine;
using System.Collections.Generic;

public enum EnvironmentMarketplace
{
    iOS,
    tvOS,
    GooglePlay,
    Amazon,
    Steam,
    PS4,
    XboxOne,
    Switch,
    Unknown
}

public enum BuildReleaseStage
{
    unknown,
    editor,
    development,
    release
}

public sealed class EnvironmentController
{
    Dictionary<string, string> environment = new Dictionary<string, string>(64, System.StringComparer.OrdinalIgnoreCase);

    Dictionary<string, bool> cachedBools = new Dictionary<string, bool>(64, System.StringComparer.OrdinalIgnoreCase);
    Dictionary<string, int> cachedInts = new Dictionary<string, int>(64, System.StringComparer.OrdinalIgnoreCase);
    Dictionary<string, float> cachedFloats = new Dictionary<string, float>(64, System.StringComparer.OrdinalIgnoreCase);
    Dictionary<string, double> cachedDoubles = new Dictionary<string, double>(64, System.StringComparer.OrdinalIgnoreCase);
    Dictionary<string, List<string>> cachedStringList = new Dictionary<string, List<string>>(64, System.StringComparer.OrdinalIgnoreCase);
    Dictionary<string, List<int>> cachedIntList = new Dictionary<string, List<int>>(64, System.StringComparer.OrdinalIgnoreCase);
    Dictionary<string, List<float>> cachedFloatList = new Dictionary<string, List<float>>(64, System.StringComparer.OrdinalIgnoreCase);
    Dictionary<string, List<double>> cachedDoubleList = new Dictionary<string, List<double>>(64, System.StringComparer.OrdinalIgnoreCase);

    static LogicEvaluator evaluator = new LogicEvaluator();

    public const string PRODUCTION_STAGE_EDITOR = "editor";
    public const string PRODUCTION_STAGE_DEV = "dev";
    public const string PRODUCTION_STAGE_RELEASE = "release";

#if UNITY_EDITOR
    private static BuildReleaseStage overrideBuildStage = BuildReleaseStage.unknown; // only works in editor
#endif

    private static EnvironmentController sharedInstance = null;
    public static EnvironmentController instance
    {
        get
        {
            if (sharedInstance == null)
            {
                sharedInstance = new EnvironmentController();
            }
            return sharedInstance;
        }
    }

    public static int evaluateString(string raw)
    {
        if (evaluator == null) evaluator = new LogicEvaluator();
        // this is the method used to turn tokens into numbers
        int eval = evaluator.evaluateString(raw, (string theToken) =>
        {
            int qty = 0;
            int checkAmount = EnvironmentController.intForKey(theToken);
            if (checkAmount > 0) qty = checkAmount;
            return qty;
        });
        return eval;
    }

    public static void addEnvironmentValueForKey(string configValue, string key)
    {
        instance.environment[key] = configValue;
        instance.clearCache();
    }

    public static void addEnvironmentVariables(Dictionary<string, string> newEntries)
    {
        foreach (KeyValuePair<string, string> keyValue in newEntries)
        { // foreach, ugh
            instance.environment[keyValue.Key] = keyValue.Value;
        }
        instance.clearCache();
    }

    public void clearCache()
    {
        cachedBools.Clear();
        cachedInts.Clear();
        cachedFloats.Clear();
        cachedDoubles.Clear();
    }

    // this reduces the bundle version down to an x.y string
    // so 1.203.4532 returns 1.2
    // this is generally used for rate me and what's new
    public static string majorVersionString()
    {
        string fullVersion = EnvironmentController.stringForKey("bundleversion");
        string[] tokens = fullVersion.Split('.');
        if (tokens.Length < 2) return fullVersion; // something is probably wrong
        return tokens[0] + "." + tokens[1].Substring(0, 1);
    }

    // this is kinda balls, but we keep it all in one place
    private static EnvironmentMarketplace cachedMarketplace = EnvironmentMarketplace.Unknown;
    public static EnvironmentMarketplace marketplace()
    {
        if (cachedMarketplace != EnvironmentMarketplace.Unknown) return cachedMarketplace;

        // we default to iOS in editor
#if UNITY_EDITOR
        string marketplace = EnvironmentController.stringForKey("marketplaceForLinking", "iOS").ToLower();
#else
        string marketplace = EnvironmentController.stringForKey("marketplaceForLinking", "unknown").ToLower();
#endif
        if (marketplace == "ios") cachedMarketplace = EnvironmentMarketplace.iOS;
        if (marketplace == "tvos") cachedMarketplace = EnvironmentMarketplace.tvOS;
        if (marketplace == "google") cachedMarketplace = EnvironmentMarketplace.GooglePlay;
        if (marketplace == "amazon") cachedMarketplace = EnvironmentMarketplace.Amazon;
        if (marketplace == "steam") cachedMarketplace = EnvironmentMarketplace.Steam;
        if (marketplace == "ps4") cachedMarketplace = EnvironmentMarketplace.PS4;
        if (marketplace == "xboxone") cachedMarketplace = EnvironmentMarketplace.XboxOne;
        if (marketplace == "switch") cachedMarketplace = EnvironmentMarketplace.Switch;
        return cachedMarketplace;
    }

    public static string objectForKey(string key)
    {
#if UNITY_ANALYTICS
        if (RemoteSettings.HasKey(key)) {
            return RemoteSettings.GetString(key);
        }
#endif
        return objectForLowercaseKey(key);
    }

    public static string objectForUnchangedCaseKey(string key)
    {
        string theValue = null;
        if (instance.environment.TryGetValue(key, out theValue)) return theValue;
        return null;
    }

    public static string objectForLowercaseKey(string lowerKey)
    {
        string theValue = null;
        if (instance.environment.TryGetValue(lowerKey, out theValue)) return theValue;
        return null;
    }

    public static string stringForKey(string key, string defaultStringValue = null)
    {
        string theValue = objectForKey(key);
        if (theValue != null) return theValue;
        return defaultStringValue; // may still be null!
    }

    public static int intForKey(string key, int defaultIntVal = 0)
    {
        int theValue;
        if (instance.cachedInts.TryGetValue(key, out theValue)) return theValue;

        object raw = objectForLowercaseKey(key);
        instance.cachedInts[key] = MathfExtensions.parseInt(raw, defaultIntVal);

        return instance.cachedInts[key];
    }

    public static List<string> stringListForKey(string key)
    {
        List<string> theList;
        if (instance.cachedStringList.TryGetValue(key, out theList))
        {
            return theList;
        }
        object raw = objectForLowercaseKey(key);
        instance.cachedStringList[key] = parseConfigStringArray(raw);

        return instance.cachedStringList[key];
    }

    public static List<int> intListForKey(string key)
    {
        List<int> theList;
        if (instance.cachedIntList.TryGetValue(key, out theList))
        {
            return theList;
        }
        object raw = objectForLowercaseKey(key);
        instance.cachedIntList[key] = parseConfigIntArray(raw);
        return instance.cachedIntList[key];
    }

    public static List<float> floatListForKey(string key)
    {
        List<float> theList;
        if (instance.cachedFloatList.TryGetValue(key, out theList))
        {
            return theList;
        }

        object raw = objectForLowercaseKey(key);
        instance.cachedFloatList[key] = parseConfigFloatArray(raw);
        return instance.cachedFloatList[key];
    }

    public static List<double> doubleListForKey(string key)
    {
        List<double> theList;
        if (instance.cachedDoubleList.TryGetValue(key, out theList))
        {
            return theList;
        }

        object raw = objectForLowercaseKey(key);
        instance.cachedDoubleList[key] = parseConfigDoubleArray(raw);

        return instance.cachedDoubleList[key];
    }

    public static double doubleForKey(string key, double defaultDoubleValue = 0)
    {
        double theValue;
        if (instance.cachedDoubles.TryGetValue(key, out theValue))
        {
            return theValue;
        }
        object raw = objectForLowercaseKey(key);
        instance.cachedDoubles[key] = MathfExtensions.parseDouble(raw, defaultDoubleValue);

        return instance.cachedDoubles[key];
    }

    public static float floatForKey(string key, float defaultFloatValue = 0f)
    {
        float theValue;
        if (instance.cachedFloats.TryGetValue(key, out theValue))
        {
            return theValue;
        }
        object raw = objectForLowercaseKey(key);
        instance.cachedFloats[key] = MathfExtensions.parseFloat(raw, defaultFloatValue);

        return instance.cachedFloats[key];
    }

    public static bool boolForKey(string key, bool defaultBoolValue = false)
    {
        bool theValue;
        if (instance.cachedBools.TryGetValue(key, out theValue))
        {
            return theValue;
        }
        object raw = objectForLowercaseKey(key);
        instance.cachedBools[key] = MathfExtensions.parseBool(raw, defaultBoolValue);
        return instance.cachedBools[key];
    }

    public static List<string> parseConfigStringArray(object raw)
    {
        string value = (string)raw;

        List<string> results = new List<string>();

        if (string.IsNullOrEmpty(value)) return results;

        string[] broken = value.Split('|');
        for (int i = 0; i < broken.Length; i++)
        {
            results.Add(broken[i].Trim());
        }

        return results;
    }

    public static List<int> parseConfigIntArray(object raw)
    {
        string value = (string)raw;

        List<int> results = new List<int>();

        if (string.IsNullOrEmpty(value)) return results;

        string[] broken = value.Split('|');
        for (int i = 0; i < broken.Length; i++)
        {
            results.Add(MathfExtensions.parseInt(broken[i].Trim()));
        }

        return results;
    }

    public static List<float> parseConfigFloatArray(object raw)
    {
        string value = (string)raw;

        List<float> results = new List<float>();

        if (string.IsNullOrEmpty(value)) return results;

        string[] broken = value.Split('|');
        for (int i = 0; i < broken.Length; i++)
        {
            results.Add(MathfExtensions.parseFloat(broken[i].Trim()));
        }

        return results;
    }

    public static List<double> parseConfigDoubleArray(object raw)
    {
        string value = (string)raw;

        List<double> results = new List<double>();

        if (string.IsNullOrEmpty(value)) return results;

        string[] broken = value.Split('|');
        for (int i = 0; i < broken.Length; i++)
        {
            results.Add(MathfExtensions.parseDouble(broken[i].Trim()));
        }

        return results;
    }

    public static Color colorForKey(string key)
    {
        string colorString = stringForKey(key);
        return colorWithString(colorString);
    }

    public static string stringWithColor(Color theColor)
    {
        return "" + theColor.r + "," + theColor.g + "," + theColor.b + "," + theColor.a;
    }

    public static Color hexColor(string colorString)
    {
        // #123456
        if (colorString[0] != '#') return colorWithString(colorString);
        if (colorString.Length != 7) return Color.white;
        string red = colorString.Substring(1, 2);
        string green = colorString.Substring(3, 2);
        string blue = colorString.Substring(5, 2);

        int redValue = System.Convert.ToInt32(red, 16);
        int greenValue = System.Convert.ToInt32(green, 16);
        int blueValue = System.Convert.ToInt32(blue, 16);

        Color newColor = Color.white;

        newColor.r = redValue / 255f;
        newColor.g = greenValue / 255f;
        newColor.b = blueValue / 255f;

        return newColor;
    }

    public static Color colorWithString(string colorString)
    {
        Color newColor = Color.white;
        if (colorString == null)
            return newColor;

        if (colorString[0] == '#') return hexColor(colorString);
        if (colorString == "clear")
            return Color.clear;

        if (colorString == "black")
            return Color.black;
        if (colorString == "white")
            return Color.white;
        if (colorString == "purple")
            return Color.magenta;
        if (colorString == "orange")
            return new Color(1f, 0.5f, 0f);
        if (colorString == "green")
            return Color.green;
        if (colorString == "blue")
            return Color.blue;
        if (colorString == "red")
            return Color.red;
        if (colorString == "yellow")
            return Color.yellow;
        if (colorString == "grey")
            return Color.grey;
        if (colorString == "gray")
            return Color.gray;

        string configColor = stringForKey("color name " + colorString);
        if (configColor == null)
        {
            configColor = colorString;
        }

        string[] tokens = configColor.Split(',');

        if (tokens.Length < 3)
            return Color.white;

        newColor.r = float.Parse(tokens[0]);
        newColor.g = float.Parse(tokens[1]);
        newColor.b = float.Parse(tokens[2]);

        if (tokens.Length > 3)
        {
            newColor.a = float.Parse(tokens[3]);
        }
        else
        {
            newColor.a = 1.0f;
        }

        return newColor;
    }

    public static bool isInChina()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        string mobileCountryCode = AndroidUtils.GetNetworkCountryIso();
        if (mobileCountryCode != null) mobileCountryCode = mobileCountryCode.ToLower();
        // China mobile country codes were found here: https://www.nationsonline.org/oneworld/country_code_list.htm
        bool mobileNetworkSaysInChina = (mobileCountryCode == "cn" || mobileCountryCode == "chn");
#elif UNITY_IOS && !UNITY_EDITOR
        string mobileCountryCode = MiOSUtilities.getMobileCountryCode();
        // China mobile country codes were found here: https://www.itu.int/dms_pub/itu-t/opb/sp/T-SP-E.212A-2017-PDF-E.pdf
        bool mobileNetworkSaysInChina = (mobileCountryCode == "460" || mobileCountryCode == "461");
#else
        bool mobileNetworkSaysInChina = false;
#endif

        // If the mobile network says we're in China, let's assume that we are. This only works for devices with a working and connected SIM
        if (mobileNetworkSaysInChina) return true;

        if (RemoteSettingsController.HasReceivedIsInChinaThisBoot)
        {
            // We've received a value for IsInChina this boot so we should be able to just grab the value directly
            return boolForKey("IsInChina");
        }
        else if (RemoteSettingsController.HasReceivedIsInChinaAnyBoot)
        {
            // We've received a value for IsInChina in the past but not yet in this boot so we should hopefully be able to grab it from the Remote Settings cache
            string isInChinaCommand = RemoteSettingsController.findCommandThatContains("IsInChina");
            if (!string.IsNullOrEmpty(isInChinaCommand))
            {
                CommandDispatch.dispatcher.runCommand(isInChinaCommand);
                return boolForKey("IsInChina");
            }
        }

        // If we've gotten here then we can't rely on the mobile network or Remote Settings so we need some sort of fallback.
        // For now, we're just going to use the user's system language and assume that if they're set to Chinese or ChineseSimplified then they're in China.
        // It's better to be overly cautious and assume everyone is in China than not because if they're in China but we classify them as not in China, then some features won't work for them.
        return (Application.systemLanguage == SystemLanguage.Chinese || Application.systemLanguage == SystemLanguage.ChineseSimplified);
    }

    public static void enableSystem(string systemID, string productionStage, bool isEnabled)
    {
        string configKey = $"enable {systemID} in {productionStage}";
        EnvironmentController.addEnvironmentValueForKey(isEnabled.ToString(), configKey);
    }

    public static bool systemIsEnabled(string systemID)
    {
        string stageString = buildReleaseStageString();
        string configKey = $"enable {systemID} in {stageString}";
        return EnvironmentController.boolForKey(configKey);
    }

#if UNITY_EDITOR
    public static void forceBuildStage(BuildReleaseStage stage)
    {
        overrideBuildStage = stage;
    }
#endif

    public static string buildReleaseStageString()
    {
#if UNITY_EDITOR
        if (overrideBuildStage == BuildReleaseStage.development) return PRODUCTION_STAGE_DEV;
        if (overrideBuildStage == BuildReleaseStage.release) return PRODUCTION_STAGE_RELEASE;
        return PRODUCTION_STAGE_EDITOR; // these should probably be constants
#elif DEVELOPMENT_BUILD
        // we might be a release testbot, in which case we want to return 'release'
        if (isReleaseTestbot()) {
            return PRODUCTION_STAGE_RELEASE;
        }
        return PRODUCTION_STAGE_DEV;
#else
        return PRODUCTION_STAGE_RELEASE;
#endif
    }

    public static BuildReleaseStage buildReleaseStage()
    {
#if UNITY_EDITOR
        if (overrideBuildStage != BuildReleaseStage.unknown) return overrideBuildStage;
        return BuildReleaseStage.editor; // these should probably be constants
#elif DEVELOPMENT_BUILD
        // we might be a release testbot, in which case we want to return 'release'
        if (isReleaseTestbot()) {
            return BuildReleaseStage.release;
        }
        return BuildReleaseStage.development;
#else
        return BuildReleaseStage.release;
#endif
    }

    public static bool isReleaseBuild()
    {
        BuildReleaseStage stage = buildReleaseStage();
        return (stage == BuildReleaseStage.release);
    }

    // a special case where we are both a development build and a release build
    public static bool isReleaseTestbot()
    {
#if (UNITY_STANDALONE && !UNITY_EDITOR) || AUTOTEST_ENABLED
        // Hacky way of checking if we're a release testbot
        if (EnvironmentController.boolForKey("autoTestingMode") && EnvironmentController.stringForKey("shortName", "none").ToLower().Contains("release")) {
            return true;
        }
#endif
        return false;
    }
}
