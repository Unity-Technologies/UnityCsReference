// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.UIToolkit.Editor.Utilities;
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

enum CanvasEventMode
{
    None = 0,
    Pick = 1,
    Forward = 2
}

[UxmlElement(visibility = LibraryVisibility.Hidden)]
sealed partial class UICanvasDocumentRoot : VisualElement, IVisualElementChangeProcessor
{
    // For testing purposes
    internal const string PickerMenuPropertyKey = "unity-ui-viewport-picker-menu";

    readonly List<VisualElementSelection> m_ElementSelections = new();
    readonly Dictionary<VisualElementSelection, VisualElement> m_CanvasTargets = new();
    readonly VisualElement m_HandlesContainer;
    readonly SelectionHandleManager m_HandleManager;
    readonly VisualElement m_ManipulatorsContainer;
    readonly VisualElementManipulatorOverlayManager m_ManipulatorOverlayManager;
    readonly UICanvasInPlaceEditor m_UICanvasInPlaceEditor;
    PanelElement m_PanelElement;

    public float ZoomScale
    {
        set => m_ManipulatorOverlayManager.ZoomScale = value;
    }

    VisualElement m_HoveredElement;
    bool m_IsPointerOver;
    Vector2 m_LastPointerPickPosition;

    // When a user is clicking on the same spot, we want to cycle through the elements under the pointer, going from
    // front to back.
    const float k_CycleClickMaxDistance = 4f;
    const long k_CycleClickMinDelayMs = 500;
    const long k_CycleClickMaxDelayMs = 1500;

    readonly List<VisualElement> m_LastCycleList = new();
    Vector2 m_LastClickCanvasPosition;
    long m_LastClickTimestampMs;
    int m_CycleIndex = -1;

    // Rectangle selection: a pointer-down + drag past the threshold draws a rectangle and selects
    // every overlapping element on release. Below the threshold the gesture falls through to the
    // regular click path so a quick click still does PerformSelection.
    const float k_RectangleSelectionDragThreshold = 4f;
    const string k_RectangleSelectionOverlayUssClass = "unity-ui-viewport__rectangle-selection";

    VisualElement m_RectangleSelectionOverlay;
    bool m_PointerCaptured;
    bool m_DragInProgress;
    int m_DragPointerId = -1;
    Vector2 m_DragStartCanvasPosition = Vector2.zero;
    Vector2 m_DragStartPanelPosition = Vector2.zero;
    // Snapshot of Selection.entityIds taken at drag start so ESC / a capture loss can revert.
    EntityId[] m_SelectionSnapshot;

    // Drag-time preview of the resulting selection, drawn over the canvas while the rectangle selection
    // changes shape. Sits in a parallel container so the regular handles can be hidden as a unit
    // and re-shown on drag end.
    VisualElement m_RectangleSelectionPreviewHandlesContainer;
    SelectionHandleManager m_RectangleSelectionPreviewHandleManager;
    readonly List<VisualElementSelection> m_PreviewedSelections = new();

    VisualElement HoveredElement
    {
        get => m_HoveredElement;
        set
        {
            if (m_HoveredElement == value)
                return;
            m_HoveredElement = value;
            if (m_HoveredElement != null)
                HighlightUtility.RequestHighlights(m_HoveredElement, CommandSources.Viewport);
            else
                HighlightUtility.ClearHighlights();
        }
    }

    CanvasEventMode m_EventMode = CanvasEventMode.Pick;

    public CanvasEventMode EventMode
    {
        get => m_EventMode;
        set
        {
            if (m_EventMode == value)
                return;
            m_EventMode = value;
            switch (m_EventMode)
            {
                case CanvasEventMode.Pick:
                    m_HandlesContainer.style.display = DisplayStyle.Flex;
                    m_ManipulatorsContainer.style.display = DisplayStyle.Flex;
                    m_HandleManager.UpdateAllHandles();
                    m_ManipulatorOverlayManager.UpdateAllOverlays();
                    break;
                case CanvasEventMode.Forward:
                    m_HandlesContainer.style.display = DisplayStyle.None;
                    m_ManipulatorsContainer.style.display = DisplayStyle.None;
                    break;
            }
        }
    }

    public PanelElement PanelElement
    {
        get => m_PanelElement;
        internal set
        {
            if (m_PanelElement == value)
                return;
            Release(m_PanelElement);
            m_PanelElement = value;
            Acquire(m_PanelElement);
        }
    }

    /// <summary>
    /// Supplies the root of the live tree this canvas is a preview of, or <see langword="null"/> when what it
    /// renders is itself the tree the rest of the editor selects.
    /// </summary>
    /// <remarks>
    /// A delegate rather than a stored root: live reload rebuilds the tree under the same component, so the
    /// answer has to be re-asked rather than captured when the context is acquired.
    /// </remarks>
    internal Func<VisualElement> AuthoritativeRootProvider { get; set; }

