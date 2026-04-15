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
    /// Reference to a Scene.
    /// </summary>
    /// <remarks>
    /// This type can be used for a field on a ScriptableObject or MonoBehaviour to hold a "pointer" to a scene. When an object
    /// with a LoadableSceneId field is included in a ContentDirectory build then the referenced scene will also automatically be included in the
    /// build.
    ///
    /// When authoring content in the Editor, use <see cref="LoadableSceneIdEditorUtility"/>  to create LoadableSceneId objects and assign
    /// them to fields on ScriptableObject-derived classes.
    ///
    /// Player and AssetBundle builds do not pull scenes referenced by LoadableSceneId into the build, only
    /// the scenes listed in <see cref="EditorBuildSettings.scenes"/> are included.  But a LoadableSceneId field in any built content can be
    /// used to load the scene at runtime if it is available inside a registered ContentDirectory.
    ///
    /// When a scripting object that has LoadableSceneId fields loads it does not automatically load the referenced scenes. Instead,
    /// scripts can use <see cref="SceneManager.LoadSceneAsync(LoadableSceneId, LoadSceneParameters)"/> to load the referenced scene at the point
    /// when the scene is needed in memory. The loading capability is available in both Editor play mode and in the Player.
    /// Similarly, scripts can use <see cref="SceneManager"/> APIs to unload the scenes when it is no longer needed.
    /// </remarks>
    [Serializable, StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Runtime/Export/SceneManager/LoadableSceneId.h")]
    [RequiredByNativeCode]
    /*UCBP-REMOVE*/[VisibleToOtherModules]
    /*UCBP-PUBLIC*/ internal struct LoadableSceneId : IEquatable<LoadableSceneId>
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

        public bool isValid => !m_SceneGUID.Empty();

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
