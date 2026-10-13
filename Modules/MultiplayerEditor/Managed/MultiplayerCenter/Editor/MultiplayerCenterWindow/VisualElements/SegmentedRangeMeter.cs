// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.Center.Editor
{
    /// <summary>
    /// A read-only meter that shows a range as nine segments coloured from green through yellow to
    /// orange, with a tick at each end of the range.
    /// </summary>
    /// <remarks>
    /// Set <see cref="Range"/> on a 0 to 100 scale; it is widened to whole segments. The track is the
    /// element's own background and border. Segment colours come from the
    /// <c>--segment-low-color</c>, <c>--segment-medium-color</c>, <c>--segment-high-color</c> and
    /// <c>--segment-empty-color</c> custom properties, and the tick colour from <c>color</c>.
    /// </remarks>
    [UxmlElement]
    partial class SegmentedRangeMeter : VisualElement
    {
        const int k_SegmentCount = 9;
        const int k_BandCount = 3;
        const float k_ScaleMaximum = 100f;
        const float k_TickOverhang = 3f;

        static readonly CustomStyleProperty<Color> k_LowColorProperty = new("--segment-low-color");
        static readonly CustomStyleProperty<Color> k_MediumColorProperty = new("--segment-medium-color");
        static readonly CustomStyleProperty<Color> k_HighColorProperty = new("--segment-high-color");
        static readonly CustomStyleProperty<Color> k_EmptyColorProperty = new("--segment-empty-color");

        Vector2 m_Range;
        Color m_LowColor = Color.green;
        Color m_MediumColor = Color.yellow;
        Color m_HighColor = Color.red;
        Color m_EmptyColor = Color.clear;

        /// <summary>
        /// The range to show, as minimum and maximum on a 0 to 100 scale.
        /// </summary>
        [UxmlAttribute, CreateProperty]
        public Vector2 Range
        {
            get => m_Range;
            set
            {
                if (m_Range == value)
                    return;

                m_Range = value;
                MarkDirtyRepaint();
            }
        }

        public SegmentedRangeMeter()
        {
            pickingMode = PickingMode.Ignore;
            generateVisualContent += GenerateVisualContent;
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
        }

        void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            var style = evt.customStyle;
            style.TryGetValue(k_LowColorProperty, out m_LowColor);
            style.TryGetValue(k_MediumColorProperty, out m_MediumColor);
            style.TryGetValue(k_HighColorProperty, out m_HighColor);
            style.TryGetValue(k_EmptyColorProperty, out m_EmptyColor);
            MarkDirtyRepaint();
        }

        void GenerateVisualContent(MeshGenerationContext context)
        {
            var track = contentRect;
            if (track.width <= 0f || track.height <= 0f)
                return;

            var pixelsPerPoint = panel?.scaledPixelsPerPoint ?? 1f;
            var gapPixels = Mathf.Max(1, Mathf.RoundToInt(pixelsPerPoint));
            var startPixel = Mathf.RoundToInt(track.xMin * pixelsPerPoint);
            var trackPixels = Mathf.RoundToInt(track.xMax * pixelsPerPoint) - startPixel;
            var segmentTotalPixels = trackPixels - (k_SegmentCount - 1) * gapPixels;
            if (segmentTotalPixels < k_SegmentCount)
                return;

            var top = Mathf.Round(track.yMin * pixelsPerPoint) / pixelsPerPoint;
            var bottom = Mathf.Round(track.yMax * pixelsPerPoint) / pixelsPerPoint;
            var overhang = Mathf.Max(1, Mathf.RoundToInt(k_TickOverhang * pixelsPerPoint)) / pixelsPerPoint;
            var tickWidth = gapPixels / pixelsPerPoint;
            var tickColor = resolvedStyle.color;

            var (first, last) = LitSegments(m_Range);
            var painter = context.painter2D;

            for (var i = 0; i < k_SegmentCount; ++i)
            {
                var lit = i >= first && i <= last;
                FillRect(painter, SegmentLeft(i), top, SegmentRight(i), bottom, lit ? BandColor(i) : m_EmptyColor);
            }

            FillRect(painter, SegmentLeft(first) - tickWidth, top - overhang, SegmentLeft(first), bottom + overhang, tickColor);
            FillRect(painter, SegmentRight(last), top - overhang, SegmentRight(last) + tickWidth, bottom + overhang, tickColor);

            // whole-pixel segments rarely divide the track evenly, so the spare pixels go one each to
            // segments spread along it, keeping the ends flush with the border
            float SegmentLeft(int index) =>
                (startPixel + index * gapPixels + index * segmentTotalPixels / k_SegmentCount) / pixelsPerPoint;

            float SegmentRight(int index) =>
                (startPixel + index * gapPixels + (index + 1) * segmentTotalPixels / k_SegmentCount) / pixelsPerPoint;
        }

        static (int first, int last) LitSegments(Vector2 range)
        {
            var segmentSize = k_ScaleMaximum / k_SegmentCount;
            var first = Mathf.Clamp(Mathf.CeilToInt(range.x / segmentSize - 0.5f), 0, k_SegmentCount - 1);
            var last = Mathf.Clamp(Mathf.FloorToInt(range.y / segmentSize - 0.5f), first, k_SegmentCount - 1);
            return (first, last);
        }

        Color BandColor(int segmentIndex)
        {
            return (segmentIndex * k_BandCount / k_SegmentCount) switch
            {
                0 => m_LowColor,
                1 => m_MediumColor,
                _ => m_HighColor,
            };
        }

        static void FillRect(Painter2D painter, float left, float top, float right, float bottom, Color color)
        {
            painter.fillColor = color;
            painter.BeginPath();
            painter.MoveTo(new Vector2(left, top));
            painter.LineTo(new Vector2(right, top));
            painter.LineTo(new Vector2(right, bottom));
            painter.LineTo(new Vector2(left, bottom));
            painter.ClosePath();
            painter.Fill();
        }
    }
}
