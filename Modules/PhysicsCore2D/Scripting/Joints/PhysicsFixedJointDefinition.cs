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
    /// A joint definition used to specify properties when creating a <see cref="PhysicsFixedJoint"/>.
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    [MovedFrom(autoUpdateAPI: ScriptUpdateConstants.AutoUpdateAPI, sourceNamespace: ScriptUpdateConstants.SourceNamespace, sourceAssembly: ScriptUpdateConstants.SourceAssembly)]
    public struct PhysicsFixedJointDefinition
    {
        /// <summary>
        /// Create a default <see cref="PhysicsFixedJoint"/> definition.
        /// </summary>
        public PhysicsFixedJointDefinition() { this = defaultDefinition; }

        /// <summary>
        /// Create a default <see cref="PhysicsFixedJoint"/> definition.
        /// </summary>
        /// <param name="useSettings">Controls whether the default settings come from the physics settings or not.</param>
        public PhysicsFixedJointDefinition(bool useSettings) { this = FixedJoint_GetDefaultDefinition(useSettings); }

        /// <summary>
        /// Get a default <see cref="PhysicsFixedJoint"/> definition.
        /// </summary>
        public static PhysicsFixedJointDefinition defaultDefinition => FixedJoint_GetDefaultDefinition(true);

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
        /// Linear stiffness frequency, in cycles per second.
        /// Use zero for maximum stiffness.
        /// </summary>
        public float linearFrequency { readonly get => m_LinearFrequency; set => m_LinearFrequency = Mathf.Max(0f, value); }

        /// <summary>
        /// Linear damping, non-dimensional.
        /// Use 1 for critical damping.
        /// </summary>
        public float linearDamping { readonly get => m_LinearDamping; set => m_LinearDamping = Mathf.Max(0f, value); }

        /// <summary>
        /// Angular stiffness frequency, in cycles per second.
        /// Use zero for maximum stiffness.
        /// </summary>
        public float angularFrequency { readonly get => m_AngularFrequency; set => m_AngularFrequency = Mathf.Max(0f, value); }

        /// <summary>
        /// Angular damping, non-dimensional.
        /// Use 1 for critical damping.
        /// </summary>
        public float angularDamping { readonly get => m_AngularDamping; set => m_AngularDamping = Mathf.Max(0f, value); }

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
        [SerializeField] [Min(0.0f)] float m_LinearFrequency;
        [SerializeField] [Min(0.0f)] float m_LinearDamping;
        [SerializeField] [Min(0.0f)] float m_AngularFrequency;
        [SerializeField] [Min(0.0f)] float m_AngularDamping;
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
