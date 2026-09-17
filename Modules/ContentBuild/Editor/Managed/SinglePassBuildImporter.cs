// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;
using UnityEngine;

namespace UnityEditor.Build.Content
{
    [ExcludeFromPreset]
    [NativeClass("SinglePassBuildImporter", PersistentTypeId = 0x1805dbbe)]
    internal sealed partial class SinglePassBuildImporter : AssetImporter
    {
    }

    /// <summary>
    /// Indicates the reason Unity is loading a scene: as part of a Player build, or when the Editor is in Play mode.
    /// </summary>
    /// <remarks>
    /// Unity passes this value in <see cref="SceneImportContext.loadingReason"/>, which <see cref="AssetPostprocessor.OnProcessScene"/> receives for every scene it processes.
    /// </remarks>
    /// <example>
    /// The following example only processes scenes that are part of a Player build.
    /// <code lang="cs"><![CDATA[
    /// using UnityEditor;
    /// using UnityEditor.Build.Content;
    /// using UnityEngine;
    /// using UnityEngine.SceneManagement;
    ///
    /// class MySceneProcessor : AssetPostprocessor
    /// {
    ///     public void OnProcessScene(Scene scene, SceneImportContext sceneContext)
    ///     {
    ///         if (sceneContext.loadingReason == ProcessSceneMode.PlayerBuild)
    ///             Debug.Log($"Processing {scene.name} for a Player build.");
    ///     }
    /// }
    /// ]]></code>
    /// </example>
    /// <seealso cref="SceneImportContext"/>
    /// <seealso cref="AssetPostprocessor.OnProcessScene"/>
    public enum ProcessSceneMode
    {
        /// <summary>
        /// The scene is being loaded as part of a Player build.
        /// </summary>
        PlayerBuild,
        /// <summary>
        /// The scene is being loaded while the Editor is in Play mode.
        /// </summary>
        PlayMode
    }

    /// <summary>
    /// Additional import context for scene assets used in <see cref="AssetPostprocessor.OnProcessScene"/>.
    /// </summary>
    /// <remarks>
    /// Unity passes an instance of this struct to <see cref="AssetPostprocessor.OnProcessScene"/> for every scene it processes during a Player build or in Play mode.
    /// </remarks>
    /// <example>
    /// The following example logs a message when a scene is processed while Awake has been called before the callback.
    /// <code lang="cs"><![CDATA[
    /// using UnityEditor;
    /// using UnityEditor.Build.Content;
    /// using UnityEngine;
    /// using UnityEngine.SceneManagement;
    ///
    /// class MySceneProcessor : AssetPostprocessor
    /// {
    ///     public void OnProcessScene(Scene scene, SceneImportContext sceneContext)
    ///     {
    ///         if (sceneContext.awakeDidRun)
    ///             Debug.Log($"Processing {scene.name} with Awake already called.");
    ///     }
    /// }
    /// ]]></code>
    /// </example>
    /// <seealso cref="AssetPostprocessor.OnProcessScene"/>
    /// <seealso cref="ProcessSceneMode"/>
    [StructLayout(LayoutKind.Sequential)]
    public struct SceneImportContext
    {
        /// <summary>
        /// Indicates whether Unity has already called Awake on the objects in the scene.
        /// </summary>
        /// <remarks>
        /// <see cref="MonoBehaviour.Awake"/> methods could have mutated the scene or moved instances to other loaded scenes,
        /// requiring some specific handling.
        /// </remarks>
        public bool awakeDidRun { get; private set; }
        /// <summary>
        /// Indicates the reason for which the scene is being loaded.
        /// </summary>
        public ProcessSceneMode loadingReason { get; private set; }
    }
}
