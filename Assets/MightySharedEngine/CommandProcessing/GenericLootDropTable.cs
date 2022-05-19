using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public sealed class GenericLootEntry
{
    public object lootObject;
    public int lootPoints;

    [HideInInspector] // kinda hate this
    public float chance = 0f;

    public GenericLootEntry(object key, int points)
    {
        lootObject = key;
        lootPoints = points;
    }

    public void calculateChanceWithTotal(int totalPoints)
    {
        chance = (float)lootPoints / (float)totalPoints;
    }
}

public sealed class GenericLootDropTable
{
    int totalPoints = 0;
    List<GenericLootEntry> entries = new List<GenericLootEntry>();

    bool tableIsDirty = true;

    List<GenericLootEntry> bagEntries = new List<GenericLootEntry>();

    public void clear()
    {
        entries.Clear();
        bagEntries.Clear();
        totalPoints = 0;
        tableIsDirty = true;
    }

    public void addEntry(GenericLootEntry theEntry)
    {
        entries.Add(theEntry);
        totalPoints += theEntry.lootPoints;
        tableIsDirty = true;
        recalculatePercentages();
    }

    public void addEntry(object item, int points)
    {
        addEntry(new GenericLootEntry(item, points));
    }

    List<object> lootList = new List<object>();
    public List<object> allEntries()
    {
        if (tableIsDirty) recalculatePercentages();
        return lootList;
    }

    public void resetBag()
    {
        bagEntries.Clear();
        if (entries.Count != 0) bagEntries.AddRange(entries);
    }

    void recalculatePercentages()
    {
        lootList.Clear();
        for (int i = 0; i < entries.Count; i++) {
            entries[i].calculateChanceWithTotal(totalPoints);
            lootList.Add(entries[i].lootObject);
        }
        tableIsDirty = false;
    }

    public bool tableIsEmpty()
    {
        return (entries.Count == 0);
    }

    public bool bagIsEmpty()
    {
        return (bagEntries.Count == 0);
    }

    public void removeEntriesFromBag(List<object> entriesToRemove)
    {
        if (bagEntries.Count == 0) return;
        // this is a bit slow and shit
        // reverse traverse so I can remove as I go
        for (int i = bagEntries.Count - 1; i >= 0; i--) {
            if (entriesToRemove.Contains(bagEntries[i].lootObject)) {
                bagEntries.RemoveAt(i);
            }
        }
    }

    public List<object> remainingBagEntries()
    {
        List<object> bagThings = new List<object>();
        for (int i = 0; i < bagEntries.Count; i++) {
            bagThings.Add(bagEntries[i].lootObject);
        }
        return bagThings;
    }

    public object dropLootFromBag()
    {
        if (entries.Count == 0) return "";
        if (bagEntries.Count == 0) bagEntries.AddRange(entries);
        if (bagEntries.Count == 1) {
            object theLoot = bagEntries[0].lootObject;
            bagEntries.Clear();
            return theLoot;
        };

        tableIsDirty = true; // we are gonna mess up the table values
        // need to do three passes (this could be optimized to two passes, but meh
        int bagTotal = 0;
        for (int i = 0; i < bagEntries.Count; i++) {
            bagTotal += bagEntries[i].lootPoints;
        }
        for (int i = 0; i < bagEntries.Count; i++) {
            bagEntries[i].calculateChanceWithTotal(bagTotal);
        }
        float theRandomPick = MightyRandom.value;
        float pickValue = 0f;
        for (int i = 0; i < bagEntries.Count; i++) {
            pickValue += bagEntries[i].chance;
            if (pickValue > theRandomPick) {
                object theLoot = bagEntries[i].lootObject;
                bagEntries.RemoveAt(i);
                return theLoot;
            }
        }
        object endLoot = bagEntries[bagEntries.Count - 1].lootObject;
        bagEntries.RemoveAt(bagEntries.Count - 1);

        return endLoot;
    }

    object lastPick = null;
    public object dropLoot(bool repickIfSameAsLast = true)
    {
        if (entries.Count == 0) return "";
        // need to recalc our percentages if we are dirty
        if (tableIsDirty) recalculatePercentages();

        object thisPick = pick();
        if (repickIfSameAsLast && thisPick == lastPick) thisPick = pick(); // try again, still possible to get the same thing twice, but at least we are trying to avoid it

        lastPick = thisPick;

        return thisPick;
    }

    public object pick()
    {
        float theRandomPick = MightyRandom.value;

        // step through the list until we go over our random value, then go back
        float pickValue = 0f;
        for (int i = 0; i < entries.Count; i++) {
            pickValue += entries[i].chance;
            if (pickValue >= theRandomPick) return entries[i].lootObject;
        }
        // no? return the last one
        return entries[entries.Count - 1].lootObject;
    }
}
