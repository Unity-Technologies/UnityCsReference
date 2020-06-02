// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Internal;
using UnityEngine.Rendering;
using UnityEngine.Scripting;
using UnityEditor.SceneManagement;
using UnityEditor.Experimental.SceneManagement;

namespace UnityEditor
{
    // Helper functions for Scene View style 3D GUI
    public sealed partial class HandleUtility
    {
        // Helper function for doing arrows.
        public static float CalcLineTranslation(Vector2 src, Vector2 dest, Vector3 srcPosition, Vector3 constraintDir)
        {
            // Apply handle matrix
            srcPosition = Handles.matrix.MultiplyPoint(srcPosition);
            constraintDir = Handles.matrix.MultiplyVector(constraintDir);


            // The constrained direction is facing towards the camera, THATS BAD when the handle is close to the camera
            // The srcPosition  goes through to the other side of the camera
            float invert = 1.0F;
            Vector3 cameraForward = Camera.current == null ? Vector3.forward : Camera.current.transform.forward;
            if (Vector3.Dot(constraintDir, cameraForward) < 0.0F)
                invert = -1.0F;

            // Ok - Get the parametrization of the line
            // p1 = src position, p2 = p1 + ConstraintDir.
            // we then parametrise the perpendicular position of dest into the line (p1-p2)
            Vector3 cd = constraintDir;
            cd.y = -cd.y;
            Camera cam = Camera.current;
            // if camera is null, then we are drawing in OnGUI, where y-coordinate goes top-to-bottom
            Vector2 p1 = cam == null
                ? Vector2.Scale(srcPosition, new Vector2(1f, -1f))
                : EditorGUIUtility.PixelsToPoints(cam.WorldToScreenPoint(srcPosition));
            Vector2 p2 = cam == null
                ? Vector2.Scale(srcPosition + constraintDir * invert, new Vector2(1f, -1f))
                : EditorGUIUtility.PixelsToPoints(cam.WorldToScreenPoint(srcPosition + constraintDir * invert));
            Vector2 p3 = dest;
            Vector2 p4 = src;

            if (p1 == p2)
                return 0;

            p3.y = -p3.y;
            p4.y = -p4.y;
            float t0 = GetParametrization(p4, p1, p2);
            float t1 = GetParametrization(p3, p1, p2);

            float output = (t1 - t0) * invert;
            return output;
        }

        internal static float GetParametrization(Vector2 x0, Vector2 x1, Vector2 x2)
        {
            return -(Vector2.Dot(x1 - x0, x2 - x1) / (x2 - x1).sqrMagnitude);
        }

        // This limits the "shoot off into infinity" factor when the cursor ray and constraint are near parallel.
        // Increase this value to more conservatively restrict movement, lower to allow more extreme values.
        // Ex, with a camera roughly 30 degrees to the handle a value of .1 restricts translation to ~1500m, whereas a
        // value of .01 will allow closer to 50000 units of movement.
        const float k_MinRayConstraintDot = .05f;

        // constraintOrigin and constraintDir are expected to be in Handle space (ie, origin and direction are
        // pre-multiplied by the Handles.matrix)
        internal static bool CalcPositionOnConstraint(Camera camera, Vector2 guiPosition, Vector3 constraintOrigin, Vector3 constraintDir, out Vector3 position)
        {
            if (CalcParamOnConstraint(camera, guiPosition, constraintOrigin, constraintDir, out float pointOnLineParam))
            {
                position = constraintOrigin + constraintDir * pointOnLineParam;
                return true;
            }

            position = Vector3.zero;
            return false;
        }

        internal static bool CalcParamOnConstraint(Camera camera, Vector2 guiPosition, Vector3 constraintOrigin, Vector3 constraintDir, out float parameterization)
        {
            Vector3 constraintToCameraTangent = Vector3.Cross(constraintDir, camera.transform.position - constraintOrigin);
            Vector3 constraintPlaneNormal = Vector3.Cross(constraintDir, constraintToCameraTangent);
            Plane plane = new Plane(constraintPlaneNormal, constraintOrigin);
            var ray = GUIPointToWorldRay(guiPosition);

            if (Vector3.Dot(ray.direction, plane.normal) > k_MinRayConstraintDot && plane.Raycast(ray, out float distance))
            {
                var pointOnPlane = ray.GetPoint(distance);
                parameterization = PointOnLineParameter(pointOnPlane, constraintOrigin, constraintDir);
                return !float.IsInfinity(parameterization);
            }

            parameterization = 0f;
            return false;
        }

        // Returns the parameter for the projection of the /point/ on the given line
        public static float PointOnLineParameter(Vector3 point, Vector3 linePoint, Vector3 lineDirection)
        {
            return (Vector3.Dot(lineDirection, (point - linePoint))) / lineDirection.sqrMagnitude;
        }

