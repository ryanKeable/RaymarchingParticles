using UnityEngine;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public sealed class MappedList<T>
{
    private List<T> dataList = null;
    private Dictionary<string, T> dataMap = null;

    public MappedList(int initialCapacity = 8)
    {
        dataList = new List<T>(initialCapacity);
        dataMap = new Dictionary<string, T>(initialCapacity, System.StringComparer.OrdinalIgnoreCase);
    }

    public void Add(T item, string key)
    {
        string lowerKey = key.ToLower();
        // if it is already in the list, then we need to remove it and replace it
        if (dataMap.ContainsKey(lowerKey)) {
            dataList.Remove(dataMap[lowerKey]);
        }
        dataMap[lowerKey] = item;
        dataList.Add(item);
    }

    public T this[int index] {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            return dataList[index];
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set {
            dataList[index] = value;
        }
    }

    public int Count {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            return dataList.Count;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        dataList.Clear();
        dataMap.Clear();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(T item)
    {
        return dataList.Contains(item);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T lastItem()
    {
        // just a handy method
        return dataList[dataList.Count - 1];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T itemWithKey(string key)
    {
        if (key == null) return default(T);
        T theItem;
        if (dataMap.TryGetValue(key, out theItem))
            return theItem;
        return default(T);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public List<T> mappedValues()
    {
        return new List<T>(dataList);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public List<string> keys()
    {
        return new List<string>(dataMap.Keys);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool containsKey(string key)
    {
        return dataMap.ContainsKey(key);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool remove(string key)
    {
        return dataMap.Remove(key);
    }
}
