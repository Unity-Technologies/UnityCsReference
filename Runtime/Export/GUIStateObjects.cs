// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngineInternal;

namespace UnityEngine
{
    internal class GUIStateObjects
    {
        static Dictionary<int, object> s_StateCache = new Dictionary<int, object>();

        [System.Security.SecuritySafeCritical]
        internal static object GetStateObject(System.Type t, int controlID)
        {
            object o;
            if (!s_StateCache.TryGetValue(controlID, out o) || o.GetType() != t)
            {
                o = System.Activator.CreateInstance(t);
                s_StateCache[controlID] = o;
            }
            return o;
        }

        internal static object QueryStateObject(System.Type t, int controlID)
        {
            object o = s_StateCache[controlID];
            if (t.IsInstanceOfType(o))
            {
                return o;
            }
            return null;
        }

        static internal void Tests_ClearObjects()
        {
            s_StateCache.Clear();
        }
    }
}
