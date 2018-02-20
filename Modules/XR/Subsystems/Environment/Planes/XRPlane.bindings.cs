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
    [Flags]
    public enum PlaneAlignment
    {
        Horizontal = 1 << 0,
        Vertical = 1 << 1,
        NonAxis = 1 << 2
    }

    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/XR/Subsystems/Environment/Planes/XRBoundedPlane.h")]
    [NativeHeader("XRScriptingClasses.h")]
    public struct BoundedPlane
    {
        private uint m_InstanceId;

        public TrackableId Id { get; set; }
        public Vector3 Position { get; set; }
        public Vector3 Center { get; set; }
        public Quaternion Rotation { get; set; }
        public Vector2 Size { get; set; }
        public PlaneAlignment Alignment { get; set; }

        public float Width { get { return Size.x; } }
        public float Height { get { return Size.y; } }
        public Vector3 Normal { get { return Rotation * Vector3.up; } }
        public Plane Plane { get { return new Plane(Normal, Center); } }

        public void GetCorners(
            out Vector3 p0,
            out Vector3 p1,
            out Vector3 p2,
            out Vector3 p3)
        {
            var worldHalfX = (Rotation * Vector3.right) * (Width * .5f);
            var worldHalfZ = (Rotation * Vector3.forward) * (Height * .5f);
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

        [NativeConditional("ENABLE_XR")]
        private static extern Vector3[] Internal_GetBoundaryAsFixedArray(
            uint instanceId,
            TrackableId id);

        [NativeConditional("ENABLE_XR")]
        private static extern bool Internal_GetBoundaryAsList(
            uint instanceId,
            TrackableId id,
            List<Vector3> boundaryOut);
    }

    public struct PlaneAddedEventArgs
    {
        internal XRPlane m_Planes;
        public XRPlane XRPlane { get { return m_Planes; } }
        public BoundedPlane AddedPlane { get; set; }
    }

    public struct PlaneUpdatedEventArgs
    {
        internal XRPlane m_Planes;
        public XRPlane XRPlane { get { return m_Planes; } }
        public BoundedPlane UpdatedPlane { get; set; }
    }

    public struct PlaneRemovedEventArgs
    {
        internal XRPlane m_Planes;
        public XRPlane XRPlane { get { return m_Planes; } }
        public BoundedPlane RemovedPlane { get; set; }
        public BoundedPlane? SubsumedByPlane { get; set; }
    }

    [NativeHeader("Modules/XR/XRPrefix.h")]
    [NativeHeader("Modules/XR/Subsystems/Environment/Planes/XRPlane.h")]
    [StructLayout(LayoutKind.Sequential)]
    public class XRPlane
    {
        private IntPtr m_Ptr;
        private XREnvironment m_Environment;

        public bool Valid
        {
            get
            {
                return m_Ptr != IntPtr.Zero;
            }
        }

        public XREnvironment XREnvironment
        {
            get
            {
                return m_Environment;
            }
        }

        public event Action<PlaneAddedEventArgs> PlaneAdded;
        public event Action<PlaneUpdatedEventArgs> PlaneUpdated;
        public event Action<PlaneRemovedEventArgs> PlaneRemoved;

        [NativeConditional("ENABLE_XR")]
        public extern int FrameOfLastPlaneUpdate { get; }

        [NativeConditional("ENABLE_XR")]
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

        internal XRPlane(IntPtr nativeEnvironment, XREnvironment managedEnvironment)
        {
            m_Ptr = Internal_Create(nativeEnvironment);
            m_Environment = managedEnvironment;
            SetHandle(this);
        }

        [RequiredByNativeCode]
        private void NotifyPlanesDestruction()
        {
            m_Ptr = IntPtr.Zero;
        }

        [RequiredByNativeCode]
        private void InvokePlaneAddedEvent(BoundedPlane plane)
        {
            if (PlaneAdded != null)
            {
                PlaneAdded(new PlaneAddedEventArgs()
                {
                    m_Planes = this,
                    AddedPlane = plane
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
                    m_Planes = this,
                    UpdatedPlane = plane
                });
            }
        }

        [RequiredByNativeCode]
        private void InvokePlaneMergedEvent(BoundedPlane removedPlane, BoundedPlane subsumedByPlane)
        {
            if (PlaneRemoved != null)
            {
                PlaneRemoved(new PlaneRemovedEventArgs()
                {
                    m_Planes = this,
                    RemovedPlane = removedPlane,
                    SubsumedByPlane = subsumedByPlane
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
                    m_Planes = this,
                    RemovedPlane = removedPlane,
                    SubsumedByPlane = null
                });
            }
        }

        [NativeConditional("ENABLE_XR")]
        private extern BoundedPlane[] GetAllPlanesAsFixedArray();

        [NativeConditional("ENABLE_XR")]
        private extern void GetAllPlanesAsList(List<BoundedPlane> planes);

        [NativeConditional("ENABLE_XR")]
        private extern bool Internal_GetBoundaryAsList(TrackableId planeId, List<Vector3> boundaryOut);

        [NativeConditional("ENABLE_XR")]
        private extern Vector3[] Internal_GetBoundaryAsFixedArray(TrackableId planeId);

        [NativeConditional("ENABLE_XR")]
        private static extern IntPtr Internal_Create(IntPtr xrEnvironment);

        [NativeConditional("ENABLE_XR")]
        private extern void SetHandle(XRPlane inst);
    }
}
