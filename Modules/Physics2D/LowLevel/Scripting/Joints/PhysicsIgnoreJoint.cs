// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using static UnityEngine.LowLevelPhysics2D.PhysicsLowLevelScripting2D;

namespace UnityEngine.LowLevelPhysics2D
{
    /// <summary>
    /// A joint used to ignore collision between two specific bodies.
    /// As a side effect of being a joint, it also keeps the two bodies in the same simulation island meaning they'll wake/sleep at the same time and be solved together on the same thread.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct PhysicsIgnoreJoint : IPhysicsJoint, IEquatable<PhysicsIgnoreJoint>
    {
        #region Id

        /// <summary>
        /// The base joint Id.
        /// </summary>
        readonly PhysicsJoint m_Id;

        /// <summary>
        /// Cast to the base <see cref="LowLevelPhysics2D.PhysicsJoint"/>.
        /// </summary>
        /// <param name="joint">The current joint.</param>
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] public static implicit operator PhysicsJoint(PhysicsIgnoreJoint joint) => joint.m_Id;

        /// <summary>
        /// Cast to a <see cref="LowLevelPhysics2D.PhysicsIgnoreJoint"/> from the base <see cref="LowLevelPhysics2D.PhysicsJoint"/>.
        /// The provided joint must be a joint type of <see cref="LowLevelPhysics2D.PhysicsJoint.JointType.IgnoreJoint"/>.
        /// </summary>
        /// <param name="physicsJoint">The base joint to cast.</param>
        /// <exception cref="InvalidCastException">Thrown if the joint type is invalid.</exception>
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] public static implicit operator PhysicsIgnoreJoint(PhysicsJoint joint) => new(joint);

        /// <summary>
        /// Create a <see cref="LowLevelPhysics2D.PhysicsIgnoreJoint"/> from the specified base joint.
        /// The provided joint must be a joint type of <see cref="LowLevelPhysics2D.PhysicsJoint.JointType.IgnoreJoint"/>.
        /// </summary>
        /// <param name="physicsJoint">The base joint to cast.</param>
        /// <exception cref="InvalidCastException">Thrown if the joint type is invalid.</exception>
        public PhysicsIgnoreJoint(PhysicsJoint physicsJoint)
        {
            // Validate.
            if (physicsJoint.jointType != PhysicsJoint.JointType.IgnoreJoint)
                throw new InvalidCastException($"The joint must be of type {nameof(PhysicsJoint.JointType.IgnoreJoint)} but is of type {physicsJoint.jointType}.");

            m_Id = physicsJoint;
        }

        /// <undoc/>
        public override readonly string ToString() => m_Id.ToString();

        #endregion

        #region Equality

        /// <undoc/>
        public override bool Equals(object obj) => m_Id.Equals(obj);

        /// <undoc/>
        public bool Equals(PhysicsIgnoreJoint other) => m_Id.Equals(other);

        /// <undoc/>
        public static bool operator ==(PhysicsIgnoreJoint lhs, PhysicsIgnoreJoint rhs) => lhs.Equals(rhs);

        /// <undoc/>
        public static bool operator !=(PhysicsIgnoreJoint lhs, PhysicsIgnoreJoint rhs) => !(lhs == rhs);

        /// <undoc/>
        public override int GetHashCode() => m_Id.GetHashCode();

        #endregion

        #region IPhysicsJoint

        /// <summary>
        /// Destroy the joint.
        /// If the object is owned with <see cref="LowLevelPhysics2D.PhysicsJoint.SetOwner(Object)"/> then you must provide the owner key it returned. Failing to do so will return a warning and the joint will not be destroyed.
        /// </summary>
        /// <param name="ownerKey">Optional owner key returned when using <see cref="LowLevelPhysics2D.PhysicsJoint.SetOwner(Object)"/>.</param>
        /// <returns>If the joint was destroyed or not.</returns>
        public readonly bool Destroy(int ownerKey = 0) => m_Id.Destroy(ownerKey);

        /// <summary>
        /// Checks if the joint is valid.
        /// </summary>
        public readonly bool isValid => m_Id.isValid;

        /// <summary>
        /// Get the world the body is attached to.
        /// </summary>
        public readonly PhysicsWorld world => m_Id.world;

        /// <summary>
        /// Gets the joint type.
        /// See <see cref="LowLevelPhysics2D.PhysicsJoint.JointType"/>.
        /// </summary>
        public readonly PhysicsJoint.JointType jointType => m_Id.jointType;

        /// <summary>
        /// The second body the joint constrains.
        /// </summary>
        public readonly PhysicsBody bodyA => m_Id.bodyA;

        /// <summary>
        /// A local anchor point on the first body for the constraint.
        /// </summary>
        public readonly PhysicsBody bodyB => m_Id.bodyB;

        /// <summary>
        /// This is unused in this specific joint.
        /// Typically this is the local anchor frame constraint relative to bodyA's origin.
        /// </summary>
        public readonly PhysicsTransform localAnchorA { get => m_Id.localAnchorA; set => m_Id.localAnchorA = value; }

        /// <summary>
        /// This is unused in this specific joint.
        /// Typically this is the local anchor frame constraint relative to bodyB's origin.
        /// </summary>
        public readonly PhysicsTransform localAnchorB { get => m_Id.localAnchorB; set => m_Id.localAnchorB = value; }

        /// <summary>
        /// This is unused in this specific joint.
        /// Typically this is the force threshold beyond which a joint event will be produced.
        /// </summary>
        public readonly float forceThreshold { get => m_Id.forceThreshold; set => m_Id.forceThreshold = value; }

        /// <summary>
        /// This is unused in this specific joint.
        /// Typically this is the torque threshold beyond which a joint event will be produced.
        /// </summary>
        public readonly float torqueThreshold { get => m_Id.torqueThreshold; set => m_Id.torqueThreshold = value; }

        /// <summary>
        /// This is unused in this specific joint and is always false.
        /// Typically this gets whether the shapes on the pair of bodies can come into contact.
        /// </summary>
        public readonly bool collideConnected { get => m_Id.collideConnected; set => m_Id.collideConnected = value; }

        /// <summary>
        /// This is unused in this specific joint.
        /// Typically this would control the joint stiffness frequency, in cycles per second.
        /// </summary>
        public readonly float tuningFrequency { get => m_Id.tuningFrequency; set => m_Id.tuningFrequency = value; }

        /// <summary>
        /// This is unused in this specific joint.
        /// Typically this would control the joint stiffness damping, non-dimensional.
        /// Use 1 for critical damping.
        /// </summary>
        public readonly float tuningDamping { get => m_Id.tuningDamping; set => m_Id.tuningDamping = value; }

        /// <summary>
        /// This is unused in this specific joint.
        /// Typically this would control the scaling of the joint drawing.
        /// </summary>
        public readonly float drawScale { get => m_Id.drawScale; set => m_Id.drawScale = value; }

        /// <summary>
        /// Wake the pair of bodies the joint is constraining.
        /// </summary>
        public readonly void WakeBodies() => m_Id.WakeBodies();

        /// <summary>
        /// This is unused in this specific joint.
        /// Typically this would get the current constraint force used by the joint, usually in newtons.
        /// </summary>
        public readonly Vector2 currentConstraintForce => m_Id.currentConstraintForce;

        /// <summary>
        /// This is unused in this specific joint.
        /// Typically this would get the current constraint torque used by the joint, usually in newtons.
        /// </summary>
        public readonly float currentConstraintTorque => m_Id.currentConstraintTorque;

        /// <summary>
        /// This is unused in this specific joint.
        /// Typically this would get the current linear separation error for this joint.
        /// </summary>
        public readonly float currentLinearSeparationError => m_Id.currentLinearSeparationError;

        /// <summary>
        /// This is unused in this specific joint.
        /// Typically this would get the current angular separation error for this joint.
        /// </summary>
        public readonly float currentAngularSeparationError => m_Id.currentAngularSeparationError;

        /// <summary>
        /// Set the (optional) owner object associated with this joint and return an owner key that must be specified when destroying the joint with <see cref="LowLevelPhysics2D.PhysicsJoint.Destroy(int)"/>.   
        /// The physics system provides access to all objects, including the ability to destroy them so this feature can be used to stop accidental destruction of objects that are owned by other objects.
        /// You can only set the owner once, multiple attempts will produce a warning.
        /// The lifetime of the specified owner object is not linked to this joint i.e. this joint will still be owned by the owner object, even if it is destroyed.
        /// It is also valid to not specify an owner object (NULL) to simply gain an owner key however it can be useful, if simply for debugging purposes and discovery, to know which object is the owner.
        /// </summary>
        /// <param name="owner">The object that owns this joint. This can be NULL if not required.</param>
        /// <returns>An owner key that must be passed to <see cref="LowLevelPhysics2D.PhysicsJoint.Destroy(int)"/> when destroying the joint.</returns>
        public readonly int SetOwner(Object owner) => m_Id.SetOwner(owner);

        /// <summary>
        /// Get the owner object associated with this joint as specified using <see cref="LowLevelPhysics2D.PhysicsJoint.SetOwner(Object)"/>.
        /// </summary>
        /// <returns>The owner object associated with this joint or NULL if no owner has been specified.</returns>
        public readonly Object GetOwner() => m_Id.GetOwner();

        /// <summary>
        /// Get if the joint is owned.
        /// See <see cref="LowLevelPhysics2D.PhysicsJoint.SetOwner(Object)"/>.
        /// </summary>
        public readonly bool isOwned => m_Id.isOwned;

        /// <summary>
        /// Get/Set the <see cref="System.Object"/> that event callbacks for this joint will be sent to.
        /// Care should be taken with any <see cref="System.Object"/> assigned as a callback target that isn't a <see cref="UnityEngine.Object"/> as this assignment will not in itself keep the object alive and can be garbage collected.
        /// To avoid this, you should have at least a single reference to the object in your code.
        /// 
        /// This includes the following events:
        /// 
        ///- A <see cref="LowLevelPhysics2D.PhysicsEvents.JointThresholdEvent"/> with call <see cref="LowLevelPhysics2D.PhysicsCallbacks.IJointThresholdCallback"/>.
        /// </summary>
        public readonly System.Object callbackTarget { get => m_Id.callbackTarget; set => m_Id.callbackTarget = value; }

        /// <summary>
        /// Get/Set <see cref="LowLevelPhysics2D.PhysicsUserData"/> that can be used for any purpose.
        /// The physics system doesn't use this data, it is entirely for custom use.
        /// </summary>
        public readonly PhysicsUserData userData { get => m_Id.userData; set => m_Id.userData = value; }

        /// <summary>
        /// Draw a PhysicsJoint that visually represents its current state in the world.
        /// This is only used in the Unity Editor or in a Development Player.
        /// See <see cref="LowLevelPhysics2D.PhysicsWorld.DrawResults"/>, <see cref="LowLevelPhysics2D.PhysicsWorld.drawOptions"/> and <see cref="LowLevelPhysics2D.PhysicsWorld.drawResults"/>.
        /// </summary>
        public readonly void Draw() => m_Id.Draw();

        #endregion

        /// <summary>
        /// Create a PhysicsIgnoreJoint in the specified world.
        /// </summary>
        /// <param name="world">The world to create the joint in.</param>
        /// <param name="definition">The joint definition to use.</param>
        /// <returns>The created joint.</returns>
        public static PhysicsIgnoreJoint Create(PhysicsWorld world, PhysicsIgnoreJointDefinition definition) => IgnorePhysicsJoint_Create(world, definition);

        /// <summary>
        /// Destroy a batch of joints.
        /// Owned joints will produce a warning and will not be destroyed (see <see cref="LowLevelPhysics2D.PhysicsJoint.SetOwner(Object)"/>).
        /// Any invalid joints will be ignored.
        /// </summary>
        /// <param name="joints">The joints to destroy.</param>
        public static void DestroyBatch(ReadOnlySpan<PhysicsJoint> joints) => PhysicsJoint.DestroyBatch(joints);
    }
}
