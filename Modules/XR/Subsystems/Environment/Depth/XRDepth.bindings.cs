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
    public struct PointCloudUpdatedEventArgs
    {
        internal XRDepth m_Depth;
        public XRDepth XRDepth { get { return m_Depth; } }
    }

    [NativeHeader("Modules/XR/XRPrefix.h")]
    [NativeHeader("Modules/XR/Subsystems/Environment/Depth/XRDepth.h")]
    [StructLayout(LayoutKind.Sequential)]
    public class XRDepth
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

        public event Action<PointCloudUpdatedEventArgs> PointCloudUpdated;

        [NativeConditional("ENABLE_XR")]
        public extern int FrameOfLastPointCloudUpdate { get; }

        [NativeConditional("ENABLE_XR")]
        public extern bool TryGetPoint(TrackableId pointId, out Vector3 point);

        public void GetPointCloudPoints(List<Vector3> pointsOut)
        {
            if (pointsOut == null)
                throw new ArgumentNullException("pointsOut");

            Internal_GetPointCloudPointsAsList(pointsOut);
        }

        public void GetPointCloudConfidence(List<float> confidenceOut)
        {
            if (confidenceOut == null)
                throw new ArgumentNullException("confidenceOut");

            Internal_GetPointCloudConfidenceAsList(confidenceOut);
        }

        internal XRDepth(IntPtr nativeEnvironment, XREnvironment managedEnvironment)
        {
            m_Ptr = Internal_Create(nativeEnvironment);
            m_Environment = managedEnvironment;
            SetHandle(this);
        }

        [RequiredByNativeCode]
        private void NotifyDepthDestruction()
        {
            m_Ptr = IntPtr.Zero;
        }

        [RequiredByNativeCode]
        private void InvokePointCloudUpdatedEvent()
        {
            if (PointCloudUpdated != null)
            {
                PointCloudUpdated(new PointCloudUpdatedEventArgs()
                {
                    m_Depth = this
                });
            }
        }

        [NativeConditional("ENABLE_XR")]
        private static extern IntPtr Internal_Create(IntPtr xrEnvironment);

        [NativeConditional("ENABLE_XR")]
        private extern void SetHandle(XRDepth inst);

        [NativeConditional("ENABLE_XR")]
        private extern void Internal_GetPointCloudPointsAsList(List<Vector3> pointsOut);

        [NativeConditional("ENABLE_XR")]
        private extern void Internal_GetPointCloudConfidenceAsList(List<float> confidenceOut);

        [NativeConditional("ENABLE_XR")]
        private extern Vector3[] Internal_GetPointCloudPointsAsFixedArray();

        [NativeConditional("ENABLE_XR")]
        private extern float[] Internal_GetPointCloudConfidenceAsFixedArray();
    }
}
