// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.ComponentModel;
using UnityEngine.Internal;

namespace UnityEditor
{
    // Asset Bundle building options.
    ///<summary>Asset Bundle building options.</summary>
    ///<remarks>These flags allow you to configure options when calling <see cref="BuildPipeline.BuildAssetBundles" />.
    ///
    ///                Use AssetBundleOptions to control the compression level of the AssetBundles.
    ///                By default, AssetBundles are built with full file compression using <see cref="UnityEngine.CompressionType.Lzma" />.
    ///                To compress the AssetBundle data into individual segments, use <see cref="BuildAssetBundleOptions.ChunkBasedCompression" />.
    ///                To avoid compressing the data, use <see cref="BuildAssetBundleOptions.UncompressedAssetBundle" />.
    ///
    ///                Additional resources: <see cref="UnityEngine.AssetBundle" />, <see cref="BuildPipeline.BuildAssetBundles" /></remarks>
    ///<example>
    ///  <code source="../../../../Modules/ContentBuild/Tests/local.test.build-examples/Editor/BuildPipeline/Settings/BuildAssetBundleOptions_BuildAssetBundleOptions.cs"/>
    ///</example>
    [Flags]
    public enum BuildAssetBundleOptions
    {
        // Perform the build without any special option.
        ///<summary>Build assetBundle without any special option.</summary>
        ///<example>
        ///  <code source="../../../../Modules/ContentBuild/Tests/local.test.build-examples/Editor/BuildPipeline/Settings/BuildAssetBundleOptions_Examples.cs"/>
        ///</example>
        None = 0,

        // Don't compress the data when creating the asset bundle.
        ///<summary>Don't compress the data when creating the AssetBundle.</summary>
        ///<remarks>Builds the AssetBundle without any compression, resulting in larger file sizes but faster build and load times. Uncompressed AssetBundles are 16-byte aligned.
        ///
        ///                    See [AssetBundles compression wiki](xref:um-asset-bundles-cache), <see cref="BuildAssetBundleOptions.ChunkBasedCompression" />, <see cref="UnityEngine.BuildCompression" />, and <see cref="UnityEngine.CompressionType" />.</remarks>
        UncompressedAssetBundle = 1, // 1 << 0

        // Includes all dependencies.
        [Obsolete("This has been made obsolete. It is always enabled in the new AssetBundle build system introduced in 5.0.", true)]
        [ExcludeFromDocs]
        CollectDependencies = 2, // 1 << 1

        // Forces inclusion of the entire asset.
        [Obsolete("This has been made obsolete. It is always disabled in the new AssetBundle build system introduced in 5.0.", true)]
        [ExcludeFromDocs]
        CompleteAssets = 4, // 1 << 2

        // Do not include type information within the AssetBundle.
        ///<summary>Omits type information from the AssetBundle.</summary>
        ///<remarks>This flag is useful for AssetBundles that are included within Player builds and are rebuilt for every new Player release.
        ///However, before using this flag, it is critical to fully understand the compatibility and redistribution implications of removing Type Trees.
        ///
        ///By default (when this flag is **not** specified), each SerializedFile inside an AssetBundle contains the Type Tree information for all types used in that file.
        ///
        ///Type Trees enable Unity to:
        ///
        ///
        ///
        ///* Load content that was built with earlier versions of Unity or with different script versions.
        ///
        ///* Load AssetBundles in the Editor.
        ///
        ///
        ///
        ///
        ///When this flag is specified only the hash of each Type Tree is recorded in the SerializedFile headers, not the entire Type Tree definition.
        ///
        ///**Example**:
        ///
        ///Suppose an AssetBundle includes instances of a MonoBehaviour-derived class with two fields: a string `A` followed by an integer `B`.
        ///The Type Tree records the order, types and names of the fields so that the binary presentation of each serialized object of that type can be interpreted properly.
        ///
        ///Then suppose the class definition changes to include a float `C` first, followed by integer `B` and string `A`.
        ///
        ///The Type Tree inside the AssetBundle ensures that new builds of the Player can still load it, recovering the values of the `A` and `B` fields even though they do not match the current definition
        ///of that class.
        ///
        ///**Advantages to using DisableWriteTypeTree**:
        ///
        ///Omitting Type Trees reduces AssetBundle size, which can be significant for projects with many small AssetBundles
        ///or complex scripting types containing numerous fields or deeply nested structs.
        ///Therefore this flag can be a useful optimization in cases where you know that you will always
        ///rebuild and redistribute all AssetBundles any time that you rebuild your player.
        ///
        ///**Important Considerations**:
        ///
        ///AssetBundles built without Type Trees will have strict compatibility requirements:
        ///
        ///* They can only be loaded by Players built with exactly the same types. Unity verifies this by comparing Type Tree hashes at load time, and the load will fail if any mismatch is detected.
        ///
        ///* They cannot be loaded in the Editor.
        ///* Tools like <c>binary2text</c> and <see href="https://github.com/Unity-Technologies/UnityDataTools">UnityDataTools</see> cannot view the serialized data.
        ///
        ///**Platform Limitations**
        ///
        ///For some platforms (e.g. <see cref="BuildTarget.WebGL" />), Type Trees are mandatory. Unity will reject attempts to build AssetBundles with this flag for such targets.</remarks>
        DisableWriteTypeTree = 8, // 1 << 3

