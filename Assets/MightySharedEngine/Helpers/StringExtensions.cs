using UnityEngine;
using System;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.IO;

public static class StringExtensions
{
    [Pure]
    public static bool StartsWithFast(this string source, string value)
    {
        if (source == null || value == null) return false;

        int sourceLength = source.Length;
        int valueLength = value.Length;

        if (sourceLength == 0 && valueLength != 0) return false;
        if (sourceLength < valueLength) return false; // source is not long enough!

        int sourceIndex = 0;
        int valueIndex = 0;

        while (sourceIndex < sourceLength && valueIndex < valueLength && source[sourceIndex] == value[valueIndex])
        {
            sourceIndex++;
            valueIndex++;
        }

        return (valueIndex == valueLength && sourceLength >= valueLength) || (sourceIndex == sourceLength && valueLength >= sourceLength);
    }

    [Pure]
    public static bool EndsWithFast(this string source, string value)
    {
        if (source == null || value == null) return false;

        int sourceLength = source.Length;
        int valueLength = value.Length;

        if (sourceLength == 0 && valueLength != 0) return false;

        int sourceIndex = source.Length - 1;
        int valueIndex = value.Length - 1;

        while (sourceIndex >= 0 && valueIndex >= 0 && source[sourceIndex] == value[valueIndex])
        {
            sourceIndex--;
            valueIndex--;
        }

        return (valueIndex < 0 && sourceLength >= valueLength) || (sourceIndex < 0 && valueLength >= sourceLength);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ReplaceFirst(this string text, string search, string replace)
    {
        int pos = text.IndexOf(search);
        if (pos < 0) return text;
        return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
    }

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool CaseInsensitiveContains(this string text, string value, StringComparison stringComparison = StringComparison.CurrentCultureIgnoreCase)
    {
        return text.IndexOf(value, stringComparison) >= 0;
    }

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Stream ToStream(this string str)
    {
        MemoryStream stream = new MemoryStream();
        StreamWriter writer = new StreamWriter(stream);
        writer.Write(str);
        writer.Flush();
        stream.Position = 0;
        return stream;
    }
}
