// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace Unity.Profiling.Editor.UI
{
    readonly struct PieChartModel
    {
        public PieChartModel(Segment[] segments)
        {
            Segments = segments;
        }

        public readonly Segment[] Segments { get; }

        public readonly struct Segment
        {
            public Segment(Color color, string name, ushort percentage, int? dataSeriesIndex = null)
            {
                Color = color;
                Name = name;
                Percentage = percentage;
                DataSeriesIndex = dataSeriesIndex;
            }

            public Color Color { get; }
            public string Name { get; }
            public ushort Percentage { get; }

            // Optional palette index. When set, consumers may re-resolve Color from a
            // caller-supplied palette to honour the colour-blind accessibility setting.
            public int? DataSeriesIndex { get; }
        }
    }
}
