// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Scripting;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.XR
{
    // Matches UnityVRTrackedNodeAttribs in IUnityVR.h
    [Flags]
    internal enum AvailableTrackingData
    {
        None = 0,

        PositionAvailable = 0x00000001,
        RotationAvailable = 0x00000002,
        VelocityAvailable = 0x00000004,
        AngularVelocityAvailable = 0x00000008,
        AccelerationAvailable = 0x00000010,
        AngularAccelerationAvailable = 0x00000020
    }

    [StructLayout(LayoutKind.Sequential)]
    [UsedByNativeCode]
    public struct XRNodeState
    {
        // This sequence of members must match the C++ struct 'XRNodeStateToManaged' in XR.bindings
        private XRNode m_Type;
        private AvailableTrackingData m_AvailableFields;
        private Vector3 m_Position;
        private Quaternion m_Rotation;
        private Vector3 m_Velocity;
        private Vector3 m_AngularVelocity;
        private Vector3 m_Acceleration;
        private Vector3 m_AngularAcceleration;
        private int m_Tracked;
        private ulong m_UniqueID;

        // Properties
        public ulong uniqueID
        {
            get
            {
                return m_UniqueID;
            }
            set
            {
                m_UniqueID = value;
            }
        }

        public XRNode nodeType
        {
            get
            {
                return m_Type;
            }
            set
            {
                m_Type = value;
            }
        }

        public bool tracked
        {
            get
            {
                return m_Tracked == 1;
            }
            set
            {
                m_Tracked = value ? 1 : 0;
            }
        }

        public Vector3 position
        {
            set
            {
                m_Position = value;
                m_AvailableFields |= AvailableTrackingData.PositionAvailable;
            }
        }

        public Quaternion rotation
        {
            set
            {
                m_Rotation = value;
                m_AvailableFields |= AvailableTrackingData.RotationAvailable;
            }
        }

        public Vector3 velocity
        {
            set
            {
                m_Velocity = value;
                m_AvailableFields |= AvailableTrackingData.VelocityAvailable;
            }
        }

        public Vector3 angularVelocity
        {
            set
            {
                m_AngularVelocity = value;
                m_AvailableFields |= AvailableTrackingData.AngularVelocityAvailable;
            }
        }

        public Vector3 acceleration
        {
            set
            {
                m_Acceleration = value;
                m_AvailableFields |= AvailableTrackingData.AccelerationAvailable;
            }
        }

        public Vector3 angularAcceleration
        {
            set
            {
                m_AngularAcceleration = value;
                m_AvailableFields |= AvailableTrackingData.AngularAccelerationAvailable;
            }
        }

        // Getters
        public bool TryGetPosition(out Vector3 position)
        {
            return TryGet<Vector3>(m_Position, AvailableTrackingData.PositionAvailable, out position);
        }

        public bool TryGetRotation(out Quaternion rotation)
        {
            return TryGet<Quaternion>(m_Rotation, AvailableTrackingData.RotationAvailable, out rotation);
        }

        public bool TryGetVelocity(out Vector3 velocity)
        {
            return TryGet<Vector3>(m_Velocity, AvailableTrackingData.VelocityAvailable, out velocity);
        }

        public bool TryGetAngularVelocity(out Vector3 angularVelocity)
        {
            return TryGet<Vector3>(m_AngularVelocity, AvailableTrackingData.AngularVelocityAvailable, out angularVelocity);
        }

        public bool TryGetAcceleration(out Vector3 acceleration)
        {
            return TryGet<Vector3>(m_Acceleration, AvailableTrackingData.AccelerationAvailable, out acceleration);
        }

        public bool TryGetAngularAcceleration(out Vector3 angularAcceleration)
        {
            return TryGet<Vector3>(m_AngularAcceleration, AvailableTrackingData.AngularAccelerationAvailable, out angularAcceleration);
        }

        private bool TryGet<T>(T inValue, AvailableTrackingData availabilityFlag, out T outValue) where T : new()
        {
            if ((m_AvailableFields & availabilityFlag) > 0)
            {
                outValue = inValue;
                return true;
            }
            else
            {
                outValue = new T();
                return false;
            }
        }
    }
}
