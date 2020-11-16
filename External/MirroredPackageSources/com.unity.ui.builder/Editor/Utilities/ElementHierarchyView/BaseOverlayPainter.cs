using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
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
            this.fadeOutRate = 0;
        }

        public VisualElement element;
        public float alpha;
        public float defaultAlpha;
        public float fadeOutRate;
        public OverlayContent content;
    }

    internal abstract class BaseOverlayPainter
    {
        protected Dictionary<VisualElement, OverlayData> m_OverlayData = new Dictionary<VisualElement, OverlayData>();
        protected List<VisualElement> m_CleanUpOverlay = new List<VisualElement>();

        public void Draw()
        {
            Draw(GUIClip.topmostRect);
        }

        public virtual void Draw(Rect clipRect)
        {
            PaintAllOverlay(clipRect);

            foreach (var ve in m_CleanUpOverlay)
            {
                m_OverlayData.Remove(ve);
            }
            m_CleanUpOverlay.Clear();
        }

        void PaintAllOverlay(Rect clipRect)
        {
            using (new GUIClip.ParentClipScope(Matrix4x4.identity, clipRect))
            {
                HandleUtility.ApplyWireMaterial();
                GL.PushMatrix();

                foreach (var kvp in m_OverlayData)
                {
                    var overlayData = kvp.Value;
                    overlayData.alpha -= overlayData.fadeOutRate;

                    DrawOverlayData(overlayData);
                    if (overlayData.alpha < Mathf.Epsilon)
                    {
                        m_CleanUpOverlay.Add(kvp.Key);
                    }
                }

                GL.PopMatrix();
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

        protected abstract void DrawOverlayData(OverlayData overlayData);

        protected void DrawRect(Rect rect, Color color, float alpha)
        {
            float x0 = rect.x;
            float x3 = rect.xMax;
            float y0 = rect.yMax;
            float y3 = rect.y;

            color.a = alpha;

            GL.Begin(GL.TRIANGLES);
            GL.Color(color);
            GL.Vertex3(x0, y0, 0);
            GL.Vertex3(x3, y0, 0);
            GL.Vertex3(x0, y3, 0);

            GL.Vertex3(x3, y0, 0);
            GL.Vertex3(x3, y3, 0);
            GL.Vertex3(x0, y3, 0);
            GL.End();
        }

        protected void DrawBorder(Rect rect, Color color, float alpha)
        {
            rect.xMin++;
            rect.xMax--;
            rect.yMin++;
            rect.yMax--;

            color.a = alpha;

            GL.Begin(GL.LINES);
            GL.Color(color);
            GL.Vertex3(rect.xMin, rect.yMin, 0);
            GL.Vertex3(rect.xMax, rect.yMin, 0);

            GL.Vertex3(rect.xMax, rect.yMin, 0);
            GL.Vertex3(rect.xMax, rect.yMax, 0);

            GL.Vertex3(rect.xMax, rect.yMax, 0);
            GL.Vertex3(rect.xMin, rect.yMax, 0);

            GL.Vertex3(rect.xMin, rect.yMax, 0);
            GL.Vertex3(rect.xMin, rect.yMin, 0);
            GL.End();
        }
    }
}
