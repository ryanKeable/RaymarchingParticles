using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using System.Collections.Generic;

// Game state is where you go for all your persistent storage needs.
// Also things that are more fleeting
public class GameStateController : BootableMonoBehaviour
{
    PersistentStorage stateHolder = null;
    PersistentStorage cloudState;

    public static bool gameIsPaused = false;
    public static float gameSpeed = 1f;

    public float saveInterval = 0.5f;

    private static GameStateController sharedInstance;
    public static GameStateController instance
    {
        get
        {
            if (sharedInstance == null)
            {
                sharedInstance = FindObjectOfType<GameStateController>();
                if (sharedInstance == null)
                {
                    //MDebug.LogError("[GameStateController] Could not locate a GameStateController object. You have to have exactly one GameStateController in the scene.");
                    return null;
                }
            }
            return sharedInstance;
        }
    }

    GameStateUnlocker unlocker = null;
    int stateVersion = 2;
    bool didBoot = false;
    public override void bootstrap(Action completion)
    {
        if (stateHolder != null && didBoot) // we move scenes sometimes, so just check that we are not extra booting this thing
        {
            completion();
            return;
        }
        stateHolder = PersistentStorage.storageFromSaveFile();
        stateHolder.loadFromDisk((bool success) =>
        {
            if (!success) PlayerPrefs.DeleteAll();

            // this is basically to force a re-load from scratch during development
            // once in production you should really never ever change this
            int storedStateVersion = stateHolder.intForKey("pers_vers");
            if (stateVersion != storedStateVersion)
            {
                MDebug.LogRed("DELETING OLD SAVE VERSION!");
                deleteAll();
                stateHolder.setInt("pers_vers", stateVersion);
            }

            StartCoroutine(checkForStateChange());

            unlocker = new GameStateUnlocker();
            didBoot = true;
            AnimationHelper.playOnNextFrame(this, completion);
        });
    }



    IEnumerator checkForStateChange()
    {
        WaitForSeconds delay = new WaitForSeconds(saveInterval);
        while (true)
        {
            yield return delay;
            if (stateHolder.stateHasChanged)
            {
                NotificationServer.instance.postNotification("GameStateWillSave");
                saveState();
                NotificationServer.instance.postNotification("GameStateDidSave");
            }
        }
    }

    public string base64State()
    {
        return System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(stateHolder.storeAsString()));
    }


    public static void save()
    {
        instance.saveState();
    }

    public void saveState()
    {
        stateHolder.save();
    }


    public void dumpState(string file)
    {
        stateHolder.saveToFile(file);
    }

    public List<string> allStateKeys()
    {
        if (stateHolder == null) return new List<string>();
        return stateHolder.allKeys();
    }

    public void deleteAll()
    {
        stateHolder.deleteAll();
    }

    /// <summary>
    /// Returns a datetime stored according to a string
    /// </summary>
    /// <param name="theKey">The string to match against in the game data</param>
    /// <returns>Stored datetime if found, else returns cloud utc time</returns>
    public static DateTime playerDateTimeForKey(string theKey, DateTime defaultTime)
    {
        string raw = GameStateController.instance.stateHolder.stringForKey(theKey);

        if (raw == null)
        {
            MDebug.LogWarning("[GameStateController] Could not find state for key, return default: " + defaultTime + " " + theKey);
            return defaultTime;
        }

        DateTime theTime;
        if (!DateTime.TryParse(raw, out theTime))
        {
            MDebug.LogWarning("[GameStateController] Could not parse Time format: " + raw);
            return defaultTime;
        }
        return theTime;
    }

    public static void setPlayerDateTimeForKey(string theKey, DateTime theDate)
    {
        GameStateController.instance.stateHolder.setString(theKey, theDate.ToString());
    }

    public static double playerDoubleForKey(string theKey, double defaultDouble = 0d)
    {
        string raw = GameStateController.instance.stateHolder.stringForKey(theKey);
        if (raw == null) return defaultDouble;

        return MathfExtensions.parseDouble(raw);
    }

    public static void setPlayerDoubleForKey(string theKey, double theDouble)
    {
        GameStateController.instance.stateHolder.setString(theKey, theDouble.ToString());
    }

    // This should ultimately probably send this to the cloud
    public static bool playerKeyExists(string theKey)
    {
        string theValue = playerStringForKey(theKey, null);
        if (theValue == null) return false;
        return true;
    }

    public static void removePlayerKey(string theKey)
    {
        GameStateController.instance.stateHolder.removeKey(theKey);
    }

    public static string playerStringForKey(string theKey, string defaultString = null)
    {
        return GameStateController.instance.stateHolder.stringForKey(theKey, defaultString);
    }

    public static int playerIntForKey(string theKey, int defaultValue = 0)
    {
        return GameStateController.instance.stateHolder.intForKey(theKey, defaultValue);
    }

    public static float playerFloatForKey(string theKey, float defaultValue = 0f)
    {
        return GameStateController.instance.stateHolder.floatForKey(theKey, defaultValue);
    }

    public static bool playerBoolForKey(string theKey, bool defaultValue = false)
    {
        string valString = GameStateController.instance.stateHolder.stringForKey(theKey);
        if (valString == null) return defaultValue;
        return MathfExtensions.parseBool(valString);
    }

    public static void setPlayerIntForKey(string theKey, int theValue)
    {
        if (!Application.isPlaying) return;

        GameStateController.instance.stateHolder.setInt(theKey, theValue);
    }

    // Gets an int, increments it, sets it back, returns the new value
    public static int incrementPlayerIntForKey(string theKey, int incrementAmount = 1)
    {
        int theNumber = playerIntForKey(theKey);
        theNumber += incrementAmount;
        setPlayerIntForKey(theKey, theNumber);
        return theNumber;
    }

    public static void setPlayerFloatForKey(string theKey, float theValue)
    {
        if (!Application.isPlaying) return;

        GameStateController.instance.stateHolder.setFloat(theKey, theValue);
    }

    public static void setPlayerStringForKey(string theKey, string theValue)
    {
        if (!Application.isPlaying) return;

        GameStateController.instance.stateHolder.setString(theKey, theValue);
    }

    public static void setPlayerBoolForKey(string theKey, bool theValue)
    {
        if (!Application.isPlaying) return;

        if (theValue)
        {
            GameStateController.instance.stateHolder.setString(theKey, "yes");
        }
        else
        {
            GameStateController.instance.stateHolder.setString(theKey, "no");
        }
    }

    public static bool unlocksAreAvailable()
    {
        if (GameStateController.instance.unlocker == null) return false;
        return true;
    }

    // Just because I am lazy and usually I am checking for locked as opposed to unlocked
    public static bool itemIsLocked(GameStateLocks lockFlag)
    {
        return !itemIsUnlocked(lockFlag);
    }

    public static bool itemIsUnlocked(GameStateLocks lockFlag)
    {
        if (GameStateController.instance == null) return false;
        if (GameStateController.instance.unlocker == null) return false;
        return GameStateController.instance.unlocker.itemIsUnlocked(lockFlag);
    }

    public static void unlockItem(GameStateLocks lockFlag)
    {
        GameStateController.instance.unlocker.unlockItem(lockFlag);
    }
}
