// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Internal;

namespace UnityEditor
{
    // Grid drawing params for Handles.DrawCamera.
    [StructLayout(LayoutKind.Sequential)]
    struct DrawGridParameters
    {
        public int          gridID;
        public Vector3      pivot;
        public Color        color;
        public Vector2      size;
    }

    public sealed partial class Handles
    {
        // Color of the X axis handle
        internal static PrefColor s_XAxisColor = new PrefColor("Scene/X Axis", 219f / 255, 62f / 255, 29f / 255, .93f);
        public static Color xAxisColor { get { return s_XAxisColor; } }
        // Color of the Y axis handle
        internal static PrefColor s_YAxisColor = new PrefColor("Scene/Y Axis", 154f / 255, 243f / 255, 72f / 255, .93f);
        public static Color yAxisColor { get { return s_YAxisColor; } }
        // Color of the Z axis handle
        internal static PrefColor s_ZAxisColor = new PrefColor("Scene/Z Axis", 58f / 255, 122f / 255, 248f / 255, .93f);
        public static Color zAxisColor { get { return s_ZAxisColor; } }
        // Color of the center handle
        internal static PrefColor s_CenterColor = new PrefColor("Scene/Center Axis", .8f, .8f, .8f, .93f);
        public static Color centerColor { get { return s_CenterColor; } }
        // color for handles the currently active handle
        internal static PrefColor s_SelectedColor = new PrefColor("Scene/Selected Axis", 246f / 255, 242f / 255, 50f / 255, .89f);
        public static Color selectedColor { get { return s_SelectedColor; } }
        // color for handles the currently hovered handle
        internal static PrefColor s_PreselectionColor = new PrefColor("Scene/Preselection Highlight", 201f / 255, 200f / 255, 144f / 255, 0.89f);
        public static Color preselectionColor { get { return s_PreselectionColor; } }
        // soft color for general stuff - used to draw e.g. the arc selection while dragging
        internal static PrefColor s_SecondaryColor = new PrefColor("Scene/Guide Line", .5f, .5f, .5f, .2f);
        public static Color secondaryColor { get { return s_SecondaryColor; } }
        // internal color for static handles
        internal static Color staticColor = new Color(.5f, .5f, .5f, 0f);
        // internal blend ratio for static colors
        internal static float staticBlend = 0.6f;

        internal static float backfaceAlphaMultiplier = 0.2f;
        internal static Color s_ColliderHandleColor = new Color(145f, 244f, 139f, 210f) / 255;
        internal static Color s_ColliderHandleColorDisabled = new Color(84, 200f, 77f, 140f) / 255;
        internal static Color s_BoundingBoxHandleColor = new Color(255, 255, 255, 150) / 255;

        internal readonly static GUIContent s_StaticLabel = EditorGUIUtility.TrTextContent("Static");
        internal readonly static GUIContent s_PrefabLabel = EditorGUIUtility.TrTextContent("Prefab");

        internal static int s_SliderHash = "SliderHash".GetHashCode();
        internal static int s_Slider2DHash = "Slider2DHash".GetHashCode();
        internal static int s_FreeRotateHandleHash = "FreeRotateHandleHash".GetHashCode();
        internal static int s_RadiusHandleHash = "RadiusHandleHash".GetHashCode();
        internal static int s_xAxisMoveHandleHash  = "xAxisFreeMoveHandleHash".GetHashCode();
        internal static int s_yAxisMoveHandleHash  = "yAxisFreeMoveHandleHash".GetHashCode();
        internal static int s_zAxisMoveHandleHash  = "zAxisFreeMoveHandleHash".GetHashCode();
        internal static int s_FreeMoveHandleHash  = "FreeMoveHandleHash".GetHashCode();
        internal static int s_xzAxisMoveHandleHash = "xzAxisFreeMoveHandleHash".GetHashCode();
        internal static int s_xyAxisMoveHandleHash = "xyAxisFreeMoveHandleHash".GetHashCode();
        internal static int s_yzAxisMoveHandleHash = "yzAxisFreeMoveHandleHash".GetHashCode();
        internal static int s_xAxisScaleHandleHash = "xAxisScaleHandleHash".GetHashCode();
        internal static int s_yAxisScaleHandleHash = "yAxisScaleHandleHash".GetHashCode();
        internal static int s_zAxisScaleHandleHash = "zAxisScaleHandleHash".GetHashCode();
        internal static int s_ScaleSliderHash = "ScaleSliderHash".GetHashCode();
        internal static int s_ScaleValueHandleHash = "ScaleValueHandleHash".GetHashCode();
        internal static int s_DiscHash = "DiscHash".GetHashCode();
        internal static int s_ButtonHash = "ButtonHash".GetHashCode();

        static readonly int kPropUseGuiClip = Shader.PropertyToID("_UseGUIClip");
        static readonly int kPropHandleZTest = Shader.PropertyToID("_HandleZTest");
        static readonly int kPropColor = Shader.PropertyToID("_Color");
        static readonly int kPropArcCenterRadius = Shader.PropertyToID("_ArcCenterRadius");
        static readonly int kPropArcNormalAngle = Shader.PropertyToID("_ArcNormalAngle");
        static readonly int kPropArcFromCount = Shader.PropertyToID("_ArcFromCount");
        static readonly int kPropArcThicknessSides = Shader.PropertyToID("_ArcThicknessSides");
        static readonly int kPropHandlesMatrix = Shader.PropertyToID("_HandlesMatrix");

        public struct DrawingScope : IDisposable
        {
            private bool m_Disposed;

            public Color originalColor { get { return m_OriginalColor; } }
            private Color m_OriginalColor;

            public Matrix4x4 originalMatrix { get { return m_OriginalMatrix; } }
            private Matrix4x4 m_OriginalMatrix;

            public DrawingScope(Color color) : this(color, Handles.matrix) {}

            public DrawingScope(Matrix4x4 matrix) : this(Handles.color, matrix) {}

            public DrawingScope(Color color, Matrix4x4 matrix)
            {
                m_Disposed = false;
                m_OriginalColor = Handles.color;
                m_OriginalMatrix = Handles.matrix;
                Handles.matrix = matrix;
                Handles.color = color;
            }

            public void Dispose()
            {
                if (m_Disposed)
                    return;
                m_Disposed = true;
                Handles.color = m_OriginalColor;
                Handles.matrix = m_OriginalMatrix;
            }
        }

        internal static Mesh cubeMesh
        {
            get
            {
                if (s_CubeMesh == null)
                    Init();
                return s_CubeMesh;
            }
        }

        internal static Mesh coneMesh
        {
            get
            {
                if (s_ConeMesh == null)
                    Init();
                return s_ConeMesh;
            }
        }

        internal static Mesh cylinderMesh
        {
            get
            {
                if (s_CylinderMesh == null)
                    Init();
                return s_CylinderMesh;
            }
        }

        internal static Mesh sphereMesh
        {
            get
            {
                if (s_SphereMesh == null)
                    Init();
                return s_SphereMesh;
            }
        }

        internal static Mesh quadMesh
        {
            get
            {
                if (s_QuadMesh == null)
                    Init();
                return s_QuadMesh;
            }
        }

        internal static int s_xRotateHandleHash = "xRotateHandleHash".GetHashCode();
        internal static int s_yRotateHandleHash = "yRotateHandleHash".GetHashCode();
        internal static int s_zRotateHandleHash = "zRotateHandleHash".GetHashCode();
        internal static int s_cameraAxisRotateHandleHash = "cameraAxisRotateHandleHash".GetHashCode();
        internal static int s_xyzRotateHandleHash = "xyzRotateHandleHash".GetHashCode();
        internal static int s_xScaleHandleHash = "xScaleHandleHash".GetHashCode();
        internal static int s_yScaleHandleHash = "yScaleHandleHash".GetHashCode();
        internal static int s_zScaleHandleHash = "zScaleHandleHash".GetHashCode();
        internal static int s_xyzScaleHandleHash = "xyzScaleHandleHash".GetHashCode();

        private static Color lineTransparency = new Color(1, 1, 1, 0.75f);

