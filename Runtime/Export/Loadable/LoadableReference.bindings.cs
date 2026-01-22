// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Internal;
using UnityEngine.Scripting;

namespace UnityEngine
{

    /// <summary>
    /// A low-level reference to an object within an asset, used to pull assets into a ContentDirectory build, and for on-demand
    /// loading.
    /// </summary>
    /// <remarks>
    /// LoadableReference is the underlying reference type used by <see cref="Loadable{T}"/>. It contains the information needed
    /// to identify and load a specific object from built content.
    ///
    /// In the Editor, use <see cref="UnityEditor.LoadableReferenceEditorUtility"/> to create LoadableReferences. Typically these
    /// will be serialized as part of Loadable<T> fields on ScriptableObject-derived classes. When those ScriptableObjects are
    /// built as part of a Content Directory, then the assets referenced by the LoadableReference will be recursively pulled into
    /// the build output. At runtime, the <see cref="ContentLoadManager"/> takes care of resolving LoadableReferences to the
    /// correct built content so long as it is part of the currently registered content directories.
    ///
    /// Player and AssetBundle builds do not pull assets referenced by LoadableReference into the build, this is only supported
    /// by <see cref="BuildPipeline.BuildContentDirectory"/>.
    /// </remarks>
    /// <seealso cref="Loadable{T}"/>
    /// <seealso cref="UnityEditor.LoadableReferenceEditorUtility"/>
    /*UCBP-REMOVE*/[VisibleToOtherModules]
    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    [NativeHeader("Runtime/BaseClasses/LoadableReference.h")]
    [UsedByNativeCode]
    /*UCBP-PUBLIC*/ internal struct LoadableReference : IEquatable<LoadableReference>
    {
        internal GUID m_GUID;
        internal FileIdentifierType m_FileIdentifierType;
        internal long m_LocalIdentifierInFile;
        internal Hash128 m_ObjectIdHash;

        /// <summary>
        /// Returns true if this LoadableReference is initialized with valid data.
        /// </summary>
        public readonly bool isValid => ((!m_GUID.Empty() && m_LocalIdentifierInFile != 0) || m_ObjectIdHash.isValid);

        [ExcludeFromDocs]
        public static bool operator ==(LoadableReference x, LoadableReference y)
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
        public static bool operator !=(LoadableReference x, LoadableReference y)
        {
            return !(x == y);
        }

        [ExcludeFromDocs]
        public override bool Equals(object obj)
        {
            return obj is LoadableReference other && this == other;
        }

        [ExcludeFromDocs]
        public bool Equals(LoadableReference other)
        {
            return this == other;
        }

        [ExcludeFromDocs]
        public override int GetHashCode()
        {
            return GetOrCalculateObjectIdHash().GetHashCode();
        }

        /// <summary>
        /// Returns a string representation of this LoadableReference.
        /// </summary>
        public override string ToString()
        {
            if (!isValid)
                return "{ Invalid }";

            return $"{{ oid-{GetOrCalculateObjectIdHash()}, guid: {m_GUID}, fileID: {m_LocalIdentifierInFile}, type: {(int)m_FileIdentifierType} }}";
        }

        internal extern Hash128 GetOrCalculateObjectIdHash();
    }
}
