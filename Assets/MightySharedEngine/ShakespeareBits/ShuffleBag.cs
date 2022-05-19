using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public sealed class ShuffleBag<T> : ICollection<T>, IList<T>
{
    private List<T> data = null;
    private List<T> dataUnshuffled = null; // Need to save this so that we can fully reset the bag back to its initial state when needed
    private int cursor = 0;

    public ShuffleBag()
    {
        data = new List<T>();
        dataUnshuffled = new List<T>();
    }

    public ShuffleBag(int initialListSize)
    {
        data = new List<T>(initialListSize);
        dataUnshuffled = new List<T>(initialListSize);
    }

    public ShuffleBag(List<T> initialListData)
    {
        data = new List<T>(initialListData);
        dataUnshuffled = new List<T>(initialListData);
        resetBagCursor();
    }

    public T Next()
    {
        if (cursor < 1) {
            resetBagCursor();
            if (data.Count < 1) {
                return default(T);
            }
            return data[0];
        }
        // int grab = Mathf.FloorToInt(MightyRandom.value * ((float)cursor + 0.99999f));
        int grab = MightyRandom.Range(0, cursor);
        T temp = data[grab];
        data[grab] = this.data[this.cursor];
        data[cursor] = temp;
        cursor--;
        return temp;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void resetBag()
    {
        data.Clear();
        data.AddRange(dataUnshuffled);
        cursor = data.Count - 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void resetBagCursor()
    {
        cursor = data.Count - 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int IndexOf(T item)
    {
        return data.IndexOf(item);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Insert(int index, T item)
    {
        data.Insert(index, item);
        // TODO: It doesn't make a lot of sense to insert into dataUnshuffled but also doesn't make sense to just add it to the end. What does makes sense?
        dataUnshuffled.Add(item);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RemoveAt(int index)
    {
        T removedItem = data.Pop(index);
        dataUnshuffled.Remove(removedItem);
    }

    public T this[int index] {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            return data[index];
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set {
            data[index] = value;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        return data.GetEnumerator();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(T item)
    {
        data.Add(item);
        dataUnshuffled.Add(item);
        resetBagCursor();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddRange(List<T> items)
    {
        data.AddRange(items);
        dataUnshuffled.AddRange(items);
        resetBagCursor();
    }

    public int Count {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            return data.Count;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        data.Clear();
        dataUnshuffled.Clear();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(T item)
    {
        return data.Contains(item);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyTo(T[] array, int arrayIndex)
    {
        foreach (T item in data) {
            array.SetValue(item, arrayIndex);
            arrayIndex = arrayIndex + 1;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Remove(T item)
    {
        bool removeResult1 = data.Remove(item);
        bool removeResult2 = dataUnshuffled.Remove(item);
        return (removeResult1 && removeResult2);
    }

    public bool IsReadOnly {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            return false;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    IEnumerator IEnumerable.GetEnumerator()
    {
        return data.GetEnumerator();
    }

    public override string ToString()
    {
        System.Text.StringBuilder theLog = new System.Text.StringBuilder("[");
        for (int i = 0; i < data.Count; i++) {
            if (i > 0) theLog.Append(",");
            theLog.Append("\"" + data[i].ToString() + "\"");
        }
        theLog.Append("]");
        return theLog.ToString();
    }
}
