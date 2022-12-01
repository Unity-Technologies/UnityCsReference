// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Profiling;
using UnityEngine.TextCore.Text;

namespace UnityEngine.UIElements
{
    internal class VisualTreeAssetChangeTrackerUpdater : BaseVisualTreeUpdater
    {
        private struct VisualTreeAssetToTrackMappingEntry
        {
            public int m_LastDirtyCount;
            public int m_LastElementCount;
            public int m_LastInlinePropertiesCount;
            public int m_LastAttributePropertiesDirtyCount;
            public HashSet<ILiveReloadAssetTracker<VisualTreeAsset>> m_Trackers;
        }

        private static readonly string s_Description = "Update UI Assets (Editor)";
        private static readonly ProfilerMarker s_ProfilerMarker = new ProfilerMarker(s_Description);
        public override ProfilerMarker profilerMarker => s_ProfilerMarker;

        // HashSet is for faster removals when elements go away from a panel
        private HashSet<TextElement> m_TextElements = new HashSet<TextElement>();

        private bool m_HasAnyTextAssetChanged;

        private readonly Action<bool, Object> m_TextAssetChange;
        private readonly Action<Object> m_ColorGradientChange;

        public VisualTreeAssetChangeTrackerUpdater()
        {
            m_TextAssetChange = OnTextAssetChange;
            m_ColorGradientChange = OnTextAssetChange;

            TextEventManager.FONT_PROPERTY_EVENT.Add(m_TextAssetChange);
            TextEventManager.SPRITE_ASSET_PROPERTY_EVENT.Add(m_TextAssetChange);
            TextEventManager.COLOR_GRADIENT_PROPERTY_EVENT.Add(m_ColorGradientChange);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                TextEventManager.FONT_PROPERTY_EVENT.Remove(m_TextAssetChange);
                TextEventManager.SPRITE_ASSET_PROPERTY_EVENT.Remove(m_TextAssetChange);
                TextEventManager.COLOR_GRADIENT_PROPERTY_EVENT.Remove(m_ColorGradientChange);

                m_TextElements.Clear();

                m_LiveReloadVisualTreeAssetTrackers.Clear();
                m_AssetToTrackerMap.Clear();
            }
        }

        void OnTextAssetChange(bool b, Object o)
        {
            OnTextAssetChange(o);
        }

        void OnTextAssetChange(Object asset)
        {
            // Note: due to Rich Text Tags, it's very difficult to predict if a text asset is actually in use
            // Therefore we will invalidate ALL text objects but only for panels which have Live Reload enabled
            if (panel.enableAssetReload)
            {
                m_HasAnyTextAssetChanged = true;
                panel.RequestUpdateAfterExternalEvent(this);
            }
        }

        public override void OnVersionChanged(VisualElement ve, VersionChangeType versionChangeType)
        {
            // If a change is done to a Runtime Panel and the Editor is not playing (i.e. it's in Edit Mode), the Game
            // View may not update itself to reflect the changes in the visual tree, so here we make sure it does that.
            if (panel.contextType == ContextType.Player && !IsEditorPlaying.Invoke())
            {
                UpdateGameView.Invoke();
            }
        }

        internal static Func<bool> IsEditorPlaying;
        internal static Action UpdateGameView;

        private int m_PreviousInMemoryAssetsVersion = 0;

        private const int kMinUpdateDelayMs = 1000; // TODO this should probably be a setting at some point
        private long m_LastUpdateTimeMs = 0;

        // These are 2 data structures that hold some duplication to allow faster access to information as needed.
        // We guarantee the keys on m_AssetToTrackerMap are exactly the same entries from m_LiveReloadVisualTreeAssetTrackers
        // to avoid having to get all keys from the dictionary, as we use the list of trackers for a few operations.
        // Having the information indexed by asset allows quick access to the trackers keeping tabs on them
        // so that we can check assets for being dirty only once per Update call (instead of potentially multiple times
        // if there are multiple trackers looking at the same asset - e.g. life bars on a game).
        private HashSet<ILiveReloadAssetTracker<VisualTreeAsset>> m_LiveReloadVisualTreeAssetTrackers = new HashSet<ILiveReloadAssetTracker<VisualTreeAsset>>();
        private Dictionary<VisualTreeAsset, VisualTreeAssetToTrackMappingEntry> m_AssetToTrackerMap = new Dictionary<VisualTreeAsset, VisualTreeAssetToTrackMappingEntry>();

