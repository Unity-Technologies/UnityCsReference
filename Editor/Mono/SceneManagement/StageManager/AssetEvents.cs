// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor
{
    class AssetEvents : AssetPostprocessor
    {
        public delegate void AssetsChangedOnHDD(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths);
        public static event AssetsChangedOnHDD assetsChangedOnHDD;

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            if (assetsChangedOnHDD != null)
                assetsChangedOnHDD(importedAssets, deletedAssets, movedAssets, movedFromAssetPaths);
        }
    }
}
