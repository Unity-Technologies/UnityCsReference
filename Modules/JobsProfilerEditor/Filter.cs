// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System;

namespace UnityEditor.JobsProfiling;

internal class Filter : IDisposable
{
    // List of filters that matches marker names.
    NativeHashSet<int> m_filterIds;
    ToolbarSearchField m_filter;
    FrameCache m_frameCache;
    string m_oldValue;
    bool m_useFilter = false;
    bool m_hasChanged = false;
    int m_oldSelectedFrame = -1;

    internal Filter(FrameCache frameCache, ToolbarSearchField filter)
    {
        m_frameCache = frameCache;
        m_filter = filter;
        m_filterIds = new NativeHashSet<int>(1, Allocator.Persistent);
    }

    public void Dispose()
    {
        if (m_filterIds.IsCreated)
            m_filterIds.Dispose();
    }

    // Update is called once per frame
    internal void Update(int selectedFrame)
    {
        // TODO: Cache and don't re-run this calculation
        FrameData frameData;

        if (!m_frameCache.Frames.TryGetValue(selectedFrame, out frameData))
            return;

        string filter = m_filter.value;

        if (m_oldValue != filter)
            m_hasChanged = true;
        else
            m_hasChanged = false;

        if (filter.Length == 0)
        {
            m_filterIds.Clear();
            m_oldValue = "";
            m_useFilter = false;
            return;
        }

        if (m_hasChanged || (m_oldSelectedFrame != selectedFrame))
        {
            m_filterIds.Clear();

            foreach (var marker in frameData.stringIndex)
            {
                int index = marker.Value;
                string name;

                if ((index >> FrameData.k_StringIndexShift) == 1)
                    name = frameData.strings512[index & FrameData.k_StringIndexMask].ToString();
                else
                    name = frameData.strings128[index].ToString();

                if (name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                    m_filterIds.Add(marker.Key);
            }

            m_useFilter = true;
        }

        m_oldSelectedFrame = selectedFrame;
        m_oldValue = filter;
    }

    internal NativeHashSet<int> FilterIds { get => m_filterIds; }
    internal bool HasChanged { get => m_hasChanged; }
    internal bool UseFilter { get => m_useFilter; }
}