        // List to help with the Update() and avoid creating and destroying the list.
        private HashSet<ILiveReloadAssetTracker<VisualTreeAsset>> m_TrackersToRefresh = new HashSet<ILiveReloadAssetTracker<VisualTreeAsset>>();

        internal ILiveReloadAssetTracker<StyleSheet> m_LiveReloadStyleSheetAssetTracker;

        internal void StartVisualTreeAssetTracking(ILiveReloadAssetTracker<VisualTreeAsset> tracker,
            VisualElement visualElementUsingAsset)
        {
            int dirtyCount = tracker.StartTrackingAsset(visualElementUsingAsset.visualTreeAssetSource);

            m_LiveReloadVisualTreeAssetTrackers.Add(tracker);

            if (!m_AssetToTrackerMap.TryGetValue(visualElementUsingAsset.visualTreeAssetSource, out var trackers))
            {
                trackers = new VisualTreeAssetToTrackMappingEntry()
                {
                    m_LastDirtyCount = dirtyCount,
                    m_LastElementCount = visualElementUsingAsset.visualTreeAssetSource.visualElementAssets.Count,
                    m_LastInlinePropertiesCount = visualElementUsingAsset.visualTreeAssetSource.inlineSheet.rules.Sum(r => r.properties.Length),
                    m_LastAttributePropertiesDirtyCount = visualElementUsingAsset.visualTreeAssetSource.GetAttributePropertiesDirtyCount(),
                    m_Trackers = new HashSet<ILiveReloadAssetTracker<VisualTreeAsset>>()
                };
                m_AssetToTrackerMap[visualElementUsingAsset.visualTreeAssetSource] = trackers;
            }
            trackers.m_Trackers.Add(tracker);
        }

        internal void StopVisualTreeAssetTracking(VisualElement visualElementUsingAsset)
        {
            var tracker = visualElementUsingAsset.visualTreeAssetTracker;
            if (tracker == null)
            {
                return;
            }

            tracker.StopTrackingAsset(visualElementUsingAsset.visualTreeAssetSource);

            if (!tracker.IsTrackingAssets())
            {
                m_LiveReloadVisualTreeAssetTrackers.Remove(tracker);
            }

            if (m_AssetToTrackerMap.TryGetValue(visualElementUsingAsset.visualTreeAssetSource, out var trackers))
            {
                trackers.m_Trackers.Remove(tracker);
                if (trackers.m_Trackers.Count == 0)
                {
                    m_AssetToTrackerMap.Remove(visualElementUsingAsset.visualTreeAssetSource);
                }
            }
        }

        internal HashSet<ILiveReloadAssetTracker<VisualTreeAsset>> GetVisualTreeAssetTrackersListCopy()
        {
            // Return a copy, we don't want it to be modified as it would break the logic in this updater.
            // This is being called by the AssetPostprocessor, so not all the time and we can take the overhead in
            // this case in exchange for safety.
            return new HashSet<ILiveReloadAssetTracker<VisualTreeAsset>>(m_LiveReloadVisualTreeAssetTrackers);
        }

