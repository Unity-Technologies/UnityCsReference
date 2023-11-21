// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal class OverlayPainterHelperElement : ImmediateModeElement
    {
        BaseOverlayPainter m_Painter;

        [Serializable]
        public new class UxmlSerializedData : ImmediateModeElement.UxmlSerializedData
        {
            public override object CreateInstance() => new OverlayPainterHelperElement();
        }

        public BaseOverlayPainter painter { set { m_Painter = value; } }

        public OverlayPainterHelperElement()
        {
            pickingMode = PickingMode.Ignore;
        }

        protected override void ImmediateRepaint()
        {
            m_Painter?.Draw(worldClip);
        }
    }
}
