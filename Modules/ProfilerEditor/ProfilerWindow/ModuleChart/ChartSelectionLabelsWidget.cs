// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Profiling;

namespace Unity.Profiling.Editor
{
    /// <summary>
    /// ChartSelectionLabelsWidget visualize chart label around selection frame
    ///
    /// TODO: instead of this complex calculations, it can be done via UITK.
    /// Two VisualElements left/right around selection:
    /// - position absolute, fixed width (like 100px), attached to left/right position
    /// - flex direction: column
    /// - add per-chart series element to each side with flex weight equals to chart value
    /// - add labels in elements which should show labels, interleaving left/right (if not at edge)
    /// - flex will handle spring-positioning, labels should stay well positioned due to min-width
    /// </summary>
    internal class ChartSelectionLabelsWidget
    {
        struct LabelLayoutData
        {
            public Rect position;
            public float desiredYPosition;
            public float rightJustifyOffset;
            public bool rightJustify;
        }

        const string k_UxmlIdentifier_UssClass_SelectionLabel = "profiler-chart-view__chart__selection__label";
        const int k_LabelLayoutMaxIterations = 5;
        const float k_LabelLerpToWhiteAmount = 0.5f;

        static Vector2 labelRange = new Vector2(Mathf.Epsilon, Mathf.Infinity);

        readonly List<LabelLayoutData> m_LabelData = new List<LabelLayoutData>(16);
        readonly List<int> m_LabelOrder = new(16);
        readonly List<int> m_MostOverlappingLabels = new(16);
        readonly List<int> m_OverlappingLabels = new(16);
        readonly List<float> m_SelectedFrameValues = new(16);

        readonly ProfilerModuleChartType m_ChartType;
        readonly ChartModel m_Model;
        readonly VisualElement m_Root;

        VisualElement m_GroupRoot;
        Label[] m_ChartLabels;

        public ChartSelectionLabelsWidget(ProfilerModuleChartType type, ChartModel model, VisualElement root)
        {
            m_ChartType = type;
            m_Model = model;
            m_Root = root;

            MakeChartLabels();
        }

        public void UpdateChartLabels(long selectedFrame, Rect chartPosition)
        {
            if (UpdateData(selectedFrame, chartPosition))
                UpdateElements();
            else
                HideElements();
        }

        float GetLabelWidth(string text)
        {
            return m_ChartLabels[0].MeasureTextSize(text, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined).x + 5;
        }