        public override void Update()
        {
            if (!panel.enableAssetReload)
            {
                return;
            }

            UpdateTextElements();

            // Early out: no tracker found for panel.
            if (m_LiveReloadVisualTreeAssetTrackers.Count == 0 && m_LiveReloadStyleSheetAssetTracker == null)
            {
                return;
            }

            if (IsEditorPlaying.Invoke())
            {
                long currentTimeMs = Panel.TimeSinceStartupMs();

                if (currentTimeMs < m_LastUpdateTimeMs + kMinUpdateDelayMs)
                {
                    return;
                }

                m_LastUpdateTimeMs = currentTimeMs;
            }

            // There's no need to reload anything if there are no changes.
            if (m_PreviousInMemoryAssetsVersion == UIElementsUtility.m_InMemoryAssetsVersion)
            {
                return;
            }

            // This value is updated by the UI Builder whenever an asset is changed.
            // We update here to prevent unnecessary checking of asset changes.
            m_PreviousInMemoryAssetsVersion = UIElementsUtility.m_InMemoryAssetsVersion;

            bool shouldRefreshStyles = false;

            // We iterate on the assets to avoid calling GetDirtyCount for the same asset more than once.
            // In Editor this seems very likely and in Runtime we're assuming there are not multiple panels going
            // around, or if there are they're not using the same UXMLs but we may have to revisit this if we ever
            // detect that to be the case (to once again avoid calling GetDirtyCount multiple times on the same asset).
            foreach (var trackedAssetEntry in m_AssetToTrackerMap)
            {
                var trackedAsset = trackedAssetEntry.Key;
                var trackersEntry = trackedAssetEntry.Value;
                var dirtyCount = AssetOperationsAccess.GetAssetDirtyCount(trackedAsset);

                // We keep a trace of the number of elements to minimize the cost of LiveReload on Layout/Style changes.
                // We also keep a trace of the number of inline rules, we need to recreate UI when they are added/removed.
                // Same goes for attribute changes, we need to re-Init elements that changed, so we recreate UI to simplify things.
                var elementCount = trackedAsset.visualElementAssets.Count;
                var inlinePropertiesCount = trackedAsset.inlineSheet.rules.Sum(r => r.properties.Length);
                var attributePropertiesDirtyCount = trackedAsset.GetAttributePropertiesDirtyCount();
                bool shouldRecreateUI = false;

                if (dirtyCount != trackersEntry.m_LastDirtyCount)
                {
                    trackersEntry.m_LastDirtyCount = dirtyCount;

                    if (elementCount != trackersEntry.m_LastElementCount ||
                        inlinePropertiesCount != trackersEntry.m_LastInlinePropertiesCount ||
                        attributePropertiesDirtyCount != trackersEntry.m_LastAttributePropertiesDirtyCount)
                    {
                        trackersEntry.m_LastElementCount = elementCount;
                        trackersEntry.m_LastInlinePropertiesCount = inlinePropertiesCount;
                        trackersEntry.m_LastAttributePropertiesDirtyCount = attributePropertiesDirtyCount;

                        shouldRecreateUI = true;
                    }
                    else
                    {
                        shouldRefreshStyles = true;
                    }
                }

                foreach (var tracker in trackersEntry.m_Trackers)
                {
                    // Update the dirty count on the tracker to keep the information correct everywhere.
                    tracker.UpdateAssetTrackerCounts(trackedAsset, dirtyCount, elementCount, inlinePropertiesCount, attributePropertiesDirtyCount);

                    if (shouldRecreateUI)
                    {
                        // Add to list to make sure we only call each tracker only once.
                        m_TrackersToRefresh.Add(tracker);
                    }
                }
            }

            foreach (var tracker in m_TrackersToRefresh)
            {
                tracker.OnTrackedAssetChanged();
            }
            m_TrackersToRefresh.Clear();

            if (shouldRefreshStyles || m_LiveReloadStyleSheetAssetTracker != null && m_LiveReloadStyleSheetAssetTracker.CheckTrackedAssetsDirty())
            {
                panel.DirtyStyleSheets();
                panel.UpdateInlineStylesRecursively();
            }
        }

        public void RegisterTextElement(TextElement element)
        {
            if (panel.enableAssetReload)
            {
                m_TextElements.Add(element);
            }
        }

        public void UnregisterTextElement(TextElement element)
        {
            if (panel.enableAssetReload)
            {
                m_TextElements.Remove(element);
            }
        }

        private void UpdateTextElements()
        {
            if (!m_HasAnyTextAssetChanged)
                return;

            try
            {
                foreach (var textElement in m_TextElements)
                {
                    textElement.IncrementVersion(VersionChangeType.Layout | VersionChangeType.Repaint);
                    textElement.textHandle.SetDirty();
                }
            }
            finally
            {
                m_HasAnyTextAssetChanged = false;
            }
        }
    }
}
