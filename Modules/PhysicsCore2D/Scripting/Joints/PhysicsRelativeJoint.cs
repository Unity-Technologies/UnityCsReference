// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using static Unity.U2D.Physics.Scripting2D;

namespace Unity.U2D.Physics
{
    /// <summary>
    /// A joint constraint used to control the relative movement two bodies while still being responsive to collisions.
    /// A spring controls the position and rotation and velocity control allows for simulated friction such as seen in top-down games.
    /// A typical usage is to control the movement of a dynamic body with respect to the ground.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [MovedFrom(autoUpdateAPI: ScriptUpdateConstants.AutoUpdateAPI, sourceNamespace: ScriptUpdateConstants.SourceNamespace, sourceAssembly: ScriptUpdateConstants.SourceAssembly)]
    public readonly struct PhysicsRelativeJoint : IPhysicsJoint<PhysicsRelativeJoint>, IPhysicsHandle<PhysicsRelativeJoint>, IEquatable<PhysicsRelativeJoint>
    {
        #region Handle

        /// <undoc/>
        readonly PhysicsJoint m_JointHandle;

        /// <summary>
        /// Create a joint from a physics handle.
        /// 
        /// NOTE: You must ensure that the physics handle represents the correct object type otherwise hard to detect bugs can occur.
        /// </summary>
        /// <param name="physicsHandle">The physics handle to use.</param>
        public PhysicsRelativeJoint(PhysicsHandle physicsHandle) { m_JointHandle = new PhysicsJoint(physicsHandle); }

        /// <summary>
        /// Get the physics handle.
        /// </summary>
        public readonly PhysicsHandle physicsHandle => m_JointHandle.physicsHandle;

        /// <summary>
        /// Cast to the base <see cref="PhysicsJoint"/>.
        /// </summary>
        /// <param name="joint">The current joint.</param>
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] public static implicit operator PhysicsJoint(PhysicsRelativeJoint joint) => joint.m_JointHandle;

