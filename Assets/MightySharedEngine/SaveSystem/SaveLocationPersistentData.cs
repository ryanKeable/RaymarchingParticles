using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

public sealed class SaveLocationPersistentData : SaveLocation
{
    private string filePath;
    private string filePathDirectoryName;
    private string filePathFileName;
    private int fileIndex = 0;

    private const int rollingFileCount = 5;

    //used for support checks
    public static string isRecordedSaveMostRecentFile {
        get { return PlayerPrefs.GetString("sv_recentChk", "none"); }
        private set { PlayerPrefs.SetString("sv_recentChk", value); }
    }

    public override void Initialise()
    {
        filePath = GetSaveFilePath();
        filePathDirectoryName = SystemHelper.GetDirectoryNameSafe(filePath);
        filePathFileName = SystemHelper.GetFileNameSafe(filePath);

        Debug.Log($"[SaveLocationPersistentData] Initialise - Save file location: [{filePath}] filename : [{filePathFileName}]");
    }

    public override void Terminate()
    {
        Debug.Log("[SaveLocationPersistentData] Terminate");
    }

    public override void Update()
    {
    }

    public override void SaveData(string saveData, Action saveCompleteCallback)
    {
        fileIndex++;
        if (fileIndex >= rollingFileCount) fileIndex = 0;

        string fileToSave = $"{filePath}.2.{fileIndex}";

        bool didSave = SystemHelper.saveStringToFile(saveData, fileToSave);

        if (didSave) {
            string lastSavedRecordFilePath = GetLastSavedRecordFilePath();
            didSave = SystemHelper.saveStringToFile(fileToSave, lastSavedRecordFilePath);

            if (!didSave) {
                // Let's try again, but let's first delete the previous file
                SystemHelper.deleteFileAtPath(lastSavedRecordFilePath);
                SystemHelper.saveStringToFile(fileToSave, lastSavedRecordFilePath);
            }
        }

        if (saveCompleteCallback != null) saveCompleteCallback();
    }

    public override void LoadData(Action<string> loadCompleteCallback)
    {
        string lastUpdated = GetLastUpdatedFileInDirectory(filePathDirectoryName, filePathFileName);

        if (string.IsNullOrEmpty(lastUpdated)) {
            if (loadCompleteCallback != null) loadCompleteCallback(null);
            return;
        }

        string result = SystemHelper.stringWithContentsOfFile(lastUpdated);
        if (loadCompleteCallback != null) loadCompleteCallback(result);
    }

    public override int GetSaveFileCount()
    {
        string[] files = SystemHelper.getFiles(filePathDirectoryName);
        int result = 0;

        int filesLength = files.Length;
        for (int i = 0; i < filesLength; i++) {
            if (files[i].Contains(filePathFileName) && !files[i].EndsWithFast(".corrupt")) {
                result++;
            }
        }

        return result;
    }

