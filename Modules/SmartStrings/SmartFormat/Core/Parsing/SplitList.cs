// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//
// Copyright SmartFormat Project maintainers and contributors.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using Unity.SmartStrings.Pooling.SmartPools;

namespace Unity.SmartStrings.Core.Parsing;

/// <summary>
/// Contains the results of a <see cref="Format"/> Split operation.
/// This allows deferred splitting of items.
/// </summary>
internal class SplitList : IList<Format>
{
    Format m_Format;
    List<int> m_Splits;
    readonly List<Format> m_FormatCache = new();

    /// <summary>
    /// Initializes the instance of <see cref="SplitList"/>.
    /// </summary>
    /// <param name="format"></param>
    /// <param name="splits"></param>
    /// <returns>This <see cref="Format"/> instance.</returns>
    public SplitList Initialize(Format format, List<int> splits)
    {
        m_Format = format;
        m_Splits = splits;

        // Resize the cache to match
        for (var i = 0; i < Count; ++i)
            m_FormatCache.Add(null);

        return this;
    }

    ///<inheritdoc/>
    public Format this[int index]
    {
        get
        {
            if (index > m_Splits.Count) throw new ArgumentOutOfRangeException(nameof(index));

            if (m_Splits.Count == 0) return m_Format;

            // Return the cached version?
            if (m_FormatCache[index] != null)
                return m_FormatCache[index];

            if (index == 0)
            {
                var f = m_Format.Substring(0, m_Splits[0]);
                m_FormatCache[index] = f;
                return f;
            }

            if (index == m_Splits.Count)
            {
                var f = m_Format.Substring(m_Splits[index - 1] + 1);
                m_FormatCache[index] = f;
                return f;
            }

            // Return the format between the splits
            var startIndex = m_Splits[index - 1] + 1;
            var format = m_Format.Substring(startIndex, m_Splits[index] - startIndex);
            m_FormatCache[index] = format;
            return format;
        }
        set => throw new NotSupportedException();
    }

    /// <summary>
    /// Clears the <see cref="SplitList"/> item.
    /// This method gets called by <see cref="SplitListPool"/> when it releases an instance.
    /// </summary>
    public void Clear()
    {
        // Format and Splits were Initialize(...) arguments, we can safely reassign
        m_Format = null;

        // m_Splits was rented from the pool by Format.FindAll; return it.
        if (m_Splits != null)
        {
            UnityEngine.Pool.ListPool<int>.Release(m_Splits);
            m_Splits = null;
        }

        // Return the Formats we created to the pool
        for (var i = 0; i < m_FormatCache.Count; i++)
            if (m_FormatCache[i] != null)
                FormatPool.Pool.Release(m_FormatCache[i]);

        m_FormatCache.Clear();
    }

    ///<inheritdoc/>
    public void CopyTo(Format[] array, int arrayIndex)
    {
        var length = m_Splits.Count + 1;
        for (var i = 0; i < length; i++) array[arrayIndex + i] = this[i];
    }

    ///<inheritdoc/>
    public int Count => m_Splits.Count + 1;

    ///<inheritdoc/>
    public bool IsReadOnly => true;

    /// <summary>
    /// This method is not implemented.
    /// </summary>
    public int IndexOf(Format item)
    {
        throw new NotSupportedException();
    }

    /// <summary>
    /// This method is not implemented.
    /// </summary>
    public void Insert(int index, Format item)
    {
        throw new NotSupportedException();
    }

    /// <summary>
    /// This method is not implemented.
    /// </summary>
    public void RemoveAt(int index)
    {
        throw new NotSupportedException();
    }

    /// <summary>
    /// This method is not implemented.
    /// </summary>
    public void Add(Format item)
    {
        throw new NotSupportedException();
    }

    /// <summary>
    /// This method is not implemented.
    /// </summary>
    public bool Contains(Format item)
    {
        throw new NotSupportedException();
    }

    /// <summary>
    /// This method is not implemented.
    /// </summary>
    public bool Remove(Format item)
    {
        throw new NotSupportedException();
    }

    /// <summary>
    /// This method is not implemented.
    /// </summary>
    public IEnumerator<Format> GetEnumerator()
    {
        throw new NotSupportedException();
    }

    /// <summary>
    /// This method is not implemented.
    /// </summary>
    IEnumerator IEnumerable.GetEnumerator()
    {
        throw new NotSupportedException();
    }
}
