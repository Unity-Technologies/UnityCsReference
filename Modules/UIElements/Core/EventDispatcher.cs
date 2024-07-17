// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine.Bindings;
using NotNullAttribute = JetBrains.Annotations.NotNullAttribute;

namespace UnityEngine.UIElements
{
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
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    enum DispatchMode
    {
        Default = Queued,
        Queued = 1,
        Immediate = 2,
    }

    /// <summary>
    /// Gates control when the dispatcher processes events.
    /// </summary>
    /// <remarks>
    /// Here is an example of using a gate:
    /// </remarks>
    /// <remarks>
    /// When the gate is instantiated, it closes automatically, causing the dispatcher to store the events it receives in a queue. At the end of the <c>using</c> block, <see cref="Dispose"/> is called, which triggers opening the gate. When all gates on a dispatcher open, the events stored in the queue are processed. Closing a gate while the event queue is processed does not stop it from being processed. Instead, a new queue is created to store new events.
    /// </remarks>
    /// <example>
    /// Here is an example of using a gate:
    /// <code>
    /// public class MyElement : VisualElement
    /// {
    ///     void Foo()
    ///     {
    ///         using (new EventDispatcherGate(dispatcher))
    ///         {
    ///             // do something that sends events
    ///         }
    ///     }
    /// }
    /// </code>
    /// </example>
    public struct EventDispatcherGate : IDisposable, IEquatable<EventDispatcherGate>
    {
        readonly EventDispatcher m_Dispatcher;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="d">The dispatcher controlled by this gate.</param>
        public EventDispatcherGate(EventDispatcher d)
        {
            if (d == null)
            {
                throw new ArgumentNullException(nameof(d));
            }
            m_Dispatcher = d;
            m_Dispatcher.CloseGate();
        }

        /// <summary>
        /// Implementation of IDisposable.Dispose. Opens the gate. If all gates are open, events in the queue are processed.
        /// </summary>
        public void Dispose()
        {
            m_Dispatcher.OpenGate();
        }

        /// <undoc/>
        public bool Equals(EventDispatcherGate other)
        {
            return Equals(m_Dispatcher, other.m_Dispatcher);
        }

        /// <undoc/>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is EventDispatcherGate && Equals((EventDispatcherGate)obj);
        }

        public override int GetHashCode()
        {
            return (m_Dispatcher != null ? m_Dispatcher.GetHashCode() : 0);
        }

        /// <undoc/>
        public static bool operator==(EventDispatcherGate left, EventDispatcherGate right)
        {
            return left.Equals(right);
        }

