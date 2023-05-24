// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEngine.InputForUI
{
    [VisibleToOtherModules("UnityEngine.UIElementsModule")]
    internal struct EventModifiers
    {
        // Types of modifier key that can be active during a keystroke event.
        [Flags]
        public enum Modifiers : uint
        {
            LeftShift = 1u << 0,
            RightShift = 1u << 1,
            Shift = LeftShift | RightShift,

            LeftCtrl = 1u << 2,
            RightCtrl = 1u << 3,
            Ctrl = LeftCtrl | RightCtrl,

            LeftAlt = 1u << 4,
            // Generally try to avoid using RightAlt for anything hardcoded due to it being the same as AltGr key.
            RightAlt = 1u << 5,
            Alt = LeftAlt | RightAlt,

            // If Windows or Command key is pressed.
            LeftMeta = 1u << 6,
            RightMeta = 1u << 7,
            Meta = LeftMeta | RightMeta,

            CapsLock = 1u << 8,

            Numlock = 1u << 9,

            // TODO For some reason IMGUI also has a flag if any F1-F15 keys are pressed + extra keys.
            // TODO Not sure what it is about or how it is used exactly.
            // TODO https://github.cds.internal.unity3d.com/unity/unity/blob/trunk/Modules/IMGUI/Event.cs#L228-L256
            FunctionKey = 1u << 10,
            Numeric = 1u << 11
        }

        private uint _state;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsPressed(Modifiers mod) => (_state & (uint)mod) != 0;

        public bool isShiftPressed => IsPressed(Modifiers.Shift);
        public bool isCtrlPressed => IsPressed(Modifiers.Ctrl);
        public bool isAltPressed => IsPressed(Modifiers.Alt);
        public bool isMetaPressed => IsPressed(Modifiers.Meta);
        public bool isCapsLockEnabled => IsPressed(Modifiers.CapsLock);
        public bool isNumLockEnabled => IsPressed(Modifiers.Numlock);
        public bool isFunctionKeyPressed => IsPressed(Modifiers.FunctionKey);
        public bool isNumericPressed => IsPressed(Modifiers.Numeric);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetPressed(Modifiers modifier, bool pressed)
        {
            if (pressed)
                _state |= (uint)modifier;
            else
                _state &= ~(uint)modifier;
        }

        public void Reset()
        {
            _state = 0;
        }

        private static void Append(ref string str, string value)
        {
            str = string.IsNullOrEmpty(str) ? value : $"{str},{value}";
        }

        public override string ToString()
        {
            var str = string.Empty;
            if (IsPressed(Modifiers.LeftShift))
                Append(ref str, "LeftShift");
            if (IsPressed(Modifiers.RightShift))
                Append(ref str, "RightShift");
            if (IsPressed(Modifiers.LeftCtrl))
                Append(ref str, "LeftCtrl");
            if (IsPressed(Modifiers.RightCtrl))
                Append(ref str, "RightCtrl");
            if (IsPressed(Modifiers.LeftAlt))
                Append(ref str, "LeftAlt");
            if (IsPressed(Modifiers.RightAlt))
                Append(ref str, "RightAlt");
            if (IsPressed(Modifiers.LeftMeta))
                Append(ref str, "LeftMeta");
            if (IsPressed(Modifiers.RightMeta))
                Append(ref str, "RightMeta");
            if (IsPressed(Modifiers.CapsLock))
                Append(ref str, "CapsLock");
            if (IsPressed(Modifiers.Numlock))
                Append(ref str, "Numlock");
            if (IsPressed(Modifiers.FunctionKey))
                Append(ref str, "FunctionKey");
            if (IsPressed(Modifiers.Numeric))
                Append(ref str, "Numeric");
            return str;
        }
    }
}
