// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace UnityEditor.Build.Content
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct BuildArtifactMetadataId : IEquatable<BuildArtifactMetadataId>
    {
        public Hash128 hash;

        public BuildArtifactMetadataId(Hash128 hash)
        {
            this.hash = hash;
        }

        public bool Equals(BuildArtifactMetadataId other) => hash.Equals(other.hash);
        public override bool Equals(object obj) => obj is BuildArtifactMetadataId other && Equals(other);
        public override int GetHashCode() => hash.GetHashCode();
        public override string ToString() => hash.ToString();

        public static bool operator ==(BuildArtifactMetadataId a, BuildArtifactMetadataId b) => a.Equals(b);
        public static bool operator !=(BuildArtifactMetadataId a, BuildArtifactMetadataId b) => !a.Equals(b);
    }
}
