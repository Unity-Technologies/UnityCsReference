// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine
{
    public sealed partial class GeometryUtility
    {
        public static Plane[] CalculateFrustumPlanes(Camera camera)
        {
            Plane[] planes = new Plane[6];
            CalculateFrustumPlanes(camera, planes);
            return planes;
        }

        public static Plane[] CalculateFrustumPlanes(Matrix4x4 worldToProjectionMatrix)
        {
            Plane[] planes = new Plane[6];
            CalculateFrustumPlanes(worldToProjectionMatrix, planes);
            return planes;
        }

        public static void CalculateFrustumPlanes(Camera camera, Plane[] planes)
        {
            CalculateFrustumPlanes(camera.projectionMatrix * camera.worldToCameraMatrix, planes);
        }

        public static void CalculateFrustumPlanes(Matrix4x4 worldToProjectionMatrix, Plane[] planes)
        {
            if (planes == null) throw new ArgumentNullException("planes");
            if (planes.Length != 6) throw new ArgumentException("Planes array must be of length 6.", "planes");
            Internal_ExtractPlanes(planes, worldToProjectionMatrix);
        }

        public static Bounds CalculateBounds(Vector3[] positions, Matrix4x4 transform)
        {
            if (positions == null) throw new ArgumentNullException("positions");
            if (positions.Length == 0) throw new ArgumentException("Zero-sized array is not allowed.", "positions");
            return Internal_CalculateBounds(positions, transform);
        }

        // Creates a plane for a polygon that's defined by an array of vertices. Works for concave polygons, polygons containing colinear vertices as well as non-planar polygons.
        // Returns false if it's not possible to determine a plane for the given vertices.
        // This can happen for certain self-intersecting polygons or when all vertices are all aligned in a line or a single point.
        public static bool TryCreatePlaneFromPolygon(Vector3[] vertices, out Plane plane)
        {
            if (vertices == null || vertices.Length < 3)
            {
                plane = new Plane(Vector3.up, 0);
                return false;
            }
            if (vertices.Length == 3)
            {
                var v0 = vertices[0];
                var v1 = vertices[1];
                var v2 = vertices[2];
                plane = new Plane(v0, v1, v2);
                return plane.normal.sqrMagnitude > 0;
            }

            Vector3 normal = Vector3.zero;
            int prev_index = vertices.Length - 1;
            Vector3 prev_vertex = vertices[prev_index];
            for (int e = 0; e < vertices.Length; e++)
            {
                Vector3 curr_vertex = vertices[e];
                normal.x = normal.x + ((prev_vertex.y - curr_vertex.y) * (prev_vertex.z + curr_vertex.z));
                normal.y = normal.y + ((prev_vertex.z - curr_vertex.z) * (prev_vertex.x + curr_vertex.x));
                normal.z = normal.z + ((prev_vertex.x - curr_vertex.x) * (prev_vertex.y + curr_vertex.y));

                prev_vertex = curr_vertex;
            }
            normal.Normalize();

            float d = 0;
            for (int e = 0; e < vertices.Length; e++)
            {
                Vector3 curr_vertex = vertices[e];
                d -= Vector3.Dot(normal, curr_vertex);
            }
            d /= vertices.Length;

            plane = new Plane(normal, d);
            return plane.normal.sqrMagnitude > 0;
        }
    }
}
