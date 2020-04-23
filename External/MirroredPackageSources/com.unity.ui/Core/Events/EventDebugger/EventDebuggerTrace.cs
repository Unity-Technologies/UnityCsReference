namespace UnityEngine.UIElements
{
    class EventDebuggerTrace
    {
        public EventDebuggerEventRecord eventBase { get; }
        public IEventHandler focusedElement { get; }
        public IEventHandler mouseCapture { get; }
        public long duration { get; set; }

        public EventDebuggerTrace(IPanel panel, EventBase evt, long duration, IEventHandler mouseCapture)
        {
            eventBase = new EventDebuggerEventRecord(evt);
            focusedElement = panel?.focusController?.focusedElement;
            this.mouseCapture = mouseCapture;
            this.duration = duration;
        }
    }

    class EventDebuggerCallTrace : EventDebuggerTrace
    {
        public int callbackHashCode { get; }
        public string callbackName { get; }
        public bool propagationHasStopped { get; }
        public bool immediatePropagationHasStopped { get; }
        public bool defaultHasBeenPrevented { get; }

        public EventDebuggerCallTrace(IPanel panel, EventBase evt, int cbHashCode, string cbName,
                                      bool propagationHasStopped,
                                      bool immediatePropagationHasStopped,
                                      bool defaultHasBeenPrevented,
                                      long duration,
                                      IEventHandler mouseCapture)
            : base(panel, evt, duration, mouseCapture)
        {
            this.callbackHashCode = cbHashCode;
            this.callbackName = cbName;
            this.propagationHasStopped = propagationHasStopped;
            this.immediatePropagationHasStopped = immediatePropagationHasStopped;
            this.defaultHasBeenPrevented = defaultHasBeenPrevented;
        }
    }

    class EventDebuggerDefaultActionTrace : EventDebuggerTrace
    {
        public PropagationPhase phase { get; }

        public string targetName
        {
            get { return eventBase.target.GetType().FullName; }
        }

        public EventDebuggerDefaultActionTrace(IPanel panel, EventBase evt, PropagationPhase phase, long duration,
                                               IEventHandler mouseCapture)
            : base(panel, evt, duration, mouseCapture)
        {
            this.phase = phase;
        }
    }

    class EventDebuggerPathTrace : EventDebuggerTrace
    {
        public PropagationPaths paths { get; }

        public EventDebuggerPathTrace(IPanel panel, EventBase evt, PropagationPaths paths)
            : base(panel, evt, -1, null)
        {
            this.paths = paths;
        }
    }
}
