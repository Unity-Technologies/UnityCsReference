// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using static Unity.U2D.Physics.Scripting2D;

namespace Unity.U2D.Physics
{
    /// <summary>
    /// A joint definition used to specify properties when creating a <see cref="PhysicsDistanceJoint"/>.
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    [MovedFrom(autoUpdateAPI: ScriptUpdateConstants.AutoUpdateAPI, sourceNamespace: ScriptUpdateConstants.SourceNamespace, sourceAssembly: ScriptUpdateConstants.SourceAssembly)]
    public record struct PhysicsDistanceJointDefinition
    {
        /// <summary>
        /// Create a default <see cref="PhysicsDistanceJoint"/> definition.
        /// </summary>
        public PhysicsDistanceJointDefinition() { this = defaultDefinition; }

        /// <summary>
        /// Create a default <see cref="PhysicsDistanceJoint"/> definition.
        /// </summary>
        /// <param name="useSettings">Controls whether the default settings come from the physics settings or not.</param>
        public PhysicsDistanceJointDefinition(bool useSettings) { this = DistanceJoint_GetDefaultDefinition(useSettings); }

        /// <summary>
        /// Get a default <see cref="PhysicsDistanceJoint"/> definition.
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
        /// When set, <see cref="localAnchorA"/> is recomputed from the bodies' current placement at create so both anchor frames coincide in world space.
        /// </summary>
        /// <remarks>
        /// This removes the solver jolt from an inconsistent initial configuration.
        /// It is applied at create only; the authored <see cref="localAnchorA"/> is ignored while this is set.
        /// </remarks>
        public bool autoAnchorA { readonly get => m_AutoAnchorA; set => m_AutoAnchorA = value; }

        /// <summary>
        /// When set, <see cref="localAnchorB"/> is recomputed from the bodies' current placement at create so both anchor frames coincide in world space.
        /// </summary>
        /// <remarks>
        /// This removes the solver jolt from an inconsistent initial configuration.
        /// It is applied at create only; the authored <see cref="localAnchorB"/> is ignored while this is set.
        /// </remarks>
        public bool autoAnchorB { readonly get => m_AutoAnchorB; set => m_AutoAnchorB = value; }

        /// <summary>
        /// When set, <see cref="distance"/> is recomputed at create from the world separation of the two anchors.
        /// </summary>
        /// <remarks>
        /// It is measured after any <see cref="autoAnchorA"/>/<see cref="autoAnchorB"/> resolution, so it reflects the anchors actually used.
        /// It is applied at create only; the authored <see cref="distance"/> is ignored while this is set.
        /// </remarks>
        public bool autoDistance { readonly get => m_AutoDistance; set => m_AutoDistance = value; }

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
        /// Controls whether this joint is automatically drawn when the world is drawn.
        ///
        /// See <see cref="PhysicsJoint.worldDrawing"/>.
        /// </summary>
        public bool worldDrawing { readonly get => m_WorldDrawing; set => m_WorldDrawing = value; }

        /// <summary>
        /// Whether the shapes on the pair of bodies can come into contact.
        /// </summary>
        public bool collideConnected { readonly get => m_CollideConnected; set => m_CollideConnected = value; }

        #region Internal

        PhysicsBody m_BodyA;
        PhysicsBody m_BodyB;
        [SerializeField] internal PhysicsTransform m_LocalAnchorA;
        [SerializeField] internal PhysicsTransform m_LocalAnchorB;
        [SerializeField] internal bool m_AutoAnchorA;
        [SerializeField] internal bool m_AutoAnchorB;
        [SerializeField] internal bool m_AutoDistance;
        [SerializeField] [Min(float.Epsilon)] internal float m_Distance;
        [SerializeField] internal bool m_EnableSpring;
        [SerializeField] [Min(0.0f)] internal float m_SpringFrequency;
        [SerializeField] [Min(0.0f)] internal float m_SpringDamping;
        [SerializeField] internal float m_SpringLowerForce;
        [SerializeField] internal float m_SpringUpperForce;
        [SerializeField] internal bool m_EnableMotor;
        [SerializeField] internal float m_MotorSpeed;
        [SerializeField] internal float m_MaxMotorForce;
        [SerializeField] internal bool m_EnableLimit;
        [SerializeField] [Min(0.0f)] internal float m_MinDistanceLimit;
        [SerializeField] [Min(0.0f)] internal float m_MaxDistanceLimit;
        [SerializeField] [Min(0.0f)] internal float m_ForceThreshold;
        [SerializeField] [Min(0.0f)] internal float m_TorqueThreshold;
        [SerializeField] [Range(0.0f, 1000.0f)] internal float m_TuningFrequency;
        [SerializeField] [Range(0.0f, 10.0f)] internal float m_TuningDamping;
        [SerializeField] [Range(0.0001f, 10.0f)] internal float m_DrawScale;
        [SerializeField] internal bool m_WorldDrawing;
        [SerializeField] internal bool m_CollideConnected;

        #endregion
    }
}
