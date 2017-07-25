// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor
{
    public sealed partial class Handles
    {
        internal struct ScaleHandleIds
        {
            public static ScaleHandleIds @default
            {
                get
                {
                    return new ScaleHandleIds(
                        GUIUtility.GetControlID(s_xScaleHandleHash, FocusType.Passive),
                        GUIUtility.GetControlID(s_yScaleHandleHash, FocusType.Passive),
                        GUIUtility.GetControlID(s_zScaleHandleHash, FocusType.Passive),
                        GUIUtility.GetControlID(s_xyzScaleHandleHash, FocusType.Passive)
                        );
                }
            }

            public readonly int x, y, z, xyz;

            public int this[int index]
            {
                get
                {
                    switch (index)
                    {
                        case 0: return x;
                        case 1: return y;
                        case 2: return z;
                        case 3: return xyz;
                    }
                    return -1;
                }
            }

            public bool Has(int id)
            {
                return x == id
                    || y == id
                    || z == id
                    || xyz == id;
            }

            public ScaleHandleIds(int x, int y, int z, int xyz)
            {
                this.x = x;
                this.y = y;
                this.z = z;
                this.xyz = xyz;
            }

            public override int GetHashCode()
            {
                return x ^ y ^ z ^ xyz;
            }

            public override bool Equals(object obj)
            {
                if (!(obj is ScaleHandleIds))
                    return false;

                var o = (ScaleHandleIds)obj;
                return o.x == x && o.y == y && o.z == z
                    && o.xyz == xyz;
            }
        }

        internal struct ScaleHandleParam
        {
            [Flags]
            public enum Handle
            {
                X = 1 << 0,
                Y = 1 << 1,
                Z = 1 << 2,
                XYZ = 1 << 3
            }

            public enum Orientation
            {
                Signed,
                Camera
            }

            static ScaleHandleParam s_Default = new ScaleHandleParam((Handle)(-1), Vector3.zero, Vector3.one, Vector3.one, 1, Orientation.Signed);
            public static ScaleHandleParam Default { get { return s_Default; } set { s_Default = value; } }

            public readonly Vector3 axisOffset;
            public readonly Vector3 axisSize;
            public readonly Vector3 axisLineScale;
            public readonly float xyzSize;
            public readonly Handle handles;
            public readonly Orientation orientation;

            public bool ShouldShow(int axis)
            {
                return (handles & (Handle)(1 << axis)) != 0;
            }

            public bool ShouldShow(Handle handle)
            {
                return (handles & handle) != 0;
            }

            public ScaleHandleParam(Handle handles, Vector3 axisOffset, Vector3 axisSize, Vector3 axisLineScale, float xyzSize, Orientation orientation)
            {
                this.axisOffset = axisOffset;
                this.axisSize = axisSize;
                this.axisLineScale = axisLineScale;
                this.xyzSize = xyzSize;
                this.handles = handles;
                this.orientation = orientation;
            }
        }

        static Vector3 s_DoScaleHandle_AxisHandlesOctant = Vector3.one;

        public static Vector3 DoScaleHandle(Vector3 scale, Vector3 position, Quaternion rotation, float size)
        {
            return DoScaleHandle(ScaleHandleIds.@default, scale, position, rotation, size, ScaleHandleParam.Default);
        }

        internal static Vector3 DoScaleHandle(ScaleHandleIds ids, Vector3 scale, Vector3 position, Quaternion rotation, float handleSize, ScaleHandleParam param)
        {
            // Calculate the camera view vector in Handle draw space
            // this handle the case where the matrix is skewed
            var handlePosition = matrix.MultiplyPoint3x4(position);
            var drawToWorldMatrix = matrix * Matrix4x4.TRS(position, rotation, Vector3.one);
            var invDrawToWorldMatrix = drawToWorldMatrix.inverse;
            var viewVectorDrawSpace = GetCameraViewFrom(handlePosition, invDrawToWorldMatrix);

            var isStatic = (!Tools.s_Hidden && EditorApplication.isPlaying && GameObjectUtility.ContainsStatic(Selection.gameObjects));

            var isHot = ids.Has(GUIUtility.hotControl);

            var axisOffset = param.axisOffset;
            var axisLineScale = param.axisLineScale;
            // When an axis is hot, draw the line from the center to the handle
            // So ignore the offset
            if (isHot)
            {
                axisLineScale += axisOffset;
                axisOffset = Vector3.zero;
            }

            var isCenterIsHot = ids.xyz == GUIUtility.hotControl;

            for (var i = 0; i < 3; ++i)
            {
                if (!param.ShouldShow(i))
                    continue;

                if (!currentlyDragging)
                {
                    switch (param.orientation)
                    {
                        case ScaleHandleParam.Orientation.Signed:
                            s_DoScaleHandle_AxisHandlesOctant[i] = 1;
                            break;
                        case ScaleHandleParam.Orientation.Camera:
                            s_DoScaleHandle_AxisHandlesOctant[i] = viewVectorDrawSpace[i] > 0.01f ? -1 : 1;
                            break;
                    }
                }


                var id = ids[i];
                var isThisAxisHot = isHot && id == GUIUtility.hotControl;

                var axisDir = GetAxisVector(i);
                var axisColor = GetColorByAxis(i);
                var offset = axisOffset[i];
                var cameraLerp = id == GUIUtility.hotControl ? 0 : GetCameraViewLerpForWorldAxis(viewVectorDrawSpace, axisDir);
                // If we are here and is hot, then this axis is hot and must be opaque
                cameraLerp = isHot ? 0 : cameraLerp;
                color = isStatic ? Color.Lerp(axisColor, staticColor, staticBlend) : axisColor;

                axisDir *= s_DoScaleHandle_AxisHandlesOctant[i];

                if (cameraLerp <= kCameraViewThreshold)
                {
                    color = Color.Lerp(color, Color.clear, cameraLerp);

                    if (isHot && !isThisAxisHot)
                        color = s_DisabledHandleColor;
                    if (isCenterIsHot)
                        color = selectedColor;

                    scale[i] = UnityEditorInternal.SliderScale.DoAxis(
                            id,
                            scale[i],
                            position,
                            rotation * axisDir,
                            rotation,
                            handleSize * param.axisSize[i],
                            SnapSettings.scale,
                            offset,
                            axisLineScale[i]);
                }
            }

            if (param.ShouldShow(ScaleHandleParam.Handle.XYZ) && (isHot && ids.xyz == GUIUtility.hotControl || !isHot))
            {
                color = centerColor;
                EditorGUI.BeginChangeCheck();
                var s = ScaleValueHandle(ids.xyz, scale.x, position, rotation, handleSize * param.xyzSize, CubeHandleCap, SnapSettings.scale);
                if (EditorGUI.EndChangeCheck() && !Mathf.Approximately(scale.x, 0))
                {
                    var dif = s / scale.x;
                    scale.x = s;
                    scale.y *= dif;
                    scale.z *= dif;
                }
            }

            return scale;
        }
    }
}