    public override VisualElement contentContainer => null;

    public VisualElement OverlayLayer => m_HandlesContainer;
    
    public UICanvasInPlaceEditor InPlaceEditor => m_UICanvasInPlaceEditor;

    public UICanvasDocumentRoot()
    {
        focusable = true;

        m_HandlesContainer = new VisualElement { name = "canvas-handles", pickingMode = PickingMode.Ignore };
        hierarchy.Add(m_HandlesContainer);
        m_HandlesContainer.StretchToParentSize();

        m_ManipulatorsContainer = new VisualElement { name = "canvas-manipulators" };
        hierarchy.Add(m_ManipulatorsContainer);
        m_ManipulatorsContainer.StretchToParentSize();
        m_HandleManager = new SelectionHandleManager(m_HandlesContainer, ResolveCanvasTarget);
        m_ManipulatorOverlayManager = new VisualElementManipulatorOverlayManager(m_ManipulatorsContainer, ResolveCanvasTarget);
        m_UICanvasInPlaceEditor = new UICanvasInPlaceEditor(this);
    }

    protected override void HandleEventTrickleDown(EventBase evt)
    {
        if (EventMode == CanvasEventMode.Forward)
            PanelElement?.ForwardEventTrickleDown(evt);

        base.HandleEventTrickleDown(evt);
    }

    protected override void HandleEventBubbleUp(EventBase evt)
    {
        switch (evt)
        {
            case AttachToPanelEvent:
                Selection.selectionChanged += OnSelectionChanged;
                break;
            case DetachFromPanelEvent:
                Selection.selectionChanged -= OnSelectionChanged;
                m_ManipulatorOverlayManager.ReleaseAll();
                break;
            case GeometryChangedEvent:
                m_HandleManager.UpdateAllHandles();
                m_ManipulatorOverlayManager.UpdateAllOverlays();
                break;
            case PointerDownEvent pointerDownEvent when EventMode == CanvasEventMode.Pick:
                // The gesture never starts when there is nothing to select: no picker popup, and no rubber band.
                if (m_PanelElement == null || !CanSelectContent)
                    break;
                // Ctrl/Cmd + Right click opens the picker popup. Intercept here so the pan manipulator
                // on UIViewport — which would normally take any right-click drag — doesn't engage.
                if (pointerDownEvent.button == (int)MouseButton.RightMouse
                    && (pointerDownEvent.modifiers & (EventModifiers.Control | EventModifiers.Command)) != 0)
                {
                    PerformPickerPopup(
                        m_PanelElement.ConvertPosition(pointerDownEvent),
                        pointerDownEvent.position);
                    pointerDownEvent.StopPropagation();
                    break;
                }
                if (pointerDownEvent.button != 0)
                    break;
                // Skip the gestures that the canvas pan manipulator (UICanvasPanManipulator) owns:
                // Alt+Left on Win/Linux, Cmd+Alt+Left on macOS. Without this gate the rectangle selection
                // briefly engages, pan steals the pointer capture, and the resulting
                // PointerCaptureOut doesn't always come back in time to hide the rectangle.
                if (IsPanModifierHeld(pointerDownEvent.modifiers))
                    break;

                // We set the flag here, but we will capture lazily on first PointerMove past the drag threshold as
                // capturing on every PointerDown stole the gesture from other potential manipulators (e.g. the canvas pan
                // manipulator on UIViewport that handles Alt+Left).
                m_PointerCaptured = true;
                m_DragInProgress = false;
                m_DragPointerId = pointerDownEvent.pointerId;
                m_DragStartCanvasPosition = m_PanelElement.ConvertPosition(pointerDownEvent);
                m_DragStartPanelPosition = pointerDownEvent.position;

                if (pointerDownEvent.clickCount >= 2)
                {
                    using var _ = ListPool<VisualElement>.Get(out var picked);
                    PickSelectableElements(m_DragStartCanvasPosition, picked);
                    if (picked.Count > 0)
                    {
                        var element = picked[0];
                        if (m_UICanvasInPlaceEditor.TryOpenEditorAt(element, m_PanelElement.ChangeCoordinatesTo(element, m_DragStartCanvasPosition)))
                            m_PointerCaptured = false; // editor consumed the gesture; skip selection on pointer-up
                    }
                }

                break;
            case PointerUpEvent pointerUpEvent when EventMode == CanvasEventMode.Pick:
                if (pointerUpEvent.button != 0 || m_PanelElement == null)
                    break;
                // If we're not tracking this gesture (PointerDown was gated, or it was already
                // aborted via ESC / capture loss), don't run any selection logic — otherwise an
                // ESC-cancelled drag would still trigger PerformSelection on release and wipe the
                // selection that Cancel just restored.
                if (!m_PointerCaptured)
                    break;
                if (this.HasPointerCapture(pointerUpEvent.pointerId))
                    this.ReleasePointer(pointerUpEvent.pointerId);
                if (m_DragInProgress)
                {
                    HideRectangleSelection();
                    PerformRectangleSelection(
                        CanvasRectFromDrag(m_PanelElement.ConvertPosition(pointerUpEvent)),
                        pointerUpEvent.modifiers);
                    ClearPreviewSelection();
                    m_SelectionSnapshot = null;
                }
                else
                {
                    PerformSelection(
                        m_PanelElement.ConvertPosition(pointerUpEvent),
                        pointerUpEvent.modifiers,
                        pointerUpEvent.position,
                        pointerUpEvent.timestamp);
                }
                m_PointerCaptured = false;
                m_DragInProgress = false;
                break;
            case KeyDownEvent keyDownEvent
                when m_DragInProgress && keyDownEvent.keyCode == KeyCode.Escape:
                CancelRectangleSelection();
                keyDownEvent.StopPropagation();
                break;
            case PointerCaptureOutEvent pointerCaptureOutEvent:
                if (pointerCaptureOutEvent.pointerId != m_DragPointerId)
                    break;
                if (m_DragInProgress)
                {
                    HideRectangleSelection();
                    ClearPreviewSelection();
                    if (m_SelectionSnapshot != null)
                    {
                        Selection.entityIds = m_SelectionSnapshot;
                        m_SelectionSnapshot = null;
                    }
                }
                m_PointerCaptured = false;
                m_DragInProgress = false;
                break;
            case PointerEnterEvent pointerEnterEvent when EventMode == CanvasEventMode.Pick:
            {
                m_IsPointerOver = true;
                if (m_PanelElement != null)
                {
                    m_LastPointerPickPosition = m_PanelElement.ConvertPosition(pointerEnterEvent);
                    UpdateHoveredElement();
                }
                break;
            }
            case PointerLeaveEvent when EventMode == CanvasEventMode.Pick:
            {
                m_IsPointerOver = false;
                HoveredElement = null;
                break;
            }
            case PointerMoveEvent pointerMoveEvent when EventMode == CanvasEventMode.Pick:
                if (m_PanelElement != null)
                {
                    var canvasPosition = (Vector2)m_PanelElement.ConvertPosition(pointerMoveEvent);
                    m_LastPointerPickPosition = canvasPosition;
                    UpdateHoveredElement();

                    if (m_PointerCaptured)
                    {
                        if (!m_DragInProgress &&
                            Vector2.Distance(canvasPosition, m_DragStartCanvasPosition) >= k_RectangleSelectionDragThreshold)
                        {
                            m_DragInProgress = true;
                            this.CapturePointer(m_DragPointerId);
                            // Take focus so a subsequent ESC key event routes through our
                            // HandleEventBubbleUp and reaches the cancel branch below.
                            Focus();
                            ShowRectangleSelection();
                            BeginRectangleSelection(pointerMoveEvent.modifiers);
                        }
                        if (m_DragInProgress)
                        {
                            UpdateRectangleSelection(pointerMoveEvent.position);
                            UpdatePreviewSelection(CanvasRectFromDrag(canvasPosition), pointerMoveEvent.modifiers);
                        }
                    }
                }
                break;
            default:
                if (EventMode == CanvasEventMode.Forward)
                    PanelElement?.ForwardEventBubbleUp(evt);
                break;
        }

        base.HandleEventBubbleUp(evt);
    }

