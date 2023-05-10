// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using Unity.IntegerTime;
using UnityEngine;

namespace UnityEngine.InputForUI
{
    // Keyboard key event
    internal struct KeyEvent : IEventProperties
    {
        public enum Type
        {
            /// <summary>
            /// Button was pressed.
            /// </summary>
            KeyPressed = 1,

            /// <summary>
            /// Button was held and press was repeated.
            /// </summary>
            KeyRepeated = 2,

            /// <summary>
            /// Button was released.
            /// </summary>
            KeyReleased = 3,

            /// <summary>
            /// No changes since last event.
            /// Used when code requested current state of devices for polling purposes.
            /// </summary>
            State = 4
        }

        public Type type;

        public KeyCode keyCode;

        public ButtonsState buttonsState;

        public DiscreteTime timestamp { get; set; }
        public EventSource eventSource { get; set; }
        public uint playerId { get; set; }
        public EventModifiers eventModifiers { get; set; }

        public override string ToString()
        {
            switch (type)
            {
                case Type.KeyPressed:
                case Type.KeyRepeated:
                case Type.KeyReleased:
                    return $"{type} {keyCode}";
                case Type.State:
                    return $"{type} Pressed:{buttonsState}";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public struct ButtonsState
        {
            // ignore everything above KeyCode.Menu as it only contains mouse and joysticks
            // TODO do we need to map to a more tight bit packing?
            private const uint kMaxIndex = (int)KeyCode.Menu;
            private const uint kSizeInBytes = (kMaxIndex + 7) / 8;

            private unsafe fixed byte buttons[(int)kSizeInBytes];

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal static bool ShouldBeProcessed(KeyCode keyCode)
            {
                var index = (uint)keyCode;
                return index <= kMaxIndex;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private unsafe bool GetUnchecked(uint index)
            {
                return (buttons[index >> 3] & (byte)(1U << (int)(index & 7))) != 0;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private unsafe void SetUnchecked(uint index)
            {
                buttons[index >> 3] |= (byte)(1U << (int)(index & 7));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private unsafe void ClearUnchecked(uint index)
            {
                buttons[index >> 3] &= (byte)~(1U << (int)(index & 7));
            }

            public bool IsPressed(KeyCode keyCode)
            {
                return ShouldBeProcessed(keyCode) && GetUnchecked((uint)keyCode);
            }

            public IEnumerable<KeyCode> GetAllPressed()
            {
                for (var index = 0U; index <= kMaxIndex; ++index)
                    if (GetUnchecked(index))
                        yield return (KeyCode)index;
            }

            public void SetPressed(KeyCode keyCode, bool pressed)
            {
                if (!ShouldBeProcessed(keyCode))
                    return;
                if (pressed)
                    SetUnchecked((uint)keyCode);
                else
                    ClearUnchecked((uint)keyCode);
            }

            public unsafe void Reset()
            {
                for (var i = 0; i < kSizeInBytes; ++i)
                    buttons[i] = 0;
            }

            public override string ToString()
            {
                return string.Join(",", GetAllPressed());
            }
        }
    }
}
