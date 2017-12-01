// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor
{
    public sealed partial class Handles
    {
        internal struct TransformHandleIds
        {
            static readonly int s_TransformTranslationXHash = "TransformTranslationXHash".GetHashCode();
            static readonly int s_TransformTranslationYHash = "TransformTranslationYHash".GetHashCode();
            static readonly int s_TransformTranslationZHash = "TransformTranslationZHash".GetHashCode();
            static readonly int s_TransformTranslationXYHash = "TransformTranslationXYHash".GetHashCode();
            static readonly int s_TransformTranslationXZHash = "TransformTranslationXZHash".GetHashCode();
            static readonly int s_TransformTranslationYZHash = "TransformTranslationYZHash".GetHashCode();
            static readonly int s_TransformTranslationXYZHash = "TransformTranslationXYZHash".GetHashCode();
            static readonly int s_TransformRotationXHash = "TransformRotationXHash".GetHashCode();
            static readonly int s_TransformRotationYHash = "TransformRotationYHash".GetHashCode();
            static readonly int s_TransformRotationZHash = "TransformRotationZHash".GetHashCode();
            static readonly int s_TransformRotationCameraAxisHash = "TransformRotationCameraAxisHash".GetHashCode();
            static readonly int s_TransformRotationXYZHash = "TransformRotationXYZHash".GetHashCode();
            static readonly int s_TransformScaleXHash = "TransformScaleXHash".GetHashCode();
            static readonly int s_TransformScaleYHash = "TransformScaleYHash".GetHashCode();
            static readonly int s_TransformScaleZHash = "TransformScaleZHash".GetHashCode();
            static readonly int s_TransformScaleXYZHash = "TransformScaleXYZHash".GetHashCode();

            public static TransformHandleIds Default
            {
                get
                {
                    return new TransformHandleIds(
                        new PositionHandleIds(
                            GUIUtility.GetControlID(s_TransformTranslationXHash, FocusType.Passive),
                            GUIUtility.GetControlID(s_TransformTranslationYHash, FocusType.Passive),
                            GUIUtility.GetControlID(s_TransformTranslationZHash, FocusType.Passive),
                            GUIUtility.GetControlID(s_TransformTranslationXYHash, FocusType.Passive),
                            GUIUtility.GetControlID(s_TransformTranslationXZHash, FocusType.Passive),
                            GUIUtility.GetControlID(s_TransformTranslationYZHash, FocusType.Passive),
                            GUIUtility.GetControlID(s_TransformTranslationXYZHash, FocusType.Passive)
                            ),
                        new RotationHandleIds(
                            GUIUtility.GetControlID(s_TransformRotationXHash, FocusType.Passive),
                            GUIUtility.GetControlID(s_TransformRotationYHash, FocusType.Passive),
                            GUIUtility.GetControlID(s_TransformRotationZHash, FocusType.Passive),
                            GUIUtility.GetControlID(s_TransformRotationCameraAxisHash, FocusType.Passive),
                            GUIUtility.GetControlID(s_TransformRotationXYZHash, FocusType.Passive)
                            ),
                        new ScaleHandleIds(
                            GUIUtility.GetControlID(s_TransformScaleXHash, FocusType.Passive),
                            GUIUtility.GetControlID(s_TransformScaleYHash, FocusType.Passive),
                            GUIUtility.GetControlID(s_TransformScaleZHash, FocusType.Passive),
                            GUIUtility.GetControlID(s_TransformScaleXYZHash, FocusType.Passive)
                            )
                        );
                }
            }

            public readonly PositionHandleIds position;
            public readonly RotationHandleIds rotation;
            public readonly ScaleHandleIds scale;

            public bool Has(int id)
            {
                return position.Has(id)
                    || rotation.Has(id)
                    || scale.Has(id);
            }

            public TransformHandleIds(PositionHandleIds position, RotationHandleIds rotation, ScaleHandleIds scale)
            {
                this.position = position;
                this.rotation = rotation;
                this.scale = scale;
            }
        }

        internal struct TransformHandleParam
        {
            static TransformHandleParam s_Default = new TransformHandleParam(
                    // Global
                    new PositionHandleParam(
                        PositionHandleParam.Handle.X | PositionHandleParam.Handle.Y | PositionHandleParam.Handle.Z
                        | PositionHandleParam.Handle.XY | PositionHandleParam.Handle.XZ | PositionHandleParam.Handle.YZ,
                        Vector3.one * 0.15f,
                        Vector3.one,
                        Vector3.zero,
                        Vector3.one * 0.375f,
                        PositionHandleParam.Orientation.Signed,
                        PositionHandleParam.Orientation.Camera
                        ),
                    new RotationHandleParam(
                        RotationHandleParam.Handle.X | RotationHandleParam.Handle.Y | RotationHandleParam.Handle.Z
                        | RotationHandleParam.Handle.CameraAxis | RotationHandleParam.Handle.XYZ,
                        Vector3.one * 1.4f,
                        1.4f,
                        1.5f,
                        false,
                        false
                        ),
                    new ScaleHandleParam(
                        ScaleHandleParam.Handle.XYZ,
                        Vector3.zero,
                        Vector3.one,
                        Vector3.one,
                        1,
                        ScaleHandleParam.Orientation.Signed
                        ),

                    // Camera aligned
                    new PositionHandleParam(
                        PositionHandleParam.Handle.X | PositionHandleParam.Handle.Y | PositionHandleParam.Handle.XY,
                        Vector3.one * 0.15f,
                        Vector3.one,
                        Vector3.zero,
                        Vector3.one * 0.375f,
                        PositionHandleParam.Orientation.Signed,
                        PositionHandleParam.Orientation.Signed
                        ),
                    new RotationHandleParam(
                        RotationHandleParam.Handle.Z | RotationHandleParam.Handle.XYZ,
                        Vector3.one * 1.4f,
                        1.4f,
                        1.5f,
                        false,
                        false
                        ),
                    new ScaleHandleParam(
                        ScaleHandleParam.Handle.XYZ,
                        Vector3.zero,
                        Vector3.one,
                        Vector3.one,
                        1,
                        ScaleHandleParam.Orientation.Signed
                        ),

                    // Local
                    new PositionHandleParam(
                        PositionHandleParam.Handle.X | PositionHandleParam.Handle.Y | PositionHandleParam.Handle.Z
                        | PositionHandleParam.Handle.XY | PositionHandleParam.Handle.XZ | PositionHandleParam.Handle.YZ,
                        Vector3.one * 0.15f,
                        Vector3.one,
                        Vector3.zero,
                        Vector3.one * 0.375f,
                        PositionHandleParam.Orientation.Signed,
                        PositionHandleParam.Orientation.Camera
                        ),
                    new RotationHandleParam(
                        RotationHandleParam.Handle.X | RotationHandleParam.Handle.Y | RotationHandleParam.Handle.Z
                        | RotationHandleParam.Handle.CameraAxis | RotationHandleParam.Handle.XYZ,
                        Vector3.one * 1.4f,
                        1.4f,
                        1.5f,
                        false,
                        false
                        ),
                    new ScaleHandleParam(
                        ScaleHandleParam.Handle.XYZ | ScaleHandleParam.Handle.X | ScaleHandleParam.Handle.Y | ScaleHandleParam.Handle.Z,
                        Vector3.one * 1.5f,
                        Vector3.one,
                        Vector3.one * 0.25f,
                        1,
                        ScaleHandleParam.Orientation.Signed
                        )
                    ,

                    // Vertex Snapping
                    new PositionHandleParam(
                        PositionHandleParam.Handle.X | PositionHandleParam.Handle.Y | PositionHandleParam.Handle.Z
                        | PositionHandleParam.Handle.XYZ,
                        Vector3.one * 0.15f,
                        Vector3.one,
                        Vector3.zero,
                        Vector3.one * 0.375f,
                        PositionHandleParam.Orientation.Signed,
                        PositionHandleParam.Orientation.Signed
                        ),
                    new RotationHandleParam(
                        0,
                        Vector3.one * 1.4f,
                        1.4f,
                        1.5f,
                        false,
                        false
                        ),
                    new ScaleHandleParam(
                        0,
                        Vector3.one * 1.5f,
                        Vector3.one,
                        Vector3.one * 0.25f,
                        1,
                        ScaleHandleParam.Orientation.Signed
                        )
                    );
            public static TransformHandleParam Default { get { return s_Default; } set { s_Default = value; } }

            public readonly PositionHandleParam position;
            public readonly RotationHandleParam rotation;
            public readonly ScaleHandleParam scale;
            public readonly PositionHandleParam cameraAlignedPosition;
            public readonly RotationHandleParam cameraAlignedRotation;
            public readonly ScaleHandleParam cameraAlignedScale;
            public readonly PositionHandleParam localPosition;
            public readonly RotationHandleParam localRotation;
            public readonly ScaleHandleParam localScale;
            public readonly PositionHandleParam vertexSnappingPosition;
            public readonly RotationHandleParam vertexSnappingRotation;
            public readonly ScaleHandleParam vertexSnappingScale;

            public TransformHandleParam(
                PositionHandleParam position,
                RotationHandleParam rotation,
                ScaleHandleParam scale,
                PositionHandleParam cameraAlignedPosition,
                RotationHandleParam cameraAlignedRotation,
                ScaleHandleParam cameraAlignedScale,
                PositionHandleParam localPosition,
                RotationHandleParam localRotation,
                ScaleHandleParam localScale,
                PositionHandleParam vertexSnappingPosition,
                RotationHandleParam vertexSnappingRotation,
                ScaleHandleParam vertexSnappingScale)
            {
                this.position = position;
                this.rotation = rotation;
                this.scale = scale;
                this.cameraAlignedPosition = cameraAlignedPosition;
                this.cameraAlignedRotation = cameraAlignedRotation;
                this.cameraAlignedScale = cameraAlignedScale;
                this.localPosition = localPosition;
                this.localRotation = localRotation;
                this.localScale = localScale;
                this.vertexSnappingPosition = vertexSnappingPosition;
                this.vertexSnappingRotation = vertexSnappingRotation;
                this.vertexSnappingScale = vertexSnappingScale;
            }
        }

        struct RotationHandleData
        {
            public bool rotationStarted;
            public Quaternion initialRotation;
        }

        static bool s_IsHotInCameraAlignedMode = false;
        static Dictionary<RotationHandleIds, RotationHandleData> s_TransformHandle_RotationData = new Dictionary<RotationHandleIds, RotationHandleData>();
        internal static void TransformHandle(TransformHandleIds ids, ref Vector3 position, ref Quaternion rotation, ref Vector3 scale, TransformHandleParam param)
        {
            var workingRotation = rotation;
            var pParam = param.position;
            var rParam = param.rotation;
            var sParam = param.scale;

            var isHot = ids.Has(GUIUtility.hotControl);
            var cameraAligned = (isHot && s_IsHotInCameraAlignedMode || !isHot && Event.current.shift);
            if (Tools.vertexDragging)
            {
                pParam = param.vertexSnappingPosition;
                rParam = param.vertexSnappingRotation;
                sParam = param.vertexSnappingScale;
            }
            else if (cameraAligned)
            {
                workingRotation = Camera.current.transform.rotation;

                pParam = param.cameraAlignedPosition;
                rParam = param.cameraAlignedRotation;
                sParam = param.cameraAlignedScale;
            }
            else if (Tools.pivotRotation == PivotRotation.Local)
            {
                pParam = param.localPosition;
                rParam = param.localRotation;
                sParam = param.localScale;
            }

            // Draw only the hot control
            if (ids.Has(GUIUtility.hotControl))
            {
                if (ids.position.Has(GUIUtility.hotControl))
                    position = DoPositionHandle_Internal(ids.position, position, workingRotation, pParam);
                else if (ids.rotation.Has(GUIUtility.hotControl))
                {
                    var endRotation = DoRotationHandle(ids.rotation, workingRotation, position, rParam);

                    if (cameraAligned)
                    {
                        // For camera aligned axis rotation, we need to store the initial rotation to rotate only the delta
                        if (!s_TransformHandle_RotationData.ContainsKey(ids.rotation))
                        {
                            s_TransformHandle_RotationData[ids.rotation] = new RotationHandleData
                            {
                                rotationStarted = true,
                                initialRotation = rotation
                            };
                        }

                        // For freemove rotation, we already have the delta rotation properly
                        var initialRotation = ids.rotation.xyz != GUIUtility.hotControl
                            ? s_TransformHandle_RotationData[ids.rotation].initialRotation
                            : rotation;
                        var d = endRotation * Quaternion.Inverse(workingRotation);
                        float angle;
                        Vector3 axis;
                        d.ToAngleAxis(out angle, out axis);
                        rotation = Quaternion.AngleAxis(angle, axis) * initialRotation;
                    }
                    else
                        rotation = endRotation;
                }
                else if (ids.scale.Has(GUIUtility.hotControl))
                    scale = DoScaleHandle(ids.scale, scale, position, workingRotation, HandleUtility.GetHandleSize(position), sParam);
            }
            else
            {
                if (s_TransformHandle_RotationData.ContainsKey(ids.rotation))
                {
                    var data = s_TransformHandle_RotationData[ids.rotation];
                    data.rotationStarted = false;
                    s_TransformHandle_RotationData[ids.rotation] = data;
                }

                // Drawing only
                DoRotationHandle(ids.rotation, workingRotation, position, rParam);
                // Draw scale position last so axes have priority
                DoPositionHandle_Internal(ids.position, position, workingRotation, pParam);
                DoScaleHandle(ids.scale, scale, position, workingRotation, HandleUtility.GetHandleSize(position), sParam);
            }

            var nowIsHot = ids.Has(GUIUtility.hotControl);
            if (nowIsHot && !isHot && cameraAligned)
                s_IsHotInCameraAlignedMode = true;
            else if (s_IsHotInCameraAlignedMode && (!isHot || !nowIsHot))
                s_IsHotInCameraAlignedMode = false;
        }
    }
}
