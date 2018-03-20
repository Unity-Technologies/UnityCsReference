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
    public struct PointCloudUpdatedEventArgs
    {
        internal XRDepthSubsystem m_DepthSubsystem;
        public XRDepthSubsystem DepthSubsystem { get { return m_DepthSubsystem; } }
    }

    [NativeHeader("Modules/XR/XRPrefix.h")]
    [NativeHeader("Modules/XR/Subsystems/Depth/XRDepthSubsystem.h")]
    [UsedByNativeCode]
    [NativeConditional("ENABLE_XR")]
    public class XRDepthSubsystem : Subsystem<XRDepthSubsystemDescriptor>
    {
        public event Action<PointCloudUpdatedEventArgs> PointCloudUpdated;

        public extern int LastUpdatedFrame { get; }

        public void GetPoints(List<Vector3> pointsOut)
        {
            if (pointsOut == null)
                throw new ArgumentNullException("pointsOut");

            Internal_GetPointCloudPointsAsList(pointsOut);
        }

        public void GetConfidence(List<float> confidenceOut)
        {
            if (confidenceOut == null)
                throw new ArgumentNullException("confidenceOut");

            Internal_GetPointCloudConfidenceAsList(confidenceOut);
        }

        [RequiredByNativeCode]
        private void InvokePointCloudUpdatedEvent()
        {
            if (PointCloudUpdated != null)
            {
                PointCloudUpdated(new PointCloudUpdatedEventArgs()
                {
                    m_DepthSubsystem = this
                });
            }
        }

        private extern void Internal_GetPointCloudPointsAsList(List<Vector3> pointsOut);

        private extern void Internal_GetPointCloudConfidenceAsList(List<float> confidenceOut);

        private extern Vector3[] Internal_GetPointCloudPointsAsFixedArray();

        private extern float[] Internal_GetPointCloudConfidenceAsFixedArray();
    }
}
