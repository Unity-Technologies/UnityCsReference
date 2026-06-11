// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Build.Analysis
{
    /// <summary>
    /// Computes <see cref="RootAssetStats"/> for every root asset in a parsed
    /// <see cref="ContentLayout"/>. The Direct stats include only what is loaded
    /// immediately with the root; the Total stats also follow loadable references.
    /// </summary>
    internal static class RootAssetStatsCalculator
    {
        public static RootAssetStats[] Calculate(ContentLayout layout)
        {
            if (layout == null)
                throw new ArgumentNullException(nameof(layout));
            try
            {
                return new Context(layout).Calculate();
            }
            catch (Exception e)
            {
                // Malformed ContentLayout (e.g. missing fields after a JSON schema change)
                // shouldn't take down callers. Match the BuildAnalysisService pattern.
                Debug.LogError($"{BuildAnalysisConstants.k_ConsoleLogPrefix} Failed to calculate root asset stats: {e.Message}");
                return Array.Empty<RootAssetStats>();
            }
        }

        private struct Stats
        {
            public int AssetCount;
            public ulong SizeBytes;
        }

        private sealed class Context
        {
            readonly SerializedFileLayout[] m_SerializedFiles;
            readonly BinaryArtifact[] m_BinaryArtifacts;
            readonly string[] m_RootHashes;
            readonly Dictionary<string, int> m_ObjectIdHashToSf;
            readonly Dictionary<string, string> m_ObjectIdHashToAssetPath; // For determining path of RootAsset
            readonly Dictionary<string, int> m_ScenePathToSf;
            readonly Dictionary<string, int> m_ContentHashToBinary;

            // Reused across every Traverse() call (2 per root) to avoid per-traversal allocations.
            readonly HashSet<int> m_VisitedSf = new();
            readonly HashSet<int> m_VisitedBinary = new();
            readonly HashSet<string> m_UniqueSources = new(StringComparer.Ordinal);
            readonly Queue<int> m_Queue = new();

            public Context(ContentLayout layout)
            {
                m_SerializedFiles = layout.SerializedFiles ?? Array.Empty<SerializedFileLayout>();
                m_BinaryArtifacts = layout.BinaryArtifacts ?? Array.Empty<BinaryArtifact>();
                m_RootHashes = layout.RootAssets ?? Array.Empty<string>();
                var loadableObjects = layout.LoadableObjectIds ?? Array.Empty<LoadableObjectIdLayout>();
                var loadableScenes = layout.LoadableSceneIds ?? Array.Empty<LoadableSceneIdLayout>();

                m_ObjectIdHashToSf = new Dictionary<string, int>(loadableObjects.Length, StringComparer.Ordinal);
                m_ObjectIdHashToAssetPath = new Dictionary<string, string>(loadableObjects.Length, StringComparer.Ordinal);
                foreach (var obj in loadableObjects)
                {
                    m_ObjectIdHashToAssetPath[obj.ObjectIdHash] = obj.AssetPath;
                    if (obj.SerializedFile >= 0)
                        m_ObjectIdHashToSf[obj.ObjectIdHash] = obj.SerializedFile;
                }

                m_ScenePathToSf = new Dictionary<string, int>(loadableScenes.Length, StringComparer.Ordinal);
                foreach (var scene in loadableScenes)
                {
                    if (scene.SerializedFile >= 0)
                        m_ScenePathToSf[scene.Path] = scene.SerializedFile;
                }

                m_ContentHashToBinary = new Dictionary<string, int>(m_BinaryArtifacts.Length, StringComparer.Ordinal);
                for (int i = 0; i < m_BinaryArtifacts.Length; i++)
                {
                    var binary = m_BinaryArtifacts[i];
                    if (binary.Category == BuildArtifactCategory.ContentFile)
                        m_ContentHashToBinary[binary.ContentHash] = i;
                }
            }

            public RootAssetStats[] Calculate()
            {
                var results = new List<RootAssetStats>(m_RootHashes.Length);
                foreach (var rootHash in m_RootHashes)
                {
                    if (!m_ObjectIdHashToSf.TryGetValue(rootHash, out var rootSfIndex))
                        continue;
                    if (m_SerializedFiles[rootSfIndex].IsBuiltIn)
                        continue;

                    m_ObjectIdHashToAssetPath.TryGetValue(rootHash, out var assetPath);

                    var direct = Traverse(rootSfIndex, includeLoadables: false);
                    var total = Traverse(rootSfIndex, includeLoadables: true);

                    results.Add(new RootAssetStats
                    {
                        AssetPath = assetPath ?? string.Empty,
                        DirectAssets = direct.AssetCount,
                        DirectSize = direct.SizeBytes,
                        TotalAssets = total.AssetCount,
                        TotalSize = total.SizeBytes,
                    });
                }
                return results.ToArray();
            }

            Stats Traverse(int startSfIndex, bool includeLoadables)
            {
                m_VisitedSf.Clear();
                m_VisitedBinary.Clear();
                m_UniqueSources.Clear();
                m_Queue.Clear();

                ulong sizeBytes = 0;
                TryEnqueue(startSfIndex);

                while (m_Queue.Count > 0)
                {
                    int sfIndex = m_Queue.Dequeue();
                    var sf = m_SerializedFiles[sfIndex];

                    // MonoScript SerializedFiles list every script in the project as SourceAssets
                    // (one cluster per build), which would swamp the asset count for every root.
                    // Match the BuildReport inspector and skip .cs entries.
                    foreach (var src in sf.SourceAssets)
                    {
                        if (!src.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                            m_UniqueSources.Add(src);
                    }

                    if (m_ContentHashToBinary.TryGetValue(sf.ContentHash, out var contentBinaryIndex))
                        // Start with the BinaryArtifact representing the SerializedFile
                        AddBinaryRecursive(contentBinaryIndex, ref sizeBytes);

                    foreach (var depIndex in sf.SerializedFileDependencies)
                        TryEnqueue(depIndex);

                    if (!includeLoadables)
                        continue;

                    foreach (var hash in sf.LoadableDependencies)
                    {
                        if (m_ObjectIdHashToSf.TryGetValue(hash, out var loadSf))
                            TryEnqueue(loadSf);
                    }

                    foreach (var path in sf.LoadableSceneDependencies)
                    {
                        if (m_ScenePathToSf.TryGetValue(path, out var sceneSf))
                            TryEnqueue(sceneSf);
                    }
                }

                return new Stats { AssetCount = m_UniqueSources.Count, SizeBytes = sizeBytes };
            }

            void TryEnqueue(int sfIndex)
            {
                if (!IsValidSf(sfIndex) || !m_VisitedSf.Add(sfIndex))
                    return;
                if (m_SerializedFiles[sfIndex].IsBuiltIn)
                    return;
                m_Queue.Enqueue(sfIndex);
            }

            bool IsValidSf(int index) => index >= 0 && index < m_SerializedFiles.Length;

            // Visit an entry in the BinaryArtifacts and all its referenced dependencies, accumulating stats.
            void AddBinaryRecursive(int binaryIndex, ref ulong sizeBytes)
            {
                if (binaryIndex < 0 || binaryIndex >= m_BinaryArtifacts.Length)
                    return;
                if (!m_VisitedBinary.Add(binaryIndex))
                    // Multiple SerializedFiles can reference the same BinaryArtifact,
                    // e.g. identical texture data.
                    return;

                var binary = m_BinaryArtifacts[binaryIndex];
                sizeBytes += binary.Size;

                if (binary.ArtifactReferences != null)
                {
                    foreach (var refIndex in binary.ArtifactReferences)
                        AddBinaryRecursive(refIndex, ref sizeBytes);
                }
            }
        }
    }
}