        // Project /point/ onto a line.
        public static Vector3 ProjectPointLine(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
        {
            Vector3 relativePoint = point - lineStart;
            Vector3 lineDirection = lineEnd - lineStart;
            float length = lineDirection.magnitude;
            Vector3 normalizedLineDirection = lineDirection;
            if (length > .000001f)
                normalizedLineDirection /= length;

            float dot = Vector3.Dot(normalizedLineDirection, relativePoint);
            dot = Mathf.Clamp(dot, 0.0F, length);

            return lineStart + normalizedLineDirection * dot;
        }

        // Calculate distance between a point and a line.
        public static float DistancePointLine(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
        {
            return Vector3.Magnitude(ProjectPointLine(point, lineStart, lineEnd) - point);
        }

        // Get standard acceleration for dragging values (RO).
        public static float acceleration { get { return NumericFieldDraggerUtility.Acceleration(Event.current.shift, Event.current.alt); } }

        // Get nice mouse delta to use for dragging a float value (RO).
        public static float niceMouseDelta { get { return NumericFieldDraggerUtility.NiceDelta(Event.current.delta, acceleration); } }

        // Get nice mouse delta to use for zooming (RO).
        public static float niceMouseDeltaZoom
        {
            get
            {
                Vector2 d = -Event.current.delta;

                // Decide which direction the mouse delta goes.
                // Problem is that when the user zooms horizontal and vertical, it can jitter back and forth.
                // So we only update from which axis we pick the sign if x and y
                // movement is not very close to each other
                if (Mathf.Abs(Mathf.Abs(d.x) - Mathf.Abs(d.y)) / Mathf.Max(Mathf.Abs(d.x), Mathf.Abs(d.y)) > .1f)
                {
                    if (Mathf.Abs(d.x) > Mathf.Abs(d.y))
                        s_UseYSignZoom = false;
                    else
                        s_UseYSignZoom = true;
                }

                if (s_UseYSignZoom)
                    return Mathf.Sign(d.y) * d.magnitude * acceleration;
                return Mathf.Sign(d.x) * d.magnitude * acceleration;
            }
        }
        static bool s_UseYSignZoom;

        // Pixel distance from mouse pointer to line.
        public static float DistanceToLine(Vector3 p1, Vector3 p2)
        {
            p1 = WorldToGUIPoint(p1);
            p2 = WorldToGUIPoint(p2);

            Vector2 point = Event.current.mousePosition;

            return DistanceToLineInternal(point, p1, p2);
        }

        internal static float DistanceToLineInternal(Vector3 point, Vector3 p1, Vector3 p2)
        {
            float retval = DistancePointLine(point, p1, p2);
            if (retval < 0)
                retval = 0.0f;
            return retval;
        }

        // Pixel distance from mouse pointer to camera facing circle.
        public static float DistanceToCircle(Vector3 position, float radius)
        {
            Vector2 screenCenter = WorldToGUIPoint(position);
            Camera cam = Camera.current;
            if (cam)
            {
                var screenEdge = WorldToGUIPoint(position + cam.transform.right * radius);
                radius = (screenCenter - screenEdge).magnitude;
            }
            float dist = (screenCenter - Event.current.mousePosition).magnitude;
            if (dist < radius)
                return 0;
            return dist - radius;
        }

        // Pixel distance from mouse pointer to cone projection on screen
        static ProfilerMarker s_DistanceToConeMarker = new ProfilerMarker("Handles.DistanceToCone");
        static readonly Vector3[] s_DistanceToConePoints = new Vector3[7];
        public static float DistanceToCone(Vector3 position, Quaternion rotation, float size)
        {
            using (s_DistanceToConeMarker.Auto())
            {
                // our handles cone mesh is along Z axis:
                // base at Z=-0.5 with radius 0.4, and apex at Z=0.7
                var baseZ = -0.5f * size;
                var apexZ = 0.7f * size;
                var baseR = 0.4f * size;

                // approximate the cone with a six-sided base
                var baseR60x = baseR * 0.5f; // cos 60
                var baseR60y = baseR * 0.866f; // sin 60
                var mat = Matrix4x4.TRS(position, rotation, Vector3.one);
                s_DistanceToConePoints[0] = mat.MultiplyPoint(new Vector3(0, 0, apexZ));
                s_DistanceToConePoints[1] = mat.MultiplyPoint(new Vector3(+baseR, 0, baseZ));
                s_DistanceToConePoints[2] = mat.MultiplyPoint(new Vector3(-baseR, 0, baseZ));
                s_DistanceToConePoints[3] = mat.MultiplyPoint(new Vector3(+baseR60x, +baseR60y, baseZ));
                s_DistanceToConePoints[4] = mat.MultiplyPoint(new Vector3(-baseR60x, +baseR60y, baseZ));
                s_DistanceToConePoints[5] = mat.MultiplyPoint(new Vector3(+baseR60x, -baseR60y, baseZ));
                s_DistanceToConePoints[6] = mat.MultiplyPoint(new Vector3(-baseR60x, -baseR60y, baseZ));

                return DistanceToPointCloudConvexHull(s_DistanceToConePoints);
            }
        }

        // Pixel distance from mouse pointer to cube projection on screen
        static ProfilerMarker s_DistanceToCubeMarker = new ProfilerMarker("Handles.DistanceToCube");
        static readonly Vector3[] s_DistanceToCubePoints = new Vector3[8];
        public static float DistanceToCube(Vector3 position, Quaternion rotation, float size)
        {
            using (s_DistanceToCubeMarker.Auto())
            {
                var s = size * 0.5f;
                var mat = Matrix4x4.TRS(position, rotation, Vector3.one);
                s_DistanceToCubePoints[0] = mat.MultiplyPoint(new Vector3(+s, +s, +s));
                s_DistanceToCubePoints[1] = mat.MultiplyPoint(new Vector3(-s, +s, +s));
                s_DistanceToCubePoints[2] = mat.MultiplyPoint(new Vector3(+s, -s, +s));
                s_DistanceToCubePoints[3] = mat.MultiplyPoint(new Vector3(-s, -s, +s));
                s_DistanceToCubePoints[4] = mat.MultiplyPoint(new Vector3(+s, +s, -s));
                s_DistanceToCubePoints[5] = mat.MultiplyPoint(new Vector3(-s, +s, -s));
                s_DistanceToCubePoints[6] = mat.MultiplyPoint(new Vector3(+s, -s, -s));
                s_DistanceToCubePoints[7] = mat.MultiplyPoint(new Vector3(-s, -s, -s));
                return DistanceToPointCloudConvexHull(s_DistanceToCubePoints);
            }
        }

        // Pixel distance from mouse pointer to a rectangle on screen
        static Vector3[] s_Points = {Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero};
        public static float DistanceToRectangle(Vector3 position, Quaternion rotation, float size)
        {
            return DistanceToRectangleInternal(position, rotation, new Vector2(size, size));
        }

        // Pixel distance from mouse pointer to a rectangle on screen.
        // The method is stable in pixel space but fails when one or more corners of the rectangle is behind the camera.
        internal static float DistanceToRectangleInternal(Vector3 position, Quaternion rotation, Vector2 size)
        {
            Vector3 sideways = rotation * new Vector3(size.x, 0, 0);
            Vector3 up = rotation * new Vector3(0, size.y, 0);
            s_Points[0] = WorldToGUIPoint(position + sideways + up);
            s_Points[1] = WorldToGUIPoint(position + sideways - up);
            s_Points[2] = WorldToGUIPoint(position - sideways - up);
            s_Points[3] = WorldToGUIPoint(position - sideways + up);
            s_Points[4] = s_Points[0];

            Vector2 pos = Event.current.mousePosition;
            bool oddNodes = false;
            int j = 4;
            for (int i = 0; i < 5; i++)
            {
                if ((s_Points[i].y > pos.y) != (s_Points[j].y > pos.y))
                {
                    if (pos.x < (s_Points[j].x - s_Points[i].x) * (pos.y - s_Points[i].y) / (s_Points[j].y - s_Points[i].y) + s_Points[i].x)
                    {
                        oddNodes = !oddNodes;
                    }
                }
                j = i;
            }
            if (!oddNodes)
            {
                // Distance to closest edge (not so fast)
                float closestDist = -1f;
                j = 1;
                for (int i = 0; i < 4; i++)
                {
                    var dist = DistancePointToLineSegment(pos, s_Points[i], s_Points[j++]);
                    if (dist < closestDist || closestDist < 0)
                        closestDist = dist;
                }
                return closestDist;
            }
            return 0;
        }

        // Pixel distance from mouse pointer to a rectangle on screen.
        // Tests if mouse ray intersects the rectangle performed in world space first,
        // then the distance between nearest point on the rectangle and mouse position calculated in pixel space.
        // This method is more stable than DistanceToRectangleInternal for cases when one or more corners of the rectangle is behind the camera.
        // But at the same time it is less stable than DistanceToRectangleInternal in pixel space when the rectangle plane is parallel to cameras forward direction.
        internal static float DistanceToRectangleInternalWorldSpace(Vector3 position, Quaternion rotation, Vector2 size)
        {
            Quaternion invRotation = Quaternion.Inverse(rotation);

            Ray ray = GUIPointToWorldRay(Event.current.mousePosition);
            ray.origin = invRotation * (ray.origin - position);
            ray.direction = invRotation * ray.direction;

            Plane plane = new Plane(Vector3.forward, Vector3.zero);

            float enter;
            if (plane.Raycast(ray, out enter))
            {
                Vector3 hitPoint = ray.GetPoint(enter);

                Vector3 d = new Vector3(
                    Mathf.Max(Mathf.Abs(hitPoint.x) - size.x, 0.0f) * Mathf.Sign(hitPoint.x),
                    Mathf.Max(Mathf.Abs(hitPoint.y) - size.y, 0.0f) * Mathf.Sign(hitPoint.y),
                    0.0f);

                Vector3 nearestPoint = hitPoint - d;

                hitPoint = rotation * hitPoint + position;
                nearestPoint = rotation * nearestPoint + position;

                return Vector2.Distance(WorldToGUIPoint(hitPoint), WorldToGUIPoint(nearestPoint));
            }

            return float.PositiveInfinity;
        }

        internal static float DistanceToDiamond(Vector3 position, Quaternion rotation, float size)
        {
            return DistanceToDiamondInternal(position, rotation, size, Event.current.mousePosition);
        }

        internal static float DistanceToDiamondInternal(Vector3 position, Quaternion rotation, float size, Vector2 mousePosition)
        {
            Vector3 sideways = rotation * new Vector3(size, 0, 0);
            Vector3 up = rotation * new Vector3(0, size, 0);
            s_Points[0] = WorldToGUIPoint(position + sideways);
            s_Points[1] = WorldToGUIPoint(position - up);
            s_Points[2] = WorldToGUIPoint(position - sideways);
            s_Points[3] = WorldToGUIPoint(position + up);
            s_Points[4] = s_Points[0];

            Vector2 pos = mousePosition;
            bool oddNodes = false;
            int j = 4;
            for (int i = 0; i < 5; i++)
            {
                if ((s_Points[i].y > pos.y) != (s_Points[j].y > pos.y))
                {
                    if (pos.x < (s_Points[j].x - s_Points[i].x) * (pos.y - s_Points[i].y) / (s_Points[j].y - s_Points[i].y) + s_Points[i].x)
                    {
                        oddNodes = !oddNodes;
                    }
                }
                j = i;
            }
            if (!oddNodes)
            {
                // Distance to closest edge (not so fast)
                float dist, closestDist = -1f;
                j = 1;
                for (int i = 0; i < 4; i++)
                {
                    dist = DistancePointToLineSegment(pos, s_Points[i], s_Points[j++]);
                    if (dist < closestDist || closestDist < 0)
                        closestDist = dist;
                }
                return closestDist;
            }
            return 0;
        }

        // Distance from a point /p/ in 2d to a line defined by two s_Points /a/ and /b/
        public static float DistancePointToLine(Vector2 p, Vector2 a, Vector2 b)
        {
            return Mathf.Abs((b.x - a.x) * (a.y - p.y) - (a.x - p.x) * (b.y - a.y)) / (b - a).magnitude;
        }

        // Distance from a point /p/ in 2d to a line segment defined by two s_Points /a/ and /b/
        public static float DistancePointToLineSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            float l2 = (b - a).sqrMagnitude;    // i.e. |b-a|^2 -  avoid a sqrt
            if (l2 == 0.0)
                return (p - a).magnitude;       // a == b case
            float t = Vector2.Dot(p - a, b - a) / l2;
            if (t < 0.0)
                return (p - a).magnitude;       // Beyond the 'a' end of the segment
            if (t > 1.0)
                return (p - b).magnitude;         // Beyond the 'b' end of the segment
            Vector2 projection = a + t * (b - a); // Projection falls on the segment
            return (p - projection).magnitude;
        }

        // Pixel distance from mouse pointer to a 3D disc.
        public static float DistanceToDisc(Vector3 center, Vector3 normal, float radius)
        {
            Vector3 tangent = Vector3.Cross(normal, Vector3.up);
            if (tangent.sqrMagnitude < .001f)
                tangent = Vector3.Cross(normal, Vector3.right);
            return DistanceToArc(center, normal, tangent, 360, radius);
        }

        // Get the nearest 3D point.
        public static Vector3 ClosestPointToDisc(Vector3 center, Vector3 normal, float radius)
        {
            Vector3 tangent = Vector3.Cross(normal, Vector3.up);
            if (tangent.sqrMagnitude < .001f)
                tangent = Vector3.Cross(normal, Vector3.right);
            return ClosestPointToArc(center, normal, tangent, 360, radius);
        }

        static Vector3[] m_ArcPointsBuffer = new Vector3[60];

        // Pixel distance from mouse pointer to a 3D section of a disc.
        public static float DistanceToArc(Vector3 center, Vector3 normal, Vector3 from, float angle, float radius)
        {
            Handles.SetDiscSectionPoints(m_ArcPointsBuffer, center, normal, from, angle, radius);
            return DistanceToPolyLineOnPlane(m_ArcPointsBuffer, center, normal);
        }

        // Get the nearest 3D point.
        public static Vector3 ClosestPointToArc(Vector3 center, Vector3 normal, Vector3 from, float angle, float radius)
        {
            Handles.SetDiscSectionPoints(m_ArcPointsBuffer, center, normal, from, angle, radius);
            return ClosestPointToPolyLine(m_ArcPointsBuffer);
        }

        // Pixel distance from mouse pointer to a polyline.
        public static float DistanceToPolyLine(params Vector3[] points)
        {
            Matrix4x4 handleMatrix = Handles.matrix;
            CameraProjectionCache cam = new CameraProjectionCache(Camera.current, Screen.height);
            Vector2 mouse = Event.current.mousePosition;

            Vector2 p1 = cam.WorldToGUIPoint(handleMatrix.MultiplyPoint3x4(points[0]));
            Vector2 p2 = cam.WorldToGUIPoint(handleMatrix.MultiplyPoint3x4(points[1]));
            float dist = DistanceToLineInternal(mouse, p1, p2);

            for (int i = 2; i < points.Length; i++)
            {
                p1 = p2;
                p2 = cam.WorldToGUIPoint(handleMatrix.MultiplyPoint3x4(points[i]));
                float d = DistanceToLineInternal(mouse, p1, p2);
                if (d < dist)
                    dist = d;
            }

            return dist;
        }

        // Pixel distance from mouse pointer to a polyline.
        internal static float DistanceToPolyLine(Vector3[] points, bool loop, out int index)
        {
            Matrix4x4 handleMatrix = Handles.matrix;
            CameraProjectionCache cam = new CameraProjectionCache(Camera.current, Screen.height);
            Vector2 mouse = Event.current.mousePosition;

            Vector2 p1 = cam.WorldToGUIPoint(handleMatrix.MultiplyPoint3x4(points[0]));
            Vector2 p2 = cam.WorldToGUIPoint(handleMatrix.MultiplyPoint3x4(points[1]));
            float dist = DistanceToLineInternal(mouse, p1, p2);
            index = 0;

            for (int i = 2, c = points.Length; i < (loop ? c + 1 : c); i++)
            {
                p1 = p2;
                p2 = cam.WorldToGUIPoint(handleMatrix.MultiplyPoint3x4(points[i % c]));
                float d = DistanceToLineInternal(mouse, p1, p2);
                if (d < dist)
                {
                    index = i - 1;
                    dist = d;
                }
            }

            return dist;
        }

        // Pixel distance from mouse pointer to a polyline on a 2D plane.
        internal static float DistanceToPolyLineOnPlane(Vector3[] points, Vector3 center, Vector3 normal)
        {
            Matrix4x4 handleMatrix = Handles.matrix;
            var worldPosition = handleMatrix.MultiplyPoint3x4(center);
            var worldNormal = handleMatrix.MultiplyVector(normal);
            Plane p = new Plane(worldNormal, worldPosition);
            Vector2 point = Event.current.mousePosition;
            Ray r = GUIPointToWorldRay(point);

            float enter;

            if (!p.Raycast(r, out enter))
                return DistanceToPolyLine(points);

            Vector3 intersect = handleMatrix.inverse.MultiplyPoint3x4(r.GetPoint(enter));
            Vector3 p1 = points[0];
            Vector3 p2 = points[1];

            float dist = DistanceToLineInternal(intersect, p1, p2);
            Vector3 s1 = Vector3.zero, s2 = Vector3.zero;

            for (int i = 2; i < points.Length; i++)
            {
                p1 = p2;
                p2 = points[i];
                float d = DistanceToLineInternal(intersect, p1, p2);

                if (d < dist)
                {
                    dist = d;
                    s1 = p1;
                    s2 = p2;
                }
            }

            return DistanceToLineInternal(point,
                WorldToGUIPoint(s1),
                WorldToGUIPoint(s2));
        }

        // Get the nearest 3D point.
        public static Vector3 ClosestPointToPolyLine(params Vector3[] vertices)
        {
            float dist = DistanceToLine(vertices[0], vertices[1]);
            int nearest = 0;// Which segment we're closest to
            for (int i = 2; i < vertices.Length; i++)
            {
                float d = DistanceToLine(vertices[i - 1], vertices[i]);
                if (d < dist)
                {
                    dist = d;
                    nearest = i - 1;
                }
            }

            Vector3 lineStart = vertices[nearest];
            Vector3 lineEnd = vertices[nearest + 1];

            Vector2 relativePoint = Event.current.mousePosition - WorldToGUIPoint(lineStart);
            Vector2 lineDirection = WorldToGUIPoint(lineEnd) - WorldToGUIPoint(lineStart);
            float length = lineDirection.magnitude;
            float dot = Vector3.Dot(lineDirection, relativePoint);
            if (length > .000001f)
                dot /= length * length;
            dot = Mathf.Clamp01(dot);

            return Vector3.Lerp(lineStart, lineEnd, dot);
        }

        static float CalcPointSide(Vector2 l0, Vector2 l1, Vector2 point)
        {
            return (l1.y - l0.y) * (point.x - l0.x) - (l1.x - l0.x) * (point.y - l0.y);
        }

        static float DistancePointToConvexHull(Vector2 p, List<Vector2> hull)
        {
            var distance = float.PositiveInfinity;
            if (hull == null || hull.Count == 0)
                return distance;

            var inside = hull.Count > 1;
            var sideSign = 0;
            for (var i = 0; i < hull.Count; ++i)
            {
                // get the line segment
                var j = i == 0 ? hull.Count - 1 : i - 1;
                var pt1 = hull[i];
                var pt2 = hull[j];

                // for point to be inside the hull, "side"
                // signs must be the same for all edges.
                var thisSide = CalcPointSide(pt1, pt2, p);
                var thisSideSign = thisSide >= 0 ? 1 : -1;
                if (sideSign == 0)
                    sideSign = thisSideSign;
                else if (thisSideSign != sideSign)
                    inside = false;

                // get minimum distance to each segment
                var thisDistance = DistancePointToLineSegment(p, pt1, pt2);
                distance = Mathf.Min(distance, thisDistance);
            }
            if (inside)
                distance = 0;
            return distance;
        }

        static void RemoveInsidePoints(int countLimit, Vector2 pt, List<Vector2> hull)
        {
            while (hull.Count >= countLimit && CalcPointSide(hull[hull.Count - 2], hull[hull.Count - 1], pt) <= 0)
                hull.RemoveAt(hull.Count - 1);
        }

        // Note: .z components of input points are ignored; result is a 2D hull on .xy
        static void CalcConvexHull2D(Vector3[] points, List<Vector2> outHull)
        {
            outHull.Clear();
            if (points == null || points.Length == 0)
                return;
            var needCapacity = points.Length + 1;
            if (outHull.Capacity < needCapacity)
                outHull.Capacity = needCapacity;
            if (points.Length == 1)
            {
                outHull.Add(points[0]);
                return;
            }

            // Andrew's monotone chain algorithm:
            // First sort the input points
            Array.Sort(points, (a, b) =>
            {
                var ca = a.x.CompareTo(b.x);
                return ca != 0 ? ca : a.y.CompareTo(b.y);
            });

            // Build lower hull
            for (int i = 0; i < points.Length; ++i)
            {
                Vector2 pt = points[i];
                RemoveInsidePoints(2, pt, outHull);
                outHull.Add(pt);
            }

            // Build upper hull
            for (int i = points.Length - 2, j = outHull.Count + 1; i >= 0; --i)
            {
                Vector2 pt = points[i];
                RemoveInsidePoints(j, pt, outHull);
                outHull.Add(pt);
            }

            // Remove last point (it's the same as the first one)
            outHull.RemoveAt(outHull.Count - 1);
        }

        // Note: modifies input points array
        static void CalcPointCloudConvexHull(Vector3[] points, List<Vector2> outHull)
        {
            outHull.Clear();
            if (points == null || points.Length == 0)
                return;

            // project point cloud into 2D GUI space
            var handleMatrix = Handles.matrix;
            var cam = new CameraProjectionCache(Camera.current, Screen.height);
            for (var i = 0; i < points.Length; ++i)
                points[i] = cam.WorldToGUIPoint(handleMatrix.MultiplyPoint3x4(points[i]));

            // calculate 2D convex hull
            CalcConvexHull2D(points, outHull);
        }

        // Note: input array contents are modified
        static readonly List<Vector2> s_PointCloudConvexHull = new List<Vector2>();
        static float DistanceToPointCloudConvexHull(params Vector3[] points)
        {
            if (points == null || points.Length == 0)
                return float.PositiveInfinity;

            var mousePos = Event.current.mousePosition;
            CalcPointCloudConvexHull(points, s_PointCloudConvexHull);
            return DistancePointToConvexHull(mousePos, s_PointCloudConvexHull);
        }

        // Record a distance measurement from a handle.
        public static void AddControl(int controlId, float distance)
        {
            if (distance < s_CustomPickDistance && distance > kPickDistance)
                distance = kPickDistance;

            if (distance <= s_NearestDistance)
            {
                s_NearestDistance = distance;
                s_NearestControl = controlId;
            }
        }

        // Add the ID for a default control. This will be picked if nothing else is
        public static void AddDefaultControl(int controlId)
        {
            AddControl(controlId, kPickDistance);
        }

        static int s_PreviousNearestControl;
        static int s_NearestControl;
        static float s_NearestDistance;
        internal const float kPickDistance = 5.0f;
        internal static float s_CustomPickDistance = kPickDistance;

        public static int nearestControl { get { return s_NearestDistance <= kPickDistance ? s_NearestControl : 0; } set { s_NearestControl = value; } }

        [RequiredByNativeCode]
        internal static void BeginHandles()
        {
            Handles.Init();
            switch (Event.current.type)
            {
                case EventType.Layout:
                    s_NearestControl = 0;
                    s_NearestDistance = kPickDistance;
                    break;
            }
            Handles.lighting = true;
            Handles.color = Color.white;
            Handles.zTest = CompareFunction.Always;
            s_CustomPickDistance = kPickDistance;
            Handles.Internal_SetCurrentCamera(null);
            EditorGUI.s_DelayedTextEditor.BeginGUI();
        }

        [RequiredByNativeCode]
        internal static void EndHandles()
        {
            if (s_PreviousNearestControl != s_NearestControl
                && s_NearestControl != 0)
            {
                s_PreviousNearestControl = s_NearestControl;
                Repaint();
            }
            // Give the delayed text editor a chance to notice that it lost focus.
            EditorGUI.s_DelayedTextEditor.EndGUI(Event.current.type);
        }

        const float k_KHandleSize = 80.0f;

        // Get world space size of a manipulator handle at given position.
        public static float GetHandleSize(Vector3 position)
        {
            Camera cam = Camera.current;
            position = Handles.matrix.MultiplyPoint(position);
            if (cam)
            {
                Transform tr = cam.transform;
                Vector3 camPos = tr.position;
                float distance = Vector3.Dot(position - camPos, tr.TransformDirection(new Vector3(0, 0, 1)));
                Vector3 screenPos = cam.WorldToScreenPoint(camPos + tr.TransformDirection(new Vector3(0, 0, distance)));
                Vector3 screenPos2 = cam.WorldToScreenPoint(camPos + tr.TransformDirection(new Vector3(1, 0, distance)));
                float screenDist = (screenPos - screenPos2).magnitude;
                return (k_KHandleSize / Mathf.Max(screenDist, 0.0001f)) * EditorGUIUtility.pixelsPerPoint;
            }
            return 20.0f;
        }

        // Convert world space point to a 2D GUI position.
        public static Vector2 WorldToGUIPoint(Vector3 world)
        {
            return WorldToGUIPointWithDepth(world);
        }

        // Convert world space point to a 2D GUI position.
        public static Vector3 WorldToGUIPointWithDepth(Vector3 world)
        {
            return WorldToGUIPointWithDepth(world, Camera.current, Handles.matrix, Screen.height);
        }

        // Convert world space point to a 2D GUI position.
        // Use this version in critical loops.
        internal static Vector3 WorldToGUIPointWithDepth(Vector3 world, Camera camera, Matrix4x4 matrixHandles, float screenHeight)
        {
            world = matrixHandles.MultiplyPoint(world);

            if (camera)
            {
                Vector3 pos = camera.WorldToScreenPoint(world);
                pos.y = screenHeight - pos.y;
                Vector2 points = EditorGUIUtility.PixelsToPoints(pos);
                points = GUIClip.Clip(points);
                return new Vector3(points.x, points.y, pos.z);
            }

            return world;
        }

        public static Vector2 GUIPointToScreenPixelCoordinate(Vector2 guiPoint)
        {
            var unclippedPosition = GUIClip.Unclip(guiPoint);
            var screenPixelPos = EditorGUIUtility.PointsToPixels(unclippedPosition);
            screenPixelPos.y = Screen.height - screenPixelPos.y;
            return screenPixelPos;
        }

        // Convert 2D GUI position to a world space ray.
        public static Ray GUIPointToWorldRay(Vector2 position)
        {
            return GUIPointToWorldRayPrecise(position);
        }

        private static Ray GUIPointToWorldRayPrecise(Vector2 position, float startZ = float.NegativeInfinity)
        {
            Camera camera = Camera.current;
            if (!camera)
            {
                Debug.LogError("Unable to convert GUI point to world ray if a camera has not been set up!");
                return new Ray(Vector3.zero, Vector3.forward);
            }

            if (float.IsNegativeInfinity(startZ))
                startZ = camera.nearClipPlane;

            Vector2 screenPixelPos = GUIPointToScreenPixelCoordinate(position);
            Rect viewport = camera.pixelRect;

            Matrix4x4 camToWorld = camera.cameraToWorldMatrix;
            Matrix4x4 camToClip = camera.projectionMatrix;
            Matrix4x4 clipToCam = camToClip.inverse;

            // calculate ray origin and direction in world space
            Vector3 rayOriginWorldSpace;
            Vector3 rayDirectionWorldSpace;

            // first construct an arbitrary point that is on the ray through this screen pixel (remap screen pixel point to clip space [-1, 1])
            Vector3 rayPointClipSpace = new Vector3(
                (screenPixelPos.x - viewport.x) * 2.0f / viewport.width - 1.0f,
                (screenPixelPos.y - viewport.y) * 2.0f / viewport.height - 1.0f,
                0.95f
            );

            // and convert that point to camera space
            Vector3 rayPointCameraSpace = clipToCam.MultiplyPoint(rayPointClipSpace);

            if (camera.orthographic)
            {
                // ray direction is always 'camera forward' in orthographic mode
                Vector3 rayDirectionCameraSpace = new Vector3(0.0f, 0.0f, -1.0f);
                rayDirectionWorldSpace = camToWorld.MultiplyVector(rayDirectionCameraSpace);
                rayDirectionWorldSpace.Normalize();

                // in camera space, the ray origin has the same XY coordinates as ANY point on the ray
                // so we just need to override the Z coordinate to startZ to get the correct starting point
                // (assuming camToWorld is a pure rotation/offset, with no scale)
                Vector3 rayOriginCameraSpace = rayPointCameraSpace;
                // The camera/projection matrices follow OpenGL convention: positive Z is towards the viewer.
                // So negate it to get into Unity convention.
                rayOriginCameraSpace.z = -startZ;

                // move it to world space
                rayOriginWorldSpace = camToWorld.MultiplyPoint(rayOriginCameraSpace);
            }
            else
            {
                // in projective mode, the ray passes through the origin in camera space
                // so the ray direction is just (ray point - origin) == (ray point)
                Vector3 rayDirectionCameraSpace = rayPointCameraSpace;
                rayDirectionCameraSpace.Normalize();

                rayDirectionWorldSpace = camToWorld.MultiplyVector(rayDirectionCameraSpace);

                // calculate the correct startZ offset from the camera by moving a distance along the ray direction
                // this assumes camToWorld is a pure rotation/offset, with no scale, so we can use rayDirection.z to calculate how far we need to move
                Vector3 cameraPositionWorldSpace = camToWorld.MultiplyPoint(Vector3.zero);
                // The camera/projection matrices follow OpenGL convention: positive Z is towards the viewer.
                // So negate it to get into Unity convention.
                Vector3 originOffsetWorldSpace = rayDirectionWorldSpace * -startZ / rayDirectionCameraSpace.z;
                rayOriginWorldSpace = cameraPositionWorldSpace + originOffsetWorldSpace;
            }

            return new Ray(rayOriginWorldSpace, rayDirectionWorldSpace);
        }

        // Figure out a rectangle to display a 2D GUI element in 3D space.
        public static Rect WorldPointToSizedRect(Vector3 position, GUIContent content, GUIStyle style)
        {
            Vector2 screenpos = WorldToGUIPoint(position);
            Vector2 size = style.CalcSize(content);
            Rect r = new Rect(screenpos.x, screenpos.y, size.x, size.y);
            switch (style.alignment)
            {
                case TextAnchor.UpperLeft:
                    break;
                case TextAnchor.UpperCenter:
                    r.xMin -= r.width * .5f;
                    break;
                case TextAnchor.UpperRight:
                    r.xMin -= r.width;
                    break;
                case TextAnchor.MiddleLeft:
                    r.yMin -= r.height * .5f;
                    break;
                case TextAnchor.MiddleCenter:
                    r.xMin -= r.width * .5f;
                    r.yMin -= r.height * .5f;
                    break;
                case TextAnchor.MiddleRight:
                    r.xMin -= r.width;
                    r.yMin -= r.height * .5f;
                    break;
                case TextAnchor.LowerLeft:
                    r.yMin -= r.height * .5f;
                    break;
                case TextAnchor.LowerCenter:
                    r.xMin -= r.width * .5f;
                    r.yMin -= r.height;
                    break;
                case TextAnchor.LowerRight:
                    r.xMin -= r.width;
                    r.yMin -= r.height;
                    break;
            }
            return style.padding.Add(r);
        }

        // Pick game object in specified rectangle
        public static GameObject[] PickRectObjects(Rect rect)
        {
            return PickRectObjects(rect, true);
        }

        // *undocumented*
        public static GameObject[] PickRectObjects(Rect rect, bool selectPrefabRootsOnly)
        {
            Camera cam = Camera.current;
            rect = EditorGUIUtility.PointsToPixels(rect);
            rect.x /= cam.pixelWidth;
            rect.width /= cam.pixelWidth;
            rect.y /= cam.pixelHeight;
            rect.height /= cam.pixelHeight;
            return Internal_PickRectObjects(cam, rect, selectPrefabRootsOnly);
        }

        internal static bool FindNearestVertex(Vector2 guiPoint, Transform[] objectsToSearch, out Vector3 vertex)
        {
            Camera cam = Camera.current;
            var screenPoint = EditorGUIUtility.PointsToPixels(guiPoint);
            screenPoint.y = cam.pixelRect.yMax - screenPoint.y;
            return Internal_FindNearestVertex(cam, screenPoint, objectsToSearch, ignoreRaySnapObjects, out vertex);
        }

#pragma warning disable 618
        [Obsolete("Use PickGameObjectCallback")]
        internal delegate GameObject PickClosestGameObjectFunc(Camera cam, int layers, Vector2 position, GameObject[] ignore, GameObject[] filter, out int materialIndex);

        [Obsolete("Use pickGameObjectCustomPasses")]
        internal static PickClosestGameObjectFunc pickClosestGameObjectDelegate;
#pragma warning restore 618

        public static GameObject PickGameObject(Vector2 position, out int materialIndex)
        {
            return PickGameObjectDelegated(position, null, null, out materialIndex);
        }

        public static GameObject PickGameObject(Vector2 position, GameObject[] ignore, out int materialIndex)
        {
            return PickGameObjectDelegated(position, ignore, null, out materialIndex);
        }

        public delegate GameObject PickGameObjectCallback(Camera cam, int layers, Vector2 position, GameObject[] ignore, GameObject[] filter, out int materialIndex);
        public static event PickGameObjectCallback pickGameObjectCustomPasses;

        internal static GameObject PickGameObjectDelegated(Vector2 position, GameObject[] ignore, GameObject[] filter, out int materialIndex)
        {
            Camera cam = Camera.current;
            int layers = cam.cullingMask;
            position = GUIClip.Unclip(position);
            position = EditorGUIUtility.PointsToPixels(position);
            position.y = Screen.height - position.y - cam.pixelRect.yMin;

            materialIndex = -1; // default

            if (ignore != null && ignore.Any(go => go == null))
                throw new ArgumentException("ignore may not contain null elements");
            if (filter != null && filter.Any(go => go == null))
                throw new ArgumentException("filter may not contain null elements");

            GameObject picked = null;

            // deprecated version
            #pragma warning disable 618
            if (pickClosestGameObjectDelegate != null)
                picked = pickClosestGameObjectDelegate(cam, layers, position, ignore, filter, out materialIndex);
            #pragma warning restore 618

            if (picked == null)
                picked = Internal_PickClosestGO(cam, layers, position, ignore, filter, out materialIndex);

            if (picked == null && pickGameObjectCustomPasses != null)
            {
                foreach (var method in pickGameObjectCustomPasses.GetInvocationList())
                {
                    picked = ((PickGameObjectCallback)method)(cam, layers, position, ignore, filter, out materialIndex);
                    // don't trust this method to respect the ignore or filter argument, because in the event that it
                    // does not it will break pick cycling in SceneViewPicking.GetAllOverlapping.
                    if (picked != null && (ignore == null || !ignore.Contains(picked)) && (filter == null || filter.Contains(picked)))
                        break;
                    picked = null;
                }
            }

            return picked;
        }

        public static GameObject PickGameObject(Vector2 position, bool selectPrefabRoot)
        {
            return PickGameObject(position, selectPrefabRoot, null);
        }

        public static GameObject PickGameObject(Vector2 position, bool selectPrefabRoot, GameObject[] ignore)
        {
            return PickGameObject(position, selectPrefabRoot, ignore, null);
        }

        internal static GameObject PickGameObject(Vector2 position, bool selectPrefabRoot, GameObject[] ignore, GameObject[] filter)
        {
            int dummyMaterialIndex;
            GameObject picked = PickGameObjectDelegated(position, ignore, filter, out dummyMaterialIndex);
            if (picked && selectPrefabRoot)
            {
                GameObject pickedRoot = FindSelectionBaseForPicking(picked) ?? picked;
                Transform atc = Selection.activeTransform;
                GameObject selectionRoot = atc ? (FindSelectionBaseForPicking(atc.gameObject) ?? atc.gameObject) : null;
                if (pickedRoot == selectionRoot)
                    return picked;
                return pickedRoot;
            }
            return picked;
        }

        // Get the selection base object, taking into account user enabled picking filter
        internal static GameObject FindSelectionBaseForPicking(GameObject go)
        {
            if (go == null)
                return null;

            // Find prefab based base
            Transform prefabBase = null;

            if (PrefabUtility.IsPartOfNonAssetPrefabInstance(go))
                prefabBase = PrefabUtility.GetOutermostPrefabInstanceRoot(go).transform;

            // Walk up the hierarchy to find the outermost prefab instance root that is not marked as non-pickable, or
            // alternatively a GameObject with the SelectionBaseAttribute assigned.
            Transform tr = go.transform;
            GameObject outerMostSelectableRoot = null;

            while (tr != null)
            {
                if (!SceneVisibilityState.IsGameObjectPickingDisabled(tr.gameObject))
                {
                    // If we come across the prefab base, no need to search further
                    if (tr == prefabBase)
                        return tr.gameObject;

                    // If prefabBase is not pickable, we want to select the nearest pickable root to the base
                    GameObject nestedRoot = PrefabUtility.GetNearestPrefabInstanceRoot(tr);

                    if (nestedRoot != null && tr == nestedRoot.transform)
                        outerMostSelectableRoot = tr.gameObject;

                    // If a SelectionBaseAttribute is found, select the nearest to the picked GameObject
                    if (AttributeHelper.GameObjectContainsAttribute<SelectionBaseAttribute>(tr.gameObject))
                        return tr.gameObject;
                }

                tr = tr.parent;
            }

            return outerMostSelectableRoot;
        }

        // The materials used to draw handles - Don't use unless you're Nicholas.
        public static Material handleMaterial
        {
            get
            {
                if (!s_HandleMaterial)
                {
                    s_HandleMaterial = (Material)EditorGUIUtility.Load("SceneView/Handles.mat");
                }
                return s_HandleMaterial;
            }
        }
        static Material s_HandleMaterial;

        // Called by native code
        [RequiredByNativeCode]
        static void CleanupHandleMaterials()
        {
            DisposeArcIndexBuffer();
            // This is enough for all of them to get re-fetched in next call to InitHandleMaterials()
            s_HandleWireMaterial = null;
        }

        static GraphicsBuffer s_ArcIndexBuffer;

        static void DisposeArcIndexBuffer()
        {
            s_ArcIndexBuffer?.Dispose();
            s_ArcIndexBuffer = null;
        }

        static internal GraphicsBuffer GetArcIndexBuffer(int segments, int sides)
        {
            int indexCount = (segments - 1) * sides * 2 * 3;
            if (s_ArcIndexBuffer != null && s_ArcIndexBuffer.count == indexCount)
                return s_ArcIndexBuffer;

            s_ArcIndexBuffer?.Dispose();
            AssemblyReloadEvents.beforeAssemblyReload += DisposeArcIndexBuffer;
            EditorApplication.quitting += DisposeArcIndexBuffer;

            s_ArcIndexBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Index, indexCount, 2);
            ushort[] ib = new ushort[indexCount];
            var idx = 0;
            for (var seg = 0; seg < segments - 1; ++seg)
            {
                for (var side = 0; side < sides; ++side)
                {
                    var idx00 = seg * sides + side;
                    var idx01 = seg * sides + (side + 1) % sides;
                    var idx10 = (seg + 1) * sides + side;
                    var idx11 = (seg + 1) * sides + (side + 1) % sides;
                    ib[idx + 0] = (ushort)idx00;
                    ib[idx + 1] = (ushort)idx10;
                    ib[idx + 2] = (ushort)idx01;
                    ib[idx + 3] = (ushort)idx01;
                    ib[idx + 4] = (ushort)idx10;
                    ib[idx + 5] = (ushort)idx11;
                    idx += 6;
                }
            }
            s_ArcIndexBuffer.SetData(ib);
            return s_ArcIndexBuffer;
        }

