using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.IO;

public abstract class GoogleDocConfig : MonoBehaviour
{
    public TextAsset localConfigFile;
    public string configID = "config";

#if UNITY_EDITOR
    public GoogleDocUri[] configGoogleDocIDs;
    public bool factoryConfigurationOnly = true;
#endif

    public string configurationUrl;
    int version = -1;

    bool _isBootstrapped = false;

    protected string localConfigCacheFilepath()
    {
        return SystemHelper.documentFilePath(configurationName);
    }

    /// <summary>
    /// Returns the locally saved configuration file, or the factory shipped configuration file (based on what's available).
    /// </summary>
    protected string configFile()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying || factoryConfigurationOnly == true) {
            MDebug.LogBlue("Editor mode detected, loading factory configs for " + configurationName);
            EnvironmentController.addEnvironmentValueForKey("factory", "configurationFileLoaded_" + configID);
            return localConfigFile.text;
        }
#endif

        string localConfig = localConfigCacheFilepath();
        if (File.Exists(localConfig)) {
            string version = SystemHelper.stringWithContentsOfFile(localConfig + "_version");
            EnvironmentController.addEnvironmentValueForKey(version, "configurationFileLoaded_" + configID);
            MDebug.LogRed("Loading locally saved configs for " + configurationName + " version: " + version);
            return SystemHelper.stringWithContentsOfFile(localConfig);
        }

        MDebug.LogRed("Loading factory configs for " + configurationName);
        return localConfigFile.text;
    }

    /// <summary>
    /// Call this function anywhere in the game startup to download the latest version of the config from the cloud.
    /// </summary>
    public void getLatestConfigFromCloud(Action onComplete)
    {
        if (_isBootstrapped) return;

#if UNITY_EDITOR
        if (!shouldLoadFromCloud()) {
            loadConfiguration();
            _isBootstrapped = true;
            if (onComplete != null) onComplete();
            return;
        }
#endif

        MDebug.LogGreen("[S3 CONFIG] " + name + " bundle version to check: " + bundleVersion());

        // Something didn't load right, just load local stuff.
        Action<string, string> onFail = (string downloadID, string error) => {
            MDebug.LogRed("Cloud " + configurationName + " failed to download.");
            loadConfiguration();
            _isBootstrapped = true;
            if (onComplete != null) onComplete();
        };

        // Manifest file loaded.
        Action<string, UnityWebRequest> downloadComplete = (string downloadID, UnityWebRequest www) => {
            string versionString = www.downloadHandler?.text;
            version = MathfExtensions.parseInt(versionString, -1);
            if (version < 0) {
                string errorMessage = $"[GoogleDocConfig] Failed to parse version string '{versionString}'";
                MDebug.LogError(errorMessage);
                onFail(downloadID, errorMessage);
            } else {
                StartCoroutine(downloadCloudConfig(onComplete, onFail));
            }
        };

        downloadManifest(downloadComplete, onFail);
    }

    IEnumerator downloadCloudConfig(Action onComplete, Action<string, string> onFailure)
    {
        if (version < 0) {
            if (onFailure != null) {
                MDebug.LogLtBlue("[ASSET BUNDLE] failed to get version");
                onFailure(string.Empty, "Failed to get version number from manifest.");
                yield break;
            }
        }

        // we have a valid version, make sure it is not less than my minor version
        int myMinorVersion = MathfExtensions.parseInt(minorVersionNumber());
        if (myMinorVersion > version) {
            // then we do NOT load this config as it is older than us
            MDebug.LogLtBlue("[ASSET BUNDLE] asset version:" + version + " is older than our minor version:" + myMinorVersion);
            onFailure(string.Empty, "Asset version is too old.");
            yield break;
        }

        System.Action<string, string> onFail = (str, www) => {
            MDebug.LogLtBlue("[ASSET BUNDLE] www download error:");
            if (onFailure != null) {
                onFailure(string.Empty, "WWW download had an error:");
            }
        };

        System.Action<string, UnityWebRequest> onSuccess = (str, www) => {
            MDebug.LogLtBlue("[ASSET BUNDLE] www download complete");

            AssetBundle bundle = DownloadHandlerAssetBundle.GetContent(www);
            MDebug.LogLtBlue("[ASSET BUNDLE] www download: " + bundle);
            if (bundle == null) {
                MDebug.LogLtBlue("[ASSET BUNDLE] null bundle. getting out.");
                onFailure(string.Empty, "Error loading bundle. Failing..");
            }
            TextAsset configFile = bundle.LoadAsset(localConfigFile.name) as TextAsset;

            SystemHelper.saveStringToFile(configFile.text, localConfigCacheFilepath());
            SystemHelper.saveStringToFile(version.ToString(), localConfigCacheFilepath() + "_version");

            MDebug.LogRed("Loading " + configurationName + " version " + version.ToString() + " from cloud");
            loadConfiguration();
            bundle.Unload(false);
        };

        downloadConfiguration(onSuccess, onFail);

        _isBootstrapped = true;
        if (onComplete != null) onComplete();
    }

    string majorVersionNumber()
    {
        string bundVer = bundleVersion();
        string[] myVersion = bundVer.Split('.');
        return myVersion[0] + "." + myVersion[1];
    }

    string minorVersionNumber()
    {
        string bundVer = bundleVersion();
        string[] myVersion = bundVer.Split('.');
        return myVersion[2];
    }

    string manifestUrl()
    {
        return formatUrl(configurationUrl, majorVersionNumber(), "manifest");
    }

    string configurationUri()
    {
        return formatUrl(configurationUrl, majorVersionNumber(), currentPlatform(), configurationName);
    }

    /// <summary>
    /// Properly formats a URL string with trailing slashes.
    /// </summary>
    static string formatUrl(params string[] list)
    {
        string result = "";
        for (int i = 0; i < list.Length; i++) {
            string listItem = list[i];
            if (listItem == null) {
                MDebug.LogWarning("URL parameter " + i + "is null, this may break the URL");
                listItem = "UNKNOWN_PARAMETER";
            }

            if (listItem.EndsWithFast("/")) {
                result += listItem;
            } else {
                if (i == list.Length - 1)
                    result += listItem;
                else
                    result += listItem + "/";
            }
        }
        return result;
    }

    void downloadConfiguration(Action<string, UnityWebRequest> onComplete, Action<string, string> onFailure)
    {
        string url = configurationUri();
        MDebug.LogGreen("[S3 CONFIG] Downloading manifest: " + url);
        DownloadHelper.download(this, url, onComplete, onFailure, "downloadConfiguration", 10f, DownloadHelper.DownloadType.AssetBundle);
    }

    void downloadManifest(Action<string, UnityWebRequest> onComplete, Action<string, string> onFailure)
    {
        string url = manifestUrl();
        MDebug.LogGreen("[S3 CONFIG] Downloading manifest: " + url);
        DownloadHelper.download(this, url, onComplete, onFailure, "downloadManifest", 5f);
    }

    /// <summary>
    /// Implement this to load the scripts.
    /// You should always use configFile() to get access to the latest scripts.
    /// </summary>
    protected abstract void loadConfiguration();

    /// <summary>
    /// Should return a human readable name of the configuration file (that you want to see in-editor).
    /// </summary>
    public abstract string configurationName { get; }

    /// <summary>
    /// Whether the environment thinks that we should load from cloud.
    /// </summary>
    protected abstract bool shouldLoadFromCloud();

    /// <summary>
    /// Returns the bundle version from the environment.
    /// </summary>
    protected abstract string bundleVersion();

    /// <summary>
    /// Returns the current platform.
    /// </summary>
    protected virtual string currentPlatform()
    {
        return string.Empty;
    }
}