    public override void DeleteLastUpdatedFile()
    {
        string lastUpdated = GetLastUpdatedFileInDirectory(filePathDirectoryName, filePathFileName);
        if (string.IsNullOrEmpty(lastUpdated)) {
            return;
        }

        SystemHelper.moveFile(lastUpdated, lastUpdated + ".corrupt");

        string lastSavedRecordFilePath = GetLastSavedRecordFilePath();
        if (!string.IsNullOrEmpty(lastSavedRecordFilePath)) {
            SystemHelper.deleteFileAtPath(lastSavedRecordFilePath);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetSaveFilePath()
    {
        return GetFilePath("core.dmp");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetLastSavedRecordFilePath()
    {
        return GetFilePath("rec.dmp");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string GetFilePath(string name)
    {
        string filePath = SystemHelper.documentFilePath(name);

#if UNITY_EDITOR
        filePath += "." + Application.platform.ToString();
#endif

#if (UNITY_STANDALONE && !UNITY_EDITOR) || AUTOTEST_ENABLED
        if (EnvironmentController.boolForKey("autoTestingMode")) {
            filePath += "." + EnvironmentController.stringForKey("testBotAppName");
        }
#endif

        return filePath;
    }

    // Called from the game state controller editor
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string LastUpdatedFile(string path)
    {
        return GetLastUpdatedFileInDirectory(Path.GetDirectoryName(path), Path.GetFileName(path));
    }

    private static string GetLastUpdatedFileInDirectory(string directory, string filePrefix)
    {
        string[] files = SystemHelper.getFiles(directory);

        string lastSavedRecordFilePath = GetLastSavedRecordFilePath();
        string lastSavedFilePath = SystemHelper.stringWithContentsOfFile(lastSavedRecordFilePath);

        string mostRecentlyChangedFile = GetFileWithMostRecentLocalWriteTime(files, filePrefix);

        if (!string.IsNullOrEmpty(lastSavedFilePath)) {
            string lastSavedRecordFileName = SystemHelper.GetFileNameSafe(lastSavedFilePath);

            if (!string.IsNullOrEmpty(lastSavedRecordFileName)) {
                int filesLength = files.Length;
                for (int i = 0; i < filesLength; i++) {
                    if (files[i].EndsWithFast(lastSavedRecordFileName)) {
                        // MDebug.Log($"[SaveLocationPersistentData] recorded last saved: [{lastSavedRecordFileName}] file path is: [{files[i]}]");

                        bool isFileMostRecentChanged = string.Equals(files[i], mostRecentlyChangedFile, StringComparison.OrdinalIgnoreCase);
                        isRecordedSaveMostRecentFile = isFileMostRecentChanged ? "y" : "n";

                        return files[i];
                    }
                }
            }
        }

        // If we haven't recorded a save being made just use the most recent file
        return mostRecentlyChangedFile;
    }

    private static string GetFileWithMostRecentLocalWriteTime(string[] files, string filePrefix)
    {
        string lastUpdatedFile = "";
        DateTime lastUpdate = DateTime.MinValue;

        int filesLength = files.Length;
        for (int i = 0; i < filesLength; i++) {
            try {
                if (!files[i].Contains(filePrefix)) continue;
                if (files[i].EndsWithFast(".corrupt")) continue;

                if (File.GetLastWriteTime(files[i]) > lastUpdate) {
                    lastUpdate = File.GetLastWriteTime(files[i]);
                    lastUpdatedFile = files[i];
                }
            } catch (Exception e) {
                MDebug.LogError($"[SaveLocationPersistentData] GetFileWithMostRecentLocalWriteTime() encountered exception: {e?.ToString()}");
            }
        }

        return lastUpdatedFile;
    }

    public override void DumpAllSaveFiles(Action<List<SaveFileDump>> dumpCompleteCallback)
    {
        List<SaveFileDump> result = new List<SaveFileDump>();

        List<string> possibleFilePaths = new List<string>(rollingFileCount * 2 + 1);

        string playerPrefsPath = SystemHelper.playerPrefsSavePath();
        if (!string.IsNullOrEmpty(playerPrefsPath)) possibleFilePaths.Add(playerPrefsPath);

        possibleFilePaths.Add(GetLastSavedRecordFilePath());

        string saveFilePath = GetSaveFilePath();
        for (int i = 0; i < rollingFileCount; i++) {
            possibleFilePaths.Add(saveFilePath + ".2." + i.ToString());
            possibleFilePaths.Add(saveFilePath + ".2." + i.ToString() + ".online");

            possibleFilePaths.Add(saveFilePath + ".2." + i.ToString() + ".corrupt");
            possibleFilePaths.Add(saveFilePath + ".2." + i.ToString() + ".online.corrupt");
        }

        for (int i = 0; i < possibleFilePaths.Count; i++) {
            if (!string.IsNullOrEmpty(possibleFilePaths[i]) && SystemHelper.fileExists(possibleFilePaths[i])) {
                long fileSize = SystemHelper.getFileSize(possibleFilePaths[i]);
                DateTime lastModifiedDate = SystemHelper.getLastModifiedDate(possibleFilePaths[i]);
                string data = SystemHelper.stringWithContentsOfFile(possibleFilePaths[i]);

                string readError = "";
                if (string.IsNullOrEmpty(data)) {
                    data = "";
                    readError = SystemHelper.getReadErrorForFile(possibleFilePaths[i]);
                }

                result.Add(new SaveFileDump(possibleFilePaths[i], fileSize, lastModifiedDate, data, readError));
            }
        }

        if (dumpCompleteCallback != null) dumpCompleteCallback(result);
    }
}
