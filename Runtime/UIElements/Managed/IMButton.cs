// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.Experimental.UIElements
{
    class IMButton : IMElement
    {
        public bool wasPressed { get; private set; }

        public override bool OnGUI(Event evt)
        {
            // re-evaluate each time
            wasPressed = false;
            return base.OnGUI(evt);
        }

        protected override int DoGenerateControlID()
        {
            return GUIUtility.GetControlID("IMButton".GetHashCode(), focusType, position);
        }

        internal override void DoRepaint(IStylePainter args)
        {
            style.Draw(position, GUIContent.Temp(text), id,
                false);          // on (???)
        }

        // TODO: Pass a ref to current here for backward compatibility
        protected override bool DoMouseDown(MouseEventArgs args)
        {
            // TODO: Support touch and mouse.
            // Needs distinction between old style (Event.current.mousePosition) and new style (Event.current.touch.pos) way to get pos.
            // This is done everywhere throughout the c++ code. Ideally, this would be abstracted at this level.
            if (position.Contains(args.mousePosition))
            {
                GUIUtility.hotControl = id;
                return true;
            }
            return false;
        }

        protected override bool DoMouseMove(MouseEventArgs args)
        {
            // TODO...

            return false;
        }

        protected override bool DoMouseUp(MouseEventArgs args)
        {
            if (GUIUtility.hotControl == id)
            {
                GUIUtility.hotControl = 0;
                if (position.Contains(args.mousePosition))
                {
                    GUIUtility.SetChanged(true);
                    wasPressed = true;
                }
                return true;
            }
            return false;
        }

        protected override bool DoKeyDown(KeyboardEventArgs args)
        {
            if (args.character == 32 && GUIUtility.keyboardControl == id)
            {
                GUIUtility.SetChanged(true);
                wasPressed = true;
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
    }
}