    public void OnCanvasChanged()
    {
        m_UICanvasInPlaceEditor.UpdateEditor();
        MarkDirtyRepaint();
    }
    
    void OnSelectionChanged()
    {
        ClearSelection();

        var selectedIds = Selection.entityIds;
        foreach (var selectedId in selectedIds)
        {
            if (EditorUtility.EntityIdToObject(selectedId) is VisualElementSelection selection
                && ResolveCanvasTarget(selection) != null)
                AddToSelection(selection);
        }

        // Transform overlay is not supported for multi-selection.
        if (m_ElementSelections.Count == 1)
            m_ManipulatorOverlayManager.AcquireOverlay(m_ElementSelections[0]);
    }

    /// <summary>
    /// The element of this canvas's panel that <paramref name="element"/> stands for, or <see langword="null"/>
    /// when it stands for none of them.
    /// </summary>
    /// <remarks>
    /// Bounds only mean anything in the panel they come from, so everything drawn over the canvas — a handle, a
    /// manipulator, a highlight — is placed against this and not against the element handed in. Outside the UI
    /// Stage the selection holds the live elements of a scene panel while the canvas shows a clone of the same
    /// document, and the in-memory id path is what picks the right clone when there is more than one.
    /// </remarks>
    internal VisualElement ResolveCanvasElement(VisualElement element)
    {
        var subPanel = m_PanelElement?.SubPanel;
        if (element?.panel == null || subPanel == null)
            return null;

        // Already ours: the UI Stage selects into the very panel it renders, and so does a preview that has no
        // live tree standing behind it.
        if (ReferenceEquals(element.panel, subPanel))
            return element;

        // Only the live tree this canvas previews. Two GameObjects can render one document, and the id path is
        // built from asset ids alone — their elements correspond to the same clone — so the panel is the only
        // thing that says whether this element is the one we stand in for.
        var authoritativeRoot = AuthoritativeRootProvider?.Invoke();
        if (authoritativeRoot == null || !ReferenceEquals(element.panel, authoritativeRoot.panel))
            return null;

        // The live document root stands for the clone's own root. Matched by identity because a root carries no
        // VisualElementAsset, so correspondence has nothing to match it on.
        if (ReferenceEquals(element, authoritativeRoot))
            return m_PanelElement.subRootVisualElement;

        return m_PanelElement.subRootVisualElement?.FindCorrespondingElement(element);
    }

