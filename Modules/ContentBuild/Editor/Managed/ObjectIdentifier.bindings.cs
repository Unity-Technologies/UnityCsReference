// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine;
using UnityEngine.Internal;
using UnityEngine.Scripting;

namespace UnityEditor.Build.Content
{
    ///<summary>Enum description of the type of file an object comes from.</summary>
    ///<remarks>Note: this enum and its values exist to provide low-level support for the **Scriptable Build Pipeline** package. This is intended for internal use only; use the &lt;a href="https://docs.unity3d.com/Packages/com.unity.scriptablebuildpipeline@latest/index.html"&gt;Scriptable Build Pipeline package&lt;/a&gt; to implement a fully featured build pipeline. You can install this via the [Package Manager window](/upm-ui.md).</remarks>
    public enum FileType
    {
        ///<summary>Object is contained in file not currently tracked by the AssetDatabase.</summary>
        ///<remarks>Internal use only. See <see cref="Build.Content.FileType" />.</remarks>
        NonAssetType = 0,
        ///<summary>Object is contained in a very old format. Currently unused.</summary>
        ///<remarks>Internal use only. See <see cref="Build.Content.FileType" />.</remarks>
        DeprecatedCachedAssetType = 1,
        ///<summary>Object is contained in a standard asset file type located in the Assets folder.</summary>
        ///<remarks>Internal use only. See <see cref="Build.Content.FileType" />.</remarks>
        SerializedAssetType = 2,
        ///<summary>Object is contained in the imported asset meta data located in the Library folder.</summary>
        ///<remarks>Internal use only. See <see cref="Build.Content.FileType" />.</remarks>
        MetaAssetType = 3
    }

    ///<summary>Struct that identifies a specific object project wide.</summary>
    ///<remarks>Note: this struct and its members exist to provide low-level support for the **Scriptable Build Pipeline** package. This is intended for internal use only; use the &lt;a href="https://docs.unity3d.com/Packages/com.unity.scriptablebuildpipeline@latest/index.html"&gt;Scriptable Build Pipeline package&lt;/a&gt; to implement a fully featured build pipeline. You can install this via the [Package Manager window](/upm-ui.md).</remarks>
    [Serializable]
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/ContentBuild/Editor/SBPSupport/ObjectIdentifier.h")]
    [StaticAccessor("BuildPipeline", StaticAccessorType.DoubleColon)]
    public struct ObjectIdentifier : IEquatable<ObjectIdentifier>, IComparable<ObjectIdentifier>
    {
        [NativeName("guid")]
        internal GUID m_GUID;
        ///<summary>The specific asset that contains this object.</summary>
        ///<remarks>Internal use only. See <see cref="Build.Content.ObjectIdentifier" />.</remarks>
        public GUID guid
        {
            get => m_GUID;
            internal set => m_GUID = value;
        }

        [NativeName("localIdentifierInFile")]
        internal long m_LocalIdentifierInFile;
        ///<summary>The index of the object inside a serialized file.</summary>
        ///<remarks>Internal use only. See <see cref="Build.Content.ObjectIdentifier" />.</remarks>
        public long localIdentifierInFile
        {
            get => m_LocalIdentifierInFile;
            internal set => m_LocalIdentifierInFile = value;
        }

        [NativeName("fileType")]
        internal FileType m_FileType;
        ///<summary>Type of file that contains this object.</summary>
        ///<remarks>Internal use only. See <see cref="Build.Content.ObjectIdentifier" />.</remarks>
        public FileType fileType
        {
            get => m_FileType;
            internal set => m_FileType = value;
        }

        [NativeName("filePath")]
        internal string m_FilePath;
        ///<summary>The file path on disk that contains this object. (Only used for objects not known by the AssetDatabase).</summary>
        ///<remarks>Internal use only. See <see cref="Build.Content.ObjectIdentifier" />.</remarks>
        public string filePath
        {
            get => m_FilePath;
            internal set => m_FilePath = value;
        }

        ///<summary>Returns a nicely formatted string for this ObjectIdentifier.</summary>
        ///<remarks>Internal use only. See <see cref="Build.Content.ObjectIdentifier" />.</remarks>
        public override string ToString()
        {
            return string.Format("{{ guid: {0}, fileID: {1}, type: {2}, path: {3}}}", m_GUID, m_LocalIdentifierInFile, m_FileType, m_FilePath);
        }

        ///<summary>Implements the IComparable interface.</summary>
        ///<returns>The returned value is the comparison result of the guid, local identifier, file type or file path depending on which of these is not equal first when checked in that order.</returns>
        public int CompareTo(ObjectIdentifier other)
        {
            if (m_GUID != other.m_GUID)
                return m_GUID.CompareTo(other.m_GUID);
            if (m_LocalIdentifierInFile != other.m_LocalIdentifierInFile)
                return m_LocalIdentifierInFile.CompareTo(other.m_LocalIdentifierInFile);
            if (m_FileType != other.m_FileType)
                return m_FileType.CompareTo(other.m_FileType);
            return m_FilePath.CompareTo(other.m_FilePath);
        }

