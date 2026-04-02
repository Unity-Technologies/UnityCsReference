// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Bindings;
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
        public int m_LastStyleSheetCount;
        public int m_ReferenceCount;
    }

    [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
    internal abstract class BaseLiveReloadAssetTracker<T> : ILiveReloadAssetTracker<T> where T : ScriptableObject
    {
        protected Dictionary<EntityId, AssetTracking<T>> m_TrackedAssets = new Dictionary<EntityId, AssetTracking<T>>();
        protected List<EntityId> m_RemovedAssets = new List<EntityId>();

        public int StartTrackingAsset(T asset)
        {
            EntityId assetId = asset.GetEntityId();

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

        public bool StopTrackingAsset(T asset)
        {
            EntityId assetId = asset.GetEntityId();

            if (!m_TrackedAssets.ContainsKey(assetId))
            {
                return false;
            }

            if (m_TrackedAssets[assetId].m_ReferenceCount <= 1)
            {
                m_TrackedAssets.Remove(assetId);
                return true;
            }

            m_TrackedAssets[assetId].m_ReferenceCount--;
            return false;
        }

        public bool IsTrackingAsset(T asset)
        {
            return m_TrackedAssets.ContainsKey(asset.GetEntityId());
        }

        public bool IsTrackingAsset(string assetPath)
        {
            foreach (var value in m_TrackedAssets.Values)
            {
                if (value.m_AssetPath == assetPath)
                    return true;
            }
            return false;
        }

        public bool IsTrackingAssets()
        {
            return m_TrackedAssets.Count > 0;
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

    [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
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
                EntityId assetId = changedAsset.GetEntityId();
                if (m_TrackedAssets.ContainsKey(assetId))
                {
                    return true;
                }
            }

            return false;
        }
    }

    [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
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
