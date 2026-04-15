// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Collections;
using Unity.Mathematics;

namespace UnityEngine.U2D.UTess
{

    struct Refinery
    {

        // Old min and max are 0.5 and 0.05. We pretty much do the same with some relaxing on both ends.
        private static readonly float kMinAreaFactor = 0.0482f;
        private static readonly float kMaxAreaFactor = 0.4820f;
        // After doing more tests with a number of Sprites, this is area to which we can reduce to considering quality and CPU cost.
        private static readonly int kMaxSteinerCount = 4084;

        // Check if Triangle is Ok.
        static bool RequiresRefining(UTriangle tri, float maxArea)
        {
            // Add any further criteria later on.
            return (tri.area > maxArea);
        }

        static void FetchEncroachedSegments(NativeArray<float2> pgPoints, int pgPointCount, NativeArray<int2> pgEdges, int pgEdgeCount, ref Array<UEncroachingSegment> encroach, ref int encroachCount, UCircle c)
        {
            for (int i = 0; i < pgEdgeCount; ++i)
            {
                var edge = pgEdges[i];
                var edgeA = pgPoints[edge.x];
                var edgeB = pgPoints[edge.y];

                // Check if center is along the Edge.
                if (!math.any(c.center - edgeA) || !math.any(c.center - edgeB))
                    continue;

                // Get Radius
                var edgeD = edgeA - edgeB;
                var edgeM = (edgeA + edgeB) * 0.5f;
                var edgeR = math.length(edgeD) * 0.5f;
                if (math.length(edgeM - c.center) > edgeR)
                    continue;

                UEncroachingSegment es = new UEncroachingSegment();
                es.a = edgeA;
                es.b = edgeB;
                es.index = i;
                encroach[encroachCount++] = es;
            }
        }

        static void InsertVertex(ref NativeArray<float2> pgPoints, ref int pgPointCount, float2 newVertex, ref int nid)
        {
            nid = pgPointCount;
            pgPoints[nid] = newVertex;
            pgPointCount++;
        }

        static void SplitSegments(ref NativeArray<float2> pgPoints, ref int pgPointCount, ref NativeArray<int2> pgEdges, ref int pgEdgeCount, UEncroachingSegment es)
        {
            var sid = es.index;
            var edge = pgEdges[sid];
            var edgeA = pgPoints[edge.x];
            var edgeB = pgPoints[edge.y];
            var split = (edgeA + edgeB) * 0.5f;
            var neid = 0;
            if (math.abs(edge.x - edge.y) == 1)
            {
                neid = (edge.x > edge.y) ? edge.x : edge.y;
                InsertVertex(ref pgPoints, ref pgPointCount, split, ref neid);

                // Add the split segments.
                var rep = pgEdges[sid];
                pgEdges[sid] = new int2(rep.x, neid);
                for (int i = pgEdgeCount; i > (sid + 1); --i)
                    pgEdges[i] = pgEdges[i - 1];
                pgEdges[sid + 1] = new int2(neid, rep.y);
                pgEdgeCount++;
            }
            else
            {
                neid = pgPointCount;
                pgPoints[pgPointCount++] = split;
                pgEdges[sid] = new int2(math.max(edge.x, edge.y), neid);
                pgEdges[pgEdgeCount++] = new int2(math.min(edge.x, edge.y), neid);
            }
        }

        internal static bool Condition(Allocator allocator, float factorArea, float targetArea, ref NativeArray<float2> pgPoints, ref int pgPointCount, ref NativeArray<int2> pgEdges, ref int pgEdgeCount, ref NativeArray<float2> vertices, ref int vertexCount, ref NativeArray<int> indices, ref int indexCount, ref float maxArea)
        {

            // Process Triangles.
            maxArea = 0.0f;
            var minArea = 0.0f;
            var avgArea = 0.0f;
            var refined = false;
            var validGraph = true;

            // Temporary Stuffs.
            int triangleCount = 0, invalidTriangle = -1, inputPointCount = pgPointCount;
            var encroach = new Array<UEncroachingSegment>(inputPointCount, ModuleHandle.kMaxEdgeCount, allocator, NativeArrayOptions.UninitializedMemory);
            var triangles = new Array<UTriangle>(inputPointCount * 4, ModuleHandle.kMaxTriangleCount, allocator, NativeArrayOptions.UninitializedMemory);
            ModuleHandle.BuildTriangles(vertices, vertexCount, indices, indexCount, ref triangles, ref triangleCount, ref maxArea, ref avgArea, ref minArea);
            factorArea = factorArea != 0 ? math.clamp(factorArea, kMinAreaFactor, kMaxAreaFactor) : factorArea;
            var criArea = maxArea * factorArea;
            criArea = math.max(criArea, targetArea);

            // Refine
            while (!refined && validGraph)
            {

                // Check if any of the Triangle is Invalid or Segment is invalid. If yes, Refine.
                for (int i = 0; i < triangleCount; ++i)
                {
                    if (RequiresRefining(triangles[i], criArea))
                    {
                        invalidTriangle = i;
                        break;
                    }
                }

                // Find any Segment that can be Split based on the Input Length.
                // todo.

                if (invalidTriangle != -1)
                {

                    // Get all Segments that are encroached.
                    var t = triangles[invalidTriangle];
                    var encroachCount = 0;
                    FetchEncroachedSegments(pgPoints, pgPointCount, pgEdges, pgEdgeCount, ref encroach, ref encroachCount, t.c);

                    // Split each Encroached Segments. If no segments are encroached. Split the Triangle.
                    if (encroachCount != 0)
                    {
                        for (int i = 0; i < encroachCount; ++i)
                        {
                            SplitSegments(ref pgPoints, ref pgPointCount, ref pgEdges, ref pgEdgeCount, encroach[i]);
                        }
                    }
                    else
                    {
                        // Update Triangulation.
                        var split = t.c.center;
                        pgPoints[pgPointCount++] = split;
                    }

                    // Tessellate again.
                    indexCount = 0; vertexCount = 0;
                    validGraph = Tessellator.Tessellate(allocator, pgPoints, pgPointCount, pgEdges, pgEdgeCount, ref vertices, ref vertexCount, ref indices, ref indexCount);

                    // Build Internal Triangles.
                    encroachCount = 0; triangleCount = 0; invalidTriangle = -1;
                    if (validGraph)
                        ModuleHandle.BuildTriangles(vertices, vertexCount, indices, indexCount, ref triangles, ref triangleCount, ref maxArea, ref avgArea, ref minArea);

                    // More than enough Steiner points inserted. This handles all sort of weird input sprites very well (even random point cloud).
                    if (pgPointCount - inputPointCount > kMaxSteinerCount)
                        break;
                }
                else
                {
                    refined = true;
                }

            }

            // Dispose off
            triangles.Dispose();
            encroach.Dispose();
            return refined;

        }

    }
}
