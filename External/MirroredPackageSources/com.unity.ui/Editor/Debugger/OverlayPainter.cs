using System;
using System.Collections.Generic;
using UnityEngine.UIElements.Experimental;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements.Debugger
{
    [Flags]
    internal enum OverlayContent
    {
        Content = 1 << 0,
        Padding = 1 << 1,
        Border = 1 << 2,
        Margin = 1 << 3,
        All = Content | Padding | Border | Margin
    }

    internal class OverlayData
    {
        public OverlayData(VisualElement ve, float alpha)
        {
            this.element = ve;
            this.alpha = alpha;
            this.defaultAlpha = alpha;
        }

        public VisualElement element;
        public float alpha;
        public float defaultAlpha;
        public OverlayContent content;
        private ValueAnimation<float> m_animation;

        public void StartFadeOutAnimation(VisualElement container, int duration)
        {
            if (m_animation != null)
            {
                m_animation.Stop();
                m_animation.durationMs = duration;
                m_animation.Start();
            }
            else
            {
                m_animation = container.experimental.animation.Start(defaultAlpha, 0, duration, (ve, value) =>
                {
                    alpha = value;
                    ve.MarkDirtyRepaint();
                }).Ease(Easing.OutCubic).KeepAlive();
            }
        }
    }

    internal abstract class BaseOverlayPainter
    {
        protected Dictionary<VisualElement, OverlayData> m_OverlayData = new Dictionary<VisualElement, OverlayData>();
        protected List<VisualElement> m_CleanUpOverlay = new List<VisualElement>();

        public virtual void Draw(MeshGenerationContext mgc)
        {
            PaintAllOverlay(mgc);

            foreach (var ve in m_CleanUpOverlay)
            {
                m_OverlayData.Remove(ve);
            }
            m_CleanUpOverlay.Clear();
        }

        private void PaintAllOverlay(MeshGenerationContext mgc)
        {
            foreach (var kvp in m_OverlayData)
            {
                var overlayData = kvp.Value;

                DrawOverlayData(mgc, overlayData);
                if (overlayData.alpha < Mathf.Epsilon)
                {
                    m_CleanUpOverlay.Add(kvp.Key);
                }
            }
        }

        public int overlayCount
        {
            get { return m_OverlayData.Count; }
        }

        public void ClearOverlay()
        {
            m_OverlayData.Clear();
        }

        protected abstract void DrawOverlayData(MeshGenerationContext mgc, OverlayData overlayData);

        protected void DrawRect(MeshGenerationContext mgc, Rect rect, Color color, float alpha)
        {
            if (mgc == null)
                throw new NullReferenceException("The MeshGenerationContext is null");

            color.a = alpha;

            var rectParams = MeshGenerationContextUtils.RectangleParams.MakeSolid(rect, color, mgc.visualElement.panel.contextType);
            mgc.Rectangle(rectParams);
        }

        protected void DrawBorder(MeshGenerationContext mgc, Rect rect, Color color, float alpha)
        {
            if (mgc == null)
                throw new NullReferenceException("The MeshGenerationContext is null");

            color.a = alpha;
            rect.xMin++;
            rect.xMax--;
            rect.yMin++;
            rect.yMax--;
            var width = rect.xMax - rect.xMin;
            var height = rect.yMax - rect.yMin;

            var topRect = new Rect(rect.xMin, rect.yMin, width, 1);
            var bottomRect = new Rect(rect.xMin, rect.yMax, width, 1);
            var rightRect = new Rect(rect.xMax, rect.yMin, 1, height);
            var lefRect = new Rect(rect.xMin, rect.yMin, 1, height);

            var rectParams = MeshGenerationContextUtils.RectangleParams.MakeSolid(topRect, color, mgc.visualElement.panel.contextType);
            mgc.Rectangle(rectParams);

            rectParams = MeshGenerationContextUtils.RectangleParams.MakeSolid(bottomRect, color, mgc.visualElement.panel.contextType);
            mgc.Rectangle(rectParams);

            rectParams = MeshGenerationContextUtils.RectangleParams.MakeSolid(rightRect, color, mgc.visualElement.panel.contextType);
            mgc.Rectangle(rectParams);

            rectParams = MeshGenerationContextUtils.RectangleParams.MakeSolid(lefRect, color, mgc.visualElement.panel.contextType);
            mgc.Rectangle(rectParams);
        }
    }

    internal class HighlightOverlayPainter : BaseOverlayPainter
    {
        private const float kDefaultHighlightAlpha = 0.4f;
        private static readonly Color kHighlightContentColor = new Color(0.1f, 0.6f, 0.9f);
        private static readonly Color kHighlightPaddingColor = new Color(0.1f, 0.9f, 0.1f);
        private static readonly Color kHighlightBorderColor = new Color(1.0f, 1.0f, 0.4f);
        private static readonly Color kHighlightMarginColor = new Color(1.0f, 0.6f, 0.0f);

        private Rect[] m_MarginRects = new Rect[4];
        private Rect[] m_BorderRects = new Rect[4];
        private Rect[] m_PaddingRects = new Rect[4];

        public void AddOverlay(VisualElement ve, OverlayContent content = OverlayContent.All, float alpha = kDefaultHighlightAlpha)
        {
            OverlayData overlayData = null;
            if (!m_OverlayData.TryGetValue(ve, out overlayData))
            {
                overlayData = new OverlayData(ve, alpha);
                m_OverlayData[ve] = overlayData;
            }

            overlayData.content = content;
        }

        protected override void DrawOverlayData(MeshGenerationContext mgc, OverlayData od)
        {
            DrawHighlights(mgc, od);
        }

        private void DrawHighlights(MeshGenerationContext mgc, OverlayData od)
        {
            var ve = od.element;
            Rect contentRect = ve.LocalToWorld(ve.contentRect);

            FillHighlightRects(od.element);

            var contentFlag = od.content;
            if ((contentFlag & OverlayContent.Content) == OverlayContent.Content)
            {
                DrawRect(mgc, contentRect, kHighlightContentColor, od.alpha);
            }

            if ((contentFlag & OverlayContent.Padding) == OverlayContent.Padding)
            {
                for (int i = 0; i < 4; i++)
                {
                    DrawRect(mgc, m_PaddingRects[i], kHighlightPaddingColor, od.alpha);
                }
            }

            if ((contentFlag & OverlayContent.Border) == OverlayContent.Border)
            {
                for (int i = 0; i < 4; i++)
                {
                    DrawRect(mgc, m_BorderRects[i], kHighlightBorderColor, od.alpha);
                }
            }

            if ((contentFlag & OverlayContent.Margin) == OverlayContent.Margin)
            {
                for (int i = 0; i < 4; i++)
                {
                    DrawRect(mgc, m_MarginRects[i], kHighlightMarginColor, od.alpha);
                }
            }
        }

        private void FillHighlightRects(VisualElement ve)
        {
            var style = ve.resolvedStyle;
            Rect contentRect = ve.LocalToWorld(ve.contentRect);

            // Paddings
            float paddingLeft = style.paddingLeft;
            float paddingRight = style.paddingRight;
            float paddingBottom = style.paddingBottom;
            float paddingTop = style.paddingTop;

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
            float borderLeft = style.borderLeftWidth;
            float borderRight = style.borderRightWidth;
            float borderBottom = style.borderBottomWidth;
            float borderTop = style.borderTopWidth;

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
            float marginLeft = style.marginLeft;
            float marginRight = style.marginRight;
            float marginBotton = style.marginBottom;
            float marginTop = style.marginTop;

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

    internal class RepaintOverlayPainter : BaseOverlayPainter
    {
        private static readonly Color kRepaintColor = Color.green;
        private static readonly float kDefaultAlpha = 1.0f;
        private static readonly int kOverlayFadeOutDuration = 500;

        public void AddOverlay(VisualElement ve, VisualElement debugContainer)
        {
            if (debugContainer == null)
                throw new ArgumentNullException("debugContainer");
            if (ve == null)
                throw new ArgumentNullException("ve");

            OverlayData overlayData = null;
            if (!m_OverlayData.TryGetValue(ve, out overlayData))
            {
                overlayData = new OverlayData(ve, kDefaultAlpha);
                m_OverlayData[ve] = overlayData;
            }
            overlayData.StartFadeOutAnimation(debugContainer, kOverlayFadeOutDuration);
        }

        protected override void DrawOverlayData(MeshGenerationContext mgc, OverlayData od)
        {
            DrawRect(mgc, od.element.worldBound, kRepaintColor, od.alpha);
            DrawBorder(mgc, od.element.worldBound, kRepaintColor, od.alpha * 4);
        }
    }

    internal class LayoutOverlayPainter : BaseOverlayPainter
    {
        private static readonly float kDefaultAlpha = 1.0f;
        private static readonly Color kBoundColor = Color.gray;
        private static readonly Color kSelectedBoundColor = Color.green;

        public VisualElement selectedElement;

        public void AddOverlay(VisualElement ve)
        {
            if (ve == null)
                throw new ArgumentNullException("ve");

            OverlayData overlayData = null;
            if (!m_OverlayData.TryGetValue(ve, out overlayData))
            {
                overlayData = new OverlayData(ve, kDefaultAlpha);
                m_OverlayData[ve] = overlayData;
            }
        }

        public override void Draw(MeshGenerationContext mgc)
        {
            base.Draw(mgc);

            if (selectedElement != null)
                DrawBorder(mgc, selectedElement.worldBound, kSelectedBoundColor, kDefaultAlpha);
        }

        protected override void DrawOverlayData(MeshGenerationContext mgc, OverlayData od)
        {
            DrawBorder(mgc, od.element.worldBound, kBoundColor, od.alpha);
        }
    }
}
