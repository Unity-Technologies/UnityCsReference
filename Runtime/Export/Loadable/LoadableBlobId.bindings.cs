// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Internal;
using UnityEngine.Scripting;

namespace Unity.Loading
{
    [VisibleToOtherModules]
    enum LoadableBlobIdFlags : uint
    {
        None = 0,
        FromBuiltContent = 1 << 0,
    }

    /// <summary>
    /// Content-addressed reference to an opaque binary blob shipped as a first-class artifact in a
    /// ContentDirectory build. The hash is the content hash of the bytes; size is carried so a
    /// runtime reader can size an asynchronous read without a manifest lookup.
    /// </summary>
    /// <remarks>
    /// Importers register bytes via <c>UnityEditor.Build.Content.LoadableBlobIdEditorUtility</c> and store
    /// the returned LoadableBlobId on a serialized object (typically a ScriptableObject field). When that
    /// object is included in a <see cref="BuildPipeline.BuildContentDirectory"/> build, the referenced
    /// blob is pulled in as a raw artifact addressable by content hash.
    ///
    /// Use <see cref="LoadableBlobIdUtility.GetVFSPath"/> to obtain a virtual filesystem path
    /// (<c>cah:/&lt;hash&gt;</c> when the id was deserialized from a built ContentDirectory,
    /// <c>uds:/&lt;hash&gt;</c> when it came from the AssetDatabase) suitable for passing to
    /// <c>AsyncReadManager.Read</c>.
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    [NativeHeader("Runtime/BaseClasses/LoadableBlobId.h")]
    [UsedByNativeCode]
    [VisibleToOtherModules]
    internal struct LoadableBlobId : IEquatable<LoadableBlobId>
    {
        // Layout must stay in lockstep with the native struct in LoadableBlobId.h, including the
        // editor-only fields: the struct is blitted between managed and native.
        [VisibleToOtherModules] internal Hash128 m_Hash;
        [VisibleToOtherModules] internal ulong m_Size;
        // Hash of the BuildArtifactMetadata created when the blob was registered. Editor/build-side
        // handle only: zero when the id was deserialized from built content.
        [VisibleToOtherModules] internal Hash128 m_MetadataId;
        // Transient: set by native serialization on read; not persisted on disk. Drives whether
        // GetVFSPath returns cah:/ or uds:/. Mirrors LoadableSceneId.m_Flags.
        [VisibleToOtherModules] internal LoadableBlobIdFlags m_Flags;

        [VisibleToOtherModules]
        internal LoadableBlobId(Hash128 hash, ulong size, Hash128 metadataId)
        {
            m_Hash = hash;
            m_Size = size;
            m_MetadataId = metadataId;
            m_Flags = LoadableBlobIdFlags.None;
        }

        /// <summary>The content hash of the blob's bytes.</summary>
        public Hash128 Hash => m_Hash;

        /// <summary>The size of the blob in bytes.</summary>
        public ulong Size => m_Size;

        /// <summary>True if this LoadableBlobId is initialized with a valid content hash.</summary>
        public bool IsValid => m_Hash.isValid;

        [ExcludeFromDocs]
        public static bool operator ==(LoadableBlobId x, LoadableBlobId y)
        {
            return x.m_Hash == y.m_Hash && x.m_Size == y.m_Size && x.m_MetadataId == y.m_MetadataId && x.m_Flags == y.m_Flags;
        }

        [ExcludeFromDocs]
        public static bool operator !=(LoadableBlobId x, LoadableBlobId y) => !(x == y);

        [ExcludeFromDocs]
        public override bool Equals(object obj) => obj is LoadableBlobId other && this == other;

        [ExcludeFromDocs]
        public bool Equals(LoadableBlobId other) => this == other;

        [ExcludeFromDocs]
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = m_Hash.GetHashCode();
                hash = (hash * 397) ^ m_Size.GetHashCode();
                hash = (hash * 397) ^ m_MetadataId.GetHashCode();
                hash = (hash * 397) ^ (int)m_Flags;
                return hash;
            }
        }

        /// <summary>Returns a string representation for debugging.</summary>
        public override string ToString()
        {
            if (!IsValid)
                return "{ Invalid }";
            return $"{{ blob hash: {m_Hash}, size: {m_Size}, metadata: {m_MetadataId} }}";
        }
    }
}
