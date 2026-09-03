// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine
{
    ///<summary>Manifest for all the AssetBundles in the build.</summary>
    ///<seealso cref="M:UnityEditor.BuildPipeline.BuildAssetBundles" />
    ///<seealso cref="AssetBundle.GetAllAssetNames" />
    [global::UnityEngine.NativeClass("AssetBundleManifest", PersistentTypeId = 290)]
    [NativeHeader("Modules/AssetBundle/Public/AssetBundleManifest.h")]
    public class AssetBundleManifest : Object
    {
        private AssetBundleManifest() {}

        ///<summary>Get all the AssetBundles in the manifest.</summary>
        ///<returns>An array of asset bundle names.</returns>
        [NativeMethod("GetAllAssetBundles")]
        public extern string[] GetAllAssetBundles();

        ///<summary>Get all the AssetBundles with variant in the manifest.</summary>
        ///<returns>An array of asset bundle names.</returns>
        [NativeMethod("GetAllAssetBundlesWithVariant")]
        public extern string[] GetAllAssetBundlesWithVariant();

        ///<summary>Get the hash for the given AssetBundle.</summary>
        ///<param name="assetBundleName">Name of the asset bundle.</param>
        ///<returns>The 128-bit hash for the asset bundle.</returns>
        [NativeMethod("GetAssetBundleHash")]
        public extern Hash128 GetAssetBundleHash(string assetBundleName);

        ///<summary>Get the direct dependent AssetBundles for the given AssetBundle.</summary>
        ///<param name="assetBundleName">Name of the asset bundle.</param>
        ///<returns>Array of asset bundle names this asset bundle depends on.</returns>
        [NativeMethod("GetDirectDependencies")]
        public extern string[] GetDirectDependencies(string assetBundleName);

        ///<summary>Get all the dependent AssetBundles for the given AssetBundle.</summary>
        ///<param name="assetBundleName">Name of the asset bundle.</param>
        ///<returns>An array of the names of all the AssetBundles that the specified AssetBundle depends on, including indirect dependencies.</returns>
        [NativeMethod("GetAllDependencies")]
        public extern string[] GetAllDependencies(string assetBundleName);
    }
}
