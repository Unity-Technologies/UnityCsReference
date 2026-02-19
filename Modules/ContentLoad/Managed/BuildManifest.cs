// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.Loading
{

    // Record the mapping from Loadable's guid to the serialized file + LFID for the root of that Asset.
    // Scenes are a special case, with only a single scene per serialized file.
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    internal struct LoadableMapEntry
    {
        public string ObjectIdHash;
        public int SerializedFile; // Index into BuildManifest.SerializedFiles
        public long Identifier; // LocalIdentifierInFileType
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

    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    internal struct SerializedFile
    {
        public int Index;           // Index of this entry in BuildManifest.SerializedFiles

        // Stable id, used to reference the file from other SerializedFile in a way that doesn't break when the content changes.
        // Currently based on the cluster or guid of the source
        public string ID;
        public bool IsBuiltIn;      // Whether to use the PersistentManager fallback for Content Files
        // Xxhash3 of the content of the SerializedFile. Used for the filename(+".cf") and for lookup into UDS
        public string ContentHash;

        public Resource[] Resources; // Array of resource files with content hash ex: .resS, GI, .resources
        public int[] SerializedFileDependencies; // Indexes into BuildManifest.SerializedFiles.  These dependencies must be loaded prior to loading this file.
    }

    // The build manifest stores information about the result of a build, specifically structured to support the functionality of the ContentLoadManager.
    // The content should be kept compact, this file needs to be shipped along with the content.
    // More detailed information e.g. helping with build reporting and profiling would be found in other non-shipping build output files
    [VisibleToOtherModules]
    [Serializable]
    [UsedByNativeCode]
    [NativeAsStruct]
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/ContentLoad/Public/BuildManifest.h")]
    internal class BuildManifest
    {
        public int Version;
        public string BuildName;
        public string[] RootAssets;
        public LoadableMapEntry[] Loadables;
        public LoadableSceneEntry[] LoadableScenes;
        public SerializedFile[] SerializedFiles;

        public static BuildManifest LoadAtPath(string path)
        {
            return ContentLoadManager.LoadBuildManifest(path);
        }
    }
}
