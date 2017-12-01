// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor
{
    public sealed partial class Handles
    {
        internal struct PositionHandleIds
        {
            public static PositionHandleIds @default
            {
                get
                {
                    return new PositionHandleIds(
                        GUIUtility.GetControlID(s_xAxisMoveHandleHash, FocusType.Passive),
                        GUIUtility.GetControlID(s_yAxisMoveHandleHash, FocusType.Passive),
                        GUIUtility.GetControlID(s_zAxisMoveHandleHash, FocusType.Passive),
                        GUIUtility.GetControlID(s_xyAxisMoveHandleHash, FocusType.Passive),
                        GUIUtility.GetControlID(s_xzAxisMoveHandleHash, FocusType.Passive),
                        GUIUtility.GetControlID(s_yzAxisMoveHandleHash, FocusType.Passive),
                        GUIUtility.GetControlID(s_FreeMoveHandleHash, FocusType.Passive)
                        );
                }
            }

            public readonly int x, y, z, xy, yz, xz, xyz;

            public int this[int index]
            {
                get
                {
                    switch (index)
                    {
                        case 0: return x;
                        case 1: return y;
                        case 2: return z;
                        case 3: return xy;
                        case 4: return yz;
                        case 5: return xz;
                        case 6: return xyz;
                    }
                    return -1;
                }
            }

            public bool Has(int id)
            {
                return x == id
                    || y == id
                    || z == id
                    || xy == id
                    || yz == id
                    || xz == id
                    || xyz == id;
            }

            public PositionHandleIds(int x, int y, int z, int xy, int xz, int yz, int xyz)
            {
                this.x = x;
                this.y = y;
                this.z = z;
                this.xy = xy;
                this.yz = yz;
                this.xz = xz;
                this.xyz = xyz;
            }

            public override int GetHashCode()
            {
                return x ^ y ^ z ^ xy ^ xz ^ yz ^ xyz;
            }

            public override bool Equals(object obj)
            {
                if (!(obj is PositionHandleIds))
                    return false;

                var o = (PositionHandleIds)obj;
                return o.x == x && o.y == y && o.z == z
                    && o.xy == xy && o.xz == xz && o.yz == yz
                    && o.xyz == xyz;
            }
        }

        internal struct PositionHandleParam
        {
            public static PositionHandleParam DefaultHandle = new PositionHandleParam(
                    Handle.X | Handle.Y | Handle.Z | Handle.XY | Handle.XZ | Handle.YZ,
                    Vector3.zero, Vector3.one, Vector3.zero, Vector3.one * 0.25f,
                    Orientation.Signed, Orientation.Camera);
            public static PositionHandleParam DefaultFreeMoveHandle = new PositionHandleParam(
                    Handle.X | Handle.Y | Handle.Z | Handle.XYZ,
                    Vector3.zero, Vector3.one, Vector3.zero, Vector3.one * 0.25f,
                    Orientation.Signed, Orientation.Signed);

            [Flags]
            public enum Handle
            {
                X = 1 << 0,
                Y = 1 << 1,
                Z = 1 << 2,
                XY = 1 << 3,
                YZ = 1 << 4,
                XZ = 1 << 5,
                XYZ = 1 << 6
            }

            public enum Orientation
            {
                Signed,
                Camera
            }

            public readonly Vector3 axisOffset;
            public readonly Vector3 axisSize;
            public readonly Vector3 planeOffset;
            public readonly Vector3 planeSize;
            public readonly Handle handles;
            public readonly Orientation axesOrientation;
            public readonly Orientation planeOrientation;

            public bool ShouldShow(int axis)
            {
                return (handles & (Handle)(1 << axis)) != 0;
            }

            public bool ShouldShow(Handle handle)
            {
                return (handles & handle) != 0;
            }

            public PositionHandleParam(
                Handle handles,
                Vector3 axisOffset,
                Vector3 axisSize,
                Vector3 planeOffset,
                Vector3 planeSize,
                Orientation axesOrientation,
                Orientation planeOrientation)
            {
                this.axisOffset = axisOffset;
                this.axisSize = axisSize;
                this.planeOffset = planeOffset;
                this.planeSize = planeSize;
                this.handles = handles;
                this.axesOrientation = axesOrientation;
                this.planeOrientation = planeOrientation;
            }
        }

        static Vector3[] verts = {Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero};

        const float kFreeMoveHandleSizeFactor = 0.15f;

        // While the user has Free Move mode turned on by holding 'shift' or 'V' (for Vertex Snapping),
        // this variable will be set to True.
        static bool s_FreeMoveMode = false;

        // Which octant the planar move handles are in.
        static Vector3 s_PlanarHandlesOctant = Vector3.one;
        static Vector3 s_DoPositionHandle_AxisHandlesOctant = Vector3.one;

        // If the user is currently mouse dragging then this value will be True
        // and will disallow toggling Free Move mode on or off, or changing the octant of the planar handles.
        static bool currentlyDragging { get { return EditorGUIUtility.hotControl != 0; } }

        static Vector3 s_DoPositionHandle_ArrowCapConeOffset = Vector3.zero;

        public static Vector3 DoPositionHandle(Vector3 position, Quaternion rotation)
        {
            return DoPositionHandle(PositionHandleIds.@default, position, rotation);
        }

        internal static Vector3 DoPositionHandle(PositionHandleIds ids, Vector3 position, Quaternion rotation)
        {
            Event evt = Event.current;
            switch (evt.type)
            {
                case EventType.KeyDown:
                    // Holding 'V' turns on the FreeMove transform gizmo and enables vertex snapping.
                    if (evt.keyCode == KeyCode.V && !currentlyDragging)
                    {
                        s_FreeMoveMode = true;
                    }
                    break;

                case EventType.KeyUp:
                    // If the user has released the 'V' key, then rendering the transform gizmo
                    // one last time with Free Move mode off is technically incorrect since it can
                    // add one additional frame of input with FreeMove enabled, but the
                    // implementation is a fair bit simpler this way.
                    // Basic idea: Leave this call above the 'if' statement.
                    position = DoPositionHandle_Internal(ids, position, rotation, PositionHandleParam.DefaultHandle);
                    if (evt.keyCode == KeyCode.V && !evt.shift && !currentlyDragging)
                    {
                        s_FreeMoveMode = false;
                    }
                    return position;

                case EventType.Layout:
                    if (!currentlyDragging && !Tools.vertexDragging)
                    {
                        s_FreeMoveMode = evt.shift;
                    }
                    break;
            }

            var param = s_FreeMoveMode ? PositionHandleParam.DefaultFreeMoveHandle : PositionHandleParam.DefaultHandle;

            return DoPositionHandle_Internal(ids, position, rotation, param);
        }

        static float[] s_DoPositionHandle_Internal_CameraViewLerp = new float[6];
        static string[] s_DoPositionHandle_Internal_AxisNames = { "xAxis", "yAxis", "zAxis" };
        static int[] s_DoPositionHandle_Internal_NextIndex = { 1, 2, 0 };
        static int[] s_DoPositionHandle_Internal_PrevIndex = { 2, 0, 1 };
        static int[] s_DoPositionHandle_Internal_PrevPlaneIndex = { 5, 3, 4 };
        static Vector3 DoPositionHandle_Internal(PositionHandleIds ids, Vector3 position, Quaternion rotation, PositionHandleParam param)
        {
            Color temp = color;

            bool isStatic = (!Tools.s_Hidden && EditorApplication.isPlaying && GameObjectUtility.ContainsStatic(Selection.gameObjects));

            // Calculate the camera view vector in Handle draw space
            // this handle the case where the matrix is skewed
            var handlePosition = matrix.MultiplyPoint3x4(position);
            var drawToWorldMatrix = matrix * Matrix4x4.TRS(position, rotation, Vector3.one);
            var invDrawToWorldMatrix = drawToWorldMatrix.inverse;
            var viewVectorDrawSpace = GetCameraViewFrom(handlePosition, invDrawToWorldMatrix);

            var size = HandleUtility.GetHandleSize(position);

            // Calculate per axis camera lerp
            for (var i = 0; i < 3; ++i)
                s_DoPositionHandle_Internal_CameraViewLerp[i] = ids[i] == GUIUtility.hotControl ? 0 : GetCameraViewLerpForWorldAxis(viewVectorDrawSpace, GetAxisVector(i));
            // Calculate per plane camera lerp (xy, yz, xz)
            for (var i = 0; i < 3; ++i)
                s_DoPositionHandle_Internal_CameraViewLerp[3 + i] = Mathf.Max(s_DoPositionHandle_Internal_CameraViewLerp[i], s_DoPositionHandle_Internal_CameraViewLerp[(i + 1) % 3]);

            var isHot = ids.Has(GUIUtility.hotControl);
            var axisOffset = param.axisOffset;
            var planeOffset = param.planeOffset;
            if (isHot)
            {
                axisOffset = Vector3.zero;
                planeOffset = Vector3.zero;
            }

            // Draw plane handles (xy, yz, xz)
            var planeSize = isHot ? param.planeSize + param.planeOffset : param.planeSize;
            for (var i = 0; i < 3; ++i)
            {
                if (!param.ShouldShow(3 + i) || isHot && ids[3 + i] != GUIUtility.hotControl)
                    continue;

                var cameraLerp = isHot ? 0 : s_DoPositionHandle_Internal_CameraViewLerp[3 + i];
                if (cameraLerp <= kCameraViewThreshold)
                {
                    var offset = planeOffset * size;
                    offset[s_DoPositionHandle_Internal_PrevIndex[i]] = 0;
                    var planarSize = Mathf.Max(planeSize[i], planeSize[s_DoPositionHandle_Internal_NextIndex[i]]);
                    position = DoPlanarHandle(ids[3 + i], i, position, offset, rotation, size * planarSize, cameraLerp, viewVectorDrawSpace, param.planeOrientation);
                }
            }

            // Draw axis sliders
            // Draw last to have priority over the planes
            for (var i = 0; i < 3; ++i)
            {
                if (!param.ShouldShow(i))
                    continue;

                if (!currentlyDragging)
                {
                    switch (param.axesOrientation)
                    {
                        case PositionHandleParam.Orientation.Camera:
                            s_DoPositionHandle_AxisHandlesOctant[i] = viewVectorDrawSpace[i] > 0.01f ? -1 : 1;
                            break;
                        case PositionHandleParam.Orientation.Signed:
                            s_DoPositionHandle_AxisHandlesOctant[i] = 1;
                            break;
                    }
                }

                var isThisAxisHot = isHot && ids[i] == GUIUtility.hotControl;

                var axisColor = GetColorByAxis(i);
                color = isStatic ? Color.Lerp(axisColor, staticColor, staticBlend) : axisColor;
                GUI.SetNextControlName(s_DoPositionHandle_Internal_AxisNames[i]);

                // if we are hot here, the hot handle must be opaque
                var cameraLerp = isThisAxisHot ? 0 : s_DoPositionHandle_Internal_CameraViewLerp[i];

                if (cameraLerp <= kCameraViewThreshold)
                {
                    color = Color.Lerp(color, Color.clear, cameraLerp);
                    var axisVector = GetAxisVector(i);
                    var dir = rotation * axisVector;
                    var offset = dir * axisOffset[i] * size;

                    dir *= s_DoPositionHandle_AxisHandlesOctant[i];
                    offset *= s_DoPositionHandle_AxisHandlesOctant[i];

                    if (isHot && !isThisAxisHot)
                        color = s_DisabledHandleColor;

                    // A plane with this axis is hot
                    if (isHot && (ids[s_DoPositionHandle_Internal_PrevPlaneIndex[i]] == GUIUtility.hotControl || ids[i + 3] == GUIUtility.hotControl))
                        color = selectedColor;

                    s_DoPositionHandle_ArrowCapConeOffset = isHot
                        ? rotation * Vector3.Scale(Vector3.Scale(axisVector, param.axisOffset), s_DoPositionHandle_AxisHandlesOctant)
                        : Vector3.zero;
                    position = Slider(ids[i], position, offset, dir, size * param.axisSize[i], DoPositionHandle_ArrowCap, GridSnapping.active ? 0f : SnapSettings.move[i]);
                }
            }

            VertexSnapping.HandleKeyAndMouseMove(ids.xyz);
            if (param.ShouldShow(PositionHandleParam.Handle.XYZ) && (isHot && ids.xyz == GUIUtility.hotControl || !isHot))
            {
                color = centerColor;
                GUI.SetNextControlName("FreeMoveAxis");
                position = FreeMoveHandle(ids.xyz, position, rotation, size * kFreeMoveHandleSizeFactor, GridSnapping.active ? Vector3.zero : SnapSettings.move, RectangleHandleCap);
            }

            color = temp;

            if (GridSnapping.active)
                position = GridSnapping.Snap(position);

            return position;
        }

        static Vector3 DoPlanarHandle(
            int id,
            int planePrimaryAxis,
            Vector3 position,
            Vector3 offset,
            Quaternion rotation,
            float handleSize,
            float cameraLerp,
            Vector3 viewVectorDrawSpace,
            PositionHandleParam.Orientation orientation)
        {
            var positionOffset = offset;

            var axis1index = planePrimaryAxis;
            var axis2index = (axis1index + 1) % 3;
            var axisNormalIndex = (axis1index  + 2) % 3;

            Color prevColor = Handles.color;

            bool isStatic = (!Tools.s_Hidden && EditorApplication.isPlaying && GameObjectUtility.ContainsStatic(Selection.gameObjects));
            color = isStatic ? staticColor : GetColorByAxis(axisNormalIndex);
            color = Color.Lerp(color, Color.clear, cameraLerp);

            var updateOpacityFillColor = false;
            if (GUIUtility.hotControl == id)
                color = selectedColor;
            else if (HandleUtility.nearestControl == id)
                color = preselectionColor;
            else
                updateOpacityFillColor = true;


            // NOTE: The planar transform handles always face toward the camera so they won't
            // obscure each other (unlike the X, Y, and Z axis handles which always face in the
            // positive axis directions). Whenever the octant that the camera is in (relative to
            // to the transform tool) changes, we need to move the planar transform handle
            // positions to the correct octant.

            // Comments below assume axis1 is X and axis2 is Z to make it easier to visualize things.

            // Shift the planar transform handle in the positive direction by half its
            // handleSize so that it doesn't overlap in the center of the transform gizmo,
            // and also move the handle origin into the octant that the camera is in.
            // Don't update the actant while dragging to avoid too much distraction.
            if (!currentlyDragging)
            {
                switch (orientation)
                {
                    case PositionHandleParam.Orientation.Camera:
                        // Offset the X position of the handle in negative direction if camera is in the -X octants; otherwise positive.
                        // Test against -0.01 instead of 0 to give a little bias to the positive quadrants. This looks better in axis views.
                        s_PlanarHandlesOctant[axis1index] = (viewVectorDrawSpace[axis1index] > 0.01f ? -1 : 1);
                        // Likewise with the other axis.
                        s_PlanarHandlesOctant[axis2index] = (viewVectorDrawSpace[axis2index] > 0.01f ? -1 : 1);
                        break;
                    case PositionHandleParam.Orientation.Signed:
                        s_PlanarHandlesOctant[axis1index] = 1;
                        s_PlanarHandlesOctant[axis2index] = 1;
                        break;
                }
            }
            Vector3 handleOffset = s_PlanarHandlesOctant;
            // Zero out the offset along the normal axis.
            handleOffset[axisNormalIndex] = 0;
            positionOffset = rotation * Vector3.Scale(positionOffset, handleOffset);
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
            verts[0] = position + positionOffset + handleOffset + (axis1 + axis2) * handleSize * 0.5f;
            verts[1] = position + positionOffset + handleOffset + (-axis1 + axis2) * handleSize * 0.5f;
            verts[2] = position + positionOffset + handleOffset + (-axis1 - axis2) * handleSize * 0.5f;
            verts[3] = position + positionOffset + handleOffset + (axis1 - axis2) * handleSize * 0.5f;
            Handles.DrawSolidRectangleWithOutline(verts, updateOpacityFillColor ? new Color(color.r, color.g, color.b, 0.1f) : color, Color.clear);

            // And then render the handle itself (this is the colored outline)
            position = Slider2D(id,
                    position,
                    handleOffset + positionOffset,
                    axisNormal,
                    axis1, axis2,
                    handleSize * 0.5f,
                    RectangleHandleCap,
                    GridSnapping.active ? Vector2.zero : new Vector2(SnapSettings.move[axis1index], SnapSettings.move[axis2index]),
                    false);

            Handles.color = prevColor;

            return position;
        }

        static void DoPositionHandle_ArrowCap(int controlId, Vector3 position, Quaternion rotation, float size, EventType eventType)
        {
            ArrowHandleCap(controlId, position, rotation, size, eventType, s_DoPositionHandle_ArrowCapConeOffset);
        }
    }
}