        internal static SavedFloat s_LineThickness = new SavedFloat("SceneView.handleLineThickness", 2.0f);
        public static float lineThickness => s_LineThickness.value;

        // When hovering over some handle axis/control, this is the indication that it would
        // get picked on mouse press:
        // Color gets a bit more bright and less opaque,
        internal static Color s_HoverIntensity = new Color(1.2f, 1.2f, 1.2f, 1.33f);
        // Handle lines get more thick,
        internal static float s_HoverExtraThickness = 1.0f;
        // 3D handle elements (caps) get slightly larger.
        internal static float s_HoverExtraScale = 1.05f;

        // When axis is looking away from camera, fade it out along 25 -> 15 degrees range
        static readonly float kCameraViewLerpStart1 = Mathf.Cos(Mathf.Deg2Rad * 25.0f);
        static readonly float kCameraViewLerpEnd1 = Mathf.Cos(Mathf.Deg2Rad * 15.0f);
        // When axis is looking towards the camera, fade it out along 170 -> 175 degrees range
        static readonly float kCameraViewLerpStart2 = Mathf.Cos(Mathf.Deg2Rad * 170.0f);
        static readonly float kCameraViewLerpEnd2 = Mathf.Cos(Mathf.Deg2Rad * 175.0f);

        // Hide & disable axis if they have faded out more than 60%
        internal const float kCameraViewThreshold = 0.6f;

        // The function for calling AddControl in Layout event and draw the handle in Repaint event.
        public delegate void CapFunction(int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType);

        public delegate float SizeFunction(Vector3 position);

        static PrefColor[] s_AxisColor = { s_XAxisColor, s_YAxisColor, s_ZAxisColor };
        static Vector3[] s_AxisVector = { Vector3.right, Vector3.up, Vector3.forward };

        internal static Color s_DisabledHandleColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);

        internal static Color GetColorByAxis(int axis)
        {
            return s_AxisColor[axis];
        }

        internal static Color ToActiveColorSpace(Color color)
        {
            return (QualitySettings.activeColorSpace == ColorSpace.Linear) ? color.linear : color;
        }

        static Vector3 GetAxisVector(int axis)
        {
            return s_AxisVector[axis];
        }

        internal static bool IsHovering(int controlID, Event evt)
        {
            return controlID == HandleUtility.nearestControl && GUIUtility.hotControl == 0 && !Tools.viewToolActive;
        }

        static internal void SetupHandleColor(int controlID, Event evt, out Color prevColor, out float thickness)
        {
            prevColor = Handles.color;
            thickness = Handles.lineThickness;
            if (controlID == GUIUtility.hotControl)
            {
                Handles.color = Handles.selectedColor;
            }
            else if (IsHovering(controlID, evt))
            {
                Handles.color = Handles.color * s_HoverIntensity;
                thickness += s_HoverExtraThickness;
            }
        }

        static void Swap(ref Vector3 v, int[] indices, int a, int b)
        {
            var f = v[a];
            v[a] = v[b];
            v[b] = f;

            var t = indices[a];
            indices[a] = indices[b];
            indices[b] = t;
        }

        // Given view direction in handle space, calculate
        // back-to-front order in which handle axes should be drawn.
        // The array should be [3] size, and will contain axis indices
        // from (0,1,2) set.
        static void CalcDrawOrder(Vector3 viewDir, int[] ordering)
        {
            ordering[0] = 0;
            ordering[1] = 1;
            ordering[2] = 2;
            // essentially an unrolled bubble sort for 3 elements
            if (viewDir.y > viewDir.x) Swap(ref viewDir, ordering, 1, 0);
            if (viewDir.z > viewDir.y) Swap(ref viewDir, ordering, 2, 1);
            if (viewDir.y > viewDir.x) Swap(ref viewDir, ordering, 1, 0);
        }

        private static bool BeginLineDrawing(Matrix4x4 matrix, bool dottedLines, int mode)
        {
            if (Event.current.type != EventType.Repaint)
                return false;

            Color col = color * lineTransparency;
            if (dottedLines)
                HandleUtility.ApplyDottedWireMaterial(zTest);
            else
                HandleUtility.ApplyWireMaterial(zTest);
            GL.PushMatrix();
            GL.MultMatrix(matrix);
            GL.Begin(mode);
            GL.Color(col);
            return true;
        }

        private static void EndLineDrawing()
        {
            GL.End();
            GL.PopMatrix();
        }

        public static void DrawPolyLine(params Vector3[] points)
        {
            if (!BeginLineDrawing(matrix, false, GL.LINE_STRIP))
                return;
            for (int i = 0; i < points.Length; i++)
            {
                GL.Vertex(points[i]);
            }
            EndLineDrawing();
        }

        [ExcludeFromDocs]
        public static void DrawLine(Vector3 p1, Vector3 p2)
        {
            DrawLine(p1, p2, false);
        }

        internal static void DrawLine(Vector3 p1, Vector3 p2, bool dottedLine)
        {
            if (!BeginLineDrawing(matrix, dottedLine, GL.LINES))
                return;
            GL.Vertex(p1);
            GL.Vertex(p2);
            EndLineDrawing();
        }

        static float ThicknessToPixels(float thickness)
        {
            var halfThicknessPixels = thickness * EditorGUIUtility.pixelsPerPoint * 0.5f;
            if (halfThicknessPixels < 0.5f)
                halfThicknessPixels = 0;
            return halfThicknessPixels;
        }

        public static void DrawLine(Vector3 p1, Vector3 p2, [DefaultValue("0.0f")] float thickness)
        {
            if (Event.current.type != EventType.Repaint)
                return;
            thickness = ThicknessToPixels(thickness);
            if (thickness <= 0)
            {
                DrawLine(p1, p2);
                return;
            }

            var mat = SetupArcMaterial();
            mat.SetVector(kPropArcCenterRadius, new Vector4(p1.x, p1.y, p1.z, 0));
            mat.SetVector(kPropArcFromCount, new Vector4(p2.x, p2.y, p2.z, 0));
            mat.SetVector(kPropArcThicknessSides, new Vector4(thickness, kArcSides, 0, 0));
            mat.SetPass(1);

            var indexBuffer = HandleUtility.GetArcIndexBuffer(kArcSegments, kArcSides);
            Graphics.DrawProceduralNow(MeshTopology.Triangles, indexBuffer, kArcSides * 6);
        }

        public static void DrawLines(Vector3[] lineSegments)
        {
            if (!BeginLineDrawing(matrix, false, GL.LINES))
                return;
            for (int i = 0; i < lineSegments.Length; i += 2)
            {
                var p1 = lineSegments[i + 0];
                var p2 = lineSegments[i + 1];
                GL.Vertex(p1);
                GL.Vertex(p2);
            }
            EndLineDrawing();
        }

        public static void DrawLines(Vector3[] points, int[] segmentIndices)
        {
            if (!BeginLineDrawing(matrix, false, GL.LINES))
                return;
            for (int i = 0; i < segmentIndices.Length; i += 2)
            {
                var p1 = points[segmentIndices[i + 0]];
                var p2 = points[segmentIndices[i + 1]];
                GL.Vertex(p1);
                GL.Vertex(p2);
            }
            EndLineDrawing();
        }

        public static void DrawDottedLine(Vector3 p1, Vector3 p2, float screenSpaceSize)
        {
            if (!BeginLineDrawing(matrix, true, GL.LINES))
                return;
            var dashSize = screenSpaceSize * EditorGUIUtility.pixelsPerPoint;
            GL.MultiTexCoord(1, p1); GL.MultiTexCoord2(2, dashSize, 0); GL.Vertex(p1);
            GL.MultiTexCoord(1, p1); GL.MultiTexCoord2(2, dashSize, 0); GL.Vertex(p2);
            EndLineDrawing();
        }

