using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Unity.Scripting.LifecycleManagement
{
    // Ideally that list is also sorted in deterministic fashion so the relative order of two assemblies doesn't change after code modifications involving dependency changes.

    // Assemblies have dependencies to other assemblies. This collection orders assemblies in dependency order, i.e.
    // every dependency of an assembly comes before that assembly in the returned list. The ordering of independent
    // assemblies relative to each other is preserved.
    // Note: this algorithm is the same as the one in Modules.ModuleCollection in \Tools\Unity.BuildSystem\Unity.BuildSystem\ModuleCollection.cs
    [System.Diagnostics.DebuggerDisplay("Count = {Count}")]
    internal sealed class OrderedAssemblyList : IReadonlyOrderedAssemblyList
    {
        private Assembly[]? _assemblies;
        private readonly Dictionary<string, int> _assemblyLookup = new();

        internal bool IsPopulated => _assemblies != null;

        public int Count => _assemblies?.Length ?? 0;

        public Assembly this[int index] => _assemblies![index];

        public OrderedAssemblyList()
        {
        }

        public OrderedAssemblyList(IEnumerable<Assembly> assemblies)
        {
            Populate(assemblies);
        }

        private void Populate(IEnumerable<Assembly> assemblies)
        {
            if (IsPopulated)
            {
                throw new InvalidOperationException("Cannot repopulating sorted assembly list");
            }

            // Or maybe in the future this is the CodeReload functionality that should sort?
            // For now we just sort

#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var inputAssemblies = assemblies.ToArray();
#pragma warning restore UA2001

            TopologicalSort(ref inputAssemblies);

            _assemblies = inputAssemblies;
            for (var i = 0; i < _assemblies.Length; i++)
            {
                _assemblyLookup[_assemblies[i].GetName().Name!] = i;
            }
        }

        struct SortNode : IComparable<SortNode>
        {
            public string AssemblyName = null!;
            public Assembly Assembly = null!;
            public int[] Dependencies = Array.Empty<int>();

            public SortNode() { }

            public int CompareTo(SortNode other)
            {
                return string.Compare(AssemblyName, other.AssemblyName, StringComparison.Ordinal);
            }
        }

        static readonly IComparer<SortNode> VersionLessComparer = Comparer<SortNode>.Create((a, b) => string.Compare(a.AssemblyName, b.AssemblyName, StringComparison.Ordinal));

        static void TopologicalSort(ref Assembly[] assemblies)
        {
            SortNode[] sortNodes = new SortNode[assemblies.Length];

            // Populate sort nodes
            for (int i = 0; i < assemblies.Length; i++)
            {
                var name = assemblies[i].GetName();
                sortNodes[i] = new SortNode { AssemblyName = name.Name!, Assembly = assemblies[i] };
            }

            // Sort them by name for fast search
            Array.Sort(sortNodes);

            // Populate dependencies
            var dependencies = new List<int>();
            for (int i = 0; i < assemblies.Length; i++)
            {
                Assembly assembly = assemblies[i];
                var name = assembly.GetName();
                {
                    var nodeIndex = Array.BinarySearch(sortNodes, new SortNode { AssemblyName = name.Name! }, VersionLessComparer);

                    if (nodeIndex < 0)
                    {
                        // dependency is not available
                        continue;
                    }

                    dependencies.Clear();
                    var referencedAssemblies = assembly.GetReferencedAssemblies();
                    foreach (var dependency in referencedAssemblies)
                    {
                        if (dependency.Name == null)
                            continue;

                        var dependencyIndex = Array.BinarySearch(sortNodes, new SortNode { AssemblyName = dependency.Name }, VersionLessComparer);
                        if (dependencyIndex >= 0)
                            dependencies.Add(dependencyIndex);
                    }

                    sortNodes[nodeIndex].Dependencies = dependencies.ToArray();
                }
            }

            // Sort nodes topologically
            bool[] visitedNodes = new bool[assemblies.Length];
            int sortedNodeCount = 0;
            for (int i = 0; i < assemblies.Length; i++)
            {
                if (!visitedNodes[i])
                {
                    TopologicalSortRecursive(sortNodes, visitedNodes, i, ref assemblies, ref sortedNodeCount);
                }
            }
        }

        // Algorithm is very simple: visit each node, then visit its dependencies.
        // Once all dependencies are visited, add a node to the result list as at
        // that point all the dependencies have already been added to it
        static void TopologicalSortRecursive(SortNode[] sortNodes, bool[] visitedNodes, int index, ref Assembly[] sortedNodes, ref int sortedCount)
        {
            visitedNodes[index] = true;

            foreach (var dependency in sortNodes[index].Dependencies)
            {
                if (!visitedNodes[dependency])
                    TopologicalSortRecursive(sortNodes, visitedNodes, dependency, ref sortedNodes, ref sortedCount);
            }

            sortedNodes[sortedCount++] = sortNodes[index].Assembly;
        }

#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
        public IEnumerator<Assembly> GetEnumerator() => (_assemblies ?? Array.Empty<Assembly>()).AsEnumerable().GetEnumerator();
#pragma warning restore UA2001

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public bool TryGetAssembly(string assemblyName, out Assembly assembly)
        {
            if (_assemblies != null && _assemblyLookup.TryGetValue(assemblyName, out var assemblyIndex))
            {
                assembly = _assemblies[assemblyIndex];
                return true;
            }

            assembly = default!;
            return false;
        }

        public bool TryGetAssembly(AssemblyName assemblyName, out Assembly assembly)
        {
            return TryGetAssembly(assemblyName.Name!, out assembly);
        }

        public bool Contains(string assemblyName)
        {
            return _assemblyLookup.ContainsKey(assemblyName);
        }

        public bool Contains(AssemblyName assemblyName)
        {
            return Contains(assemblyName.Name!);
        }
    }
}
