// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using UnityEngine.Experimental;

namespace UnityEngine.Experimental.XR
{
    // Must match XRManagedReferencePoint
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/XR/Subsystems/ReferencePoints/XRManagedReferencePoint.h")]
    [NativeHeader("Modules/XR/Subsystems/Session/XRSessionSubsystem.h")]
    public struct ReferencePoint
    {
        public TrackableId Id { get; internal set; }
        public TrackingState TrackingState { get; internal set; }
        public Pose Pose { get; internal set; }
    }

    [NativeHeader("Modules/XR/Subsystems/Session/XRSessionSubsystem.h")]
    public struct ReferencePointUpdatedEventArgs
    {
        public ReferencePoint ReferencePoint { get; internal set; }
        public TrackingState PreviousTrackingState { get; internal set; }
        public Pose PreviousPose { get; internal set; }
    }

    [NativeHeader("Modules/XR/XRPrefix.h")]
    [NativeHeader("Modules/XR/Subsystems/ReferencePoints/XRReferencePointSubsystem.h")]
    [UsedByNativeCode]
    [NativeConditional("ENABLE_XR")]
    public class XRReferencePointSubsystem : Subsystem<XRReferencePointSubsystemDescriptor>
    {
        public event Action<ReferencePointUpdatedEventArgs> ReferencePointUpdated;

        public extern int LastUpdatedFrame { get; }

        public extern bool TryAddReferencePoint(Vector3 position, Quaternion rotation, out TrackableId referencePointId);

        public bool TryAddReferencePoint(Pose pose, out TrackableId referencePointId)
        {
            return TryAddReferencePoint(pose.position, pose.rotation, out referencePointId);
        }

        public extern bool TryRemoveReferencePoint(TrackableId referencePointId);

        public extern bool TryGetReferencePoint(TrackableId referencePointId, out ReferencePoint referencePoint);

        public void GetAllReferencePoints(List<ReferencePoint> referencePointsOut)
        {
            if (referencePointsOut == null)
                throw new ArgumentNullException("referencePointsOut");

            Internal_GetAllReferencePointsAsList(referencePointsOut);
        }

        [RequiredByNativeCode]
        private void InvokeReferencePointUpdatedEvent(ReferencePoint updatedReferencePoint, TrackingState previousTrackingState, Pose previousPose)
        {
            if (ReferencePointUpdated != null)
                ReferencePointUpdated(new ReferencePointUpdatedEventArgs()
                {
                    ReferencePoint = updatedReferencePoint,
                    PreviousTrackingState = previousTrackingState,
                    PreviousPose = previousPose
                });
        }

        private extern void Internal_GetAllReferencePointsAsList(List<ReferencePoint> referencePointsOut);

        private extern ReferencePoint[] Internal_GetAllReferencePointsAsFixedArray();
    }
}