        public static void DrawDottedLines(Vector3[] lineSegments, float screenSpaceSize)
        {
            if (!BeginLineDrawing(matrix, true, GL.LINES))
                return;
            var dashSize = screenSpaceSize * EditorGUIUtility.pixelsPerPoint;
            for (int i = 0; i < lineSegments.Length; i += 2)
            {
                var p1 = lineSegments[i + 0];
                var p2 = lineSegments[i + 1];
                GL.MultiTexCoord(1, p1); GL.MultiTexCoord2(2, dashSize, 0); GL.Vertex(p1);
                GL.MultiTexCoord(1, p1); GL.MultiTexCoord2(2, dashSize, 0); GL.Vertex(p2);
            }
            EndLineDrawing();
        }

        public static void DrawDottedLines(Vector3[] points, int[] segmentIndices, float screenSpaceSize)
        {
            if (!BeginLineDrawing(matrix, true, GL.LINES))
                return;
            var dashSize = screenSpaceSize * EditorGUIUtility.pixelsPerPoint;
            for (int i = 0; i < segmentIndices.Length; i += 2)
            {
                var p1 = points[segmentIndices[i + 0]];
                var p2 = points[segmentIndices[i + 1]];
                GL.MultiTexCoord(1, p1); GL.MultiTexCoord2(2, dashSize, 0); GL.Vertex(p1);
                GL.MultiTexCoord(1, p1); GL.MultiTexCoord2(2, dashSize, 0); GL.Vertex(p2);
            }
            EndLineDrawing();
        }

        public static void DrawWireCube(Vector3 center, Vector3 size)
        {
            Vector3 halfsize = size * 0.5f;

            Vector3[] points = new Vector3[10];
            points[0] = center + new Vector3(-halfsize.x, -halfsize.y, -halfsize.z);
            points[1] = center + new Vector3(-halfsize.x, halfsize.y, -halfsize.z);
            points[2] = center + new Vector3(halfsize.x, halfsize.y, -halfsize.z);
            points[3] = center + new Vector3(halfsize.x, -halfsize.y, -halfsize.z);
            points[4] = center + new Vector3(-halfsize.x, -halfsize.y, -halfsize.z);

            points[5] = center + new Vector3(-halfsize.x, -halfsize.y, halfsize.z);
            points[6] = center + new Vector3(-halfsize.x, halfsize.y, halfsize.z);
            points[7] = center + new Vector3(halfsize.x, halfsize.y, halfsize.z);
            points[8] = center + new Vector3(halfsize.x, -halfsize.y, halfsize.z);
            points[9] = center + new Vector3(-halfsize.x, -halfsize.y, halfsize.z);

            Handles.DrawPolyLine(points);
            Handles.DrawLine(points[1], points[6]);
            Handles.DrawLine(points[2], points[7]);
            Handles.DrawLine(points[3], points[8]);
        }

        public static bool ShouldRenderGizmos()
        {
            var playModeView = PlayModeView.GetRenderingView();
            SceneView sv = SceneView.currentDrawingSceneView;

            if (playModeView != null)
                return playModeView.IsShowingGizmos();

            if (sv != null)
                return sv.drawGizmos;

            return false;
        }

        public static void DrawGizmos(Camera camera)
        {
            if (ShouldRenderGizmos())
                Internal_DoDrawGizmos(camera);
        }

        public static Quaternion Disc(int id, Quaternion rotation, Vector3 position, Vector3 axis, float size, bool cutoffPlane, float snap)
        {
            return UnityEditorInternal.Disc.Do(id, rotation, position, axis, size, cutoffPlane, snap);
        }

        public static Quaternion FreeRotateHandle(int id, Quaternion rotation, Vector3 position, float size)
        {
            return UnityEditorInternal.FreeRotate.Do(id, rotation, position, size);
        }

        // Make a 3D slider
        public static Vector3 Slider(Vector3 position, Vector3 direction)
        {
            return Slider(position, direction, HandleUtility.GetHandleSize(position), ArrowHandleCap, -1);
        }

        public static Vector3 Slider(Vector3 position, Vector3 direction, float size, CapFunction capFunction, float snap)
        {
            int id = GUIUtility.GetControlID(s_SliderHash, FocusType.Passive);
            return UnityEditorInternal.Slider1D.Do(id, position, direction, size, capFunction, snap);
        }

        public static Vector3 Slider(int controlID, Vector3 position, Vector3 direction, float size, CapFunction capFunction, float snap)
        {
            return UnityEditorInternal.Slider1D.Do(controlID, position, direction, size, capFunction, snap);
        }

        public static Vector3 Slider(int controlID, Vector3 position, Vector3 offset, Vector3 direction, float size, CapFunction capFunction, float snap)
        {
            return UnityEditorInternal.Slider1D.Do(controlID, position, offset, direction, direction, size, capFunction, snap);
        }

        public static Vector3 FreeMoveHandle(Vector3 position, Quaternion rotation, float size, Vector3 snap, CapFunction capFunction)
        {
            int id = GUIUtility.GetControlID(s_FreeMoveHandleHash, FocusType.Passive);
            return UnityEditorInternal.FreeMove.Do(id, position, rotation, size, snap, capFunction);
        }

        public static Vector3 FreeMoveHandle(int controlID, Vector3 position, Quaternion rotation, float size, Vector3 snap, CapFunction capFunction)
        {
            return UnityEditorInternal.FreeMove.Do(controlID, position, rotation, size, snap, capFunction);
        }

        // Make a single-float draggable handle.
        public static float ScaleValueHandle(float value, Vector3 position, Quaternion rotation, float size, CapFunction capFunction, float snap)
        {
            int id = GUIUtility.GetControlID(s_ScaleValueHandleHash, FocusType.Passive);
            return UnityEditorInternal.SliderScale.DoCenter(id, value, position, rotation, size, capFunction, snap);
        }

        public static float ScaleValueHandle(int controlID, float value, Vector3 position, Quaternion rotation, float size, CapFunction capFunction, float snap)
        {
            return UnityEditorInternal.SliderScale.DoCenter(controlID, value, position, rotation, size, capFunction, snap);
        }

        // Make a 3D Button.
        public static bool Button(Vector3 position, Quaternion direction, float size, float pickSize, CapFunction capFunction)
        {
            int id = GUIUtility.GetControlID(s_ButtonHash, FocusType.Passive);
            return UnityEditorInternal.Button.Do(id, position, direction, size, pickSize, capFunction);
        }

        internal static bool Button(int controlID, Vector3 position, Quaternion direction, float size, float pickSize, CapFunction capFunction)
        {
            return UnityEditorInternal.Button.Do(controlID, position, direction, size, pickSize, capFunction);
        }

        // Draw a cube. Pass this into handle functions.
        public static void CubeHandleCap(int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType)
        {
            switch (eventType)
            {
                case EventType.Layout:
                case EventType.MouseMove:
                    HandleUtility.AddControl(controlID, HandleUtility.DistanceToCube(position, rotation, size));
                    break;
                case (EventType.Repaint):
                    Graphics.DrawMeshNow(cubeMesh, StartCapDraw(position, rotation, size));
                    break;
            }
        }

        // Draw a Sphere. Pass this into handle functions.
        public static void SphereHandleCap(int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType)
        {
            switch (eventType)
            {
                case EventType.Layout:
                case EventType.MouseMove:
                    // TODO: Create DistanceToCube
                    HandleUtility.AddControl(controlID, HandleUtility.DistanceToCircle(position, size));
                    break;
                case (EventType.Repaint):
                    Graphics.DrawMeshNow(sphereMesh, StartCapDraw(position, rotation, size));
                    break;
            }
        }

        // Draw a Cone. Pass this into handle functions.
        public static void ConeHandleCap(int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType)
        {
            switch (eventType)
            {
                case EventType.Layout:
                case EventType.MouseMove:
                    HandleUtility.AddControl(controlID, HandleUtility.DistanceToCone(position, rotation, size));
                    break;
                case (EventType.Repaint):
                    Graphics.DrawMeshNow(coneMesh, StartCapDraw(position, rotation, size));
                    break;
            }
        }

        // Draw a Cylinder. Pass this into handle functions.
        public static void CylinderHandleCap(int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType)
        {
            switch (eventType)
            {
                case EventType.Layout:
                case EventType.MouseMove:
                    // TODO: Create DistanceToCylinder
                    HandleUtility.AddControl(controlID, HandleUtility.DistanceToCircle(position, size));
                    break;
                case (EventType.Repaint):
                    Graphics.DrawMeshNow(cylinderMesh, StartCapDraw(position, rotation, size));
                    break;
            }
        }