        static void InitHandleMaterials()
        {
            if (!s_HandleWireMaterial)
            {
                RegisterGfxDeviceCleanupIfNeeded();

                s_HandleWireMaterial = (Material)EditorGUIUtility.LoadRequired("SceneView/HandleLines.mat");
                s_HandleWireMaterial2D = (Material)EditorGUIUtility.LoadRequired("SceneView/2DHandleLines.mat");
                s_HandleWireTextureIndex = ShaderUtil.GetTextureBindingIndex(s_HandleWireMaterial.shader, Shader.PropertyToID("_MainTex"));
                s_HandleWireTextureIndex2D = ShaderUtil.GetTextureBindingIndex(s_HandleWireMaterial2D.shader, Shader.PropertyToID("_MainTex"));
                s_HandleWireTextureSamplerIndex = ShaderUtil.GetTextureSamplerBindingIndex(s_HandleWireMaterial.shader, Shader.PropertyToID("_MainTex"));
                s_HandleWireTextureSamplerIndex2D = ShaderUtil.GetTextureSamplerBindingIndex(s_HandleWireMaterial2D.shader, Shader.PropertyToID("_MainTex"));

                s_HandleDottedWireMaterial = (Material)EditorGUIUtility.LoadRequired("SceneView/HandleDottedLines.mat");
                s_HandleDottedWireMaterial2D = (Material)EditorGUIUtility.LoadRequired("SceneView/2DHandleDottedLines.mat");
                s_HandleDottedWireTextureIndex = ShaderUtil.GetTextureBindingIndex(s_HandleDottedWireMaterial.shader, Shader.PropertyToID("_MainTex"));
                s_HandleDottedWireTextureIndex2D = ShaderUtil.GetTextureBindingIndex(s_HandleDottedWireMaterial2D.shader, Shader.PropertyToID("_MainTex"));
                s_HandleDottedWireTextureSamplerIndex = ShaderUtil.GetTextureSamplerBindingIndex(s_HandleDottedWireMaterial.shader, Shader.PropertyToID("_MainTex"));
                s_HandleDottedWireTextureSamplerIndex2D = ShaderUtil.GetTextureSamplerBindingIndex(s_HandleDottedWireMaterial2D.shader, Shader.PropertyToID("_MainTex"));

                s_HandleArcMaterial = (Material)EditorGUIUtility.LoadRequired("SceneView/CircularArc.mat");
            }
        }

