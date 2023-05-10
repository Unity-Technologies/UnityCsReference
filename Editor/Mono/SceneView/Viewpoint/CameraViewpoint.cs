// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    class CameraViewpoint : Viewpoint<Camera>, ICameraLensData
    {
        public CameraViewpoint(Camera target)
            : base(target)
        {
        }

        public bool Orthographic
        {
            get => Target.orthographic;
            set => Target.orthographic = value;
        }

        public float OrthographicSize
        {
            get => Target.orthographicSize;
            set => Target.orthographicSize = value;
        }

        public float FieldOfView
        {
            get => Target.usePhysicalProperties ? Target.GetGateFittedFieldOfView() : Target.fieldOfView;
            set => Target.fieldOfView = value;
        }

        public float FocalLength
        {
            get => Target.focalLength;
            set => Target.focalLength = value;
        }

        public bool UsePhysicalProperties
        {
            get => Target.usePhysicalProperties;
        }

        public Vector2 SensorSize => Target.sensorSize;

        public float NearClipPlane => Target.nearClipPlane;

        public float FarClipPlane => Target.farClipPlane;

        public Vector2 LensShift => Target.lensShift;

        public Camera.GateFitMode GateFit => Target.gateFit;
    }
}