    /// <summary>
    /// <see cref="ResolveCanvasElement"/> for a selection, memoised.
    /// </summary>
    /// <remarks>
    /// The answer is always an element of this canvas, so it only stops being valid when the preview is
    /// re-cloned — which detaches it and is what the panel check below notices. Worth caching because the
    /// geometry paths ask on every viewport resize and zoom, once per selected element, and outside the UI
    /// Stage each miss walks the live tree looking for the clone it already found last frame.
    /// </remarks>
    VisualElement ResolveCanvasTarget(VisualElementSelection selection)
    {
        if (selection == null)
            return null;

        if (m_CanvasTargets.TryGetValue(selection, out var cached) && cached?.panel != null)
            return cached;

        var target = ResolveCanvasElement(selection.Element);
        m_CanvasTargets[selection] = target;
        return target;
    }

    /// <summary>
    /// What a click on <paramref name="canvasElement"/> should write to the editor selection.
    /// </summary>
    /// <remarks>
    /// The live element the preview stands in for wins, so the viewport, the Hierarchy and the inspector all
    /// name one element rather than a clone each. The clone is the fallback: a document can be shown with no
    /// live tree behind it, and an element may correspond to nothing in the one there is. Asked of the registry
    /// rather than read off the element, because the live tree only carries selection objects once it has been
    /// walked — reading the property would quietly fall back to the clone until then.
    /// </remarks>
    EntityId ResolveSelectionId(VisualElement canvasElement)
    {
        if (canvasElement == null)
            return EntityId.None;

        var authoritative = ResolveAuthoritativeElement(canvasElement);
        if (authoritative != null)
        {
            var authoritativeId = SelectionIdOf(authoritative);
            if (authoritativeId != EntityId.None)
                return authoritativeId;
        }

        return SelectionIdOf(canvasElement);
    }

    // The element's own object first: that is the one everything else already names, and asking the registry for
    // an element it has not filed yet mints a second one. The registry is only the fallback for a live tree that
    // has not been walked, whose elements carry nothing to read.
    static EntityId SelectionIdOf(VisualElement element)
    {
        var selectionObject = element.GetSelectionObject();
        if (selectionObject)
            return selectionObject.GetEntityId();

        return VisualElementSelectionRegistry.Instance?.GetOrCreateEntityId(element) ?? EntityId.None;
    }

    UISelectionObject ResolveSelectionObject(VisualElement canvasElement)
    {
        var id = ResolveSelectionId(canvasElement);
        return id == EntityId.None ? null : EditorUtility.EntityIdToObject(id) as UISelectionObject;
    }

    /// <summary>
    /// The live element <paramref name="canvasElement"/> stands in for, or <see langword="null"/> when there is
    /// no live tree behind this canvas or nothing in it corresponds.
    /// </summary>
    /// <remarks>
    /// The inverse of <see cref="ResolveCanvasElement"/>, for the callers that have to name an element to
    /// something outside this canvas: which instance of a repeated document an edit landed in, for one. It
    /// answers <see langword="null"/> rather than handing back the clone, because a caller that reports the
    /// clone outside the canvas names an element the rest of the editor cannot place — a Hierarchy selection
    /// request scoped to it resolves to no instance in particular, which is the opposite of scoping.
    /// </remarks>
    internal VisualElement ResolveAuthoritativeElement(VisualElement canvasElement)
    {
        if (canvasElement == null)
            return null;

        var authoritativeRoot = AuthoritativeRootProvider?.Invoke();
        if (authoritativeRoot == null)
            return null;

        // Mirror of the root case in ResolveCanvasElement: the clone's root stands for the live document root.
        if (ReferenceEquals(canvasElement, m_PanelElement?.subRootVisualElement))
            return authoritativeRoot;

        return authoritativeRoot.FindCorrespondingElement(canvasElement);
    }

    // Nothing carries a selection object while the panel is untracked, so there is nothing here to select —
    // least of all the empty selection a click would otherwise write over the user's.
    bool CanSelectContent => VisualElementSelectionRegistry.Instance?.IsTracked(m_PanelElement?.SubPanel) ?? false;

