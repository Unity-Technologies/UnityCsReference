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
    /// A joint definition used to specify properties when creating a <see cref="PhysicsRelativeJoint"/>.
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    [MovedFrom(autoUpdateAPI: ScriptUpdateConstants.AutoUpdateAPI, sourceNamespace: ScriptUpdateConstants.SourceNamespace, sourceAssembly: ScriptUpdateConstants.SourceAssembly)]
    public struct PhysicsRelativeJointDefinition
    {
        /// <summary>
        /// Create a default <see cref="PhysicsRelativeJoint"/> definition.
        /// </summary>
        public PhysicsRelativeJointDefinition() { this = defaultDefinition; }

        /// <summary>
        /// Create a default <see cref="PhysicsRelativeJoint"/> definition.
        /// </summary>
        /// <param name="useSettings">Controls whether the default settings come from the physics settings or not.</param>
        public PhysicsRelativeJointDefinition(bool useSettings) { this = RelativeJoint_GetDefaultDefinition(useSettings); }

        /// <summary>
        /// Create a default <see cref="PhysicsRelativeJoint"/> definition.
        /// </summary>
        public static PhysicsRelativeJointDefinition defaultDefinition => RelativeJoint_GetDefaultDefinition(true);

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
	    /// The desired linear velocity.
        /// </summary>
        public Vector2 linearVelocity { readonly get => m_LinearVelocity; set => m_LinearVelocity = value; }

        /// <summary>
	    /// The desired angular velocity.
        /// </summary>
        public float angularVelocity { readonly get => m_AngularVelocity; set => m_AngularVelocity = value; }

        /// <summary>
	    /// The maximum linear force, usually in newtons.
        /// A value of zero is a special case which turns the limit off.
        /// </summary>
        public float maxForce { readonly get => m_MaxForce; set => m_MaxForce = Mathf.Max(0f, value); }

        /// <summary>
	    /// The maximum torque, usually in newton-meters.
        /// A value of zero is a special case which turns the limit off.
        /// </summary>
        public float maxTorque { readonly get => m_MaxTorque; set => m_MaxTorque = Mathf.Max(0f, value); }

        /// <summary>
	    /// The spring linear frequency, in cycles per second.
        /// A value of zero is a special case which turns the linear spring off.
        /// </summary>
        public float springLinearFrequency { readonly get => m_SpringLinearFrequency; set => m_SpringLinearFrequency = Mathf.Max(0f, value); }

        /// <summary>
	    /// The spring angular frequency, in cycles per second.
        /// A value of zero is a special case which turns the angular spring off.
        /// </summary>
        public float springAngularFrequency { readonly get => m_SpringAngularFrequency; set => m_SpringAngularFrequency = Mathf.Max(0f, value); }

        /// <summary>
	    /// The spring linear damping.
        /// Use 1 for critical damping.
        /// </summary>
        public float springLinearDamping { readonly get => m_SpringLinearDamping; set => m_SpringLinearDamping = Mathf.Max(0f, value); }

        /// <summary>
	    /// The spring angular damping.
        /// Use 1 for critical damping.
        /// </summary>
        public float springAngularDamping { readonly get => m_SpringAngularDamping; set => m_SpringAngularDamping = Mathf.Max(0f, value); }

        /// <summary>
	    /// The spring maximum linear force, usually in newtons.
        /// A value of zero is a special case which turns the force limit off.
        /// </summary>
        public float springMaxForce { readonly get => m_SpringMaxForce; set => m_SpringMaxForce = Mathf.Max(0f, value); }

        /// <summary>
	    /// The spring maximum torque, usually in newton-meters.
        /// A value of zero is a special case which turns the torque limit off.
        /// </summary>
        public float springMaxTorque { readonly get => m_SpringMaxTorque; set => m_SpringMaxTorque = Mathf.Max(0f, value); }

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
        [SerializeField] PhysicsTransform m_LocalAnchorA;
        [SerializeField] PhysicsTransform m_LocalAnchorB;

	    [SerializeField] Vector2 m_LinearVelocity;
	    [SerializeField] float m_AngularVelocity;
	    [SerializeField] [Min(0.0f)] float m_MaxForce;
	    [SerializeField] [Min(0.0f)] float m_MaxTorque;
	    [SerializeField] [Min(0.0f)] float m_SpringLinearFrequency;
	    [SerializeField] [Min(0.0f)] float m_SpringAngularFrequency;
	    [SerializeField] [Min(0.0f)] float m_SpringLinearDamping;
	    [SerializeField] [Min(0.0f)] float m_SpringAngularDamping;
	    [SerializeField] [Min(0.0f)] float m_SpringMaxForce;
	    [SerializeField] [Min(0.0f)] float m_SpringMaxTorque;

        [SerializeField] [Min(0.0f)] float m_ForceThreshold;
        [SerializeField] [Min(0.0f)] float m_TorqueThreshold;
        [SerializeField] [Range(0.0f, 1000.0f)] float m_TuningFrequency;
        [SerializeField] [Range(0.0f, 10.0f)] float m_TuningDamping;
        [SerializeField] [Range(0.0001f, 10.0f)] float m_DrawScale;
        [SerializeField] bool m_WorldDrawing;
        [SerializeField] bool m_CollideConnected;

        #endregion
    }
}
