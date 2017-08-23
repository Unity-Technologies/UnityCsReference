// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.Experimental.UIElements
{
    public struct ManipulatorActivationFilter
    {
        public MouseButton button;
        public EventModifiers modifiers;

        public bool Matches(IMouseEvent e)
        {
            return button == (MouseButton)e.button && HasModifiers(e);
        }

        private bool HasModifiers(IMouseEvent e)
        {
            if (((modifiers & EventModifiers.Alt) != 0 && !e.altKey) ||
                ((modifiers & EventModifiers.Alt) == 0 && e.altKey))
            {
                return false;
            }

            if (((modifiers & EventModifiers.Control) != 0 && !e.ctrlKey) ||
                ((modifiers & EventModifiers.Control) == 0 && e.ctrlKey))
            {
                return false;
            }

            if (((modifiers & EventModifiers.Shift) != 0 && !e.shiftKey) ||
                ((modifiers & EventModifiers.Shift) == 0 && e.shiftKey))
            {
                return false;
            }

            if (((modifiers & EventModifiers.Command) != 0 && !e.commandKey) ||
                ((modifiers & EventModifiers.Command) == 0 && e.commandKey))
            {
                return false;
            }

            return true;
        }
    }
}
