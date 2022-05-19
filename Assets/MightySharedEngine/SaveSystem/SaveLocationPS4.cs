#if UNITY_PS4

using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Sony.PS4.SavedGame;

public sealed class SaveLocationPS4 : SaveLocation
{
    // All files in the 'Assets/StreamingAssets' folder will get moved to a folder called 'Media' when built
    private const string iconPathWithinUnity = "Media/StreamingAssets/SaveIcon.png";
    private const int kSaveDataMaxSize = 5 * 1024 * 1024;
    private const string saveSlotName = "autoSaveData1";

    private Action saveCompleteCallback;
    private Action<string> loadCompleteCallback;

    public override void Initialise()
    {
        Debug.Log("[SaveLocationPS4] Initialise");

        Main.OnLog += OnLog;
        Main.OnLogWarning += OnLogWarning;
        Main.OnLogError += OnLogError;
        SaveLoad.OnGameSaved += OnSavedGameSaved;
        SaveLoad.OnGameLoaded += OnSavedGameLoaded;
        SaveLoad.OnGameDeleted += OnSavedGameDeleted;
        SaveLoad.OnCanceled += OnSavedGameCanceled;
        SaveLoad.OnSaveError += OnSaveError;
        SaveLoad.OnLoadError += OnLoadError;
        SaveLoad.OnLoadNoData += OnLoadNoData;

        Main.Initialise();
    }

    public override void Terminate()
    {
        Debug.Log("[SaveLocationPS4] Terminate");

        Main.OnLog -= OnLog;
        Main.OnLogWarning -= OnLogWarning;
        Main.OnLogError -= OnLogError;
        SaveLoad.OnGameSaved -= OnSavedGameSaved;
        SaveLoad.OnGameLoaded -= OnSavedGameLoaded;
        SaveLoad.OnGameDeleted -= OnSavedGameDeleted;
        SaveLoad.OnCanceled -= OnSavedGameCanceled;
        SaveLoad.OnSaveError -= OnSaveError;
        SaveLoad.OnLoadError -= OnLoadError;
        SaveLoad.OnLoadNoData -= OnLoadNoData;

        Main.Terminate();
    }

    public override void Update()
    {
        Main.Update();
    }

    public override void SaveData(string saveData, Action saveCompleteCallback)
    {
        this.saveCompleteCallback = saveCompleteCallback;
        WriteSaveFile(saveSlotName, 1, saveData);
    }

    public override void LoadData(Action<string> loadCompleteCallback)
    {
        this.loadCompleteCallback = loadCompleteCallback;
        LoadSaveFile(saveSlotName);
    }

    public override int GetSaveFileCount()
    {
        // This is pretty hacky
        SaveLoad.SavedGameSlotParams slotParams = new SaveLoad.SavedGameSlotParams();
        SetupGameParams(ref slotParams);

        slotParams.dirName = saveSlotName;

        if (SaveLoad.Exists(slotParams) == 0u) {
            return 1;
        } else {
            return 0;
        }
    }

    public override void DeleteLastUpdatedFile()
    {
        SaveLoad.SavedGameSlotParams slotParams = new SaveLoad.SavedGameSlotParams();
        SetupGameParams(ref slotParams);

        slotParams.dirName = saveSlotName;

        SaveLoad.Delete(slotParams, false);
    }

    public override void DumpAllSaveFiles(Action<List<SaveFileDump>> dumpCompleteCallback)
    {
        Debug.LogError("[SaveLocationPS4] DumpAllSaveFiles has not been implemented yet");
        if (dumpCompleteCallback != null) dumpCompleteCallback(null);
    }

    private void WriteSaveFile(string saveSlot, int saveId, string saveData)
    {
        SaveLoad.SavedGameSlotParams slotParams = new SaveLoad.SavedGameSlotParams();
        SetupGameParams(ref slotParams);

        slotParams.dirName = saveSlot;
        slotParams.title = SimplifiedLocControl.replaceVariables(SimplifiedLocControl.localString("ps4_save_title"), saveId.ToString());
        slotParams.newTitle = "";
        slotParams.subTitle = SimplifiedLocControl.replaceVariables(SimplifiedLocControl.localString("ps4_save_subtitle"), saveId.ToString());
        slotParams.detail = SimplifiedLocControl.replaceVariables(SimplifiedLocControl.localString("ps4_save_detail"), saveId.ToString());
        slotParams.iconPath = GetIconPath();
        // Optionally we can configure the save so that if the disk is full we are able to display the NOSPACE_CONTINUABLE message
        slotParams.noSpaceSysMsg = SaveLoad.DialogSysmsgType.NOSPACE_CONTINUABLE;

        byte[] bytes = Encoding.Unicode.GetBytes(saveData);

        SaveLoad.SaveGame(bytes, slotParams, false);
    }