        // Draw a camera-facing Rectangle. Pass this into handle functions.
        static Vector3[] s_RectangleHandlePointsCache = new Vector3[5];
        public static void RectangleHandleCap(int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType)
        {
            RectangleHandleCap(controlID, position, rotation, new Vector2(size, size), eventType);
        }

        internal static void RectangleHandleCap(int controlID, Vector3 position, Quaternion rotation, Vector2 size, EventType eventType)
        {
            switch (eventType)
            {
                case EventType.Layout:
                case EventType.MouseMove:
                    // TODO: Create DistanceToRectangle
                    HandleUtility.AddControl(controlID, HandleUtility.DistanceToRectangleInternal(position, rotation, size));
                    break;
                case (EventType.Repaint):
                    Vector3 sideways = rotation * new Vector3(size.x, 0, 0);
                    Vector3 up = rotation * new Vector3(0, size.y, 0);
                    s_RectangleHandlePointsCache[0] = position + sideways + up;
                    s_RectangleHandlePointsCache[1] = position + sideways - up;
                    s_RectangleHandlePointsCache[2] = position - sideways - up;
                    s_RectangleHandlePointsCache[3] = position - sideways + up;
                    s_RectangleHandlePointsCache[4] = position + sideways + up;
                    Handles.DrawPolyLine(s_RectangleHandlePointsCache);
                    break;
            }
        }

        internal static void RectangleHandleCapWorldSpace(int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType)
        {
            RectangleHandleCapWorldSpace(controlID, position, rotation, new Vector2(size, size), eventType);
        }

        internal static void RectangleHandleCapWorldSpace(int controlID, Vector3 position, Quaternion rotation, Vector2 size, EventType eventType)
        {
            switch (eventType)
            {
                case EventType.Layout:
                case EventType.MouseMove:
                    HandleUtility.AddControl(controlID, HandleUtility.DistanceToRectangleInternalWorldSpace(position, rotation, size));
                    break;
                case (EventType.Repaint):
                    Vector3 sideways = rotation * new Vector3(size.x, 0, 0);
                    Vector3 up = rotation * new Vector3(0, size.y, 0);
                    s_RectangleHandlePointsCache[0] = position + sideways + up;
                    s_RectangleHandlePointsCache[1] = position + sideways - up;
                    s_RectangleHandlePointsCache[2] = position - sideways - up;
                    s_RectangleHandlePointsCache[3] = position - sideways + up;
                    s_RectangleHandlePointsCache[4] = position + sideways + up;
                    Handles.DrawPolyLine(s_RectangleHandlePointsCache);
                    break;
            }
        }

        // Draw a camera-facing dot. Pass this into handle functions.
        public static void DotHandleCap(int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType)
        {
            switch (eventType)
            {
                case EventType.Layout:
                case EventType.MouseMove:
                    HandleUtility.AddControl(controlID, HandleUtility.DistanceToRectangle(position, rotation, size));
                    break;
                case (EventType.Repaint):
                    // Only apply matrix to the position because DotCap is camera facing
                    position = matrix.MultiplyPoint(position);

                    Vector3 sideways = (Camera.current == null ? Vector3.right : Camera.current.transform.right) * size;
                    Vector3 up = (Camera.current == null ? Vector3.up : Camera.current.transform.up) * size;

                    Color col = color * new Color(1, 1, 1, 0.99f);
                    HandleUtility.ApplyWireMaterial(Handles.zTest);
                    GL.Begin(GL.QUADS);
                    GL.Color(col);
                    GL.Vertex(position + sideways + up);
                    GL.Vertex(position + sideways - up);
                    GL.Vertex(position - sideways - up);
                    GL.Vertex(position - sideways + up);
                    GL.End();
                    break;
            }
        }

        // Draw a camera-facing Circle. Pass this into handle functions.
        public static void CircleHandleCap(int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType)
        {
            switch (eventType)
            {
                case EventType.Layout:
                case EventType.MouseMove:
                    HandleUtility.AddControl(controlID, HandleUtility.DistanceToRectangle(position, rotation, size));
                    break;
                case (EventType.Repaint):
                    StartCapDraw(position, rotation, size);
                    Vector3 forward = rotation * new Vector3(0, 0, 1);
                    Handles.DrawWireDisc(position, forward, size);
                    break;
            }
        }

        // Draw an arrow like those used by the move tool.
        public static void ArrowHandleCap(int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType)
        {
            ArrowHandleCap(controlID, position, rotation, size, eventType, Vector3.zero);
        }

        internal static void ArrowHandleCap(int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType, Vector3 coneOffset)
        {
            switch (eventType)
            {
                case EventType.Layout:
                case EventType.MouseMove:
                {
                    Vector3 direction = rotation * Vector3.forward;
                    HandleUtility.AddControl(controlID, HandleUtility.DistanceToLine(position, position + (direction + coneOffset) * (size * .9f)));
                    HandleUtility.AddControl(controlID, HandleUtility.DistanceToCone(position + (direction + coneOffset) * size, rotation, size * .2f));
                    break;
                }
                case EventType.Repaint:
                {
                    Vector3 direction = rotation * Vector3.forward;
                    float thickness = Handles.lineThickness;
                    float coneSize = size * .2f;
                    if (IsHovering(controlID, Event.current))
                    {
                        thickness += s_HoverExtraThickness;
                        coneSize *= s_HoverExtraScale;
                    }
                    ConeHandleCap(controlID, position + (direction + coneOffset) * size, rotation, coneSize, eventType);
                    Handles.DrawLine(position, position + (direction + coneOffset) * (size * .9f), thickness);
                    break;
                }
            }
        }

        // Draw a camera facing selection frame.
        public static void DrawSelectionFrame(int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType)
        {
            if (eventType != EventType.Repaint)
                return;

            Handles.StartCapDraw(position, rotation, size);
            Vector3 sideways = rotation * new Vector3(size, 0, 0);
            Vector3 up = rotation * new Vector3(0, size, 0);

            var point1 = position - sideways + up;
            var point2 = position + sideways + up;
            var point3 = position + sideways - up;
            var point4 = position - sideways - up;

            Handles.DrawLine(point1, point2);
            Handles.DrawLine(point2, point3);
            Handles.DrawLine(point3, point4);
            Handles.DrawLine(point4, point1);
        }

        internal static Color GetFadedAxisColor(Color col, float fade, int id)
        {
            // never fade out axes that are being hover-highlighted or currently interacted with
            if (id != 0 && id == GUIUtility.hotControl || id == HandleUtility.nearestControl)
                fade = 0;
            col = Color.Lerp(col, Color.clear, fade);
            return col;
        }

        internal static float GetCameraViewLerpForWorldAxis(Vector3 viewVector, Vector3 axis)
        {
            var dot = Vector3.Dot(viewVector, axis);
            var l1 = Mathf.InverseLerp(kCameraViewLerpStart1, kCameraViewLerpEnd1, dot);
            var l2 = Mathf.InverseLerp(kCameraViewLerpStart2, kCameraViewLerpEnd2, dot);
            return Mathf.Max(l1, l2);
        }

        internal static Vector3 GetCameraViewFrom(Vector3 position, Matrix4x4 matrix)
        {
            Camera camera = Camera.current;
            return camera.orthographic
                ? matrix.MultiplyVector(camera.transform.forward).normalized
                : matrix.MultiplyVector(position - camera.transform.position).normalized;
        }

        // Make a 3D Scene view position handle.
        public static Vector3 PositionHandle(Vector3 position, Quaternion rotation)
        {
            return DoPositionHandle(position, rotation);
        }

        // Make a Scene view rotation handle.
        public static Quaternion RotationHandle(Quaternion rotation, Vector3 position)
        {
            return DoRotationHandle(rotation, position);
        }

        // Make a Scene view scale handle
        public static Vector3 ScaleHandle(Vector3 scale, Vector3 position, Quaternion rotation, float size)
        {
            return DoScaleHandle(scale, position, rotation, size);
        }

