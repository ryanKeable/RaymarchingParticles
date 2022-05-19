using UnityEngine;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public sealed class TypedObjectPool<T> where T : MonoBehaviour
{
    private T templateObject; // Used to make more!
    private List<T> activeObjects;
    private Queue<T> poolQueue;
    private int count = 0;

    public TypedObjectPool(T template, int initialSize = 10)
    {
        if (template == null)
        {
            MDebug.LogError("[TypedObjectPool] template object is null");
        }

        templateObject = template;
        activeObjects = new List<T>(initialSize);
        poolQueue = new Queue<T>(initialSize);
        activate(templateObject, false);
    }

    public T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return activeObjects[index];
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            activeObjects[index] = value;
        }
    }

    public int Count
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return activeObjects.Count;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T template()
    {
        return templateObject;
    }

    public T nextItem()
    {
        // Returns the next available item from the pool, or creates ones
        if (poolQueue.Count > 0)
        {
            T item = poolQueue.Dequeue();
            activeObjects.Add(item);
            activate(item, true);
            return item;
        }

        // Pool is empty? Make a new one!
        GameObject newObject = GameObject.Instantiate(templateObject.gameObject, templateObject.transform.parent, false);
        if (newObject == null)
        {
            MDebug.LogError($"[TypedObjectPool] Failed to Instantiate a new copy of template: '{templateObject?.gameObject.name}'");
            return null;
        }
        newObject.name = newObject.name.Replace("(Clone)", (count++).ToString());
        newObject.SetActive(true);
        T component = newObject.GetComponent<T>();
        activeObjects.Add(component);
        return component;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void returnToPool(T item)
    {
        if (item != null)
        {
            activate(item, false);
            poolQueue.Enqueue(item);
            activeObjects.Remove(item);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void reset()
    {
        for (int i = activeObjects.Count - 1; i >= 0; i--)
        {
            returnToPool(activeObjects[i]);
        }
        activate(templateObject, false); // in case it got activated elsewhere
        activeObjects.Clear();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void activate(T item, bool activeState)
    {
        item.gameObject.SetActive(activeState);
    }
}
