// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.UIElements.Experimental
{
    [Serializable]
    internal class EventDebuggerRecordList
    {
        public List<EventDebuggerEventRecord> eventList;
    }

    [Serializable]
    internal class EventDebuggerEventRecord
    {
        [field: SerializeField]
        public string eventBaseName { get; private set; }
        [field: SerializeField]
        public long eventTypeId { get; private set; }
        [field: SerializeField]
        public ulong eventId { get; private set; }
        [field: SerializeField]
        private ulong triggerEventId { get; set; }
        [field: SerializeField]
        internal long timestamp { get; private set; }
        public IEventHandler target { get; set; }
        private bool isPropagationStopped { get; set; }
        private bool isImmediatePropagationStopped { get; set; }
        public PropagationPhase propagationPhase { get; private set; }
        private IEventHandler currentTarget { get; set; }
        private bool dispatch { get; set; }
        private Vector2 originalMousePosition { get; set; }

        // Input events specific
        public EventModifiers modifiers { get; private set; }

        // Mouse events specific
        [field: SerializeField]
        public Vector2 mousePosition { get; private set; }
        [field: SerializeField]
        public int clickCount { get; private set; }
        [field: SerializeField]
        public int button { get; private set; }
        [field: SerializeField]
        public int pressedButtons { get; private set; }
        [field: SerializeField]
        public int pointerId { get; private set; }

        // Wheel event specific
        [field: SerializeField]
        public Vector3 delta { get; private set; }

        // Keyboard events specific
        [field: SerializeField]
        public char character { get; private set; }
        [field: SerializeField]
        public KeyCode keyCode { get; private set; }

        // Command events specific
        [field: SerializeField]
        public string commandName { get; private set; }

        // Navigation events specific
        [field: SerializeField]
        public NavigationDeviceType deviceType { get; private set; }
        [field: SerializeField]
        public NavigationMoveEvent.Direction navigationDirection { get; private set; }

        void Init(EventBase evt)
        {
            var type = evt.GetType();
            eventBaseName = EventDebugger.GetTypeDisplayName(type);
            eventTypeId = evt.eventTypeId;
            eventId = evt.eventId;
            triggerEventId = evt.triggerEventId;

            timestamp = evt.timestamp;

            target = evt.target;

            isPropagationStopped = evt.isPropagationStopped;
            isImmediatePropagationStopped = evt.isImmediatePropagationStopped;

            propagationPhase = evt.propagationPhase;

            originalMousePosition = evt.originalMousePosition;
            currentTarget = evt.currentTarget;

            dispatch = evt.dispatch;

            if (evt is IMouseEvent mouseEvent)
            {
                modifiers = mouseEvent.modifiers;
                mousePosition = mouseEvent.mousePosition;
                button = mouseEvent.button;
                pressedButtons = mouseEvent.pressedButtons;
                clickCount = mouseEvent.clickCount;

                if (mouseEvent is WheelEvent wheelEvent)
                {
                    delta = wheelEvent.delta;
                }
            }

            if (evt is IPointerEvent pointerEvent)
            {
                modifiers = pointerEvent.modifiers;
                mousePosition = pointerEvent.position;
                button = pointerEvent.button;
                pressedButtons = pointerEvent.pressedButtons;
                clickCount = pointerEvent.clickCount;
                pointerId = pointerEvent.pointerId;
            }

            if (evt is IKeyboardEvent keyboardEvent)
            {
                modifiers = keyboardEvent.modifiers;
                character = keyboardEvent.character;
                keyCode = keyboardEvent.keyCode;
            }

            if (evt is ICommandEvent commandEvent)
            {
                commandName = commandEvent.commandName;
            }

            if (evt is INavigationEvent navigationEvent)
            {
                deviceType = navigationEvent.deviceType;

                if (navigationEvent is NavigationMoveEvent nme)
                {
                    navigationDirection = nme.direction;
                }
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
