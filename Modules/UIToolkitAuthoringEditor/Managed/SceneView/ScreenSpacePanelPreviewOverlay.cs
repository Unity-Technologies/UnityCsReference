// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: UIToolkitAuthoringFramework not yet converted
using System.Collections.Generic;
using Unity.UIToolkit.Editor.Utilities;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.Overlays;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Unity.UIToolkit.Editor
{
    // Read-only, scaled-to-fit preview of the selected screen-space panel (PanelRenderer / UIDocument),
    // which otherwise doesn't render in the Scene. Reuses PanelElement and stays non-interactive.
    // Selected elements get read-only outlines; clicking one selects it in the hierarchy (repeated
    // clicks walk up ancestors); the CTA / double-click opens it in the UI Viewport. Shown only while
    // a screen-space panel is selected (ITransientOverlay). See UI-5290.
    [Overlay(typeof(SceneView), k_OverlayId, k_DisplayName, defaultDisplay = false,
        defaultDockZone = DockZone.LeftColumn, defaultDockPosition = DockPosition.Bottom)]
    [Icon("UIToolkit/Icons/UIViewportWindow.png")]
    sealed class ScreenSpacePanelPreviewOverlay : Overlay, ITransientOverlay
    {
        const string k_OverlayId = "SceneView/ScreenSpacePanelPreview";
        const string k_DisplayName = "UI Preview";

        // Base + dark/light stylesheets from the module's editor default resources.
        const string k_StyleSheet = "UIToolkitAuthoring/UIPreviewOverlay/UIPreviewOverlay.uss";
        const string k_StyleSheetDark = "UIToolkitAuthoring/UIPreviewOverlay/UIPreviewOverlayDark.uss";
        const string k_StyleSheetLight = "UIToolkitAuthoring/UIPreviewOverlay/UIPreviewOverlayLight.uss";

        const string k_RootUssClass = "unity-ui-preview-overlay";
        const string k_PreviewContainerUssClass = k_RootUssClass + "__preview-container";
        const string k_ScreenUssClass = k_RootUssClass + "__screen";
        const string k_CheckerboardUssClass = k_RootUssClass + "__checkerboard";
        const string k_PreviewSurfaceUssClass = k_RootUssClass + "__preview-surface";
        const string k_SelectionHighlightLayerUssClass = k_RootUssClass + "__selection-highlight-layer";
        const string k_SelectionHighlightUssClass = k_RootUssClass + "__selection-highlight";
        const string k_SelectionOutlineUssClass = k_RootUssClass + "__selection-outline";
        const string k_EmptyStateUssClass = k_RootUssClass + "__empty-state";
        const string k_EditButtonUssClass = k_RootUssClass + "__edit-button";
        const string k_HiddenUssClass = k_RootUssClass + "--hidden";

        // Compact default; resizable (the framework remembers the user's size).
        static readonly Vector2 k_DefaultSize = new(240, 200);
        static readonly Vector2 k_MinSize = new(160, 140);
        static readonly Vector2 k_MaxSize = new(2000, 2000);

        // Fallback when the panel has no usable reference resolution.
        static readonly Vector2 k_DefaultScreenSize = new(1200, 800);

        // Auto-fit into the painted UI: margin, zoom cap, and the alpha floor for "painted".
        const float k_ContentFitPadding = 0.95f;
        const float k_MaxContentZoom = 32f;
        const float k_MinVisibleAlpha = 0.02f;

        PanelElement m_PanelElement;
        VisualElement m_PreviewContainer;
        VisualElement m_Screen;
        CheckerboardBackground m_Checkerboard;
        VisualElement m_SelectionHighlightLayer;
        Label m_EmptyState;
        Button m_EditButton;

        // Pooled selection outlines, one per highlighted element; extras hidden, not destroyed.
        readonly List<VisualElement> m_SelectionHighlights = new();

        // Previewed panel component; may be a destroyed (fake-null) Object, checked via IsAlive.
        IPanelComponent m_Target;

        // What the preview is bound to; Update re-binds when the selection's document/settings change.
        VisualTreeAsset m_BoundAsset;
        PanelSettings m_BoundSettings;

        // Layout-affecting settings the fit was computed from; in-place edits don't change the settings
        // reference, so Update watches this to re-fit.
        (Vector2Int reference, PanelScaleMode scaleMode, float scale, PanelScreenMatchMode matchMode, float match) m_BoundLayoutSignature;

        // Flags a re-clone when the bound document or a nested template changes; the host panel's
        // asset-change updater notifies on in-memory edits and reimports.
        class ContentTracker(ScreenSpacePanelPreviewOverlay owner)
            : BaseLiveReloadVisualTreeAssetTracker, IAuthoringLiveReloadAssetTracker<VisualTreeAsset>
        {
            internal override void OnVisualTreeAssetChanged() => owner.m_ContentDirty = true;
        }

        readonly ContentTracker m_ContentTracker;
        readonly List<VisualTreeAsset> m_TrackedContent = new();
        ILiveReloadSystem m_ContentLiveReload;
        bool m_ContentDirty;

        // Rendering must run inside a panel tick (see OnBeforeTickingAnyScheduledPanel), not the idle
        // update tick, which wedges the Metal backend on macOS; idle-tick callers flag this instead.
        bool m_LayoutDirty;

        // Live selected elements, each mapped to its exact preview clone for the outlines (instance-aware).
        // Ones not in the previewed document don't resolve to a clone and are skipped.
        readonly List<VisualElement> m_SelectedElements = new();

        // Repeated-click ancestor cycling. The min delay avoids treating a double-click as a cycle
        // step; the pick stack is kept as assets so it survives re-cloning.
        const float k_CycleClickMaxDistance = 4f;
        const long k_CycleClickMinDelayMs = 500;
        const long k_CycleClickMaxDelayMs = 1500;

        readonly List<VisualElementAsset> m_LastCycleAssets = new();
        Vector2 m_LastClickPanelPosition;
        long m_LastClickTimestampMs;
        int m_CycleIndex = -1;

        public ScreenSpacePanelPreviewOverlay()
        {
            defaultSize = k_DefaultSize;
            minSize = k_MinSize;
            maxSize = k_MaxSize;
            m_ContentTracker = new ContentTracker(this);
        }

        // ITransientOverlay: shown only while a screen-space panel is selected.
        public bool visible => IsScreenSpace(m_Target);

        public override VisualElement CreatePanelContent()
        {
            // Runs during layout restore, so it must not build the PanelElement (deferred to Bind).
            // Fresh root each call; an existing PanelElement is re-parented into the new screen.
            var root = new VisualElement { name = "UI Preview Overlay" };
            root.AddToClassList(k_RootUssClass);
            ApplyStyleSheets(root);

            m_PreviewContainer = new VisualElement { name = "preview-container" };
            m_PreviewContainer.AddToClassList(k_PreviewContainerUssClass);
            m_PreviewContainer.RegisterCallback<GeometryChangedEvent>(OnPreviewGeometryChanged);
            // Double-click = "Edit in UI Viewport"; single click selects (OnPreviewPointerUp).
            m_PreviewContainer.RegisterCallback<PointerDownEvent>(OnPreviewPointerDown);
            m_PreviewContainer.RegisterCallback<PointerUpEvent>(OnPreviewPointerUp);
            // Content tracking needs the host panel; it also covers a Bind that ran before attach.
            m_PreviewContainer.RegisterCallback<AttachToPanelEvent>(OnPreviewAttached);
            m_PreviewContainer.RegisterCallback<DetachFromPanelEvent>(OnPreviewDetached);
            root.Add(m_PreviewContainer);

            // Transparency checkerboard behind the UI. It doesn't auto-regenerate on resize, so
            // repaint it once its geometry settles.
            m_Checkerboard = new CheckerboardBackground();
            m_Checkerboard.AddToClassList(k_CheckerboardUssClass);
            m_Checkerboard.RegisterCallback<GeometryChangedEvent>(OnCheckerboardGeometryChanged);
            m_PreviewContainer.Add(m_Checkerboard);

            // Sized to the fitted panel rect; hosts the panel RenderTexture above the checkerboard.
            m_Screen = new VisualElement { name = "screen" };
            m_Screen.AddToClassList(k_ScreenUssClass);
            m_PreviewContainer.Add(m_Screen);

            if (m_PanelElement != null)
                m_Screen.Add(m_PanelElement);

            // Selection-outline layer, above the panel (re-inserted below it in Bind). Pool is reset
            // here since a fresh screen discards the old outlines.
            m_SelectionHighlights.Clear();
            m_SelectionHighlightLayer = new VisualElement { name = "selection-highlights", pickingMode = PickingMode.Ignore };
            m_SelectionHighlightLayer.AddToClassList(k_SelectionHighlightLayerUssClass);
            m_Screen.Add(m_SelectionHighlightLayer);

            m_EmptyState = new Label
            {
                name = "empty-state",
                text = L10n.Tr("No UI Document assigned", null),
            };
            m_EmptyState.AddToClassList(k_EmptyStateUssClass);
            m_PreviewContainer.Add(m_EmptyState);

            m_EditButton = new Button(EditInViewport) { name = "edit-button", text = L10n.Tr("Edit in UI Viewport", null) };
            m_EditButton.AddToClassList(k_EditButtonUssClass);
            root.Add(m_EditButton);

            RefreshContentState();
            return root;
        }

        // Base stylesheet first, then the themed (dark/light) overrides on top.
        static void ApplyStyleSheets(VisualElement root)
        {
            if (EditorGUIUtility.Load(k_StyleSheet) is StyleSheet baseSheet)
                root.styleSheets.Add(baseSheet);

            var themedPath = EditorGUIUtility.isProSkin ? k_StyleSheetDark : k_StyleSheetLight;
            if (EditorGUIUtility.Load(themedPath) is StyleSheet themedSheet)
                root.styleSheets.Add(themedSheet);
        }

        public override void OnCreated()
        {
            Selection.selectionChanged += OnSelectionChanged;
            // Re-resolve after undo (it can rebuild the panel / fire selection events).
            Undo.undoRedoPerformed += OnSelectionChanged;
            EditorApplication.update += Update;
            // Drives the actual panel render, in a valid render context (see OnBeforeTickingAnyScheduledPanel).
            Panel.beforeTickingAnyScheduledPanel += OnBeforeTickingAnyScheduledPanel;

            // OnCreated runs during the Scene view's layout restore; building the runtime panel
            // mid-restore corrupts sibling windows' deserialization, so defer the initial resolve.
            EditorApplication.delayCall += OnSelectionChanged;
        }

        void DestroyPreviewPanel()
        {
            if (m_PanelElement == null)
                return;

            m_PanelElement.RemoveFromHierarchy();
            m_PanelElement.DestroyPanelPermanently();
            m_PanelElement = null;
        }

        public override void OnWillBeDestroyed()
        {
            Selection.selectionChanged -= OnSelectionChanged;
            Undo.undoRedoPerformed -= OnSelectionChanged;
            EditorApplication.update -= Update;
            EditorApplication.delayCall -= OnSelectionChanged;
            Panel.beforeTickingAnyScheduledPanel -= OnBeforeTickingAnyScheduledPanel;

            if (m_PreviewContainer != null)
            {
                m_PreviewContainer.UnregisterCallback<GeometryChangedEvent>(OnPreviewGeometryChanged);
                m_PreviewContainer.UnregisterCallback<PointerDownEvent>(OnPreviewPointerDown);
                m_PreviewContainer.UnregisterCallback<PointerUpEvent>(OnPreviewPointerUp);
                m_PreviewContainer.UnregisterCallback<AttachToPanelEvent>(OnPreviewAttached);
                m_PreviewContainer.UnregisterCallback<DetachFromPanelEvent>(OnPreviewDetached);
                m_PreviewContainer = null;
            }

            UntrackContent();

            if (m_Checkerboard != null)
            {
                m_Checkerboard.UnregisterCallback<GeometryChangedEvent>(OnCheckerboardGeometryChanged);
                m_Checkerboard = null;
            }

            m_Screen = null;
            m_SelectionHighlightLayer = null;
            m_SelectionHighlights.Clear();
            m_EmptyState = null;
            m_EditButton = null;

            DestroyPreviewPanel();

            m_Target = null;
            m_SelectedElements.Clear();
        }

        void EnsurePanelElement()
        {
            if (m_PanelElement != null)
                return;

            m_PanelElement = new PanelElement();
            m_PanelElement.CreateSubPanel();
            // Read-only: no event forwarding, hover, picking, or animation ticking.
            m_PanelElement.ForwardHierarchicalEvents = false;
            m_PanelElement.EnableAnimationSystem(false);
            m_PanelElement.pickingMode = PickingMode.Ignore;
            m_PanelElement.AddToClassList(k_PreviewSurfaceUssClass);
        }

        void OnSelectionChanged()
        {
            var target = ResolveTarget();
            m_Target = target;

            if (target == null)
                ClearPreview();
            else
                Bind(target);

            UpdateBoundHeader();
        }

        // Nearest screen-space panel from the selection (a GameObject, a panel-root selection, or an
        // element). Also refreshes the selected elements backing the outlines.
        IPanelComponent ResolveTarget()
        {
            CollectSelectedElements();

            foreach (var obj in Selection.objects)
            {
                IPanelComponent component;
                switch (obj)
                {
                    case GameObject go:
                        component = go.GetComponentInParent<IPanelComponent>(true);
                        break;
                    case VisualTreeAssetSelection { PanelComponent: { } panelComponent }:
                        component = panelComponent;
                        break;
                    case VisualElementSelection { Element: { } element }:
                        component = ResolvePanelComponentForElement(element);
                        break;
                    default:
                        continue;
                }

                if (IsScreenSpace(component))
                    return component;
            }

            return null;
        }

        // Refreshes the outline-backing elements from the current selection. The selection objects are
        // repointed in place on edits (no selectionChanged), so this must re-run on every re-bind.
        void CollectSelectedElements()
        {
            m_SelectedElements.Clear();
            foreach (var obj in Selection.objects)
                if (obj is VisualElementSelection { Element: { visualElementAsset: not null } selectedElement })
                    m_SelectedElements.Add(selectedElement);
        }

        // A selected element can be stale or a stage clone; redirect it to a live scene instance before
        // resolving its panel (mirrors VisualElementSceneViewOverlay).
        static IPanelComponent ResolvePanelComponentForElement(VisualElement element)
        {
            var stagePanel = (StageUtility.GetCurrentStage() as VisualElementEditingStage)?.GetAuthoringPanel();
            if (element.panel == null || (stagePanel != null && element.panel == stagePanel))
                element = VisualElementToolUtility.FindFirstSceneInstanceOfAsset(element.visualElementAsset);

            return element != null ? VisualElementSceneViewOverlay.FindPanelComponentForElement(element) : null;
        }

        void Bind(IPanelComponent target)
        {
            EnsurePanelElement();

            // Parent the panel into the screen, below the outlines (index 0), if not already.
            if (m_Screen != null && m_PanelElement.parent != m_Screen)
                m_Screen.Insert(0, m_PanelElement);

            m_PanelElement.PanelSettings = target.panelSettings;

            var subRoot = m_PanelElement.subRootVisualElement;
            subRoot?.Clear();

            var visualTreeAsset = target.visualTreeAsset;
            if (visualTreeAsset != null && subRoot != null)
                visualTreeAsset.CloneTree(subRoot);

            m_BoundAsset = visualTreeAsset;
            m_BoundSettings = target.panelSettings;
            m_BoundLayoutSignature = target.panelSettings != null ? GetLayoutSignature(target.panelSettings) : default;

            TrackContent();
            m_ContentDirty = false;

            RefreshContentState();
            RequestLayout();
        }

        void ClearPreview()
        {
            // Release the panel + RenderTexture while nothing is previewed (rebuilt on next Bind), so
            // the transient overlay doesn't pin GPU memory while cleared.
            DestroyPreviewPanel();

            m_BoundAsset = null;
            m_BoundSettings = null;
            m_BoundLayoutSignature = default;

            UntrackContent();
            m_ContentDirty = false;

            foreach (var highlight in m_SelectionHighlights)
                highlight.EnableInClassList(k_HiddenUssClass, true);
            ResetCycleState();

            RefreshContentState();
        }

        void OnPreviewAttached(AttachToPanelEvent evt)
        {
            TrackContent();
            // Edits made while detached went unobserved.
            m_ContentDirty = true;
        }

        void OnPreviewDetached(DetachFromPanelEvent evt) => UntrackContent();

        // (Re-)registers the tracker for the bound document and its nested templates on the host panel.
        void TrackContent()
        {
            UntrackContent();

            m_ContentLiveReload = ((Panel)m_PreviewContainer?.panel)?.liveReloadSystem;
            if (m_ContentLiveReload == null || m_BoundAsset == null)
                return;

            CollectContentAssets(m_BoundAsset, m_TrackedContent);
            foreach (var asset in m_TrackedContent)
                m_ContentLiveReload.RegisterAuthoringTrackerForAsset(m_ContentTracker, asset);
        }

        void UntrackContent()
        {
            foreach (var asset in m_TrackedContent)
                m_ContentLiveReload?.UnregisterAuthoringTrackerForAsset(m_ContentTracker, asset);
            m_TrackedContent.Clear();
            m_ContentLiveReload = null;
        }

        // The document and its nested templates, deduped.
        internal static void CollectContentAssets(VisualTreeAsset asset, List<VisualTreeAsset> result)
        {
            if (asset == null || result.Contains(asset))
                return;

            result.Add(asset);
            foreach (var dependency in asset.templateDependencies)
                CollectContentAssets(dependency, result);
        }

        // Show the empty-state prompt vs. the preview, and keep the CTA label in sync.
        void RefreshContentState()
        {
            if (m_PreviewContainer == null)
                return;

            var hasTarget = m_Target != null;
            var hasContent = hasTarget && m_Target.visualTreeAsset != null;

            m_EmptyState?.EnableInClassList(k_HiddenUssClass, !(hasTarget && !hasContent));
            m_Screen?.EnableInClassList(k_HiddenUssClass, !hasContent);
            m_Checkerboard?.EnableInClassList(k_HiddenUssClass, !hasContent);

            if (m_EditButton != null)
            {
                m_EditButton.EnableInClassList(k_HiddenUssClass, !hasTarget);
                m_EditButton.text = L10n.Tr(hasContent ? "Edit in UI Viewport" : "Open UI Viewport", null);
            }
        }

        // Reflect the bound document in the overlay title (e.g. "UI Preview · PauseScreen").
        void UpdateBoundHeader()
        {
            var visualTreeAsset = IsAlive(m_Target) ? m_Target.visualTreeAsset : null;
            displayName = visualTreeAsset != null ? $"{k_DisplayName}  ·  {visualTreeAsset.name}" : k_DisplayName;
        }

        void OnPreviewGeometryChanged(GeometryChangedEvent evt)
        {
            RequestLayout();
        }

        void OnCheckerboardGeometryChanged(GeometryChangedEvent evt)
        {
            m_Checkerboard?.MarkDirtyRepaint();
        }

        void OnPreviewPointerDown(PointerDownEvent evt)
        {
            if (evt.button == 0 && evt.clickCount == 2)
            {
                EditInViewport();
                evt.StopPropagation();
            }
        }

        void OnPreviewPointerUp(PointerUpEvent evt)
        {
            // Single click selects; double-click is the Edit shortcut (pointer-down).
            if (evt.button != 0 || evt.clickCount != 1 || m_PanelElement?.SubPanel == null)
                return;

            // Pointer position in sub-panel space (handles the letterboxing + fit scale).
            var panelPosition = m_PanelElement.LocalToPanelPosition(m_PanelElement.WorldToLocal(evt.position));
            var authoritativeRoot = IsAlive(m_Target) ? m_Target.GetRootVisualElement() : null;

            if (PerformSelection(panelPosition, evt.timestamp, authoritativeRoot))
                evt.StopPropagation();
        }

        // Selects the element under the cursor, cycling up ancestors on repeated same-spot clicks.
        // authoritativeRoot owns the selection objects; the clicked clone maps back to its counterpart.
        internal bool PerformSelection(Vector2 panelPosition, long timestampMs, VisualElement authoritativeRoot)
        {
            if (m_PanelElement?.SubPanel == null || authoritativeRoot == null)
                return false;

            using var _ = ListPool<VisualElement>.Get(out var picked);
            PickSelectableElements(panelPosition, authoritativeRoot, picked);

            if (picked.Count == 0)
            {
                // Empty click is a no-op (clearing the selection would hide the overlay).
                ResetCycleState();
                return false;
            }

            VisualElement target;
            if (IsCycleClick(panelPosition, timestampMs, picked))
            {
                m_CycleIndex = (m_CycleIndex + 1) % picked.Count;
                target = picked[m_CycleIndex];
            }
            else
            {
                m_CycleIndex = 0;
                target = picked[0];
            }

            m_LastClickPanelPosition = panelPosition;
            m_LastClickTimestampMs = timestampMs;
            m_LastCycleAssets.Clear();
            foreach (var element in picked)
                m_LastCycleAssets.Add(element.visualElementAsset);

            return SelectElement(target, authoritativeRoot);
        }

        // Selectable clones under the point, topmost-first: asset-less children resolve to their
        // authored ancestor, only selectable ones kept, deduped by element so overlapping repeated
        // instances stay distinct.
        void PickSelectableElements(Vector2 panelPosition, VisualElement authoritativeRoot, List<VisualElement> result)
        {
            using var _ = ListPool<VisualElement>.Get(out var rawPicks);
            m_PanelElement.SubPanel.PickAll(panelPosition, rawPicks);

            using var __ = HashSetPool<VisualElement>.Get(out var seen);
            foreach (var candidate in rawPicks)
            {
                var element = candidate.visualElementAsset != null
                    ? candidate
                    : candidate.GetFirstAncestorWhere(e => e.visualElementAsset != null);

                if (element == null)
                    continue;
                if (ResolveSelection(authoritativeRoot, element) == null)
                    continue;
                if (seen.Add(element))
                    result.Add(element);
            }
        }

        bool IsCycleClick(Vector2 panelPosition, long timestampMs, List<VisualElement> picked)
        {
            if (m_CycleIndex < 0 || m_LastCycleAssets.Count == 0 || picked.Count <= 1)
                return false;

            var elapsed = timestampMs - m_LastClickTimestampMs;
            if (elapsed < k_CycleClickMinDelayMs || elapsed > k_CycleClickMaxDelayMs)
                return false;

            if (Vector2.Distance(panelPosition, m_LastClickPanelPosition) > k_CycleClickMaxDistance)
                return false;

            if (m_LastCycleAssets.Count != picked.Count)
                return false;
            for (var i = 0; i < picked.Count; i++)
            {
                if (m_LastCycleAssets[i] != picked[i].visualElementAsset)
                    return false;
            }
            return true;
        }

        void ResetCycleState()
        {
            m_LastCycleAssets.Clear();
            m_CycleIndex = -1;
            m_LastClickTimestampMs = 0;
        }

        // Maps a preview-clone element to its hierarchy VisualElementSelection (instance-aware, so
        // repeated nested-UXML instances resolve to their own counterpart). Null if it has none.
        static VisualElementSelection ResolveSelection(VisualElement authoritativeRoot, VisualElement previewElement)
        {
            if (authoritativeRoot == null || previewElement == null)
                return null;

            var sceneElement = authoritativeRoot.FindCorrespondingElement(previewElement);
            if (sceneElement == null)
                return null;

            var stagePanel = (StageUtility.GetCurrentStage() as VisualElementEditingStage)?.GetAuthoringPanel();
            var selectionTarget = stagePanel != null
                ? sceneElement.FindCorrespondingStageClone(stagePanel) ?? sceneElement
                : sceneElement;

            return selectionTarget.GetSelectionObject<VisualElementSelection>();
        }

        static bool SelectElement(VisualElement previewElement, VisualElement authoritativeRoot)
        {
            var selectionObject = ResolveSelection(authoritativeRoot, previewElement);
            if (selectionObject == null)
                return false;

            Selection.entityIds = new[] { selectionObject.GetEntityId() };
            return true;
        }

        // Stages the bound document and opens/focuses the UI Viewport (no duplicate if already staging it).
        void EditInViewport()
        {
            if (!IsAlive(m_Target))
                return;

            var visualTreeAsset = m_Target.visualTreeAsset;
            var panelSettings = m_Target.panelSettings;
            if (visualTreeAsset != null && !IsStaging(visualTreeAsset, panelSettings))
            {
                var context = new VisualTreeAssetEditingContext(visualTreeAsset, panelSettings);
                VisualElementEditingStage.GoToStage(context, BreadcrumbBar.SeparatorStyle.Line);
            }

            EditorWindow.GetWindow<UIViewportWindow>(null, true, typeof(SceneView));
        }

        // Match document and PanelSettings: the same UXML in another panel would carry the wrong settings.
        static bool IsStaging(VisualTreeAsset visualTreeAsset, PanelSettings panelSettings)
        {
            return StageUtility.GetCurrentStage() is VisualElementEditingStage stage
                && stage.EditedVisualTreeAsset == visualTreeAsset
                && stage.Context.PanelSettings == panelSettings;
        }

        // Defers the GPU-rendering layout to the next panel tick (see OnBeforeTickingAnyScheduledPanel);
        // the repaint nudge guarantees that tick even when requested from the idle update tick.
        void RequestLayout()
        {
            m_LayoutDirty = true;
            m_PreviewContainer?.MarkDirtyRepaint();
        }

        // Fits the preview to the pane. Aspect comes from the pane (not the Game View): the reference
        // resolution is reshaped to the pane's aspect; PanelSettings drives only the content scaling.
        void UpdateLayout()
        {
            if (m_PreviewContainer == null || m_PanelElement == null || m_Target == null)
                return;

            var settings = m_Target.panelSettings;
            if (settings == null || m_Target.visualTreeAsset == null)
                return;

            var available = m_PreviewContainer.contentRect.size;
            if (!IsUsable(available.x) || !IsUsable(available.y) || available.x <= 0 || available.y <= 0)
                return;

            // Design resolution (independent of the Game View).
            Vector2 reference = settings.referenceResolution;
            if (!IsUsable(reference.x) || !IsUsable(reference.y) || reference.x <= 0 || reference.y <= 0)
                reference = k_DefaultScreenSize;

            // Reshape the reference to the pane's aspect (grow the shorter side) so nothing is letterboxed.
            var paneAspect = available.x / available.y;
            var referenceAspect = reference.x / reference.y;
            var screen = paneAspect > referenceAspect
                ? new Vector2(reference.y * paneAspect, reference.y)
                : new Vector2(reference.x, reference.x / paneAspect);

            var fit = Mathf.Min(available.x / screen.x, available.y / screen.y);
            if (fit <= 0 || !IsUsable(fit))
                return;

            var displaySize = screen * fit;

            // Size the screen (checkerboard + surface fill it).
            if (m_Screen != null)
            {
                m_Screen.style.width = displaySize.x;
                m_Screen.style.height = displaySize.y;
            }

            m_Checkerboard?.MarkDirtyRepaint();

            // First pass: lay out at the full-screen fit.
            m_PanelElement.SubPanelPixelsPerPoint = GetPixelsPerPoint(m_PreviewContainer);
            m_PanelElement.ResizeRenderTexture(displaySize);
            m_PanelElement.Offset = Vector2.zero;
            m_PanelElement.ScaleFactor = fit;
            m_PanelElement.Size = screen;
            m_PanelElement.FrameUpdate();

            // Auto-fit: zoom/center into the painted UI to trim empty space (no-op if it already fills).
            if (TryComputeContentBounds(out var content))
            {
                var zoom = Mathf.Min(displaySize.x / content.width, displaySize.y / content.height) * k_ContentFitPadding;
                zoom = Mathf.Clamp(zoom, 1f, k_MaxContentZoom);
                if (zoom > 1f + 1e-3f)
                {
                    m_PanelElement.Offset = (displaySize - content.size * zoom) * 0.5f - content.position * zoom;
                    m_PanelElement.ScaleFactor = fit * zoom;
                    m_PanelElement.FrameUpdate();
                }
            }

            UpdateSelectionHighlights();
        }

        // Bounds of the painted UI in preview-surface points (worldBound / pixelsPerPoint), skipping
        // unpainted containers so a floating UI reports its own bounds. False when nothing is painted.
        internal bool TryComputeContentBounds(out Rect bounds)
        {
            bounds = default;

            var subRoot = m_PanelElement?.subRootVisualElement;
            if (subRoot == null)
                return false;

            var pixelsPerPoint = m_PanelElement.SubPanelPixelsPerPoint;
            if (!IsUsable(pixelsPerPoint) || pixelsPerPoint <= 0)
                return false;

            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);
            var found = false;

            subRoot.Query<VisualElement>().ForEach(element =>
            {
                if (!PaintsContent(element))
                    return;

                var wb = element.worldBound;
                if (!IsUsable(wb.x) || !IsUsable(wb.y) || !IsUsable(wb.width) || !IsUsable(wb.height) ||
                    wb.width <= 0 || wb.height <= 0)
                    return;

                // Clip to the visible area so offscreen ScrollView/overflow children don't inflate it.
                var clip = element.worldClip;
                if (IsUsable(clip.x) && IsUsable(clip.y) && IsUsable(clip.width) && IsUsable(clip.height))
                    wb = Intersection(wb, clip);
                if (wb.width <= 0 || wb.height <= 0)
                    return;

                min = Vector2.Min(min, new Vector2(wb.xMin, wb.yMin) / pixelsPerPoint);
                max = Vector2.Max(max, new Vector2(wb.xMax, wb.yMax) / pixelsPerPoint);
                found = true;
            });

            if (!found)
                return false;

            bounds = new Rect(min, max - min);
            return bounds.width > 0 && bounds.height > 0;
        }

        // "Draws something" (vs. a layout container). Layout-only bounds can't be used — they'd
        // include the full-screen transparent root and defeat the fit.
        static bool PaintsContent(VisualElement element)
        {
            var resolved = element.resolvedStyle;

            if (resolved.backgroundColor.a > k_MinVisibleAlpha)
                return true;

            var background = resolved.backgroundImage;
            if (background.texture != null || background.sprite != null ||
                background.renderTexture != null || background.vectorImage != null)
                return true;

            var hasBorderWidth = resolved.borderTopWidth > 0f || resolved.borderRightWidth > 0f ||
                                 resolved.borderBottomWidth > 0f || resolved.borderLeftWidth > 0f;
            var hasBorderColor = resolved.borderTopColor.a > k_MinVisibleAlpha || resolved.borderRightColor.a > k_MinVisibleAlpha ||
                                 resolved.borderBottomColor.a > k_MinVisibleAlpha || resolved.borderLeftColor.a > k_MinVisibleAlpha;
            if (hasBorderWidth && hasBorderColor)
                return true;

            if (element is TextElement { text: { Length: > 0 } } && resolved.color.a > k_MinVisibleAlpha)
                return true;

            return element is Image { } image && (image.image != null || image.sprite != null || image.vectorImage != null);
        }

        // Outlines each selected element over its exact preview clone (instance-aware). Pooled outlines;
        // unused ones hidden; elements not in this document skipped.
        internal void UpdateSelectionHighlights()
        {
            if (m_SelectionHighlightLayer == null)
                return;

            var subRoot = m_PanelElement?.subRootVisualElement;
            var pixelsPerPoint = m_PanelElement?.SubPanelPixelsPerPoint ?? 0f;

            var shown = 0;
            if (subRoot != null && IsUsable(pixelsPerPoint) && pixelsPerPoint > 0)
            {
                foreach (var selected in m_SelectedElements)
                {
                    var clone = subRoot.FindCorrespondingElement(selected);
                    if (clone == null)
                        continue;

                    var bounds = clone.worldBound;
                    // Skip not-yet-laid-out clones (zero/NaN bounds).
                    if (!IsUsable(bounds.x) || !IsUsable(bounds.y) ||
                        !IsUsable(bounds.width) || !IsUsable(bounds.height) ||
                        bounds.width <= 0 || bounds.height <= 0)
                        continue;

                    var highlight = GetOrCreateSelectionHighlight(shown);
                    highlight.style.left = bounds.x / pixelsPerPoint;
                    highlight.style.top = bounds.y / pixelsPerPoint;
                    highlight.style.width = bounds.width / pixelsPerPoint;
                    highlight.style.height = bounds.height / pixelsPerPoint;
                    highlight.EnableInClassList(k_HiddenUssClass, false);
                    shown++;
                }
            }

            // Hide unused pooled outlines.
            for (var i = shown; i < m_SelectionHighlights.Count; i++)
                m_SelectionHighlights[i].EnableInClassList(k_HiddenUssClass, true);
        }

        // Pooled outline at index; created (bordered from the shared pref) and parented on demand.
        VisualElement GetOrCreateSelectionHighlight(int index)
        {
            if (index < m_SelectionHighlights.Count)
                return m_SelectionHighlights[index];

            var highlight = new VisualElement { name = "selection-highlight", pickingMode = PickingMode.Ignore };
            highlight.AddToClassList(k_SelectionHighlightUssClass);
            highlight.AddToClassList(k_HiddenUssClass);

            var outline = new VisualElement { name = "selection-outline", pickingMode = PickingMode.Ignore };
            outline.AddToClassList(k_SelectionOutlineUssClass);
            outline.SetInlineBorderColor(ColorPreferences.SelectionOutline);
            highlight.Add(outline);

            m_SelectionHighlightLayer.Add(highlight);
            m_SelectionHighlights.Add(highlight);
            return highlight;
        }

        // Idle-tick bookkeeping only: target liveness, document/settings re-binds, layout-setting edits.
        // Must NOT render the panel (see OnBeforeTickingAnyScheduledPanel); it only flags a deferred layout.
        void Update()
        {
            if (m_PanelElement == null)
                return;

            // Re-resolve if the target died or went world-space.
            if (m_Target != null && (!IsAlive(m_Target) || !IsScreenSpace(m_Target)))
            {
                OnSelectionChanged();
                return;
            }

            // Re-bind if the selection's document / settings reference changed.
            if (m_Target != null &&
                (m_Target.visualTreeAsset != m_BoundAsset || m_Target.panelSettings != m_BoundSettings))
            {
                CollectSelectedElements();
                ResetCycleState();
                Bind(m_Target);
                UpdateBoundHeader();
                return;
            }

            if (displayed && m_Target != null && m_Target.visualTreeAsset != null)
            {
                // Re-clone the preview to follow in-place document edits (element created, moved...).
                if (m_ContentDirty)
                {
                    // Re-collect the stale elements and drop the in-flight click-cycle: the edit repointed
                    // the selection objects in place and changed the pick stack.
                    CollectSelectedElements();
                    ResetCycleState();
                    Bind(m_Target);
                    UpdateBoundHeader();
                    return;
                }

                var settings = m_Target.panelSettings;
                // Re-fit on in-place edits to layout settings (the reference didn't change, so Bind
                // isn't hit).
                if (settings != null)
                {
                    var signature = GetLayoutSignature(settings);
                    if (signature != m_BoundLayoutSignature)
                    {
                        m_BoundLayoutSignature = signature;
                        RequestLayout();
                    }
                }
            }
        }

        // Renders the preview inside a panel tick — a valid render context (the hook UICanvas uses).
        // FrameUpdate/UpdateLayout from the idle update tick wedge the Metal backend on macOS.
        void OnBeforeTickingAnyScheduledPanel(Panel p)
        {
            if (m_PreviewContainer == null || p != m_PreviewContainer.panel)
                return;

            if (m_PanelElement == null || !displayed || !IsAlive(m_Target) || m_Target.visualTreeAsset == null)
                return;

            if (m_LayoutDirty)
            {
                m_LayoutDirty = false;
                UpdateLayout();
                return;
            }

            m_PanelElement.FrameUpdate();
            UpdateSelectionHighlights();
        }

        // Layout-affecting settings, compared each tick to detect in-place edits.
        static (Vector2Int, PanelScaleMode, float, PanelScreenMatchMode, float) GetLayoutSignature(PanelSettings settings)
            => (settings.referenceResolution, settings.scaleMode, settings.scale, settings.screenMatchMode, settings.match);

        static bool IsScreenSpace(IPanelComponent component)
        {
            if (!IsAlive(component))
                return false;

            var settings = component.panelSettings;
            return settings != null && settings.renderMode != PanelRenderMode.WorldSpace;
        }

        // Backed by a UnityEngine.Object, so use Unity null semantics for destroyed objects.
        static bool IsAlive(IPanelComponent component)
        {
            return component is Object obj ? obj != null : component != null;
        }

        static bool IsUsable(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        // Rect overlap (non-positive size when disjoint).
        static Rect Intersection(Rect a, Rect b)
        {
            var xMin = Mathf.Max(a.xMin, b.xMin);
            var yMin = Mathf.Max(a.yMin, b.yMin);
            var xMax = Mathf.Min(a.xMax, b.xMax);
            var yMax = Mathf.Min(a.yMax, b.yMax);
            return new Rect(xMin, yMin, xMax - xMin, yMax - yMin);
        }

        static float GetPixelsPerPoint(VisualElement element)
        {
            return element?.panel == null
                ? EditorGUIUtility.pixelsPerPoint
                : element.scaledPixelsPerPoint;
        }

        // --- Test seams ---

        internal PanelElement PanelElementForTests => m_PanelElement;

        internal void SetSelectedElementsForTests(params VisualElement[] elements)
        {
            m_SelectedElements.Clear();
            foreach (var element in elements)
                if (element != null)
                    m_SelectedElements.Add(element);
        }

        internal void CollectSelectedElementsForTests() => CollectSelectedElements();

        // Shown outlines, in layout order.
        internal List<VisualElement> GetVisibleSelectionHighlightsForTests()
        {
            var visible = new List<VisualElement>();
            foreach (var highlight in m_SelectionHighlights)
                if (!highlight.ClassListContains(k_HiddenUssClass))
                    visible.Add(highlight);
            return visible;
        }

        // Builds the preview at a fixed size, bypassing UpdateLayout. Call after CreatePanelContent.
        internal void BuildPreviewForTests(VisualTreeAsset visualTreeAsset, PanelSettings panelSettings, Vector2 screenSize)
        {
            EnsurePanelElement();
            if (m_Screen != null && m_PanelElement.parent != m_Screen)
                m_Screen.Insert(0, m_PanelElement);

            m_PanelElement.PanelSettings = panelSettings;

            var subRoot = m_PanelElement.subRootVisualElement;
            subRoot?.Clear();
            visualTreeAsset?.CloneTree(subRoot);

            m_PanelElement.SetPanelSize(screenSize);
            m_PanelElement.Size = screenSize;
            m_PanelElement.ScaleFactor = 1f;
            m_PanelElement.FrameUpdate();
        }
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
