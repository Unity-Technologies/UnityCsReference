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
    /// The bridge only ever produces an <see cref="AccessibilityHierarchy"/> — the shipped platform
    /// adapters below <see cref="AssistiveSupport.activeHierarchy"/> handle the native side.
    ///
    /// The whole feature sits behind <see cref="featureEnabled"/>, off by default (fed by the
    /// experimental UI Toolkit project setting): while off, the bridge never subscribes to screen
    /// reader status nor allocates, and no panel carries a <see cref="VisualTreeAccessibilityUpdater"/>.
    /// Individual panel components additionally carry a serialized, default-on opt-out gate that
    /// is evaluated at registration, before any traversal.
    ///
    /// Once a hierarchy exists, it is retained and kept in sync even while the screen reader is off
    /// (mutating an inactive hierarchy is cheap, managed-only work), so turning the screen reader
    /// back on only re-assigns the current hierarchy instead of rebuilding it. Live sync is driven
    /// two ways: <see cref="VisualTreeAccessibilityUpdater"/> forwards version-bit changes
    /// (structure, name, geometry, style- and picking-driven inclusion) and flushes them once per
    /// frame after layout; leaf data (value, text, caption) arrives through per-element event hooks,
    /// disabled/displayed flips through their single engine code paths, all applied immediately.
    ///
    /// Beyond keeping data in sync, the bridge places open dropdown menus' node segments under
    /// their field's node (or at the root level when the field is unmapped).
    ///
    /// The bridge yields to manual authoring: it never overwrites an active hierarchy assigned by
    /// the developer, and it only ever mutates nodes it created.
    /// </remarks>
    internal static partial class UITKAccessibilityBridge
    {
        [AutoStaticsCleanupOnCodeReload]
        static bool s_FeatureEnabled;

        [AutoStaticsCleanupOnCodeReload]
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

        // The root-level nodes generated for each top-level component, in reading order. A
        // structural change regenerates the children of its nearest mapped Container ancestor;
        // when no such ancestor exists (common: no labeled container between the panel root and
        // the controls), the scope is the component's root segment, and these lists identify
        // which root nodes belong to the affected component and where its segment starts.
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

        // The node whose visual cursor needs re-anchoring: the screen reader cursor arrived on an
        // element that had to be scrolled into view, and the cursor's visual highlight is drawn
        // from geometry the platform samples before delivering the focus change. Once the node's
        // frame stops moving across flushes, one layout-changed notification carrying the node
        // redraws the cursor in place — unless the cursor has already moved on (quick sequential
        // navigation is the normal screen reader gait, and a stale re-anchor yanks the cursor
        // backward). Chatter-free by construction: only focus arrivals that actually scrolled get
        // here, and each sends at most one notification.
        [AutoStaticsCleanupOnCodeReload]
        static VisualElement s_CursorReanchorElement;

        [AutoStaticsCleanupOnCodeReload]
        static AccessibilityNode s_CursorReanchorNode;

        // The frame observed at the previous flush, for the stability wait.
        [AutoStaticsCleanupOnCodeReload]
        static Rect s_CursorReanchorLastFrame;

        [AutoStaticsCleanupOnCodeReload]
        static int s_CursorReanchorFlushesLeft;

        // How many consecutive frame-moving flushes the settle wait tolerates before forcing the
        // notification out. Only flushes where the focused node's frame moved consume it — the
        // first quiet flush ends the wait — so it only binds while content keeps moving under the
        // cursor. Two covers the deferred nested-scroll case (the outer ScrollTo re-applies from
        // the scheduler one frame later and settles the next); doubled for slack. Deliberately
        // far below ScrollView's own deferred-retry cap (k_MaxDeferredScrollToAttempts, 60
        // frames of layout that keeps changing): a cursor redrawn after four moving flushes at a
        // near-final position beats one redrawn a second late, and a stale redraw self-heals on
        // the next cursor move.
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

        // In players, the experimental project setting arrives as a TextAsset the build processor
        // placed in a Resources folder ("1" = enabled), applied lazily at the first panel attach.
        // A boot hook is not an option: [RuntimeInitializeOnLoadMethod] is banned in engine
        // modules (BannedSymbols.RuntimeModules.txt — it acts as [Preserve]) and player builds
        // only extract it from script assemblies anyway. A ScriptableObject cannot carry the
        // setting either: internal engine-module classes have no MonoScript in player data, so
        // their assets deserialize typeless. In the editor, the play-mode hook applies it instead.
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
        /// Called by <see cref="PanelSettings"/> when a top-level panel component attaches to the
        /// visual tree — the registration entry where the per-component gate is evaluated. An
        /// opted-out or non-participating component (see <see cref="IsParticipatingPanel"/>) is
        /// never registered, so it is never walked.
        /// </summary>
        internal static void OnPanelComponentAttached(IPanelComponent panelComponent)
        {
            // The Accessibility module cannot represent hierarchies natively on this platform, so
            // the bridge never registers anything (the editor is exempt: assignments there feed
            // the Accessibility Hierarchy Viewer, and the tests run in it). Without this check,
            // forcing AssistiveSupport.screenReaderStatusOverride on an unsupported player would
            // make the bridge build and sync hierarchies nothing can consume, and log an
            // assignment error per activation attempt.
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
            if (AssistiveSupport.isScreenReaderEnabled || s_Hierarchy != null)
                ScheduleRebuildAndActivate(panelComponent);
        }

        // The participant bookkeeping invariant — list membership, the status subscription and
        // the tracked-panel set — is owned here; the attach/detach and gate-flip entry points
        // only differ in how they react to a membership change.
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
        /// Called by <see cref="PanelSettings"/> when a property that decides panel participation
        /// (render mode, target display, target texture) or cross-panel reading order (sorting
        /// order) changes on a live asset. Every attached component is re-evaluated, and a rebuild
        /// is scheduled so order-only changes are reflected too.
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
            if (scheduleTrigger != null && (AssistiveSupport.isScreenReaderEnabled || s_Hierarchy != null))
                ScheduleRebuildAndActivate(scheduleTrigger);
        }

        /// <summary>
        /// Called by the per-component gate setters when the value changes on a live object. A
        /// top-level component (un)registers as if it had just attached or detached; a nested
        /// document's inclusion changes inside an already-walked tree, which is a structural
        /// change at its root.
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

            // Unlike attach/detach — panel lifecycle events where wholesale change is under way —
            // a gate flip happens on settled UI, so it must not take the full-rebuild path: a
            // hierarchy swap tears down every native node and SendScreenChanged re-anchors the
            // screen reader to the first element. The component's root segment is added or
            // removed in place instead, leaving every other node (and the user's focus) alone.
            if (panelComponent.GetAccessibilityEnabled())
            {
                if (!TryRegisterParticipant(panelComponent))
                    return;

                if (s_Hierarchy == null)
                {
                    if (AssistiveSupport.isScreenReaderEnabled)
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
        /// The menu mounts at the panel root, outside every component root, so the bridge places
        /// it explicitly: under its field's node when the menu's target is mapped — reading order
        /// then flows field, items, back out to the field's next sibling — else appended at the
        /// root level. An open menu's items are readable and selectable, and — a dropdown being
        /// light-dismiss, not modal — the rest of the UI stays represented and active alongside
        /// it. The cursor deliberately stays on the field when the menu opens; the user walks in
        /// when they choose to.
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
            // closing menu would be stranded on a removed node, so it returns to the field —
            // whether an item was picked or the menu was dismissed. A cursor anywhere else (on
            // the field, or on unrelated UI during a light dismiss) stays where the user put it.
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

            if (AssistiveSupport.isScreenReaderEnabled || s_Hierarchy != null)
                ScheduleRebuildAndActivate(panelComponent);
        }

        // Attach/detach-triggered rebuilds are deferred to the panel's next update tick. A
        // component's content routinely arrives after its attach (PanelRenderer invokes its UI
        // reload callbacks right after inserting the root; UIDocument content is usually added
        // after panelSettings is assigned), so a synchronous rebuild at attach time snapshots
        // empty roots when the screen reader is already on at startup — the primary path for
        // screen reader users. Deferring one tick also coalesces a startup burst of attaches
        // into a single rebuild. The screen-reader-on event path needs no rebuild at all: the
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

            // Schedule on every participating panel and let the first one to tick rebuild: the
            // trigger's own panel is not guaranteed to get update ticks (and on detach it may be
            // mid-teardown), while any live participating panel is an equally valid point past
            // the attach-burst coalescing window.
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

        // AssistiveSupport.activeHierarchy only represents the application's main window (wherever
        // that window sits — the adapters resolve frames against the live window position), so only
        // main-window screen-space overlay panels participate. Additional windows (targetDisplay >
        // 0), offscreen surfaces (targetTexture) and world-space panels would merge nodes whose
        // surface-local frames collide with main-window content, so they are not represented.
        // Revisit after the platform display API work concludes (display/window separation):
        // additional displays need the platform adapters to expose more than one window's
        // accessibility tree. Render-texture panels blitted 1:1 to the main window are a candidate
        // for an explicit opt-in later. Changing these values on a live PanelSettings re-evaluates
        // its components through OnPanelSettingsChanged.
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
                // Everything derived from the cursor belongs to the screen reader session that is
                // ending: the tracked position (which gets no focus-loss event of its own) and a
                // pending re-anchor. Kept, they would answer for a cursor that no longer exists
                // on the next "on".
                AccessibilityTreeGenerator.ClearScreenReaderFocusedNode();
                ClearCursorReanchor();
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

                foreach (var child in root.hierarchy.Children())
                {
                    AccessibilityTreeGenerator.GenerateSubtree(child, s_Hierarchy, s_NodeMap);
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

            // Yield to a hierarchy the developer assigned manually; only replace nothing or our own
            // previous generation.
            ActivateIfUnclaimed(previousHierarchy);

            RestoreCursorAfterRebuild();
        }

        // Activating a hierarchy sends a screen-changed notification carrying no target, and the
        // platform then places the cursor on its own — the first node, on macOS. That is right for
        // a genuinely new screen, but a rebuild is not one: it is the same UI, re-derived. When a
        // dialog that lived in its own panel component closes, the detach rebuilds, and without
        // this, the cursor lands at the top of the UI instead of on whatever opened the dialog.
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

            // Put the cursor back where the user had it: the tracked element, re-resolved to the
            // node the rebuild just gave it.
            var nodeToFocus = trackedNode is { isActive: true } ? trackedNode : null;

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

#pragma warning disable UAL0018 // Reset() clears AssistiveSupport.activeHierarchy back to null before s_Hierarchy is reset, and this method rebuilds+reassigns s_Hierarchy on the next activation, so a stale capture never outlives one refresh cycle
            if (activeHierarchy == null || activeHierarchy == replaceable)
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

            // Panels only carry an accessibility updater while tracked (the default updater set
            // leaves the phase empty): updaters are called for every version change on every
            // element, so an untracked panel must not pay for the feature — not even an
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
            s_ComponentRootSegments.Clear();
            s_OpenDropdownMenus.Clear();
            s_OpenDropdownSegmentNodes.Clear();
            s_PendingStructural.Clear();
            s_PendingRelabel.Clear();
            s_PendingInclusionChecks.Clear();
            s_PendingFrameRefreshes.Clear();
            s_CurrentPanelPending.Clear();
            ClearCursorReanchor();
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

            s_Hierarchy = null;

            UpdateLiveState();
        }

        #region Live sync change notifications

        /// <summary>
        /// A structural version change happened at this element (child added/removed/reparented —
        /// the bit fires on the parent and on an added child); its context regenerates at the next
        /// flush. Known controls are accessibility leaves, so a structural change *on* one is a
        /// change to its internals (a Toggle inserting its caption label, a ScrollView attaching a
        /// scroller) and is not represented; their own add/remove is signalled through their
        /// parent. A ScrollView's represented content is not exempted here: content changes fire
        /// on its content container, which is not a registered control.
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
        /// Called from <see cref="VisualElement.ApplyPseudoStateChange"/> — the one code path every
        /// disabled-state change passes through — whenever an element's effective disabled state
        /// flips (including elements disabled through an ancestor). Applies immediately: node writes
        /// are managed-only while the hierarchy is inactive.
        /// </summary>
        internal static void OnElementEnabledChanged(VisualElement ve)
        {
            if (!s_IsLive)
                return;

            if (s_NodeMap.TryGetNode(ve, out var node))
                node.state = AccessibilityTreeGenerator.DeriveState(ve);
        }

        /// <summary>
        /// Called from the <see cref="VisualElement.areAncestorsAndSelfDisplayed"/> setter — the one
        /// code path every displayed-state change passes through, driven by the layout updater —
        /// whenever an element's effective displayed state flips; displayed state decides subtree
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

        // The pending entries taken for the panel currently being flushed. One reused list rather
        // than a per-step allocation: each TakePanelPending call clears and refills it, so its
        // contents are only meaningful within the step that filled it — never hold it across
        // steps. Safe as a single shared instance because all flushing happens on the main thread
        // and the steps run strictly one after another.
        [AutoStaticsCleanupOnCodeReload]
        static readonly List<VisualElement> s_CurrentPanelPending = new();

        /// <summary>
        /// Called by <see cref="AccessibilityTreeGenerator"/> when the screen reader cursor's
        /// arrival on the element scrolled it into view. The caller already pushed the corrected
        /// frame for late readers, but the cursor's highlight is drawn from geometry sampled
        /// before the focus change was delivered; <see cref="ProcessCursorReanchor"/> redraws it
        /// once the scrolled frames settle.
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
        /// Called when the node loses the screen reader cursor: a re-anchor for a node the cursor
        /// already left would yank the cursor back. The pending state self-guards by node
        /// identity, so callers need not (and must not) gate this on event ordering — the gain
        /// for the next node can arrive before the loss for the previous one.
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

            // The re-anchor is a repair for one specific arrival: the moment the cursor moves
            // on, it is stale, and sending it would yank the cursor backward. The cancel on
            // cursor loss already covers the delivered-event case; this covers the race where
            // the next gain arrived without the loss having been processed yet.
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

            // Only structural changes warrant a layout-changed notification. Frame-only refreshes
            // already reach the native side through the frame setters (pushed into the platform
            // nodes on macOS/iOS, re-read on query on Windows/Android); notifying on every
            // geometry tick makes screen readers chatter through ordinary UI animation, like a
            // label resizing once per second.
            if (structurallyChanged && AssistiveSupport.activeHierarchy == s_Hierarchy)
                AssistiveSupport.notificationDispatcher.SendLayoutChanged();

            // Left uncleared, the scratch list keeps the last step's elements (and through them
            // their subtrees) reachable until the next non-empty flush step — indefinitely on
            // quiet frames, since the steps early-out before refilling it.
            s_CurrentPanelPending.Clear();
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
                    coveredRoots.Add(contextElement);
                }
                else
                {
                    RegenerateRootSegment(rootSegmentComponent);
                    if (rootSegmentComponent.GetRootVisualElement() is { } componentRoot)
                        coveredRoots.Add(componentRoot);
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

        // Resolves the unit of regeneration for a structural change at the element: the nearest
        // mapped Container ancestor (regenerate its node children), or the top-level component's
        // root-level segment when no such ancestor exists. The context is chosen strictly above the
        // element because the change may alter the element's own representation (kind, existence).
        // Changes inside known-control leaves (whose internals are never represented) and inside
        // omitted subtrees resolve to no context at all.
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

                // Inside a known control's internals: not represented, nothing to regenerate.
                // The exception is a content host's content, which is represented under the
                // host's own node; changes in its chrome (scrollers) still resolve to nothing.
                // The element may be the content container itself: structural bits fire on the
                // parent of an added or removed child.
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

        // Both regeneration flavors diff against the previous generation instead of rebuilding:
        // the walk reuses every node whose element still produces the same kind of node within
        // the same scope (screen reader focus on it survives), and the nodes of the previous
        // generation that the walk did not visit are swept afterwards. An element that moved in
        // from another scope gets a fresh node — reuse never crosses scopes, so no walk disturbs
        // another scope's node positions (the invariant the segment bookkeeping below relies on).

        // The node-side recursions below index node.children instead of enumerating it: the
        // property is an interface-typed view of the child list, and foreach through it boxes
        // the enumerator for every node in the segment — including the leaf majority with empty
        // child lists — on every structural flush.
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

            // The first old node's live position is the segment's position. It is guaranteed to
            // still be live here: only this scope's own sweep (below) removes this segment's
            // nodes, because node reuse never crosses scopes — even when another scope
            // regenerated first in this flush (a cross-document reparent dirties both sides),
            // the most it did to this segment is shift it whole, which the live lookup absorbs.
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

            // The sweep inside RegenerateChildren runs before the capture below: stale root nodes
            // left inside [segmentStart, cursor) would corrupt the segment bookkeeping. Reused
            // nodes were moved into position by the walk, so everything swept sits after the
            // walked range or deeper in the tree.
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
                for (var i = 0; i < contextElement.hierarchy.childCount; i++)
                {
                    AccessibilityTreeGenerator.GenerateSubtree(contextElement.hierarchy[i], s_Hierarchy, parentNode, s_NodeMap, ref cursor, previousNodes, visited);
                }
            }

            // Open dropdown segments come from a foreign element tree (the menu mounts at the
            // panel root), so a context walk outside the menu never visits them; sparing them
            // keeps churn near the field from tearing down its open menu. A context inside the
            // menu (item churn) sweeps normally — there, the walk does visit the live nodes.
            SweepStaleNodes(previousNodes, visited,
                spareOpenDropdownSegments: !IsInOpenDropdownSegment(parentNode));
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

        // A geometry bit fires on the topmost element whose parent-relative rect changed; the
        // worldBounds of everything below shift with it without further bits, so the refresh
        // walks each pending element's subtree. Pending elements sitting under another pending
        // element are covered by that ancestor's walk and skipped.
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
            }

            foreach (var element in s_CurrentPanelPending)
            {
                if (!IsUnderAny(element.hierarchy.parent, pendingRoots))
                    RefreshSubtreeFrames(element);
            }
        }

        static void RefreshSubtreeFrames(VisualElement element)
        {
            if (s_NodeMap.TryGetNode(element, out var node))
            {
                // Pushing through the setter forwards to native even when the value is unchanged,
                // which native needs for its own screen-coordinate conversion; while the hierarchy
                // is inactive, this is a managed-only write.
                node.frame = AccessibilityFrameProjection.GetScreenFrame(element);

                // A content host's content is mapped, so the walk continues through its content
                // container (skipping the chrome); a scroll view's percentage depends on this
                // same geometry, so it refreshes here too — guarded, since this runs per frame
                // (see ScrollPercentageIsCurrent).
                if (AccessibilityTreeGenerator.TryGetContentHost(element, out var content))
                {
                    if (!AccessibilityTreeGenerator.ScrollPercentageIsCurrent(node.value, element))
                        node.value = AccessibilityTreeGenerator.DeriveValue(element);

                    RefreshSubtreeFrames(content);
                    return;
                }

                // Known controls are accessibility leaves; their internals are never mapped.
                if (AccessibilityRoleRegistry.TryGetRole(element, out _))
                    return;
            }

            for (var i = 0; i < element.hierarchy.childCount; i++)
            {
                RefreshSubtreeFrames(element.hierarchy[i]);
            }
        }

        #endregion // Per-frame flush
    }
}
