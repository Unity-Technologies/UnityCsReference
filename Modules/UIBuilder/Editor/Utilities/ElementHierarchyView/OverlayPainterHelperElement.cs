// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal class OverlayPainterHelperElement : ImmediateModeElement
    {
        BaseOverlayPainter m_Painter;

        protected new class UxmlFactory : UxmlFactory<OverlayPainterHelperElement, UxmlTraits> { }

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="OverlayPainterHelperElement"/>.
        /// </summary>
        /// <remarks>
        /// This class defines the properties of a OverlayPainterHelperElement element that you can
        /// use in a UXML asset.
        /// </remarks>
        protected new class UxmlTraits : ImmediateModeElement.UxmlTraits
        {
            /// <summary>
            /// Constructor.
            /// </summary>
            public UxmlTraits()
            {
                m_PickingMode.defaultValue = PickingMode.Ignore;
            }
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
