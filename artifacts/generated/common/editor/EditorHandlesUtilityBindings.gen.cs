// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;


using UnityEngine;
using System.Runtime.CompilerServices;
using System.Collections;


namespace UnityEditor
{


public sealed partial class HandleUtility
{
    static public float CalcLineTranslation(Vector2 src, Vector2 dest, Vector3 srcPosition, Vector3 constraintDir)
        {
            srcPosition = Handles.matrix.MultiplyPoint(srcPosition);
            constraintDir = Handles.matrix.MultiplyVector(constraintDir);


            float invert = 1.0F;
            Vector3 cameraForward = Camera.current.transform.forward;
            if (Vector3.Dot(constraintDir, cameraForward) < 0.0F)
                invert = -1.0F;

            Vector3 cd = constraintDir;
            cd.y = -cd.y;
            Camera cam = Camera.current;
            Vector2 p1 = EditorGUIUtility.PixelsToPoints(cam.WorldToScreenPoint(srcPosition));
            Vector2 p2 = EditorGUIUtility.PixelsToPoints(cam.WorldToScreenPoint(srcPosition + constraintDir * invert));
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
    
    
    public static float PointOnLineParameter(Vector3 point, Vector3 linePoint, Vector3 lineDirection)
        {
            return (Vector3.Dot(lineDirection , (point - linePoint))) / lineDirection.sqrMagnitude;
        }
    
    
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
    
    
    public static float DistancePointLine(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
        {
            return Vector3.Magnitude(ProjectPointLine(point, lineStart, lineEnd) - point);
        }
    
    
    static public float acceleration { get { return (Event.current.shift ? 4 : 1) * (Event.current.alt ? .25f : 1); } }
    
    
    static public float niceMouseDelta
        {
            get
            {
                Vector2 d = Event.current.delta;
                d.y = -d.y;

                if (Mathf.Abs(Mathf.Abs(d.x) - Mathf.Abs(d.y)) / Mathf.Max(Mathf.Abs(d.x), Mathf.Abs(d.y)) > .1f)
                {
                    if (Mathf.Abs(d.x) > Mathf.Abs(d.y))
                        s_UseYSign = false;
                    else
                        s_UseYSign = true;
                }

                if (s_UseYSign)
                    return Mathf.Sign(d.y) * d.magnitude * acceleration;
                else
                    return Mathf.Sign(d.x) * d.magnitude * acceleration;
            }
        }
            static bool s_UseYSign = false;
    
    
    static public float niceMouseDeltaZoom
        {
            get
            {
                Vector2 d = -Event.current.delta;

                if (Mathf.Abs(Mathf.Abs(d.x) - Mathf.Abs(d.y)) / Mathf.Max(Mathf.Abs(d.x), Mathf.Abs(d.y)) > .1f)
                {
                    if (Mathf.Abs(d.x) > Mathf.Abs(d.y))
                        s_UseYSignZoom = false;
                    else
                        s_UseYSignZoom = true;
                }

                if (s_UseYSignZoom)
                    return Mathf.Sign(d.y) * d.magnitude * acceleration;
                else
                    return Mathf.Sign(d.x) * d.magnitude * acceleration;
            }
        }
            static bool s_UseYSignZoom = false;
    
    
    
    public static float DistancePointBezier (Vector3 point, Vector3 startPosition, Vector3 endPosition, Vector3 startTangent, Vector3 endTangent) {
        return INTERNAL_CALL_DistancePointBezier ( ref point, ref startPosition, ref endPosition, ref startTangent, ref endTangent );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static float INTERNAL_CALL_DistancePointBezier (ref Vector3 point, ref Vector3 startPosition, ref Vector3 endPosition, ref Vector3 startTangent, ref Vector3 endTangent);
    public static float DistanceToLine(Vector3 p1, Vector3 p2)
        {
            p1 = WorldToGUIPoint(p1);
            p2 = WorldToGUIPoint(p2);

            Vector2 point = Event.current.mousePosition;

            float retval = DistancePointLine(point, p1, p2);
            if (retval < 0) retval = 0.0f;
            return retval;
        }
    
    
    public static float DistanceToCircle(Vector3 position, float radius)
        {
            Vector2 screenCenter = WorldToGUIPoint(position);
            Camera cam = Camera.current;
            Vector2 screenEdge = Vector2.zero;
            if (cam)
            {
                screenEdge = WorldToGUIPoint(position + cam.transform.right * radius);
                radius = (screenCenter - screenEdge).magnitude;
            }
            float dist = (screenCenter - Event.current.mousePosition).magnitude;
            if (dist < radius)
                return 0;
            return dist - radius;
        }
    
            static Vector3[] points = {Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero};
    public static float DistanceToRectangle(Vector3 position, Quaternion rotation, float size)
        {
            return DistanceToRectangleInternal(position, rotation, new Vector2(size, size));
        }
    
    
    internal static float DistanceToRectangleInternal(Vector3 position, Quaternion rotation, Vector2 size)
        {
            Vector3 sideways = rotation * new Vector3(size.x, 0, 0);
            Vector3 up = rotation * new Vector3(0, size.y, 0);
            points[0] = WorldToGUIPoint(position + sideways + up);
            points[1] = WorldToGUIPoint(position + sideways - up);
            points[2] = WorldToGUIPoint(position - sideways - up);
            points[3] = WorldToGUIPoint(position - sideways + up);
            points[4] = points[0];

            Vector2 pos = Event.current.mousePosition;
            bool oddNodes = false;
            int j = 4;
            for (int i = 0; i < 5; i++)
            {
                if ((points[i].y > pos.y) != (points[j].y > pos.y))
                {
                    if (pos.x < (points[j].x - points[i].x) * (pos.y - points[i].y) / (points[j].y - points[i].y) + points[i].x)
                    {
                        oddNodes = !oddNodes;
                    }
                }
                j = i;
            }
            if (!oddNodes)
            {
                float dist, closestDist = -1f;
                j = 1;
                for (int i = 0; i < 4; i++)
                {
                    dist = DistancePointToLineSegment(pos, points[i], points[j++]);
                    if (dist < closestDist || closestDist < 0)
                        closestDist = dist;
                }
                return closestDist;
            }
            else
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
            points[0] = WorldToGUIPoint(position + sideways);
            points[1] = WorldToGUIPoint(position - up);
            points[2] = WorldToGUIPoint(position - sideways);
            points[3] = WorldToGUIPoint(position + up);
            points[4] = points[0];

            Vector2 pos = mousePosition;
            bool oddNodes = false;
            int j = 4;
            for (int i = 0; i < 5; i++)
            {
                if ((points[i].y > pos.y) != (points[j].y > pos.y))
                {
                    if (pos.x < (points[j].x - points[i].x) * (pos.y - points[i].y) / (points[j].y - points[i].y) + points[i].x)
                    {
                        oddNodes = !oddNodes;
                    }
                }
                j = i;
            }
            if (!oddNodes)
            {
                float dist, closestDist = -1f;
                j = 1;
                for (int i = 0; i < 4; i++)
                {
                    dist = DistancePointToLineSegment(pos, points[i], points[j++]);
                    if (dist < closestDist || closestDist < 0)
                        closestDist = dist;
                }
                return closestDist;
            }
            else
                return 0;
        }
    
    
    public static float DistancePointToLine(Vector2 p, Vector2 a, Vector2 b)
        {
            return Mathf.Abs((b.x - a.x) * (a.y - p.y) - (a.x - p.x) * (b.y - a.y)) / (b - a).magnitude;
        }
    
    
    public static float DistancePointToLineSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            float l2 = (b - a).sqrMagnitude;    
            if (l2 == 0.0)
                return (p - a).magnitude;       
            float t = Vector2.Dot(p - a, b - a) / l2;
            if (t < 0.0)
                return (p - a).magnitude;       
            else if (t > 1.0)
                return (p - b).magnitude;         
            Vector2 projection = a + t * (b - a); 
            return (p - projection).magnitude;
        }
    
    
    public static float DistanceToDisc(Vector3 center, Vector3 normal, float radius)
        {
            Vector3 tangent = Vector3.Cross(normal, Vector3.up);
            if (tangent.sqrMagnitude < .001f)
                tangent = Vector3.Cross(normal, Vector3.right);
            return DistanceToArc(center, normal, tangent, 360, radius);
        }
    
    
    public static Vector3 ClosestPointToDisc(Vector3 center, Vector3 normal, float radius)
        {
            Vector3 tangent = Vector3.Cross(normal, Vector3.up);
            if (tangent.sqrMagnitude < .001f)
                tangent = Vector3.Cross(normal, Vector3.right);
            return ClosestPointToArc(center, normal, tangent, 360, radius);
        }
    
    
    public static float DistanceToArc(Vector3 center, Vector3 normal, Vector3 from, float angle, float radius)
        {
            Vector3[] points = new Vector3[60];
            Handles.SetDiscSectionPoints(points, center, normal, from, angle, radius);
            return DistanceToPolyLine(points);
        }
    
    
    public static Vector3 ClosestPointToArc(Vector3 center, Vector3 normal, Vector3 from, float angle, float radius)
        {
            Vector3[] points = new Vector3[60];
            Handles.SetDiscSectionPoints(points, center, normal, from, angle, radius);
            return ClosestPointToPolyLine(points);
        }
    
    
    public static float DistanceToPolyLine(params Vector3[] points)
        {
            float dist = HandleUtility.DistanceToLine(points[0], points[1]);
            for (int i = 2; i < points.Length; i++)
            {
                float d = HandleUtility.DistanceToLine(points[i - 1], points[i]);
                if (d < dist)
                    dist = d;
            }
            return dist;
        }
    
    
    public static Vector3 ClosestPointToPolyLine(params Vector3[] vertices)
        {
            float dist = HandleUtility.DistanceToLine(vertices[0], vertices[1]);
            int nearest = 0;
            for (int i = 2; i < vertices.Length; i++)
            {
                float d = HandleUtility.DistanceToLine(vertices[i - 1], vertices[i]);
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
    static internal void BeginHandles()
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
            Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
            s_CustomPickDistance = kPickDistance;
            Handles.Internal_SetCurrentCamera(null);
            EditorGUI.s_DelayedTextEditor.BeginGUI();
        }
    
    
    [RequiredByNativeCode]
    static private void SetViewInfo(Vector2 screenPosition)
        {
            GUIUtility.s_EditorScreenPointOffset = screenPosition;
        }
    
    
    [RequiredByNativeCode]
    static internal void EndHandles()
        {
            if (s_PreviousNearestControl != s_NearestControl
                && s_NearestControl != 0)
            {
                s_PreviousNearestControl = s_NearestControl;
                Repaint();
            }
            EditorGUI.s_DelayedTextEditor.EndGUI(Event.current.type);
        }
    
            const float kHandleSize = 80.0f;
    
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
                return (kHandleSize / Mathf.Max(screenDist, 0.0001f)) * EditorGUIUtility.pixelsPerPoint;
            }
            else
            {
                return 20.0f;
            }

        }
    
    
    public static Vector2 WorldToGUIPoint(Vector3 world)
        {
            world = Handles.matrix.MultiplyPoint(world);
            Camera cam = Camera.current;
            if (cam)
            {
                Vector2 pos = cam.WorldToScreenPoint(world);
                pos.y = Screen.height - pos.y;
                pos = EditorGUIUtility.PixelsToPoints(pos);
                return GUIClip.Clip(pos);
            }
            else
            {
                return new Vector2(world.x, world.y);
            }
        }
    
    
    public static Vector2 GUIPointToScreenPixelCoordinate(Vector2 guiPoint)
        {
            var unclippedPosition = GUIClip.Unclip(guiPoint);
            var screenPixelPos = EditorGUIUtility.PointsToPixels(unclippedPosition);
            screenPixelPos.y = Screen.height - screenPixelPos.y;
            return screenPixelPos;
        }
    
    
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
    
    public static Rect WorldPointToSizedRect(Vector3 position, GUIContent content, GUIStyle style)
        {
            Vector2 screenpos = HandleUtility.WorldToGUIPoint(position);
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
    
    
    public static GameObject[] PickRectObjects(Rect rect)
        {
            return PickRectObjects(rect, true);
        }
    
    
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
    
    
    internal static GameObject[] Internal_PickRectObjects (Camera cam, Rect rect, bool selectPrefabRoots) {
        return INTERNAL_CALL_Internal_PickRectObjects ( cam, ref rect, selectPrefabRoots );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static GameObject[] INTERNAL_CALL_Internal_PickRectObjects (Camera cam, ref Rect rect, bool selectPrefabRoots);
    internal static bool FindNearestVertex(Vector2 guiPoint, Transform[] objectsToSearch, out Vector3 vertex)
        {
            Camera cam = Camera.current;
            var screenPoint = EditorGUIUtility.PointsToPixels(guiPoint);
            screenPoint.y = cam.pixelRect.yMax - screenPoint.y;
            return Internal_FindNearestVertex(cam, screenPoint, objectsToSearch, ignoreRaySnapObjects, out vertex);
        }
    
    
    private static bool Internal_FindNearestVertex (Camera cam, Vector2 screenPoint, Transform[] objectsToSearch, Transform[] ignoreObjects, out Vector3 vertex) {
        return INTERNAL_CALL_Internal_FindNearestVertex ( cam, ref screenPoint, objectsToSearch, ignoreObjects, out vertex );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_Internal_FindNearestVertex (Camera cam, ref Vector2 screenPoint, Transform[] objectsToSearch, Transform[] ignoreObjects, out Vector3 vertex);
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

            materialIndex = -1; 

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
                else
                    return pickedRoot;
            }
            return picked;
        }
    
    
    internal static GameObject FindSelectionBase(GameObject go)
        {
            if (go == null)
                return null;

            Transform prefabBase = null;
            PrefabType pickedType = PrefabUtility.GetPrefabType(go);
            if (pickedType == PrefabType.PrefabInstance || pickedType == PrefabType.ModelPrefabInstance)
            {
                prefabBase = PrefabUtility.FindPrefabRoot(go).transform;
            }

            Transform tr = go.transform;
            while (tr != null)
            {
                if (tr == prefabBase)
                    return tr.gameObject;

                if (AttributeHelper.GameObjectContainsAttribute(tr.gameObject, typeof(SelectionBaseAttribute)))
                    return tr.gameObject;

                tr = tr.parent;
            }

            return null;
        }
    
    
    internal static GameObject Internal_PickClosestGO (Camera cam, int layers, Vector2 position, GameObject[] ignore, GameObject[] filter, out int materialIndex) {
        return INTERNAL_CALL_Internal_PickClosestGO ( cam, layers, ref position, ignore, filter, out materialIndex );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static GameObject INTERNAL_CALL_Internal_PickClosestGO (Camera cam, int layers, ref Vector2 position, GameObject[] ignore, GameObject[] filter, out int materialIndex);
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
    
    
    private static void InitHandleMaterials()
        {
            if (!s_HandleWireMaterial)
            {
                s_HandleWireMaterial = (Material)EditorGUIUtility.LoadRequired("SceneView/HandleLines.mat");
                s_HandleWireMaterial2D = (Material)EditorGUIUtility.LoadRequired("SceneView/2DHandleLines.mat");
                s_HandleWireTextureIndex = ShaderUtil.GetTextureBindingIndex(s_HandleWireMaterial.shader, Shader.PropertyToID("_MainTex"));
                s_HandleWireTextureIndex2D = ShaderUtil.GetTextureBindingIndex(s_HandleWireMaterial2D.shader, Shader.PropertyToID("_MainTex"));

                s_HandleDottedWireMaterial = (Material)EditorGUIUtility.LoadRequired("SceneView/HandleDottedLines.mat");
                s_HandleDottedWireMaterial2D = (Material)EditorGUIUtility.LoadRequired("SceneView/2DHandleDottedLines.mat");
                s_HandleDottedWireTextureIndex = ShaderUtil.GetTextureBindingIndex(s_HandleDottedWireMaterial.shader, Shader.PropertyToID("_MainTex"));
                s_HandleDottedWireTextureIndex2D = ShaderUtil.GetTextureBindingIndex(s_HandleDottedWireMaterial2D.shader, Shader.PropertyToID("_MainTex"));
            }
        }
    
    
    private static Material handleWireMaterial
        {
            get
            {
                InitHandleMaterials();
                return Camera.current ? s_HandleWireMaterial : s_HandleWireMaterial2D;
            }
        }
    
    
    private static Material handleDottedWireMaterial
        {
            get
            {
                InitHandleMaterials();
                return Camera.current ? s_HandleDottedWireMaterial : s_HandleDottedWireMaterial2D;
            }
        }
    
            static private Material s_HandleWireMaterial, s_HandleWireMaterial2D;
            static private int s_HandleWireTextureIndex, s_HandleWireTextureIndex2D;
    
            static private Material s_HandleDottedWireMaterial, s_HandleDottedWireMaterial2D;
            static private int s_HandleDottedWireTextureIndex, s_HandleDottedWireTextureIndex2D;
    
    
    [uei.ExcludeFromDocs]
internal static void ApplyWireMaterial () {
    UnityEngine.Rendering.CompareFunction zTest = UnityEngine.Rendering.CompareFunction.Always;
    ApplyWireMaterial ( zTest );
}

internal static void ApplyWireMaterial( [uei.DefaultValue("UnityEngine.Rendering.CompareFunction.Always")] UnityEngine.Rendering.CompareFunction zTest )
        {
            Material mat = handleWireMaterial;
            mat.SetInt("_HandleZTest", (int)zTest);
            mat.SetPass(0);
            int textureIndex = Camera.current ? s_HandleWireTextureIndex : s_HandleWireTextureIndex2D;
            Internal_SetHandleWireTextureIndex(textureIndex);
        }

    
    
    [uei.ExcludeFromDocs]
internal static void ApplyDottedWireMaterial () {
    UnityEngine.Rendering.CompareFunction zTest = UnityEngine.Rendering.CompareFunction.Always;
    ApplyDottedWireMaterial ( zTest );
}

internal static void ApplyDottedWireMaterial( [uei.DefaultValue("UnityEngine.Rendering.CompareFunction.Always")] UnityEngine.Rendering.CompareFunction zTest )
        {
            Material mat = handleDottedWireMaterial;
            mat.SetInt("_HandleZTest", (int)zTest);
            mat.SetPass(0);
            int textureIndex = Camera.current ? s_HandleDottedWireTextureIndex : s_HandleDottedWireTextureIndex2D;
            Internal_SetHandleWireTextureIndex(textureIndex);
        }

    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void Internal_SetHandleWireTextureIndex (int textureIndex) ;

    public static void PushCamera(Camera camera)
        {
            s_SavedCameras.Push(new SavedCamera(camera));
        }
    
    
    public static void PopCamera(Camera camera)
        {
            SavedCamera cam = (SavedCamera)s_SavedCameras.Pop();
            cam.Restore(camera);
        }
    
    
    private sealed partial class SavedCamera    
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

            static private Stack s_SavedCameras = new Stack();
    
    
    internal static Transform[] ignoreRaySnapObjects = null;
    
    
    
    public static object RaySnap(Ray ray)
        {
            RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity, Camera.current.cullingMask);

            float nearestHitDist = Mathf.Infinity;
            int nearestHitIndex = -1;
            if (ignoreRaySnapObjects != null)
            {
                for (int i = 0; i < hits.Length; i++)
                {
                    if (!hits[i].collider.isTrigger && hits[i].distance < nearestHitDist)
                    {
                        bool ignore = false;
                        for (int j = 0; j < ignoreRaySnapObjects.Length; j++)
                        {
                            if (hits[i].transform == ignoreRaySnapObjects[j])
                            {
                                ignore = true;
                                break;
                            }
                        }
                        if (!ignore)
                        {
                            nearestHitDist = hits[i].distance;
                            nearestHitIndex = i;
                        }
                    }
                }
            }
            else
            {
                for (int i = 0; i < hits.Length; i++)
                {
                    if (hits[i].distance < nearestHitDist)
                    {
                        nearestHitDist = hits[i].distance;
                        nearestHitIndex = i;
                    }
                }
            }

            if (nearestHitIndex >= 0)
                return hits[nearestHitIndex];
            return null;
        }
    
    
    internal static float CalcRayPlaceOffset (Transform[] objects, Vector3 normal) {
        return INTERNAL_CALL_CalcRayPlaceOffset ( objects, ref normal );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static float INTERNAL_CALL_CalcRayPlaceOffset (Transform[] objects, ref Vector3 normal);
    public static void Repaint()
        {
            Internal_Repaint();
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void Internal_Repaint () ;

    internal static bool IntersectRayMesh (Ray ray, Mesh mesh, Matrix4x4 matrix, out RaycastHit hit) {
        return INTERNAL_CALL_IntersectRayMesh ( ref ray, mesh, ref matrix, out hit );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_IntersectRayMesh (ref Ray ray, Mesh mesh, ref Matrix4x4 matrix, out RaycastHit hit);
}

}
