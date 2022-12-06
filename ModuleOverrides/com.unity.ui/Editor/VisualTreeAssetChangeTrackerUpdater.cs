// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;
using Object = System.Object;
using TextElement = UnityEngine.UIElements.TextElement;

namespace UnityEditor.UIElements
{
    internal class VisualTreeAssetChangeTrackerUpdater : BaseVisualTreeUpdater, ILiveReloadSystem
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
            m_LiveReloadStyleSheetAssetTracker = new LiveReloadStyleSheetAssetTracker();

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

                m_EditorVisualTreeAssetTracker = null;
                m_RuntimeVisualTreeAssetTrackers.Clear();
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
            if (enable && (enabledTrackers & LiveReloadTrackers.Text) != 0)
            {
                m_HasAnyTextAssetChanged = true;
                panel.RequestUpdateAfterExternalEvent(this);
            }
        }

        public override void OnVersionChanged(VisualElement ve, VersionChangeType versionChangeType)
        {
            // If a change is done to a Runtime Panel and the Editor is not playing (i.e. it's in Edit Mode), the Game
            // View may not update itself to reflect the changes in the visual tree, so here we make sure it does that.
            if (panel.contextType == ContextType.Player && !EditorApplication.isPlaying)
            {
                EditorApplication.QueuePlayerLoopUpdate();
            }
        }

        private int m_PreviousInMemoryAssetsVersion = 0;

        private const int kMinUpdateDelayMs = 1000; // TODO this should probably be a setting at some point
        private long m_LastUpdateTimeMs = 0;

        // m_AssetToTrackerMap is used for faster access to information as needed.
        // Having the information indexed by asset allows quick access to the trackers keeping tabs on them
        // so that we can check assets for being dirty only once per Update call (instead of potentially multiple times
        // if there are multiple trackers looking at the same asset - e.g. life bars on a game).
        private Dictionary<VisualTreeAsset, VisualTreeAssetToTrackMappingEntry> m_AssetToTrackerMap = new Dictionary<VisualTreeAsset, VisualTreeAssetToTrackMappingEntry>();

        // List to help with the Update() and avoid creating and destroying the list.
        private HashSet<ILiveReloadAssetTracker<VisualTreeAsset>> m_TrackersToRefresh = new HashSet<ILiveReloadAssetTracker<VisualTreeAsset>>();

        private ILiveReloadAssetTracker<StyleSheet> m_LiveReloadStyleSheetAssetTracker;

        // Depending on the panel context type either m_EditorVisualTreeAssetTracker or m_RuntimeVisualTreeAssetTrackers will be set.
        // For the editor, only one tracker is needed for the whole window.
        // For runtime, there's one tracker per UIDocument. Tracker are registered with the root VisualElement they belong to.
        private ILiveReloadAssetTracker<VisualTreeAsset> m_EditorVisualTreeAssetTracker;
        private Dictionary<VisualElement, ILiveReloadAssetTracker<VisualTreeAsset>> m_RuntimeVisualTreeAssetTrackers = new Dictionary<VisualElement, ILiveReloadAssetTracker<VisualTreeAsset>>();

        public bool enable { get; set; }

        public LiveReloadTrackers enabledTrackers { get; set; } = (LiveReloadTrackers)(~0);

        public void RegisterVisualTreeAssetTracker(ILiveReloadAssetTracker<VisualTreeAsset> tracker, VisualElement rootElement)
        {
            if (panel.contextType == ContextType.Editor)
            {
                m_EditorVisualTreeAssetTracker = tracker;
            }
            else
            {
                m_RuntimeVisualTreeAssetTrackers[rootElement] = tracker;

                // Add template container to be able to track newly created uxml assets
                if (rootElement is TemplateContainer container && container.templateSource != null)
                    StartVisualTreeAssetTracking(tracker, container.templateSource);

                // The enablement of Live Reload for panels of type ContextType.Player depends on the preference set
                // for the Game View itself.
                enable = DefaultEditorWindowBackend.IsGameViewWindowLiveReloadOn();
            }
        }

        public void UnregisterVisualTreeAssetTracker(VisualElement rootElement)
        {
            Debug.Assert(panel.contextType == ContextType.Player);
            m_RuntimeVisualTreeAssetTrackers.Remove(rootElement);
        }

        public void StartTracking(List<VisualElement> elements)
        {
            if (!enable)
                return;

            // Some panels like the main Toolbar don't have any tracking set up
            if (panel.contextType == ContextType.Editor && m_EditorVisualTreeAssetTracker == null)
                return;

            VisualTreeAsset currentAsset = null;
            ILiveReloadAssetTracker<VisualTreeAsset> tracker = null;
            foreach (var ve in elements)
            {
                if (ve.visualTreeAssetSource != null)
                {
                    if (ve.visualTreeAssetSource != currentAsset)
                    {
                        currentAsset = ve.visualTreeAssetSource;
                        tracker = FindTracker(ve);
                    }
                    StartVisualTreeAssetTracking(tracker, ve.visualTreeAssetSource);
                }

                if (ve.styleSheetList?.Count > 0)
                {
                    foreach (var styleSheet in ve.styleSheetList)
                    {
                        m_LiveReloadStyleSheetAssetTracker.StartTrackingAsset(styleSheet);
                    }
                }
            }
        }

        public void StopTracking(List<VisualElement> elements)
        {
            // Some panels like the main Toolbar don't have any tracking set up
            if (panel.contextType == ContextType.Editor && m_EditorVisualTreeAssetTracker == null)
                return;

            VisualTreeAsset currentAsset = null;
            ILiveReloadAssetTracker<VisualTreeAsset> tracker = null;
            foreach (var ve in elements)
            {
                if (ve.visualTreeAssetSource != null)
                {
                    if (ve.visualTreeAssetSource != currentAsset)
                    {
                        currentAsset = ve.visualTreeAssetSource;
                        tracker = FindTracker(ve);
                    }
                    StopVisualTreeAssetTracking(tracker, ve.visualTreeAssetSource);
                }

                if (ve.styleSheetList?.Count > 0)
                {
                    foreach (var styleSheet in ve.styleSheetList)
                    {
                        m_LiveReloadStyleSheetAssetTracker.StopTrackingAsset(styleSheet);
                    }
                }
            }
        }

        public void StartStyleSheetAssetTracking(StyleSheet styleSheet)
        {
            m_LiveReloadStyleSheetAssetTracker.StartTrackingAsset(styleSheet);
        }

        public void StopStyleSheetAssetTracking(StyleSheet styleSheet)
        {
            m_LiveReloadStyleSheetAssetTracker.StopTrackingAsset(styleSheet);
        }

        public void OnStyleSheetAssetsImported(HashSet<StyleSheet> changedAssets, HashSet<string> deletedAssets)
        {
            m_LiveReloadStyleSheetAssetTracker.OnAssetsImported(changedAssets, deletedAssets);
        }

        private void StartVisualTreeAssetTracking(ILiveReloadAssetTracker<VisualTreeAsset> tracker, VisualTreeAsset asset)
        {
            int dirtyCount = tracker.StartTrackingAsset(asset);

            if (!m_AssetToTrackerMap.TryGetValue(asset, out var trackers))
            {
                trackers = new VisualTreeAssetToTrackMappingEntry()
                {
                    m_LastDirtyCount = dirtyCount,
                    m_LastElementCount = asset.visualElementAssets.Count,
                    m_LastInlinePropertiesCount = asset.inlineSheet.rules.Sum(r => r.properties.Length),
                    m_LastAttributePropertiesDirtyCount = asset.GetAttributePropertiesDirtyCount(),
                    m_Trackers = new HashSet<ILiveReloadAssetTracker<VisualTreeAsset>>()
                };
                m_AssetToTrackerMap[asset] = trackers;
            }
            trackers.m_Trackers.Add(tracker);
        }

        private void StopVisualTreeAssetTracking(ILiveReloadAssetTracker<VisualTreeAsset> tracker, VisualTreeAsset asset)
        {
            if (tracker == null)
            {
                return;
            }

            tracker.StopTrackingAsset(asset);

            if (m_AssetToTrackerMap.TryGetValue(asset, out var trackers))
            {
                trackers.m_Trackers.Remove(tracker);
                if (trackers.m_Trackers.Count == 0)
                {
                    m_AssetToTrackerMap.Remove(asset);
                }
            }
        }

        public void OnVisualTreeAssetsImported(HashSet<VisualTreeAsset> changedAssets, HashSet<string> deletedAssets)
        {
            if (panel.contextType == ContextType.Editor && m_EditorVisualTreeAssetTracker != null)
            {
                if (m_EditorVisualTreeAssetTracker.OnAssetsImported(changedAssets, deletedAssets))
                    m_TrackersToRefresh.Add(m_EditorVisualTreeAssetTracker);
            }
            else
            {
                foreach (var tracker in m_RuntimeVisualTreeAssetTrackers.Values)
                {
                    if (tracker.OnAssetsImported(changedAssets, deletedAssets))
                        m_TrackersToRefresh.Add(tracker);
                }
            }

            if (m_TrackersToRefresh.Count == 0)
                return;

            UIElementsUtility.InMemoryAssetsHaveBeenChanged();

            // Player panel require an update here or else it will only update when Unity is focused
            if (panel.contextType == ContextType.Player)
                Update();
        }

        private ILiveReloadAssetTracker<VisualTreeAsset> FindTracker(VisualElement ve)
        {
            if (panel.contextType == ContextType.Editor)
            {
                Debug.Assert(m_EditorVisualTreeAssetTracker != null, $"No editor tracker setup {(panel as Panel).name}");
                return m_EditorVisualTreeAssetTracker;
            }

            if (m_RuntimeVisualTreeAssetTrackers.Count == 1)
                return m_RuntimeVisualTreeAssetTrackers.Single().Value;

            if (m_RuntimeVisualTreeAssetTrackers.TryGetValue(ve, out var tracker))
                return tracker;

            var parent = ve.hierarchy.parent;
            while (parent != null)
            {
                if (m_RuntimeVisualTreeAssetTrackers.TryGetValue(parent, out var parentTracker))
                    return parentTracker;

                parent = parent.hierarchy.parent;
            }

            return null;
        }

        public override void Update()
        {
            if (!enable)
            {
                return;
            }

            // Windows can decide to enable/disable live reload of text elements,
            // For example, the UI Builder will only refresh changes made to font assets and not the rest.
            if ((enabledTrackers & LiveReloadTrackers.Text) != 0)
            {
                UpdateTextElements();
            }

            // Windows can also decide to skip document live reload, and only do text elements.
            // The UI Builder will skip the live reload of hierarchy and styles, because it is already editing them.
            if ((enabledTrackers & LiveReloadTrackers.Document) == 0)
            {
                return;
            }

            // Early out: no tracker found for panel.
            if (m_EditorVisualTreeAssetTracker == null && m_RuntimeVisualTreeAssetTrackers.Count == 0)
            {
                return;
            }

            if (EditorApplication.isPlaying)
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
                var dirtyCount = EditorUtility.GetDirtyCount(trackedAsset);

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

                    // Add to list to make sure we only call each tracker only once.
                    if (shouldRecreateUI)
                    {
                        m_TrackersToRefresh.Add(tracker);
                    }
                }
            }

            foreach (var tracker in m_TrackersToRefresh)
            {
                tracker.OnTrackedAssetChanged();
            }
            m_TrackersToRefresh.Clear();

            if (shouldRefreshStyles || m_LiveReloadStyleSheetAssetTracker.CheckTrackedAssetsDirty())
            {
                panel.DirtyStyleSheets();
                panel.UpdateInlineStylesRecursively();
            }
        }

        public void RegisterTextElement(TextElement element)
        {
            if ((enabledTrackers & LiveReloadTrackers.Text) != 0)
            {
                m_TextElements.Add(element);
            }
        }

        public void UnregisterTextElement(TextElement element)
        {
            m_TextElements.Remove(element);
        }

        private void UpdateTextElements()
        {
            if (!m_HasAnyTextAssetChanged)
                return;

            m_HasAnyTextAssetChanged = false;

            foreach (var textElement in m_TextElements)
            {
                textElement.IncrementVersion(VersionChangeType.Layout | VersionChangeType.Repaint);
                textElement.uitkTextHandle.SetDirty();
            }
        }
    }
}
