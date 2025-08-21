// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;

using static UnityEngine.LowLevelPhysics2D.PhysicsLowLevelScripting2D;

namespace UnityEngine.LowLevelPhysics2D
{
    /// <summary>
    /// A joint definition used to specify properties when creating a <see cref="LowLevelPhysics2D.PhysicsDistanceJoint"/>.
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct PhysicsDistanceJointDefinition
    {
        /// <summary>
        /// Create a default <see cref="LowLevelPhysics2D.PhysicsDistanceJoint"/> definition.
        /// </summary>
        public PhysicsDistanceJointDefinition() { this = defaultDefinition; }

        /// <summary>
        /// Create a default <see cref="LowLevelPhysics2D.PhysicsDistanceJoint"/> definition.
        /// </summary>
        /// <param name="useSettings">Controls whether the default settings come from the physics settings or not.</param>
        public PhysicsDistanceJointDefinition(bool useSettings) { this = DistanceJoint_GetDefaultDefinition(useSettings); }

        /// <summary>
        /// Get a default <see cref="LowLevelPhysics2D.PhysicsDistanceJoint"/> definition.
        /// </summary>
        public static PhysicsDistanceJointDefinition defaultDefinition => DistanceJoint_GetDefaultDefinition(true);

        /// <summary>
        /// The first body the joint constrains.
        /// </summary>
        public PhysicsBody bodyA { readonly get => m_BodyA; set => m_BodyA = value; }

        /// <summary>
        /// The second body the joint constrains.
        /// </summary>
        public PhysicsBody bodyB { readonly get => m_BodyB; set => m_BodyB = value; }

        /// <summary>
        /// The local anchor frame constraint relative to bodyA's origin.
        /// </summary>
        public PhysicsTransform localAnchorA { readonly get => m_LocalAnchorA; set => m_LocalAnchorA = value; }

        /// <summary>
        /// The local anchor frame constraint relative to bodyB's origin.
        /// </summary>
        public PhysicsTransform localAnchorB { readonly get => m_LocalAnchorB; set => m_LocalAnchorB = value; }

        /// <summary>
        /// The desired distance constraint i.e. the rest length of this joint.
        /// This has a lower stable limit of just above zero.
        /// </summary>
        public float distance { readonly get => m_Distance; set => m_Distance = Math.Max(float.Epsilon, value); }

        /// <summary>
        /// Enable/Disable the distance constraint to behave like a spring.
        /// If false then the distance joint will be rigid, overriding the limit and motor.
        /// </summary>
        public bool enableSpring { readonly get => m_EnableSpring; set => m_EnableSpring = value; }

        /// <summary>
        /// The spring linear stiffness frequency, in cycles per second.
        /// </summary>
        public float springFrequency { readonly get => m_SpringFrequency; set => m_SpringFrequency = Mathf.Max(0f, value); }

        /// <summary>
        /// The spring linear damping, non-dimensional.
        /// Use 1 for critical damping.
        /// </summary>
        public float springDamping { readonly get => m_SpringDamping; set => m_SpringDamping = Mathf.Max(0f, value); }


        /// <summary>
        /// The lower spring force controls how much tension the spring can sustain.
        /// </summary>
        public float springLowerForce { readonly get => m_SpringLowerForce; set => m_SpringLowerForce = value; }

        /// <summary>
        /// The upper spring force controls how much compression the spring can sustain.
        /// </summary>
        public float springUpperForce { readonly get => m_SpringUpperForce; set => m_SpringUpperForce = value; }

        /// <summary>
        /// Enable/Disable the joint motor.
        /// </summary>
        public bool enableMotor { readonly get => m_EnableMotor; set => m_EnableMotor = value; }

        /// <summary>
        /// The desired motor speed, usually in meters per second.
        /// </summary>
        public float motorSpeed { readonly get => m_MotorSpeed; set => m_MotorSpeed = value; }

        /// <summary>
        /// The maximum force the motor can apply, usually in newtons.
        /// </summary>
        public float maxMotorForce { readonly get => m_MaxMotorForce; set => m_MaxMotorForce = value; }

        /// <summary>
        /// Enable/disable the joint limit.
        /// </summary>
        public bool enableLimit { readonly get => m_EnableLimit; set => m_EnableLimit = value; }

        /// <summary>
        /// Minimum length limit of this joint.
        /// This will be clamped to a lower stable limit.
        /// </summary>
        public float minDistanceLimit { readonly get => m_MinDistanceLimit; set => m_MinDistanceLimit = Mathf.Max(0f, value); }

        /// <summary>
        /// Maximum length limit of this joint.
        /// Must be greater than or equal to the minimum length.
        /// </summary>
        public float maxDistanceLimit { readonly get => m_MaxDistanceLimit; set => m_MaxDistanceLimit = Mathf.Max(0f, value); }

        /// <summary>
        /// The force threshold beyond which a joint event will be produced.
        /// </summary>
        public float forceThreshold { readonly get => m_ForceThreshold; set => m_ForceThreshold = Mathf.Max(0f, value); }

        /// <summary>
        /// The torque threshold beyond which a joint event will be produced.
        /// </summary>
        public float torqueThreshold { readonly get => m_TorqueThreshold; set => m_TorqueThreshold = Mathf.Max(0f, value); }

        /// <summary>
        /// Controls the joint stiffness frequency, in cycles per second.
        /// </summary>
        public float tuningFrequency { readonly get => m_TuningFrequency; set => m_TuningFrequency = Mathf.Clamp(value, 0f, 1000f); }

        /// <summary>
        /// Controls the joint stiffness damping, non-dimensional.
        /// Use 1 for critical damping.
        /// </summary>
        public float tuningDamping { readonly get => m_TuningDamping; set => m_TuningDamping = Mathf.Clamp(value, 0f, 10f); }

        /// <summary>
        /// Controls the scaling of the joint drawing.
        /// Not all joints have scalable elements but those that do will use this scaling.
        /// </summary>
        public float drawScale { readonly get => m_DrawScale; set => m_DrawScale = Mathf.Clamp(value, 0.001f, 10f); }

        /// <summary>
        /// Whether the shapes on the pair of bodies can come into contact.
        /// </summary>
        public bool collideConnected { readonly get => m_CollideConnected; set => m_CollideConnected = value; }

        #region Internal

        [SerializeField] PhysicsBody m_BodyA;
        [SerializeField] PhysicsBody m_BodyB;
        [SerializeField] PhysicsTransform m_LocalAnchorA;
        [SerializeField] PhysicsTransform m_LocalAnchorB;
        [SerializeField] [Min(float.Epsilon)] float m_Distance;
        [SerializeField] bool m_EnableSpring;
        [SerializeField] [Min(0.0f)] float m_SpringFrequency;
        [SerializeField] [Min(0.0f)] float m_SpringDamping;
        [SerializeField] float m_SpringLowerForce;
        [SerializeField] float m_SpringUpperForce;
        [SerializeField] bool m_EnableMotor;
        [SerializeField] float m_MotorSpeed;
        [SerializeField] float m_MaxMotorForce;
        [SerializeField] bool m_EnableLimit;
        [SerializeField] [Min(0.0f)] float m_MinDistanceLimit;
        [SerializeField] [Min(0.0f)] float m_MaxDistanceLimit;
        [SerializeField] [Min(0.0f)] float m_ForceThreshold;
        [SerializeField] [Min(0.0f)] float m_TorqueThreshold;
        [SerializeField] [Range(0.0f, 1000.0f)] float m_TuningFrequency;
        [SerializeField] [Range(0.0f, 10.0f)] float m_TuningDamping;
        [SerializeField] [Range(0.001f, 10.0f)] float m_DrawScale;
        [SerializeField] bool m_CollideConnected;

        #endregion
    }
}
