using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;

public sealed class DownloadHelper
{
    public const float defaultDownloadTimeout = 30f;

    public enum DownloadType
    {
        Standard,
        Texture,
        AssetBundle
    }

    public static void download(MonoBehaviour script, string URL, Action<string, UnityWebRequest> completionAction, Action<string, string> errorAction, string downloadID, DownloadType downloadType = DownloadType.Standard)
    {
        script.StartCoroutine(doDownload(URL, completionAction, errorAction, downloadID, defaultDownloadTimeout, downloadType));
    }

    public static void download(MonoBehaviour script, string URL, Action<string, UnityWebRequest> completionAction, Action<string, string> errorAction, string downloadID, float timeout, DownloadType downloadType = DownloadType.Standard)
    {
        script.StartCoroutine(doDownload(URL, completionAction, errorAction, downloadID, timeout, downloadType));
    }

    private static IEnumerator doDownload(string URL, Action<string, UnityWebRequest> completionAction, Action<string, string> errorAction, string downloadID, float timeout, DownloadType downloadType)
    {
        int timeoutInt = Mathf.RoundToInt(timeout);
        if (timeoutInt <= 0)
        {
            MDebug.LogError($"[DownloadHelper] timeout variable needs to be >= 1 but was passed '{timeout}'. Setting to the default timeout value of '{defaultDownloadTimeout}'.");
        }

        UnityWebRequest www = null;
        if (downloadType == DownloadType.Texture)
        {
            www = UnityWebRequestTexture.GetTexture(URL);
        }
        else if (downloadType == DownloadType.AssetBundle)
        {
            www = UnityWebRequestAssetBundle.GetAssetBundle(URL);
        }
        else
        {
            www = UnityWebRequest.Get(URL);
        }

        www.timeout = timeoutInt;

        MDebug.Log($"[DownloadHelper] Loading URL: {URL}");
        yield return www.SendWebRequest();

        if (www.responseCode != 200 || www.result != UnityWebRequest.Result.Success || !string.IsNullOrEmpty(www.error))
        {
            string errorMessage = $"[DownloadHelper] Got error loading URL: {URL} - responseCode: {www.responseCode} - error: {www.error}";
            MDebug.LogError(errorMessage);
            errorAction(downloadID, errorMessage);
            yield break;
        }

        MDebug.Log("[DownloadHelper] Loaded bytes: " + (www.downloadHandler?.data?.Length ?? 0));
        completionAction(downloadID, www);
    }

    public static void downloadFile(MonoBehaviour script, string URL, string path, Action<string, string> completionAction, Action<string, string> errorAction, string downloadID)
    {
        script.StartCoroutine(doFileDownload(URL, path, completionAction, errorAction, downloadID, defaultDownloadTimeout));
    }

    private static IEnumerator doFileDownload(string URL, string path, Action<string, string> completionAction, Action<string, string> errorAction, string downloadID, float timeout)
    {
        int timeoutInt = Mathf.RoundToInt(timeout);
        if (timeoutInt <= 0)
        {
            MDebug.LogError($"[DownloadHelper] timeout variable needs to be >= 1 but was passed '{timeout}'. Setting to the default timeout value of '{defaultDownloadTimeout}'.");
        }

        UnityWebRequest www = UnityWebRequest.Get(URL);
        www.downloadHandler = new DownloadHandlerFile(path);
        www.timeout = timeoutInt;

        MDebug.Log($"[DownloadHelper] Loading URL: {URL}");
        yield return www.SendWebRequest();

        if (www.responseCode != 200 || www.result != UnityWebRequest.Result.Success || !string.IsNullOrEmpty(www.error))
        {
            string errorMessage = $"[DownloadHelper] Got error loading URL: {URL} - responseCode: {www.responseCode} - error: {www.error}";
            MDebug.LogError(errorMessage);
            errorAction(downloadID, errorMessage);
            yield break;
        }

        MDebug.Log("[DownloadHelper] Loaded file to path: " + path);
        completionAction(downloadID, path);
    }

    public static void checkInternetConnection(MonoBehaviour script, Action successAction, Action failureAction)
    {
        script.StartCoroutine(DownloadHelper.doInternetCheck(script, successAction, failureAction));
    }

    private static IEnumerator doInternetCheck(MonoBehaviour script, Action successAction, Action failureAction)
    {
        UnityWebRequest www = UnityWebRequest.Get("https://www.google.com");
        www.timeout = 50;

        yield return www.SendWebRequest();

        if (www.responseCode == 200 && www.result == UnityWebRequest.Result.Success && string.IsNullOrEmpty(www.error))
        {
            successAction();
            yield break;
        }

        // Google isn't available in China so try a different website before properly declaring an internet check failure
        www = UnityWebRequest.Get("https://www.baidu.com");
        www.timeout = 50;

        yield return www.SendWebRequest();

        if (www.responseCode == 200 && www.result == UnityWebRequest.Result.Success && string.IsNullOrEmpty(www.error))
        {
            successAction();
            yield break;
        }

        failureAction();
    }
}
