using UnityEngine;
using UnityEngine.Networking;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
#if !NETFX_CORE
using System.Security.Cryptography.X509Certificates;
#endif
using System.Text;

public sealed class GoogleAPIRequest
{
    public string url;
    public HttpMethod method;
    public Dictionary<string, string> headers;
    public Dictionary<string, string> postFields;
    public byte[] bodyData;
    public Action<string, bool, GoogleAPIRequest> callback;

    public enum HttpMethod
    {
        GET,
        POST,
        PUT,
        DELETE
    }

    public GoogleAPIRequest(string url, Action<string, bool, GoogleAPIRequest> callback)
    {
        this.url = url;
        this.method = HttpMethod.GET;
        this.headers = new Dictionary<string, string>();
        this.postFields = new Dictionary<string, string>();
        this.bodyData = new byte[0];
        this.callback = callback;
    }

    public void send(GoogleAPIRequest followupRequest)
    {
        if (Application.isPlaying)
            GoogleAPIController.Instance.StartCoroutine(makeRequestASync(this, followupRequest));
        else
            makeRequest(this, followupRequest);
    }

    // Callback returns data and success/fail as true/false
    private void makeRequest(GoogleAPIRequest request, GoogleAPIRequest followupRequest)
    {
#if ENABLE_UNITYWEBREQUEST
        WWWForm form = new WWWForm();
        if (request.postFields != null) {
            foreach (KeyValuePair<string, string> kvp in request.postFields) {
                form.AddField(kvp.Key, kvp.Value);
            }
        }

        UnityWebRequest www = null;
        if (request.method == HttpMethod.POST) {
            if (form.data.Length > 0) {
                www = UnityWebRequest.Post(request.url, form);
            } else {
                // Need to do some encoding shenanigans because if we try to do UnityWebRequest.Post(request.url, request.bodyData),
                // UnityWebRequest will mangle our data if it is something like JSON and cause parse errors on the server side :'(
                www = new UnityWebRequest(url, "POST");
                www.uploadHandler = new UploadHandlerRaw(request.bodyData);
                www.downloadHandler = new DownloadHandlerBuffer();
            }
        } else if (request.method == HttpMethod.PUT) {
            www = UnityWebRequest.Put(request.url, request.bodyData);
        } else if (request.method == HttpMethod.DELETE) {
            www = new UnityWebRequest(url, "DELETE");
            www.uploadHandler = new UploadHandlerRaw(request.bodyData);
            www.downloadHandler = new DownloadHandlerBuffer();
        } else {
            www = UnityWebRequest.Get(request.url);
        }

        if (request.headers != null) {
            foreach (KeyValuePair<string, string> kvp in request.headers) {
                www.SetRequestHeader(kvp.Key, kvp.Value);
            }
        }

        www.SendWebRequest();

        while (!www.isDone) {
        }

        if (www.result != UnityWebRequest.Result.Success) {
            Debug.LogError("[GoogleAPIController] Error: " + www.error + ", Text: " + www.downloadHandler.text);
            if (callback != null)
                callback(www.error, false, followupRequest);
        } else {
            Dictionary<string, object> json = GenericsJSONParser.JsonDecode(www.downloadHandler.text) as Dictionary<string, object>;
            if (json != null && json.ContainsKey("error")) {
                Debug.LogError("[GoogleAPIController] Error: \n" + www.downloadHandler.text);
                if (callback != null)
                    callback(www.downloadHandler.text, false, followupRequest);
            } else {
                Debug.Log("[GoogleAPIController] Data: " + www.downloadHandler.text);
                if (callback != null)
                    callback(www.downloadHandler.text, true, followupRequest);
            }
        }
#else
        if (callback != null) callback("UnityWebRequest not supported on this platform yet", false, followupRequest);
#if UNITY_EDITOR
        EditorUtility.ClearProgressBar();
#endif
#endif
    }

