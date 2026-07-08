// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Internal;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;
using UnityEngine;

namespace Unity.Loading
{
    enum LoadableSceneIdFlags : int
    {
        None = 0,
        FromBuiltContent = 1 << 0,
    }

    /// <summary>
    /// Stable serialized identifier for a Scene asset so it can be packed into content directory builds and loaded asynchronously at runtime.
    /// </summary>
    /// <remarks>
    /// This type can be used for a field on a ScriptableObject or MonoBehaviour to hold a "pointer" to a scene. When an object
    /// with a LoadableSceneId field is included in a ContentDirectory build, the referenced scene is also automatically included in the
    /// build.
    ///
    /// When authoring content in the Editor, use <see cref="LoadableSceneIdEditorUtility"/> to create LoadableSceneId objects and assign
    /// them to fields on ScriptableObject-derived classes.
    ///
    /// A LoadableSceneId is only supported in content built with <see cref="BuildPipeline.BuildContentDirectory"/>. If a
    /// LoadableSceneId is found in serialized data during a Player or AssetBundle build, the reference is set to null in the
    /// build output and an error is logged. Suppress this error with <see cref="BuildOptions.SuppressLoadableErrors"/> for Player
    /// builds or <see cref="BuildAssetBundleOptions.SuppressLoadableErrors"/> for AssetBundle builds.
    ///
    /// When a scripting object that has LoadableSceneId fields loads, it does not automatically load the referenced scenes. Instead,
    /// scripts can use <see cref="SceneManager.LoadSceneAsync(LoadableSceneId, LoadSceneParameters)"/> to load the referenced scene when needed.
    /// Similarly, scripts can use <see cref="SceneManager"/> APIs to unload scenes when no longer needed.
    ///
    /// In the Player, <see cref="SceneManager.LoadSceneAsync(LoadableSceneId, LoadSceneParameters)"/> loads the scene from built content.
    /// In Play mode it loads either the built scene or the live project scene, depending on where the LoadableSceneId came from.
    /// A LoadableSceneId reached from built content, for example through a root asset, loads the built scene, while one created with
    /// <see cref="LoadableSceneIdEditorUtility"/> loads the live project scene.
    /// </remarks>
    /// <example>
    /// <code source="../../../Modules/ContentBuild/Tests/local.test.build-examples/Editor/ContentLoad/LoadableSceneId_Example.cs"/>
    /// </example>
    [Serializable, StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Runtime/Export/SceneManager/LoadableSceneId.h")]
    [RequiredByNativeCode]
    public struct LoadableSceneId : IEquatable<LoadableSceneId>
    {
        internal GUID m_SceneGUID;

        private LoadableSceneIdFlags m_Flags;

        /// <summary>
        /// Construct a LoadableSceneId.  Typically this is called in the Editor, during content authoring.
        /// </summary>
        /// <param name="guid">AssetDatabase GUID of the Scene</param>
        /// <seealso cref="UnityEditor.AssetDatabase.AssetPathToGUID"/>
        [VisibleToOtherModules]
        internal LoadableSceneId(in GUID guid)
        {
            m_SceneGUID = guid;
        }

        /// <summary>
        /// True if this LoadableSceneId is initialized with valid data.
        /// </summary>
        public bool IsValid => !m_SceneGUID.Empty();

        [ExcludeFromDocs]
        public override string ToString()
        {
            return m_SceneGUID.ToString();
        }

        [ExcludeFromDocs]
        public override int GetHashCode()
        {
            return m_SceneGUID.GetHashCode();
        }

        [ExcludeFromDocs]
        public override bool Equals(System.Object other)
        {
            if (other is not LoadableSceneId otherId)
                return false;
            return m_SceneGUID == otherId.m_SceneGUID;
        }

        [ExcludeFromDocs]
        public bool Equals(LoadableSceneId other)
        {
            return other.m_SceneGUID.Equals(m_SceneGUID);
        }

        [ExcludeFromDocs]
        public static bool operator ==(LoadableSceneId left, LoadableSceneId right)
        {
            return left.m_SceneGUID == right.m_SceneGUID;
        }

        [ExcludeFromDocs]
        public static bool operator !=(LoadableSceneId left, LoadableSceneId right) { return !(left == right); }
    }
}