        // Material used to draw "lines" and other wireframe-like stuff.
        // Private for a reason; always use ApplyWireMaterial to set it up for rendering!
        // It needs to remember texture binding index so that DrawAA* functions can properly
        // set the anti-aliasing texture.
        static Material handleWireMaterial
        {
            get
            {
                InitHandleMaterials();
                return Camera.current ? s_HandleWireMaterial : s_HandleWireMaterial2D;
            }
        }

        // Material used to draw lines like above, only "dotted"
        static Material handleDottedWireMaterial
        {
            get
            {
                InitHandleMaterials();
                return Camera.current ? s_HandleDottedWireMaterial : s_HandleDottedWireMaterial2D;
            }
        }

        static internal Material handleArcMaterial
        {
            get
            {
                InitHandleMaterials();
                return s_HandleArcMaterial;
            }
        }

        static Material s_HandleWireMaterial;
        static Material s_HandleWireMaterial2D;
        static int s_HandleWireTextureIndex;
        static int s_HandleWireTextureSamplerIndex;
        static int s_HandleWireTextureIndex2D;
        static int s_HandleWireTextureSamplerIndex2D;

        static Material s_HandleDottedWireMaterial;
        static Material s_HandleDottedWireMaterial2D;
        static int s_HandleDottedWireTextureIndex;
        static int s_HandleDottedWireTextureSamplerIndex;
        static int s_HandleDottedWireTextureIndex2D;
        static int s_HandleDottedWireTextureSamplerIndex2D;

