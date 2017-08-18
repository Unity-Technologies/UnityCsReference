// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace UnityEditorInternal
{
    internal class Chart
    {
        public delegate void ChangedEventHandler(Chart sender);

        private static int s_ChartHash = "Charts".GetHashCode();
        public const float kSideWidth = 180.0f;
        private const int kDistFromTopToFirstLabel = 38;
        private const int kLabelHeight = 11;
        private const int kCloseButtonSize = 13;
        private const float kLabelOffset = 5f;
        private const float kWarningLabelHeightOffset = 43f;
        private const float kChartMinHeight = 130;
        private const float k_LineWidth = 2f;
        private const int k_LabelLayoutMaxIterations = 5;
        private Vector3[] m_LineDrawingPoints;
        private float[] m_StackedSampleSums;
        private static readonly Color s_OverlayBackgroundDimFactor = new Color(0.9f, 0.9f, 0.9f, 0.4f);

        string m_ChartSettingsName;
        int m_chartControlID;

        public void LoadAndBindSettings(string chartSettingsName, ChartViewData cdata)
        {
            m_ChartSettingsName = chartSettingsName;
            LoadChartsSettings(cdata);
        }

        internal enum ChartType
        {
            StackedFill,
            Line,
        }

        static class Styles
        {
            public static readonly GUIStyle background = "OL Box";
            public static readonly GUIStyle legendHeaderLabel = EditorStyles.label;
            public static readonly GUIStyle legendBackground = "ProfilerLeftPane";
            public static readonly GUIStyle rightPane = "ProfilerRightPane";
            public static readonly GUIStyle seriesLabel = "ProfilerPaneSubLabel";
            public static readonly GUIStyle seriesDragHandle = "RL DragHandle";
            public static readonly GUIStyle closeButton = "WinBtnClose";
            public static readonly GUIStyle whiteLabel = "ProfilerBadge";
            public static readonly GUIStyle selectedLabel = "ProfilerSelectedLabel";

            public static readonly float labelDropShadowOpacity = 0.3f;
            public static readonly float labelLerpToWhiteAmount = 0.5f;

            public static readonly Color selectedFrameColor1 = new Color(1, 1, 1, 0.6f);
            public static readonly Color selectedFrameColor2 = new Color(1, 1, 1, 0.7f);
        }

        public event ChangedEventHandler closed;
        public event ChangedEventHandler selected;

        public GUIContent legendHeaderLabel { get; set; }
        public Vector2 labelRange { get; set; }

        int m_DragItemIndex = -1;
        Vector2 m_DragDownPos;
        int[] m_OldChartOrder;
        public string m_NotSupportedWarning = null;

        public Chart()
        {
            labelRange = new Vector2(-Mathf.Infinity, Mathf.Infinity);
        }

        private int MoveSelectedFrame(int selectedFrame, ChartViewData cdata, int direction)
        {
            Vector2 domain = cdata.GetDataDomain();
            int length = (int)(domain.y - domain.x);
            int newSelectedFrame = selectedFrame + direction;
            if (newSelectedFrame < cdata.firstSelectableFrame || newSelectedFrame > cdata.chartDomainOffset + length)
                return selectedFrame;

            return newSelectedFrame;
        }

        private int DoFrameSelectionDrag(float x, Rect r, ChartViewData cdata, int len)
        {
            int frame = Mathf.RoundToInt((x - r.x) / r.width * len - 0.5f);
            GUI.changed = true;
            return Mathf.Clamp(frame + cdata.chartDomainOffset, cdata.firstSelectableFrame, cdata.chartDomainOffset + len);
        }

        private int HandleFrameSelectionEvents(int selectedFrame, int chartControlID, Rect chartFrame, ChartViewData cdata)
        {
            Event evt = Event.current;
            switch (evt.type)
            {
                case EventType.MouseDown:
                    if (chartFrame.Contains(evt.mousePosition))
                    {
                        GUIUtility.keyboardControl = chartControlID;
                        GUIUtility.hotControl = chartControlID;
                        Vector2 domain = cdata.GetDataDomain();
                        int len = (int)(domain.y - domain.x);
                        selectedFrame = DoFrameSelectionDrag(evt.mousePosition.x, chartFrame, cdata, len);
                        evt.Use();
                    }
                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == chartControlID)
                    {
                        Vector2 domain = cdata.GetDataDomain();
                        int len = (int)(domain.y - domain.x);
                        selectedFrame = DoFrameSelectionDrag(evt.mousePosition.x, chartFrame, cdata, len);
                        evt.Use();
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == chartControlID)
                    {
                        GUIUtility.hotControl = 0;
                        evt.Use();
                    }
                    break;
                case EventType.KeyDown:
                    if (GUIUtility.keyboardControl != chartControlID || selectedFrame < 0)
                        break;

                    if (evt.keyCode == KeyCode.LeftArrow)
                    {
                        selectedFrame = MoveSelectedFrame(selectedFrame, cdata, -1);
                        evt.Use();
                    }
                    else if (evt.keyCode == KeyCode.RightArrow)
                    {
                        selectedFrame = MoveSelectedFrame(selectedFrame, cdata, 1);
                        evt.Use();
                    }
                    break;
            }

            return selectedFrame;
        }

        public void OnLostFocus()
        {
            if (GUIUtility.hotControl == m_chartControlID)
            {
                GUIUtility.hotControl = 0;
            }
            m_chartControlID = 0;
        }

        protected virtual void DoLegendGUI(Rect position, ChartType type, ChartViewData cdata, EventType evtType, bool active)
        {
            if (Event.current.type == EventType.Repaint)
                Styles.legendBackground.Draw(position, GUIContent.none, false, false, active, false);

            Rect headerRect = position;
            GUIContent headerLabel = legendHeaderLabel ?? GUIContent.none;
            headerRect.height = Styles.legendHeaderLabel.CalcSize(headerLabel).y;
            GUI.Label(headerRect, headerLabel, Styles.legendHeaderLabel);

            position.yMin += headerRect.height;
            position.xMin += kLabelOffset;
            position.xMax -= kLabelOffset;
            DoSeriesList(position, m_chartControlID, type, cdata);

            Rect closeButtonRect = headerRect;
            closeButtonRect.xMax -= Styles.legendHeaderLabel.padding.right;
            closeButtonRect.xMin = closeButtonRect.xMax - kCloseButtonSize;
            closeButtonRect.yMin += Styles.legendHeaderLabel.padding.top;
            closeButtonRect.yMax = closeButtonRect.yMin + kCloseButtonSize;

            if (GUI.Button(closeButtonRect, GUIContent.none, Styles.closeButton) && closed != null)
                closed(this);
        }

        public int DoGUI(ChartType type, int selectedFrame, ChartViewData cdata, bool active)
        {
            if (cdata == null)
                return selectedFrame;

            m_chartControlID = GUIUtility.GetControlID(s_ChartHash, FocusType.Keyboard);

            var chartHeight = GUILayout.MinHeight(
                    Math.Max(kLabelOffset + ((cdata.numSeries + 1) * kLabelHeight) + kDistFromTopToFirstLabel, kChartMinHeight)
                    );
            Rect chartRect = GUILayoutUtility.GetRect(GUIContent.none, Styles.background, chartHeight);

            Rect r = chartRect;

            r.x += kSideWidth;
            r.width -= kSideWidth;

            Event evt = Event.current;
            EventType evtType = evt.GetTypeForControl(m_chartControlID);

            if (evtType == EventType.MouseDown && chartRect.Contains(evt.mousePosition) && selected != null)
                selected(this);

            // if we are not dragging labels, handle graph frame selection
            if (m_DragItemIndex == -1)
                selectedFrame = HandleFrameSelectionEvents(selectedFrame, m_chartControlID, r, cdata);

            Rect sideRect = r;
            sideRect.x -= kSideWidth;
            sideRect.width = kSideWidth;

            DoLegendGUI(sideRect, type, cdata, evtType, active);

            if (evt.type == EventType.Repaint)
            {
                Styles.rightPane.Draw(r, false, false, active, false);

                if (m_NotSupportedWarning == null)
                {
                    r.height -= 1.0f; // do not draw the bottom pixel
                    if (type == ChartType.StackedFill)
                        DrawChartStacked(selectedFrame, cdata, r);
                    else
                        DrawChartLine(selectedFrame, cdata, r);
                }
                else
                {
                    Rect labelRect = r;
                    labelRect.x += kSideWidth * 0.33F;
                    labelRect.y += kWarningLabelHeightOffset;
                    GUI.Label(labelRect, m_NotSupportedWarning, EditorStyles.boldLabel);
                }
            }

            return selectedFrame;
        }

        private void DrawSelectedFrame(int selectedFrame, ChartViewData cdata, Rect r)
        {
            if (cdata.firstSelectableFrame != -1 && selectedFrame - cdata.firstSelectableFrame >= 0)
                DrawVerticalLine(selectedFrame, cdata, r, Styles.selectedFrameColor1, Styles.selectedFrameColor2, 1.0f);
        }

        internal static void DrawVerticalLine(int frame, ChartViewData cdata, Rect r, Color color1, Color color2, float widthFactor)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            frame -= cdata.chartDomainOffset;
            if (frame < 0)
                return;


            Vector2 domain = cdata.GetDataDomain();
            float domainSize = domain.y - domain.x;
            HandleUtility.ApplyWireMaterial();
            GL.Begin(GL.QUADS);
            GL.Color(color1);
            GL.Vertex3(r.x + r.width / domainSize * frame, r.y + 1, 0);
            GL.Vertex3(r.x + r.width / domainSize * frame + r.width / domainSize, r.y + 1, 0);

            GL.Color(color2);
            GL.Vertex3(r.x + r.width / domainSize * frame + r.width / domainSize, r.yMax, 0);
            GL.Vertex3(r.x + r.width / domainSize * frame, r.yMax, 0);
            GL.End();
        }

        private void DrawMaxValueScale(ChartViewData cdata, Rect r)
        {
            Handles.Label(new Vector3(r.x + r.width / 2 - 20, r.yMin + 2, 0), "Scale: " + cdata.maxValue);
        }

        private void DrawChartLine(int selectedFrame, ChartViewData cdata, Rect r)
        {
            for (int i = 0; i < cdata.numSeries; i++)
            {
                DrawChartItemLine(r, cdata, i);
            }

            if (cdata.maxValue > 0)
            {
                DrawMaxValueScale(cdata, r);
            }
            DrawSelectedFrame(selectedFrame, cdata, r);

            DrawLabels(r, cdata, selectedFrame, ChartType.Line);
        }

        private void DrawChartStacked(int selectedFrame, ChartViewData cdata, Rect r)
        {
            HandleUtility.ApplyWireMaterial();

            Vector2 domain = cdata.GetDataDomain();
            int numSamples = (int)(domain.y - domain.x);
            if (numSamples <= 0)
                return;

            if (m_StackedSampleSums == null || m_StackedSampleSums.Length < numSamples)
                m_StackedSampleSums = new float[numSamples];
            for (int i = 0; i < numSamples; ++i)
                m_StackedSampleSums[i] = 0f;

            for (int i = 0; i < cdata.numSeries; i++)
            {
                if (cdata.hasOverlay)
                    DrawChartItemStackedOverlay(r, i, cdata, m_StackedSampleSums);
                DrawChartItemStacked(r, i, cdata, m_StackedSampleSums);
            }

            DrawSelectedFrame(selectedFrame, cdata, r);

            DrawGridStacked(r, cdata);
            DrawLabels(r, cdata, selectedFrame, ChartType.StackedFill);

            // Show selected property name
            //@TODO: not the best place to put this code.

            if (!cdata.hasOverlay)
                return;

            string selectedName = ProfilerDriver.selectedPropertyPath;
            if (selectedName.Length > 0)
            {
                int selectedNameBegin = selectedName.LastIndexOf('/');
                if (selectedNameBegin != -1)
                    selectedName = selectedName.Substring(selectedNameBegin + 1);

                GUIContent content = EditorGUIUtility.TempContent("Selected: " + selectedName);
                Vector2 size = EditorStyles.whiteBoldLabel.CalcSize(content);
                EditorGUI.DropShadowLabel(new Rect(r.x + r.width - size.x - 3.0f, r.y + 3.0f, size.x, size.y), content,
                    Styles.selectedLabel);
            }
        }

        internal static void DoLabel(float x, float y, string text, float alignment)
        {
            if (string.IsNullOrEmpty(text))
                return;

            GUIContent content = EditorGUIUtility.TempContent(text);
            Vector2 size = Styles.whiteLabel.CalcSize(content);
            Rect r = new Rect(x + size.x * alignment, y, size.x, size.y);

            EditorGUI.DoDropShadowLabel(r, content, Styles.whiteLabel, Styles.labelDropShadowOpacity);
        }

        private void DrawGridStacked(Rect r, ChartViewData cdata)
        {
            if (Event.current.type != EventType.Repaint ||
                cdata.grid == null || cdata.gridLabels == null)
                return;

            GL.Begin(GL.LINES);
            GL.Color(new Color(1, 1, 1, 0.2f));
            float rangeScale = cdata.series[0].rangeAxis.sqrMagnitude == 0f ?
                0f : 1f / (cdata.series[0].rangeAxis.y - cdata.series[0].rangeAxis.x) * r.height;
            float rectBottom = r.y + r.height;
            for (int i = 0; i < cdata.grid.Length; ++i)
            {
                float y = rectBottom - (cdata.grid[i] - cdata.series[0].rangeAxis.x) * rangeScale;
                if (y > r.y)
                {
                    GL.Vertex3(r.x + 80, y, 0.0f);
                    GL.Vertex3(r.x + r.width, y, 0.0f);
                }
            }
            GL.End();

            for (int i = 0; i < cdata.grid.Length; ++i)
            {
                float y = rectBottom - (cdata.grid[i] - cdata.series[0].rangeAxis.x) * rangeScale;
                if (y > r.y)
                {
                    DoLabel(r.x + 5, y - 8, cdata.gridLabels[i], 0.0f);
                }
            }
        }

        private struct LabelLayoutData
        {
            public Rect position;
            public float desiredYPosition;
        }

        private readonly List<LabelLayoutData> m_LabelData = new List<LabelLayoutData>(16);
        private readonly List<int> m_LabelOrder = new List<int>(16);
        private readonly List<int> m_MostOverlappingLabels = new List<int>(16);
        private readonly List<int> m_OverlappingLabels = new List<int>(16);
        private readonly List<float> m_SelectedFrameValues = new List<float>(16);

        private void DrawLabels(Rect chartPosition, ChartViewData data, int selectedFrame, ChartType chartType)
        {
            if (data.selectedLabels == null || Event.current.type != EventType.Repaint)
                return;

            // exit early if the selected frame is outside the domain of the chart
            var domain = data.GetDataDomain();
            if (
                selectedFrame < data.firstSelectableFrame ||
                selectedFrame > data.chartDomainOffset + (int)(domain.y - domain.x) ||
                domain.y - domain.x == 0f
                )
                return;

            var selectedIndex = selectedFrame - data.chartDomainOffset;

            m_LabelOrder.Clear();
            m_LabelOrder.AddRange(data.order);

            // get values of all series and cumulative value of all enabled stacks
            m_SelectedFrameValues.Clear();
            var cumulativeValueOfAllEnabledSeries = 0f;
            var stacked = chartType == ChartType.StackedFill;
            var numLabels = 0;
            for (int s = 0, count = data.numSeries; s < count; ++s)
            {
                var value = data.series[s].yValues[selectedIndex];
                m_SelectedFrameValues.Add(value);
                if (data.series[s].enabled)
                {
                    cumulativeValueOfAllEnabledSeries += value;
                    ++numLabels;
                }
            }

            if (numLabels == 0)
                return;

            // populate layout data array with default data
            m_LabelData.Clear();
            var selectedFrameMidline =
                chartPosition.x + chartPosition.width * ((selectedIndex + 0.5f) / (domain.y - domain.x));
            var maxLabelWidth = 0f;
            numLabels = 0;
            for (int s = 0, count = data.numSeries; s < count; ++s)
            {
                var labelData = new LabelLayoutData();

                var value = m_SelectedFrameValues[s];

                if (data.series[s].enabled && value >= labelRange.x && value <= labelRange.y)
                {
                    var rangeAxis = data.series[s].rangeAxis;
                    var rangeSize = rangeAxis.sqrMagnitude == 0f ? 1f : rangeAxis.y - rangeAxis.x;

                    // convert stacked series to cumulative value of enabled series
                    if (stacked)
                    {
                        var accumulatedValues = 0f;
                        for (int i = 0; i < count; ++i)
                        {
                            var otherSeriesIdx = data.order[i];
                            if (otherSeriesIdx < s && data.series[otherSeriesIdx].enabled)
                                accumulatedValues += data.series[otherSeriesIdx].yValues[selectedIndex];
                        }
                        // labels for stacked series will be in the middle of their stacks
                        value = cumulativeValueOfAllEnabledSeries - accumulatedValues - 0.5f * value;
                    }

                    // default position is left aligned to midline
                    var position = new Vector2(
                            // offset by half a point so there is a 1-point gap down the midline if labels are on both sides
                            selectedFrameMidline + 0.5f,
                            chartPosition.y + chartPosition.height * (1f - (value - rangeAxis.x) / rangeSize)
                            );
                    var size = Styles.whiteLabel.CalcSize(EditorGUIUtility.TempContent(data.selectedLabels[s]));
                    position.y -= 0.5f * size.y;
                    position.y = Mathf.Clamp(position.y, chartPosition.yMin, chartPosition.yMax - size.y);

                    labelData.position = new Rect(position, size);
                    labelData.desiredYPosition = labelData.position.center.y;

                    ++numLabels;
                }

                m_LabelData.Add(labelData);

                maxLabelWidth = Mathf.Max(maxLabelWidth, labelData.position.width);
            }

            if (numLabels == 0)
                return;

            // line charts order labels based on series values
            if (!stacked)
                m_LabelOrder.Sort(SortLineLabelIndices);

            // right align labels to the selected frame midline if approaching right border
            if (selectedFrameMidline > chartPosition.x + chartPosition.width - maxLabelWidth)
            {
                for (int s = 0, count = data.numSeries; s < count; ++s)
                {
                    var label = m_LabelData[s];
                    label.position.x -= label.position.width;
                    m_LabelData[s] = label;
                }
            }
            // alternate right/left alignment if in the middle
            else if (selectedFrameMidline > chartPosition.x + maxLabelWidth)
            {
                var processed = 0;
                for (int s = 0, count = data.numSeries; s < count; ++s)
                {
                    var labelIndex = m_LabelOrder[s];

                    if (m_LabelData[labelIndex].position.size.sqrMagnitude == 0f)
                        continue;

                    if ((processed & 1) == 0)
                    {
                        var label = m_LabelData[labelIndex];
                        // ensure there is a 1-point gap down the midline
                        label.position.x -= label.position.width + 1f;
                        m_LabelData[labelIndex] = label;
                    }

                    ++processed;
                }
            }

            // separate overlapping labels
            for (int it = 0; it < k_LabelLayoutMaxIterations; ++it)
            {
                m_MostOverlappingLabels.Clear();

                // work on the biggest cluster of overlapping rects
                for (int s1 = 0, count = data.numSeries; s1 < count; ++s1)
                {
                    m_OverlappingLabels.Clear();
                    m_OverlappingLabels.Add(s1);

                    if (m_LabelData[s1].position.size.sqrMagnitude == 0f)
                        continue;

                    for (int s2 = 0; s2 < count; ++s2)
                    {
                        if (m_LabelData[s2].position.size.sqrMagnitude == 0f)
                            continue;

                        if (s1 != s2 && m_LabelData[s1].position.Overlaps(m_LabelData[s2].position))
                            m_OverlappingLabels.Add(s2);
                    }

                    if (m_OverlappingLabels.Count > m_MostOverlappingLabels.Count)
                    {
                        m_MostOverlappingLabels.Clear();
                        m_MostOverlappingLabels.AddRange(m_OverlappingLabels);
                    }
                }

                // finish if there are no more overlapping rects
                if (m_MostOverlappingLabels.Count == 1)
                    break;

                float totalHeight;
                var geometricCenter = GetGeometricCenter(m_MostOverlappingLabels, m_LabelData, out totalHeight);

                // account for other rects that will overlap after expanding
                var foundOverlaps = true;
                while (foundOverlaps)
                {
                    foundOverlaps = false;
                    var minY = geometricCenter - 0.5f * totalHeight;
                    var maxY = geometricCenter + 0.5f * totalHeight;
                    for (int s = 0, count = data.numSeries; s < count; ++s)
                    {
                        if (m_MostOverlappingLabels.Contains(s))
                            continue;

                        var testRect = m_LabelData[s].position;

                        if (testRect.size.sqrMagnitude == 0f)
                            continue;

                        var x = testRect.xMax < selectedFrameMidline ? testRect.xMax : testRect.xMin;
                        if (
                            testRect.Contains(new Vector2(x, minY)) ||
                            testRect.Contains(new Vector2(x, maxY))
                            )
                        {
                            m_MostOverlappingLabels.Add(s);
                            foundOverlaps = true;
                        }
                    }

                    GetGeometricCenter(m_MostOverlappingLabels, m_LabelData, out totalHeight);

                    // keep labels inside chart rect
                    if (geometricCenter - 0.5f * totalHeight < chartPosition.yMin)
                        geometricCenter = chartPosition.yMin + 0.5f * totalHeight;
                    else if (geometricCenter + 0.5f * totalHeight > chartPosition.yMax)
                        geometricCenter = chartPosition.yMax - 0.5f * totalHeight;
                }

                // separate overlapping rects and distribute them away from their geometric center
                m_MostOverlappingLabels.Sort(SortOverlappingRectIndices);
                var heightAllotted = 0f;
                for (int i = 0, count = m_MostOverlappingLabels.Count; i < count; ++i)
                {
                    var labelIndex = m_MostOverlappingLabels[i];
                    var label = m_LabelData[labelIndex];
                    label.position.y = geometricCenter - totalHeight * 0.5f + heightAllotted;
                    m_LabelData[labelIndex] = label;
                    heightAllotted += label.position.height;
                }
            }

            // draw the labels
            var oldContentColor = GUI.contentColor;
            for (int s = 0; s < data.numSeries; ++s)
            {
                var labelIndex = m_LabelOrder[s];

                if (m_LabelData[labelIndex].position.size.sqrMagnitude == 0f)
                    continue;

                GUI.contentColor = Color.Lerp(data.series[labelIndex].color, Color.white, Styles.labelLerpToWhiteAmount);
                var layoutData = m_LabelData[labelIndex];
                EditorGUI.DoDropShadowLabel(
                    layoutData.position,
                    EditorGUIUtility.TempContent(data.selectedLabels[labelIndex]),
                    Styles.whiteLabel,
                    Styles.labelDropShadowOpacity
                    );
            }
            GUI.contentColor = oldContentColor;
        }

        private int SortLineLabelIndices(int index1, int index2)
        {
            return -m_LabelData[index1].desiredYPosition.CompareTo(m_LabelData[index2].desiredYPosition);
        }

        private int SortOverlappingRectIndices(int index1, int index2)
        {
            return -m_LabelOrder.IndexOf(index1).CompareTo(m_LabelOrder.IndexOf(index2));
        }

        private float GetGeometricCenter(List<int> overlappingRects, List<LabelLayoutData> labelData, out float totalHeight)
        {
            var geometricCenter = 0f;
            totalHeight = 0f;
            for (int i = 0, count = overlappingRects.Count; i < count; ++i)
            {
                var labelIndex = overlappingRects[i];
                geometricCenter += labelData[labelIndex].desiredYPosition;
                totalHeight += labelData[labelIndex].position.height;
            }
            return geometricCenter / overlappingRects.Count;
        }

        private void DrawChartItemLine(Rect r, ChartViewData cdata, int index)
        {
            ChartSeriesViewData series = cdata.series[index];

            if (!series.enabled)
                return;

            if (m_LineDrawingPoints == null || series.numDataPoints > m_LineDrawingPoints.Length)
                m_LineDrawingPoints = new Vector3[series.numDataPoints];

            Vector2 domain = cdata.GetDataDomain();
            float domainSize = domain.y - domain.x;
            if (domainSize <= 0f)
                return;

            float domainScale = 1f / domainSize * r.width;
            float rangeScale = cdata.series[index].rangeAxis.sqrMagnitude == 0f ?
                0f : 1f / (cdata.series[index].rangeAxis.y - cdata.series[index].rangeAxis.x) * r.height;
            float rectBottom = r.y + r.height;
            for (int i = 0; i < series.numDataPoints; ++i)
            {
                m_LineDrawingPoints[i].Set(
                    (series.xValues[i] - domain.x) * domainScale + r.x,
                    rectBottom - (series.yValues[i] - series.rangeAxis.x) * rangeScale,
                    0f
                    );
            }

            using (new Handles.DrawingScope(cdata.series[index].color))
                Handles.DrawAAPolyLine(k_LineWidth, series.numDataPoints, m_LineDrawingPoints);
        }

        private void DrawChartItemStacked(Rect r, int index, ChartViewData cdata, float[] stackedSampleSums)
        {
            Vector2 domain = cdata.GetDataDomain();
            int numSamples = (int)(domain.y - domain.x);

            float step = r.width / numSamples;

            index = cdata.order[index];

            if (!cdata.series[index].enabled)
                return;

            Color color = cdata.series[index].color;
            if (cdata.hasOverlay)
                color *= s_OverlayBackgroundDimFactor;

            GL.Begin(GL.TRIANGLE_STRIP);

            float x = r.x + step * 0.5f;
            float rangeScale = cdata.series[0].rangeAxis.sqrMagnitude == 0f ?
                0f : 1f / (cdata.series[0].rangeAxis.y - cdata.series[0].rangeAxis.x) * r.height;
            float rectBottom = r.y + r.height;
            for (int i = 0; i < numSamples; i++, x += step)
            {
                float y = rectBottom - stackedSampleSums[i];

                float value = cdata.series[index].yValues[i];
                if (value == -1f)
                    continue;

                float val = (value - cdata.series[0].rangeAxis.x) * rangeScale;
                if (y - val < r.yMin)
                    val = y - r.yMin; // Clamp the values to be inside drawrect
                GL.Color(color);
                GL.Vertex3(x, y - val, 0f); // clip chart top
                GL.Vertex3(x, y, 0f);

                stackedSampleSums[i] += val;
            }
            GL.End();
        }

        private void DrawChartItemStackedOverlay(Rect r, int index, ChartViewData cdata, float[] stackedSampleSums)
        {
            Vector2 domain = cdata.GetDataDomain();
            int numSamples = (int)(domain.y - domain.x);
            float step = r.width / numSamples;

            int orderIdx = cdata.order[index];

            if (!cdata.series[orderIdx].enabled)
                return;

            Color color = cdata.series[orderIdx].color;

            GL.Begin(GL.TRIANGLE_STRIP);

            float x = r.x + step * 0.5f;
            float rangeScale = cdata.series[0].rangeAxis.sqrMagnitude == 0f ?
                0f : 1f / (cdata.series[0].rangeAxis.y - cdata.series[0].rangeAxis.x) * r.height;
            float rectBottom = r.y + r.height;
            for (int i = 0; i < numSamples; i++, x += step)
            {
                float y = rectBottom - stackedSampleSums[i];

                float value = cdata.overlays[orderIdx].yValues[i];
                if (value == -1f)
                    continue;

                float val = (value - cdata.series[0].rangeAxis.x) * rangeScale;
                GL.Color(color);
                GL.Vertex3(x, y - val, 0f);
                GL.Vertex3(x, y, 0f);
            }
            GL.End();
        }

        protected virtual Rect DoSeriesList(Rect position, int chartControlID, ChartType chartType, ChartViewData cdata)
        {
            Rect elementPosition = position;
            Event evt = Event.current;
            EventType eventType = evt.GetTypeForControl(chartControlID);
            Vector2 mousePosition = evt.mousePosition;

            if (m_DragItemIndex != -1)
            {
                switch (eventType)
                {
                    case EventType.MouseUp:
                        if (GUIUtility.hotControl == chartControlID)
                        {
                            GUIUtility.hotControl = 0;
                            m_DragItemIndex = -1;
                            evt.Use();
                        }
                        break;
                    case EventType.KeyDown:
                        if (evt.keyCode == KeyCode.Escape)
                        {
                            GUIUtility.hotControl = 0;
                            System.Array.Copy(m_OldChartOrder, cdata.order, m_OldChartOrder.Length);
                            m_DragItemIndex = -1;
                            evt.Use();
                        }
                        break;
                }
            }

            for (int i = cdata.numSeries - 1; i >= 0; --i)
            {
                int orderIdx = cdata.order[i];

                GUIContent elementLabel = EditorGUIUtility.TempContent(cdata.series[orderIdx].name);
                elementPosition.height = Styles.seriesLabel.CalcHeight(elementLabel, elementPosition.width);

                Rect controlPosition = elementPosition;
                if (i == m_DragItemIndex)
                    controlPosition.y = mousePosition.y - m_DragDownPos.y;

                if (chartType == ChartType.StackedFill)
                {
                    Rect dragHandlePosition = controlPosition;
                    dragHandlePosition.xMin = dragHandlePosition.xMax - elementPosition.height;
                    switch (eventType)
                    {
                        case EventType.Repaint:
                            Styles.seriesDragHandle.Draw(dragHandlePosition, false, false, false, false);
                            break;
                        case EventType.MouseDown:
                            if (dragHandlePosition.Contains(mousePosition))
                            {
                                m_DragItemIndex = i;
                                m_DragDownPos = mousePosition;
                                m_DragDownPos.x -= elementPosition.x;
                                m_DragDownPos.y -= elementPosition.y;

                                m_OldChartOrder = new int[cdata.numSeries];
                                System.Array.Copy(cdata.order, m_OldChartOrder, m_OldChartOrder.Length);

                                GUIUtility.hotControl = chartControlID;
                                evt.Use();
                            }
                            break;
                        case EventType.MouseDrag:
                            if (i == m_DragItemIndex)
                            {
                                bool moveDn = mousePosition.y > elementPosition.yMax;
                                bool moveUp = mousePosition.y < elementPosition.yMin;
                                if (moveDn || moveUp)
                                {
                                    int draggedItemOrder = cdata.order[i];
                                    int targetIdx = moveUp ?
                                        Mathf.Min(cdata.numSeries - 1, i + 1) : Mathf.Max(0, i - 1);

                                    cdata.order[i] = cdata.order[targetIdx];
                                    cdata.order[targetIdx] = draggedItemOrder;

                                    m_DragItemIndex = targetIdx;

                                    SaveChartsSettingsOrder(cdata);
                                }
                                evt.Use();
                            }
                            break;
                        case EventType.MouseUp:
                            if (m_DragItemIndex == i)
                                evt.Use();
                            m_DragItemIndex = -1;
                            break;
                    }
                }

                DoSeriesToggle(controlPosition, elementLabel, ref cdata.series[orderIdx].enabled, cdata.series[orderIdx].color, cdata);

                elementPosition.y += elementPosition.height + EditorGUIUtility.standardVerticalSpacing;
            }

            return elementPosition;
        }

        protected void DoSeriesToggle(Rect position, GUIContent label, ref bool enabled, Color color, ChartViewData cdata)
        {
            Color oldColor = GUI.backgroundColor;

            GUI.backgroundColor = enabled ? color : Color.black;

            EditorGUI.BeginChangeCheck();
            enabled = GUI.Toggle(position, enabled, label, Styles.seriesLabel);
            if (EditorGUI.EndChangeCheck())
                SaveChartsSettingsEnabled(cdata);

            GUI.backgroundColor = oldColor;
        }

        private void LoadChartsSettings(ChartViewData cdata)
        {
            if (string.IsNullOrEmpty(m_ChartSettingsName))
                return;

            var str = EditorPrefs.GetString(m_ChartSettingsName + "Order");
            if (!string.IsNullOrEmpty(str))
            {
                try
                {
                    var values = str.Split(',');
                    if (values.Length == cdata.numSeries)
                    {
                        for (var i = 0; i < cdata.numSeries; i++)
                            cdata.order[i] = int.Parse(values[i]);
                    }
                }
                catch (FormatException) {}
            }

            str = EditorPrefs.GetString(m_ChartSettingsName + "Visible");

            for (var i = 0; i < cdata.numSeries; i++)
            {
                if (i < str.Length && str[i] == '0')
                    cdata.series[i].enabled = false;
            }
        }

        private void SaveChartsSettingsOrder(ChartViewData cdata)
        {
            if (string.IsNullOrEmpty(m_ChartSettingsName))
                return;

            var str = string.Empty;

            for (var i = 0; i < cdata.numSeries; i++)
            {
                if (str.Length != 0)
                    str += ",";
                str += cdata.order[i];
            }

            EditorPrefs.SetString(m_ChartSettingsName + "Order", str);
        }

        protected void SaveChartsSettingsEnabled(ChartViewData cdata)
        {
            var str = string.Empty;

            for (var i = 0; i < cdata.numSeries; i++)
                str += cdata.series[i].enabled ? '1' : '0';

            EditorPrefs.SetString(m_ChartSettingsName + "Visible", str);
        }
    }

    internal class ChartSeriesViewData
    {
        public string name { get; private set; }
        public Color color { get; private set; }
        public bool enabled;
        public float[] xValues { get; private set; }
        public float[] yValues { get; private set; }
        public Vector2 rangeAxis { get; set; }
        public int numDataPoints { get; private set; }

        public ChartSeriesViewData(string name, int numDataPoints, Color color)
        {
            this.name = name;
            this.color = color;
            this.numDataPoints = numDataPoints;
            xValues = new float[numDataPoints];
            yValues = new float[numDataPoints];
            enabled = true;
        }
    }

    internal class ChartViewData
    {
        public ChartSeriesViewData[] series { get; private set; }
        public ChartSeriesViewData[] overlays { get; private set; }
        public int[] order { get; private set; }
        public float[] grid { get; private set; }
        public string[] gridLabels { get; private set; }
        public string[] selectedLabels { get; private set; }
        public int firstSelectableFrame { get; private set; }
        public bool hasOverlay { get; set; }
        public float maxValue { get; set; }
        public int numSeries { get; private set; }
        public int chartDomainOffset { get; private set; }

        public void Assign(ChartSeriesViewData[] series, int firstFrame, int firstSelectableFrame)
        {
            this.series = series;
            this.chartDomainOffset = firstFrame;
            this.firstSelectableFrame = firstSelectableFrame;
            numSeries = series.Length;

            if (order == null || order.Length != numSeries)
            {
                order = new int[numSeries];
                for (int i = 0, count = order.Length; i < count; ++i)
                    order[i] = order.Length - 1 - i;
            }

            if (overlays == null || overlays.Length != numSeries)
                overlays = new ChartSeriesViewData[numSeries];
        }

        public void AssignSelectedLabels(string[] selectedLabels)
        {
            this.selectedLabels = selectedLabels;
        }

        public void SetGrid(float[] grid, string[] labels)
        {
            this.grid = grid;
            this.gridLabels = labels;
        }

        public Vector2 GetDataDomain()
        {
            // TODO: this currently assumes data points are in order and first series has at least one data point
            if (series == null || numSeries == 0 || series[0].numDataPoints == 0)
                return Vector2.zero;
            Vector2 result = Vector2.one * series[0].xValues[0];
            for (int i = 0; i < numSeries; ++i)
            {
                if (series[i].numDataPoints == 0)
                    continue;
                result.x = Mathf.Min(result.x, series[i].xValues[0]);
                result.y = Mathf.Max(result.y, series[i].xValues[series[i].numDataPoints - 1]);
            }
            return result;
        }
    }
}
