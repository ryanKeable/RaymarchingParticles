using System.Collections.Generic;

[System.Serializable]
public sealed class GoogleDocUri
{
    public string name;
    public string googleDocID;
    public bool publishedDoc = false;
    public List<string> sheetIds = new List<string> { "0" };
}
