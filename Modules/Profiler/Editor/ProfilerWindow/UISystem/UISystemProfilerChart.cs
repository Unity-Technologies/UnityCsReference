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

        public bool showMarkers = true;

        public UISystemProfilerChart(Chart.ChartType type, float dataScale, int seriesCount) : base(ProfilerArea.UIDetails, type, dataScale, seriesCount)
        {
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

        public override int DoChartGUI(int currentFrame, ProfilerArea currentArea)
        {
            int res = base.DoChartGUI(currentFrame, currentArea);
            if (m_Markers != null && showMarkers)
            {
                Rect rect = GUILayoutUtility.GetLastRect();
                rect.xMin += Chart.kSideWidth;
                for (int index = 0; index < m_Markers.Length; index++)
                {
                    var marker = m_Markers[index];
                    Color color = ProfilerColors.currentColors[(uint)m_Series.Length % ProfilerColors.currentColors.Length];
                    Chart.DrawVerticalLine(marker.frame, m_Data, rect, color.AlphaMultiplied(0.3f), color.AlphaMultiplied(0.4f), 1.0f);
                }
                DrawMarkerLabels(m_Data, rect, m_Markers, m_MarkerNames);
            }
            return res;
        }

        private void DrawMarkerLabels(ChartViewData cdata, Rect r, EventMarker[] markers, string[] markerNames)
        {
            Color cc = GUI.contentColor;
            Vector2 domain = cdata.GetDataDomain();
            int length = (int)(domain.y - domain.x);
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
                    if (!markerLastFrame.TryGetValue(markers[s].nameOffset, out lframe) || lframe != frame - 1 || lframe < cdata.chartDomainOffset)
                    {
                        frame -= cdata.chartDomainOffset;
                        if (frame >= 0)
                        {
                            float xpos = r.x + frameWidth * frame;
                            // color the label slightly (half way between line color and white)
                            Color color = ProfilerColors.currentColors[(uint)m_Series.Length % ProfilerColors.currentColors.Length];
                            GUI.contentColor = (color + Color.white) * 0.5f;

                            const float offset = -1;
                            Chart.DoLabel(xpos + offset, r.y + r.height - (s % maxCount + 1) * labelPositions, markerNames[s], 0);
                        }
                    }
                    markerLastFrame[markers[s].nameOffset] = markers[s].frame;
                }
            }
            GUI.contentColor = cc;
        }

        protected override Rect DoSeriesList(Rect position, int chartControlID, ChartType chartType, ChartViewData cdata)
        {
            Rect elementPosition = base.DoSeriesList(position, chartControlID, chartType, cdata);
            GUIContent label = EditorGUIUtility.TempContent("Markers");
            Color color = ProfilerColors.currentColors[cdata.numSeries % ProfilerColors.currentColors.Length];
            DoSeriesToggle(elementPosition, label, ref showMarkers, color, cdata);

            elementPosition.y += elementPosition.height;
            return elementPosition;
        }
    }
}
