// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.Experimental.UIElements
{
    public struct ManipulatorActivationFilter
    {
        public MouseButton button;
        public EventModifiers modifiers;
        public int clickCount;

        public bool Matches(IMouseEvent e)
        {
            // Default clickCount field value is 0 since we're in a struct -- this case is covered if the user
            // did not explicitly set clickCount
            var minClickCount = (clickCount == 0 || (e.clickCount >= clickCount));
            return button == (MouseButton)e.button && HasModifiers(e) && minClickCount;
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

            return ((modifiers & EventModifiers.Command) == 0 || e.commandKey) &&
                ((modifiers & EventModifiers.Command) != 0 || !e.commandKey);
        }
    }
}
