// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace UnityEngine.UIElements
{
    // What a GridViewComponent tracks for its owner: the tracks of each axis, the lines, the hover and the drag.
    sealed class GridViewComponentState
    {
        internal const int Columns = 0;
        internal const int Rows = 1;

        const float k_Epsilon = 0.01f;

        // UnityEditor.MouseCursor values; panels that don't know them ignore them.
        const int k_ResizeVerticalCursorId = 2;
        const int k_ResizeHorizontalCursorId = 3;

        internal sealed class Axis
        {
            public readonly int index;

            // The component's template, or empty when the tracks come from the owner's style.
            public string attributeText = string.Empty;

            // The tracks the axis started from, and their text, which the view data is matched against.
            public List<GridTrackSize> baseTracks;
            public string templateText;

            // The tracks the user wants. Applied as they are, unless a track collapses or the owner is too small.
            public List<GridTrackSize> desired;
            public readonly List<GridTrackSize> applied = new();
            public List<GridTrackSize> written;

            // The resolved tracks of the last layout.
            public float[] sizes = Array.Empty<float>();
            public float[] offsets = Array.Empty<float>();
            public float contentSize;

            // Per track of the template.
            public bool[] collapsed = Array.Empty<bool>();
            public float[] minSizes = Array.Empty<float>();

            public Axis(int index)
            {
                this.index = index;
            }

            public int resolvedCount => sizes.Length;
            public int templateCount => desired?.Count ?? 0;

            public bool IsCollapsed(int track) => track < collapsed.Length && collapsed[track];
            public float GetMinSize(int track) => track < minSizes.Length ? minSizes[track] : 0f;
        }

        internal struct ItemSpan
        {
            public int first;
            public int last;
            public float min;
            public float max;

            public bool Covers(int track) => first <= track && track <= last;
        }

        internal struct Item
        {
            public bool visible;
            public ItemSpan columns;
            public ItemSpan rows;

            public ItemSpan this[int axis] => axis == Columns ? columns : rows;
        }

        internal struct Divider
        {
            // The line runs across the tracks of the other axis, between the tracks before and after of this axis.
            public int axis;
            public int before;
            public int after;
            public float position;

            // 1 when the track before is on the left or at the top, -1 when it is on the right (right-to-left columns).
            public float direction;
            public bool resizable;
            public int segmentStart;
            public int segmentCount;

            public bool Is(int axis, int before, int after)
                => this.axis == axis && this.before == before && this.after == after;
        }

        readonly VisualElement m_Owner;
        readonly Axis[] m_Axes = { new Axis(Columns), new Axis(Rows) };
        readonly List<Item> m_Items = new();
        List<Divider> m_Dividers = new();
        List<Divider> m_PreviousDividers = new();

        // Start and end of each part of a line, along the line.
        List<Vector2> m_Segments = new();
        List<Vector2> m_PreviousSegments = new();

        readonly GridViewComponentData m_ViewData = new();
        bool m_PersistsViewData;
        bool m_HadHierarchyGeometryChangedEvents;

        int m_HoverAxis = -1;
        int m_HoverBefore;
        int m_HoverAfter;
        bool m_CursorSet;

        int m_DragAxis = -1;
        int m_DragBefore;
        int m_DragAfter;
        int m_DragPointerId = PointerId.invalidPointerId;
        float m_DragStart;
        float m_DragDirection;
        float m_DragMinDelta;
        float m_DragMaxDelta;
        float[] m_DragSizes = Array.Empty<float>();
        readonly List<GridTrackSize> m_DragTracks = new();

        public GridViewComponentState(VisualElement owner)
        {
            m_Owner = owner;
        }

        internal Axis columns => m_Axes[Columns];
        internal Axis rows => m_Axes[Rows];
        internal IReadOnlyList<Divider> dividers => m_Dividers;
        internal IReadOnlyList<Vector2> segments => m_Segments;
        internal bool isDragging => m_DragAxis >= 0;

        public void Attach()
        {
            m_HadHierarchyGeometryChangedEvents = m_Owner.receivesHierarchyGeometryChangedEvents;

            // The lines follow the cells, which can move without the owner changing size.
            m_Owner.receivesHierarchyGeometryChangedEvents = true;
            m_Owner.style.display = DisplayStyle.Grid;

            Refresh();
            LoadViewData();
        }

        public void Detach()
        {
            CancelInteraction();

            m_Owner.receivesHierarchyGeometryChangedEvents = m_HadHierarchyGeometryChangedEvents;
            m_Owner.style.display = StyleKeyword.Null;
            foreach (var axis in m_Axes)
            {
                if (axis.written != null)
                    SetInlineTemplate(axis, new StyleList<GridTrackSize>(StyleKeyword.Null));
                axis.written = null;
            }

            m_Dividers.Clear();
            m_Segments.Clear();
            m_Owner.MarkDirtyRepaint();
        }

        void SetAttributeText(Axis axis, string text)
        {
            text = text?.Trim() ?? string.Empty;
            if (axis.attributeText == text && (text.Length == 0 || axis.baseTracks != null))
                return;

            axis.attributeText = text;
            if (text.Length > 0)
            {
                SetBaseTracks(axis, GridTrackSize.ParseList(text));
                return;
            }

            // Back to the style's tracks, which show again once the inline template is gone.
            axis.baseTracks = null;
            axis.templateText = null;
            axis.desired = null;
            axis.applied.Clear();
            if (axis.written != null)
                SetInlineTemplate(axis, new StyleList<GridTrackSize>(StyleKeyword.Null));
            axis.written = null;
        }

        void SetBaseTracks(Axis axis, List<GridTrackSize> tracks)
        {
            axis.baseTracks = tracks;
            axis.templateText = GridTrackSize.FormatList(tracks);
            axis.desired = RestoreFromViewData(axis) ?? new List<GridTrackSize>(tracks);
        }

        // Takes the tracks from the owner's style when the component gives none, before any inline template hides them.
        void CaptureStyleTracks(Axis axis)
        {
            if (axis.baseTracks != null || axis.attributeText.Length > 0 || axis.written != null)
                return;

            var template = axis.index == Columns
                ? m_Owner.computedStyle.gridTemplateColumns
                : m_Owner.computedStyle.gridTemplateRows;
            if (template.Length > 0)
                SetBaseTracks(axis, new List<GridTrackSize>(template.ToArray()));
        }

        /// <summary>
        /// Brings the applied tracks and the lines up to date with the last layout.
        /// </summary>
        public void Refresh()
        {
            // UXML writes the authored attributes after the component is added, and without a change notification.
            ref var component = ref m_Owner.GetComponentRefOrNullRef<GridViewComponent>();
            if (!Unsafe.IsNullRef(ref component))
            {
                SetAttributeText(m_Axes[Columns], component.columns);
                SetAttributeText(m_Axes[Rows], component.rows);
            }

            if (m_Owner.elementPanel == null)
            {
                foreach (var axis in m_Axes)
                    ApplyWithoutLayout(axis);
                return;
            }

            foreach (var axis in m_Axes)
                CaptureStyleTracks(axis);

            ReadTracks();
            ReadItems();

            foreach (var axis in m_Axes)
            {
                ComputeMinSizes(axis);
                ComputeCollapsedTracks(axis);
                ComputeAppliedTracks(axis);
                WriteAppliedTracks(axis);
            }

            if (BuildDividers())
                m_Owner.MarkDirtyRepaint();

            if (m_HoverAxis >= 0 && FindDivider(m_HoverAxis, m_HoverBefore, m_HoverAfter) < 0)
                SetHover(-1);
        }

        void ApplyWithoutLayout(Axis axis)
        {
            if (axis.desired == null)
                return;

            ComputeAppliedTracks(axis);
            WriteAppliedTracks(axis);
        }

        void ReadTracks()
        {
            var node = m_Owner.layoutNode;
            var contentRect = m_Owner.contentRect;

            // Tracks before grid line 1 are dropped so a track's index is its index in the template.
            var columnAxis = m_Axes[Columns];
            var columnStart = node.GridResolvedExplicitColumnStart;
            Copy(FromExplicitStart(node.GridResolvedColumnSizes, columnStart), ref columnAxis.sizes);
            Copy(FromExplicitStart(node.GridResolvedColumnOffsets, columnStart), ref columnAxis.offsets);
            columnAxis.contentSize = contentRect.width;

            var rowAxis = m_Axes[Rows];
            var rowStart = node.GridResolvedExplicitRowStart;
            Copy(FromExplicitStart(node.GridResolvedRowSizes, rowStart), ref rowAxis.sizes);
            Copy(FromExplicitStart(node.GridResolvedRowOffsets, rowStart), ref rowAxis.offsets);
            rowAxis.contentSize = contentRect.height;

            // Both lists come from the same layout; a mismatch only happens before the first grid layout.
            if (columnAxis.offsets.Length != columnAxis.sizes.Length || rowAxis.offsets.Length != rowAxis.sizes.Length)
            {
                columnAxis.sizes = columnAxis.offsets = Array.Empty<float>();
                rowAxis.sizes = rowAxis.offsets = Array.Empty<float>();
            }
        }

        static ReadOnlySpan<float> FromExplicitStart(ReadOnlySpan<float> tracks, int explicitStart)
            => explicitStart < tracks.Length ? tracks.Slice(explicitStart) : default;

        static void Copy(ReadOnlySpan<float> source, ref float[] destination)
        {
            if (destination.Length != source.Length)
                destination = source.Length == 0 ? Array.Empty<float>() : new float[source.Length];
            source.CopyTo(destination);
        }

        void ReadItems()
        {
            m_Items.Clear();

            var childCount = m_Owner.hierarchy.childCount;
            for (var i = 0; i < childCount; ++i)
            {
                var child = m_Owner.hierarchy[i];
                ref var style = ref child.computedStyle;
                if (style.position == Position.Absolute)
                    continue;

                var item = new Item { visible = style.display != DisplayStyle.None };
                if (!TryReadSpan(child, Columns, style.gridColumnStart, style.gridColumnEnd, item.visible, out item.columns) ||
                    !TryReadSpan(child, Rows, style.gridRowStart, style.gridRowEnd, item.visible, out item.rows))
                    continue;

                m_Items.Add(item);
            }
        }

        // An item placed by its style is found even while hidden. An item placed automatically is found from its
        // geometry, so only while it is displayed: hidden, it has no cell.
        bool TryReadSpan(VisualElement child, int axisIndex, GridLine start, GridLine end, bool visible, out ItemSpan span)
        {
            var axis = m_Axes[axisIndex];
            var trackCount = Math.Max(axis.resolvedCount, axis.templateCount);
            span = default;

            if (TryGetExplicitSpan(start, end, out span.first, out span.last))
            {
                if (span.first >= trackCount)
                    return false;
                span.last = Math.Min(span.last, trackCount - 1);
            }
            else if (!visible || !TryGetSpanFromLayout(child, axis, out span.first, out span.last))
                return false;

            var resolved = child.resolvedStyle;
            float margins, min, max;
            if (axisIndex == Columns)
            {
                margins = resolved.marginLeft + resolved.marginRight;
                min = ValueOr(resolved.minWidth, 0f);
                max = ValueOr(resolved.maxWidth, float.PositiveInfinity);
            }
            else
            {
                margins = resolved.marginTop + resolved.marginBottom;
                min = ValueOr(resolved.minHeight, 0f);
                max = ValueOr(resolved.maxHeight, float.PositiveInfinity);
            }

            span.min = Mathf.Max(0f, min) + margins;
            span.max = max + margins;
            return true;
        }

        static float ValueOr(StyleFloat value, float keywordValue)
            => value.keyword == StyleKeyword.Undefined && !float.IsNaN(value.value) ? value.value : keywordValue;

        // Zero-based, inclusive track indices.
        internal static bool TryGetExplicitSpan(GridLine start, GridLine end, out int first, out int last)
        {
            // Negative lines depend on the explicit track count; callers fall back to the laid-out span.
            if (start.line < 0 || end.line < 0)
            {
                first = last = -1;
                return false;
            }

            if (start.isLine)
            {
                first = start.line - 1;
                if (end.isLine)
                    last = Math.Max(first, end.line - 2);
                else if (end.isSpan)
                    last = first + end.span - 1;
                else
                    last = first;
                return true;
            }

            if (end.isLine && end.line >= 2)
            {
                last = end.line - 2;
                first = Math.Max(0, start.isSpan ? last - start.span + 1 : last);
                return true;
            }

            first = last = -1;
            return false;
        }

        static bool TryGetSpanFromLayout(VisualElement child, Axis axis, out int first, out int last)
        {
            first = last = -1;
            var layout = child.layout;
            var resolved = child.resolvedStyle;
            float start, end;
            if (axis.index == Columns)
            {
                start = layout.xMin - resolved.marginLeft;
                end = layout.xMax + resolved.marginRight;
            }
            else
            {
                start = layout.yMin - resolved.marginTop;
                end = layout.yMax + resolved.marginBottom;
            }

            for (var k = 0; k < axis.resolvedCount; ++k)
            {
                var trackStart = axis.offsets[k];
                var trackEnd = trackStart + axis.sizes[k];
                if (Mathf.Min(end, trackEnd) - Mathf.Max(start, trackStart) <= k_Epsilon)
                    continue;

                if (first < 0)
                    first = k;
                last = k;
            }

            return first >= 0;
        }

        void ComputeMinSizes(Axis axis)
        {
            Resize(ref axis.minSizes, axis.templateCount);
            Array.Clear(axis.minSizes, 0, axis.minSizes.Length);

            foreach (var item in m_Items)
            {
                var span = item[axis.index];
                if (item.visible && span.first == span.last && span.first < axis.minSizes.Length)
                    axis.minSizes[span.first] = Mathf.Max(axis.minSizes[span.first], span.min);
            }
        }

        // A track collapses when it holds hidden items and each displayed item on it also has a track that stays.
        void ComputeCollapsedTracks(Axis axis)
        {
            var count = axis.templateCount;
            Resize(ref axis.collapsed, count);
            Array.Clear(axis.collapsed, 0, count);

            foreach (var item in m_Items)
            {
                if (item.visible)
                    continue;

                var span = item[axis.index];
                for (var k = span.first; k <= span.last && k < count; ++k)
                    axis.collapsed[k] = true;
            }

            for (var k = 0; k < count; ++k)
            {
                if (!axis.collapsed[k])
                    continue;

                foreach (var item in m_Items)
                {
                    var span = item[axis.index];
                    if (item.visible && span.Covers(k) && !HasTrackThatStays(axis, span))
                    {
                        axis.collapsed[k] = false;
                        break;
                    }
                }
            }
        }

        // Reads the candidates before any of them is cleared, so the answer doesn't depend on the order of the tracks.
        static bool HasTrackThatStays(Axis axis, ItemSpan span)
        {
            for (var k = span.first; k <= span.last; ++k)
            {
                if (!axis.IsCollapsed(k))
                    return true;
            }

            return false;
        }

        // Depends on the wanted tracks and the cells only, never on the owner's size: an owner sized by its content
        // would otherwise feed the result back into the next layout.
        void ComputeAppliedTracks(Axis axis)
        {
            axis.applied.Clear();
            if (axis.desired == null)
                return;

            var count = axis.templateCount;
            axis.applied.AddRange(axis.desired);

            var collapsedFlexibleTrack = -1;
            for (var k = 0; k < count; ++k)
            {
                if (!axis.IsCollapsed(k))
                    continue;

                if (collapsedFlexibleTrack < 0 && IsFlexible(axis.applied[k]))
                    collapsedFlexibleTrack = k;
                axis.applied[k] = GridTrackSize.Pixels(0f);
            }

            // The space of a collapsed flexible track goes to the closest track that stays, like the other pane of a
            // collapsed split view.
            if (collapsedFlexibleTrack >= 0 && !HasFlexibleTrack(axis))
            {
                var closest = FindClosestTrackThatStays(axis, collapsedFlexibleTrack);
                if (closest >= 0)
                    axis.applied[closest] = GridTrackSize.Fraction(1f);
            }

            // The cells' minimum size becomes the minimum of their track. A bare fr track would otherwise grow to its
            // cells' content, which a split view must not do. A length track gets its length while there is room for
            // it, and gives up what it has above the minimum when the owner is too small, once the flexible tracks are
            // at their own minimum: the grid fills the length tracks before the flexible ones.
            for (var k = 0; k < count; ++k)
            {
                var track = axis.applied[k];
                if (axis.IsCollapsed(k))
                    continue;

                var minimum = GridTrackSize.Pixels(axis.GetMinSize(k));
                if (IsFlexible(track) && track.minUnit == GridTrackSizeUnit.Auto)
                    axis.applied[k] = GridTrackSize.Minmax(minimum, GridTrackSize.Fraction(track.maxValue));
                else if (IsLength(track))
                    axis.applied[k] = GridTrackSize.Minmax(minimum, track);
            }
        }

        static bool HasFlexibleTrack(Axis axis)
        {
            for (var k = 0; k < axis.applied.Count; ++k)
            {
                if (!axis.IsCollapsed(k) && IsFlexible(axis.applied[k]))
                    return true;
            }

            return false;
        }

        static int FindClosestTrackThatStays(Axis axis, int track)
        {
            for (var distance = 1; distance < axis.applied.Count; ++distance)
            {
                if (track + distance < axis.applied.Count && !axis.IsCollapsed(track + distance))
                    return track + distance;
                if (track - distance >= 0 && !axis.IsCollapsed(track - distance))
                    return track - distance;
            }

            return -1;
        }

        void WriteAppliedTracks(Axis axis)
        {
            if (axis.desired == null)
                return;

            if (axis.written != null && SequenceEqual(axis.written, axis.applied))
                return;

            axis.written = new List<GridTrackSize>(axis.applied);
            SetInlineTemplate(axis, new StyleList<GridTrackSize>(new List<GridTrackSize>(axis.applied)));
        }

        void SetInlineTemplate(Axis axis, StyleList<GridTrackSize> template)
        {
            if (axis.index == Columns)
                m_Owner.style.gridTemplateColumns = template;
            else
                m_Owner.style.gridTemplateRows = template;
        }

        static bool SequenceEqual(List<GridTrackSize> a, List<GridTrackSize> b)
        {
            if (a.Count != b.Count)
                return false;

            for (var i = 0; i < a.Count; ++i)
            {
                if (!a[i].Equals(b[i]))
                    return false;
            }

            return true;
        }

        // Returns whether the lines changed.
        bool BuildDividers()
        {
            (m_PreviousDividers, m_Dividers) = (m_Dividers, m_PreviousDividers);
            (m_PreviousSegments, m_Segments) = (m_Segments, m_PreviousSegments);
            m_Dividers.Clear();
            m_Segments.Clear();

            foreach (var axis in m_Axes)
            {
                var before = -1;
                for (var k = 0; k < axis.resolvedCount; ++k)
                {
                    if (axis.IsCollapsed(k))
                        continue;

                    if (before >= 0)
                        AddDivider(axis, before, k);
                    before = k;
                }
            }

            return !DividersEqual();
        }

        void AddDivider(Axis axis, int before, int after)
        {
            var across = m_Axes[1 - axis.index];
            var divider = new Divider
            {
                axis = axis.index,
                before = before,
                after = after,
                segmentStart = m_Segments.Count,
                resizable = IsResizable(axis, after),
            };

            if (axis.offsets[before] <= axis.offsets[after])
            {
                divider.position = (axis.offsets[before] + axis.sizes[before] + axis.offsets[after]) * 0.5f;
                divider.direction = 1f;
            }
            else
            {
                divider.position = (axis.offsets[after] + axis.sizes[after] + axis.offsets[before]) * 0.5f;
                divider.direction = -1f;
            }

            for (var j = 0; j < across.resolvedCount; ++j)
            {
                if (across.IsCollapsed(j) || !SeparatesCells(axis.index, before, after, j))
                    continue;

                var start = across.offsets[j];
                var end = start + across.sizes[j];

                // Each part reaches to the middle of the gaps on its sides, to meet the lines across it.
                ExtendToNeighbor(across, j, -1, ref start, ref end);
                ExtendToNeighbor(across, j, 1, ref start, ref end);
                AddSegment(divider.segmentStart, start, end);
            }

            divider.segmentCount = m_Segments.Count - divider.segmentStart;
            if (divider.segmentCount > 0)
                m_Dividers.Add(divider);
        }

        static bool IsResizable(Axis axis, int after)
        {
            if (axis.desired == null || after >= axis.templateCount || axis.templateCount > axis.resolvedCount)
                return false;

            foreach (var track in axis.desired)
            {
                if (track.isAutoFill || track.isAutoFit)
                    return false;
            }

            return true;
        }

        // Whether cells meet on both sides of the line in track j of the other axis, with no cell spanning it.
        bool SeparatesCells(int axis, int before, int after, int j)
        {
            var hasBefore = false;
            var hasAfter = false;
            foreach (var item in m_Items)
            {
                if (!item.visible || !item[1 - axis].Covers(j))
                    continue;

                var span = item[axis];
                var coversBefore = span.Covers(before);
                var coversAfter = span.Covers(after);
                if (coversBefore && coversAfter)
                    return false;

                hasBefore |= coversBefore;
                hasAfter |= coversAfter;
            }

            return hasBefore && hasAfter;
        }

        static void ExtendToNeighbor(Axis axis, int track, int step, ref float start, ref float end)
        {
            var neighbor = track + step;
            while (neighbor >= 0 && neighbor < axis.resolvedCount && axis.IsCollapsed(neighbor))
                neighbor += step;
            if (neighbor < 0 || neighbor >= axis.resolvedCount)
                return;

            var neighborStart = axis.offsets[neighbor];
            var neighborEnd = neighborStart + axis.sizes[neighbor];
            if (neighborStart >= end)
                end = (end + neighborStart) * 0.5f;
            else if (neighborEnd <= start)
                start = (neighborEnd + start) * 0.5f;
        }

        void AddSegment(int dividerSegmentStart, float start, float end)
        {
            for (var i = dividerSegmentStart; i < m_Segments.Count; ++i)
            {
                var segment = m_Segments[i];
                if (start <= segment.y + k_Epsilon && end >= segment.x - k_Epsilon)
                {
                    m_Segments[i] = new Vector2(Mathf.Min(start, segment.x), Mathf.Max(end, segment.y));
                    return;
                }
            }

            m_Segments.Add(new Vector2(start, end));
        }

        bool DividersEqual()
        {
            if (m_Dividers.Count != m_PreviousDividers.Count || m_Segments.Count != m_PreviousSegments.Count)
                return false;

            for (var i = 0; i < m_Dividers.Count; ++i)
            {
                var a = m_Dividers[i];
                var b = m_PreviousDividers[i];
                if (!a.Is(b.axis, b.before, b.after) || a.resizable != b.resizable || !Mathf.Approximately(a.position, b.position))
                    return false;
            }

            for (var i = 0; i < m_Segments.Count; ++i)
            {
                if (m_Segments[i] != m_PreviousSegments[i])
                    return false;
            }

            return true;
        }

        int FindDivider(int axis, int before, int after)
        {
            for (var i = 0; i < m_Dividers.Count; ++i)
            {
                if (m_Dividers[i].Is(axis, before, after))
                    return i;
            }

            return -1;
        }

        /// <summary>
        /// The resizable line within half <paramref name="handleSize"/> of <paramref name="localPosition"/>, or -1.
        /// </summary>
        internal int HitTest(Vector2 localPosition, float handleSize)
        {
            var best = -1;
            var bestDistance = Mathf.Max(handleSize, 1f) * 0.5f;
            for (var i = 0; i < m_Dividers.Count; ++i)
            {
                var divider = m_Dividers[i];
                if (!divider.resizable)
                    continue;

                var along = divider.axis == Columns ? localPosition.x : localPosition.y;
                var across = divider.axis == Columns ? localPosition.y : localPosition.x;
                var distance = Mathf.Abs(along - divider.position);
                if (distance > bestDistance || !IsOnSegment(divider, across))
                    continue;

                best = i;
                bestDistance = distance;
            }

            return best;
        }

        bool IsOnSegment(Divider divider, float position)
        {
            for (var i = 0; i < divider.segmentCount; ++i)
            {
                var segment = m_Segments[divider.segmentStart + i];
                if (position >= segment.x && position <= segment.y)
                    return true;
            }

            return false;
        }

        public void OnPointerDown(PointerDownEvent evt, float handleSize)
        {
            if (isDragging || evt.button != (int)MouseButton.LeftMouse)
                return;

            var index = HitTest(m_Owner.WorldToLocal(evt.position), handleSize);
            if (index < 0)
                return;

            BeginDrag(m_Dividers[index], evt.pointerId, m_Owner.WorldToLocal(evt.position));
            m_Owner.CapturePointer(evt.pointerId);
            evt.StopPropagation();
        }

        public void OnPointerMove(PointerMoveEvent evt, float handleSize)
        {
            var localPosition = m_Owner.WorldToLocal(evt.position);
            if (isDragging)
            {
                if (evt.pointerId != m_DragPointerId || !m_Owner.HasPointerCapture(evt.pointerId))
                    return;

                var along = m_DragAxis == Columns ? localPosition.x : localPosition.y;
                var delta = Mathf.Clamp(Mathf.Round(m_DragDirection * (along - m_DragStart)), m_DragMinDelta, m_DragMaxDelta);
                ResizeTracks(delta);
                UpdateCursor();
                evt.StopPropagation();
                return;
            }

            // Another element dragging over the lines keeps its cursor.
            if (m_Owner.panel?.GetCapturingElement(evt.pointerId) != null)
                return;

            SetHover(HitTest(localPosition, handleSize));
        }

        public void OnPointerUp(PointerUpEvent evt, float handleSize)
        {
            if (!isDragging || evt.pointerId != m_DragPointerId)
                return;

            EndDrag();

            // The drag can stop short of the pointer, which may be off the line by now.
            SetHover(HitTest(m_Owner.WorldToLocal(evt.position), handleSize));
            evt.StopPropagation();
        }

        // The panel gives the cursor to the element the pointer enters, and to the element under the pointer when a
        // capture ends, before any callback sees these events.
        public void RestoreCursor()
        {
            if (m_HoverAxis >= 0)
                UpdateCursor();
        }

        public void ClearHover()
        {
            if (!isDragging)
                SetHover(-1);
        }

        public void CancelInteraction()
        {
            EndDrag();
            SetHover(-1);
        }

        void SetHover(int index)
        {
            var axis = index >= 0 ? m_Dividers[index].axis : -1;
            var before = index >= 0 ? m_Dividers[index].before : 0;
            var after = index >= 0 ? m_Dividers[index].after : 0;
            if (axis == m_HoverAxis && (axis < 0 || (before == m_HoverBefore && after == m_HoverAfter)))
            {
                // Restyling the element under the pointer gives it the cursor again, which no event reports.
                RestoreCursor();
                return;
            }

            m_HoverAxis = axis;
            m_HoverBefore = before;
            m_HoverAfter = after;
            UpdateCursor();
            m_Owner.MarkDirtyRepaint();
        }

        // The pointer can be over a cell: the cursor is set on the panel rather than through the owner's style.
        void UpdateCursor()
        {
            var cursorManager = m_Owner.elementPanel?.cursorManager;
            if (cursorManager == null)
                return;

            if (m_HoverAxis >= 0)
            {
                var cursorId = m_HoverAxis == Columns ? k_ResizeHorizontalCursorId : k_ResizeVerticalCursorId;
                cursorManager.SetCursor(new Cursor { defaultCursorId = cursorId });
                m_CursorSet = true;
                return;
            }

            if (!m_CursorSet)
                return;

            m_CursorSet = false;
            var elementUnderPointer = m_Owner.elementPanel.GetTopElementUnderPointer(PointerId.mousePointerId);
            if (elementUnderPointer != null)
                cursorManager.SetCursor(elementUnderPointer.computedStyle.cursor);
            else
                cursorManager.ResetCursor();
        }

        void BeginDrag(Divider divider, int pointerId, Vector2 localPosition)
        {
            var axis = m_Axes[divider.axis];
            m_DragAxis = divider.axis;
            m_DragBefore = divider.before;
            m_DragAfter = divider.after;
            m_DragPointerId = pointerId;
            m_DragStart = divider.axis == Columns ? localPosition.x : localPosition.y;
            m_DragDirection = divider.direction;
            m_DragSizes = (float[])axis.sizes.Clone();
            m_DragTracks.Clear();
            m_DragTracks.AddRange(axis.applied);

            // A cell's minimum or maximum size can change without moving it, which no event reports.
            ReadItems();
            ComputeDragLimits(axis);
            m_HoverAxis = divider.axis;
            m_HoverBefore = divider.before;
            m_HoverAfter = divider.after;
            m_Owner.MarkDirtyRepaint();
        }

        // The track before grows by the delta and the track after shrinks by it. Neither goes below zero or its own
        // minimum, and no cell on one side only goes past its minimum or maximum size. A cell already past them is
        // only kept from going further.
        void ComputeDragLimits(Axis axis)
        {
            var before = m_DragBefore;
            var after = m_DragAfter;
            var min = -m_DragSizes[before] + GetTrackMinimum(m_DragTracks[before]);
            var max = m_DragSizes[after] - GetTrackMinimum(m_DragTracks[after]);

            foreach (var item in m_Items)
            {
                if (!item.visible)
                    continue;

                var span = item[axis.index];
                var coversBefore = span.Covers(before);
                if (coversBefore == span.Covers(after))
                    continue;

                var size = GetExtent(axis, span);
                if (coversBefore)
                {
                    min = Mathf.Max(min, Mathf.Min(0f, span.min - size));
                    max = Mathf.Min(max, Mathf.Max(0f, span.max - size));
                }
                else
                {
                    max = Mathf.Min(max, Mathf.Max(0f, size - span.min));
                    min = Mathf.Max(min, Mathf.Min(0f, size - span.max));
                }
            }

            m_DragMinDelta = Mathf.Min(min, 0f);
            m_DragMaxDelta = Mathf.Max(max, 0f);
        }

        float GetExtent(Axis axis, ItemSpan span)
        {
            var start = float.PositiveInfinity;
            var end = float.NegativeInfinity;
            for (var k = span.first; k <= span.last && k < m_DragSizes.Length; ++k)
            {
                start = Mathf.Min(start, axis.offsets[k]);
                end = Mathf.Max(end, axis.offsets[k] + m_DragSizes[k]);
            }

            return end > start ? end - start : 0f;
        }

        // Sets the two tracks of the dragged line to their new sizes and keeps the others as they were at the start of
        // the drag. Length tracks get the new length. The fr factors are set so that each flexible track gets its size
        // back from the space left to the flexible tracks.
        void ResizeTracks(float delta)
        {
            var axis = m_Axes[m_DragAxis];
            var before = m_DragBefore;
            var after = m_DragAfter;
            var beforeSize = m_DragSizes[before] + delta;
            var afterSize = m_DragSizes[after] - delta;
            var beforeIsFlexible = IsFlexible(m_DragTracks[before]);
            var afterIsFlexible = IsFlexible(m_DragTracks[after]);

            if (beforeIsFlexible && afterIsFlexible)
            {
                var factors = m_DragTracks[before].maxValue + m_DragTracks[after].maxValue;
                var total = m_DragSizes[before] + m_DragSizes[after];
                if (total > 0f)
                {
                    var beforeFactor = factors * beforeSize / total;
                    axis.desired[before] = WithFactor(axis.desired[before], beforeFactor);
                    axis.desired[after] = WithFactor(axis.desired[after], factors - beforeFactor);
                }
            }
            else if (beforeIsFlexible || afterIsFlexible)
            {
                // The flexible tracks share a different amount of space now: all their factors change.
                var factors = 0f;
                var flexibleSize = 0f;
                for (var k = 0; k < m_DragTracks.Count; ++k)
                {
                    if (axis.IsCollapsed(k) || !IsFlexible(m_DragTracks[k]))
                        continue;

                    factors += m_DragTracks[k].maxValue;
                    flexibleSize += m_DragSizes[k];
                }

                flexibleSize += beforeIsFlexible ? delta : -delta;
                if (flexibleSize > 0f)
                {
                    // Below a total of 1fr, the flexible tracks would not take all the space they are given.
                    var sizePerFactor = flexibleSize / Mathf.Max(factors, 1f);
                    for (var k = 0; k < m_DragTracks.Count; ++k)
                    {
                        if (axis.IsCollapsed(k) || !IsFlexible(m_DragTracks[k]))
                            continue;

                        var size = k == before ? beforeSize : k == after ? afterSize : m_DragSizes[k];
                        axis.desired[k] = WithFactor(axis.desired[k], size / sizePerFactor);
                    }
                }
            }

            if (!beforeIsFlexible)
                axis.desired[before] = WithLength(axis.desired[before], beforeSize, axis.contentSize);
            if (!afterIsFlexible)
                axis.desired[after] = WithLength(axis.desired[after], afterSize, axis.contentSize);

            Refresh();
        }

        public void EndDrag()
        {
            if (!isDragging)
                return;

            var pointerId = m_DragPointerId;
            m_DragAxis = -1;
            m_DragPointerId = PointerId.invalidPointerId;

            // Cleared first: releasing the capture sends PointerCaptureOutEvent, which ends the drag too.
            if (m_Owner.HasPointerCapture(pointerId))
                m_Owner.ReleasePointer(pointerId);

            SaveViewData();
            m_Owner.MarkDirtyRepaint();
        }

        public void Paint(MeshGenerationContext mgc, Color lineColor, Color activeLineColor, float lineWidth)
        {
            if (m_Dividers.Count == 0 || lineWidth <= 0f)
                return;

            var painter = mgc.painter2D;
            painter.lineWidth = lineWidth;
            painter.lineCap = LineCap.Butt;

            foreach (var divider in m_Dividers)
            {
                var isActive = isDragging
                    ? divider.Is(m_DragAxis, m_DragBefore, m_DragAfter)
                    : divider.Is(m_HoverAxis, m_HoverBefore, m_HoverAfter);
                painter.strokeColor = isActive ? activeLineColor : lineColor;
                painter.BeginPath();
                for (var i = 0; i < divider.segmentCount; ++i)
                {
                    var segment = m_Segments[divider.segmentStart + i];
                    if (divider.axis == Columns)
                    {
                        painter.MoveTo(new Vector2(divider.position, segment.x));
                        painter.LineTo(new Vector2(divider.position, segment.y));
                    }
                    else
                    {
                        painter.MoveTo(new Vector2(segment.x, divider.position));
                        painter.LineTo(new Vector2(segment.y, divider.position));
                    }
                }

                painter.Stroke();
            }
        }

        /// <summary>
        /// Reads the tracks the view data holds for the owner, and saves them there from now on.
        /// </summary>
        public void LoadViewData()
        {
            if (m_Owner.elementPanel == null || string.IsNullOrEmpty(m_Owner.viewDataKey) || !m_Owner.enableViewDataPersistence)
                return;

            m_Owner.OverwriteFromViewData(m_ViewData, m_Owner.GetFullHierarchicalViewDataKey());
            m_PersistsViewData = true;

            foreach (var axis in m_Axes)
            {
                if (axis.baseTracks == null)
                    continue;

                var restored = RestoreFromViewData(axis);
                if (restored != null)
                    axis.desired = restored;
            }

            Refresh();
        }

        // Only tracks saved for the same template are restored: a new template in the UXML or the code wins.
        List<GridTrackSize> RestoreFromViewData(Axis axis)
        {
            if (!m_PersistsViewData)
                return null;

            var (template, tracks) = axis.index == Columns
                ? (m_ViewData.columnsTemplate, m_ViewData.columns)
                : (m_ViewData.rowsTemplate, m_ViewData.rows);
            if (template != axis.templateText || tracks == null || tracks.Count != axis.baseTracks.Count)
                return null;

            return new List<GridTrackSize>(tracks);
        }

        void SaveViewData()
        {
            if (!m_PersistsViewData)
                return;

            var columnAxis = m_Axes[Columns];
            var rowAxis = m_Axes[Rows];
            m_ViewData.columnsTemplate = columnAxis.templateText;
            m_ViewData.columns = columnAxis.desired != null ? new List<GridTrackSize>(columnAxis.desired) : null;
            m_ViewData.rowsTemplate = rowAxis.templateText;
            m_ViewData.rows = rowAxis.desired != null ? new List<GridTrackSize>(rowAxis.desired) : null;
            m_Owner.SaveViewData();
        }

        internal static bool IsFlexible(GridTrackSize track)
            => track.maxUnit == GridTrackSizeUnit.Fraction && !track.isAutoFill && !track.isAutoFit;

        static float GetTrackMinimum(GridTrackSize track)
            => track.isMinmax && track.minUnit == GridTrackSizeUnit.Pixel ? track.minValue : 0f;

        static bool IsLength(GridTrackSize track)
            => !track.isMinmax && !track.isFitContent && !track.isAutoFill && !track.isAutoFit &&
                (track.maxUnit == GridTrackSizeUnit.Pixel || track.maxUnit == GridTrackSizeUnit.Percent);

        // A track that only became flexible to take the space of a collapsed one becomes a plain fr track.
        static GridTrackSize WithFactor(GridTrackSize track, float factor)
            => IsFlexible(track)
                ? new GridTrackSize(track.m_Kind, track.minValue, track.minUnit, factor, GridTrackSizeUnit.Fraction)
                : GridTrackSize.Fraction(factor);

        // A percentage stays a percentage; anything else that isn't flexible becomes a fixed length.
        static GridTrackSize WithLength(GridTrackSize track, float length, float contentSize)
        {
            length = Mathf.Max(0f, length);
            var isPercent = !track.isMinmax && !track.isFitContent && track.maxUnit == GridTrackSizeUnit.Percent;
            return isPercent && contentSize > 0f
                ? GridTrackSize.Percent(length * 100f / contentSize)
                : GridTrackSize.Pixels(length);
        }

        static void Resize<T>(ref T[] array, int count)
        {
            if (array.Length != count)
                array = count == 0 ? Array.Empty<T>() : new T[count];
        }
    }

    [Serializable]
    sealed class GridViewComponentData
    {
        public string columnsTemplate;
        public List<GridTrackSize> columns;
        public string rowsTemplate;
        public List<GridTrackSize> rows;
    }
}