        // Builds an asset bundle using a hash for the id of the object stored in the asset bundle.
        [Obsolete("This has been made obsolete. It is always enabled in the new AssetBundle build system introduced in 5.0.", true)]
        [ExcludeFromDocs]
        DeterministicAssetBundle = 16, // 1 << 4

        // Force rebuild the asset bundle.
        ///<summary>Initiates a complete rebuild of AssetBundles.</summary>
        ///<remarks>When this option is specified all the AssetBundles are rebuilt, even if none of the included assets have changed.  This flag is recommended when preparing an official build for release to make sure that all the content is accurately rebuilt based on the current state of the project.</remarks>
        ForceRebuildAssetBundle = 32, // 1 << 5

        // Ignore the type tree changes.
        ///<summary>Ignore the type tree changes when doing the incremental build check.</summary>
        ///<remarks>This allows you to ignore the type tree changes when doing the incremental build check. With this flag set, if the included assets haven't change but type trees have changed, the target assetBundle will not be rebuilt.</remarks>
        ///<example>
        ///  <code source="../../../../Modules/ContentBuild/Tests/local.test.build-examples/Editor/BuildPipeline/Settings/BuildAssetBundleOptions_Examples.cs"/>
        ///</example>
        IgnoreTypeTreeChanges = 64, // 1 << 6,

        // Append hash to the output name.
        ///<summary>Appends the hash to the AssetBundle name.</summary>
        ///<remarks>This allows you to append the hash to the AssetBundle name.</remarks>
        ///<example>
        ///  <code source="../../../../Modules/ContentBuild/Tests/local.test.build-examples/Editor/BuildPipeline/Settings/BuildAssetBundleOptions_Examples.cs"/>
        ///</example>
        AppendHashToAssetBundleName = 128, // 1 << 7

        // Forces chunk-based LZ4 compression for the asset bundle. Such asset bundles can be decompressed on the fly.
        ///<summary>Use chunk-based LZ4 compression when creating the AssetBundle.</summary>
        ///<remarks>When chunk-based compression is used, the content of the AssetBundle is broken into individual segments, which are compressed independently using the <see cref="CompressionType.Lz4HC" /> algorithm.
        ///                    The resulting file is typically larger than full file <see cref="UnityEngine.CompressionType.Lzma" /> compression which is used by default,
        ///                    but AssetBundles built with this format can be loaded incrementally, e.g. by only decompressing the needed chunks.
        ///                    This is the default format used for AssetBundles stored in the AssetBundle Cache and provides a good balance between compression ratio and load performance.
        ///
        ///Related topics: [AssetBundles compression](xref:um-asset-bundles-cache), <see cref="UnityEngine.BuildCompression" />, <see cref="UnityEngine.CompressionType" />, <see cref="Unity.IO.Archive.ArchiveHandle.Compression" />, <see cref="UnityEngine.Caching" />, <see cref="BuildAssetBundleOptions.UncompressedAssetBundle" />, <see cref="BuildOptions.CompressWithLz4" />.</remarks>
        ChunkBasedCompression = 256, // 1 << 8

        // Force the build to fail when any errors are encountered
        ///<summary>Fails the build if any errors are reported during it.</summary>
        ///<remarks>Without this flag, non-fatal errors - such as a failure to compile a shader for a particular platform - will not cause the build to fail, but may result in incorrect behaviour at runtime.</remarks>
        ///<example>
        ///  <code source="../../../../Modules/ContentBuild/Tests/local.test.build-examples/Editor/BuildPipeline/Settings/BuildAssetBundleOptions_Examples.cs"/>
        ///</example>
        StrictMode = 512, // 1 << 9

        // Do a dry run build which doesn't actually build the asset bundles.
        ///<summary>Performs a simulated build of AssetBundles without actually creating the files.</summary>
        ///<remarks>This allows you to do a dry run build for the AssetBundles but not actually build them. With this option enabled, <see cref="BuildPipeline.BuildAssetBundles" /> still returns an <see cref="UnityEngine.AssetBundleManifest" /> object which contains valid AssetBundle dependencies and hashes.</remarks>
        ///<example>
        ///  <code source="../../../../Modules/ContentBuild/Tests/local.test.build-examples/Editor/BuildPipeline/Settings/BuildAssetBundleOptions_Examples.cs"/>
        ///</example>
        DryRunBuild = 1024, // 1 << 10

