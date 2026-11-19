// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Internal;
using UnityEngine.Scripting;

#pragma warning disable CS1574 // XML comment with cref attribute to types in UnityEditor namespace

namespace Unity.Loading
{
    /// <summary>
    /// Identifies a content file of a content directory build, as listed in <see cref="LoadedContentFileInfo.Identity"/>.
    /// </summary>
    /// <remarks>
    /// A file has the same identity in every <see cref="ContentLoadState"/> of its content directory, so compare identities
    /// to follow a file from one state to the next. The same asset built into two content directories has the same
    /// identity in both.
    ///
    /// <see cref="ToString"/> returns the file's <c>StableId</c> entry in the <c>ContentLayout.json</c> file of the build.
    /// Use it to find the file in that layout, for example to read the source assets of a file listed in a Player. Compare
    /// the string, do not parse it: its format belongs to the layout of the Unity version that made the build.
    ///
    /// In the Editor, <see cref="UnityEditor.ContentLoadEditorUtility.TryGetSourceAssetGUID(ContentFileIdentity, out GUID)"/>
    /// gives the asset that the file was built from.
    /// </remarks>
    /// <example>
    /// <code source="../../ContentBuild/Tests/local.test.build-examples/Editor/ContentLoad/ContentDirectoryLoadInfo_BuildManifestHash.cs"/>
    /// </example>
    /// <seealso cref="ContentDirectoryLoadInfo.BuildManifestHash"/>
    [StructLayout(LayoutKind.Sequential)]
    [RequiredByNativeCode]
    public readonly struct ContentFileIdentity : IEquatable<ContentFileIdentity>
    {
        // Field order must match native BuiltContentFileId.
        [VisibleToOtherModules] internal readonly GUID m_GUID;
        readonly long m_LocalIdentifierInFile;
        [VisibleToOtherModules] internal readonly ContentFileSourceType m_Kind;

        /// <summary>
        /// True if this identity names a content file.
        /// </summary>
        /// <remarks>
        /// Every file listed in <see cref="ContentLoadState.Files"/> has a valid identity. A default identity is not valid.
        /// </remarks>
        public bool IsValid => m_Kind != ContentFileSourceType.None;

        [ExcludeFromDocs]
        public static bool operator ==(ContentFileIdentity x, ContentFileIdentity y)
        {
            return x.m_GUID == y.m_GUID && x.m_LocalIdentifierInFile == y.m_LocalIdentifierInFile && x.m_Kind == y.m_Kind;
        }

        [ExcludeFromDocs]
        public static bool operator !=(ContentFileIdentity x, ContentFileIdentity y)
        {
            return !(x == y);
        }

        [ExcludeFromDocs]
        public override bool Equals(object obj)
        {
            return obj is ContentFileIdentity other && this == other;
        }

        [ExcludeFromDocs]
        public bool Equals(ContentFileIdentity other)
        {
            return this == other;
        }

        [ExcludeFromDocs]
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = m_GUID.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)m_Kind;
                hashCode = (hashCode * 397) ^ m_LocalIdentifierInFile.GetHashCode();
                return hashCode;
            }
        }

        /// <summary>
        /// Returns the <c>StableId</c> that the <c>ContentLayout.json</c> file of the build lists for this content file.
        /// </summary>
        /// <returns>The file's entry in the build layout.</returns>
        public override string ToString()
        {
            // Must match native BuiltContentFileIdToString, which writes the layout's StableId
            return FormattableString.Invariant($"cfid-{m_GUID}-{m_LocalIdentifierInFile}-{(int)m_Kind}");
        }
    }
}
