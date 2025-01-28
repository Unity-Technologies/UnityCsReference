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
    // Determines in which event phase an event handler wants to handle events.
    // The handler always gets called if it is the target VisualElement.
    /// <summary>
    /// The propagation phases of an event.
    /// </summary>
    /// <remarks>
    /// When an element receives an event, the event propagates from the panel's root element to the target element.
    ///
    /// In the TrickleDown phase, the event is sent from the panel's root element to the target element.
    ///
    /// In the BubbleUp phase, the event is sent from the target element back to the panel's root element.
    ///
    /// Refer to the [[wiki:UIE-Events-Dispatching|Dispatch events]] manual page for more information and examples.
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
        /// <remarks>
        /// Refer to the [[wiki:UIE-Events-Dispatching|Dispatch events]] manual page for more information and examples.
        /// </remarks>
        TrickleDown = 1,

        // After the target has gotten the chance to handle the event, the event walks up the hierarchy back to root.
        /// <summary>
        /// The event is sent from the target element back to the panel's root element.
        /// </summary>
        /// <remarks>
        /// Refer to the [[wiki:UIE-Events-Dispatching|Dispatch events]] manual page for more information and examples.
        /// </remarks>
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
        private static void PropagateEvent(EventBase evt, [NotNull] BaseVisualElementPanel panel, [NotNull] VisualElement target, bool isCapturingTarget)
        {
            // Assume events here bubble up or trickle down, otherwise HandleEventAtTargetPhase is called directly.
            if ((evt as IPointerEventInternal)?.compatibilityMouseEvent is EventBase compatibilityEvt)
            {
                HandleEventAcrossPropagationPathWithCompatibilityEvent(evt, compatibilityEvt, panel, target, isCapturingTarget);
            }
            else
            {
                HandleEventAcrossPropagationPath(evt, panel, target, isCapturingTarget);
            }
        }

        public static void HandleEventAtTargetAndDefaultPhase(EventBase evt, [NotNull] BaseVisualElementPanel panel, [NotNull] VisualElement target)
        {
            var eventCategories = evt.eventCategories;
            if (!target.HasSelfEventInterests(eventCategories) || evt.isPropagationStopped)
                return;

            evt.currentTarget = target;

            try
            {
                // None of the mouse compatibility events are dispatched using HandleEventAtTargetAndDefaultPhase
                Debug.Assert(!(evt is IPointerEventInternal pe) || pe.compatibilityMouseEvent == null, "!(evt is IPointerEventInternal pe) || pe.compatibilityMouseEvent == null");

                evt.propagationPhase = PropagationPhase.TrickleDown;
                if (target.HasTrickleDownEventCallbacks(eventCategories))
                {
                    HandleEvent_TrickleDownCallbacks(evt, panel, target);
                    if (evt.isImmediatePropagationStopped)
                        return;
                }

                if (target.HasTrickleDownHandleEvent(eventCategories))
                {
                    HandleEvent_TrickleDownHandleEvent(evt, panel, target, Disabled(evt, target));
                }

                if (evt.isPropagationStopped)
                    return;

                evt.propagationPhase = PropagationPhase.BubbleUp;

                if (target.HasBubbleUpHandleEvent(eventCategories))
                {
                    var disabled = Disabled(evt, target);
                    HandleEvent_DefaultActionAtTarget(evt, panel, target, disabled);
                    HandleEvent_BubbleUpHandleEvent(evt, panel, target, disabled);
                    HandleEvent_DefaultAction(evt, panel, target, disabled);
                    if (evt.isImmediatePropagationStopped)
                        return;
                }

                if (target.HasBubbleUpEventCallbacks(eventCategories))
                {
                    HandleEvent_BubbleUpCallbacks(evt, panel, target);
                }
            }
            finally
            {
                evt.currentTarget = null;
                evt.propagationPhase = PropagationPhase.None;
            }
        }

        private static void HandleEventAcrossPropagationPath(EventBase evt, [NotNull] BaseVisualElementPanel panel, [NotNull] VisualElement target, bool isCapturingTarget)
        {
            var eventCategories = evt.eventCategories;

            if (!target.HasParentEventInterests(eventCategories) || evt.isPropagationStopped)
                return;

            using var path = PropagationPaths.Build(target, evt, eventCategories);

            try
            {
                Debug.Assert(!evt.dispatch, "Event is being dispatched recursively.");
                evt.dispatch = true;

                // Phase 1: TrickleDown phase
                // Propagate event from root to target
                evt.propagationPhase = PropagationPhase.TrickleDown;

                int i = path.trickleDownPath.Count - 1;

                // Skip trickle down on non-target when propagating during pointer capture
                if (isCapturingTarget && i >= 0)
                    i = path.trickleDownPath[0] == target ? 0 : -1;

                for (; i >= 0; i--)
                {
                    var element = path.trickleDownPath[i];

                    evt.currentTarget = element;

                    if (element.HasTrickleDownEventCallbacks(eventCategories))
                    {
                        HandleEvent_TrickleDownCallbacks(evt, panel, element);

                        if (evt.isImmediatePropagationStopped)
                            return;
                    }

                    if (element.HasTrickleDownHandleEvent(eventCategories))
                    {
                        HandleEvent_TrickleDownHandleEvent(evt, panel, element, Disabled(evt, element));
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

                    if (element.HasBubbleUpHandleEvent(eventCategories))
                    {
                        HandleEvent_BubbleUpAllDefaultActions(evt, panel, element, Disabled(evt, element), isCapturingTarget);

                        if (evt.isImmediatePropagationStopped)
                            return;
                    }

                    if (element.HasBubbleUpEventCallbacks(eventCategories) && (!isCapturingTarget || element == target))
                    {
                        HandleEvent_BubbleUpCallbacks(evt, panel, element);
                    }

                    if (evt.isPropagationStopped)
                        return;
                }
            }
            finally
            {
                evt.currentTarget = null;
                evt.propagationPhase = PropagationPhase.None;
                evt.dispatch = false;
            }
        }

        private static void HandleEventAcrossPropagationPathWithCompatibilityEvent(EventBase evt, [NotNull] EventBase compatibilityEvt, [NotNull] BaseVisualElementPanel panel, [NotNull] VisualElement target, bool isCapturingTarget)
        {
            // Match callbacks from both evt and compatibilityEvt.
            int eventCategories = evt.eventCategories | compatibilityEvt.eventCategories;

            if (!target.HasParentEventInterests(eventCategories) || evt.isPropagationStopped || compatibilityEvt.isPropagationStopped)
                return;

            compatibilityEvt.elementTarget = target;
            compatibilityEvt.skipDisabledElements = evt.skipDisabledElements;

            if (DebuggerEventDispatchUtilities.InterceptEvent(compatibilityEvt, panel))
                return;

            using var path = PropagationPaths.Build(target, evt, eventCategories);
            try
            {
                Debug.Assert(!evt.dispatch, "Event is being dispatched recursively.");
                evt.dispatch = true;

                // Phase 1: TrickleDown phase
                // Propagate event from root to target
                evt.propagationPhase = PropagationPhase.TrickleDown;
                compatibilityEvt.propagationPhase = PropagationPhase.TrickleDown;

                int i = path.trickleDownPath.Count - 1;

                // Skip trickle down on non-target when propagating during pointer capture
                if (isCapturingTarget && i >= 0)
                    i = path.trickleDownPath[0] == target ? 0 : -1;

                for (; i >= 0; i--)
                {
                    var element = path.trickleDownPath[i];

                    evt.currentTarget = element;

                    compatibilityEvt.currentTarget = element;

                    if (element.HasTrickleDownEventCallbacks(eventCategories))
                    {
                        HandleEvent_TrickleDownCallbacks(evt, panel, element);

                        if (evt.isImmediatePropagationStopped)
                            return;

                        if (panel.ShouldSendCompatibilityMouseEvents((IPointerEvent)evt))
                        {
                            HandleEvent_TrickleDownCallbacks(compatibilityEvt, panel, element);

                            if (evt.isImmediatePropagationStopped)
                                return;
                        }
                    }

                    if (element.HasTrickleDownHandleEvent(eventCategories))
                    {
                        var disabled = Disabled(evt, element);
                        HandleEvent_TrickleDownHandleEvent(evt, panel, element, disabled);

                        if (evt.isImmediatePropagationStopped)
                            return;

                        if (panel.ShouldSendCompatibilityMouseEvents((IPointerEvent)evt))
                        {
                            HandleEvent_TrickleDownHandleEvent(compatibilityEvt, panel, element, disabled);

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
                        var disabled = Disabled(evt, element);
                        HandleEvent_BubbleUpAllDefaultActions(evt, panel, element, disabled, isCapturingTarget);

                        if (evt.isImmediatePropagationStopped)
                            return;

                        if (panel.ShouldSendCompatibilityMouseEvents((IPointerEvent)evt))
                        {
                            HandleEvent_BubbleUpAllDefaultActions(compatibilityEvt, panel, element, disabled, isCapturingTarget);

                            if (compatibilityEvt.isImmediatePropagationStopped)
                                return;
                        }
                    }

                    if (element.HasBubbleUpEventCallbacks(eventCategories) && (!isCapturingTarget || element == target))
                    {
                        HandleEvent_BubbleUpCallbacks(evt, panel, element);

                        if (evt.isImmediatePropagationStopped)
                            return;

                        if (panel.ShouldSendCompatibilityMouseEvents((IPointerEvent)evt))
                        {
                            HandleEvent_BubbleUpCallbacks(compatibilityEvt, panel, element);

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

                evt.dispatch = false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void HandleEvent_DefaultActionAtTarget(EventBase evt, [NotNull] BaseVisualElementPanel panel,
            [NotNull] VisualElement element, bool disabled)
        {
            if (element.elementPanel != panel)
                return;

            using (new EventDebuggerLogExecuteDefaultAction(evt))
            {
                if (disabled)
                    element.ExecuteDefaultActionDisabledAtTargetInternal(evt);
                else
                    element.ExecuteDefaultActionAtTargetInternal(evt);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void HandleEvent_DefaultAction(EventBase evt, [NotNull] BaseVisualElementPanel panel,
            [NotNull] VisualElement element, bool disabled)
        {
            if (element.elementPanel != panel)
                return;

            using (new EventDebuggerLogExecuteDefaultAction(evt))
            {
                if (disabled)
                    element.ExecuteDefaultActionDisabledInternal(evt);
                else
                    element.ExecuteDefaultActionInternal(evt);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void HandleEvent_TrickleDownCallbacks(EventBase evt, [NotNull] BaseVisualElementPanel panel, [NotNull] VisualElement element)
        {
            element.m_CallbackRegistry?.m_TrickleDownCallbacks.Invoke(evt, panel, element);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void HandleEvent_BubbleUpCallbacks(EventBase evt, [NotNull] BaseVisualElementPanel panel, [NotNull] VisualElement element)
        {
            element.m_CallbackRegistry?.m_BubbleUpCallbacks.Invoke(evt, panel, element);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void HandleEvent_TrickleDownHandleEvent(EventBase evt, [NotNull] BaseVisualElementPanel panel, [NotNull] VisualElement element, bool disabled)
        {
            if (element.elementPanel != panel)
                return;

            if (disabled)
                element.HandleEventTrickleDownDisabled(evt);
            else
                element.HandleEventTrickleDownInternal(evt);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void HandleEvent_BubbleUpHandleEvent(EventBase evt, [NotNull] BaseVisualElementPanel panel, [NotNull] VisualElement element, bool disabled)
        {
            if (element.elementPanel != panel)
                return;

            if (disabled)
                element.HandleEventBubbleUpDisabled(evt);
            else
                element.HandleEventBubbleUpInternal(evt);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void HandleEvent_BubbleUpAllDefaultActions(EventBase evt, [NotNull] BaseVisualElementPanel panel, [NotNull] VisualElement element, bool disabled, bool isCapturingTarget)
        {
            // Exclusive processing by capturing element but ExecuteDefaultActions on all composite roots.
            var handleEvent = element == evt.target || !isCapturingTarget;
            var executeDefault = element == evt.target || element.isCompositeRoot;

            if (executeDefault)
                HandleEvent_DefaultActionAtTarget(evt, panel, element, disabled);
            if (handleEvent)
                HandleEvent_BubbleUpHandleEvent(evt, panel, element, disabled);
            if (executeDefault)
                HandleEvent_DefaultAction(evt, panel, element, disabled);
        }

        private static bool Disabled([NotNull] EventBase evt, [NotNull] VisualElement target)
        {
            return evt.skipDisabledElements && !target.enabledInHierarchy;
        }

        public static void HandleEvent([NotNull] EventBase evt, [NotNull] VisualElement target)
        {
            if (evt.isPropagationStopped)
                return;

            var panel = target.elementPanel;
            var disabled = Disabled(evt, target);

            switch (evt.propagationPhase)
            {
                case PropagationPhase.TrickleDown:
                    HandleEvent_TrickleDownCallbacks(evt, panel, target);
                    if (!evt.isImmediatePropagationStopped)
                        HandleEvent_TrickleDownHandleEvent(evt, panel, target, disabled);
                    break;
                case PropagationPhase.BubbleUp:
                    HandleEvent_BubbleUpAllDefaultActions(evt, panel, target, disabled, false);
                    if (!evt.isImmediatePropagationStopped)
                        HandleEvent_BubbleUpCallbacks(evt, panel, target);
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
                    capturingElement.HasSelfEventInterests(evt.eventCategories))
                {
                    evt.elementTarget = capturingElement;

                    var skipDisabledElements = evt.skipDisabledElements;
                    evt.skipDisabledElements = false;
                    HandleEventAtTargetAndDefaultPhase(evt, panel, capturingElement);
                    evt.skipDisabledElements = skipDisabledElements;
                }

                evt.elementTarget = target;
            }

            PropagateEvent(evt, panel, target, false);

            if (propagateToIMGUI && evt.propagateToIMGUI)
                PropagateToRemainingIMGUIContainers(evt, panel.visualTree);
        }

        public static void DispatchToElementUnderPointerOrPanelRoot(EventBase evt,
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

            PropagateEvent(evt, panel, target, false);

            if (propagateToIMGUI && evt.propagateToIMGUI)
                PropagateToRemainingIMGUIContainers(evt, panel.visualTree);
        }

        public static void DispatchToAssignedTarget(EventBase evt, [NotNull] BaseVisualElementPanel panel)
        {
            var target = evt.elementTarget;
            if (target == null)
                throw new ArgumentException($"Event target not set. Event type {evt.GetType()} requires a target.");

            PropagateEvent(evt, panel, target, false);
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
                PropagateEvent(evt, panel, target, false);
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

            evt.skipDisabledElements = false;
            evt.elementTarget = capturingElement;
            PropagateEvent(evt, panel, capturingElement, true);

            return true;
        }

        internal static void DispatchToPanelRoot(EventBase evt, [NotNull] BaseVisualElementPanel panel)
        {
            var target = evt.elementTarget = panel.visualTree;

            PropagateEvent(evt, panel, target, false);
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
                        Debug.Assert(evt.isPropagationStopped, "evt.isPropagationStopped");
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