    private void LoadSaveFile(string saveSlot)
    {
        SaveLoad.SavedGameSlotParams slotParams = new SaveLoad.SavedGameSlotParams();
        SetupGameParams(ref slotParams);

        slotParams.dirName = saveSlot;

        if (SaveLoad.Exists(slotParams) == 0u) {
            SaveLoad.LoadGame(slotParams, false);
        } else {
            DoNoLoadDataCallback();
        }
    }

    private void SetupGameParams(ref SaveLoad.SavedGameSlotParams slotParams)
    {
        slotParams.userId = 0;      // By passing a userId of 0 we use the default user that started the title
        slotParams.titleId = null;  // By passing null we use the game's title id from the publishing settings
        slotParams.dirName = "";
        slotParams.fileName = "unitySaveData";
    }

    private string GetIconPath()
    {
        // Get the root directory of Unity. Necessary for located the icon location in bootloader operations.
        string fsr = fsr = UnityEngine.PS4.Utility.GetFileSystemRoot();

        // WORKAROUND: Currently returns empty if in the root project or a non-bootloader project. This will be fixed.
        if (string.IsNullOrEmpty(fsr)) {
            fsr = "/app0/";
        }

        return fsr + iconPathWithinUnity;
    }

    private void OnLog(Messages.PluginMessage msg)
    {
        Debug.Log("[SaveLocationPS4] OnLog: " + msg.Text);
    }

    private void OnLogWarning(Messages.PluginMessage msg)
    {
        Debug.LogWarning("[SaveLocationPS4] OnLogWarning: " + msg.Text);
    }

    private void OnLogError(Messages.PluginMessage msg)
    {
        Debug.LogError("[SaveLocationPS4] OnLogError: " + msg.Text);
    }

    private void OnSavedGameSaved(Messages.PluginMessage msg)
    {
        Debug.Log("[SaveLocationPS4] OnSavedGameSaved");

        if (saveCompleteCallback != null) {
            Action temp = saveCompleteCallback;
            saveCompleteCallback = null;
            temp();
        }
    }

    private void OnSavedGameDeleted(Messages.PluginMessage msg)
    {
        Debug.Log("[SaveLocationPS4] OnSavedGameDeleted");
    }

    private void OnSavedGameLoaded(Messages.PluginMessage msg)
    {
        string result = null;

        Debug.Log("[SaveLocationPS4] OnSavedGameLoaded");
        byte[] bytes = SaveLoad.GetLoadedGame();

        if (bytes != null) {
            result = Encoding.Unicode.GetString(bytes);
        } else {
            Debug.LogError("[SaveLocationPS4] OnSavedGameLoaded ERROR: No data");
        }

        if (loadCompleteCallback != null) {
            Action<string> temp = loadCompleteCallback;
            loadCompleteCallback = null;
            temp(result);
        }
    }

    private void OnSavedGameCanceled(Messages.PluginMessage msg)
    {
        Debug.Log("[SaveLocationPS4] OnSavedGameCanceled");

        // TODO: Should probably do something other than just calling the callback like everything succeeded here
        if (saveCompleteCallback != null) {
            Action temp = saveCompleteCallback;
            saveCompleteCallback = null;
            temp();
        }
    }

    private void OnSaveError(Messages.PluginMessage msg)
    {
        Debug.LogError(String.Format("[SaveLocationPS4] OnSaveError: {0}", msg.type.ToString()));
        if (msg.data != IntPtr.Zero) {
            int sceResultcode = Marshal.ReadInt32(msg.data);
            Debug.LogError("[SaveLocationPS4] OnSaveError: Save result code : 0x" + sceResultcode.ToString("X"));
        }

        // TODO: Should probably do something other than just calling the callback like everything succeeded here
        if (saveCompleteCallback != null) {
            Action temp = saveCompleteCallback;
            saveCompleteCallback = null;
            temp();
        }
    }

    private void OnLoadError(Messages.PluginMessage msg)
    {
        Debug.LogError(String.Format("[SaveLocationPS4] OnLoadError: {0}", msg.type.ToString()));
        if (msg.data != IntPtr.Zero) {
            int sceResultcode = Marshal.ReadInt32(msg.data);
            Debug.LogError("[SaveLocationPS4] OnLoadError: Load result code : 0x" + sceResultcode.ToString("X"));
        }

        DoNoLoadDataCallback();
    }

    private void OnLoadNoData(Messages.PluginMessage msg)
    {
        Debug.Log("[SaveLocationPS4] OnLoadNoData");

        DoNoLoadDataCallback();
    }

    private void DoNoLoadDataCallback()
    {
        if (loadCompleteCallback != null) {
            Action<string> temp = loadCompleteCallback;
            loadCompleteCallback = null;
            temp(null);
        }
    }
}

#endif