        /// <summary>
        /// Cast to a <see cref="PhysicsRelativeJoint"/> from the base <see cref="PhysicsJoint"/>.
        /// The provided joint must be a joint type of <see cref="PhysicsJoint.JointType.RelativeJoint"/>.
        /// </summary>
        /// <param name="joint">The base joint to cast.</param>
        /// <exception cref="InvalidCastException">Thrown if the joint type is invalid.</exception>
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] public static implicit operator PhysicsRelativeJoint(PhysicsJoint joint) => new(joint);

        /// <summary>
        /// Create a <see cref="PhysicsRelativeJoint"/> from the specified base joint.
        /// The provided joint must be a joint type of <see cref="PhysicsJoint.JointType.RelativeJoint"/>.
        /// </summary>
        /// <param name="physicsJoint">The base joint to cast.</param>
        /// <exception cref="InvalidCastException">Thrown if the joint type is invalid.</exception>
        public PhysicsRelativeJoint(PhysicsJoint physicsJoint)
        {
            // Validate.
            if (physicsJoint.jointType != PhysicsJoint.JointType.RelativeJoint)
                throw new InvalidCastException($"The joint must be of type {nameof(PhysicsJoint.JointType.RelativeJoint)} but is of type {physicsJoint.jointType}.");

            m_JointHandle = physicsJoint;
        }

        /// <undoc/>
        public override readonly string ToString() => m_JointHandle.ToString();

        #endregion

        #region Equality

        /// <undoc/>
        public override bool Equals(object obj) => obj is PhysicsRelativeJoint other && Equals(other);

        /// <undoc/>
        public bool Equals(PhysicsRelativeJoint other) => m_JointHandle.Equals(other.m_JointHandle);

        /// <undoc/>
        public static bool operator ==(PhysicsRelativeJoint lhs, PhysicsRelativeJoint rhs) => lhs.Equals(rhs);

        /// <undoc/>
        public static bool operator !=(PhysicsRelativeJoint lhs, PhysicsRelativeJoint rhs) => !(lhs == rhs);

        /// <undoc/>
        public override int GetHashCode() => m_JointHandle.GetHashCode();

        #endregion

        #region IPhysicsJoint

        /// <summary>
        /// Destroy the joint.
        /// If the object is owned with <see cref="PhysicsJoint.SetOwner(UnityEngine.Object)"/> then you must provide the owner key it returned. Failing to do so will return a warning and the joint will not be destroyed.
        /// </summary>
        /// <param name="ownerKey">The owner key returned when using <see cref="PhysicsJoint.SetOwner(UnityEngine.Object)"/>.</param>
        /// <returns>If the joint was destroyed or not.</returns>
        public readonly bool Destroy(int ownerKey = 0) => m_JointHandle.Destroy(ownerKey);

        /// <summary>
        /// Checks if the joint is valid.
        /// </summary>
        public readonly bool isValid => m_JointHandle.isValid;

        /// <summary>
        /// Get the world the body is attached to.
        /// </summary>
        public readonly PhysicsWorld world => m_JointHandle.world;

        /// <summary>
        /// Gets the joint type.
        /// See <see cref="PhysicsJoint.JointType"/>.
        /// </summary>
        public readonly PhysicsJoint.JointType jointType => m_JointHandle.jointType;

        /// <summary>
        /// The second body the joint constrains.
        /// </summary>
        public readonly PhysicsBody bodyA => m_JointHandle.bodyA;

        /// <summary>
        /// A local anchor point on the first body for the constraint.
        /// </summary>
        public readonly PhysicsBody bodyB => m_JointHandle.bodyB;

        /// <summary>
        /// The local anchor frame constraint relative to bodyA's origin.
        /// </summary>
        public readonly PhysicsTransform localAnchorA { get => m_JointHandle.localAnchorA; set => m_JointHandle.localAnchorA = value; }

        /// <summary>
        /// The local anchor frame constraint relative to bodyB's origin.
        /// </summary>
        public readonly PhysicsTransform localAnchorB { get => m_JointHandle.localAnchorB; set => m_JointHandle.localAnchorB = value; }

        /// <summary>
        /// The force threshold beyond which a joint event will be produced.
        /// </summary>
        public float forceThreshold { get => m_JointHandle.forceThreshold; set => m_JointHandle.forceThreshold = value; }

        /// <summary>
        /// The torque threshold beyond which a joint event will be produced.
        /// </summary>
        public float torqueThreshold { get => m_JointHandle.torqueThreshold; set => m_JointHandle.torqueThreshold = value; }

        /// <summary>
        /// Whether the shapes on the pair of bodies can come into contact.
        /// </summary>
        public readonly bool collideConnected { get => m_JointHandle.collideConnected; set => m_JointHandle.collideConnected = value; }

        /// <summary>
        /// Controls the joint stiffness frequency, in cycles per second.
        /// </summary>
        public readonly float tuningFrequency { get => m_JointHandle.tuningFrequency; set => m_JointHandle.tuningFrequency = value; }

        /// <summary>
        /// Controls the joint stiffness damping, non-dimensional.
        /// Use 1 for critical damping.
        /// </summary>
        public readonly float tuningDamping { get => m_JointHandle.tuningDamping; set => m_JointHandle.tuningDamping = value; }

        /// <summary>
        /// Controls the scaling of the joint drawing.
        /// </summary>
        public readonly float drawScale { get => m_JointHandle.drawScale; set => m_JointHandle.drawScale = value; }

        /// <summary>
        /// Wake the pair of bodies the joint is constraining.
        /// </summary>
        public readonly void WakeBodies() => m_JointHandle.WakeBodies();

        /// <summary>
        /// Get the current constraint force used by the joint, usually in newtons.
        /// </summary>
        public readonly Vector2 currentConstraintForce => m_JointHandle.currentConstraintForce;

        /// <summary>
        /// Get the current constraint torque used by the joint, usually in newtons.
        /// </summary>
        public readonly float currentConstraintTorque => m_JointHandle.currentConstraintTorque;

        /// <summary>
        /// Get the current linear separation error for this joint, usually in meters.
        /// This does not consider admissible movement.
        /// </summary>
        public readonly float currentLinearSeparationError => m_JointHandle.currentLinearSeparationError;

        /// <summary>
        /// Get the current angular separation error for this joint, in degrees.
        /// This does not consider admissible movement.
        /// </summary>
        public readonly float currentAngularSeparationError => m_JointHandle.currentAngularSeparationError;

        /// <summary>
        /// Set the (optional) owner object associated with this joint and return an owner key that must be specified when destroying the joint with <see cref="PhysicsJoint.Destroy(int)"/>.   
        /// The physics system provides access to all objects, including the ability to destroy them so this feature can be used to stop accidental destruction of objects that are owned by other objects.
        /// You can only set the owner once, multiple attempts will produce a warning.
        /// The lifetime of the specified owner object is not linked to this joint i.e. this joint will still be owned by the owner object, even if it is destroyed.
        /// It is also valid to not specify an owner object (NULL) to simply gain an owner key however it can be useful, if simply for debugging purposes and discovery, to know which object is the owner.
        /// </summary>
        /// <param name="owner">The object that owns this joint. This can be NULL if not required.</param>
        /// <returns>An owner key that must be passed to <see cref="PhysicsJoint.Destroy(int)"/> when destroying the joint.</returns>
        public readonly int SetOwner(UnityEngine.Object owner) => m_JointHandle.SetOwner(owner);

        /// <summary>
        /// Set the owner object using the specified owner key.
        /// You can only set the owner once, multiple attempts will produce a warning.
        /// This call does not bind the lifetime of the specified owner object, it is simply a reference.
        /// It is also valid to not specify an owner object (NULL) to simply gain an owner key however it can be useful, if simply for debugging purposes and discovery, to know which object is the owner.
        /// </summary>
        /// <param name="owner">The object that owns this key. This can be NULL if not required but is recommended as the key is formed in part by the hash-code of the owner object.</param>
        /// <param name="ownerKey">The owner key to be used. If zero then a new owner key is created. You can use <see cref="PhysicsWorld.CreateOwnerKey(UnityEngine.Object)"/> for this value although any non-zero integer will work.</param>
        public readonly void SetOwner(UnityEngine.Object owner, int ownerKey) => m_JointHandle.SetOwner(owner, ownerKey);

        /// <summary>
        /// Get the owner object associated with this joint as specified using <see cref="PhysicsJoint.SetOwner(UnityEngine.Object)"/>.
        /// </summary>
        /// <returns>The owner object associated with this joint or NULL if no owner has been specified.</returns>
        public readonly UnityEngine.Object GetOwner() => m_JointHandle.GetOwner();

        /// <summary>
        /// Get if the joint is owned.
        /// See <see cref="PhysicsJoint.SetOwner(UnityEngine.Object)"/>.
        /// </summary>
        public readonly bool isOwned => m_JointHandle.isOwned;

        /// <summary>
        /// Get/Set the <see cref="System.Object"/> that event callbacks for this joint will be sent to.
        /// Care should be taken with any <see cref="System.Object"/> assigned as a callback target that isn't a <see cref="UnityEngine.Object"/> as this assignment will not in itself keep the object alive and can be garbage collected.
        /// To avoid this, you should have at least a single reference to the object in your code.
        /// To remove the object assigned here, set the callback target to NULL.
        /// 
        /// This includes the following events:
        /// 
        ///- A <see cref="PhysicsEvents.JointThresholdEvent"/> with call <see cref="PhysicsCallbacks.IJointThresholdCallback"/>.
        /// </summary>
        public readonly System.Object callbackTarget { get => m_JointHandle.callbackTarget; set => m_JointHandle.callbackTarget = value; }

        /// <summary>
        /// Get/Set <see cref="PhysicsUserData"/> that can be used for any purpose.
        /// The physics system doesn't use this data, it is entirely for custom use.
        /// </summary>
        public readonly PhysicsUserData userData { get => m_JointHandle.userData; set => m_JointHandle.userData = value; }

        /// <summary>
        /// Get <see cref="PhysicsUserData"/> that can be used for any purpose, typically by the owner only.
        /// </summary>
        public readonly PhysicsUserData ownerUserData { get => m_JointHandle.ownerUserData; }

        /// <summary>
        /// Set <see cref="PhysicsUserData"/> that can be used for any purpose, typically by the owner only.
        /// </summary>
        /// <param name="physicsUserData">The user data to set.</param>
        /// <param name="ownerKey">Optional owner key returned when using <see cref="PhysicsJoint.SetOwner(UnityEngine.Object)"/>.</param>
        public readonly void SetOwnerUserData(PhysicsUserData physicsUserData, int ownerKey = 0) => m_JointHandle.SetOwnerUserData(physicsUserData, ownerKey);

        /// <summary>
        /// Controls whether this joint is automatically drawn when the world is drawn.
        /// </summary>
        public readonly bool worldDrawing { get => m_JointHandle.worldDrawing; set => m_JointHandle.worldDrawing = value; }

        /// <summary>
        /// Draw a PhysicsJoint that visually represents its current state in the world.
        /// </summary>
        public readonly void Draw() => m_JointHandle.Draw();

        #endregion

        /// <summary>
        /// Create a PhysicsRelativeJoint in the specified world.
        /// </summary>
        /// <param name="world">The world to create the joint in.</param>
        /// <param name="definition">The joint definition to use.</param>
        /// <returns>The created joint.</returns>
        public static PhysicsRelativeJoint Create(PhysicsWorld world, PhysicsRelativeJointDefinition definition) => RelativeJoint_Create(world, definition);

        /// <summary>
        /// Destroy a batch of joints.
        /// Owned joints will produce a warning and will not be destroyed (see <see cref="PhysicsJoint.SetOwner(UnityEngine.Object)"/>).
        /// Any invalid joints will be ignored.
        /// </summary>
        /// <param name="joints">The joints to destroy.</param>
        public static void DestroyBatch(ReadOnlySpan<PhysicsJoint> joints) => PhysicsJoint.DestroyBatch(joints);

        /// <summary>
	    /// The desired linear velocity.
        /// </summary>
        public readonly Vector2 linearVelocity { get => RelativeJoint_GetLinearVelocity(this); set => RelativeJoint_SetLinearVelocity(this, value); }

        /// <summary>
	    /// The desired angular velocity.
        /// </summary>
        public readonly float angularVelocity { get => RelativeJoint_GetAngularVelocity(this); set => RelativeJoint_SetAngularVelocity(this, value); }

        /// <summary>
	    /// The maximum linear force, usually in newtons.
        /// A value of zero is a special case which turns the limit off.
        /// </summary>
        public readonly float maxForce { get => RelativeJoint_GetMaxForce(this); set => RelativeJoint_SetMaxForce(this, value); }

        /// <summary>
	    /// The maximum torque, usually in newton-meters.
        /// A value of zero is a special case which turns the limit off.
        /// </summary>
        public readonly float maxTorque { get => RelativeJoint_GetMaxTorque(this); set => RelativeJoint_SetMaxTorque(this, value); }

        /// <summary>
	    /// The spring linear frequency, in cycles per second.
        /// A value of zero is a special case which turns the linear spring off.
        /// </summary>
        public readonly float springLinearFrequency { get => RelativeJoint_GetSpringLinearFrequency(this); set => RelativeJoint_SetSpringLinearFrequency(this, value); }

        /// <summary>
	    /// The spring angular frequency, in cycles per second.
        /// A value of zero is a special case which turns the angular spring off.
        /// </summary>
        public readonly float springAngularFrequency { get => RelativeJoint_GetSpringAngularFrequency(this); set => RelativeJoint_SetSpringAngularFrequency(this, value); }

        /// <summary>
	    /// The spring linear damping.
        /// </summary>
        public readonly float springLinearDamping { get => RelativeJoint_GetSpringLinearDamping(this); set => RelativeJoint_SetSpringLinearDamping(this, value); }

        /// <summary>
	    /// The spring angular damping.
        /// </summary>
        public readonly float springAngularDamping { get => RelativeJoint_GetSpringAngularDamping(this); set => RelativeJoint_SetSpringAngularDamping(this, value); }

        /// <summary>
	    /// The spring maximum linear force, usually in newtons.
        /// A value of zero is a special case which turns the force limit off.
        /// </summary>
        public readonly float springMaxForce { get => RelativeJoint_GetSpringMaxForce(this); set => RelativeJoint_SetSpringMaxForce(this, value); }

        /// <summary>
	    /// The spring maximum torque, usually in newton-meters.
        /// A value of zero is a special case which turns the torque limit off.
        /// </summary>
        public readonly float springMaxTorque { get => RelativeJoint_GetSpringMaxTorque(this); set => RelativeJoint_SetSpringMaxTorque(this, value); }
    }
}
