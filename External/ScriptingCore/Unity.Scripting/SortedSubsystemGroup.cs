using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Unity.Scripting
{
    /// <summary>
    /// A group of subsystems sorted by their dependencies
    /// </summary>
    /// <typeparam name="TInitDelegate">Type of the subsystems initialization delegate (e.g. Action&lt;AssemblyLoadContext&gt;)</typeparam>
    /// <typeparam name="TCleanupDelegate">Type of the cleanup delegate</typeparam>
    internal class SortedSubsystemGroup<TInitDelegate, TCleanupDelegate>
    {
        readonly Dictionary<string, SubsystemEntry> m_Subsystems = new();
        bool Frozen => m_InitDelegates != null;
        TInitDelegate[]? m_InitDelegates;
        TCleanupDelegate[]? m_CleanupDelegates;

        /// <summary>
        /// Register a subsystem with the group
        /// </summary>
        /// <param name="name">declarative name of the subsystem</param>
        /// <param name="initDelegate">delegate to call for initialization</param>
        /// <param name="cleanupDelegate">delegate to call for cleanup</param>
        /// <param name="dependencies">other subsystems depended on</param>
        public void RegisterSubsystem(string name, TInitDelegate? initDelegate, TCleanupDelegate? cleanupDelegate, string[]? dependencies)
        {
            if (Frozen)
                throw new InvalidOperationException("Cannot register subsystems after the group has been frozen");
            if (m_Subsystems.ContainsKey(name))
                throw new ArgumentException($"Subsystem {name} already registered");
            m_Subsystems.Add(name, new SubsystemEntry(name, initDelegate, cleanupDelegate, dependencies ?? Array.Empty<string>()));
        }

        public TInitDelegate[] SortedInitCallbacks
        {
            get
            {
                if (m_InitDelegates == null)
                    SortAndFreeze();
                return m_InitDelegates!;
            }
        }

        public TCleanupDelegate[] SortedCleanupCallbacks
        {
            get
            {
                if (m_CleanupDelegates == null)
                    SortAndFreeze();
                return m_CleanupDelegates!;
            }
        }

        private void SortAndFreeze()
        {
            SubsystemEntry[] sorted = TopologicalSort();
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            m_InitDelegates = sorted.Where(s => s.InitDelegate != null).Select(s => s.InitDelegate!).ToArray();
#pragma warning restore UA2001

#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            m_CleanupDelegates = sorted.AsEnumerable().Reverse().Where(s => s.CleanupDelegate != null).Select(s => s.CleanupDelegate!).ToArray();
#pragma warning restore UA2001
        }

        private SortedSubsystemGroup<TInitDelegate, TCleanupDelegate>.SubsystemEntry[] TopologicalSort()
        {
            var visited = new HashSet<string>();
            var sorted = new List<SubsystemEntry>();
            var pendingDependencies = new HashSet<string>();
            foreach (var subsystem in m_Subsystems.Values)
            {
                Visit(subsystem, visited, sorted, pendingDependencies);
            }

            return sorted.ToArray();
        }

        private void Visit(SortedSubsystemGroup<TInitDelegate, TCleanupDelegate>.SubsystemEntry subsystem, HashSet<string> visited, List<SortedSubsystemGroup<TInitDelegate, TCleanupDelegate>.SubsystemEntry> sorted, HashSet<string> pendingDependencies)
        {
            if (visited.Contains(subsystem.Name))
                return;

            visited.Add(subsystem.Name);
            pendingDependencies.Add(subsystem.Name);
            foreach (var dependency in subsystem.Dependencies)
            {
                if (!m_Subsystems.TryGetValue(dependency, out var depSubsystem))
                    throw new InvalidOperationException($"Subsystem {subsystem.Name} depends on unknown subsystem {dependency}");
                // handle circular dependencies
                if (pendingDependencies.Contains(dependency))
                    throw new InvalidOperationException($"Circular dependency detected: {subsystem.Name} -> {dependency}");
                Visit(depSubsystem, visited, sorted, pendingDependencies);
            }
            pendingDependencies.Remove(subsystem.Name);

            sorted.Add(subsystem);
        }

        class SubsystemEntry
        {
            public string Name { get; }
            public TInitDelegate? InitDelegate { get; }
            public TCleanupDelegate? CleanupDelegate { get; }
            public string[] Dependencies { get; }

            public SubsystemEntry(string name, TInitDelegate? initDelegate, TCleanupDelegate? cleanupDelegate, string[] dependencies)
            {
                Name = name;
                InitDelegate = initDelegate;
                CleanupDelegate = cleanupDelegate;
                Dependencies = dependencies;
            }
        }
    }
}
