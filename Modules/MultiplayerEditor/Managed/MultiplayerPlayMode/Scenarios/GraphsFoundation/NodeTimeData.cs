// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Editor
{
    [Serializable]
    internal struct NodeTimeData
    {
        [SerializeField] private long m_StartTimeTicks;
        [SerializeField] private long m_EndTimeTicks;

        public DateTime StartTime
        {
            get => new(m_StartTimeTicks);
            set => m_StartTimeTicks = value.Ticks;
        }

        public DateTime EndTime
        {
            get => new(m_EndTimeTicks);
            set => m_EndTimeTicks = value.Ticks;
        }

        public bool HasStarted => m_StartTimeTicks != default;
        public bool HasEnded => m_EndTimeTicks != default;
    }
}
