// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using Unity.Collections;

using static UnityEngine.LowLevelPhysics2D.PhysicsLowLevelScripting2D;

namespace UnityEngine.LowLevelPhysics2D
{
    /// <summary>
    /// A body is contained within a world and has 3 degrees-of-freedom, two for position and one for rotation.
    /// A body can have forces, torques and impulses applied to it.
    /// A body has three distinct types:
    /// 
    ///- Static: This type of body does not move under simulation and behaves as if it has infinite mass, essentially an immovable object. Static bodies never interact with other Static or Kinematic bodies.
    ///- Dynamic: This type of body is fully simulated and moves according to forces and torques applied to its linear/angular velocities. It can interact with all other body types. It always has finite, non-zero mass.
    ///- Kinematic: This type of body moves under simulation and moves according to its linear/angular velocities and never uses forces or torques. It only interacts with Dynamic body types. It behaves as if it has infinite mass.
    ///
    /// A body is automatically destroyed when the world it is in is destroyed. A body cannot exist outside a world.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly partial struct PhysicsBody : IEquatable<PhysicsBody>
    {
        #region Id

        readonly Int32 m_Index1;
        readonly UInt16 m_World0;
        readonly UInt16 m_Generation;

        /// <undoc/>
        public override readonly string ToString() => isValid ? $"type={type}, index={m_Index1}, world={m_World0}, generation={m_Generation}" : "<INVALID>";

        #endregion

        #region Equality

        /// <undoc/>
        public override bool Equals(object obj) { return base.Equals(obj); }

        /// <undoc/>
        public bool Equals(PhysicsBody other) { return m_Index1 == other.m_Index1 && m_World0 == other.m_World0 && m_Generation == other.m_Generation; }

        /// <undoc/>
        public static bool operator ==(PhysicsBody lhs, PhysicsBody rhs) => lhs.Equals(rhs);

        /// <undoc/>
        public static bool operator !=(PhysicsBody lhs, PhysicsBody rhs) => !(lhs == rhs);

        /// <undoc/>
        public override int GetHashCode() { return HashCode.Combine(m_Index1, m_World0, m_Generation); }

        #endregion

        /// <summary>
        /// A body is one of these three body types, Dynamic, Kinematic or Static, each of which determines how the body behaves in the simulation.
        /// </summary>
        public enum BodyType
        {
            /// <summary>
            /// A dynamic body has positive mass, velocity determined by forces and is moved by solver.
            /// </summary>
            Dynamic = 0,

            /// <summary>
            /// A kinematic body has zero mass, velocity set by user and is moved by solver
            /// </summary>
            Kinematic = 1,

            /// <summary>
            /// A static body has zero mass, zero velocity and may be manually moved.
            /// </summary>
            Static = 2,
        }

        /// <summary>
        /// Body constrains constrain the degrees of freedom a body when solving the simulation.
        /// </summary>
        [Flags]
        public enum BodyConstraints
        {
            /// <summary>
            /// No constraints
            /// </summary>
            None = 0,

            /// <summary>
            /// Constrain motion along the X-axis.
            /// </summary>
            PositionX = 1 << 0,

            /// <summary>
            /// Constrain motion along the Y-axis.
            /// </summary>
            PositionY = 1 << 1,

            /// <summary>
            /// FreConstraineze rotation along the Z-axis.
            /// </summary>
            Rotation = 1 << 2,

            /// <summary>
            /// Constrain motion along all axes.
            /// </summary>
            Position = PositionX | PositionY,

            /// <summary>
            /// Constrain rotation and motion along all axes.
            /// </summary>
            All = Position | Rotation,
        }

        /// <summary>
        /// The method used to Write the body pose to the Transform.
        /// See <see cref="LowLevelPhysics2D.PhysicsWorld.transformWriteMode"/>.
        /// </summary>
        public enum TransformWriteMode
        {
            /// <summary>
            /// The current body pose will be written to the Transform.
            /// </summary>
            Current,

            /// <summary>
            /// The interpolated pose from the previous body pose to the current body pose will be written to the Transform.
            /// The transform pose is essentially historic.
            /// </summary>
            Interpolate,

            /// <summary>
            /// The pose extrapolated from the current body pose to a future pose based upon the current linear/angular velocities will be written to the Transform.
            /// The transform pose is essentially predictive.
            /// </summary>
            Extrapolate,

            /// <summary>
            /// This body pose won't be written to the Transform.
            /// </summary>
            Off
        }

        /// <summary>
        /// Used to define a Transform write "tween" for a body.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct TransformWriteTween
        {
            /// <summary>
            /// The body to be used during the lifetime of the tween.
            /// </summary>
            public PhysicsBody body { readonly get => m_Body; set => m_Body = value; }

            /// <summary>
            /// The transform write mode to be used during the lifetime of the tween.
            /// Anything other than <see cref="LowLevelPhysics2D.PhysicsBody.TransformWriteMode.Interpolate"/> or <see cref="LowLevelPhysics2D.PhysicsBody.TransformWriteMode.Extrapolate"/> will be removed.
            /// </summary>
            public TransformWriteMode transformWriteMode { readonly get => m_TransformWriteMode; set => m_TransformWriteMode = value; }

            /// <summary>
            /// The physics transform to be used during the lifetime of the tween.
            /// </summary>
            public PhysicsTransform physicsTransform { readonly get => m_PhysicsTransform; set => m_PhysicsTransform = value; }

            /// <summary>
            /// The linear velocity of the body to be used during the lifetime of the tween.
            /// </summary>
            public Vector2 linearVelocity { readonly get => m_LinearVelocity; set => m_LinearVelocity = value; }

            /// <summary>
            /// The angular velocity of the body to be used during the lifetime of the tween, in degrees per second.
            /// </summary>
            public float angularVelocity { readonly get => m_AngularVelocity; set => m_AngularVelocity = value; }

            /// <summary>
            /// The start position of the Transform tween.
            /// See <see cref="UnityEngine.Transform.position"/>.
            /// </summary>
            public Vector3 positionFrom { readonly get => m_PositionFrom; set => m_PositionFrom = value; }

            /// <summary>
            /// The start rotation of the Transform.
            /// See <see cref="UnityEngine.Transform.rotation"/>.
            /// </summary>
            public Quaternion rotationFrom { readonly get => m_RotationFrom; set => m_RotationFrom = value; }

            #region Internal

            PhysicsBody m_Body;
            TransformWriteMode m_TransformWriteMode;
            PhysicsTransform m_PhysicsTransform;
            Vector2 m_LinearVelocity;
            float m_AngularVelocity;
            Vector3 m_PositionFrom;
            Quaternion m_RotationFrom;

            #endregion
        }

        /// <summary>
        /// This holds the mass configuration computed for a PhysicsBody.
        /// </summary>
        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        public struct MassConfiguration
        {
            /// <summary>
            /// The mass of the shape, usually in kilograms.
            /// </summary>
            public float mass { readonly get => m_Mass; set => m_Mass = value; }

            /// <summary>
            /// The position of the shape's centroid relative to the shape's origin.
            /// </summary>
            public Vector2 center { readonly get => m_Center; set => m_Center = value; }

            /// <summary>
            /// The rotational inertia of the shape about the shape center.
            /// </summary>
            public float rotationalInertia { readonly get => m_RotationalInertia; set => m_RotationalInertia = value; }

            #region Internal

            [SerializeField] float m_Mass;
            [SerializeField] Vector2 m_Center;
            [SerializeField] float m_RotationalInertia;

            #endregion
        }

        /// <summary>
        /// A batch item used to set the velocity of a <see cref="LowLevelPhysics2D.PhysicsBody"/>.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct BatchVelocity
        {
            /// <summary>
            /// Create a default batch velocity, assigning the <see cref="LowLevelPhysics2D.PhysicsBody"/>.
            /// </summary>
            /// <param name="physicsBody">The <see cref="LowLevelPhysics2D.PhysicsBody"/> to write to.</param>
            public BatchVelocity(PhysicsBody physicsBody)
            {
                m_PhysicsBody = physicsBody;
                m_LinearVelocity = default;
                m_AngularVelocity = default;
                m_UseLinearVelocity = default;
                m_UseAngularVelocity = default;
            }

            /// <summary>
            /// The <see cref="LowLevelPhysics2D.PhysicsBody"/> to write to.
            /// </summary>
            public PhysicsBody physicsBody { readonly get => m_PhysicsBody; set => m_PhysicsBody = value; }

            /// <summary>
            /// The linear velocity of the body.
            /// <see cref="LowLevelPhysics2D.PhysicsBody.linearVelocity"/>.
            /// </summary>
            public Vector2 linearVelocity { readonly get => m_LinearVelocity; set { m_LinearVelocity = value; m_UseLinearVelocity = true; } }

            /// <summary>
            /// The angular velocity of the body, in degrees per second.
            /// <see cref="LowLevelPhysics2D.PhysicsBody.angularVelocity"/>.
            /// </summary>
            public float angularVelocity { readonly get => m_AngularVelocity; set { m_AngularVelocity = value; m_UseAngularVelocity = true; } }

            #region Internal

            PhysicsBody m_PhysicsBody;
            Vector2 m_LinearVelocity;
            float m_AngularVelocity;

            bool m_UseLinearVelocity;
            bool m_UseAngularVelocity;

            #endregion
        };

        /// <summary>
        /// A batch item used to apply a force to a <see cref="LowLevelPhysics2D.PhysicsBody"/>.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct BatchForce
        {
            /// <summary>
            /// Create a default batch force, assigning the <see cref="LowLevelPhysics2D.PhysicsBody"/>.
            /// </summary>
            /// <param name="physicsBody">The <see cref="LowLevelPhysics2D.PhysicsBody"/> to write to.</param>
            public BatchForce(PhysicsBody physicsBody)
            {
                m_PhysicsBody = physicsBody;

                m_LinearForce = default;
                m_LinearForcePosition = default;
                m_Torque = default;
                m_WakeBody = default;
                m_UseLinearForce = default;
                m_UseLinearForcePosition = default;
                m_UseTorque = default;
            }

            /// <summary>
            /// The <see cref="LowLevelPhysics2D.PhysicsBody"/> to write to.
            /// </summary>
            public PhysicsBody physicsBody { readonly get => m_PhysicsBody; set => m_PhysicsBody = value; }

            /// <summary>
            /// Apply a force at a world point.
            /// If the force is not applied at the center of mass, it will generate a torque and affect the angular velocity.
            /// <see cref="LowLevelPhysics2D.PhysicsBody.ApplyForce(Vector2, Vector2, bool)"/>.
            /// </summary>
            /// <param name="force">The world force vector, usually in newtons (N)</param>
            /// <param name="point">The world position of the point of application.</param>
            /// <param name="wake">Should the body be woken up.</param>
            public void ApplyForce(Vector2 force, Vector2 point, bool wake = true)
            {
                m_LinearForce = force;
                m_LinearForcePosition = point;
                m_WakeBody = wake;
                m_UseLinearForce = true;
                m_UseLinearForcePosition = true;
            }

            /// <summary>
            /// Apply a force to the center of mass.
            /// <see cref="LowLevelPhysics2D.PhysicsBody.ApplyForceToCenter(Vector2, bool)"/>.
            /// </summary>
            /// <param name="force">The world force vector, usually in newtons (N).</param>
            /// <param name="wake">Should the body be woken up.</param>
            public void ApplyForceToCenter(Vector2 force, bool wake = true)
            {
                m_LinearForce = force;
                m_WakeBody = wake;
                m_UseLinearForce = true;
                m_UseLinearForcePosition = false;
            }

            /// <summary>
            /// Apply a torque.
            /// This affects the angular velocity without affecting the linear velocity.
            /// <see cref="LowLevelPhysics2D.PhysicsBody.ApplyTorque(float, bool)"/>.
            /// </summary>
            /// <param name="torque">Torque, usually in N*m.</param>
            /// <param name="wake">Should the body be woken up.</param>
            public void ApplyTorque(float torque, bool wake = true)
            {
                m_Torque = torque;
                m_WakeBody = wake;

                m_UseTorque = true;
            }

            #region Internal

            PhysicsBody m_PhysicsBody;
            Vector2 m_LinearForce;
            Vector2 m_LinearForcePosition;
            float m_Torque;
            bool m_WakeBody;

            bool m_UseLinearForce;
            bool m_UseLinearForcePosition;
            bool m_UseTorque;

            #endregion
        };

        /// <summary>
        /// A batch item used to apply an impulse to a <see cref="LowLevelPhysics2D.PhysicsBody"/>.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct BatchImpulse
        {
            /// <summary>
            /// Create a default batch impulse, assigning the <see cref="LowLevelPhysics2D.PhysicsBody"/>.
            /// </summary>
            /// <param name="physicsBody">The <see cref="LowLevelPhysics2D.PhysicsBody"/> to write to.</param>
            public BatchImpulse(PhysicsBody physicsBody)
            {
                m_PhysicsBody = physicsBody;

                m_LinearImpulse = default;
                m_LinearImpulsePosition = default;
                m_AngularImpulse = default;
                m_WakeBody = default;
                m_UseLinearImpulse = default;
                m_UseLinearImpulsePosition = default;
                m_UseAngularImpulse = default;
            }

            /// <summary>
            /// The <see cref="LowLevelPhysics2D.PhysicsBody"/> to write to.
            /// </summary>
            public PhysicsBody physicsBody { readonly get => m_PhysicsBody; set => m_PhysicsBody = value; }

            /// <summary>
            /// Apply an impulse at a point.
            /// This immediately modifies the velocity and also modifies the angular velocity if the point of application is not at the center of mass.
            ///	This should be used for one-shot impulses.
            ///	If you need a steady force, use a force instead, which will work better with the sub-stepping solver.
            /// <see cref="LowLevelPhysics2D.PhysicsBody.ApplyLinearImpulse(Vector2, Vector2, bool)"/>.
            /// </summary>
            /// <param name="impulse">The world impulse vector, usually in N*s or kg*m/s.</param>
            /// <param name="point">The world position of the point of application.</param>
            /// <param name="wake">Should the body be woken up.</param>
            public void ApplyLinearImpulse(Vector2 impulse, Vector2 point, bool wake = true)
            {
                m_LinearImpulse = impulse;
                m_LinearImpulsePosition = point;
                m_WakeBody = wake;

                m_UseLinearImpulse = true;
                m_UseLinearImpulsePosition = true;
            }

            /// <summary>
            /// Apply an impulse to the center of mass.
            /// This immediately modifies the velocity.
            ///	This should be used for one-shot impulses.
            ///	If you need a steady force, use a force instead, which will work better with the sub-stepping solver.
            /// <see cref="LowLevelPhysics2D.PhysicsBody.ApplyLinearImpulseToCenter(Vector2, bool)"/>.
            /// </summary>
            /// <param name="impulse">The world impulse vector, usually in N*s or kg*m/s.</param>
            /// <param name="wake">Should the body be woken up.</param>
            public void ApplyLinearImpulseToCenter(Vector2 impulse, bool wake = true)
            {
                m_LinearImpulse = impulse;
                m_WakeBody = wake;

                m_UseLinearImpulse = true;
                m_UseLinearImpulsePosition = false;
            }

            /// <summary>
            /// Apply an angular impulse.
            ///	This should be used for one-shot impulses.
            ///	If you need a steady torque, use a torque instead, which will work better with the sub-stepping solver.
            /// <see cref="LowLevelPhysics2D.PhysicsBody.ApplyAngularImpulse(float, bool)"/>.
            /// </summary>
            /// <param name="impulse">The angular impulse, usually in units of kg*m*m/s.</param>
            /// <param name="wake">Should the body be woken up.</param>
            public void ApplyAngularImpulse(float impulse, bool wake = true)
            {
                m_AngularImpulse = impulse;
                m_WakeBody = true;

                m_UseAngularImpulse = true;
            }

            #region Internal

            PhysicsBody m_PhysicsBody;
            Vector2 m_LinearImpulse;
            Vector2 m_LinearImpulsePosition;
            float m_AngularImpulse;
            bool m_WakeBody;

            bool m_UseLinearImpulse;
            bool m_UseLinearImpulsePosition;
            bool m_UseAngularImpulse;

            #endregion
        };

        /// <summary>
        /// A batch item used to set the pose of a <see cref="LowLevelPhysics2D.PhysicsBody"/>.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct BatchTransform
        {
            /// <summary>
            /// Create a default batch transform, assigning the <see cref="LowLevelPhysics2D.PhysicsBody"/>.
            /// </summary>
            /// <param name="physicsBody">The <see cref="LowLevelPhysics2D.PhysicsBody"/> to write to.</param>
            public BatchTransform(PhysicsBody physicsBody)
            {
                m_PhysicsBody = physicsBody;

                m_PhysicsTransform = default;
                m_UsePosition = default;
                m_UseRotation = default;
            }

            /// <summary>
            /// The <see cref="LowLevelPhysics2D.PhysicsBody"/> to write to.
            /// </summary>
            public PhysicsBody physicsBody { readonly get => m_PhysicsBody; set => m_PhysicsBody = value; }

            /// <summary>
            /// The position of the body in the world.
            /// <see cref="LowLevelPhysics2D.PhysicsBody.position"/>.
            /// </summary>
            public Vector2 position { readonly get => m_PhysicsTransform.position; set { m_PhysicsTransform.position = value; m_UsePosition = true; } }

            /// <summary>
            /// The rotation of the body.
            /// <see cref="LowLevelPhysics2D.PhysicsBody.rotation"/>.
            /// </summary>
            public PhysicsRotate rotation { readonly get => m_PhysicsTransform.rotation; set { m_PhysicsTransform.rotation = value; m_UseRotation = true; } }

            /// <summary>
            /// The full transform of the body composed of position and rotation.
            /// <see cref="LowLevelPhysics2D.PhysicsBody.transform"/>.
            /// </summary>
            public PhysicsTransform transform { readonly get => m_PhysicsTransform; set { m_PhysicsTransform = value; m_UsePosition = m_UseRotation = true; } }

            #region Internal

            PhysicsBody m_PhysicsBody;
            PhysicsTransform m_PhysicsTransform;

            bool m_UsePosition;
            bool m_UseRotation;

            #endregion
        };

        /// <summary>
        /// Create a body using <see cref="LowLevelPhysics2D.PhysicsBodyDefinition.defaultDefinition"/> in the specified world.
        /// </summary>
        /// <param name="world">The world to create the body in.</param>
        /// <returns>The created body.</returns>
        public static PhysicsBody Create(PhysicsWorld world) => PhysicsBody_Create(world, PhysicsBodyDefinition.defaultDefinition);

        /// <summary>
        /// Create a body in the specified world.
        /// </summary>
        /// <param name="world">The world to create the body in.</param>
        /// <param name="definition">The body definition to use.</param>
        /// <returns>The created body.</returns>
        public static PhysicsBody Create(PhysicsWorld world, PhysicsBodyDefinition definition) => PhysicsBody_Create(world, definition);

        /// <summary>
        /// Create a batch of bodies in the specified world.
        /// </summary>
        /// <param name="world">The world to create the bodies in.</param>
        /// <param name="definition">The body definition to use for all bodies.</param>
        /// <param name="bodyCount">The number of bodies to create.</param>
        /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
        /// <returns>The created bodies. This NativeArray must be disposed of after use otherwise leaks will occur. The exception to this is if the array is empty.</returns>
        public static unsafe NativeArray<PhysicsBody> CreateBatch(PhysicsWorld world, PhysicsBodyDefinition definition, int bodyCount, Allocator allocator = Unity.Collections.Allocator.Temp) => PhysicsBody_CreateBatch(world, new ReadOnlySpan<PhysicsBodyDefinition>(&definition, 1), bodyCount, allocator).ToNativeArray<PhysicsBody>();

        /// <summary>
        /// Create a batch of bodies in the specified world.
        /// </summary>
        /// <param name="world">The world to create the bodies in.</param>
        /// <param name="definitions">The definitions used to create the bodies. The number of bodies produced is implicitly controlled by the number of definitions in this span.</param>
        /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
        /// <returns>The created bodies. This NativeArray must be disposed of after use otherwise leaks will occur. The exception to this is if the array is empty.</returns>
        public static NativeArray<PhysicsBody> CreateBatch(PhysicsWorld world, ReadOnlySpan<PhysicsBodyDefinition> definitions, Allocator allocator = Unity.Collections.Allocator.Temp) => PhysicsBody_CreateBatch(world, definitions, definitions.Length, allocator).ToNativeArray<PhysicsBody>();

        /// <summary>
        /// Destroy a body, destroying all attached <see cref="LowLevelPhysics2D.PhysicsShape"/> and <see cref="LowLevelPhysics2D.PhysicsJoint"/>.
        /// If the object is owned with <see cref="LowLevelPhysics2D.PhysicsBody.SetOwner(Object)"/> then you must provide the owner key it returned. Failing to do so will return a warning and the body will not be destroyed.
        /// </summary>
        /// <param name="ownerKey">Optional owner key returned when using <see cref="LowLevelPhysics2D.PhysicsBody.SetOwner(Object)"/>.</param>
        /// <returns>If the body was destroyed or not.</returns>
        public readonly bool Destroy(int ownerKey = 0) => PhysicsBody_Destroy(this, ownerKey);

        /// <summary>
        /// Destroy a batch of bodies, destroying all attached <see cref="LowLevelPhysics2D.PhysicsShape"/> and <see cref="LowLevelPhysics2D.PhysicsJoint"/>.
        /// Any invalid bodies will be ignored.
        /// Owned bodies will produce a warning and will not be destroyed (See <see cref="LowLevelPhysics2D.PhysicsBody.SetOwner(Object)"/>).
        /// </summary>
        /// <param name="bodies">The bodies to destroy.</param>
        public static void DestroyBatch(ReadOnlySpan<PhysicsBody> bodies) => PhysicsBody_DestroyBatch(bodies);

        /// <summary>
        /// Set the velocity for a batch of <see cref="LowLevelPhysics2D.PhysicsBody"/> using a span of <see cref="LowLevelPhysics2D.PhysicsBody.BatchVelocity"/>.
        /// If invalid values are passed to the batch, they will simply be ignored.
        /// For best performance, the bodies contained in the batch should all be part of the same <see cref="LowLevelPhysics2D.PhysicsWorld"/>.
        /// If the bodies in the batch are not contained in the same <see cref="LowLevelPhysics2D.PhysicsWorld"/>, the batch should be sorted by the <see cref="LowLevelPhysics2D.PhysicsWorld"/> the bodies are contained within.
        /// </summary>
        /// <param name="batch">The batch of bodies and values to set.</param>
        public static void SetBatchVelocity(ReadOnlySpan<BatchVelocity> batch) => PhysicsBody_SetBatchVelocity(batch);

        /// <summary>
        /// Apply a force for a batch of <see cref="LowLevelPhysics2D.PhysicsBody"/> using a span of <see cref="LowLevelPhysics2D.PhysicsBody.BatchForce"/>.
        /// If invalid values are passed to the batch, they will simply be ignored.
        /// For best performance, the bodies contained in the batch should all be part of the same <see cref="LowLevelPhysics2D.PhysicsWorld"/>.
        /// If the bodies in the batch are not contained in the same <see cref="LowLevelPhysics2D.PhysicsWorld"/>, the batch should be sorted by the <see cref="LowLevelPhysics2D.PhysicsWorld"/> the bodies are contained within.
        /// </summary>
        /// <param name="batch">The batch of bodies and values to set.</param>
        public static void SetBatchForce(ReadOnlySpan<BatchForce> batch) => PhysicsBody_SetBatchForce(batch);

        /// <summary>
        /// Apply an impulse for a batch of <see cref="LowLevelPhysics2D.PhysicsBody"/> using a span of <see cref="LowLevelPhysics2D.PhysicsBody.BatchImpulse"/>.
        /// If invalid values are passed to the batch, they will simply be ignored.
        /// For best performance, the bodies contained in the batch should all be part of the same <see cref="LowLevelPhysics2D.PhysicsWorld"/>.
        /// If the bodies in the batch are not contained in the same <see cref="LowLevelPhysics2D.PhysicsWorld"/>, the batch should be sorted by the <see cref="LowLevelPhysics2D.PhysicsWorld"/> the bodies are contained within.
        /// </summary>
        /// <param name="batch">The batch of bodies and values to set.</param>
        public static void SetBatchImpulse(ReadOnlySpan<BatchImpulse> batch) => PhysicsBody_SetBatchImpulse(batch);

        /// <summary>
        /// Set the transform for a batch of <see cref="LowLevelPhysics2D.PhysicsBody"/> using a span of <see cref="LowLevelPhysics2D.PhysicsBody.BatchTransform"/>.
        /// If invalid values are passed to the batch, they will simply be ignored.
        /// For best performance, the bodies contained in the batch should all be part of the same <see cref="LowLevelPhysics2D.PhysicsWorld"/>.
        /// If the bodies in the batch are not contained in the same <see cref="LowLevelPhysics2D.PhysicsWorld"/>, the batch should be sorted by the <see cref="LowLevelPhysics2D.PhysicsWorld"/> the bodies are contained within.
        /// </summary>
        /// <param name="batch">The batch of bodies and values to set.</param>
        public static void SetBatchTransform(ReadOnlySpan<BatchTransform> batch) => PhysicsBody_SetBatchTransform(batch);

        /// <summary>
        /// Get/Set a body definition by accessing all of its current properties.
        /// This is provided as convenience only and should not be used when performance is important as all the properties defined in the definition are accessed sequentially.
        /// You should try to only use the specific properties you need rather than using this feature.
        /// </summary>
        public readonly PhysicsBodyDefinition definition { get => PhysicsBody_ReadDefinition(this); set => PhysicsBody_WriteDefinition(this, value, false); }

        /// <summary>
        /// Checks if a body is valid.
        /// </summary>
        public readonly bool isValid => PhysicsBody_IsValid(this);

        /// <summary>
        /// Get the world the body is attached to.
        /// </summary>
        public readonly PhysicsWorld world => PhysicsBody_GetWorld(this);

        /// <summary>
        /// A body is one of these three body types, Dynamic, Kinematic or Static, each of which determines how the body behaves in the simulation.
        /// </summary>
        public readonly PhysicsBody.BodyType type { get => PhysicsBody_GetBodyType(this); set => PhysicsBody_SetBodyType(this, value); }

        /// <summary>
        /// Get/Set the degrees of freedom constraints (locks) for the body of Linear X, Linear Y and Rotation Z.
        /// </summary>
        public readonly PhysicsBody.BodyConstraints constraints { get => PhysicsBody_GetBodyConstraints(this); set => PhysicsBody_SetBodyConstraints(this, value); }

        /// <summary>
        /// The position of the body in the world.
        /// </summary>
        public readonly Vector2 position { get => PhysicsBody_GetPosition(this); set => PhysicsBody_SetPosition(this, value); }

        /// <summary>
        /// The rotation of the body.
        /// </summary>
        public readonly PhysicsRotate rotation { get => PhysicsBody_GetRotation(this); set => PhysicsBody_SetRotation(this, value); }

        /// <summary>
        /// The full transform of the body composed of position and rotation.
        /// </summary>
        public readonly PhysicsTransform transform { get => PhysicsBody_GetTransform(this); set => PhysicsBody_SetTransform(this, value); }

        /// <summary>
        /// Set the <see cref="LowLevelPhysics2D.PhysicsBody.linearVelocity"/> and <see cref="LowLevelPhysics2D.PhysicsBody.angularVelocity"/> to reach the specified transform in the specified time.
        /// The resultant transform will be closed by may not be exact.
        /// This is designed ideally for Kinematic bodies but will work with Dynamic bodies if nothing changes the assigned velocities.
        /// This will be ignored if the calculated <see cref="LowLevelPhysics2D.PhysicsBody.linearVelocity"/> and <see cref="LowLevelPhysics2D.PhysicsBody.angularVelocity"/> would be below the <see cref="LowLevelPhysics2D.PhysicsBody.sleepThreshold"/>.
        /// This will automatically wake the body if it is asleep.
        /// </summary>
        /// <param name="transform">The transform target for the body.</param>
        /// <param name="deltaTime">The timer over which to calculate the required velocities to move to the transform.</param>
        public readonly void SetTransformTarget(PhysicsTransform transform, float deltaTime) => PhysicsBody_SetTransformTarget(this, transform, deltaTime);

        /// <summary>
        /// Get the full 3D position and rotation of the body given the specified <see cref="LowLevelPhysics2D.PhysicsWorld.TransformWriteMode"/> and <see cref="LowLevelPhysics2D.PhysicsWorld.TransformPlane"/>.
        /// Usually both the write-mode and transform-plane of the world the body is in would be used.
        /// This can only be called when a <see cref="LowLevelPhysics2D.PhysicsBody.transformObject"/> is assigned. Without this, an exception is thrown.
        /// See <see cref="LowLevelPhysics2D.PhysicsWorld.transformWriteMode"/> and <see cref="LowLevelPhysics2D.PhysicsWorld.transformPlane"/>.
        /// </summary>
        /// <param name="transform">The Transform object to be used as a reference when converting from 2D position/rotation to 3D position/rotation, usually the same as the TransformObject assigned to the PhysicsBody.</param>
        /// <param name="transformWriteMode">The transform write mode to use, usually the one currently assigned to the world is used.</param>
        /// <param name="transformPlane">The transform plane to use, usually the one currently assigned to the world is used.</param>
        /// <param name="position">The calculated output position.</param>
        /// <param name="rotation">The calculated output rotation.</param>
        /// <exception cref="System.ArgumentNullException">Thrown if the Transform argument is NULL.</exception>
        /// <exception cref="System.InvalidOperationException">Thrown if the transform write mode is Off or there is no TransformObject assigned to the PhysicsBody.</exception>
        public readonly void GetPositionAndRotation3D(Transform transform, PhysicsWorld.TransformWriteMode transformWriteMode, PhysicsWorld.TransformPlane transformPlane, out Vector3 position, out Quaternion rotation)
        {
            // Validate.
            if (transform == null)
                throw new ArgumentNullException(nameof(transform), "Transform cannot be NULL.");

            // Fetch the body transform.
            var bodyTransform = this.transform;

            // Fetch the world configuration.
            var bodyWorld = world;

            // Handle the write mode appropriately.
            switch (transformWriteMode)
            {
                // Write the fast case.
                case PhysicsWorld.TransformWriteMode.Fast2D:
                {
                    position = PhysicsMath.ToPosition3D(position: bodyTransform.position, reference: transform.position, transformPlane: transformPlane);
                    rotation = PhysicsMath.ToRotationFast3D(angle: bodyTransform.rotation.angle, transformPlane: transformPlane);
                    return;
                }

                // Write the slow case.
                case PhysicsWorld.TransformWriteMode.Slow3D:
                {
                    transform.GetPositionAndRotation(out var transformPosition, out var transformRotation);
                    position = PhysicsMath.ToPosition3D(position: bodyTransform.position, reference: transformPosition, transformPlane: transformPlane);
                    rotation = PhysicsMath.ToRotationSlow3D(angle: bodyTransform.rotation.angle, reference: transformRotation, transformPlane: transformPlane);
                    return;
                }

                case PhysicsWorld.TransformWriteMode.Off:
                default:
                    throw new InvalidOperationException("Invalid Transform Write Mode.");
            }
        }

        /// <summary>
        /// Set the full transform of the body composed of position and rotation and also write to the associated <see cref="LowLevelPhysics2D.PhysicsBody.transformObject"/>.
        /// The <see cref="LowLevelPhysics2D.PhysicsBody.transformObject"/> won't be written to if it isn't assigned or the <see cref="LowLevelPhysics2D.PhysicsWorld.transformWriteMode"/> are off. The body will always be updated however.
        /// See <see cref="LowLevelPhysics2D.PhysicsBody.transformObject"/>.
        /// </summary>
        /// <param name="transform">The full transform of the body composed of position and rotation.</param>
        /// <returns>Whether the <see cref="LowLevelPhysics2D.PhysicsBody.transformObject"/> was written to.</returns>
        public readonly bool SetAndWriteTransform(PhysicsTransform transform)
        {
            // Set the transform.
            this.transform = transform;

            // Fetch the transform object.
            var bodyTransformObject = transformObject;
            if (bodyTransformObject == null)
                return false;

            // Fetch the world configuration.
            var bodyWorld = world;
            var transformWriteMode = bodyWorld.transformWriteMode;
            var transformPlane = bodyWorld.transformPlane;

            // Handle the write mode appropriately.
            switch (transformWriteMode)
            {
                // Don't write.
                case PhysicsWorld.TransformWriteMode.Off:
                    return false;

                // Write both the fast and slow cases.
                case PhysicsWorld.TransformWriteMode.Fast2D:
                case PhysicsWorld.TransformWriteMode.Slow3D:
                {
                    // Set the transform pose.
                    GetPositionAndRotation3D(transformObject, transformWriteMode, transformPlane, out var newPosition, out var newRotation);
                    bodyTransformObject.SetPositionAndRotation(newPosition, newRotation);
                    return true;
                }

                default:
                    throw new InvalidOperationException("Invalid Transform Write Mode.");
            }
        }

        /// <summary>
        /// Gets a local point relative to the body given a world point.
        /// </summary>
        /// <param name="worldPoint">The world point to transform.</param>
        /// <returns>The local point relative to the body.</returns>
        public readonly Vector2 GetLocalPoint(Vector2 worldPoint) => PhysicsBody_GetLocalPoint(this, worldPoint);

        /// <summary>
        /// Gets a world point transformed from a local point relative to the body.
        /// </summary>
        /// <param name="localPoint">The local point to transform.</param>
        /// <returns>The transformed world point.</returns>
        public readonly Vector2 GetWorldPoint(Vector2 localPoint) => PhysicsBody_GetWorldPoint(this, localPoint);

        /// <summary>
        /// Gets a local vector on a body given a world vector.
        /// </summary>
        /// <param name="worldVector">The world vector to transform.</param>
        /// <returns>The local vector relative to the body.</returns>
        public readonly Vector2 GetLocalVector(Vector2 worldVector) => PhysicsBody_GetLocalVector(this, worldVector);

        /// <summary>
        /// Gets a world vector transformed from a local vector relative to the body.
        /// </summary>
        /// <param name="localVector">The local vector to transform.</param>
        /// <returns>The transformed world vector.</returns>
        public readonly Vector2 GetWorldVector(Vector2 localVector) => PhysicsBody_GetWorldVector(this, localVector);

        /// <summary>
        /// Get the linear velocity of a local point attached to a body. Usually in meters per second.
        /// </summary>
        /// <param name="localPoint">The local point to transform.</param>
        /// <returns>The linear velocity at the specified local point attached to a body.</returns>
        public readonly Vector2 GetLocalPointVelocity(Vector2 localPoint) => PhysicsBody_GetLocalPointVelocity(this, localPoint);

        /// <summary>
        /// Get the linear velocity of a world point attached to a body. Usually in meters per second.
        /// </summary>
        /// <param name="worldPoint">The world point to transform.</param>
        /// <returns>The linear velocity at the specified world point attached to a body.</returns>
        public readonly Vector2 GetWorldPointVelocity(Vector2 worldPoint) => PhysicsBody_GetWorldPointVelocity(this, worldPoint);

        /// <summary>
        /// The linear velocity of the body.
        /// </summary>
        public readonly Vector2 linearVelocity { get => PhysicsBody_GetLinearVelocity(this); set => PhysicsBody_SetLinearVelocity(this, value); }

        /// <summary>
        /// The angular velocity of the body.
        /// </summary>
        public readonly float angularVelocity { get => PhysicsBody_GetAngularVelocity(this); set => PhysicsBody_SetAngularVelocity(this, value); }

        /// <summary>
        /// The calculated mass of the body, usually in kilograms.
        /// See <see cref="LowLevelPhysics2D.PhysicsBody.massConfiguration"/>.
        /// </summary>
        public readonly float mass => PhysicsBody_GetMass(this);

        /// <summary>
        /// The rotational inertia of the body, usually in kg*m^2.
        /// See <see cref="LowLevelPhysics2D.PhysicsBody.massConfiguration"/>.
        /// </summary>
        public readonly float rotationalInertia => PhysicsBody_GetRotationalInertia(this);

        /// <summary>
        /// The center of mass position of the body in local space.
        /// </summary>
        public readonly Vector2 localCenterOfMass => PhysicsBody_GetLocalCenterOfMass(this);

        /// <summary>
        /// The center of mass position of the body in world space.
        /// </summary>
        public readonly Vector2 worldCenterOfMass => PhysicsBody_GetWorldCenterOfMass(this);

        /// <summary>
        /// The body mass configuration. Normally this is computed automatically using the shape geometry and density.
        /// This information changes if a shape is added or removed or if the body type changes.
        /// See <see cref="LowLevelPhysics2D.PhysicsBody.MassConfiguration"/>.
        /// </summary>
        public readonly MassConfiguration massConfiguration { get => PhysicsBody_GetMassConfiguration(this); set => PhysicsBody_SetMassConfiguration(this, value); }

        /// <summary>
        /// This updates the mass configuration to the sum of the mass configuration of all the attached shapes.
        /// This normally does not need to be called unless you set the mass configuration to override the mass and you later want to reset the mass.
        ///	You should call this regardless of body type.
        /// Note that sensor shapes may have mass.
        /// </summary>
        public readonly void ApplyMassFromShapes() => PhysicsBody_ApplyMassFromShapes(this);

        /// <summary>
        /// The linear damping of the body. This will reduce the linear velocity over time.
        /// See <see cref="LowLevelPhysics2D.PhysicsBody.linearVelocity"/>.
        /// </summary>
        public readonly float linearDamping { get => PhysicsBody_GetLinearDamping(this); set => PhysicsBody_SetLinearDamping(this, value); }

        /// <summary>
        /// The angular damping of the body. This will reduce the angular velocity over time.
        /// See <see cref="LowLevelPhysics2D.PhysicsBody.angularVelocity"/>.
        /// </summary>
        public readonly float angularDamping { get => PhysicsBody_GetAngularDamping(this); set => PhysicsBody_SetAngularDamping(this, value); }

        /// <summary>
        /// Scales the world gravity that is applied to this body.
        /// Setting the gravity scale to zero stops any gravity being applied. Likewise, a negative value inverts gravity.
        /// See <see cref="LowLevelPhysics2D.PhysicsWorld.gravity"/>.
        /// </summary>
        public readonly float gravityScale { get => PhysicsBody_GetGravityScale(this); set => PhysicsBody_SetGravityScale(this, value); }

        /// <summary>
        /// The awake state of the body.
        /// </summary>
        public readonly bool awake { get => PhysicsBody_GetAwake(this); set => PhysicsBody_SetAwake(this, value); }

        /// <summary>
        /// The sleeping ability of the body. If false, the body will never sleep and will be woken up.
        /// See <see cref="LowLevelPhysics2D.PhysicsBody.awake"/>.
        /// </summary>
        public readonly bool sleepingAllowed { get => PhysicsBody_GetSleepingAllowed(this); set => PhysicsBody_SetSleepingAllowed(this, value); }

        /// <summary>
        /// The threshold below which the body will sleep, usually in m/s.
        /// </summary>
        public readonly float sleepThreshold { get => PhysicsBody_GetSleepThreshold(this); set => PhysicsBody_SetSleepThreshold(this, value); }

        /// <summary>
        /// The enabled state of the body. If false, the body and anything attached to it will not participate in the simulation.
        /// </summary>
        public readonly bool enabled { get => PhysicsBody_GetEnabled(this); set => PhysicsBody_SetEnabled(this, value); }

        /// <summary>
        /// This allows this body to bypass rotational speed limits.
        /// This should only be used for circular objects, such as wheels, balls etc.
        /// </summary>
        public readonly bool fastRotationAllowed { get => PhysicsBody_GetFastRotationAllowed(this); set => PhysicsBody_SetFastRotationAllowed(this, value); }

        /// <summary>
        /// Treat this body as high speed object that performs continuous collision detection against dynamic and kinematic bodies, but not other high speed bodies.
        /// Fast collision bodies should be used sparingly. They are not a solution for general dynamic-versus-dynamic continuous collision.
        /// </summary>
        public readonly bool fastCollisionsAllowed { get => PhysicsBody_GetFastCollisionsAllowed(this); set => PhysicsBody_SetFastCollisionsAllowed(this, value); }

        /// <summary>
        /// Apply a force at a world point.
        /// If the force is not applied at the center of mass, it will generate a torque and affect the angular velocity.
        /// </summary>
        /// <param name="force">The world force vector, usually in newtons (N)</param>
        /// <param name="point">The world position of the point of application.</param>
        /// <param name="wake">Should the body be woken up.</param>
        public readonly void ApplyForce(Vector2 force, Vector2 point, bool wake = true) => PhysicsBody_ApplyForce(this, force, point, wake);

        /// <summary>
        /// Apply a force to the center of mass.
        /// </summary>
        /// <param name="force">The world force vector, usually in newtons (N).</param>
        /// <param name="wake">Should the body be woken up.</param>
        public readonly void ApplyForceToCenter(Vector2 force, bool wake = true) => PhysicsBody_ApplyForceToCenter(this, force, wake);

        /// <summary>
        /// Apply a torque.
        /// This affects the angular velocity without affecting the linear velocity.
        /// </summary>
        /// <param name="torque">Torque, usually in N*m.</param>
        /// <param name="wake">Should the body be woken up.</param>
        public readonly void ApplyTorque(float torque, bool wake = true) => PhysicsBody_ApplyTorque(this, torque, wake);

        /// <summary>
        /// Apply an impulse at a point.
        /// This immediately modifies the velocity and also modifies the angular velocity if the point of application is not at the center of mass.
        ///	This should be used for one-shot impulses.
        ///	If you need a steady force, use a force instead, which will work better with the sub-stepping solver.
        /// </summary>
        /// <param name="impulse">The world impulse vector, usually in N*s or kg*m/s.</param>
        /// <param name="point">The world position of the point of application.</param>
        /// <param name="wake">Should the body be woken up.</param>
        public readonly void ApplyLinearImpulse(Vector2 impulse, Vector2 point, bool wake = true) => PhysicsBody_ApplyLinearImpulse(this, impulse, point, wake);

        /// <summary>
        /// Apply an impulse to the center of mass.
        /// This immediately modifies the velocity.
        ///	This should be used for one-shot impulses.
        ///	If you need a steady force, use a force instead, which will work better with the sub-stepping solver.
        /// </summary>
        /// <param name="impulse">The world impulse vector, usually in N*s or kg*m/s.</param>
        /// <param name="wake">Should the body be woken up.</param>
        public readonly void ApplyLinearImpulseToCenter(Vector2 impulse, bool wake = true) => PhysicsBody_ApplyLinearImpulseToCenter(this, impulse, wake);

        /// <summary>
        /// Apply an angular impulse.
        ///	This should be used for one-shot impulses.
        ///	If you need a steady torque, use a torque instead, which will work better with the sub-stepping solver.
        /// </summary>
        /// <param name="impulse">The angular impulse, usually in units of kg*m*m/s.</param>
        /// <param name="wake">Should the body be woken up.</param>
        public readonly void ApplyAngularImpulse(float impulse, bool wake = true) => PhysicsBody_ApplyAngularImpulse(this, impulse, wake);

        /// <summary>
        /// Clear any user forces that have been applied to this body.
        /// Forces on a body are automatically cleared when a simulation step completes, however under some circumstances it may be desirable to clear the forces explicitly.
        /// </summary>
        public readonly void ClearForces() => PhysicsBody_ClearForces(this);

        /// <summary>
        /// Wake any bodies that are touching this body via their shapes.
        /// This also works for Static bodies.
        /// </summary>
        public readonly void WakeTouching() => PhysicsBody_WakeTouching(this);

        /// <summary>
        /// Enable/disable contact events on all shapes attached to the body.
        /// See <see cref="LowLevelPhysics2D.PhysicsShape.contactEvents"/>.
        /// </summary>
        /// <param name="contactEvents">Whether contact events are allowed on all shapes attached to this body or not.</param>
        public readonly void SetContactEvents(bool contactEvents) => PhysicsBody_SetContactEvents(this, contactEvents);

        /// <summary>
        /// Enable/disable hit events on all shapes attached to the body.
        /// See <see cref="LowLevelPhysics2D.PhysicsShape.hitEvents"/>.
        /// </summary>
        /// <param name="hitEvents">Whether hit events are allowed on all shapes attached to this body or not.</param>
        public readonly void SetHitEvents(bool hitEvents) => PhysicsBody_SetHitEvents(this, hitEvents);

        /// <summary>
        /// Set the (optional) owner object associated with this body and return an owner key that must be specified when destroying the body with <see cref="LowLevelPhysics2D.PhysicsBody.Destroy(int)"/>.   
        /// The physics system provides access to all objects, including the ability to destroy them so this feature can be used to stop accidental destruction of objects that are owned by other objects.
        /// You can only set the owner once, multiple attempts will produce a warning.
        /// The lifetime of the specified owner object is not linked to this body i.e. this body will still be owned by the owner object, even if it is destroyed.
        /// It is also valid to not specify an owner object (NULL) to simply gain an owner key however it can be useful, if simply for debugging purposes and discovery, to know which object is the owner.
        /// </summary>
        /// <param name="owner">The object that owns this body. This can be NULL if not required.</param>
        /// <returns>An owner key that must be passed to <see cref="LowLevelPhysics2D.PhysicsBody.Destroy(int)"/> when destroying the body.</returns>
        public readonly int SetOwner(Object owner) => PhysicsBody_SetOwner(this, owner);

        /// <summary>
        /// Get the owner object associated with this body as specified using <see cref="LowLevelPhysics2D.PhysicsBody.SetOwner(Object)"/>.
        /// </summary>
        /// <returns>The owner object associated with this body or NULL if no owner has been specified.</returns>
        public readonly Object GetOwner() => PhysicsBody_GetOwner(this);

        /// <summary>
        /// Get if the body is owned.
        /// See <see cref="LowLevelPhysics2D.PhysicsBody.SetOwner(Object)"/>.
        /// </summary>
        public readonly bool isOwned => PhysicsBody_IsOwned(this);

        /// <summary>
        /// Get/Set the <see cref="System.Object"/> that event callbacks for this body will be sent to.
        /// Care should be taken with any <see cref="System.Object"/> assigned as a callback target that isn't a <see cref="UnityEngine.Object"/> as this assignment will not in itself keep the object alive and can be garbage collected.
        /// To avoid this, you should have at least a single reference to the object in your code.
        /// 
        /// This includes the following events:
        /// 
        ///- A <see cref="LowLevelPhysics2D.PhysicsEvents.BodyUpdateEvent"/> with call <see cref="LowLevelPhysics2D.PhysicsCallbacks.IBodyUpdateCallback"/>.
        /// </summary>
        public readonly System.Object callbackTarget { get => PhysicsBody_GetCallbackTarget(this); set => PhysicsBody_SetCallbackTarget(this, value); }

        /// <summary>
        /// Get/Set <see cref="LowLevelPhysics2D.PhysicsUserData"/> that can be used for any purpose.
        /// The physics system doesn't use this data, it is entirely for custom use.
        /// </summary>
        public readonly PhysicsUserData userData { get => PhysicsBody_GetUserData(this); set => PhysicsBody_SetUserData(this, value); }

        /// <summary>
        /// Get <see cref="LowLevelPhysics2D.PhysicsUserData"/> that can be used for any purpose, typically by the owner only.
        /// </summary>
        public readonly PhysicsUserData ownerUserData { get => PhysicsBody_GetOwnerUserData(this); }

        /// <summary>
        /// Set <see cref="LowLevelPhysics2D.PhysicsUserData"/> that can be used for any purpose, typically by the owner only.
        /// </summary>
        /// <param name="physicsUserData">The user data to set.</param>
        /// <param name="ownerKey">Optional owner key returned when using <see cref="PhysicsBody.SetOwner(UnityEngine.Object)"/>.</param>
        public readonly void SetOwnerUserData(PhysicsUserData physicsUserData, int ownerKey = 0) => PhysicsBody_SetOwnerUserData(this, physicsUserData, ownerKey);

        /// <summary>
        /// Get/Set the transform object associated with the body.
        /// This can be used as a write transform and/or as a hint for debug drawing.
        /// See <see cref="LowLevelPhysics2D.PhysicsBody.transformWriteMode"/>.
        /// </summary>
        public readonly Transform transformObject { get => PhysicsBody_GetTransformObject(this); set => PhysicsBody_SetTransformObject(this, value); }

        /// <summary>
        /// Get/Set how the <see cref="LowLevelPhysics2D.PhysicsBody.transformObject"/> should be written to after the simulation has completed.
        /// Transform write will only occur if it is enabled on the world using <see cref="LowLevelPhysics2D.PhysicsWorld.transformWriteMode"/>.
        /// </summary>
        public readonly PhysicsBody.TransformWriteMode transformWriteMode { get => PhysicsBody_GetTransformWriteMode(this); set => PhysicsBody_SetTransformWriteMode(this, value); }

        /// <summary>
        /// Get the number of shapes attached to this body.
        /// Use <see cref="LowLevelPhysics2D.PhysicsBody.GetShapes"/> to retrieve the shapes.
        /// </summary>
        public readonly int shapeCount => PhysicsBody_GetShapeCount(this);

        /// <summary>
        /// Get the shapes attached to this body.
        /// </summary>
        /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
        /// <returns>The shapes attached to this body. This NativeArray must be disposed of after use otherwise leaks will occur. The exception to this is if the array is empty.</returns>
        public readonly NativeArray<PhysicsShape> GetShapes(Allocator allocator = Unity.Collections.Allocator.Temp) => PhysicsBody_GetShapes(this, allocator).ToNativeArray<PhysicsShape>();

        /// <summary>
        /// Get the number of joints attached to this body.
        /// Use <see cref="LowLevelPhysics2D.PhysicsBody.GetJoints"/> to retrieve the joints.
        /// </summary>
        public readonly int jointCount => PhysicsBody_GetJointCount(this);

        /// <summary>
        /// Get the joints attached to this body.
        /// </summary>
        /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
        /// <returns>The joints attached to this body. This NativeArray must be disposed of after use otherwise leaks will occur. The exception to this is if the array is empty.</returns>
        public readonly NativeArray<PhysicsJoint> GetJoints(Allocator allocator = Unity.Collections.Allocator.Temp) => PhysicsBody_GetJoints(this, allocator).ToNativeArray<PhysicsJoint>();

        /// <summary>
        /// Get all the touching contacts this body is currently participating in. Speculative collision is used so some contact points may be separated, a property available in the provided contact manifold.
        /// </summary>
        /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
        /// <returns>The touching contacts this body is currently participating in. This NativeArray must be disposed of after use otherwise leaks will occur. The exception to this is if the array is empty.</returns>
        public readonly NativeArray<PhysicsShape.Contact> GetContacts(Allocator allocator = Unity.Collections.Allocator.Temp) => PhysicsBody_GetContacts(this, allocator).ToNativeArray<PhysicsShape.Contact>();

        /// <summary>
        /// Get the world AABB that bounds all the shapes attached to this body.
        ///	If there are no shapes attached to the body then the returned AABB is empty and centered on the body origin.
        /// </summary>
        /// <returns>The world AABB that bounds all the shapes attached to this body.</returns>
        public readonly PhysicsAABB GetAABB() => PhysicsBody_CalculateAABB(this);

        #region Create Shapes

        /// <summary>
        /// Create a Circle shape, using its default definition, attached to this body.
        /// </summary>
        /// <param name="geometry">The geometry to use.</param>
        /// <param name="definition">The shape definition to use.</param>
        /// <returns>The created shape.</returns>
        public readonly PhysicsShape CreateShape(CircleGeometry geometry) => PhysicsShape.CreateShape(this, geometry);

        /// <summary>
        /// Create a Circle shape attached to this body.
        /// </summary>
        /// <param name="geometry">The geometry to use.</param>
        /// <param name="definition">The shape definition to use.</param>
        /// <returns>The created shape.</returns>
        public readonly PhysicsShape CreateShape(CircleGeometry geometry, PhysicsShapeDefinition definition) => PhysicsShape.CreateShape(this, geometry, definition);

        /// <summary>
        /// Create a batch of Circle shapes attached to this body.
        /// </summary>
        /// <param name="geometry">The shape geometry to use.</param>
        /// <param name="definition">The shape definition to use.</param>
        /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
        /// <returns>The created shapes. This NativeArray must be disposed of after use otherwise leaks will occur. The exception to this is if the array is empty.</returns>
        public readonly NativeArray<PhysicsShape> CreateShapeBatch(ReadOnlySpan<CircleGeometry> geometry, PhysicsShapeDefinition definition, Allocator allocator = Unity.Collections.Allocator.Temp) => PhysicsShape.CreateShapeBatch(this, geometry, definition, allocator);

        /// <summary>
        /// Create a Polygon shape, using its default definition, attached to this body.
        /// </summary>
        /// <param name="geometry">The geometry to use.</param>
        /// <param name="definition">The shape definition to use.</param>
        /// <returns>The created shape.</returns>
        public readonly PhysicsShape CreateShape(PolygonGeometry geometry) => PhysicsShape.CreateShape(this, geometry);

        /// <summary>
        /// Create a Polygon shape attached to this body.
        /// </summary>
        /// <param name="geometry">The geometry to use.</param>
        /// <param name="definition">The shape definition to use.</param>
        /// <returns>The created shape.</returns>
        public readonly PhysicsShape CreateShape(PolygonGeometry geometry, PhysicsShapeDefinition definition) => PhysicsShape.CreateShape(this, geometry, definition);

        /// <summary>
        /// Create a batch of Polygon shapes attached to this body.
        /// </summary>
        /// <param name="geometry">The shape geometry to use.</param>
        /// <param name="definition">The shape definition to use.</param>
        /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
        /// <returns>The created shapes. This NativeArray must be disposed of after use otherwise leaks will occur. The exception to this is if the array is empty.</returns>
        public readonly NativeArray<PhysicsShape> CreateShapeBatch(ReadOnlySpan<PolygonGeometry> geometry, PhysicsShapeDefinition definition, Allocator allocator = Unity.Collections.Allocator.Temp) => PhysicsShape.CreateShapeBatch(this, geometry, definition, allocator);

        /// <summary>
        /// Create a Capsule shape, using its default definition, attached to this body.
        /// </summary>
        /// <param name="geometry">The geometry to use.</param>
        /// <param name="definition">The shape definition to use.</param>
        /// <returns>The created shape.</returns>
        public readonly PhysicsShape CreateShape(CapsuleGeometry geometry) => PhysicsShape.CreateShape(this, geometry);

        /// <summary>
        /// Create a Capsule shape attached to this body.
        /// </summary>
        /// <param name="geometry">The geometry to use.</param>
        /// <param name="definition">The shape definition to use.</param>
        /// <returns>The created shape.</returns>
        public readonly PhysicsShape CreateShape(CapsuleGeometry geometry, PhysicsShapeDefinition definition) => PhysicsShape.CreateShape(this, geometry, definition);

        /// <summary>
        /// Create a batch of Capsule shapes attached to this body.
        /// </summary>
        /// <param name="geometry">The shape geometry to use.</param>
        /// <param name="definition">The shape definition to use.</param>
        /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
        /// <returns>The created shapes. This NativeArray must be disposed of after use otherwise leaks will occur. The exception to this is if the array is empty.</returns>
        public readonly NativeArray<PhysicsShape> CreateShapeBatch(ReadOnlySpan<CapsuleGeometry> geometry, PhysicsShapeDefinition definition, Allocator allocator = Unity.Collections.Allocator.Temp) => PhysicsShape.CreateShapeBatch(this, geometry, definition, allocator);

        /// <summary>
        /// Create a Segment shape, using its default definition, attached to this body.
        /// </summary>
        /// <param name="geometry">The geometry to use.</param>
        /// <param name="definition">The shape definition to use.</param>
        /// <returns>The created shape.</returns>
        public readonly PhysicsShape CreateShape(SegmentGeometry geometry) => PhysicsShape.CreateShape(this, geometry);

        /// <summary>
        /// Create a Segment shape attached to this body.
        /// </summary>
        /// <param name="geometry">The geometry to use.</param>
        /// <param name="definition">The shape definition to use.</param>
        /// <returns>The created shape.</returns>
        public readonly PhysicsShape CreateShape(SegmentGeometry geometry, PhysicsShapeDefinition definition) => PhysicsShape.CreateShape(this, geometry, definition);

        /// <summary>
        /// Create a batch of Segment shapes attached to this body.
        /// </summary>
        /// <param name="geometry">The shape geometry to use.</param>
        /// <param name="definition">The shape definition to use.</param>
        /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
        /// <returns>The created shapes. This NativeArray must be disposed of after use otherwise leaks will occur. The exception to this is if the array is empty.</returns>
        public readonly NativeArray<PhysicsShape> CreateShapeBatch(ReadOnlySpan<SegmentGeometry> geometry, PhysicsShapeDefinition definition, Allocator allocator = Unity.Collections.Allocator.Temp) => PhysicsShape.CreateShapeBatch(this, geometry, definition, allocator);

        /// <summary>
        /// Create a Chain Segment shape, using its default definition, attached to this body.
        /// </summary>
        /// <param name="geometry">The geometry to use.</param>
        /// <param name="definition">The shape definition to use.</param>
        /// <returns>The created shape.</returns>
        public readonly PhysicsShape CreateShape(ChainSegmentGeometry geometry) => PhysicsShape.CreateShape(this, geometry);

        /// <summary>
        /// Create a Chain Segment shape attached to this body.
        /// </summary>
        /// <param name="geometry">The geometry to use.</param>
        /// <param name="definition">The shape definition to use.</param>
        /// <returns>The created shape.</returns>
        public readonly PhysicsShape CreateShape(ChainSegmentGeometry geometry, PhysicsShapeDefinition definition) => PhysicsShape.CreateShape(this, geometry, definition);

        /// <summary>
        /// Create a batch of Chain Segment shapes attached to this body.
        /// </summary>
        /// <param name="geometry">The shape geometry to use.</param>
        /// <param name="definition">The shape definition to use.</param>
        /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
        /// <returns>The created shapes. This NativeArray must be disposed of after use otherwise leaks will occur. The exception to this is if the array is empty.</returns>
        public readonly NativeArray<PhysicsShape> CreateShapeBatch(ReadOnlySpan<ChainSegmentGeometry> geometry, PhysicsShapeDefinition definition, Allocator allocator = Unity.Collections.Allocator.Temp) => PhysicsShape.CreateShapeBatch(this, geometry, definition, allocator);

        /// <summary>
        /// Create a Chain attached to this body.
        /// </summary>
        /// <param name="geometry">The geometry to use.</param>
        /// <param name="definition">The chain definition to use.</param>
        /// <returns>The created chain.</returns>
        public readonly PhysicsChain CreateChain(ChainGeometry geometry, PhysicsChainDefinition definition) => PhysicsChain.Create(this, geometry, definition);

        #endregion

        #region Debugging

        /// <summary>
        /// Draw a body that visually represents its current state in the world.
        /// This is only used in the Unity Editor or in a Development Player.
        /// </summary>
        public readonly void Draw() => PhysicsBody_Draw(this);

        #endregion
    }
}
