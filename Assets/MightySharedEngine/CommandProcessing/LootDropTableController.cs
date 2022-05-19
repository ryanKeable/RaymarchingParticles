using UnityEngine;
using System.Collections.Generic;
using System.Text;

[System.Serializable]
public sealed class LootEntry
{
    public string lootKey;
    public int lootPoints;

    [HideInInspector] // kinda hate this
    public float chance = 0f;

    public LootEntry(string key, int points)
    {
        lootKey = key;
        lootPoints = points;
    }

    public bool isZeroChance()
    {
        return (chance < 0.00001f);
    }

    public void calculateChanceWithTotal(int totalPoints)
    {
        chance = (float)lootPoints / (float)totalPoints;
    }
}

public sealed class LootDropTableChunkHolder
{
    List<int> chunkValues = new List<int>();
    Dictionary<int, LootDropTable> chunkMap = new Dictionary<int, LootDropTable>();
    bool dirty = true;

    public void addEntry(string itemKey, int points, int chunkValue)
    {
        if (!chunkMap.ContainsKey(chunkValue))
        {
            chunkMap[chunkValue] = new LootDropTable();
            chunkValues.Add(chunkValue);
            dirty = true;
        }
        chunkMap[chunkValue].addEntry(itemKey, points);
    }

    void sortValues()
    {
        chunkValues.Sort();
        dirty = false;
    }

    // returns the table that has a chunk that is greater or equal to the value
    // so a table at chunk 5 will apply to values of 1,2,3,4,5 (and 5+ if it is the last entry)
    public LootDropTable tableForValue(int theValue)
    {
        if (dirty) sortValues();
        if (chunkValues.Count == 0) return null;
        for (int i = 0; i < chunkValues.Count; i++)
        {
            int thisValue = chunkValues[i];
            if (theValue <= thisValue) return chunkMap[thisValue];
        }
        return chunkMap[chunkValues[chunkValues.Count - 1]]; // the highest one then
    }
}

public sealed class LootDropTable
{
    int totalPoints = 0;
    List<LootEntry> entries = new List<LootEntry>();
    bool tableIsDirty = true;
    List<LootEntry> bagEntries = new List<LootEntry>();
    List<string> lootList = new List<string>();
    string lastPick = null;

    public void clear()
    {
        totalPoints = 0;
        entries.Clear();
        tableIsDirty = true;
        bagEntries.Clear();
        lootList.Clear();
        lastPick = null;
    }

    public void addEntry(LootEntry theEntry)
    {
        entries.Add(theEntry);
        totalPoints += theEntry.lootPoints;
        tableIsDirty = true;
        recalculatePercentages();
    }

    public void addEntry(string itemKey, int points)
    {
        addEntry(new LootEntry(itemKey, points));
    }

    public void addEntries(LootDropTable otherTable, bool resetBagNow = true)
    {
        if (otherTable == null) return;

        for (int i = 0; i < otherTable.entries.Count; i++)
        {
            entries.Add(otherTable.entries[i]);
            totalPoints += otherTable.entries[i].lootPoints;
        }

        tableIsDirty = true;
        recalculatePercentages();
        if (resetBagNow) resetBag();
    }

    public List<string> allEntries(bool includeZeroWeights = true)
    {
        if (tableIsDirty) recalculatePercentages();
        return lootList;
    }

    public int Count
    {
        get { return allEntries().Count; }
    }

    public void resetBag()
    {
        bagEntries.Clear();

        if (entries.Count != 0)
        {
            bagEntries.AddRange(entries);
            tableIsDirty = true;
        }

        lastPick = null;
    }

    public void recalculatePercentages(bool removeZeroWeighted = false)
    {
        lootList.Clear();
        for (int i = 0; i < entries.Count; i++)
        {
            entries[i].calculateChanceWithTotal(totalPoints);
            if (removeZeroWeighted && entries[i].isZeroChance()) continue;
            lootList.Add(entries[i].lootKey);
        }
        tableIsDirty = false;
    }

