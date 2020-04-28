// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Snap;
using UnityEngine;

namespace UnityEditor
{
    public sealed partial class Handles
    {
        internal struct RotationHandleIds
        {
            public static RotationHandleIds @default
            {
                get
                {
                    return new RotationHandleIds(
                        GUIUtility.GetControlID(s_xRotateHandleHash, FocusType.Passive),
                        GUIUtility.GetControlID(s_yRotateHandleHash, FocusType.Passive),
                        GUIUtility.GetControlID(s_zRotateHandleHash, FocusType.Passive),
                        GUIUtility.GetControlID(s_cameraAxisRotateHandleHash, FocusType.Passive),
                        GUIUtility.GetControlID(s_xyzRotateHandleHash, FocusType.Passive)
                    );
                }
            }

            public readonly int x, y, z, cameraAxis, xyz;

            public int this[int index]
            {
                get
                {
                    switch (index)
                    {
                        case 0: return x;
                        case 1: return y;
                        case 2: return z;
                        case 3: return cameraAxis;
                        case 4: return xyz;
                    }
                    return -1;
                }
            }

            public bool Has(int id)
            {
                return x == id
                    || y == id
                    || z == id
                    || cameraAxis == id
                    || xyz == id;
            }

            public RotationHandleIds(int x, int y, int z, int cameraAxis, int xyz)
            {
                this.x = x;
                this.y = y;
                this.z = z;
                this.cameraAxis = cameraAxis;
                this.xyz = xyz;
            }

            public override int GetHashCode()
            {
                return x ^ y ^ z ^ cameraAxis ^ xyz;
            }

            public override bool Equals(object obj)
            {
                if (!(obj is RotationHandleIds))
                    return false;

                var o = (RotationHandleIds)obj;
                return o.x == x && o.y == y && o.z == z
                    && o.cameraAxis == cameraAxis && o.xyz == xyz;
            }
        }

        internal struct RotationHandleParam
        {
            [Flags]
            public enum Handle
            {
                None = 0,
                X = 1 << 0,
                Y = 1 << 1,
                Z = 1 << 2,
                CameraAxis = 1 << 3,
                XYZ = 1 << 4,
                All = ~None
            }

            static RotationHandleParam s_Default = new RotationHandleParam((Handle)(-1), Vector3.one, 1f, 1.1f, true, true);
            public static RotationHandleParam Default { get { return s_Default; } set { s_Default = value; } }

            public readonly Vector3 axisSize;
            public readonly float cameraAxisSize;
            public readonly float xyzSize;
            public readonly Handle handles;
            public readonly bool enableRayDrag;
            public readonly bool displayXYZCircle;

            public bool ShouldShow(int axis)
            {
                return (handles & (Handle)(1 << axis)) != 0;
            }

            public bool ShouldShow(Handle handle)
            {
                return (handles & handle) != 0;
            }

            public RotationHandleParam(Handle handles, Vector3 axisSize, float xyzSize, float cameraAxisSize, bool enableRayDrag, bool displayXYZCircle)
            {
                this.axisSize = axisSize;
                this.xyzSize = xyzSize;
                this.handles = handles;
                this.cameraAxisSize = cameraAxisSize;
                this.enableRayDrag = enableRayDrag;
                this.displayXYZCircle = displayXYZCircle;
            }
        }

        static readonly Color k_RotationPieColor = new Color(246f / 255, 242f / 255, 50f / 255, .89f);

        public static Quaternion DoRotationHandle(Quaternion rotation, Vector3 position)
        {
            return DoRotationHandle(RotationHandleIds.@default, rotation, position, RotationHandleParam.Default);
        }

        internal static Quaternion DoRotationHandle(RotationHandleIds ids, Quaternion rotation, Vector3 position, RotationHandleParam param)
        {
            var evt = Event.current;
            var camForward = Handles.inverseMatrix.MultiplyVector(Camera.current != null ? Camera.current.transform.forward : Vector3.forward);

            var size = HandleUtility.GetHandleSize(position);
            var temp = color;
            bool isDisabled = !GUI.enabled;

            var isHot = ids.Has(GUIUtility.hotControl);

            // Draw free rotation first to give it the lowest priority
            if (!isDisabled
                && param.ShouldShow(RotationHandleParam.Handle.XYZ)
                && (isHot && ids.xyz == GUIUtility.hotControl || !isHot))
            {
                color = new Color(0, 0, 0, 0.3f);
                rotation = UnityEditorInternal.FreeRotate.Do(ids.xyz, rotation, position, size * param.xyzSize, param.displayXYZCircle);
            }

            for (var i = 0; i < 3; ++i)
            {
                if (!param.ShouldShow(i))
                    continue;

                var axisColor = GetColorByAxis(i);
                color = isDisabled ? Color.Lerp(axisColor, staticColor, staticBlend) : axisColor;
                color = ToActiveColorSpace(color);
                var axisDir = GetAxisVector(i);

                var radius = size * param.axisSize[i];
                rotation = UnityEditorInternal.Disc.Do(ids[i], rotation, position, rotation * axisDir, radius, true, EditorSnapSettings.rotate, param.enableRayDrag, true, k_RotationPieColor);
            }

            // while dragging any rotation handles, draw a gray disc outline
            if (isHot && evt.type == EventType.Repaint)
            {
                color = ToActiveColorSpace(s_DisabledHandleColor);
                Handles.DrawWireDisc(position, camForward, size * param.axisSize[0], Handles.lineThickness);
            }

            if (!isDisabled
                && param.ShouldShow(RotationHandleParam.Handle.CameraAxis)
                && (isHot && ids.cameraAxis == GUIUtility.hotControl || !isHot))
            {
                color = ToActiveColorSpace(centerColor);
                rotation = UnityEditorInternal.Disc.Do(ids.cameraAxis, rotation, position, camForward, size * param.cameraAxisSize, false, 0, param.enableRayDrag, true, k_RotationPieColor);
            }

            color = temp;
            return rotation;
        }
    }
}
