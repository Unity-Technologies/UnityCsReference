// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEditor;
using System.Collections;

namespace UnityEditorInternal
{
    internal class Chart
    {
        static int s_ChartHash = "Charts".GetHashCode();
        public const float kSideWidth = 170.0f;
        const int kDistFromTopToFirstLabel = 20;
        const int kLabelHeight = 11;
        const int kCloseButtonSize = 13;
        const float kLabelXOffset = 40.0f;
        const float kWarningLabelHeightOffset = 43.0f;
        Vector3[] m_CachedLineData;

        string m_ChartSettingsName;
        int m_chartControlID;

        public void LoadAndBindSettings(string chartSettingsName, ChartData cdata)
        {
            m_ChartSettingsName = chartSettingsName;
            LoadChartsSettings(cdata);
        }

        internal enum ChartAction
        {
            None,
            Activated,
            Closed
        }

        internal enum ChartType
        {
            StackedFill,
            Line,
        }

        internal class Styles
        {
            public GUIContent performanceWarning = new GUIContent("", EditorGUIUtility.LoadIcon("console.warnicon.sml"), "Collecting GPU Profiler data might have overhead. Close graph if you don't need its data");

            public GUIStyle background = "OL Box";
            public GUIStyle leftPane = "ProfilerLeftPane";
            public GUIStyle rightPane = "ProfilerRightPane";
            public GUIStyle paneSubLabel = "ProfilerPaneSubLabel";
            public GUIStyle closeButton = "WinBtnClose";
            public GUIStyle whiteLabel = "ProfilerBadge";
            public GUIStyle selectedLabel = "ProfilerSelectedLabel";

            public Color selectedFrameColor1 = new Color(1, 1, 1, 0.6f);
            public Color selectedFrameColor2 = new Color(1, 1, 1, 0.7f);
        }

        private static Styles ms_Styles = null;

        int m_DragItemIndex = -1;
        Vector2 m_DragDownPos;
        int[] m_ChartOrderBackup;
        int m_MouseDownIndex = -1;
        public string m_NotSupportedWarning = null;

        private int MoveSelectedFrame(int selectedFrame, ChartData cdata, int direction)
        {
            int length = cdata.NumberOfFrames;
            int newSelectedFrame = selectedFrame + direction;
            if (newSelectedFrame < cdata.firstSelectableFrame || newSelectedFrame > cdata.firstFrame + length)
                return selectedFrame;

            return newSelectedFrame;
        }

        private int DoFrameSelectionDrag(float x, Rect r, ChartData cdata, int len)
        {
            int frame = Mathf.RoundToInt((x - r.x) / r.width * len - 0.5f);
            GUI.changed = true;
            return Mathf.Clamp(frame + cdata.firstFrame, cdata.firstSelectableFrame, cdata.firstFrame + len);
        }