        static Material s_HandleArcMaterial;

        // Setup shader for later drawing of lines / anti-aliased lines.
        internal static void ApplyWireMaterial([DefaultValue("UnityEngine.Rendering.CompareFunction.Always")] CompareFunction zTest)
        {
            Material mat = handleWireMaterial;
            // Note: important to call this from C# side, so that it tracks "scripting channels"
            // for any later GL.Begin calls.
            mat.SetInt("_HandleZTest", (int)zTest);
            mat.SetPass(0);
            int textureIndex = Camera.current ? s_HandleWireTextureIndex : s_HandleWireTextureIndex2D;
            int samplerIndex = Camera.current ? s_HandleWireTextureSamplerIndex : s_HandleWireTextureSamplerIndex2D;
            Internal_SetHandleWireTextureIndex(textureIndex, samplerIndex);
        }

        [ExcludeFromDocs]
        internal static void ApplyWireMaterial()
        {
            CompareFunction zTest = CompareFunction.Always;
            ApplyWireMaterial(zTest);
        }

        internal static void ApplyDottedWireMaterial([DefaultValue("UnityEngine.Rendering.CompareFunction.Always")] CompareFunction zTest)
        {
            Material mat = handleDottedWireMaterial;
            // Note: important to call this from C# side, so that it tracks "scripting channels"
            // for any later GL.Begin calls.
            mat.SetInt("_HandleZTest", (int)zTest);
            mat.SetPass(0);
            int textureIndex = Camera.current ? s_HandleDottedWireTextureIndex : s_HandleDottedWireTextureIndex2D;
            int samplerIndex = Camera.current ? s_HandleDottedWireTextureSamplerIndex : s_HandleDottedWireTextureSamplerIndex2D;
            Internal_SetHandleWireTextureIndex(textureIndex, samplerIndex);
        }