    public bool tableIsEmpty()
    {
        if (entries.Count == 0)
            return true;
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].chance > 0)
                return false;
        }
        return true;
    }

    public bool bagIsEmpty()
    {
        return (bagEntries.Count == 0);
    }

    public void removeEntriesFromTable(List<string> entriesToRemove)
    {
        if (entries.Count == 0) return;
        // this is a bit slow and shit
        // reverse traverse so I can remove as I go
        for (int i = entries.Count - 1; i >= 0; i--)
        {
            if (entriesToRemove.Contains(entries[i].lootKey))
            {
                entries.RemoveAt(i);
            }
        }
    }

    public void removeEntriesFromBag(List<string> entriesToRemove)
    {
        if (bagEntries.Count == 0) return;
        // this is a bit slow and shit
        // reverse traverse so I can remove as I go
        for (int i = bagEntries.Count - 1; i >= 0; i--)
        {
            if (entriesToRemove.Contains(bagEntries[i].lootKey))
            {
                bagEntries.RemoveAt(i);
            }
        }
    }

    public List<string> remainingBagEntries()
    {
        List<string> bagThings = new List<string>();
        for (int i = 0; i < bagEntries.Count; i++)
        {
            bagThings.Add(bagEntries[i].lootKey);
        }
        return bagThings;
    }

    public string dropLootFromBag()
    {
        if (entries.Count == 0) return "";
        if (bagEntries.Count == 0) bagEntries.AddRange(entries);
        if (bagEntries.Count == 1)
        {
            string theLoot = bagEntries[0].lootKey;
            bagEntries.Clear();
            return theLoot;
        };

        tableIsDirty = true; // we are gonna mess up the table values
                             // need to do three passes (this could be optimized to two passes, but meh
        int bagTotal = 0;
        for (int i = 0; i < bagEntries.Count; i++)
        {
            bagTotal += bagEntries[i].lootPoints;
        }
        for (int i = 0; i < bagEntries.Count; i++)
        {
            bagEntries[i].calculateChanceWithTotal(bagTotal);
        }
        float theRandomPick = MightyRandom.value;
        float pickValue = 0f;
        for (int i = 0; i < bagEntries.Count; i++)
        {
            pickValue += bagEntries[i].chance;
            if (pickValue > theRandomPick)
            {
                string theLoot = bagEntries[i].lootKey;
                bagEntries.RemoveAt(i);
                return theLoot;
            }
        }
        string endLoot = bagEntries[bagEntries.Count - 1].lootKey;
        bagEntries.RemoveAt(bagEntries.Count - 1);

        return endLoot;
    }

    public string dropLoot(bool repickIfSameAsLast = true)
    {
        if (entries.Count == 0) return "";
        // need to recalc our percentages if we are dirty
        if (tableIsDirty) recalculatePercentages();

        string thisPick = pick();
        if (repickIfSameAsLast && lastPick != null && thisPick == lastPick) thisPick = pick(); // try again, still possible to get the same thing twice, but at least we are trying to avoid it

        lastPick = thisPick;

        return thisPick;
    }

    public string pick()
    {
        float theRandomPick = MightyRandom.value;

        // step through the list until we go over our random value, then go back
        float pickValue = 0f;
        for (int i = 0; i < entries.Count; i++)
        {
            pickValue += entries[i].chance;
            if (pickValue >= theRandomPick) return entries[i].lootKey;
        }
        // no? return the last one
        return entries[entries.Count - 1].lootKey;
    }

    public int weightForEntry(string key)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].lootKey.Equals(key, System.StringComparison.OrdinalIgnoreCase)) return entries[i].lootPoints;
        }
        return 0;
    }

    public void updateEntry(string key, int newPoints)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].lootKey.Equals(key, System.StringComparison.OrdinalIgnoreCase))
            {
                totalPoints -= entries[i].lootPoints;
                entries[i].lootPoints = newPoints;
                totalPoints += newPoints;
                tableIsDirty = true;
                recalculatePercentages();
                return;
            }
        }

        // didn't find it?
        addEntry(key, newPoints);
    }

    public override string ToString()
    {
        StringBuilder theLog = new StringBuilder("[");
        for (int i = 0; i < entries.Count; i++)
        {
            if (i > 0) theLog.Append(",");
            theLog.Append("\"");
            theLog.Append(entries[i].lootKey);
            theLog.Append("\":");
            theLog.Append(entries[i].lootPoints);
        }
        theLog.Append("]");
        return theLog.ToString();
    }
}

