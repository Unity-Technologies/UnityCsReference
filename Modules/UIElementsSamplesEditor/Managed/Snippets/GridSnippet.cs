// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements.Samples
{
    internal class GridSnippet : ElementSnippet<GridSnippet>
    {
        internal override void Apply(VisualElement container)
        {
            /// <sample>
            #region sample
            // Each grid below (declared in GridSnippet.uss / GridSnippet.uxml) isolates one property so
            // its control shows exactly what that property does. The controls drive the grid live.

            List<GridTrackSize> Fractions(int count)
            {
                var tracks = new List<GridTrackSize>();
                for (var i = 0; i < count; ++i)
                    tracks.Add(GridTrackSize.Fraction(1));
                return tracks;
            }

            // Columns: grid-template-columns = repeat(N, 1fr)
            var columnsGrid = container.Q<VisualElement>("columns-grid");
            container.Q<SliderInt>("columns-slider").RegisterValueChangedCallback(
                evt => columnsGrid.style.gridTemplateColumns = Fractions(evt.newValue));

            // Gap: column-gap and row-gap
            var gapGrid = container.Q<VisualElement>("gap-grid");
            container.Q<SliderInt>("gap-slider").RegisterValueChangedCallback(evt =>
            {
                gapGrid.style.columnGap = evt.newValue;
                gapGrid.style.rowGap = evt.newValue;
            });

            // Auto flow: cell numbers are the child order. "3L" is a 2x2 item that leaves a hole the Dense
            // variants backfill with a later item.
            var flowGrid = container.Q<VisualElement>("flow-grid");
            var large = container.Q<VisualElement>("flow-large");
            large.style.gridColumnEnd = GridLine.Span(2); // auto-placed, span 2 columns
            large.style.gridRowEnd = GridLine.Span(2);    // and span 2 rows -> a 2x2 block
            var flow = container.Q<DropdownField>("flow-dropdown");
            flow.choices = new List<string> { "Row", "Column", "Row Dense", "Column Dense" };
            flow.index = 0;
            flow.RegisterValueChangedCallback(evt =>
            {
                flowGrid.style.gridAutoFlow = evt.newValue switch
                {
                    "Column" => GridAutoFlow.Column,
                    "Row Dense" => GridAutoFlow.RowDense,
                    "Column Dense" => GridAutoFlow.ColumnDense,
                    _ => GridAutoFlow.Row
                };
            });

            // Spanning: the accent item spans N columns (grid-column: 1 / 1 + N)
            var spanCell = container.Q<VisualElement>("span-cell");
            spanCell.style.gridColumnStart = 1;
            spanCell.style.gridColumnEnd = 3;
            container.Q<SliderInt>("span-slider").RegisterValueChangedCallback(evt =>
            {
                spanCell.style.gridColumnStart = 1;
                spanCell.style.gridColumnEnd = 1 + evt.newValue;
            });

            // Item alignment: justify-items is the inline (horizontal) axis, align-items the block
            // (vertical) axis. The row is 60px tall so the vertical movement is visible.
            var alignGrid = container.Q<VisualElement>("align-grid");
            Align ToAlign(string v) => v switch
            {
                "start" => Align.FlexStart,
                "center" => Align.Center,
                "end" => Align.FlexEnd,
                _ => Align.Stretch
            };
            var justify = container.Q<DropdownField>("justify-dropdown");
            justify.choices = new List<string> { "start", "center", "end", "stretch" };
            justify.index = 3;
            justify.RegisterValueChangedCallback(evt => alignGrid.style.justifyItems = ToAlign(evt.newValue));

            var align = container.Q<DropdownField>("align-dropdown");
            align.choices = new List<string> { "start", "center", "end", "stretch" };
            align.index = 3;
            align.RegisterValueChangedCallback(evt => alignGrid.style.alignItems = ToAlign(evt.newValue));

            // Responsive: repeat(auto-fill, minmax(N, 1fr)), so the column count adapts to the width
            var responsiveGrid = container.Q<VisualElement>("responsive-grid");
            container.Q<SliderInt>("mintrack-slider").RegisterValueChangedCallback(evt =>
                responsiveGrid.style.gridTemplateColumns = new List<GridTrackSize>
                {
                    GridTrackSize.RepeatAutoFill(
                        GridTrackSize.Minmax(GridTrackSize.Pixels(evt.newValue), GridTrackSize.Fraction(1)))
                });
            #endregion
            /// </sample>
        }
    }
}
