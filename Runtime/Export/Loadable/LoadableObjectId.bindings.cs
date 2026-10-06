// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Internal;
using UnityEngine.Scripting;

namespace Unity.Loading
{
    // Mirror of native ContentFileSourceType, documented in Runtime/BaseClasses/BuiltContentFileId.h.
    [UsedByNativeCode]
    [VisibleToOtherModules]
    internal enum ContentFileSourceType : byte
    {
        None = 0,
        ObjectCluster = 1,
        SingleImportedObject = 2,
        SingleSourceObject = 3,
        MonoScript = 4,
        DefaultResources = 5,
    }

    /// <summary>
    /// A low-level reference to an object within an asset, used to pull assets into a content directory build, and for on-demand
    /// loading.
    /// </summary>
    /// <remarks>
    /// LoadableObjectId is the underlying reference type used by <see cref="Loadable{T}"/>. It contains the information needed
    /// to identify and load a specific object from built content.
    ///
    /// In the Editor, use <see cref="UnityEditor.LoadableObjectIdEditorUtility"/> to create LoadableObjectIds. Typically these
    /// are serialized as part of <see cref="Loadable{T}"/> fields on classes derived from <see cref="ScriptableObject"/> or
    /// <see cref="MonoBehaviour"/>. You can also use LoadableObjectId directly as a field type in a ScriptableObject or MonoBehaviour,
    /// and then create <see cref="Loadable{T}"/> objects on the fly as needed.
    ///
    /// Using LoadableObjectId directly suits a root asset that acts as a library of assets shared by several components. Because
    /// LoadableObjectId has no load or release methods of its own, it makes clear that each caller constructs its own
    /// <see cref="Loadable{T}"/> and owns the lifetime of the object it loads.
    ///
    /// LoadableObjectId has no generic type parameter, so it can reference any <see cref="Object"/>. Add your own validation if a
    /// field must only accept a specific type.
    ///
    /// When those objects are built as part of a content directory, the assets referenced by the
    /// LoadableObjectId are recursively pulled into the build output. At runtime, the <see cref="ContentLoadManager"/> resolves
    /// LoadableObjectIds to the correct built content as long as it is part of the currently registered content directories.
    ///
    /// A LoadableObjectId is only supported in content built with <see cref="BuildPipeline.BuildContentDirectory"/>. If a
    /// LoadableObjectId is found in serialized data during a Player or AssetBundle build, the reference is set to null in the
    /// build output and an error is logged. Suppress this error with <see cref="BuildOptions.SuppressLoadableErrors"/> for Player
    /// builds or <see cref="BuildAssetBundleOptions.SuppressLoadableErrors"/> for AssetBundle builds.
    /// </remarks>
    /// <example>
    /// <code source="../../../Modules/ContentBuild/Tests/local.test.build-examples/Editor/ContentLoad/LoadableObjectId_Example.cs"/>
    /// </example>
    /// <seealso cref="Loadable{T}"/>
    /// <seealso cref="UnityEditor.LoadableObjectIdEditorUtility"/>
    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    [NativeHeader("Runtime/BaseClasses/LoadableObjectId.h")]
    [UsedByNativeCode]
    public struct LoadableObjectId : IEquatable<LoadableObjectId>
    {
        // Field order must match native LoadableObjectId (packed to 32 bytes).
        [VisibleToOtherModules] internal GUID m_GUID;
        [VisibleToOtherModules] internal long m_LocalIdentifierInFile;
        [VisibleToOtherModules] internal FileIdentifierType m_FileIdentifierType;
        internal ContentFileSourceType m_ContentFileSourceType;

        /// <summary>
        /// True if this LoadableObjectId is initialized with valid data.
        /// </summary>
        public readonly bool IsValid => ((!m_GUID.Empty() && m_LocalIdentifierInFile != 0) || m_ContentFileSourceType != ContentFileSourceType.None);

        [ExcludeFromDocs]
        public static bool operator ==(LoadableObjectId x, LoadableObjectId y)
        {
            if (x.m_ContentFileSourceType != y.m_ContentFileSourceType)
                return false;

            if (x.m_ContentFileSourceType != ContentFileSourceType.None)
            {
                // Baked ids compare by owning content file + target lfid. All MonoScripts share one
                // file, so the (source script) GUID is not part of a MonoScript reference's identity.
                return x.m_LocalIdentifierInFile == y.m_LocalIdentifierInFile &&
                       (x.m_ContentFileSourceType == ContentFileSourceType.MonoScript || x.m_GUID == y.m_GUID);
            }

            // Otherwise use the guid, type, and fileid
            return x.m_GUID == y.m_GUID &&
                   x.m_FileIdentifierType == y.m_FileIdentifierType &&
                   x.m_LocalIdentifierInFile == y.m_LocalIdentifierInFile;
        }

        [ExcludeFromDocs]
        public static bool operator !=(LoadableObjectId x, LoadableObjectId y)
        {
            return !(x == y);
        }

        [ExcludeFromDocs]
        public override bool Equals(object obj)
        {
            return obj is LoadableObjectId other && this == other;
        }

        [ExcludeFromDocs]
        public bool Equals(LoadableObjectId other)
        {
            return this == other;
        }

        [ExcludeFromDocs]
        public override int GetHashCode()
        {
            unchecked
            {
                if (m_ContentFileSourceType != ContentFileSourceType.None)
                {
                    var bakedHashCode = m_ContentFileSourceType == ContentFileSourceType.MonoScript ? 0 : m_GUID.GetHashCode();
                    bakedHashCode = (bakedHashCode * 397) ^ (int)m_ContentFileSourceType;
                    bakedHashCode = (bakedHashCode * 397) ^ m_LocalIdentifierInFile.GetHashCode();
                    return bakedHashCode;
                }

                var hashCode = m_GUID.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)m_FileIdentifierType;
                hashCode = (hashCode * 397) ^ m_LocalIdentifierInFile.GetHashCode();
                return hashCode;
            }
        }

        /// <summary>
        /// Returns a string representation of this LoadableObjectId.
        /// </summary>
        /// <returns>A string that represents the object ID.</returns>
        public override string ToString()
        {
            if (!IsValid)
                return "{ Invalid }";

            if (m_ContentFileSourceType != ContentFileSourceType.None)
                return $"{{ guid: {m_GUID}, fileID: {m_LocalIdentifierInFile}, kind: {m_ContentFileSourceType} }}";

            return $"{{ guid: {m_GUID}, fileID: {m_LocalIdentifierInFile}, type: {(int)m_FileIdentifierType} }}";
        }
    }
}
