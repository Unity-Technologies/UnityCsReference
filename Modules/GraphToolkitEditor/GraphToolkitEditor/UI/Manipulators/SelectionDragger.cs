// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// #define DEBUG_SELECTION_DRAGGER // uncomment to show the panning borders on the graph

using System;
using System.Collections.Generic;
using Unity.GraphToolkit.InternalBridge;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Manipulator to move the selected elements by click and drag.
    /// </summary>
    [UnityRestricted]
    internal class SelectionDragger : MouseManipulator
    {
        ISelectionDraggerTarget m_CurrentSelectionDraggerTarget;
        bool m_Dragging;
        readonly Snapper m_Snapper = new Snapper();
        bool m_Active;
        HashSet<GraphElement> m_ElementsToMove = new HashSet<GraphElement>();
        GraphViewPanHelper m_PanHelper = new GraphViewPanHelper();

        // Internal for testing
        internal IReadOnlyCollection<GraphElement> ElementsToMove => m_ElementsToMove;

        // selectedElement is used to store a unique selection candidate for cases where user clicks on an item not to
        // drag it but just to reset the selection -- we only know this after the manipulation has ended
        GraphElement SelectedElement => SelectedMovingElement.Element;

        MovingElement SelectedMovingElement => m_MovingElements.Count > m_SelectedMovingElementIndex ? m_MovingElements[m_SelectedMovingElementIndex] : default;

        int m_SelectedMovingElementIndex;

        List<VisualElement> m_DropTargetPickList = new List<VisualElement>();

        GraphView m_GraphView;
        Vector2 m_MouseStartInGraph;
        Vector2 m_TotalMouseDelta;
        Vector2 m_LastMousePosition;
        bool m_MoveOnlyPlacemats;

        /// <summary>
        /// Elements to be dragged and their initial position
        /// </summary>
        List<MovingElement> m_MovingElements;

        struct MovingElement
        {
            public GraphElement Element;
            public Vector2 InitialPosition;
        }

        public bool IsActive => m_Active;

        ISelectionDraggerTarget GetTargetAt(Vector2 mousePosition, IReadOnlyList<ChildView> exclusionList)
        {
            Vector2 pickPoint = mousePosition;

            m_DropTargetPickList.Clear();
            target.panel.PickAll(pickPoint, m_DropTargetPickList);

            ISelectionDraggerTarget selectionDraggerTarget = null;

            for (int i = 0; i < m_DropTargetPickList.Count; i++)
            {
                if (m_DropTargetPickList[i] == target && target != m_GraphView)
                    continue;

                VisualElement picked = m_DropTargetPickList[i];

                selectionDraggerTarget = picked as ISelectionDraggerTarget;

                if (selectionDraggerTarget != null)
                {
                    foreach (var element in exclusionList)
                    {
                        if (element == picked || element.FindCommonAncestor(picked) == element)
                        {
                            selectionDraggerTarget = null;
                            break;
                        }
                    }

                    if (selectionDraggerTarget != null)
                        break;
                }
            }

            return selectionDraggerTarget;
        }

        public SelectionDragger(GraphView graphView)
        {
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse, modifiers = EventModifiers.Shift });
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse, modifiers = ClickSelector.PlatformMultiSelectModifier });

            m_GraphView = graphView;
            m_MovingElements = new List<MovingElement>();
            m_SelectedMovingElementIndex = 0;

        }

        /// <inheritdoc />
        protected override void RegisterCallbacksOnTarget()
        {
            if (!(target is IDragSource))
            {
                throw new InvalidOperationException("Manipulator can only be added to a control that supports selection");
            }

            target.RegisterCallback<MouseDownEvent>(OnMouseDown);
            target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
            target.RegisterCallback<MouseUpEvent>(OnMouseUp);

            target.RegisterCallback<KeyDownEvent>(OnKeyDown);
            target.RegisterCallback<MouseCaptureOutEvent>(OnMouseCaptureOutEvent);

            m_Dragging = false;
        }

        /// <inheritdoc />
        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp);

            target.UnregisterCallback<KeyDownEvent>(OnKeyDown);
            target.UnregisterCallback<MouseCaptureOutEvent>(OnMouseCaptureOutEvent);
        }

        /// <summary>
        /// Callback for the MouseCaptureOut event.
        /// </summary>
        /// <param name="e">The event.</param>
        protected void OnMouseCaptureOutEvent(MouseCaptureOutEvent e)
        {
            if (m_Active)
            {
                // Stop processing the event sequence if the target has lost focus, then.
                // Don't allow dropping if you just lost focus.
                ApplyDrag(false);
                StopManipulation();
            }
        }

        /// <summary>
        /// Adds a snap strategy to the selection dragger. This is in addition to the strategies enabled by the user in the preferences.
        /// </summary>
        /// <param name="strategy">The strategy to add.</param>
        public void AddSnapStrategy(SnapStrategy strategy)
        {
            m_Snapper.AddSnapStrategy(strategy);
        }

        /// <summary>
        /// Removes a snap strategy to the selection dragger.
        /// </summary>
        /// <param name="strategy">The strategy to remove.</param>
        public void RemoveSnapStrategy(SnapStrategy strategy)
        {
            m_Snapper.RemoveSnapStrategy(strategy);
        }

        Vector2 GetViewPositionInGraphSpace(Vector2 localPosition)
        {
            var gvPos = m_GraphView.Pan;
            var gvScale = m_GraphView.Zoom;
            return (localPosition - gvPos) / gvScale;
        }

        /// <summary>
        /// Callback for the MouseDown event.
        /// </summary>
        /// <param name="e">The event.</param>
        protected void OnMouseDown(MouseDownEvent e)
        {
            if (m_Active)
            {
                e.StopPropagation();
                return;
            }

            if (CanStartManipulation(e))
            {
                if (m_GraphView == null)
                    return;

                // avoid starting a manipulation on a non movable object
                var clickedElement = e.target as GraphElement;
                if (clickedElement == null)
                {
                    var ve = e.target as VisualElement;
                    clickedElement = ve?.GetFirstAncestorOfType<GraphElement>();
                    if (clickedElement == null)
                        return;
                }

                // Only start manipulating if the clicked element is movable.
                if (clickedElement is Marker marker)
                {
                    // If the clicked element is a marker, check if its parent model is movable.
                    if (!marker.ParentModel.IsMovable())
                        return;
                }
                else if (!clickedElement.IsMovable())
                    return;

                // Only start manipulating if the mouse is in its clickable region (it must be deselected otherwise).
                if (!clickedElement.ContainsPoint(clickedElement.WorldToLocal(e.mousePosition)))
                    return;
                var selection = m_GraphView.GetSelection();

                m_MoveOnlyPlacemats = e.shiftKey;
                RefreshElementsToMove(selection);

                if (m_ElementsToMove.Count == 0)
                    return;

                m_MovingElements.Clear();
                if (m_ElementsToMove.Count > m_MovingElements.Capacity)
                    m_MovingElements.Capacity = m_ElementsToMove.Count;

                m_TotalMouseDelta = Vector2.zero;
                m_SelectedMovingElementIndex = 0;
                m_TotalFreePanTravel = Vector2.zero;

                foreach (GraphElement ce in m_ElementsToMove)
                {
                    ce.PositionIsOverriddenByManipulator = true;

                    if (ce == clickedElement)
                        m_SelectedMovingElementIndex = m_MovingElements.Count;
                    m_MovingElements.Add(new MovingElement
                    {
                        Element = ce,
                        InitialPosition = ce.layout.position
                    });

                    if (ce is Placemat placemat)
                    {
                        m_LastConsideredPosition[placemat] = ce.layout.position;

                        if (m_MoveOnlyPlacemats)
                            placemat.MoveOnly = true;
                    }
                }

                m_MouseStartInGraph = GetViewPositionInGraphSpace(e.localMousePosition);
                m_TotalFreePanTravel = Vector2.zero;
                m_LastMousePosition = e.localMousePosition;

                m_PanHelper.OnMouseDown(e, m_GraphView, Pan);

                m_Snapper.BeginSnap(SelectedElement);

                m_Active = true;
                e.StopPropagation();
            }
        }

        void RefreshElementsToMove(IReadOnlyList<GraphElementModel> selection)
        {
            m_ElementsToMove.Clear();

            for (var i = 0; i < selection.Count; i++)
            {
                var view = selection[i].GetView(m_GraphView);
                if (view is GraphElement ge and not Wire && ge.IsMovable())
                    m_ElementsToMove.Add(ge);
            }


            if (m_ElementsToMove.Count == 0)
                return;

            var selectedPlacemats = new HashSet<Placemat>();
            foreach (var element in m_ElementsToMove)
            {
                if (element is Placemat placemat)
                    selectedPlacemats.Add(placemat);
            }

            foreach (var placemat in selectedPlacemats)
                placemat.GetElementsToMove(m_MoveOnlyPlacemats, m_ElementsToMove);
        }

        /// <summary>
        /// The offset by which the graphview has been panned during the move.
        /// <remarks>Used to figure out if we need to send a reframe command or not on escape.</remarks>
        /// </summary>
        Vector2 m_TotalFreePanTravel = Vector2.zero;

        /// <summary>
        /// Callback for the MouseMove event.
        /// </summary>
        /// <param name="e">The event.</param>
        protected void OnMouseMove(MouseMoveEvent e)
        {
            if (!m_Active)
                return;

            if (m_GraphView == null)
                return;

            if ((e.pressedButtons & (1 << (int)MouseButton.MiddleMouse)) != 0)
            {
                OnMouseUp(e);
                return;
            }

            // We want the manipulator target to receive events even when mouse is not over it.
            // We wait for the (first) mouse move to capture the mouse because this is here that the interaction really begins.
            // At the mouse down stage, it is still to early, since the interaction could simply be a click and then should
            // be fully handled by another manipulator/element.
            // If can happen (at least on mac) that a move event is sent even if the mouse was not moved.
            if (!target.HasMouseCapture() && e.localMousePosition != m_LastMousePosition)
            {
                target.CaptureMouse();
            }

            m_TotalFreePanTravel = Vector2.zero;

            m_PanHelper.OnMouseMove(e);

            if (SelectedElement.parent != null)
            {
                m_TotalMouseDelta = GetDragAndSnapOffset(GetViewPositionInGraphSpace(e.localMousePosition));

                HighlightNewElementsInPlacemats();

                MoveElements(m_TotalMouseDelta);
            }

            var selection = m_GraphView.GetSelection();
            var selectedUI = new List<ChildView>();
            for (var i = 0; i < selection.Count; i++)
            {
                var view = selection[i].GetView(m_GraphView);
                selectedUI.Add(view);
            }

            var previousTarget = m_CurrentSelectionDraggerTarget;
            m_CurrentSelectionDraggerTarget = GetTargetAt(e.mousePosition, selectedUI);

            if (m_CurrentSelectionDraggerTarget != previousTarget)
            {
                previousTarget?.ClearDropHighlightStatus();
                m_CurrentSelectionDraggerTarget?.SetDropHighlightStatus(selection);
            }

            m_LastMousePosition = e.localMousePosition;
            m_Dragging = true;
            e.StopPropagation();
        }

        Vector2 GetDragAndSnapOffset(Vector2 mouseGraphPosition)
        {
            var dragDelta = mouseGraphPosition - m_MouseStartInGraph;

            if (m_Snapper.IsActive)
            {
                dragDelta = GetSnapCorrectedDelta(SelectedMovingElement, dragDelta);
            }

            return dragDelta;
        }

        HashSet<GraphElement> m_ElementsAdded = new HashSet<GraphElement>();
        Dictionary<Placemat, Vector2> m_LastConsideredPosition = new Dictionary<Placemat, Vector2>();
        void HighlightNewElementsInPlacemats()
        {
            if (m_TotalMouseDelta.sqrMagnitude == 0)
                return;

            HashSet<GraphElement> elementsAdded = new HashSet<GraphElement>();
            HashSet<GraphElement> elementsRemoved = new HashSet<GraphElement>();
            List<GraphElement> elementOverlapping = new List<GraphElement>();
            foreach (var element in m_MovingElements)
            {
                if (element.Element is not Placemat placemat)
                    continue;

                var oldRect = placemat.layout;
                oldRect.position = m_LastConsideredPosition[placemat];
                var newRect = oldRect;
                newRect.position = element.InitialPosition + m_TotalMouseDelta;

                var delta = newRect.position - oldRect.position;
                if (delta.sqrMagnitude < 1)
                    continue;

                m_LastConsideredPosition[placemat] = newRect.position;

                if (newRect.Overlaps(oldRect))
                {
                    // If the rect overlaps, consider the rects that are added and removed by the drag.
                    //This is the most common case as we don't usually drag the mouse by more than the placemat's size.

                    Rect[] addedRects = new Rect[2];
                    if (delta.x > 0)
                    {
                        addedRects[0].x = oldRect.xMax;
                        addedRects[1].x = newRect.xMin;
                    }
                    else
                    {
                        addedRects[0].x = newRect.xMin;
                        addedRects[1].x = oldRect.xMin;
                    }

                    addedRects[0].y = newRect.yMin;
                    addedRects[0].height = newRect.height;
                    addedRects[0].width = Mathf.Abs(delta.x);

                    addedRects[1].width = newRect.width - Mathf.Abs(delta.x);
                    if (delta.y > 0)
                        addedRects[1].y = oldRect.yMax;
                    else
                        addedRects[1].y = newRect.yMin;
                    addedRects[1].height = Mathf.Abs(delta.y);

                    elementOverlapping.Clear();
                    for (int i = 0; i < addedRects.Length; ++i)
                    {
                        if (addedRects[i].width * addedRects[i].height != 0)
                        {
                            m_GraphView.GetGraphElementsInRegion(addedRects[i], elementOverlapping, GraphView.PartitioningMode.PlacematBody, true);
                        }
                    }

                    foreach (var overlapping in elementOverlapping)
                    {
                        var layout = overlapping.layout;
                        if (overlapping.IsMovable() && newRect.x < layout.x && newRect.xMax > layout.xMax && newRect.yMin < layout.yMin && newRect.yMax > layout.yMax)
                            elementsAdded.Add(overlapping);
                    }

                    Rect[] removedRects = new Rect[2];
                    if (delta.x > 0)
                    {
                        removedRects[0].x = oldRect.xMin;
                        removedRects[1].x = newRect.xMin;
                    }
                    else
                    {
                        removedRects[0].x = newRect.xMax;
                        removedRects[1].x = oldRect.xMin;
                    }

                    removedRects[0].width = Mathf.Abs(delta.x);
                    removedRects[0].y = oldRect.yMin;
                    removedRects[0].height = newRect.height;

                    removedRects[1].width = newRect.width - Mathf.Abs(delta.x);

                    if (delta.y > 0)
                    {
                        removedRects[1].y = oldRect.yMin;
                    }
                    else
                    {
                        removedRects[1].y = newRect.yMax;
                    }
                    removedRects[1].height = Mathf.Abs(delta.y);

                    elementOverlapping.Clear();
                    for (int i = 0; i < removedRects.Length; ++i)
                    {
                        if (removedRects[i].width * removedRects[i].height != 0)
                        {
                            m_GraphView.GetGraphElementsInRegion(removedRects[i], elementOverlapping, GraphView.PartitioningMode.PlacematBody, true);
                        }
                    }

                    foreach (var overlapping in elementOverlapping)
                    {
                        var layout = overlapping.layout;
                        if (newRect.x >= layout.x || newRect.xMax <= layout.xMax || newRect.yMin >= layout.yMin || newRect.yMax <= layout.yMax)
                            elementsRemoved.Add(overlapping);
                    }
                }
                else
                {
                    // If the rect do not overlap, simply remove those in the old position and add those in the new position but not dragged.
                    elementOverlapping.Clear();
                    m_GraphView.GetGraphElementsInRegion(newRect, elementOverlapping, GraphView.PartitioningMode.PlacematBody, false);
                    foreach (var overlapped in elementOverlapping)
                    {
                        if (overlapped.GraphElementModel.IsMovable())
                            m_ElementsAdded.Add(overlapped);
                    }

                    elementOverlapping.Clear();
                    m_GraphView.GetGraphElementsInRegion(oldRect, elementOverlapping, GraphView.PartitioningMode.PlacematBody, false);
                    elementsRemoved.UnionWith(elementOverlapping);
                }
            }

            foreach (var element in m_MovingElements)
            {
                elementsAdded.Remove(element.Element);
            }

            foreach (var element in elementsAdded)
            {
                element.OverrideHighlighted = true;
            }
            m_ElementsAdded.UnionWith(elementsAdded);

            foreach (var element in elementsRemoved)
            {
                element.OverrideHighlighted = false;
            }
            m_ElementsAdded.ExceptWith(elementsRemoved);
        }

        void MoveElements(Vector2 delta)
        {
            foreach (var movingElement in m_MovingElements)
            {
                // Protect against stale visual elements that have been deparented since the start of the manipulation
                if (movingElement.Element.hierarchy.parent == null)
                    continue;

                movingElement.Element.SetPositionOverride(movingElement.InitialPosition + delta);
            }
            using (var updater = m_GraphView.GraphViewModel.GraphViewState.UpdateScope)
            {
                updater.MarkContentUpdated();
            }
        }

        Vector2 GetSnapCorrectedDelta(MovingElement movingElement, Vector2 delta)
        {
            // Check if snapping is paused first: if yes, the snapper will return the original dragging position
            if (Event.current != null)
            {
                m_Snapper.PauseSnap(Event.current.shift);
            }

            Rect initialRect = movingElement.Element.layout;
            initialRect.position = movingElement.InitialPosition + delta;
            var snappedPosition = m_Snapper.GetSnappedPosition(initialRect, movingElement.Element);
            return snappedPosition - movingElement.InitialPosition;
        }

        void Pan(TimerState ts)
        {
            m_TotalFreePanTravel += m_PanHelper.TraveledThisFrame / m_PanHelper.Scale;
            MoveElements(m_TotalMouseDelta + m_TotalFreePanTravel);
        }

        /// <summary>
        /// Callback for the MouseUp event.
        /// </summary>
        /// <param name="evt">The event.</param>
        protected void OnMouseUp(IMouseEvent evt)
        {
            if (m_GraphView == null)
            {
                if (m_Active)
                    StopManipulation();

                return;
            }

            if (CanStopManipulation(evt))
            {
                if (m_Active)
                    ((EventBase)evt).StopPropagation();
                m_LastMousePosition = evt.localMousePosition;
                ApplyDrag(true);
                m_PanHelper.OnMouseUp(evt);
                StopManipulation();
            }
        }

        void ApplyDrag(bool canDrop)
        {
            if (m_Active)
            {
                var selectedModels = m_GraphView.GetSelection();
                if (m_Dragging || SelectedElement == null)
                {
                    if (target is GraphView graphView)
                    {
                        var changedElements = graphView.PositionDependenciesManager.StopNotifyMove();
                        using (var graphUpdater = m_GraphView.GraphViewModel.GraphModelState.UpdateScope)
                        {
                            graphUpdater.MarkChanged(changedElements, ChangeHint.Layout);
                        }
                    }

                    // if we stop dragging on something else than a DropTarget, just move elements
                    if (!canDrop || m_CurrentSelectionDraggerTarget == null || !m_CurrentSelectionDraggerTarget.CanAcceptDrop(selectedModels))
                    {
                        var models = new List<IMovable>();
                        for (var i = 0; i < m_MovingElements.Count; i++)
                        {
                            var ge = m_MovingElements[i].Element;
                            if (ge.Model is not AbstractNodeModel || ge.IsMovable())
                            {
                                if (ge.Model is IMovable movableModel)
                                {
                                    models.Add(movableModel);
                                }
                            }
                        }
                        var dragDelta = GetDragAndSnapOffset(GetViewPositionInGraphSpace(m_LastMousePosition));

                        List<(PlacematModel, PlacematModel)> placematToBringInFrontOf = null;

                        for (var i = 0; i < models.Count; ++i)
                        {
                            if (models[i] is PlacematModel placematModel)
                            {
                                var index = m_GraphView.GraphModel.PlacematModels.IndexOf(models[i]);

                                for (var j = 0; j < index; ++j)
                                {
                                    var subPlacemat = m_GraphView.GraphModel.PlacematModels[j];

                                    if (models.Contains(subPlacemat))
                                        continue;

                                    var newPlacematPosition = placematModel.PositionAndSize;
                                    newPlacematPosition.position += dragDelta;

                                    if (newPlacematPosition.Contains(subPlacemat.PositionAndSize.position) && newPlacematPosition.Contains(subPlacemat.Position + subPlacemat.PositionAndSize.size))
                                    {
                                        placematToBringInFrontOf ??= new List<(PlacematModel, PlacematModel)>();

                                        placematToBringInFrontOf.Add((subPlacemat, placematModel));
                                    }
                                }
                            }
                        }

                        m_GraphView.Dispatch(new MoveElementsAndBringPlacematToFrontCommand(dragDelta, models, placematToBringInFrontOf));
                    }
                }

                if (canDrop && (m_CurrentSelectionDraggerTarget?.CanAcceptDrop(selectedModels) ?? false))
                {
                    m_CurrentSelectionDraggerTarget?.PerformDrop(selectedModels);
                }
            }
        }

        void StopManipulation()
        {
            if (m_Active)
            {

                if (target is GraphView graphView)
                    graphView.StopSelectionDragger();
                m_PanHelper.Stop();
                m_CurrentSelectionDraggerTarget?.ClearDropHighlightStatus();
                if (m_Snapper.IsActive)
                    m_Snapper.EndSnap();

                target.ReleaseMouse();

                foreach (var element in m_MovingElements)
                {
                    element.Element.PositionIsOverriddenByManipulator = false;
                    if (element.Element is Placemat placemat)
                        placemat.MoveOnly = false;
                }

                foreach (var element in m_ElementsAdded)
                {
                    element.OverrideHighlighted = false;
                }
                m_LastConsideredPosition.Clear();
                m_ElementsAdded.Clear();
            }
            m_SelectedMovingElementIndex = 0;
            m_Active = false;
            m_CurrentSelectionDraggerTarget = null;
            m_Dragging = false;
        }

        /// <summary>
        /// Callback for the KeyDown event.
        /// </summary>
        /// <param name="e">The event.</param>
        protected void OnKeyDown(KeyDownEvent e)
        {
            if (e.keyCode != KeyCode.Escape || m_GraphView == null || !m_Active)
                return;

            // Reset the items to their original pos.
            MoveElements(Vector2.zero);

            if (m_TotalFreePanTravel != Vector2.zero)
            {
                var position = m_GraphView.ContentViewContainer.resolvedStyle.translate;
                var scale = m_GraphView.ContentViewContainer.resolvedStyle.scale.value;
                m_GraphView.Dispatch(new ReframeGraphViewCommand(position, scale));
            }

            StopManipulation();
            e.StopPropagation();
        }

    }
}
