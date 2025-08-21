// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using Unity.Profiling;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Editor
{
    internal static class HashUtils
    {
        private const int ChunkSize = 8192; // 8KB

        private static readonly ProfilerMarker s_ComputeForFiles = new("HashUtils.ComputeForFiles");

        public static Hash128 ComputeForFiles(params string[] files)
        {
            using var _ = s_ComputeForFiles.Auto();

            Array.Sort(files, StringComparer.Ordinal);

            var hash = new Hash128();
            var chunk = new byte[ChunkSize];

            foreach (var file in files)
            {
                using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, ChunkSize, FileOptions.SequentialScan);

                int bytesRead;
                while ((bytesRead = stream.Read(chunk, 0, ChunkSize)) > 0)
                {
                    hash.Append(chunk, 0, bytesRead);
                }
            }

            return hash;
        }
    }
}