    // Callback returns data and success/fail as true/false
    private IEnumerator makeRequestASync(GoogleAPIRequest request, GoogleAPIRequest followupRequest)
    {
#if ENABLE_UNITYWEBREQUEST
        WWWForm form = new WWWForm();
        if (postFields != null) {
            foreach (KeyValuePair<string, string> kvp in postFields) {
                form.AddField(kvp.Key, kvp.Value);
            }
        }

        UnityWebRequest www = null;
        if (request.method == HttpMethod.POST) {
            if (form.data.Length > 0) {
                www = UnityWebRequest.Post(request.url, form);
            } else {
                // Need to do some encoding shenanigans because if we try to do UnityWebRequest.Post(request.url, request.bodyData),
                // UnityWebRequest will mangle our data if it is something like JSON and cause parse errors on the server side :'(
                www = new UnityWebRequest(url, "POST");
                www.uploadHandler = new UploadHandlerRaw(request.bodyData);
                www.downloadHandler = new DownloadHandlerBuffer();
            }
        } else if (request.method == HttpMethod.PUT) {
            www = UnityWebRequest.Put(request.url, request.bodyData);
        } else if (request.method == HttpMethod.DELETE) {
            www = new UnityWebRequest(url, "DELETE");
            www.uploadHandler = new UploadHandlerRaw(request.bodyData);
            www.downloadHandler = new DownloadHandlerBuffer();
        } else {
            www = UnityWebRequest.Get(request.url);
        }

        if (request.headers != null) {
            foreach (KeyValuePair<string, string> kvp in request.headers) {
                www.SetRequestHeader(kvp.Key, kvp.Value);
            }
        }

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success) {
            Debug.LogError("[GoogleAPIController] Error: " + www.error + ", Text: " + www.downloadHandler.text);
            if (callback != null)
                callback(www.error, false, followupRequest);
        } else {
            Dictionary<string, object> json = GenericsJSONParser.JsonDecode(www.downloadHandler.text) as Dictionary<string, object>;
            if (json != null && json.ContainsKey("error")) {
                Debug.LogError("[GoogleAPIController] Error: \n" + www.downloadHandler.text);
                if (callback != null)
                    callback(www.downloadHandler.text, false, followupRequest);
            } else {
                Debug.Log("[GoogleAPIController] Data: " + www.downloadHandler.text);
                if (callback != null)
                    callback(www.downloadHandler.text, true, followupRequest);
            }
        }
#else
        yield return null;
        if (callback != null) callback("UnityWebRequest not supported on this platform yet", false, followupRequest);
#endif
    }
}

public sealed class GoogleAPIController : MonoBehaviour
{
#if UNITY_EDITOR
    private const string scope = @"https://www.googleapis.com/auth/spreadsheets.readonly https://www.googleapis.com/auth/androidpublisher";
    private const string clientEmail = "googleplaymodifier@api-project-602814036307.iam.gserviceaccount.com";
    private const string certBase64 = "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";
    private const string certPassword = "notasecret";
#else
    private const string scope = "Oh hey";
    private const string clientEmail = "What're you doing here? :3";
    private const string certBase64 = "Lookin' for this? :P";
    private const string certPassword = "Why you looking here?";
#endif

    private const string googleGenericAPIHost = @"https://www.googleapis.com/";
    private const string googleSheetAPIHost = @"https://sheets.googleapis.com/";
    private const string authURI = "oauth2/v4/token";
    private const string sheetsURI = "v4/spreadsheets/";
    private const string sheetsCommandAllValues = "/values/";
    private const string gpEditInsert = "androidpublisher/v2/applications/{0}/edits";
    private const string gpEditCommit = "androidpublisher/v2/applications/{0}/edits/{1}:commit";
    private const string gpListingUpdate = "androidpublisher/v2/applications/{0}/edits/{1}/listings/{2}";
    private const string gpImageDeleteAll = "androidpublisher/v2/applications/{0}/edits/{1}/listings/{2}/{3}";
    private const string gpImageUpload = "upload/androidpublisher/v2/applications/{0}/edits/{1}/listings/{2}/{3}?uploadType=media";
    private const string gpAPKUpload = "upload/androidpublisher/v2/applications/{0}/edits/{1}/apks?uploadType=media";
    private const string gpAPKListingUpdate = "androidpublisher/v2/applications/{0}/edits/{1}/apks/{2}/listings/{3}";
    private const string gpTrackUpdate = "androidpublisher/v2/applications/{0}/edits/{1}/tracks/{2}";
    private const string gpgsAchievementsList = "games/v1configuration/applications/{0}/achievements";
    private const string gpgsAchievementsUpdate = "games/v1configuration/achievements/{0}";
    private const string gpgsImageUpload = "upload/games/v1configuration/images/{0}/imageType/{1}?uploadType=media";

    private GoogleAuthToken token;
    private double tokenExpiryTime;
    static private GoogleAPIController instance;
    static public GoogleAPIController Instance {
        get {
            if (instance == null) {
                GameObject controllerObject;
                if ((controllerObject = GameObject.Find("GoogleAPIController")) != null) {
                    instance = controllerObject.GetComponent<GoogleAPIController>();
                } else if ((controllerObject = GameObject.Find("ConfigurationController")) != null) {
                    instance = controllerObject.GetComponent<GoogleAPIController>();
                } else {
                    controllerObject = new GameObject();
                    controllerObject.name = "GoogleAPIController";
                    controllerObject.hideFlags = HideFlags.HideAndDontSave;
                    instance = controllerObject.AddComponent<GoogleAPIController>();
                }
            }

            return instance;
        }
    }

