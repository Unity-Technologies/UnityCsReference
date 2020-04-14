// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements.GraphView;

namespace UnityEditor.Experimental.GraphView
{
    internal class Snapper
    {
        SnapService m_Service;
        LineView m_LineView;
        GraphView m_GraphView;
        bool active { get { return m_Service != null ? m_Service.active : false; } }
        public Snapper()
        {
        }

        internal void BeginSnap(GraphView graphView)
        {
            if (m_Service == null)
            {
                m_Service = new SnapService();
            }
            if (m_LineView == null)
            {
                m_LineView = new LineView();
            }

            m_GraphView = graphView;
            m_GraphView.Add(m_LineView);
            m_LineView.layout = new Rect(0, 0, m_GraphView.layout.width, m_GraphView.layout.height);

            var notSelectedElementRects = GetNotSelectedElementRectsInView();
            m_Service.BeginSnap(notSelectedElementRects);
        }

        internal List<Rect> GetNotSelectedElementRectsInView()
        {
            List<Rect> notSelectedElementRects = new List<Rect>();
            List<GraphElement> ignoreElements = new List<GraphElement>();

            // If dragging a group we need to find all elements inside and disregard them
            // For Groups they derive from Scope in which have containedElements.
            // If we are dragging around a node that are inside a Group,
            // the groups border might adjust and we don't want to snap to that.
            // We also check if there are any GraphElement children of the selected graphelement
            // And add them to the ignore list.
            foreach (GraphElement ge in m_GraphView.selection.OfType<GraphElement>())
            {
                if (ge is Group)
                {
                    Group group = ge as Group;
                    var grpElements = group.containedElements;
                    foreach (GraphElement element in grpElements)
                    {
                        ignoreElements.AddRange(element.Query<GraphElement>("", "graphElement").ToList());
                    }
                }
                else if (ge.GetContainingScope() != null)
                {
                    ignoreElements.Add(ge.GetContainingScope());
                }
                else
                {
                    ignoreElements.Add(ge);
                }

                ignoreElements.AddRange(ge.Query<GraphElement>("", "graphElement").ToList());
            }

            // Consider only the visible nodes.
            Rect rectToFit = m_GraphView.layout;
            // a copy is necessary because Add To selection might cause a SendElementToFront which will change the order.
            List<ISelectable> checkOnlyInViewElements = new List<ISelectable>();
            foreach (GraphElement element in m_GraphView.graphElements.ToList())
            {
                var localSelRect = m_GraphView.ChangeCoordinatesTo(element, rectToFit);
                if (element.Overlaps(localSelRect))
                {
                    checkOnlyInViewElements.Add(element);
                }
            }

            foreach (GraphElement element in checkOnlyInViewElements)
            {
                if (!element.IsSelected(m_GraphView) && (element.capabilities & Capabilities.Snappable) != 0 && !(ignoreElements.Contains(element)))
                {
                    Rect geomtryInContentViewContainerSpace = element.parent.ChangeCoordinatesTo(m_GraphView.contentViewContainer, element.GetPosition());

                    notSelectedElementRects.Add(geomtryInContentViewContainerSpace);
                }
            }

            return notSelectedElementRects;
        }

        internal Rect GetSnappedRect(Rect sourceRect, float scale = 1.0f)
        {
            m_Service.UpdateSnapRects(GetNotSelectedElementRectsInView());
            List<SnapResult> results;
            Rect snappedRect = m_Service.GetSnappedRect(sourceRect, out results, scale);
            m_LineView.lines.Clear();
            foreach (SnapResult result in results)
            {
                m_LineView.lines.Add(result.indicatorLine);
            }
            m_LineView.MarkDirtyRepaint();
            return snappedRect;
        }

        internal void EndSnap(GraphView graphView)
        {
            m_LineView.lines.Clear();
            m_LineView.Clear();
            m_LineView.RemoveFromHierarchy();
            m_Service.EndSnap();
        }

        internal void ClearSnapLines()
        {
            m_LineView.lines.Clear();
            m_LineView.MarkDirtyRepaint();
        }
    }
}
