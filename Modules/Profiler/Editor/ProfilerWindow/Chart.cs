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
        const int kDistFromTopToFirstLabel = 38;
        const int kLabelHeight = 11;
        const int kCloseButtonSize = 13;
        const float kLabelOffset = 5f;
        const float kWarningLabelHeightOffset = 43.0f;
        const float kChartMinHeight = 130;
        private const float k_LineWidth = 2f;
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

            public static readonly Color selectedFrameColor1 = new Color(1, 1, 1, 0.6f);
            public static readonly Color selectedFrameColor2 = new Color(1, 1, 1, 0.7f);
        }

        public event ChangedEventHandler closed;
        public event ChangedEventHandler selected;

        public GUIContent legendHeaderLabel { get; set; }

        int m_DragItemIndex = -1;
        Vector2 m_DragDownPos;
        int[] m_OldChartOrder;
        public string m_NotSupportedWarning = null;

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
            if (Event.current.type != EventType.repaint)
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

            DrawLabelsLine(selectedFrame, cdata, r);
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
            DrawLabelsStacked(selectedFrame, cdata, r);

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

            GUIContent content = new GUIContent(text);
            Vector2 size = Styles.whiteLabel.CalcSize(content);
            Rect r = new Rect(x + size.x * alignment, y, size.x, size.y);

            EditorGUI.DoDropShadowLabel(r, content, Styles.whiteLabel, .3f);
        }

        // Pushes labels away from each other (so they do not overlap)
        private static void CorrectLabelPositions(float[] ypositions, float[] heights, float maxHeight)
        {
            // arbitrary iteration count
            int iterationCount = 5;
            for (int it = 0; it < iterationCount; ++it)
            {
                bool corrected = false;

                for (int i = 0; i < ypositions.Length; ++i)
                {
                    if (heights[i] <= 0)
                        continue;

                    float halfHeight = heights[i] / 2;

                    // we skip every second, because labels are on different sides of the vertical line
                    for (int j = i + 2; j < ypositions.Length; j += 2)
                    {
                        if (heights[j] <= 0)
                            continue;

                        float delta = ypositions[i] - ypositions[j];
                        float minDistance = (heights[i] + heights[j]) / 2;

                        if (Mathf.Abs(delta) < minDistance)
                        {
                            delta = (minDistance - Mathf.Abs(delta)) / 2 * Mathf.Sign(delta);

                            ypositions[i] += delta;
                            ypositions[j] -= delta;

                            corrected = true;
                        }
                    }

                    // fitting into graph boundaries
                    if (ypositions[i] + halfHeight > maxHeight)
                        ypositions[i] = maxHeight - halfHeight;
                    if (ypositions[i] - halfHeight < 0)
                        ypositions[i] = halfHeight;
                }

                if (!corrected)
                    break;
            }
        }

        private static float GetLabelHeight(string text)
        {
            GUIContent content = new GUIContent(text);
            Vector2 size = Styles.whiteLabel.CalcSize(content);

            return size.y;
        }

        private void DrawLabelsStacked(int selectedFrame, ChartViewData cdata, Rect r)
        {
            if (cdata.selectedLabels == null)
                return;

            Vector2 domain = cdata.GetDataDomain();
            int length = (int)(domain.y - domain.x);

            if (selectedFrame < cdata.firstSelectableFrame || selectedFrame >= cdata.chartDomainOffset + length)
                return;
            selectedFrame -= cdata.chartDomainOffset;

            float frameWidth = r.width / length;
            float xpos = r.x + frameWidth * selectedFrame;
            float rangeScale = cdata.series[0].rangeAxis.sqrMagnitude == 0f ?
                0f : 1f / (cdata.series[0].rangeAxis.y - cdata.series[0].rangeAxis.x) * r.height;

            float[] ypositions = new float[cdata.numSeries];
            float[] heights = new float[cdata.numSeries];

            float accum = 0.0f;
            for (int s = 0; s < cdata.numSeries; ++s)
            {
                ypositions[s] = -1;
                heights[s] = 0;

                int index = cdata.order[s];

                if (!cdata.series[index].enabled)
                    continue;

                float value = cdata.series[index].yValues[selectedFrame];
                if (value == -1.0f)
                    continue;

                float labelValue = cdata.hasOverlay ? cdata.overlays[index].yValues[selectedFrame] : value;
                // only draw labels for large enough stacks
                if ((labelValue - cdata.series[0].rangeAxis.x) * rangeScale > 5f)
                {
                    // place value in the middle of this stack vertically
                    ypositions[s] = (accum + labelValue * 0.5f) * rangeScale;
                    heights[s] = GetLabelHeight(cdata.selectedLabels[index]);
                }

                // accumulate stacked value
                accum += value;
            }

            CorrectLabelPositions(ypositions, heights, r.height);

            for (int s = 0; s < cdata.numSeries; ++s)
            {
                if (heights[s] > 0)
                {
                    int index = cdata.order[s];

                    // tint color slightly towards white
                    Color clr = cdata.series[index].color;
                    GUI.contentColor = clr * 0.8f + Color.white * 0.2f;

                    // Place odd labels on one side, even labels on another side.
                    // Do not tweak to e.g. -1.05f! It will give different offsets depending on text length.
                    float alignment = (index & 1) == 0 ? -1 : 0;
                    float offset = (index & 1) == 0 ? -1 : frameWidth + 1;
                    DoLabel(xpos + offset, r.y + r.height - ypositions[s] - 8, cdata.selectedLabels[index], alignment);
                }
            }

            GUI.contentColor = Color.white;
        }

        private void DrawGridStacked(Rect r, ChartViewData cdata)
        {
            if (Event.current.type != EventType.repaint ||
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

        private void DrawLabelsLine(int selectedFrame, ChartViewData cdata, Rect r)
        {
            if (cdata.selectedLabels == null)
                return;

            Vector2 domain = cdata.GetDataDomain();
            int length = (int)(domain.y - domain.x);

            if (selectedFrame < cdata.firstSelectableFrame || selectedFrame >= cdata.chartDomainOffset + length)
                return;
            selectedFrame -= cdata.chartDomainOffset;

            float[] ypositions = new float[cdata.numSeries];
            float[] heights = new float[cdata.numSeries];

            for (int s = 0; s < cdata.numSeries; ++s)
            {
                ypositions[s] = -1;
                heights[s] = 0;

                float value = cdata.series[s].yValues[selectedFrame];
                if (value != -1)
                {
                    ypositions[s] = (value - cdata.series[s].rangeAxis.x) / (cdata.series[s].rangeAxis.y - cdata.series[s].rangeAxis.x) * r.height;
                    heights[s] = GetLabelHeight(cdata.selectedLabels[s]);
                }
            }

            CorrectLabelPositions(ypositions, heights, r.height);

            float frameWidth = r.width / length;
            float xpos = r.x + frameWidth * selectedFrame;

            for (int s = 0; s < cdata.numSeries; ++s)
            {
                if (heights[s] > 0)
                {
                    // color the label slightly (half way between line color and white)
                    Color clr = cdata.series[s].color;
                    GUI.contentColor = (clr + Color.white) * 0.5f;

                    // Place odd labels on one side, even labels on another side.
                    // Do not tweak to e.g. -1.05f! It will give different offsets depending on text length.
                    float alignment = (s & 1) == 0 ? -1 : 0;
                    float offset = (s & 1) == 0 ? -1 : frameWidth + 1;
                    DoLabel(xpos + offset, r.y + r.height - ypositions[s] - 8, cdata.selectedLabels[s], alignment);
                }
            }
            GUI.contentColor = Color.white;
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
