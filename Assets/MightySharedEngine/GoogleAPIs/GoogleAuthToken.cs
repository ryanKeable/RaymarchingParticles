using UnityEngine;

[System.Serializable]
public sealed class GoogleAuthToken
{
    public string access_token;
    public string token_type;
    public int expires_in;

    public static GoogleAuthToken CreateFromJSON(string jsonString)
    {
        return JsonUtility.FromJson<GoogleAuthToken>(jsonString);
    }
}
