// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.Experimental.UIElements
{
    class IMRepeatButton : IMElement
    {
        // TODO Find a way to deprecate RepeatButtons?
        public bool isPressed { get; private set; }

        public IMRepeatButton()
        {
            this.focusType = FocusType.Keyboard;
        }

        public override bool OnGUI(Event evt)
        {
            // Re-evaluate this every time to maintain isomorphic behavior
            isPressed = false;
            return base.OnGUI(evt);
        }

        protected override int DoGenerateControlID()
        {
            return GUIUtility.GetControlID("IMRepeatButton".GetHashCode(), focusType, position);
        }

        public override void DoRepaint(IStylePainter args)
        {
            style.Draw(position, content, id);
            if (GUIUtility.hotControl == id)
            {
                isPressed = position.Contains(args.mousePosition);
            }
        }

        protected override bool DoMouseDown(MouseEventArgs args)
        {
            // Needs distinction between old style (Event.current.mousePosition) and new style (Event.current.touch.pos) way to get pos.
            // This is done everywhere throughout the c++ code. Ideally, this would be abstracted at this level.

            // If the mouse is inside the button, we say that we're the hot control
            if (position.Contains(args.mousePosition))
            {
                GUIUtility.hotControl = id;
                return true;
            }
            return false;
        }

        protected override bool DoMouseUp(MouseEventArgs args)
        {
            if (GUIUtility.hotControl == id)
            {
                GUIUtility.hotControl = 0;

                // But we only return true if the button was actually clicked
                isPressed = position.Contains(args.mousePosition);

                // If we got the mousedown, the mouseup is ours as well
                // (no matter if the click was in the button or not)
                return true;
            }
            return false;
        }

        protected override bool DoMouseDrag(MouseEventArgs args)
        {
            if (GUIUtility.hotControl == id)
            {
                return true;
            }
            return false;
        }

        protected override bool DoKeyDown(KeyboardEventArgs args)
        {
            if (args.character == 32 && GUIUtility.keyboardControl == id)
            {
                GUIUtility.SetChanged(true);
                isPressed = true;
                return true;
            }
            return false;
        }

        protected override bool DoKeyUp(KeyboardEventArgs args)
        {
            if (args.character == 32 && GUIUtility.keyboardControl == id)
            {
                isPressed = false;
                return true;
            }
            return false;
        }
    }
}
