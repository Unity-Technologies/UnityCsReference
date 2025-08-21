// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;

using static UnityEngine.LowLevelPhysics2D.PhysicsLowLevelScripting2D;

namespace UnityEngine.LowLevelPhysics2D
{
    /// <summary>
    /// A joint definition used to specify properties when creating a <see cref="LowLevelPhysics2D.PhysicsHingeJoint"/>.
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct PhysicsHingeJointDefinition
    {
        /// <summary>
        /// Create a default <see cref="LowLevelPhysics2D.PhysicsHingeJoint"/> definition.
        /// </summary>
        public PhysicsHingeJointDefinition() { this = defaultDefinition; }

        /// <summary>
        /// Create a default <see cref="LowLevelPhysics2D.PhysicsHingeJoint"/> definition.
        /// </summary>
        /// <param name="useSettings">Controls whether the default settings come from the physics settings or not.</param>
        public PhysicsHingeJointDefinition(bool useSettings) { this = HingeJoint_GetDefaultDefinition(useSettings); }

        /// <summary>
        /// Create a default <see cref="LowLevelPhysics2D.PhysicsHingeJoint"/> definition.
        /// </summary>
        public static PhysicsHingeJointDefinition defaultDefinition => HingeJoint_GetDefaultDefinition(true);

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
        /// Enable/Disable the rotational spring.
        /// </summary>
        public bool enableSpring { readonly get => m_EnableSpring; set => m_EnableSpring = value; }

        /// <summary>
	    /// The spring target angle, in degrees.
        /// </summary>
        public float springTargetAngle { readonly get => m_SpringTargetAngle; set => m_SpringTargetAngle = value; }

        /// <summary>
        /// The spring stiffness frequncy, in cycles per second.
        /// </summary>
        public float springFrequency { readonly get => m_SpringFrequency; set => m_SpringFrequency = Mathf.Max(0f, value); }

        /// <summary>
        /// The spring damping, non-dimensional.
        /// Use 1 for critical damping.
        /// </summary>
        public float springDamping { readonly get => m_SpringDamping; set => m_SpringDamping = Mathf.Max(0f, value); }

        /// <summary>
        /// Enable/disable the joint motor.
        /// </summary>
        public bool enableMotor { readonly get => m_EnableMotor; set => m_EnableMotor = value; }

        /// <summary>
        /// The desired motor speed, usually in meters per second.
        /// </summary>
        public float motorSpeed { readonly get => m_MotorSpeed; set => m_MotorSpeed = value; }

        /// <summary>
        /// The maximum torque the motor can apply, usually in newton-meters.
        /// </summary>
        public float maxMotorTorque { readonly get => m_MaxMotorTorque; set => m_MaxMotorTorque = Mathf.Max(0f, value); }

        /// <summary>
        /// Enable/disable the joint angle limit.
        /// </summary>
        public bool enableLimit { readonly get => m_EnableLimit; set => m_EnableLimit = value; }

        /// <summary>
        /// The lower angle limit, in degrees.
        /// </summary>
        public float lowerAngleLimit { readonly get => m_LowerAngleLimit; set => m_LowerAngleLimit = Mathf.Clamp(value, -178f, 178f); }

        /// <summary>
        /// The upper angle limit, in degrees.
        /// </summary>
        public float upperAngleLimit { readonly get => m_UpperAngleLimit; set => m_UpperAngleLimit = Mathf.Clamp(value, -178f, 178f); }

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
        /// Controls the joint stiffness damping, non-dimensional. Use 1 for critical damping.
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
        [SerializeField] bool m_EnableSpring;
        [SerializeField] float m_SpringTargetAngle;
        [SerializeField] [Min(0.0f)] float m_SpringFrequency;
        [SerializeField] [Min(0.0f)] float m_SpringDamping;
        [SerializeField] bool m_EnableMotor;
        [SerializeField] float m_MotorSpeed;
        [SerializeField] [Min(0.0f)] float m_MaxMotorTorque;
        [SerializeField] bool m_EnableLimit;
        [SerializeField] [Range(-178f, 178f)] float m_LowerAngleLimit;
        [SerializeField] [Range(-178f, 178f)] float m_UpperAngleLimit;
        [SerializeField] [Min(0.0f)] float m_ForceThreshold;
        [SerializeField] [Min(0.0f)] float m_TorqueThreshold;
        [SerializeField] [Range(0.0f, 1000.0f)] float m_TuningFrequency;
        [SerializeField] [Range(0.0f, 10.0f)] float m_TuningDamping;
        [SerializeField] [Range(0.001f, 10.0f)] float m_DrawScale;
        [SerializeField] bool m_CollideConnected;

        #endregion
    }
}