        ///<summary>Returns true if the ObjectIdentifiers are the same.</summary>
        ///<remarks>Internal use only. See <see cref="Build.Content.ObjectIdentifier" />.</remarks>
        public static bool operator ==(ObjectIdentifier a, ObjectIdentifier b)
        {
            return a.CompareTo(b) == 0;
        }

        ///<summary>Returns true if the ObjectIdentifiers are different.</summary>
        ///<remarks>Internal use only. See <see cref="Build.Content.ObjectIdentifier" />.</remarks>
        public static bool operator !=(ObjectIdentifier a, ObjectIdentifier b)
        {
            return a.CompareTo(b) != 0;
        }

        [ExcludeFromDocs]
        public static bool operator <(ObjectIdentifier a, ObjectIdentifier b)
        {
            return a.CompareTo(b) < 0;
        }

        [ExcludeFromDocs]
        public static bool operator >(ObjectIdentifier a, ObjectIdentifier b)
        {
            return a.CompareTo(b) > 0;
        }

        ///<summary>Returns true if the objects are equal.</summary>
        ///<remarks>Internal use only. See <see cref="Build.Content.ObjectIdentifier" />.</remarks>
        ///<param name="obj">The object to check equality with.</param>
        ///<returns>Returns true if all fields are equal.</returns>
        public override bool Equals(object obj)
        {
            return obj is ObjectIdentifier objId && Equals(objId);
        }

        ///<summary>Returns true if the objects are equal.</summary>
        ///<remarks>Internal use only. See <see cref="Build.Content.ObjectIdentifier" />.</remarks>
        ///<param name="other">The other object identifier to check equality with.</param>
        ///<returns>Returns true if all fields are equal.</returns>
        public bool Equals(ObjectIdentifier other)
        {
            return CompareTo(other) == 0;
        }

        ///<summary>Gets the hash code for the ObjectIdentifier.</summary>
        ///<remarks>Internal use only. See <see cref="Build.Content.ObjectIdentifier" />.</remarks>
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = m_GUID.GetHashCode();
                hashCode = (hashCode * 397) ^ m_LocalIdentifierInFile.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)m_FileType;
                if (!string.IsNullOrEmpty(m_FilePath))
                    hashCode = (hashCode * 397) ^ m_FilePath.GetHashCode();
                return hashCode;
            }
        }

        ///<summary>Tries to find, load, and return the Object that represents this ObjectIdentifier.</summary>
        ///<remarks>Returns null if the Object cannot be found or loaded. This can happen if the asset that contains the Object identified was deleted or the ObjectIdentifier is invalid.</remarks>
        [FreeFunction("GetObjectFromObjectIdentifier")]
        public static extern UnityEngine.Object ToObject(ObjectIdentifier objectId);

        ///<summary>Tries to return the InstanceID that represents this ObjectIdentifier.</summary>
        ///<remarks>Returns 0 if the ObjectIdentifier is invalid.</remarks>
        [Obsolete("Deprecated, use ToEntityId instead.", true)]
        public static int ToInstanceID(ObjectIdentifier objectId) => ToEntityId(objectId);
        ///<summary>Tries to return the EntityId that represents this ObjectIdentifier.</summary>
        ///<remarks>Returns EntityId.None if the ObjectIdentifier is invalid.</remarks>
        [FreeFunction("GetEntityIdFromObjectIdentifier")]
        public static extern EntityId ToEntityId(ObjectIdentifier objectId);

        public static bool TryGetObjectIdentifier(UnityEngine.Object targetObject, out ObjectIdentifier objectId)
        {
            return GetObjectIdentifierFromObject(targetObject, out objectId);
        }

        ///<summary>Tries to convert a persistent Object into an ObjectIdentifier.</summary>
        ///<param name="entityId">The object identifier's entity id to look up.</param>
        ///<param name="objectId">Out parameter with the found object identifier.</param>
        ///<remarks>Returns false if it was not possible. This can happen if the Object is a Scene Object, or was not loaded from and Object on disk.</remarks>
        public static bool TryGetObjectIdentifier(EntityId entityId, out ObjectIdentifier objectId)
        {
            return GetObjectIdentifierFromEntityId(entityId, out objectId);
        }

        internal static extern bool GetObjectIdentifierFromObject(UnityEngine.Object targetObject, out ObjectIdentifier objectId);

        internal static extern bool GetObjectIdentifierFromEntityId(EntityId entityId, out ObjectIdentifier objectId);
    }
}