        ///*listonly*
        public static float RadiusHandle(Quaternion rotation, Vector3 position, float radius, bool handlesOnly)
        {
            return DoRadiusHandle(rotation, position, radius, handlesOnly);
        }

        // Make a Scene view radius handle
        public static float RadiusHandle(Quaternion rotation, Vector3 position, float radius)
        {
            return DoRadiusHandle(rotation, position, radius, false);
        }

        // Make a Scene View cone handle
        internal static Vector2 ConeHandle(Quaternion rotation, Vector3 position, Vector2 angleAndRange, float angleScale, float rangeScale, bool handlesOnly)
        {
            return DoConeHandle(rotation, position, angleAndRange, angleScale, rangeScale, handlesOnly);
        }

        // Make a Scene View cone frustrum handle
        internal static Vector3 ConeFrustrumHandle(Quaternion rotation, Vector3 position, Vector3 radiusAngleRange, ConeHandles showHandles = ConeHandles.All)
        {
            return DoConeFrustrumHandle(rotation, position, radiusAngleRange, showHandles);
        }

        // Slide a handle in a 2D plane
        public static Vector3 Slider2D(int id, Vector3 handlePos, Vector3 offset, Vector3 handleDir, Vector3 slideDir1, Vector3 slideDir2, float handleSize, CapFunction capFunction, Vector2 snap)
        {
            return Slider2D(id, handlePos, offset, handleDir, slideDir1, slideDir2, handleSize, capFunction, snap, false);
        }

        public static Vector3 Slider2D(int id, Vector3 handlePos, Vector3 offset, Vector3 handleDir, Vector3 slideDir1, Vector3 slideDir2, float handleSize, CapFunction capFunction, Vector2 snap, [DefaultValue("false")] bool drawHelper)
        {
            return UnityEditorInternal.Slider2D.Do(id, handlePos, offset, handleDir, slideDir1, slideDir2, handleSize, capFunction, snap, drawHelper);
        }

        /// *listonly*
        public static Vector3 Slider2D(Vector3 handlePos, Vector3 handleDir, Vector3 slideDir1, Vector3 slideDir2, float handleSize, CapFunction capFunction, Vector2 snap)
        {
            return Slider2D(handlePos, handleDir, slideDir1, slideDir2, handleSize, capFunction, snap, false);
        }

        public static Vector3 Slider2D(Vector3 handlePos, Vector3 handleDir, Vector3 slideDir1, Vector3 slideDir2, float handleSize, CapFunction capFunction, Vector2 snap, [DefaultValue("false")] bool drawHelper)
        {
            int id = GUIUtility.GetControlID(s_Slider2DHash, FocusType.Passive);
            return UnityEditorInternal.Slider2D.Do(id, handlePos, new Vector3(0, 0, 0), handleDir, slideDir1, slideDir2, handleSize, capFunction, snap, drawHelper);
        }

        /// *listonly*
        public static Vector3 Slider2D(int id, Vector3 handlePos, Vector3 handleDir, Vector3 slideDir1, Vector3 slideDir2, float handleSize, CapFunction capFunction, Vector2 snap)
        {
            return Slider2D(id, handlePos, handleDir, slideDir1, slideDir2, handleSize, capFunction, snap, false);
        }

        public static Vector3 Slider2D(int id, Vector3 handlePos, Vector3 handleDir, Vector3 slideDir1, Vector3 slideDir2, float handleSize, CapFunction capFunction, Vector2 snap, [DefaultValue("false")] bool drawHelper)
        {
            return UnityEditorInternal.Slider2D.Do(id, handlePos, new Vector3(0, 0, 0), handleDir, slideDir1, slideDir2, handleSize, capFunction, snap, drawHelper);
        }

        /// *listonly*
        public static Vector3 Slider2D(Vector3 handlePos, Vector3 handleDir, Vector3 slideDir1, Vector3 slideDir2, float handleSize, CapFunction capFunction, float snap)
        {
            return Slider2D(handlePos, handleDir, slideDir1, slideDir2, handleSize, capFunction, snap, false);
        }

        public static Vector3 Slider2D(Vector3 handlePos, Vector3 handleDir, Vector3 slideDir1, Vector3 slideDir2, float handleSize, CapFunction capFunction, float snap, [DefaultValue("false")] bool drawHelper)
        {
            int id = GUIUtility.GetControlID(s_Slider2DHash, FocusType.Passive);
            return Slider2D(id, handlePos, new Vector3(0, 0, 0), handleDir, slideDir1, slideDir2, handleSize, capFunction, new Vector2(snap, snap), drawHelper);
        }

        // Make an unconstrained rotation handle.
        public static Quaternion FreeRotateHandle(Quaternion rotation, Vector3 position, float size)
        {
            int id = GUIUtility.GetControlID(s_FreeRotateHandleHash, FocusType.Passive);
            return UnityEditorInternal.FreeRotate.Do(id, rotation, position, size);
        }

        // Make a directional scale slider
        public static float ScaleSlider(float scale, Vector3 position, Vector3 direction, Quaternion rotation, float size, float snap)
        {
            int id = GUIUtility.GetControlID(s_ScaleSliderHash, FocusType.Passive);
            return UnityEditorInternal.SliderScale.DoAxis(id, scale, position, direction, rotation, size, snap);
        }

        // Make a 3D disc that can be dragged with the mouse
        public static Quaternion Disc(Quaternion rotation, Vector3 position, Vector3 axis, float size, bool cutoffPlane, float snap)
        {
            int id = GUIUtility.GetControlID(s_DiscHash, FocusType.Passive);
            return UnityEditorInternal.Disc.Do(id, rotation, position, axis, size, cutoffPlane, snap);
        }

        internal static void SetupIgnoreRaySnapObjects()
        {
            HandleUtility.ignoreRaySnapObjects = Selection.GetTransforms(SelectionMode.Editable | SelectionMode.Deep);
        }

        // If snapping is active, return a new value rounded to the nearest increment of snap.
        public static float SnapValue(float value, float snap)
        {
            if (EditorSnapSettings.incrementalSnapActive)
                return Snapping.Snap(value, snap);
            return value;
        }

        // If snapping is active, return a new value rounded to the nearest increment of snap.
        public static Vector2 SnapValue(Vector2 value, Vector2 snap)
        {
            if (EditorSnapSettings.incrementalSnapActive)
                return Snapping.Snap(value, snap);
            return value;
        }

        // If snapping is active, return a new value rounded to the nearest increment of snap.
        public static Vector3 SnapValue(Vector3 value, Vector3 snap)
        {
            if (EditorSnapSettings.incrementalSnapActive)
                return Snapping.Snap(value, snap);
            return value;
        }

        // Snap all transform positions to the grid
        public static void SnapToGrid(Transform[] transforms, SnapAxis axis = SnapAxis.All)
        {
            if (transforms != null && transforms.Length > 0)
            {
                foreach (var t in transforms)
                {
                    if (t != null)
                        t.position = Snapping.Snap(t.position, Vector3.Scale(GridSettings.size, new SnapAxisFilter(axis)));
                }
            }
        }

        // The camera used for deciding where 3D handles end up
        public Camera currentCamera { get { return Camera.current; } set { Internal_SetCurrentCamera(value); } }


        internal static Color realHandleColor { get { return color * new Color(1, 1, 1, .5f) + (lighting ? new Color(0, 0, 0, .5f) : new Color(0, 0, 0, 0)); } }


        // Draw two-shaded wire-disc that is fully shadowed
        internal static void DrawTwoShadedWireDisc(Vector3 position, Vector3 axis, float radius)
        {
            Color col = Handles.color;
            Color origCol = col;
            col.a *= backfaceAlphaMultiplier;
            Handles.color = col;
            Handles.DrawWireDisc(position, axis, radius);
            Handles.color = origCol;
        }

