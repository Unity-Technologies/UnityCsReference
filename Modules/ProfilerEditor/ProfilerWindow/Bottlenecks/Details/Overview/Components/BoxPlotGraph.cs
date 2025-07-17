// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Profiling.Editor.UI
{
    class BoxPlotGraph : VisualElement
    {
        BoxPlotModel m_Model;

        //[UxmlAttribute]
        public Color StrokeColor { get; set; } = Color.white;

        public BoxPlotGraph()
        {
            AddToClassList("box-plot-graph");
            generateVisualContent += GenerateVisualContent;
        }

        public void ReloadData(BoxPlotModel model)
        {
            m_Model = model;
            MarkDirtyRepaint();
        }

        void GenerateVisualContent(MeshGenerationContext mgc)
        {
            const float k_LineWidth = 2f;
            const float k_HalfLineWidth = k_LineWidth / 2f;

            var painter = mgc.painter2D;
            var rect = contentRect;

            // Keep the stroke inside the content rect.
            rect.y += k_HalfLineWidth;
            rect.height -= k_LineWidth;

            painter.lineWidth = k_LineWidth;
            painter.lineCap = LineCap.Round;
            painter.strokeColor = StrokeColor;

            painter.BeginPath();

            // Maximum line.
            painter.MoveTo(new Vector2(rect.min.x, rect.min.y));
            painter.LineTo(new Vector2(rect.max.x, rect.min.y));

            // Minimum line.
            painter.MoveTo(new Vector2(rect.min.x, rect.max.y));
            painter.LineTo(new Vector2(rect.max.x, rect.max.y));

            // The box drawing approach below always separates minimum, maximum, quartile,
            // and median lines by at least a pixel to produce a visually coherent box.
            // This ensures that the box plot still resembles a box plot with extreme data
            // values, which are common in profiling data.
            var model = m_Model;
            var minimumValue = model.Minimum.Value;
            var medianValue = model.Median.Value;
            var maximumValue = model.Maximum.Value;
            var rectHeight = rect.height;

            // Median line.
            var medianNormalized = Mathf.InverseLerp(minimumValue, maximumValue, medianValue);
            var medianHeight = rectHeight * medianNormalized;
            {
                // Median line must be at least 2 line widths and 2 separating pixels away
                // from both the minimum and maximum lines.
                const float spacing = (k_LineWidth * 2f) + 2f;
                medianHeight = Mathf.Clamp(medianHeight, spacing, rectHeight - spacing);
            }
            painter.MoveTo(new Vector2(rect.min.x, rect.max.y - medianHeight));
            painter.LineTo(new Vector2(rect.max.x, rect.max.y - medianHeight));

            // Lower quartile line.
            var lowerQuartileNormalized = Mathf.InverseLerp(minimumValue, maximumValue, model.LowerQuartile);
            var lowerQuartileHeight = rectHeight * lowerQuartileNormalized;
            {
                // Lower quartile line must be at least 1 line width and 1 separating pixel
                // above the minimum line and 1 line width and 1 separating pixel below the
                // median line.
                const float spacing = k_LineWidth + 1f;
                lowerQuartileHeight = Mathf.Clamp(lowerQuartileHeight, spacing, medianHeight - spacing);
            }
            var lowerQuartileY = rect.max.y - lowerQuartileHeight;
            painter.MoveTo(new Vector2(rect.min.x, lowerQuartileY));
            painter.LineTo(new Vector2(rect.max.x, lowerQuartileY));

            // Upper quartile line.
            var upperQuartileNormalized = Mathf.InverseLerp(minimumValue, maximumValue, model.UpperQuartile);
            var upperQuartileHeight = rectHeight * upperQuartileNormalized;
            {
                // Upper quartile line must be at least 1 line width and 1 separating pixel
                // below the maximum line and 1 line width and 1 separating pixel above the
                // median line.
                const float spacing = k_LineWidth + 1f;
                upperQuartileHeight = Mathf.Clamp(upperQuartileHeight, medianHeight + spacing, rectHeight - spacing);
            }
            var upperQuartileY = rect.max.y - upperQuartileHeight;
            painter.MoveTo(new Vector2(rect.min.x, upperQuartileY));
            painter.LineTo(new Vector2(rect.max.x, upperQuartileY));

            // Box left line.
            painter.MoveTo(new Vector2(rect.min.x, lowerQuartileY));
            painter.LineTo(new Vector2(rect.min.x, upperQuartileY));

            // Box right line.
            painter.MoveTo(new Vector2(rect.max.x, lowerQuartileY));
            painter.LineTo(new Vector2(rect.max.x, upperQuartileY));

            // Vertical lines.
            painter.MoveTo(new Vector2(rect.center.x, rect.min.y));
            painter.LineTo(new Vector2(rect.center.x, upperQuartileY));
            painter.MoveTo(new Vector2(rect.center.x, rect.max.y));
            painter.LineTo(new Vector2(rect.center.x, lowerQuartileY));

            painter.Stroke();
        }

        // [UxmlElement] does no codegen in trunk (6000.2); we have to provide the generated UxmlSerializedData manually.
        [Serializable]
        public new class UxmlSerializedData : VisualElement.UxmlSerializedData
        {
            public override object CreateInstance() => new BoxPlotGraph();
        }
    }
}
