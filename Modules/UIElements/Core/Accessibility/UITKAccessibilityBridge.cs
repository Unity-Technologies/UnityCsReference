// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.Accessibility;
using UnityEngine.Pool;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Automatically generates an <see cref="AccessibilityHierarchy"/> from the visual trees of
    /// participating runtime panels, activates it when the screen reader turns on, and keeps it in
    /// sync with the UI as it changes.
    /// </summary>
    /// <remarks>
    /// The bridge only produces an <see cref="AccessibilityHierarchy"/>; the platform adapters
    /// below <see cref="AssistiveSupport.activeHierarchy"/> handle the native side.
    ///
    /// Everything sits behind <see cref="featureEnabled"/>, off by default. While off, the bridge
    /// never subscribes nor allocates, and no panel carries a
    /// <see cref="VisualTreeAccessibilityUpdater"/>. Panel components also carry a serialized
    /// opt-out gate, evaluated at registration.
    ///
    /// A built hierarchy is retained and kept in sync even while the screen reader is off, so
    /// turning it back on only re-assigns it. Live sync is hybrid: the updater forwards
    /// version-bit changes and flushes once per frame after layout; leaf data arrives through
    /// per-element event hooks, disabled/displayed flips through their single engine code paths.
    ///
    /// The bridge also manages exposure — the panel cover and out-of-view hiding compose on
    /// <see cref="AccessibilityNode.isActive"/> — and places open dropdown menus' nodes under
    /// their field's node.
    ///
    /// The bridge yields to manual authoring: it never overwrites a developer-assigned active
    /// hierarchy, and it only mutates nodes it created.
    /// </remarks>
    internal static partial class UITKAccessibilityBridge
    {
        [AutoStaticsCleanupOnCodeReload]
        static bool s_FeatureEnabled;

        [AutoStaticsCleanupOnCodeReload]
        // Derived state: UpdateLiveState() recomputes it from the feature flag and the hierarchy every
        // time either changes, so the value cleared on reload is re-derived rather than carried over.
        [IgnoreForUAL0015("Derived flag recomputed by UpdateLiveState() from the current feature flag and hierarchy")]
        static bool s_IsLive;

        [AutoStaticsCleanupOnCodeReload]
        static bool s_Subscribed;

        [AutoStaticsCleanupOnCodeReload]
        static readonly List<IPanelComponent> s_ParticipatingComponents = new();

        // The panels currently hosting participating components. Membership decides which panels
        // carry a VisualTreeAccessibilityUpdater (installed and removed by RefreshTrackedPanels)
        // and lets the updater reject a stale panel with a set lookup.
        [AutoStaticsCleanupOnCodeReload]
        static HashSet<BaseVisualElementPanel> s_TrackedPanels = new();

        // Scratch for RefreshTrackedPanels: holds the previous tracked set for the duration of a
        // refresh and is left empty between refreshes.
        [AutoStaticsCleanupOnCodeReload]
        static HashSet<BaseVisualElementPanel> s_PreviousTrackedPanels = new();

        [AutoStaticsCleanupOnCodeReload]
        static readonly AccessibilityNodeMap s_NodeMap = new();

        // Each top-level component's root-level nodes, in reading order. A structural change
        // with no mapped Container ancestor (the common case) regenerates the component's root
        // segment; these lists say where that segment starts and what belongs to it.
        [AutoStaticsCleanupOnCodeReload]
        static readonly Dictionary<IPanelComponent, List<AccessibilityNode>> s_ComponentRootSegments = new();

        // The node segments generated for open dropdown menus. A menu's segment sits under its
        // field's node when the target is mapped; otherwise, it is appended after the component
        // segments at the root level (topmost content reads last).
        [AutoStaticsCleanupOnCodeReload]
        static readonly Dictionary<GenericDropdownMenu, List<AccessibilityNode>> s_OpenDropdownMenus = new();

        // The open menus' top-level segment nodes, flattened: sweep-time membership checks walk
        // node ancestors against this set instead of scanning every segment list per ancestor.
        [AutoStaticsCleanupOnCodeReload]
        static readonly HashSet<AccessibilityNode> s_OpenDropdownSegmentNodes = new();

        // Version-bit-driven changes coalesce here between frames and are flushed per panel by the
        // accessibility updater, after layout settles. All pending state is computed against the
        // current generation: a full rebuild clears it wholesale.
        [AutoStaticsCleanupOnCodeReload]
        static readonly HashSet<VisualElement> s_PendingStructural = new();

        [AutoStaticsCleanupOnCodeReload]
        static readonly HashSet<VisualElement> s_PendingRelabel = new();

        [AutoStaticsCleanupOnCodeReload]
        static readonly HashSet<VisualElement> s_PendingInclusionChecks = new();

        [AutoStaticsCleanupOnCodeReload]
        static readonly HashSet<VisualElement> s_PendingFrameRefreshes = new();

        // The topmost element that covers its whole panel and blocks pointer input — a modal
        // backdrop. Nodes painted below it are hidden from the screen reader (isActive = false),
        // mirroring what a pointer can reach. Null when nothing covers.
        [AutoStaticsCleanupOnCodeReload]
        static VisualElement s_PanelCover;

        [AutoStaticsCleanupOnCodeReload]
        static bool s_PanelCoverDirty;

        // The nodes hidden by the current cover. isActive folds two reasons (cover and
        // out-of-view); this set lets geometry refreshes re-apply the cover part without
        // recomputing exposure per node. Empty while nothing covers.
        [AutoStaticsCleanupOnCodeReload]
        static readonly HashSet<AccessibilityNode> s_NodesHiddenByCover = new();

        // The cover's position in its tree: each ancestor with the child index leading down to
        // the cover. A flush under an unchanged cover re-derives exposure only for freshly
        // placed nodes, which is only sound while the cover has not moved; this cache proves
        // that in O(depth) per flush (see PanelCoverPathIsCurrent). Empty while nothing covers.
        [AutoStaticsCleanupOnCodeReload]
        static readonly List<(VisualElement ancestor, int branchIndex)> s_PanelCoverPath = new();

        // The nodes placed by this flush's regenerations while a cover is up — the scope of the
        // unchanged-cover exposure re-apply. Cleared at the end of every flush.
        [AutoStaticsCleanupOnCodeReload]
        static readonly HashSet<AccessibilityNode> s_FlushPlacedNodes = new();

        // Where the screen reader cursor was when the cover went up; dismissal sends it back
        // (the native modal convention). Captured at cover-raise, the last moment the cursor
        // still sits on the opener. Null while nothing covers or nothing needs restoring.
        //
        // An element, not a node: own-document dialogs rebuild the hierarchy on attach/detach,
        // and a rebuild replaces every node. That is also why this outlives ClearSyncState; the
        // node is re-resolved when the cursor is sent back.
        [AutoStaticsCleanupOnCodeReload]
        static VisualElement s_CursorElementBeforeCover;

        // The node whose cursor highlight needs redrawing: its element was scrolled into view
        // on arrival, but the platform sampled the old geometry before delivering the focus
        // change. Once the frame stops moving across flushes, one layout-changed notification
        // redraws the cursor — dropped if the cursor already moved on, because a stale
        // re-anchor yanks it backward. Only scrolled arrivals get here, one notification each.
        [AutoStaticsCleanupOnCodeReload]
        static VisualElement s_CursorReanchorElement;

        [AutoStaticsCleanupOnCodeReload]
        static AccessibilityNode s_CursorReanchorNode;

        // The frame observed at the previous flush, for the stability wait.
        [AutoStaticsCleanupOnCodeReload]
        static Rect s_CursorReanchorLastFrame;

        [AutoStaticsCleanupOnCodeReload]
        static int s_CursorReanchorFlushesLeft;

        // How many frame-moving flushes the settle wait tolerates before the notification is
        // forced out; the first quiet flush ends the wait early. Two covers the deferred
        // nested-scroll case, doubled for slack — and deliberately far below ScrollView's
        // 60-frame deferred-retry cap: a cursor redrawn early at a near-final position beats one
        // redrawn a second late, and a stale redraw self-heals on the next cursor move.
        const int k_CursorReanchorSettleBudget = 4;

        [AutoStaticsCleanupOnCodeReload]
        static AccessibilityHierarchy s_Hierarchy;

        // Rebuild counter, exposed through <see cref="generation"/> so tests can assert whether a
        // full rebuild happened. The actual stale-update guards live elsewhere: pending sets are
        // cleared on rebuild, and the deferred rebuild schedule uses a last-scheduled-wins token.
        [AutoStaticsCleanupOnCodeReload]
        static int s_Generation;

        // Last-scheduled-wins token for deferred rebuilds: only the most recently scheduled item
        // rebuilds, and an item lost with a dying panel can never block future rebuilds (the
        // coalescing story lives on ScheduleRebuildAndActivate).
        [AutoStaticsCleanupOnCodeReload]
        static int s_ScheduleToken;

        // A cached delegate to a static method of this module: it holds no state and cannot pin
        // a user assembly across a code reload, so it is safe to persist.
        [NoAutoStaticsCleanup]
        static readonly EventCallback<PropertyChangedEvent> s_OnElementPropertyChanged = OnElementPropertyChanged;

        /// <summary>
        /// Whether hierarchies are built and kept in sync without a screen reader. On in the
        /// editor: no screen reader reaches it there, and the Accessibility Hierarchy Viewer
        /// should show the result as soon as play mode runs. Players keep screen-reader-driven
        /// activation. Settable so tests control activation explicitly.
        /// </summary>
        [AutoStaticsCleanupOnCodeReload] // Auto re-captures the initializer: the platform default is re-derived on reload.
        internal static bool activatesWithoutScreenReader { get; set; } = Application.isEditor;

        // Whether a hierarchy should exist right now: a screen reader is on, or activation is
        // platform-unconditional (the editor, for the Hierarchy Viewer).
        static bool shouldMaintainHierarchy =>
            AssistiveSupport.isScreenReaderEnabled || activatesWithoutScreenReader;

        /// <summary>
        /// The runtime gate for the whole bridge, off by default. Enabling it at runtime picks
        /// up panels that attached while it was off.
        /// </summary>
        internal static bool featureEnabled
        {
            get => s_FeatureEnabled;
            set
            {
                if (s_FeatureEnabled == value)
                    return;

                s_FeatureEnabled = value;

                if (value)
                    RegisterLivePanelComponents();
                else
                    Reset();

                UpdateLiveState();
            }
        }

        /// <summary>
        /// Whether a generated hierarchy exists and is being kept in sync. This is the cheap
        /// early-out the per-panel updaters read on every version change.
        /// </summary>
        internal static bool isLive => s_IsLive;

        internal static AccessibilityHierarchy hierarchy => s_Hierarchy;

        internal static int generation => s_Generation;

        internal static AccessibilityNodeMap nodeMap => s_NodeMap;

        internal static bool IsTrackedPanel(BaseVisualElementPanel panel) => s_TrackedPanels.Contains(panel);

        // In players, the project setting ships as a Resources TextAsset ("1" = enabled),
        // applied at the first panel attach. A boot hook is not an option
        // ([RuntimeInitializeOnLoadMethod] is banned in engine modules), and a ScriptableObject
        // cannot carry the setting (internal engine-module assets deserialize typeless in
        // players). In the editor, the play-mode hook applies it instead.
        internal const string playerConfigurationResourceName = "UIToolkitAccessibilitySettings";

        [AutoStaticsCleanupOnCodeReload]
        static bool s_PlayerConfigurationApplied;

        static void ApplyPlayerConfigurationOnce()
        {
            if (s_PlayerConfigurationApplied || Application.isEditor)
                return;

            s_PlayerConfigurationApplied = true;

            var configuration = Resources.Load<TextAsset>(playerConfigurationResourceName);
            if (configuration == null)
                return;

            featureEnabled = configuration.text.Trim() == "1";
        }

        /// <summary>
        /// Called by <see cref="PanelSettings"/> when a top-level panel component attaches —
        /// the registration entry where the per-component gate is evaluated. Opted-out or
        /// non-participating components (see <see cref="IsParticipatingPanel"/>) are never
        /// registered, so they are never walked.
        /// </summary>
        internal static void OnPanelComponentAttached(IPanelComponent panelComponent)
        {
            // Register nothing on platforms the Accessibility module does not support (the
            // editor is exempt: it feeds the Hierarchy Viewer and the tests). Otherwise a forced
            // screenReaderStatusOverride would build hierarchies nothing can consume and log an
            // assignment error per activation.
            if (!AssistiveSupport.isSupportedPlatform && !Application.isEditor)
                return;

            ApplyPlayerConfigurationOnce();

            if (!s_FeatureEnabled)
                return;

            if (!panelComponent.GetAccessibilityEnabled())
                return;

            if (!TryRegisterParticipant(panelComponent))
                return;

            // A retained hierarchy is kept in sync while the screen reader is off, so lifecycle
            // rebuilds also run in that state.
            if (shouldMaintainHierarchy || s_Hierarchy != null)
                ScheduleRebuildAndActivate(panelComponent);
        }

        // The participant bookkeeping rule — list membership, the status subscription and the
        // tracked-panel set — is owned here; the attach/detach and gate-flip entry points only
        // differ in how they react to a membership change.
        static bool TryRegisterParticipant(IPanelComponent panelComponent)
        {
            if (!IsParticipatingPanel(panelComponent.panelSettings) ||
                s_ParticipatingComponents.Contains(panelComponent))
                return false;

            s_ParticipatingComponents.Add(panelComponent);
            EnsureSubscribed();
            RefreshTrackedPanels();
            return true;
        }

        static bool TryUnregisterParticipant(IPanelComponent panelComponent)
        {
            if (!s_ParticipatingComponents.Remove(panelComponent))
                return false;

            RefreshTrackedPanels();
            return true;
        }

        /// <summary>
        /// Called by <see cref="PanelSettings"/> when a property deciding panel participation or
        /// cross-panel reading order changes on a live asset. Attached components re-evaluate
        /// and a rebuild is scheduled, so order-only changes are reflected too.
        /// </summary>
        internal static void OnPanelSettingsChanged(PanelSettings panelSettings)
        {
            if (!s_FeatureEnabled || panelSettings.m_AttachedPanelComponentsList == null)
                return;

            var participating = IsParticipatingPanel(panelSettings);
            IPanelComponent scheduleTrigger = null;

            // Iterate a copy: (de)registration must not observe list mutations mid-walk.
            var components = new List<IPanelComponent>(panelSettings.m_AttachedPanelComponentsList.m_AttachedPanelComponents);
            foreach (var component in components)
            {
                scheduleTrigger ??= component;

                if (participating)
                    OnPanelComponentAttached(component);
                else
                    OnPanelComponentDetached(component);
            }

            // Registration transitions schedule their own rebuild; an order-only change (sorting
            // order) does not, so make sure one lands whenever a hierarchy is in play.
            if (scheduleTrigger != null && (shouldMaintainHierarchy || s_Hierarchy != null))
                ScheduleRebuildAndActivate(scheduleTrigger);
        }

        /// <summary>
        /// Called by the per-component gate setters when the value changes on a live object.
        /// A top-level component (un)registers as on attach/detach; a nested document flips
        /// inclusion inside an already-walked tree, which is a structural change at its root.
        /// </summary>
        internal static void OnPanelComponentGateChanged(IPanelComponent panelComponent)
        {
            if (!s_FeatureEnabled)
                return;

            if (panelComponent.parentUI != null)
            {
                if (!s_IsLive)
                    return;

                if (panelComponent.GetRootVisualElement() is { } nestedRoot &&
                    nestedRoot.elementPanel is { } panel && s_TrackedPanels.Contains(panel))
                    s_PendingStructural.Add(nestedRoot);

                return;
            }

            // A gate flip happens on settled UI, unlike attach/detach, so no full rebuild: a
            // hierarchy swap tears down every native node and re-anchors the screen reader to
            // the first element. The component's root segment is added or removed in place,
            // leaving every other node (and the user's focus) alone.
            if (panelComponent.GetAccessibilityEnabled())
            {
                if (!TryRegisterParticipant(panelComponent))
                    return;

                if (s_Hierarchy == null)
                {
                    if (shouldMaintainHierarchy)
                        ScheduleRebuildAndActivate(panelComponent);
                    return;
                }

                // The updater's next flush inserts the segment at its reading-order position.
                if (panelComponent.GetRootVisualElement() is { } root && root.elementPanel != null)
                    s_PendingStructural.Add(root);
            }
            else
            {
                if (TryUnregisterParticipant(panelComponent))
                    RemoveComponentSegment(panelComponent);
            }
        }

        /// <summary>
        /// The open dropdown menu targeting the element, if any — the lookup behind collapsing a
        /// dropdown field by activating it again.
        /// </summary>
        internal static bool TryGetOpenDropdownMenu(VisualElement targetElement, out GenericDropdownMenu openMenu)
        {
            foreach (var menu in s_OpenDropdownMenus.Keys)
            {
                if (menu.targetElement == targetElement)
                {
                    openMenu = menu;
                    return true;
                }
            }

            openMenu = null;
            return false;
        }

        /// <summary>
        /// Called by <see cref="GenericDropdownMenu"/> when its container attaches to a panel.
        /// The menu mounts at the panel root, so the bridge places its nodes explicitly: under
        /// the field's node when the menu's target is mapped, else appended at the root level.
        /// Dropdowns are light-dismiss, not modal — the rest of the UI stays active — and the
        /// cursor stays on the field until the user walks into the menu.
        /// </summary>
        internal static void OnDropdownMenuOpened(GenericDropdownMenu menu)
        {
            if (!s_IsLive || s_OpenDropdownMenus.ContainsKey(menu))
                return;

            if (menu.menuContainer?.elementPanel is not { } panel || !s_TrackedPanels.Contains(panel))
                return;

            AccessibilityNode fieldNode = null;
            if (menu.targetElement != null)
                s_NodeMap.TryGetNode(menu.targetElement, out fieldNode);

            List<AccessibilityNode> segment;
            if (fieldNode != null)
            {
                // The field announces its menu is open; cleared when the menu closes, and
                // preserved by reusing regenerations (the generator treats Expanded as
                // bridge-owned session state).
                fieldNode.state |= AccessibilityState.Expanded;

                var siblings = fieldNode.children;
                var segmentStart = siblings.Count;
                var cursor = segmentStart;
                AccessibilityTreeGenerator.GenerateSubtree(menu.menuContainer, s_Hierarchy, fieldNode,
                    s_NodeMap, ref cursor, null, null);

                segment = CaptureSegment(siblings, segmentStart);
            }
            else
            {
                var segmentStart = s_Hierarchy.rootNodes.Count;
                AccessibilityTreeGenerator.GenerateSubtree(menu.menuContainer, s_Hierarchy, s_NodeMap);
                segment = CaptureSegment(s_Hierarchy.rootNodes, segmentStart);
            }

            s_OpenDropdownMenus[menu] = segment;
            s_OpenDropdownSegmentNodes.UnionWith(segment);

            foreach (var node in segment)
            {
                RegisterPropertyCallbacks(node);
            }

            if (AssistiveSupport.activeHierarchy == s_Hierarchy)
                AssistiveSupport.notificationDispatcher.SendLayoutChanged();
        }

        /// <summary>
        /// Called by <see cref="GenericDropdownMenu"/> when it hides; the menu's segment is
        /// removed in place.
        /// </summary>
        internal static void OnDropdownMenuClosed(GenericDropdownMenu menu)
        {
            if (!s_OpenDropdownMenus.Remove(menu, out var segment) || s_Hierarchy == null)
                return;

            // Called before the menu clears its target reference, so the field is still
            // reachable to fold its Expanded state back.
            AccessibilityNode fieldNode = null;
            if (menu.targetElement != null)
                s_NodeMap.TryGetNode(menu.targetElement, out fieldNode);

            if (fieldNode != null)
                fieldNode.state &= ~AccessibilityState.Expanded;

            // Captured before the removal below unmaps the menu's nodes: a cursor inside the
            // closing menu returns to the field (item picked or dismissed alike); a cursor
            // anywhere else stays where the user put it.
            var cursorWasInMenu = AccessibilityTreeGenerator.screenReaderFocusedElement is { } cursorElement &&
                menu.menuContainer.Contains(cursorElement);

            s_OpenDropdownSegmentNodes.ExceptWith(segment);

            foreach (var node in segment)
            {
                CleanupSubtreeMapping(node);
                if (s_Hierarchy.ContainsNode(node))
                    s_Hierarchy.RemoveNode(node, removeChildren: true);
            }

            if (AssistiveSupport.activeHierarchy != s_Hierarchy)
                return;

            if (cursorWasInMenu && fieldNode is { isActive: true })
                AssistiveSupport.notificationDispatcher.SendLayoutChanged(fieldNode);
            else
                AssistiveSupport.notificationDispatcher.SendLayoutChanged();
        }

        // Removes a component's root segment from the retained hierarchy in place; used by gate
        // flips, where a full rebuild would reset screen reader focus everywhere.
        static void RemoveComponentSegment(IPanelComponent panelComponent)
        {
            if (s_Hierarchy == null || !s_ComponentRootSegments.Remove(panelComponent, out var segment))
                return;

            foreach (var node in segment)
            {
                CleanupSubtreeMapping(node);
                s_Hierarchy.RemoveNode(node, removeChildren: true);
            }

            // The removed component may have carried the panel cover; the next flush recomputes.
            s_PanelCoverDirty = true;

            if (AssistiveSupport.activeHierarchy == s_Hierarchy)
                AssistiveSupport.notificationDispatcher.SendLayoutChanged();
        }

        /// <summary>
        /// Called by <see cref="PanelSettings"/> when a top-level panel component detaches from the
        /// visual tree.
        /// </summary>
        internal static void OnPanelComponentDetached(IPanelComponent panelComponent)
        {
            if (!TryUnregisterParticipant(panelComponent))
                return;

            if (shouldMaintainHierarchy || s_Hierarchy != null)
                ScheduleRebuildAndActivate(panelComponent);
        }

        // Attach-triggered rebuilds are deferred one update tick: component content routinely
        // arrives right after the attach, so a synchronous rebuild would snapshot empty roots
        // when the screen reader is already on at startup. Deferring also coalesces a startup
        // burst of attaches into one rebuild. The screen-reader-on path needs no rebuild: the
        // retained hierarchy is current.
        static void ScheduleRebuildAndActivate(IPanelComponent triggerComponent)
        {
            var token = ++s_ScheduleToken;
            var scheduled = false;

            void ScheduleOn(IPanelComponent component)
            {
                if (component?.GetRootVisualElement() is not { } root || root.elementPanel == null)
                    return;

                root.schedule.Execute(() =>
                {
                    if (token != s_ScheduleToken || !s_FeatureEnabled)
                        return;

                    // Consume the token so the copies scheduled on other panels become no-ops.
                    s_ScheduleToken++;
                    RebuildAndActivate();
                });

                scheduled = true;
            }

            // Schedule on every participating panel and let the first to tick rebuild: the
            // trigger's own panel is not guaranteed updates (on detach it may be mid-teardown),
            // and any live panel is equally past the attach-burst window.
            foreach (var component in s_ParticipatingComponents)
            {
                ScheduleOn(component);
            }

            // The trigger may have just been unregistered (detach, gate-off): its panel can still
            // carry the deferred rebuild when no other participant is live.
            if (!scheduled)
                ScheduleOn(triggerComponent);

            // No live panel to defer on (for example the last component just detached): rebuild
            // immediately.
            if (!scheduled)
                RebuildAndActivate();
        }

        // AssistiveSupport.activeHierarchy represents only the application's main window, so
        // only main-window screen-space overlay panels participate. Other displays, render
        // textures and world-space panels would merge nodes whose frames collide with
        // main-window content. Revisit when the platform display API work lands; 1:1
        // render-texture blits are a candidate for an explicit opt-in later. Live PanelSettings
        // changes re-evaluate through OnPanelSettingsChanged.
        static bool IsParticipatingPanel(PanelSettings panelSettings)
        {
            return panelSettings != null &&
                panelSettings.renderMode == PanelRenderMode.ScreenSpaceOverlay &&
                panelSettings.targetDisplay == 0 &&
                panelSettings.targetTexture == null;
        }

        static void EnsureSubscribed()
        {
            if (s_Subscribed)
                return;

            AssistiveSupport.screenReaderStatusChanged += OnScreenReaderStatusChanged;
            s_Subscribed = true;
        }

        internal static void OnScreenReaderStatusChanged(bool enabled)
        {
            if (!s_FeatureEnabled)
                return;

            // On "off", AssistiveSupport already clears the active assignment; retain-and-update
            // (see the class remarks) means the next "on" only re-assigns. The first "on" builds.
            if (!enabled)
            {
                // Cursor-derived state belongs to the ending screen reader session: the tracked
                // position (no focus-loss event will clear it), a pending re-anchor, and the
                // pre-cover position. Kept, they would answer for a cursor that no longer exists.
                AccessibilityTreeGenerator.ClearScreenReaderFocusedNode();
                ClearCursorReanchor();
                s_CursorElementBeforeCover = null;
                return;
            }

            if (s_Hierarchy == null)
                RebuildAndActivate();
            else
                ActivateIfUnclaimed(s_Hierarchy);
        }

        static void RebuildAndActivate()
        {
            var previousHierarchy = s_Hierarchy;

            // Menus still open across the rebuild re-register their segments afterwards;
            // ClearSyncState is about to forget them.
            var openMenus = new List<GenericDropdownMenu>(s_OpenDropdownMenus.Keys);

            s_Generation++;
            ClearSyncState();
            s_Hierarchy = new AccessibilityHierarchy();
            UpdateLiveState();

            foreach (var root in GetParticipatingRootsInReadingOrder())
            {
                var component = ((IPanelComponentRootElement)root).panelComponent;
                var segmentStart = s_Hierarchy.rootNodes.Count;
                var childState = AccessibilityFrameProjection.Descend(
                    AccessibilityFrameProjection.GetClipStateAbove(root), root);

                for (var i = 0; i < root.hierarchy.childCount; i++)
                {
                    var cursor = s_Hierarchy.rootNodes.Count;
                    AccessibilityTreeGenerator.GenerateSubtree(root.hierarchy[i], s_Hierarchy, null, s_NodeMap, ref cursor, null, null, childState);
                }

                s_ComponentRootSegments[component] = CaptureSegment(s_Hierarchy.rootNodes, segmentStart);
            }

            foreach (var element in s_NodeMap.elements)
            {
                element.RegisterCallback<PropertyChangedEvent>(s_OnElementPropertyChanged);
            }

            RefreshTrackedPanels();

            foreach (var menu in openMenus)
            {
                if (menu.menuContainer?.elementPanel != null)
                    OnDropdownMenuOpened(menu);
            }

            // Nodes are created exposed; a cover already up (a rebuild under an open dialog) must
            // hide the content below it before the native tree is built from the node data.
            // ClearSyncState dropped the previous cover, so a found cover registers as a change.
            s_PanelCoverDirty = true;
            UpdatePanelCover(structurallyChanged: false);

            // Yield to a hierarchy the developer assigned manually; only replace nothing or our own
            // previous generation.
            ActivateIfUnclaimed(previousHierarchy);

            RestoreCursorAfterRebuild();
        }

        // Activation sends an untargeted screen-changed notification, and the platform parks
        // the cursor on the first node. Right for a new screen — wrong for a rebuild, which is
        // the same UI re-derived: without this, closing an own-document dialog drops the cursor
        // at the top of the UI instead of on the opener.
        static void RestoreCursorAfterRebuild()
        {
            // The tracked cursor node belongs to the generation just discarded; re-point it at the
            // node its element maps to now (or forget it, when the element is no longer
            // represented) before anything reads it.
            AccessibilityNode trackedNode = null;
            if (AccessibilityTreeGenerator.screenReaderFocusedElement is { } trackedElement)
            {
                s_NodeMap.TryGetNode(trackedElement, out trackedNode);
                AccessibilityTreeGenerator.RetrackScreenReaderFocusedNode(trackedNode);
            }

            if (AssistiveSupport.activeHierarchy != s_Hierarchy)
                return;

            // A cover is up — either the rebuild is what put it there (a dialog opening) or it
            // happened underneath one. Either way the dialog owns the cursor, and the pre-cover
            // position waits for its dismissal.
            if (s_PanelCover != null)
                return;

            // A dismissed cover returns the cursor to whatever opened it; failing that, the cursor
            // stays on its own element. Both are the same request: put the cursor back where the
            // user had it.
            var nodeToFocus = TakeCursorNodeToRestore() ??
                (trackedNode is { isActive: true } ? trackedNode : null);

            if (nodeToFocus != null)
                AssistiveSupport.notificationDispatcher.SendLayoutChanged(nodeToFocus);
        }

        static void ActivateIfUnclaimed(AccessibilityHierarchy replaceable)
        {
            // Assigning while the screen reader is off is a silent no-op in players (and an error
            // on unsupported platforms); the editor accepts it for the Hierarchy Viewer.
            if (!AssistiveSupport.isScreenReaderEnabled && !Application.isEditor)
                return;

            var activeHierarchy = AssistiveSupport.activeHierarchy;

            // Already active: re-assigning would tear down and rebuild the native tree for nothing.
            if (activeHierarchy == s_Hierarchy)
                return;

            if (activeHierarchy == null || activeHierarchy == replaceable)
#pragma warning disable UAL0018 // the capturing field (AccessibilityHierarchyService.s_ActiveHierarchy) is itself cleaned on code reload, so the reference cannot go stale; Reset() also clears activeHierarchy back to null before s_Hierarchy is reset, and this method rebuilds and reassigns s_Hierarchy on the next activation
                AssistiveSupport.activeHierarchy = s_Hierarchy;
#pragma warning restore UAL0018
        }

        static List<AccessibilityNode> CaptureSegment(IReadOnlyList<AccessibilityNode> siblings, int segmentStart)
        {
            var segment = new List<AccessibilityNode>(siblings.Count - segmentStart);
            for (var i = segmentStart; i < siblings.Count; i++)
            {
                segment.Add(siblings[i]);
            }

            return segment;
        }

        // Roots come from the sorted player panels and, within a panel, from the visual tree
        // sibling order of the component roots, so reading order follows depth-first tree order.
        static List<VisualElement> GetParticipatingRootsInReadingOrder()
        {
            var roots = new List<VisualElement>();

            foreach (var panel in UIElementsRuntimeUtility.GetSortedPlayerPanels())
            {
                // Indexed rather than enumerated: this runs on the flush path (cover updates on
                // structural changes), and Children() boxes the list enumerator.
                var panelRoot = panel.visualTree.hierarchy;
                for (var i = 0; i < panelRoot.childCount; i++)
                {
                    if (panelRoot[i] is IPanelComponentRootElement rootElement &&
                        s_ParticipatingComponents.Contains(rootElement.panelComponent))
                        roots.Add(panelRoot[i]);
                }
            }

            return roots;
        }

        // Catches up on panels that attached while the feature was off (the boot path enables the
        // feature before any panel exists, so this mainly serves runtime/test toggling).
        static void RegisterLivePanelComponents()
        {
            foreach (var panel in UIElementsRuntimeUtility.GetSortedPlayerPanels())
            {
                if (panel is not RuntimePanel runtimePanel)
                    continue;

                // Iterate a copy: registration must not observe list mutations mid-walk.
                var components = new List<IPanelComponent>(runtimePanel.panelComponents);
                foreach (var component in components)
                {
                    OnPanelComponentAttached(component);
                }
            }
        }

        static void RefreshTrackedPanels()
        {
            // Swap instead of copying: the old tracked set becomes this refresh's previous
            // view and the empty scratch becomes the set being rebuilt.
            (s_PreviousTrackedPanels, s_TrackedPanels) = (s_TrackedPanels, s_PreviousTrackedPanels);

            foreach (var component in s_ParticipatingComponents)
            {
                if (component.GetRootVisualElement()?.elementPanel is { } panel)
                    s_TrackedPanels.Add(panel);
            }

            // Panels carry an accessibility updater only while tracked: updaters run on every
            // version change of every element, so untracked panels must not pay even an
            // early-out call.
            foreach (var panel in s_TrackedPanels)
            {
                s_PreviousTrackedPanels.Remove(panel);

                if (panel.GetUpdater(VisualTreeUpdatePhase.Accessibility) == null)
                    panel.SetUpdater(new VisualTreeAccessibilityUpdater(), VisualTreeUpdatePhase.Accessibility);
            }

            // Only the no-longer-tracked panels remain.
            foreach (var panel in s_PreviousTrackedPanels)
            {
                // A disposed panel took its updater down with its updater table.
                if (!panel.disposed)
                    panel.SetUpdater(null, VisualTreeUpdatePhase.Accessibility);
            }

            s_PreviousTrackedPanels.Clear();
        }

        static void UpdateLiveState()
        {
            s_IsLive = s_FeatureEnabled && s_Hierarchy != null;
        }

        static void ClearSyncState()
        {
            foreach (var element in s_NodeMap.elements)
            {
                element.UnregisterCallback<PropertyChangedEvent>(s_OnElementPropertyChanged);
            }

            s_NodeMap.Clear();
            s_NodesHiddenByCover.Clear();
            s_PanelCoverPath.Clear();
            s_FlushPlacedNodes.Clear();
            s_ComponentRootSegments.Clear();
            s_OpenDropdownMenus.Clear();
            s_OpenDropdownSegmentNodes.Clear();
            s_PendingStructural.Clear();
            s_PendingRelabel.Clear();
            s_PendingInclusionChecks.Clear();
            s_PendingFrameRefreshes.Clear();
            s_CurrentPanelPending.Clear();
            ClearCursorReanchor();
            s_PanelCover = null;
            s_PanelCoverDirty = false;
        }

        internal static void Reset()
        {
            if (s_Subscribed)
            {
                AssistiveSupport.screenReaderStatusChanged -= OnScreenReaderStatusChanged;
                s_Subscribed = false;
            }

            if (s_Hierarchy != null && AssistiveSupport.activeHierarchy == s_Hierarchy)
                AssistiveSupport.activeHierarchy = null;

            s_ParticipatingComponents.Clear();

            // With no participants left, this empties the tracked set and removes the per-panel
            // updaters.
            RefreshTrackedPanels();

            ClearSyncState();

            // Normally, the screen-reader-off event clears the tracked cursor, but this teardown
            // just unsubscribed from it; left set, the tracked node keeps the entire discarded
            // hierarchy reachable (nodes hold parent/children references).
            AccessibilityTreeGenerator.ClearScreenReaderFocusedNode();

            // Survives a rebuild's ClearSyncState by design, so the teardown clears it here.
            s_CursorElementBeforeCover = null;
            s_Hierarchy = null;
            UpdateLiveState();
        }

        #region Live sync change notifications

        /// <summary>
        /// A structural change happened at this element (the bit fires on the parent and on an
        /// added child); its context regenerates at the next flush. A structural change *on* a
        /// known control is internal chrome and not represented — except a content host's
        /// content, whose changes fire on its content container.
        /// </summary>
        internal static void OnElementStructureChanged(VisualElement ve)
        {
            if (AccessibilityRoleRegistry.TryGetRole(ve, out _))
                return;

            s_PendingStructural.Add(ve);
        }

        /// <summary>
        /// The element's name changed; a mapped node relabels (names are the last-resort label
        /// source), an unmapped element may now warrant a node.
        /// </summary>
        internal static void OnElementLabelSourceChanged(VisualElement ve)
        {
            s_PendingRelabel.Add(ve);
        }

        /// <summary>
        /// A style or picking change happened that may have flipped the element's inclusion in
        /// the hierarchy (visibility, and pickingMode deciding whether unknown elements are
        /// kept); the flush re-checks and only regenerates on an actual flip.
        /// </summary>
        internal static void OnElementInclusionMayHaveChanged(VisualElement ve)
        {
            s_PendingInclusionChecks.Add(ve);
        }

        /// <summary>
        /// The element's own layout rect or transform changed; its node frame refreshes at the
        /// next flush, together with its mapped descendants' (ProcessFrameRefreshes explains the
        /// subtree walk).
        /// </summary>
        internal static void OnElementGeometryChanged(VisualElement ve)
        {
            s_PendingFrameRefreshes.Add(ve);
        }

        /// <summary>
        /// Called from <see cref="VisualElement.ApplyPseudoStateChange"/> — the one path every
        /// disabled-state change passes through, ancestors included. Applies immediately; node
        /// writes are managed-only while the hierarchy is inactive.
        /// </summary>
        internal static void OnElementEnabledChanged(VisualElement ve)
        {
            if (!s_IsLive)
                return;

            if (s_NodeMap.TryGetNode(ve, out var node))
                node.state = AccessibilityTreeGenerator.DeriveState(ve);
        }

        /// <summary>
        /// Called from the <see cref="VisualElement.areAncestorsAndSelfDisplayed"/> setter — the
        /// one path every displayed-state change passes through. Displayed state decides subtree
        /// inclusion, so this is structural.
        /// </summary>
        internal static void OnElementDisplayedChanged(VisualElement ve)
        {
            if (!s_IsLive)
                return;

            if (ve.elementPanel is not { } panel || !s_TrackedPanels.Contains(panel))
                return;

            s_PendingStructural.Add(ve);
        }

        // Leaf data changes (value, text, caption) never bump version bits; they arrive through
        // this per-mapped-element hook and apply immediately.
        static void OnElementPropertyChanged(PropertyChangedEvent evt)
        {
            if (!s_IsLive || evt.target is not VisualElement element)
                return;

            if (!s_NodeMap.TryGetNode(element, out var node))
                return;

            // TextElement notifies both "value" and "text" for the same change; the state and
            // value derivations are cheap enough that distinguishing is not worth it. The node
            // setters no-op when nothing changed.
            if (evt.property == BaseField<bool>.valueProperty)
            {
                node.state = AccessibilityTreeGenerator.DeriveState(element);
                node.value = AccessibilityTreeGenerator.DeriveValue(element);
            }
            else if (evt.property == TextElement.textProperty || evt.property == AbstractBaseField.labelProperty)
            {
                RelabelNode(element, node);
            }
            else if (evt.property == ScrollView.scrollOffsetProperty)
            {
                // Every interactive scroll path (scroller drags and buttons, the wheel, touch)
                // funnels through the scrollOffset setter, so this keeps the percentage current —
                // per frame during drags, hence the check (see ScrollPercentageIsCurrent).
                if (!AccessibilityTreeGenerator.ScrollPercentageIsCurrent(node.value, element))
                    node.value = AccessibilityTreeGenerator.DeriveValue(element);
            }
        }

        static void RelabelNode(VisualElement element, AccessibilityNode node)
        {
            var label = AccessibilityTreeGenerator.DeriveLabel(element);

            // Roles that only exist because a label could be derived lose their reason to exist
            // with it; that is a structural change, not a relabel.
            if (label == null &&
                node.role is AccessibilityRole.None or AccessibilityRole.Container)
            {
                s_PendingStructural.Add(element);
                return;
            }

            node.label = label;
        }

        #endregion // Live sync change notifications

        #region Per-frame flush

        // The pending entries taken for the panel being flushed. One reused list: each
        // TakePanelPending call clears and refills it, so never hold it across steps. Safe
        // shared — flushing is main-thread and the steps run strictly in order.
        [AutoStaticsCleanupOnCodeReload]
        static readonly List<VisualElement> s_CurrentPanelPending = new();

        /// <summary>
        /// Called by <see cref="AccessibilityTreeGenerator"/> when a cursor arrival scrolled the
        /// element into view. The corrected frame is already pushed, but the platform sampled the
        /// old geometry before delivering focus; <see cref="ProcessCursorReanchor"/> redraws the
        /// cursor once the frames settle.
        /// </summary>
        internal static void OnCursorArrivalScrolled(VisualElement element, AccessibilityNode node)
        {
            if (!s_IsLive)
                return;

            s_CursorReanchorElement = element;
            s_CursorReanchorNode = node;
            s_CursorReanchorLastFrame = node.frame;
            s_CursorReanchorFlushesLeft = k_CursorReanchorSettleBudget;
        }

        /// <summary>
        /// Called when the node loses the screen reader cursor: a re-anchor for a node the
        /// cursor left would yank it back. The pending state self-guards by node identity;
        /// callers must not gate this on event order — the next gain can arrive before the
        /// previous loss.
        /// </summary>
        internal static void OnCursorArrivalScrollCanceled(AccessibilityNode node)
        {
            if (s_CursorReanchorNode == node)
                ClearCursorReanchor();
        }

        static void ClearCursorReanchor()
        {
            s_CursorReanchorElement = null;
            s_CursorReanchorNode = null;
            s_CursorReanchorFlushesLeft = 0;
        }

        static void ProcessCursorReanchor(BaseVisualElementPanel panel)
        {
            if (s_CursorReanchorNode is not { } node)
                return;

            var element = s_CursorReanchorElement;
            if (element.elementPanel != panel)
            {
                // This flush belongs to another panel; the element's own panel flushes
                // separately — unless the element left every panel, where nothing is left to
                // re-anchor.
                if (element.elementPanel == null)
                    ClearCursorReanchor();
                return;
            }

            // A re-anchor repairs one arrival; once the cursor moves on it is stale. The loss
            // cancel covers the delivered case; this covers a gain processed before its loss.
            if (AccessibilityTreeGenerator.screenReaderFocusedNode != node)
            {
                ClearCursorReanchor();
                return;
            }

            // A regeneration may have re-mapped the element since the cursor arrived; the
            // notification must carry the node the cursor actually sits on.
            if (!s_NodeMap.TryGetNode(element, out var currentNode) || currentNode != node)
            {
                ClearCursorReanchor();
                return;
            }

            // Wait for the scrolled frames to settle across flushes; the budget forces the
            // notification out even when the frame never settles (content animating under the
            // cursor).
            var frame = node.frame;
            if (frame != s_CursorReanchorLastFrame && s_CursorReanchorFlushesLeft > 0)
            {
                s_CursorReanchorLastFrame = frame;
                s_CursorReanchorFlushesLeft--;
                return;
            }

            ClearCursorReanchor();

            if (AssistiveSupport.activeHierarchy == s_Hierarchy)
                AssistiveSupport.notificationDispatcher.SendLayoutChanged(node);
        }

        /// <summary>
        /// Called by the panel's <see cref="VisualTreeAccessibilityUpdater"/> after layout settled.
        /// Processes the pending changes belonging to this panel and refreshes node frames where
        /// geometry moved.
        /// </summary>
        internal static void FlushPanelUpdates(BaseVisualElementPanel panel)
        {
            ProcessInclusionChecks(panel);
            ProcessRelabels(panel);
            var structurallyChanged = ProcessStructural(panel);

            ProcessFrameRefreshes(panel);

            ProcessCursorReanchor(panel);

            var panelCoverChanged = UpdatePanelCover(structurallyChanged);

            // Only structural changes (and cover flips, which change what is reachable) warrant a
            // layout-changed notification. Frame-only refreshes already reach the native side
            // through the frame setters (pushed into the platform nodes on macOS/iOS, re-read on
            // query on Windows/Android); notifying on every geometry change makes screen readers
            // chatter through ordinary UI animation, like a label resizing once per second.
            if ((structurallyChanged || panelCoverChanged) && AssistiveSupport.activeHierarchy == s_Hierarchy)
                AssistiveSupport.notificationDispatcher.SendLayoutChanged(TakeCursorNodeToRestore());

            // Left uncleared, the scratch list keeps the last step's elements (and through them
            // their subtrees) reachable until the next non-empty flush step — indefinitely on
            // quiet frames, since the steps early-out before refilling it.
            s_CurrentPanelPending.Clear();
            s_FlushPlacedNodes.Clear();
        }

        static void TakePanelPending(HashSet<VisualElement> pending, BaseVisualElementPanel panel, List<VisualElement> taken)
        {
            taken.Clear();

            foreach (var element in pending)
            {
                // Detached elements belong to no panel; their removal is covered by the structural
                // change on their (still attached) old parent.
                if (element.elementPanel == null || element.elementPanel == panel)
                    taken.Add(element);
            }

            foreach (var element in taken)
            {
                pending.Remove(element);
            }

            taken.RemoveAll(element => element.elementPanel == null);
        }

        static void ProcessInclusionChecks(BaseVisualElementPanel panel)
        {
            if (s_PendingInclusionChecks.Count == 0)
                return;

            TakePanelPending(s_PendingInclusionChecks, panel, s_CurrentPanelPending);

            foreach (var element in s_CurrentPanelPending)
            {
                if (InclusionFlipped(element))
                    s_PendingStructural.Add(element);

                // A pickingMode change can promote the element to panel cover or retire the
                // current one (the Picking bit routes through the inclusion recheck).
                NotePanelCoverSignal(element);
            }
        }

        static void ProcessRelabels(BaseVisualElementPanel panel)
        {
            if (s_PendingRelabel.Count == 0)
                return;

            TakePanelPending(s_PendingRelabel, panel, s_CurrentPanelPending);

            foreach (var element in s_CurrentPanelPending)
            {
                if (s_NodeMap.TryGetNode(element, out var node))
                    RelabelNode(element, node);
                else if (InclusionFlipped(element))
                    s_PendingStructural.Add(element);
            }
        }

        static bool InclusionFlipped(VisualElement element)
        {
            return s_NodeMap.ContainsKey(element) != AccessibilityTreeGenerator.WouldGenerateNode(element);
        }

        static bool ProcessStructural(BaseVisualElementPanel panel)
        {
            if (s_PendingStructural.Count == 0)
                return false;

            TakePanelPending(s_PendingStructural, panel, s_CurrentPanelPending);

            if (s_CurrentPanelPending.Count == 0)
                return false;

            // Shallow elements first, so a wide regeneration runs before pending descendants and
            // the covered-roots check can skip them. Depths are computed once up front: inside
            // the comparator, each would be re-walked O(log n) times.
            using var pooledPending = ListPool<(VisualElement element, int depth)>.Get(out var pendingByDepth);
            foreach (var element in s_CurrentPanelPending)
            {
                pendingByDepth.Add((element, TreeDepth(element)));
            }

            pendingByDepth.Sort((lhs, rhs) => lhs.depth.CompareTo(rhs.depth));

            using var pooledCovered = HashSetPool<VisualElement>.Get(out var coveredRoots);
            var changed = false;

            foreach (var (element, _) in pendingByDepth)
            {
                if (IsUnderAny(element, coveredRoots))
                    continue;

                if (!TryFindScopeToRegenerate(element, out var contextNode, out var contextElement, out var rootSegmentComponent))
                    continue;

                if (contextNode != null)
                {
                    RegenerateContainerContext(contextNode, contextElement);
                    NotePanelCoverSignalInSubtree(contextElement);
                    coveredRoots.Add(contextElement);
                }
                else
                {
                    RegenerateRootSegment(rootSegmentComponent);
                    if (rootSegmentComponent.GetRootVisualElement() is { } componentRoot)
                    {
                        NotePanelCoverSignalInSubtree(componentRoot);
                        coveredRoots.Add(componentRoot);
                    }
                }

                changed = true;
            }

            return changed;
        }

        static int TreeDepth(VisualElement element)
        {
            var depth = 0;
            for (var ancestor = element.hierarchy.parent; ancestor != null; ancestor = ancestor.hierarchy.parent)
            {
                depth++;
            }

            return depth;
        }

        static bool IsUnderAny(VisualElement element, HashSet<VisualElement> roots)
        {
            if (roots.Count == 0)
                return false;

            for (var current = element; current != null; current = current.hierarchy.parent)
            {
                if (roots.Contains(current))
                    return true;
            }

            return false;
        }

        // Resolves what regenerates for a structural change: the nearest mapped Container
        // ancestor's children, or the component's root segment when there is none. The context
        // sits strictly above the element because the change may alter the element's own
        // representation. Known-control internals and omitted subtrees resolve to nothing.
        static bool TryFindScopeToRegenerate(VisualElement element, out AccessibilityNode contextNode,
            out VisualElement contextElement, out IPanelComponent rootSegmentComponent)
        {
            contextNode = null;
            contextElement = null;
            rootSegmentComponent = null;

            // The element is a top-level component root itself (its parent is the panel's root).
            if (element is IPanelComponentRootElement componentRoot &&
                element.hierarchy.parent is { } panelRoot && panelRoot.hierarchy.parent == null)
            {
                if (!s_ParticipatingComponents.Contains(componentRoot.panelComponent))
                    return false;

                rootSegmentComponent = componentRoot.panelComponent;
                return true;
            }

            VisualElement topLevelCandidate = null;

            for (var ancestor = element.hierarchy.parent; ancestor != null; ancestor = ancestor.hierarchy.parent)
            {
                // Reached the panel's root element: regenerate the top-level component's segment.
                if (ancestor.hierarchy.parent == null)
                {
                    if (topLevelCandidate is IPanelComponentRootElement rootElement &&
                        s_ParticipatingComponents.Contains(rootElement.panelComponent))
                    {
                        rootSegmentComponent = rootElement.panelComponent;
                        return true;
                    }

                    return false;
                }

                // Known-control internals are not represented — except a content host's
                // content, which regenerates under the host's node (chrome still resolves to
                // nothing). The element may be the content container itself: structural bits
                // fire on the parent.
                if (AccessibilityRoleRegistry.TryGetRole(ancestor, out _))
                {
                    if (AccessibilityTreeGenerator.TryGetContentHost(ancestor, out var content) &&
                        (element == content || content.Contains(element)) &&
                        s_NodeMap.TryGetNode(ancestor, out var hostNode))
                    {
                        contextNode = hostNode;
                        contextElement = content;
                        return true;
                    }

                    return false;
                }

                // Inside an omitted subtree: nothing there is represented. (A display flip enqueues
                // the flipped element itself, whose ancestors are displayed.)
                if (AccessibilityTreeGenerator.IsOmittedSubtree(ancestor))
                    return false;

                if (s_NodeMap.TryGetNode(ancestor, out var node) && node.role == AccessibilityRole.Container)
                {
                    contextNode = node;
                    contextElement = ancestor;
                    return true;
                }

                topLevelCandidate = ancestor;
            }

            return false;
        }

        // Both flavors diff against the previous generation: nodes still producing the same
        // kind of node in the same scope are reused (screen reader focus survives), the
        // unvisited remainder is swept. Reuse never crosses scopes, so no walk disturbs another
        // scope's node positions — the rule the segment bookkeeping relies on.

        // The recursions below index node.children instead of foreach: the property is an
        // interface-typed view, and enumerating it boxes for every node on every structural
        // flush.
        static void RegenerateContainerContext(AccessibilityNode contextNode, VisualElement contextElement)
        {
            using var pooledPrevious = HashSetPool<AccessibilityNode>.Get(out var previousNodes);
            var contextChildren = contextNode.children;
            for (var i = 0; i < contextChildren.Count; i++)
            {
                SnapshotSubtree(contextChildren[i], previousNodes);
            }

            var cursor = 0;
            RegenerateChildren(contextElement, contextNode, ref cursor, previousNodes);

            for (var i = 0; i < contextChildren.Count; i++)
            {
                RegisterPropertyCallbacks(contextChildren[i]);
            }
        }

        static void RegenerateRootSegment(IPanelComponent component)
        {
            if (!s_ComponentRootSegments.TryGetValue(component, out var oldSegment))
                oldSegment = null;

            // The first old node's live position anchors the segment, and it is still live
            // here: reuse never crosses scopes, so another scope regenerating first can at most
            // shift this segment whole — which the live lookup absorbs.
            var segmentStart = oldSegment is { Count: > 0 }
                ? IndexOfRootNode(oldSegment[0])
                : ComputeRootSegmentStartIndex(component);

            using var pooledPrevious = HashSetPool<AccessibilityNode>.Get(out var previousNodes);
            if (oldSegment != null)
            {
                foreach (var node in oldSegment)
                {
                    SnapshotSubtree(node, previousNodes);
                }
            }

            // The sweep runs before the capture below: stale root nodes inside the walked range
            // would corrupt the segment bookkeeping. Everything swept sits after the range or
            // deeper.
            var cursor = segmentStart;
            RegenerateChildren(component.GetRootVisualElement(), null, ref cursor, previousNodes);

            var newSegment = new List<AccessibilityNode>(cursor - segmentStart);
            for (var i = segmentStart; i < cursor; i++)
            {
                newSegment.Add(s_Hierarchy.rootNodes[i]);
            }

            s_ComponentRootSegments[component] = newSegment;

            foreach (var node in newSegment)
            {
                RegisterPropertyCallbacks(node);
            }
        }

        // The shared diff core: walk the context element's children into place under the parent
        // node (reusing this scope's previous-generation nodes through the generator) and sweep
        // whatever the walk did not visit from the previous generation.
        static void RegenerateChildren(VisualElement contextElement, AccessibilityNode parentNode,
            ref int cursor, HashSet<AccessibilityNode> previousNodes)
        {
            using var pooledVisited = HashSetPool<AccessibilityNode>.Get(out var visited);

            if (contextElement != null)
            {
                // Derived once for all sibling subtrees (see AccessibilityFrameProjection.ClipState).
                var childState = AccessibilityFrameProjection.Descend(
                    AccessibilityFrameProjection.GetClipStateAbove(contextElement), contextElement);
                for (var i = 0; i < contextElement.hierarchy.childCount; i++)
                {
                    AccessibilityTreeGenerator.GenerateSubtree(contextElement.hierarchy[i], s_Hierarchy,
                        parentNode, s_NodeMap, ref cursor, previousNodes, visited, childState);
                }
            }

            // Open menus mount at the panel root, so a context walk outside the menu never
            // visits them; sparing them keeps churn near the field from tearing the menu down.
            // Item churn inside the menu sweeps normally.
            SweepStaleNodes(previousNodes, visited,
                spareOpenDropdownSegments: !IsInOpenDropdownSegment(parentNode));

            // While a cover is up, the flush-wide set of placed nodes is the scope of the
            // unchanged-cover exposure re-apply (see UpdatePanelCover).
            if (s_PanelCover != null)
                s_FlushPlacedNodes.UnionWith(visited);
        }

        static void SnapshotSubtree(AccessibilityNode node, HashSet<AccessibilityNode> snapshot)
        {
            snapshot.Add(node);

            var children = node.children;
            for (var i = 0; i < children.Count; i++)
            {
                SnapshotSubtree(children[i], snapshot);
            }
        }

        static void SweepStaleNodes(HashSet<AccessibilityNode> previousNodes, HashSet<AccessibilityNode> visited,
            bool spareOpenDropdownSegments)
        {
            foreach (var node in previousNodes)
            {
                // A node already gone was removed with an ancestor's subtree; nothing that the
                // walk visited can sit under an unvisited node (reused nodes are moved out).
                if (visited.Contains(node) || !s_Hierarchy.ContainsNode(node))
                    continue;

                // Open menus are removed by their close, never by a sweep.
                if (spareOpenDropdownSegments && IsInOpenDropdownSegment(node))
                    continue;

                CleanupSubtreeMapping(node);
                s_Hierarchy.RemoveNode(node, removeChildren: true);
            }
        }

        // Whether the node is part of an open dropdown menu's segment (one of its captured nodes
        // or a descendant of one).
        static bool IsInOpenDropdownSegment(AccessibilityNode node)
        {
            if (s_OpenDropdownSegmentNodes.Count == 0)
                return false;

            for (var current = node; current != null; current = current.parent)
            {
                if (s_OpenDropdownSegmentNodes.Contains(current))
                    return true;
            }

            return false;
        }

        static int IndexOfRootNode(AccessibilityNode node)
        {
            var rootNodes = s_Hierarchy.rootNodes;
            for (var i = 0; i < rootNodes.Count; i++)
            {
                if (rootNodes[i] == node)
                    return i;
            }

            return rootNodes.Count;
        }

        static int ComputeRootSegmentStartIndex(IPanelComponent component)
        {
            var index = 0;

            foreach (var root in GetParticipatingRootsInReadingOrder())
            {
                var current = ((IPanelComponentRootElement)root).panelComponent;
                if (current == component)
                    break;

                if (s_ComponentRootSegments.TryGetValue(current, out var segment))
                    index += segment.Count;
            }

            return index;
        }

        static void CleanupSubtreeMapping(AccessibilityNode node)
        {
            var children = node.children;
            for (var i = 0; i < children.Count; i++)
            {
                CleanupSubtreeMapping(children[i]);
            }

            // Only release the element's hook when its mapping still points at this node; a
            // reparented element regenerated under its new context first must keep its fresh
            // registration (the hook delegate is shared, so unregistering here would kill it).
            if (s_NodeMap.RemoveNode(node, out var element))
                element.UnregisterCallback<PropertyChangedEvent>(s_OnElementPropertyChanged);

            // With the scoped re-apply, removals are the only path off the hidden set for a
            // node that never gets re-derived.
            if (s_NodesHiddenByCover.Count > 0)
                s_NodesHiddenByCover.Remove(node);
        }

        static void RegisterPropertyCallbacks(AccessibilityNode node)
        {
            // Registration is a no-op for an already-registered handler, so re-visiting a
            // reparented element is safe.
            if (s_NodeMap.TryGetElement(node, out var element))
                element.RegisterCallback<PropertyChangedEvent>(s_OnElementPropertyChanged);

            var children = node.children;
            for (var i = 0; i < children.Count; i++)
            {
                RegisterPropertyCallbacks(children[i]);
            }
        }

        // A geometry bit fires on the topmost moved element; descendants shift without further
        // bits, so the refresh walks each pending element's subtree. Pending elements under
        // another pending element are covered by that walk and skipped.
        static void ProcessFrameRefreshes(BaseVisualElementPanel panel)
        {
            if (s_PendingFrameRefreshes.Count == 0)
                return;

            TakePanelPending(s_PendingFrameRefreshes, panel, s_CurrentPanelPending);

            if (s_CurrentPanelPending.Count == 0)
                return;

            using var pooledRoots = HashSetPool<VisualElement>.Get(out var pendingRoots);
            foreach (var element in s_CurrentPanelPending)
            {
                pendingRoots.Add(element);

                // Geometry can grow an element into panel cover or shrink the current one out of
                // the role; both sides reduce to one cheap rect test per moved element.
                NotePanelCoverSignal(element);
            }

            foreach (var element in s_CurrentPanelPending)
            {
                if (!IsUnderAny(element.hierarchy.parent, pendingRoots))
                    RefreshSubtreeFrames(element);
            }
        }

        static void RefreshSubtreeFrames(VisualElement element)
        {
            // The clip state above the subtree is derived once and carried down the recursion
            // (see AccessibilityFrameProjection.ClipState).
            RefreshSubtreeFrames(element, AccessibilityFrameProjection.GetClipStateAbove(element));
        }

        static void RefreshSubtreeFrames(VisualElement element, AccessibilityFrameProjection.ClipState inherited)
        {
            if (s_NodeMap.TryGetNode(element, out var node))
            {
                var bounds = element.worldBound;

                // Pushing through the setter forwards to native even when the value is unchanged,
                // which native needs for its own screen-coordinate conversion; while the hierarchy
                // is inactive, this is a managed-only write.
                node.frame = AccessibilityFrameProjection.GetScreenFrame(element, bounds);

                // The same geometry decides in-view state (content scrolling under a viewport,
                // an element leaving the panel); the cover term rides the set so cover exposure
                // is not recomputed per node. The isActive setter no-ops when unchanged.
                node.isActive = !s_NodesHiddenByCover.Contains(node) &&
                    !AccessibilityFrameProjection.IsFullyOutOfView(inherited, bounds);

                // A content host's content is mapped, so the walk continues through the content
                // container. The scroll percentage depends on the same geometry, so it refreshes
                // here too — guarded, since this runs per frame (see ScrollPercentageIsCurrent).
                if (AccessibilityTreeGenerator.TryGetContentHost(element, out var content, out var viewport))
                {
                    if (!AccessibilityTreeGenerator.ScrollPercentageIsCurrent(node.value, element))
                        node.value = AccessibilityTreeGenerator.DeriveValue(element);

                    RefreshSubtreeFrames(content,
                        AccessibilityFrameProjection.DescendToHostedContent(inherited, element, viewport));
                    return;
                }

                // Known controls are accessibility leaves; their internals are never mapped.
                if (AccessibilityRoleRegistry.TryGetRole(element, out _))
                    return;
            }

            var childState = AccessibilityFrameProjection.Descend(inherited, element);
            for (var i = 0; i < element.hierarchy.childCount; i++)
            {
                RefreshSubtreeFrames(element.hierarchy[i], childState);
            }
        }

        #endregion // Per-frame flush

        #region Panel cover (modal occlusion)

        // Recomputes the panel cover when something that decides it may have changed: the dirty
        // flag (geometry/picking signals, regenerated-subtree scans), or the current cover no
        // longer covering — checked directly, one element per flush, so structural churn never
        // rescans every tree just to notice cover loss. Returns whether the cover changed.
        static bool UpdatePanelCover(bool structurallyChanged)
        {
            // The rect test alone is not enough: an element hidden by a display flip (its own or
            // an ancestor's) or turned invisible keeps its last laid-out worldBound, so the
            // stale rect still spans the panel while the element no longer covers anything.
            if (s_PanelCover != null &&
                (s_PanelCover.elementPanel == null || !s_PanelCover.areAncestorsAndSelfDisplayed ||
                    !s_PanelCover.visible || !IsCoveringItsPanel(s_PanelCover)))
                s_PanelCoverDirty = true;

            // A moved cover invalidates every node's derived exposure (see s_PanelCoverPath)
            // and takes the full recompute. Computed once — nothing between here and the apply
            // decision moves the cover.
            var structuralUnderCover = structurallyChanged && s_PanelCover != null;
            var coverMoved = structuralUnderCover && !PanelCoverPathIsCurrent();
            if (!s_PanelCoverDirty && coverMoved)
                s_PanelCoverDirty = true;

            if (!s_PanelCoverDirty && !structuralUnderCover)
                return false;

            var changed = false;

            if (s_PanelCoverDirty)
            {
                s_PanelCoverDirty = false;

                var roots = GetParticipatingRootsInReadingOrder();
                var cover = FindPanelCover(roots);
                var previousCover = s_PanelCover;
                changed = cover != previousCover;
                s_PanelCover = cover;

                // Entering the covered state: remember where the cursor was so dismissal can
                // send it back. An existing snapshot is kept — a cover replacing another, or a
                // rebuild re-finding a standing cover from the cleared state, must not
                // overwrite the pre-cover position with one inside a dialog.
                if (changed && previousCover == null && s_CursorElementBeforeCover == null)
                    s_CursorElementBeforeCover = AccessibilityTreeGenerator.screenReaderFocusedElement;

                // Churn under a cover that recomputed unchanged AND unmoved re-derives only the
                // placed nodes; everything else re-derives the whole map.
                if (changed || (cover != null && structurallyChanged && coverMoved))
                    ApplyPanelCoverToNodes(roots);
                else if (cover != null && structurallyChanged)
                    ApplyPanelCoverToPlacedNodes(roots);
            }
            else if (s_FlushPlacedNodes.Count > 0)
            {
                // Structural churn under a standing, unmoved cover: regeneration created its
                // nodes exposed, so the cover term is re-applied to just those. (The count
                // guard keeps a placement-free flush from building the roots list for nothing.)
                ApplyPanelCoverToPlacedNodes(GetParticipatingRootsInReadingOrder());
            }

            // A cursor the cover never hid has nothing to return to. Resolved through the map,
            // not the tracked node: this flush may have regenerated the element's node, leaving
            // the tracked one stale.
            if (s_PanelCover != null && s_CursorElementBeforeCover != null &&
                !(s_NodeMap.TryGetNode(s_CursorElementBeforeCover, out var coveredCursorNode) &&
                    s_NodesHiddenByCover.Contains(coveredCursorNode)))
                s_CursorElementBeforeCover = null;

            return changed;
        }

        // The restore target for the flush notification, consumed once per cover cycle: leaving
        // the covered state returns the cursor where it was, instead of the top of the
        // hierarchy. Null otherwise — placement stays with the platform.
        static AccessibilityNode TakeCursorNodeToRestore()
        {
            if (s_PanelCover != null || s_CursorElementBeforeCover is not { } element)
                return null;

            s_CursorElementBeforeCover = null;

            // The element may be gone (removed while the dialog was up) or its node hidden for
            // reasons of its own beyond the cover (scrolled out of view); either way, there is no
            // longer a position to return to, and the platform places the cursor itself.
            var resolved = s_NodeMap.TryGetNode(element, out var node);

            return resolved && node.isActive ? node : null;
        }

        // The topmost cover wins: panels and components are walked from the top of the paint
        // order down, and within a subtree children paint above their parent, so the reverse
        // pre-order finds the last-painted covering element first.
        static VisualElement FindPanelCover(List<VisualElement> roots)
        {
            for (var i = roots.Count - 1; i >= 0; i--)
            {
                if (FindPanelCoverIn(roots[i]) is { } cover)
                    return cover;
            }

            return null;
        }

        static VisualElement FindPanelCoverIn(VisualElement element)
        {
            if (AccessibilityTreeGenerator.IsOmittedSubtree(element))
                return null;

            for (var i = element.hierarchy.childCount - 1; i >= 0; i--)
            {
                if (FindPanelCoverIn(element.hierarchy[i]) is { } cover)
                    return cover;
            }

            return IsCoveringItsPanel(element) ? element : null;
        }

        // Dirty only when the outcome can change: a new element covering, or the current cover
        // no longer covering. The cover merely moving while still covering cannot change the
        // outcome, and skipping it keeps animated covers from forcing a whole-tree recompute
        // every frame.
        static void NotePanelCoverSignal(VisualElement element)
        {
            var covering = IsCoveringItsPanel(element);
            if (element == s_PanelCover ? !covering : covering)
                s_PanelCoverDirty = true;
        }

        // A regenerated subtree is the one place a new cover can arrive through a structural
        // change, so each regeneration scans its own context instead of every flush rescanning
        // every tree. The standing cover is deliberately not a hit: churn inside a modal
        // resolves to the backdrop as its context, and treating it as an arrival would force
        // the full recompute every flush. The scan still descends into it (a panel-spanning
        // child is a new topmost cover); the cover's own movement and loss are caught in
        // UpdatePanelCover.
        static void NotePanelCoverSignalInSubtree(VisualElement element)
        {
            if (s_PanelCoverDirty || AccessibilityTreeGenerator.IsOmittedSubtree(element))
                return;

            if (element != s_PanelCover && IsCoveringItsPanel(element))
            {
                s_PanelCoverDirty = true;
                return;
            }

            for (var i = 0; i < element.hierarchy.childCount; i++)
            {
                NotePanelCoverSignalInSubtree(element.hierarchy[i]);
            }
        }

        // The pointer-parallel definition of a cover: a pickable element spanning its whole
        // panel blocks every pointer interaction below it, so the screen reader mirrors pointer
        // reach. Visual transparency plays no part — a transparent backdrop blocks clicks all
        // the same. Screen-space panels span the window, so covering one covers lower panels.
        static bool IsCoveringItsPanel(VisualElement element)
        {
            if (element.pickingMode != PickingMode.Position || element.elementPanel is not { } panel)
                return false;

            var viewport = panel.visualTree.layout;
            var bounds = element.worldBound;

            // NaN bounds (pre-layout) fail the comparisons and correctly count as not covering.
            return bounds.xMin <= viewport.xMin + 0.5f && bounds.yMin <= viewport.yMin + 0.5f &&
                bounds.xMax >= viewport.xMax - 0.5f && bounds.yMax >= viewport.yMax - 0.5f;
        }

        // Hides every mapped node painted below the cover (isActive = false) and exposes the
        // rest. Node identity is untouched, so screen reader focus survives a dialog closing.
        static void ApplyPanelCoverToNodes(List<VisualElement> roots)
        {
            s_NodesHiddenByCover.Clear();

            // For each cover ancestor, the child index leading down to the cover: an element
            // elsewhere in the panel compares sibling branches at the common ancestor. The panel
            // root is included, so same-panel components compare by sibling order too. The walk
            // also caches the cover path for the scoped re-applies.
            using var pooledBranches = DictionaryPool<VisualElement, int>.Get(out var coverBranchIndex);
            var coverComponentOrder = -1;
            s_PanelCoverPath.Clear();
            if (s_PanelCover != null)
            {
                var branchChild = s_PanelCover;
                for (var ancestor = s_PanelCover.hierarchy.parent; ancestor != null; ancestor = ancestor.hierarchy.parent)
                {
                    var branchIndex = ancestor.hierarchy.IndexOf(branchChild);
                    coverBranchIndex[ancestor] = branchIndex;
                    s_PanelCoverPath.Add((ancestor, branchIndex));
                    if (ancestor.hierarchy.parent == null)
                        coverComponentOrder = roots.IndexOf(branchChild);
                    branchChild = ancestor;
                }
            }

            foreach (var (element, node) in s_NodeMap)
            {
                var exposed = s_PanelCover == null ||
                    IsExposedWithPanelCover(element, coverBranchIndex, roots, coverComponentOrder);
                if (!exposed)
                    s_NodesHiddenByCover.Add(node);

                node.isActive = exposed && !AccessibilityFrameProjection.IsFullyOutOfView(element);
            }
        }

        // Whether the cover still sits exactly where exposure was last derived for every node:
        // same ancestor chain, same branch indices. O(cover depth), with one IndexOf per level —
        // the lookups the full apply would pay once, without the map-wide node walk.
        static bool PanelCoverPathIsCurrent()
        {
            var i = 0;
            var branchChild = s_PanelCover;
            for (var ancestor = s_PanelCover.hierarchy.parent; ancestor != null; ancestor = ancestor.hierarchy.parent)
            {
                if (i >= s_PanelCoverPath.Count)
                    return false;

                var (cachedAncestor, cachedIndex) = s_PanelCoverPath[i];
                if (cachedAncestor != ancestor || ancestor.hierarchy.IndexOf(branchChild) != cachedIndex)
                    return false;

                branchChild = ancestor;
                i++;
            }

            return i == s_PanelCoverPath.Count;
        }

        // The scoped counterpart of ApplyPanelCoverToNodes: exposure re-derived only for nodes
        // this flush placed. Sound because an exposure-flipping change always fires inside a
        // context containing the affected node — except the cover itself moving, which
        // PanelCoverPathIsCurrent rules out first.
        static void ApplyPanelCoverToPlacedNodes(List<VisualElement> roots)
        {
            if (s_FlushPlacedNodes.Count == 0)
                return;

            using var pooledBranches = DictionaryPool<VisualElement, int>.Get(out var coverBranchIndex);
            var coverComponentOrder = -1;
            foreach (var (ancestor, branchIndex) in s_PanelCoverPath)
            {
                coverBranchIndex[ancestor] = branchIndex;
                if (ancestor.hierarchy.parent == null)
                    coverComponentOrder = roots.IndexOf(ancestor.hierarchy[branchIndex]);
            }

            foreach (var node in s_FlushPlacedNodes)
            {
                // A mapping can be gone when a later step of the same flush removed the node;
                // nothing to expose then.
                if (!s_NodeMap.TryGetElement(node, out var element))
                    continue;

                var exposed = IsExposedWithPanelCover(element, coverBranchIndex, roots, coverComponentOrder);
                if (exposed)
                    s_NodesHiddenByCover.Remove(node);
                else
                    s_NodesHiddenByCover.Add(node);

                node.isActive = exposed && !AccessibilityFrameProjection.IsFullyOutOfView(element);
            }
        }

        static bool IsExposedWithPanelCover(VisualElement element,
            Dictionary<VisualElement, int> coverBranchIndex, List<VisualElement> roots, int coverComponentOrder)
        {
            VisualElement previous = null;
            for (var current = element; current != null; current = current.hierarchy.parent)
            {
                // The cover itself, or reached from inside it: painted at or above the cover.
                if (current == s_PanelCover)
                    return true;

                if (coverBranchIndex.TryGetValue(current, out var coverChildIndex))
                {
                    // The element is an ancestor of the cover: it stays exposed, since it groups
                    // content that sits above the cover.
                    if (previous == null)
                        return true;

                    // Common ancestor found: the later sibling branch is painted on top.
                    return current.hierarchy.IndexOf(previous) > coverChildIndex;
                }

                // A panel root that is not on the cover's path: the element lives in another
                // panel, and panels stack by their sort order — the reading order of the roots.
                if (current.hierarchy.parent == null)
                    return roots.IndexOf(previous) > coverComponentOrder;

                previous = current;
            }

            return true;
        }

        #endregion // Panel cover (modal occlusion)
    }
}
