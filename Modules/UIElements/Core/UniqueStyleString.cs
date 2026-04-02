// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.UIElements;

/// <summary>
/// A unique integer-based representation for common strings used in various style algorithms for faster comparison
/// and reduced memory footprint.
/// </summary>
/// <remarks>
/// Attempting to represent a null string with this structure has an undefined behavior and may throw exceptions.
///
/// Ids may very between consecutive runs and may be regenerated on domain reload.
/// </remarks>
public readonly struct UniqueStyleString : IEquatable<UniqueStyleString>
{
    private static readonly Dictionary<string, int> k_StringToIndex = new();
    private static readonly List<string> k_IndexToString = new();

    private readonly int m_Id;

    /// <summary>
    /// An integer representing the underlying string uniquely.
    /// </summary>
    /// <remarks>
    /// Strings with different storage addresses but identical content will always be translated to the same id.
    ///
    /// The id associated with a string may very between consecutive runs and may be regenerated on domain reload.
    /// Use the <see cref="value"/> field for serialization instead.
    /// </remarks>
    public int id => m_Id;

    /// <summary>
    /// A string value that's equal to the string that was used to obtain this UniqueStyleString.
    /// </summary>
    public string value => k_IndexToString[m_Id];

    internal UniqueStyleString(int id)
    {
        m_Id = id;
    }

    /// <summary>
    /// Creates a new UniqueStyleString from a string.
    /// </summary>
    /// <param name="s">A string value that this UniqueStyleString will represent.</param>
    /// <remarks>
    /// Throws an exception if the provided string is null.
    /// String values used to create UniqueStyleStrings are stored together internally.
    /// UniqueStyleStrings can improve performance when used to replace strings that are very commonly use.
    /// Calling this constructor with single-use strings is not recommended.
    /// </remarks>
    public UniqueStyleString(string s)
    {
        if (!k_StringToIndex.TryGetValue(s, out m_Id))
        {
            k_StringToIndex.Add(s, m_Id = k_StringToIndex.Count);
            k_IndexToString.Add(s);
        }
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
        if (!k_StringToIndex.TryGetValue(value, out var id))
        {
            result = default;
            return false;
        }

        result = new(id);
        return true;
    }

    /// <summary>
    /// Converts a UniqueStyleString to string.
    /// </summary>
    /// <param name="ss">The UniqueStyleString to convert</param>
    /// <returns>The converted string value</returns>
    /// <remarks>
    /// The returned string value is guaranteed to be equal to the string that was used to obtain this
    /// UniqueStyleString. However, it may or may not have the same reference value.
    /// </remarks>
    public static explicit operator string(UniqueStyleString ss) => ss.value;

    /// <summary>
    /// Converts a string to UniqueStyleString.
    /// </summary>
    /// <param name="s">The string to convert from</param>
    /// <returns>A UniqueStyleString representing the given string</returns>
    /// <remarks>Throws an exception if the provided string is null.</remarks>
    public static explicit operator UniqueStyleString(string s) => new(s);

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
