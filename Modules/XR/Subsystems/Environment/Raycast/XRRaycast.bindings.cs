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
    // Must match UnityXRTrackableType
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
    [NativeHeader("Modules/XR/Subsystems/Environment/Raycast/XRRaycast.h")]
    [UsedByNativeCode]
    public struct XRRaycastHit
    {
        public TrackableId TrackableId { get; set; }
        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; }
        public float Distance { get; set; }
        public TrackableType HitType { get; set; }
    }

    [NativeHeader("Modules/XR/XRPrefix.h")]
    [NativeHeader("Modules/XR/Subsystems/Environment/Raycast/XRRaycast.h")]
    [StructLayout(LayoutKind.Sequential)]
    public class XRRaycast
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

        public XREnvironment Environment
        {
            get
            {
                return m_Environment;
            }
        }

        [NativeConditional("ENABLE_XR")]
        public extern float RaycastAngle { get; set; }

        public bool Raycast(Ray ray, List<XRRaycastHit> hitResults, TrackableType trackableTypeMask = TrackableType.All)
        {
            if (hitResults == null)
                throw new ArgumentNullException("hitResults");

            hitResults.Clear();

            return Internal_RaycastAsList(ray.origin, ray.direction, trackableTypeMask, hitResults);
        }

        public bool Raycast(Vector3 screenPoint, List<XRRaycastHit> hitResults, TrackableType trackableTypeMask = TrackableType.All)
        {
            if (hitResults == null)
                throw new ArgumentNullException("hitResults");

            float screenX = Mathf.Clamp01(screenPoint.x / Screen.width);
            float screenY = Mathf.Clamp01(screenPoint.y / Screen.height);

            return Internal_ScreenRaycastAsList(screenX, screenY, trackableTypeMask, hitResults);
        }

        internal XRRaycast(IntPtr nativeEnvironment, XREnvironment managedEnvironment)
        {
            m_Ptr = Internal_Create(nativeEnvironment);
            m_Environment = managedEnvironment;
            SetHandle(this);
        }

        [RequiredByNativeCode]
        private void NotifyRaycastDestruction()
        {
            m_Ptr = IntPtr.Zero;
        }

        [NativeConditional("ENABLE_XR")]
        private static extern IntPtr Internal_Create(IntPtr xrEnvironment);

        [NativeConditional("ENABLE_XR")]
        private extern void SetHandle(XRRaycast inst);

        [NativeConditional("ENABLE_XR")]
        private extern XRRaycastHit[] Internal_RaycastAsFixedArray(
            Vector3 origin,
            Vector3 direction,
            TrackableType hitMask);

        [NativeConditional("ENABLE_XR")]
        private extern bool Internal_RaycastAsList(
            Vector3 origin,
            Vector3 direction,
            TrackableType hitMask,
            List<XRRaycastHit> hitResultsOut);

        [NativeConditional("ENABLE_XR")]
        private extern bool Internal_ScreenRaycastAsList(
            float screenX,
            float screenY,
            TrackableType hitMask,
            List<XRRaycastHit> hitResultsOut);

        [NativeConditional("ENABLE_XR")]
        private extern XRRaycastHit[] Internal_ScreenRaycastAsFixedArray(
            float screenX,
            float screenY,
            TrackableType hitMask);
    }
}
