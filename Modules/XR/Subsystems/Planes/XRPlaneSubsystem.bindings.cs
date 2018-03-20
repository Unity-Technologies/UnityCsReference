// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace UnityEngine.Experimental.XR
{
    [UsedByNativeCode]
    [Flags]
    public enum PlaneAlignment
    {
        Horizontal = 1 << 0,
        Vertical = 1 << 1,
        NonAxis = 1 << 2
    }

    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/XR/XRPrefix.h")]
    [NativeHeader("Modules/XR/Subsystems/Planes/XRBoundedPlane.h")]
    [NativeHeader("XRScriptingClasses.h")]
    [NativeConditional("ENABLE_XR")]
    public struct BoundedPlane
    {
        private uint m_InstanceId;

        public TrackableId Id { get; set; }
        public TrackableId SubsumedById { get; set; }
        public Pose Pose { get; set; }
        public Vector3 Center { get; set; }
        public Vector2 Size { get; set; }
        public PlaneAlignment Alignment { get; set; }

        public float Width { get { return Size.x; } }
        public float Height { get { return Size.y; } }
        public Vector3 Normal { get { return Pose.up; } }
        public Plane Plane { get { return new Plane(Normal, Center); } }

        public void GetCorners(
            out Vector3 p0,
            out Vector3 p1,
            out Vector3 p2,
            out Vector3 p3)
        {
            var worldHalfX = (Pose.right) * (Width * .5f);
            var worldHalfZ = (Pose.forward) * (Height * .5f);
            p0 = Center - worldHalfX - worldHalfZ;
            p1 = Center - worldHalfX + worldHalfZ;
            p2 = Center + worldHalfX + worldHalfZ;
            p3 = Center + worldHalfX - worldHalfZ;
        }

        public bool TryGetBoundary(List<Vector3> boundaryOut)
        {
            if (boundaryOut == null)
                throw new ArgumentNullException("boundaryOut");

            return Internal_GetBoundaryAsList(m_InstanceId, Id, boundaryOut);
        }

        private static extern Vector3[] Internal_GetBoundaryAsFixedArray(
            uint instanceId,
            TrackableId id);

        private static extern bool Internal_GetBoundaryAsList(
            uint instanceId,
            TrackableId id,
            List<Vector3> boundaryOut);
    }

    public struct PlaneAddedEventArgs
    {
        public XRPlaneSubsystem PlaneSubsystem { get; internal set; }
        public BoundedPlane Plane { get; internal set; }
    }

    public struct PlaneUpdatedEventArgs
    {
        public XRPlaneSubsystem PlaneSubsystem { get; internal set; }
        public BoundedPlane Plane { get; internal set; }
    }

    public struct PlaneRemovedEventArgs
    {
        public XRPlaneSubsystem PlaneSubsystem { get; internal set; }
        public BoundedPlane Plane { get; internal set; }
    }

    [NativeHeader("Modules/XR/XRPrefix.h")]
    [NativeHeader("Modules/XR/Subsystems/Planes/XRPlaneSubsystem.h")]
    [UsedByNativeCode]
    [NativeConditional("ENABLE_XR")]
    public class XRPlaneSubsystem : Subsystem<XRPlaneSubsystemDescriptor>
    {
        public event Action<PlaneAddedEventArgs> PlaneAdded;
        public event Action<PlaneUpdatedEventArgs> PlaneUpdated;
        public event Action<PlaneRemovedEventArgs> PlaneRemoved;

        public extern int LastUpdatedFrame { get; }

        public extern bool TryGetPlane(TrackableId planeId, out BoundedPlane plane);

        public void GetAllPlanes(List<BoundedPlane> planesOut)
        {
            if (planesOut == null)
                throw new ArgumentNullException("planesOut");

            GetAllPlanesAsList(planesOut);
        }

        public bool TryGetPlaneBoundary(TrackableId planeId, List<Vector3> boundaryOut)
        {
            if (boundaryOut == null)
                throw new ArgumentNullException("boundaryOut");

            return Internal_GetBoundaryAsList(planeId, boundaryOut);
        }

        [RequiredByNativeCode]
        private void InvokePlaneAddedEvent(BoundedPlane plane)
        {
            if (PlaneAdded != null)
            {
                PlaneAdded(new PlaneAddedEventArgs()
                {
                    PlaneSubsystem = this,
                    Plane = plane
                });
            }
        }

        [RequiredByNativeCode]
        private void InvokePlaneUpdatedEvent(BoundedPlane plane)
        {
            if (PlaneUpdated != null)
            {
                PlaneUpdated(new PlaneUpdatedEventArgs()
                {
                    PlaneSubsystem = this,
                    Plane = plane
                });
            }
        }

        [RequiredByNativeCode]
        private void InvokePlaneRemovedEvent(BoundedPlane removedPlane)
        {
            if (PlaneRemoved != null)
            {
                PlaneRemoved(new PlaneRemovedEventArgs()
                {
                    PlaneSubsystem = this,
                    Plane = removedPlane
                });
            }
        }

        private extern BoundedPlane[] GetAllPlanesAsFixedArray();

        private extern void GetAllPlanesAsList(List<BoundedPlane> planes);

        private extern bool Internal_GetBoundaryAsList(TrackableId planeId, List<Vector3> boundaryOut);

        private extern Vector3[] Internal_GetBoundaryAsFixedArray(TrackableId planeId);
    }
}