    void AddToSelection(VisualElementSelection selection)
    {
        if (m_ElementSelections.Contains(selection))
            return;
        m_ElementSelections.Add(selection);
        m_HandleManager.AcquireSelectionHandle(selection);
    }

    void RemoveFromSelection(int index)
    {
        var selection = m_ElementSelections[index];
        m_ElementSelections.RemoveAt(index);
        m_CanvasTargets.Remove(selection);
        m_HandleManager.ReleaseSelectionHandle(selection);
        m_ManipulatorOverlayManager.ReleaseOverlay(selection);
    }

    void ClearSelection()
    {
        for(var i = m_ElementSelections.Count - 1; i >= 0; --i)
            RemoveFromSelection(i);
    }

    void Release(PanelElement panelElement)
    {
        if (panelElement == null)
            return;

        ClearSelection();
        panelElement.SubPanel?.UnregisterChangeProcessor(this);
    }

    void Acquire(PanelElement panelElement)
    {
        if (panelElement == null)
            return;

        panelElement.SubPanel?.RegisterChangeProcessor(this);
        // Rebuild the handles from the current selection when the panel is (re)acquired, e.g. re-entering the stage.
        OnSelectionChanged();
        m_HandleManager.UpdateAllHandles();
        m_ManipulatorOverlayManager.UpdateAllOverlays();
    }

    void IVisualElementChangeProcessor.BeginProcessing(BaseVisualElementPanel panelElementPanel)
    {
        m_HandleManager.UpdateAllHandles();
        m_ManipulatorOverlayManager.UpdateAllOverlays();
    }

    void IVisualElementChangeProcessor.ProcessChanges(BaseVisualElementPanel panelElementPanel, AuthoringChanges changes)
    {
        using var _ = HashSetPool<VisualElementSelection>.Get(out var updateSet);
        if (panelElementPanel is PanelElement.RuntimePanel runtimePanel &&
            (changes.styleChanged.Contains(runtimePanel.Root) || changes.layoutChanged.Contains(runtimePanel.Root)))
        {
            foreach (var selection in m_ElementSelections)
                updateSet.Add(selection);
        }
        else
        {
            PopulateUpdateSet(m_ElementSelections, changes.styleChanged, updateSet);
            PopulateUpdateSet(m_ElementSelections, changes.layoutChanged, updateSet);
        }

        var firstSelection = true;

        foreach (var selection in updateSet)
        {
            m_HandleManager.UpdateSelectionHandle(selection);

            // Only support single selection for now
            if (firstSelection)
            {
                m_ManipulatorOverlayManager.UpdateOverlay(selection);
                m_ManipulatorOverlayManager.OnProcessChangeOnTarget(selection);
            }
            firstSelection  = false;
        }

        if (m_IsPointerOver && EventMode == CanvasEventMode.Pick)
            UpdateHoveredElement();
    }

    void IVisualElementChangeProcessor.EndProcessing(BaseVisualElementPanel panelElementPanel)
    {
    }

    void UpdateHoveredElement()
    {
        if (PanelElement?.SubPanel == null)
            return;
        using (ListPool<VisualElement>.Get(out var pickedElements))
        {
            m_PanelElement.SubPanel.PickAll(m_LastPointerPickPosition, pickedElements);
            HoveredElement = pickedElements.Count > 0 ? pickedElements[0] : null;
        }
    }

    // Matched on the canvas side: the changes reported name this panel's elements, while a selection outside the
    // UI Stage names the live ones it stands in for.
    void PopulateUpdateSet(List<VisualElementSelection> selectedList, HashSet<VisualElement> set, HashSet<VisualElementSelection> updateSet)
    {
        foreach (var selected in selectedList)
        {
            var target = ResolveCanvasTarget(selected);
            if (target != null && set.Contains(target))
                updateSet.Add(selected);
        }
    }

    internal void PerformSelection(Vector2 canvasPosition, EventModifiers modifiers, Vector2 menuPanelPosition, long timestampMs)
    {
        if (m_PanelElement == null || !CanSelectContent)
            return;

        var isShift = (modifiers & EventModifiers.Shift) != 0;
        var isAction = (modifiers & (EventModifiers.Control | EventModifiers.Command)) != 0;

        using var _ = ListPool<VisualElement>.Get(out var picked);
        PickSelectableElements(canvasPosition, picked);

        // Shift OR Ctrl/Cmd toggle the topmost picked element in/out of selection..
        if (isShift || isAction)
        {
            ResetCycleState();
            if (picked.Count > 0)
                ToggleSingleSelection(picked[0]);
            return;
        }

        if (picked.Count == 0)
        {
            ResetCycleState();
            Selection.entityIds = Array.Empty<EntityId>();
            return;
        }

        VisualElement target;
        if (IsCycleClick(canvasPosition, timestampMs, picked))
        {
            m_CycleIndex = (m_CycleIndex + 1) % picked.Count;
            target = picked[m_CycleIndex];
        }
        else
        {
            m_CycleIndex = 0;
            target = picked[0];
        }

        m_LastClickCanvasPosition = canvasPosition;
        m_LastClickTimestampMs = timestampMs;
        m_LastCycleList.Clear();
        m_LastCycleList.AddRange(picked);

        SetSingleSelection(target);
    }

