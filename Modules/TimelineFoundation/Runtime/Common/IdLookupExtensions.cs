// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Bindings;

namespace Unity.Timeline.Foundation.Common
{
    [VisibleToOtherModules("UnityEditor.TimelineFoundationModule")]
    internal static class IdLookupExtensions
    {
        public static T GetValue<T>(this IReadOnlyDictionary<UniqueID, T> dictionary, UniqueID id)
        {
            return dictionary.TryGetValue(id, out T value) ? value : default;
        }

        public static IEnumerable<T> GetValues<T>(this IReadOnlyDictionary<UniqueID, T> dictionary, IEnumerable<UniqueID> ids)
        {
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return dictionary.Keys.Intersect(ids).Select(id => dictionary[id]);
#pragma warning restore UA2001
        }

        public static void RemoveIntersection<T>(this Dictionary<UniqueID, T> dictionary, IReadOnlyDictionary<UniqueID, T> other)
        {
            SetOperation(dictionary, other, (i, _) => dictionary.Remove(i), null);
        }

        public static void Union<T>(this Dictionary<UniqueID, T> dictionary, IReadOnlyDictionary<UniqueID, T> other)
        {
            SetOperation(dictionary, other, null, (i, v) => dictionary[i] = v);
        }

        static void SetOperation<T>(Dictionary<UniqueID, T> dictionary, IReadOnlyDictionary<UniqueID, T> other,
            Action<UniqueID, T> toIntersected, Action<UniqueID, T> toDisjointed)
        {
            foreach (var p in other)
            {
                if (dictionary.ContainsKey(p.Key))
                    toIntersected?.Invoke(p.Key, p.Value);
                else
                    toDisjointed?.Invoke(p.Key, p.Value);
            }
        }
    }
}
