// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    [Flags]
    enum OverlayContent
    {
        ContentBox = 1 << 0,
        PaddingBox = 1 << 1,
        BorderBox = 1 << 2,
        MarginBox = 1 << 3,
        AllBoxes = ContentBox | PaddingBox | BorderBox | MarginBox,
        Outline = 1 << 4,
    }

    struct OverlayData(VisualElement ve, float alpha)
    {
        public VisualElement Element = ve;
        public float Alpha = alpha;
        public OverlayContent Content = OverlayContent.AllBoxes;
    }

    class HighlightOverlayPainter2D : Manipulator
    {
        static readonly float k_DefaultHighlightAlpha = 0.4f;
        static readonly Color k_HighlightOutlineColor = new (1.0f, 1.0f, 1.0f);
        static readonly Color k_HighlightContentColor = new (0.1f, 0.6f, 0.9f);
        static readonly Color k_HighlightPaddingColor = new (0.1f, 0.9f, 0.1f);
        static readonly Color k_HighlightBorderColor = new (1.0f, 1.0f, 0.4f);
        static readonly Color k_HighlightMarginColor = new (1.0f, 0.6f, 0.0f);

        readonly Dictionary<VisualElement, OverlayData> m_OverlayData = new();
        internal int OverlayCount => m_OverlayData.Count;

        float m_ZoomScale;

        public float ZoomScale
        {
            get => m_ZoomScale;
            set
            {
                if (Mathf.Approximately(m_ZoomScale, value))
                    return;
                m_ZoomScale = value;
                target?.MarkDirtyRepaint();
            }
        }

        public HighlightOverlayPainter2D(VisualElement overlayElement)
        {
            target = overlayElement;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.generateVisualContent += Draw;
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.generateVisualContent -= Draw;
        }

        public void AddOverlay(VisualElement ve, OverlayContent content = OverlayContent.AllBoxes)
        {
            if (!m_OverlayData.TryGetValue(ve, out var data))
            {
                data = new OverlayData(ve, k_DefaultHighlightAlpha);
                m_OverlayData[ve] = data;
            }

            data.Content = content;
            m_OverlayData[ve] = data;
            target?.MarkDirtyRepaint();
        }

        public void ClearOverlay()
        {
            if (m_OverlayData.Count == 0)
                return;

            m_OverlayData.Clear();
            target?.MarkDirtyRepaint();
        }

        public void Draw(MeshGenerationContext mgc)
        {
            var painter = mgc.painter2D;
            foreach (var kvp in m_OverlayData)
                DrawHighlights(painter, kvp.Value);
        }

        static void DrawHighlights(Painter2D painter, OverlayData od)
        {
            var ve = od.Element;

            Span<Rect> paddingRect = stackalloc Rect[4];
            Span<Rect> borderRect = stackalloc Rect[4];
            Span<Rect> marginRect = stackalloc Rect[4];

            FillHighlightRects(ve, out var contentRect, ref paddingRect, ref borderRect, ref marginRect);

            if ((od.Content & OverlayContent.ContentBox) != 0)
                DrawOBB(painter, ve, contentRect, k_HighlightContentColor, od.Alpha);

            if ((od.Content & OverlayContent.PaddingBox) != 0)
                foreach (var r in paddingRect)
                    DrawOBB(painter, ve, r, k_HighlightPaddingColor, od.Alpha);

            if ((od.Content & OverlayContent.BorderBox) != 0)
                foreach (var r in borderRect)
                    DrawOBB(painter, ve, r, k_HighlightBorderColor, od.Alpha);

            if ((od.Content & OverlayContent.MarginBox) != 0)
                foreach (var r in marginRect)
                    DrawOBB(painter, ve, r, k_HighlightMarginColor, od.Alpha);


            if ((od.Content & OverlayContent.Outline) != 0)
            {
                var style = ve.resolvedStyle;
                var outlineRect = new Rect(-style.marginLeft, -style.marginTop,
                    ve.layout.width + style.marginLeft + style.marginRight,
                    ve.layout.height + style.marginTop + style.marginBottom);
                DrawOBB(painter, ve, outlineRect, k_HighlightOutlineColor, 1.0f, false);
            }

        }

        // Fills the content/padding/border/margin rects in the element's local coordinate space.
        // The border box occupies Rect(0, 0, layout.width, layout.height) in local space;
        // style values are already in logical pixels and need no ZoomScale adjustment here —
        // LocalToWorld (called per-corner in DrawOBB) applies the full world transform including zoom.
        static void FillHighlightRects(VisualElement ve,
            out Rect contentRect,
            ref Span<Rect> paddingRect,
            ref Span<Rect> borderRect,
            ref Span<Rect> marginRect)
        {
            var style = ve.resolvedStyle;
            var w = ve.layout.width;
            var h = ve.layout.height;

            var paddingLeft   = style.paddingLeft;
            var paddingRight  = style.paddingRight;
            var paddingTop    = style.paddingTop;
            var paddingBottom = style.paddingBottom;

            var borderLeft   = style.borderLeftWidth;
            var borderRight  = style.borderRightWidth;
            var borderTop    = style.borderTopWidth;
            var borderBottom = style.borderBottomWidth;

            var marginLeft   = style.marginLeft;
            var marginRight  = style.marginRight;
            var marginTop    = style.marginTop;
            var marginBottom = style.marginBottom;

            // Content rect: border box inset by border widths and padding
            contentRect = new Rect(
                borderLeft + paddingLeft,
                borderTop + paddingTop,
                w - borderLeft - borderRight - paddingLeft - paddingRight,
                h - borderTop - borderBottom - paddingTop - paddingBottom);

            // Padding strips (between content and border)
            paddingRect[0] = new Rect(borderLeft, contentRect.yMin, paddingLeft, contentRect.height);
            paddingRect[1] = new Rect(contentRect.xMax, contentRect.yMin, paddingRight, contentRect.height);
            paddingRect[2] = new Rect(borderLeft, borderTop, w - borderLeft - borderRight, paddingTop);
            paddingRect[3] = new Rect(borderLeft, contentRect.yMax, w - borderLeft - borderRight, paddingBottom);

            // Border strips (between border box edge and padding box)
            borderRect[0] = new Rect(0, borderTop, borderLeft, h - borderTop - borderBottom);
            borderRect[1] = new Rect(w - borderRight, borderTop, borderRight, h - borderTop - borderBottom);
            borderRect[2] = new Rect(0, 0, w, borderTop);
            borderRect[3] = new Rect(0, h - borderBottom, w, borderBottom);

            // Margin strips (outside the border box)
            marginRect[0] = new Rect(-marginLeft, 0, marginLeft, h);
            marginRect[1] = new Rect(w, 0, marginRight, h);
            marginRect[2] = new Rect(-marginLeft, -marginTop, w + marginLeft + marginRight, marginTop);
            marginRect[3] = new Rect(-marginLeft, h, w + marginLeft + marginRight, marginBottom);
        }

        // Transforms each corner of localRect from the element's local space to world/overlay space
        // via LocalToWorld, then draws the resulting OBB. This preserves any rotation or scale
        // applied to the element, unlike worldBound which collapses the OBB to an AABB.
        static void DrawOBB(Painter2D painter, VisualElement ve, Rect localRect, Color color, float alpha, bool fill = true)
        {
            if (localRect.width <= 0 || localRect.height <= 0)
                return;

            var pixelsPerPoint = ((Panel)ve.panel)?.pixelsPerPoint ?? 1.0f;

            var tl = ve.LocalToWorld(new Vector2(localRect.xMin, localRect.yMin)) / pixelsPerPoint;
            var tr = ve.LocalToWorld(new Vector2(localRect.xMax, localRect.yMin)) / pixelsPerPoint;
            var br = ve.LocalToWorld(new Vector2(localRect.xMax, localRect.yMax)) / pixelsPerPoint;
            var bl = ve.LocalToWorld(new Vector2(localRect.xMin, localRect.yMax)) / pixelsPerPoint;

            color.a = alpha;
            painter.lineWidth = 2;
            if (fill)
                painter.fillColor = color;
            else
                painter.strokeColor = color;
            painter.BeginPath();
            painter.MoveTo(tl);
            painter.LineTo(tr);
            painter.LineTo(br);
            painter.LineTo(bl);
            painter.ClosePath();
            if (fill)
                painter.Fill();
            else
                painter.Stroke();
        }
    }
}
