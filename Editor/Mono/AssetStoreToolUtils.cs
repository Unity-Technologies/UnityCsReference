// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditorInternal;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace UnityEditorInternal
{
    /**
     *  Utilities for the Asset Store upload tool
     */
    public partial class AssetStoreToolUtils
    {
        public static bool PreviewAssetStoreAssetBundleInInspector(AssetBundle bundle, AssetStoreAsset info)
        {
            info.id = 0; // make sure the id is zero when previewing
            info.previewAsset = bundle.mainAsset;
            AssetStoreAssetSelection.Clear();
            AssetStoreAssetSelection.AddAssetInternal(info);

            // Make the inspector show the asset
            Selection.activeObject = AssetStoreAssetInspector.Instance;
            AssetStoreAssetInspector.Instance.Repaint();
            return true;
        }

        public static void UpdatePreloadingInternal()
        {
            AssetStoreUtils.UpdatePreloading();
        }
    }
} // UnityEditor namespace
