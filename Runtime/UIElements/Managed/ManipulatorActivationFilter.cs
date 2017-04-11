// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.Experimental.UIElements
{
    public struct ManipulatorActivationFilter
    {
        public MouseButton button;
        public EventModifiers modifiers;

        public bool Matches(Event evt)
        {
            return button == (MouseButton)evt.button && HasModifiers(evt);
        }

        private bool HasModifiers(Event evt)
        {
            if (((modifiers & EventModifiers.Alt) != 0 && !evt.alt) ||
                ((modifiers & EventModifiers.Alt) == 0 && evt.alt))
            {
                return false;
            }

            if (((modifiers & EventModifiers.Control) != 0 && !evt.control) ||
                ((modifiers & EventModifiers.Control) == 0 && evt.control))
            {
                return false;
            }

            if (((modifiers & EventModifiers.Shift) != 0 && !evt.shift) ||
                ((modifiers & EventModifiers.Shift) == 0 && evt.shift))
            {
                return false;
            }

            if (((modifiers & EventModifiers.Command) != 0 && !evt.command) ||
                ((modifiers & EventModifiers.Command) == 0 && evt.command))
            {
                return false;
            }

            return true;
        }
    }
}
