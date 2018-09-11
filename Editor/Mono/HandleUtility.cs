// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Internal;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;

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

        // Pixel distance from mouse pointer to a rectangle on screen
        static Vector3[] s_Points = {Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero};
        public static float DistanceToRectangle(Vector3 position, Quaternion rotation, float size)
        {
            return DistanceToRectangleInternal(position, rotation, new Vector2(size, size));
        }

        // Pixel distance from mouse pointer to a rectangle on screen
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

        // Pixel distance from mouse pointer to a 3D section of a disc.
        public static float DistanceToArc(Vector3 center, Vector3 normal, Vector3 from, float angle, float radius)
        {
            Vector3[] points = new Vector3[60];
            Handles.SetDiscSectionPoints(points, center, normal, from, angle, radius);
            return DistanceToPolyLine(points);
        }

        // Get the nearest 3D point.
        public static Vector3 ClosestPointToArc(Vector3 center, Vector3 normal, Vector3 from, float angle, float radius)
        {
            Vector3[] points = new Vector3[60];
            Handles.SetDiscSectionPoints(points, center, normal, from, angle, radius);
            return ClosestPointToPolyLine(points);
        }

        // Pixel distance from mouse pointer to a polyline.
        public static float DistanceToPolyLine(params Vector3[] points)
        {
            float dist = DistanceToLine(points[0], points[1]);
            for (int i = 2; i < points.Length; i++)
            {
                float d = DistanceToLine(points[i - 1], points[i]);
                if (d < dist)
                    dist = d;
            }
            return dist;
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
            world = Handles.matrix.MultiplyPoint(world);
            Camera cam = Camera.current;
            if (cam)
            {
                Vector3 pos = cam.WorldToScreenPoint(world);
                pos.y = Screen.height - pos.y;
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
            if (!Camera.current)
            {
                Debug.LogError("Unable to convert GUI point to world ray if a camera has not been set up!");
                return new Ray(Vector3.zero, Vector3.forward);
            }
            Vector2 screenPixelPos = GUIPointToScreenPixelCoordinate(position);
            Camera camera = Camera.current;
            return camera.ScreenPointToRay(screenPixelPos);
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

        internal delegate GameObject PickClosestGameObjectFunc(Camera cam, int layers, Vector2 position, GameObject[] ignore, GameObject[] filter, out int materialIndex);
        internal static PickClosestGameObjectFunc pickClosestGameObjectDelegate;

        public static GameObject PickGameObject(Vector2 position, out int materialIndex)
        {
            return PickGameObjectDelegated(position, null, null, out materialIndex);
        }

        public static GameObject PickGameObject(Vector2 position, GameObject[] ignore, out int materialIndex)
        {
            return PickGameObjectDelegated(position, ignore, null, out materialIndex);
        }

        internal static GameObject PickGameObjectDelegated(Vector2 position, GameObject[] ignore, GameObject[] filter, out int materialIndex)
        {
            Camera cam = Camera.current;
            int layers = cam.cullingMask;
            position = GUIClip.Unclip(position);
            position = EditorGUIUtility.PointsToPixels(position);
            position.y = Screen.height - position.y - cam.pixelRect.yMin;

            materialIndex = -1; // default

            GameObject picked = null;
            if (pickClosestGameObjectDelegate != null)
                picked = pickClosestGameObjectDelegate(cam, layers, position, ignore, filter, out materialIndex);

            if (picked == null)
                picked = Internal_PickClosestGO(cam, layers, position, ignore, filter, out materialIndex);

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
                GameObject pickedRoot = FindSelectionBase(picked) ?? picked;
                Transform atc = Selection.activeTransform;
                GameObject selectionRoot = atc ? (FindSelectionBase(atc.gameObject) ?? atc.gameObject) : null;
                if (pickedRoot == selectionRoot)
                    return picked;
                return pickedRoot;
            }
            return picked;
        }

        internal static GameObject FindSelectionBase(GameObject go)
        {
            if (go == null)
                return null;

            // Find prefab based base
            Transform prefabBase = null;
            if (PrefabUtility.IsPartOfNonAssetPrefabInstance(go))
            {
                prefabBase = PrefabUtility.GetOutermostPrefabInstanceRoot(go).transform;
            }

            // Find attribute based base
            Transform tr = go.transform;
            while (tr != null)
            {
                // If we come across the prefab base, no need to search further down.
                if (tr == prefabBase)
                    return tr.gameObject;

                // If this one has the attribute, return this one.
                if (AttributeHelper.GameObjectContainsAttribute<SelectionBaseAttribute>(tr.gameObject))
                    return tr.gameObject;

                tr = tr.parent;
            }

            // There is neither a prefab or attribute based selection root, so return null
            return null;
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
            // This is enough for all of them to get re-fetched in next call to InitHandleMaterials()
            s_HandleWireMaterial = null;
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
            PhysicsScene physicsScene = Physics.defaultPhysicsScene;
            Scene customScene = Camera.current.scene;

            if (customScene.IsValid())
            {
                physicsScene = customScene.GetPhysicsScene();
            }

            int numHits = physicsScene.Raycast(ray.origin, ray.direction, s_RaySnapHits, Mathf.Infinity, Camera.current.cullingMask, QueryTriggerInteraction.Ignore);

            // We are not sure at this point if the hits returned from RaycastAll are sorted or not, so go through them all
            float nearestHitDist = Mathf.Infinity;
            int nearestHitIndex = -1;
            if (ignoreRaySnapObjects != null)
            {
                for (int i = 0; i < numHits; i++)
                {
                    if (s_RaySnapHits[i].distance < nearestHitDist)
                    {
                        bool ignore = false;
                        for (int j = 0; j < ignoreRaySnapObjects.Length; j++)
                        {
                            if (s_RaySnapHits[i].transform == ignoreRaySnapObjects[j])
                            {
                                ignore = true;
                                break;
                            }
                        }
                        if (!ignore)
                        {
                            nearestHitDist = s_RaySnapHits[i].distance;
                            nearestHitIndex = i;
                        }
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
                return s_RaySnapHits[nearestHitIndex];
            return null;
        }

        // Repaint the current view
        public static void Repaint()
        {
            Internal_Repaint();
        }
    }
}
