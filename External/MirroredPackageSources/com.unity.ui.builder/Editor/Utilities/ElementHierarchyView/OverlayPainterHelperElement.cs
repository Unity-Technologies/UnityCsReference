using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal class OverlayPainterHelperElement : ImmediateModeElement
    {
        BaseOverlayPainter m_Painter;

        public new class UxmlFactory : UxmlFactory<OverlayPainterHelperElement> { }

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
