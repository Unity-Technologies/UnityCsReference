// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;

#pragma warning disable CS1574 // XML comment with cref attribute to types in UnityEditor namespace

namespace Unity.Loading
{
    /// <summary>
    /// A snapshot of what is loaded from one content directory: the content files in memory, the loaded scenes, and the
    /// dependencies between them.
    /// </summary>
    /// <remarks>
    /// <see cref="ContentDirectoryDiagnostics.GetContentDirectoryLoadInfo(ContentDirectoryHandle, ContentDirectoryLoadInfo)"/>
    /// fills the state in <see cref="ContentDirectoryLoadInfo.LoadState"/>. The lists stay empty in release Players.
    ///
    /// The lists refer to each other by index. Each file and scene names a range of <see cref="Dependencies"/>, and each
    /// entry of that range is the index in <see cref="Files"/> of a file it depends on. An index is only valid in the state
    /// it comes from. To follow a file from one state to another, compare <see cref="LoadedContentFileInfo.Identity"/>.
    ///
    /// The lists do not change after the call, so a state can be kept after the content directory is unregistered or after
    /// Play mode ends. <see cref="LoadedSceneInfo.Scene"/> refers to a live scene, which can be unloaded after the call.
    ///
    /// To find the source assets of a file, look up <see cref="ContentFileIdentity.ToString"/> in the build's
    /// <c>ContentLayout.json</c> file, as <see cref="ContentDirectoryDiagnostics"/> describes. This also works for a state filled in a Player: send each
    /// <see cref="ContentFileIdentity.ToString"/> with the <see cref="ContentDirectoryLoadInfo.BuildManifestHash"/>.
    /// In the Editor, <see cref="UnityEditor.ContentLoadEditorUtility.TryGetSourceAssetGUID(ContentFileIdentity, out GUID)"/>
    /// gives the source asset directly.
    /// </remarks>
    /// <example>
    /// <code source="../../ContentBuild/Tests/local.test.build-examples/Editor/ContentLoad/ContentDirectoryDiagnostics_GetContentDirectoryLoadInfo.cs"/>
    /// </example>
    /// <seealso cref="ContentDirectoryDiagnostics.GetContentDirectoryLoadInfo(ContentDirectoryHandle, ContentDirectoryLoadInfo)"/>
    public sealed class ContentLoadState
    {
        internal ContentLoadState()
        {
        }

        /// <summary>
        /// The content files of the content directory that are in memory, including those loaded only as dependencies.
        /// </summary>
        /// <remarks>
        /// The files of scenes are not listed. <see cref="Scenes"/> lists the loaded scenes.
        /// </remarks>
        public List<LoadedContentFileInfo> Files { get; } = new List<LoadedContentFileInfo>();

        /// <summary>
        /// The scenes loaded from the content directory.
        /// </summary>
        public List<LoadedSceneInfo> Scenes { get; } = new List<LoadedSceneInfo>();

        /// <summary>
        /// Indices into <see cref="Files"/>, in one range for each file and each scene.
        /// </summary>
        /// <remarks>
        /// <see cref="LoadedContentFileInfo.DependencyStartIndex"/> and <see cref="LoadedContentFileInfo.DependencyCount"/> give the
        /// range of a file, and <see cref="LoadedSceneInfo.DependencyStartIndex"/> and <see cref="LoadedSceneInfo.DependencyCount"/>
        /// the range of a scene. A range lists only the files that the file or scene references directly. Follow the ranges of
        /// those files to find the rest.
        /// </remarks>
        public List<int> Dependencies { get; } = new List<int>();

        internal List<int> RootAssetReferences { get; } = new List<int>();
    }
}
