// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// #define DEBUG_SELECTION_DRAGGER // uncomment to show the panning borders on the graph

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.CommandStateObserver;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Manipulator to move the selected elements by click and drag.
    /// </summary>
    class SelectionDragger : MouseManipulator
    {
        ISelectionDraggerTarget m_CurrentSelectionDraggerTarget;
        bool m_Dragging;
        readonly Snapper_Internal m_Snapper = new Snapper_Internal();
        bool m_Active;
        bool m_ElementsToMoveDirty;
        HashSet<GraphElement> m_ElementsToMove = new HashSet<GraphElement>();
        GraphViewPanHelper_Internal m_PanHelper = new GraphViewPanHelper_Internal();

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

        List<GraphElementModel> m_PreviousSelection = new List<GraphElementModel>();

        /// <summary>
        /// Elements to be dragged and their initial position
        /// </summary>
        List<MovingElement> m_MovingElements;

        struct MovingElement
        {
            public GraphElement Element;
            public Vector2 InitialPosition;
        }

        SelectionObserver m_SelectionObserver;

        public bool IsActive => m_Active;

        ISelectionDraggerTarget GetTargetAt(Vector2 mousePosition, IReadOnlyList<ModelView> exclusionList)
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


        class SelectionObserver : StateObserver
        {
            SelectionDragger m_SelectionDragger;
            SelectionStateComponent m_SelectionState;
            public SelectionObserver(GraphView gv,SelectionDragger sd):
                base(gv?.GraphViewModel?.SelectionState)
            {
                m_SelectionDragger = sd;
                m_SelectionState = gv?.GraphViewModel?.SelectionState;
            }

            public override void Observe()
            {
                using (var selectionObservation = this.ObserveState(m_SelectionState))
                {
                    if( selectionObservation.UpdateType != UpdateType.None)
                        m_SelectionDragger.m_ElementsToMoveDirty = true;
                }
            }
        }
        public SelectionDragger(GraphView graphView)
        {
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse, modifiers = EventModifiers.Shift });
            if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer)
            {
                activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse, modifiers = EventModifiers.Command });
            }
            else
            {
                activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse, modifiers = EventModifiers.Control });
            }

            m_GraphView = graphView;
            m_MovingElements = new List<MovingElement>();
            m_SelectedMovingElementIndex = 0;

            m_SelectionObserver = new SelectionObserver(graphView, this);
        }

        public void RegisterObservers(ObserverManager observerManager)
        {
            if( m_SelectionObserver != null)
                observerManager.RegisterObserver(m_SelectionObserver);
        }

        public void UnregisterObservers(ObserverManager observerManager)
        {
            if( m_SelectionObserver != null)
                observerManager.UnregisterObserver(m_SelectionObserver);
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

                // Temporary workaround until IN-17870 is fixed. When a contextual menu appears, if a mouse button
                // was already pressed, it gets stuck and the "pressedButtons" properties contains the wrong values.
                if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer)
                    PointerDeviceState.ReleaseButton(PointerId.mousePointerId, (int)MouseButton.LeftMouse);
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


        public void SetSelectionDirty()
        {
            m_ElementsToMoveDirty = true;
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
            var gvPos = new Vector2(m_GraphView.ViewTransform.position.x, m_GraphView.ViewTransform.position.y);
            var gvScale = m_GraphView.ViewTransform.scale.x;
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

                // Only start manipulating if the clicked element is movable, selected and that the mouse is in its clickable region (it must be deselected otherwise).
                if (!clickedElement.IsMovable() || !clickedElement.ContainsPoint(clickedElement.WorldToLocal(e.mousePosition)))
                    return;

                // In the case of a drag starting on an unselected element, the m_SelectionObserver notification will be too late, and the selection will indeed probably change.
                if (!m_PreviousSelection.Contains(clickedElement.Model))
                {
                    m_ElementsToMoveDirty = true;
                }

                if (m_ElementsToMoveDirty || e.shiftKey != m_MoveOnlyPlacemats)
                {
                    var selection = m_GraphView.GetSelection();
                    m_ElementsToMoveDirty = false;
                    m_PreviousSelection.Clear();
                    m_PreviousSelection.AddRange(selection);
                    m_MoveOnlyPlacemats = e.shiftKey;
                    RefreshElementsToMove(selection);
                }

                if (!m_ElementsToMove.Any())
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
                        Element = ce, InitialPosition = ce.layout.position
                    });
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

        void RefreshElementsToMove(IEnumerable<GraphElementModel> selection)
        {
            m_ElementsToMove.Clear();

            m_ElementsToMove.UnionWith(selection
                .Select(model => model.GetView_Internal(m_GraphView))
                .OfType<GraphElement>()
                .Where(t => !(t is Wire) && t.IsMovable()));

            if( ! m_ElementsToMove.Any())
                return;

            var selectedPlacemats = new HashSet<Placemat>(m_ElementsToMove.OfType<Placemat>());
            foreach (var placemat in selectedPlacemats)
                placemat.GetElementsToMove_Internal(m_MoveOnlyPlacemats, m_ElementsToMove);
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

            m_ElementsToMoveDirty = false;

            if ((e.pressedButtons & (1 << (int) MouseButton.MiddleMouse)) != 0)
            {
                OnMouseUp(e);
                return;
            }

            // We want the manipulator target to receive events even when mouse is not over it.
            // We wait for the (first) mouse move to capture the mouse because this is here that the interaction really begins.
            // At the mouse down stage, it is still to early, since the interaction could simply be a click and then should
            // be fully handled by another manipulator/element.
            if (!target.HasMouseCapture())
            {
                target.CaptureMouse();
            }

            m_TotalFreePanTravel = Vector2.zero;

            m_PanHelper.OnMouseMove(e);

            if (SelectedElement.parent != null)
            {
                m_TotalMouseDelta = GetDragAndSnapOffset(GetViewPositionInGraphSpace(e.localMousePosition));
                MoveElements(m_TotalMouseDelta);
            }

            var selection = m_GraphView.GetSelection();
            var selectedUI = selection.Select(m => m.GetView_Internal(m_GraphView));

            var previousTarget = m_CurrentSelectionDraggerTarget;
            m_CurrentSelectionDraggerTarget = GetTargetAt(e.mousePosition, selectedUI.ToList());

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

            if (m_Snapper.IsActive_Internal)
            {
                dragDelta = GetSnapCorrectedDelta(SelectedMovingElement, dragDelta);
            }

            return dragDelta;
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
                updater.MarkContentUpdated_Internal();
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
                        var changedElements = graphView.PositionDependenciesManager_Internal.StopNotifyMove();
                        using (var graphUpdater = m_GraphView.GraphViewModel.GraphModelState.UpdateScope)
                        {
                            graphUpdater.MarkChanged(changedElements, ChangeHint.Layout);
                        }
                    }

                    // if we stop dragging on something else than a DropTarget, just move elements
                    if (!canDrop || m_CurrentSelectionDraggerTarget == null || !m_CurrentSelectionDraggerTarget.CanAcceptDrop(selectedModels))
                    {
                        var models = m_MovingElements.Select(m => m.Element)
                            // PF remove this Where clause. It comes from VseGraphView.OnGraphViewChanged.
                            .Where(e => !(e.Model is AbstractNodeModel) || e.IsMovable())
                            .Select(e => e.Model)
                            .OfType<IMovable>()
                            .ToList();
                        var dragDelta = GetDragAndSnapOffset(GetViewPositionInGraphSpace(m_LastMousePosition));
                        m_GraphView.Dispatch(new MoveElementsCommand(dragDelta, models));
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
                if (m_Snapper.IsActive_Internal)
                    m_Snapper.EndSnap();

                target.ReleaseMouse();

                foreach (var element in m_MovingElements)
                {
                    element.Element.PositionIsOverriddenByManipulator = false;
                }
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
                var position = m_GraphView.ContentViewContainer.transform.position;
                var scale = m_GraphView.ContentViewContainer.transform.scale;
                m_GraphView.Dispatch(new ReframeGraphViewCommand(position, scale));
            }

            StopManipulation();
            e.StopPropagation();
        }

    }
}
