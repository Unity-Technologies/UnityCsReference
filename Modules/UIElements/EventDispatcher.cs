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
        TrickleDown,

        [Obsolete("Use TrickleDown instead of Capture.")]
        Capture = TrickleDown,

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
    // result ==> Phase TrickleDown [ root, D, E ], Phase Target [F], Phase BubbleUp [ E, D, root ]
    //
    // For example 2: A keydown with Textfocus in TextField C
    // result ==> Phase TrickleDown [ root, A], Phase Target [C], Phase BubbleUp [ A, root ]

    enum DispatchMode
    {
        Default = Queued,
        Queued = 1,
        Immediate = 2,
    }

    public sealed class EventDispatcher
    {
        public struct Gate : IDisposable
        {
            EventDispatcher m_Dispatcher;

            public Gate(EventDispatcher d)
            {
                m_Dispatcher = d;
                m_Dispatcher.CloseGate();
            }

            public void Dispose()
            {
                m_Dispatcher.OpenGate();
            }
        }

        struct EventRecord
        {
            public EventBase m_Event;
            public IPanel m_Panel;
        }

        static readonly ObjectPool<Queue<EventRecord>> k_EventQueuePool = new ObjectPool<Queue<EventRecord>>();
        Queue<EventRecord> m_Queue;

        uint m_GateCount;

        struct DispatchContext
        {
            public uint m_GateCount;
            public Queue<EventRecord> m_Queue;
        }

        Stack<DispatchContext> m_DispatchContexts = new Stack<DispatchContext>();

        static EventDispatcher s_EventDispatcher;

        internal static EventDispatcher instance
        {
            get
            {
                if (s_EventDispatcher == null)
                    s_EventDispatcher = new EventDispatcher();

                return s_EventDispatcher;
            }
        }

        internal static void ClearDispatcher()
        {
            s_EventDispatcher = null;
        }

        EventDispatcher()
        {
            m_Queue = k_EventQueuePool.Get();
        }

        bool dispatchImmediately
        {
            get { return m_GateCount == 0; }
        }

        IPanel m_LastMousePositionPanel;
        Vector2 m_LastMousePosition;

        void DispatchEnterLeave(VisualElement previousTopElementUnderMouse, VisualElement currentTopElementUnderMouse, Func<EventBase> getEnterEventFunc, Func<EventBase> getLeaveEventFunc)
        {
            if (previousTopElementUnderMouse == currentTopElementUnderMouse)
            {
                return;
            }

            if (previousTopElementUnderMouse != null && previousTopElementUnderMouse.panel == null)
            {
                // If previousTopElementUnderMouse has been removed from panel,
                // do as if there is no element under the mouse.
                previousTopElementUnderMouse = null;
            }

            // We want to find the common ancestor CA of previousTopElementUnderMouse and currentTopElementUnderMouse,
            // send Leave (MouseLeave or DragLeave) events to elements between CA and previousTopElementUnderMouse
            // and send Enter (MouseEnter or DragEnter) events to elements between CA and currentTopElementUnderMouse.

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
                using (var leaveEvent = getLeaveEventFunc())
                {
                    leaveEvent.target = p;
                    p.SendEvent(leaveEvent);
                }

                prevDepth--;
                p = p.shadow.parent;
            }

            // We want to send enter events after all the leave events.
            // We will store the elements being entered in this list.
            List<VisualElement> enteringElements = VisualElementListPool.Get(currDepth);

            while (currDepth > prevDepth)
            {
                enteringElements.Add(c);

                currDepth--;
                c = c.shadow.parent;
            }

            // Now p and c are at the same depth. Go up the tree until p == c.
            while (p != c)
            {
                using (var leaveEvent = getLeaveEventFunc())
                {
                    leaveEvent.target = p;
                    p.SendEvent(leaveEvent);
                }

                enteringElements.Add(c);

                p = p.shadow.parent;
                c = c.shadow.parent;
            }

            for (var i = enteringElements.Count - 1; i >= 0; i--)
            {
                using (var enterEvent = getEnterEventFunc())
                {
                    enterEvent.target = enteringElements[i];
                    enteringElements[i].SendEvent(enterEvent);
                }
            }
            VisualElementListPool.Release(enteringElements);
        }

        void DispatchDragEnterDragLeave(VisualElement previousTopElementUnderMouse, VisualElement currentTopElementUnderMouse, IMouseEvent triggerEvent)
        {
            if (triggerEvent != null)
            {
                DispatchEnterLeave(previousTopElementUnderMouse, currentTopElementUnderMouse, () => DragEnterEvent.GetPooled(triggerEvent), () => DragLeaveEvent.GetPooled(triggerEvent));
            }
            else
            {
                DispatchEnterLeave(previousTopElementUnderMouse, currentTopElementUnderMouse, () => DragEnterEvent.GetPooled(m_LastMousePosition), () => DragLeaveEvent.GetPooled(m_LastMousePosition));
            }
        }

        void DispatchMouseEnterMouseLeave(VisualElement previousTopElementUnderMouse, VisualElement currentTopElementUnderMouse, IMouseEvent triggerEvent)
        {
            if (triggerEvent != null)
            {
                DispatchEnterLeave(previousTopElementUnderMouse, currentTopElementUnderMouse, () => MouseEnterEvent.GetPooled(triggerEvent), () => MouseLeaveEvent.GetPooled(triggerEvent));
            }
            else
            {
                DispatchEnterLeave(previousTopElementUnderMouse, currentTopElementUnderMouse, () => MouseEnterEvent.GetPooled(m_LastMousePosition), () => MouseLeaveEvent.GetPooled(m_LastMousePosition));
            }
        }

        void DispatchMouseOverMouseOut(VisualElement previousTopElementUnderMouse, VisualElement currentTopElementUnderMouse, IMouseEvent triggerEvent)
        {
            if (previousTopElementUnderMouse == currentTopElementUnderMouse)
            {
                return;
            }

            // Send MouseOut event for element no longer under the mouse.
            if (previousTopElementUnderMouse != null && previousTopElementUnderMouse.panel != null)
            {
                using (var outEvent = (triggerEvent == null) ? MouseOutEvent.GetPooled(m_LastMousePosition) : MouseOutEvent.GetPooled(triggerEvent))
                {
                    outEvent.target = previousTopElementUnderMouse;
                    previousTopElementUnderMouse.SendEvent(outEvent);
                }
            }

            // Send MouseOver event for element now under the mouse
            if (currentTopElementUnderMouse != null)
            {
                using (var overEvent = (triggerEvent == null) ? MouseOverEvent.GetPooled(m_LastMousePosition) : MouseOverEvent.GetPooled(triggerEvent))
                {
                    overEvent.target = currentTopElementUnderMouse;
                    currentTopElementUnderMouse.SendEvent(overEvent);
                }
            }
        }

        void DispatchEnterLeaveEvents(VisualElement previousTopElementUnderMouse, VisualElement currentTopElementUnderMouse, EventBase triggerEvent)
        {
            IMouseEvent mouseEvent = triggerEvent as IMouseEvent;

            if (mouseEvent == null)
            {
                return;
            }

            if (triggerEvent.GetEventTypeId() == MouseMoveEvent.TypeId() ||
                triggerEvent.GetEventTypeId() == MouseDownEvent.TypeId() ||
                triggerEvent.GetEventTypeId() == MouseUpEvent.TypeId() ||
                triggerEvent.GetEventTypeId() == MouseEnterWindowEvent.TypeId() ||
                triggerEvent.GetEventTypeId() == WheelEvent.TypeId())
            {
                DispatchMouseEnterMouseLeave(previousTopElementUnderMouse, currentTopElementUnderMouse, mouseEvent);
                DispatchMouseOverMouseOut(previousTopElementUnderMouse, currentTopElementUnderMouse, mouseEvent);
            }
            else if (triggerEvent.GetEventTypeId() == DragUpdatedEvent.TypeId())
            {
                DispatchDragEnterDragLeave(previousTopElementUnderMouse, currentTopElementUnderMouse, mouseEvent);
            }
        }

        internal void Dispatch(EventBase evt, IPanel panel, DispatchMode dispatchMode)
        {
            evt.MarkReceivedByDispatcher();

            if (evt.GetEventTypeId() == IMGUIEvent.TypeId())
            {
                Event e = evt.imguiEvent;
                if (e.type == EventType.Repaint)
                {
                    return;
                }
            }

            if (dispatchImmediately || (dispatchMode == DispatchMode.Immediate))
            {
                ProcessEvent(evt, panel);
            }
            else
            {
                evt.Acquire();
                m_Queue.Enqueue(new EventRecord {m_Event = evt, m_Panel = panel});
            }
        }

        internal void PushDispatcherContext()
        {
            m_DispatchContexts.Push(new DispatchContext() {m_GateCount = m_GateCount, m_Queue = m_Queue});
            m_GateCount = 0;
            m_Queue = k_EventQueuePool.Get();
        }

        internal void PopDispatcherContext()
        {
            Debug.Assert(m_GateCount == 0, "All gates should have been opened before popping dispatch context.");
            Debug.Assert(m_Queue.Count == 0, "Queue should be empty when popping dispatch context.");

            k_EventQueuePool.Release(m_Queue);

            m_GateCount = m_DispatchContexts.Peek().m_GateCount;
            m_Queue = m_DispatchContexts.Peek().m_Queue;
            m_DispatchContexts.Pop();
        }

        internal void CloseGate()
        {
            m_GateCount++;
        }

        internal void OpenGate()
        {
            Debug.Assert(m_GateCount > 0);

            if (m_GateCount > 0)
            {
                m_GateCount--;
            }

            if (m_GateCount == 0)
            {
                ProcessEventQueue();
            }
        }

        void ProcessEventQueue()
        {
            // While processing the current queue, we need a new queue to store additional events that
            // might be generated during current queue events processing. Thanks to the gate mechanism,
            // events put in the new queue will be processed before the remaining events in the current
            // queue (but after processing of the event generating them is completed).
            //
            // For example, MouseDownEvent generates FocusOut, FocusIn, Blur and Focus events. And let's
            // say that FocusIn generates ValueChanged and GeometryChanged events.
            //
            // Without queue swapping, order of event processing would be MouseDown, FocusOut, FocusIn,
            // Blur, Focus, ValueChanged, GeometryChanged. It is not the same as order of event emission.
            //
            // With queue swapping, order is MouseDown, FocusOut, FocusIn, ValueChanged, GeometryChanged,
            // Blur, Focus. This preserve the order of event emission, and each event is completely
            // processed before processing the next event.

            Queue<EventRecord> queueToProcess = m_Queue;
            m_Queue = k_EventQueuePool.Get();

            ExitGUIException caughtExitGUIException = null;

            try
            {
                while (queueToProcess.Count > 0)
                {
                    EventRecord eventRecord = queueToProcess.Dequeue();
                    EventBase evt = eventRecord.m_Event;
                    IPanel panel = eventRecord.m_Panel;
                    try
                    {
                        ProcessEvent(evt, panel);
                    }
                    catch (ExitGUIException e)
                    {
                        Debug.Assert(caughtExitGUIException == null);
                        caughtExitGUIException = e;
                    }
                    finally
                    {
                        evt.Dispose();
                    }
                }
            }
            finally
            {
                k_EventQueuePool.Release(queueToProcess);
            }

            if (caughtExitGUIException != null)
            {
                throw caughtExitGUIException;
            }
        }

        void ProcessEvent(EventBase evt, IPanel panel)
        {
            using (new Gate(this))
            {
                evt.PreDispatch();

                var panelDebug = (panel as BaseVisualElementPanel)?.panelDebug;
                if (panelDebug != null && panelDebug.showOverlay)
                {
                    if (panelDebug.InterceptEvents(evt.imguiEvent))
                    {
                        evt.StopPropagation();
                        evt.PostDispatch();
                        return;
                    }
                }

                IMouseEvent mouseEvent = evt as IMouseEvent;
                IMouseEventInternal mouseEventInternal = evt as IMouseEventInternal;
                if (mouseEvent != null && mouseEventInternal != null && mouseEventInternal.hasUnderlyingPhysicalEvent)
                {
                    m_LastMousePositionPanel = panel;
                    m_LastMousePosition = mouseEvent.mousePosition;
                }

                bool eventHandled = false;

                // Release mouse capture if capture element is not in a panel.
                VisualElement captureVE = MouseCaptureController.mouseCapture as VisualElement;
                if (evt.GetEventTypeId() != MouseCaptureOutEvent.TypeId() && captureVE != null && captureVE.panel == null)
                {
                    Event e = evt.imguiEvent;
                    Debug.Log(String.Format("Capture has no panel, forcing removal (capture={0} eventType={1})", MouseCaptureController.mouseCapture, e != null ? e.type.ToString() : "null"));
                    MouseCaptureController.ReleaseMouse();
                }

                // Send all IMGUI events (for backward compatibility) and MouseEvents with null target (because thats what we want to do in the new system)
                // to the capture, if there is one. Note that events coming from IMGUI have their target set to null.
                bool sendEventToMouseCapture = false;
                bool mouseEventWasCaptured = false;
                if (MouseCaptureController.mouseCapture != null)
                {
                    if (evt.imguiEvent != null && evt.target == null)
                    {
                        // Non exclusive processing by capturing element.
                        sendEventToMouseCapture = true;
                        mouseEventWasCaptured = false;
                    }

                    if (mouseEvent != null && (evt.target == null || evt.target == MouseCaptureController.mouseCapture))
                    {
                        // Exclusive processing by capturing element.
                        sendEventToMouseCapture = true;
                        mouseEventWasCaptured = true;
                    }

                    if (panel != null)
                    {
                        if (captureVE != null && captureVE.panel.contextType != panel.contextType)
                        {
                            // Capturing element is not in the right context. Ignore it.
                            sendEventToMouseCapture = false;
                            mouseEventWasCaptured = false;
                        }
                    }

                    if (evt.GetEventTypeId() == WheelEvent.TypeId())
                    {
                        sendEventToMouseCapture = false;
                        mouseEventWasCaptured = false;
                    }
                }

                evt.skipElement = null;

                if (sendEventToMouseCapture)
                {
                    BaseVisualElementPanel basePanel = panel as BaseVisualElementPanel;

                    if (mouseEvent != null && basePanel != null)
                    {
                        VisualElement currentTopElementUnderMouse = basePanel.topElementUnderMouse;

                        if (evt.target == null)
                        {
                            basePanel.topElementUnderMouse = basePanel.Pick(mouseEvent.mousePosition);
                        }

                        DispatchEnterLeaveEvents(currentTopElementUnderMouse, basePanel.topElementUnderMouse, evt);
                    }

                    IEventHandler originalCaptureElement = MouseCaptureController.mouseCapture;

                    eventHandled = true;

                    evt.dispatch = true;
                    evt.target = MouseCaptureController.mouseCapture;
                    evt.currentTarget = MouseCaptureController.mouseCapture;
                    evt.propagationPhase = PropagationPhase.AtTarget;
                    MouseCaptureController.mouseCapture.HandleEvent(evt);

                    // Do further processing with a target computed the usual way.
                    // However, if mouseEventWasCaptured, the only thing remaining to do is ExecuteDefaultAction,
                    // which whould be done with mouseCapture as the target.
                    if (!mouseEventWasCaptured)
                    {
                        evt.target = null;
                    }

                    evt.currentTarget = null;
                    evt.propagationPhase = PropagationPhase.None;
                    evt.dispatch = false;

                    // Do not call HandleEvent again for this element.
                    evt.skipElement = originalCaptureElement;
                }

                if (!mouseEventWasCaptured && !evt.isPropagationStopped)
                {
                    if (evt is IKeyboardEvent && panel != null)
                    {
                        eventHandled = true;
                        if (panel.focusController.focusedElement != null)
                        {
                            IMGUIContainer imguiContainer = panel.focusController.focusedElement as IMGUIContainer;

                            if (imguiContainer != null)
                            {
                                // THINK ABOUT THIS PF: shoudln't we allow for the TrickleDown dispatch phase?
                                if (imguiContainer != evt.skipElement && imguiContainer.HandleIMGUIEvent(evt.imguiEvent))
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

                            if (!evt.isPropagationStopped)
                                PropagateToIMGUIContainer(panel.visualTree, evt);
                        }
                    }
                    else if (mouseEvent != null)
                    {
                        // FIXME: we should not change hover state when capture is true.
                        // However, when doing drag and drop, drop target should be highlighted.

                        // TODO when EditorWindow is docked MouseLeaveWindow is not always sent
                        // this is a problem in itself but it could leave some elements as "hover"

                        BaseVisualElementPanel basePanel = panel as BaseVisualElementPanel;

                        if (basePanel != null && evt.GetEventTypeId() == MouseLeaveWindowEvent.TypeId())
                        {
                            VisualElement currentTopElementUnderMouse = basePanel.topElementUnderMouse;
                            basePanel.topElementUnderMouse = null;
                            DispatchMouseEnterMouseLeave(currentTopElementUnderMouse, basePanel.topElementUnderMouse, mouseEvent);
                            DispatchMouseOverMouseOut(currentTopElementUnderMouse, basePanel.topElementUnderMouse, mouseEvent);
                        }
                        else if (basePanel != null && evt.GetEventTypeId() == DragExitedEvent.TypeId())
                        {
                            VisualElement currentTopElementUnderMouse = basePanel.topElementUnderMouse;
                            basePanel.topElementUnderMouse = null;
                            DispatchDragEnterDragLeave(currentTopElementUnderMouse, basePanel.topElementUnderMouse, mouseEvent);
                        }
                        // update element under mouse and fire necessary events
                        else
                        {
                            VisualElement currentTopElementUnderMouse = null;
                            if (evt.target == null && basePanel != null)
                            {
                                currentTopElementUnderMouse = basePanel.topElementUnderMouse;
                                basePanel.topElementUnderMouse = panel.Pick(mouseEvent.mousePosition);
                                evt.target = basePanel.topElementUnderMouse;
                            }

                            if (evt.target != null)
                            {
                                eventHandled = true;
                                PropagateEvent(evt);
                            }

                            if (basePanel != null)
                            {
                                DispatchEnterLeaveEvents(currentTopElementUnderMouse, basePanel.topElementUnderMouse, evt);
                            }
                        }
                    }
                    else if (panel != null && evt is ICommandEvent)
                    {
                        IMGUIContainer imguiContainer = panel.focusController.focusedElement as IMGUIContainer;

                        eventHandled = true;
                        if (imguiContainer != null)
                        {
                            if (imguiContainer != evt.skipElement && imguiContainer.HandleIMGUIEvent(evt.imguiEvent))
                            {
                                evt.StopPropagation();
                                evt.PreventDefault();
                            }
                        }
                        else if (panel.focusController.focusedElement != null)
                        {
                            evt.target = panel.focusController.focusedElement;
                            PropagateEvent(evt);
                        }
                        else
                        {
                            PropagateToIMGUIContainer(panel.visualTree, evt);
                        }
                    }
                    else if (evt is IPropagatableEvent ||
                             evt is IFocusEvent ||
                             evt is IChangeEvent ||
                             evt.GetEventTypeId() == InputEvent.TypeId() ||
                             evt.GetEventTypeId() == GeometryChangedEvent.TypeId())
                    {
                        Debug.Assert(evt.target != null);
                        eventHandled = true;
                        PropagateEvent(evt);
                    }
                }

                if (!mouseEventWasCaptured && !evt.isPropagationStopped && panel != null)
                {
                    Event e = evt.imguiEvent;
                    if (!eventHandled || (e != null && e.type == EventType.Used) ||
                        evt.GetEventTypeId() == MouseEnterWindowEvent.TypeId() ||
                        evt.GetEventTypeId() == MouseLeaveWindowEvent.TypeId())
                    {
                        PropagateToIMGUIContainer(panel.visualTree, evt);
                    }
                }

                if (evt.target == null && panel != null)
                {
                    evt.target = panel.visualTree;
                }

                ExecuteDefaultAction(evt);

                evt.PostDispatch();
            }
        }

        internal void UpdateElementUnderMouse(BaseVisualElementPanel panel)
        {
            if (panel != m_LastMousePositionPanel)
            {
                return;
            }

            Vector2 localMousePosition = panel.visualTree.WorldToLocal(m_LastMousePosition);
            VisualElement currentTopElementUnderMouse = panel.topElementUnderMouse;
            panel.topElementUnderMouse = panel.Pick(localMousePosition);
            DispatchMouseEnterMouseLeave(currentTopElementUnderMouse, panel.topElementUnderMouse, null);
            DispatchMouseOverMouseOut(currentTopElementUnderMouse, panel.topElementUnderMouse, null);
        }

        private static void PropagateToIMGUIContainer(VisualElement root, EventBase evt)
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
            }
            else
            {
                if (root != null)
                {
                    for (int i = 0; i < root.shadow.childCount; i++)
                    {
                        PropagateToIMGUIContainer(root.shadow[i], evt);
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

            PropagationPaths.Type pathTypesRequested = (evt.tricklesDown ? PropagationPaths.Type.TrickleDown : PropagationPaths.Type.None);
            pathTypesRequested |= (evt.bubbles ? PropagationPaths.Type.BubbleUp : PropagationPaths.Type.None);

            using (var paths = BuildPropagationPath(evt.target as VisualElement, pathTypesRequested))
            {
                evt.dispatch = true;

                if (evt.tricklesDown && paths != null && paths.trickleDownPath.Count > 0)
                {
                    // Phase 1: TrickleDown phase
                    // Propagate event from root to target.parent
                    evt.propagationPhase = PropagationPhase.TrickleDown;

                    for (int i = paths.trickleDownPath.Count - 1; i >= 0; i--)
                    {
                        if (evt.isPropagationStopped)
                            break;

                        if (paths.trickleDownPath[i] == evt.skipElement)
                        {
                            continue;
                        }

                        evt.currentTarget = paths.trickleDownPath[i];
                        evt.currentTarget.HandleEvent(evt);
                    }
                }

                // Phase 2: Target
                // Call HandleEvent() even if propagation is stopped, for the default actions at target.
                if (evt.target != evt.skipElement)
                {
                    evt.propagationPhase = PropagationPhase.AtTarget;
                    evt.currentTarget = evt.target;
                    evt.currentTarget.HandleEvent(evt);
                }

                // Phase 3: bubble Up phase
                // Propagate event from target parent up to root
                if (evt.bubbles && paths != null && paths.bubblePath.Count > 0)
                {
                    evt.propagationPhase = PropagationPhase.BubbleUp;

                    foreach (VisualElement ve in paths.bubblePath)
                    {
                        if (evt.isPropagationStopped)
                            break;

                        if (ve == evt.skipElement)
                        {
                            continue;
                        }

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
                TrickleDown = 1,

                [Obsolete("Use TrickleDown instead of Capture.")]
                Capture = TrickleDown,

                BubbleUp = 2
            }

            [Obsolete("Use trickleDownPath instead of capturePath.")]
            public List<VisualElement> capturePath { get { return trickleDownPath; } }
            public readonly List<VisualElement> trickleDownPath;
            public readonly List<VisualElement> bubblePath;

            public PropagationPaths(int initialSize)
            {
                trickleDownPath = new List<VisualElement>(initialSize);
                bubblePath = new List<VisualElement>(initialSize);
            }

            public void Dispose()
            {
                PropagationPathsPool.Release(this);
            }

            public void Clear()
            {
                bubblePath.Clear();
                trickleDownPath.Clear();
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
                    if ((pathTypesRequested & PropagationPaths.Type.TrickleDown) == PropagationPaths.Type.TrickleDown && elem.shadow.parent.HasTrickleDownHandlers())
                        ret.trickleDownPath.Add(elem.shadow.parent);
                    if ((pathTypesRequested & PropagationPaths.Type.BubbleUp) == PropagationPaths.Type.BubbleUp && elem.shadow.parent.HasBubbleUpHandlers())
                        ret.bubblePath.Add(elem.shadow.parent);
                }
                elem = elem.shadow.parent;
            }
            return ret;
        }
    }
}
