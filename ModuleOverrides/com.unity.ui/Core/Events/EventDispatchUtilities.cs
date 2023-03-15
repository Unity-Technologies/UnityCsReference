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
    /// In the TrickleDown phase, the event is sent from the panel's root element to the target element.
    ///
    /// In the BubbleUp phase, the event is sent from the target element back to the panel's root element.
    /// </remarks>
    public enum PropagationPhase
    {
        // Not propagating at the moment.
        /// <summary>
        /// The event is not propagated.
        /// </summary>
        None = 0,

        // Propagation from root of tree to target.
        /// <summary>
        /// The event is sent from the panel's root element to the target element.
        /// </summary>
        TrickleDown = 1,

        // After the target has gotten the chance to handle the event, the event walks up the hierarchy back to root.
        /// <summary>
        /// The event is sent from the target element back to the panel's root element.
        /// </summary>
        BubbleUp = 3,

        [Obsolete("PropagationPhase.AtTarget has been removed as part of an event propagation simplification. Events now propagate through the TrickleDown phase followed immediately by the BubbleUp phase. Please use TrickleDown or BubbleUp. You can check if the event target is the current element by testing event.target == this in your local callback.", false)]
        AtTarget = 2,
        [Obsolete("PropagationPhase.DefaultAction has been removed as part of an event propagation simplification. ExecuteDefaultAction now occurs as part of the BubbleUp phase. Please use BubbleUp.", false)]
        DefaultAction = 4,
        [Obsolete("PropagationPhase.DefaultActionAtTarget has been removed as part of an event propagation simplification. ExecuteDefaultActionAtTarget now occurs as part of the BubbleUp phase. Please use BubbleUp", false)]
        DefaultActionAtTarget = 5,
    }

    static class EventDispatchUtilities
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void PropagateEvent(EventBase evt, [NotNull] BaseVisualElementPanel panel, [NotNull] VisualElement target)
        {
            // Assume events here bubble up or trickle down, otherwise HandleEventAtTargetPhase is called directly.
            // Early out if no callback on target or any of its parents.
            if (target.HasParentEventInterests(evt.eventCategory) && !evt.isPropagationStopped)
            {
                Debug.Assert(!evt.dispatch, "Event is being dispatched recursively.");
                evt.dispatch = true;

                if ((evt as IPointerEventInternal)?.compatibilityMouseEvent is EventBase compatibilityEvt)
                {
                    HandleEventAcrossPropagationPathWithCompatibilityEvent(evt, compatibilityEvt, panel, target);
                }
                else
                {
                    HandleEventAcrossPropagationPath(evt, panel, target);
                }

                evt.dispatch = false;
            }
        }

        public static void HandleEventAtTargetAndDefaultPhase(EventBase evt, [NotNull] BaseVisualElementPanel panel, [NotNull] VisualElement target)
        {
            if (!target.HasSelfEventInterests(evt.eventCategory) || evt.isPropagationStopped)
                return;

            evt.currentTarget = target;

            try
            {
                // None of the mouse compatibility events are dispatched using HandleEventAtTargetAndDefaultPhase
                Debug.Assert(!(evt is IPointerEventInternal pe) || pe.compatibilityMouseEvent == null, "!(evt is IPointerEventInternal pe) || pe.compatibilityMouseEvent == null");

                evt.propagationPhase = PropagationPhase.TrickleDown;
                if (target.HasTrickleDownEventCallbacks(evt.eventCategory))
                {
                    target.m_CallbackRegistry?.m_TrickleDownCallbacks.Invoke(evt, panel, target);
                    if (evt.isImmediatePropagationStopped)
                        return;
                }

                if (target.HasTrickleDownHandleEvent(evt.eventCategory))
                {
                    HandleEvent_DefaultActionTrickleDown(evt, panel, target);
                }

                if (evt.isPropagationStopped)
                    return;

                evt.propagationPhase = PropagationPhase.BubbleUp;

                if (target.HasBubbleUpHandleEvent(evt.eventCategory))
                {
                    HandleEvent_DefaultActionBubbleUp(evt, panel, target);
                    if (evt.isImmediatePropagationStopped)
                        return;
                }

                if (target.HasBubbleUpEventCallbacks(evt.eventCategory))
                {
                    target.m_CallbackRegistry?.m_BubbleUpCallbacks.Invoke(evt, panel, target);
                }
            }
            finally
            {
                evt.currentTarget = null;
                evt.propagationPhase = PropagationPhase.None;
            }
        }

        private static void HandleEventAcrossPropagationPath(EventBase evt, [NotNull] BaseVisualElementPanel panel, [NotNull] VisualElement target)
        {
            var eventCategory = evt.eventCategory;

            using var path = PropagationPaths.Build(target, evt, 1 << (int) eventCategory);

            try
            {
                // Phase 1: TrickleDown phase
                // Propagate event from root to target
                evt.propagationPhase = PropagationPhase.TrickleDown;

                for (int i = path.trickleDownPath.Count - 1; i >= 0; i--)
                {
                    var element = path.trickleDownPath[i];

                    evt.currentTarget = element;

                    if (element.HasTrickleDownEventCallbacks(eventCategory))
                    {
                        element.m_CallbackRegistry?.InvokeCallbacks(evt, panel, element, CallbackPhase.TrickleDown);

                        if (evt.isImmediatePropagationStopped)
                            return;
                    }

                    if (element.HasTrickleDownHandleEvent(eventCategory))
                    {
                        HandleEvent_DefaultActionTrickleDown(evt, panel, element);
                    }

                    if (evt.isPropagationStopped)
                        return;
                }

                // Phase 2: bubble up phase
                // Propagate event from target up to root
                evt.propagationPhase = PropagationPhase.BubbleUp;

                foreach (var element in path.bubbleUpPath)
                {
                    evt.currentTarget = element;

                    if (element.HasBubbleUpHandleEvent(eventCategory))
                    {
                        HandleEvent_DefaultActionBubbleUp(evt, panel, element);

                        if (evt.isImmediatePropagationStopped)
                            return;
                    }

                    if (element.HasBubbleUpEventCallbacks(eventCategory))
                    {
                        element.m_CallbackRegistry?.InvokeCallbacks(evt, panel, element, CallbackPhase.BubbleUp);
                    }

                    if (evt.isPropagationStopped)
                        return;
                }
            }
            finally
            {
                evt.currentTarget = null;
                evt.propagationPhase = PropagationPhase.None;
            }
        }

        private static void HandleEventAcrossPropagationPathWithCompatibilityEvent(EventBase evt, [NotNull] EventBase compatibilityEvt, [NotNull] BaseVisualElementPanel panel, [NotNull] VisualElement target)
        {
            compatibilityEvt.elementTarget = target;
            compatibilityEvt.skipDisabledElements = evt.skipDisabledElements;

            if (DebuggerEventDispatchUtilities.InterceptEvent(compatibilityEvt, panel))
            {
                HandleEventAcrossPropagationPath(evt, panel, target);
                DebuggerEventDispatchUtilities.PostDispatch(compatibilityEvt, panel);
                return;
            }

            // Match callbacks from both evt and compatibilityEvt.
            int eventCategories = (1 << (int) evt.eventCategory) | (1 << (int) compatibilityEvt.eventCategory);

            using var path = PropagationPaths.Build(target, evt, eventCategories);

            try
            {
                // Phase 1: TrickleDown phase
                // Propagate event from root to target
                evt.propagationPhase = PropagationPhase.TrickleDown;
                compatibilityEvt.propagationPhase = PropagationPhase.TrickleDown;

                for (int i = path.trickleDownPath.Count - 1; i >= 0; i--)
                {
                    var element = path.trickleDownPath[i];

                    evt.currentTarget = element;

                    compatibilityEvt.currentTarget = element;

                    if (element.HasTrickleDownEventCallbacks(eventCategories))
                    {
                        element.m_CallbackRegistry?.InvokeCallbacks(evt, panel, element, CallbackPhase.TrickleDown);

                        if (evt.isImmediatePropagationStopped)
                            return;

                        if (panel.ShouldSendCompatibilityMouseEvents((IPointerEvent)evt))
                        {
                            element.m_CallbackRegistry?.InvokeCallbacks(compatibilityEvt, panel, element, CallbackPhase.TrickleDown);

                            if (evt.isImmediatePropagationStopped)
                                return;
                        }
                    }

                    if (element.HasTrickleDownHandleEvent(eventCategories))
                    {
                        HandleEvent_DefaultActionTrickleDown(evt, panel, element);

                        if (evt.isImmediatePropagationStopped)
                            return;

                        if (panel.ShouldSendCompatibilityMouseEvents((IPointerEvent)evt))
                        {
                            HandleEvent_DefaultActionTrickleDown(compatibilityEvt, panel, element);

                            if (compatibilityEvt.isImmediatePropagationStopped)
                                return;
                        }
                    }

                    if (evt.isPropagationStopped || compatibilityEvt.isPropagationStopped)
                        return;
                }

                // Phase 2: bubble up phase
                // Propagate event from target up to root
                evt.propagationPhase = PropagationPhase.BubbleUp;
                compatibilityEvt.propagationPhase = PropagationPhase.BubbleUp;

                foreach (var element in path.bubbleUpPath)
                {
                    evt.currentTarget = element;
                    compatibilityEvt.currentTarget = element;

                    if (element.HasBubbleUpHandleEvent(eventCategories))
                    {
                        HandleEvent_DefaultActionBubbleUp(evt, panel, element);

                        if (evt.isImmediatePropagationStopped)
                            return;

                        if (panel.ShouldSendCompatibilityMouseEvents((IPointerEvent)evt))
                        {
                            HandleEvent_DefaultActionBubbleUp(compatibilityEvt, panel, element);

                            if (compatibilityEvt.isImmediatePropagationStopped)
                                return;
                        }
                    }

                    if (element.HasBubbleUpEventCallbacks(eventCategories))
                    {
                        element.m_CallbackRegistry?.InvokeCallbacks(evt, panel, element, CallbackPhase.BubbleUp);

                        if (evt.isImmediatePropagationStopped)
                            return;

                        if (panel.ShouldSendCompatibilityMouseEvents((IPointerEvent)evt))
                        {
                            element.m_CallbackRegistry?.InvokeCallbacks(compatibilityEvt, panel, element, CallbackPhase.BubbleUp);

                            if (compatibilityEvt.isImmediatePropagationStopped)
                                return;
                        }
                    }

                    if (evt.isPropagationStopped || compatibilityEvt.isPropagationStopped)
                        return;
                }
            }
            finally
            {
                evt.currentTarget = null;
                evt.propagationPhase = PropagationPhase.None;

                compatibilityEvt.currentTarget = null;
                compatibilityEvt.propagationPhase = PropagationPhase.None;

                DebuggerEventDispatchUtilities.PostDispatch(compatibilityEvt, panel);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void HandleEvent_DefaultActionTrickleDown(EventBase evt, [NotNull] BaseVisualElementPanel panel, [NotNull] VisualElement target)
        {
            if (target.elementPanel != panel)
                return;

            using (new EventDebuggerLogExecuteDefaultAction(evt))
            {
                if (evt.skipDisabledElements && !target.enabledInHierarchy)
                {
                    target.HandleEventTrickleDownDisabled(evt);
                }
                else
                {
                    target.HandleEventTrickleDownInternal(evt);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void HandleEvent_DefaultActionBubbleUp(EventBase evt, [NotNull] BaseVisualElementPanel panel, [NotNull] VisualElement target)
        {
            if (target.elementPanel != panel)
                return;

            using (new EventDebuggerLogExecuteDefaultAction(evt))
            {
                if (evt.skipDisabledElements && !target.enabledInHierarchy)
                {
                    // Offer minimal support for [Obsolete] ExecuteDefaultAction and ExecuteDefaultActionAtTarget.
                    if (target == evt.elementTarget || target.isCompositeRoot)
                    {
#pragma warning disable 618
                        target.ExecuteDefaultActionDisabledAtTarget(evt);
#pragma warning restore 618
                        target.HandleEventBubbleUpDisabled(evt);
                        target.ExecuteDefaultActionDisabledInternal(evt);
                    }
                    else
                    {
                        target.HandleEventBubbleUpDisabled(evt);
                    }
                }
                else
                {
                    if (target == evt.elementTarget || target.isCompositeRoot)
                    {
                        target.ExecuteDefaultActionAtTargetInternal(evt);
                        target.HandleEventBubbleUpInternal(evt);
                        target.ExecuteDefaultActionInternal(evt);
                    }
                    else
                    {
                        target.HandleEventBubbleUpInternal(evt);
                    }
                }
            }
        }

        public static void HandleEvent(EventBase evt, VisualElement target)
        {
            if (evt.isPropagationStopped)
                return;

            switch (evt.propagationPhase)
            {
                case PropagationPhase.TrickleDown:
                    target.m_CallbackRegistry?.InvokeCallbacks(evt, target.elementPanel, target, CallbackPhase.TrickleDown);
                    if (!evt.isImmediatePropagationStopped)
                        HandleEvent_DefaultActionTrickleDown(evt, target.elementPanel, target);
                    break;
                case PropagationPhase.BubbleUp:
                    HandleEvent_DefaultActionBubbleUp(evt, target.elementPanel, target);
                    if (!evt.isImmediatePropagationStopped)
                        target.m_CallbackRegistry?.InvokeCallbacks(evt, target.elementPanel, target, CallbackPhase.BubbleUp);
                    break;
            }
        }

        public static void DispatchToFocusedElementOrPanelRoot(EventBase evt, [NotNull] BaseVisualElementPanel panel)
        {
            bool propagateToIMGUI = false;
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
                    propagateToIMGUI = true;
                }

                // If mouse is captured, handle event at captured element if it's not already in the propagation path
                if (panel.GetCapturingElement(PointerId.mousePointerId) is VisualElement capturingElement &&
                    capturingElement != target && !capturingElement.Contains(target) &&
                    capturingElement.HasSelfEventInterests(evt.eventCategory))
                {
                    evt.elementTarget = capturingElement;

                    var skipDisabledElements = evt.skipDisabledElements;
                    evt.skipDisabledElements = false;
                    HandleEventAtTargetAndDefaultPhase(evt, panel, capturingElement);
                    evt.skipDisabledElements = skipDisabledElements;
                }

                evt.elementTarget = target;
            }

            PropagateEvent(evt, panel, target);

            if (propagateToIMGUI && evt.propagateToIMGUI)
                PropagateToRemainingIMGUIContainers(evt, panel.visualTree);
        }

        public static void DispatchToElementUnderPointerOrPanelRoot(EventBase evt,
            [NotNull] BaseVisualElementPanel panel, int pointerId, Vector2 position)
        {
            // Important: don't inline this. We need to RecomputeTopElement even if it's not going to be used.
            var topElement = panel.RecomputeTopElementUnderPointer(pointerId, position, evt);

            bool propagateToIMGUI = false;
            var target = evt.elementTarget;
            if (target == null)
            {
                target = topElement;
                if (target == null)
                {
                    target = panel.visualTree;
                    propagateToIMGUI = true;
                }

                evt.elementTarget = target;
            }

            PropagateEvent(evt, panel, target);

            if (propagateToIMGUI && evt.propagateToIMGUI)
                PropagateToRemainingIMGUIContainers(evt, panel.visualTree);
        }

        public static void DispatchToCachedElementUnderPointerOrPanelRoot(EventBase evt,
            [NotNull] BaseVisualElementPanel panel, int pointerId, Vector2 position)
        {
            bool propagateToIMGUI = false;
            var target = evt.elementTarget;
            if (target == null)
            {
                target = panel.GetTopElementUnderPointer(pointerId);
                if (target == null)
                {
                    target = panel.visualTree;
                    propagateToIMGUI = true;
                }

                evt.elementTarget = target;
            }

            PropagateEvent(evt, panel, target);

            if (propagateToIMGUI && evt.propagateToIMGUI)
                PropagateToRemainingIMGUIContainers(evt, panel.visualTree);
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
            // Case 1353921: this will enforce PointerEnter/Out events even during pointer capture.
            // According to the W3 standard (https://www.w3.org/TR/pointerevents3/#the-pointerout-event), these events
            // are *not* supposed to occur, but we have been sending MouseEnter/Out events during mouse capture
            // since the early days of UI Toolkit, and users have been relying on it.
            panel.RecomputeTopElementUnderPointer(pointerId, position, evt);

            if (DispatchToCapturingElement(evt, panel, pointerId, position))
                return;

            DispatchToCachedElementUnderPointerOrPanelRoot(evt, panel, pointerId, position);
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

            evt.skipDisabledElements = false;
            evt.elementTarget = capturingElement;
            PropagateEvent(evt, panel, capturingElement);

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
    }
}
