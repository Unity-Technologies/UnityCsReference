// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;
using UObject = UnityEngine.Object;

namespace UnityEditor
{
    interface IViewpoint
    {
        UObject TargetObject { get; }

        Vector3 Position { get; set; }
        Quaternion Rotation { get; set; }

        VisualElement CreateVisualElement();
    }

    public interface ICameraLensData
    {
        float NearClipPlane { get; }
        float FarClipPlane { get; }
        float FieldOfView { get; set; }

        bool UsePhysicalProperties { get; }
        float FocalLength { get; set; }
        Vector2 SensorSize { get; }
        Vector2 LensShift { get; }
        Camera.GateFitMode GateFit { get; }

        bool Orthographic { get; set; }
        float OrthographicSize { get; set; }
    }

    internal class ViewpointUtility
    {
        static internal void ApplyTransformData(IViewpoint viewpoint, Transform destination)
        {
            destination.transform.position = viewpoint.Position;
            destination.transform.rotation = viewpoint.Rotation;
        }

        static internal void ApplyCameraLensData(ICameraLensData data, Camera destination)
        {
            destination.nearClipPlane = data.NearClipPlane;
            destination.farClipPlane = data.FarClipPlane;
            destination.orthographic = data.Orthographic;
            destination.orthographicSize = data.OrthographicSize;
            destination.fieldOfView = data.FieldOfView;
            destination.focalLength = data.FocalLength;
            destination.usePhysicalProperties = data.UsePhysicalProperties;
            destination.sensorSize = data.SensorSize;
            destination.lensShift = data.LensShift;
            destination.gateFit = data.GateFit;
        }
    }

    [Serializable]
    public abstract class Viewpoint<T> : IViewpoint where T : Component
    {
        [SerializeField]
        T m_Target;

        public UObject TargetObject
        {
            get => m_Target;
            set => m_Target = value as T;
        }

        public virtual Vector3 Position
        {
            get => m_Target.transform.position;
            set
            {
                Undo.RecordObject(m_Target.transform, $"Move {m_Target.name}");
                m_Target.transform.position = value;
            }
        }

        public virtual Quaternion Rotation
        {
            get => m_Target.transform.rotation;
            set
            {
                Undo.RecordObject(m_Target.transform, $"Rotate {m_Target.name}");
                m_Target.transform.rotation = value;
            }
        }

        protected T Target
        {
            get => m_Target;
        }

        public Viewpoint(T target)
        {
            m_Target = target;
        }

        public virtual VisualElement CreateVisualElement() { return null; }
    }
}