    // Right-click + Ctrl/Cmd entry point: shows the picker popup for every selectable element
    // under the pointer. Mirrors SceneView's SceneViewPiercingMenu (Mouse1 + ShortcutModifiers.Action).
    internal void PerformPickerPopup(Vector2 canvasPosition, Vector2 menuPanelPosition)
    {
        if (m_PanelElement == null || !CanSelectContent)
            return;
        using var _ = ListPool<VisualElement>.Get(out var picked);
        PickSelectableElements(canvasPosition, picked);
        ResetCycleState();
        ShowPickerPopup(picked, menuPanelPosition);
    }

    void PickSelectableElements(Vector2 canvasPosition, List<VisualElement> result)
    {
        using var _ = ListPool<VisualElement>.Get(out var rawPicks);
        m_PanelElement.SubPanel.PickAll(canvasPosition, rawPicks);

        using var __ = HashSetPool<VisualElement>.Get(out var seen);
        foreach (var candidate in rawPicks)
        {
            var element = candidate.visualElementAsset != null
                ? candidate
                : candidate.GetFirstAncestorWhere(e => e.visualElementAsset != null);
            if (element == null)
                continue;
            if (!element.GetSelectionObject())
                continue;
            if (seen.Add(element))
                result.Add(element);
        }
    }

    bool IsCycleClick(Vector2 canvasPosition, long timestampMs, List<VisualElement> picked)
    {
        if (m_CycleIndex < 0 || m_LastCycleList.Count == 0 || picked.Count <= 1)
            return false;

        var elapsed = timestampMs - m_LastClickTimestampMs;
        if (elapsed < k_CycleClickMinDelayMs || elapsed > k_CycleClickMaxDelayMs)
            return false;

        if (Vector2.Distance(canvasPosition, m_LastClickCanvasPosition) > k_CycleClickMaxDistance)
            return false;

        if (m_LastCycleList.Count != picked.Count)
            return false;
        for (var i = 0; i < picked.Count; i++)
        {
            if (m_LastCycleList[i] != picked[i])
                return false;
        }
        return true;
    }

    void ResetCycleState()
    {
        m_LastCycleList.Clear();
        m_CycleIndex = -1;
        m_LastClickTimestampMs = 0;
    }

    void SetSingleSelection(VisualElement element)
    {
        var id = ResolveSelectionId(element);
        if (id == EntityId.None)
        {
            Selection.entityIds = Array.Empty<EntityId>();
            return;
        }
        Selection.entityIds = new[] { id };
    }

    void ToggleSingleSelection(VisualElement element)
    {
        var id = ResolveSelectionId(element);
        if (id == EntityId.None)
            return;

        var currentIds = Selection.entityIds;
        if (Array.IndexOf(currentIds, id) < 0)
            Selection.Add(id);
        else
            Selection.Remove(id);
    }

    void ShowPickerPopup(List<VisualElement> picked, Vector2 menuPanelPosition)
    {
        if (picked.Count == 0)
            return;

        if (picked.Count == 1)
        {
            SetSingleSelection(picked[0]);
            return;
        }

        var menu = new GenericDropdownMenu();
        AddPickerItems(menu, picked);
        SetProperty(PickerMenuPropertyKey, menu);
        menu.DropDown(new Rect(menuPanelPosition, Vector2.zero), this, DropdownMenuSizeMode.Content);
    }

    void AddPickerItems(GenericDropdownMenu menu, List<VisualElement> picked)
    {
        // GenericDropdownMenu silently drops items whose name collides with an earlier one, so
        // five overlapping unnamed Buttons would collapse to a single entry. Suffix duplicates
        // with a 1-based index in z-order so every picked element gets its own row.
        using var _ = DictionaryPool<string, int>.Get(out var totals);
        foreach (var element in picked)
        {
            var name = GetPickerItemDisplayName(element);
            totals[name] = totals.TryGetValue(name, out var count) ? count + 1 : 1;
        }

        using var __ = DictionaryPool<string, int>.Get(out var seen);
        foreach (var element in picked)
        {
            var captured = element;
            var name = GetPickerItemDisplayName(captured);
            string label;
            if (totals[name] > 1)
            {
                var index = seen.TryGetValue(name, out var c) ? c + 1 : 1;
                seen[name] = index;
                label = $"{name} [{index}]";
            }
            else
            {
                label = name;
            }
            menu.AddItem(label, false, () => SetSingleSelection(captured));
        }
    }

