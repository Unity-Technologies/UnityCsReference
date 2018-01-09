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
    // Must match XRManagedReferencePoint
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/XR/Subsystems/Environment/ReferencePoints/XRManagedReferencePoint.h")]
    public struct ReferencePoint
    {
        public TrackableId Id { get; set; }
        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; }
    }

    public struct ReferencePointUpdatedEventArgs
    {
        public ReferencePoint UpdatedReferencePoint { get; set; }
    }

    [NativeHeader("Modules/XR/XRPrefix.h")]
    [NativeHeader("Modules/XR/Subsystems/Environment/ReferencePoints/XRReferencePoint.h")]
    [StructLayout(LayoutKind.Sequential)]
    public class XRReferencePoint
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

        public event Action<ReferencePointUpdatedEventArgs> ReferencePointUpdated;

        [NativeConditional("ENABLE_XR")]
        public extern int FrameOfLastReferencePointUpdate { get; }

        [NativeConditional("ENABLE_XR")]
        public extern bool TryAddReferencePoint(Vector3 position, Quaternion rotation, out TrackableId referencePointId);

        [NativeConditional("ENABLE_XR")]
        public extern bool TryRemoveReferencePoint(TrackableId referencePointId);

        [NativeConditional("ENABLE_XR")]
        public extern bool TryGetReferencePoint(TrackableId referencePointId, out ReferencePoint referencePoint);

        public void GetAllReferencePoints(List<ReferencePoint> referencePointsOut)
        {
            if (referencePointsOut == null)
                throw new ArgumentNullException("referencePointsOut");

            Internal_GetAllReferencePointsAsList(referencePointsOut);
        }

        internal XRReferencePoint(IntPtr nativeEnvironment, XREnvironment managedEnvironment)
        {
            m_Ptr = Internal_Create(nativeEnvironment);
            m_Environment = managedEnvironment;
            SetHandle(this);
        }

        [RequiredByNativeCode]
        private void NotifyReferencePointsDestruction()
        {
            m_Ptr = IntPtr.Zero;
        }

        [RequiredByNativeCode]
        private void InvokeReferencePointUpdatedEvent(ReferencePoint updatedReferencePoint)
        {
            if (ReferencePointUpdated != null)
                ReferencePointUpdated(new ReferencePointUpdatedEventArgs()
                {
                    UpdatedReferencePoint = updatedReferencePoint
                });
        }

        [NativeConditional("ENABLE_XR")]
        private static extern IntPtr Internal_Create(IntPtr xrEnvironment);

        [NativeConditional("ENABLE_XR")]
        private extern void SetHandle(XRReferencePoint inst);

        [NativeConditional("ENABLE_XR")]
        private extern void Internal_GetAllReferencePointsAsList(List<ReferencePoint> referencePointsOut);

        [NativeConditional("ENABLE_XR")]
        private extern ReferencePoint[] Internal_GetAllReferencePointsAsFixedArray();
    }
}
