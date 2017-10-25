// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using System.Runtime.InteropServices;


namespace UnityEngine
{
    [NativeHeader("Runtime/Vehicles/WheelCollider.h")]
    public struct WheelHit
    {
        [NativeName("point")] private Vector3 m_Point;
        [NativeName("normal")] private Vector3 m_Normal;
        [NativeName("forwardDir")] private Vector3 m_ForwardDir;
        [NativeName("sidewaysDir")] private Vector3 m_SidewaysDir;
        [NativeName("force")] private float m_Force;
        [NativeName("forwardSlip")] private float m_ForwardSlip;
        [NativeName("sidewaysSlip")] private float m_SidewaysSlip;
        [NativeName("collider")] private Collider m_Collider;

        public Collider collider { get { return m_Collider; } set { m_Collider = value; }}

        public Vector3    point { get { return m_Point; } set { m_Point = value; } }
        public Vector3    normal { get { return m_Normal; } set { m_Normal = value; } }
        public Vector3    forwardDir { get { return m_ForwardDir; } set { m_ForwardDir = value; } }
        public Vector3    sidewaysDir { get { return m_SidewaysDir; } set { m_SidewaysDir = value; } }
        public float      force { get { return m_Force; } set { m_Force = value; } }
        public float      forwardSlip { get { return m_ForwardSlip; } set { m_ForwardSlip = value; } }
        public float      sidewaysSlip { get { return m_SidewaysSlip; } set { m_SidewaysSlip = value; } }
    }

    [NativeHeader("Runtime/Vehicles/WheelCollider.h")]
    [NativeHeader("PhysicsScriptingClasses.h")]
    public class WheelCollider : Collider
    {
        public extern Vector3 center {get; set; }
        public extern float radius {get; set; }
        public extern float suspensionDistance {get; set; }
        public extern JointSpring suspensionSpring {get; set; }
        public extern float forceAppPointDistance {get; set; }
        public extern float mass {get; set; }
        public extern float wheelDampingRate {get; set; }
        public extern WheelFrictionCurve forwardFriction {get; set; }
        public extern WheelFrictionCurve sidewaysFriction {get; set; }
        public extern float motorTorque {get; set; }
        public extern float brakeTorque {get; set; }
        public extern float steerAngle {get; set; }
        public extern bool isGrounded {[NativeName("IsGrounded")] get; }
        public extern float rpm { get; }
        public extern float sprungMass { get; }

        public extern void ConfigureVehicleSubsteps(float speedThreshold, int stepsBelowThreshold, int stepsAboveThreshold);

        public extern void GetWorldPose(out Vector3 pos, out Quaternion quat);
        public extern bool GetGroundHit(out WheelHit hit);
    }
}

