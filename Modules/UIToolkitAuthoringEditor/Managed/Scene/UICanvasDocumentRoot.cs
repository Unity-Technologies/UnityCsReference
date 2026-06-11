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
    readonly VisualElement m_HandlesContainer;
    readonly SelectionHandleManager m_HandleManager;
    PanelElement m_PanelElement;

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
                    m_HandleManager.UpdateAllHandles();
                    break;
                case CanvasEventMode.Forward:
                    m_HandlesContainer.style.display = DisplayStyle.None;
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

    public override VisualElement contentContainer => null;

    public VisualElement OverlayLayer => m_HandlesContainer;

    public UICanvasDocumentRoot()
    {
        focusable = true;
        m_HandlesContainer = new VisualElement();
        hierarchy.Add(m_HandlesContainer);
        m_HandlesContainer.StretchToParentSize();
        m_HandleManager = new SelectionHandleManager(m_HandlesContainer);
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
                break;
            case GeometryChangedEvent:
                m_HandleManager.UpdateAllHandles();
                break;
            case PointerDownEvent pointerDownEvent when EventMode == CanvasEventMode.Pick:
                if (m_PanelElement == null)
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

    void OnSelectionChanged()
    {
        ClearSelection();

        var selectedIds = Selection.entityIds;
        foreach (var selectedId in selectedIds)
        {
            if (EditorUtility.EntityIdToObject(selectedId) is VisualElementSelection selection)
                AddToSelection(selection);
        }
    }

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
        m_HandleManager.ReleaseSelectionHandle(selection);
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

        panelElement.SubPanel?.UnregisterChangeProcessor(this);
    }

    void Acquire(PanelElement panelElement)
    {
        if (panelElement == null)
            return;

        panelElement.SubPanel?.RegisterChangeProcessor(this);
        m_HandleManager.UpdateAllHandles();
    }

    void IVisualElementChangeProcessor.BeginProcessing(BaseVisualElementPanel panelElementPanel)
    {
        m_HandleManager.UpdateAllHandles();
    }

    void IVisualElementChangeProcessor.ProcessChanges(BaseVisualElementPanel panelElementPanel, AuthoringChanges changes)
    {
        using var _ = HashSetPool<VisualElementSelection>.Get(out var updateSet);
        if (panelElementPanel is PanelElement.RuntimePanel runtimePanel && changes.styleChanged.Contains(runtimePanel.Root))
        {
            foreach (var selection in m_ElementSelections)
                updateSet.Add(selection);
        }
        else
        {
            PopulateUpdateSet(m_ElementSelections, changes.styleChanged, updateSet);

        }
        foreach (var selection in updateSet)
            m_HandleManager.UpdateSelectionHandle(selection);

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

    static void PopulateUpdateSet(List<VisualElementSelection> selectedList, HashSet<VisualElement> set, HashSet<VisualElementSelection> updateSet)
    {
        foreach (var selected in selectedList)
        {
            if (set.Contains(selected.Element))
                updateSet.Add(selected);
        }
    }

    internal void PerformSelection(Vector2 canvasPosition, EventModifiers modifiers, Vector2 menuPanelPosition, long timestampMs)
    {
        if (m_PanelElement == null)
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
        if (m_PanelElement == null)
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

    static void SetSingleSelection(VisualElement element)
    {
        var selectionObject = element?.GetSelectionObject();
        if (!selectionObject)
        {
            Selection.entityIds = Array.Empty<EntityId>();
            return;
        }
        Selection.entityIds = new[] { selectionObject.GetEntityId() };
    }

    static void ToggleSingleSelection(VisualElement element)
    {
        var selectionObject = element.GetSelectionObject();
        if (!selectionObject)
            return;
        var id = selectionObject.GetEntityId();

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
        if (m_PanelElement == null)
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
                if (EditorUtility.EntityIdToObject(id) is VisualElementSelection sel)
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
            var selectionObject = element.GetSelectionObject();
            if (!selectionObject)
                return;
            if (!RectIncludesElement(canvasRect, element.worldBound, mode))
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
        m_RectangleSelectionPreviewHandleManager = new SelectionHandleManager(m_RectangleSelectionPreviewHandlesContainer);
    }

    internal void UpdatePreviewSelection(Rect canvasRect, EventModifiers modifiers)
    {
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
