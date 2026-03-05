// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;
using Object = System.Object;

namespace UnityEditor.UIElements
{
    internal class VisualTreeAssetChangeTrackerUpdater : BaseVisualTreeUpdater, ILiveReloadSystem
    {
        internal class VisualTreeAssetToTrackMappingEntry
        {
            public int m_LastDirtyCount;
            public HashSet<ILiveReloadAssetTracker<VisualTreeAsset>> m_Trackers;
        }

        internal class StyleSheetToTrackMappingEntry
        {
            public int m_LastDirtyCount;
            public HashSet<ILiveReloadAssetTracker<StyleSheet>> m_Trackers;
        }

        private static readonly string s_Description = "UIElements.UpdateAssetsInEditor";
        private static readonly ProfilerMarker s_ProfilerMarker = new ProfilerMarker(ProfilerCategory.UIToolkit, s_Description);
        public override ProfilerMarker profilerMarker => s_ProfilerMarker;

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

                m_EditorVisualTreeAssetTracker = null;
                m_RuntimeVisualTreeAssetTrackers.Clear();
                m_AssetToTrackerMap.Clear();
                m_StyleSheetToTrackerMap.Clear();
                m_VisualTreeAssetAuthoringTrackers.Clear();
                m_StyleSheetAuthoringTrackers.Clear();
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

        private const int kMinUpdateDelayMs = 1000; // TODO this should probably be a setting at some point
        private long m_LastUpdateTimeMs = 0;

        // m_AssetToTrackerMap is used for faster access to information as needed.
        // Having the information indexed by asset allows quick access to the trackers keeping tabs on them
        // so that we can check assets for being dirty only once per Update call (instead of potentially multiple times
        // if there are multiple trackers looking at the same asset - e.g. life bars on a game).
        internal Dictionary<VisualTreeAsset, VisualTreeAssetToTrackMappingEntry> m_AssetToTrackerMap = new Dictionary<VisualTreeAsset, VisualTreeAssetToTrackMappingEntry>();

        // Similar to m_AssetToTrackerMap, but for StyleSheet tracking.
        internal Dictionary<StyleSheet, StyleSheetToTrackMappingEntry> m_StyleSheetToTrackerMap = new Dictionary<StyleSheet, StyleSheetToTrackMappingEntry>();

        // List to help with the Update() and avoid creating and destroying the list.
        private HashSet<ILiveReloadAssetTracker<VisualTreeAsset>> m_TrackersToRefresh = new HashSet<ILiveReloadAssetTracker<VisualTreeAsset>>();
        private HashSet<ILiveReloadAssetTracker<StyleSheet>> m_StyleSheetTrackersToRefresh = new HashSet<ILiveReloadAssetTracker<StyleSheet>>();

        // Contains assets that were changed through code.
        private readonly HashSet<VisualTreeAsset> m_ChangedVisualTreeAssets = new HashSet<VisualTreeAsset>();
        private readonly HashSet<StyleSheet> m_ChangedStyleSheets = new HashSet<StyleSheet>();

        // Registered trackers for each type
        private HashSet<ILiveReloadAssetTracker<VisualTreeAsset>> m_VisualTreeAssetAuthoringTrackers = new HashSet<ILiveReloadAssetTracker<VisualTreeAsset>>();
        private HashSet<ILiveReloadAssetTracker<StyleSheet>> m_StyleSheetAuthoringTrackers = new HashSet<ILiveReloadAssetTracker<StyleSheet>>();

        private ILiveReloadAssetTracker<StyleSheet> m_LiveReloadStyleSheetAssetTracker;

        // Depending on the panel context type either m_EditorVisualTreeAssetTracker or m_RuntimeVisualTreeAssetTrackers will be set.
        // For the editor, only one tracker is needed for the whole window.
        // For runtime, there's one tracker per UIDocument. Tracker are registered with the root VisualElement they belong to.
        private ILiveReloadAssetTracker<VisualTreeAsset> m_EditorVisualTreeAssetTracker;
        internal Dictionary<VisualElement, ILiveReloadAssetTracker<VisualTreeAsset>> m_RuntimeVisualTreeAssetTrackers = new Dictionary<VisualElement, ILiveReloadAssetTracker<VisualTreeAsset>>();

        public bool enable { get; set; }

        public LiveReloadTrackers enabledTrackers { get; set; } = LiveReloadTrackers.All;

