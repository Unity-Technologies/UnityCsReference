using System;
using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    class EventDebuggerEventRecord
    {
        public string eventBaseName { get; private set; }
        public long eventTypeId { get; private set; }
        public ulong eventId { get; private set; }
        private ulong triggerEventId { get; set; }
        private long timestamp { get; set; }
        public IEventHandler target { get; set; }
        private List<IEventHandler> skipElements { get; set; }
        public bool hasUnderlyingPhysicalEvent { get; private set; }
        private bool isPropagationStopped { get; set; }
        private bool isImmediatePropagationStopped { get; set; }
        private bool isDefaultPrevented { get; set; }
        public PropagationPhase propagationPhase { get; private set; }
        private IEventHandler currentTarget { get; set; }
        private bool dispatch { get; set; }
        private Vector2 originalMousePosition { get; set; }

        // Input events specific
        public EventModifiers modifiers { get; private set; }

        // Mouse events specific
        public Vector2 mousePosition { get; private set; }
        public int clickCount { get; private set; }
        public int button { get; private set; }
        public int pressedButtons { get; private set; }

        // Wheel event specific
        public Vector3 delta { get; private set; }

        // Keyboard events specific
        public char character { get; private set; }
        public KeyCode keyCode { get; private set; }

        // Command events specific
        public string commandName { get; private set; }

        void Init(EventBase evt)
        {
            eventBaseName = evt.GetType().Name;
            eventTypeId = evt.eventTypeId;
            eventId = evt.eventId;
            triggerEventId = evt.triggerEventId;

            timestamp = evt.timestamp;

            target = evt.target;

            skipElements = evt.skipElements;

            isPropagationStopped = evt.isPropagationStopped;
            isImmediatePropagationStopped = evt.isImmediatePropagationStopped;
            isDefaultPrevented = evt.isDefaultPrevented;

            IMouseEvent mouseEvent = evt as IMouseEvent;
            IMouseEventInternal mouseEventInternal = evt as IMouseEventInternal;
            hasUnderlyingPhysicalEvent = mouseEvent != null &&
                mouseEventInternal != null &&
                mouseEventInternal.triggeredByOS;

            propagationPhase = evt.propagationPhase;

            originalMousePosition = evt.originalMousePosition;
            currentTarget = evt.currentTarget;

            dispatch = evt.dispatch;

            if (mouseEvent != null)
            {
                modifiers = mouseEvent.modifiers;
                mousePosition = mouseEvent.mousePosition;
                button = mouseEvent.button;
                pressedButtons = mouseEvent.pressedButtons;
                clickCount = mouseEvent.clickCount;
                // TODO: Scroll Wheel
                //delta = mouseEvent.delta;
            }

            IPointerEvent pointerEvent = evt as IPointerEvent;
            IPointerEventInternal pointerEventInternal = evt as IPointerEventInternal;
            hasUnderlyingPhysicalEvent = pointerEvent != null &&
                pointerEventInternal != null &&
                pointerEventInternal.triggeredByOS;

            if (pointerEvent != null)
            {
                modifiers = pointerEvent.modifiers;
                mousePosition = pointerEvent.position;
                button = pointerEvent.button;
                pressedButtons = pointerEvent.pressedButtons;
                clickCount = pointerEvent.clickCount;
                // TODO: Scroll Wheel
                //delta = mouseEvent.delta;
            }

            IKeyboardEvent keyboardEvent = evt as IKeyboardEvent;
            if (keyboardEvent != null)
            {
                character = keyboardEvent.character;
                keyCode = keyboardEvent.keyCode;
            }

            ICommandEvent commandEvent = evt as ICommandEvent;
            if (commandEvent != null)
            {
                commandName = commandEvent.commandName;
            }
        }

        public EventDebuggerEventRecord(EventBase evt)
        {
            Init(evt);
        }

        public string TimestampString()
        {
            long ticks = (long)(timestamp / 1000f * TimeSpan.TicksPerSecond);
            return new DateTime(ticks).ToString("HH:mm:ss.ffffff");
        }
    }
}
