// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements
{
    internal class PanelDebug : IPanelDebug
    {
        private HashSet<IPanelDebugger> m_Debuggers = new HashSet<IPanelDebugger>();
        private List<RepaintData> m_RepaintDatas = new List<RepaintData>();
        private IPanel m_Panel;
        private bool m_IsShowingOverlay = false;

        public bool showOverlay
        {
            get
            {
                foreach (var debugger in m_Debuggers)
                {
                    if (debugger.showOverlay)
                        return true;
                }

                return false;
            }
        }

        public uint highlightedElement { get; private set; } = 0;

        public PanelDebug(IPanel panel)
        {
            m_Panel = panel;
        }

        public void AttachDebugger(IPanelDebugger debugger)
        {
            if (m_Debuggers.Add(debugger))
            {
                debugger.panelDebug = this;
                m_Panel.visualTree.MarkDirtyRepaint();
            }
        }

        public void DetachDebugger(IPanelDebugger debugger)
        {
            debugger.panelDebug = null;
            m_Debuggers.Remove(debugger);
            m_Panel.visualTree.MarkDirtyRepaint();
        }

        public void Refresh()
        {
            if (showOverlay)
            {
                RecordRepaintData(m_Panel.visualTree);
                DrawRepaintData();
                m_IsShowingOverlay = true;
            }
            else if (m_IsShowingOverlay)
            {
                // Clear the overlay
                m_IsShowingOverlay = false;
                m_Panel.visualTree.MarkDirtyRepaint();
            }

            foreach (var debugger in m_Debuggers)
            {
                debugger.Refresh();
            }
        }

        public bool InterceptEvents(Event ev)
        {
            bool intercepted = false;
            foreach (var debugger in m_Debuggers)
            {
                intercepted |= debugger.InterceptEvents(ev);
            }

            return intercepted;
        }

        public void SetHighlightElement(VisualElement ve)
        {
            var controlId = ve != null ? ve.controlid : 0;
            if (highlightedElement != controlId)
            {
                highlightedElement = controlId;
                m_Panel.visualTree.MarkDirtyRepaint();
            }
        }

        private void RecordRepaintData(VisualElement ve)
        {
            m_RepaintDatas.Add(new RepaintData(ve.controlid,
                ve.worldBound,
                Color.HSVToRGB(ve.controlid * 11 % 32 / 32.0f, .6f, 1.0f)));

            for (int i = 0; i < ve.shadow.childCount; i++)
            {
                var child = ve.shadow[i];
                RecordRepaintData(child);
            }
        }

        public void DrawRepaintData()
        {
            RepaintData onTopElement = null;
            foreach (var repaintData in m_RepaintDatas)
            {
                var color = repaintData.color;
                if (highlightedElement != 0)
                    if (highlightedElement != repaintData.controlId)
                    {
                        color = Color.gray;
                    }
                    else
                    {
                        onTopElement = repaintData;
                        continue;
                    }
                DrawRect(repaintData.contentRect, color);
            }

            m_RepaintDatas.Clear();
            if (onTopElement != null)
                DrawRect(onTopElement.contentRect, onTopElement.color);
        }

        public static void DrawRect(Rect sp, Color c)
        {
            sp.xMin++;
            sp.xMax--;
            sp.yMin++;
            sp.yMax--;

            HandleUtility.ApplyWireMaterial();
            GL.PushMatrix();
            GL.Begin(GL.LINES);

            GL.Color(c);
            GL.Vertex3(sp.xMin, sp.yMin, 0);
            GL.Color(c);
            GL.Vertex3(sp.xMax, sp.yMin, 0);

            GL.Color(c);
            GL.Vertex3(sp.xMax, sp.yMin, 0);
            GL.Color(c);
            GL.Vertex3(sp.xMax, sp.yMax, 0);

            GL.Color(c);
            GL.Vertex3(sp.xMax, sp.yMax, 0);
            GL.Color(c);
            GL.Vertex3(sp.xMin, sp.yMax, 0);

            GL.Color(c);
            GL.Vertex3(sp.xMin, sp.yMax, 0);
            GL.Color(c);
            GL.Vertex3(sp.xMin, sp.yMin, 0);
            GL.End();
            GL.PopMatrix();
        }

        private class RepaintData
        {
            public readonly Color color;
            public readonly Rect contentRect;
            public readonly uint controlId;

            public RepaintData(uint controlId, Rect contentRect, Color color)
            {
                this.contentRect = contentRect;
                this.color = color;
                this.controlId = controlId;
            }
        }
    }
}
