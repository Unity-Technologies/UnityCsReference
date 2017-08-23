// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements.GraphView
{
    internal
    struct ManipulatorActivationFilter
    {
        public MouseButton button;
        public EventModifiers modifiers;

        public bool Matches(IMouseEvent evt)
        {
            return button == (MouseButton)evt.button && HasModifiers(evt);
        }

        private bool HasModifiers(IMouseEvent evt)
        {
            if ((modifiers & EventModifiers.Alt) != 0 && !evt.altKey ||
                (modifiers & EventModifiers.Alt) == 0 && evt.altKey)
            {
                return false;
            }

            if ((modifiers & EventModifiers.Control) != 0 && !evt.ctrlKey ||
                (modifiers & EventModifiers.Control) == 0 && evt.ctrlKey)
            {
                return false;
            }

            if ((modifiers & EventModifiers.Shift) != 0 && !evt.shiftKey ||
                (modifiers & EventModifiers.Shift) == 0 && evt.shiftKey)
            {
                return false;
            }

            if ((modifiers & EventModifiers.Command) != 0 && !evt.commandKey ||
                (modifiers & EventModifiers.Command) == 0 && evt.commandKey)
            {
                return false;
            }

            return true;
        }
    }
}
