// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.Profiling.Editor.UI
{
    internal class CaptureFileModel : IEquatable<CaptureFileModel>
    {
        public CaptureFileModel(
            string name,
            string fullPath,
            string productName,
            string metaDataDescription,
            uint sessionId,
            DateTime timestamp,
            RuntimePlatform platform,
            bool editorPlatform,
            string unityVersion)
        {
            Name = name;
            FullPath = fullPath;
            ProductName = productName;
            MetadataDescription = metaDataDescription;
            SessionId = sessionId;
            Timestamp = timestamp;
            Platform = platform;
            EditorPlatform = editorPlatform;
            UnityVersion = unityVersion;
        }

        public string Name { get; }
        public string FullPath { get; }
        public string ProductName { get; }
        public string MetadataDescription { get; }
        public uint SessionId { get; }
        public DateTime Timestamp { get; }
        public RuntimePlatform Platform { get; }
        public bool EditorPlatform { get; }
        public string UnityVersion { get; }

        public bool Equals(CaptureFileModel other)
        {
            return other != null &&
                Name == other.Name &&
                FullPath == other.FullPath &&
                ProductName == other.ProductName &&
                MetadataDescription == other.MetadataDescription &&
                SessionId == other.SessionId &&
                Timestamp == other.Timestamp &&
                Platform == other.Platform &&
                EditorPlatform == other.EditorPlatform &&
                UnityVersion == other.UnityVersion;
        }
    }
}
