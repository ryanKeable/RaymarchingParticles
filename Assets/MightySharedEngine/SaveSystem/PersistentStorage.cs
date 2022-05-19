using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Text;

public sealed class PersistentStorage
{
    ConcurrentDictionary<string, string> prefMap = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    SaveLocation saveLocation;
    Action<bool> loadCompleteCallback = null;
    bool currentlySaving = false;
    StringBuilder stringBuilder = new StringBuilder(4096);

    const string lineBreak = "%$%";
    const string tab = "!%#%!";

    // yeah, never change this.
    //	string version1Sig = "MIIEpAIBAAKCAQEAwf8F+B6P51JPihz3Q3tvxynu/qZV8SEbZaKZkWbicee+ZpERyr+L6WtWwRzTptJ9mHHk8DpczeOnkJRUqM3/vk0cj0qPTAzYiU1KX1OntGzKZxyMS2SHRtyN2k7znnTOzBSsQsWALCVrGONvBuJNQh8pfgCvHgp7wqD7eaHjIJveobhqFkY+hxDjlP+/GNwyXxeBO1LC0xWawX6SuOUOt1V0oLrg82KD2qLiEdcaqaq1RrUo";
    string version2Sig = "v2MIIEpAIBAAKCAQEAwfk0cj0qPTAzYiU1KX1OntGzKZxyMS2SHRtyN2k7znnTOzBSsQsWALCVrGONvBuJNQh8pfgCvHgp7wqD7eaHjIJveobhqFkYhxDjlGNwyXxeBO1LC0xWawX6SuOUOt1ALCVrGONvBuJNQh8pfgCvHgp7wqD7eaHjIJ0oLrg82KD2qLiEdcaqaq1RrUV0oL8FB6P51JPihz3Q3tvxynuqZVxyMS2SHRtyN2k7znnTOzBSsQsW";

    public bool stateHasChanged { get; private set; }
    public int corruptSavesFound { get; private set; }

    public static PersistentStorage storageFromSaveFile()
    {
        PersistentStorage storage = new PersistentStorage();
        storage.saveLocationInitialise();
        return storage;
    }

    public static PersistentStorage storageFromString(string rawContent)
    {
        PersistentStorage storage = new PersistentStorage();
        rawContent = rawContent.Replace(lineBreak, "\n").Replace(tab, "\t");
        storage.loadFromRaw(rawContent);
        return storage;
    }

    public PersistentStorage()
    {
        version2Sig = version2Sig.ToLower();
    }

    public void saveLocationInitialise()
    {
#if UNITY_PS4 && !UNITY_EDITOR
        saveLocation = new SaveLocationPS4();
#elif UNITY_XBOXONE && !UNITY_EDITOR
        // TODO: Get Xbox One save files working
        saveLocation = new SaveLocationNull();
#elif UNITY_SWITCH && !UNITY_EDITOR
        saveLocation = new SaveLocationSwitch();
#else
        saveLocation = new SaveLocationPersistentData();
#endif
        saveLocation.Initialise();
    }

    public void saveLocationTerminate()
    {
        if (saveLocation != null)
        {
            saveLocation.Terminate();
            saveLocation = null;
        }
    }

    public void saveLocationUpdate()
    {
        if (saveLocation != null)
        {
            saveLocation.Update();
        }
    }

    public void deleteAll()
    {
        prefMap.Clear();
    }

    public void save()
    {
        saveToDisk();
    }

    public List<string> allKeys()
    {
        List<string> theKeys = new List<string>(prefMap.Keys);
        return theKeys;
    }

    public void removeKey(string key)
    {
        string raw = stringForKey(key);
        if (raw != null)
        {
            prefMap[key] = null;
        }
    }

