// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;
using Color = UnityEngine.Color;

namespace UnityEditor.Audio.UIElements
{
    internal class Tickmarks : VisualElement
    {
        private interface Scale
        {
            int DivisionCount();
            string[] Labels();
            int[] SubDivisionCount();
            float[] LabelOffsets();
        }

        private struct LargeScale : Scale
        {
            public int DivisionCount() { return 15; }
            public string[] Labels() { return new string[] { "80", "70", "60", "50", "40", "30", "24", "21", "18", "15", "12", "9", "6", "3", "0" }; }
            public int[] SubDivisionCount() { return new int[] { 4, 4, 4, 4, 4, 5, 2, 2, 2, 2, 2, 2, 2, 2 };  }
            public float[] LabelOffsets() { return new float[] { -6, -6, -6, -6, -6, -6, -6, -6, -6, -6, -6, -3.5f, -3.5f, -3.5f, -3.5f }; }
        }

        private struct CompactScale : Scale
        {
            public int DivisionCount() { return 8; }
            public string[] Labels() { return new string[] { "80", "60", "40", "24", "18", "12", "6", "0" }; }
            public int[] SubDivisionCount() { return new int[] { 4, 4, 3, 2, 2, 2, 2 };  }
            public float[] LabelOffsets() { return new float[] { -6, -6, -6, -6, -6, -6, -3.5f, -3.5f }; }
        }

        private struct MiniScale : Scale
        {
            public int DivisionCount() { return 5; }
            public string[] Labels() { return new string[] { "80", "45", "21", "12", "0" }; }
            public int[] SubDivisionCount() { return new int[] { 6, 3, 2, 3 };  }
            public float[] LabelOffsets() { return new float[] { -6, -6, -6, -6, -3.5f }; }
        }

        [Serializable]
        public new class UxmlSerializedData : VisualElement.UxmlSerializedData
        {
            public override object CreateInstance() => new Tickmarks();
        }

        public Tickmarks() : base()
        {
            generateVisualContent += context => GenerateVisualContent(context);
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            RegisterCallback<CustomStyleResolvedEvent>(CustomStylesResolved);
        }

        private static void CustomStylesResolved(CustomStyleResolvedEvent evt)
        {
            Tickmarks element = (Tickmarks)evt.currentTarget;
            element.GetColorsFromStylesheet();
        }

        private static readonly CustomStyleProperty<Color> s_TickmarkColorProperty = new("--tickmark-color");

        private Color m_TickmarkColor;

        private void GetColorsFromStylesheet()
        {
            if (customStyle.TryGetValue(s_TickmarkColorProperty, out var tickmarkColor))
            {
                m_TickmarkColor = tickmarkColor;
            }
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            MarkDirtyRepaint();
        }

        private static void GenerateVisualContent(MeshGenerationContext context)
        {
            var painter2D = context.painter2D;

            var contentRect = context.visualElement.contentRect;

            Scale scale = contentRect.width > 350 ? new LargeScale() : (contentRect.width > 175 ? new CompactScale() : new MiniScale());

            var tickmarkColor = (context.visualElement as Tickmarks).m_TickmarkColor;
            var textColor = tickmarkColor.RGBMultiplied(1.2f); // Compensate for a bug in UI Toolkit which causes MeshGenerationContext.DrawText to render text lighter than the color that you provide.

            for (int index = 0; index < scale.DivisionCount(); index += 1)
            {
                var pos = contentRect.width * index / (scale.DivisionCount() - 1.0f);

                var rect = new Rect(pos - 0.5f, 5.0f, 1.0f, 8.0f);

                painter2D.fillColor = tickmarkColor;
                painter2D.BeginPath();
                painter2D.MoveTo(new Vector2(rect.xMin, rect.yMin));
                painter2D.LineTo(new Vector2(rect.xMax, rect.yMin));
                painter2D.LineTo(new Vector2(rect.xMax, rect.yMax));
                painter2D.LineTo(new Vector2(rect.xMin, rect.yMax));
                painter2D.ClosePath();
                painter2D.Fill();
            }

            for (int index = 0; index < scale.DivisionCount() - 1; index += 1)
            {
                var pos = contentRect.width * index / (scale.DivisionCount() - 1.0f);
                var spacing = contentRect.width / (scale.DivisionCount() - 1.0f);
                var subDivCount = scale.SubDivisionCount()[index];

                for (int subIndex = 0; subIndex < subDivCount; subIndex++)
                {
                    var rect = new Rect(pos - 0.5f + spacing * (subIndex + 1.0f) / (subDivCount + 1.0f), 5.0f, 1.0f, 4.0f);

                    painter2D.fillColor = tickmarkColor;
                    painter2D.BeginPath();
                    painter2D.MoveTo(new Vector2(rect.xMin, rect.yMin));
                    painter2D.LineTo(new Vector2(rect.xMax, rect.yMin));
                    painter2D.LineTo(new Vector2(rect.xMax, rect.yMax));
                    painter2D.LineTo(new Vector2(rect.xMin, rect.yMax));
                    painter2D.ClosePath();
                    painter2D.Fill();
                }
            }

            for (int index = 0; index < scale.DivisionCount(); index += 1)
            {
                var pos = contentRect.width * index / (scale.DivisionCount() - 1.0f);

                var rect = new Rect(pos - 10.0f, 10, 20.0f, 20.0f);

                context.DrawText(scale.Labels()[index], new Vector2(pos + scale.LabelOffsets()[index], 14.0f), 10.0f, textColor);
            }
        }
    }
}
