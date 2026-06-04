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
    /// Defines a common joint interface.
    /// This is a helper implementation interface (used for commonality/consistency) and should not be used to access a joint.
    /// </summary>
    interface IPhysicsJoint<T>
    {
        /// <undoc/>
        bool Destroy(int ownerKey = 0);

        /// <undoc/>
        bool isValid { get; }

        /// <undoc/>
        PhysicsWorld world { get; }

        /// <undoc/>
        PhysicsJoint.JointType jointType { get; }

        /// <undoc/>
        PhysicsBody bodyA { get; }

        /// <undoc/>
        PhysicsBody bodyB { get; }

        /// <undoc/>
        PhysicsTransform localAnchorA { get; set; }

        /// <undoc/>
        PhysicsTransform localAnchorB { get; set; }

        /// <undoc/>
        float forceThreshold { get; set; }

        /// <undoc/>
        float torqueThreshold { get; set; }

        /// <undoc/>
        bool collideConnected { get; set; }

        /// <undoc/>
        float tuningFrequency { get; set; }

        /// <undoc/>
        float tuningDamping { get; set; }

        /// <undoc/>
        float drawScale { get; set; }

        /// <undoc/>
        void WakeBodies();

        /// <undoc/>
        Vector2 currentConstraintForce { get; }

        /// <undoc/>
        float currentConstraintTorque { get; }

        /// <undoc/>
        float currentLinearSeparationError { get; }

        /// <undoc/>
        float currentAngularSeparationError { get; }

        /// <undoc/>
        void SetOwner(UnityEngine.Object owner, int ownerKey);

        /// <undoc/>
        int SetOwner(UnityEngine.Object owner);

        /// <undoc/>
        UnityEngine.Object GetOwner();

        /// <undoc/>
        bool isOwned { get; }

        /// <undoc/>
        System.Object callbackTarget { get; set; }

        /// <undoc/>
        PhysicsUserData userData { get; set; }

        /// <undoc/>
        PhysicsUserData ownerUserData { get; }

        /// <undoc/>
        void SetOwnerUserData(PhysicsUserData physicsUserData, int ownerKey = 0);

        /// <undoc/>
        bool worldDrawing { get; set; }

        /// <undoc/>
        void Draw();
    }

    /// <summary>
    /// A joint is used to constrain bodies to the world or to each other in various ways.
    /// A joint is automatically destroyed when either body it is attached to is destroyed. A joint cannot exist unattached from a body.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [MovedFrom(autoUpdateAPI: ScriptUpdateConstants.AutoUpdateAPI, sourceNamespace: ScriptUpdateConstants.SourceNamespace, sourceAssembly: ScriptUpdateConstants.SourceAssembly)]
    public readonly struct PhysicsJoint : IPhysicsJoint<PhysicsJoint>, IPhysicsHandle<PhysicsJoint>, IEquatable<PhysicsJoint>
    {
        #region Handle

        /// <undoc/>
        readonly PhysicsHandle m_PhysicsHandle;

        /// <summary>
        /// Create a joint from a physics handle.
        /// 
        /// NOTE: You must ensure that the physics handle represents the correct object type otherwise hard to detect bugs can occur.
        /// </summary>
        /// <param name="physicsHandle">The physics handle to use.</param>
        public PhysicsJoint(PhysicsHandle physicsHandle) { m_PhysicsHandle = physicsHandle; }

        /// <summary>
        /// Get the physics handle.
        /// </summary>
        public readonly PhysicsHandle physicsHandle => m_PhysicsHandle;

        /// <undoc/>
        public override readonly string ToString() => isValid ? $"type={jointType}, {m_PhysicsHandle}" : "<INVALID>";

        #endregion

        #region Equality

        /// <undoc/>
        public override bool Equals(object obj) => obj is PhysicsJoint other && Equals(other);

        /// <undoc/>
        public bool Equals(PhysicsJoint other) => m_PhysicsHandle == other.m_PhysicsHandle;

        /// <undoc/>
        public static bool operator ==(PhysicsJoint lhs, PhysicsJoint rhs) => lhs.Equals(rhs);

        /// <undoc/>
        public static bool operator !=(PhysicsJoint lhs, PhysicsJoint rhs) => !(lhs == rhs);

        /// <undoc/>
        public override int GetHashCode() => m_PhysicsHandle.GetHashCode();

        #endregion

        #region IPhysicsJoint

        /// <summary>
        /// Destroy the joint.
        /// If the object is owned with <see cref="PhysicsJoint.SetOwner(UnityEngine.Object)"/> then you must provide the owner key it returned. Failing to do so will return a warning and the joint will not be destroyed.
        /// </summary>
        /// <param name="ownerKey">Optional owner key returned when using <see cref="PhysicsJoint.SetOwner(UnityEngine.Object)"/>.</param>
        /// <returns>If the joint was destroyed or not.</returns>
        public readonly bool Destroy(int ownerKey = 0) => PhysicsJoint_Destroy(this, ownerKey);

        /// <summary>
        /// Checks if the joint is valid.
        /// </summary>
        public readonly bool isValid => PhysicsJoint_IsValid(this);

        /// <summary>
        /// Get the world the body is attached to.
        /// </summary>
        public readonly PhysicsWorld world => PhysicsJoint_GetWorld(this);

        /// <summary>
        /// Gets the joint type.
        /// See <see cref="PhysicsJoint.JointType"/>.
        /// </summary>
        public readonly PhysicsJoint.JointType jointType => PhysicsJoint_GetJointType(this);

        /// <summary>
        /// The second body the joint constrains.
        /// </summary>
        public readonly PhysicsBody bodyA => PhysicsJoint_GetBodyA(this);

        /// <summary>
        /// A local anchor point on the first body for the constraint.
        /// </summary>
        public readonly PhysicsBody bodyB => PhysicsJoint_GetBodyB(this);

        /// <summary>
        /// The local anchor frame constraint relative to bodyA's origin.
        /// </summary>
        public readonly PhysicsTransform localAnchorA { get => PhysicsJoint_GetLocalAnchorA(this); set => PhysicsJoint_SetLocalAnchorA(this, value); }

        /// <summary>
        /// The local anchor frame constraint relative to bodyB's origin.
        /// </summary>
        public readonly PhysicsTransform localAnchorB { get => PhysicsJoint_GetLocalAnchorB(this); set => PhysicsJoint_SetLocalAnchorB(this, value); }

        /// <summary>
        /// The force threshold beyond which a joint event will be produced.
        /// </summary>
        public readonly float forceThreshold { get => PhysicsJoint_GetForceThreshold(this); set => PhysicsJoint_SetForceThreshold(this, value); }

        /// <summary>
        /// The torque threshold beyond which a joint event will be produced.
        /// </summary>
        public readonly float torqueThreshold { get => PhysicsJoint_GetTorqueThreshold(this); set => PhysicsJoint_SetTorqueThreshold(this, value); }

        /// <summary>
        /// Whether the shapes on the pair of bodies can come into contact.
        /// </summary>
        public readonly bool collideConnected { get => PhysicsJoint_GetCollideConnected(this); set => PhysicsJoint_SetCollideConnected(this, value); }

        /// <summary>
        /// Controls the joint stiffness frequency, in cycles per second.
        /// </summary>
        public readonly float tuningFrequency { get => PhysicsJoint_GetTuningFrequency(this); set => PhysicsJoint_SetTuningFrequency(this, value); }

        /// <summary>
        /// Controls the joint stiffness damping, non-dimensional.
        /// Use 1 for critical damping.
        /// </summary>
        public readonly float tuningDamping { get => PhysicsJoint_GetTuningDamping(this); set => PhysicsJoint_SetTuningDamping(this, value); }

        /// <summary>
        /// Controls the scaling of the joint drawing.
        /// Not all joints have scalable elements but those that do will use this scaling.
        /// </summary>
        public readonly float drawScale { get => PhysicsJoint_GetDrawScale(this); set => PhysicsJoint_SetDrawScale(this, value); }

        /// <summary>
        /// Wake the pair of bodies the joint is constraining.
        /// </summary>
        public readonly void WakeBodies() => PhysicsJoint_WakeBodies(this);

        /// <summary>
        /// Get the current constraint force used by the joint, usually in newtons.
        /// </summary>
        public readonly Vector2 currentConstraintForce => PhysicsJoint_GetCurrentConstraintForce(this);

        /// <summary>
        /// Get the current constraint torque used by the joint, usually in newtons.
        /// </summary>
        public readonly float currentConstraintTorque => PhysicsJoint_GetCurrentConstraintTorque(this);

        /// <summary>
        /// Get the current linear separation error for this joint, usually in meters.
        /// This does not consider admissible movement.
        /// </summary>
        public readonly float currentLinearSeparationError => PhysicsJoint_GetCurrentLinearSeparation(this);

        /// <summary>
        /// Get the current angular separation error for this joint, in degrees.
        /// This does not consider admissible movement.
        /// </summary>
        public readonly float currentAngularSeparationError => PhysicsJoint_GetCurrentAngularSeparation(this);

        /// <summary>
        /// Set the owner object using the specified owner key.
        /// You can only set the owner once, multiple attempts will produce a warning.
        /// This call does not bind the lifetime of the specified owner object, it is simply a reference.
        /// Whilst it is valid to not specify an owner object (NULL), it is recommended for debugging purposes.
        /// </summary>
        /// <param name="joints">The bodies to set ownership for.</param>
        /// <param name="owner">The object that owns this key. Whilst it is valid to not specify an owner object (NULL), it is recommended for debugging purposes.</param>
        /// <param name="ownerKey">The owner key to be used. The value must be non-zero. You can use <see cref="PhysicsWorld.CreateOwnerKey(UnityEngine.Object)"/> for this value although any non-zero integer will work.</param>
        public static void SetOwner(ReadOnlySpan<PhysicsJoint> joints, UnityEngine.Object owner, int ownerKey) => PhysicsJoint_SetOwner(joints, owner, ownerKey);

        /// <summary>
        /// Set the owner object using the specified owner key.
        /// You can only set the owner once, multiple attempts will produce a warning.
        /// This call does not bind the lifetime of the specified owner object, it is simply a reference.
        /// It is also valid to not specify an owner object (NULL) to simply gain an owner key however it can be useful, if simply for debugging purposes and discovery, to know which object is the owner.
        /// </summary>
        /// <param name="owner">The object that owns this key. This can be NULL if not required but is recommended as the key is formed in part by the hash-code of the owner object.</param>
        /// <param name="ownerKey">The owner key to be used. If zero then a new owner key is created. You can use <see cref="PhysicsWorld.CreateOwnerKey(UnityEngine.Object)"/> for this value although any non-zero integer will work.</param>
        public unsafe readonly void SetOwner(UnityEngine.Object owner, int ownerKey)
        {
            var joint = this;
            SetOwner(new ReadOnlySpan<PhysicsJoint>(&joint, 1), owner, ownerKey);
        }

        /// <summary>
        /// Set the owner object using the specified owner key.
        /// You can only set the owner once, multiple attempts will produce a warning.
        /// This call does not bind the lifetime of the specified owner object, it is simply a reference.
        /// It is also valid to not specify an owner object (NULL) to simply gain an owner key however it can be useful, if simply for debugging purposes and discovery, to know which object is the owner.
        /// </summary>
        /// <param name="owner">The object that owns this key. This can be NULL if not required but is recommended as the key is formed in part by the hash-code of the owner object.</param>
        /// <returns>The owner key assigned.</returns>
        public readonly int SetOwner(UnityEngine.Object owner)
        {
            var ownerKey = PhysicsWorld.CreateOwnerKey(owner);
            SetOwner(owner, ownerKey);
            return ownerKey;
        }

        /// <summary>
        /// Get the owner object associated with this joint as specified using <see cref="PhysicsJoint.SetOwner(UnityEngine.Object)"/>.
        /// </summary>
        /// <returns>The owner object associated with this joint or NULL if no owner has been specified.</returns>
        public readonly UnityEngine.Object GetOwner() => PhysicsJoint_GetOwner(this);

        /// <summary>
        /// Get if the joint is owned.
        /// See <see cref="PhysicsJoint.SetOwner(UnityEngine.Object)"/>.
        /// </summary>
        public readonly bool isOwned => PhysicsJoint_IsOwned(this);

        /// <summary>
        /// Get/Set the <see cref="System.Object"/> object that event callbacks for this joint will be sent to.
        /// Care should be taken with any <see cref="System.Object"/> assigned as a callback target that isn't a <see cref="UnityEngine.Object"/> as this assignment will not in itself keep the object alive and can be garbage collected.
        /// To avoid this, you should have at least a single reference to the object in your code.
        /// To remove the object assigned here, set the callback target to NULL.
        /// 
        /// This includes the following events:
        /// 
        ///- A <see cref="PhysicsEvents.JointThresholdEvent"/> with call <see cref="PhysicsCallbacks.IJointThresholdCallback"/>.
        /// </summary>
        public readonly System.Object callbackTarget { get => PhysicsJoint_GetCallbackTarget(this); set => PhysicsJoint_SetCallbackTarget(this, value); }

        /// <summary>
        /// Get/Set <see cref="PhysicsUserData"/> that can be used for any purpose.
        /// The physics system doesn't use this data, it is entirely for custom use.
        /// </summary>
        public readonly PhysicsUserData userData { get => PhysicsJoint_GetUserData(this); set => PhysicsJoint_SetUserData(this, value); }

        /// <summary>
        /// Get <see cref="PhysicsUserData"/> that can be used for any purpose, typically by the owner only.
        /// </summary>
        public readonly PhysicsUserData ownerUserData { get => PhysicsJoint_GetOwnerUserData(this); }

        /// <summary>
        /// Set <see cref="PhysicsUserData"/> that can be used for any purpose, typically by the owner only.
        /// </summary>
        /// <param name="physicsUserData">The user data to set.</param>
        /// <param name="ownerKey">Optional owner key returned when using <see cref="PhysicsJoint.SetOwner(UnityEngine.Object)"/>.</param>
        public readonly void SetOwnerUserData(PhysicsUserData physicsUserData, int ownerKey = 0) => PhysicsJoint_SetOwnerUserData(this, physicsUserData, ownerKey);

        /// <summary>
        /// Controls whether this joint is automatically drawn when the world is drawn.
        /// </summary>
        public readonly bool worldDrawing { get => PhysicsJoint_GetWorldDrawing(this); set => PhysicsJoint_SetWorldDrawing(this, value); }

        /// <summary>
        /// Draw a PhysicsJoint that visually represents its current state in the world.
        /// </summary>
        public readonly void Draw() => PhysicsJoint_Draw(this);

        #endregion

        /// <summary>
        /// The type of joint.
        /// </summary>
        public enum JointType
        {
            /// <summary>
            /// Constrain the distance between a pair of bodies.
            /// </summary>
            DistanceJoint = 0,

            /// <summary>
            /// Used to ignore collision between two specific bodies.
            /// As a side effect of being a joint, it also keeps the two bodies in the same simulation island.
            /// </summary>
            IgnoreJoint = 1,

            /// <summary>
            /// Constrain the relative translation and rotation between a pair of bodies.
            /// This joint type is also know as a Motor joint.
            /// </summary>
            RelativeJoint = 2,

            /// <summary>
            /// Constrain the relative translation along an axis between a pair of bodies.
            /// This joint type is also know as a Prismatic joint.
            /// </summary>
            SliderJoint = 3,

            /// <summary>
            /// Constrain the rotation between a pair of bodies.
            /// This joint type is also know as a Revolute joint.
            /// </summary>
            HingeJoint = 4,

            /// <summary>
            /// Constrain a fixed translation and rotation between a pair of bodies.
            /// This joint type is also know as a Weld joint.
            /// </summary>
            FixedJoint = 5,

            /// <summary>
            /// Constrain a translation and rotation between a pair of bodies.
            /// </summary>
            WheelJoint = 6
        }

        #region Create / Destroy

        /// <summary>
        /// Create a PhysicsDistanceJoint in the world.
        /// See <see cref="PhysicsDistanceJoint.Create(PhysicsWorld, PhysicsDistanceJointDefinition)"/>.
        /// </summary>
        /// <param name="world">The world to create the joint in.</param>
        /// <param name="definition">The joint definition to use.</param>
        /// <returns>The created joint.</returns>
        public static PhysicsDistanceJoint CreateJoint(PhysicsWorld world, PhysicsDistanceJointDefinition definition) => PhysicsDistanceJoint.Create(world, definition);

        /// <summary>
        /// Create a PhysicsRelativeJoint in the world.
        /// See <see cref="PhysicsRelativeJoint.Create(PhysicsWorld, PhysicsRelativeJointDefinition)"/>.
        /// </summary>
        /// <param name="world">The world to create the joint in.</param>
        /// <param name="definition">The joint definition to use.</param>
        /// <returns>The created joint.</returns>
        public static PhysicsRelativeJoint CreateJoint(PhysicsWorld world, PhysicsRelativeJointDefinition definition) => PhysicsRelativeJoint.Create(world, definition);

        /// <summary>
        /// Create an IgnoreJoint in the world.
        /// See <see cref="PhysicsIgnoreJoint.Create(PhysicsWorld, PhysicsIgnoreJointDefinition)"/>.
        /// </summary>
        /// <param name="world">The world to create the joint in.</param>
        /// <param name="definition">The joint definition to use.</param>
        /// <returns>The created joint.</returns>
        public static PhysicsIgnoreJoint CreateJoint(PhysicsWorld world, PhysicsIgnoreJointDefinition definition) => PhysicsIgnoreJoint.Create(world, definition);

        /// <summary>
        /// Create a SliderJoint in the world.
        /// See <see cref="PhysicsSliderJoint.Create(PhysicsWorld, PhysicsSliderJointDefinition)"/>.
        /// </summary>
        /// <param name="world">The world to create the joint in.</param>
        /// <param name="definition">The joint definition to use.</param>
        /// <returns>The created joint.</returns>
        public static PhysicsSliderJoint CreateJoint(PhysicsWorld world, PhysicsSliderJointDefinition definition) => PhysicsSliderJoint.Create(world, definition);

        /// <summary>
        /// Create a PhysicsHingeJoint in the world.
        /// See <see cref="PhysicsHingeJoint.Create(PhysicsWorld, PhysicsHingeJointDefinition)"/>.
        /// </summary>
        /// <param name="world">The world to create the joint in.</param>
        /// <param name="definition">The joint definition to use.</param>
        /// <returns>The created joint.</returns>
        public static PhysicsHingeJoint CreateJoint(PhysicsWorld world, PhysicsHingeJointDefinition definition) => PhysicsHingeJoint.Create(world, definition);

        /// <summary>
        /// Create a FixedJoint in the world.
        /// See <see cref="PhysicsFixedJoint.Create(PhysicsWorld, PhysicsFixedJointDefinition)"/>.
        /// </summary>
        /// <param name="world">The world to create the joint in.</param>
        /// <param name="definition">The joint definition to use.</param>
        /// <returns>The created joint.</returns>
        public static PhysicsFixedJoint CreateJoint(PhysicsWorld world, PhysicsFixedJointDefinition definition) => PhysicsFixedJoint.Create(world, definition);

        /// <summary>
        /// Create a WheelJoint in the world.
        /// See <see cref="PhysicsWheelJoint.Create(PhysicsWorld, PhysicsWheelJointDefinition)"/>.
        /// </summary>
        /// <param name="world">The world to create the joint in.</param>
        /// <param name="definition">The joint definition to use.</param>
        /// <returns>The created joint.</returns>
        public static PhysicsWheelJoint CreateJoint(PhysicsWorld world, PhysicsWheelJointDefinition definition) => PhysicsWheelJoint.Create(world, definition);

        /// <summary>
        /// Destroy a batch of joints.
        /// Owned joints will produce a warning and will not be destroyed (see <see cref="PhysicsJoint.SetOwner(UnityEngine.Object)"/>).
        /// Any invalid joints will be ignored.
        /// </summary>
        /// <param name="joints">The joints to destroy.</param>
        public static void DestroyBatch(ReadOnlySpan<PhysicsJoint> joints) => PhysicsJoint_DestroyBatch(joints);

        #endregion
    }
}