        // Draw two-shaded wire-disc with from and degrees specifying the lit part and the rest being shadowed
        internal static void DrawTwoShadedWireDisc(Vector3 position, Vector3 axis, Vector3 from, float degrees, float radius)
        {
            Handles.DrawWireArc(position, axis, from, degrees, radius);
            Color col = Handles.color;
            Color origCol = col;
            col.a *= backfaceAlphaMultiplier;
            Handles.color = col;
            Handles.DrawWireArc(position, axis, from, degrees - 360, radius);
            Handles.color = origCol;
        }

        // Sets up matrix
        internal static Matrix4x4 StartCapDraw(Vector3 position, Quaternion rotation, float size)
        {
            Shader.SetGlobalColor("_HandleColor", realHandleColor);
            Shader.SetGlobalFloat("_HandleSize", size);
            Matrix4x4 mat = matrix * Matrix4x4.TRS(position, rotation, Vector3.one);
            Shader.SetGlobalMatrix("_ObjectToWorld", mat);
            HandleUtility.handleMaterial.SetInt("_HandleZTest", (int)zTest);
            HandleUtility.handleMaterial.SetPass(0);
            return mat;
        }

        // Draw a camera-facing Rectangle. Pass this into handle functions.
        static Vector3[] s_RectangleCapPointsCache = new Vector3[5];

        internal static void RectangleCap(int controlID, Vector3 position, Quaternion rotation, Vector2 size)
        {
            if (Event.current.type != EventType.Repaint)
                return;
            Vector3 sideways = rotation * new Vector3(size.x, 0, 0);
            Vector3 up = rotation * new Vector3(0, size.y, 0);
            s_RectangleCapPointsCache[0] = position + sideways + up;
            s_RectangleCapPointsCache[1] = position + sideways - up;
            s_RectangleCapPointsCache[2] = position - sideways - up;
            s_RectangleCapPointsCache[3] = position - sideways + up;
            s_RectangleCapPointsCache[4] = position + sideways + up;
            Handles.DrawPolyLine(s_RectangleCapPointsCache);
        }

        // Draw a camera facing selection frame.
        public static void SelectionFrame(int controlID, Vector3 position, Quaternion rotation, float size)
        {
            if (Event.current.type != EventType.Repaint)
                return;
            Handles.StartCapDraw(position, rotation, size);
            Vector3 sideways = rotation * new Vector3(size, 0, 0);
            Vector3 up = rotation * new Vector3(0, size, 0);

            var point1 = position - sideways + up;
            var point2 = position + sideways + up;
            var point3 = position + sideways - up;
            var point4 = position - sideways - up;

            Handles.DrawLine(point1, point2);
            Handles.DrawLine(point2, point3);
            Handles.DrawLine(point3, point4);
            Handles.DrawLine(point4, point1);
        }

        internal static void DrawAAPolyLine(Color[] colors, Vector3[] points)                { DoDrawAAPolyLine(colors, points, -1, null, 2, 0.75f); }
        internal static void DrawAAPolyLine(float width, Color[] colors, Vector3[] points)   { DoDrawAAPolyLine(colors, points, -1, null, width, 0.75f); }
        /// *listonly*
        public static void DrawAAPolyLine(params Vector3[] points)                       { DoDrawAAPolyLine(null, points, -1, null, 2, 0.75f); }
        /// *listonly*
        public static void DrawAAPolyLine(float width, params Vector3[] points)          { DoDrawAAPolyLine(null, points, -1, null, width, 0.75f); }
        /// *listonly*
        public static void DrawAAPolyLine(Texture2D lineTex, params Vector3[] points)    { DoDrawAAPolyLine(null, points, -1, lineTex, lineTex.height / 2, 0.99f); }
        /// *listonly*
        public static void DrawAAPolyLine(float width, int actualNumberOfPoints, params Vector3[] points) { DoDrawAAPolyLine(null, points, actualNumberOfPoints, null, width, 0.75f); }

        // Draw anti-aliased line specified with point array and width.
        public static void DrawAAPolyLine(Texture2D lineTex, float width, params Vector3[] points) {  DoDrawAAPolyLine(null, points, -1, lineTex, width, 0.99f); }


        static void DoDrawAAPolyLine(Color[] colors, Vector3[] points, int actualNumberOfPoints, Texture2D lineTex, float width, float alpha)
        {
            if (Event.current.type != EventType.Repaint)
                return;
            HandleUtility.ApplyWireMaterial(zTest);

            Color defaultColor = new Color(1, 1, 1, alpha);

            if (colors != null)
            {
                for (int i = 0; i < colors.Length; i++)
                    colors[i] *= defaultColor;
            }
            else
                defaultColor *= color;

            Internal_DrawAAPolyLine(colors, points, defaultColor, actualNumberOfPoints, lineTex, width, matrix);
        }

        // Draw anti-aliased convex polygon specified with point array.
        public static void DrawAAConvexPolygon(params Vector3[] points) {  DoDrawAAConvexPolygon(points, -1, 1.0f); }

        static void DoDrawAAConvexPolygon(Vector3[] points, int actualNumberOfPoints, float alpha)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            HandleUtility.ApplyWireMaterial(zTest);

