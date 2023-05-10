// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;
using Color = UnityEngine.Color;

namespace UnityEditor.Audio.UIElements
{
    internal class AudioLevelMeter : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<AudioLevelMeter, UxmlTraits> { }

        public new class UxmlTraits : VisualElement.UxmlTraits { }

        private float m_Value = -80.0f; // Value of the meter in dBFS.

        public float Value
        {
            get
            {
                return m_Value;
            }

            set
            {
                m_Value = value;
                MarkDirtyRepaint();
            }
        }

        public AudioLevelMeter() : base()
        {
            generateVisualContent += GenerateVisualContent;

            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            MarkDirtyRepaint();
        }

        private const float k_YellowStart = 11.0f / 14.0f;
        private const float k_RedStart    = 13.0f / 14.0f;

        private static float ConvertDBFSToMeterScale(float value)
        {
            // Converts dBFS to the non-linear scale that the meter uses.

            switch (value)
            {
                case <= -80.0f: return 0.0f;
                case <= -30.0f: return value / (10.0f * 14.0f) + 80.0f / (10.0f * 14.0f) + 0.0f / 14.0f;
                case <= -24.0f: return value / ( 6.0f * 14.0f) + 30.0f / ( 6.0f * 14.0f) + 5.0f / 14.0f;
                case <=   0.0f: return value / ( 3.0f * 14.0f) + 24.0f / ( 3.0f * 14.0f) + 6.0f / 14.0f;
                default: return 1.0f;
            }
        }

        private static Rect ShrinkRectBy(Rect rect, RectOffset offset)
        {
            var minX = rect.xMin + offset.left;
            var maxX = rect.xMax - offset.right;
            var minY = rect.yMin + offset.bottom;
            var maxY = rect.yMax - offset.top;

            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }

        static void GenerateVisualContent(MeshGenerationContext context)
        {
            var painter2D = context.painter2D;

            var value = ConvertDBFSToMeterScale((context.visualElement as AudioLevelMeter).m_Value);

            var width = context.visualElement.contentRect.width;

            var rect1 = context.visualElement.contentRect;

            painter2D.fillColor = Color.gray;
            painter2D.BeginPath();
            painter2D.MoveTo(new Vector2(rect1.xMin, rect1.yMin));
            painter2D.LineTo(new Vector2(rect1.xMax, rect1.yMin));
            painter2D.LineTo(new Vector2(rect1.xMax, rect1.yMax));
            painter2D.LineTo(new Vector2(rect1.xMin, rect1.yMax));
            painter2D.ClosePath();
            painter2D.Fill();

            if (value > 0)
            {
                var start = 0.0f;
                var end = width * Mathf.Min(value, k_YellowStart);
                var rect2 = ShrinkRectBy(context.visualElement.contentRect, new RectOffset((int) start, (int) (width - end), 0, 0));

                painter2D.fillColor = Color.green;
                painter2D.BeginPath();
                painter2D.MoveTo(new Vector2(rect2.xMin, rect2.yMin));
                painter2D.LineTo(new Vector2(rect2.xMax, rect2.yMin));
                painter2D.LineTo(new Vector2(rect2.xMax, rect2.yMax));
                painter2D.LineTo(new Vector2(rect2.xMin, rect2.yMax));
                painter2D.ClosePath();
                painter2D.Fill();
            }

            if (value > k_YellowStart)
            {
                var start = width * k_YellowStart;
                var end = width * Mathf.Min(value, k_RedStart);
                var rect2 = ShrinkRectBy(context.visualElement.contentRect, new RectOffset((int) start, (int) (width - end), 0, 0));

                painter2D.fillColor = Color.yellow;
                painter2D.BeginPath();
                painter2D.MoveTo(new Vector2(rect2.xMin, rect2.yMin));
                painter2D.LineTo(new Vector2(rect2.xMax, rect2.yMin));
                painter2D.LineTo(new Vector2(rect2.xMax, rect2.yMax));
                painter2D.LineTo(new Vector2(rect2.xMin, rect2.yMax));
                painter2D.ClosePath();
                painter2D.Fill();
            }

            if (value > k_RedStart)
            {
                var start = width * k_RedStart;
                var end = width * Mathf.Min(value, width);
                var rect2 = ShrinkRectBy(context.visualElement.contentRect, new RectOffset((int) start, (int) (width - end), 0, 0));

                painter2D.fillColor = Color.red;
                painter2D.BeginPath();
                painter2D.MoveTo(new Vector2(rect2.xMin, rect2.yMin));
                painter2D.LineTo(new Vector2(rect2.xMax, rect2.yMin));
                painter2D.LineTo(new Vector2(rect2.xMax, rect2.yMax));
                painter2D.LineTo(new Vector2(rect2.xMin, rect2.yMax));
                painter2D.ClosePath();
                painter2D.Fill();
            }
        }
    }
}
