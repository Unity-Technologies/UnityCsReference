// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Utility class for wires.
    /// </summary>
    [UnityRestricted]
    internal static class WireUtilities
    {
        /// <summary>
        /// The default width of a wire.
        /// </summary>
        public const float DefaultWireWidth = 2f;

        /// <summary>
        /// The minimum distance the mouse must move to start a wire.
        /// </summary>
        public const float WireCreationDistanceThreshold = 10f;

        /// <summary>
        /// The default color of a wire.
        /// </summary>
        public static Color DefaultWireColor
        {
            get
            {
                if (EditorGUIUtility.isProSkin)
                {
                    return new Color(193 / 255f, 193 / 255f, 193 / 255f);
                }

                return new Color(90 / 255f, 90 / 255f, 90 / 255f);
            }
        }

        /// <summary>
        /// Tests whether a point lies on one of the line segments.
        /// </summary>
        /// <param name="point">The point to test.</param>
        /// <param name="lineSegments">The line segments to test against.</param>
        /// <param name="lineWidth">The width of the line segments.</param>
        /// <returns>True if the point lies on one of the line segments, false otherwise.</returns>
        public static bool IsPointOnLine(Vector2 point, Vector2[] lineSegments, float lineWidth)
        {
            for (var index = 0; index < lineSegments.Length - 1; index++)
            {
                var a = lineSegments[index];
                var b = lineSegments[index + 1];
                var squareDistance = SquaredDistanceToSegment(point, a, b);
                if (squareDistance < lineWidth * lineWidth)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Tests whether a rectangle intersects with a line.
        /// </summary>
        /// <param name="r">The rectangle to test.</param>
        /// <param name="lineSegments">The line segments to test against.</param>
        /// <returns>True if the rectangle intersects with the line, false otherwise.</returns>
        public static bool RectIntersectsLine(Rect r, Vector2[] lineSegments)
        {
            for (var a = 0; a < lineSegments.Length - 1; a++)
            {
                if (RectUtils.IntersectsSegment(r, lineSegments[a], lineSegments[a + 1]))
                    return true;
            }

            return false;
        }

        static float SquaredDistanceToSegment(Vector2 p, Vector2 s0, Vector2 s1)
        {
            var x = p.x;
            var y = p.y;
            var x1 = s0.x;
            var y1 = s0.y;
            var x2 = s1.x;
            var y2 = s1.y;

            var a = x - x1;
            var b = y - y1;
            var c = x2 - x1;
            var d = y2 - y1;

            var dot = a * c + b * d;
            var lenSq = c * c + d * d;
            float param = -1;
            if (lenSq > float.Epsilon) //in case of 0 length line
                param = dot / lenSq;

            float xx, yy;

            if (param < 0)
            {
                xx = x1;
                yy = y1;
            }
            else if (param > 1)
            {
                xx = x2;
                yy = y2;
            }
            else
            {
                xx = x1 + param * c;
                yy = y1 + param * d;
            }

            var dx = x - xx;
            var dy = y - yy;
            return dx * dx + dy * dy;
        }

        /// <summary>
        /// Given a side, offset, and zoom factor, returns the position of a transition anchor point in world coordinates.
        /// </summary>
        /// <param name="state">The state on which the transition anchor is located.</param>
        /// <param name="side">The transition anchor side, on the state.</param>
        /// <param name="offset">The transition anchor offset from the top or left side of the state.</param>
        /// <param name="zoomFactor">The GraphView zoom factor.</param>
        /// <returns>The position of the transition anchor in world coordinates.</returns>
        public static Vector2 GetPositionFromAnchorAndOffset(this VisualElement state, AnchorSide side, float offset, float zoomFactor)
        {
            const float transitionEndPointOffset = 5f;

            var worldBoundCache = state.worldBound;
            var localBoundCache = state.localBound;

            switch (side)
            {
                case AnchorSide.Left:
                {
                    var yPos = Math.Clamp(worldBoundCache.yMin + offset / localBoundCache.height * worldBoundCache.height, worldBoundCache.yMin, worldBoundCache.yMax);
                    return new Vector2(worldBoundCache.xMin - transitionEndPointOffset * zoomFactor, yPos);
                }
                case AnchorSide.Right:
                {
                    var yPos = Math.Clamp(worldBoundCache.yMin + offset / localBoundCache.height * worldBoundCache.height, worldBoundCache.yMin, worldBoundCache.yMax);
                    return new Vector2(worldBoundCache.xMax + transitionEndPointOffset * zoomFactor, yPos);
                }
                case AnchorSide.Top:
                {
                    var xPos = Math.Clamp(worldBoundCache.xMin + offset / localBoundCache.width * worldBoundCache.width, worldBoundCache.xMin, worldBoundCache.xMax);
                    return new Vector2(xPos, worldBoundCache.yMin - transitionEndPointOffset * zoomFactor);
                }
                case AnchorSide.Bottom:
                {
                    var xPos = Math.Clamp(worldBoundCache.xMin + offset / localBoundCache.width * worldBoundCache.width, worldBoundCache.xMin, worldBoundCache.xMax);
                    return new Vector2(xPos, worldBoundCache.yMax + transitionEndPointOffset * zoomFactor);
                }
                case AnchorSide.None:
                default:
                    return worldBoundCache.center;
            }
        }
    }
}
