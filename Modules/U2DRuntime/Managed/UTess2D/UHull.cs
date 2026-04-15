// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using System.Collections.Generic;
using System.Reflection;
using Unity.Burst;
using Unity.Mathematics;

namespace UnityEngine.U2D.UTess
{

    ////////////////////////////////////////////////////////////////
    /// Convex Hull Generation
    ////////////////////////////////////////////////////////////////
    internal struct ConvexHull2D
    {

        private static readonly float kEpsilon = 0.00001f;

        struct F3Compare : IComparer<float3>
        {
            public int Compare(float3 x, float3 y)
            {
                if (x.x != y.x)
                    return (x.x < y.x) ? -1 : 1;
                return 0;
            }
        }

        // Distance of Point from Line
        static float DistancePointToLine(float2 pq, float2 p0, float2 p1)
        {
            float2 v = p1 - p0;
            float2 w = pq - p0;

            float a = math.dot(w, v);
            if (a <= 0)
                return math.length(p0 - pq);

            float b = math.dot(v, v);
            if (b <= a)
                return math.length(p1 - pq);

            float c = a / b;
            float2 p = p0 + v * c;
            return math.length(p - pq);
        }

        static float Sign(float2 p1, float2 p2, float2 p3)
        {
            return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
        }

        static bool PointInTriangle(float2 pt, float2 v1, float2 v2, float2 v3)
        {
            float d1, d2, d3;
            bool hasNeg, hasPos;

            d1 = Sign(pt, v1, v2);
            d2 = Sign(pt, v2, v3);
            d3 = Sign(pt, v3, v1);

            hasNeg = (d1 < 0) || (d2 < 0) || (d3 < 0);
            hasPos = (d1 > 0) || (d2 > 0) || (d3 > 0);

            return !(hasNeg && hasPos);
        }

        // Fetch Points Outside Triangle
        static void FetchPointsOutsideTriangle(ref NativeArray<float2> input, int inputCount, ref NativeArray<float2> output, ref int outputCount, float2 lp, float2 p, float2 rp)
        {
            for (int i = 0; i < inputCount; ++i)
            {
                bool pointInTri = PointInTriangle(input[i], lp, p, rp);
                if (pointInTri)
                    continue;
                output[outputCount++] = input[i];
            }
        }

        // Get Points to Right of Line.
        static void FetchPointsOnRight(ref NativeArray<float2> input, int inputCount, ref NativeArray<float2> output, ref int outputCount, float2 l, float2 r)
        {
            float2 v1 = r - l;
            for (int index = 0; index < inputCount; ++index)
            {
                float2 v2 = r - input[index];
                float xp = v1.x * v2.y - v1.y * v2.x;
                if (xp > 0)
                {
                    output[outputCount++] = input[index];
                }
            }
        }

        // Get Points to the right & left of line
        static unsafe void FetchPoints(float2* input, int inputCount, ref NativeArray<float2> lp, ref int lpCount, ref NativeArray<float2> rp, ref int rpCount, float2 l, float2 r)
        {
            float2 v1 = r - l;
            for (int index = 0; index < inputCount; ++index)
            {
                float2 vx = input[index];
                float2 v2 = r - vx;
                float xp = v1.x * v2.y - v1.y * v2.x;
                if (xp > 0)
                {
                    lp[lpCount++] = vx;
                }
                if (xp < 0)
                {
                    rp[rpCount++] = vx;
                }
            }
        }

        // Generate Hull
        static void Generate(ref NativeArray<float2> output, ref int outputCount, ref NativeArray<float2> input, int inputCount, float2 l, float2 r)
        {

            float2 lp = new float2(l.x, l.y);
            float2 rp = new float2(r.x, r.y);
            float2 pt = new float2(0, 0);

            // Calculate Pivot.
            float c = 0.00001f;
            float d = c, f = c;
            for (int i = 0; i < inputCount; ++i)
            {
                d = DistancePointToLine(input[i], lp, rp);
                if (d > f)
                {
                    pt = input[i];
                    f = d;
                }
            }

            // If we found a Pivot, fetch points outside the Triangle formed by the PivotPoint and Recurse.
            if (f != c)
            {
                // Add to List of Outputs.
                output[outputCount++] = pt;

                // Get points outside of Triangle.
                int pointCount = 0;
                var pointsOutsideTriangle = new NativeArray<float2>(inputCount, Allocator.Temp);
                FetchPointsOutsideTriangle(ref input, inputCount, ref pointsOutsideTriangle, ref pointCount, l, pt, r);

                // Get Points Right of l to pt.
                int lpCount = 0;
                var lpPoints = new NativeArray<float2>(pointCount, Allocator.Temp);
                FetchPointsOnRight(ref pointsOutsideTriangle, pointCount, ref lpPoints, ref lpCount, l, pt);
                if (lpCount != 0)
                    Generate(ref output, ref outputCount, ref lpPoints, lpCount, l, pt);

                int rpCount = 0;
                var rpPoints = new NativeArray<float2>(pointCount, Allocator.Temp);
                FetchPointsOnRight(ref pointsOutsideTriangle, pointCount, ref rpPoints, ref rpCount, pt, r);
                if (rpCount != 0)
                    Generate(ref output, ref outputCount, ref rpPoints, rpCount, pt, r);

                // Dispose
                rpPoints.Dispose();
                lpPoints.Dispose();
                pointsOutsideTriangle.Dispose();
            }

        }

