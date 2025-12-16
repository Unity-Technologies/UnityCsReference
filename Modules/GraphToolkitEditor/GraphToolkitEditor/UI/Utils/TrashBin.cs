// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Pool;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Keeps track of disposable items that should be disposed of when the TrashBin is disposed.
    /// The TrashBin itself use the object pool pattern to avoid frequent allocations and deallocations.
    /// </summary>
    class TrashBin : IDisposable
    {
        static List<TrashBin> s_Pool;

        private TrashBin() { } // Private constructor to prevent instantiation outside of the pool


        public static TrashBin Get()
        {
            if (s_Pool?.Count > 0)
            {
                var lastIndex = s_Pool.Count - 1;
                var trashBin = s_Pool[lastIndex];
                s_Pool.RemoveAt(lastIndex);
                return trashBin;
            }

            return new TrashBin();
        }

        List<IDisposable> m_TrashBin = new();

        public void Add(IDisposable disposable)
        {
            m_TrashBin.Add(disposable);
        }

        public void Dispose()
        {
            if (m_TrashBin == null)
                return;
            foreach (var disposable in m_TrashBin)
            {
                disposable.Dispose();
            }
            m_TrashBin.Clear();
            s_Pool ??= new List<TrashBin>();
            s_Pool.Add(this);
        }
    }
}
