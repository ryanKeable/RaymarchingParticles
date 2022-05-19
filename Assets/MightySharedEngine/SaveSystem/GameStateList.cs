using UnityEngine;
using System;
using System.Collections.Generic;
using System.Globalization;

// A handy class to stack clustered data
public class GameStateList
{
    public static string separator = ",";
    public string stateKey;

    List<string> gameState = new List<string>();
    int readIndex = 0;

    // Never change this or all old saved DateTimes won't be parsable
    private const string dateTimeStringFormat = "yyyy-MM-dd hh:mm:ss tt";
    private static readonly CultureInfo dateTimeCulture = CultureInfo.CreateSpecificCulture("en-US");

    public GameStateList(string key)
    {
        stateKey = key;
        load();
    }

    public GameStateList(string key, string rawSave)
    {
        stateKey = key;
        load(rawSave);
    }

    public int stateCount()
    {
        return gameState.Count;
    }

    public List<string> allEntries()
    {
        List<string> newList = new List<string>();
        newList.AddRange(gameState);
        return newList;
    }

    public void save()
    {
        GameStateController.setPlayerStringForKey(stateKey, String.Join(separator, gameState.ToArray()));
    }

    public void load()
    {
        string raw = GameStateController.playerStringForKey(stateKey, null);
        reset();
        if (raw == null) return;
        if (raw.Length == 0) return;
        gameState.AddRange(raw.Split(separator[0]));
    }

    public void load(string rawFile)
    {
        reset();
        if (rawFile == null) return;
        if (rawFile.Length == 0) return;
        gameState.AddRange(rawFile.Split(separator[0]));
    }

    public void removeFromState()
    {
        GameStateController.removePlayerKey(stateKey);
    }

    public void reset()
    {
        gameState.Clear();
        readIndex = 0;
    }

    public float nextFloat(float defaultValue = 0f)
    {
        int thisIndex = readIndex;
        readIndex++;
        if (thisIndex >= gameState.Count) return defaultValue;
        return MathfExtensions.parseFloat(gameState[thisIndex]);
    }

    public int nextInt(int defaultValue = 0)
    {
        int thisIndex = readIndex;
        readIndex++;
        if (thisIndex >= gameState.Count) return defaultValue;
        return MathfExtensions.parseInt(gameState[thisIndex]);
    }

    public double nextDouble(double defaultValue = 0)
    {
        int thisIndex = readIndex;
        readIndex++;
        if (thisIndex >= gameState.Count) return defaultValue;
        return MathfExtensions.parseDouble(gameState[thisIndex]);
    }

    public string nextString(string defaultValue = null)
    {
        int thisIndex = readIndex;
        readIndex++;
        if (thisIndex >= gameState.Count) return defaultValue;
        return gameState[thisIndex];
    }

    public bool nextBool(bool defaultValue = false)
    {
        int thisIndex = readIndex;
        readIndex++;
        if (thisIndex >= gameState.Count) return defaultValue;
        return MathfExtensions.parseBool(gameState[thisIndex]);
    }

    public DateTime nextDate(DateTime defaultValue)
    {
        int thisIndex = readIndex;
        readIndex++;
        if (thisIndex >= gameState.Count) return defaultValue;

        string raw = gameState[thisIndex];

        if (raw == null)
        {
            return defaultValue;
        }

        DateTime theTime;
        if (!DateTime.TryParse(raw, out theTime))
        {
            MDebug.LogWarning("[GameStateList] Could not parse time format: " + raw);
            return defaultValue;
        }
        return theTime;
    }

    public void pushState(object stateObject)
    {
        if (stateObject == null)
        {
            gameState.Add("");  // TODO: Is this a gooood idea?
            return;
        }
        gameState.Add(stateObject.ToString());
    }

    //NOTE: floats need to be handled explicitly to make sure culture is accounted for
    public void pushFloatState(float state)
    {
        pushState(state.ToString(CultureInfo.InvariantCulture));
    }

    //NOTE: DateTimes need to be handled explicitly to make sure culture is accounted for
    public void pushDateTimeState(DateTime state)
    {
        pushState(state.ToString(dateTimeStringFormat, dateTimeCulture));
    }

}
