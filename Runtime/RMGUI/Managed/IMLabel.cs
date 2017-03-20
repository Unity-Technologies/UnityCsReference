// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.Experimental.RMGUI
{
    class IMLabel : IMElement
    {
        public void ShowTooltip(Vector2 tooltipPos)
        {
            // TODO Fix tooltips
            // This is the implementation of tooltips from unity\Runtime\IMGUI\GUILabel.cpp, GUILabel.
            // There is another in unity\Runtime\IMGUI\GUIStyle.cpp, GUIStyle::Draw.

            // Ideally, in C#, we'd try to avoid null string as much as possible. Null breaks stuff left and right.
            if (!string.IsNullOrEmpty(content.tooltip))
            {
                // Needs distinction between old style (Event.current.mousePosition) and new style (Event.current.touch.pos) way to get pos.
                // This is done everywhere throughout the c++ code. Ideally, this would be abstracted at this level.
                if (position.Contains(tooltipPos))
                {
                    GUIStyle.SetMouseTooltip(content.tooltip, position);
                }
            }
        }

        protected override int DoGenerateControlID()
        {
            // We do not call GUIUtility.GetControlID for labels
            return NonInteractiveControlID;
        }

        public override void DoRepaint(IStylePainter args)
        {
            // Same as box but for tooltip
            style.Draw(position, content,
                // We need to do something about these values. Can we get them from C# of something?
                false, // IsHover
                false, // IsActive
                false, // on (???)
                false); // hasKeyboardFocus
            ShowTooltip(args.mousePosition);
        }
    }
}
