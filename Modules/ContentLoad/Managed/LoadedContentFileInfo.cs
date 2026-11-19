// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;
using UnityEngine.Scripting;

namespace Unity.Loading
{
    /// <summary>
    /// Describes one content file that is in memory, as listed in <see cref="ContentLoadState.Files"/>.
    /// </summary>
    /// <remarks>
    /// The list includes files that no load request holds. A file whose <see cref="ReferenceCount"/> is 0 is in memory
    /// because a listed file or scene depends on it, or because it is being released.
    /// </remarks>
    /// <seealso cref="ContentDirectoryDiagnostics.GetContentDirectoryLoadInfo(ContentDirectoryHandle, ContentDirectoryLoadInfo)"/>
    [StructLayout(LayoutKind.Sequential)]
    [RequiredByNativeCode]
    public readonly struct LoadedContentFileInfo
    {
        // Field order must match native ContentLoad::LoadedContentFileInfo.
        readonly ContentFileIdentity m_Identity;
        readonly int m_ReferenceCount;
        readonly LoadableStatus m_Status;
        readonly int m_DependencyStartIndex;
        readonly int m_DependencyCount;

        /// <summary>
        /// Identifies the content file.
        /// </summary>
        public ContentFileIdentity Identity => m_Identity;

        /// <summary>
        /// The number of load requests that hold the file.
        /// </summary>
        /// <remarks>
        /// Each <see cref="Loadable{T}"/> that is loaded and not released holds the file of its object, and Unity holds the
        /// file of each root asset while the content directory is registered. Files that depend on this file are not
        /// counted. <see cref="ContentLoadState.Dependencies"/> lists the dependencies of each file. A file whose count stays
        /// above its <see cref="ContentDirectoryLoadInfo.RootAssetReferences"/> entry after the content that loaded it is gone
        /// is held by a <see cref="Loadable{T}"/> that was not released.
        /// </remarks>
        /// <seealso cref="ContentDirectoryLoadInfo.RootAssetReferences"/>
        public int ReferenceCount => m_ReferenceCount;

        /// <summary>
        /// Whether the file is loading, has loaded, or failed to load.
        /// </summary>
        public LoadableStatus Status => m_Status;

        /// <summary>
        /// The index in <see cref="ContentLoadState.Dependencies"/> of the first file that this file depends on.
        /// </summary>
        public int DependencyStartIndex => m_DependencyStartIndex;

        /// <summary>
        /// The number of entries in <see cref="ContentLoadState.Dependencies"/>, from <see cref="DependencyStartIndex"/>, that list
        /// the files this file depends on.
        /// </summary>
        /// <remarks>
        /// Dependencies on built-in resources are left out, because they do not load from a content directory.
        /// </remarks>
        public int DependencyCount => m_DependencyCount;
    }
}
