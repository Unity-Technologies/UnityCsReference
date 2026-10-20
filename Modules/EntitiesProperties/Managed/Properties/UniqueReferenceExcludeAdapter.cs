// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Properties;
using Unity.Scripting.LifecycleManagement;

namespace Unity.Entities
{
    // Exclude adapter for PropertyVisitor that ensures each unique reference instance is
    // visited at most once during a single root traversal. Avoiding infinite recursion on
    // reference cycles is a side effect of this; the primary intent is to deduplicate visits
    // to shared instances (e.g. the same managed object reached via multiple paths).
    //
    // For the consumers of this adapter (entity-reference fixup, entity/blob-reference patch
    // extraction and application), visit-once-per-instance is the correct semantic: the patch
    // mutates the shared instance in place, so visiting any one path is enough. A path-based
    // cycle detector would re-visit shared sub-graphs once per incoming path with no benefit.
    internal sealed class UniqueReferenceExcludeAdapter : IExcludePropertyAdapter
    {
        // ReferenceEqualityComparer is .NET 5+; use this shim for Unity's target framework.
        sealed class ReferenceEqualityComparer : IEqualityComparer<object>
        {
            [NoAutoStaticsCleanup]
            internal static readonly ReferenceEqualityComparer Instance = new ReferenceEqualityComparer();
            bool IEqualityComparer<object>.Equals(object x, object y) => ReferenceEquals(x, y);
            int IEqualityComparer<object>.GetHashCode(object obj) => RuntimeHelpers.GetHashCode(obj);
        }

        HashSet<object> m_VisitedReferences;

        // Null out instead of Clear() to release capacity from large traversals.
        public void PrepareForNewRootVisit() => m_VisitedReferences = null;

        public bool IsExcluded<TContainer, TValue>(in ExcludeContext<TContainer, TValue> context, ref TContainer container, ref TValue value)
        {
            if (typeof(TValue).IsValueType)
                return false;

            if (typeof(TValue) == typeof(string))
                return false;

            if (TypeTraits<TValue>.CanBeNull && null == value)
                return false;

            // Skip Unity objects entirely; they manage their own lifecycle and graph traversal.
            if (value is UnityEngine.Object)
                return true;

            // Use reference equality so that distinct instances with the same Equals/GetHashCode
            // are never mistaken for already-visited nodes.
            if (m_VisitedReferences == null)
                m_VisitedReferences = new HashSet<object>(ReferenceEqualityComparer.Instance);

            return !m_VisitedReferences.Add(value);
        }
    }
}
