// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace UnityEditor
{
    public sealed partial class Selection
    {
        public static bool Contains(Object obj) { return Contains(obj.GetInstanceID()); }

        internal static void Add(int instanceID)
        {
            var ids = new List<int>(Selection.instanceIDs);
            if (ids.IndexOf(instanceID) < 0)
            {
                ids.Add(instanceID);
                Selection.instanceIDs = ids.ToArray();
            }
        }

        internal static void Add(Object obj)
        {
            if (obj != null)
                Add(obj.GetInstanceID());
        }

        internal static void Remove(int instanceID)
        {
            var ids = new List<int>(Selection.instanceIDs);
            ids.Remove(instanceID);
            Selection.instanceIDs = ids.ToArray();
        }

        internal static void Remove(Object obj)
        {
            if (obj != null)
                Remove(obj.GetInstanceID());
        }

        private static IEnumerable GetFilteredInternal(System.Type type, SelectionMode mode)
        {
            if (typeof(Component).IsAssignableFrom(type) || type.IsInterface)
                return GetTransforms(mode).Select(t => t.GetComponent(type)).Where(c => c != null);
            else if (typeof(GameObject).IsAssignableFrom(type))
                return GetTransforms(mode).Select(t => t.gameObject);
            else
                return GetObjectsMode(mode).Where(o => o != null && type.IsAssignableFrom(o.GetType()));
        }

        public static T[] GetFiltered<T>(SelectionMode mode) // no generic constraint because we also want to allow interfaces
        {
            return GetFilteredInternal(typeof(T), mode).Cast<T>().ToArray();
        }

        public static Object[] GetFiltered(System.Type type, SelectionMode mode)
        {
            return GetFilteredInternal(type, mode).Cast<Object>().ToArray();
        }
    }
}