    // mostly just for editor
    public void saveToFile(string file)
    {
        stringBuilder.Remove(0, stringBuilder.Length);
        // bleah
        int checksum = 0;
        foreach (string key in prefMap.Keys)
        {
            if (prefMap[key] != null)
            {
                stringBuilder.Append(key);
                stringBuilder.Append("\t");
                stringBuilder.Append(prefMap[key]);
                stringBuilder.Append("\n");

                checksum += checksumForString(key);
                checksum += checksumForString(prefMap[key]);
            }
        }
        stringBuilder.Insert(0, "\n");
        stringBuilder.Insert(0, checksum.ToString());

        SystemHelper.saveStringToFile(stringBuilder.ToString(), file);

        stateHasChanged = false;
    }

    public string stringForKey(string key, string defaultString = null)
    {
        if (key == null) return defaultString;
        string outString;
        if (!prefMap.TryGetValue(key, out outString))
        {
            return defaultString;
        }
        return outString;
    }

    public int intForKey(string key, int defaultInt = 0)
    {
        if (key == null) return defaultInt;
        string theString = stringForKey(key);
        if (theString == null) return defaultInt;
        return MathfExtensions.parseInt(theString);
    }

    public float floatForKey(string key, float defaultFloat = 0f)
    {
        if (key == null) return defaultFloat;
        string theString = stringForKey(key);
        if (theString == null) return defaultFloat;
        return MathfExtensions.parseFloat(theString);
    }

    public void setFloat(string key, float theFloat)
    {
        if (key == null) return;
        setString(key, theFloat.ToString());
    }

    public void setInt(string key, int theInt)
    {
        if (key == null) return;
        setString(key, theInt.ToString());
    }

    // Yeah, everything is just a string, keeps clouds happy
    public void setString(string key, string theString)
    {
        if (key == null) return;
        prefMap[key] = theString;
        stateHasChanged = true;
    }

    public void loadFromDisk(Action<bool> loadCompleteCallback)
    {
        this.loadCompleteCallback = loadCompleteCallback;

        if (saveLocation.GetSaveFileCount() == 0)
        {
            if (loadCompleteCallback != null) loadCompleteCallback(false);
            return;
        }

        saveLocation.LoadData(loadCallback);
    }

    void loadCallback(string saveData)
    {
        if (string.IsNullOrEmpty(saveData))
        {
            saveLocation.DeleteLastUpdatedFile();
            if (saveLocation.GetSaveFileCount() > 0)
            {
                saveLocation.LoadData(loadCallback);
            }
            else
            {
                if (loadCompleteCallback != null) loadCompleteCallback(false);
            }
            return;
        }

        if (fillPrefMapAndVerify(saveData))
        {
            // Success
            if (loadCompleteCallback != null) loadCompleteCallback(true);
        }
        else
        {
            // Fail
            saveLocation.DeleteLastUpdatedFile();
            if (saveLocation.GetSaveFileCount() > 0)
            {
                saveLocation.LoadData(loadCallback);
            }
            else
            {
                if (loadCompleteCallback != null) loadCompleteCallback(false);
            }
        }
    }

    bool fillPrefMapAndVerify(string raw)
    {
        raw = unobfuscate(raw, version2Sig);
        raw = raw.Replace(lineBreak, "\n").Replace(tab, "\t");
        string[] lines = raw.Split('\n');
        int linesLength = lines.Length;

        int checksum = MathfExtensions.parseInt(lines[0], -1);
        if (checksum < 0)
        {
            MDebug.LogError($"[PersistentStorage] fillPrefMapAndVerify - Failed to parse checksum string: '{checksum}'");
            return false;
        }

        int sumToCheck = 0;
        for (int i = 1; i < linesLength; i++)
        {
            string[] tokens = lines[i].Split('\t');
            if (tokens.Length < 2) continue;
            prefMap[tokens[0]] = tokens[1];

            sumToCheck += checksumForString(tokens[0]);
            sumToCheck += checksumForString(tokens[1]);
        }
        if (sumToCheck == checksum) return true;

        MDebug.LogError("[PersistentStorage] fillPrefMapAndVerify - Checksum comparison failed so am clearing content");
        corruptSavesFound++;
        prefMap.Clear();
        return false;
    }