        static unsafe int CheckSide(float2* convex, int start, int end, float2 p, float2 d)
        {
            int pos = 0, neg = 0;

            for (int i = start; i < end; i++)
            {
                var nm = convex[i] - p;
                var dt = math.dot(d, nm);
                pos = (dt > 0) ? (pos + 1) : (pos);
                neg = (dt < 0) ? (neg + 1) : (neg);
                if (0 != pos && 0 != neg)
                {
                    return 0;
                }
            }

            // Both Sides.
            return (pos > 0) ? 1 : -1;
        }

        // Seperating Axis.
        public static bool CheckCollisionSeparatingAxis(ref NativeArray<float2> convex1_, int start1, int end1, ref NativeArray<float2> convex2_, int start2, int end2)
        {
            unsafe
            {
                var convex1 = (float2*)convex1_.GetUnsafeReadOnlyPtr();
                var convex2 = (float2*)convex2_.GetUnsafeReadOnlyPtr();

                for (int i = start1, j = end1 - 1; i < end1; j = i++)
                {
                    var p = convex1[i];
                    var d = convex1[i] - convex1[j];
                    d = new float2(d.y, -d.x);
                    if (CheckSide(convex2, start2, end2, p, d) > 0)
                        return false;
                }

                for (int i = start2, j = end2 - 1; i < end2; j = i++)
                {
                    var p = convex2[i];
                    var d = convex2[i] - convex2[j];
                    d = new float2(d.y, -d.x);
                    if (CheckSide(convex1, start1, end1, p, d) > 0)
                        return false;
                }
            }
            return true;
        }

        internal static bool LineLineIntersection(float2 p1, float2 p2, float2 p3, float2 p4, ref float2 result)
        {
            float bx = p2.x - p1.x;
            float by = p2.y - p1.y;
            float dx = p4.x - p3.x;
            float dy = p4.y - p3.y;
            float bDotDPerp = bx * dy - by * dx;
            if (math.abs(bDotDPerp) < kEpsilon)
            {
                return false;
            }

            float cx = p3.x - p1.x;
            float cy = p3.y - p1.y;
            float t = (cx * dy - cy * dx) / bDotDPerp;
            if ((t >= -kEpsilon) && (t <= 1.0f + kEpsilon))
            {
                result.x = p1.x + t * bx;
                result.y = p1.y + t * by;
                return true;
            }
            return false;
        }