    public static void requestSpreadsheet(string sheetID, List<string> tabIDs, Action<string> callback)
    {
        try {
#if UNITY_EDITOR
            EditorUtility.DisplayProgressBar("Loading Google config", "Authenticating", 0f);
#endif
            Instance.whenReady(() => {
#if UNITY_EDITOR
                EditorUtility.DisplayProgressBar("Loading Google config", "Requesting sheet", 0f);
#endif
                Instance.requestSpreadsheetMapping(sheetID, (string mappingData) => {
                    // Translate sheet IDs in to names
                    Dictionary<string, string> sheetIdToName = new Dictionary<string, string>();
                    Dictionary<string, object> mappingJson = GenericsJSONParser.JsonDecode(mappingData) as Dictionary<string, object>;
                    List<object> SheetProperties = mappingJson["sheets"] as List<object>;
                    foreach (Dictionary<string, object> tabProperty in SheetProperties) {
                        Dictionary<string, object> properties = tabProperty["properties"] as Dictionary<string, object>;
                        sheetIdToName.Add(properties["sheetId"].ToString(), properties["title"].ToString());
                    }

                    List<string> sheetTabs = new List<string>();
                    foreach (string id in tabIDs) {
                        if (sheetIdToName.ContainsKey(id))
                            sheetTabs.Add(sheetIdToName[id]);
                        else
                            sheetTabs.Add(id);
                    }

                    Instance.requestSpreadsheetByName(sheetID, sheetTabs, (string data) => {
                        callback(data);
#if UNITY_EDITOR
                        EditorUtility.ClearProgressBar();
#endif
                    });
                });
            });
        } catch (Exception e) {
            MDebug.LogError($"[GoogleAPIController] Error making request (Did you share the sheet with {clientEmail}): {e?.ToString()}");
#if UNITY_EDITOR
            EditorUtility.ClearProgressBar();
#endif
        }
    }

    public static void requestSpreadsheet(GoogleDocUri[] googleDocList, Action<string> callback)
    {
        try {
#if UNITY_EDITOR
            EditorUtility.DisplayProgressBar("Loading Google config", "Authenticating", 0f);
#endif
            StringBuilder allGoogleDocs = new StringBuilder();
            int count = 0;
            Instance.whenReady(() => {
#if UNITY_EDITOR
                EditorUtility.DisplayProgressBar("Loading Google config", "Requesting sheet " + (count + 1) + " / " + googleDocList.Length, (float)count + 1 / googleDocList.Length);
#endif
                foreach (GoogleDocUri config in googleDocList) {
                    Instance.requestSpreadsheetMapping(config.googleDocID, (string mappingData) => {
                        // Translate sheet IDs in to names
                        Dictionary<string, string> sheetIdToName = new Dictionary<string, string>();
                        Dictionary<string, object> mappingJson = GenericsJSONParser.JsonDecode(mappingData) as Dictionary<string, object>;
                        List<object> SheetProperties = mappingJson["sheets"] as List<object>;
                        foreach (Dictionary<string, object> tabProperty in SheetProperties) {
                            Dictionary<string, object> properties = tabProperty["properties"] as Dictionary<string, object>;
                            sheetIdToName.Add(properties["sheetId"].ToString(), properties["title"].ToString());
                        }

                        List<string> sheetTabs = new List<string>();
                        foreach (string id in config.sheetIds) {
                            if (sheetIdToName.ContainsKey(id))
                                sheetTabs.Add(sheetIdToName[id]);
                            else
                                sheetTabs.Add(id);
                        }

                        Instance.requestSpreadsheetByName(config.googleDocID, sheetTabs, (string data) => {
                            count++;
                            allGoogleDocs.Append(data);
                            if (count == googleDocList.Length) {
                                callback(allGoogleDocs.ToString());
#if UNITY_EDITOR
                                EditorUtility.ClearProgressBar();
#endif
                            }
                        });
                    });
                }
            });
        } catch (Exception e) {
            MDebug.LogError($"[GoogleAPIController] Error making request (Did you share the sheet with {clientEmail}): {e?.ToString()}");
#if UNITY_EDITOR
            EditorUtility.ClearProgressBar();
#endif
        }
    }

