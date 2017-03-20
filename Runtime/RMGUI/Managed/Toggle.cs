// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.Experimental.RMGUI
{
    [GUISkinStyle("toggle")]
    // TODO Make public. It's currently internal because it clashes in a doc tool with another class in another namespace.
    // Once the tool is fixed, this becomes public.
    internal class Toggle : Button
    {
        public bool on
        {
            get
            {
                return (paintFlags & PaintFlags.On) == PaintFlags.On;
            }
            set
            {
                if (value)
                {
                    paintFlags |= PaintFlags.On;
                }
                else
                {
                    paintFlags &= ~PaintFlags.On;
                }
            }
        }

        public Toggle(ClickEvent clickEvent)
            : base(clickEvent)
        {
            // TODO: This is fragile and could break if someone fiddles around with clickable.OnClick later on.
            clickable.OnClick += () =>
                {
                    on = !on;
                };
        }
    }
}
