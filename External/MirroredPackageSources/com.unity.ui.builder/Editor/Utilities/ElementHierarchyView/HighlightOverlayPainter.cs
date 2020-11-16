using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal class HighlightOverlayPainter : BaseOverlayPainter
    {
        static readonly float kDefaultHighlightAlpha = 0.4f;
        static readonly Color kHighlightContentColor = new Color(0.1f, 0.6f, 0.9f);
        static readonly Color kHighlightPaddingColor = new Color(0.1f, 0.9f, 0.1f);
        static readonly Color kHighlightBorderColor = new Color(1.0f, 1.0f, 0.4f);
        static readonly Color kHighlightMarginColor = new Color(1.0f, 0.6f, 0.0f);

        Rect[] m_MarginRects = new Rect[4];
        Rect[] m_BorderRects = new Rect[4];
        Rect[] m_PaddingRects = new Rect[4];

        public void AddOverlay(VisualElement ve, OverlayContent content = OverlayContent.All)
        {
            OverlayData overlayData = null;
            if (!m_OverlayData.TryGetValue(ve, out overlayData))
            {
                overlayData = new OverlayData(ve, kDefaultHighlightAlpha);
                m_OverlayData[ve] = overlayData;
            }

            overlayData.content = content;
        }

        protected override void DrawOverlayData(OverlayData od)
        {
            DrawHighlights(od);
        }

        void DrawHighlights(OverlayData od)
        {
            var ve = od.element;
            Rect contentRect = ve.LocalToWorld(ve.contentRect);

            FillHighlightRects(od.element);

            var contentFlag = od.content;
            if ((contentFlag & OverlayContent.Content) == OverlayContent.Content)
            {
                DrawRect(contentRect, kHighlightContentColor, od.alpha);
            }

            if ((contentFlag & OverlayContent.Padding) == OverlayContent.Padding)
            {
                for (int i = 0; i < 4; i++)
                {
                    DrawRect(m_PaddingRects[i], kHighlightPaddingColor, od.alpha);
                }
            }

            if ((contentFlag & OverlayContent.Border) == OverlayContent.Border)
            {
                for (int i = 0; i < 4; i++)
                {
                    DrawRect(m_BorderRects[i], kHighlightBorderColor, od.alpha);
                }
            }

            if ((contentFlag & OverlayContent.Margin) == OverlayContent.Margin)
            {
                for (int i = 0; i < 4; i++)
                {
                    DrawRect(m_MarginRects[i], kHighlightMarginColor, od.alpha);
                }
            }
        }

        void FillHighlightRects(VisualElement ve)
        {
            var viewport = ve.GetFirstAncestorOfType<BuilderViewport>();

            if (viewport == null)
                return;

            var style = ve.resolvedStyle;
            Rect contentRect = ve.LocalToWorld(ve.contentRect);

            // Paddings
            float paddingLeft = style.paddingLeft * viewport.zoomScale;
            float paddingRight = style.paddingRight * viewport.zoomScale;
            float paddingBottom = style.paddingBottom * viewport.zoomScale;
            float paddingTop = style.paddingTop * viewport.zoomScale;

            Rect paddingLeftRect = Rect.zero;
            Rect paddingRightRect = Rect.zero;
            Rect paddingBottomRect = Rect.zero;
            Rect paddingTopRect = Rect.zero;

            paddingLeftRect = new Rect(contentRect.xMin - paddingLeft, contentRect.yMin,
                paddingLeft, contentRect.height);

            paddingRightRect = new Rect(contentRect.xMax, contentRect.yMin,
                paddingRight, contentRect.height);

            paddingTopRect = new Rect(contentRect.xMin - paddingLeft, contentRect.yMin - paddingTop,
                contentRect.width + paddingLeft + paddingRight, paddingTop);

            paddingBottomRect = new Rect(contentRect.xMin - paddingLeft, contentRect.yMax,
                contentRect.width + paddingLeft + paddingRight, paddingBottom);

            m_PaddingRects[0] = paddingLeftRect;
            m_PaddingRects[1] = paddingRightRect;
            m_PaddingRects[2] = paddingTopRect;
            m_PaddingRects[3] = paddingBottomRect;

            // Borders
            float borderLeft = style.borderLeftWidth * viewport.zoomScale;
            float borderRight = style.borderRightWidth * viewport.zoomScale;
            float borderBottom = style.borderBottomWidth * viewport.zoomScale;
            float borderTop = style.borderTopWidth * viewport.zoomScale;

            Rect borderLeftRect = Rect.zero;
            Rect borderRightRect = Rect.zero;
            Rect borderBottomRect = Rect.zero;
            Rect borderTopRect = Rect.zero;

            borderLeftRect = new Rect(paddingLeftRect.xMin - borderLeft, paddingTopRect.yMin,
                borderLeft, paddingLeftRect.height + paddingBottomRect.height + paddingTopRect.height);

            borderRightRect = new Rect(paddingRightRect.xMax, paddingTopRect.yMin,
                borderRight, paddingRightRect.height + paddingBottomRect.height + paddingTopRect.height);

            borderTopRect = new Rect(paddingTopRect.xMin - borderLeft, paddingTopRect.yMin - borderTop,
                paddingTopRect.width + borderLeft + borderRight, borderTop);

            borderBottomRect = new Rect(paddingBottomRect.xMin - borderLeft, paddingBottomRect.yMax,
                paddingBottomRect.width + borderLeft + borderRight, borderBottom);

            m_BorderRects[0] = borderLeftRect;
            m_BorderRects[1] = borderRightRect;
            m_BorderRects[2] = borderTopRect;
            m_BorderRects[3] = borderBottomRect;

            // Margins
            float marginLeft = style.marginLeft * viewport.zoomScale;
            float marginRight = style.marginRight * viewport.zoomScale;
            float marginBotton = style.marginBottom * viewport.zoomScale;
            float marginTop = style.marginTop * viewport.zoomScale;

            Rect marginLeftRect = Rect.zero;
            Rect marginRightRect = Rect.zero;
            Rect marginBottomRect = Rect.zero;
            Rect marginTopRect = Rect.zero;

            marginLeftRect = new Rect(borderLeftRect.xMin - marginLeft, borderTopRect.yMin,
                marginLeft, borderLeftRect.height + borderBottomRect.height + borderTopRect.height);

            marginRightRect = new Rect(borderRightRect.xMax, borderTopRect.yMin,
                marginRight, borderRightRect.height + borderBottomRect.height + borderTopRect.height);

            marginTopRect = new Rect(borderTopRect.xMin - marginLeft, borderTopRect.yMin - marginTop,
                borderTopRect.width + marginLeft + marginRight, marginTop);

            marginBottomRect = new Rect(borderBottomRect.xMin - marginLeft, borderBottomRect.yMax,
                borderBottomRect.width + marginLeft + marginRight, marginBotton);

            m_MarginRects[0] = marginLeftRect;
            m_MarginRects[1] = marginRightRect;
            m_MarginRects[2] = marginTopRect;
            m_MarginRects[3] = marginBottomRect;
        }
    }
}
