// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Text;
using Unity.Profiling;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace UnityEditor.Build.Analysis
{
    internal interface IDependencyGraphStore
    {
        void Write(string path, DependencyGraph graph);

        /// <summary>
        /// Removes a previously written graph, if one exists. Called when a generation produced no
        /// graph, so a leftover from an earlier generation is not served beside the fresh analysis.
        /// </summary>
        void Delete(string path);

        /// <summary>
        /// Reads a graph, or returns false if there isn't one this build can use. Takes the expected
        /// manifest hash rather than returning it, so no call site can forget to check that the graph
        /// and the analysis describe the same build.
        /// </summary>
        bool TryRead(string path, string expectedBuildManifestHash, out DependencyGraph graph);
    }

    /// <summary>
    /// Reads and writes cache/DependencyGraph.bin, sibling of cache/BuildAnalysis.json.
    ///
    /// Binary rather than JSON, which is a deliberate exception to the readable-artifact principle:
    /// as JSON this would be roughly an order of magnitude larger and would reintroduce the parse
    /// cost the artifact exists to avoid.
    ///
    /// Layout, little-endian throughout:
    ///   magic          4 bytes   'B','A','D','G'
    ///   Version        int32     BuildAnalysisConstants.k_SchemaVersion
    ///   LayoutVersion  int32     ContentLayout.Version the graph was built from
    ///   ManifestHash   string    length-prefixed UTF8
    ///   NodeCount      int32
    ///   EdgeCount      int32
    ///   offsets        int32  x (NodeCount + 1)
    ///   targets        int32  x EdgeCount
    ///   edgeBytes      uint32 x EdgeCount
    ///   loadable       uint32 x ceil(NodeCount / 32)
    ///   attributed     uint32 x ceil(NodeCount / 32)
    /// </summary>
    internal sealed class DependencyGraphStore : IDependencyGraphStore
    {
        static readonly ProfilerMarker s_WriteMarker = new ProfilerMarker("DependencyGraphStore.Write");
        static readonly ProfilerMarker s_ReadMarker = new ProfilerMarker("DependencyGraphStore.Read");

        [NoAutoStaticsCleanup] // magic constant, immutable in practice; resetting to null would break every read
        static readonly byte[] k_Magic = { (byte)'B', (byte)'A', (byte)'D', (byte)'G' };

        public void Write(string path, DependencyGraph graph)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Path is required.", nameof(path));
            if (graph == null)
                throw new ArgumentNullException(nameof(graph));

            using (s_WriteMarker.Auto())
            {
                var directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);

                using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write))
                using (var writer = new BinaryWriter(stream, Encoding.UTF8))
                {
                    writer.Write(k_Magic);
                    writer.Write(BuildAnalysisConstants.k_SchemaVersion);
                    writer.Write(graph.ContentLayoutVersion);
                    writer.Write(graph.BuildManifestHash ?? string.Empty);
                    writer.Write(graph.NodeCount);
                    writer.Write(graph.EdgeCount);

                    WriteBlock(writer, graph.Offsets);
                    WriteBlock(writer, graph.Targets);
                    WriteBlock(writer, graph.EdgeBytes);
                    WriteBlock(writer, graph.Loadable);
                    WriteBlock(writer, graph.Attributed);
                }
            }
        }

        public void Delete(string path)
        {
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
                File.Delete(path);
        }

        public bool TryRead(string path, string expectedBuildManifestHash, out DependencyGraph graph)
        {
            graph = null;

            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return false;

            using (s_ReadMarker.Auto())
            {
                try
                {
                    graph = Read(path, expectedBuildManifestHash ?? string.Empty);
                }
                catch (Exception e) when (e is IOException || e is UnauthorizedAccessException
                                          || e is ArgumentException || e is FormatException)
                {
                    // Includes the torn read: the analysis is handed to the UI before the fire-and-forget
                    // write finishes, so a click in that window can catch the file mid-write.
                    Reject(path, e.Message);
                    return false;
                }

                return graph != null;
            }
        }

        DependencyGraph Read(string path, string expectedBuildManifestHash)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            using (var reader = new BinaryReader(stream, Encoding.UTF8))
            {
                if (!MagicMatches(reader.ReadBytes(k_Magic.Length)))
                    return Reject(path, "not a dependency graph file");

                var version = reader.ReadInt32();
                if (version != BuildAnalysisConstants.k_SchemaVersion)
                    return Reject(path, $"written by schema version {version}, expected {BuildAnalysisConstants.k_SchemaVersion}");

                var contentLayoutVersion = reader.ReadInt32();

                // Node ids are positional Assets-table ids, so a graph is only ever valid against the
                // analysis generated with it. Without this the two could quietly disagree about a build.
                var manifestHash = reader.ReadString();
                if (!string.Equals(manifestHash, expectedBuildManifestHash, StringComparison.Ordinal))
                    return Reject(path, "built for a different build manifest");

                var nodeCount = reader.ReadInt32();
                var edgeCount = reader.ReadInt32();
                if (nodeCount < 0 || edgeCount < 0)
                    return Reject(path, "negative node or edge count");

                var bitWords = DependencyGraph.BitWordCount(nodeCount);
                var expectedLength = stream.Position
                                     + (long)(nodeCount + 1) * sizeof(int)
                                     + (long)edgeCount * sizeof(int)
                                     + (long)edgeCount * sizeof(uint)
                                     + (long)bitWords * sizeof(uint) * 2;
                if (stream.Length != expectedLength)
                    return Reject(path, $"is {stream.Length} bytes, expected {expectedLength}");

                var offsets = ReadInts(reader, nodeCount + 1);
                var targets = ReadInts(reader, edgeCount);
                var edgeBytes = ReadUInts(reader, edgeCount);
                var loadable = ReadUInts(reader, bitWords);
                var attributed = ReadUInts(reader, bitWords);

                if (offsets[0] != 0 || offsets[nodeCount] != edgeCount)
                    return Reject(path, "row offsets don't span the edges");

                return new DependencyGraph(offsets, targets, edgeBytes, loadable, attributed, contentLayoutVersion, manifestHash);
            }
        }

        static DependencyGraph Reject(string path, string reason)
        {
            Debug.LogWarning($"{BuildAnalysisConstants.k_ConsoleLogPrefix} Ignoring '{path}': {reason}.");
            return null;
        }

        static bool MagicMatches(byte[] candidate)
        {
            if (candidate == null || candidate.Length != k_Magic.Length)
                return false;
            for (var i = 0; i < k_Magic.Length; i++)
            {
                if (candidate[i] != k_Magic[i])
                    return false;
            }
            return true;
        }

        static void WriteBlock(BinaryWriter writer, Array values)
        {
            var byteCount = Buffer.ByteLength(values);
            if (byteCount == 0)
                return;
            var bytes = new byte[byteCount];
            Buffer.BlockCopy(values, 0, bytes, 0, byteCount);
            writer.Write(bytes);
        }

        static int[] ReadInts(BinaryReader reader, int count)
        {
            var values = new int[count];
            ReadBlock(reader, values, count * sizeof(int));
            return values;
        }

        static uint[] ReadUInts(BinaryReader reader, int count)
        {
            var values = new uint[count];
            ReadBlock(reader, values, count * sizeof(uint));
            return values;
        }

        static void ReadBlock(BinaryReader reader, Array destination, int byteCount)
        {
            if (byteCount == 0)
                return;
            var bytes = reader.ReadBytes(byteCount);
            if (bytes.Length != byteCount)
                throw new EndOfStreamException($"Expected {byteCount} bytes, read {bytes.Length}.");
            Buffer.BlockCopy(bytes, 0, destination, 0, byteCount);
        }
    }
}
