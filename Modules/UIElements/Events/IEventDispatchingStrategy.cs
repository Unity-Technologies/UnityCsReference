// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.UIElements
{
    // determines in which event phase an event handler wants to handle events
    // the handler always gets called if it is the target VisualElement
    public enum PropagationPhase
    {
        // Not propagating at the moment.
        None,

        // Propagation from root of tree to immediate parent of target.
        TrickleDown,

        // Event is at target.
        AtTarget,

        // After the target has gotten the chance to handle the event, the event walks back up the parent hierarchy back to root.
        BubbleUp,

        // At last, execute the default action(s).
        DefaultAction
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
            Debug.Assert(!evt.dispatch, "Event is being dispatched recursively.");

            evt.dispatch = true;

            if (evt.tricklesDown && evt.path != null && evt.path.trickleDownPath.Count > 0)
            {
                // Phase 1: TrickleDown phase
                // Propagate event from root to target.parent
                evt.propagationPhase = PropagationPhase.TrickleDown;

                for (int i = evt.path.trickleDownPath.Count - 1; i >= 0; i--)
                {
                    if (evt.isPropagationStopped)
                        break;

                    if (evt.path.trickleDownPath[i] == evt.skipElement)
                    {
                        continue;
                    }

                    evt.currentTarget = evt.path.trickleDownPath[i];
                    evt.currentTarget.HandleEvent(evt);
                }
            }

            // Phase 2: Target
            // Call HandleEvent() even if propagation is stopped, for the default actions at target.
            if (evt.target != null && evt.target != evt.skipElement)
            {
                evt.propagationPhase = PropagationPhase.AtTarget;
                evt.currentTarget = evt.target;
                evt.currentTarget.HandleEvent(evt);
            }

            // Phase 2+3: target/bubble up phase
            // Propagate event from target parent up to root
            if (evt.path != null && evt.path.targetAndBubblePath.Count > 0)
            {
                foreach (var element in evt.path.targetAndBubblePath)
                {
                    if (element.m_VisualElement == evt.skipElement)
                    {
                        continue;
                    }

                    if (element.m_IsTarget)
                    {
                        evt.propagationPhase = PropagationPhase.AtTarget;
                        evt.target = element.m_VisualElement;
                        evt.currentTarget = evt.target;
                        evt.currentTarget.HandleEvent(evt);
                    }
                    else if (evt.bubbles)
                    {
                        evt.propagationPhase = PropagationPhase.BubbleUp;
                        evt.currentTarget = element.m_VisualElement;
                        evt.currentTarget.HandleEvent(evt);
                    }
                }

                evt.target = evt.leafTarget;
            }

            evt.dispatch = false;
            evt.propagationPhase = PropagationPhase.None;
            evt.currentTarget = null;
        }

        internal static void PropagateToIMGUIContainer(VisualElement root, EventBase evt)
        {
            if (evt.imguiEvent == null)
            {
                return;
            }

            // Send the event to the first IMGUIContainer that can handle it.
            // If e.type != EventType.Used, avoid resending the event to the capture as it already had the chance to handle it.

            var imContainer = root as IMGUIContainer;
            if (imContainer != null && (evt.imguiEvent.type == EventType.Used || root != evt.skipElement))
            {
                if (imContainer.HandleIMGUIEvent(evt.imguiEvent))
                {
                    evt.StopPropagation();
                    evt.PreventDefault();
                }

                if (evt.imguiEvent.type == EventType.Used)
                    Debug.Assert(evt.isPropagationStopped);
            }
            else
            {
                if (root != null)
                {
                    for (int i = 0; i < root.hierarchy.childCount; i++)
                    {
                        PropagateToIMGUIContainer(root.hierarchy[i], evt);
                        if (evt.isPropagationStopped)
                            break;
                    }
                }
            }
        }

        public static void ExecuteDefaultAction(EventBase evt, IPanel panel)
        {
            if (evt.target == null && panel != null)
            {
                evt.target = panel.visualTree;
            }

            if (evt.target != null)
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
