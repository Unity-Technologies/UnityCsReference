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
    [NativeHeader("Modules/XR/XRPrefix.h")]
    [NativeHeader("Modules/XR/Subsystems/Experience/XRExperienceSubsystem.h")]
    [UsedByNativeCode]
    [NativeConditional("ENABLE_XR")]
    public class XRExperienceSubsystem : IntegratedSubsystem<XRExperienceSubsystemDescriptor>
    {
        public event Action ExperienceTypeChanged;
        public event Action BoundaryChanged;

        [RequiredByNativeCode]
        private void InvokeExperienceTypeChanged()
        {
            if (ExperienceTypeChanged != null)
            {
                ExperienceTypeChanged();
            }
        }

        [RequiredByNativeCode]
        private void InvokeBoundaryChanged()
        {
            if (BoundaryChanged != null)
            {
                BoundaryChanged();
            }
        }

        public enum ExperienceType
        {
            Local = 0,
            Bounded,
            UnBounded
        }

        public extern ExperienceType experienceType { get; }

        public void GetAllBoundaryPoints(List<Vector3> boundaryPointsOut)
        {
            if (boundaryPointsOut == null)
                throw new ArgumentNullException("boundaryPointsOut");

            Internal_GetAllBoundaryPointsAsList(boundaryPointsOut);
        }

        private extern void Internal_GetAllBoundaryPointsAsList(List<Vector3> boundaryPointsOut);

        public enum TrackingOrigin
        {
            Device = 0,
            Floor
        }

        public extern TrackingOrigin trackingOrigin { get; }
    }
}
