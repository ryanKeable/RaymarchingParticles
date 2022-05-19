using UnityEngine;

// // // // //
// NOTE NOTE NOTE
// // // // //
// Once this gets to 32 then we need to add a second game state flags object
// ALSO!!
// Don't fuck up the order or all the save games will be boned!
public enum GameStateLocks
{
    none = 0, // used when I need to pass in a lock but I dont wanna
}

public class GameStateUnlocker
{
    GameStateFlags stateFlags;

    public GameStateUnlocker()
    {
        loadState();
    }

    void loadState()
    {
        stateFlags = new GameStateFlags("GSUNLK");
    }

    void saveState()
    {
        stateFlags.save();
    }

    public void lockAll()
    {
        stateFlags.clear();
    }

    public bool itemIsUnlocked(GameStateLocks flag)
    {
        return stateFlags[(int)flag];
    }

    public void unlockItem(GameStateLocks flag, bool isUnlocked = true)
    {
        stateFlags[(int)flag] = isUnlocked;
        saveState();
    }
}
