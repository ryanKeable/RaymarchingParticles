#if UNITY_SWITCH

using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

public sealed class SaveLocationSwitch : SaveLocation
{
    private const string mountName = "MySave";
    private const string fileName = "MySaveData";

    private nn.account.Uid userId;
    private string filePath;
    private nn.fs.FileHandle fileHandle = new nn.fs.FileHandle();

    public override void Initialise()
    {
        Debug.Log("[SaveLocationSwitch] Initialise");

        nn.account.Account.Initialize();
        nn.account.UserHandle userHandle = new nn.account.UserHandle();

        nn.account.Account.OpenPreselectedUser(ref userHandle);
        nn.account.Account.GetUserId(ref userId, userHandle);

        nn.Result result = nn.fs.SaveData.Mount(mountName, userId);
        result.abortUnlessSuccess();

        filePath = string.Format("{0}:/{1}", mountName, fileName);
    }

    public override void Terminate()
    {
        Debug.Log("[SaveLocationSwitch] Terminate");

        nn.fs.FileSystem.Unmount(mountName);
    }

    public override void Update() { }

    public override void SaveData(string saveData, Action saveCompleteCallback)
    {
        // Nintendo Switch Guideline 0080
        UnityEngine.Switch.Notification.EnterExitRequestHandlingSection();

        try {
            byte[] data = Encoding.Unicode.GetBytes(saveData);

            nn.Result result = nn.fs.File.Delete(filePath);
            if (!nn.fs.FileSystem.ResultPathNotFound.Includes(result)) {
                result.abortUnlessSuccess();
            }

            result = nn.fs.File.Create(filePath, data.LongLength);
            result.abortUnlessSuccess();

            result = nn.fs.File.Open(ref fileHandle, filePath, nn.fs.OpenFileMode.Write);
            result.abortUnlessSuccess();

            result = nn.fs.File.Write(fileHandle, 0, data, data.LongLength, nn.fs.WriteOption.Flush);
            result.abortUnlessSuccess();

            nn.fs.File.Close(fileHandle);
            result = nn.fs.SaveData.Commit(mountName);
            result.abortUnlessSuccess();
        } catch (System.Exception e) {
            Debug.LogError($"[SaveLocationSwitch] Error saving to file path '{filePath}'. Exception: {e?.ToString()}");
        }

        // Nintendo Switch Guideline 0080
        UnityEngine.Switch.Notification.LeaveExitRequestHandlingSection();

        if (saveCompleteCallback != null) saveCompleteCallback();
    }

    public override void LoadData(Action<string> loadCompleteCallback)
    {
        string dataToReturn = null;

        try {
            nn.fs.EntryType entryType = 0;
            nn.Result result = nn.fs.FileSystem.GetEntryType(ref entryType, filePath);
            if (nn.fs.FileSystem.ResultPathNotFound.Includes(result)) {
                if (loadCompleteCallback != null) loadCompleteCallback(null);
                return;
            }
            result.abortUnlessSuccess();

            result = nn.fs.File.Open(ref fileHandle, filePath, nn.fs.OpenFileMode.Read);
            result.abortUnlessSuccess();

            long fileSize = 0;
            result = nn.fs.File.GetSize(ref fileSize, fileHandle);
            result.abortUnlessSuccess();

            byte[] data = new byte[fileSize];
            result = nn.fs.File.Read(fileHandle, 0, data, fileSize);
            result.abortUnlessSuccess();

            nn.fs.File.Close(fileHandle);

            dataToReturn = Encoding.Unicode.GetString(data);
        } catch (System.Exception e) {
            Debug.LogError($"[SaveLocationSwitch] Error reading from save file so going to set to empty. Exception: {e?.ToString()}");
        }

        if (loadCompleteCallback != null) loadCompleteCallback(dataToReturn);
    }

    public override int GetSaveFileCount()
    {
        // This is pretty hacky
        nn.fs.EntryType entryType = 0;
        nn.Result result = nn.fs.FileSystem.GetEntryType(ref entryType, filePath);
        if (nn.fs.FileSystem.ResultPathNotFound.Includes(result)) {
            return 0;
        } else {
            return 1;
        }
    }

    public override void DeleteLastUpdatedFile()
    {
        // Nintendo Switch Guideline 0080
        UnityEngine.Switch.Notification.EnterExitRequestHandlingSection();

        try {
            nn.Result result = nn.fs.File.Delete(filePath);
            if (!nn.fs.FileSystem.ResultPathNotFound.Includes(result)) {
                result.abortUnlessSuccess();
            }
        } catch (System.Exception e) {
            Debug.LogError($"[SaveLocationSwitch] Error deleting the file at path '{filePath}'. Exception: {e?.ToString()}");
        }

        // Nintendo Switch Guideline 0080
        UnityEngine.Switch.Notification.LeaveExitRequestHandlingSection();
    }

    public override void DumpAllSaveFiles(Action<List<SaveFileDump>> dumpCompleteCallback)
    {
        Debug.LogError("[SaveLocationSwitch] DumpAllSaveFiles has not been implemented yet");
        if (dumpCompleteCallback != null) dumpCompleteCallback(null);
    }
}

#endif