        [ExcludeFromDocs]
        internal static void ApplyDottedWireMaterial()
        {
            CompareFunction zTest = CompareFunction.Always;
            ApplyDottedWireMaterial(zTest);
        }

        // Store all camera settings
        public static void PushCamera(Camera camera)
        {
            s_SavedCameras.Push(new SavedCamera(camera));
        }

        // Retrieve all camera settings
        public static void PopCamera(Camera camera)
        {
            SavedCamera cam = (SavedCamera)s_SavedCameras.Pop();
            cam.Restore(camera);
        }

        sealed class SavedCamera
        {
            float near, far;
            Rect pixelRect;
            Vector3 pos;
            Quaternion rot;
            CameraClearFlags clearFlags;
            int cullingMask;
            float fov;
            float orthographicSize;
            bool isOrtho;

            internal SavedCamera(Camera source)
            {
                near = source.nearClipPlane;
                far = source.farClipPlane;
                pixelRect = source.pixelRect;
                pos = source.transform.position;
                rot = source.transform.rotation;
                clearFlags = source.clearFlags;
                cullingMask = source.cullingMask;
                fov = source.fieldOfView;
                orthographicSize = source.orthographicSize;
                isOrtho = source.orthographic;
            }

            internal void Restore(Camera dest)
            {
                dest.nearClipPlane = near;
                dest.farClipPlane = far;
                dest.pixelRect = pixelRect;
                dest.transform.position = pos;
                dest.transform.rotation = rot;
                dest.clearFlags = clearFlags;
                dest.fieldOfView = fov;
                dest.orthographicSize = orthographicSize;
                dest.orthographic = isOrtho;
                dest.cullingMask = cullingMask;
            }
        }

