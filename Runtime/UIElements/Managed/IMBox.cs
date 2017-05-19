// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.Experimental.UIElements
{
    class IMBox : IMElement
    {
        protected override int DoGenerateControlID()
        {
            return GUIUtility.GetControlID("IMBox".GetHashCode(), FocusType.Passive);
        }

        internal override void DoRepaint(IStylePainter args)
        {
            style.Draw(position, GUIContent.Temp(text), id);
        }
    }
}
