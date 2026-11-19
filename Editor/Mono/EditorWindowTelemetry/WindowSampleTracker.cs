// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor
{
    internal sealed class WindowSampleTracker
    {
        [Serializable]
        class PersistedState
        {
            public int activeSamples;
            public int playModeSamples;
            public List<WindowUsage> rows;
        }

        readonly Dictionary<(string id, bool inPlayMode), WindowUsage> m_Rows = new Dictionary<(string id, bool inPlayMode), WindowUsage>();
        int m_SampleStamp;
        bool m_SampleInPlayMode;

        internal int activeSamples { get; private set; }
        internal int playModeSamples { get; private set; }

        internal void BeginSample(bool inPlayMode)
        {
            ++m_SampleStamp;
            m_SampleInPlayMode = inPlayMode;
            ++activeSamples;
            if (inPlayMode)
                ++playModeSamples;
        }

        internal void CountInstance(string id, bool visible, bool focused)
        {
            var row = GetOrAddRow(id, m_SampleInPlayMode);
            if (row.m_LastSampleStamp != m_SampleStamp)
            {
                row.m_LastSampleStamp = m_SampleStamp;
                row.m_Instances = 0;
                row.m_VisibleCounted = false;
                ++row.openSamples;
            }

            ++row.m_Instances;
            if (row.m_Instances > row.peakInstances)
                row.peakInstances = row.m_Instances;

            if (!visible)
                return;

            if (!row.m_VisibleCounted)
            {
                row.m_VisibleCounted = true;
                ++row.visibleSamples;
            }

            if (focused)
                ++row.focusedSamples;
        }

        internal void AddFocusGain(string id, bool inPlayMode)
        {
            ++GetOrAddRow(id, inPlayMode).focusGains;
        }

        internal List<WindowUsage> Snapshot()
        {
            return new List<WindowUsage>(m_Rows.Values);
        }

        internal string ToJson()
        {
            var state = new PersistedState
            {
                activeSamples = activeSamples,
                playModeSamples = playModeSamples,
                rows = Snapshot(),
            };
            return JsonUtility.ToJson(state);
        }

        internal void FromJson(string json)
        {
            var state = JsonUtility.FromJson<PersistedState>(json);
            Clear();
            activeSamples = state.activeSamples;
            playModeSamples = state.playModeSamples;
            foreach (var row in state.rows)
                m_Rows[(row.id, row.inPlayMode)] = row;
        }

        internal void Clear()
        {
            m_Rows.Clear();
            activeSamples = 0;
            playModeSamples = 0;
        }

        WindowUsage GetOrAddRow(string id, bool inPlayMode)
        {
            var key = (id, inPlayMode);
            if (!m_Rows.TryGetValue(key, out var row))
            {
                row = new WindowUsage { id = id, inPlayMode = inPlayMode };
                m_Rows.Add(key, row);
            }
            return row;
        }
    }
}
