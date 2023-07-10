// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.UIElements.UIR
{
    // This pool doesn't require items to be explicitly returned to the pool. When the owner calls Reset on it, the
    // items are implicitly considered being returned to the pool. This pool may allocate more items than the maximum
    // capacity of the pool. The excess items will be discarded when Reset is called. Only the remaining items will be
    // reset, if an action is provided.
    class ImplicitPool<T> where T : class
    {
        readonly int m_StartCapacity;
        readonly int m_MaxCapacity;

        Func<T> m_CreateAction;
        Action<T> m_ResetAction;
        List<T> m_List;
        int m_UsedCount;

        public ImplicitPool(Func<T> createAction, Action<T> resetAction, int startCapacity, int maxCapacity)
        {
            Debug.Assert(createAction != null);
            Debug.Assert(startCapacity > 0);
            Debug.Assert(startCapacity <= maxCapacity);
            Debug.Assert(maxCapacity > 0);

            m_List = new(0);
            m_StartCapacity = startCapacity;
            m_MaxCapacity = maxCapacity;
            m_CreateAction = createAction;
            m_ResetAction = resetAction;
        }

        public T Get()
        {
            if (m_UsedCount < m_List.Count)
                return m_List[m_UsedCount++];

            if (m_UsedCount < m_MaxCapacity)
            {
                // Expand by doubling the number of items
                int desiredAllocs = Mathf.Max(m_StartCapacity, m_UsedCount);
                int maxAllocs = m_MaxCapacity - m_UsedCount;
                int n = Mathf.Min(maxAllocs, desiredAllocs);

                m_List.Capacity = m_UsedCount + n;

                // We keep the first item
                var result = m_CreateAction();
                m_List.Add(result);
                ++m_UsedCount;

                for (int i = 1; i < n; ++i)
                    m_List.Add(m_CreateAction());

                return result;
            }

            // The pool is full
            return m_CreateAction();
        }

        public void ReturnAll()
        {
            Debug.Assert(m_List.Count <= m_MaxCapacity);

            // Reset what's been used
            if (m_ResetAction != null)
            {
                for (int i = 0; i < m_UsedCount; ++i)
                    m_ResetAction(m_List[i]);
            }

            m_UsedCount = 0;
        }
    }
}
