// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.UIElements.HierarchyV2;

interface IStickyRowController
{
    event Action<int, bool> onStickyStateChanged;
    void SetSticky(int index, bool enabled);
    bool IsSticky(int index);
    int GetPreviousStickyIndex(int index);
}

internal class StickyRowController : IStickyRowController
{
    readonly List<int> m_StickyItems = new();

    public event Action<int, bool> onStickyStateChanged;

    /// <summary>
    /// Sets the index sticky state.
    /// </summary>
    /// <param name="index"></param>
    /// <param name="enabled"></param>
    public void SetSticky(int index, bool enabled)
    {
        if (enabled)
        {
            if (!m_StickyItems.Contains(index))
            {
                m_StickyItems.Add(index);
                onStickyStateChanged?.Invoke(index, true);
            }
        }
        else if (m_StickyItems.Remove(index))
        {
            onStickyStateChanged?.Invoke(index, false);
        }
    }

    /// <summary>
    /// Sets the index sticky state without firing <see cref="onStickyStateChanged"/>.
    /// Use this when rebuilding sticky indices in batch before a refresh.
    /// </summary>
    /// <param name="index">The item index.</param>
    /// <param name="enabled">Whether the item should be sticky.</param>
    public void SetStickyWithoutNotify(int index, bool enabled)
    {
        if (enabled)
        {
            if (!m_StickyItems.Contains(index))
                m_StickyItems.Add(index);
        }
        else
        {
            m_StickyItems.Remove(index);
        }
    }

    /// <summary>
    /// Is the given index sticky.
    /// </summary>
    /// <param name="index">The item index.</param>
    /// <returns>True if the index is sticky.</returns>
    public bool IsSticky(int index)
    {
        return m_StickyItems.Contains(index);
    }

    /// <summary>
    /// Returns the largest sticky index that is less than or equal to <paramref name="startIndex"/>,
    /// or -1 if none exists.
    /// </summary>
    /// <param name="startIndex">The index to search from.</param>
    /// <returns>The previous sticky index, or -1.</returns>
    public int GetPreviousStickyIndex(int startIndex)
    {
        // Linear scan for the largest index <= startIndex. The list is likely to be tiny, so this is faster and more
        // cache-friendly than a binary search, and it avoids the need to sort.
        // We can revisit this decision if/when it becomes a bottleneck.
        var result = -1;
        for (var i = 0; i < m_StickyItems.Count; i++)
        {
            var index = m_StickyItems[i];
            if (index <= startIndex && index > result)
                result = index;
        }
        return result;
    }

    /// <summary>
    /// Removes all sticky indices without firing events.
    /// </summary>
    public void ClearWithoutNotify()
    {
        m_StickyItems.Clear();
    }
}
