// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine;
using UnityEngine.Scripting;

namespace UnityEditor.Experimental.Build.AssetBundle
{
    [NativeType(CodegenOptions.Custom, "SInt32")]
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
    public struct ObjectIdentifier
    {
        [NativeName("guid")]
        internal GUID m_GUID;
        public GUID guid { get { return m_GUID; } }

        [NativeName("localIdentifierInFile")]
        internal long m_LocalIdentifierInFile;
        public long localIdentifierInFile { get { return m_LocalIdentifierInFile; } }

        [NativeName("fileType")]
        internal FileType m_FileType;
        public FileType fileType { get { return m_FileType; } }

        [NativeName("filePath")]
        internal string m_FilePath;
        public string filePath { get { return m_FilePath; } }

        public override string ToString()
        {
            return UnityString.Format("{{guid: {0}, fileID: {1}, type: {2}, path: {3}}}", m_GUID, m_LocalIdentifierInFile, m_FileType, m_FilePath);
        }

        public static bool operator==(ObjectIdentifier a, ObjectIdentifier b)
        {
            if (a.m_GUID != b.m_GUID)
                return false;
            if (a.m_LocalIdentifierInFile != b.m_LocalIdentifierInFile)
                return false;
            if (a.m_FileType != b.m_FileType)
                return false;
            if (a.m_FilePath != b.m_FilePath)
                return false;
            return true;
        }

        public static bool operator!=(ObjectIdentifier a, ObjectIdentifier b)
        {
            return !(a == b);
        }

        public static bool operator<(ObjectIdentifier a, ObjectIdentifier b)
        {
            if (a.m_GUID == b.m_GUID)
                return a.m_LocalIdentifierInFile < b.m_LocalIdentifierInFile;
            return a.m_GUID < b.m_GUID;
        }

        public static bool operator>(ObjectIdentifier a, ObjectIdentifier b)
        {
            if (a.m_GUID == b.m_GUID)
                return a.m_LocalIdentifierInFile > b.m_LocalIdentifierInFile;
            return a.m_GUID > b.m_GUID;
        }

        public override bool Equals(object obj)
        {
            return obj is ObjectIdentifier && this == (ObjectIdentifier)obj;
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
    }
}
