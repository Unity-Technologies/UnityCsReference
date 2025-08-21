// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.VectorGraphics
{
    public partial class VectorUtils
    {
        /// <summary>Builds a BezierContour from a Rectangle.</summary>
        /// <param name="rect">The rectangle to build the contour from</param>
        /// <param name="radiusTL">The top-left radius of the rectangle</param>
        /// <param name="radiusTR">The top-right radius of the rectangle</param>
        /// <param name="radiusBR">The bottom-right radius of the rectangle</param>
        /// <param name="radiusBL">The bottom-left radius of the rectangle</param>
        /// <returns>A BezierContour that follows the rectangle contour</returns>
        public static BezierContour BuildRectangleContour(Rect rect, Vector2 radiusTL, Vector2 radiusTR, Vector2 radiusBR, Vector2 radiusBL)
        {
            var width = rect.size.x;
            var height = rect.size.y;

            var halfSize = new Vector2(width / 2.0f, height / 2.0f);
            radiusTL = Vector2.Max(Vector2.Min(radiusTL, halfSize), Vector2.zero);
            radiusTR = Vector2.Max(Vector2.Min(radiusTR, halfSize), Vector2.zero);
            radiusBR = Vector2.Max(Vector2.Min(radiusBR, halfSize), Vector2.zero);
            radiusBL = Vector2.Max(Vector2.Min(radiusBL, halfSize), Vector2.zero);

            var leftSegmentSize = height - (radiusBL.y + radiusTL.y);
            var topSegmentSize = width - (radiusTL.x + radiusTR.x);
            var rightSegmentSize = height - (radiusBR.y + radiusTR.y);
            var bottomSegmentSize = width - (radiusBL.x + radiusBR.x);

            var segments = new List<BezierPathSegment>(8);
            BezierPathSegment seg;

            if (leftSegmentSize > VectorUtils.Epsilon)
            {
                seg = MakePathLine(new Vector2(0.0f, radiusTL.y + leftSegmentSize), new Vector2(0.0f, radiusTL.y))[0];
                segments.Add(seg);
            }

            if (radiusTL.magnitude > VectorUtils.Epsilon)
            {
                var circleArc = VectorUtils.MakeArc(Vector2.zero, -Mathf.PI, Mathf.PI / 2.0f, 1.0f);
                circleArc = VectorUtils.TransformBezierPath(circleArc, radiusTL, 0.0f, radiusTL);
                segments.Add(circleArc[0]);
            }

            if (topSegmentSize > VectorUtils.Epsilon)
            {
                seg = MakePathLine(new Vector2(radiusTL.x, 0.0f), new Vector2(radiusTL.x + topSegmentSize, 0.0f))[0];
                segments.Add(seg);
            }

            if (radiusTR.magnitude > VectorUtils.Epsilon)
            {
                var topRight = new Vector2(width - radiusTR.x, radiusTR.y);
                var circleArc = VectorUtils.MakeArc(Vector2.zero, -Mathf.PI / 2.0f, Mathf.PI / 2.0f, 1.0f);
                circleArc = VectorUtils.TransformBezierPath(circleArc, topRight, 0.0f, radiusTR);
                segments.Add(circleArc[0]);
            }

            if (rightSegmentSize > VectorUtils.Epsilon)
            {
                seg = MakePathLine(new Vector2(width, radiusTR.y), new Vector2(width, radiusTR.y + rightSegmentSize))[0];
                segments.Add(seg);
            }

            if (radiusBR.magnitude > VectorUtils.Epsilon)
            {
                var bottomRight = new Vector2(width - radiusBR.x, height - radiusBR.y);
                var circleArc = VectorUtils.MakeArc(Vector2.zero, 0.0f, Mathf.PI / 2.0f, 1.0f);
                circleArc = VectorUtils.TransformBezierPath(circleArc, bottomRight, 0.0f, radiusBR);
                segments.Add(circleArc[0]);
            }

            if (bottomSegmentSize > VectorUtils.Epsilon)
            {
                seg = MakePathLine(new Vector2(width - radiusBR.x, height), new Vector2(width - (radiusBR.x + bottomSegmentSize), height))[0];
                segments.Add(seg);
            }

            if (radiusBL.magnitude > VectorUtils.Epsilon)
            {
                var bottomLeft = new Vector2(radiusBL.x, height - radiusBL.y);
                var circleArc = VectorUtils.MakeArc(Vector2.zero, Mathf.PI / 2.0f, Mathf.PI / 2.0f, 1.0f);
                circleArc = VectorUtils.TransformBezierPath(circleArc, bottomLeft, 0.0f, radiusBL);
                segments.Add(circleArc[0]);
            }

            // Offset segments to position
            for (int i = 0; i < segments.Count; ++i)
            {
                var s = segments[i];
                s.P0 += rect.position;
                s.P1 += rect.position;
                s.P2 += rect.position;
                segments[i] = s;
            }

            return new BezierContour() { Segments = segments.ToArray(), Closed = true };
        }
    }
}