public sealed class LootDropTableController
{
    private static LootDropTableController sharedInstance = null;
    public static LootDropTableController instance
    {
        get
        {
            if (sharedInstance == null)
            {
                sharedInstance = new LootDropTableController();
            }
            return sharedInstance;
        }
    }

    Dictionary<string, LootDropTable> allTables = new Dictionary<string, LootDropTable>(10, System.StringComparer.OrdinalIgnoreCase);
    Dictionary<string, LootDropTableChunkHolder> chunkedTables = new Dictionary<string, LootDropTableChunkHolder>(10, System.StringComparer.OrdinalIgnoreCase);

    public void registerConfigsMethods()
    {
        allTables.Clear();
        chunkedTables.Clear();
        CommandDispatch.dispatcher.registerFunction("lootEntry", addLootEntry);
        CommandDispatch.dispatcher.registerFunction("chunkedLootEntry", addChunkedLootEntry);
    }

    public void addChunkedLootEntry(Command theCommand)
    {
        if (theCommand.argCount < 5)
        {
            theCommand.setError("syntax error. chunkedLootEntry <tableName> <chunk> <lootKey> <chancePoints>");
            return;
        }
        string tableName = theCommand[1];
        int chunkValue = theCommand.intForArg(2);
        string lootKey = theCommand[3];
        int points = theCommand.intForArg(4);
        addChunkedLootEntry(tableName, chunkValue, lootKey, points);
    }

    public void addChunkedLootEntry(string tableName, int chunkValue, string lootKey, int weight)
    {
        if (tableName == null) MDebug.LogError("the tableName is NULL");
        if (chunkValue == -99999) MDebug.LogError("the chunkValue is in error");
        if (lootKey == null) MDebug.LogError("the lootKey is NULL");
        if (weight == -99999) MDebug.LogError("the points is in error");
        if (weight < 1)
        {
            MDebug.LogPurple("Low Loot Entry: " + tableName + " : " + lootKey + " : " + chunkValue + " : " + weight);
            return;
        }
        if (!chunkedTables.ContainsKey(tableName))
        {
            chunkedTables[tableName] = new LootDropTableChunkHolder();
        }
        chunkedTables[tableName].addEntry(lootKey, weight, chunkValue);
    }

    public void clearLootTable(string tableName)
    {
        allTables.Remove(tableName.ToLower());
    }

    public void addLootEntry(string tableName, string lootKey, int points)
    {
        if (tableName == null) MDebug.LogError("the tableName is NULL");
        if (lootKey == null) MDebug.LogError("the lootKey is NULL");
        if (points == -99999) MDebug.LogError("the points are in error");
        if (points < 1)
        {
            MDebug.LogPurple("Low Loot Entry: " + tableName + " : " + lootKey + " : " + points);
            return;
        }
        string lowerTable = tableName.ToLower();
        if (!allTables.ContainsKey(lowerTable))
        {
            allTables[lowerTable] = new LootDropTable();
        }
        allTables[lowerTable].addEntry(lootKey, points);
    }

    public void addLootEntry(Command theCommand)
    {
        if (theCommand.argCount < 4)
        {
            theCommand.setError("syntax error. lootEntry <tableName> <lootKey> <chancePoints>");
            return;
        }
        string tableName = theCommand[1];
        string lootKey = theCommand[2];
        int points = theCommand.intForArg(3);
        addLootEntry(tableName, lootKey, points);
    }

    public void lootDrop(Command theCommand)
    {
        if (theCommand.argCount < 2)
        {
            theCommand.setError("syntax error. lootDrop <tableName> <variableName>");
            return;
        }
        string tableName = theCommand[1];
        string loot = dropLoot(tableName);
        string variableKey = theCommand[2];
        EnvironmentController.addEnvironmentValueForKey(loot, variableKey);
    }

