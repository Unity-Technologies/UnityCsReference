// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Customization of the <see cref="ContextualMenuManipulator"/> for the <see cref="GraphView"/>.
    /// </summary>
    [UnityRestricted]
    internal class GraphViewContextualMenuManipulator : ContextualMenuManipulator
    {
        // Set on registration so unregistration removes the same callbacks.
        bool m_DetectsPan;

        // State for right-click pan detection, used on platforms where the menu opens on mouse up.
        Vector2 m_RightMouseDownPosition;
        // Matches ContentDragger's right-button drag threshold for consistent pan classification.
        const float k_DragThreshold = 8f;
        // Smaller than k_DragThreshold: a pan rarely moves 8px within k_SelectionDelayMs, so the drag threshold
        // alone lets the delayed selection fire at the start of most pans. Kept above zero to tolerate click jitter.
        const float k_SelectionCancelThreshold = 1f;
        // Allows pan detection to kick in before selection is applied; deliberate right-clicks hold longer.
        const long k_SelectionDelayMs = 30;
        IVisualElementScheduledItem m_PendingSelectionTimer;
        GraphElement m_PendingTarget;
        // The press target; unlike m_PendingTarget, it is kept when a pan is detected.
        GraphElement m_GestureTarget;
        bool m_PanDetected;
        bool m_GestureActive;
        bool m_GestureActionKey;
        GraphView m_ActiveGestureGraphView;

        /// <inheritdoc cref="ContextualMenuManipulator(Action{ContextualMenuPopulateEvent})"/>
        public GraphViewContextualMenuManipulator(Action<ContextualMenuPopulateEvent> menuBuilder)
            : base(menuBuilder)
        {
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.RightMouse });
            if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer)
            {
                activators.Add(new ManipulatorActivationFilter { button = MouseButton.RightMouse, modifiers = EventModifiers.Command });
            }
            else
            {
                activators.Add(new ManipulatorActivationFilter { button = MouseButton.RightMouse, modifiers = EventModifiers.Control });
            }
        }

        /// <inheritdoc />
        protected override void RegisterCallbacksOnTarget()
        {
            // On macOS the menu opens on mouse down, before a pan can be detected.
            m_DetectsPan = !UIElementsUtility.isOSXContextualMenuPlatform;
            if (m_DetectsPan)
            {
                target.RegisterCallback<MouseDownEvent>(OnRightMouseDown);
                // Register before base so our handler fires before ContextualMenuManipulator's
                // MouseUpEvent handler, letting us suppress the menu for panning.
                target.RegisterCallback<MouseUpEvent>(OnRightMouseUp);
                // OnMouseMove, OnGraphViewMouseUp, and OnGraphViewPointerCaptureOut are registered
                // lazily on the GraphView per gesture; the mouse-up handler cleans up sub-threshold
                // gestures released over the background, and the capture-out handler cleans up
                // confirmed pans where ContentDragger takes pointer capture.
            }
            else
            {
                target.RegisterCallback<MouseDownEvent>(SelectOnMouseDown);
            }
            base.RegisterCallbacksOnTarget();
        }

        /// <inheritdoc />
        protected override void UnregisterCallbacksFromTarget()
        {
            if (m_DetectsPan)
            {
                target.UnregisterCallback<MouseDownEvent>(OnRightMouseDown);
                target.UnregisterCallback<MouseUpEvent>(OnRightMouseUp);
                EndGesture();
                m_PendingSelectionTimer?.Pause();
                m_PendingSelectionTimer = null;
            }
            else
            {
                target.UnregisterCallback<MouseDownEvent>(SelectOnMouseDown);
            }
            base.UnregisterCallbacksFromTarget();
        }

        void OnRightMouseDown(MouseDownEvent evt)
        {
            if (evt.button != (int)MouseButton.RightMouse)
                return;

            if (evt.currentTarget is not GraphElement graphElement)
                return;

            if (!IsInnermostGraphElementTarget(evt.target, graphElement))
                return;

            if (!CanStartManipulation(evt))
                return;

            var graphView = graphElement.GraphView;

            // Store in GraphView local space so OnMouseMove (registered on the GraphView) can compare directly.
            m_RightMouseDownPosition = graphView != null
                ? graphElement.ChangeCoordinatesTo(graphView, evt.localMousePosition)
                : evt.localMousePosition;

            EndGesture();
            m_GestureActionKey = evt.actionKey;
            m_PendingTarget = graphElement;
            m_GestureTarget = graphElement;
            m_PanDetected = false;
            m_GestureActive = true;
            m_ActiveGestureGraphView = graphView;
            m_ActiveGestureGraphView?.RegisterCallback<MouseMoveEvent>(OnMouseMove);
            // Trickle down so the release is seen even when the element under the cursor is not the press target.
            m_ActiveGestureGraphView?.RegisterCallback<MouseUpEvent>(OnGraphViewMouseUp, TrickleDown.TrickleDown);
            m_ActiveGestureGraphView?.RegisterCallback<PointerCaptureOutEvent>(OnGraphViewPointerCaptureOut);

            m_PendingSelectionTimer?.Pause();
            m_PendingSelectionTimer = target.schedule.Execute(ApplyPendingSelection).StartingIn(k_SelectionDelayMs);
        }

        void OnMouseMove(MouseMoveEvent evt)
        {
            if (m_PendingTarget == null)
                return;

            // evt.localMousePosition is in GraphView local space (handler registered on GraphView).
            var delta = evt.localMousePosition - m_RightMouseDownPosition;

            // Only cancels the delayed selection; if the gesture ends as a click, the selection is applied on mouse up.
            if (delta.sqrMagnitude > k_SelectionCancelThreshold * k_SelectionCancelThreshold)
                m_PendingSelectionTimer?.Pause();

            if (delta.sqrMagnitude <= k_DragThreshold * k_DragThreshold)
                return;

            m_PendingSelectionTimer?.Pause();
            m_PanDetected = true;
            m_PendingTarget = null;
        }

        void OnRightMouseUp(MouseUpEvent evt)
        {
            if (evt.button != (int)MouseButton.RightMouse)
                return;

            if (!m_GestureActive)
                return;

            if (evt.currentTarget is not GraphElement graphElement)
                return;

            if (!IsInnermostGraphElementTarget(evt.target, graphElement))
                return;

            // Convert to GraphView local space to match m_RightMouseDownPosition.
            var mousePos = m_ActiveGestureGraphView != null
                ? graphElement.ChangeCoordinatesTo(m_ActiveGestureGraphView, evt.localMousePosition)
                : evt.localMousePosition;
            var delta = mousePos - m_RightMouseDownPosition;
            bool isPan = m_PanDetected || delta.sqrMagnitude > k_DragThreshold * k_DragThreshold;

            EndGesture();
            m_PendingTarget = null;
            m_PanDetected = false;

            if (isPan)
            {
                // Stop all remaining handlers on this element (so the base does not open the menu)
                // and stop bubbling so outer GraphElements do not open their own menus.
                evt.StopImmediatePropagation();
                return;
            }

            // Ensure selection is applied before the context menu is built, even for fast clicks
            // where the timer has not fired yet.
            if (!graphElement.IsSelected())
                SelectElement(graphElement, m_GestureActionKey);

            // Stop bubbling so outer GraphElements do not open their own menus; same-element
            // handlers (the base) still fire and open the context menu.
            evt.StopPropagation();
        }

        // Fires after k_SelectionDelayMs on a stationary right-click, pre-painting the selection
        // border before the OS blocks the render loop to show the context menu.
        void ApplyPendingSelection()
        {
            if (m_PendingTarget == null)
                return;
            if (m_PendingTarget.panel == null)
                return;

            if (!m_PendingTarget.IsSelected())
                SelectElement(m_PendingTarget, m_GestureActionKey);
        }

        // Pre-paints the selection border before the context menu blocks the render loop.
        // Resolves visuals through the mapped model so a Marker pre-paints its parent node
        // and skips when the parent is already selected.
        void SelectElement(GraphElement graphElement, bool actionKey)
        {
            var model = graphElement is Marker marker ? marker.ParentModel : graphElement.GraphElementModel;
            if (model == null || !model.IsSelectable())
                return;

            var graphView = graphElement.GraphView;
            var visualTarget = graphView != null ? model.GetView<GraphElement>(graphView) ?? graphElement : graphElement;

            if (visualTarget.IsSelected())
                return;

            var currentSelection = actionKey ? null : graphView?.GetSelection();
            if (currentSelection != null)
            {
                foreach (var selectedModel in currentSelection)
                    selectedModel.GetView<GraphElement>(graphView)?.UpdateSelectionVisuals(false);
            }

            visualTarget.UpdateSelectionVisuals(true);
            GraphViewClickSelector.SelectElements(graphElement, actionKey, true);
        }

        // Prevents outer GraphElements from handling events that belong to a nested one (e.g. blocks in contexts).
        static bool IsInnermostGraphElementTarget(IEventHandler eventTarget, GraphElement graphElement)
        {
            var innermost = eventTarget as GraphElement
                ?? (eventTarget as VisualElement)?.GetFirstAncestorOfType<GraphElement>();
            return innermost == graphElement;
        }

        // ContentDragger released the pointer after a confirmed pan; end the gesture so OnMouseMove
        // and this handler are not left permanently registered on the GraphView.
        void OnGraphViewPointerCaptureOut(PointerCaptureOutEvent evt)
        {
            m_PendingSelectionTimer?.Pause();
            m_PendingTarget = null;
            m_PanDetected = false;
            EndGesture();
        }

        // Handles a right-button release away from the press target; releases over the press target are left to
        // OnRightMouseUp. A click released over another element selects that element, since its menu is the one
        // that opens. A release over the graph background only cleans up, so per-gesture callbacks don't accumulate.
        void OnGraphViewMouseUp(MouseUpEvent evt)
        {
            if (evt.button != (int)MouseButton.RightMouse)
                return;
            if (!m_GestureActive)
                return;

            var releaseTarget = evt.target as GraphElement
                ?? (evt.target as VisualElement)?.GetFirstAncestorOfType<GraphElement>();
            if (releaseTarget == m_GestureTarget)
                return;

            // evt.localMousePosition is in GraphView local space (handler registered on GraphView).
            var delta = evt.localMousePosition - m_RightMouseDownPosition;
            bool isPan = m_PanDetected || delta.sqrMagnitude > k_DragThreshold * k_DragThreshold;
            if (releaseTarget != null && !isPan)
                SelectElement(releaseTarget, m_GestureActionKey);

            m_PendingSelectionTimer?.Pause();
            m_PendingTarget = null;
            m_PanDetected = false;
            EndGesture();
        }

        // Unregisters the per-gesture GraphView callbacks and clears gesture state.
        void EndGesture()
        {
            m_ActiveGestureGraphView?.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
            m_ActiveGestureGraphView?.UnregisterCallback<MouseUpEvent>(OnGraphViewMouseUp, TrickleDown.TrickleDown);
            m_ActiveGestureGraphView?.UnregisterCallback<PointerCaptureOutEvent>(OnGraphViewPointerCaptureOut);
            m_ActiveGestureGraphView = null;
            m_GestureTarget = null;
            m_GestureActive = false;
        }

        void SelectOnMouseDown(MouseDownEvent e)
        {
            if (CanStartManipulation(e) && e.currentTarget is GraphElement graphElement && !graphElement.IsSelected())
                GraphViewClickSelector.SelectElements(graphElement, e.actionKey, true);
        }
    }
}
