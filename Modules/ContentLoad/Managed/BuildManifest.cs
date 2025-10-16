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
    // Record a mapping from a string to a Loadable's guid.
    // This makes it possible to load Loadables by a user friendly string even in the context of Runtime which doesn't have AssetDatabase.
    // Expected usage would be to record the Asset Path of the "Root Assets", whereas regular Loadables already serialize their guid and don't need a string mapping.
    // However the data structure is intentionally generic enough to support multiple strings per GUID, and not enforcing that only RootAssets have entries.
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    internal struct LoadableKeyEntry
    {
        public string LoadableKey;
        public string ObjectIdHash;
    }

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

    // UCBP-Backport
    /*
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    internal struct LoadableSceneEntry
    {
        public UnityEngine.GUID GUID;
        public string Path;
        public int SerializedFile; // Index into BuildManifest.SerializedFiles
    }
    */
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

        public int Archive;         // Index into BuildManifest.Archives, or -1 when the serialized file is not inside an Archive

        // Xxhash3 of the content of the SerializedFile. Used for the filename(+".cf") and for lookup into UDS
        public string ContentHash;

        public Resource[] Resources; // Array of resource files with content hash ex: .resS, GI, .resources
        public int[] SerializedFileDependencies; // Indexes into BuildManifest.SerializedFiles.  These dependencies must be loaded prior to loading this file.
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    internal struct Archive
    {
        public int Index; // Index of this entry in BuildManifest.Archives
        public string FileName; // Path relative to the build output folder

        public int CompressionType;
        public uint CRC;
        public string ContentHash; // Hash128 in string form
        public int[] SerializedFiles; // Index into SerializedFiles array for the SerializedFiles located inside this archive
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
        public LoadableKeyEntry[] LoadableKeys;
        public LoadableMapEntry[] Loadables;
        // UCBP-Backport public LoadableSceneEntry[] LoadableScenes;
        public SerializedFile[] SerializedFiles;
        public Archive[] Archives;

        public static BuildManifest LoadAtPath(string path)
        {
            return ContentLoadManager.LoadBuildManifest(path);
        }
    }
}
