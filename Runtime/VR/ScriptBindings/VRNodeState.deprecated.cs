// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Scripting;

namespace UnityEngine.VR
{
    [Obsolete("VRNodeState has been moved and renamed.  Use UnityEngine.XR.XRNodeState instead (UnityUpgradable) -> UnityEngine.XR.XRNodeState", true)]
    [StructLayout(LayoutKind.Sequential)]
    public struct VRNodeState
    {
        // Properties
        public ulong uniqueID
        {
            get
            {
                throw new NotSupportedException("VRNodeState has been moved and renamed.  Use UnityEngine.XR.XRNodeState instead.");
            }
            set
            {
                throw new NotSupportedException("VRNodeState has been moved and renamed.  Use UnityEngine.XR.XRNodeState instead.");
            }
        }

        public VRNode nodeType
        {
            get
            {
                throw new NotSupportedException("VRNodeState has been moved and renamed.  Use UnityEngine.XR.XRNodeState instead.");
            }
            set
            {
                throw new NotSupportedException("VRNodeState has been moved and renamed.  Use UnityEngine.XR.XRNodeState instead.");
            }
        }

        public bool tracked
        {
            get
            {
                throw new NotSupportedException("VRNodeState has been moved and renamed.  Use UnityEngine.XR.XRNodeState instead.");
            }
            set
            {
                throw new NotSupportedException("VRNodeState has been moved and renamed.  Use UnityEngine.XR.XRNodeState instead.");
            }
        }

        public Vector3 position
        {
            set
            {
                throw new NotSupportedException("VRNodeState has been moved and renamed.  Use UnityEngine.XR.XRNodeState instead.");
            }
        }

        public Quaternion rotation
        {
            set
            {
                throw new NotSupportedException("VRNodeState has been moved and renamed.  Use UnityEngine.XR.XRNodeState instead.");
            }
        }

        public Vector3 velocity
        {
            set
            {
                throw new NotSupportedException("VRNodeState has been moved and renamed.  Use UnityEngine.XR.XRNodeState instead.");
            }
        }

        public Vector3 angularVelocity
        {
            set
            {
                throw new NotSupportedException("VRNodeState has been moved and renamed.  Use UnityEngine.XR.XRNodeState instead.");
            }
        }

        public Vector3 acceleration
        {
            set
            {
                throw new NotSupportedException("VRNodeState has been moved and renamed.  Use UnityEngine.XR.XRNodeState instead.");
            }
        }

        public Vector3 angularAcceleration
        {
            set
            {
                throw new NotSupportedException("VRNodeState has been moved and renamed.  Use UnityEngine.XR.XRNodeState instead.");
            }
        }

        // Getters
        public bool TryGetPosition(out Vector3 position)
        {
            position = new Vector3();
            throw new NotSupportedException("VRNodeState has been moved and renamed.  Use UnityEngine.XR.XRNodeState instead.");
        }

        public bool TryGetRotation(out Quaternion rotation)
        {
            rotation = new Quaternion();
            throw new NotSupportedException("VRNodeState has been moved and renamed.  Use UnityEngine.XR.XRNodeState instead.");
        }

        public bool TryGetVelocity(out Vector3 velocity)
        {
            velocity = new Vector3();
            throw new NotSupportedException("VRNodeState has been moved and renamed.  Use UnityEngine.XR.XRNodeState instead.");
        }

        public bool TryGetAngularVelocity(out Vector3 angularVelocity)
        {
            angularVelocity = new Vector3();
            throw new NotSupportedException("VRNodeState has been moved and renamed.  Use UnityEngine.XR.XRNodeState instead.");
        }

        public bool TryGetAcceleration(out Vector3 acceleration)
        {
            acceleration = new Vector3();
            throw new NotSupportedException("VRNodeState has been moved and renamed.  Use UnityEngine.XR.XRNodeState instead.");
        }

        public bool TryGetAngularAcceleration(out Vector3 angularAcceleration)
        {
            angularAcceleration = new Vector3();
            throw new NotSupportedException("VRNodeState has been moved and renamed.  Use UnityEngine.XR.XRNodeState instead.");
        }
    }
}
