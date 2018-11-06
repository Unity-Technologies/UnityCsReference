// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.UIElements
{
    public abstract class ImmediateModeElement : VisualElement
    {
        internal override void DoRepaint(IStylePainter painter)
        {
            var stylePainter = (IStylePainterInternal)painter;
            stylePainter.DrawImmediate(ImmediateRepaint);
        }

        protected abstract void ImmediateRepaint();
    }
}
