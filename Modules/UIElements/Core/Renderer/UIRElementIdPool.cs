// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEngine.UIElements.UIR
{
    // Dense 16-bit elementId allocation: free-list of released ids + a high-water counter (ids 1..65535; 0 reserved).
    class ElementIdPool
    {
        public const int kMaxElementIds = ushort.MaxValue + 1; // 0 reserved for the default record

        readonly Stack<ushort> m_Free = new();
        int m_HighWater = 1;

        public int highWater => m_HighWater;

        public bool Acquire(out ushort id)
        {
            if (m_Free.Count > 0)
            {
                id = m_Free.Pop();
                return true;
            }

            if (m_HighWater < kMaxElementIds)
            {
                id = (ushort)m_HighWater++;
                return true;
            }

            id = 0;
            return false;
        }

        public void Release(ushort id)
        {
            if (id != 0)
                m_Free.Push(id);
        }

        public void Clear()
        {
            m_Free.Clear();
            m_HighWater = 1;
        }
    }
}
