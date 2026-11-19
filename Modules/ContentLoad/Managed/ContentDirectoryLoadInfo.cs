// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;

#pragma warning disable CS1574 // XML comment with cref attribute to types in UnityEditor namespace

namespace Unity.Loading
{
    /// <summary>
    /// What is loaded from one content directory, which of it Unity holds for the directory's root assets, and the build
    /// that produced the directory.
    /// </summary>
    /// <remarks>
    /// Pass an instance to
    /// <see cref="ContentDirectoryDiagnostics.GetContentDirectoryLoadInfo(ContentDirectoryHandle, ContentDirectoryLoadInfo)"/>
    /// to fill it, and reuse the instance across calls. The lists stay empty in release Players.
    /// </remarks>
    /// <example>
    /// <code source="../../ContentBuild/Tests/local.test.build-examples/Editor/ContentLoad/ContentDirectoryDiagnostics_GetContentDirectoryLoadInfo.cs"/>
    /// </example>
    /// <seealso cref="ContentDirectoryDiagnostics.GetContentDirectoryLoadInfo(ContentDirectoryHandle, ContentDirectoryLoadInfo)"/>
    public sealed class ContentDirectoryLoadInfo
    {
        /// <summary>
        /// The files, scenes and dependencies loaded from the content directory.
        /// </summary>
        public ContentLoadState LoadState { get; } = new ContentLoadState();

        /// <summary>
        /// For each entry of <see cref="ContentLoadState.Files"/> in <see cref="LoadState"/>, how many of its
        /// <see cref="LoadedContentFileInfo.ReferenceCount"/> are holds that Unity keeps on root assets.
        /// </summary>
        /// <remarks>
        /// Unity loads the root assets of a content directory when it is registered, and holds them until it is
        /// unregistered. The holds that remain once these are subtracted come from load requests made by game code.
        /// </remarks>
        /// <seealso cref="ContentLoadManager.GetRootAssets(ContentDirectoryHandle)"/>
        public List<int> RootAssetReferences => LoadState.RootAssetReferences;

        /// <summary>
        /// Identifies the build that produced the content directory.
        /// </summary>
        /// <remarks>
        /// It is the hash in the content directory's <c>BuildManifestHash.txt</c> file, and the <c>BuildManifestHash</c>
        /// value of the build's <c>ContentLayout.json</c> file. In the Editor, pass it to
        /// <see cref="UnityEditor.Build.BuildHistory.TryGetBuildSummaryForManifestHash"/> to find the build in this
        /// project's build history. <see cref="ContentDirectoryDiagnostics"/> describes how to keep the layout of builds
        /// that the build history does not have.
        ///
        /// The hash is a default <see cref="Hash128"/> when the content directory is not registered.
        /// </remarks>
        /// <example>
        /// <code source="../../ContentBuild/Tests/local.test.build-examples/Editor/ContentLoad/ContentDirectoryLoadInfo_BuildManifestHash.cs"/>
        /// </example>
        public Hash128 BuildManifestHash { get; internal set; }
    }
}
