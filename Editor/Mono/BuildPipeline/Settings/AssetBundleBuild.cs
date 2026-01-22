// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEditor
{
    ///<summary>AssetBundle building map entry.</summary>
    ///<remarks>This structure is used with <see cref="BuildPipeline.BuildAssetBundles" /> to specify the name of a bundle and the names of the assets that it will contain. The array of AssetBundleBuild elements that is passed to the function is known as the "building map" and serves as an alternative to specifying the contents of bundles from the editor.</remarks>
    ///<seealso cref="BuildPipeline.BuildAssetBundles" />
    public struct AssetBundleBuild
    {
        ///<summary>AssetBundle name.</summary>
        ///<remarks>When building an AssetBundle this property is converted to lowercase and used as the filename of the AssetBundle.  On platforms with case sensitive file systems, such as Linux, the AssetBundle load would fail unless the lowercase form of the AssetBundle name is specified.  To avoid surprises we recommend choosing a lowercase name for your AssetBundle.
        ///
        ///The name may start with folder names, for example "level1/materials/bundle_a", in which case the build creates those as subfolders of the output path.
        ///
        ///The provided name can end with a file extension, but typically AssetBundles are built with no extension because of the way AssetBundle variants work.
        ///
        ///In the case of AssetBundle variants, the name of the AssetBundle file is this string, concatenated with the <see cref="AssetBundleBuild.assetBundleVariant" /> property as its extension, and all converted to lowercase.</remarks>
        public string assetBundleName;
        ///<summary>AssetBundle variant.</summary>
        ///<remarks>When specified, this property is converted to lowercase and appended, like a file extension, to the assetBundleName property to build the complete AssetBundle filename.
        ///
        ///AssetBundle variants are used to achieve virtual assets in AssetBundles. Each AssetBundle with the same assetBundleName property will have the same internal IDs for equivalent Objects.
        ///For example one variant may contain high resolution images and the other could have matching images at a lower resolution.  Other AssetBundles can reference the images, and depending on which
        ///variant is loaded, those will resolve to either high or low resolution equivalents.
        ///
        ///To function correctly, each variant of an AssetBundle should have a matching list of assets.</remarks>
        ///<seealso cref="AssetImporter.assetBundleVariant" />
        public string assetBundleVariant;
        ///<summary>Asset names which belong to the given AssetBundle.</summary>
        ///<remarks>Please use the asset path relative to the project folder, for example "Assets/MyPrefab.prefab".
        ///
        ///An AssetBundle can contain either Scene files or Asset files, but not a mix of the two.
        ///
        ///This same path is used to retrieve assets from a loaded AssetBundle, for example with <see cref="UnityEngine.AssetBundle.LoadAsset" />, unless an alternative name has been specified in <see cref="AssetBundleBuild.addressableNames" />.</remarks>
        public string[] assetNames;
        ///<summary>Addressable name used to load an asset.</summary>
        ///<remarks>To provide custom addressable names for assets in the bundle, this array needs to be the same size as <see cref="AssetBundleBuild.assetNames" />. Each entry in this array will be matched to the asset in assetNames based on index. If the string in a given index in addressableNames is empty, the value in assetNames at the same index is used instead (default behaviour).</remarks>
        ///<seealso cref="UnityEngine.AssetBundle.LoadAsset" />
        [NativeName("nameOverrides")]
        public string[] addressableNames;
    }
}
