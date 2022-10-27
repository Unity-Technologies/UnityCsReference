// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using UnityEngine.Pool;
using UnityEngine.UIElements.Experimental;

namespace UnityEngine.UIElements
{
    // determines in which event phase an event handler wants to handle events
    // the handler always gets called if it is the target VisualElement
    /// <summary>
    /// The propagation phases of an event.
    /// </summary>
    /// <remarks>
    /// When an element receives an event, the event propagates from the panel's root element to the target element.
    ///
    /// In the TrickleDown phase, the event is sent from the panel's root element to the target element's parent.
    ///
    /// In the AtTarget phase, the event is sent to the target element.
    ///
    /// In the BubbleUp phase, the event is sent from the target element's parent back to the panel's root element.
    ///
    /// In the last phase, the DefaultAction phase, the event is resent to the target element.
    /// </remarks>
    public enum PropagationPhase
    {
        // Not propagating at the moment.
        /// <summary>
        /// The event is not propagated.
        /// </summary>
        None = 0,

        // Propagation from root of tree to immediate parent of target.
        /// <summary>
        /// The event is sent from the panel's root element to the target element's parent.
        /// </summary>
        TrickleDown = 1,

        // Event is at target.
        /// <summary>
        /// The event is sent to the target.
        /// </summary>
        AtTarget = 2,

        // Execute the default action(s) at target.
        /// <summary>
        /// The event is sent to the target element, which can then execute its default actions for the event at the target phase. Event handlers do not receive the event in this phase. Instead, ExecuteDefaultActionAtTarget is called on the target element.
        /// </summary>
        DefaultActionAtTarget = 5,

        // After the target has gotten the chance to handle the event, the event walks back up the parent hierarchy back to root.
        /// <summary>
        /// The event is sent from the target element's parent back to the panel's root element.
        /// </summary>
        BubbleUp = 3,

        // At last, execute the default action(s).
        /// <summary>
        /// The event is sent to the target element, which can then execute its final default actions for the event. Event handlers do not receive the event in this phase. Instead, ExecuteDefaultAction is called on the target element.
        /// </summary>
        DefaultAction = 4
    }

