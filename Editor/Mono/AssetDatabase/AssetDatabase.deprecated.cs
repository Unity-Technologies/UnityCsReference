// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;

namespace UnityEditor
{
    partial class AssetDatabase
    {
        // Gets the path to the text .meta file associated with an asset
        [Obsolete("GetTextMetaDataPathFromAssetPath has been renamed to GetTextMetaFilePathFromAssetPath (UnityUpgradable) -> GetTextMetaFilePathFromAssetPath(*)")]
        public static string GetTextMetaDataPathFromAssetPath(string path) { return null; }
    }

    // Used to be part of Asset Server, and public API for some reason.
    [Obsolete("AssetStatus enum is not used anymore (Asset Server has been removed)")]
    public enum AssetStatus
    {
        Calculating = -1,
        ClientOnly = 0,
        ServerOnly = 1,
        Unchanged = 2,
        Conflict = 3,
        Same = 4,
        NewVersionAvailable = 5,
        NewLocalVersion = 6,
        RestoredFromTrash = 7,
        Ignored = 8,
        BadState = 9
    }

    // Used to be part of Asset Server, and public API for some reason.
    [Obsolete("AssetsItem class is not used anymore (Asset Server has been removed)")]
    [StructLayout(LayoutKind.Sequential)]
    [System.Serializable]
    public sealed class AssetsItem
    {
        public string guid;
        public string pathName;
        public string message;
        public string exportedAssetPath;
        public string guidFolder;
        public int enabled;
        public int assetIsDir;
        public int changeFlags;
        public string previewPath;
        public int exists;
    }
}

