// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Loading;
using UnityEngine;

namespace UnityEditor
{
    /// <summary>
    /// Editor utilities that describe content loaded from content directories.
    /// </summary>
    /// <seealso cref="ContentDirectoryDiagnostics.GetContentDirectoryLoadInfo(ContentDirectoryHandle, ContentDirectoryLoadInfo)"/>
    /// <seealso cref="LoadableObjectIdEditorUtility"/>
    public static class ContentLoadEditorUtility
    {
        /// <summary>
        /// Gets the GUID of the asset that a content file was built from.
        /// </summary>
        /// <remarks>
        /// Use this to name the entries of <see cref="ContentLoadState.Files"/> of a state filled in the Editor. Use
        /// <see cref="AssetDatabase.GUIDToAssetPath(GUID)"/> to get the current path of the asset. The GUID stays the same when
        /// the asset moves. For a state filled in a Player, look up <see cref="ContentFileIdentity.ToString"/> in the build's
        /// <c>ContentLayout.json</c> file, as <see cref="ContentDirectoryDiagnostics"/> describes.
        /// </remarks>
        /// <param name="file">The identity of a content file, from <see cref="LoadedContentFileInfo.Identity"/>.</param>
        /// <param name="guid">The GUID of the asset, or an empty GUID when the method returns <c>false</c>.</param>
        /// <returns>
        /// <c>true</c> if the file was built from an asset. Returns <c>false</c> if it holds the scripts of the build, default
        /// resources or built-in resources, or if <paramref name="file"/> is not valid.
        /// </returns>
        /// <example>
        /// <code source="../../ContentBuild/Tests/local.test.build-examples/Editor/ContentLoad/ContentDirectoryDiagnostics_GetContentDirectoryLoadInfo.cs"/>
        /// </example>
        public static bool TryGetSourceAssetGUID(ContentFileIdentity file, out GUID guid)
        {
            return LoadableObjectIdEditorUtility.TryGetSourceAssetGUID(file.m_GUID, file.m_Kind, out guid);
        }
    }
}