    public static void createGooglePlayEdit(string packageName, Action<string> callback)
    {
        try {
#if UNITY_EDITOR
            EditorUtility.DisplayProgressBar("Create Google Play Edit", "Authenticating", 0f);
#endif
            Instance.whenReady(() => {
#if UNITY_EDITOR
                EditorUtility.DisplayProgressBar("Create Google Play Edit", "Creating Google Play Edit", 0f);
#endif
                GoogleAPIRequest request = new GoogleAPIRequest(googleGenericAPIHost + string.Format(gpEditInsert, packageName),
                    (string data, bool success, GoogleAPIRequest followupRequest) => {
                        if (callback != null)
                            callback(data);
#if UNITY_EDITOR
                        EditorUtility.ClearProgressBar();
#endif
                    });
                request.method = GoogleAPIRequest.HttpMethod.POST;
                request.headers = new Dictionary<string, string>() { { "Content-Type", "application/json" } };
                request.bodyData = Encoding.UTF8.GetBytes("{}"); // Need to pass something but it is ignored so just pass an empty JSON object
                Instance.makeRequest(request);
            });
        } catch (Exception e) {
            MDebug.LogError($"[GoogleAPIController] Error: {e?.ToString()}");
#if UNITY_EDITOR
            EditorUtility.ClearProgressBar();
#endif
        }
    }

    public static void commitGooglePlayEdit(string packageName, string editId, Action<string> callback)
    {
        try {
#if UNITY_EDITOR
            EditorUtility.DisplayProgressBar("Commit Google Play Edit", "Authenticating", 1f);
#endif
            Instance.whenReady(() => {
#if UNITY_EDITOR
                EditorUtility.DisplayProgressBar("Commit Google Play Edit", "Committing Google Play Edit", 1f);
#endif
                GoogleAPIRequest request = new GoogleAPIRequest(googleGenericAPIHost + string.Format(gpEditCommit, packageName, editId),
                    (string data, bool success, GoogleAPIRequest followupRequest) => {
                        if (callback != null)
                            callback(data);
#if UNITY_EDITOR
                        EditorUtility.ClearProgressBar();
#endif
                    });
                request.method = GoogleAPIRequest.HttpMethod.POST;
                request.headers = new Dictionary<string, string>() { { "Content-Type", "application/json" } };
                request.bodyData = Encoding.UTF8.GetBytes("{}"); // Need to pass something but it is ignored so just pass an empty JSON object
                Instance.makeRequest(request);
            });
        } catch (Exception e) {
            MDebug.LogError($"[GoogleAPIController] Error: {e?.ToString()}");
#if UNITY_EDITOR
            EditorUtility.ClearProgressBar();
#endif
        }
    }

    public static void updateGooglePlayListing(string packageName, string editId, string language, string listingData, float progressPercent, Action<string> callback)
    {
        try {
#if UNITY_EDITOR
            EditorUtility.DisplayProgressBar("Update Google Play Listing", "Authenticating", progressPercent);
#endif
            Instance.whenReady(() => {
#if UNITY_EDITOR
                EditorUtility.DisplayProgressBar("Update Google Play Listing", "Updating Google Play Listing", progressPercent);
#endif
                GoogleAPIRequest request = new GoogleAPIRequest(googleGenericAPIHost + string.Format(gpListingUpdate, packageName, editId, language),
                    (string data, bool success, GoogleAPIRequest followupRequest) => {
                        if (callback != null)
                            callback(data);
#if UNITY_EDITOR
                        EditorUtility.ClearProgressBar();
#endif
                    });
                request.method = GoogleAPIRequest.HttpMethod.PUT;
                request.headers = new Dictionary<string, string>() { { "Content-Type", "application/json" } };
                request.bodyData = Encoding.UTF8.GetBytes(listingData);
                Instance.makeRequest(request);
            });
        } catch (Exception e) {
            MDebug.LogError($"[GoogleAPIController] Error: {e?.ToString()}");
#if UNITY_EDITOR
            EditorUtility.ClearProgressBar();
#endif
        }
    }

    public static void deleteGooglePlayImages(string packageName, string editId, string language, string imageType, Action<string> callback)
    {
        try {
#if UNITY_EDITOR
            EditorUtility.DisplayProgressBar("Delete Google Play Images", "Authenticating", 0f);
#endif
            Instance.whenReady(() => {
#if UNITY_EDITOR
                EditorUtility.DisplayProgressBar("Delete Google Play Images", "Deleting Google Play Images", 0f);
#endif
                GoogleAPIRequest request = new GoogleAPIRequest(googleGenericAPIHost + string.Format(gpImageDeleteAll, packageName, editId, language, imageType),
                    (string data, bool success, GoogleAPIRequest followupRequest) => {
                        if (callback != null)
                            callback(data);
#if UNITY_EDITOR
                        EditorUtility.ClearProgressBar();
#endif
                    });
                request.method = GoogleAPIRequest.HttpMethod.DELETE;
                request.headers = new Dictionary<string, string>() { { "Content-Type", "application/json" } };
                request.bodyData = Encoding.UTF8.GetBytes("{}"); // Need to pass something but it is ignored so just pass an empty JSON object
                Instance.makeRequest(request);
            });
        } catch (Exception e) {
            MDebug.LogError($"[GoogleAPIController] Error: {e?.ToString()}");
#if UNITY_EDITOR
            EditorUtility.ClearProgressBar();
#endif
        }
    }

