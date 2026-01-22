// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.Profiling.Editor
{
    internal class ChartSeriesViewData
    {
        public string name { get; private set; }
        public string category { get; private set; }
        public Color color { get; private set; }
        public bool enabled;
        public float yScale { get; internal set; }
        public float[] yValues { get; private set; }
        public Vector2 rangeAxis { get; set; }
        public int numDataPoints { get; private set; }
        public readonly int firstXValue = 0;
        public readonly int lastXValue = 0;

        public ChartSeriesViewData(string name, string category, int numDataPoints, Color color)
        {
            this.name = name;
            this.category = category;
            this.color = color;
            this.numDataPoints = numDataPoints;
            yValues = new float[numDataPoints];
            lastXValue = numDataPoints - 1;
            yScale = 1.0f;
            enabled = true;
        }

        // Used by legacy areas that don't use counters and therefore don't have a category.
        public ChartSeriesViewData(string name, int numDataPoints, Color color) : this(name, string.Empty, numDataPoints, color) {}
    }

    internal class ChartModel
    {
        static readonly float[] k_ChartGridValues1ms = { 1000, 250, 100 };
        static readonly string[] k_ChartGridLabels1ms = { "1ms (1000FPS)", "0.25ms (4000FPS)", "0.1ms (10000FPS)" };
        static readonly float[] k_ChartGridValues8ms = { 8333, 4000, 1000 };
        static readonly string[] k_ChartGridLabels8ms = { "8ms (120FPS)", "4ms (250FPS)", "1ms (1000FPS)" };
        static readonly float[] k_ChartGridValues16ms = { 16667, 10000, 5000 };
        static readonly string[] k_ChartGridLabels16ms = { "16ms (60FPS)", "10ms (100FPS)", "5ms (200FPS)" };
        static readonly float[] k_ChartGridValues66ms = { 66667, 33333, 16667 };
        static readonly string[] k_ChartGridLabels66ms = { "66ms (15FPS)", "33ms (30FPS)", "16ms (60FPS)" };

        public ChartSeriesViewData[] series { get; private set; }
        public ChartSeriesViewData[] overlays { get; private set; }
        public int[] order { get; private set; }
        public float[] grid { get; private set; }
        public string[] gridLabels { get; private set; }
        public string[] selectedLabels { get; private set; }

        /// <summary>
        /// if dataAvailable has this bit set, there is data
        /// </summary>
        public const int dataAvailableBit = 1;
        /// <summary>
        /// 0 = No Data
        /// bit 1 set = Data available
        /// >1 = There's a additional info that may provide a reason for missing data
        /// </summary>
        public int[] dataAvailable { get; set; }
        public int firstSelectableFrame { get; private set; }
        public bool hasOverlay { get; set; }
        public float maxValue { get; set; }
        public int numSeries => series.Length;
        public int chartDomainOffset { get; private set; }
        public string Tooltip { get; set; }
        public string Header { get; set; }
        public string HeaderIconName { get; set; }
        public string WarningMsg { get; set; }

        public void Assign(ChartSeriesViewData[] series, int firstFrame, int firstSelectableFrame)
        {
            if (series == null)
                throw new ArgumentNullException(nameof(series));

            this.series = series;
            this.chartDomainOffset = firstFrame;
            this.firstSelectableFrame = firstSelectableFrame;

            if (order == null || order.Length != numSeries)
            {
                order = new int[numSeries];
                for (int i = 0, count = order.Length; i < count; ++i)
                    order[i] = order.Length - 1 - i;
            }

            if (overlays == null || overlays.Length != numSeries)
                overlays = new ChartSeriesViewData[numSeries];
        }

        public void ClearDataAvailableBuffer()
        {
            for (int i = 0; i < dataAvailable.Length; ++i)
                dataAvailable[i] = 0;
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
            if (series == null || numSeries == 0)
                return Vector2.zero;

            float minX = float.MaxValue;
            float maxY = float.MinValue;

            for (int i = 0; i < numSeries; ++i)
            {
                if (series[i].numDataPoints == 0)
                    continue;

                minX = Mathf.Min(minX, series[i].firstXValue);
                maxY = Mathf.Max(maxY, series[i].lastXValue);
            }

            if (minX == float.MaxValue)
                return Vector2.zero;

            return new Vector2(minX, maxY);
        }

        public int GetDataDomainLength()
        {
            var domain = GetDataDomain();
            // the domain is a range of indices, logically starting at 0. The Length is therefore the (lastIndex - firstIndex + 1)
            return (int)(domain.y - domain.x) + 1;
        }

        public void UpdateChartGrid(float timeMax, bool showGrid)
        {
            if (!showGrid)
            {
                SetGrid(null, null);
                return;
            }
            if (timeMax < 1500)
            {
                SetGrid(k_ChartGridValues1ms, k_ChartGridLabels1ms);
            }
            else if (timeMax < 10000)
            {
                SetGrid(k_ChartGridValues8ms, k_ChartGridLabels8ms);
            }
            else if (timeMax < 30000)
            {
                SetGrid(k_ChartGridValues16ms, k_ChartGridLabels16ms);
            }
            else
            {
                SetGrid(k_ChartGridValues66ms, k_ChartGridLabels66ms);
            }
        }
    }
}
