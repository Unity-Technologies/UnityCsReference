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
    /// Connects an anchor point on body A with an anchor point on body B via a line segment of a specified distance.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct PhysicsDistanceJoint : IPhysicsJoint, IEquatable<PhysicsDistanceJoint>
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
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] public static implicit operator PhysicsJoint(PhysicsDistanceJoint joint) => joint.m_Id;

        /// <summary>
        /// Cast to a <see cref="LowLevelPhysics2D.PhysicsDistanceJoint"/> from the base <see cref="LowLevelPhysics2D.PhysicsJoint"/>.
        /// The provided joint must be a joint type of <see cref="LowLevelPhysics2D.PhysicsJoint.JointType.DistanceJoint"/>.
        /// </summary>
        /// <param name="physicsJoint">The base joint to cast.</param>
        /// <exception cref="InvalidCastException">Thrown if the joint type is invalid.</exception>
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] public static implicit operator PhysicsDistanceJoint(PhysicsJoint joint) => new(joint);

        /// <summary>
        /// Create a <see cref="LowLevelPhysics2D.PhysicsDistanceJoint"/> from the specified base joint.
        /// The provided joint must be a joint type of <see cref="LowLevelPhysics2D.PhysicsJoint.JointType.DistanceJoint"/>.
        /// </summary>
        /// <param name="physicsJoint">The base joint to cast.</param>
        /// <exception cref="InvalidCastException">Thrown if the joint type is invalid.</exception>
        PhysicsDistanceJoint(PhysicsJoint physicsJoint)
        {
            // Validate.
            if (physicsJoint.jointType != PhysicsJoint.JointType.DistanceJoint)
                throw new InvalidCastException($"The joint must be of type {nameof(PhysicsJoint.JointType.DistanceJoint)} but is of type {physicsJoint.jointType}.");

            m_Id = physicsJoint;
        }

        /// <undoc/>
        public override readonly string ToString() => m_Id.ToString();

        #endregion

        #region Equality

        /// <undoc/>
        public override bool Equals(object obj) => m_Id.Equals(obj);

        /// <undoc/>
        public bool Equals(PhysicsDistanceJoint other) => m_Id.Equals(other);

        /// <undoc/>
        public static bool operator ==(PhysicsDistanceJoint lhs, PhysicsDistanceJoint rhs) => lhs.Equals(rhs);

        /// <undoc/>
        public static bool operator !=(PhysicsDistanceJoint lhs, PhysicsDistanceJoint rhs) => !(lhs == rhs);

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
        /// The local anchor frame constraint relative to bodyA's origin.
        /// </summary>
        public readonly PhysicsTransform localAnchorA { get => m_Id.localAnchorA; set => m_Id.localAnchorA = value; }

        /// <summary>
        /// The local anchor frame constraint relative to bodyB's origin.
        /// </summary>
        public readonly PhysicsTransform localAnchorB { get => m_Id.localAnchorB; set => m_Id.localAnchorB = value; }

        /// <summary>
        /// The force threshold beyond which a joint event will be produced.
        /// </summary>
        public readonly float forceThreshold { get => m_Id.forceThreshold; set => m_Id.forceThreshold = value; }

        /// <summary>
        /// The torque threshold beyond which a joint event will be produced.
        /// </summary>
        public readonly float torqueThreshold { get => m_Id.torqueThreshold; set => m_Id.torqueThreshold = value; }

        /// <summary>
        /// Whether the shapes on the pair of bodies can come into contact.
        /// </summary>
        public readonly bool collideConnected { get => m_Id.collideConnected; set => m_Id.collideConnected = value; }

        /// <summary>
        /// Controls the joint stiffness frequency, in cycles per second.
        /// </summary>
        public readonly float tuningFrequency { get => m_Id.tuningFrequency; set => m_Id.tuningFrequency = value; }

        /// <summary>
        /// Controls the joint stiffness damping, non-dimensional. Use 1 for critical damping.
        /// </summary>
        public readonly float tuningDamping { get => m_Id.tuningDamping; set => m_Id.tuningDamping = value; }

        /// <summary>
        /// Controls the scaling of the joint drawing.
        /// </summary>
        public readonly float drawScale { get => m_Id.drawScale; set => m_Id.drawScale = value; }

        /// <summary>
        /// Wake the pair of bodies the joint is constraining.
        /// </summary>
        public readonly void WakeBodies() => m_Id.WakeBodies();

        /// <summary>
        /// Get the current constraint force used by the joint, usually in newtons.
        /// </summary>
        public readonly Vector2 currentConstraintForce => m_Id.currentConstraintForce;

        /// <summary>
        /// Get the current constraint torque used by the joint, usually in newtons.
        /// </summary>
        public readonly float currentConstraintTorque => m_Id.currentConstraintTorque;

        /// <summary>
        /// Get the current linear separation error for this joint, usually in meters.
        /// This does not consider admissible movement.
        /// </summary>
        public readonly float currentLinearSeparationError => m_Id.currentLinearSeparationError;

        /// <summary>
        /// Get the current angular separation error for this joint, in degrees.
        /// This does not consider admissible movement.
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
        /// Create a <see cref="LowLevelPhysics2D.PhysicsDistanceJoint"/> in the specified world.
        /// </summary>
        /// <param name="world">The world to create the joint in.</param>
        /// <param name="definition">The joint definition to use.</param>
        /// <returns>The created joint.</returns>
        public static PhysicsDistanceJoint Create(PhysicsWorld world, PhysicsDistanceJointDefinition definition) => DistanceJoint_Create(world, definition);

        /// <summary>
        /// Destroy a batch of joints.
        /// Owned joints will produce a warning and will not be destroyed (see <see cref="LowLevelPhysics2D.PhysicsJoint.SetOwner(Object)"/>).
        /// Any invalid joints will be ignored.
        /// </summary>
        /// <param name="joints">The joints to destroy.</param>
        public static void DestroyBatch(ReadOnlySpan<PhysicsJoint> joints) => PhysicsJoint.DestroyBatch(joints);

        /// <summary>
        /// The desired distance constraint i.e. the rest length of this joint.
        /// This has a lower stable limit of just above zero.
        /// </summary>
        public readonly float distance { get => DistanceJoint_GetDistance(this); set => DistanceJoint_SetDistance(this, value); }

        /// <summary>
        /// Get the current distance.
        /// </summary>
        public readonly float currentDistance => DistanceJoint_GetCurrentDistance(this);

        /// <summary>
        /// Enable/Disable the spring behaviour.
        /// If false then the joint will be rigid, overriding the limit and motor.
        /// </summary>
        public readonly bool enableSpring { get => DistanceJoint_GetEnableSpring(this); set => DistanceJoint_SetEnableSpring(this, value); }

        /// <summary>
        /// The spring linear stiffness frequency, in cycles per second.
        /// </summary>
        public readonly float springFrequency { get => DistanceJoint_GetSpringFrequency(this); set => DistanceJoint_SetSpringFrequency(this, value); }

        /// <summary>
        /// The spring linear damping, non-dimensional.
        /// </summary>
        public readonly float springDamping { get => DistanceJoint_GetSpringDamping(this); set => DistanceJoint_SetSpringDamping(this, value); }

        /// <summary>
        /// The lower spring force controls how much tension the spring can sustain.
        /// </summary>
        public readonly float springLowerForce { get => DistanceJoint_GetSpringLowerForce(this); set => DistanceJoint_SetSpringLowerForce(this, value); }

        /// <summary>
        /// The upper spring force controls how much compression the spring can sustain.
        /// </summary>
        public readonly float springUpperForce { get => DistanceJoint_GetSpringUpperForce(this); set => DistanceJoint_SetSpringUpperForce(this, value); }

        /// <summary>
        /// Enable/Disable the joint motor.
        /// </summary>
        public readonly bool enableMotor { get => DistanceJoint_GetEnableMotor(this); set => DistanceJoint_SetEnableMotor(this, value); }

        /// <summary>
        /// The desired motor speed, usually in meters per second.
        /// </summary>
        public readonly float motorSpeed { get => DistanceJoint_GetMotorSpeed(this); set => DistanceJoint_SetMotorSpeed(this, value); }

        /// <summary>
        /// The maximum force the motor can apply, usually in newtons.
        /// </summary>
        public readonly float maxMotorForce { get => DistanceJoint_GetMaxMotorForce(this); set => DistanceJoint_SetMaxMotorForce(this, value); }

        /// <summary>
        /// The current motor force, usually in newtons.
        /// </summary>
        public readonly float currentMotorForce { get => DistanceJoint_GetCurrentMotorForce(this); }

        /// <summary>
        /// Enable/Disable the joint distance limit.
        /// </summary>
        public readonly bool enableLimit { get => DistanceJoint_GetEnableLimit(this); set => DistanceJoint_SetEnableLimit(this, value); }

        /// <summary>
        /// Minimum distance limit of this joint.
        /// This will be clamped to a lower stable limit.
        /// </summary>
        public readonly float minDistanceLimit { get => DistanceJoint_GetMinDistanceLimit(this); set => DistanceJoint_SetMinDistanceLimit(this, value); }

        /// <summary>
        /// Maximum distance limit of this joint.
        /// Must be greater than or equal to the minimum length.
        /// </summary>
        public readonly float maxDistanceLimit { get => DistanceJoint_GetMaxDistanceLimit(this); set => DistanceJoint_SetMaxDistanceLimit(this, value); }
    }
}
