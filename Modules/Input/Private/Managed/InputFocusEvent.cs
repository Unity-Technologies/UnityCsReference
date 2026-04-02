// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;

namespace UnityEngineInternal.Input
{
    /// <summary>
    /// A Focus input event.
    /// </summary>
    /// <remarks>
    /// InputFocusEvent is sent when focus state changes.
    /// </remarks>
    [StructLayout(LayoutKind.Explicit, Size = NativeInputEvent.structSize + 4, Pack = 1)]
    internal ref struct InputFocusEvent
    {
        /// <summary>
        /// The native input event type identifier for focus events (FOCU).
        /// </summary>
        public const NativeInputEventType Type = NativeInputEventType.Focus;

        /// <summary>
        /// The underlying native input event structure.
        /// </summary>
        [FieldOffset(0)]
        public NativeInputEvent baseEvent;

        /// <summary>
        /// Flags indicating the current focus state.
        /// </summary>
        [FieldOffset(NativeInputEvent.structSize)]
        public FocusFlags focusFlags;

        /// <summary>
        /// Whether the application has gained (true) or lost (false) focus.
        /// </summary>
        public bool hasApplicationFocus => (focusFlags & FocusFlags.ApplicationFocus) != 0;

        /// <summary>
        /// Creates a new InputFocusEvent
        /// </summary>
        /// <param name="flags">The focus flags indicating the current focus state.</param>
        /// <returns>A new InputFocusEvent instance with the specified flags.</returns>
        public static InputFocusEvent Create(FocusFlags flags)
        {
            var inputEvent = new InputFocusEvent
            {
                baseEvent = new NativeInputEvent(Type, NativeInputEvent.structSize + 4, 0, -1.0),
                focusFlags = flags
            };
            return inputEvent;
        }
    }
}
