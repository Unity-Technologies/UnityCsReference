// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace UnityEngine.UIElements.UIR
{
    class GCHandlePool : IDisposable
    {
        List<GCHandle> m_Handles;
        int m_UsedHandlesCount;
        readonly int k_AllocBatchSize;

        public GCHandlePool(int capacity = 256, int allocBatchSize = 64)
        {
            m_Handles = new List<GCHandle>(capacity);
            m_UsedHandlesCount = 0;
            k_AllocBatchSize = allocBatchSize;
        }

        public GCHandle Get(object target)
        {
            if (target == null)
                return new GCHandle();

            if (m_UsedHandlesCount < m_Handles.Count)
            {
                var h = m_Handles[m_UsedHandlesCount++];
                h.Target = target;
                return h;
            }
            else
            {
                var h = GCHandle.Alloc(target);
                m_Handles.Add(h);
                ++m_UsedHandlesCount;

                for (int i = 0, count = k_AllocBatchSize - 1; i < count; ++i)
                    m_Handles.Add(GCHandle.Alloc(null));

                return h;
            }
        }

        public IntPtr GetIntPtr(object target)
        {
            if (target == null)
                return IntPtr.Zero;

            return GCHandle.ToIntPtr(Get(target));
        }

        public void ReturnAll()
        {
            for (int i = 0; i < m_UsedHandlesCount; ++i)
            {
                var h = m_Handles[i];
                h.Target = null;
                m_Handles[i] = h;
            }
            m_UsedHandlesCount = 0;
        }

        #region Dispose Pattern

        internal bool disposed { get; private set; }


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                foreach (var h in m_Handles)
                {
                    if (h.IsAllocated)
                        h.Free();
                }

                m_Handles = null;
            }

            disposed = true;
        }

        #endregion // Dispose Pattern
    }
}
