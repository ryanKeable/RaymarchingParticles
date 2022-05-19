using UnityEngine;
using System;
using System.Collections.Generic;

public sealed class SaveLocationNull : SaveLocation
{
    public override void Initialise() { }
    public override void Terminate() { }

    public override void Update() { }

    public override void SaveData(string saveData, Action saveCompleteCallback)
    {
        if (saveCompleteCallback != null) saveCompleteCallback();
    }

    public override void LoadData(Action<string> loadCompleteCallback)
    {
        if (loadCompleteCallback != null) loadCompleteCallback(null);
    }

    public override int GetSaveFileCount()
    {
        return 0;
    }

    public override void DeleteLastUpdatedFile() { }

    public override void DumpAllSaveFiles(Action<List<SaveFileDump>> dumpCompleteCallback)
    {
        if (dumpCompleteCallback != null) dumpCompleteCallback(null);
    }
}