    static string GetPickerItemDisplayName(VisualElement element)
    {
        return string.IsNullOrEmpty(element.name)
            ? element.typeName
            : $"{element.name} ({element.typeName})";
    }

    // Called once when the drag crosses the threshold. Matches SceneView: a rectangle selection clears
    // the current selection at start so the inspector / live highlights reflect the in-progress
    // state. Shift/Ctrl/Cmd drags preserve the existing selection because their semantics depend
    // on it (union / subtract).
    internal void BeginRectangleSelection(EventModifiers modifiers)
    {
        if (!CanSelectContent)
            return;

        m_SelectionSnapshot = Selection.entityIds;

        var hasShift = (modifiers & EventModifiers.Shift) != 0;
        var hasAction = (modifiers & (EventModifiers.Control | EventModifiers.Command)) != 0;
        if (!hasShift && !hasAction)
            Selection.entityIds = Array.Empty<EntityId>();
    }

    internal void CancelRectangleSelection()
    {
        if (m_SelectionSnapshot == null)
            return;

        HideRectangleSelection();
        ClearPreviewSelection();
        Selection.entityIds = m_SelectionSnapshot;
        m_SelectionSnapshot = null;
        if (m_PointerCaptured && this.HasPointerCapture(m_DragPointerId))
            this.ReleasePointer(m_DragPointerId);
        m_PointerCaptured = false;
        m_DragInProgress = false;
        ResetCycleState();
    }

    internal void PerformRectangleSelection(Rect canvasRect, EventModifiers modifiers)
    {
        if (m_PanelElement == null || !CanSelectContent)
            return;

        using var _ = HashSetPool<VisualElementSelection>.Get(out var candidates);
        ComputeRectangleSelectionCandidates(canvasRect, modifiers, candidates);

        var next = new EntityId[candidates.Count];
        var i = 0;
        foreach (var sel in candidates)
            next[i++] = sel.GetEntityId();

        Selection.entityIds = next;
        ResetCycleState();
    }

    internal void ComputeRectangleSelectionCandidates(
        Rect canvasRect, EventModifiers modifiers, HashSet<VisualElementSelection> result)
    {
        if (m_PanelElement == null)
            return;

        // Adding to existing selection.
        var isShift = (modifiers & EventModifiers.Shift) != 0;
        // Removing from existing selection.
        var isAction = (modifiers & (EventModifiers.Control | EventModifiers.Command)) != 0;

        if (isShift || isAction)
        {
            foreach (var id in Selection.entityIds)
            {
                // Filtered like the handles are: a selection mapping onto nothing this canvas shows cannot be
                // drawn, kept or subtracted here.
                if (EditorUtility.EntityIdToObject(id) is VisualElementSelection sel && ResolveCanvasTarget(sel) != null)
                    result.Add(sel);
            }
        }

        using var _ = HashSetPool<VisualElementSelection>.Get(out var inRect);
        PickElementsInRect(canvasRect, inRect);

        if (isAction)
            result.ExceptWith(inRect);
        else
            result.UnionWith(inRect);
    }

    void PickElementsInRect(Rect canvasRect, HashSet<VisualElementSelection> result)
    {
        var mode = UIToolkitAuthoringSettings.RectangleSelectionMode;
        m_PanelElement.SubPanel.visualTree.Query<VisualElement>().ForEach(element =>
        {
            if (element.visualElementAsset == null)
                return;
            // Tested before the selection object is resolved: resolving walks the live tree looking for the
            // counterpart, and this runs over the whole document on every pointer move of the drag.
            if (!RectIncludesElement(canvasRect, element.worldBound, mode))
                return;
            if (ResolveSelectionObject(element) is not VisualElementSelection selectionObject)
                return;
            result.Add(selectionObject);
        });
    }

    // Matches the activator filter in UICanvasPanManipulator: Alt+Left on Win/Linux,
    // Cmd+Alt+Left on macOS. Centralised so the test fixture can exercise the same gating.
    internal static bool IsPanModifierHeld(EventModifiers modifiers)
    {
        if ((modifiers & EventModifiers.Alt) == 0)
            return false;
        return true;
    }

    Rect CanvasRectFromDrag(Vector2 currentCanvasPosition) => Rect.MinMaxRect(
        Mathf.Min(m_DragStartCanvasPosition.x, currentCanvasPosition.x),
        Mathf.Min(m_DragStartCanvasPosition.y, currentCanvasPosition.y),
        Mathf.Max(m_DragStartCanvasPosition.x, currentCanvasPosition.x),
        Mathf.Max(m_DragStartCanvasPosition.y, currentCanvasPosition.y));

