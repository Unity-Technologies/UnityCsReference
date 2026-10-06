// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace Unity.Loading
{

    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    internal struct RootAssetEntry
    {
        public int SerializedFile; // Index into BuildManifest.SerializedFiles
        public long Identifier; // LocalIdentifierInFileType of the root's main object
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    internal struct LoadableSceneEntry
    {
        public UnityEngine.GUID GUID;
        public string Path;
        public int SerializedFile; // Index into BuildManifest.SerializedFiles
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    internal struct Resource
    {
        public string FileName;     // file name of the resource file
        public string ContentHash;  // xxh3 hash of the content of the resource file
    }

    // Mirror of native ContentLoad::BuildInfo (Modules/ContentLoad/Public/BuildManifest.h).
    // Properties of the build that produced this manifest. Add new build-time facts here.
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    internal struct BuildInfo
    {
        public string BuildName;
        public bool BuiltWithTypeTrees;
    }

    // Canonical source identity of a SerializedFile: {Guid, Lfid, Kind}. The stable Hash128 key
    // used for runtime lookups is derived from these at load. Lfid is only written for singleton
    // kinds. Mirror of native ContentLoad::ContentFileIdEntry.
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    internal struct ContentFileIdEntry
    {
        public int Guid;            // Index into BuildManifest.Guids
        public long Lfid;
        public int Kind;            // ContentFileSourceType
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    internal struct SerializedFile
    {
        public int Index;           // Index of this entry in BuildManifest.SerializedFiles

        public ContentFileIdEntry Id;
        // Xxhash3 of the content of the SerializedFile. Used for the filename(+".cf") and for lookup into UDS
        public string ContentHash;

        public Resource[] Resources; // Array of resource files with content hash ex: .resS, GI, .resources
        public int[] SerializedFileDependencies; // Indexes into BuildManifest.SerializedFiles.  These dependencies must be loaded prior to loading this file.
    }

    // The build manifest stores information about the result of a build, specifically structured to support the functionality of the ContentLoadManager.
    // The content should be kept compact, this file needs to be shipped along with the content.
    // More detailed information e.g. helping with build reporting and profiling would be found in other non-shipping build output files
    [Serializable]
    [UsedByNativeCode]
    [NativeAsStruct]
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/ContentLoad/Public/BuildManifest.h")]
    internal class BuildManifest
    {
        public int Version;
        public BuildInfo BuildInfo;
        public RootAssetEntry[] RootAssets;
        public LoadableSceneEntry[] LoadableScenes;
        public string[] Guids;
        public SerializedFile[] SerializedFiles;

        public static BuildManifest LoadAtPath(string path)
        {
            return ContentLoadManager.LoadBuildManifest(path);
        }
    }
}
