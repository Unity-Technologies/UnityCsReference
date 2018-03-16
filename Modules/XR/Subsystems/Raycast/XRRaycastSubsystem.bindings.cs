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
    // Must match UnityXRTrackableType
    [UsedByNativeCode]
    [Flags]
    public enum TrackableType
    {
        None = 0,
        PlaneWithinPolygon = 1 << 0,
        PlaneWithinBounds = 1 << 1,
        PlaneWithinInfinity = 1 << 2,
        PlaneEstimated = 1 << 3,
        Planes =
            PlaneWithinPolygon |
            PlaneWithinBounds |
            PlaneWithinInfinity |
            PlaneEstimated,
        FeaturePoint = 1 << 4,
        All = Planes | FeaturePoint
    }

    // Must match UnityXRRaycastHit
    [NativeHeader("Modules/XR/Subsystems/Raycast/XRRaycastSubsystem.h")]
    [UsedByNativeCode]
    public struct XRRaycastHit
    {
        public TrackableId TrackableId { get; set; }
        public Pose Pose { get; set; }
        public float Distance { get; set; }
        public TrackableType HitType { get; set; }
    }

    [NativeHeader("Modules/XR/XRPrefix.h")]
    [NativeHeader("Modules/XR/Subsystems/Raycast/XRRaycastSubsystem.h")]
    [UsedByNativeCode]
    [NativeConditional("ENABLE_XR")]
    public class XRRaycastSubsystem : Subsystem<XRRaycastSubsystemDescriptor>
    {
        public bool Raycast(Vector3 screenPoint, List<XRRaycastHit> hitResults, TrackableType trackableTypeMask = TrackableType.All)
        {
            if (hitResults == null)
                throw new ArgumentNullException("hitResults");

            float screenX = Mathf.Clamp01(screenPoint.x / Screen.width);
            float screenY = Mathf.Clamp01(screenPoint.y / Screen.height);

            return Internal_ScreenRaycastAsList(screenX, screenY, trackableTypeMask, hitResults);
        }

        static public void Raycast(
            Ray ray,
            XRDepthSubsystem depthSubsystem,
            XRPlaneSubsystem planeSubsystem,
            List<XRRaycastHit> hitResults,
            TrackableType trackableTypeMask = TrackableType.All,
            float pointCloudRaycastAngleInDegrees = 5f)
        {
            if (hitResults == null)
                throw new ArgumentNullException("hitResults");

            IntPtr depthPtr = depthSubsystem == null ? IntPtr.Zero : depthSubsystem.m_Ptr;
            IntPtr planePtr = planeSubsystem == null ? IntPtr.Zero : planeSubsystem.m_Ptr;

            Internal_RaycastAsList(ray, pointCloudRaycastAngleInDegrees, depthPtr, planePtr, trackableTypeMask, hitResults);
        }

        private static extern void Internal_RaycastAsList(
            Ray ray,
            float pointCloudRaycastAngleInDegrees,
            IntPtr depthSubsystem,
            IntPtr planeSubsystem,
            TrackableType trackableTypeMask,
            List<XRRaycastHit> hitResultsOut);

        private static extern XRRaycastHit[] Internal_RaycastAsFixedArray(
            Ray ray,
            float pointCloudRaycastAngleInDegrees,
            IntPtr depthSubsystem,
            IntPtr planeSubsystem,
            TrackableType trackableTypeMask);

        private extern bool Internal_ScreenRaycastAsList(
            float screenX,
            float screenY,
            TrackableType hitMask,
            List<XRRaycastHit> hitResultsOut);

        private extern XRRaycastHit[] Internal_ScreenRaycastAsFixedArray(
            float screenX,
            float screenY,
            TrackableType hitMask);
    }
}