            Color defaultColor = new Color(1, 1, 1, alpha) * color;
            Internal_DrawAAConvexPolygon(points, defaultColor, actualNumberOfPoints, matrix);
        }

        // Draw textured bezier line through start and end points with the given tangents.  To get an anti-aliased effect use a texture that is 1x2 pixels with one transparent white pixel and one opaque white pixel.  The bezier curve will be swept using this texture.
        public static void DrawBezier(Vector3 startPosition, Vector3 endPosition, Vector3 startTangent, Vector3 endTangent, Color color, Texture2D texture, float width)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            HandleUtility.ApplyWireMaterial(zTest);
            Internal_DrawBezier(startPosition, endPosition, startTangent, endTangent, color, texture, width, matrix);
        }

        // Draw the outline of a flat disc in 3D space.
        [ExcludeFromDocs]
        public static void DrawWireDisc(Vector3 center, Vector3 normal, float radius)
        {
            DrawWireDisc(center, normal, radius, 0.0f);
        }

        public static void DrawWireDisc(Vector3 center, Vector3 normal, float radius, [DefaultValue("0.0f")] float thickness)
        {
            Vector3 tangent = Vector3.Cross(normal, Vector3.up);
            if (tangent.sqrMagnitude < .001f)
                tangent = Vector3.Cross(normal, Vector3.right);
            DrawWireArc(center, normal, tangent, 360, radius, thickness);
        }

        private static readonly Vector3[] s_WireArcPoints = new Vector3[60];

        // Draw a circular arc in 3D space.
        [ExcludeFromDocs]
        public static void DrawWireArc(Vector3 center, Vector3 normal, Vector3 from, float angle, float radius)
        {
            DrawWireArc(center, normal, from, angle, radius, 0.0f);
        }

        static Material SetupArcMaterial()
        {
            var col = color * lineTransparency;
            var mat = HandleUtility.handleArcMaterial;
            mat.SetInt(kPropUseGuiClip, Camera.current ? 0 : 1);
            mat.SetInt(kPropHandleZTest, (int)zTest);
            mat.SetColor(kPropColor, col);
            mat.SetMatrix(kPropHandlesMatrix, matrix);
            return mat;
        }

        const int kArcSegments = 60;
        const int kArcSides = 8;

        public static void DrawWireArc(Vector3 center, Vector3 normal, Vector3 from, float angle, float radius, [DefaultValue("0.0f")] float thickness)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            thickness = ThicknessToPixels(thickness);

            var mat = SetupArcMaterial();
            mat.SetVector(kPropArcCenterRadius, new Vector4(center.x, center.y, center.z, radius));
            mat.SetVector(kPropArcNormalAngle, new Vector4(normal.x, normal.y, normal.z, angle * Mathf.Deg2Rad));
            mat.SetVector(kPropArcFromCount, new Vector4(from.x, from.y, from.z, kArcSegments));
            mat.SetVector(kPropArcThicknessSides, new Vector4(thickness, kArcSides, 0, 0));
            mat.SetPass(0);

            if (thickness <= 0.0f)
                Graphics.DrawProceduralNow(MeshTopology.LineStrip, kArcSegments);
            else
            {
                var indexBuffer = HandleUtility.GetArcIndexBuffer(kArcSegments, kArcSides);
                Graphics.DrawProceduralNow(MeshTopology.Triangles, indexBuffer, indexBuffer.count);
            }
        }

        public static void DrawSolidRectangleWithOutline(Rect rectangle, Color faceColor, Color outlineColor)
        {
            Vector3[] points =
            {
                new Vector3(rectangle.xMin, rectangle.yMin, 0.0f),
                new Vector3(rectangle.xMax, rectangle.yMin, 0.0f),
                new Vector3(rectangle.xMax, rectangle.yMax, 0.0f),
                new Vector3(rectangle.xMin, rectangle.yMax, 0.0f)
            };

            Handles.DrawSolidRectangleWithOutline(points, faceColor, outlineColor);
        }

        // Draw a solid outlined rectangle in 3D space.
        public static void DrawSolidRectangleWithOutline(Vector3[] verts, Color faceColor, Color outlineColor)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            HandleUtility.ApplyWireMaterial(zTest);

            GL.PushMatrix();
            GL.MultMatrix(matrix);

            // Triangles (Draw it twice to ensure render of both front and back faces)
            if (faceColor.a > 0)
            {
                Color col = faceColor * color;
                GL.Begin(GL.TRIANGLES);
                for (int i = 0; i < 2; i++)
                {
                    GL.Color(col);
                    GL.Vertex(verts[i * 2 + 0]);
                    GL.Vertex(verts[i * 2 + 1]);
                    GL.Vertex(verts[(i * 2 + 2) % 4]);

                    GL.Vertex(verts[i * 2 + 0]);
                    GL.Vertex(verts[(i * 2 + 2) % 4]);
                    GL.Vertex(verts[i * 2 + 1]);
                }
                GL.End();
            }

            // Outline
            if (outlineColor.a > 0)
            {
                //HandleUtility.ApplyWireMaterial ();
                Color col = outlineColor * color;
                GL.Begin(GL.LINES);
                GL.Color(col);
                for (int i = 0; i < 4; i++)
                {
                    GL.Vertex(verts[i]);
                    GL.Vertex(verts[(i + 1) % 4]);
                }
                GL.End();
            }

            GL.PopMatrix();
        }

        // Draw a solid flat disc in 3D space.
        public static void DrawSolidDisc(Vector3 center, Vector3 normal, float radius)
        {
            Vector3 tangent = Vector3.Cross(normal, Vector3.up);
            if (tangent.sqrMagnitude < .001f)
                tangent = Vector3.Cross(normal, Vector3.right);
            DrawSolidArc(center, normal, tangent, 360, radius);
        }

        // Draw a circular sector (pie piece) in 3D space.
        public static void DrawSolidArc(Vector3 center, Vector3 normal, Vector3 from, float angle, float radius)
        {
            if (Event.current.type != EventType.Repaint)
                return;
            SetDiscSectionPoints(s_WireArcPoints, center, normal, from, angle, radius);

            Shader.SetGlobalColor("_HandleColor", color * new Color(1, 1, 1, .5f));
            Shader.SetGlobalFloat("_HandleSize", 1);

            HandleUtility.ApplyWireMaterial(zTest);

            // Draw it twice to ensure backface culling doesn't hide any of the faces
            GL.PushMatrix();
            GL.MultMatrix(matrix);
            GL.Begin(GL.TRIANGLES);
            for (int i = 1, count = s_WireArcPoints.Length; i < count; ++i)
            {
                GL.Color(color);
                GL.Vertex(center);
                GL.Vertex(s_WireArcPoints[i - 1]);
                GL.Vertex(s_WireArcPoints[i]);
                GL.Vertex(center);
                GL.Vertex(s_WireArcPoints[i]);
                GL.Vertex(s_WireArcPoints[i - 1]);
            }
            GL.End();
            GL.PopMatrix();
        }

        internal static Mesh s_CubeMesh, s_SphereMesh, s_ConeMesh, s_CylinderMesh, s_QuadMesh;
        internal static void Init()
        {
            if (!s_CubeMesh)
            {
                GameObject handleGo = (GameObject)EditorGUIUtility.Load("SceneView/HandlesGO.fbx");
                if (!handleGo)
                {
                    Debug.Log("Couldn't find SceneView/HandlesGO.fbx");
                }
                // @TODO: temp workaround to make it not render in the scene
                handleGo.SetActive(false);

                const string k_AssertMessage = "mesh is null. A problem has occurred with `SceneView/HandlesGO.fbx`";

                foreach (Transform t in handleGo.transform)
                {
                    var meshFilter = t.GetComponent<MeshFilter>();
                    switch (t.name)
                    {
                        case "Cube":
                            s_CubeMesh = meshFilter.sharedMesh;
                            Debug.AssertFormat(s_CubeMesh != null, k_AssertMessage);
                            break;
                        case "Sphere":
                            s_SphereMesh = meshFilter.sharedMesh;
                            Debug.AssertFormat(s_SphereMesh != null, k_AssertMessage);
                            break;
                        case "Cone":
                            s_ConeMesh = meshFilter.sharedMesh;
                            Debug.AssertFormat(s_ConeMesh != null, k_AssertMessage);
                            break;
                        case "Cylinder":
                            s_CylinderMesh = meshFilter.sharedMesh;
                            Debug.AssertFormat(s_CylinderMesh != null, k_AssertMessage);
                            break;
                        case "Quad":
                            s_QuadMesh = meshFilter.sharedMesh;
                            Debug.AssertFormat(s_QuadMesh != null, k_AssertMessage);
                            break;
                    }
                }
            }
        }

        /// *listonly*
        public static void Label(Vector3 position, string text)                          { Label(position, EditorGUIUtility.TempContent(text), GUI.skin.label); }
        /// *listonly*
        public static void Label(Vector3 position, Texture image)                        { Label(position, EditorGUIUtility.TempContent(image), GUI.skin.label); }
        /// *listonly*
        public static void Label(Vector3 position, GUIContent content)                   { Label(position, content, GUI.skin.label); }
        /// *listonly*
        public static void Label(Vector3 position, string text, GUIStyle style)              { Label(position, EditorGUIUtility.TempContent(text), style); }
        // Make a text label positioned in 3D space.
        public static void Label(Vector3 position, GUIContent content, GUIStyle style)
        {
            Vector3 screenPoint = HandleUtility.WorldToGUIPointWithDepth(position);
            if (screenPoint.z < 0)
                return; //label is behind camera

            Handles.BeginGUI();
            GUI.Label(HandleUtility.WorldPointToSizedRect(position, content, style), content, style);
            Handles.EndGUI();
        }

        // Returns actual rectangle where the camera will be rendered
        internal static Rect GetCameraRect(Rect position)
        {
            Rect screenRect = GUIClip.Unclip(position);
            Rect cameraRect = new Rect(screenRect.xMin, Screen.height - screenRect.yMax, screenRect.width, screenRect.height);
            return cameraRect;
        }

        // Get the size of the main playModeView window
        public static Vector2 GetMainGameViewSize()
        {
            return PlayModeView.GetMainPlayModeViewTargetSize();
        }

        // Clears the camera.
        public static void ClearCamera(Rect position, Camera camera)
        {
            Event evt = Event.current;
            if (camera.targetTexture == null)
            {
                Rect screenRect = GUIClip.Unclip(position);
                screenRect = EditorGUIUtility.PointsToPixels(screenRect);
                Rect cameraRect = new Rect(screenRect.xMin, Screen.height - screenRect.yMax, screenRect.width, screenRect.height);
                camera.pixelRect = cameraRect;
            }
            else
            {
                camera.rect = new Rect(0, 0, 1, 1);
            }
            if (evt.type == EventType.Repaint)
                Internal_ClearCamera(camera);
            else
                Internal_SetCurrentCamera(camera);
        }

        internal static void DrawCameraImpl(Rect position, Camera camera,
            DrawCameraMode drawMode, bool drawGrid, DrawGridParameters gridParam, bool finish, bool renderGizmos = true
        )
        {
            Event evt = Event.current;

            if (evt.type == EventType.Repaint)
            {
                if (camera.targetTexture == null)
                {
                    Rect screenRect = GUIClip.Unclip(position);
                    screenRect = EditorGUIUtility.PointsToPixels(screenRect);
                    camera.pixelRect = new Rect(screenRect.xMin, Screen.height - screenRect.yMax, screenRect.width, screenRect.height);
                }
                else
                {
                    camera.rect = new Rect(0, 0, 1, 1);
                }
                if (drawMode == DrawCameraMode.Normal)
                {
                    RenderTexture temp = camera.targetTexture;
                    camera.targetTexture = RenderTexture.active;
                    camera.Render();
                    camera.targetTexture = temp;
                }
                else
                {
                    if (drawGrid)
                        Internal_DrawCameraWithGrid(camera, drawMode, ref gridParam, renderGizmos);
                    else
                        Internal_DrawCamera(camera, drawMode, renderGizmos);

                    // VR scene cameras finish drawing with each eye render
                    if (finish && camera.cameraType != CameraType.VR)
                        Internal_FinishDrawingCamera(camera, renderGizmos);
                }
            }
            else
                Internal_SetCurrentCamera(camera);
        }

        // Draws a camera inside a rectangle.
        // It also sets up the (for now, anyways) undocumented Event.current.mouseRay and Event.current.lastMouseRay for handleutility functions.
        //
        internal static void DrawCamera(Rect position, Camera camera, DrawCameraMode drawMode, DrawGridParameters gridParam)
        {
            DrawCameraImpl(position, camera, drawMode, true, gridParam, true);
        }

        internal static void DrawCameraStep1(Rect position, Camera camera, DrawCameraMode drawMode, DrawGridParameters gridParam, bool drawGizmos)
        {
            DrawCameraImpl(position, camera, drawMode, true, gridParam, false, drawGizmos);
        }

        internal static void DrawCameraStep2(Camera camera, DrawCameraMode drawMode, bool drawGizmos)
        {
            if (Event.current.type == EventType.Repaint && drawMode != DrawCameraMode.Normal)
                Internal_FinishDrawingCamera(camera, drawGizmos);
        }

        // Draws a camera inside a rectangle.
        // It also sets up the (for now, anyways) undocumented Event.current.mouseRay and Event.current.lastMouseRay for handleutility functions.
        //
        public static void DrawCamera(Rect position, Camera camera)
        {
            DrawCamera(position, camera, DrawCameraMode.Normal);
        }

        public static void DrawCamera(Rect position, Camera camera, [DefaultValue("UnityEditor.DrawCameraMode.Normal")] DrawCameraMode drawMode)
        {
            DrawCamera(position, camera, drawMode, true);
        }

        public static void DrawCamera(Rect position, Camera camera, [DefaultValue("UnityEditor.DrawCameraMode.Normal")] DrawCameraMode drawMode, bool drawGizmos)
        {
            DrawGridParameters nullGridParam = new DrawGridParameters();
            DrawCameraImpl(position, camera, drawMode, false, nullGridParam, true, drawGizmos);
        }

        internal enum CameraFilterMode
        {
            Off = 0,
            ShowFiltered = 1
        }

        /// *listonly*
        public static void SetCamera(Camera camera)
        {
            if (Event.current.type == EventType.Repaint)
                Internal_SetupCamera(camera);
            else
                Internal_SetCurrentCamera(camera);
        }

        // Set the current camera so all Handles and Gizmos are draw with its settings.
        public static void SetCamera(Rect position, Camera camera)
        {
            Rect screenRect = GUIClip.Unclip(position);

            screenRect = EditorGUIUtility.PointsToPixels(screenRect);

            Rect cameraRect = new Rect(screenRect.xMin, Screen.height - screenRect.yMax, screenRect.width, screenRect.height);
            camera.pixelRect = cameraRect;

            Event evt = Event.current;

            if (evt.type == EventType.Repaint)
                Internal_SetupCamera(camera);
            else
                Internal_SetCurrentCamera(camera);
        }

        ///*listonly*
        public static void BeginGUI()
        {
            if (Camera.current && Event.current.type == EventType.Repaint)
            {
                GUIClip.Reapply();
            }
        }

        // Begin a 2D GUI block inside the 3D handle GUI.
        [Obsolete("Please use BeginGUI() with GUILayout.BeginArea(position) / GUILayout.EndArea()")]
        public static void BeginGUI(Rect position)
        {
            GUILayout.BeginArea(position);
        }

        // End a 2D GUI block and get back to the 3D handle GUI.
        public static void EndGUI()
        {
            Camera cam = Camera.current;
            if (cam && Event.current.type == EventType.Repaint)
                Internal_SetupCamera(cam);
        }

        internal static void ShowSceneViewLabel(Vector3 pos, GUIContent label)
        {
            Handles.color = Color.white;
            Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
            GUIStyle style = "SC ViewAxisLabel";
            style.alignment = TextAnchor.MiddleLeft;
            style.fixedWidth = 0;
            Handles.BeginGUI();
            Rect rect = HandleUtility.WorldPointToSizedRect(pos, label, style);
            rect.x += 10;
            rect.y += 10;
            GUI.Label(rect, label, style);
            Handles.EndGUI();
        }

        public static Vector3[] MakeBezierPoints(Vector3 startPosition, Vector3 endPosition, Vector3 startTangent, Vector3 endTangent, int division)
        {
            if (division < 1)
                throw new ArgumentOutOfRangeException("division", "Must be greater than zero");
            return Internal_MakeBezierPoints(startPosition, endPosition, startTangent, endTangent, division);
        }

        public static void DrawTexture3DSDF(Texture texture,
            [DefaultValue("1.0f")] float stepScale = 1.0f,
            [DefaultValue("0.0f")] float surfaceOffset = 0.0f,
            [DefaultValue("null")] Gradient customColorRamp = null)
        {
            Vector3 localScale = new Vector3(matrix.GetColumn(0).magnitude, matrix.GetColumn(1).magnitude, matrix.GetColumn(2).magnitude);
            Texture3DInspector.PrepareSDFPreview(Texture3DInspector.Materials.SDF, texture, localScale, stepScale, surfaceOffset, customColorRamp);
            Texture3DInspector.Materials.SDF.SetPass(0);
            Graphics.DrawMeshNow(cubeMesh, matrix);
        }

        public static void DrawTexture3DSlice(Texture texture, Vector3 slicePositions,
            [DefaultValue("FilterMode.Bilinear")] FilterMode filterMode = FilterMode.Bilinear,
            [DefaultValue("false")] bool useColorRamp = false,
            [DefaultValue("null")] Gradient customColorRamp = null)
        {
            Vector3 localScale = new Vector3(matrix.GetColumn(0).magnitude, matrix.GetColumn(1).magnitude, matrix.GetColumn(2).magnitude);
            Texture3DInspector.PrepareSlicePreview(Texture3DInspector.Materials.Slice, texture, slicePositions, filterMode, useColorRamp, customColorRamp);
            Texture3DInspector.Materials.Slice.SetPass(0);
            Graphics.DrawMeshNow(cubeMesh, matrix);
        }

        public static void DrawTexture3DVolume(Texture texture,
            [DefaultValue("1.0f")] float opacity = 1.0f,
            [DefaultValue("1.0f")] float qualityModifier = 1.0f,
            [DefaultValue("FilterMode.Bilinear")] FilterMode filterMode = FilterMode.Bilinear,
            [DefaultValue("false")] bool useColorRamp = false,
            [DefaultValue("null")] Gradient customColorRamp = null)
        {
            Vector3 localScale = new Vector3(matrix.GetColumn(0).magnitude, matrix.GetColumn(1).magnitude, matrix.GetColumn(2).magnitude);
            int sampleCount = Texture3DInspector.PrepareVolumePreview(Texture3DInspector.Materials.Volume, texture, localScale, opacity,
                filterMode, useColorRamp, customColorRamp, Camera.current, matrix, qualityModifier);
            Texture3DInspector.Materials.Volume.SetPass(0);
            Graphics.DrawProceduralNow(MeshTopology.Quads, 4, sampleCount);
        }
    }
}