        bool UpdateData(long selectedFrame, Rect chartPosition)
        {
            if (selectedFrame == -1)
                return false;
            if ((m_Model == null) || (m_Model.selectedLabels == null) || (m_ChartLabels == null))
                return false;

            // Exit early if the selected frame is outside the domain of the chart
            var domain = m_Model.GetDataDomain();
            var domainSpan = (int)(domain.y - domain.x);

            if (!ProfilerUserSettings.showStatsLabelsOnCurrentFrame && selectedFrame == (m_Model.chartDomainOffset + domainSpan))
                return false;

            if ((domainSpan == 0) || (selectedFrame < m_Model.firstSelectableFrame) || (selectedFrame > m_Model.chartDomainOffset + domainSpan))
                return false;

            var selectedIndex = selectedFrame - m_Model.chartDomainOffset;

            m_LabelOrder.Clear();
            m_LabelOrder.AddRange(m_Model.order);

            // get values of all series and cumulative value of all enabled stacks
            m_SelectedFrameValues.Clear();
            var stacked = m_ChartType.IsStackedChartType();
            var numLabels = 0;

            for (int s = 0; s < m_Model.numSeries; ++s)
            {
                var chartData = m_Model.hasOverlay ? m_Model.overlays[s] : m_Model.series[s];
                var value = chartData.yValues[selectedIndex] * chartData.yScale;
                m_SelectedFrameValues.Add(value);
                if (m_Model.series[s].enabled)
                {
                    ++numLabels;
                }
            }

            if (numLabels == 0)
                return false;

            // populate layout data array with default data
            m_LabelData.Clear();

            var selectedFrameMidline = chartPosition.x + chartPosition.width * ((selectedIndex + 0.5f) / (domain.y - domain.x));

            var maxLabelWidth = 0f;
            numLabels = 0;
            var labelOffsetY = 0f;
            for (int s = 0; s < m_Model.numSeries; ++s)
            {
                var labelData = new LabelLayoutData();
                var chartData = m_Model.series[s];
                var value = m_SelectedFrameValues[s];

                if (chartData.enabled && value >= labelRange.x && value <= labelRange.y)
                {
                    var rangeAxis = chartData.rangeAxis;
                    var rangeSize = rangeAxis.sqrMagnitude == 0f ? 1f : rangeAxis.y * chartData.yScale - rangeAxis.x;

                    // convert stacked series to cumulative value of enabled series
                    if (stacked)
                    {
                        var accumulatedValues = 0f;
                        int currentChartIndex = m_LabelOrder.FindIndex(x => x == s);

                        for (int i = currentChartIndex - 1; i >= 0; --i)
                        {
                            var otherSeriesIdx = m_Model.order[i];
                            var otherChartData = m_Model.hasOverlay ? m_Model.overlays[otherSeriesIdx] : m_Model.series[otherSeriesIdx];
                            bool enabled = m_Model.series[otherSeriesIdx].enabled;

                            if (enabled)
                            {
                                accumulatedValues += otherChartData.yValues[selectedIndex] * otherChartData.yScale;
                            }
                        }
                        // labels for stacked series will be in the middle of their stacks
                        value = accumulatedValues + (0.5f * value);
                    }

                    // default position is left aligned to midline
                    var position = new Vector2(
                        // offset by half a point so there is a 1-point gap down the midline if labels are on both sides
                        selectedFrameMidline + 0.5f,
                        chartPosition.y + chartPosition.height * (1.0f - ((value * chartData.yScale - rangeAxis.x) / rangeSize))
                    );
                    var size = new Vector2(GetLabelWidth(m_Model.selectedLabels[s]), 16);
                    position.y += 0.5f * size.y;
                    position.y = Mathf.Clamp(position.y, chartPosition.yMin, chartPosition.yMax - size.y);

                    labelData.position = new Rect(position, size);
                    labelData.desiredYPosition = labelData.position.center.y + labelOffsetY;
                    labelOffsetY += size.y;

                    ++numLabels;
                }

                m_LabelData.Add(labelData);

                maxLabelWidth = Mathf.Max(maxLabelWidth, labelData.position.width);
            }

            if (numLabels == 0)
                return false;

            // line charts order labels based on series values
            if (!stacked)
                m_LabelOrder.Sort(SortLineLabelIndices);

            // right align labels to the selected frame midline if approaching right border
            if (selectedFrameMidline > chartPosition.x + chartPosition.width - maxLabelWidth)
            {
                for (int s = 0; s < m_Model.numSeries; ++s)
                {
                    var label = m_LabelData[s];
                    label.position.x -= label.position.width;
                    label.rightJustify = true;
                    label.rightJustifyOffset = (chartPosition.x + chartPosition.width - label.position.xMax) + 5;
                    m_LabelData[s] = label;
                }
            }
            // alternate right/left alignment if in the middle
            else if (selectedFrameMidline > chartPosition.x + maxLabelWidth)
            {
                var processed = 0;
                for (int s = 0; s < m_Model.numSeries; ++s)
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
                for (int s1 = 0; s1 < m_Model.numSeries; ++s1)
                {
                    m_OverlappingLabels.Clear();
                    m_OverlappingLabels.Add(s1);

                    if (m_LabelData[s1].position.size.sqrMagnitude == 0f)
                        continue;

                    for (int s2 = 0; s2 < m_Model.numSeries; ++s2)
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

                // keep labels inside chart rect
                if (geometricCenter - 0.5f * totalHeight < chartPosition.yMin)
                    geometricCenter = chartPosition.yMin + 0.5f * totalHeight;
                else if (geometricCenter + 0.5f * totalHeight > chartPosition.yMax)
                    geometricCenter = chartPosition.yMax - 0.5f * totalHeight;

                // account for other rects that will overlap after expanding
                var foundOverlaps = true;
                while (foundOverlaps)
                {
                    foundOverlaps = false;
                    var minY = geometricCenter - 0.5f * totalHeight;
                    var maxY = geometricCenter + 0.5f * totalHeight;
                    for (int s = 0; s < m_Model.numSeries; ++s)
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
                for (int i = 0; i < m_MostOverlappingLabels.Count; ++i)
                {
                    var labelIndex = m_MostOverlappingLabels[i];
                    var label = m_LabelData[labelIndex];
                    label.position.y = geometricCenter - totalHeight * 0.5f + heightAllotted;
                    m_LabelData[labelIndex] = label;
                    heightAllotted += label.position.height;
                }
            }

            return true;
        }

        void UpdateElements()
        {
            for (int s = 0; s < m_Model.numSeries; ++s)
            {
                var labelIndex = m_LabelOrder[s];
                var labelElement = m_ChartLabels[s];

                var labelData = m_LabelData[labelIndex];
                if (labelData.position.size.sqrMagnitude > 0f)
                {
                    if (labelData.rightJustify)
                    {
                        labelElement.style.right = labelData.rightJustifyOffset;
                        labelElement.style.left = StyleKeyword.Auto;
                    }
                    else
                    {
                        labelElement.style.right = StyleKeyword.Auto;
                        labelElement.style.left = labelData.position.xMin;
                    }

                    labelElement.text = m_Model.selectedLabels[labelIndex];
                    labelElement.style.color = Color.Lerp(m_Model.series[labelIndex].color, Color.white, k_LabelLerpToWhiteAmount);
                    labelElement.style.top = labelData.position.yMin;
                    labelElement.style.visibility = Visibility.Visible;
                }
                else if (labelElement.style.visibility == Visibility.Visible)
                {
                    labelElement.style.left = 0;
                    labelElement.style.top = 0;
                    labelElement.style.visibility = Visibility.Hidden;
                }
            }
        }

        void HideElements()
        {
            for (int s = 0; s < m_Model.numSeries; ++s)
            {
                var labelElement = m_ChartLabels[s];
                if (labelElement.style.visibility == Visibility.Hidden)
                    continue;

                labelElement.style.left = 0;
                labelElement.style.top = 0;
                labelElement.style.visibility = Visibility.Hidden;
            }
        }


        int SortLineLabelIndices(int index1, int index2)
        {
            return -m_LabelData[index1].desiredYPosition.CompareTo(m_LabelData[index2].desiredYPosition);
        }

        int SortOverlappingRectIndices(int index1, int index2)
        {
            return -m_LabelOrder.IndexOf(index1).CompareTo(m_LabelOrder.IndexOf(index2));
        }

        float GetGeometricCenter(List<int> overlappingRects, List<LabelLayoutData> labelData, out float totalHeight)
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

        void MakeChartLabels()
        {
            if (m_ChartLabels != null)
                return;

            m_ChartLabels = new Label[m_Model.numSeries];
            m_GroupRoot = new VisualElement();
            m_GroupRoot.name = GetType().Name;
            m_GroupRoot.StretchToParentSize();
            m_Root.Add(m_GroupRoot);
            for (int i = 0; i < m_Model.numSeries; i++)
            {
                var label = new Label();
                label.AddToClassList(k_UxmlIdentifier_UssClass_SelectionLabel);
                label.style.visibility = Visibility.Hidden;
                m_GroupRoot.Add(label);
                m_ChartLabels[i] = label;
            }
        }
    }
}
