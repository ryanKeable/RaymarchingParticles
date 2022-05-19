using UnityEngine;
using System.Runtime.CompilerServices;

public static class MightyRandom
{
    static PRNG generator;
    static readonly bool IsLogging = false;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Init(ulong seed)
    {
        Clear();
        generator = new PRNG(IsLogging, seed);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Init(string seed)
    {
        Clear();
        generator = new PRNG(IsLogging, seed);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Clear()
    {
        if (generator != null) generator = null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Range(float min, float max)
    {
        if (generator == null) return UnityEngine.Random.Range(min, max);
        return generator.Range(min, max);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Range(int min, int max)
    {
        if (generator == null) return UnityEngine.Random.Range(min, max);
        return generator.Range(min, max);
    }

    public static float value {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            if (generator == null) return UnityEngine.Random.value;
            return generator.value;
        }
    }

    public static Vector3 onUnitSphere {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            if (generator == null) return UnityEngine.Random.onUnitSphere;
            return generator.onUnitSphere;
        }
    }
}