    public static void uploadGooglePlayImage(string packageName, string editId, string language, string imageType, string imagePath, float progressPercent, Action<string> callback)
    {
        try {
#if UNITY_EDITOR
            EditorUtility.DisplayProgressBar("Upload Google Play Images", "Authenticating", progressPercent);
#endif
            Instance.whenReady(() => {
#if UNITY_EDITOR
                EditorUtility.DisplayProgressBar("Upload Google Play Images", "Uploading Google Play Images", progressPercent);
#endif
                GoogleAPIRequest request = new GoogleAPIRequest(googleGenericAPIHost + string.Format(gpImageUpload, packageName, editId, language, imageType),
                    (string data, bool success, GoogleAPIRequest followupRequest) => {
                        if (callback != null)
                            callback(data);
#if UNITY_EDITOR
                        EditorUtility.ClearProgressBar();
#endif
                    });
                request.method = GoogleAPIRequest.HttpMethod.POST;
                request.headers = new Dictionary<string, string>() { { "Content-Type", "image/png" } };
                byte[] imageBytes = System.IO.File.ReadAllBytes(imagePath);
                request.bodyData = imageBytes;
                Instance.makeRequest(request);
            });
        } catch (Exception e) {
            MDebug.LogError($"[GoogleAPIController] Error: {e?.ToString()}");
#if UNITY_EDITOR
            EditorUtility.ClearProgressBar();
#endif
        }
    }

    public static void uploadGooglePlayAPK(string packageName, string editId, string apkPath, Action<string> callback)
    {
        try {
#if UNITY_EDITOR
            EditorUtility.DisplayProgressBar("Upload Google Play APK", "Authenticating", 0f);
#endif
            Instance.whenReady(() => {
#if UNITY_EDITOR
                EditorUtility.DisplayProgressBar("Upload Google Play APK", "Uploading Google Play APK", 0.5f);
#endif
                GoogleAPIRequest request = new GoogleAPIRequest(googleGenericAPIHost + string.Format(gpAPKUpload, packageName, editId),
                    (string data, bool success, GoogleAPIRequest followupRequest) => {
                        if (callback != null)
                            callback(data);
#if UNITY_EDITOR
                        EditorUtility.ClearProgressBar();
#endif
                    });
                request.method = GoogleAPIRequest.HttpMethod.POST;
                request.headers = new Dictionary<string, string>() { { "Content-Type", "application/octet-stream" } };
                byte[] apkBytes = System.IO.File.ReadAllBytes(apkPath);
                request.bodyData = apkBytes;
                Instance.makeRequest(request);
            });
        } catch (Exception e) {
            MDebug.LogError($"[GoogleAPIController] Error: {e?.ToString()}");
#if UNITY_EDITOR
            EditorUtility.ClearProgressBar();
#endif
        }
    }

    public static void updateGooglePlayAPKListing(string packageName, string editId, int versionCode, string language, string listingData, float progressPercent, Action<string> callback)
    {
        try {
#if UNITY_EDITOR
            EditorUtility.DisplayProgressBar("Updating Google Play APK Listing", "Authenticating", progressPercent);
#endif
            Instance.whenReady(() => {
#if UNITY_EDITOR
                EditorUtility.DisplayProgressBar("Updating Google Play APK Listing", "Updating listing", progressPercent);
#endif
                GoogleAPIRequest request = new GoogleAPIRequest(googleGenericAPIHost + string.Format(gpAPKListingUpdate, packageName, editId, versionCode.ToString(), language),
                    (string data, bool success, GoogleAPIRequest followupRequest) => {
                        if (callback != null)
                            callback(data);
#if UNITY_EDITOR
                        EditorUtility.ClearProgressBar();
#endif
                    });
                request.method = GoogleAPIRequest.HttpMethod.PUT;
                request.headers = new Dictionary<string, string>() { { "Content-Type", "application/json" } };
                request.bodyData = Encoding.UTF8.GetBytes(listingData);
                Instance.makeRequest(request);
            });
        } catch (Exception e) {
            MDebug.LogError($"[GoogleAPIController] Error: {e?.ToString()}");
#if UNITY_EDITOR
            EditorUtility.ClearProgressBar();
#endif
        }
    }

