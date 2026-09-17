// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Accessibility;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Projects element rectangles from panel space to the screen space expected by
    /// <see cref="AccessibilityNode.frame"/>.
    /// </summary>
    /// <remarks>
    /// The native accessibility adapters expect frames in rendered-surface pixels with a top-left
    /// origin and y pointing down (each platform then converts to its own convention, for example
    /// macOS divides by the backing scale factor and reverses y itself). Panel space already uses a
    /// top-left origin, so the projection is only the panel scale factor — the inverse of
    /// <see cref="BaseRuntimePanel.ScreenToPanel(Vector2)"/> — with no y-flip.
    /// </remarks>
    internal static class AccessibilityFrameProjection
    {
        /// <summary>
        /// The visibility context a subtree walk carries top-down: the current scroll segment's
        /// accumulated static clips, and whether an enclosing scroll viewport is already fully
        /// out of view. Same verdicts as <see cref="IsFullyOutOfView(VisualElement)"/> — the
        /// canonical statement of the segment rules — with each ancestor examined once per walk
        /// instead of once per descendant.
        /// </summary>
        internal struct ClipState
        {
            internal bool subtreeOutOfView;

            // The current segment's accumulated static clips; unbounded at segment start.
            internal float xMin, yMin, xMax, yMax;

            internal static ClipState Unclipped(bool subtreeOutOfView) => new()
            {
                subtreeOutOfView = subtreeOutOfView,
                xMin = float.MinValue,
                yMin = float.MinValue,
                xMax = float.MaxValue,
                yMax = float.MaxValue,
            };

            // Intersects the clipping rect into the current segment. A NaN rect only ever
            // loosens the segment (Mathf.Max/Min keep the incoming operand), so indeterminate
            // bounds over-report visibility — matching IsOutside's NaN handling.
            internal void Clip(Rect clippingRect)
            {
                xMin = Mathf.Max(xMin, clippingRect.xMin);
                yMin = Mathf.Max(yMin, clippingRect.yMin);
                xMax = Mathf.Min(xMax, clippingRect.xMax);
                yMax = Mathf.Min(yMax, clippingRect.yMax);
            }
        }

        /// <summary>
        /// The state above the element — the starting state for a top-down refresh of its subtree.
        /// One ancestor walk per subtree: the same per-segment logic as
        /// <see cref="IsFullyOutOfView(VisualElement)"/>, minus the element's own bounds test.
        /// </summary>
        public static ClipState GetClipStateAbove(VisualElement element)
        {
            var state = ClipState.Unclipped(false);

            if (element.elementPanel is not { } panel)
                return state;

            for (var ancestor = element.hierarchy.parent; ancestor != null; ancestor = ancestor.hierarchy.parent)
            {
                if (!ancestor.ShouldClip())
                    continue;

                // The class pre-filter spares ordinary clipping containers an ancestor walk on
                // this per-frame path; the owning-scroll-view check confirms identity, so a
                // ScrollView structure change cannot silently break the exemption.
                if (ancestor.ClassListContains(ScrollView.viewportUssClassNameUnique) &&
                    ancestor.GetFirstAncestorOfType<ScrollView>() is { } scrollView &&
                    scrollView.contentViewport == ancestor)
                {
                    // The element's own segment ends at this viewport (see
                    // IsFullyOutOfView(VisualElement) for the segment rules).
                    state.subtreeOutOfView = IsFullyOutOfView(ancestor);
                    return state;
                }

                state.Clip(ancestor.worldBound);
            }

            // The outermost segment is additionally clipped by the panel's bounds (everything a
            // panel renders is within them).
            state.Clip(panel.visualTree.layout);
            return state;
        }

        /// <summary>
        /// Clips the state by the element's own clipping before its children inherit it.
        /// Represented scroll views never come through here — their content takes
        /// <see cref="DescendToHostedContent"/>.
        /// </summary>
        public static ClipState Descend(ClipState state, VisualElement element)
        {
            if (element.ShouldClip())
                state.Clip(element.worldBound);

            return state;
        }

        /// <summary>
        /// The state for a content host's represented content: clips by the chrome between host
        /// and viewport, closes that segment at the viewport, and opens the content's own,
        /// unbounded one. The pair comes from
        /// <see cref="AccessibilityTreeGenerator.TryGetContentHost(VisualElement, out VisualElement, out VisualElement)"/>.
        /// </summary>
        public static ClipState DescendToHostedContent(ClipState state, VisualElement host, VisualElement viewport)
        {
            var segment = Descend(state, host);
            for (var element = viewport.hierarchy.parent; element != null && element != host;
                 element = element.hierarchy.parent)
            {
                segment = Descend(segment, element);
            }

            return ClipState.Unclipped(segment.subtreeOutOfView ||
                IsOutside(viewport.worldBound, segment.xMin, segment.yMin, segment.xMax, segment.yMax));
        }

        // Out of view when the visible region is empty or the bounds only touch its edge. NaN
        // anywhere fails the comparisons and counts as in view.
        static bool IsOutside(Rect bounds, float xMin, float yMin, float xMax, float yMax)
        {
            return xMax <= xMin || yMax <= yMin ||
                bounds.xMax <= xMin || bounds.xMin >= xMax ||
                bounds.yMax <= yMin || bounds.yMin >= yMax;
        }

        /// <summary>
        /// Whether no part of the element can be in view: fully outside its panel or a static
        /// clipping ancestor. Such nodes are hidden through
        /// <see cref="AccessibilityNode.isActive"/> — still generated, so identity and screen
        /// reader focus survive. A scroll viewport's clip deliberately does not hide: scrolling
        /// can reveal that content and macOS VoiceOver has no scroll action, so exposure is the
        /// only path to it. Content of a scroll view that is itself out of view stays hidden —
        /// crossing a viewport continues the test with the viewport's own bounds. Partial
        /// visibility and anything indeterminate (NaN or rotated bounds) count as in view.
        /// </summary>
        public static bool IsFullyOutOfView(VisualElement element)
        {
            // The state-threaded walk is the one implementation of the segment-clip rules; this
            // per-element form just derives it from the element's own ancestor chain.
            return IsFullyOutOfView(GetClipStateAbove(element), element.worldBound);
        }

        /// <summary>
        /// The walk-state form of <see cref="IsFullyOutOfView(VisualElement)"/>: the same
        /// verdict, with the ancestors read off the inherited state and the bounds shared with
        /// the caller. NaN bounds count as in view whatever the state says.
        /// </summary>
        public static bool IsFullyOutOfView(ClipState state, Rect bounds)
        {
            if (float.IsNaN(bounds.x) || float.IsNaN(bounds.y) ||
                float.IsNaN(bounds.width) || float.IsNaN(bounds.height))
                return false;

            return state.subtreeOutOfView ||
                IsOutside(bounds, state.xMin, state.yMin, state.xMax, state.yMax);
        }

        /// <summary>
        /// Returns the element's screen-space frame, or <see cref="Rect.zero"/> when the element is
        /// not part of a panel this projection supports (only screen-space overlay panels; a
        /// world-space panel has no meaningful screen rectangle here).
        /// </summary>
        /// <remarks>
        /// The guards below are not the participation gate (that is the bridge's registration
        /// filter). This runs inside long-lived <see cref="AccessibilityNode.frameGetter"/>
        /// closures that can outlive registration-time facts, so a degenerate state degrades to
        /// <see cref="Rect.zero"/>, which screen readers skip. The non-runtime-panel guard also
        /// keeps the generator's unit tests runnable on editor panels, where frames are
        /// intentionally zero.
        /// </remarks>
        public static Rect GetScreenFrame(VisualElement element)
        {
            return GetScreenFrame(element, element.worldBound);
        }

        /// <summary>
        /// The precomputed-bounds form of <see cref="GetScreenFrame(VisualElement)"/> for walks
        /// that already read the element's worldBound for the visibility test.
        /// </summary>
        public static Rect GetScreenFrame(VisualElement element, Rect worldBound)
        {
            if (element.elementPanel is not BaseRuntimePanel panel)
                return Rect.zero;

            if (!panel.isFlat)
                return Rect.zero;

            // Layout may not have run yet (for example when the hierarchy is built on screen reader
            // activation before the panel's first update); report an empty frame instead of NaNs.
            if (float.IsNaN(worldBound.x) || float.IsNaN(worldBound.y) ||
                float.IsNaN(worldBound.width) || float.IsNaN(worldBound.height))
                return Rect.zero;

            var scale = panel.scale;
            return new Rect(worldBound.position * scale, worldBound.size * scale);
        }
    }
}