    public string dropLoot(string tableName)
    {
        if (!allTables.ContainsKey(tableName.ToLower()))
        {
            MDebug.Log("syntax error. no table entries for table: " + tableName);
            return "none";
        }
        return allTables[tableName.ToLower()].dropLoot();
    }

    public string dropLootFromBag(string tableName)
    {
        if (!allTables.ContainsKey(tableName.ToLower()))
        {
            MDebug.Log("syntax error. no table entries for loot bag: " + tableName);
            return "none";
        }
        return allTables[tableName.ToLower()].dropLootFromBag();
    }

    public void removeEntriesFromLootBag(string tableName, List<string> entriesToRemove)
    {
        if (!allTables.ContainsKey(tableName.ToLower()))
        {
            MDebug.Log("syntax error. no table entries for loot bag: " + tableName);
            return;
        }
        allTables[tableName.ToLower()].removeEntriesFromBag(entriesToRemove);
    }

    public bool lootBagIsEmpty(string tableName)
    {
        if (!allTables.ContainsKey(tableName.ToLower()))
        {
            MDebug.Log("syntax error. no table entries for loot bag: " + tableName);
            return true;
        }
        return allTables[tableName.ToLower()].bagIsEmpty();
    }

    public List<string> allLootInTable(string tableName)
    {
        if (!allTables.ContainsKey(tableName))
        {
            MDebug.Log("syntax error. no table entries for table: " + tableName);
            return null;
        }
        return allTables[tableName].allEntries();
    }

    public List<string> remainingLootInBag(string tableName)
    {
        if (!allTables.ContainsKey(tableName))
        {
            MDebug.Log("syntax error. no table entries for table: " + tableName);
            return null;
        }
        return allTables[tableName].remainingBagEntries();
    }

    public LootDropTable chunkedTableWithNameAndValue(string tableName, int chunkValue)
    {
        if (!chunkedTables.ContainsKey(tableName.ToLower()))
        {
            MDebug.Log("syntax error. no table entries for chunked table: " + tableName);
            return null;
        }
        LootDropTable chunkedTable = chunkedTables[tableName.ToLower()].tableForValue(chunkValue);
        if (chunkedTable == null)
        {
            MDebug.Log("syntax error. chunked table: " + tableName + " has no entries.");
            return null;
        }
        return chunkedTable;
    }

    public void resetChunkedBag(string tableName, int chunkValue)
    {
        LootDropTable chunkedTable = chunkedTableWithNameAndValue(tableName, chunkValue);
        if (chunkedTable == null)
        {
            return;
        }
        chunkedTable.resetBag();
    }

    public void resetBag(string tableName)
    {
        if (!allTables.ContainsKey(tableName.ToLower()))
        {
            MDebug.Log("syntax error. no table entries for bag: " + tableName);
            return;
        }
        allTables[tableName.ToLower()].resetBag();
    }

    public string dropChunkedLoot(string tableName, int chunkValue)
    {
        LootDropTable chunkedTable = chunkedTableWithNameAndValue(tableName, chunkValue);
        if (chunkedTable == null)
        {
            MDebug.Log("There is no chunked table for " + tableName);
            return "none";
        }
        return chunkedTable.dropLoot();
    }

    public string dropChunkedLootFromBag(string tableName, int chunkValue)
    {
        LootDropTable chunkedTable = chunkedTableWithNameAndValue(tableName, chunkValue);
        if (chunkedTable == null)
        {
            MDebug.Log("There is no chunked table for " + tableName);
            return "none";
        }
        return chunkedTable.dropLootFromBag();
    }

    public List<string> allLootInChunkedTable(string tableName, int chunkValue)
    {
        LootDropTable chunkedTable = chunkedTableWithNameAndValue(tableName, chunkValue);
        if (chunkedTable == null)
        {
            return null;
        }
        return chunkedTable.allEntries();
    }

    public LootDropTable tableWithName(string tableName)
    {
        LootDropTable theTable;
        if (allTables.TryGetValue(tableName, out theTable)) return theTable;
        return null;
    }
}
