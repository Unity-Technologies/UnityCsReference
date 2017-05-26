// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    public sealed partial class Handles
    {
        static Vector3[] verts = {Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero};

        const float kPlanarHandleSizeFactor = 0.25f;
        const float kFreeMoveHandleSizeFactor = 0.15f;

        // While the user has Free Move mode turned on by holding 'shift' or 'V' (for Vertex Snapping),
        // this variable will be set to True.
        static bool s_FreeMoveMode = false;

        // Which octant the planar move handles are in.
        static Vector3 s_PlanarHandlesOctant = Vector3.one;

        // If the user is currently mouse dragging then this value will be True
        // and will disallow toggling Free Move mode on or off, or changing the octant of the planar handles.
        static bool currentlyDragging { get { return EditorGUIUtility.hotControl != 0; } }

        enum PlaneHandle
        {
            xzPlane,
            xyPlane,
            yzPlane
        };

        public static Vector3 DoPositionHandle(Vector3 position, Quaternion rotation)
        {
            Event evt = Event.current;
            switch (evt.type)
            {
                case EventType.keyDown:
                    // Holding 'V' turns on the FreeMove transform gizmo and enables vertex snapping.
                    if (evt.keyCode == KeyCode.V && !currentlyDragging)
                    {
                        s_FreeMoveMode = true;
                    }
                    break;

                case EventType.keyUp:
                    // If the user has released the 'V' key, then rendering the transform gizmo
                    // one last time with Free Move mode off is technically incorrect since it can
                    // add one additional frame of input with FreeMove enabled, but the
                    // implementation is a fair bit simpler this way.
                    // Basic idea: Leave this call above the 'if' statement.
                    position = DoPositionHandle_Internal(position, rotation);
                    if (evt.keyCode == KeyCode.V && !evt.shift && !currentlyDragging)
                    {
                        s_FreeMoveMode = false;
                    }
                    return position;

                case EventType.layout:
                    if (!currentlyDragging && !Tools.vertexDragging)
                    {
                        s_FreeMoveMode = evt.shift;
                    }
                    break;
            }
            return DoPositionHandle_Internal(position, rotation);
        }

        static Vector3 DoPositionHandle_Internal(Vector3 position, Quaternion rotation)
        {
            // We must call ALL of the GetControlIDs call here to get consistent IDs
            // If one call is skipped from time to time, IDs will not be consistent
            var xId = GUIUtility.GetControlID(s_xAxisMoveHandleHash, FocusType.Passive);
            var yId = GUIUtility.GetControlID(s_yAxisMoveHandleHash, FocusType.Passive);
            var zId = GUIUtility.GetControlID(s_zAxisMoveHandleHash, FocusType.Passive);
            var xzId = GUIUtility.GetControlID(s_xzAxisMoveHandleHash, FocusType.Passive);
            var xyId = GUIUtility.GetControlID(s_xyAxisMoveHandleHash, FocusType.Passive);
            var yzId = GUIUtility.GetControlID(s_yzAxisMoveHandleHash, FocusType.Passive);
            var freeMoveId = GUIUtility.GetControlID(s_FreeMoveHandleHash, FocusType.Passive);

            // Calculate the camera view vector in Handle draw space
            // this handle the case where the matrix is skewed
            var handlePosition = matrix.MultiplyPoint3x4(position);
            var drawToWorldMatrix = matrix * Matrix4x4.TRS(position, rotation, Vector3.one);
            var invDrawToWorldMatrix = drawToWorldMatrix.inverse;
            var viewVectorDrawSpace = GetCameraViewFrom(handlePosition, invDrawToWorldMatrix);

            float size = HandleUtility.GetHandleSize(position);
            var xCameraViewLerp = xId == GUIUtility.hotControl ? 0 : GetCameraViewLerpForWorldAxis(viewVectorDrawSpace, Vector3.right);
            var yCameraViewLerp = yId == GUIUtility.hotControl ? 0 : GetCameraViewLerpForWorldAxis(viewVectorDrawSpace, Vector3.up);
            var zCameraViewLerp = zId == GUIUtility.hotControl ? 0 : GetCameraViewLerpForWorldAxis(viewVectorDrawSpace, Vector3.forward);

            Color temp = color;
            bool isStatic = (!Tools.s_Hidden && EditorApplication.isPlaying && GameObjectUtility.ContainsStatic(Selection.gameObjects));

            color = isStatic ? Color.Lerp(xAxisColor, staticColor, staticBlend) : xAxisColor;
            GUI.SetNextControlName("xAxis");
            if (xCameraViewLerp <= kCameraViewThreshold)
            {
                color = Color.Lerp(color, Color.clear, xCameraViewLerp);
                position = Slider(xId, position, rotation * Vector3.right, size, ArrowHandleCap, SnapSettings.move.x);
            }

            color = isStatic ? Color.Lerp(yAxisColor, staticColor, staticBlend) : yAxisColor;
            GUI.SetNextControlName("yAxis");
            if (yCameraViewLerp <= kCameraViewThreshold)
            {
                color = Color.Lerp(color, Color.clear, yCameraViewLerp);
                position = Slider(yId, position, rotation * Vector3.up, size, ArrowHandleCap, SnapSettings.move.y);
            }

            color = isStatic ? Color.Lerp(zAxisColor, staticColor, staticBlend) : zAxisColor;
            GUI.SetNextControlName("zAxis");
            if (zCameraViewLerp <= kCameraViewThreshold)
            {
                color = Color.Lerp(color, Color.clear, zCameraViewLerp);
                position = Slider(zId, position, rotation * Vector3.forward, size, ArrowHandleCap, SnapSettings.move.z);
            }

            if (s_FreeMoveMode)
            {
                color = centerColor;
                GUI.SetNextControlName("FreeMoveAxis");
                position = FreeMoveHandle(freeMoveId, position, rotation, size * kFreeMoveHandleSizeFactor, SnapSettings.move, RectangleHandleCap);
            }
            else
            {
                var xzCameraViewLerp = Mathf.Max(xCameraViewLerp, zCameraViewLerp);
                var xyCameraViewLerp = Mathf.Max(xCameraViewLerp, yCameraViewLerp);
                var yzCameraViewLerp = Mathf.Max(yCameraViewLerp, zCameraViewLerp);

                if (xzCameraViewLerp <= kCameraViewThreshold)
                    position = DoPlanarHandle(xzId, PlaneHandle.xzPlane, position, rotation, size * kPlanarHandleSizeFactor, xzCameraViewLerp);
                if (xyCameraViewLerp <= kCameraViewThreshold)
                    position = DoPlanarHandle(xyId, PlaneHandle.xyPlane, position, rotation, size * kPlanarHandleSizeFactor, xyCameraViewLerp);
                if (yzCameraViewLerp <= kCameraViewThreshold)
                    position = DoPlanarHandle(yzId, PlaneHandle.yzPlane, position, rotation, size * kPlanarHandleSizeFactor, yzCameraViewLerp);
            }

            color = temp;

            return position;
        }

        static Vector3 DoPlanarHandle(int id, PlaneHandle planeID, Vector3 position, Quaternion rotation, float handleSize, float cameraLerp)
        {
            int axis1index = 0;
            int axis2index = 0;
            bool isStatic = (!Tools.s_Hidden && EditorApplication.isPlaying && GameObjectUtility.ContainsStatic(Selection.gameObjects));
            switch (planeID)
            {
                case PlaneHandle.xzPlane:
                {
                    axis1index = 0;
                    axis2index = 2;
                    color = isStatic ? staticColor : yAxisColor;
                    break;
                }
                case PlaneHandle.xyPlane:
                {
                    axis1index = 0;
                    axis2index = 1;
                    color = isStatic ? staticColor : zAxisColor;
                    break;
                }
                case PlaneHandle.yzPlane:
                {
                    axis1index = 1;
                    axis2index = 2;
                    color = isStatic ? staticColor : xAxisColor;
                    break;
                }
            }

            color = Color.Lerp(color, Color.clear, cameraLerp);

            int axisNormalIndex = 3 - axis2index - axis1index;
            Color prevColor = Handles.color;

            // NOTE: The planar transform handles always face toward the camera so they won't
            // obscure each other (unlike the X, Y, and Z axis handles which always face in the
            // positive axis directions). Whenever the octant that the camera is in (relative to
            // to the transform tool) changes, we need to move the planar transform handle
            // positions to the correct octant.

            Matrix4x4 handleTransform = Matrix4x4.TRS(position, rotation, Vector3.one);
            Vector3 cameraToTransformToolVector;
            if (Camera.current == null)
                return position;
            if (Camera.current.orthographic)
                cameraToTransformToolVector = handleTransform.inverse.MultiplyVector(Camera.current.transform.rotation * -Vector3.forward).normalized;
            else
                cameraToTransformToolVector = handleTransform.inverse.MultiplyPoint(Camera.current.transform.position).normalized;

            // Cull handling this rect if it's almost entirely flat on the screen (except if it's currently being dragged)
            if (Mathf.Abs(cameraToTransformToolVector[axisNormalIndex]) < 0.05f && GUIUtility.hotControl != id)
            {
                Handles.color = prevColor;
                return position;
            }

            // Comments below assume axis1 is X and axis2 is Z to make it easier to visualize things.

            // Shift the planar transform handle in the positive direction by half its
            // size so that it doesn't overlap in the center of the transform gizmo,
            // and also move the handle origin into the octant that the camera is in.
            // Don't update the actant while dragging to avoid too much distraction.
            if (!currentlyDragging)
            {
                // Offset the X position of the handle in negative direction if camera is in the -X octants; otherwise positive.
                // Test against -0.01 instead of 0 to give a little bias to the positive quadrants. This looks better in axis views.
                s_PlanarHandlesOctant[axis1index] = (cameraToTransformToolVector[axis1index] < -0.01f ? -1 : 1);
                // Likewise with the other axis.
                s_PlanarHandlesOctant[axis2index] = (cameraToTransformToolVector[axis2index] < -0.01f ? -1 : 1);
            }
            Vector3 handleOffset = s_PlanarHandlesOctant;
            // Zero out the offset along the normal axis.
            handleOffset[axisNormalIndex] = 0;
            // Rotate and scale the offset
            handleOffset = rotation * (handleOffset * handleSize * 0.5f);

            // Calculate 3 axes
            Vector3 axis1 = Vector3.zero;
            Vector3 axis2 = Vector3.zero;
            Vector3 axisNormal = Vector3.zero;
            axis1[axis1index] = 1;
            axis2[axis2index] = 1;
            axisNormal[axisNormalIndex] = 1;
            axis1 = rotation * axis1;
            axis2 = rotation * axis2;
            axisNormal = rotation * axisNormal;

            // Draw the "filler" color for the handle
            verts[0] = position + handleOffset + (axis1 + axis2) * handleSize * 0.5f;
            verts[1] = position + handleOffset + (-axis1 + axis2) * handleSize * 0.5f;
            verts[2] = position + handleOffset + (-axis1 - axis2) * handleSize * 0.5f;
            verts[3] = position + handleOffset + (axis1 - axis2) * handleSize * 0.5f;
            Handles.DrawSolidRectangleWithOutline(verts, new Color(color.r, color.g, color.b, 0.1f), new Color(0f, 0f, 0f, 0.0f));

            // And then render the handle itself (this is the colored outline)
            position = Slider2D(id,
                    position,
                    handleOffset,
                    axisNormal,
                    axis1, axis2,
                    handleSize * 0.5f,
                    RectangleHandleCap,
                    new Vector2(SnapSettings.move[axis1index], SnapSettings.move[axis2index]));

            Handles.color = prevColor;

            return position;
        }
    }
}