        static Stack s_SavedCameras = new Stack();

        // Objects to ignore when raysnapping (typically the objects being dragged by the handles)
        internal static Transform[] ignoreRaySnapObjects = null;
        static RaycastHit[] s_RaySnapHits = new RaycastHit[100];

        // Casts /ray/ against the scene.
        public static object RaySnap(Ray ray)
        {
            Camera cam = Camera.current;
            ulong sceneCullingMask = cam.sceneCullingMask;
            int layerCullingMask = cam.cullingMask;

            bool hitAny = false;
            RaycastHit raycastHit = default(RaycastHit);
            raycastHit.distance = Mathf.Infinity;

            if (sceneCullingMask == SceneCullingMasks.MainStageSceneViewObjects)
            {
                // Default code path for Scene view that is just displaying the Main Stage.
                // Note that even if Prefab Mode is open, special Scene views can still show the Main Stage!
                // We only check against default physics scene here, and shouldn't ignore Prefab instances
                // that are opened in Prefab Mode in Context.
                hitAny |= GetNearestHitFromPhysicsScene(ray, Physics.defaultPhysicsScene, layerCullingMask, false, ref raycastHit);
            }
            else
            {
                // Code path is Scene view is displaying a Prefab Stage.
                // Here we dig down from the top of the stage history stack and continue
                // including each stage as long as they are displayed as context. Prefab instances
                // that are hidden due to being opened in Prefab Mode in Context should be ignored.
                var stageHistory = StageNavigationManager.instance.stageHistory;
                for (int i = stageHistory.Count - 1; i >= 0; i--)
                {
                    Stage stage = stageHistory[i];
                    var previewSceneStage = stage as PreviewSceneStage;
                    PhysicsScene physics = previewSceneStage != null ? previewSceneStage.scene.GetPhysicsScene() : Physics.defaultPhysicsScene;
                    hitAny |= GetNearestHitFromPhysicsScene(ray, physics, layerCullingMask, true, ref raycastHit);
                    var prefabStage = previewSceneStage as PrefabStage;
                    if (prefabStage == null ||
                        prefabStage.mode == PrefabStage.Mode.InIsolation ||
                        StageNavigationManager.instance.contextRenderMode == StageUtility.ContextRenderMode.Hidden)
                        break;
                }
            }

