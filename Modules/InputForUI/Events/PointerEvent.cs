// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using Unity.IntegerTime;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEngine.InputForUI
{
    [VisibleToOtherModules("UnityEngine.UIElementsModule")]
    internal struct PointerEvent : IEventProperties
    {
        public enum Type
        {
            /// <summary>
            /// Pointer position changed or delta changed.
            /// Event is sent strictly between PointerEntered and PointerLeft.
            /// </summary>
            PointerMoved = 1,

            /// <summary>
            /// Scroll a panel underneath pointer.
            /// Event is sent strictly between PointerEntered and PointerLeft.
            /// </summary>
            Scroll = 2,

            /// <summary>
            /// Button was pressed.
            /// Event is sent strictly between PointerEntered and PointerLeft.
            /// </summary>
            ButtonPressed = 3,

            /// <summary>
            /// Button was released.
            /// Event is sent strictly between PointerEntered and PointerLeft.
            /// </summary>
            ButtonReleased = 4,

            /// <summary>
            /// No changes since last event.
            /// Used when code requested current state of devices for polling purposes.
            /// </summary>
            State = 5,

            /// <summary>
            /// Button was released.
            /// Event is sent strictly between PointerEntered and PointerLeft.
            /// </summary>
            TouchCanceled = 6,
        }

        [Flags]
        public enum Button : uint
        {
            None = 0x0,

            /// <summary>
            /// Primary button, either mouse left, pen tip, or _any_ finger.
            /// </summary>
            Primary = 0x1,

            /// <summary>
            /// Represents if any of fingers is touching the touch screen surface.
            /// </summary>
            FingerInTouch = 0x1,

            /// <summary>
            /// Pen tip is touching the digitizer.
            /// </summary>
            PenTipInTouch = 0x1,

            /// <summary>
            /// Pen eraser is touching the digitizer.
            /// </summary>
            PenEraserInTouch = 0x2,

            /// <summary>
            /// First barrel button is pressed.
            /// </summary>
            PenBarrelButton = 0x4,

            MouseLeft = 0x1,
            MouseRight = 0x2,
            MouseMiddle = 0x4,
            MouseForward = 0x8,
            MouseBack = 0x10,
        }

        public Type type;

        /// <summary>
        /// For fingers is a number of order of fingers touching the screen,
        /// e.g. first finger is 0, second finger is 1, third finger is 2, ...
        /// For mouse or pen is equal to 0.
        /// </summary>
        public int pointerIndex;

        /// <summary>
        /// Returns true if pointer can be considered to be a primary pointer.
        /// True for mouse or pen, and true for first finger of the touch.
        /// </summary>
        public bool isPrimaryPointer => pointerIndex == 0;

        /// <summary>
        /// Current position of the pointer.
        /// </summary>
        public Vector2 position;

        /// <summary>
        /// Difference between current position and last position of the pointer
        /// </summary>
        public Vector2 deltaPosition;

        /// <summary>
        /// Scroll value for scroll events.
        /// (0, 0) for other type of events.
        /// </summary>
        public Vector2 scroll;

        /// <summary>
        /// Index of the display where the event happened.
        /// </summary>
        public int displayIndex;

        /// <summary>
        /// Pen or finger tilt respective the input surface.
        /// (0, 0) if not available.
        /// </summary>
        public Vector2 tilt;

        public float azimuth => InputManagerProvider.TiltToAzimuth(tilt);
        public float altitude => InputManagerProvider.TiltToAltitude(tilt);

        /// <summary>
        /// Pen twist, 0 for other event sources.
        /// </summary>
        public float twist;

        /// <summary>
        /// Pen or finger pressure on the input surface.
        /// Value from [0, 1].
        /// 0 if not available.
        /// TODO: revisit if we can use a value other than 0 for when not available.
        /// </summary>
        public float pressure;

        /// <summary>
        /// True if pen is inverted, even if pen is not touching the digitizer surface.
        /// False for mouse or touch.
        /// </summary>
        public bool isInverted;

        /// <summary>
        /// The pressed or released button in ButtonPressed and ButtonReleased events.
        /// Button.None in all other cases.
        /// </summary>
        public Button button;

        /// <summary>
        /// State of all buttons at the time of event for the respective source,
        /// e.g. state of all mouse buttons if eventSource is mouse,
        /// state of all pen buttons if eventSource is pen,
        /// etc.
        /// </summary>
        public ButtonsState buttonsState;

        /// <summary>
        /// Returns true if:
        /// - Left mouse button is pressed.
        /// - Primary finger is in contact with touchscreen.
        /// - Pen tip is in touch with digitizer.
        /// - Pen eraser is in touch with digitizer.
        /// </summary>
        public bool isPressed => buttonsState.Get(isInverted ? Button.PenEraserInTouch : Button.Primary);

        /// <summary>
        /// Amount of times the current button was pressed fast enough to count as a click.
        /// </summary>
        public int clickCount;

        public DiscreteTime timestamp { get; set; }
        public EventSource eventSource { get; set; }
        public uint playerId { get; set; }
        public EventModifiers eventModifiers { get; set; }

        public override string ToString()
        {
            var pen = eventSource == EventSource.Pen ? $" tilt:({tilt.x:f1},{tilt.y:f1}) az:{azimuth:f2} al:{altitude:f2} twist:{twist} pressure:{pressure} isInverted:{(isInverted ? 1 : 0)}" : "";
            var touch = eventSource == EventSource.Touch ? $" finger:{pointerIndex} tilt:({tilt.x:f1},{tilt.y:f1}) twist:{twist} pressure:{pressure}" : "";
            var dsp = $" dsp:{displayIndex}";
            var gen = $"{pen}{touch}{dsp}";

            switch (type)
            {
                case Type.PointerMoved:
                    return $"{type} pos:{position} dlt:{deltaPosition} btns:{buttonsState}{gen}";
                case Type.Scroll:
                    return $"{type} pos:{position} scr:{scroll}{gen}";
                case Type.ButtonPressed:
                case Type.ButtonReleased:
                    return $"{type} pos:{position} btn:{button} btns:{buttonsState} clk:{clickCount}{gen}";
                case Type.State:
                    return $"{type} pos:{position} btns:{buttonsState}{gen}";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public struct ButtonsState
        {
            private uint _state;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Set(Button button, bool pressed)
            {
                if (pressed)
                    _state |= (uint)button;
                else
                    _state &= ~(uint)button;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Get(Button button)
            {
                return (_state & (uint)button) != 0;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Reset()
            {
                _state = 0;
            }

            public override string ToString()
            {
                return $"{_state:x2}";
            }
        }

        internal static Button ButtonFromButtonIndex(int index)
        {
            return index <= 31 ? (Button)(1u << index) : Button.None;
        }
    }
}
