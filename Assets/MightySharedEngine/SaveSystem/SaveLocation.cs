using UnityEngine;
using System;
using System.Collections.Generic;

public abstract class SaveLocation
{
    public abstract void Initialise();
    public abstract void Terminate();

    public abstract void Update();

    public abstract void SaveData(string saveData, Action saveCompleteCallback);
    public abstract void LoadData(Action<string> loadCompleteCallback);

    public abstract int GetSaveFileCount();
    public abstract void DeleteLastUpdatedFile();

    public abstract void DumpAllSaveFiles(Action<List<SaveFileDump>> dumpCompleteCallback);
}