            if (hitAny)
                return raycastHit;
            return null;
        }

        static bool GetNearestHitFromPhysicsScene(Ray ray, PhysicsScene physicsScene, int cullingMask, bool ignorePrefabInstance, ref RaycastHit raycastHit)
        {
            float maxDist = raycastHit.distance;
            int numHits = physicsScene.Raycast(ray.origin, ray.direction, s_RaySnapHits, maxDist, cullingMask, QueryTriggerInteraction.Ignore);

            // We are not sure at this point if the hits returned from RaycastAll are sorted or not, so go through them all
            float nearestHitDist = maxDist;
            int nearestHitIndex = -1;
            if (ignoreRaySnapObjects != null)
            {
                for (int i = 0; i < numHits; i++)
                {
                    if (s_RaySnapHits[i].distance < nearestHitDist)
                    {
                        Transform tr = s_RaySnapHits[i].transform;
                        if (ignorePrefabInstance && GameObjectUtility.IsPrefabInstanceHiddenForInContextEditing(tr.gameObject))
                            continue;

                        bool ignore = false;
                        for (int j = 0; j < ignoreRaySnapObjects.Length; j++)
                        {
                            if (tr == ignoreRaySnapObjects[j])
                            {
                                ignore = true;
                                break;
                            }
                        }
                        if (ignore)
                            continue;

                        nearestHitDist = s_RaySnapHits[i].distance;
                        nearestHitIndex = i;
                    }
                }
            }
            else
            {
                for (int i = 0; i < numHits; i++)
                {
                    if (s_RaySnapHits[i].distance < nearestHitDist)
                    {
                        nearestHitDist = s_RaySnapHits[i].distance;
                        nearestHitIndex = i;
                    }
                }
            }

            if (nearestHitIndex >= 0)
            {
                raycastHit = s_RaySnapHits[nearestHitIndex];
                return true;
            }
            else
            {
                return false;
            }
        }

        public delegate bool PlaceObjectDelegate(Vector2 guiPosition, out Vector3 position, out Vector3 normal);
        public static event PlaceObjectDelegate placeObjectCustomPasses;

        public static bool PlaceObject(Vector2 guiPosition, out Vector3 position, out Vector3 normal)
        {
            Ray ray = GUIPointToWorldRay(guiPosition);
            object hit = RaySnap(ray);
            bool objectIntersected = hit != null;
            float bestDistance = objectIntersected ? ((RaycastHit)hit).distance : Mathf.Infinity;
            position = objectIntersected ? ray.GetPoint(((RaycastHit)hit).distance) : Vector3.zero;
            normal = objectIntersected ? ((RaycastHit)hit).normal : Vector3.up;

            if (placeObjectCustomPasses != null)
            {
                foreach (var del in placeObjectCustomPasses.GetInvocationList())
                {
                    Vector3 pos, nrm;

                    if (((PlaceObjectDelegate)del)(guiPosition, out pos, out nrm))
                    {
                        var dst = Vector3.Distance(ray.origin, pos);
                        if (dst < bestDistance)
                        {
                            objectIntersected = true;
                            bestDistance = dst;
                            position = pos;
                            normal = nrm;
                        }
                    }
                }
            }

            return objectIntersected;
        }

        // Repaint the current view
        public static void Repaint()
        {
            Internal_Repaint();
        }
    }
}
