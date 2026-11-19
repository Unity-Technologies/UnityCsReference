// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;

namespace Unity.Loading
{
    /// <summary>
    /// Describes one scene loaded from a content directory, as listed in <see cref="ContentLoadState.Scenes"/>.
    /// </summary>
    /// <remarks>
    /// A scene loaded with <see cref="SceneManager.LoadSceneAsync(LoadableSceneId, LoadSceneParameters)"/> appears from the
    /// call until the scene unloads, whether game code unloads it or a later load with <see cref="LoadSceneMode.Single"/>
    /// replaces it. The files that the scene depends on stay in memory for as long as it appears. The scene's own file is
    /// not listed in <see cref="ContentLoadState.Files"/>.
    /// </remarks>
    /// <seealso cref="ContentDirectoryDiagnostics.GetContentDirectoryLoadInfo(ContentDirectoryHandle, ContentDirectoryLoadInfo)"/>
    [StructLayout(LayoutKind.Sequential)]
    [RequiredByNativeCode]
    public readonly struct LoadedSceneInfo
    {
        // Field order must match native ContentLoad::LoadedSceneInfo.
        readonly LoadableSceneId m_Id;
        readonly Scene m_Scene;
        readonly LoadableStatus m_Status;
        readonly int m_DependencyStartIndex;
        readonly int m_DependencyCount;

        /// <summary>
        /// The id of the scene, equal to the one passed to <see cref="SceneManager.LoadSceneAsync(LoadableSceneId, LoadSceneParameters)"/>.
        /// </summary>
        public LoadableSceneId Id => m_Id;

        /// <summary>
        /// The loaded scene.
        /// </summary>
        /// <remarks>
        /// Pass it to <see cref="SceneManager.UnloadSceneAsync(Scene)"/> to unload the scene.
        /// </remarks>
        public Scene Scene => m_Scene;

        /// <summary>
        /// Whether the scene is loading, has loaded, or failed to load.
        /// </summary>
        /// <remarks>
        /// The status is <see cref="LoadableStatus.Loading"/> until the scene is integrated, including while
        /// <see cref="AsyncOperation.allowSceneActivation"/> holds it.
        /// </remarks>
        public LoadableStatus Status => m_Status;

        /// <summary>
        /// The index in <see cref="ContentLoadState.Dependencies"/> of the first file that the scene depends on.
        /// </summary>
        public int DependencyStartIndex => m_DependencyStartIndex;

        /// <summary>
        /// The number of entries in <see cref="ContentLoadState.Dependencies"/>, from <see cref="DependencyStartIndex"/>, that list
        /// the files the scene depends on.
        /// </summary>
        public int DependencyCount => m_DependencyCount;
    }
}