        /// <undoc/>
        public static bool operator!=(EventDispatcherGate left, EventDispatcherGate right)
        {
            return !left.Equals(right);
        }
    }

    /// <summary>
    /// Dispatches events to a <see cref="IPanel"/>.
    /// </summary>
    public sealed class EventDispatcher
    {
        struct EventRecord
        {
            public EventBase m_Event;
            public BaseVisualElementPanel m_Panel;
            public StackTrace m_StackTrace;
            public string stackTrace => m_StackTrace?.ToString() ?? string.Empty;
        }

        internal ClickDetector m_ClickDetector = new ClickDetector();

        static readonly ObjectPool<Queue<EventRecord>> k_EventQueuePool = new ObjectPool<Queue<EventRecord>>(() => new Queue<EventRecord>());
        Queue<EventRecord> m_Queue;
        internal PointerDispatchState pointerState { get; } = new PointerDispatchState();

        uint m_GateCount;

        # region Infinite dispatching prevention and logging
        // m_GateDepth is meant to count the amount of times an event was scheduled during another closed gate.
        // This is used to trigger some warning once we get too deep, before a stack overflow.
        // m_GateCount is decremented too early/before the dispatch
        uint m_GateDepth = 0;
        internal const int k_MaxGateDepth = 500;
        internal const int k_NumberOfEventsWithStackInfo = 10;
        internal const int k_NumberOfEventsWithEventInfo = 100;
        internal uint GateDepth { get => m_GateDepth; }//for unit test
        int m_DispatchStackFrame=0; //To record where the stack begin to be relevent for the user when we log the stack
        private EventBase m_CurrentEvent;
        #endregion

        struct DispatchContext
        {
            public uint m_GateCount;
            public Queue<EventRecord> m_Queue;
        }

        Stack<DispatchContext> m_DispatchContexts = new Stack<DispatchContext>();

        static EventDispatcher s_EditorEventDispatcher;

        internal static EventDispatcher editorDispatcher
        {
            get
            {
                if (s_EditorEventDispatcher == null)
                    s_EditorEventDispatcher = CreateDefault();

                return s_EditorEventDispatcher;
            }
        }

        internal static void ClearEditorDispatcher()
        {
            s_EditorEventDispatcher = null;
        }

        internal static EventDispatcher CreateDefault()
        {
#pragma warning disable 618
            return new EventDispatcher();
#pragma warning restore 618
        }

        // Remove once PanelDebug stops using it.
        [Obsolete("Please use EventDispatcher.CreateDefault().")]
        internal EventDispatcher()
        {
            m_Queue = k_EventQueuePool.Get();
        }

        bool m_Immediate = false;

        bool dispatchImmediately
        {
            get { return m_Immediate || m_GateCount == 0; }
        }

        internal bool processingEvents { get; private set; }

        internal void Dispatch(EventBase evt, [NotNull] BaseVisualElementPanel panel, DispatchMode dispatchMode)
        {
            evt.MarkReceivedByDispatcher();

            if (evt.eventTypeId == IMGUIEvent.TypeId())
            {
                Event e = evt.imguiEvent;
                if (e.rawType == EventType.Repaint)
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
                if (HandleRecursiveState(evt))
                    return;

                evt.Acquire();
                m_Queue.Enqueue(new EventRecord
                {
                    m_Event = evt,
                    m_Panel = panel,
                    m_StackTrace = (panel.panelDebug?.hasAttachedDebuggers ?? false) ? new StackTrace() : null
                });
            }
        }

        /// <summary>
        /// Log info to help diagnostic
        /// </summary>
        /// <returns>true if likely to cause stack overflow</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool HandleRecursiveState(EventBase evt)
        {
            if (m_GateDepth <= k_MaxGateDepth - k_NumberOfEventsWithEventInfo)
                return false;

            //Getting the stack trace is painfully slow, only enable this for the last 10 calls
            if (m_DispatchStackFrame != 0)
            {

                var stack = new StackTrace(1, true); // skip this method, start with panel.SendEvent
                System.Text.StringBuilder sb = new();
                int frameToDisplay = stack.FrameCount - m_DispatchStackFrame;

                //By default evt.ToString() will display the class name, but it allows users to implement their own ToString to provide more information
                sb.AppendLine($"Recursively dispatching event {evt} from another event {m_CurrentEvent} (depth = {m_GateDepth})");
                for (int i = 0; i < frameToDisplay; i++)
                {
                    var frame = stack.GetFrame(i);
                    sb.Append(frame.GetMethod()).AppendFormat("({0}:{1}", frame.GetFileName(), frame.GetFileLineNumber()).AppendLine(")");
                }

                Debug.LogFormat(LogType.Error, LogOption.NoStacktrace, null, sb.ToString());
            }
            else
            {
                Debug.LogFormat(LogType.Error, LogOption.NoStacktrace, null, $"Recursively dispatching event {evt} from another event {m_CurrentEvent} (depth = {m_GateDepth})");
            }

            if (m_GateDepth > k_MaxGateDepth) //Stack overflow prevention
            {
                Debug.LogErrorFormat("Ignoring event {0}: too many events dispatched recurively", evt);
                return true;
            }
            
            return false;
        }

        internal void PushDispatcherContext()
        {
            // Drain the event queue before pushing a new context.  This allows some important events
            // (such as AttachToPanel events) to be processed before showing a modal window. (Fixes case 1215148).
            ProcessEventQueue();

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
            m_GateDepth++;
        }

        internal void OpenGate()
        {
            Debug.Assert(m_GateCount > 0, "m_GateCount > 0");

            if (m_GateCount > 0)
            {
                m_GateCount--;
            }

            try
            {
                if (m_GateCount == 0)
                {
                    ProcessEventQueue();
                }
            }
            finally
            {
                // The static initialization of m_GateDepth is the only one so if we skip decrementing the counter because of an exception,
                // the warning will be displayed for all subsequent events dispatched...
                Debug.Assert(m_GateDepth > 0, "m_GateDepth > 0");
                if (m_GateDepth > 0)
                {
                    m_GateDepth--;
                }
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
                processingEvents = true;
                while (queueToProcess.Count > 0)
                {
                    EventRecord eventRecord = queueToProcess.Dequeue();
                    EventBase evt = eventRecord.m_Event;
                    BaseVisualElementPanel panel = eventRecord.m_Panel;
                    try
                    {
                        ProcessEvent(evt, panel);
                    }
                    catch (ExitGUIException e)
                    {
                        Debug.Assert(caughtExitGUIException == null, eventRecord.stackTrace);
                        caughtExitGUIException = e;
                    }
                    finally
                    {
                        // Balance the Acquire when the event was put in queue.
                        evt.Dispose();
                    }
                }
            }
            finally
            {
                processingEvents = false;
                k_EventQueuePool.Release(queueToProcess);
            }

            if (caughtExitGUIException != null)
            {
                throw caughtExitGUIException;
            }
        }

        void ProcessEvent(EventBase evt, [NotNull] BaseVisualElementPanel panel)
        {
            Event e = evt.imguiEvent;
            // Sometimes (in tests only?) we receive Used events. Protect our verification from this case.
            bool imguiEventIsInitiallyUsed = e != null && e.rawType == EventType.Used;

            using (new EventDispatcherGate(this))
            {
                evt.PreDispatch(panel);

                if (!DebuggerEventDispatchUtilities.InterceptEvent(evt, panel))
                {
                    try
                    {
                        m_CurrentEvent = evt;
                        // Record the stack frame depth for later trimming the irrelevant part of the stack when logging recursive events.
                        // 0 mean no stack trace displayed
                        m_DispatchStackFrame = m_GateDepth > k_MaxGateDepth - k_NumberOfEventsWithStackInfo ? new StackTrace().FrameCount : 0; 
                        evt.Dispatch(panel);
                    }
                    finally
                    {
                        m_CurrentEvent = null;
                    }
                }
                DebuggerEventDispatchUtilities.PostDispatch(evt, panel);

                evt.PostDispatch(panel);

                Debug.Assert(imguiEventIsInitiallyUsed || evt.isPropagationStopped || e == null || e.rawType != EventType.Used, "Event is used but not stopped.");
            }
        }
    }
}
