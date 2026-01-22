// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Profiling.Editor
{
    internal class LineChartWidget : ChartWidget
    {
        static readonly ProfilerMarker k_GeometryUpdate = new($"{nameof(LineChartWidget)}.UpdateGeometry");

        public LineChartWidget(ChartModel model, VisualElement root) :
            base(model, root)
        {
        }

        protected override void UpdateGeometry(MeshGenerationContext mgc)
        {
            using var _ = k_GeometryUpdate.Auto();

            var workArea = Root.contentRect;

            var painter = mgc.painter2D;

            var model = Model;
            for (int series = 0; series < model.numSeries; series++)
            {
                var seriesValues = model.series[series];
                if (!seriesValues.enabled)
                    continue;

                var seriesLength = seriesValues.yValues.Length;
                if (seriesLength == 0)
                    continue;
                var seriesRange = seriesValues.rangeAxis;

                painter.strokeColor = seriesValues.color;
                painter.lineCap = LineCap.Butt;

                painter.BeginPath();
                var posY = (seriesValues.yValues[0] - seriesRange.x) / seriesRange.y;
                painter.MoveTo(new Vector2(workArea.x, (1 - posY) * workArea.height + workArea.y));
                bool firstValue = true;

                for (int i = 1; i < seriesLength; i++)
                {
                    if (seriesValues.yValues[i] == -1)
                    {
                        // skip this value
                    }
                    else
                    {
                        var unitX = ((float)i) / seriesLength;
                        var unitY = (seriesValues.yValues[i] - seriesRange.x) / seriesRange.y;
                        if (firstValue)
                        {
                            painter.MoveTo(new Vector2(unitX * workArea.width + workArea.x, (1 - unitY) * workArea.height + workArea.y));
                            firstValue = false;
                        }
                        else
                        {
                            painter.LineTo(new Vector2(unitX * workArea.width + workArea.x, (1 - unitY) * workArea.height + workArea.y));
                        }
                    }
                }
                painter.Stroke();
            }
        }
    }
}
