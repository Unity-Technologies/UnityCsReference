// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor
{
    public sealed partial class Handles
    {
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

        static Mesh cubeMesh
        {
            get
            {
                if (s_CubeMesh == null)
                    Init();
                return s_CubeMesh;
            }
        }

        static Mesh coneMesh
        {
            get
            {
                if (s_ConeMesh == null)
                    Init();
                return s_ConeMesh;
            }
        }

        static Mesh cylinderMesh
        {
            get
            {
                if (s_CylinderMesh == null)
                    Init();
                return s_CylinderMesh;
            }
        }

        static Mesh quadMesh
        {
            get
            {
                if (s_QuadMesh == null)
                    Init();
                return s_QuadMesh;
            }
        }

        static Mesh sphereMesh
        {
            get
            {
                if (s_SphereMesh == null)
                    Init();
                return s_SphereMesh;
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

        internal const float kCameraViewLerpStart = 0.85f;
        internal const float kCameraViewThreshold = 0.9f;
        internal const float kCameraViewLerpSpeed = 1f / (1 - kCameraViewLerpStart);

        // The function for calling AddControl in Layout event and draw the handle in Repaint event.
        public delegate void CapFunction(int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType);

        // Signatures expecting DrawCapFunction were marked plannned obsolete by @juha on 2016-03-16, marked obsolete warning by @adamm on 2016-12-21
        [Obsolete("This delegate is obsolete. Use CapFunction instead.")]
        public delegate void DrawCapFunction(int controlID, Vector3 position, Quaternion rotation, float size);

        public delegate float SizeFunction(Vector3 position);

        static PrefColor[] s_AxisColor = { s_XAxisColor, s_YAxisColor, s_ZAxisColor };
        static Vector3[] s_AxisVector = { Vector3.right, Vector3.up, Vector3.forward };

        internal static Color s_DisabledHandleColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);

        internal static Color GetColorByAxis(int axis)
        {
            return s_AxisColor[axis];
        }

        static Vector3 GetAxisVector(int axis)
        {
            return s_AxisVector[axis];
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

        [Obsolete("DrawCapFunction is obsolete. Use the version with CapFunction instead. Example: Change SphereCap to SphereHandleCap.")]
        public static Vector3 Slider(Vector3 position, Vector3 direction, float size, DrawCapFunction drawFunc, float snap)
        {
            int id = GUIUtility.GetControlID(s_SliderHash, FocusType.Passive);
            return UnityEditorInternal.Slider1D.Do(id, position, direction, size, drawFunc, snap);
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

        [Obsolete("DrawCapFunction is obsolete. Use the version with CapFunction instead. Example: Change SphereCap to SphereHandleCap.")]
        public static Vector3 FreeMoveHandle(Vector3 position, Quaternion rotation, float size, Vector3 snap, DrawCapFunction capFunc)
        {
            int id = GUIUtility.GetControlID(s_FreeMoveHandleHash, FocusType.Passive);
            return UnityEditorInternal.FreeMove.Do(id, position, rotation, size, snap, capFunc);
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

        [Obsolete("DrawCapFunction is obsolete. Use the version with CapFunction instead. Example: Change SphereCap to SphereHandleCap.")]
        public static float ScaleValueHandle(float value, Vector3 position, Quaternion rotation, float size, DrawCapFunction capFunc, float snap)
        {
            int id = GUIUtility.GetControlID(s_ScaleValueHandleHash, FocusType.Passive);
            return UnityEditorInternal.SliderScale.DoCenter(id, value, position, rotation, size, capFunc, snap);
        }

        // Make a 3D Button.
        public static bool Button(Vector3 position, Quaternion direction, float size, float pickSize, CapFunction capFunction)
        {
            int id = GUIUtility.GetControlID(s_ButtonHash, FocusType.Passive);
            return UnityEditorInternal.Button.Do(id, position, direction, size, pickSize, capFunction);
        }

        [Obsolete("DrawCapFunction is obsolete. Use the version with CapFunction instead. Example: Change SphereCap to SphereHandleCap.")]
        public static bool Button(Vector3 position, Quaternion direction, float size, float pickSize, DrawCapFunction capFunc)
        {
            int id = GUIUtility.GetControlID(s_ButtonHash, FocusType.Passive);
            return UnityEditorInternal.Button.Do(id, position, direction, size, pickSize, capFunc);
        }

        internal static bool Button(int controlID, Vector3 position, Quaternion direction, float size, float pickSize, CapFunction capFunction)
        {
            return UnityEditorInternal.Button.Do(controlID, position, direction, size, pickSize, capFunction);
        }

        [Obsolete("DrawCapFunction is obsolete. Use the version with CapFunction instead. Example: Change SphereCap to SphereHandleCap.")]
        internal static bool Button(int controlID, Vector3 position, Quaternion direction, float size, float pickSize, DrawCapFunction capFunc)
        {
            return UnityEditorInternal.Button.Do(controlID, position, direction, size, pickSize, capFunc);
        }

        // Draw a cube. Pass this into handle functions.
        public static void CubeHandleCap(int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType)
        {
            switch (eventType)
            {
                case (EventType.Layout):
                    // TODO: Create DistanceToCube
                    HandleUtility.AddControl(controlID, HandleUtility.DistanceToCircle(position, size));
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
                case (EventType.Layout):
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
                case (EventType.Layout):
                    // TODO: Create DistanceToCone
                    HandleUtility.AddControl(controlID, HandleUtility.DistanceToCircle(position, size));
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
                case (EventType.Layout):
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
                case (EventType.Layout):
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

        // Draw a camera-facing dot. Pass this into handle functions.
        public static void DotHandleCap(int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType)
        {
            switch (eventType)
            {
                case (EventType.Layout):
                    HandleUtility.AddControl(controlID, HandleUtility.DistanceToRectangle(position, rotation, size));
                    break;
                case (EventType.Repaint):
                    // Only apply matrix to the position because DotCap is camera facing
                    position = matrix.MultiplyPoint(position);

                    Vector3 sideways = Camera.current.transform.right * size;
                    Vector3 up = Camera.current.transform.up * size;

                    Color col = color * new Color(1, 1, 1, 0.99f);
                    HandleUtility.ApplyWireMaterial();
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
                case (EventType.Layout):
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
                case (EventType.Layout):
                {
                    Vector3 direction = rotation * Vector3.forward;
                    HandleUtility.AddControl(controlID, HandleUtility.DistanceToLine(position, position + (direction + coneOffset) * size * .9f));
                    HandleUtility.AddControl(controlID, HandleUtility.DistanceToCircle(position + (direction + coneOffset) * size, size * .2f));
                    break;
                }
                case (EventType.Repaint):
                {
                    Vector3 direction = rotation * Vector3.forward;
                    ConeHandleCap(controlID, position + (direction + coneOffset) * size, Quaternion.LookRotation(direction), size * .2f, eventType);
                    Handles.DrawLine(position, position + (direction + coneOffset) * size * .9f, false);
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

        internal static float GetCameraViewLerpForWorldAxis(Vector3 viewVector, Vector3 axis)
        {
            return
                Mathf.Clamp01(kCameraViewLerpSpeed *
                (Mathf.Abs(Vector3.Dot(viewVector, axis)) - kCameraViewLerpStart));
        }

        internal static Vector3 GetCameraViewFrom(Vector3 position, Matrix4x4 matrix)
        {
            Camera camera = Camera.current;
            return camera.orthographic
                ? matrix.MultiplyVector(-camera.transform.forward).normalized
                : matrix.MultiplyVector(position - camera.transform.position).normalized;
        }
    }
}