        // Turns off loading an asset using file name. Results in faster AssetBundle.LoadFromFile.
        ///<summary>Disables calling LoadAsset on Asset Bundles using only the file name.</summary>
        ///<remarks>Asset Bundles by default have three ways to look up the same asset: the full asset path, asset file name, and asset file name with extension. The full path is serialized into Asset Bundles, while the file name and file name with extension are generated when an Asset Bundle is loaded from a file.
        ///
        ///                    For example, "Assets/Prefabs/Player.prefab", "Player", and "Player.prefab" are different ways to reference the same asset.
        ///
        ///                    This option will set a flag on Asset Bundles to prevent creating the asset file name lookup, saving runtime memory and improving loading performance for asset bundles.
        ///
        ///                    Related topic: <see cref="BuildAssetBundleOptions.DisableLoadAssetByFileNameWithExtension" />.</remarks>
        DisableLoadAssetByFileName = 4096, // 1 << 12,

        // Turns off loading an asset using file name + extension. Results in faster AssetBundle.LoadFromFile.
        ///<summary>Prevents loading assets from Asset Bundles using the file name with its extension.</summary>
        ///<remarks>By default, Asset Bundles support three methods to look up the same asset: full asset path, asset file name, and asset file name with extension. The full path is serialized into Asset Bundles, whereas the file name and the file name with extension are generated when an Asset Bundle is loaded from a file.
        ///
        ///                    For example, "Assets/Prefabs/Player.prefab", "Player", and "Player.prefab" are different ways to reference the same asset.
        ///
        ///                    Selecting this option sets a flag on Asset Bundles to prevent creating the asset file name with extension lookup, helping conserve runtime memory and improving loading performance for asset bundles.
        ///
        ///                    Related topic: <see cref="BuildAssetBundleOptions.DisableLoadAssetByFileName" />.</remarks>
        DisableLoadAssetByFileNameWithExtension = 8192, // 1 << 13,

        //kAssetBundleAllowEditorOnlyScriptableObjects is defined in the native BuildAssetBundleOptions as 1 << 14
        //AssetBundleAllowEditorOnlyScriptableObjects = 1 << 14,

        //Removes the Unity Version number in the Archive File & Serialized File headers during the build.
        ///<summary>Prevents the Unity Editor version from being recorded in the AssetBundle.</summary>
        ///<remarks>This flag is highly recommended in most scenarios.
        ///
        ///                    When this flag is **not** specified, the current version of the Unity Editor is recorded in the AssetBundle, specifically in the archive file header and header of each SerializedFile inside the archive.
        ///                    Including the Unity Editor version can be helpful for debugging purposes, as it allows you to identify the version of Unity used to build a specific AssetBundle.
        ///
        ///                    However, this behavior can result in unwanted side effects. For example, rebuilding an AssetBundle after installing a Unity patch, even if the actual content has not changed, will cause a change in the content of the AssetBundle. This can lead to unnecessary distribution overhead.
        ///
        ///                    Using <c>AssetBundleStripUnityVersion</c> eliminates this issue by using a placeholder version of "0.0.0" instead of the actual Unity Editor version.
        ///
        ///                    The version information is purely informational and does not affect the ability to load the AssetBundle across different Unity versions.</remarks>
        AssetBundleStripUnityVersion = 32768, // 1 << 15

        // Calculate bundle hash on the bundle content
        ///<summary>Use the content of the asset bundle to calculate the hash. This feature is always enabled.</summary>
        UseContentHash = 65536, // 1 << 16

        // Use when AssetBundle dependencies need to be calculated recursively, such as when you have a dependency chain of matching typed Scriptable Objects
        [ExcludeFromDocs]
        RecurseDependencies = 131072, // 1 << 17

        // Sprites are normally copied to all bundles that reference them. This flag prevents that behavior if the sprite is not in an atlas.
        ///<summary>Use to prevent duplicating a texture when it is referenced in multiple bundles. This would primarily happen with particle systems. The new behavior does not duplicate the texture if the sprite does not belong to an atlas. Using this flag is the desired behavior, but is not set by default for backwards compatability reasons.</summary>
        StripUnatlasedSpriteCopies = 262144, // 1 << 18

        ///<summary>Suppress the error reported when a LoadableObjectId or LoadableSceneId is encountered during an AssetBundle build.</summary>
        ///<remarks>Use this when migrating between build pipeline backends when assets legitimately have Loadable references, but the same content is also built into AssetBundles. The references still resolve to null in the resulting bundle; this flag only silences the error log to keep the build usable.</remarks>
        SuppressLoadableErrors = 1048576 // 1 << 20
    }
}