    static class EventDispatchUtilities
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void PropagateEvent(EventBase evt, [NotNull] BaseVisualElementPanel panel, [NotNull] VisualElement target)
        {
            evt.leafTarget = target;

            // Assume events here bubble up or trickle down, otherwise HandleEventAtTargetPhase is called directly.
            // Early out if no callback on target or any of its parents.
            if (target.HasParentEventCallbacksOrDefaultActions(evt.eventCategory))
            {
                Debug.Assert(!evt.dispatch, "Event is being dispatched recursively.");
                evt.dispatch = true;

                using (var path = PropagationPaths.Build(target, evt))
                {
                    HandleEventAcrossPropagationPath(evt, panel, target, path);
                }

                evt.dispatch = false;

                // Reset target to leaf target so it can be accessed in PostDispatch.
                evt.elementTarget = target;
                evt.currentTarget = null;
                evt.propagationPhase = PropagationPhase.None;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void HandleEventAtTargetPhase(EventBase evt, [NotNull] BaseVisualElementPanel panel, [NotNull] VisualElement target)
        {
            evt.currentTarget = evt.leafTarget = target;

            if (!evt.isPropagationStopped && target.HasEventCallbacks(evt.eventCategory))
            {
                evt.propagationPhase = PropagationPhase.AtTarget;
                target.m_CallbackRegistry?.InvokeCallbacksAtTarget(evt, panel, target);
            }

            if (!evt.isDefaultPrevented && target.HasDefaultActionAtTarget(evt.eventCategory))
            {
                evt.propagationPhase = PropagationPhase.DefaultActionAtTarget;
                HandleEvent_DefaultActionAtTarget(evt, panel, target);
            }
        }

        public static void HandleEventAtTargetAndDefaultPhase(EventBase evt, [NotNull] BaseVisualElementPanel panel, [NotNull] VisualElement target)
        {
            evt.currentTarget = evt.leafTarget = target;

            if (!evt.isPropagationStopped && target.HasEventCallbacks(evt.eventCategory))
            {
                evt.propagationPhase = PropagationPhase.AtTarget;
                target.m_CallbackRegistry?.InvokeCallbacksAtTarget(evt, panel, target);
            }

            if (evt.isDefaultPrevented)
                return;

            if (target.HasDefaultActionAtTarget(evt.eventCategory))
            {
                evt.propagationPhase = PropagationPhase.DefaultActionAtTarget;
                HandleEvent_DefaultActionAtTarget(evt, panel, target);

                if (evt.isDefaultPrevented)
                    return;
            }

            if (target.HasDefaultAction(evt.eventCategory))
            {
                evt.propagationPhase = PropagationPhase.DefaultAction;
                HandleEvent_DefaultAction(evt, panel, target);
            }
        }

        private static void HandleEventAcrossPropagationPath(EventBase evt, [NotNull] BaseVisualElementPanel panel, [NotNull] VisualElement leafTarget, [NotNull] PropagationPaths path)
        {
            var eventCategory = evt.eventCategory;

            // Phase 1: TrickleDown phase
            // Propagate event from root to target.parent
            if (evt.tricklesDown)
            {
                evt.propagationPhase = PropagationPhase.TrickleDown;

                for (int i = path.trickleDownPath.Count - 1; i >= 0; i--)
                {
                    if (evt.isPropagationStopped)
                        break;

                    var element = path.trickleDownPath[i];

                    Debug.Assert(element.HasEventCallbacks(eventCategory));
                    evt.currentTarget = element;
                    element.m_CallbackRegistry?.InvokeCallbacks(evt, panel, element, CallbackPhase.TrickleDown);
                }
            }

            // Phase 2: Target / DefaultActionAtTarget
            // Propagate event from target parent up to root for the target phase
            evt.propagationPhase = PropagationPhase.AtTarget;

            foreach (var element in path.targetElements)
            {
                if (!element.HasEventCallbacks(eventCategory))
                    continue;

                if (evt.isPropagationStopped)
                    break;

                evt.currentTarget = evt.elementTarget = element;
                element.m_CallbackRegistry?.InvokeCallbacksAtTarget(evt, panel, element);
            }

            // Call ExecuteDefaultActionAtTarget
            evt.propagationPhase = PropagationPhase.DefaultActionAtTarget;

            foreach (var element in path.targetElements)
            {
                if (!element.HasDefaultActionAtTarget(eventCategory))
                    continue;

                if (evt.isDefaultPrevented)
                    break;

                evt.currentTarget = evt.elementTarget = element;
                HandleEvent_DefaultActionAtTarget(evt, panel, element);
            }

            // Phase 3: bubble up phase
            // Propagate event from target parent up to root
            if (evt.bubbles)
            {
                evt.propagationPhase = PropagationPhase.BubbleUp;

                // Reset target to original target
                evt.elementTarget = leafTarget;

                foreach (var element in path.bubbleUpPath)
                {
                    if (evt.isPropagationStopped)
                        break;

                    Debug.Assert(element.HasEventCallbacks(eventCategory));
                    evt.currentTarget = element;
                    element.m_CallbackRegistry?.InvokeCallbacks(evt, panel, element, CallbackPhase.BubbleUp);
                }
            }

            // Call ExecuteDefaultAction
            evt.propagationPhase = PropagationPhase.DefaultAction;

            foreach (var element in path.targetElements)
            {
                if (!element.HasDefaultAction(eventCategory))
                    continue;

                if (evt.isDefaultPrevented)
                    break;

                evt.currentTarget = evt.elementTarget = element;
                HandleEvent_DefaultAction(evt, panel, element);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void PropagateEvent_DefaultAction(EventBase evt, [NotNull] BaseVisualElementPanel panel, [NotNull] VisualElement target, [NotNull] PropagationPaths path)
        {
            // Call ExecuteDefaultAction
            evt.propagationPhase = PropagationPhase.DefaultAction;

            foreach (var element in path.targetElements)
            {
                if (element.HasDefaultAction(evt.eventCategory))
                {
                    evt.currentTarget = evt.elementTarget = element;
                    HandleEvent_DefaultAction(evt, panel, element);

                    if (evt.isDefaultPrevented)
                        break;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void HandleEvent_DefaultActionAtTarget(EventBase evt, [NotNull] BaseVisualElementPanel panel, [NotNull] VisualElement target)
        {
            if (target.elementPanel != panel)
                return;

            using (new EventDebuggerLogExecuteDefaultAction(evt))
            {
                if (evt.skipDisabledElements && !target.enabledInHierarchy)
                    target.ExecuteDefaultActionDisabledAtTarget(evt);
                else
                    target.ExecuteDefaultActionAtTargetInternal(evt);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void HandleEvent_DefaultAction(EventBase evt, [NotNull] BaseVisualElementPanel panel, [NotNull] VisualElement target)
        {
            if (target.elementPanel != panel)
                return;

            using (new EventDebuggerLogExecuteDefaultAction(evt))
            {
                if (evt.skipDisabledElements && !target.enabledInHierarchy)
                    target.ExecuteDefaultActionDisabled(evt);
                else
                    target.ExecuteDefaultActionInternal(evt);
            }
        }

        public static void DispatchToFocusedElementOrPanelRoot(EventBase evt, [NotNull] BaseVisualElementPanel panel)
        {
            var target = evt.elementTarget;
            if (target == null)
            {
                // Assign target to focused element or panel visual tree.
                var leafFocusedElement = panel.focusController.GetLeafFocusedElement();
                if (leafFocusedElement is VisualElement ve)
                {
                    target = ve;
                }
                else
                {
                    target = panel.visualTree;
                }

                // If mouse is captured, handle event at captured element if it's not already in the propagation path
                if (panel.GetCapturingElement(PointerId.mousePointerId) is VisualElement capturingElement &&
                    capturingElement != target && !capturingElement.Contains(target) &&
                    capturingElement.HasEventCallbacksOrDefaultActionAtTarget(evt.eventCategory))
                {
                    evt.elementTarget = capturingElement;
                    var skipDisabledElements = evt.skipDisabledElements;
                    evt.skipDisabledElements = false;
                    HandleEventAtTargetPhase(evt, panel, capturingElement);
                    evt.skipDisabledElements = skipDisabledElements;
                }

                evt.elementTarget = target;
            }

            PropagateEvent(evt, panel, target);
        }

        public static void DispatchToElementUnderPointerOrPanelRoot(EventBase evt,
            [NotNull] BaseVisualElementPanel panel, int pointerId, Vector2 position)
        {
            // Important: don't inline this. We need to RecomputeTopElement even if it's not going to be used.
            var topElement = panel.RecomputeTopElementUnderPointer(pointerId, position, evt);

            var target = evt.elementTarget ??= topElement ?? panel.visualTree;

            PropagateEvent(evt, panel, target);
        }

        public static void DispatchToCachedElementUnderPointerOrPanelRoot(EventBase evt,
            [NotNull] BaseVisualElementPanel panel, int pointerId, Vector2 position)
        {
            var target = evt.elementTarget ??= panel.GetTopElementUnderPointer(pointerId) ?? panel.visualTree;

            PropagateEvent(evt, panel, target);
        }

        public static void DispatchToAssignedTarget(EventBase evt, [NotNull] BaseVisualElementPanel panel)
        {
            var target = evt.elementTarget;
            if (target == null)
                throw new ArgumentException($"Event target not set. Event type {evt.GetType()} requires a target.");

            PropagateEvent(evt, panel, target);
        }

        public static void DefaultDispatch(EventBase evt, [NotNull] BaseVisualElementPanel panel)
        {
            var target = evt.elementTarget;
            if (target == null)
            {
                // Don't throw an exception, event may have Pre/PostDispatch overrides that make it relevant.
                return;
            }

            // Most events will bubble up or trickle down, but custom events may not.
            if (evt.bubblesOrTricklesDown)
                PropagateEvent(evt, panel, target);
            else
                HandleEventAtTargetAndDefaultPhase(evt, panel, target);
        }

        public static void DispatchToCapturingElementOrElementUnderPointer(EventBase evt,
            [NotNull] BaseVisualElementPanel panel, int pointerId, Vector2 position)
        {
            if (DispatchToCapturingElement(evt, panel, pointerId, position))
                return;

            DispatchToElementUnderPointerOrPanelRoot(evt, panel, pointerId, position);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool DispatchToCapturingElement(EventBase evt, [NotNull] BaseVisualElementPanel panel, int pointerId, Vector2 position)
        {
            var capturingElement = panel.GetCapturingElement(pointerId) as VisualElement;

            if (capturingElement == null)
            {
                return false;
            }

            // Release pointer capture if capture element is not in a panel.
            if (capturingElement.panel == null)
            {
                panel.ReleasePointer(pointerId);
                return false;
            }

            if (evt.target != null && evt.target != capturingElement)
            {
                return false;
            }

            // Case 1342115: mouse position is in local panel coordinates; sending event to a target from a different
            // panel will lead to a wrong position, so we don't allow it. Note that in general the mouse-down-move-up
            // sequence still works properly because the OS captures the mouse on the starting EditorWindow.
            if (capturingElement.panel != panel)
            {
                return false;
            }

            evt.dispatch = true;

            // Case 1353921: this will enforce PointerEnter/Out events even during pointer capture.
            // According to the W3 standard (https://www.w3.org/TR/pointerevents3/#the-pointerout-event), these events
            // are *not* supposed to occur, but we have been sending MouseEnter/Out events during mouse capture
            // since the early days of UI Toolkit, and users have been relying on it.
            panel.RecomputeTopElementUnderPointer(pointerId, position, evt);

            // Exclusive processing by capturing element but ExecuteDefaultActions on all composite roots.
            // Assume capturing element panel matching current panel has already been tested before calling this method.
            evt.skipDisabledElements = false;
            if (capturingElement.HasEventCallbacksOrDefaultActionAtTarget(evt.eventCategory))
            {
                evt.elementTarget = capturingElement;
                HandleEventAtTargetPhase(evt, panel, capturingElement);
            }
            else
            {
                // Make sure target is assigned for ExecuteDefaultActions
                evt.elementTarget = evt.leafTarget = capturingElement;
            }

            if (!evt.isDefaultPrevented)
            {
                // Pointer events with capturing element
                // don't call PropagateEvents but still need to call ExecuteDefaultActions on composite roots.
                using (var path = PropagationPaths.BuildAtTarget(capturingElement, evt))
                {
                    PropagateEvent_DefaultAction(evt, panel, capturingElement, path);
                }

                // Reset target to leaf target so it can be accessed in PostDispatch.
                evt.elementTarget = capturingElement;
            }

            evt.dispatch = false;
            return true;
        }

        internal static void DispatchToPanelRoot(EventBase evt, [NotNull] BaseVisualElementPanel panel)
        {
            var target = evt.elementTarget = panel.visualTree;

            PropagateEvent(evt, panel, target);
        }

        internal static void PropagateToRemainingIMGUIContainers(EventBase evt, [NotNull] VisualElement root)
        {
            if (evt.imguiEvent != null && root.elementPanel.contextType != ContextType.Player)
                PropagateToRemainingIMGUIContainerRecursive(evt, root);
        }

        private static void PropagateToRemainingIMGUIContainerRecursive(EventBase evt, [NotNull] VisualElement root)
        {
            // Send the event to the first IMGUIContainer that can handle it.
            if (root.isIMGUIContainer)
            {
                if (root != evt.target)
                {
                    var imContainer = (IMGUIContainer) root;

                    // Only permit switching the focus to another IMGUIContainer if the event target was not focusable.
                    bool targetIsFocusable = evt.elementTarget?.focusable ?? false;

                    if (imContainer.SendEventToIMGUI(evt, !targetIsFocusable))
                    {
                        evt.StopPropagation();
                        evt.PreventDefault();
                    }

                    if (evt.imguiEvent.rawType == EventType.Used)
                        Debug.Assert(evt.isPropagationStopped);
                }

                // IMGUIContainer have no children. We can return without iterating the children list.
                return;
            }

            if (root.imguiContainerDescendantCount > 0)
            {
                using (ListPool<VisualElement>.Get(out var childrenToNotify))
                {
                    childrenToNotify.AddRange(root.hierarchy.children);

                    foreach (var child in childrenToNotify)
                    {
                        // if child is no longer in the hierarchy (removed when notified another child. See issue 1413477)
                        // , then ignore it.
                        if (child.hierarchy.parent != root)
                            continue;

                        PropagateToRemainingIMGUIContainerRecursive(evt, child);

                        if (evt.isPropagationStopped)
                            break;
                    }
                }
            }
        }

        public static void HandleEvent(EventBase evt, VisualElement target)
        {
            switch (evt.propagationPhase)
            {
                case PropagationPhase.TrickleDown:
                    if (!evt.isPropagationStopped)
                        target.m_CallbackRegistry?.InvokeCallbacks(evt, target.elementPanel, target, CallbackPhase.TrickleDown);
                    break;
                case PropagationPhase.BubbleUp:
                    if (!evt.isPropagationStopped)
                        target.m_CallbackRegistry?.InvokeCallbacks(evt, target.elementPanel, target, CallbackPhase.BubbleUp);
                    break;

                case PropagationPhase.AtTarget:
                    if (!evt.isPropagationStopped)
                        target.m_CallbackRegistry?.InvokeCallbacksAtTarget(evt, target.elementPanel, target);
                    break;

                case PropagationPhase.DefaultActionAtTarget:
                    if (!evt.isDefaultPrevented)
                        HandleEvent_DefaultActionAtTarget(evt, target.elementPanel, target);
                    break;

                case PropagationPhase.DefaultAction:
                    if (!evt.isDefaultPrevented)
                        HandleEvent_DefaultAction(evt, target.elementPanel, target);
                    break;
            }
        }
    }
}