    void validateLastSave(Action<bool> validateCallback)
    {
        // No point in doing anything if we aren't passed a callback
        if (validateCallback == null)
        {
            MDebug.LogError("[PersistentStorage] validateLastSave() was passed a null callback");
            return;
        }

        if (saveLocation.GetSaveFileCount() == 0)
        {
            validateCallback(false);
            return;
        }

        saveLocation.LoadData((string saveData) =>
        {
            if (string.IsNullOrEmpty(saveData))
            {
                validateCallback(false);
            }
            else
            {
                validateCallback(validateData(saveData));
            }
        });
    }

    bool validateData(string raw)
    {
        raw = unobfuscate(raw, version2Sig);
        raw = raw.Replace(lineBreak, "\n").Replace(tab, "\t");
        string[] lines = raw.Split('\n');
        int linesLength = lines.Length;

        int checksum = MathfExtensions.parseInt(lines[0], -1);
        if (checksum < 0)
        {
            MDebug.LogError($"[PersistentStorage] validateData - Failed to parse checksum string: '{checksum}'");
            return false;
        }

        int sumToCheck = 0;
        for (int i = 1; i < linesLength; i++)
        {
            string[] tokens = lines[i].Split('\t');
            if (tokens.Length < 2) continue;

            sumToCheck += checksumForString(tokens[0]);
            sumToCheck += checksumForString(tokens[1]);
        }
        if (sumToCheck == checksum) return true;

        MDebug.LogError("[PersistentStorage] validateData - Checksum comparison failed");
        return false;
    }

    string obfuscate(string source, string sig)
    {
        stringBuilder.Remove(0, stringBuilder.Length);
        int md5Index = 3;
        char[] letters = source.ToCharArray();
        int lettersLength = letters.Length;
        for (int i = 0; i < lettersLength; i++)
        {
            letters[i] += sig[md5Index];
            md5Index++;
            if (md5Index >= sig.Length) md5Index = 0;
        }
        stringBuilder.Append(letters);
        return stringBuilder.ToString();
    }

    string unobfuscate(string source, string sig)
    {
        stringBuilder.Remove(0, stringBuilder.Length);
        int md5Index = 3;

        char[] letters = source.ToCharArray();
        int lettersLength = letters.Length;
        for (int i = 0; i < lettersLength; i++)
        {
            letters[i] -= sig[md5Index];
            md5Index++;
            if (md5Index >= sig.Length) md5Index = 0;
        }
        stringBuilder.Append(letters);
        return stringBuilder.ToString();
    }

    bool loadFromRaw(string rawContent)
    {
        string[] lines = rawContent.Split('\n');
        int linesLength = lines.Length;

        int checksum = MathfExtensions.parseInt(lines[0], -1);
        if (checksum < 0)
        {
            MDebug.LogError($"[PersistentStorage] loadFromRaw - Failed to parse checksum string: '{checksum}'");
            return false;
        }

        int sumToCheck = 0;
        for (int i = 1; i < linesLength; i++)
        {
            string[] tokens = lines[i].Split('\t');
            if (tokens.Length < 2) continue;
            prefMap[tokens[0]] = tokens[1];

            sumToCheck += checksumForString(tokens[0]);
            sumToCheck += checksumForString(tokens[1]);
        }
        if (sumToCheck == checksum) return true;

        MDebug.LogError("[PersistentStorage] loadFromRaw - Checksum comparison failed so am clearing content");
        prefMap.Clear();
        return false;
    }

