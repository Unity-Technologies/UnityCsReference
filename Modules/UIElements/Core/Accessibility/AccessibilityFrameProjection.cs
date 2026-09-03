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
        /// Returns the element's screen-space frame, or <see cref="Rect.zero"/> when the element is
        /// not part of a panel this projection supports (only screen-space overlay panels; a
        /// world-space panel has no meaningful screen rectangle here).
        /// </summary>
        /// <remarks>
        /// The bridge's registration filter is the real gate on which panels participate; the
        /// guards below are not that gate. This method runs inside long-lived
        /// <see cref="AccessibilityNode.frameGetter"/> closures that can outlive registration-time
        /// facts — the element can be reparented or detached, and the panel's render mode can
        /// change after attach — so a degenerate state must degrade to <see cref="Rect.zero"/>
        /// (a zero-size frame makes screen readers skip the node). The non-runtime-panel guard also
        /// keeps the generator pure: its unit tests exercise it on editor panels, where frames are
        /// intentionally zero.
        /// </remarks>
        public static Rect GetScreenFrame(VisualElement element)
        {
            if (element.elementPanel is not BaseRuntimePanel panel)
                return Rect.zero;

            if (!panel.isFlat)
                return Rect.zero;

            var worldBound = element.worldBound;

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