    public static void updateGooglePlayTrack(string packageName, string editId, string track, string trackData, Action<string> callback)
    {
        try {
#if UNITY_EDITOR
            EditorUtility.DisplayProgressBar("Updating Google Play Track", "Authenticating", 0f);
#endif
            Instance.whenReady(() => {
#if UNITY_EDITOR
                EditorUtility.DisplayProgressBar("Updating Google Play Track", "Updating track", 0.5f);
#endif
                GoogleAPIRequest request = new GoogleAPIRequest(googleGenericAPIHost + string.Format(gpTrackUpdate, packageName, editId, track),
                    (string data, bool success, GoogleAPIRequest followupRequest) => {
                        if (callback != null)
                            callback(data);
#if UNITY_EDITOR
                        EditorUtility.ClearProgressBar();
#endif
                    });
                request.method = GoogleAPIRequest.HttpMethod.PUT;
                request.headers = new Dictionary<string, string>() { { "Content-Type", "application/json" } };
                request.bodyData = Encoding.UTF8.GetBytes(trackData);
                Instance.makeRequest(request);
            });
        } catch (Exception e) {
            MDebug.LogError($"[GoogleAPIController] Error: {e?.ToString()}");
#if UNITY_EDITOR
            EditorUtility.ClearProgressBar();
#endif
        }
    }

    public static void requestAllAchievements(string applicationID, Action<string> callback)
    {
        try {
#if UNITY_EDITOR
            EditorUtility.DisplayProgressBar("Loading All Achievements", "Authenticating", 0f);
#endif
            Instance.whenReady(() => {
#if UNITY_EDITOR
                EditorUtility.DisplayProgressBar("Loading All Achievements", "Requesting achievements", 0.5f);
#endif
                GoogleAPIRequest request = new GoogleAPIRequest(googleGenericAPIHost + string.Format(gpgsAchievementsList, applicationID),
                    (string data, bool success, GoogleAPIRequest followupRequest) => {
                        if (callback != null)
                            callback(data);
#if UNITY_EDITOR
                        EditorUtility.ClearProgressBar();
#endif
                    });
                Instance.makeRequest(request);
            });
        } catch (Exception e) {
            MDebug.LogError($"[GoogleAPIController] Error: {e?.ToString()}");
#if UNITY_EDITOR
            EditorUtility.ClearProgressBar();
#endif
        }
    }

    public static void updateAchievement(string achievementID, string achievementData, float progressPercent, Action<string> callback)
    {
        try {
#if UNITY_EDITOR
            EditorUtility.DisplayProgressBar("Updating Achievement", "Authenticating", progressPercent);
#endif
            Instance.whenReady(() => {
#if UNITY_EDITOR
                EditorUtility.DisplayProgressBar("Updating Achievement", "Updating achievement", progressPercent);
#endif
                GoogleAPIRequest request = new GoogleAPIRequest(googleGenericAPIHost + string.Format(gpgsAchievementsUpdate, achievementID),
                    (string data, bool success, GoogleAPIRequest followupRequest) => {
                        if (callback != null)
                            callback(data);
#if UNITY_EDITOR
                        EditorUtility.ClearProgressBar();
#endif
                    });
                request.method = GoogleAPIRequest.HttpMethod.PUT;
                request.headers = new Dictionary<string, string>() { { "Content-Type", "application/json" } };
                request.bodyData = Encoding.UTF8.GetBytes(achievementData);
                Instance.makeRequest(request);
            });
        } catch (Exception e) {
            MDebug.LogError($"[GoogleAPIController] Error: {e?.ToString()}");
#if UNITY_EDITOR
            EditorUtility.ClearProgressBar();
#endif
        }
    }

    public static void insertAchievement(string applicationID, string achievementData, float progressPercent, Action<string> callback)
    {
        try {
#if UNITY_EDITOR
            EditorUtility.DisplayProgressBar("Inserting Achievement", "Authenticating", progressPercent);
#endif
            Instance.whenReady(() => {
#if UNITY_EDITOR
                EditorUtility.DisplayProgressBar("Inserting Achievement", "Inserting achievement", progressPercent);
#endif
                GoogleAPIRequest request = new GoogleAPIRequest(googleGenericAPIHost + string.Format(gpgsAchievementsList, applicationID),
                    (string data, bool success, GoogleAPIRequest followupRequest) => {
                        if (callback != null)
                            callback(data);
#if UNITY_EDITOR
                        EditorUtility.ClearProgressBar();
#endif
                    });
                request.method = GoogleAPIRequest.HttpMethod.POST;
                request.headers = new Dictionary<string, string>() { { "Content-Type", "application/json" } };
                request.bodyData = Encoding.UTF8.GetBytes(achievementData);
                Instance.makeRequest(request);
            });
        } catch (Exception e) {
            MDebug.LogError($"[GoogleAPIController] Error: {e?.ToString()}");
#if UNITY_EDITOR
            EditorUtility.ClearProgressBar();
#endif
        }
    }

