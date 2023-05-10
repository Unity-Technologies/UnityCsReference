// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace Unity.Profiling.Editor.UI
{
    // This will become more relevant once we get to the redesign and refactoring of all charts to UIToolkit. For now it simply serves as a base for the Bottlenecks chart to make future migration easier.
    // It uses 'width + offset' rendering approach to make it compatible with external scroll/zoom control that applies to multiple graphs and can render subsets of the data.
    abstract class GraphView : VisualElement
    {
        const int k_DefaultUnitWidth = 20;

        // The width of one unit on the graph's horizontal x-axis, in points.
        public float UnitWidth { get; set; } = k_DefaultUnitWidth;

        // The horizontal offset to begin rendering the graph's data, in points.
        public float HorizontalOffset { get; set; }
    }
}
