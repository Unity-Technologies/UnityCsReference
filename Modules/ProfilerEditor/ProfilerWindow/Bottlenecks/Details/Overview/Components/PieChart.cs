// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Profiling.Editor.UI
{
    class PieChart : VisualElement
    {
        PieChartModel m_Model;

        public PieChart()
        {
            AddToClassList("pie-chart");
            generateVisualContent += GenerateVisualContent;
        }

        public void ReloadData(PieChartModel model)
        {
            m_Model = model;
            MarkDirtyRepaint();
        }

        void GenerateVisualContent(MeshGenerationContext mgc)
        {
            var segments = m_Model.Segments;
            if (segments == null)
                return;

            var painter = mgc.painter2D;
            var rect = contentRect;

            // The radius is half the smallest side of our bounds,
            // so the circle always fits within our bounds.
            var radius = Mathf.Min(rect.width, rect.height) * 0.5f;
            var center = rect.center;

            // Start a -90deg to draw segments clockwise from 12 o'clock.
            const float k_PieStartAngle = -90f;
            var arcStartAngle = k_PieStartAngle;
            foreach (var segment in segments)
            {
                var percentage = segment.Percentage;
                if (percentage == 0)
                    continue;

                painter.fillColor = segment.Color;
                painter.BeginPath();

                var scalar = percentage / 100f;
                var angularSize = 360f * scalar;
                var arcEndAngle = arcStartAngle + angularSize;

                painter.Arc(center, radius, arcStartAngle, arcEndAngle);

                // The arc is already a complete shape if it's a full circle.
                if (percentage < 100)
                    painter.LineTo(center);

                painter.ClosePath();
                painter.Fill();

                arcStartAngle = arcEndAngle;
            }
        }

        // [UxmlElement] does no codegen in trunk (6000.2); we have to provide the generated UxmlSerializedData manually.
        [Serializable]
        public new class UxmlSerializedData : VisualElement.UxmlSerializedData
        {
            public override object CreateInstance() => new PieChart();
        }
    }
}
