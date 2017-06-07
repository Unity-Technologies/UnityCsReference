// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEditor.Experimental.Build.Player;

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
    public struct ObjectIdentifier
    {
        [NativeName("guid")]
        private GUID m_GUID;
        public GUID guid { get { return m_GUID; } }

        [NativeName("localIdentifierInFile")]
        private long m_LocalIdentifierInFile;
        public long localIdentifierInFile { get { return m_LocalIdentifierInFile; } }

        [NativeName("fileType")]
        private FileType m_FileType;
        public FileType fileType { get { return m_FileType; } }

        [NativeName("filePath")]
        private string m_FilePath;
        public string filePath { get { return m_FilePath; } }

        public override string ToString()
        {
            return UnityString.Format("{{guid: {0}, fileID: {1}, type: {2}, path: {3}}}", m_GUID, m_LocalIdentifierInFile, m_FileType, m_FilePath);
        }

        public static bool operator==(ObjectIdentifier a, ObjectIdentifier b)
        {
            bool equals = a.m_GUID == b.m_GUID;
            equals &= a.m_LocalIdentifierInFile == b.m_LocalIdentifierInFile;
            equals &= a.m_FileType == b.m_FileType;
            equals &= a.m_FilePath == b.m_FilePath;
            return equals;
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
                return hashCode;
            }
        }
    }

    public enum CompressionType
    {
        None,
        Lzma,
        Lz4,
        Lz4HC,
    }

    public enum CompressionLevel
    {
        None,
        Fastest,
        Fast,
        Normal,
        High,
        Maximum,
    }

    [Serializable]
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    public struct BuildCompression
    {
        public static readonly BuildCompression DefaultUncompressed = new BuildCompression
        {
            compression = CompressionType.None,
            level = CompressionLevel.Maximum,
            blockSize = 128 * 1024
        };

        public static readonly BuildCompression DefaultLZ4 = new BuildCompression
        {
            compression = CompressionType.Lz4HC,
            level = CompressionLevel.Maximum,
            blockSize = 128 * 1024
        };

        public static readonly BuildCompression DefaultLZMA = new BuildCompression
        {
            compression = CompressionType.Lzma,
            level = CompressionLevel.Maximum,
            blockSize = 128 * 1024
        };

        public CompressionType compression;
        public CompressionLevel level;
        public uint blockSize;
    }

    [Serializable]
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    public struct BuildSettings
    {
        public string outputFolder;
        public TypeDB typeDB;
        public BuildTarget target;
        public BuildTargetGroup group;
    }

    [Serializable]
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    public struct BuildInput
    {
        [Serializable]
        [UsedByNativeCode]
        [StructLayout(LayoutKind.Sequential)]
        public struct AddressableAsset
        {
            public GUID asset;
            public string address;
        }

        [Serializable]
        [UsedByNativeCode]
        [StructLayout(LayoutKind.Sequential)]
        public struct Definition
        {
            public string assetBundleName;
            public AddressableAsset[] explicitAssets;
        }

        public Definition[] definitions;
    }

    [Serializable]
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    public struct BuildCommandSet
    {
        [Serializable]
        [UsedByNativeCode]
        [StructLayout(LayoutKind.Sequential)]
        public struct AssetLoadInfo
        {
            public GUID asset;
            public string address;
            public ObjectIdentifier[] includedObjects;
            public ObjectIdentifier[] referencedObjects;
        }

        [Serializable]
        [UsedByNativeCode]
        [StructLayout(LayoutKind.Sequential)]
        public struct SerializationInfo
        {
            public ObjectIdentifier serializationObject;
            public long serializationIndex;
        }

        [Serializable]
        [UsedByNativeCode]
        [StructLayout(LayoutKind.Sequential)]
        public struct Command
        {
            public string assetBundleName;
            public AssetLoadInfo[] explicitAssets;
            public SerializationInfo[] assetBundleObjects;
            public string[] assetBundleDependencies;
        }

        public Command[] commands;
    }


    [Serializable]
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    public struct BuildOutput
    {
        [Serializable]
        [UsedByNativeCode]
        [StructLayout(LayoutKind.Sequential)]
        public struct ResourceFile
        {
            public string fileName;
            public string fileAlias;
            public bool serializedFile;
        }

        [Serializable]
        [UsedByNativeCode]
        [StructLayout(LayoutKind.Sequential)]
        public struct ObjectLocation
        {
            public string fileName;
            public ulong offset;
            public uint size;
        }

        [Serializable]
        [UsedByNativeCode]
        [StructLayout(LayoutKind.Sequential)]
        public struct SerializedObject
        {
            public ObjectIdentifier serializedObject;
            public ObjectLocation header;
            public ObjectLocation rawData;
        }

        [Serializable]
        [UsedByNativeCode]
        [StructLayout(LayoutKind.Sequential)]
        public struct Result
        {
            public string assetBundleName;
            public SerializedObject[] assetBundleObjects;
            public ResourceFile[] resourceFiles;
            //Disabling for experimental release
            //public System.Type[] includedTypes;
        }

        public Result[] results;
    }

    [NativeHeader("Modules/BuildPipeline/Editor/Public/AssetBundleBuildInterface.h")]
    public class BuildInterface
    {
        [FreeFunction("BuildPipeline::GenerateBuildInput")]
        // DEPRECATED - We want to move AB info out of asset meta file into seperate asset for all bundle info
        extern public static BuildInput GenerateBuildInput();

        [FreeFunction("BuildPipeline::GetPlayerObjectIdentifiersInAsset")]
        extern public static ObjectIdentifier[] GetPlayerObjectIdentifiersInAsset(GUID asset, BuildTarget target);

        [FreeFunction("BuildPipeline::GetPlayerDependenciesForObject")]
        extern public static ObjectIdentifier[] GetPlayerDependenciesForObject(ObjectIdentifier objectID, BuildTarget target);

        [FreeFunction("BuildPipeline::GetPlayerDependenciesForObjects")]
        extern public static ObjectIdentifier[] GetPlayerDependenciesForObjects(ObjectIdentifier[] objectIDs, BuildTarget target);

        [FreeFunction("BuildPipeline::GetTypeForObject")]
        extern public static System.Type GetTypeForObject(ObjectIdentifier objectID);

        //[FreeFunction("BuildPipeline::GetTypeForObjects")]
        // Disabling because new bindings can't handle System.Type[] yet
        //extern public static System.Type[] GetTypeForObjects(ObjectIdentifier[] objectIDs);

        [FreeFunction("BuildPipeline::WriteResourceFiles")]
        extern public static BuildOutput WriteResourceFiles(BuildCommandSet commands, BuildSettings settings);

        [FreeFunction("BuildPipeline::ArchiveAndCompress")]
        extern public static uint ArchiveAndCompress(BuildOutput.ResourceFile[] resourceFiles, string outputBundlePath, BuildCompression compression);
    }
}
