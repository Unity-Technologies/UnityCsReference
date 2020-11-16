using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal class RepaintOverlayPainter : BaseOverlayPainter
    {
        static readonly Color kRepaintColor = Color.green;
        static readonly float kOverlayFadeOut = 0.01f;
        static readonly float kDefaultRepaintAlpha = 0.2f;

        public void AddOverlay(VisualElement ve)
        {
            OverlayData overlayData = null;
            if (!m_OverlayData.TryGetValue(ve, out overlayData))
            {
                overlayData = new OverlayData(ve, kDefaultRepaintAlpha) { fadeOutRate = kOverlayFadeOut };
                m_OverlayData[ve] = overlayData;
            }
            else
            {
                // Reset alpha
                overlayData.alpha = overlayData.defaultAlpha;
            }
        }

        protected override void DrawOverlayData(OverlayData od)
        {
            DrawRect(od.element.worldBound, kRepaintColor, od.alpha);
            DrawBorder(od.element.worldBound, kRepaintColor, od.alpha * 4);
        }
    }
}
