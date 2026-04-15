// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Mathematics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.U2D.UTess
{

    struct Smoothen
    {

        // This is an arbitrary value less than 2 to ensure points are not moved too far away from the source polygon.
        private static readonly float kMaxAreaTolerance = 1.842f;
        private static readonly float kMaxEdgeTolerance = 2.482f;

        // Trim Edges
        static void RefineEdges(ref NativeArray<int4> refinedEdges, ref NativeArray<int4> delaEdges, ref int delaEdgeCount, ref NativeArray<int4> voronoiEdges)
        {
            int origEdgeCount = delaEdgeCount;
            int compareEdgeCount = origEdgeCount - 1;
            delaEdgeCount = 0;

            // Check Neighbour Triangles.
            for (int i = 0; i < origEdgeCount; ++i)
            {
                var edge = delaEdges[i];
                if (i < compareEdgeCount)
                {
                    var neighbor = delaEdges[i + 1];
                    if (edge.x == neighbor.x && edge.y == neighbor.y)
                    {
                        // Found the Opposite Edge. i.e Nearby Triangle.
                        edge.w = neighbor.z;
                        ++i;
                    }
                }
                // Update new list.
                refinedEdges[delaEdgeCount++] = edge;
            }

            // Generate Voronoi Edges.
            for (int i = 0; i < delaEdgeCount; ++i)
            {
                var ti1 = refinedEdges[i].z;
                var ti2 = refinedEdges[i].w;

                // We only really care about Bounded Edges. This is simplification. Hoping this garbage works.
                if (ti1 != -1 && ti2 != -1)
                {
                    // Get Triangles
                    int4 e = new int4(ti2, ti1, i, 0);
                    voronoiEdges[i] = e;
                }
            }

            ModuleHandle.Copy(refinedEdges, delaEdges, delaEdgeCount);
        }

        // Get all the Edges that has this Point.
        static void GetAffectingEdges(int pointIndex, NativeArray<int4> edges, int edgeCount, ref NativeArray<int> resultSet, ref NativeArray<int> checkSet, ref int resultCount)
        {
            resultCount = 0;
            for (int i = 0; i < edgeCount; ++i)
            {
                if (pointIndex == edges[i].x || pointIndex == edges[i].y)
                    resultSet[resultCount++] = i;
                checkSet[i] = 0;
            }
        }

        // Add to Centroids Triangles.
        static void CentroidByPoints(int triIndex, NativeArray<UTriangle> triangles, ref NativeArray<int> centroidTris, ref int centroidCount, ref float2 aggregate, ref float2 point)
        {
            for (int i = 0; i < centroidCount; ++i)
                if (triIndex == centroidTris[i])
                    return;
            centroidTris[centroidCount++] = triIndex;
            aggregate += triangles[triIndex].c.center;
            point = aggregate / centroidCount;
        }

        static void CentroidByPolygon(int4 e, NativeArray<UTriangle> triangles, ref float2 centroid, ref float area, ref float distance)
        {
            var es = triangles[e.x].c.center;
            var ee = triangles[e.y].c.center;
            var d = es.x * ee.y - ee.x * es.y;
            distance = distance + math.distance(es, ee);
            area = area + d;
            centroid.x += (ee.x + es.x) * d;
            centroid.y += (ee.y + es.y) * d;
        }

        // Connect Triangles
        static bool ConnectTriangles(ref NativeArray<int4> connectedTri, ref NativeArray<int> affectEdges, ref NativeArray<int> checkSet, NativeArray<int4> voronoiEdges, int triangleCount)
        {
            var ei = affectEdges[0];
            var ni = affectEdges[0];
            connectedTri[0] = new int4(voronoiEdges[ei].x, voronoiEdges[ei].y, 0, 0);
            checkSet[ni] = 1;

            for (int i = 1; i < triangleCount; ++i)
            {
                ni = affectEdges[i];
                if (checkSet[ni] == 0)
                {
                    if (voronoiEdges[ni].x == connectedTri[i - 1].y)
                    {
                        connectedTri[i] = new int4(voronoiEdges[ni].x, voronoiEdges[ni].y, 0, 0);
                        checkSet[ni] = 1;
                        continue;
                    }
                    else
                    {
                        if (voronoiEdges[ni].y == connectedTri[i - 1].y)
                        {
                            connectedTri[i] = new int4(voronoiEdges[ni].y, voronoiEdges[ni].x, 0, 0);
                            checkSet[ni] = 1;
                            continue;
                        }
                    }
                }

                var connected = false;
                for (int j = 0; j < triangleCount; ++j)
                {
                    ni = affectEdges[j];
                    if (checkSet[ni] == 1)
                        continue;
                    if (voronoiEdges[ni].x == connectedTri[i - 1].y)
                    {
                        connectedTri[i] = new int4(voronoiEdges[ni].x, voronoiEdges[ni].y, 0, 0);
                        checkSet[ni] = 1;
                        connected = true;
                        break;
                    }
                    else if (voronoiEdges[ni].y == connectedTri[i - 1].y)
                    {
                        connectedTri[i] = new int4(voronoiEdges[ni].y, voronoiEdges[ni].x, 0, 0);
                        checkSet[ni] = 1;
                        connected = true;
                        break;
                    }
                }
                if (!connected)
                    return false;
            }

            return true;
        }


        // Perform Voronoi based Smoothing. Does not add/remove points but merely relocates internal vertices so they are uniform distributed.
        internal static bool Condition(Allocator allocator, ref NativeArray<float2> pgPoints, int pgPointCount, NativeArray<int2> pgEdges, int pgEdgeCount, ref NativeArray<float2> vertices, ref int vertexCount, ref NativeArray<int> indices, ref int indexCount)
        {

            // Build Triangles and Edges.
            float maxArea = 0, cmxArea = 0, minArea = 0, cmnArea = 0, avgArea = 0, minEdge = 0, maxEdge = 0, avgEdge = 0;
            bool polygonCentroid = true, validGraph = true;
            int triangleCount = 0, delaEdgeCount = 0, affectingEdgeCount = 0;
            var triangles = new NativeArray<UTriangle>(indexCount, allocator);    // Intentionally added more room than actual Triangles needed here.
            var delaEdges = new NativeArray<int4>(indexCount, allocator);
            var voronoiEdges = new NativeArray<int4>(indexCount, allocator);
            var connectedTri = new NativeArray<int4>(vertexCount, allocator);
            var voronoiCheck = new NativeArray<int>(indexCount, allocator);
            var affectsEdges = new NativeArray<int>(indexCount, allocator);
            var triCentroids = new NativeArray<int>(vertexCount, allocator);
            ModuleHandle.BuildTrianglesAndEdges(vertices, vertexCount, indices, indexCount, ref triangles, ref triangleCount, ref delaEdges, ref delaEdgeCount, ref maxArea, ref avgArea, ref minArea);
            var refinedEdges = new NativeArray<int4>(delaEdgeCount, allocator);

            // Sort the Delaunay Edges.
            unsafe
            {
                ModuleHandle.InsertionSort<int4, DelaEdgeCompare>(
                    NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(delaEdges), 0, delaEdgeCount - 1,
                    new DelaEdgeCompare());
            }

            // TrimEdges. Update Triangle Info for Shared Edges and remove Duplicates.
            RefineEdges(ref refinedEdges, ref delaEdges, ref delaEdgeCount, ref voronoiEdges);

            // Now for each point, generate Voronoi diagram.
            for (int i = 0; i < vertexCount; ++i)
            {

                // Try moving this to Centroid of the Voronoi Polygon.
                GetAffectingEdges(i, delaEdges, delaEdgeCount, ref affectsEdges, ref voronoiCheck, ref affectingEdgeCount);
                var bounded = affectingEdgeCount != 0;

                // Check for Boundedness
                for (int j = 0; j < affectingEdgeCount; ++j)
                {
                    // Edge Index.
                    var ei = affectsEdges[j];
                    if (delaEdges[ei].z == -1 || delaEdges[ei].w == -1)
                    {
                        bounded = false;
                        break;
                    }
                }

                // If this is bounded point, relocate to Voronoi Diagram's Centroid
                if (bounded)
                {
                    polygonCentroid = ConnectTriangles(ref connectedTri, ref affectsEdges, ref voronoiCheck, voronoiEdges, affectingEdgeCount);
                    if (!polygonCentroid)
                    {
                        break;
                    }

                    float2 point = float2.zero;
                    float area = 0, distance = 0;
                    for (int k = 0; k < affectingEdgeCount; ++k)
                    {
                        CentroidByPolygon(connectedTri[k], triangles, ref point, ref area, ref distance);
                    }
                    point /= (3 * area);
                    pgPoints[i] = point;
                }

            }

            // Do Delaunay Again.
            int srcIndexCount = indexCount, srcVertexCount = vertexCount;
            indexCount = 0; vertexCount = 0; triangleCount = 0;
            if (polygonCentroid)
            {
                validGraph = Tessellator.Tessellate(allocator, pgPoints, pgPointCount, pgEdges, pgEdgeCount, ref vertices, ref vertexCount, ref indices, ref indexCount);
                if (validGraph)
                    ModuleHandle.BuildTriangles(vertices, vertexCount, indices, indexCount, ref triangles, ref triangleCount, ref cmxArea, ref avgArea, ref cmnArea, ref maxEdge, ref avgEdge, ref minEdge);
                // This Edge validation prevents artifacts by forcing a fallback. todo: Fix the actual bug in Outline generation.
                validGraph = validGraph && (cmxArea < maxArea * kMaxAreaTolerance) && (maxEdge < avgEdge * kMaxEdgeTolerance);
            }

            // Cleanup.
            triangles.Dispose();
            delaEdges.Dispose();
            refinedEdges.Dispose();
            voronoiCheck.Dispose();
            voronoiEdges.Dispose();
            affectsEdges.Dispose();
            triCentroids.Dispose();
            connectedTri.Dispose();
            return (validGraph && srcIndexCount == indexCount && srcVertexCount == vertexCount);

        }

    }

}
