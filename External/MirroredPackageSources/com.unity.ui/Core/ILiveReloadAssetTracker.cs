using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    internal interface ILiveReloadAssetTracker<T> where T : ScriptableObject
    {
        void StartTrackingAsset(T asset);
        void StopTrackingAsset(T asset);
        bool IsTrackingAsset(T asset);
        bool CheckTrackedAssetsDirty();
        void OnAssetsImported(HashSet<T> changedAssets, HashSet<string> deletedAssets);
    }
}