        private int HandleFrameSelectionEvents(int selectedFrame, int chartControlID, Rect chartFrame, ChartData cdata, int len)
        {
            Event evt = Event.current;
            switch (evt.type)
            {
                case EventType.MouseDown:
                    if (chartFrame.Contains(evt.mousePosition))
                    {
                        GUIUtility.keyboardControl = chartControlID;
                        GUIUtility.hotControl = chartControlID;
                        selectedFrame = DoFrameSelectionDrag(evt.mousePosition.x, chartFrame, cdata, len);
                        evt.Use();
                    }
                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == chartControlID)
                    {
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

        public int DoGUI(ChartType type, int selectedFrame, ChartData cdata, ProfilerArea area, bool active, GUIContent icon, out ChartAction action)
        {
            action = ChartAction.None;
            if (cdata == null)
                return selectedFrame;

            int len = cdata.NumberOfFrames;

            if (ms_Styles == null)
                ms_Styles = new Styles();

            m_chartControlID = GUIUtility.GetControlID(s_ChartHash, FocusType.Keyboard);

            Rect chartRect = GUILayoutUtility.GetRect(GUIContent.none, ms_Styles.background, GUILayout.MinHeight(120.0f));


            Rect r = chartRect;

            r.x += kSideWidth;
            r.width -= kSideWidth;

            Event evt = Event.current;
            EventType evtType = evt.GetTypeForControl(m_chartControlID);

            if (evtType == EventType.MouseDown && chartRect.Contains(evt.mousePosition))
                action = ChartAction.Activated;

            // if we are not dragging labels, handle graph frame selection
            if (m_DragItemIndex == -1)
                selectedFrame = HandleFrameSelectionEvents(selectedFrame, m_chartControlID, r, cdata, len);

            Rect sideRect = r;
            sideRect.x -= kSideWidth;
            sideRect.width = kSideWidth;


            // tooltip
            GUI.Label(new Rect(sideRect.x, sideRect.y, sideRect.width, 20), GUIContent.Temp("", icon.tooltip));

            if (evt.type == EventType.Repaint)
            {
                ms_Styles.rightPane.Draw(r, false, false, active, false);
                ms_Styles.leftPane.Draw(sideRect, EditorGUIUtility.TempContent(icon.text), false, false, active, false);

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

                sideRect.x += 10;
                sideRect.y += 10;

                GUIStyle.none.Draw(sideRect, EditorGUIUtility.TempContent(icon.image), false, false, false, false);

                sideRect.x += kLabelXOffset;

                DrawLabelDragger(type, sideRect, cdata);
            }
            else
            {
                sideRect.y += 10;
                LabelDraggerDrag(m_chartControlID, type, cdata, sideRect, active);
            }

            if (area == ProfilerArea.GPU)
            {
                GUI.Label(new Rect(chartRect.x + kSideWidth - ms_Styles.performanceWarning.image.width, chartRect.yMax - ms_Styles.performanceWarning.image.height - 2,
                        ms_Styles.performanceWarning.image.width, ms_Styles.performanceWarning.image.height), ms_Styles.performanceWarning);
            }

            if (GUI.Button(new Rect(chartRect.x + kSideWidth - kCloseButtonSize - 2, chartRect.y + 2, kCloseButtonSize, kCloseButtonSize), GUIContent.none, ms_Styles.closeButton))
                action = ChartAction.Closed;

            return selectedFrame;
        }

        private void DrawSelectedFrame(int selectedFrame, ChartData cdata, Rect r)
        {
            if (cdata.firstSelectableFrame != -1 && selectedFrame - cdata.firstSelectableFrame >= 0)
                DrawVerticalLine(selectedFrame, cdata, r, ms_Styles.selectedFrameColor1, ms_Styles.selectedFrameColor2, 1.0f);
        }

        internal static void DrawVerticalLine(int frame, ChartData cdata, Rect r, Color color1, Color color2, float widthFactor)
        {
            if (Event.current.type != EventType.repaint)
                return;

            frame -= cdata.firstFrame;
            if (frame < 0)
                return;
            float len = cdata.NumberOfFrames;
            HandleUtility.ApplyWireMaterial();
            GL.Begin(GL.QUADS);
            GL.Color(color1);
            GL.Vertex3(r.x + r.width / len * frame, r.y + 1, 0);
            GL.Vertex3(r.x + r.width / len * frame + r.width / len, r.y + 1, 0);

            GL.Color(color2);
            GL.Vertex3(r.x + r.width / len * frame + r.width / len, r.yMax, 0);
            GL.Vertex3(r.x + r.width / len * frame, r.yMax, 0);
            GL.End();
        }

        private void DrawMaxValueScale(ChartData cdata, Rect r)
        {
            Handles.Label(new Vector3(r.x + r.width / 2 - 20, r.yMin + 2, 0), "Scale: " + cdata.maxValue);
        }

        private void DrawChartLine(int selectedFrame, ChartData cdata, Rect r)
        {
            for (int i = 0; i < cdata.charts.Length; i++)
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

        private void DrawChartStacked(int selectedFrame, ChartData cdata, Rect r)
        {
            HandleUtility.ApplyWireMaterial();

            float[] sumbuf = new float[cdata.NumberOfFrames];

            for (int i = 0; i < cdata.charts.Length; i++)
            {
                if (cdata.hasOverlay)
                    DrawChartItemStackedOverlay(r, i, cdata, sumbuf);
                DrawChartItemStacked(r, i, cdata, sumbuf);
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
                    ms_Styles.selectedLabel);
            }
        }

        internal static void DoLabel(float x, float y, string text, float alignment)
        {
            if (string.IsNullOrEmpty(text))
                return;

            GUIContent content = new GUIContent(text);
            Vector2 size = ms_Styles.whiteLabel.CalcSize(content);
            Rect r = new Rect(x + size.x * alignment, y, size.x, size.y);

            EditorGUI.DoDropShadowLabel(r, content, ms_Styles.whiteLabel, .3f);
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
            Vector2 size = ms_Styles.whiteLabel.CalcSize(content);

            return size.y;
        }

        private void DrawLabelsStacked(int selectedFrame, ChartData cdata, Rect r)
        {
            if (cdata.selectedLabels == null)
                return;

            int length = cdata.NumberOfFrames;
            if (selectedFrame < cdata.firstSelectableFrame || selectedFrame >= cdata.firstFrame + length)
                return;
            selectedFrame -= cdata.firstFrame;

            float frameWidth = r.width / length;
            float xpos = r.x + frameWidth * selectedFrame;
            float scale = cdata.scale[0] * r.height;

            float[] ypositions = new float[cdata.charts.Length];
            float[] heights = new float[ypositions.Length];

            float accum = 0.0f;
            for (int s = 0; s < cdata.charts.Length; ++s)
            {
                ypositions[s] = -1;
                heights[s] = 0;

                int index = cdata.chartOrder[s];

                if (!cdata.charts[index].enabled)
                    continue;

                float value = cdata.charts[index].data[selectedFrame];
                if (value == -1.0f)
                    continue;

                float labelValue = cdata.hasOverlay ? cdata.charts[index].overlayData[selectedFrame] : value;
                // only draw labels for large enough stacks
                if (labelValue * scale > 5.0f)
                {
                    // place value in the middle of this stack vertically
                    ypositions[s] = (accum + labelValue * 0.5f) * scale;
                    heights[s] = GetLabelHeight(cdata.selectedLabels[index]);
                }

                // accumulate stacked value
                accum += value;
            }

            CorrectLabelPositions(ypositions, heights, r.height);

            for (int s = 0; s < cdata.charts.Length; ++s)
            {
                if (heights[s] > 0)
                {
                    int index = cdata.chartOrder[s];

                    // tint color slightly towards white
                    Color clr = cdata.charts[index].color;
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

        private void DrawGridStacked(Rect r, ChartData cdata)
        {
            if (Event.current.type != EventType.repaint ||
                cdata.grid == null || cdata.gridLabels == null)
                return;

            GL.Begin(GL.LINES);
            GL.Color(new Color(1, 1, 1, 0.2f));
            for (int i = 0; i < cdata.grid.Length; ++i)
            {
                float y = r.y + r.height - cdata.grid[i] * cdata.scale[0] * r.height;
                if (y > r.y)
                {
                    GL.Vertex3(r.x + 80, y, 0.0f);
                    GL.Vertex3(r.x + r.width, y, 0.0f);
                }
            }
            GL.End();

            for (int i = 0; i < cdata.grid.Length; ++i)
            {
                float y = r.y + r.height - cdata.grid[i] * cdata.scale[0] * r.height;
                if (y > r.y)
                {
                    DoLabel(r.x + 5, y - 8, cdata.gridLabels[i], 0.0f);
                }
            }
        }

        private void DrawLabelsLine(int selectedFrame, ChartData cdata, Rect r)
        {
            if (cdata.selectedLabels == null)
                return;
            int length = cdata.NumberOfFrames;

            if (selectedFrame < cdata.firstSelectableFrame || selectedFrame >= cdata.firstFrame + length)
                return;
            selectedFrame -= cdata.firstFrame;

            float[] ypositions = new float[cdata.charts.Length];
            float[] heights = new float[ypositions.Length];

            for (int s = 0; s < cdata.charts.Length; ++s)
            {
                ypositions[s] = -1;
                heights[s] = 0;

                float value = cdata.charts[s].data[selectedFrame];
                if (value != -1)
                {
                    ypositions[s] = value * cdata.scale[s] * r.height;
                    heights[s] = GetLabelHeight(cdata.selectedLabels[s]);
                }
            }

            CorrectLabelPositions(ypositions, heights, r.height);

            float frameWidth = r.width / length;
            float xpos = r.x + frameWidth * selectedFrame;

            for (int s = 0; s < cdata.charts.Length; ++s)
            {
                if (heights[s] > 0)
                {
                    // color the label slightly (half way between line color and white)
                    Color clr = cdata.charts[s].color;
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

        private void DrawChartItemLine(Rect r, ChartData cdata, int index)
        {
            if (!cdata.charts[index].enabled)
                return;

            Color clr = cdata.charts[index].color;

            int total = cdata.NumberOfFrames;
            int start = -cdata.firstFrame;
            start = Mathf.Clamp(start, 0, total);
            int len = total - start;
            if (len <= 0)
                return;

            if (m_CachedLineData == null || total > m_CachedLineData.Length)
                m_CachedLineData = new Vector3[total];

            float step = r.width / total;
            float x = r.x + step * 0.5f + start * step;
            float rectHeight = r.height;
            float rectY = r.y;
            for (int i = start; i < total; i++, x += step)
            {
                float y = 0;
                y = rectY + rectHeight;
                if (cdata.charts[index].data[i] != -1)
                {
                    float val = cdata.charts[index].data[i] * cdata.scale[index] * rectHeight;
                    y -= val;
                }
                m_CachedLineData[i - start].Set(x, y, 0.0f);
            }

            /*
            if (index != 0)
                return;
            pts = new Vector3[6];
            pts[0] = new Vector3 (r.x + 105, r.y + 5, 0);
            pts[1] = new Vector3 (r.x + 155, r.y + 10, 0);
            pts[2] = new Vector3 (r.x + 165, r.y + 63, 0);
            pts[3] = new Vector3 (r.x + 200, r.y + 7, 0);
            pts[4] = new Vector3 (r.x + 300, r.y + 7, 0);
            pts[5] = new Vector3 (r.x + 300, r.y + 100, 0);
            */

            Handles.color = clr;
            Handles.DrawAAPolyLine(2.0F, len, m_CachedLineData);
        }

        private void DrawChartItemStacked(Rect r, int index, ChartData cdata, float[] sumbuf)
        {
            if (Event.current.type != EventType.repaint)
                return;

            int len = cdata.NumberOfFrames;
            float step = r.width / len;

            index = cdata.chartOrder[index];

            if (!cdata.charts[index].enabled)
                return;

            Color clr = cdata.charts[index].color;
            if (cdata.hasOverlay)
            {
                clr.r *= 0.9f;
                clr.g *= 0.9f;
                clr.b *= 0.9f;
                clr.a *= 0.4f;
            }
            Color clr2 = clr;

            clr2.r *= 0.8f;
            clr2.g *= 0.8f;
            clr2.b *= 0.8f;
            clr2.a *= 0.8f;

            GL.Begin(GL.TRIANGLE_STRIP);

            float x = r.x + step * 0.5f;
            float rectHeight = r.height;
            float rectY = r.y;
            for (int i = 0; i < len; i++, x += step)
            {
                float y = rectY + rectHeight - sumbuf[i];

                float value = cdata.charts[index].data[i];
                if (value == -1.0f)
                    continue;

                float val = value * cdata.scale[0] * rectHeight;
                if (y - val < r.yMin)
                    val = y - r.yMin; // Clamp the values to be inside drawrect
                GL.Color(clr);
                GL.Vertex3(x, y - val, 0); // clip chart top
                GL.Color(clr2);
                GL.Vertex3(x, y, 0);

                sumbuf[i] += val;
            }
            GL.End();
        }

        private void DrawChartItemStackedOverlay(Rect r, int index, ChartData cdata, float[] sumbuf)
        {
            if (Event.current.type != EventType.repaint)
                return;

            int len = cdata.NumberOfFrames;
            float step = r.width / len;

            index = cdata.chartOrder[index];

            if (!cdata.charts[index].enabled)
                return;

            Color clr = cdata.charts[index].color;
            Color clr2 = clr;

            clr2.r *= 0.8f;
            clr2.g *= 0.8f;
            clr2.b *= 0.8f;
            clr2.a *= 0.8f;

            GL.Begin(GL.TRIANGLE_STRIP);

            float x = r.x + step * 0.5f;
            float rectHeight = r.height;
            float rectY = r.y;
            for (int i = 0; i < len; i++, x += step)
            {
                float y = rectY + rectHeight - sumbuf[i];

                float value = cdata.charts[index].overlayData[i];
                if (value == -1.0f)
                    continue;

                float val = value * cdata.scale[0] * rectHeight;
                GL.Color(clr);
                GL.Vertex3(x, y - val, 0);
                GL.Color(clr2);
                GL.Vertex3(x, y, 0);
            }
            GL.End();
        }

        protected virtual void DrawLabelDragger(ChartType type, Rect r, ChartData cdata)
        {
            Vector2 mousePos = Event.current.mousePosition;
            if (type == ChartType.StackedFill)
            {
                int idx = 0;
                for (int i = cdata.charts.Length - 1; i >= 0; --i, ++idx)
                {
                    Rect pos = (m_DragItemIndex == i) ?
                        new Rect(r.x, mousePos.y - m_DragDownPos.y, kSideWidth, kLabelHeight) :
                        new Rect(r.x, r.y + kDistFromTopToFirstLabel + idx * kLabelHeight, kSideWidth, kLabelHeight);
                    if (cdata.charts[cdata.chartOrder[i]].enabled)
                        GUI.backgroundColor = cdata.charts[cdata.chartOrder[i]].color;
                    else
                        GUI.backgroundColor = Color.black;
                    GUI.Label(pos, cdata.charts[cdata.chartOrder[i]].name, ms_Styles.paneSubLabel);
                }
            }
            else
            {
                for (int i = 0; i < cdata.charts.Length; ++i)
                {
                    GUI.backgroundColor = cdata.charts[i].color;
                    var name = cdata.charts[i].name;
                    DrawSubLabel(r, i, name);
                }
            }
            GUI.backgroundColor = Color.white;
        }

        protected static void DrawSubLabel(Rect r, int i, string name)
        {
            Rect pos;
            pos = new Rect(r.x, r.y + kDistFromTopToFirstLabel + i * kLabelHeight, kSideWidth, kLabelHeight);
            GUI.Label(pos, name, ms_Styles.paneSubLabel);
        }

        protected internal virtual void LabelDraggerDrag(int chartControlID, ChartType chartType, ChartData cdata, Rect r, bool active)
        {
            // Line charts don't need label reordering.
            // Inactive charts also don't need that.
            if (chartType == ChartType.Line || !active)
                return;

            Event evt = Event.current;
            EventType type = evt.GetTypeForControl(chartControlID);

            if ((type != EventType.MouseDown && type != EventType.MouseUp && type != EventType.KeyDown && type != EventType.MouseDrag))
                return;

            if (type == EventType.KeyDown && evt.keyCode == KeyCode.Escape && m_DragItemIndex != -1)
            {
                GUIUtility.hotControl = 0;
                System.Array.Copy(m_ChartOrderBackup, cdata.chartOrder, m_ChartOrderBackup.Length);
                m_DragItemIndex = -1;
                evt.Use();
            }

            int idx = 0;
            for (int i = cdata.charts.Length - 1; i >= 0; --i, ++idx)
            {
                if ((evt.type == EventType.MouseUp && m_MouseDownIndex != -1) || evt.type == EventType.MouseDown)
                {
                    var togglePos = GetToggleRect(r, idx);
                    if (togglePos.Contains(evt.mousePosition))
                    {
                        m_DragItemIndex = -1;
                        if (evt.type == EventType.MouseUp && m_MouseDownIndex == i)
                        {
                            m_MouseDownIndex = -1;
                            cdata.charts[cdata.chartOrder[i]].enabled = !cdata.charts[cdata.chartOrder[i]].enabled;

                            if (chartType == ChartType.StackedFill)
                                SaveChartsSettingsEnabled(cdata);
                        }
                        else
                        {
                            m_MouseDownIndex = i;
                        }
                        evt.Use();
                    }
                }
                if (evt.type == EventType.MouseDown)
                {
                    Rect pos = new Rect(r.x, r.y + kDistFromTopToFirstLabel + idx * kLabelHeight, kSideWidth, kLabelHeight);
                    if (pos.Contains(evt.mousePosition))
                    {
                        m_MouseDownIndex = -1;
                        m_DragItemIndex = i;
                        m_DragDownPos = evt.mousePosition;
                        m_DragDownPos.x -= pos.x;
                        m_DragDownPos.y -= pos.y;

                        m_ChartOrderBackup = new int[cdata.chartOrder.Length];
                        System.Array.Copy(cdata.chartOrder, m_ChartOrderBackup, m_ChartOrderBackup.Length);

                        GUIUtility.hotControl = chartControlID;
                        Event.current.Use();
                    }
                }
                else if (m_DragItemIndex != -1 && type == EventType.MouseDrag && i != m_DragItemIndex)
                {
                    float mouseY = evt.mousePosition.y;
                    float y = r.y + kDistFromTopToFirstLabel + idx * kLabelHeight;
                    if (mouseY >= y && mouseY < y + kLabelHeight)
                    {
                        int foo = cdata.chartOrder[i];
                        cdata.chartOrder[i] = cdata.chartOrder[m_DragItemIndex];
                        cdata.chartOrder[m_DragItemIndex] = foo;

                        m_DragItemIndex = i;

                        SaveChartsSettingsOrder(cdata);
                    }
                }
            }

            if (type == EventType.MouseDrag && m_DragItemIndex != -1)
                evt.Use(); // repaint when dragging

            if (type == EventType.MouseUp && GUIUtility.hotControl == chartControlID)
            {
                GUIUtility.hotControl = 0;
                m_DragItemIndex = -1;
                evt.Use();
            }
        }

        protected static Rect GetToggleRect(Rect r, int idx)
        {
            return new Rect(r.x + 10 + kLabelXOffset, r.y + kDistFromTopToFirstLabel + idx * kLabelHeight, 9, 9);
        }

        private void LoadChartsSettings(ChartData cdata)
        {
            if (string.IsNullOrEmpty(m_ChartSettingsName))
                return;

            var str = EditorPrefs.GetString(m_ChartSettingsName + "Order");
            if (!string.IsNullOrEmpty(str))
            {
                try
                {
                    var values = str.Split(',');
                    if (values.Length == cdata.charts.Length)
                        for (var i = 0; i < cdata.charts.Length; i++)
                            cdata.chartOrder[i] = int.Parse(values[i]);
                }
                catch (FormatException) {}
            }

            str = EditorPrefs.GetString(m_ChartSettingsName + "Visible");

            for (var i = 0; i < cdata.charts.Length; i++)
                if (i < str.Length && str[i] == '0')
                    cdata.charts[i].enabled = false;
        }

        private void SaveChartsSettingsOrder(ChartData cdata)
        {
            if (string.IsNullOrEmpty(m_ChartSettingsName))
                return;

            var str = string.Empty;

            for (var i = 0; i < cdata.charts.Length; i++)
            {
                if (str.Length != 0)
                    str += ",";
                str += cdata.chartOrder[i];
            }

            EditorPrefs.SetString(m_ChartSettingsName + "Order", str);
        }

        protected void SaveChartsSettingsEnabled(ChartData cdata)
        {
            var str = string.Empty;

            for (var i = 0; i < cdata.charts.Length; i++)
                str += cdata.charts[i].enabled ? '1' : '0';

            EditorPrefs.SetString(m_ChartSettingsName + "Visible", str);
        }
    }

    internal class ChartSeries
    {
        public string identifierName;
        public string name;
        public float[] data;
        public float[] overlayData;
        public Color color;
        public bool enabled;

        public ChartSeries(string name, int len, Color clr)
        {
            this.name = name;
            this.identifierName = name;
            data = new float[len];
            overlayData = null;
            color = clr;
            enabled = true;
        }

        public void CreateOverlayData()
        {
            overlayData = new float[data.Length];
        }
    }

    internal class ChartData
    {
        public ChartSeries[] charts;
        public int[] chartOrder;
        public float[] scale;
        public float[] grid;
        public string[] gridLabels;
        public string[] selectedLabels;
        public int firstFrame;
        public int firstSelectableFrame;
        public bool hasOverlay;
        public float maxValue;

        public int NumberOfFrames { get { return charts[0].data.Length; } }


        public void Assign(ChartSeries[] items, int firstFrame, int firstSelectableFrame)
        {
            this.charts = items;
            this.firstFrame = firstFrame;
            this.firstSelectableFrame = firstSelectableFrame;

            if (chartOrder == null || chartOrder.Length != items.Length)
            {
                chartOrder = new int[items.Length];
                for (int i = 0; i < chartOrder.Length; i++)
                    chartOrder[i] = chartOrder.Length - 1 - i;
            }
        }

        public void AssignScale(float[] scale)
        {
            this.scale = scale;
        }

        public void SetGrid(float[] grid, string[] labels)
        {
            this.grid = grid;
            this.gridLabels = labels;
        }
    }
}
