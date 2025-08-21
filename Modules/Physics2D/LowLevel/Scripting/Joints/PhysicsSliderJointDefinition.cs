// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;

using static UnityEngine.LowLevelPhysics2D.PhysicsLowLevelScripting2D;

namespace UnityEngine.LowLevelPhysics2D
{
    /// <summary>
    /// A joint definition used to specify properties when creating a <see cref="LowLevelPhysics2D.PhysicsSliderJoint"/>.
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct PhysicsSliderJointDefinition
    {
        /// <summary>
        /// Create a default <see cref="LowLevelPhysics2D.PhysicsSliderJoint"/> definition.
        /// </summary>
        public PhysicsSliderJointDefinition() { this = defaultDefinition; }

        /// <summary>
        /// Create a default <see cref="LowLevelPhysics2D.PhysicsSliderJoint]"/> definition.
        /// </summary>
        /// <param name="useSettings">Controls whether the default settings come from the physics settings or not.</param>
        public PhysicsSliderJointDefinition(bool useSettings) { this = SliderJoint_GetDefaultDefinition(useSettings); }

        /// <summary>
        /// Get a default <see cref="LowLevelPhysics2D.PhysicsSliderJoint"/> definition.
        /// </summary>
        public static PhysicsSliderJointDefinition defaultDefinition => SliderJoint_GetDefaultDefinition(true);

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
        /// Enable/Disable a spring along the slider joint axis.
        /// </summary>
        public bool enableSpring { readonly get => m_EnableSpring; set => m_EnableSpring = value; }

        /// <summary>
	    /// The spring target translation, usually in meters.
        /// The spring-damper will drive to this translation.
        /// </summary>
        public float springTargetTranslation { readonly get => m_SpringTargetTranslation; set => m_SpringTargetTranslation = value; }

        /// <summary>
        /// The spring stiffness frequency, in cycles per second.
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
        /// The maximum force the motor can apply, usually in newtons.
        /// </summary>
        public float maxMotorForce { readonly get => m_MaxMotorForce; set => m_MaxMotorForce = Mathf.Max(0f, value); }

        /// <summary>
        /// Enable/disable the joint translation limit.
        /// </summary>
        public bool enableLimit { readonly get => m_EnableLimit; set => m_EnableLimit = value; }

        /// <summary>
        /// The lower translation limit of this joint.
        /// This will be clamped to a lower stable limit.
        /// </summary>
        public float lowerTranslationLimit { readonly get => m_LowerTranslationLimit; set => m_LowerTranslationLimit = value; }

        /// <summary>
        /// The upper trasnlation limit of this joint.
        /// Must be greater than or equal to the minimum length.
        /// </summary>
        public float upperTranslationLimit { readonly get => m_UpperTranslationLimit; set => m_UpperTranslationLimit = value; }

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
        [SerializeField] bool m_EnableSpring;
        [SerializeField] float m_SpringTargetTranslation;
        [SerializeField] [Min(0.0f)] float m_SpringFrequency;
        [SerializeField] [Min(0.0f)] float m_SpringDamping;
        [SerializeField] bool m_EnableMotor;
        [SerializeField] float m_MotorSpeed;
        [SerializeField] [Min(0.0f)] float m_MaxMotorForce;
        [SerializeField] bool m_EnableLimit;
        [SerializeField] float m_LowerTranslationLimit;
        [SerializeField] float m_UpperTranslationLimit;
        [SerializeField] [Min(0.0f)] float m_ForceThreshold;
        [SerializeField] [Min(0.0f)] float m_TorqueThreshold;
        [SerializeField] [Range(0.0f, 1000.0f)] float m_TuningFrequency;
        [SerializeField] [Range(0.0f, 10.0f)] float m_TuningDamping;
        [SerializeField] [Range(0.001f, 10.0f)] float m_DrawScale;
        [SerializeField] bool m_CollideConnected;

        #endregion
    }
}
