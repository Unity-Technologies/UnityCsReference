// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.Experimental.UIElements
{
    class IMToggle : IMElement
    {
        // TODO: should be just another type of button
        public bool value { get; set; }

        public IMToggle()
        {
            this.focusType = FocusType.Passive;
        }

        // TODO Refactor IDs forcing out.
        public void ForceIdValue(int newId)
        {
            id = newId;
        }

        protected override int DoGenerateControlID()
        {
            return GUIUtility.GetControlID("IMToggle".GetHashCode(), focusType, position);
        }

        internal override void DoRepaint(IStylePainter args)
        {
            guiStyle.Draw(position, GUIContent.Temp(text), id, value);
        }

        protected override bool DoMouseDown(MouseEventArgs args)
        {
            // Needs distinction between old style (Event.current.mousePosition) and new style (Event.current.touch.pos) way to get pos.
            // This is done everywhere throughout the c++ code. Ideally, this would be abstracted at this level.
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
                if (position.Contains(args.mousePosition))
                {
                    GUIUtility.SetChanged(true);
                    value = !value;
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
                value = !value;
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
