// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

#pragma warning disable CS1574 // XML comment with cref attribute to types in UnityEditor namespace

namespace Unity.Loading
{
    /// <summary>
    /// Describes what is loaded from a content directory, including the holds that Unity keeps on its root assets.
    /// </summary>
    /// <remarks>
    /// To name the files that a state lists, use the <c>ContentLayout.json</c> file of the build that produced the content
    /// directory. Its entry whose <c>StableId</c> equals <see cref="ContentFileIdentity.ToString"/> lists the source assets
    /// of the file. The layout is in the build's report directory, which <see cref="UnityEditor.Build.BuildHistory"/> keeps
    /// for builds made in this project. Find the build with <see cref="ContentDirectoryLoadInfo.BuildManifestHash"/>.
    ///
    /// The build history deletes its oldest builds once it holds
    /// <see cref="UnityEditor.Build.BuildHistory.BuildHistoryLimit"/> builds, and has no builds made on other machines, such
    /// as a build server. To diagnose those builds, keep the build report directory with the build output.
    /// </remarks>
    public static class ContentDirectoryDiagnostics
    {
        /// <summary>
        /// Fills a <see cref="ContentDirectoryLoadInfo"/> for one content directory.
        /// </summary>
        /// <remarks>
        /// The info lists the content files of the directory that are in memory, the scenes loaded from it, and the
        /// dependencies between them, all taken at the same moment. The entries of each list are in no particular order.
        ///
        /// Use this to find Loadables that were loaded and not released. The state reports content files, not Loadables: a
        /// loaded <see cref="Loadable{T}"/> holds the content file of its object, so a Loadable that was not released shows
        /// as a hold on that file. Fill one info before a level loads and another after it unloads. A file whose
        /// <see cref="LoadedContentFileInfo.ReferenceCount"/>, minus its <see cref="ContentDirectoryLoadInfo.RootAssetReferences"/>
        /// entry, grew in between is held by a Loadable that the level loaded and did not release.
        ///
        /// Only content loaded from the content directory is listed. In Editor Play mode, Loadables that the
        /// <see cref="UnityEditor.AssetDatabase"/> serves are not listed. To check them, load the content from a content
        /// directory build. To describe every registered directory, call this method for each handle that
        /// <see cref="ContentLoadManager.GetContentDirectories"/> returns.
        ///
        /// The lists are filled in the Editor and in development Players. In release Players they stay empty. Use
        /// <see cref="Debug.isDebugBuild"/> to tell an empty info in a release Player from an info with nothing loaded. When
        /// <paramref name="directory"/> is not registered, the lists stay empty and
        /// <see cref="ContentDirectoryLoadInfo.BuildManifestHash"/> is a default <see cref="Hash128"/>.
        ///
        /// This method clears the lists of <paramref name="info"/> before it fills them, and only allocates managed memory
        /// when a list needs more capacity. Call it from the main thread.
        /// </remarks>
        /// <param name="directory">The content directory to describe.</param>
        /// <param name="info">The information to fill. Reuse one instance across calls.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="info"/> is null.</exception>
        /// <example>
        /// <code source="../../ContentBuild/Tests/local.test.build-examples/Editor/ContentLoad/ContentDirectoryDiagnostics_GetContentDirectoryLoadInfo.cs"/>
        /// </example>
        /// <seealso cref="ContentLoadState"/>
        /// <seealso cref="Loadable{T}.Release"/>
        public static void GetContentDirectoryLoadInfo(ContentDirectoryHandle directory, ContentDirectoryLoadInfo info)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));

            var state = info.LoadState;
            ContentLoadManager.GetLoadState(directory, state.Files, state.Scenes, state.Dependencies, state.RootAssetReferences);
            ContentLoadManager.TryGetBuildManifestHash(directory, out Hash128 buildManifestHash);
            info.BuildManifestHash = buildManifestHash;
        }
    }
}
