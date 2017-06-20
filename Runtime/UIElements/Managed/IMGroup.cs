// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.Experimental.UIElements
{
    class IMGroup : IMContainer
    {
        public override bool OnGUI(Event evt)
        {
            if (!string.IsNullOrEmpty(text) ||
                style != GUIStyle.none)
            {
                switch (evt.type)
                {
                    case EventType.Repaint:
                        DoRepaint(new StylePainter(evt.mousePosition));
                        break;
                    default:
                        if (layout.Contains(evt.mousePosition))
                        {
                            GUIUtility.mouseUsed = true;
                        }
                        break;
                }
            }
            return false; // use no events
        }

        public override void GenerateControlID()
        {
            id = GUIUtility.GetControlID("IMGroup".GetHashCode(), FocusType.Passive);
        }

        internal override void DoRepaint(IStylePainter args)
        {
            // TODO: Same as box. Should move in common base class?
            style.Draw(layout, GUIContent.Temp(text), id);
        }
    }
}
