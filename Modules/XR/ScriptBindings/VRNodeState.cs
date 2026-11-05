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

    ///<summary>Describes the state of a node tracked by an XR system.</summary>
    ///<remarks>To track available XR nodes and acquire state data, handle the <see cref="InputTracking.nodeAdded" /> and <see cref="InputTracking.nodeRemoved" /> events or call <see cref="InputTracking.GetNodeStates" />.
    ///
    ///            Not all XR platforms provide complete tracking data. Use the methods <see cref="XR.XRNodeState.TryGetPosition" />, <see cref="XR.XRNodeState.TryGetRotation" />, etc. to read the data if it's available.
    ///
    ///            XR devices can be accessed in different ways, with the XR Node representing a physical input source such as a head position, hand, or camera.
    ///
    ///            See [XR Input](xref:xr_input) for an overview of accessing XR devices.</remarks>
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
        ///<summary>The unique identifier of the tracked node.</summary>
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

        ///<summary>The type of the tracked node as specified in <see cref="XR.XRNode" />.</summary>
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

        ///<summary>Set to true if the node is presently being tracked by the underlying XR system,
        ///and false if the node is not presently being tracked by the underlying XR system.</summary>
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

        ///<summary>Sets the vector representing the current position of the tracked node.</summary>
        public Vector3 position
        {
            set
            {
                m_Position = value;
                m_AvailableFields |= AvailableTrackingData.PositionAvailable;
            }
        }

        ///<summary>Sets the quaternion representing the current rotation of the tracked node.</summary>
        public Quaternion rotation
        {
            set
            {
                m_Rotation = value;
                m_AvailableFields |= AvailableTrackingData.RotationAvailable;
            }
        }

        ///<summary>Sets the vector representing the current velocity of the tracked node.</summary>
        public Vector3 velocity
        {
            set
            {
                m_Velocity = value;
                m_AvailableFields |= AvailableTrackingData.VelocityAvailable;
            }
        }

        ///<summary>Sets the vector representing the current angular velocity of the tracked node.</summary>
        public Vector3 angularVelocity
        {
            set
            {
                m_AngularVelocity = value;
                m_AvailableFields |= AvailableTrackingData.AngularVelocityAvailable;
            }
        }

        ///<summary>Sets the vector representing the current acceleration of the tracked node.</summary>
        public Vector3 acceleration
        {
            set
            {
                m_Acceleration = value;
                m_AvailableFields |= AvailableTrackingData.AccelerationAvailable;
            }
        }

        ///<summary>Sets the vector representing the current angular acceleration of the tracked node.</summary>
        public Vector3 angularAcceleration
        {
            set
            {
                m_AngularAcceleration = value;
                m_AvailableFields |= AvailableTrackingData.AngularAccelerationAvailable;
            }
        }

        // Getters
        ///<summary>Attempt to retrieve a vector representing the current position of the tracked node.</summary>
        ///<param name="position">A Vector3 that the method sets to the current position of the tracked node, or to <see cref="Vector3.zero" /> if the position isn't available.</param>
        ///<returns>True if the position was set in the output parameter. False if the position is not available due to limitations of the underlying platform or if the node is not presently tracked.</returns>
        public bool TryGetPosition(out Vector3 position)
        {
            return TryGet(m_Position, AvailableTrackingData.PositionAvailable, out position);
        }

        ///<summary>Attempt to retrieve a quaternion representing the current rotation of the tracked node.</summary>
        ///<param name="rotation">A Quaternion that the method sets to the current rotation of the tracked node, or to <see cref="Quaternion.identity" /> if the rotation isn't available.</param>
        ///<returns>True if the rotation was set in the output parameter. False if the rotation is not available due to limitations of the underlying platform or if the node is not presently tracked.</returns>
        public bool TryGetRotation(out Quaternion rotation)
        {
            return TryGet(m_Rotation, AvailableTrackingData.RotationAvailable, out rotation);
        }

        ///<summary>Attempt to retrieve a vector representing the current velocity of the tracked node.</summary>
        ///<param name="velocity">A Vector3 that the method sets to the current velocity of the tracked node, or to <see cref="Vector3.zero" /> if the velocity isn't available.</param>
        ///<returns>True if the velocity was set in the output parameter. False if the velocity is not available due to limitations of the underlying platform or if the node is not presently tracked.</returns>
        public bool TryGetVelocity(out Vector3 velocity)
        {
            return TryGet(m_Velocity, AvailableTrackingData.VelocityAvailable, out velocity);
        }

        ///<summary>Attempt to retrieve a Vector3 representing the current angular velocity of the tracked node.</summary>
        ///<param name="angularVelocity">A Vector3 that the method sets to the current angular velocity of the tracked node, or to <see cref="Vector3.zero" /> if the angular velocity isn't available.</param>
        ///<returns>True if the angular velocity was set in the output parameter. False if the angular velocity is not available due to limitations of the underlying platform or if the node is not presently tracked.</returns>
        public bool TryGetAngularVelocity(out Vector3 angularVelocity)
        {
            return TryGet(m_AngularVelocity, AvailableTrackingData.AngularVelocityAvailable, out angularVelocity);
        }

        ///<summary>Attempt to retrieve a vector representing the current acceleration of the tracked node.</summary>
        ///<param name="acceleration">A Vector3 that the method sets to the current acceleration of the tracked node, or to <see cref="Vector3.zero" /> if the acceleration isn't available.</param>
        ///<returns>True if the acceleration was set in the output parameter. False if the acceleration is not available due to limitations of the underlying platform or if the node is not presently tracked.</returns>
        public bool TryGetAcceleration(out Vector3 acceleration)
        {
            return TryGet(m_Acceleration, AvailableTrackingData.AccelerationAvailable, out acceleration);
        }

        ///<summary>Attempt to retrieve a Vector3 representing the current angular acceleration of the tracked node.</summary>
        ///<param name="angularAcceleration">A Vector3 that the method sets to the current angular acceleration of the tracked node, or to <see cref="Vector3.zero" /> if the angular acceleration isn't available.</param>
        ///<returns>True if the angular acceleration was set in the output parameter. False if the angular acceleration is not available due to limitations of the underlying platform or if the node is not presently tracked.</returns>
        public bool TryGetAngularAcceleration(out Vector3 angularAcceleration)
        {
            return TryGet(m_AngularAcceleration, AvailableTrackingData.AngularAccelerationAvailable, out angularAcceleration);
        }

        private bool TryGet(Vector3 inValue, AvailableTrackingData availabilityFlag, out Vector3 outValue)
        {
            if ((m_AvailableFields & availabilityFlag) > 0)
            {
                outValue = inValue;
                return true;
            }
            else
            {
                outValue = Vector3.zero;
                return false;
            }
        }

        private bool TryGet(Quaternion inValue, AvailableTrackingData availabilityFlag, out Quaternion outValue)
        {
            if ((m_AvailableFields & availabilityFlag) > 0)
            {
                outValue = inValue;
                return true;
            }
            else
            {
                outValue = Quaternion.identity;
                return false;
            }
        }
    }
}
