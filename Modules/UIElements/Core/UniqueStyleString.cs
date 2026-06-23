// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine.Bindings;

namespace UnityEngine.UIElements;

/// <summary>
/// A unique integer-based representation for common strings used in various style algorithms for faster comparison
/// and reduced memory footprint.
/// </summary>
/// <remarks>
/// Ids may very between consecutive runs and may be regenerated on domain reload.
/// </remarks>
public readonly struct UniqueStyleString : IEquatable<UniqueStyleString>
{
    /// <summary>
    /// The UniqueStyleString representation of a @@null@@ string.
    /// </summary>
    /// <remarks>This value is also equal to default(UniqueStyleString).</remarks>
    public static readonly UniqueStyleString Null = default;

    /// <summary>
    /// The UniqueStyleString representation of an empty string.
    /// </summary>
    public static readonly UniqueStyleString Empty = new(1);

    private static Dictionary<string, int> k_StringToIndex = new() { { "", 1 } };
    private static List<string> k_IndexToString = new() { null, "" };

    // For tests, operations inside this scope target a fresh internal storage so that pollution
    // from built-in stylesheets or other tests does not skew the size — and therefore the cache
    // footprint — of the lookup table being measured.
    internal readonly struct TestScope : IDisposable
    {
        private readonly Dictionary<string, int> m_PrevStringToIndex;
        private readonly List<string> m_PrevIndexToString;

        public TestScope()
        {
            m_PrevStringToIndex = k_StringToIndex;
            m_PrevIndexToString = k_IndexToString;
            k_StringToIndex = new() { { "", 1 } };
            k_IndexToString = new() { null, "" };
        }

        public void Dispose()
        {
            k_StringToIndex = m_PrevStringToIndex;
            k_IndexToString = m_PrevIndexToString;
        }
    }

    private readonly int m_Id;

    /// <summary>
    /// An integer representing the underlying string uniquely.
    /// </summary>
    /// <remarks>
    /// Strings with different storage addresses but identical content will always be translated to the same id.
    ///
    /// The id associated with a string may vary between consecutive runs and may be regenerated on domain reload.
    /// Use the <see cref="value"/> field for serialization instead.
    /// </remarks>
    public int id => m_Id;

    /// <summary>
    /// A string value that's equal to the string that was used to obtain this UniqueStyleString.
    /// </summary>
    internal string value
    {
        [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEngine.HierarchyModule")]
        get => k_IndexToString[m_Id];
    }

    /// <summary>
    /// Returns whether this UniqueStyleString represents a @@null@@ string.
    /// </summary>
    public bool IsNull => m_Id == 0;

    /// <summary>
    /// Returns whether this UniqueStyleString represents an empty string.
    /// </summary>
    public bool IsEmpty => m_Id == 1;

    /// <summary>
    /// Returns @@true@@ if this UniqueStyleString represents either a @@null@@ or empty string.
    /// </summary>
    public bool IsNullOrEmpty() => IsNullOrEmpty(m_Id);

    // Overload that operates directly on a raw id. Used in hot paths that already have the id
    // and want to avoid constructing a UniqueStyleString just to call the instance method.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsNullOrEmpty(int id) => (uint)id <= 1u;

    internal UniqueStyleString(int id)
    {
        m_Id = id;
    }

    /// <summary>
    /// Creates a new UniqueStyleString from a string.
    /// </summary>
    /// <param name="s">A string value that this UniqueStyleString will represent.</param>
    /// <remarks>
    /// If provided string is null, the returned result is the same as <see cref="Null"/>.
    /// If provided string is empty, the returned result is the same as <see cref="Empty"/>.
    /// String values used to create UniqueStyleStrings are stored together internally.
    /// UniqueStyleStrings can improve performance when used to replace strings that are very commonly use.
    /// Calling this constructor with single-use strings is not recommended.
    /// </remarks>
    public UniqueStyleString(string s)
    {
        if (s == null)
        {
            m_Id = 0;
            return;
        }

        if (!k_StringToIndex.TryGetValue(s, out m_Id))
        {
            k_StringToIndex.Add(s, m_Id = k_IndexToString.Count);
            k_IndexToString.Add(s);
        }
    }

    /// <summary>
    /// Compares this UniqueStyleString to a given string for equality.
    /// </summary>
    /// <param name="s">The string with which to compare for equality.</param>
    /// <returns>True if this UniqueStyleString represents an underlying string value that is equal to the provided string.</returns>
    /// <remarks>
    /// This method uses a lightweight operation that avoids creating intermediary data and can be faster than
    /// converting the provided string to a UniqueStyleString.
    /// </remarks>
    public bool IsSame(string s)
    {
        return s == null ? this.IsNull : k_StringToIndex.TryGetValue(s, out var id) && m_Id == id;
    }

    /// <summary>
    /// Attempts to retrieve an existing UniqueStyleString from a string value.
    /// </summary>
    /// <param name="value">The string value that the resulting UniqueStyleString needs to represent</param>
    /// <param name="result">A UniqueStyleString that matches the provided value, if any</param>
    /// <returns>True if a UniqueStyleString representing the provided value currently exists</returns>
    /// <remarks>
    /// Methods such as <see cref="VisualElement.AddToClassList(UniqueStyleString)"/> guarantee that the
    /// UniqueStyleString exists after the call. If @@TryGet@@ returns false for a given value, then there is no
    /// need to call <see cref="VisualElement.ClassListContains(UniqueStyleString)"/> for that same value, as it
    /// will necessarily return false. Similarly, there is no need to call
    /// <see cref="VisualElement.RemoveFromClassList(UniqueStyleString)"/> for that value.
    /// </remarks>
    public static bool TryGet(string value, out UniqueStyleString result)
    {
        if (value == null)
        {
            result = Null;
            return true;
        }

        if (k_StringToIndex.TryGetValue(value, out var id))
        {
            result = new(id);
            return true;
        }

        result = default;
        return false;
    }

    /// <summary>
    /// Returns a string value with the same content as the string that was used to obtain this UniqueStyleString.
    /// </summary>
    /// <remarks>
    /// The returned string may require access to large memory lookups or be reconstructed from other internal data.
    /// For performance-critical scenarios where both the UniqueStyleString and its underlying string value
    /// are required, keeping a local reference to both representations may give better results.
    /// </remarks>
    public override string ToString()
    {
        return k_IndexToString[m_Id];
    }

    /// <summary>
    /// Computes a valid hash code for this UniqueStyleString.
    /// </summary>
    /// <returns>A number that represents this UniqueStyleString</returns>
    /// <remarks>
    /// This method returns a unique hash code for the given UniqueStyleString.
    /// The result hash has no chance of producing a collision, that is,
    /// distinct UniqueStyleStrings will always produce distinct hash codes.
    /// </remarks>
    public override int GetHashCode()
    {
        return m_Id.GetHashCode();
    }

    /// <summary>
    /// Computes whether or not this UniqueStyleString is identical to the other given UniqueStyleString.
    /// </summary>
    /// <param name="other">The other UniqueStyleString compared for equality</param>
    /// <returns>True if both UniqueStyleStrings represent underlying string values that are equal</returns>
    public bool Equals(UniqueStyleString other)
    {
        return m_Id == other.m_Id;
    }

    /// <summary>
    /// Computes whether or not this UniqueStyleString is identical another given object.
    /// </summary>
    /// <param name="obj">The object compared for equality</param>
    /// <returns>
    /// True if the compared object is a UniqueStyleString and
    /// both UniqueStyleStrings represent underlying string values that are equal.
    /// </returns>
    /// <seealso cref="Equals(UniqueStyleString)"/>
    public override bool Equals(object obj)
    {
        return obj is UniqueStyleString other && Equals(other);
    }

    /// <summary>
    /// Computes whether or not this UniqueStyleString is identical to the other given UniqueStyleString.
    /// </summary>
    /// <param name="a">The first UniqueStyleString compared for equality</param>
    /// <param name="b">The second UniqueStyleString compared for equality</param>
    /// <returns>True if both UniqueStyleStrings represent underlying string values that are equal</returns>
    public static bool operator==(UniqueStyleString a, UniqueStyleString b)
    {
        return a.m_Id == b.m_Id;
    }

    /// <summary>
    /// Computes whether or not this UniqueStyleString is different from the other given UniqueStyleString.
    /// </summary>
    /// <param name="a">The first UniqueStyleString compared for equality</param>
    /// <param name="b">The second UniqueStyleString compared for equality</param>
    /// <returns>True if both UniqueStyleStrings represent underlying string values that are different</returns>
    public static bool operator!=(UniqueStyleString a, UniqueStyleString b)
    {
        return a.m_Id != b.m_Id;
    }
}
