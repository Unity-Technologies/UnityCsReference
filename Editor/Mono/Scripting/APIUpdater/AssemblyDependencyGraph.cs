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

        private DependencyEntry InternalAddDependencies(string root, params string[] dependencies)
        {
            var existsingDependency = FindRoot(root);
            if (existsingDependency == null)
            {
                existsingDependency = new DependencyEntry(root);
                m_Graph.Add(existsingDependency);
            }

            foreach (var dependency in dependencies)
            {
                var dep = FindRoot(dependency)
                    ?? InternalAddDependencies(dependency);

                existsingDependency.m_Dependencies.Add(dep);
            }

            return existsingDependency;
        }

        public IEnumerable<string> DependenciesFor(string dependent)
        {
            var found = FindRoot(dependent);
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

            var entry = FindRoot(dependent);
            if (entry == null)
                return;

            entry.m_Dependencies.RemoveAll(candidate => dependencies.Contains(candidate.m_Name));
        }

        public void RemoveRoot(string tbr)
        {
            var toBeRemoved = m_Graph.Find(e => e.m_Name == tbr);
            if (toBeRemoved == null)
                return;

            foreach (var entry in m_Graph)
            {
                var danglingRereference = entry.m_Dependencies.Find(e => e == toBeRemoved);
                if (danglingRereference == null)
                    continue;

                entry.m_Dependencies.Remove(danglingRereference);
            }

            m_Graph.Remove(toBeRemoved);
        }

        public IEnumerable<string> SortedDependents()
        {
            if (m_Graph.Count == 0)
                return new string[0];

            var array = m_Graph.ToArray();

            bool exchangeElementsInLastPass;
            do
            {
                exchangeElementsInLastPass = false;
                for (int i = 0; i < array.Length - 1; i++)
                {
                    if (CompareElements(array[i], array[i + 1]) > 0)
                    {
                        var temp = array[i];
                        array[i] = array[i + 1];
                        array[i + 1] = temp;

                        exchangeElementsInLastPass = true;
                    }
                }
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
            var rshDependsOnLhs = HasDirectOrIndirectDependency(lhs, rhs);
            if (rshDependsOnLhs)
                return 1;

            var lhsDependsOnRhs = HasDirectOrIndirectDependency(rhs, lhs);
            if (lhsDependsOnRhs)
                return -1;

            return 0;
        }

        private static bool HasDirectOrIndirectDependency(DependencyEntry lhs, DependencyEntry rhs)
        {
            var lhsDependendsOnRhs = lhs.m_Dependencies.Contains(rhs);
            if (lhsDependendsOnRhs)
                return true;

            return HasDirectOrIndirectDependencyRecursive(rhs, lhs.m_Dependencies);
        }

        private static bool HasDirectOrIndirectDependencyRecursive(DependencyEntry toBeLookedUp, IEnumerable<DependencyEntry> dependencies)
        {
            foreach (var entry in dependencies)
            {
                if (entry == toBeLookedUp)
                    return true;

                if (HasDirectOrIndirectDependencyRecursive(toBeLookedUp, entry.m_Dependencies))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Serialized format:
        ///
        /// +--------------+------------------+-----------------------------+
        /// |   Hash Lengh | Hash of Payload  | Payload (serialized data)   |
        /// +--------------+------------------+-----------------------------+
        /// </summary>
        public void SaveTo(Stream stream)
        {
            var hasher = SHA256.Create();
            var hash = hasher.ComputeHash(BitConverter.GetBytes(42));

            long hashLength = hash.Length;
            var h = BitConverter.GetBytes(hashLength);

            stream.Write(h, 0, h.Length); // Write the hash length
            stream.Write(hash, 0, hash.Length); // and reserve space of the "payload" hash.

            var formater = new BinaryFormatter();
            formater.Serialize(stream, this);

            var endOfStream = stream.Position;

            stream.Position = hash.Length + h.Length; // Position the stream in the first byte of the serialized data (i.e, skip *hash lenght* and *hash*
            hash = hasher.ComputeHash(stream);

            stream.Position = h.Length; // position the stream past the *hash lenght* (i.e, *hash first byte*)
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

        private DependencyEntry FindRoot(string dependent)
        {
            return m_Graph.Find(e => e.m_Name == dependent);
        }

        [Serializable]
        private class DependencyEntry : IComparable<DependencyEntry>
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

            public int CompareTo(DependencyEntry other)
            {
                return string.Compare(m_Name, other.m_Name, StringComparison.Ordinal);
            }

            public override string ToString()
            {
                return string.Format("{0} ({1})", m_Name, m_Dependencies.Count);
            }
        }

        private List<DependencyEntry> m_Graph;
    }
}
