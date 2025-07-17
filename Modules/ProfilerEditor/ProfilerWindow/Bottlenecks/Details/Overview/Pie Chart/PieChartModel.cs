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
            public Segment(Color color, string name, ushort percentage)
            {
                Color = color;
                Name = name;
                Percentage = percentage;
            }

            public Color Color { get; }
            public string Name { get; }
            public ushort Percentage { get; }
        }
    }
}
