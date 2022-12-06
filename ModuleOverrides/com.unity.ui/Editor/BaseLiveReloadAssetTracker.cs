// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    class AssetTracking<T> where T : ScriptableObject
    {
        public T m_Asset;
        public string m_AssetPath;
        public int m_LastDirtyCount;
        public int m_LastElementCount;
        public int m_LastInlinePropertiesCount;
        public int m_LastAttributePropertiesDirtyCount;
        public int m_ReferenceCount;
    }

    internal abstract class BaseLiveReloadAssetTracker<T> : ILiveReloadAssetTracker<T> where T : ScriptableObject
    {
        protected Dictionary<int, AssetTracking<T>> m_TrackedAssets = new Dictionary<int, AssetTracking<T>>();
        protected List<int> m_RemovedAssets = new List<int>();

        public int StartTrackingAsset(T asset)
        {
            int assetId = asset.GetInstanceID();

            if (m_TrackedAssets.TryGetValue(assetId, out var tracking))
            {
                tracking.m_ReferenceCount++;
            }
            else
            {
                tracking = new AssetTracking<T>()
                {
                    m_Asset = asset,
                    m_AssetPath = AssetDatabase.GetAssetPath(asset),
                    m_LastDirtyCount = EditorUtility.GetDirtyCount(asset),
                    m_ReferenceCount = 1
                };
                m_TrackedAssets[assetId] = tracking;
            }

            return tracking.m_LastDirtyCount;
        }

        public void StopTrackingAsset(T asset)
        {
            int assetId = asset.GetInstanceID();

            if (!m_TrackedAssets.ContainsKey(assetId))
            {
                return;
            }

            if (m_TrackedAssets[assetId].m_ReferenceCount <= 1)
            {
                m_TrackedAssets.Remove(assetId);
            }
            else
            {
                m_TrackedAssets[assetId].m_ReferenceCount--;
            }
        }

        public bool IsTrackingAsset(T asset)
        {
            return m_TrackedAssets.ContainsKey(asset.GetInstanceID());
        }

        public bool IsTrackingAssets()
        {
            return m_TrackedAssets.Count > 0;
        }

        public bool CheckTrackedAssetsDirty()
        {
            // Early out: no assets being tracked.
            if (m_TrackedAssets.Count == 0)
            {
                return false;
            }

            bool isTrackedAssetDirty = false;

            foreach (var styleSheetAssetEntry in m_TrackedAssets)
            {
                var tracking = styleSheetAssetEntry.Value;
                int currentDirtyCount = EditorUtility.GetDirtyCount(tracking.m_Asset);

                if (tracking.m_LastDirtyCount != currentDirtyCount)
                {
                    tracking.m_LastDirtyCount = currentDirtyCount;
                    isTrackedAssetDirty = true;
                }
            }

            return isTrackedAssetDirty;
        }

        public void UpdateAssetTrackerCounts(T asset, int newDirtyCount, int newElementCount, int newInlinePropertiesCount, int newAttributePropertiesDirtyCount)
        {
            if (m_TrackedAssets.TryGetValue(asset.GetInstanceID(), out var assetTracking))
            {
                assetTracking.m_LastDirtyCount = newDirtyCount;
                assetTracking.m_LastElementCount = newElementCount;
                assetTracking.m_LastInlinePropertiesCount = newInlinePropertiesCount;
                assetTracking.m_LastAttributePropertiesDirtyCount = newAttributePropertiesDirtyCount;
                assetTracking.m_LastDirtyCount = newDirtyCount;
            }
        }

        public abstract bool OnAssetsImported(HashSet<T> changedAssets, HashSet<string> deletedAssets);
        public virtual void OnTrackedAssetChanged() {}

        protected virtual bool ProcessChangedAssets(HashSet<T> changedAssets)
        {
            return false;
        }

        protected virtual bool ProcessDeletedAssets(HashSet<string> deletedAssets)
        {
            // Early out: nothing to be checked
            if (deletedAssets.Count == 0)
            {
                return false;
            }

            // We have the path to the deleted assets, but the dictionary uses the asset instance IDs as keys
            // so we need to look into the related paths and then delete the entries outside of the loop.
            foreach (var trackingPair in m_TrackedAssets)
            {
                var tracking = trackingPair.Value;
                if (deletedAssets.Contains(tracking.m_AssetPath))
                {
                    m_RemovedAssets.Add(trackingPair.Key);
                }
            }

            return m_RemovedAssets.Count > 0;
        }
    }

    internal abstract class BaseLiveReloadVisualTreeAssetTracker : BaseLiveReloadAssetTracker<VisualTreeAsset>
    {
        internal abstract void OnVisualTreeAssetChanged();

        public override void OnTrackedAssetChanged()
        {
            OnVisualTreeAssetChanged();
        }

        public override bool OnAssetsImported(HashSet<VisualTreeAsset> changedAssets, HashSet<string> deletedAssets)
        {
            // Early out: no asset being tracked.
            if (m_TrackedAssets.Count == 0)
            {
                return false;
            }

            bool shouldReload = ProcessChangedAssets(changedAssets);

            if (ProcessDeletedAssets(deletedAssets) || shouldReload)
            {
                shouldReload = true;
                if (m_RemovedAssets.Count > 0)
                {
                    foreach (var removedAsset in m_RemovedAssets)
                    {
                        m_TrackedAssets.Remove(removedAsset);
                    }

                    m_RemovedAssets.Clear();
                }
            }

            return shouldReload;
        }

        protected override bool ProcessChangedAssets(HashSet<VisualTreeAsset> changedAssets)
        {
            // Early out: nothing to be checked.
            if (changedAssets == null)
            {
                return false;
            }

            foreach (var changedAsset in changedAssets)
            {
                int assetId = changedAsset.GetInstanceID();
                if (m_TrackedAssets.ContainsKey(assetId))
                {
                    return true;
                }
            }

            return false;
        }
    }

    internal class LiveReloadStyleSheetAssetTracker : BaseLiveReloadAssetTracker<StyleSheet>
    {
        public override bool OnAssetsImported(HashSet<StyleSheet> changedAssets, HashSet<string> deletedAssets)
        {
            // Early out: no assets being tracked.
            if (m_TrackedAssets.Count == 0)
            {
                return false;
            }

            if (ProcessDeletedAssets(deletedAssets))
            {
                foreach (var removedAsset in m_RemovedAssets)
                {
                    m_TrackedAssets.Remove(removedAsset);
                }
                m_RemovedAssets.Clear();
            }

            return true;
        }
    }
}
