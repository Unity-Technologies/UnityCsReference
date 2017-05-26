// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.Experimental.UIElements
{
    public abstract class EventBase
    {
        // Automatic event type id.
        private static long s_LastClassId = 0;
        protected static long RegisterEventClass()
        {
            return ++s_LastClassId;
        }

        public abstract long GetEventTypeId();

        [Flags]
        public enum EventFlags
        {
            None = 0,
            Bubbles = 1,
            Cancellable = 2,
        }

        // Read-only state
        public long timestamp { get; private set; }
        private EventFlags flags;

        public bool bubbles
        {
            get { return (flags & EventFlags.Bubbles) == 0 ? false : true; }
        }

        public IEventHandler target { get; internal set; }

        public bool isPropagationStopped { get; private set; }
        public void StopPropagation()
        {
            isPropagationStopped = true;
        }

        public bool isImmediatePropagationStopped { get; private set; }
        public void StopImmediatePropagation()
        {
            isPropagationStopped = true;
            isImmediatePropagationStopped = true;
        }

        // Propagation state
        public PropagationPhase propagationPhase { get; internal set; }
        public virtual IEventHandler currentTarget { get; internal set; }
        public bool dispatch { get; internal set; }

        internal Event imguiEvent { get; private set; }

        public EventBase(EventFlags flags, Event imguiEvent)
        {
            this.flags = flags;
            timestamp = DateTime.Now.Ticks;

            target = null;
            currentTarget = null;
            propagationPhase = PropagationPhase.None;

            isPropagationStopped = false;
            isImmediatePropagationStopped = false;

            this.imguiEvent = imguiEvent;
        }
    }
}