    void EnsurePreviewHandles()
    {
        if (m_RectangleSelectionPreviewHandlesContainer != null)
            return;
        m_RectangleSelectionPreviewHandlesContainer = new VisualElement { pickingMode = PickingMode.Ignore };
        m_RectangleSelectionPreviewHandlesContainer.StretchToParentSize();
        hierarchy.Add(m_RectangleSelectionPreviewHandlesContainer);
        m_RectangleSelectionPreviewHandleManager =
            new SelectionHandleManager(m_RectangleSelectionPreviewHandlesContainer, ResolveCanvasTarget);
    }

    internal void UpdatePreviewSelection(Rect canvasRect, EventModifiers modifiers)
    {
        if (!CanSelectContent)
            return;

        EnsurePreviewHandles();

        // Hide the regular handles while the preview is on screen so the user sees only one set
        // — the one that would be the result of releasing the mouse now.
        m_HandlesContainer.style.display = DisplayStyle.None;

        using var _ = HashSetPool<VisualElementSelection>.Get(out var next);
        ComputeRectangleSelectionCandidates(canvasRect, modifiers, next);

        for (var i = m_PreviewedSelections.Count - 1; i >= 0; i--)
        {
            if (!next.Contains(m_PreviewedSelections[i]))
            {
                m_RectangleSelectionPreviewHandleManager.ReleaseSelectionHandle(m_PreviewedSelections[i]);
                m_PreviewedSelections.RemoveAt(i);
            }
        }
        foreach (var sel in next)
        {
            if (!m_PreviewedSelections.Contains(sel))
            {
                m_RectangleSelectionPreviewHandleManager.AcquireSelectionHandle(sel);
                m_PreviewedSelections.Add(sel);
            }
        }
    }

    internal void ClearPreviewSelection()
    {
        if (m_RectangleSelectionPreviewHandleManager != null)
        {
            for (var i = m_PreviewedSelections.Count - 1; i >= 0; i--)
                m_RectangleSelectionPreviewHandleManager.ReleaseSelectionHandle(m_PreviewedSelections[i]);
        }
        m_PreviewedSelections.Clear();
        m_HandlesContainer.style.display = StyleKeyword.Null;
    }

    internal IReadOnlyList<VisualElementSelection> PreviewedSelections => m_PreviewedSelections;

    static bool RectIncludesElement(Rect canvasRect, Rect elementBounds, RectangleSelectionMode mode)
    {
        return mode switch
        {
            RectangleSelectionMode.FullyContained =>
                elementBounds.xMin >= canvasRect.xMin &&
                elementBounds.yMin >= canvasRect.yMin &&
                elementBounds.xMax <= canvasRect.xMax &&
                elementBounds.yMax <= canvasRect.yMax,
            _ => elementBounds.Overlaps(canvasRect),
        };
    }

    void EnsureRectangleSelectionOverlay()
    {
        if (m_RectangleSelectionOverlay != null)
            return;
        m_RectangleSelectionOverlay = new VisualElement { pickingMode = PickingMode.Ignore };
        m_RectangleSelectionOverlay.AddToClassList(k_RectangleSelectionOverlayUssClass);
        m_RectangleSelectionOverlay.style.position = Position.Absolute;
        m_RectangleSelectionOverlay.style.display = DisplayStyle.None;
        hierarchy.Add(m_RectangleSelectionOverlay);
    }

    void ShowRectangleSelection()
    {
        EnsureRectangleSelectionOverlay();
        var color = ColorPreferences.SelectionOutline;
        m_RectangleSelectionOverlay.SetInlineBorderColor(color);
        m_RectangleSelectionOverlay.style.backgroundColor = new Color(color.r, color.g, color.b, color.a * 0.2f);
        m_RectangleSelectionOverlay.style.borderTopWidth = 1f;
        m_RectangleSelectionOverlay.style.borderRightWidth = 1f;
        m_RectangleSelectionOverlay.style.borderBottomWidth = 1f;
        m_RectangleSelectionOverlay.style.borderLeftWidth = 1f;
        m_RectangleSelectionOverlay.style.display = DisplayStyle.Flex;
        UpdateRectangleSelection(m_DragStartPanelPosition);
    }

    void UpdateRectangleSelection(Vector2 currentPanelPosition)
    {
        if (m_RectangleSelectionOverlay == null)
            return;
        var min = Vector2.Min(m_DragStartPanelPosition, currentPanelPosition);
        var max = Vector2.Max(m_DragStartPanelPosition, currentPanelPosition);
        var local = this.WorldToLocal(min);
        var size = max - min;
        m_RectangleSelectionOverlay.style.left = local.x;
        m_RectangleSelectionOverlay.style.top = local.y;
        m_RectangleSelectionOverlay.style.width = size.x;
        m_RectangleSelectionOverlay.style.height = size.y;
    }

    void HideRectangleSelection()
    {
        if (m_RectangleSelectionOverlay != null)
            m_RectangleSelectionOverlay.style.display = DisplayStyle.None;
    }
}
