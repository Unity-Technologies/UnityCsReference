// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//
// Copyright SmartFormat Project maintainers and contributors.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Unity.SmartStrings.Core.Parsing;

/// <summary>
/// A set of characters with constant-time lookup: a bitmap for ASCII and a hash set for the rest.
/// </summary>
class CharSet : IEnumerable<char>
{
    const int k_AsciiLimit = 128;
    const int k_BitsPerUint = 32;
    const int k_BitmapLength = k_AsciiLimit / k_BitsPerUint;

    readonly uint[] m_AsciiBitmap = new uint[k_BitmapLength];
    readonly HashSet<char> m_NonAsciiChars = new HashSet<char>();

    /// <summary>
    /// Whether the set lists the allowed characters (<see langword="true"/>) or the disallowed ones.
    /// </summary>
    public bool IsAllowList { get; set; }

    public CharSet()
    {
    }

    public CharSet(ReadOnlySpan<char> characters)
    {
        AddRange(characters);
    }

    public CharSet(IEnumerable<char> characters)
    {
        AddRange(characters);
    }

    public void AddRange(ReadOnlySpan<char> characters)
    {
        foreach (var c in characters)
            Add(c);
    }

    public void AddRange(IEnumerable<char> characters)
    {
        foreach (var c in characters)
            Add(c);
    }

    public void Add(char c)
    {
        if (c < k_AsciiLimit)
            m_AsciiBitmap[c / k_BitsPerUint] |= 1u << c % k_BitsPerUint;
        else
            m_NonAsciiChars.Add(c);
    }

    public bool Remove(char c)
    {
        if (c < k_AsciiLimit)
        {
            ref var bitmap = ref m_AsciiBitmap[c / k_BitsPerUint];
            var mask = 1u << c % k_BitsPerUint;
            if ((bitmap & mask) == 0) return false;
            bitmap &= ~mask;
            return true;
        }

        return m_NonAsciiChars.Remove(c);
    }

    public bool Contains(char c)
    {
        if (c < k_AsciiLimit)
            return (m_AsciiBitmap[c / k_BitsPerUint] & 1u << c % k_BitsPerUint) != 0;

        return m_NonAsciiChars.Contains(c);
    }

    public int Count
    {
        get
        {
            var count = 0;
            foreach (var segment in m_AsciiBitmap)
                count += BitCount(segment);
            return count + m_NonAsciiChars.Count;
        }
    }

    public IEnumerator<char> GetEnumerator()
    {
        for (var i = 0; i < k_AsciiLimit; i++)
        {
            if ((m_AsciiBitmap[i / k_BitsPerUint] & 1u << i % k_BitsPerUint) != 0)
                yield return (char)i;
        }

        foreach (var c in m_NonAsciiChars)
            yield return c;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    static int BitCount(uint value)
    {
        value -= value >> 1 & 0x55555555;
        value = (value & 0x33333333) + (value >> 2 & 0x33333333);
        return (int)((value + (value >> 4) & 0x0F0F0F0F) * 0x01010101) >> 24;
    }
}
