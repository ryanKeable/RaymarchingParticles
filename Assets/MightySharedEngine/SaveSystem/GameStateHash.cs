using UnityEngine;
using System.Collections.Generic;
using System.Text;

// A handy class to stack clustered data
public class GameStateHash
{
	public static string separator = ",";
	public string stateKey;

	Dictionary<string, string> gameState = new Dictionary<string, string>();

	public GameStateHash(string key)
	{
		stateKey = key;
		load();
	}

	public int stateCount()
	{
		return gameState.Count;
	}

	StringBuilder saveString = new StringBuilder();
	List<string> keys = new List<string>(); // Secondary fast storage of keys so the save is quicker
	public void save()
	{
		keys.Clear();
		saveString.Remove(0, saveString.Length);
		keys.AddRange(gameState.Keys);
		for (int i = 0; i < keys.Count; i++) {
			if (i > 0) {
				saveString.Append(separator);
			}
			saveString.Append(keys[i]);
			saveString.Append(separator);
			saveString.Append(gameState[keys[i]]);
		}
		GameStateController.setPlayerStringForKey(stateKey, saveString.ToString());
	}

	public void load()
	{
		string raw = GameStateController.playerStringForKey(stateKey, null);
		reset();
		if (raw == null) return;
		if (raw.Length == 0) return;
		string[] tokens = raw.Split(separator[0]);
		for (int i = 1; i < tokens.Length; i += 2) {
			gameState[tokens[i - 1]] = tokens[i];
		}
	}

	public void removeFromState()
	{
		GameStateController.removePlayerKey(stateKey);
	}

	public void reset()
	{
		gameState.Clear();
	}

	public float floatForKey(string key, float defaultValue = 0f)
	{
		string raw = stringForKey(key);
		if (raw == null) return defaultValue;
		return MathfExtensions.parseFloat(raw, defaultValue);
	}

	public int intForKey(string key, int defaultValue = 0)
	{
		string raw = stringForKey(key);
		if (raw == null) return defaultValue;
		return MathfExtensions.parseInt(raw, defaultValue);
	}

	public double doubleForKey(string key, double defaultValue = 0d)
	{
		string raw = stringForKey(key);
		if (raw == null) return defaultValue;
		return MathfExtensions.parseDouble(raw, defaultValue);
	}

	public long longForKey(string key, long defaultValue = 0L)
	{
		string raw = stringForKey(key);
		if (raw == null) return defaultValue;
		return MathfExtensions.parseLong(raw, defaultValue);
	}

	public string stringForKey(string key, string defaultValue = null)
	{
		string valueString = null;
		if (gameState.TryGetValue(key, out valueString)) return valueString;
		return defaultValue;
	}

	public bool boolForKey(string key, bool defaultValue = false)
	{
		string raw = stringForKey(key);
		if (raw == null) return defaultValue;
		return MathfExtensions.parseBool(raw, defaultValue);
	}

	public void setValueForKey(string key, object valueObject)
	{
		gameState[key] = valueObject.ToString();
	}

	public void removeKey(string key)
	{
		gameState.Remove(key);
	}
}
