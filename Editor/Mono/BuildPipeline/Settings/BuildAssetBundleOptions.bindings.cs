// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor
{
    // Asset Bundle building options.
    [Flags]
    public enum BuildAssetBundleOptions
    {
        // Perform the build without any special option.
        None = 0,

        // Don't compress the data when creating the asset bundle.
        UncompressedAssetBundle = 1, // 1 << 0

        // Includes all dependencies.
        [Obsolete("This has been made obsolete. It is always enabled in the new AssetBundle build system introduced in 5.0.", true)]
        CollectDependencies = 2, // 1 << 1

        // Forces inclusion of the entire asset.
        [Obsolete("This has been made obsolete. It is always disabled in the new AssetBundle build system introduced in 5.0.", true)]
        CompleteAssets = 4, // 1 << 2

        // Do not include type information within the AssetBundle.
        DisableWriteTypeTree = 8, // 1 << 3

        // Builds an asset bundle using a hash for the id of the object stored in the asset bundle.
        [Obsolete("This has been made obsolete. It is always enabled in the new AssetBundle build system introduced in 5.0.", true)]
        DeterministicAssetBundle = 16, // 1 << 4

        // Force rebuild the asset bundle.
        ForceRebuildAssetBundle = 32, // 1 << 5

        // Ignore the type tree changes.
        IgnoreTypeTreeChanges = 64, // 1 << 6,

        // Append hash to the output name.
        AppendHashToAssetBundleName = 128, // 1 << 7

        // Forces chunk-based LZ4 compression for the asset bundle. Such asset bundles can be decompressed on the fly.
        ChunkBasedCompression = 256, // 1 << 8

        // Force the build to fail when any errors are encountered
        StrictMode = 512, // 1 << 9

        // Do a dry run build which doesn't actually build the asset bundles.
        DryRunBuild = 1024, // 1 << 10

        // Turns off loading an asset using file name. Results in faster AssetBundle.LoadFromFile.
        DisableLoadAssetByFileName = 4096, // 1 << 12,

        // Turns off loading an asset using file name + extension. Results in faster AssetBundle.LoadFromFile.
        DisableLoadAssetByFileNameWithExtension = 8192, // 1 << 13,

        //kAssetBundleAllowEditorOnlyScriptableObjects is defined in the native BuildAssetBundleOptions as 1 << 14
        //AssetBundleAllowEditorOnlyScriptableObjects = 1 << 14,

        //Removes the Unity Version number in the Archive File & Serialized File headers during the build.
        AssetBundleStripUnityVersion = 32768, // 1 << 15

        // Calculate bundle hash on the bundle content
        UseContentHash = 65536, // 1 << 16

        // Use when AssetBundle dependencies need to be calculated recursively, such as when you have a dependency chain of matching typed Scriptable Objects
        RecurseDependencies = 131072, // 1 << 17

        // Sprites are normally copied to all bundles that reference them. This flag prevents that behavior if the sprite is not in an atlas.
        StripUnatlasedSpriteCopies = 262144 // 1 << 18
    }
}