        // For testing purposes.
        internal bool AnyTrackedStyleSheetsChangedThroughCode()
        {
            return m_ChangedStyleSheets.Count > 0;
        }

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
            if (m_RuntimeVisualTreeAssetTrackers.TryGetValue(rootElement, out var tracker))
            {
                // Remove template container to clean up uxml asset tracking
                if (rootElement is TemplateContainer container && container.templateSource != null)
                    StopVisualTreeAssetTracking(tracker, container.templateSource);
            }
            m_RuntimeVisualTreeAssetTrackers.Remove(rootElement);
        }

        /// <summary>
        /// Registers a tracker for a specific VisualTreeAsset.
        /// Multiple trackers can be registered for the same asset.
        /// </summary>
        public void RegisterAuthoringTrackerForAsset(IAuthoringLiveReloadAssetTracker<VisualTreeAsset> tracker, VisualTreeAsset asset)
        {
            if (tracker == null || asset == null)
                return;

            m_VisualTreeAssetAuthoringTrackers.Add(tracker);
            StartVisualTreeAssetTracking(tracker, asset);
        }

        /// <summary>
        /// Unregisters a tracker from a specific VisualTreeAsset.
        /// </summary>
        public void UnregisterAuthoringTrackerForAsset(IAuthoringLiveReloadAssetTracker<VisualTreeAsset> tracker, VisualTreeAsset asset)
        {
            if (tracker == null || asset == null)
                return;

            StopVisualTreeAssetTracking(tracker, asset);

            // Only remove from registered trackers if it's not tracking any other assets
            if (!IsTrackerActive(tracker))
                m_VisualTreeAssetAuthoringTrackers.Remove(tracker);
        }

        /// <summary>
        /// Registers a tracker for a specific StyleSheet.
        /// Multiple trackers can be registered for the same asset.
        /// </summary>
        public void RegisterAuthoringTrackerForAsset(IAuthoringLiveReloadAssetTracker<StyleSheet> tracker, StyleSheet asset)
        {
            if (tracker == null || asset == null)
                return;

            m_StyleSheetAuthoringTrackers.Add(tracker);
            StartStyleSheetAssetTracking(tracker, asset);
        }

        /// <summary>
        /// Unregisters a tracker from a specific StyleSheet.
        /// </summary>
        public void UnregisterAuthoringTrackerForAsset(IAuthoringLiveReloadAssetTracker<StyleSheet> tracker, StyleSheet asset)
        {
            if (tracker == null || asset == null)
                return;

            StopStyleSheetAssetTracking(tracker, asset);

            // Only remove from registered trackers if it's not tracking any other assets
            if (!IsTrackerActive(tracker))
                m_StyleSheetAuthoringTrackers.Remove(tracker);
        }

        private bool IsTrackerActive(ILiveReloadAssetTracker<VisualTreeAsset> tracker)
        {
            foreach (var entry in m_AssetToTrackerMap.Values)
            {
                if (entry.m_Trackers.Contains(tracker))
                    return true;
            }
            return false;
        }

        private bool IsTrackerActive(ILiveReloadAssetTracker<StyleSheet> tracker)
        {
            foreach (var entry in m_StyleSheetToTrackerMap.Values)
            {
                if (entry.m_Trackers.Contains(tracker))
                    return true;
            }
            return false;
        }

        public void StartTracking(List<VisualElement> elements)
        {
            // Some panels like the main Toolbar don't have any tracking set up
            var enabledVtaTracking = enable && !(panel.contextType == ContextType.Editor && m_EditorVisualTreeAssetTracker == null);

            VisualTreeAsset currentAsset = null;
            ILiveReloadAssetTracker<VisualTreeAsset> tracker = null;
            foreach (var ve in elements)
            {
                if (enabledVtaTracking)
                {
                    if (ve.visualTreeAssetSource != null)
                    {
                        if (ve.visualTreeAssetSource != currentAsset)
                        {
                            currentAsset = ve.visualTreeAssetSource;
                            tracker = FindTracker(ve);
                        }

                        if (tracker != null)
                            StartVisualTreeAssetTracking(tracker, ve.visualTreeAssetSource);
                    }
                }

                if (ve.styleSheetList?.Count > 0)
                {
                    foreach (var styleSheet in ve.styleSheetList)
                    {
                        StartStyleSheetAssetTracking(m_LiveReloadStyleSheetAssetTracker, styleSheet);
                    }
                }

                if (ve.hasInlineStyle && ve.inlineStyleAccess.inlineRule.rule != null)
                {
                    var styleSheet = ve.inlineStyleAccess.inlineRule.sheet;
                    StartStyleSheetAssetTracking(m_LiveReloadStyleSheetAssetTracker, styleSheet);
                }
            }
        }

