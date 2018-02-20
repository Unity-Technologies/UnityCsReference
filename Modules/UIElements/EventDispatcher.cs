// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.Experimental.UIElements
{
    // value that determines if a event handler stops propagation of events or allows it to continue.
    // TODO: [Obsolete("Call EventBase.StopPropagation() instead of using EventPropagation.Stop.")]
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

    public interface IEventDispatcher
    {
        void DispatchEvent(EventBase evt, IPanel panel);
    }

    internal class EventDispatcher : IEventDispatcher
    {
        VisualElement m_TopElementUnderMouse;

        void DispatchMouseEnterMouseLeave(VisualElement previousTopElementUnderMouse, VisualElement currentTopElementUnderMouse, Event triggerEvent)
        {
            if (previousTopElementUnderMouse == currentTopElementUnderMouse)
            {
                return;
            }

            // We want to find the common ancestor CA of previousTopElementUnderMouse and currentTopElementUnderMouse,
            // send MouseLeave events to elements between CA and previousTopElementUnderMouse
            // and send MouseEnter events to elements between CA and currentTopElementUnderMouse.

            int prevDepth = 0;
            var p = previousTopElementUnderMouse;
            while (p != null)
            {
                prevDepth++;
                p = p.shadow.parent;
            }

            int currDepth = 0;
            var c = currentTopElementUnderMouse;
            while (c != null)
            {
                currDepth++;
                c = c.shadow.parent;
            }

            p = previousTopElementUnderMouse;
            c = currentTopElementUnderMouse;

            while (prevDepth > currDepth)
            {
                using (var leaveEvent = MouseLeaveEvent.GetPooled(triggerEvent))
                {
                    leaveEvent.target = p;
                    DispatchEvent(leaveEvent, p.panel);
                }

                prevDepth--;
                p = p.shadow.parent;
            }

            // We want to send enter events after all the leave events.
            // We will store the elements being entered in this list.
            List<VisualElement> enteringElements = new List<VisualElement>(currDepth);

            while (currDepth > prevDepth)
            {
                enteringElements.Add(c);

                currDepth--;
                c = c.shadow.parent;
            }

            // Now p and c are at the same depth. Go up the tree until p == c.
            while (p != c)
            {
                using (var leaveEvent = MouseLeaveEvent.GetPooled(triggerEvent))
                {
                    leaveEvent.target = p;
                    DispatchEvent(leaveEvent, p.panel);
                }

                enteringElements.Add(c);

                p = p.shadow.parent;
                c = c.shadow.parent;
            }

            for (var i = enteringElements.Count - 1; i >= 0; i--)
            {
                using (var enterEvent = MouseEnterEvent.GetPooled(triggerEvent))
                {
                    enterEvent.target = enteringElements[i];
                    DispatchEvent(enterEvent, enteringElements[i].panel);
                }
            }
        }

        void DispatchMouseOverMouseOut(VisualElement previousTopElementUnderMouse, VisualElement currentTopElementUnderMouse, Event triggerEvent)
        {
            if (previousTopElementUnderMouse == currentTopElementUnderMouse)
            {
                return;
            }

            // Send MouseOut event for element no longer under the mouse.
            if (previousTopElementUnderMouse != null)
            {
                using (var outEvent = MouseOutEvent.GetPooled(triggerEvent))
                {
                    outEvent.target = previousTopElementUnderMouse;
                    DispatchEvent(outEvent, previousTopElementUnderMouse.panel);
                }
            }

            // Send MouseOver event for element now under the mouse
            if (currentTopElementUnderMouse != null)
            {
                using (var overEvent = MouseOverEvent.GetPooled(triggerEvent))
                {
                    overEvent.target = currentTopElementUnderMouse;
                    DispatchEvent(overEvent, currentTopElementUnderMouse.panel);
                }
            }
        }

        public void DispatchEvent(EventBase evt, IPanel panel)
        {
            Event e = evt.imguiEvent;
            if (e != null && e.type == EventType.Repaint)
            {
                return;
            }

            if (panel != null && panel.panelDebug != null && panel.panelDebug.enabled && panel.panelDebug.interceptEvents != null)
                if (panel.panelDebug.interceptEvents(e))
                {
                    evt.StopPropagation();
                    return;
                }

            bool invokedHandleEvent = false;
            VisualElement captureVE = null;

            // Send all IMGUI events (for backward compatibility) and MouseEvents (because thats what we want to do in the new system)
            // to the capture, if there is one.
            if ((evt is IMouseEvent || e != null) && MouseCaptureController.mouseCapture != null)
            {
                captureVE = MouseCaptureController.mouseCapture as VisualElement;
                if (captureVE != null && captureVE.panel == null)
                {
                    Debug.Log(String.Format("Capture has no panel, forcing removal (capture={0} eventType={1})", MouseCaptureController.mouseCapture, e != null ? e.type.ToString() : "null"));
                    MouseCaptureController.ReleaseMouseCapture();
                    captureVE = null;
                }

                if (panel != null)
                {
                    if (captureVE != null && captureVE.panel.contextType != panel.contextType)
                    {
                        return;
                    }
                }

                invokedHandleEvent = true;
                evt.dispatch = true;

                if (MouseCaptureController.mouseCapture != null)
                {
                    evt.target = MouseCaptureController.mouseCapture;
                    evt.currentTarget = MouseCaptureController.mouseCapture;
                    evt.propagationPhase = PropagationPhase.AtTarget;
                    MouseCaptureController.mouseCapture.HandleEvent(evt);
                }
                evt.propagationPhase = PropagationPhase.None;
                evt.currentTarget = null;
                evt.dispatch = false;
            }

            if (evt.isPropagationStopped)
            {
                // Make sure to call default actions at target, but dont call it twice on mouse capture
                if (evt.target == null && panel != null)
                {
                    evt.target = panel.visualTree;
                }
                if (evt.target != null && evt.target != MouseCaptureController.mouseCapture)
                {
                    evt.dispatch = true;
                    evt.currentTarget = evt.target;
                    evt.propagationPhase = PropagationPhase.AtTarget;
                    evt.target.HandleEvent(evt);
                    evt.propagationPhase = PropagationPhase.None;
                    evt.currentTarget = null;
                    evt.dispatch = false;
                }
            }

            if (!evt.isPropagationStopped)
            {
                if (evt is IKeyboardEvent && panel != null)
                {
                    if (panel.focusController.focusedElement != null)
                    {
                        IMGUIContainer imguiContainer = panel.focusController.focusedElement as IMGUIContainer;

                        invokedHandleEvent = true;
                        if (imguiContainer != null)
                        {
                            if (imguiContainer.HandleIMGUIEvent(evt.imguiEvent))
                            {
                                evt.StopPropagation();
                                evt.PreventDefault();
                            }
                        }
                        else
                        {
                            evt.target = panel.focusController.focusedElement;
                            PropagateEvent(evt);
                        }
                    }
                    else
                    {
                        evt.target = panel.visualTree;
                        PropagateEvent(evt);

                        // Force call to PropagateToIMGUIContainer(), even if capture != null.
                        invokedHandleEvent = false;
                    }
                }
                else if (evt.GetEventTypeId() == MouseEnterEvent.TypeId() ||
                         evt.GetEventTypeId() == MouseLeaveEvent.TypeId())
                {
                    Debug.Assert(evt.target != null);
                    invokedHandleEvent = true;
                    PropagateEvent(evt);
                }
                else if (evt is IMouseEvent || (
                             e != null && (
                                 e.type == EventType.ContextClick ||
                                 e.type == EventType.DragUpdated ||
                                 e.type == EventType.DragPerform ||
                                 e.type == EventType.DragExited
                                 )
                             ))
                {
                    // FIXME: we should not change hover state when capture is true.
                    // However, when doing drag and drop, drop target should be highlighted.

                    // TODO when EditorWindow is docked MouseLeaveWindow is not always sent
                    // this is a problem in itself but it could leave some elements as "hover"
                    VisualElement currentTopElementUnderMouse = m_TopElementUnderMouse;

                    if (evt.GetEventTypeId() == MouseLeaveWindowEvent.TypeId())
                    {
                        m_TopElementUnderMouse = null;
                        DispatchMouseEnterMouseLeave(currentTopElementUnderMouse, m_TopElementUnderMouse, e);
                        DispatchMouseOverMouseOut(currentTopElementUnderMouse, m_TopElementUnderMouse, e);
                    }
                    // update element under mouse and fire necessary events
                    else if (evt is IMouseEvent || e != null)
                    {
                        if (evt.target == null && panel != null)
                        {
                            if (evt is IMouseEvent)
                            {
                                m_TopElementUnderMouse = panel.Pick((evt as IMouseEvent).localMousePosition);
                            }
                            else if (e != null)
                            {
                                m_TopElementUnderMouse = panel.Pick(e.mousePosition);
                            }

                            evt.target = m_TopElementUnderMouse;
                        }

                        if (evt.target != null)
                        {
                            invokedHandleEvent = true;
                            PropagateEvent(evt);
                        }

                        if (evt.GetEventTypeId() == MouseMoveEvent.TypeId() ||
                            evt.GetEventTypeId() == MouseEnterWindowEvent.TypeId() ||
                            evt.GetEventTypeId() == WheelEvent.TypeId() ||
                            (e != null && e.type == EventType.DragUpdated))
                        {
                            DispatchMouseEnterMouseLeave(currentTopElementUnderMouse, m_TopElementUnderMouse, e);
                            DispatchMouseOverMouseOut(currentTopElementUnderMouse, m_TopElementUnderMouse, e);
                        }
                    }
                }
                else if (panel != null && e != null && (e.type == EventType.ExecuteCommand || e.type == EventType.ValidateCommand))
                {
                    IMGUIContainer imguiContainer = panel.focusController.focusedElement as IMGUIContainer;

                    if (imguiContainer != null)
                    {
                        invokedHandleEvent = true;
                        if (imguiContainer.HandleIMGUIEvent(evt.imguiEvent))
                        {
                            evt.StopPropagation();
                            evt.PreventDefault();
                        }
                    }
                    else if (panel.focusController.focusedElement != null)
                    {
                        invokedHandleEvent = true;
                        evt.target = panel.focusController.focusedElement;
                        PropagateEvent(evt);
                    }
                    else
                    {
                        invokedHandleEvent = true;
                        PropagateToIMGUIContainer(panel.visualTree, evt, captureVE);
                    }
                }
                else if (evt is IPropagatableEvent ||
                         evt is IFocusEvent ||
                         evt is IChangeEvent ||
                         evt.GetEventTypeId() == InputEvent.TypeId() ||
                         evt.GetEventTypeId() == PostLayoutEvent.TypeId())
                {
                    Debug.Assert(evt.target != null);
                    invokedHandleEvent = true;
                    PropagateEvent(evt);
                }
            }

            if (!evt.isPropagationStopped && e != null && panel != null)
            {
                if (!invokedHandleEvent || e != null && (
                        e.type == EventType.MouseEnterWindow ||
                        e.type == EventType.MouseLeaveWindow ||
                        e.type == EventType.Used
                        ))
                {
                    PropagateToIMGUIContainer(panel.visualTree, evt, captureVE);
                }
            }

            if (evt.target == null && panel != null)
            {
                evt.target = panel.visualTree;
            }
            ExecuteDefaultAction(evt);
        }

        private static void PropagateToIMGUIContainer(VisualElement root, EventBase evt, VisualElement capture)
        {
            // Send the event to the first IMGUIContainer that can handle it.
            // If e.type != EventType.Used, avoid resending the event to the capture as it already had the chance to handle it.

            var imContainer = root as IMGUIContainer;
            if (imContainer != null && (evt.imguiEvent.type == EventType.Used || root != capture))
            {
                if (imContainer.HandleIMGUIEvent(evt.imguiEvent))
                {
                    evt.StopPropagation();
                    evt.PreventDefault();
                }
            }
            else
            {
                if (root != null)
                {
                    for (int i = 0; i < root.shadow.childCount; i++)
                    {
                        PropagateToIMGUIContainer(root.shadow[i], evt, capture);
                        if (evt.isPropagationStopped)
                            break;
                    }
                }
            }
        }

        private static void PropagateEvent(EventBase evt)
        {
            if (evt.dispatch)
            {
                // FIXME: signal this as an error
                return;
            }

            PropagationPaths.Type pathTypesRequested = (evt.capturable ? PropagationPaths.Type.Capture : PropagationPaths.Type.None);
            pathTypesRequested |= (evt.bubbles ? PropagationPaths.Type.BubbleUp : PropagationPaths.Type.None);

            using (var paths = BuildPropagationPath(evt.target as VisualElement, pathTypesRequested))
            {
                evt.dispatch = true;

                if (evt.capturable && paths != null && paths.capturePath.Count > 0)
                {
                    // Phase 1: Capture phase
                    // Propagate event from root to target.parent
                    evt.propagationPhase = PropagationPhase.Capture;

                    for (int i = paths.capturePath.Count - 1; i >= 0; i--)
                    {
                        if (evt.isPropagationStopped)
                            break;

                        evt.currentTarget = paths.capturePath[i];
                        evt.currentTarget.HandleEvent(evt);
                    }
                }

                // Phase 2: Target
                // Call HandleEvent() even if propagation is stopped, for the default actions at target.
                evt.propagationPhase = PropagationPhase.AtTarget;
                evt.currentTarget = evt.target;
                evt.currentTarget.HandleEvent(evt);

                // Phase 3: bubble Up phase
                // Propagate event from target parent up to root
                if (evt.bubbles && paths != null && paths.bubblePath.Count > 0)
                {
                    evt.propagationPhase = PropagationPhase.BubbleUp;

                    foreach (VisualElement ve in paths.bubblePath)
                    {
                        if (evt.isPropagationStopped)
                            break;

                        evt.currentTarget = ve;
                        evt.currentTarget.HandleEvent(evt);
                    }
                }

                evt.dispatch = false;
                evt.propagationPhase = PropagationPhase.None;
                evt.currentTarget = null;
            }
        }

        private static void ExecuteDefaultAction(EventBase evt)
        {
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

        private const int k_DefaultPropagationDepth = 16;

        class PropagationPaths : IDisposable
        {
            [Flags]
            public enum Type
            {
                None = 0,
                Capture = 1,
                BubbleUp = 2
            }

            public readonly List<VisualElement> capturePath;
            public readonly List<VisualElement> bubblePath;

            public PropagationPaths(int initialSize)
            {
                capturePath = new List<VisualElement>(initialSize);
                bubblePath = new List<VisualElement>(initialSize);
            }

            public void Dispose()
            {
                PropagationPathsPool.Release(this);
            }

            public void Clear()
            {
                bubblePath.Clear();
                capturePath.Clear();
            }
        }

        private static class PropagationPathsPool
        {
            private static readonly List<PropagationPaths> s_Available = new List<PropagationPaths>();

            public static PropagationPaths Acquire()
            {
                if (s_Available.Count != 0)
                {
                    PropagationPaths po = s_Available[0];
                    s_Available.RemoveAt(0);
                    return po;
                }
                else
                {
                    PropagationPaths po = new PropagationPaths(k_DefaultPropagationDepth);
                    return po;
                }
            }

            public static void Release(PropagationPaths po)
            {
                po.Clear();
                s_Available.Add(po);
            }
        }

        private static PropagationPaths BuildPropagationPath(VisualElement elem, PropagationPaths.Type pathTypesRequested)
        {
            if (elem == null || pathTypesRequested == PropagationPaths.Type.None)
                return null;
            var ret = PropagationPathsPool.Acquire();
            while (elem.shadow.parent != null)
            {
                if (elem.shadow.parent.enabledInHierarchy)
                {
                    if ((pathTypesRequested & PropagationPaths.Type.Capture) == PropagationPaths.Type.Capture && elem.shadow.parent.HasCaptureHandlers())
                        ret.capturePath.Add(elem.shadow.parent);
                    if ((pathTypesRequested & PropagationPaths.Type.BubbleUp) == PropagationPaths.Type.BubbleUp && elem.shadow.parent.HasBubbleHandlers())
                        ret.bubblePath.Add(elem.shadow.parent);
                }
                elem = elem.shadow.parent;
            }
            return ret;
        }
    }
}
