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
    /// A <see cref="PhysicsBody"/> definition used to specify important initial properties.
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    [MovedFrom(autoUpdateAPI: ScriptUpdateConstants.AutoUpdateAPI, sourceNamespace: ScriptUpdateConstants.SourceNamespace, sourceAssembly: ScriptUpdateConstants.SourceAssembly)]
    public partial struct PhysicsBodyDefinition
    {
        /// <summary>
        /// Create a default <see cref="PhysicsBody"/> definition.
        /// </summary>
        public PhysicsBodyDefinition() { this = defaultDefinition; }

        /// <summary>
        /// Create a default <see cref="PhysicsBody"/> definition.
        /// </summary>
        /// <param name="useSettings">Controls whether the default settings come from the physics settings or not.</param>
        public PhysicsBodyDefinition(bool useSettings) { this = PhysicsBody_GetDefaultDefinition(useSettings); }

        /// <summary>
        /// Get a default <see cref="PhysicsBody"/> definition.
        /// </summary>
        public static PhysicsBodyDefinition defaultDefinition => PhysicsBody_GetDefaultDefinition(true);

        /// <summary>
        /// A body is one of these three body types, Dynamic, Kinematic or Static, each of which determines how the body behaves in the simulation.
        /// </summary>
        public PhysicsBody.BodyType type { readonly get => m_BodyType; set => m_BodyType = value; }

        /// <summary>
        /// The degrees of freedom constraints (locks) for the body of Linear X, Linear Y and Rotation Z.
        /// </summary>
        public PhysicsBody.BodyConstraints constraints { readonly get => m_BodyConstraints; set => m_BodyConstraints = value; }

        /// <summary>
        /// The method used to Write the body pose to the Transform.
        /// </summary>
        public PhysicsBody.TransformWriteMode transformWriteMode { readonly get => m_TransformWriteMode; set => m_TransformWriteMode = value; }

        /// <summary>
        /// The initial position of the body, in world-space.
        /// Bodies should be created with the desired position as creating bodies at the origin and then moving them nearly doubles the cost of body creation, especially if the body is moved after shapes have been added.
        /// </summary>
        public Vector2 position { readonly get => m_Position; set => m_Position = value; }

        /// <summary>
        /// The initial rotation of the body, in world-space.
        /// Bodies should be created with the desired rotation as creating bodies at the origin and then rotating them nearly doubles the cost of body creation, especially if the body is moved after shapes have been added.
        /// </summary>
        public PhysicsRotate rotation { readonly get => m_Rotation; set => m_Rotation = value; }

        /// <summary>
        /// The initial linear velocity of the body's origin, in meters/sec.
        /// </summary>
        public Vector2 linearVelocity { readonly get => m_LinearVelocity; set => m_LinearVelocity = value; }

        /// <summary>
        /// The initial angular velocity of the body, in degrees per second.
        /// </summary>
        public float angularVelocity { readonly get => m_AngularVelocity; set => m_AngularVelocity = value; }

        /// <summary>
        /// Linear damping is use to reduce the linear velocity i.e. slow down translating bodies.
        /// The damping parameter can be larger than 1 but the damping effect becomes sensitive to the time step when the damping parameter is large.
        /// Generally linear damping is undesirable because it makes objects move slowly as if they are floating.
        /// </summary>
        public float linearDamping { readonly get => m_LinearDamping; set => m_LinearDamping = Mathf.Max(0f, value); }

        /// <summary>
        /// Angular damping is used to reduce the angular velocity over time i.e. slow down rotating bodies.
        /// The damping parameter can be larger than 1.0f but the damping effect becomes sensitive to the time step when the damping parameter is large.
        /// </summary>
        public float angularDamping { readonly get => m_AngularDamping; set => m_AngularDamping = Mathf.Max(0f, value); }

        /// <summary>
        /// Scale the gravity applied to this body, non-dimensional.
        /// </summary>
        public float gravityScale { readonly get => m_GravityScale; set => m_GravityScale = value; }

        /// <summary>
        /// A speed threshold below which the body is allowed to sleep, in meters/sec.
        /// </summary>
        public float sleepThreshold { readonly get => m_SleepThreshold; set => m_SleepThreshold = Mathf.Max(0f, value); }

        /// <summary>
        /// A threshold used to control when continuous collision detection is used when a body moves.
        /// The value is used to compare the body linear velocity movement against the extents of all the shapes added to the body scaled by this threshold.
        /// If the movement exceeds the extents scaled by the threshold then continuous collision detection is used to stop tunneling.
        /// Lower values reduce the distance the body must move before continuous collision detection is used and can have a considerable impact on performance!
        /// Higher values increase the distance the body must move before continuous collision detection is used.
        /// Too low a threshold will result in continuous collision detection being used more often therefore affecting performance so this should be limited to specific bodies only.
        /// The default threshold is 0.5 which equates to half the total shape extents.
        /// The threshold is clamped to a range of 0.0 to 1.0 with 0.0 meaning continuous collision detection will always be used.
        /// </summary>
        public float collisionThreshold { readonly get => m_CollisionThreshold; set => m_CollisionThreshold = Mathf.Clamp01(value); }

        /// <summary>
        /// This allows this body to bypass rotational speed limits.
        /// This should only be used for circular objects, such as wheels, balls etc.
        /// </summary>
        public bool fastRotationAllowed { readonly get => m_FastRotationAllowed; set => m_FastRotationAllowed = value; }

        /// <summary>
        /// Treat this body as high speed object that performs continuous collision detection against dynamic and kinematic bodies, but not other high speed bodies.
        /// Fast collision bodies should be used sparingly. They are not a solution for general dynamic-versus-dynamic continuous collision.
        /// </summary>
        public bool fastCollisionsAllowed { readonly get => m_FastCollisionsAllowed; set => m_FastCollisionsAllowed = value; }

        /// <summary>
        /// Set this flag to false if this body should never fall asleep.
        /// </summary>
        public bool sleepingAllowed { readonly get => m_SleepingAllowed; set => m_SleepingAllowed = value; }

        /// <summary>
        /// Is this body initially awake or sleeping?
        /// </summary>
        public bool awake { readonly get => m_Awake; set => m_Awake = value; }

        /// <summary>
        /// Used to disable a body. A disabled body does not move or collide.
        /// </summary>
        public bool enabled { readonly get => m_Enabled; set => m_Enabled = value; }

        /// <summary>
        /// Controls whether this body is automatically drawn when the world is drawn.
        ///
        /// See <see cref="PhysicsBody.worldDrawing"/>.
        /// </summary>
        public bool worldDrawing { readonly get => m_WorldDrawing; set => m_WorldDrawing = value; }

        #region Internal

        [SerializeField] PhysicsBody.BodyType m_BodyType;
        [SerializeField] PhysicsBody.BodyConstraints m_BodyConstraints;
        [SerializeField] PhysicsBody.TransformWriteMode m_TransformWriteMode;
        [SerializeField] Vector2 m_Position;
        [SerializeField] PhysicsRotate m_Rotation;
        [SerializeField] Vector2 m_LinearVelocity;
        [SerializeField] float m_AngularVelocity;
        [SerializeField] [Min(0.0f)]float m_LinearDamping;
        [SerializeField] [Min(0.0f)] float m_AngularDamping;
        [SerializeField] float m_GravityScale;
        [SerializeField] [Min(0.0f)] float m_SleepThreshold;
        [SerializeField] [Range(0.0f, 1.0f)] float m_CollisionThreshold;
        [SerializeField] bool m_FastCollisionsAllowed;
        [SerializeField] bool m_FastRotationAllowed;
        [SerializeField] bool m_SleepingAllowed;
        [SerializeField] bool m_Awake;
        [SerializeField] bool m_Enabled;
        [SerializeField] bool m_WorldDrawing;

        #endregion
    }
}
