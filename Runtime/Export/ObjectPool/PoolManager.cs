// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.Pool
{
    static class PoolManager
    {
        static readonly List<WeakReference> s_WeakPoolReferences = new();

        public static void Reset()
        {
            for (int i = s_WeakPoolReferences.Count - 1; i >= 0; i--)
            {
                if (s_WeakPoolReferences[i].Target is IPool pool)
                {
                    pool.Clear();
                }
                else
                {
                    s_WeakPoolReferences.RemoveAt(i);
                }
            }
        }

        public static void Register(IPool pool)
        {
            s_WeakPoolReferences.Add(new WeakReference(pool));
        }
    }
}