        // Convex HUll Generator.
        public static unsafe float3 Generate(ref NativeArray<float2> result, ref float4 aabb, ref int pointCount, int seed, Vector2* vertexInput, int vertexCount, float extrude)
        {

            float2* vertices = (float2*)vertexInput;
            float2* convex = (float2*)result.GetUnsafePtr();
            float2 leftMost, rightMost, topMost, bottomMost, center = float2.zero;
            float lx = Single.MaxValue, ly = Single.MaxValue;
            float3 area = float3.zero;
            leftMost.x = bottomMost.y = Single.MaxValue;
            rightMost.x = topMost.y = Single.MinValue;
            leftMost.y = rightMost.y = topMost.x = bottomMost.x = 0;

            // Temporary Array for Calc.
            int outputCount = 0;
            var output = new NativeArray<float2>(vertexCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

            // Find the Left and Right EXtremes.
            for (int i = 0; i < vertexCount; ++i)
            {
                leftMost = (leftMost.x > vertices[i].x) ? vertices[i] : leftMost;
                rightMost = (rightMost.x < vertices[i].x) ? vertices[i] : rightMost;
                bottomMost = (bottomMost.y > vertices[i].y) ? vertices[i] : bottomMost;
                topMost = (topMost.y > vertices[i].y) ? vertices[i] : topMost;
            }

            // Add the Extreme to Output.
            output[outputCount++] = leftMost;
            output[outputCount++] = rightMost;

            // Get Points for the Edges
            int lpCount = 0, rpCount = 0;
            var lp = new NativeArray<float2>(vertexCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            var rp = new NativeArray<float2>(vertexCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            FetchPoints(vertices, vertexCount, ref lp, ref lpCount, ref rp, ref rpCount, leftMost, rightMost);

            // Generate Convex Hull
            if (lpCount != 0)
                Generate(ref output, ref outputCount, ref lp, lpCount, leftMost, rightMost);
            if (rpCount != 0)
                Generate(ref output, ref outputCount, ref rp, rpCount, rightMost, leftMost);

            if (outputCount >= 3)
            {
                // Output. First two points are the pivot points.
                var sortedData = new NativeArray<float3>(outputCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                var sorted = (float3*)sortedData.GetUnsafePtr();

                var v1 = rightMost - leftMost;
                for (int i = 0; i < outputCount; ++i)
                {
                    float3 val = output[i].xyx;
                    val.z = 0;
                    if (i > 1)
                    {
                        float2 v2 = rightMost - val.xy;
                        val.z = v1.x * v2.y - v1.y * v2.x;
                    }
                    sorted[i] = val;
                }

                // Sort by X.
                ModuleHandle.InsertionSort<float3, F3Compare>(sorted, 0, outputCount - 1, new F3Compare());

                // Copy to Convex.
                convex[pointCount] = leftMost;
                center += convex[pointCount++];
                for (int i = 0; i < outputCount; ++i)
                {
                    if (sorted[i].z > 0)
                    {
                        convex[pointCount] = sorted[i].xy;
                        center += convex[pointCount++];
                    }
                }
                convex[pointCount] = rightMost;
                center += convex[pointCount++];
                for (int i = outputCount - 1; i > 0; --i)
                {
                    if (sorted[i].z < 0)
                    {
                        convex[pointCount] = sorted[i].xy;
                        center += convex[pointCount++];
                    }
                }
                center = center / pointCount;
                convex[pointCount++] = leftMost;

                // Move it to Origin and Extrude.
                leftMost.x = bottomMost.y = Single.MaxValue;
                rightMost.x = topMost.y = Single.MinValue;
                leftMost.y = rightMost.y = topMost.x = bottomMost.x = 0;

                for (int i = 0; i < pointCount; ++i)
                {
                    var v = convex[i];
                    var d = math.normalizesafe(v - center);
                    convex[i] = center + (d * (math.length(v - center) + extrude));

                    leftMost = (leftMost.x > convex[i].x) ? convex[i] : leftMost;
                    rightMost = (rightMost.x < convex[i].x) ? convex[i] : rightMost;
                    bottomMost = (bottomMost.y > convex[i].y) ? convex[i] : bottomMost;
                    topMost = (topMost.y < convex[i].y) ? convex[i] : topMost;
                }

                lx = leftMost.x;
                ly = bottomMost.y;
                area.x = rightMost.x - leftMost.x;
                area.y = topMost.y - bottomMost.y;
                center = new float2(center.x - lx, center.y - ly);

                float minx = 9999999.0f, miny = 9999999.0f;
                float maxx = -9999999.0f, maxy = -9999999.0f;

                for (int i = 0; i < pointCount; ++i)
                {
                    var cx = new float2((int)math.floor(convex[i].x - lx), (int)math.floor(convex[i].y - ly));
                    convex[i] = cx;
                    minx = (cx.x < minx) ? cx.x : minx;
                    maxx = (cx.x > maxx) ? cx.x : maxx;
                    miny = (cx.y < miny) ? cx.y : miny;
                    maxy = (cx.y > maxy) ? cx.y : maxy;

                    if (i != 0)
                        area.z += UnityEngine.U2D.UTess.ModuleHandle.TriangleArea(convex[i], center, convex[i - 1]);
                }

                // Set the Center and Width/Height
                aabb.z = (maxx - minx / 2.0f);
                aabb.w = (maxy - miny / 2.0f);
                aabb.x = minx + aabb.z;
                aabb.y = miny + aabb.w;


                // Dispose.
                sortedData.Dispose();
            }
            else
            {
                Debug.Log("[failed to generate convex hull2d]");
            }

            rp.Dispose();
            lp.Dispose();
            output.Dispose();
            return area;
        }

    }

}
