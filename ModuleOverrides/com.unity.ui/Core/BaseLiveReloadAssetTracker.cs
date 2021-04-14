// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    ////////////////////////////////////////////////////// ATTENTION! ////////////////////////////////////////////////////
    // These classes should be in the Editor module for 2020.2+, but since 2020.1 does not recognize the Editor module, //
    // this is put here temporarily, with delegates present in both the Editor and GameObject.Editor modules to cover   //
    // all cases. When support for Unity 2020.1 is dropped, we should move this file and get rid of the delegate calls. //
    // The rest of the code is ready to work with just these (internal) classes moving.                                 //
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    internal delegate T LoadAssetAtPathMethod<T>(string asset) where T : UnityEngine.Object;

    internal static class AssetOperationsAccess
    {
        internal static Func<Object, string> GetAssetPath;
        internal static Func<Object, int> GetAssetDirtyCount;
        internal static LoadAssetAtPathMethod<StyleSheet> LoadStyleSheetAtPath;
        internal static LoadAssetAtPathMethod<ThemeStyleSheet> LoadThemeAtPath;
    }

    class AssetTracking<T> where T : ScriptableObject
    {
        public T m_Asset;
        public string m_AssetPath;
        public int m_LastDirtyCount;
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
                    m_AssetPath = AssetOperationsAccess.GetAssetPath(asset),
                    m_LastDirtyCount = AssetOperationsAccess.GetAssetDirtyCount(asset),
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
                int currentDirtyCount = AssetOperationsAccess.GetAssetDirtyCount(tracking.m_Asset);

                if (tracking.m_LastDirtyCount != currentDirtyCount)
                {
                    tracking.m_LastDirtyCount = currentDirtyCount;
                    isTrackedAssetDirty = true;
                }
            }

            return isTrackedAssetDirty;
        }

        public void UpdateAssetDirtyCount(T asset, int newDirtyCount)
        {
            if (m_TrackedAssets.TryGetValue(asset.GetInstanceID(), out var assetTracking))
            {
                assetTracking.m_LastDirtyCount = newDirtyCount;
            }
        }

        public abstract void OnAssetsImported(HashSet<T> changedAssets, HashSet<string> deletedAssets);
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

        public override void OnAssetsImported(HashSet<VisualTreeAsset> changedAssets, HashSet<string> deletedAssets)
        {
            // Early out: no asset being tracked.
            if (m_TrackedAssets.Count == 0)
            {
                return;
            }

            bool shouldReload = ProcessChangedAssets(changedAssets);

            if (ProcessDeletedAssets(deletedAssets) || shouldReload)
            {
                OnVisualTreeAssetChanged();

                if (m_RemovedAssets.Count > 0)
                {
                    foreach (var removedAsset in m_RemovedAssets)
                    {
                        m_TrackedAssets.Remove(removedAsset);
                    }

                    m_RemovedAssets.Clear();
                }
            }
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
        public override void OnAssetsImported(HashSet<StyleSheet> changedAssets, HashSet<string> deletedAssets)
        {
            // Early out: no assets being tracked.
            if (m_TrackedAssets.Count == 0)
            {
                return;
            }

            if (ProcessDeletedAssets(deletedAssets))
            {
                foreach (var removedAsset in m_RemovedAssets)
                {
                    m_TrackedAssets.Remove(removedAsset);
                }
                m_RemovedAssets.Clear();
            }
        }
    }
}

