// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine;
using UnityEngine.Scripting;

namespace UnityEditor.Build.Content
{
    public enum FileType
    {
        NonAssetType = 0,
        DeprecatedCachedAssetType = 1,
        SerializedAssetType = 2,
        MetaAssetType = 3
    }

    [Serializable]
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/BuildPipeline/Editor/Public/ObjectIdentifier.h")]
    [StaticAccessor("BuildPipeline", StaticAccessorType.DoubleColon)]
    public struct ObjectIdentifier : IEquatable<ObjectIdentifier>, IComparable<ObjectIdentifier>
    {
        [NativeName("guid")]
        internal GUID m_GUID;
        public GUID guid
        {
            get => m_GUID;
            internal set => m_GUID = value;
        }

        [NativeName("localIdentifierInFile")]
        internal long m_LocalIdentifierInFile;
        public long localIdentifierInFile
        {
            get => m_LocalIdentifierInFile;
            internal set => m_LocalIdentifierInFile = value;
        }

        [NativeName("fileType")]
        internal FileType m_FileType;
        public FileType fileType
        {
            get => m_FileType;
            internal set => m_FileType = value;
        }

        [NativeName("filePath")]
        internal string m_FilePath;
        public string filePath
        {
            get => m_FilePath;
            internal set => m_FilePath = value;
        }

        public override string ToString()
        {
            return UnityString.Format("{{ guid: {0}, fileID: {1}, type: {2}, path: {3}}}", m_GUID, m_LocalIdentifierInFile, m_FileType, m_FilePath);
        }

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

        public static bool operator ==(ObjectIdentifier a, ObjectIdentifier b)
        {
            return a.CompareTo(b) == 0;
        }

        public static bool operator!=(ObjectIdentifier a, ObjectIdentifier b)
        {
            return a.CompareTo(b) != 0;
        }

        public static bool operator<(ObjectIdentifier a, ObjectIdentifier b)
        {
            return a.CompareTo(b) < 0;
        }

        public static bool operator>(ObjectIdentifier a, ObjectIdentifier b)
        {
            return a.CompareTo(b) > 0;
        }

        public override bool Equals(object obj)
        {
            return obj is ObjectIdentifier objId && Equals(objId);
        }

        public bool Equals(ObjectIdentifier other)
        {
            return CompareTo(other) == 0;
        }

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

        [FreeFunction("GetObjectFromObjectIdentifier")]
        public static extern UnityEngine.Object ToObject(ObjectIdentifier objectId);

        [FreeFunction("GetInstanceIDFromObjectIdentifier")]
        public static extern int ToInstanceID(ObjectIdentifier objectId);

        public static bool TryGetObjectIdentifier(UnityEngine.Object targetObject, out ObjectIdentifier objectId)
        {
            return GetObjectIdentifierFromObject(targetObject, out objectId);
        }

        public static bool TryGetObjectIdentifier(int instanceID, out ObjectIdentifier objectId)
        {
            return GetObjectIdentifierFromInstanceID(instanceID, out objectId);
        }

        internal static extern bool GetObjectIdentifierFromObject(UnityEngine.Object targetObject, out ObjectIdentifier objectId);

        internal static extern bool GetObjectIdentifierFromInstanceID(int instanceID, out ObjectIdentifier objectId);
    }
}