    public static void uploadAchievementIcon(string achievementId, string imagePath, float progressPercent, Action<string> callback)
    {
        try {
#if UNITY_EDITOR
            EditorUtility.DisplayProgressBar("Upload Achievement Icon", "Authenticating", progressPercent);
#endif
            Instance.whenReady(() => {
#if UNITY_EDITOR
                EditorUtility.DisplayProgressBar("Upload Achievement Icon", "Uploading achievement icon", progressPercent);
#endif
                GoogleAPIRequest request = new GoogleAPIRequest(googleGenericAPIHost + string.Format(gpgsImageUpload, achievementId, "ACHIEVEMENT_ICON"),
                    (string data, bool success, GoogleAPIRequest followupRequest) => {
                        if (callback != null)
                            callback(data);
#if UNITY_EDITOR
                        EditorUtility.ClearProgressBar();
#endif
                    });
                request.method = GoogleAPIRequest.HttpMethod.POST;
                request.headers = new Dictionary<string, string>() { { "Content-Type", "image/png" } };
                byte[] imageBytes = System.IO.File.ReadAllBytes(imagePath);
                request.bodyData = imageBytes;
                Instance.makeRequest(request);
            });
        } catch (Exception e) {
            MDebug.LogError($"[GoogleAPIController] Error: {e?.ToString()}");
#if UNITY_EDITOR
            EditorUtility.ClearProgressBar();
#endif
        }
    }

    private void requestSpreadsheetByName(string sheetID, List<string> tabIDs, Action<string> callback)
    {
        StringBuilder allGoogleDocs = new StringBuilder();
        int count = 0;
        foreach (string tab in tabIDs) {
#if UNITY_EDITOR
            EditorUtility.DisplayProgressBar("Loading Google config", "Fetching " + tab + " (" + (count) + " / " + tabIDs.Count + ")", (float)count / tabIDs.Count);
#endif
            requestSpreadsheet(sheetID, tab, (string data) => {
                count++;
#if UNITY_EDITOR
                EditorUtility.DisplayProgressBar("Loading Google config", "Received " + tab + " (" + (count) + " / " + tabIDs.Count + ")", (float)count / tabIDs.Count);
#endif
                allGoogleDocs.Append(sheetJsonToFlatFile(data));
                if (count == tabIDs.Count)
                    callback(allGoogleDocs.ToString());
            });
        }
    }

    private void requestSpreadsheet(string sheetID, string tabID, Action<string> callback)
    {
        GoogleAPIRequest request = new GoogleAPIRequest(googleSheetAPIHost + sheetsURI + sheetID + sheetsCommandAllValues + Uri.EscapeDataString(tabID) + "?majorDimension=ROWS",
            (string data, bool success, GoogleAPIRequest followupRequest) => {
                if (callback != null)
                    callback(data);
            });
        makeRequest(request);
    }

    private void requestSpreadsheetMapping(string sheetID, Action<string> callback)
    {
        GoogleAPIRequest request = new GoogleAPIRequest(googleSheetAPIHost + sheetsURI + sheetID,
            (string data, bool success, GoogleAPIRequest followupRequest) => {
                if (callback != null)
                    callback(data);
            });
        makeRequest(request);
    }

    private void makeRequest(GoogleAPIRequest request)
    {
        if (ready()) {
            request.headers.Add("Authorization", token.token_type + " " + token.access_token);
            request.send(null);
        } else {
            authenticate(request);
        }
    }

    private string sheetJsonToFlatFile(string inputSheetJson)
    {
        try {
            StringBuilder sheetData = new StringBuilder();
            Dictionary<string, object> sheetObject = GenericsJSONParser.JsonDecode(inputSheetJson) as Dictionary<string, object>;
            List<object> sheet = sheetObject["values"] as List<object>;

            foreach (List<object> row in sheet) {
                foreach (string cell in row) {
                    sheetData.Append(cell + "\t");
                }
                sheetData.Append("\n");
            }

            return sheetData.ToString();
        } catch (Exception e) {
            MDebug.LogError($"[GoogleAPIController] Error parsing sheet JSON: {e?.ToString()}");
            return "";
        }
    }

    private bool ready()
    {
        return token != null && token.access_token != "" && tokenExpiryTime > getUtcUnixTimestamp();
    }

    private static double getUtcUnixTimestamp()
    {
        DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        TimeSpan diff = DateTime.UtcNow - origin;
        return diff.TotalSeconds;
    }

