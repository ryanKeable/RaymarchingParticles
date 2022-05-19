using UnityEngine;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public sealed class MiOSCloudKeystore : MonoBehaviour
{
    public Action<Dictionary<string, object>> storeDidChangeAction;

    public static MiOSCloudKeystore startCloudKeystore()
    {
        MDebug.LogBlue("[MiOSCloudKeystore] startCloudKeystore");

        MiOSCloudKeystore pluginSupport = null;
#if (UNITY_IOS || UNITY_TVOS) && !UNITY_EDITOR
        GameObject newObject = new GameObject("iOSListenerCloudKeystore");
        DontDestroyOnLoad(newObject);
        pluginSupport = newObject.AddComponent<MiOSCloudKeystore>();
#endif
        return pluginSupport;
    }

#if (UNITY_IOS || UNITY_TVOS) && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void startupiCloudIntegration();

    [DllImport("__Internal")]
    private static extern void requestUbiquitousStore();

    [DllImport("__Internal")]
    private static extern void setUbiquitousString(string key, string stringValue);

    [DllImport("__Internal")]
    private static extern void clearUbiquitousStore();
#endif

    public void startCloud()
    {
#if (UNITY_IOS || UNITY_TVOS) && !UNITY_EDITOR
        startupiCloudIntegration();
#endif
    }

    public void cloudStoreDidChange(string json)
    {
        MDebug.Log("[MiOSCloudKeystore] cloudStoreDidChange: " + json);
        if (json.Length == 0)
        {
            if (storeDidChangeAction != null) storeDidChangeAction(new Dictionary<string, object>());
            return;
        }
        Dictionary<string, object> store = GenericsJSONParser.JsonDecode(json) as Dictionary<string, object>;
        if (storeDidChangeAction != null) storeDidChangeAction(store);
    }

    public static void requestCloudStore()
    {
#if (UNITY_IOS || UNITY_TVOS) && !UNITY_EDITOR
        requestUbiquitousStore();
#endif
    }

    public static void setCloudString(string cloudKey, string cloudString)
    {
#if (UNITY_IOS || UNITY_TVOS) && !UNITY_EDITOR
        setUbiquitousString(cloudKey, cloudString);
#endif
    }

    public static void clearCloudStore()
    {
#if (UNITY_IOS || UNITY_TVOS) && !UNITY_EDITOR
        clearUbiquitousStore();
#endif
    }
}
