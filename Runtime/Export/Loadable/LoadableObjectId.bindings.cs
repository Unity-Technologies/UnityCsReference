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
    /// A low-level reference to an object within an asset, used to pull assets into a ContentDirectory build, and for on-demand
    /// loading.
    /// </summary>
    /// <remarks>
    /// LoadableObjectId is the underlying reference type used by <see cref="Loadable{T}"/>. It contains the information needed
    /// to identify and load a specific object from built content.
    ///
    /// In the Editor, use <see cref="UnityEditor.LoadableObjectIdEditorUtility"/> to create LoadableObjectIds. Typically these
    /// will be serialized as part of `Loadable{T}` fields on ScriptableObject-derived classes. When those ScriptableObjects are
    /// built as part of a Content Directory, the assets referenced by the LoadableObjectId are recursively pulled into
    /// the build output. At runtime, the <see cref="ContentLoadManager"/> resolves LoadableObjectIds to the
    /// correct built content as long as it is part of the currently registered content directories.
    ///
    /// Player and AssetBundle builds do not pull assets referenced by LoadableObjectId into the build, this is only supported
    /// by <see cref="BuildPipeline.BuildContentDirectory"/>.
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
