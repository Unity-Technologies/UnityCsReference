// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;

namespace UnityEditor.Scripting.APIUpdater
{
    [Serializable]
    internal class AssemblyDependencyGraph
    {
        public AssemblyDependencyGraph()
        {
            m_Graph = new List<DependencyEntry>();
        }

        public void AddDependencies(string root, params string[] dependencies)
        {
            InternalAddDependencies(root, dependencies);
        }

        public void SetDependencies(string root, params string[] dependencies)
        {
            var dep = FindAssembly(root);
            if (dep == null)
            {
                dep = FindInDependents(root);
            }

            if (dep != null)
            {
                dep.Dependencies.Clear();
            }

            InternalAddDependencies(root, dependencies);
        }

        private DependencyEntry InternalAddDependencies(string root, params string[] dependencies)
        {
            var existingDependency = FindAssembly(root);
            if (existingDependency == null)
            {
                existingDependency =  FindInDependents(root) ?? new DependencyEntry(root);
                m_Graph.Add(existingDependency);
            }

            foreach (var dependency in dependencies)
            {
                var dep = FindAssembly(dependency) ?? InternalAddDependencies(dependency);
                existingDependency.m_Dependencies.Add(dep);
            }

            return existingDependency;
        }

        private DependencyEntry FindInDependents(string root)
        {
            foreach (var entry in m_Graph)
            {
                var found = entry.m_Dependencies.FirstOrDefault(dependency => dependency.m_Name == root);
                if (found != null)
                    return found;
            }

            return null;
        }

        public IList<string> GetDependentsOf(string source)
        {
            var result = new List<string>();
            foreach (var dependencyEntry in m_Graph)
            {
                if (dependencyEntry.m_Dependencies.Any(candidate => candidate.m_Name == source))
                    result.Add(dependencyEntry.m_Name);
            }

            return result;
        }

        public IEnumerable<string> DependenciesFor(string dependent)
        {
            var found = FindAssembly(dependent);
            if (found != null)
            {
                return found.m_Dependencies.Select(dep => dep.m_Name);
            }

            return new string[0];
        }

        public void RemoveDependencies(string dependent, params string[] dependencies)
        {
            if (dependencies.Length == 0)
                return;

            var entry = FindAssembly(dependent);
            if (entry == null)
                return;

            entry.m_Dependencies.RemoveAll(candidate => dependencies.Contains(candidate.m_Name));
        }

        public void RemoveRoot(string tbr, bool updateDependents = false)
        {
            var toBeRemoved = m_Graph.Find(e => e.m_Name == tbr);
            if (toBeRemoved == null)
                return;

            m_Graph.Remove(toBeRemoved);

            if (updateDependents)
            {
                foreach (var entry in m_Graph)
                {
                    var danglingReference = entry.m_Dependencies.Find(e => e == toBeRemoved);
                    if (danglingReference == null)
                        continue;

                    entry.m_Dependencies.Remove(danglingReference);
                }
            }
        }

        public IEnumerable<string> SortedDependents()
        {
            if (m_Graph.Count == 0)
                return new string[0];

            var array = m_Graph.ToArray();

            m_Processed = new HashSet<string>();
            LogCycles(array, m_Processed);

            m_Processed.Clear();

            bool exchangeElementsInLastPass;
            var arrayLength = array.Length - 1;
            do
            {
                exchangeElementsInLastPass = false;
                for (int i = 0; i < arrayLength; i++)
                {
                    if (CompareElements(array[i], array[i + 1]) > 0)
                    {
                        var temp = array[i];
                        array[i] = array[i + 1];
                        array[i + 1] = temp;

                        exchangeElementsInLastPass = true;
                    }
                }

                arrayLength--;
            }
            while (exchangeElementsInLastPass);

            return array.Select(e => e.m_Name);
        }

        /*
         * Given two nodes (A & B ) in the graph
         * A > B if A -> B (direct or indirect)
         * A < B if B -> A (direct or indirect)
         */
        private int CompareElements(DependencyEntry lhs, DependencyEntry rhs)
        {
            var lhsDependsOnRhs = HasDirectOrIndirectDependency(lhs, rhs);
            if (lhsDependsOnRhs)
                return 1;

            var rshDependsOnLhs = HasDirectOrIndirectDependency(rhs, lhs);
            if (rshDependsOnLhs)
                return -1;

            return 0;
        }

        private bool HasDirectOrIndirectDependency(DependencyEntry lhs, DependencyEntry rhs)
        {
            var lhsDependsOnRhs = lhs.m_Dependencies.Contains(rhs);
            if (lhsDependsOnRhs)
                return true;

            m_Processed.Clear();
            return HasDirectOrIndirectDependencyRecursive(rhs, lhs.m_Dependencies);
        }

