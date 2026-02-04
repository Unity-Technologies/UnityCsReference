// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEngine.UIElements
{
    [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
    internal interface ILiveReloadAssetTracker<T> where T : ScriptableObject
    {
        int StartTrackingAsset(T asset);
        void StopTrackingAsset(T asset);
        bool IsTrackingAsset(T asset);
        bool IsTrackingAsset(string assetPath);
        bool IsTrackingAssets();
        bool CheckTrackedAssetsDirty();
        void UpdateAssetTrackerCounts(T asset, int newDirtyCount, int newElementCount, int newInlinePropertiesCount, int newAttributePropertiesDirtyCount);
        bool OnAssetsImported(HashSet<T> changedAssets, HashSet<string> deletedAssets);
        void OnTrackedAssetChanged();
    }
}