    public string storeAsString()
    {
        stringBuilder.Remove(0, stringBuilder.Length);
        // bleah
        int checksum = 0;
        foreach (string key in prefMap.Keys)
        {
            if (prefMap[key] != null)
            {
                stringBuilder.Append(key);
                stringBuilder.Append(tab);
                stringBuilder.Append(prefMap[key]);
                stringBuilder.Append(lineBreak);

                checksum += checksumForString(key);
                checksum += checksumForString(prefMap[key]);

#if UNITY_EDITOR || UNITY_STANDALONE
                if (key.Contains(tab) || key.Contains(lineBreak) || prefMap[key].Contains(tab) || prefMap[key].Contains(lineBreak))
                {
                    MDebug.LogError($"[PersistentStorage] storeAsString - Line contains special tab or line break substring which is going to break parsing later - Key: '{key}' - Value: '{prefMap[key]}'");
                }
#endif
            }
        }
        stringBuilder.Insert(0, lineBreak);
        stringBuilder.Insert(0, checksum.ToString());

        return stringBuilder.ToString();
    }

    int checksumForString(string source)
    {
        if (source == null) return 0;
        int checksum = 0;
        int sourceLength = source.Length;
        for (int i = 0; i < sourceLength; i++)
        {
            checksum += source[i];
        }
        return checksum;
    }

    void saveToDisk(bool validateSave = true)
    {
        if (currentlySaving) return;
        currentlySaving = true;
        // MDebug.LogGreen($"SAVE!! {SaveLocationPersistentData.GetSaveFilePath()}");
        string saveData = obfuscate(storeAsString(), version2Sig);
        saveLocation.SaveData(saveData, () =>
        {
            currentlySaving = false;

            if (validateSave)
            {
                validateLastSave((bool wasValidated) =>
                {
                    if (!wasValidated)
                    {
                        // If the last save file validation failed, let's try to save again.
                        // Don't bother validating the next save or we'll get into an infinite loop.
                        saveToDisk(false);
                    }
                });
            }
        });

        stateHasChanged = false;
    }

    public void DumpAllSaveFiles(Action<List<SaveFileDump>> dumpCompleteCallback)
    {
        if (saveLocation != null)
        {
            saveLocation.DumpAllSaveFiles(dumpCompleteCallback);
        }
        else if (dumpCompleteCallback != null)
        {
            dumpCompleteCallback(null);
        }
    }
}

[System.Serializable]
public sealed class SaveFileDump
{
    public string Filename;
    public long FileSize;
    public string LastModifiedDate;
    public string Data;
    public string ReadError;

    public SaveFileDump() { }

    public SaveFileDump(string filename, long fileSize, string lastModifiedDate, string data, string readError)
    {
        this.Filename = filename;
        this.FileSize = fileSize;
        this.LastModifiedDate = lastModifiedDate;
        this.Data = data;
        this.ReadError = readError;
    }

    public SaveFileDump(string filename, long fileSize, DateTime lastModifiedDate, string data, string readError)
    {
        this.Filename = filename;
        this.FileSize = fileSize;
        this.Data = data;
        this.ReadError = readError;

        if (lastModifiedDate == DateTime.MinValue)
        {
            this.LastModifiedDate = "";
        }
        else
        {
            this.LastModifiedDate = lastModifiedDate.ToString("yyyy-MM-dd hh:mm:ss tt", CultureInfo.CreateSpecificCulture("en-US"));
        }
    }

    public void Base64EncodeData()
    {
        Filename = Convert.ToBase64String(Encoding.UTF8.GetBytes(Filename));
        LastModifiedDate = Convert.ToBase64String(Encoding.UTF8.GetBytes(LastModifiedDate));
        Data = Convert.ToBase64String(Encoding.UTF8.GetBytes(Data));
        ReadError = Convert.ToBase64String(Encoding.UTF8.GetBytes(ReadError));
    }

    public void Base64DecodeData()
    {
        Filename = Encoding.UTF8.GetString(Convert.FromBase64String(Filename));
        LastModifiedDate = Encoding.UTF8.GetString(Convert.FromBase64String(LastModifiedDate));
        Data = Encoding.UTF8.GetString(Convert.FromBase64String(Data));
        ReadError = Encoding.UTF8.GetString(Convert.FromBase64String(ReadError));
    }
}
