// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Profiling.Editor
{
    internal class UISystemProfilerChartViewController
    {
        const string k_UxmlIdentifier_EventLine = "profiler-chart-view__chart__ui-event-line";
        const string k_UxmlIdentifier_MarkerLabel = "profiler-chart-view__chart__ui-marker-label";

        readonly ProfilerModule m_Module;
        readonly VisualElement m_Chart;
        readonly UISystemProfilerModel m_Model;

        VisualElement m_LinesGroup;
        List<VisualElement> m_LinesCache;

        VisualElement m_LabelsGroup;
        List<Label> m_LabelsCache;

        bool eventsShown = false;

        public UISystemProfilerChartViewController(ProfilerModule module, VisualElement chart, UISystemProfilerModel model)
        {
            m_Module = module;
            m_Chart = chart;
            m_Model = model;

            m_LinesGroup = new VisualElement { name = "LinesGroup" };
            m_LinesGroup.StretchToParentSize();
            m_Chart.parent.Insert(0, m_LinesGroup);
            m_LinesGroup.SendToBack();
            m_LinesCache = new List<VisualElement>();

            m_LabelsGroup = new VisualElement { name = "LabelsGroup" };
            m_LabelsGroup.StretchToParentSize();
            m_Chart.Add(m_LabelsGroup);
            m_LabelsCache = new List<Label>();
            m_LabelsGroup.RegisterCallback<GeometryChangedEvent>(LayoutChange);
        }

        public void Update()
        {
            if (eventsShown != m_Model.ShowEvents)
            {
                eventsShown = m_Model.ShowEvents;
                m_LinesGroup.Clear();
                m_LabelsGroup.Clear();
                m_LinesCache.Clear();
                m_LabelsCache.Clear();
            }

            if (!m_Module.active)
                return;
            if (!m_Model.ShowEvents)
                return;

            UpdateLines();
            UpdateLabels(m_LabelsGroup.contentRect);
        }

        void LayoutChange(GeometryChangedEvent evt)
        {
            if (!m_Module.active)
                return;
            if (!m_Model.ShowEvents)
                return;

            // Labels positioning aren't relative, so we need to
            // update them when layout change
            UpdateLabels(evt.newRect);
        }


        void UpdateLines()
        {
            int eventIndex = 0;
            if (m_Model.Events != null)
            {
                for (; eventIndex < m_Model.Events.Length; eventIndex++)
                {
                    var markerEvt = m_Model.Events[eventIndex];
                    VisualElement markerLine;
                    if (eventIndex < m_LinesCache.Count)
                    {
                        markerLine = m_LinesCache[eventIndex];
                    }
                    else
                    {
                        markerLine = new VisualElement();
                        markerLine.AddToClassList(k_UxmlIdentifier_EventLine);
                        m_LinesGroup.Add(markerLine);
                        m_LinesCache.Add(markerLine);
                    }

                    var pos = (float)(markerEvt.frame - m_Model.DomainOffset) / m_Model.DomainSize;
                    markerLine.style.left = new Length(100.0f * pos, LengthUnit.Percent);
                    markerLine.style.width = new Length(1.0f, LengthUnit.Pixel);
                    markerLine.style.backgroundColor = m_Model.EventsColor;
                    markerLine.style.visibility = Visibility.Visible;
                }
            }

            // Hide leftovers
            for (; eventIndex < m_LinesCache.Count; eventIndex++)
            {
                var markerLine = m_LinesCache[eventIndex];
                markerLine.style.left = 0;
                markerLine.style.visibility = Visibility.Hidden;
            }
        }

        void UpdateLabels(Rect viewRect)
        {
            var domainSize = m_Model.DomainSize;
            var oneFrameWidth = viewRect.width / domainSize;

            const int labelPositions = 16;
            int maxCount = (int)(viewRect.height / labelPositions);
            int totalLabels = 0;
            if ((m_Model.Events != null) && (maxCount != 0))
            {
                var markerLastFrame = new Dictionary<int, int>();
                for (int s = 0; s < m_Model.Events.Length; ++s)
                {
                    int lframe;
                    int frame = m_Model.Events[s].frame;
                    if (!markerLastFrame.TryGetValue(m_Model.Events[s].nameOffset, out lframe) || lframe != frame - 1 || lframe < m_Model.DomainOffset)
                    {
                        frame -= m_Model.DomainOffset;
                        if (frame >= 0)
                        {
                            float xpos = viewRect.x + oneFrameWidth * frame;

                            Label labelElem;
                            if (totalLabels < m_LabelsCache.Count)
                            {
                                labelElem = m_LabelsCache[totalLabels];
                            }
                            else
                            {
                                labelElem = new Label();
                                labelElem.AddToClassList(k_UxmlIdentifier_MarkerLabel);
                                m_LabelsGroup.Add(labelElem);
                                m_LabelsCache.Add(labelElem);
                            }

                            totalLabels++;

                            labelElem.style.visibility = Visibility.Visible;
                            labelElem.style.left = xpos;
                            labelElem.style.top = viewRect.y + viewRect.height - (s % maxCount + 1) * labelPositions;
                            labelElem.style.color = m_Model.EventsColor;
                            labelElem.text = m_Model.MarkerNames[s];
                        }
                    }

                    markerLastFrame[m_Model.Events[s].nameOffset] = m_Model.Events[s].frame;
                }
            }

            for (int i = totalLabels; i < m_LabelsCache.Count; i++)
            {
                var labelElem = m_LabelsCache[i];
                labelElem.style.left = 0;
                labelElem.style.top = 0;
                labelElem.style.visibility = Visibility.Hidden;
            }
        }
    }
}
