// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security.Cryptography;

namespace UnityEditor.Scripting.APIUpdater
{
    [Serializable]
    [DataContract]
    internal class AssemblyDependencyGraph
    {
        public static readonly byte[] Signature = new byte[] { 0x55, 0x41, 0x44, 0x47 }; // UADG

        // Version 1 used binary serialization, version 2 uses DataContractSerializer and extends the format to include a signature and version.
        public static readonly byte Version = 2;

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
                existingDependency = FindInDependents(root) ?? new DependencyEntry(root);
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
#pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                var found = entry.m_Dependencies.FirstOrDefault(dependency => dependency.m_Name == root);
#pragma warning restore RS0030
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
#pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                if (dependencyEntry.m_Dependencies.Any(candidate => candidate.m_Name == source))
#pragma warning restore RS0030
                    result.Add(dependencyEntry.m_Name);
            }

            return result;
        }

        public IEnumerable<string> DependenciesFor(string dependent)
        {
            var found = FindAssembly(dependent);
            if (found != null)
            {
#pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                return found.m_Dependencies.Select(dep => dep.m_Name);
#pragma warning restore RS0030
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

#pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            entry.m_Dependencies.RemoveAll(candidate => dependencies.Contains(candidate.m_Name));
#pragma warning restore RS0030
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
                return Array.Empty<string>();

            var array = m_Graph.ToArray();

            m_Processed = new HashSet<string>();
            LogCycles(array, m_Processed);

            m_Processed.Clear();

            Array.Sort(array, CompareElements);

#pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return array.Select(e => e.m_Name);
#pragma warning restore RS0030
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
#pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                    Console.WriteLine($"[APIUpdater] Warning: Cycle detected in assembly references: {string.Join("->", seen.ToArray())}->{entry.Name}. This is not supported and AssemblyUpdater may not work as expected.");
#pragma warning restore RS0030
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
        /// +-----------+----------+-------------+------------------+---------------------------+
        /// | Signature | Version | Hash Length | Hash of Payload  | Payload (serialized data) |
        /// +-----------+---------+-------------+------------------+--------------------------+
        /// Signature: 4 bytes = `UADG` (0x55, 0x41, 0x44, 0x47) (Unity Assembly Dependency Graph)
        /// Version: 1 byte = 2 (current version)
        /// Hash Length: 8 bytes = sizeof(long)
        /// Hash: 32 bytes = SHA256 hash of the serialized data
        /// Payload: Serialized data
        /// </summary>
        public void SaveTo(Stream stream)
        {
            stream.Write(Signature, 0, Signature.Length);
            stream.WriteByte(Version);

            var hasher = SHA256.Create();
            long hashLength = hasher.HashSize / 8;

            Span<byte> hashLengthSpan = stackalloc byte[sizeof(long)];
            MemoryMarshal.TryWrite(hashLengthSpan, ref hashLength);

            stream.Write(hashLengthSpan); // Write the hash length
            var hashOffset = stream.Position;
            stream.Position += hashLength; // and reserve space of the "payload" hash.
            var payloadOffset = stream.Position;

            var serializer = new DataContractSerializer(
                typeof(AssemblyDependencyGraph),
                new DataContractSerializerSettings { PreserveObjectReferences = true });
            serializer.WriteObject(stream, this);

            var endOfStream = stream.Position;

            stream.Position = payloadOffset; // Position the stream in the first byte of the serialized data (i.e, skip *hash length* and *hash*
            var hash = hasher.ComputeHash(stream);
            stream.Position = hashOffset; // position the stream past the *hash length* (i.e, *hash-first byte*)
            stream.Write(hash);

            stream.Position = endOfStream;
        }

        public static AssemblyDependencyGraph LoadFrom(Stream stream)
        {
            ValidateMinimumLength(stream);

            Span<byte> signatureHashSpan = stackalloc byte[Signature.Length];
            var read = stream.Read(signatureHashSpan);
            Debug.Assert(read == signatureHashSpan.Length); // ValidateMinimumLength() guarantees there's enough data to fulfill the request.

            if (!signatureHashSpan.SequenceEqual(Signature))
            {
                throw new InvalidDataException($"""
                                                Invalid signature ({BitConverter.ToString(signatureHashSpan.ToArray())}) found (Expected: {BitConverter.ToString(Signature)}).{Environment.NewLine}
                                                DependencyGraph file is either corrupted or from a previous version of Unity. If you observe inexplicable runtime/compilation errors, please delete `Library/` folder and reopen the project.
                                                """);
            }

            int version = stream.ReadByte();
            if (version != Version)
            {
                throw new InvalidDataException($"""
                                                Unsupported DependencyGraph version ({version}).{Environment.NewLine}
                                                DependencyGraph file is either corrupted or from a newer version of Unity.
                                                """);
            }

            Span<byte> hashLengthSpan = stackalloc byte[sizeof(long)];
            read = stream.Read(hashLengthSpan);
            if (read != hashLengthSpan.Length)
                throw new InvalidDataException($"Read only {read} out of {hashLengthSpan.Length} bytes for hash length.");

            long hashLengthSize = BitConverter.ToInt64(hashLengthSpan);
            Debug.Assert(hashLengthSize < 256);
            Span<byte> storedHash = stackalloc byte[(int)hashLengthSize];
            read = stream.Read(storedHash);
            if (read != hashLengthSize)
                throw new InvalidDataException($"Read only {read} out of {storedHash.Length} bytes for hash.");

            var startOfSerializedData = stream.Position;
            var hasher = SHA256.Create();
            var computedHash = hasher.ComputeHash(stream);
            if (storedHash.Length != computedHash.Length)
            {
                return null;
            }

            if (!computedHash.AsSpan().SequenceEqual(storedHash))
                return null;

            stream.Position = startOfSerializedData;

            var serializer = new DataContractSerializer(
                typeof(AssemblyDependencyGraph),
                new DataContractSerializerSettings { PreserveObjectReferences = true });
            return (AssemblyDependencyGraph)serializer.ReadObject(stream);
        }

        static void ValidateMinimumLength(Stream stream)
        {
            var lengthOfDataExcludingPayload =
                                Signature.Length +
                                1 + // Version
                                sizeof(long) + // hash length
                                32; // actual SHA256 hash
            if (stream.Length < lengthOfDataExcludingPayload + 1)
            {
                throw new InvalidDataException($"Dependency graph data seems to be corrupted. Expected {lengthOfDataExcludingPayload} bytes but got {stream.Length} bytes.");
            }
        }

        internal DependencyEntry FindAssembly(string dependent)
        {
            return m_Graph.Find(e => e.m_Name == dependent);
        }

        [Serializable]
        [DataContract]
        internal class DependencyEntry : IComparable<DependencyEntry>
        {
            [DataMember]
            public string m_Name;
            [DataMember]
            public List<DependencyEntry> m_Dependencies;

            public DependencyEntry()
            {
            }

            public DependencyEntry(string root)
            {
                m_Name = root;
                m_Dependencies = new List<DependencyEntry>();
            }

            [DataMember]
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
                return $"{m_Name} ({m_Dependencies.Count})";
            }
        }

        [DataMember]
        List<DependencyEntry> m_Graph;

        [DataMember]
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
