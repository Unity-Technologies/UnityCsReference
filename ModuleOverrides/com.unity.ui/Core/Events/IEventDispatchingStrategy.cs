// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

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

    interface IEventDispatchingStrategy
    {
        bool CanDispatchEvent(EventBase evt);
        void DispatchEvent(EventBase evt, IPanel panel);
    }

    static class EventDispatchUtilities
    {
        public static void PropagateEvent(EventBase evt)
        {
            // If there is no target or it's somehow not a VisualElement, we assume the event handling is empty work.
            if (!(evt.target is VisualElement ve))
                return;

            Debug.Assert(!evt.dispatch, "Event is being dispatched recursively.");
            evt.dispatch = true;

            if (!evt.bubblesOrTricklesDown)
            {
                // Early out if no callback on target.
                if (ve.HasEventCallbacksOrDefaultActionAtTarget(evt.eventCategory))
                {
                    ve.HandleEventAtTargetPhase(evt);
                }
            }
            else
            {
                // Early out if no callback on target or any of its parents.
                if (ve.HasParentEventCallbacksOrDefaultActionAtTarget(evt.eventCategory))
                {
                    HandleEventAcrossPropagationPath(evt);
                }
            }

            evt.dispatch = false;
        }

        private static void HandleEventAcrossPropagationPath(EventBase evt)
        {
            // Build and store propagation path
            var leafTarget = (VisualElement) evt.leafTarget;
            var path = PropagationPaths.Build(leafTarget, evt);
            evt.path = path;
            EventDebugger.LogPropagationPaths(evt, path);

            var panel = leafTarget.panel;

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
                    if (evt.Skip(element) || element.panel != panel)
                    {
                        continue;
                    }

                    evt.currentTarget = element;
                    evt.currentTarget.HandleEvent(evt);
                }
            }

            // Phase 2: Target / DefaultActionAtTarget
            // Propagate event from target parent up to root for the target phase

            // Call HandleEvent() even if propagation is stopped, for the default actions at target.
            evt.propagationPhase = PropagationPhase.AtTarget;
            foreach (var element in path.targetElements)
            {
                if (evt.Skip(element) || element.panel != panel)
                {
                    continue;
                }

                evt.target = element;
                evt.currentTarget = evt.target;
                evt.currentTarget.HandleEvent(evt);
            }

            // Call ExecuteDefaultActionAtTarget
            evt.propagationPhase = PropagationPhase.DefaultActionAtTarget;
            foreach (var element in path.targetElements)
            {
                if (evt.Skip(element) || element.panel != panel)
                {
                    continue;
                }

                evt.target = element;
                evt.currentTarget = evt.target;
                evt.currentTarget.HandleEvent(evt);
            }

            // Reset target to original target
            evt.target = evt.leafTarget;

            // Phase 3: bubble up phase
            // Propagate event from target parent up to root
            if (evt.bubbles)
            {
                evt.propagationPhase = PropagationPhase.BubbleUp;

                foreach (var element in path.bubbleUpPath)
                {
                    if (evt.Skip(element) || element.panel != panel)
                    {
                        continue;
                    }

                    evt.currentTarget = element;
                    evt.currentTarget.HandleEvent(evt);
                }
            }

            evt.propagationPhase = PropagationPhase.None;
            evt.currentTarget = null;
        }

        internal static void PropagateToIMGUIContainer(VisualElement root, EventBase evt)
        {
            //We don't support IMGUIContainers in player
            if (evt.imguiEvent == null || root.elementPanel.contextType == ContextType.Player)
            {
                return;
            }

            // Send the event to the first IMGUIContainer that can handle it.
            if (root.isIMGUIContainer)
            {
                var imContainer = root as IMGUIContainer;

                if (evt.Skip(imContainer))
                {
                    // IMGUIContainer have no children. We can return without iterating the children list.
                    return;
                }

                // Only permit switching the focus to another IMGUIContainer if the event target was not focusable.
                bool targetIsFocusable = (evt.target as Focusable)?.focusable ?? false;
                if (imContainer.SendEventToIMGUI(evt, !targetIsFocusable))
                {
                    evt.StopPropagation();
                    evt.PreventDefault();
                }

                if (evt.imguiEvent.rawType == EventType.Used)
                    Debug.Assert(evt.isPropagationStopped);
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

                        PropagateToIMGUIContainer(child, evt);
                        if (evt.isPropagationStopped)
                            break;
                    }
                }
            }
        }

        public static void ExecuteDefaultAction(EventBase evt)
        {
            if (evt.target is VisualElement ve && ve.HasDefaultAction(evt.eventCategory))
            {
                evt.dispatch = true;
                evt.currentTarget = evt.target;
                evt.propagationPhase = PropagationPhase.DefaultAction;

                evt.currentTarget.HandleEvent(evt);

                evt.propagationPhase = PropagationPhase.None;
                evt.currentTarget = null;
                evt.dispatch = false;
            }
        }
    }
}
