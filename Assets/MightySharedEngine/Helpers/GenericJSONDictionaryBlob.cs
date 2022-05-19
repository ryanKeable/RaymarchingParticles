using System.Collections;
using System.Collections.Generic;
public class GenericJSONDictionaryBlob
{
    public Dictionary<string, object> blob;

    public static GenericJSONDictionaryBlob blobFromFile(string filePath)
    {
        string raw = SystemHelper.stringWithContentsOfFile(filePath);
        if (raw == null) return null;
        return blobFromString(raw);
    }

    public static GenericJSONDictionaryBlob blobFromString(string rawJSON)
    {
        object rawBlob = GenericsJSONParser.JsonDecode(rawJSON);
        if (rawBlob == null)
        {
            MDebug.LogRed($"CANNOT PARSE JSON: {rawJSON}");
            return null;
        }
        return new GenericJSONDictionaryBlob(rawBlob);
    }

    public GenericJSONDictionaryBlob()
    {
        blob = new Dictionary<string, object>();
    }

    public GenericJSONDictionaryBlob(object obj)
    {
        Dictionary<string, object> tempBlob = obj as Dictionary<string, object>;
        // if (tempBlob.ContainsKey("blob"))
        // {
        //     blob = tempBlob["blob"] as Dictionary<string, object>;
        // }
        // else
        // {
        blob = tempBlob;
        // }
    }

    public override string ToString()
    {
        string raw = GenericsJSONParser.JsonEncode(blob);
        return raw;
    }

    public List<string> allKeys()
    {
        return new List<string>(blob.Keys);
    }

    public bool hasKey(string key)
    {
        return blob.ContainsKey(key);
    }

    public GenericJSONDictionaryBlob genericJSONDictionaryWithKey(string key)
    {
        object blorb = itemWithKey(key);
        if (blorb == null) return null;
        return new GenericJSONDictionaryBlob(blorb);
    }

    // will overwrite existing entries if they collide
    public void addEntriesFromDictionary(Dictionary<string, object> source)
    {
        foreach (string key in source.Keys)
        {
            setItemForKey(key, source[key]);
        }
    }

    public void setItemForKey(string key, object item)
    {
        blob[key] = item;
    }

    public object itemWithKey(string key)
    {
        if (blob == null) return null;
        object theVal;
        if (blob.TryGetValue(key, out theVal)) return theVal;
        return null;
    }

    public List<object> listWithKey(string key)
    {
        object item = itemWithKey(key);
        if (item != null) return item as List<object>;
        return new List<object>();
    }

    public List<string> stringListWithKey(string key)
    {
        object item = itemWithKey(key);
        List<string> theList = new List<string>();
        if (item != null)
        {
            List<object> itemList = item as List<object>;
            for (int i = 0; i < itemList.Count; i++)
            {
                theList.Add(itemList[i] as string);
            }
        }
        return theList;
    }

    public int intWithKey(string key, int defaultArg = 0)
    {
        return MathfExtensions.parseInt(itemWithKey(key), defaultArg);
    }

    public bool flagWithKey(string key, bool defaultArg = false)
    {
        return boolWithKey(key, defaultArg);
    }

    public bool boolWithKey(string key, bool defaultArg = false)
    {
        return MathfExtensions.parseBool(itemWithKey(key), defaultArg);
    }

    public float floatWithKey(string key, float defaultArg = 0.0f)
    {
        return MathfExtensions.parseFloat(itemWithKey(key), defaultArg);
    }

    public long longWithKey(string key, long defaultArg = 0)
    {
        return MathfExtensions.parseLong(itemWithKey(key), defaultArg);
    }

    public double doubleWithKey(string key, double defaultArg = 0.0)
    {
        return MathfExtensions.parseDouble(itemWithKey(key), defaultArg);
    }


    public string stringWithKey(string key, string defaultValue = null)
    {
        object arg = itemWithKey(key);
        if (arg == null) return defaultValue;
        return arg as string;
    }

}
