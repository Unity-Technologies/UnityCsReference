using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    internal interface ILiveReloadAssetTracker<T> where T : ScriptableObject
    {
        int StartTrackingAsset(T asset);
        void StopTrackingAsset(T asset);
        bool IsTrackingAsset(T asset);
        bool IsTrackingAssets();
        bool CheckTrackedAssetsDirty();
        void UpdateAssetDirtyCount(T asset, int newDirtyCount);
        void OnAssetsImported(HashSet<T> changedAssets, HashSet<string> deletedAssets);
        void OnTrackedAssetChanged();
    }
}
