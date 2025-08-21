// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Unity.VectorGraphics
{
    public partial class VectorUtils
    {
        internal static BezierPathSegment[] BuildEllipsePath(Vector2 p0, Vector2 p1, float rotation, float rx, float ry, bool largeArc, bool sweep)
        {
            if ((p1-p0).magnitude < VectorUtils.Epsilon)
                return new BezierPathSegment[0];

            Vector2 c;
            float theta1;
            float sweepTheta;
            float adjustedRx;
            float adjustedRy;
            ComputeEllipseParameters(p0, p1, rotation, rx, ry, largeArc, sweep, out c, out theta1, out sweepTheta, out adjustedRx, out adjustedRy);

            BezierPathSegment[] path;
            if (Mathf.Abs(sweepTheta) <= Mathf.Epsilon)
            {
                // Use a straight line if the sweep angle is tiny
                path = VectorUtils.BezierSegmentToPath(VectorUtils.MakeLine(p0, p1));
            }
            else
            {
                path = VectorUtils.MakeArc(Vector2.zero, theta1, sweepTheta, 1.0f);

                var scaling = new Vector2(adjustedRx, adjustedRy);
                path = VectorUtils.TransformBezierPath(path, c, rotation, scaling);                
            }


            return path;
        }

        private static void ComputeEllipseParameters(Vector2 p0, Vector2 p1, float phi, float rx, float ry, bool fa, bool fs, out Vector2 c, out float theta1, out float sweepTheta, out float adjustedRx, out float adjustedRy)
        {
            // See https://www.w3.org/TR/SVG/implnote.html#ArcImplementationNotes
            var cosPhi = Mathf.Cos(phi);
            var sinPhi = Mathf.Sin(phi);

            var m0 = Matrix2D.identity;
            m0.m00 = cosPhi;
            m0.m01 = -sinPhi;
            m0.m10 = sinPhi;
            m0.m11 = cosPhi;

            var m1 = m0;
            m1.m01 = -m1.m01;
            m1.m10 = -m1.m10;

            var pp = m0 * new Vector2((p0.x - p1.x) / 2.0f, (p0.y - p1.y) / 2.0f);

            rx = Mathf.Abs(rx);
            ry = Mathf.Abs(ry);
            EnsureRadiiAreLargeEnough(pp, ref rx, ref ry);
            adjustedRx = rx;
            adjustedRy = ry;

            var ppx2 = pp.x * pp.x;
            var ppy2 = pp.y * pp.y;
            var rx2 = rx * rx;
            var ry2 = ry * ry;
            var cp = new Vector2((rx * pp.y) / ry, -(ry * pp.x) / rx);
            cp *= Mathf.Sqrt(Mathf.Abs((rx2 * ry2 - rx2 * ppy2 - ry2 * ppx2) / (rx2 * ppy2 + ry2 * ppx2)));
            if (fa == fs)
                cp = -cp;

            c = (m1 * cp) + new Vector2((p0.x + p1.x) / 2.0f, (p0.y + p1.y) / 2.0f);

            theta1 = Vector2.SignedAngle(new Vector2(1, 0), new Vector2((pp.x - cp.x) / rx, (pp.y - cp.y) / ry)) % 360.0f;
            sweepTheta = Vector2.SignedAngle(new Vector2((pp.x - cp.x) / rx, (pp.y - cp.y) / ry), new Vector2((-pp.x - cp.x) / rx, (-pp.y - cp.y) / ry));

            if (!fs && sweepTheta > 0)
                sweepTheta -= 360;
            if (fs && sweepTheta < 0)
                sweepTheta += 360;

            theta1 *= Mathf.Deg2Rad;
            sweepTheta *= Mathf.Deg2Rad;
        }

        private static void EnsureRadiiAreLargeEnough(Vector2 p, ref float rx, ref float ry)
        {
            var d = (p.x * p.x) / (rx * rx) + (p.y * p.y) / (ry * ry);
            if (d > 1.0f)
            {
                var sqrtD = Mathf.Sqrt(d);
                rx *= sqrtD;
                ry *= sqrtD;
            }
        }
    }
}
