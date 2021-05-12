// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.Profiling.Editor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Profiling;

namespace UnityEditor
{
    internal class UISystemProfilerChart : ProfilerChart
    {
        private EventMarker[] m_Markers;
        private string[] m_MarkerNames;

        public bool showMarkers = true;

        public UISystemProfilerChart(ProfilerModuleChartType type, float dataScale, float maximumScaleInterpolationValue, int seriesCount, string name, string localizedName, string iconName) : base(ProfilerArea.UIDetails, type, dataScale, maximumScaleInterpolationValue, seriesCount, name, localizedName, iconName)
        {
        }

        public override void UpdateData(int firstEmptyFrame, int firstFrame, int frameCount)
        {
            base.UpdateData(firstEmptyFrame, firstFrame, frameCount);

            m_Markers = null;
            m_MarkerNames = null;
            int count = ProfilerDriver.GetUISystemEventMarkersCount(firstFrame, frameCount);
            if (count == 0)
                return;

            m_Markers = new EventMarker[count];
            m_MarkerNames = new string[count];
            ProfilerDriver.GetUISystemEventMarkersBatch(firstFrame, frameCount, m_Markers, m_MarkerNames);
        }

        public override int DoChartGUI(Rect chartRect, int currentFrame, bool active)
        {
            int res = base.DoChartGUI(chartRect, currentFrame, active);
            if (m_Markers != null && showMarkers)
            {
                Rect rect = GUILayoutUtility.GetLastRect();
                rect.xMin += Chart.kSideWidth;
                for (int index = 0; index < m_Markers.Length; index++)
                {
                    var marker = m_Markers[index];
                    Color color = ProfilerColors.chartAreaColors[(uint)m_Series.Length % ProfilerColors.chartAreaColors.Length];
                    Chart.DrawVerticalLine(marker.frame, m_Data, rect, color.AlphaMultiplied(0.4f), 1.0f);
                }
                DrawMarkerLabels(m_Data, rect, m_Markers, m_MarkerNames);
            }
            return res;
        }

        private void DrawMarkerLabels(ChartViewData cdata, Rect r, EventMarker[] markers, string[] markerNames)
        {
            Color cc = GUI.contentColor;
            int length = cdata.GetDataDomainLength();
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
                            Color color = ProfilerColors.chartAreaColors[(uint)m_Series.Length % ProfilerColors.chartAreaColors.Length];
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

        protected override Rect DoSeriesList(Rect position, int chartControlID, ProfilerModuleChartType chartType, ChartViewData cdata)
        {
            Rect elementPosition = base.DoSeriesList(position, chartControlID, chartType, cdata);
            GUIContent label = EditorGUIUtility.TempContent("Markers");
            Color color = ProfilerColors.chartAreaColors[cdata.numSeries % ProfilerColors.chartAreaColors.Length];
            DoSeriesToggle(elementPosition, label, ref showMarkers, color, cdata);

            elementPosition.y += elementPosition.height;
            return elementPosition;
        }
    }
}
