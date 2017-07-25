// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.Experimental.UIElements
{
    // value that determines if a event handler stops propagation of events or allows it to continue.
    public enum  EventPropagation
    {
        // continue event propagation after this handler
        Continue,
        // stop event propagation after this handler
        Stop
    }

    // determines in which event phase an event handler wants to handle events
    // the handler always gets called if it is the target VisualElement
    public enum PropagationPhase
    {
        // Not propagating at the moment.
        None,

        // Propagation from root of tree to immediate parent of target.
        Capture,

        // Event is at target.
        AtTarget,

        // After the target has gotten the chance to handle the event, the event walks back up the parent hierarchy back to root.
        BubbleUp,

        // At last, execute the default action(s).
        DefaultAction
    }

    // With the following VisualElement tree existing
    // root
    //  container A
    //      button B
    //      Textfield C with KeyboardFocus  <-- Event 2 Key Down A
    //  container D
    //      container E
    //          button F  <-- Event 1 Click
    //
    // For example: In the case of Event 1 Button F getting clicked, the following handlers will be called if registered:
    // result ==> Phase Capture [ root, D, E ], Phase Target [F], Phase BubbleUp [ E, D, root ]
    //
    // For example 2: A keydown with Textfocus in TextField C
    // result ==> Phase Capture [ root, A], Phase Target [C], Phase BubbleUp [ A, root ]

    public interface IDispatcher
    {
        IEventHandler capture { get; }

        // used when the capture is done receiving events
        void ReleaseCapture(IEventHandler handler);

        // removed a capture without it being done (will invoke OnLostCapture())
        void RemoveCapture();

        // use to set a capture. if any capture is set, it will be removed
        void TakeCapture(IEventHandler handler);
    }

    internal class EventDispatcher : IDispatcher
    {
        // 0. global capture
        public IEventHandler capture { get; set; }

        public void ReleaseCapture(IEventHandler handler)
        {
            Debug.Assert(handler == capture, "Element releasing capture does not have capture");
            capture = null;
        }

        public void RemoveCapture()
        {
            if (capture != null)
            {
                capture.OnLostCapture();
            }
            capture = null;
        }

        public void TakeCapture(IEventHandler handler)
        {
            if (capture == handler)
                return;
            if (GUIUtility.hotControl != 0)
            {
                Debug.Log("Should not be capturing when there is a hotcontrol");
                return;
            }
            // TODO: assign a reserved control id to hotControl so that repaint events in OnGUI() have their hotcontrol check behave normally
            RemoveCapture();
            capture = handler;
        }

        private HashSet<VisualElement> m_ElementsUnderMouse;
        private void ClearHoverOnElementsUnderMouse()
        {
            if (m_ElementsUnderMouse == null)
            {
                return;
            }

            foreach (var element in m_ElementsUnderMouse)
            {
                // if we set/unset the hover state on the PanelContainer, when the mouse exits
                // the panel window, all hover states are unset as expected but after the draw
                // of the panel so the hover states "look" still active until the panel redraws
                if (element.parent == null)
                    continue;

                // let element know
                element.pseudoStates = element.pseudoStates & ~PseudoStates.Hover;
                // TODO send mouse enter event
            }
            m_ElementsUnderMouse.Clear();
        }

        private void SetHoverOnElementsUnderMouse()
        {
            if (m_ElementsUnderMouse == null)
            {
                return;
            }

            foreach (var element in m_ElementsUnderMouse)
            {
                // if we set/unset the hover state on the PanelContainer, when the mouse exits
                // the panel window, all hover states are unset as expected but after the draw
                // of the panel so the hover states "look" still active until the panel redraws
                if (element.parent == null)
                    continue;

                // let element know
                element.pseudoStates = element.pseudoStates | PseudoStates.Hover;
                // TODO send mouse leave event
            }
        }

        public void DispatchEvent(EventBase evt, BaseVisualElementPanel panel)
        {
            Event e = evt.imguiEvent;

            if (e.type == EventType.Repaint)
            {
                Debug.Log("Repaint should be handled by Panel before Dispatcher");
                return;
            }

            bool invokedHandleEvent = false;

            if (panel.panelDebug != null && panel.panelDebug.enabled && panel.panelDebug.interceptEvents != null)
                if (panel.panelDebug.interceptEvents(e))
                {
                    evt.StopPropagation();
                    return;
                }

            if (capture != null && capture.panel == null)
            {
                Debug.Log(string.Format("Capture has no panel, forcing removal (capture={0} eventType={1})", capture, e.type));
                RemoveCapture();
            }

            if (capture != null)
            {
                if (capture.panel.contextType != panel.contextType)
                {
                    return;
                }

                invokedHandleEvent = true;

                evt.dispatch = true;
                evt.target = capture;
                evt.currentTarget = capture;
                evt.propagationPhase = PropagationPhase.AtTarget;
                capture.HandleEvent(evt);
                evt.propagationPhase = PropagationPhase.None;
                evt.currentTarget = null;
                evt.dispatch = false;

                if (evt.isPropagationStopped)
                {
                    return;
                }
            }

            if (e.isKey)
            {
                if (panel.focusedElement != null)
                {
                    invokedHandleEvent = true;
                    PropagateEvent(panel.focusedElement, evt);
                }
                else
                {
                    // Force call to SendEventToIMGUIContainers(), even if capture != null.
                    invokedHandleEvent = false;
                }

                // if the event was not handled than we want to check for focus move, ie: tabbing.
            }
            else if (e.isMouse
                     || e.isScrollWheel
                     || e.type == EventType.DragUpdated
                     || e.type == EventType.DragPerform
                     || e.type == EventType.DragExited)
            {
                VisualElement topElementUnderMouse;

                // TODO when EditorWindow is docked MouseLeaveWindow is not always sent
                // this is a problem in itself but it could leave some elements as "hover"
                if (e.type == EventType.MouseLeaveWindow)
                {
                    ClearHoverOnElementsUnderMouse();
                    topElementUnderMouse = null;
                }
                // update element under mouse and fire necessary events
                else
                {
                    if (m_ElementsUnderMouse == null)
                    {
                        m_ElementsUnderMouse = new HashSet<VisualElement>();
                    }

                    var picked = new List<VisualElement>();
                    topElementUnderMouse = panel.PickAll(e.mousePosition, picked);

                    if (!m_ElementsUnderMouse.SetEquals(picked))
                    {
                        ClearHoverOnElementsUnderMouse();
                        m_ElementsUnderMouse.UnionWith(picked);
                        SetHoverOnElementsUnderMouse();
                    }
                }

                if (e.type == EventType.MouseDown
                    && topElementUnderMouse != null
                    && topElementUnderMouse.enabled)
                {
                    SetFocusedElement(panel, topElementUnderMouse);
                }

                if (topElementUnderMouse != null)
                {
                    invokedHandleEvent = true;
                    PropagateEvent(topElementUnderMouse, evt);
                }
            }
            else if (e.type == EventType.ExecuteCommand || e.type == EventType.ValidateCommand)
            {
                if (panel.focusedElement != null)
                {
                    invokedHandleEvent = true;
                    PropagateEvent(panel.focusedElement, evt);
                }
            }

            // Fallback on IMGUI propagation if we don't recognize this event
            if (!evt.isPropagationStopped && (e.type == EventType.MouseEnterWindow || e.type == EventType.MouseLeaveWindow || e.type == EventType.Used || !invokedHandleEvent))
            {
                SendEventToIMGUIContainers(panel.visualTree, evt);
            }
            if (evt.target == null)
            {
                evt.target = panel.visualTree;
            }
            ExecuteDefaultAction(evt);
        }

        private void SendEventToIMGUIContainers(VisualElement root, EventBase evt)
        {
            // Send the event to the first IMGUIContainer that can handle it.

            var imContainer = root as IMGUIContainer;
            if (imContainer != null)
            {
                if (imContainer.HandleIMGUIEvent(evt.imguiEvent))
                {
                    evt.StopPropagation();
                    evt.PreventDefault();
                }
            }
            else
            {
                var container = root as VisualContainer;
                if (container != null)
                {
                    for (int i = 0; i < container.childrenCount; i++)
                    {
                        SendEventToIMGUIContainers(container.GetChildAt(i), evt);
                        if (evt.isPropagationStopped)
                            break;
                    }
                }
            }
        }

        private void PropagateEvent(VisualElement target, EventBase evt)
        {
            if (evt.dispatch)
            {
                // FIXME: signal this as an error
                return;
            }

            var path = BuildPropagationPath(target);

            evt.dispatch = true;
            evt.target = target;

            // Phase 1: Capture phase
            // Propagate event from root to target.parent
            evt.propagationPhase = PropagationPhase.Capture;
            for (int i = 0; i < path.Count; i++)
            {
                if (evt.isPropagationStopped)
                    break;

                var currentTarget = path[i];
                if (currentTarget.enabled)
                {
                    evt.currentTarget = currentTarget;
                    evt.currentTarget.HandleEvent(evt);
                }
            }

            // Phase 2: Target
            if (!evt.isPropagationStopped && target.enabled)
            {
                evt.propagationPhase = PropagationPhase.AtTarget;
                evt.currentTarget = target;
                evt.currentTarget.HandleEvent(evt);
            }

            // Phase 3: bubble Up phase
            // Propagate event from target parent up to root
            if (evt.bubbles)
            {
                evt.propagationPhase = PropagationPhase.BubbleUp;

                for (int i = path.Count - 1; i >= 0; i--)
                {
                    if (evt.isPropagationStopped)
                        break;

                    var currentTarget = path[i];
                    if (currentTarget.enabled)
                    {
                        evt.currentTarget = currentTarget;
                        evt.currentTarget.HandleEvent(evt);
                    }
                }
            }

            evt.dispatch = false;
            evt.propagationPhase = PropagationPhase.None;
            evt.currentTarget = null;
        }

        private static void ExecuteDefaultAction(EventBase evt)
        {
            if (!evt.isDefaultPrevented && evt.target != null)
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

        void SetFocusedElement(BaseVisualElementPanel panel, VisualElement element)
        {
            if (panel.focusedElement == element)
                return;

            if (panel.focusedElement != null)
            {
                // let element know
                panel.focusedElement.pseudoStates = panel.focusedElement.pseudoStates & ~PseudoStates.Focus;
                // TODO replace with focus lost event
                panel.focusedElement.OnLostKeyboardFocus();
            }

            panel.focusedElement = element;

            if (element != null)
            {
                // let element know
                element.pseudoStates = element.pseudoStates | PseudoStates.Focus;
                // TODO send focus gain event
            }
        }

        private static List<VisualElement> BuildPropagationPath(VisualElement elem)
        {
            var ret = new List<VisualElement>(16);
            if (elem == null)
                return ret;

            while (elem.parent != null)
            {
                ret.Add(elem.parent);
                elem = elem.parent;
            }

            ret.Reverse();

            return ret;
        }
    }
}
