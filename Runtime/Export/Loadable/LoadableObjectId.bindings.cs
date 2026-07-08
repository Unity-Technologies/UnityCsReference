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
        [VisibleToOtherModules] internal GUID m_GUID;
        [VisibleToOtherModules] internal FileIdentifierType m_FileIdentifierType;
        [VisibleToOtherModules] internal long m_LocalIdentifierInFile;
        internal Hash128 m_ObjectIdHash;

        /// <summary>
        /// True if this LoadableObjectId is initialized with valid data.
        /// </summary>
        public readonly bool IsValid => ((!m_GUID.Empty() && m_LocalIdentifierInFile != 0) || m_ObjectIdHash.isValid);

        [ExcludeFromDocs]
        public static bool operator ==(LoadableObjectId x, LoadableObjectId y)
        {
            if (x.m_ObjectIdHash.isValid != y.m_ObjectIdHash.isValid)
                return false;

            if (x.m_ObjectIdHash.isValid)
            {
                return x.m_ObjectIdHash == y.m_ObjectIdHash;
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
                if (m_ObjectIdHash.isValid)
                    return m_ObjectIdHash.GetHashCode();

                var hashCode = m_GUID.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)m_FileIdentifierType;
                hashCode = (hashCode * 397) ^ m_LocalIdentifierInFile.GetHashCode();
                return hashCode;
            }
        }

        /// <summary>
        /// Returns a string representation of this LoadableObjectId.
        /// </summary>
        public override string ToString()
        {
            if (!IsValid)
                return "{ Invalid }";

            return $"{{ oid-{GetOrCalculateObjectIdHash()}, guid: {m_GUID}, fileID: {m_LocalIdentifierInFile}, type: {(int)m_FileIdentifierType} }}";
        }

        internal extern Hash128 GetOrCalculateObjectIdHash();
    }
}
