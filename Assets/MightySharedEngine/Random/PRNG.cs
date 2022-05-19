using UnityEngine;
using System;
using System.IO;
using System.Runtime.CompilerServices;

public sealed class PRNG
{
    StreamWriter sw = null;

    private ulong[] s = new ulong[16];
    int p;

    // For compatibility with Random
    public ulong seed {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set { InitWithSeed(value); }
    }

    // For compatibility with Random
    public float value {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get { return Float(1.0f); }
    }

    // For compatibility with Random
    public Vector3 insideUnitSphere {
        get {
            // Generate a random point within a cube and then check to see if it is within a sphere.
            // This gives a uniform distribution and is relatively fast.
            float x, y, z;
            do {
                x = Range(-1f, 1f);
                y = Range(-1f, 1f);
                z = Range(-1f, 1f);
            } while (Mathf.Pow(x, 2) + Mathf.Pow(y, 2) + Mathf.Pow(z, 2) > 1f);
            return new Vector3(x, y, z);
        }
    }

    // For compatibility with Random
    public Vector3 onUnitSphere {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            return insideUnitSphere.normalized;
        }
    }

    public PRNG(bool log)
    {
        InitLog(log);
    }

    ~PRNG()
    {
        if (sw != null) {
            sw.Close();
        }
    }

    public PRNG(bool log, ulong seed)
    {
        InitLog(log);
        InitWithSeed(seed);
    }

    // Utility function to let you initialise using a string seed
    public PRNG(bool log, string seed)
    {
        InitLog(log);
        InitWithSeed((ulong)seed.GetHashCode());
    }

    private void InitLog(bool log)
    {
#if (UNITY_PS4 || UNITY_XBOXONE || UNITY_SWITCH) && !UNITY_EDITOR
            return;
#endif

        if (log) {
            sw = new StreamWriter(SystemHelper.documentFilePath("seedlog.txt"));
            sw.Write("PRNG seeds for ");
            sw.WriteLine(DateTime.Now);
            sw.WriteLine("-------------------");
        }
    }

    private void InitWithSeed(ulong seed)
    {
        if (sw != null) {
            sw.WriteLine(seed);
            sw.Flush();
        }

        // We need a non-zero seed in order to produce useful output, so if
        // we've been given a zero seed, let's set it to some oether arbitrary
        // (but repeatable) value.
        if (seed == 0) {
            seed = 4; // chosen by fair dice roll.
                      // guaranteed to be random.
        }

        // We actually need a lot more state data than just this 64 bits -- we're
        // going to be using Xorshift1024*, which requires 1024 bits of state.  So
        // we're going to use our 64 bits of seed data as the state of an Xorshift64*
        // PRNG, and use that smaller PRNG to generate the initial state for our real,
        // full PRNG.
        //
        // This Xorshift64* implementation is adapted from here:
        //
        // http://xorshift.di.unimi.it/xorshift64star.c

        for (int i = 0; i < 16; i++) {
            seed ^= seed >> 12; // a
            seed ^= seed << 25; // b
            seed ^= seed >> 27; // c
            s[i] = seed * 2685821657736338717;
        }
        p = 0;
    }

    private ulong Next()
    {
        // This is the core of our pseudo-random number generation.  This function
        // generates 64 bits of pseudo-random data, which can then be consumed by
        // the other (externally visible) functions on this class.
        //
        // What follows is an implementation of Xorshift1024*;  an xorshift-based
        // PRNG which uses 1024 bits of state, adapted from the implementation here:
        //
        // http://xorshift.di.unimi.it/xorshift1024star.c

        ulong s0 = s[p];
        ulong s1 = s[p = (p + 1) & 15];
        s1 ^= s1 << 31; // a
        s1 ^= s1 >> 11; // b
        s0 ^= s0 >> 30; // c
        return (s[p] = s0 ^ s1) * 1181783497276652981;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float Float(float maxValue)
    {
        float result = Next() / (float)ulong.MaxValue;
        result *= maxValue;
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float Range(float minValue, float maxValue)
    {
        float delta = maxValue - minValue;
        return Float(delta) + minValue;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Range(int minValue, int maxValue)
    {
        float delta = maxValue - minValue;
        return (int)Float(delta) + minValue;
    }

    // For compatibility with Random
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Color ColorHSV()
    {
        return ColorHSV(0f, 1f, 0f, 1f, 0f, 1f, 1f, 1f);
    }

    // For compatibility with Random
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Color ColorHSV(float hueMin, float hueMax)
    {
        return ColorHSV(hueMin, hueMax, 0f, 1f, 0f, 1f, 1f, 1f);
    }

    // For compatibility with Random
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Color ColorHSV(float hueMin, float hueMax, float saturationMin, float saturationMax)
    {
        return ColorHSV(hueMin, hueMax, saturationMin, saturationMax, 0f, 1f, 1f, 1f);
    }

    // For compatibility with Random
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Color ColorHSV(float hueMin, float hueMax, float saturationMin, float saturationMax, float valueMin, float valueMax)
    {
        return ColorHSV(hueMin, hueMax, saturationMin, saturationMax, valueMin, valueMax, 1f, 1f);
    }

    // For compatibility with Random
    public Color ColorHSV(float hueMin, float hueMax, float saturationMin, float saturationMax, float valueMin, float valueMax, float alphaMin, float alphaMax)
    {
        float h = Mathf.Lerp(hueMin, hueMax, value);
        float s = Mathf.Lerp(saturationMin, saturationMax, value);
        float v = Mathf.Lerp(valueMin, valueMax, value);
        Color result = Color.HSVToRGB(h, s, v, true);
        result.a = Mathf.Lerp(alphaMin, alphaMax, value);
        return result;
    }
}
