using UnityEngine;
using System.Collections.Generic;

// A handy class for storing heaps (32) of bools
public class GameStateFlags
{
	static List<int> upgradeMasks = new List<int>();
	public string stateKey = null;

	int flagInt = 0;

	// Could probably do this with a bit shifter instead, but meh
	void loadMasks()
	{
		// ints are 32 bits, so we add a bitmask for every single bit in the mask
		// 0001 - 1
		// 0010 - 2
		// 0100 - 4
		// 1000 - 8
		// etc

		int mask = 1;
		upgradeMasks.Add(mask);
		for (int i = 1; i < 32; i++) {
			mask *= 2;
			upgradeMasks.Add(mask);
		}
	}

	void setup(int raw)
	{
		if (upgradeMasks.Count == 0) loadMasks();
		flagInt = raw;
	}

	public GameStateFlags(int rawValue)
	{
		setup(rawValue);
	}

	// Note: You can use the state flags by themselves without the state load and save
	public GameStateFlags(string key)
	{
		stateKey = key;
		int stateInt = GameStateController.playerIntForKey(stateKey, 0);
		setup(stateInt);
	}

	public void save()
	{
		if (stateKey == null) return;
		GameStateController.setPlayerIntForKey(stateKey, flagInt);
	}

	public void removeFromState()
	{
		if (stateKey == null) return;
		GameStateController.removePlayerKey(stateKey);
	}

	public void clear(int value = 0)
	{
		flagInt = value;
		save();
	}

	public int intValue()
	{
		return flagInt;
	}

	public bool this[int key]
	{
		get
		{
			if (key >= upgradeMasks.Count) return false;
			return flagIsSet(key);
		}
		set
		{
			setFlag(key, value);
		}
	}

	public bool flagIsSet(int indexNumber)
	{
		return (flagInt & upgradeMasks[indexNumber]) > 0;
	}

	public void setFlag(int indexNumber, bool stateValue)
	{
		if (stateValue) {
			flagInt = flagInt | upgradeMasks[indexNumber];
		} else {
			flagInt = flagInt & ~upgradeMasks[indexNumber];
		}
		save();
	}
}
