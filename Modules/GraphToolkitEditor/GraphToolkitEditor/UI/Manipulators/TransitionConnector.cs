// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// A manipulator to create transitions between states.
    /// </summary>
    [UnityRestricted]
    internal class TransitionConnector : MouseManipulator
    {
        const float k_WireCreationDistanceThreshold = WireUtilities.WireCreationDistanceThreshold;

        StateModel m_AnchorStateModel;
        GraphElement m_ConnectionTargetState;
        AbstractTransition m_TransitionCandidate;
        AbstractTransition m_ManipulatedTransition;

        bool m_Active;
        bool m_IsMovingFromConnector;
        Vector2 m_MouseDownPosition;

        /// <summary>
        /// The element that owns this manipulator.
        /// </summary>
        protected GraphElement OwnerElement => target as GraphElement;

        /// <summary>
        /// The model of the element that owns this manipulator.
        /// </summary>
        protected StateModel OwnerModel => OwnerElement?.Model as StateModel;

        /// <summary>
        /// The graph view that contains the element that owns this manipulator.
        /// </summary>
        protected GraphView GraphView => OwnerElement?.GraphView;

        /// <summary>
        /// The transition candidate model.
        /// </summary>
        protected AbstractGhostTransitionSupportModel TransitionCandidateModel => m_TransitionCandidate?.TransitionModel as AbstractGhostTransitionSupportModel;

        /// <summary>
        /// A function used to create the ghost transition model. If null, a default <see cref="GhostTransitionSupportModel"/> will be created.
        /// </summary>
        public Func<GraphModel, GhostTransitionSupportModel> GhostTransitionModelCreator { get; set; } = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="TransitionConnector"/> class.
        /// </summary>
        public TransitionConnector()
        {
            m_Active = false;
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
        }

        /// <inheritdoc />
        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<MouseDownEvent>(OnMouseDown);
            target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
            target.RegisterCallback<MouseUpEvent>(OnMouseUp);
            target.RegisterCallback<KeyDownEvent>(OnKeyDown);
            target.RegisterCallback<MouseCaptureOutEvent>(OnCaptureOut);
        }

        /// <inheritdoc />
        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
            target.UnregisterCallback<KeyDownEvent>(OnKeyDown);
            target.UnregisterCallback<MouseCaptureOutEvent>(OnCaptureOut);
        }

        void OnMouseDown(MouseDownEvent e)
        {
            if (m_Active)
            {
                e.StopImmediatePropagation();
                return;
            }

            if (!CanStartManipulation(e))
            {
                return;
            }

            m_MouseDownPosition = e.localMousePosition;

            if (HandleMouseDown(e))
            {
                m_Active = true;
                target.CaptureMouse();
                e.StopImmediatePropagation();
            }
        }

        /// <summary>
        /// Handles a mouse down event.
        /// </summary>
        /// <param name="evt">The mouse down event.</param>
        protected bool HandleMouseDown(MouseDownEvent evt)
        {
            var border = OwnerElement?.Border;

            if (border == null || !border.ContainsPoint(border.WorldToLocal(evt.mousePosition)))
                return false;

            CreateTransitionCandidate();
            PickTransitionFromState(OwnerModel, evt.mousePosition, true, out m_ManipulatedTransition, out m_IsMovingFromConnector);

            GraphElement anchorStateUI;

            if (m_ManipulatedTransition == null)
            {
                var snappedPoint = Vector2.zero;
                var anchorStateSide = AnchorSide.None;
                var anchorStateOffset = 0f;

                if (border is StateBorder stateBorder)
                {
                    var borderPoint = stateBorder.WorldToLocal(evt.mousePosition);
                    (snappedPoint, anchorStateSide, anchorStateOffset) = stateBorder.SnapPointToBorder(borderPoint, true);
                    snappedPoint = stateBorder.LocalToWorld(snappedPoint);
                }

                if (anchorStateSide == AnchorSide.None)
                {
                    Reset();
                    return false;
                }

                m_IsMovingFromConnector = false;
                m_AnchorStateModel = OwnerModel;
                anchorStateUI = OwnerElement;
                m_ManipulatedTransition = m_TransitionCandidate;

                TransitionCandidateModel.FromWorldPoint = snappedPoint;
                TransitionCandidateModel.FromPort = OwnerModel.GetOutPort();
                TransitionCandidateModel.FromNodeAnchorSide = anchorStateSide;
                TransitionCandidateModel.FromNodeAnchorOffset = anchorStateOffset;

                TransitionCandidateModel.ToWorldPoint = evt.mousePosition;
                TransitionCandidateModel.ToPort = null;
                TransitionCandidateModel.ToNodeAnchorSide = AnchorSide.None;
                TransitionCandidateModel.ToNodeAnchorOffset = 0;

                m_ConnectionTargetState = null;
            }
            else
            {
                if (m_IsMovingFromConnector)
                {
                    m_AnchorStateModel = m_ManipulatedTransition.TransitionModel.ToPort.NodeModel as StateModel;
                }
                else
                {
                    m_AnchorStateModel = m_ManipulatedTransition.TransitionModel.FromPort.NodeModel as StateModel;
                }

                if (m_AnchorStateModel == null)
                {
                    Reset();
                    return false;
                }

                anchorStateUI = m_AnchorStateModel.GetView<GraphElement>(GraphView);

                // Hide manipulated transition.
                m_ManipulatedTransition.style.display = DisplayStyle.None;

                // Update candidate model using manipulated transition model.
                var (snappedPoint, _, anchorSide, anchorOffset) = SnapPoint(evt.mousePosition);
                if (m_IsMovingFromConnector)
                {
                    TransitionCandidateModel.FromWorldPoint = snappedPoint;
                    TransitionCandidateModel.FromNodeAnchorSide = anchorSide;
                    TransitionCandidateModel.FromNodeAnchorOffset = anchorOffset;

                    TransitionCandidateModel.ToNodeAnchorSide = m_ManipulatedTransition.TransitionModel.ToNodeAnchorSide;
                    TransitionCandidateModel.ToNodeAnchorOffset = m_ManipulatedTransition.TransitionModel.ToNodeAnchorOffset;

                    TransitionCandidateModel.FromPort = null;
                    TransitionCandidateModel.ToPort = m_ManipulatedTransition.TransitionModel.ToPort;
                }
                else
                {
                    TransitionCandidateModel.ToWorldPoint = snappedPoint;
                    TransitionCandidateModel.ToNodeAnchorSide = anchorSide;
                    TransitionCandidateModel.ToNodeAnchorOffset = anchorOffset;

                    TransitionCandidateModel.FromNodeAnchorSide = m_ManipulatedTransition.TransitionModel.FromNodeAnchorSide;
                    TransitionCandidateModel.FromNodeAnchorOffset = m_ManipulatedTransition.TransitionModel.FromNodeAnchorOffset;

                    TransitionCandidateModel.FromPort = m_ManipulatedTransition.TransitionModel.FromPort;
                    TransitionCandidateModel.ToPort = null;
                }

                m_ConnectionTargetState = OwnerElement;
            }

            ShowNodeConnector(m_ConnectionTargetState);
            ShowNodeConnector(anchorStateUI);

            m_TransitionCandidate.DoCompleteUpdate();

            return true;
        }

        void OnMouseMove(MouseMoveEvent e)
        {
            if (!m_Active) return;
            HandleMouseMove(e);
            e.StopPropagation();
        }

        /// <summary>
        /// Handles a mouse move event.
        /// </summary>
        /// <param name="evt">The mouse move event.</param>
        protected virtual void HandleMouseMove(MouseMoveEvent evt)
        {
            var (snappedPoint, snappedElement, anchorSide, anchorOffset) = SnapPoint(evt.mousePosition);

            if (m_IsMovingFromConnector)
            {
                TransitionCandidateModel.FromWorldPoint = snappedPoint;
                TransitionCandidateModel.FromNodeAnchorSide = anchorSide; // for proper drawing
                TransitionCandidateModel.FromNodeAnchorOffset = anchorOffset;
            }
            else
            {
                TransitionCandidateModel.ToWorldPoint = snappedPoint;
                TransitionCandidateModel.ToNodeAnchorSide = anchorSide; // for proper drawing
                TransitionCandidateModel.ToNodeAnchorOffset = anchorOffset;
            }

            if (m_ConnectionTargetState != snappedElement)
            {
                HideNodeConnector(m_ConnectionTargetState);
                m_ConnectionTargetState = snappedElement;
            }

            if (anchorSide != AnchorSide.None)
            {
                ShowNodeConnector(m_ConnectionTargetState);
            }
            else
            {
                HideNodeConnector(m_ConnectionTargetState);
            }

            m_TransitionCandidate.DoCompleteUpdate();
        }

        void OnMouseUp(MouseUpEvent e)
        {
            if (!m_Active || !CanStopManipulation(e))
                return;

            try
            {
                var canConnect = Vector2.Distance(m_MouseDownPosition, e.localMousePosition) > k_WireCreationDistanceThreshold;
                if (canConnect)
                    HandleMouseUp(e);
                else
                    Reset();
            }
            finally
            {
                m_Active = false;
                target.ReleaseMouse();
                e.StopPropagation();
            }
        }

        /// <summary>
        /// Handles a mouse up event.
        /// </summary>
        /// <param name="evt">The mouse up event.</param>
        protected virtual void HandleMouseUp(MouseUpEvent evt)
        {
            var (_, snappedElement, targetStateSide, targetStateOffset) = SnapPoint(evt.mousePosition);

            var anchorStateUI = m_AnchorStateModel.GetView<GraphElement>(GraphView);

            HideNodeConnector(anchorStateUI);
            HideNodeConnector(m_ConnectionTargetState);

            if (snappedElement == null)
            {
                var anchorSide = m_IsMovingFromConnector ? TransitionCandidateModel.ToNodeAnchorSide : TransitionCandidateModel.FromNodeAnchorSide;
                var anchorOffset = m_IsMovingFromConnector ? TransitionCandidateModel.ToNodeAnchorOffset : TransitionCandidateModel.FromNodeAnchorOffset;
                var draggedTransition = (m_ManipulatedTransition == m_TransitionCandidate) ? TransitionCandidateModel : m_ManipulatedTransition.TransitionModel;
                var localPosition = GraphView.ContentViewContainer.WorldToLocal(evt.mousePosition);
                var anchorStateModel = m_AnchorStateModel;

                var fromStateLocalBounds = anchorStateUI?.localBound ?? default;
                float newStateAnchorOffset;
                AnchorSide newStateAnchorSide;
                switch (anchorSide)
                {
                    case AnchorSide.Right:
                        newStateAnchorSide = AnchorSide.Left;
                        newStateAnchorOffset = fromStateLocalBounds.height * 0.5f;
                        break;
                    case AnchorSide.Left:
                        newStateAnchorSide = AnchorSide.Right;
                        newStateAnchorOffset = fromStateLocalBounds.height * 0.5f;
                        break;
                    case AnchorSide.Top:
                        newStateAnchorSide = AnchorSide.Bottom;
                        newStateAnchorOffset = fromStateLocalBounds.width * 0.5f;
                        break;
                    case AnchorSide.None:
                    case AnchorSide.Bottom:
                    default:
                        newStateAnchorSide = AnchorSide.Top;
                        newStateAnchorOffset = fromStateLocalBounds.width * 0.5f;
                        break;
                }

                ItemLibraryService.ShowGraphNodes(GraphView, evt.mousePosition, item =>
                {
                    if (item is GraphNodeModelLibraryItem nodeItem)
                    {
                        GraphView.Dispatch(new CreateStateFromTransitionCommand(
                            nodeItem, draggedTransition, anchorStateModel, localPosition,
                            anchorSide, anchorOffset,
                            newStateAnchorSide, newStateAnchorOffset));
                    }
                    Reset();
                });
            }
            else if (targetStateSide != AnchorSide.None && snappedElement.Model is StateModel targetStateModel)
            {
                if (m_ManipulatedTransition == m_TransitionCandidate)
                {
                    GraphView.Dispatch(new CreateTransitionSupportCommand(
                        m_AnchorStateModel, TransitionCandidateModel.FromNodeAnchorSide, TransitionCandidateModel.FromNodeAnchorOffset,
                        targetStateModel, targetStateSide, targetStateOffset));
                }
                else
                {
                    GraphView.Dispatch(new MoveTransitionSupportCommand(
                        m_ManipulatedTransition.TransitionModel, targetStateSide, targetStateOffset,
                        m_IsMovingFromConnector ? MoveTransitionSupportCommand.FromTo.FromState : MoveTransitionSupportCommand.FromTo.ToState, targetStateModel));
                }

                Reset();
            }
        }

        /// <summary>
        /// Handles a key down event.
        /// </summary>
        /// <param name="e">The key down event.</param>
        protected virtual void OnKeyDown(KeyDownEvent e)
        {
            if (e.keyCode != KeyCode.Escape || !m_Active)
                return;

            Reset();

            m_Active = false;
            target.ReleaseMouse();
            e.StopPropagation();
        }

        /// <summary>
        /// Handles a capture out event.
        /// </summary>
        /// <param name="e">The capture out event.</param>
        protected virtual void OnCaptureOut(MouseCaptureOutEvent e)
        {
            m_Active = false;
            if (TransitionCandidateModel != null)
                Reset(false);
        }

        void CreateTransitionCandidate()
        {
            var model = GhostTransitionModelCreator != null ? GhostTransitionModelCreator.Invoke(GraphView.GraphModel) : new GhostTransitionSupportModel { GraphModel = GraphView.GraphModel };
            m_TransitionCandidate = ModelViewFactory.CreateUI<AbstractTransition>(GraphView, model);

            if (m_TransitionCandidate == null)
                return;

            m_TransitionCandidate.Layer = int.MaxValue;
            GraphView.AddElement(m_TransitionCandidate);
        }

        void Reset(bool removeCandidate = true)
        {
            if (removeCandidate)
            {
                if (m_ManipulatedTransition != null && m_ManipulatedTransition != m_TransitionCandidate)
                {
                    // Revert display value to non-overridden.
                    m_ManipulatedTransition.style.display = StyleKeyword.Null;
                }

                if (m_TransitionCandidate != null)
                {
                    GraphView.RemoveElement(m_TransitionCandidate);
                    m_TransitionCandidate.ResetLayer();
                }

                m_TransitionCandidate = null;
                m_ManipulatedTransition = null;
            }

            m_ConnectionTargetState = null;
            m_AnchorStateModel = null;
        }

        void ShowNodeConnector(GraphElement node)
        {
            if (node is INodeWithConnector nodeWithConnector)
            {
                nodeWithConnector.ShowConnector(m_TransitionCandidate);
            }
        }

        void HideNodeConnector(GraphElement node)
        {
            if (node is INodeWithConnector nodeWithConnector)
            {
                nodeWithConnector.HideConnector(m_TransitionCandidate);
            }
        }

        (Vector2 SnappedPosition, GraphElement SnappedElement, AnchorSide SnappedSide, float Offset) SnapPoint(Vector2 globalPoint)
        {
            GraphElement snappedElement = null;
            StateModel snappedStateModel = null;

            foreach (var nodeModel in GraphView.GraphModel.NodeModels)
            {
                if (nodeModel is not StateModel stateModel || stateModel == m_AnchorStateModel)
                    continue;

                var stateUI = stateModel.GetView<GraphElement>(GraphView);
                if (stateUI != null)
                {
                    var bounds = stateUI.worldBound;

                    var interactionBorder = stateUI.Border;
                    if (interactionBorder != null)
                        bounds = interactionBorder.worldBound;
                    if (bounds.Contains(globalPoint))
                    {
                        snappedElement = stateUI;
                        snappedStateModel = stateModel;
                        break;
                    }
                }
            }

            return SnapPointToState(globalPoint, snappedElement, snappedStateModel);
        }

        (Vector2 SnappedPosition, GraphElement SnappedElement, AnchorSide SnappedSide, float Offset) SnapPointToState(Vector2 globalPoint, GraphElement targetState, StateModel targetStateModel)
        {
            var isHoveringTransition = PickTransitionFromState(targetStateModel, globalPoint, false, out var transition, out var isOutgoing);

            if (isHoveringTransition)
            {
                return (transition.LocalToWorld(isOutgoing ? transition.GetFrom() : transition.GetTo()),
                    targetState,
                    isOutgoing ? transition.TransitionModel.FromNodeAnchorSide : transition.TransitionModel.ToNodeAnchorSide,
                    isOutgoing ? transition.TransitionModel.FromNodeAnchorOffset : transition.TransitionModel.ToNodeAnchorOffset);
            }

            if (targetState?.Border is StateBorder stateBorder)
            {
                var borderPoint = stateBorder.WorldToLocal(globalPoint);
                var (snappedPoint, anchorSide, offset) = stateBorder.SnapPointToBorder(borderPoint, false);
                return (stateBorder.LocalToWorld(snappedPoint), targetState, anchorSide, offset);
            }

            return (globalPoint, null, AnchorSide.None, 0f);
        }

        bool PickTransitionFromState(StateModel stateModel, Vector2 mousePosition, bool onlySelected, out AbstractTransition transition, out bool isOutgoing)
        {
            transition = null;
            isOutgoing = false;

            if (stateModel == null)
                return false;

            foreach (var wireModel in stateModel.GetConnectedWires())
            {
                if (wireModel == m_ManipulatedTransition?.WireModel)
                    continue;

                if (wireModel == m_TransitionCandidate?.WireModel)
                    continue;

                if (wireModel is TransitionSupportModel { IsSingleStateTransition: true })
                    continue;

                if (onlySelected && !GraphView.GraphViewModel.SelectionState.IsSelected(wireModel))
                    continue;

                var transitionUI = wireModel.GetView<AbstractTransition>(GraphView);
                if (transitionUI == null)
                    continue;

                if (transitionUI.ContainsPoint(transitionUI.WorldToLocal(mousePosition)))
                {
                    transition = transitionUI;
                    isOutgoing = wireModel.FromPort == stateModel.GetOutPort();
                    return true;
                }
            }

            return false;
        }
    }
}
