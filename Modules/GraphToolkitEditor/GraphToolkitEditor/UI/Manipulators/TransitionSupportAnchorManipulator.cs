// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Manipulator to move transition anchors.
    /// </summary>
    [UnityRestricted]
    internal class TransitionSupportAnchorManipulator : MouseManipulator
    {
        const int k_StartDragDistanceSquare = 4 * 4;
        const float k_PositionPadding = 15.0f;

        TransitionSupportModel m_TransitionModel;
        Vector2 m_MouseDownPosition;
        Vector2 m_AnchorWorldOffset;
        AnchorSide m_OriginalAnchorSide;
        float m_AdjustedPositionPadding;
        float m_OriginalAnchorOffset;

        GraphView GraphView => (target as ChildView)?.RootView as GraphView;

        bool m_MovingAnchor;

        /// <summary>
        /// Initializes a new instance of the <see cref="TransitionSupportAnchorManipulator"/> class.
        /// </summary>
        public TransitionSupportAnchorManipulator()
        {
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
            Reset();
        }

        /// <inheritdoc />
        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<MouseDownEvent>(OnMouseDown);
            target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
            target.RegisterCallback<MouseUpEvent>(OnMouseUp);
            target.RegisterCallback<KeyDownEvent>(OnKeyDown);
        }

        /// <inheritdoc />
        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
            target.UnregisterCallback<KeyDownEvent>(OnKeyDown);
        }

        void Reset()
        {
            m_MovingAnchor = false;
            m_TransitionModel = null;
            m_OriginalAnchorSide = AnchorSide.None;
            m_OriginalAnchorOffset = 0.0f;
        }

        void OnMouseDown(MouseDownEvent evt)
        {
            if (!CanStartManipulation(evt))
            {
                return;
            }

            var transition = target as AbstractTransition;
            m_TransitionModel = transition?.TransitionModel;

            m_OriginalAnchorOffset = m_TransitionModel?.ToNodeAnchorOffset ?? 0.0f;
            m_OriginalAnchorSide = m_TransitionModel?.ToNodeAnchorSide ?? AnchorSide.None;
            m_AnchorWorldOffset = evt.mousePosition - transition.LocalToWorld(transition?.GetTo() ?? Vector2.zero);
            m_MouseDownPosition = evt.mousePosition;
            m_AdjustedPositionPadding = k_PositionPadding * GraphView.Zoom;

            transition.CaptureMouse();
            evt.StopPropagation();
        }

        void OnMouseMove(MouseMoveEvent evt)
        {
            // If the left mouse button is not down then return
            if (m_TransitionModel == null)
            {
                return;
            }

            evt.StopPropagation();

            if (!m_MovingAnchor)
            {
                var deltaSquare = (evt.mousePosition - m_MouseDownPosition).sqrMagnitude;

                if (deltaSquare < k_StartDragDistanceSquare)
                {
                    return;
                }

                m_MovingAnchor = true;
            }

            var targetState = m_TransitionModel.ToNodeGuid;
            var targetSide = m_TransitionModel.ToNodeAnchorSide;

            var targetStateUI = targetState.GetView<State>(GraphView);
            if (targetStateUI == null)
                return;

            var targetOffset = 0.0f;
            switch (targetSide)
            {
                case AnchorSide.Top:
                case AnchorSide.Bottom:
                {
                    var newPosX = Math.Clamp(evt.mousePosition.x - m_AnchorWorldOffset.x, targetStateUI.worldBound.xMin + m_AdjustedPositionPadding, targetStateUI.worldBound.xMax - m_AdjustedPositionPadding);
                    targetOffset = (newPosX - targetStateUI.worldBound.xMin) / targetStateUI.worldBound.width * targetStateUI.localBound.width;
                    break;
                }
                case AnchorSide.Left:
                case AnchorSide.Right:
                {
                    var newPosY = Math.Clamp(evt.mousePosition.y - m_AnchorWorldOffset.y, targetStateUI.worldBound.yMin + m_AdjustedPositionPadding, targetStateUI.worldBound.yMax - m_AdjustedPositionPadding);
                    targetOffset = (newPosY - targetStateUI.worldBound.yMin) / targetStateUI.worldBound.height * targetStateUI.localBound.height;
                    break;
                }
            }

            if (targetSide != AnchorSide.None)
            {
                m_TransitionModel.SetToAnchor(targetSide, targetOffset);
            }
        }

        void OnMouseUp(MouseUpEvent evt)
        {
            if (CanStopManipulation(evt) && m_TransitionModel != null)
            {
                var currentSide = m_TransitionModel.ToNodeAnchorSide;
                var currentOffset = m_TransitionModel.ToNodeAnchorOffset;

                if (currentSide != m_OriginalAnchorSide || Math.Abs(currentOffset - m_OriginalAnchorOffset) > float.Epsilon)
                {
                    ResetTransitionToDefaultValues();
                    var toState = m_TransitionModel.ToNodeGuid.GetView<GraphElement>(GraphView);
                    if (toState != null)
                    {
                        var stateRect = toState.localBound;
                        currentOffset = Math.Clamp(currentOffset, 0, currentSide is AnchorSide.Bottom or AnchorSide.Top ? stateRect.width : stateRect.height);
                    }
                    GraphView.Dispatch(new MoveTransitionSupportCommand(m_TransitionModel, currentSide, currentOffset, MoveTransitionSupportCommand.FromTo.ToState));
                }

                target.ReleaseMouse();
                Reset();
                evt.StopPropagation();
            }
        }

        void ResetTransitionToDefaultValues()
        {
            m_TransitionModel.SetToAnchor(m_OriginalAnchorSide, m_OriginalAnchorOffset);
        }

        void OnKeyDown(KeyDownEvent evt)
        {
            if (m_TransitionModel != null)
            {
                if (evt.keyCode == KeyCode.Escape)
                {
                    ResetTransitionToDefaultValues();
                    Reset();
                    target.ReleaseMouse();
                    evt.StopPropagation();
                }
            }
        }
    }
}
