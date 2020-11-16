using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal class LayoutOverlayPainter : BaseOverlayPainter
    {
        static readonly float kDefaultAlpha = 1.0f;
        static readonly Color kBoundColor = Color.gray;
        static readonly Color kSelectedBoundColor = Color.green;

        public VisualElement selectedElement;

        public LayoutOverlayPainter()
        {
            selectedElement = null;
        }

        public void AddOverlay(VisualElement ve)
        {
            OverlayData overlayData = null;
            if (!m_OverlayData.TryGetValue(ve, out overlayData))
            {
                overlayData = new OverlayData(ve, kDefaultAlpha);
                m_OverlayData[ve] = overlayData;
            }
        }

        public override void Draw(Rect clipRect)
        {
            base.Draw(clipRect);

            if (selectedElement != null)
                DrawBorder(selectedElement.worldBound, kSelectedBoundColor, kDefaultAlpha);
        }

        protected override void DrawOverlayData(OverlayData od)
        {
            DrawBorder(od.element.worldBound, kBoundColor, od.alpha);
        }
    }
}
