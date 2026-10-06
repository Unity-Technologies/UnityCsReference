// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using System.Collections.Generic;

namespace UnityEditor.Scripting.CodeReload
{
    internal readonly struct LeakIdentity : IEquatable<LeakIdentity>, IComparable<LeakIdentity>
    {
        readonly string m_AssemblyName;
        readonly string m_TypeName;

        internal LeakIdentity(string assemblyName, string typeName)
        {
            m_AssemblyName = assemblyName ?? string.Empty;
            m_TypeName = typeName ?? string.Empty;
        }

        internal string AssemblyName => m_AssemblyName;
        internal string TypeName => m_TypeName;

        internal bool IsValid => m_AssemblyName.Length > 0 && m_TypeName.Length > 0;

        public bool Equals(LeakIdentity other)
        {
            return string.Equals(m_AssemblyName, other.m_AssemblyName, StringComparison.Ordinal) &&
                string.Equals(m_TypeName, other.m_TypeName, StringComparison.Ordinal);
        }

        public override bool Equals(object obj) => obj is LeakIdentity other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(m_AssemblyName, m_TypeName);

        public int CompareTo(LeakIdentity other)
        {
            var byAssembly = string.CompareOrdinal(m_AssemblyName, other.m_AssemblyName);
            return byAssembly != 0 ? byAssembly : string.CompareOrdinal(m_TypeName, other.m_TypeName);
        }

        public override string ToString() => m_AssemblyName + " / " + m_TypeName;
    }

    internal readonly struct LeakAllowListEntry
    {
        readonly LeakIdentity m_Identity;
        readonly string m_OwningTeam;
        readonly string m_TrackingTicket;

        internal LeakAllowListEntry(string assemblyName, string typeName, string owningTeam, string trackingTicket)
        {
            m_Identity = new LeakIdentity(assemblyName, typeName);
            m_OwningTeam = owningTeam;
            m_TrackingTicket = trackingTicket;
        }

        internal LeakIdentity Identity => m_Identity;
        internal string OwningTeam => m_OwningTeam;
        internal string TrackingTicket => m_TrackingTicket;
    }

    internal static class LeakDetectorAllowList
    {
        internal static IReadOnlyList<LeakAllowListEntry> Entries => LeakDetectorAllowListEntries.Entries;

        internal static bool IsAllowed(in LeakIdentity identity)
        {
            if (!identity.IsValid)
                return false;

            var entries = Entries;
            for (int i = 0; i < entries.Count; ++i)
            {
                if (entries[i].Identity.Equals(identity))
                    return true;
            }

            return false;
        }

        internal static LeakIdentity GetLeakIdentity(in AssemblyLoadContextLeakDetector.FullLeakDetectionResult result, in AssemblyLoadContextLeakDetector.Leak leak)
        {
            if (leak.PathItems == null || leak.PathItems.Length == 0)
                return new LeakIdentity(string.Empty, string.Empty);

            var leakedType = result.MemberTypes[leak.PathItems[0].TypeIndex];
            return new LeakIdentity(leakedType.AssemblyName, leakedType.TypeName);
        }

        internal static List<LeakIdentity> GetLeakIdentities(AssemblyLoadContextLeakDetector.FullLeakDetectionResult result)
        {
            var identities = new List<LeakIdentity>();
            var leaks = result.Leaks;
            if (leaks == null)
                return identities;

            for (int i = 0; i < leaks.Length; ++i)
                identities.Add(GetLeakIdentity(result, leaks[i]));

            return identities;
        }

        internal static SortedDictionary<LeakIdentity, int> GetDisallowedLeaks(IEnumerable<LeakIdentity> observed)
        {
            var disallowed = new SortedDictionary<LeakIdentity, int>();
            foreach (var identity in observed)
            {
                if (IsAllowed(identity))
                    continue;

                disallowed.TryGetValue(identity, out var count);
                disallowed[identity] = count + 1;
            }

            return disallowed;
        }

        internal static List<LeakAllowListEntry> GetUnusedEntries(IEnumerable<LeakIdentity> observed)
        {
            var observedSet = new HashSet<LeakIdentity>(observed);
            var unused = new List<LeakAllowListEntry>();

            foreach (var entry in Entries)
            {
                if (!observedSet.Contains(entry.Identity))
                    unused.Add(entry);
            }

            return unused;
        }
    }
}