        public void StopTracking(List<VisualElement> elements)
        {
            // Some panels like the main Toolbar don't have any tracking set up
            var enabledVtaTracking = !(panel.contextType == ContextType.Editor && m_EditorVisualTreeAssetTracker == null);

            VisualTreeAsset currentAsset = null;
            ILiveReloadAssetTracker<VisualTreeAsset> tracker = null;
            foreach (var ve in elements)
            {
                if (enabledVtaTracking)
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
                }

                if (ve.styleSheetList?.Count > 0)
                {
                    foreach (var styleSheet in ve.styleSheetList)
                    {
                        m_LiveReloadStyleSheetAssetTracker.StopTrackingAsset(styleSheet);
                        StopStyleSheetAssetTracking(m_LiveReloadStyleSheetAssetTracker, styleSheet);
                    }
                }

                if (ve.hasInlineStyle && ve.inlineStyleAccess.inlineRule.rule != null)
                {
                    var styleSheet = ve.inlineStyleAccess.inlineRule.sheet;
                    m_LiveReloadStyleSheetAssetTracker.StopTrackingAsset(styleSheet);
                    StopStyleSheetAssetTracking(m_LiveReloadStyleSheetAssetTracker, styleSheet);
                }
            }
        }

        public void StartStyleSheetAssetTracking(StyleSheet styleSheet)
        {
            StartStyleSheetAssetTracking(m_LiveReloadStyleSheetAssetTracker, styleSheet);
        }

        public void StopStyleSheetAssetTracking(StyleSheet styleSheet)
        {
            StopStyleSheetAssetTracking(m_LiveReloadStyleSheetAssetTracker, styleSheet);
        }

        public void OnVisualTreeAssetChanged(VisualTreeAsset visualTreeAsset)
        {
            if (m_AssetToTrackerMap.ContainsKey(visualTreeAsset))
            {
                m_ChangedVisualTreeAssets.Add(visualTreeAsset);
            }
        }

        public void OnStyleSheetChanged(List<StyleSheet> styleSheets)
        {
            foreach (var styleSheet in styleSheets)
            {
                // Check legacy tracker
                if (m_LiveReloadStyleSheetAssetTracker != null && m_LiveReloadStyleSheetAssetTracker.IsTrackingAsset(styleSheet))
                {
                    m_ChangedStyleSheets.Add(styleSheet);
                }
                // Check if any registered trackers are tracking this StyleSheet
                else if (m_StyleSheetToTrackerMap.ContainsKey(styleSheet))
                {
                    m_ChangedStyleSheets.Add(styleSheet);
                }
            }
        }

        public void OnStyleSheetChanged(List<string> styleSheetPaths)
        {
            foreach (var styleSheetPath in styleSheetPaths)
            {
                // Check legacy tracker
                if (m_LiveReloadStyleSheetAssetTracker != null && m_LiveReloadStyleSheetAssetTracker.IsTrackingAsset(styleSheetPath))
                {
                    m_ChangedStyleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(styleSheetPath));
                }
                else
                {
                    // Check if any registered trackers are tracking this StyleSheet by path
                    foreach (var tracker in m_StyleSheetAuthoringTrackers)
                    {
                        if (tracker.IsTrackingAsset(styleSheetPath))
                        {
                            m_ChangedStyleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(styleSheetPath));
                            break;
                        }
                    }
                }
            }
        }

        public void OnStyleSheetAssetsImported(HashSet<StyleSheet> changedAssets, HashSet<string> deletedAssets)
        {
            // Notify legacy tracker if it exists
            if (m_LiveReloadStyleSheetAssetTracker != null)
            {
                m_LiveReloadStyleSheetAssetTracker.OnAssetsImported(changedAssets, deletedAssets);
            }

            // Notify all registered StyleSheet trackers
            foreach (var tracker in m_StyleSheetAuthoringTrackers)
            {
                if (tracker.OnAssetsImported(changedAssets, deletedAssets))
                    m_StyleSheetTrackersToRefresh.Add(tracker);
            }

            // Notify trackers that need refreshing
            if (m_StyleSheetTrackersToRefresh.Count > 0)
            {
                foreach (var tracker in m_StyleSheetTrackersToRefresh)
                {
                    tracker.OnTrackedAssetChanged();
                }
                m_StyleSheetTrackersToRefresh.Clear();

                panel.DirtyStyleSheets();
                panel.UpdateInlineStylesRecursively();
            }
        }

        private void StartVisualTreeAssetTracking([NotNull] ILiveReloadAssetTracker<VisualTreeAsset> tracker, VisualTreeAsset asset)
        {
            int dirtyCount = tracker.StartTrackingAsset(asset);

            if (!m_AssetToTrackerMap.TryGetValue(asset, out var trackers))
            {
                using var _ = ListPool<UxmlAsset>.Get(out var list);
                list.AddRange(asset.DepthFirstTraversal());

                trackers = new VisualTreeAssetToTrackMappingEntry()
                {
                    m_LastDirtyCount = dirtyCount,
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

        private void StartStyleSheetAssetTracking([NotNull] ILiveReloadAssetTracker<StyleSheet> tracker, StyleSheet asset)
        {
            int dirtyCount = tracker.StartTrackingAsset(asset);

            if (!m_StyleSheetToTrackerMap.TryGetValue(asset, out var trackers))
            {
                trackers = new StyleSheetToTrackMappingEntry()
                {
                    m_LastDirtyCount = dirtyCount,
                    m_Trackers = new HashSet<ILiveReloadAssetTracker<StyleSheet>>()
                };
                m_StyleSheetToTrackerMap[asset] = trackers;
            }

            trackers.m_Trackers.Add(tracker);
        }

        private void StopStyleSheetAssetTracking(ILiveReloadAssetTracker<StyleSheet> tracker, StyleSheet asset)
        {
            if (tracker == null)
            {
                return;
            }

            tracker.StopTrackingAsset(asset);

            if (m_StyleSheetToTrackerMap.TryGetValue(asset, out var trackers))
            {
                trackers.m_Trackers.Remove(tracker);
                if (trackers.m_Trackers.Count == 0)
                {
                    m_StyleSheetToTrackerMap.Remove(asset);
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

            // Also notify all authoring VisualTreeAsset trackers
            foreach (var tracker in m_VisualTreeAssetAuthoringTrackers)
            {
                if (tracker.OnAssetsImported(changedAssets, deletedAssets))
                    m_TrackersToRefresh.Add(tracker);
            }

            if (m_TrackersToRefresh.Count == 0)
                return;

            if (changedAssets != null)
                foreach (var visualTreeAsset in changedAssets)
                {
                    UIElementsUtility.MarkVisualTreeAssetAsChanged(visualTreeAsset);
                    UIElementsUtility.MarkStyleSheetAsChanged(visualTreeAsset.inlineSheet);
                }

            // Player panel require an update here or else it will only update when Unity is focused
            if (panel.contextType == ContextType.Player)
                panel.UpdateAssetTrackers();
        }

        private ILiveReloadAssetTracker<VisualTreeAsset> FindTracker(VisualElement ve)
        {
            if (panel.contextType == ContextType.Editor)
            {
                Debug.Assert(m_EditorVisualTreeAssetTracker != null, $"No editor tracker setup {(panel as Panel).name}");
                return m_EditorVisualTreeAssetTracker;
            }

            if (m_RuntimeVisualTreeAssetTrackers.Count == 1)
                #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                return m_RuntimeVisualTreeAssetTrackers.Single().Value;
#pragma warning restore UA2001

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
            UpdateTextElements();

            if (EditorApplication.isPlaying)
            {
                long currentTimeMs = panel.TimeSinceStartupMs();

                if (currentTimeMs < m_LastUpdateTimeMs + kMinUpdateDelayMs)
                {
                    return;
                }

                m_LastUpdateTimeMs = currentTimeMs;
            }

            UpdateDocuments();
            UpdateStyleSheets();
        }

        private void UpdateTextElements()
        {
            // Windows can decide to enable/disable live reload of text elements,
            // For example, the UI Builder will only refresh changes made to font assets and not the rest.
            if (!enable ||
                (enabledTrackers & LiveReloadTrackers.Text) == 0 ||
                !m_HasAnyTextAssetChanged)
                return;

            m_HasAnyTextAssetChanged = false;

            if (!panel.textElementRegistry.IsValueCreated)
                return;

            foreach (var textElement in panel.textElementRegistry.Value)
            {
                textElement.IncrementVersion(VersionChangeType.Layout | VersionChangeType.Repaint);
                textElement.uitkTextHandle.SetDirty();
            }
        }

        private void UpdateDocuments()
        {
            // Individual windows can enable/disable live reload of the VisualTreeAsset documents tracked by the window.
            var shouldLiveReloadWindow = enable && (enabledTrackers & LiveReloadTrackers.Document) != 0;

            // Authoring tool can also opt-in to receive live reload events for VisualTreeAssets that are not specifically
            // tracked by the window.
            var shouldLiveReloadAuthoring = m_VisualTreeAssetAuthoringTrackers.Count > 0;

            if (!shouldLiveReloadWindow && !shouldLiveReloadAuthoring)
                return;

            // InMemoryAssets versions are split into two categories to help track what needs recreating.
            var shouldRecreateUI = m_ChangedVisualTreeAssets.Count > 0;

            // Early out: no tracker found for panel.
            if (m_EditorVisualTreeAssetTracker == null &&
                m_RuntimeVisualTreeAssetTrackers.Count == 0 &&
                m_VisualTreeAssetAuthoringTrackers.Count == 0 &&
                // There's no need to reload anything if there are no changes.
                !shouldRecreateUI)
            {
                return;
            }

            // We iterate on the assets to avoid calling GetDirtyCount for the same asset more than once.
            // In Editor this seems very likely and in Runtime we're assuming there are not multiple panels going
            // around, or if there are they're not using the same UXMLs but we may have to revisit this if we ever
            // detect that to be the case (to once again avoid calling GetDirtyCount multiple times on the same asset).
            foreach (var trackedAssetEntry in m_AssetToTrackerMap)
            {
                var trackedAsset = trackedAssetEntry.Key;
                var trackersEntry = trackedAssetEntry.Value;
                var dirtyCount = EditorUtility.GetDirtyCount(trackedAsset);
                var assetDeemedChanged = false;

                if (dirtyCount != trackersEntry.m_LastDirtyCount)
                {
                    assetDeemedChanged = true;
                    trackersEntry.m_LastDirtyCount = dirtyCount;
                }

                if (m_ChangedVisualTreeAssets.Contains(trackedAsset))
                    assetDeemedChanged = true;

                if (!assetDeemedChanged)
                    continue;

                foreach (var tracker in trackersEntry.m_Trackers)
                {
                    var isAuthoringTracker = tracker is IAuthoringLiveReloadAssetTracker;
                    if (shouldLiveReloadWindow && !isAuthoringTracker)
                        m_TrackersToRefresh.Add(tracker);

                    if (shouldLiveReloadAuthoring && isAuthoringTracker)
                        m_TrackersToRefresh.Add(tracker);
                }
            }

            foreach (var tracker in m_TrackersToRefresh)
            {
                tracker.OnTrackedAssetChanged();
            }

            m_TrackersToRefresh.Clear();
            m_ChangedVisualTreeAssets.Clear();
        }

        private void UpdateStyleSheets()
        {
            var shouldLiveReloadWindow = (enabledTrackers & LiveReloadTrackers.StyleSheet) != 0;
            var shouldLiveReloadAuthoring = m_StyleSheetAuthoringTrackers.Count > 0;

            if (!shouldLiveReloadWindow && !shouldLiveReloadAuthoring)
                return;

            var shouldRefreshStyles = m_ChangedStyleSheets.Count > 0;

            // Check all registered StyleSheet trackers
            foreach (var trackedStyleSheetEntry in m_StyleSheetToTrackerMap)
            {
                var trackedStyleSheet = trackedStyleSheetEntry.Key;
                var trackersEntry = trackedStyleSheetEntry.Value;
                var dirtyCount = EditorUtility.GetDirtyCount(trackedStyleSheet);

                if (dirtyCount != trackersEntry.m_LastDirtyCount)
                {
                    trackersEntry.m_LastDirtyCount = dirtyCount;
                    shouldRefreshStyles = true;

                    foreach (var tracker in trackersEntry.m_Trackers)
                    {
                        var isAuthoringTracker = tracker is IAuthoringLiveReloadAssetTracker;
                        if (shouldLiveReloadWindow && !isAuthoringTracker)
                            m_StyleSheetTrackersToRefresh.Add(tracker);

                        if (shouldLiveReloadAuthoring && isAuthoringTracker)
                            m_StyleSheetTrackersToRefresh.Add(tracker);
                    }
                }
            }

            // Notify all trackers that need refreshing
            foreach (var tracker in m_StyleSheetTrackersToRefresh)
            {
                tracker.OnTrackedAssetChanged();
            }
            m_StyleSheetTrackersToRefresh.Clear();

            if (shouldRefreshStyles)
            {
                panel.DirtyStyleSheets();
                panel.UpdateInlineStylesRecursively();
            }

            m_ChangedStyleSheets.Clear();
        }


        public bool AnyStyleSheetMarkedDirtyAfterUndo()
        {
            var any = false;

            foreach (var (styleSheet, tracker) in m_StyleSheetToTrackerMap)
            {
                if (EditorUtility.GetDirtyCount(styleSheet) != tracker.m_LastDirtyCount)
                {
                    any = true;
                    styleSheet.RequestRebuild();
                }
            }

            if (any)
            {
                panel.DirtyStyleSheets();
                panel.UpdateInlineStylesRecursively();
            }

            return any;
        }
    }
}
