// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine
{
    [NativeHeader("Modules/AssetBundle/Public/AssetBundleManifest.h")]
    public class AssetBundleManifest : Object
    {
        private AssetBundleManifest() {}

        // Get all assetBundles from assetBundle manifest.
        [NativeMethod("GetAllAssetBundles")]
        public extern string[] GetAllAssetBundles();

        // Get all assetBundles with variant from assetBundle manifest.
        [NativeMethod("GetAllAssetBundlesWithVariant")]
        public extern string[] GetAllAssetBundlesWithVariant();

        // Get the assetBundle hash.
        [NativeMethod("GetAssetBundleHash")]
        public extern Hash128 GetAssetBundleHash(string assetBundleName);

        // Get the direct dependent assetBundles for the given assetBundle.
        [NativeMethod("GetDirectDependencies")]
        public extern string[] GetDirectDependencies(string assetBundleName);

        // Get all dependent assetBundles for the given assetBundle.
        [NativeMethod("GetAllDependencies")]
        public extern string[] GetAllDependencies(string assetBundleName);
    }
}
