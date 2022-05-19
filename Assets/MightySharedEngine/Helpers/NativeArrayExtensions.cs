using UnityEngine;
using Unity.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;

public static class NativeArrayExtensions
{
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T First<T>(this NativeArray<T> source) where T : struct
    {
        if (source.Length == 0)
            return default(T);

        return source[0];
    }

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Last<T>(this NativeArray<T> source) where T : struct
    {
        if (source.Length == 0)
            return default(T);

        return source[source.Length - 1];
    }

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static NativeArray<T> Clone<T>(this NativeArray<T> source) where T : struct
    {
        return new NativeArray<T>(source, Allocator.Persistent);
    }

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T PickRandom<T>(this NativeArray<T> source) where T : struct
    {
        if (source.Length == 0)
            return default(T);

        return source[MightyRandom.Range(0, source.Length)];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Shuffle<T>(this NativeArray<T> source) where T : struct
    {
        int index = source.Length;
        while (index > 1) {
            index--;
            int newPos = MightyRandom.Range(0, index + 1);
            T temp = source[newPos];
            source[newPos] = source[index];
            source[index] = temp;
        }
    }

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static List<T> ToList<T>(this NativeArray<T> source) where T : struct
    {
        return new List<T>(source);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SwitchPlaces<T>(this NativeArray<T> source, int indexOne, int indexTwo) where T : struct
    {
        if (indexOne < 0 || indexTwo < 0 || indexOne == indexTwo || indexOne >= source.Length || indexTwo >= source.Length)
            return;

        T temp = source[indexOne];
        source[indexOne] = source[indexTwo];
        source[indexTwo] = temp;
    }
}