    private void whenReady(Action readyCall)
    {
        if (!ready()) {
            authenticate(null, () => {
                if (readyCall != null)
                    readyCall();
            });
        } else if (readyCall != null) {
            readyCall();
        }
    }

    private void authenticate(GoogleAPIRequest followupRequest, Action doneCallback = null)
    {
        int timeToEpoch = (int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
        int expireyTime = timeToEpoch + 3600; //plus 60 minutes

        Dictionary<string, string> JWTLoginData = new Dictionary<string, string>() {
            { "iss", clientEmail},
            { "scope", scope},
            { "aud", googleGenericAPIHost + authURI },
            { "exp", expireyTime.ToString() },
            { "iat", timeToEpoch.ToString() }
        };

        Dictionary<string, string> fields = new Dictionary<string, string>() {
            { "grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer" },
            { "assertion", createJWT(JWTLoginData) }
        };

        GoogleAPIRequest request = new GoogleAPIRequest(
            googleGenericAPIHost + authURI,
            (string data, bool success, GoogleAPIRequest callbackFollowupRequest) => {
                MDebug.Log("[GoogleAPIController] Got auth req back");
                if (success) {
                    token = GoogleAuthToken.CreateFromJSON(data);
                    tokenExpiryTime = getUtcUnixTimestamp() + token.expires_in - 300; // minus 5 minutes just for safety
                    if (doneCallback != null) doneCallback();
                    if (callbackFollowupRequest != null) makeRequest(callbackFollowupRequest);
                } else {
                    Debug.LogError("[GoogleAPIController] Error authenticating: " + data);
                }
            }
        );

        request.method = GoogleAPIRequest.HttpMethod.POST;
        request.headers = new Dictionary<string, string>() { { "Content-Type", "application/x-www-form-urlencoded" } };
        request.postFields = fields;

        MDebug.Log("[GoogleAPIController] Sending auth req");
        request.send(followupRequest);
    }

    private string createJWT(Dictionary<string, string> data)
    {
        //Build the data dict in to JSON dict wew!
        StringBuilder bodyJSON = new StringBuilder();
        bodyJSON.Append("{"); // open JSON object
        bool first = true;
        foreach (KeyValuePair<string, string> kvp in data) {
            if (first) first = false; // don't add a comma to the first entry 
            else bodyJSON.Append(","); // do add a comma to all other entries

            bodyJSON.Append("\"" + kvp.Key + "\":\"" + kvp.Value + "\"");
        }
        bodyJSON.Append("}"); // close JSON object

        //Google only accepts RSA SHA256 signed JWT tokens
        string jwt = encodeBase64("{ \"alg\":\"RS256\",\"typ\":\"JWT\"}") + "." + encodeBase64(bodyJSON.ToString());
        //sign the JWT and return the it
        return jwt + "." + RsaSign(jwt);
    }

    private string RsaSign(string data)
    {
        //Turn the input string into a byte array so that we can process it
        return RsaSign(System.Text.Encoding.UTF8.GetBytes(data));
    }

    private string RsaSign(byte[] data)
    {
        try {
#if !NETFX_CORE
            //Open the service account's p12 key file
            X509Certificate2 cert = new X509Certificate2(decodeBase64(certBase64), certPassword);
            //Initialise the RSACryptoServiceProvider from the p12 key file
            using (RSACryptoServiceProvider rsa = (RSACryptoServiceProvider)cert.PrivateKey) {
                //Generate the SHA256 hash
                byte[] hash;
                using (SHA256 sha256 = SHA256.Create()) {
                    hash = sha256.ComputeHash(data);
                }

                //Set the RSA's algorithm to SHA256.
                RSAPKCS1SignatureFormatter RSAFormatter = new RSAPKCS1SignatureFormatter(rsa);
                RSAFormatter.SetHashAlgorithm("SHA256");

                //Sign the hash and return it base64 encoded.
                byte[] SignedHash = RSAFormatter.CreateSignature(hash);
                return encodeBase64(SignedHash);
            }
#else
            throw new Exception("System.Security.Cryptography.X509Certificates namespace not available on this platform");
#endif
        } catch (CryptographicException e) {
            //Catch any errors and return an empty string
            Debug.LogError($"[GoogleAPIController] Error: {e?.ToString()}");
            return "";
        }
    }

    private string encodeBase64(string str)
    {
        return encodeBase64(System.Text.Encoding.UTF8.GetBytes(str));
    }

    private string encodeBase64(byte[] byteArr)
    {
        return System.Convert.ToBase64String(byteArr);
    }

    private byte[] decodeBase64(string byteArrBase64)
    {
        return Convert.FromBase64String(byteArrBase64);
    }
}