        bool HasDirectOrIndirectDependencyRecursive(DependencyEntry toBeLookedUp, IList<DependencyEntry> dependencies)
        {
            foreach (var entry in dependencies)
            {
                if (entry == toBeLookedUp)
                    return true;

                if (m_Processed.Contains(entry.Name))
                {
                    // We've found a cycle in the assemblies (which has already been logged)
                    return false;
                }

                m_Processed.Add(entry.Name);
                try
                {
                    if (HasDirectOrIndirectDependencyRecursive(toBeLookedUp, entry.m_Dependencies))
                        return true;
                }
                finally
                {
                    m_Processed.Remove(entry.Name);
                }
            }

            return false;
        }

        static void LogCycles(IEnumerable<DependencyEntry> entries, HashSet<string> seen)
        {
            foreach (var entry in entries)
            {
                if ((entry.Status & AssemblyStatus.NoCyclesDetected) == AssemblyStatus.NoCyclesDetected)
                    continue;

                if (seen.Contains(entry.Name))
                {
                    Console.WriteLine($"[APIUpdater] Warning: Cycle detected in assembly references: {string.Join("->", seen.ToArray())}->{entry.Name}. This is not supported and AssemblyUpdater may not work as expected.");
                    continue;
                }

                seen.Add(entry.Name);

                LogCycles(entry.Dependencies, seen);
                entry.Status |= AssemblyStatus.NoCyclesDetected;

                seen.Remove(entry.Name);
            }
        }

        /// <summary>
        /// Serialized format:
        ///
        /// +---------------+------------------+-----------------------------+
        /// |   Hash Length | Hash of Payload  | Payload (serialized data)   |
        /// +---------------+------------------+-----------------------------+
        /// </summary>
        public void SaveTo(Stream stream)
        {
            var hasher = SHA256.Create();
            var hash = hasher.ComputeHash(BitConverter.GetBytes(42));

            long hashLength = hash.Length;
            var h = BitConverter.GetBytes(hashLength);

            stream.Write(h, 0, h.Length); // Write the hash length
            stream.Write(hash, 0, hash.Length); // and reserve space of the "payload" hash.

            var formatter = new BinaryFormatter();
            formatter.Serialize(stream, this);

            var endOfStream = stream.Position;

            stream.Position = hash.Length + h.Length; // Position the stream in the first byte of the serialized data (i.e, skip *hash length* and *hash*
            hash = hasher.ComputeHash(stream);

            stream.Position = h.Length; // position the stream past the *hash length* (i.e, *hash first byte*)
            stream.Write(hash, 0, hash.Length);

            stream.Position = endOfStream;
        }

        public static AssemblyDependencyGraph LoadFrom(Stream stream)
        {
            var hashLengthArray = new byte[sizeof(long)];
            stream.Read(hashLengthArray, 0, hashLengthArray.Length);

            long hashLengthSize = BitConverter.ToInt64(hashLengthArray, 0);
            var storedHash = new byte[hashLengthSize];
            stream.Read(storedHash, 0, storedHash.Length);

            var startOfSerializedData = stream.Position;
            var hasher = SHA256.Create();
            var computedHash = hasher.ComputeHash(stream);

            if (storedHash.Length != computedHash.Length)
            {
                return null;
            }

            for (int i = 0; i < hashLengthSize; i++)
            {
                if (storedHash[i] != computedHash[i])
                {
                    return null;
                }
            }

            stream.Position = startOfSerializedData;

            var serializer = new BinaryFormatter();
            return (AssemblyDependencyGraph)serializer.Deserialize(stream);
        }

        internal DependencyEntry FindAssembly(string dependent)
        {
            return m_Graph.Find(e => e.m_Name == dependent);
        }

        [Serializable]
        internal class DependencyEntry : IComparable<DependencyEntry>
        {
            public string m_Name;
            public List<DependencyEntry> m_Dependencies;

            public DependencyEntry()
            {
            }

            public DependencyEntry(string root)
            {
                m_Name = root;
                m_Dependencies = new List<DependencyEntry>();
            }

            public AssemblyStatus Status { get; set; }

            public string Name => m_Name;

            public int CompareTo(DependencyEntry other)
            {
                return string.Compare(m_Name, other.m_Name, StringComparison.Ordinal);
            }

            public List<DependencyEntry> Dependencies
            {
                get { return m_Dependencies; }
            }

            public override string ToString()
            {
                return string.Format("{0} ({1})", m_Name, m_Dependencies.Count);
            }
        }

        List<DependencyEntry> m_Graph;
        HashSet<string> m_Processed; // used to ignore cycles.
    }

    [Flags]
    internal enum AssemblyStatus
    {
        None = 0x0,
        PublishesUpdaterConfigurations = 0x02,
        NoCyclesDetected = 0x04
    }
}
