// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor
{
    internal class UISystemProfilerChart : ProfilerChart
    {
        private EventMarker[] m_Markers;
        private string[] m_MarkerNames;
        public bool showMarkers { get { return ((UISystemChart)m_Chart).showMarkers; } }

        public UISystemProfilerChart(Chart.ChartType type, float dataScale, int seriesCount) : base(ProfilerArea.UIDetails, type, dataScale, seriesCount)
        {
            m_Chart = new UISystemChart();
        }

        public void Update(int firstFrame, int historyLength)
        {
            int count = ProfilerDriver.GetUISystemEventMarkersCount(firstFrame, historyLength);
            if (count == 0)
                return;
            m_Markers = new EventMarker[count];
            m_MarkerNames = new string[count];
            ProfilerDriver.GetUISystemEventMarkersBatch(firstFrame, historyLength, m_Markers, m_MarkerNames);
        }

        public override int DoChartGUI(int currentFrame, ProfilerArea currentArea, out Chart.ChartAction action)
        {
            int res = base.DoChartGUI(currentFrame, currentArea, out action);
            if (m_Markers != null && showMarkers)
            {
                Rect rect = GUILayoutUtility.GetLastRect();
                rect.xMin += Chart.kSideWidth;
                for (int index = 0; index < m_Markers.Length; index++)
                {
                    var marker = m_Markers[index];
                    Color c = ProfilerColors.colors[(uint)marker.objectInstanceId % ProfilerColors.colors.Length];
                    Chart.DrawVerticalLine(marker.frame, m_Data, rect, c.AlphaMultiplied(0.3f), c.AlphaMultiplied(0.4f), 1.0f);
                }
                DrawMarkerLabels(m_Data, rect, m_Markers, m_MarkerNames);
            }
            return res;
        }

        private void DrawMarkerLabels(ChartData cdata, Rect r, EventMarker[] markers, string[] markerNames)
        {
            Color cc = GUI.contentColor;
            int length = cdata.NumberOfFrames;
            float frameWidth = r.width / length;

            const int labelPositions = 12;
            int maxCount = (int)(r.height / labelPositions);
            if (maxCount != 0)
            {
                Dictionary<int, int> markerLastFrame = new Dictionary<int, int>();
                for (int s = 0; s < markers.Length; ++s)
                {
                    int lframe;
                    int frame = markers[s].frame;
                    if (!markerLastFrame.TryGetValue(markers[s].nameOffset, out lframe) || lframe != frame - 1 || lframe < cdata.firstFrame)
                    {
                        frame -= cdata.firstFrame;
                        if (frame >= 0)
                        {
                            float xpos = r.x + frameWidth * frame;
                            // color the label slightly (half way between line color and white)
                            Color clr = ProfilerColors.colors[(uint)markers[s].objectInstanceId % ProfilerColors.colors.Length];
                            GUI.contentColor = (clr + Color.white) * 0.5f;

                            const float offset = -1;
                            Chart.DoLabel(xpos + offset, r.y + r.height - (s % maxCount + 1) * labelPositions, markerNames[s], 0);
                        }
                    }
                    markerLastFrame[markers[s].nameOffset] = markers[s].frame;
                }
            }
            GUI.contentColor = cc;
        }
    }

    internal class UISystemChart : Chart
    {
        public bool showMarkers;

        protected internal override void LabelDraggerDrag(int chartControlID, ChartType chartType, ChartData cdata, Rect r, bool active)
        {
            Event evt = Event.current;
            if (evt.type == EventType.MouseDown)
            {
                var togglePos = GetToggleRect(r, cdata.charts.Length);
                if (togglePos.Contains(evt.mousePosition))
                {
                    showMarkers = !showMarkers;
                    SaveChartsSettingsEnabled(cdata);
                    evt.Use();
                }
            }
            base.LabelDraggerDrag(chartControlID, chartType, cdata, r, active);
        }

        protected override void DrawLabelDragger(ChartType type, Rect r, ChartData cdata)
        {
            base.DrawLabelDragger(type, r, cdata);
            GUI.backgroundColor = showMarkers ? Color.white : Color.black;
            DrawSubLabel(r, cdata.charts.Length, "Markers");
            GUI.backgroundColor = Color.white;
        }
    }
}
