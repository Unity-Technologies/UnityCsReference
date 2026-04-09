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
    /// A joint where an anchor point on body B is fixed to an anchor point on body A.
    /// This joint allows relative rotation.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [MovedFrom(autoUpdateAPI: ScriptUpdateConstants.AutoUpdateAPI, sourceNamespace: ScriptUpdateConstants.SourceNamespace, sourceAssembly: ScriptUpdateConstants.SourceAssembly)]
    public readonly struct PhysicsHingeJoint : IPhysicsJoint, IEquatable<PhysicsHingeJoint>
    {
        #region Id

        /// <summary>
        /// The base joint Id.
        /// </summary>
        readonly PhysicsJoint m_Id;

        /// <summary>
        /// Cast to the base <see cref="PhysicsJoint"/>.
        /// </summary>
        /// <param name="joint">The current joint.</param>
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] public static implicit operator PhysicsJoint(PhysicsHingeJoint joint) => joint.m_Id;

        /// <summary>
        /// Cast to a <see cref="PhysicsHingeJoint"/> from the base <see cref="PhysicsJoint"/>.
        /// The provided joint must be a joint type of <see cref="PhysicsJoint.JointType.HingeJoint"/>.
        /// </summary>
        /// <param name="joint">The base joint to cast.</param>
        /// <exception cref="InvalidCastException">Thrown if the joint type is invalid.</exception>
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] public static implicit operator PhysicsHingeJoint(PhysicsJoint joint) => new(joint);

        /// <summary>
        /// Create a <see cref="PhysicsHingeJoint"/> from the specified base joint.
        /// The provided joint must be a joint type of <see cref="PhysicsJoint.JointType.HingeJoint"/>.
        /// </summary>
        /// <param name="physicsJoint">The base joint to cast.</param>
        /// <exception cref="InvalidCastException">Thrown if the joint type is invalid.</exception>
        public PhysicsHingeJoint(PhysicsJoint physicsJoint)
        {
            // Validate.
            if (physicsJoint.jointType != PhysicsJoint.JointType.HingeJoint)
                throw new InvalidCastException($"The joint must be of type {nameof(PhysicsJoint.JointType.HingeJoint)} but is of type {physicsJoint.jointType}.");

            m_Id = physicsJoint;
        }

        /// <undoc/>
        public override readonly string ToString() => m_Id.ToString();

        #endregion

        #region Equality

        /// <undoc/>
        public override bool Equals(object obj) => m_Id.Equals(obj);

        /// <undoc/>
        public bool Equals(PhysicsHingeJoint other) => m_Id.Equals(other);

        /// <undoc/>
        public static bool operator ==(PhysicsHingeJoint lhs, PhysicsHingeJoint rhs) => lhs.Equals(rhs);

        /// <undoc/>
        public static bool operator !=(PhysicsHingeJoint lhs, PhysicsHingeJoint rhs) => !(lhs == rhs);

        /// <undoc/>
        public override int GetHashCode() => m_Id.GetHashCode();

        #endregion

        #region IPhysicsJoint

        /// <summary>
        /// Destroy the joint.
        /// If the object is owned with <see cref="PhysicsJoint.SetOwner(UnityEngine.Object)"/> then you must provide the owner key it returned. Failing to do so will return a warning and the joint will not be destroyed.
        /// </summary>
        /// <param name="ownerKey">Optional owner key returned when using <see cref="PhysicsJoint.SetOwner(UnityEngine.Object)"/>.</param>
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
        /// See <see cref="PhysicsJoint.JointType"/>.
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
        /// Set the (optional) owner object associated with this joint and return an owner key that must be specified when destroying the joint with <see cref="PhysicsJoint.Destroy(int)"/>.   
        /// The physics system provides access to all objects, including the ability to destroy them so this feature can be used to stop accidental destruction of objects that are owned by other objects.
        /// You can only set the owner once, multiple attempts will produce a warning.
        /// The lifetime of the specified owner object is not linked to this joint i.e. this joint will still be owned by the owner object, even if it is destroyed.
        /// It is also valid to not specify an owner object (NULL) to simply gain an owner key however it can be useful, if simply for debugging purposes and discovery, to know which object is the owner.
        /// </summary>
        /// <param name="owner">The object that owns this joint. This can be NULL if not required.</param>
        /// <returns>An owner key that must be passed to <see cref="PhysicsJoint.Destroy(int)"/> when destroying the joint.</returns>
        public readonly int SetOwner(UnityEngine.Object owner) => m_Id.SetOwner(owner);

        /// <summary>
        /// Set the owner object using the specified owner key.
        /// You can only set the owner once, multiple attempts will produce a warning.
        /// This call does not bind the lifetime of the specified owner object, it is simply a reference.
        /// It is also valid to not specify an owner object (NULL) to simply gain an owner key however it can be useful, if simply for debugging purposes and discovery, to know which object is the owner.
        /// </summary>
        /// <param name="owner">The object that owns this key. This can be NULL if not required but is recommended as the key is formed in part by the hash-code of the owner object.</param>
        /// <param name="ownerKey">The owner key to be used. If zero then a new owner key is created. You can use <see cref="PhysicsWorld.CreateOwnerKey(UnityEngine.Object)"/> for this value although any non-zero integer will work.</param>
        /// <returns>The owner key assigned.</returns>
        public readonly void SetOwner(UnityEngine.Object owner, int ownerKey) => m_Id.SetOwner(owner, ownerKey);

        /// <summary>
        /// Get the owner object associated with this joint as specified using <see cref="PhysicsJoint.SetOwner(UnityEngine.Object)"/>.
        /// </summary>
        /// <returns>The owner object associated with this joint or NULL if no owner has been specified.</returns>
        public readonly UnityEngine.Object GetOwner() => m_Id.GetOwner();

        /// <summary>
        /// Get if the joint is owned.
        /// See <see cref="PhysicsJoint.SetOwner(UnityEngine.Object)"/>.
        /// </summary>
        public readonly bool isOwned => m_Id.isOwned;

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
        public readonly System.Object callbackTarget { get => m_Id.callbackTarget; set => m_Id.callbackTarget = value; }

        /// <summary>
        /// Get/Set <see cref="PhysicsUserData"/> that can be used for any purpose.
        /// The physics system doesn't use this data, it is entirely for custom use.
        /// </summary>
        public readonly PhysicsUserData userData { get => m_Id.userData; set => m_Id.userData = value; }

        /// <summary>
        /// Get <see cref="PhysicsUserData"/> that can be used for any purpose, typically by the owner only.
        /// </summary>
        public readonly PhysicsUserData ownerUserData { get => m_Id.ownerUserData; }

        /// <summary>
        /// Set <see cref="PhysicsUserData"/> that can be used for any purpose, typically by the owner only.
        /// </summary>
        /// <param name="physicsUserData">The user data to set.</param>
        /// <param name="ownerKey">Optional owner key returned when using <see cref="PhysicsJoint.SetOwner(UnityEngine.Object)"/>.</param>
        public readonly void SetOwnerUserData(PhysicsUserData physicsUserData, int ownerKey = 0) => m_Id.SetOwnerUserData(physicsUserData, ownerKey);

        /// <summary>
        /// Controls whether this joint is automatically drawn when the world is drawn.
        /// </summary>
        public readonly bool worldDrawing { get => m_Id.worldDrawing; set => m_Id.worldDrawing = value; }

        /// <summary>
        /// Draw a PhysicsJoint that visually represents its current state in the world.
        /// </summary>
        public readonly void Draw() => m_Id.Draw();

        #endregion

        /// <summary>
        /// Create a PhysicsHingeJoint in the specified world.
        /// </summary>
        /// <param name="world">The world to create the joint in.</param>
        /// <param name="definition">The joint definition to use.</param>
        /// <returns>The created joint.</returns>
        public static PhysicsHingeJoint Create(PhysicsWorld world, PhysicsHingeJointDefinition definition) => HingeJoint_Create(world, definition);

        /// <summary>
        /// Destroy a batch of joints.
        /// Owned joints will produce a warning and will not be destroyed (see <see cref="PhysicsJoint.SetOwner(UnityEngine.Object)"/>).
        /// Any invalid joints will be ignored.
        /// </summary>
        /// <param name="joints">The joints to destroy.</param>
        public static void DestroyBatch(ReadOnlySpan<PhysicsJoint> joints) => PhysicsJoint.DestroyBatch(joints);

        /// <summary>
        /// Enable/Disable unpinned mode where only Body A is affected and body B and its local anchor point is ignored.
        /// Body B must still be assigned so it is typical to assign a static ground body, preferably shared/reused.
        /// 
        /// </summary>
        public readonly bool enableUnpinned { get => HingeJoint_GetEnableUnpinned(this); set => HingeJoint_SetEnableUnpinned(this, value); }

        /// <summary>
        /// Enable/Disable the rotational spring.
        /// </summary>
        public readonly bool enableSpring { get => HingeJoint_GetEnableSpring(this); set => HingeJoint_SetEnableSpring(this, value); }

        /// <summary>
        /// The spring stiffness, in cycles per second.
        /// </summary>
        public readonly float springFrequency { get => HingeJoint_GetSpringFrequency(this); set => HingeJoint_SetSpringFrequency(this, value); }

        /// <summary>
        /// The spring damping, non-dimensional.
        /// </summary>
        public readonly float springDamping { get => HingeJoint_GetSpringDamping(this); set => HingeJoint_SetSpringDamping(this, value); }

        /// <summary>
        /// The spring target angle, in degrees.
        /// </summary>
        public readonly float springTargetAngle { get => HingeJoint_GetSpringTargetAngle(this); set => HingeJoint_SetSpringTargetAngle(this, value); }

        /// <summary>
        /// Get the current angle of the joint, in degrees.
        /// </summary>
        public readonly float angle { get => HingeJoint_GetAngle(this); }

        /// <summary>
        /// Enable/Disable the joint motor.
        /// </summary>
        public readonly bool enableMotor { get => HingeJoint_GetEnableMotor(this); set => HingeJoint_SetEnableMotor(this, value); }

        /// <summary>
        /// The desired motor speed, usually in degrees per second.
        /// </summary>
        public readonly float motorSpeed { get => HingeJoint_GetMotorSpeed(this); set => HingeJoint_SetMotorSpeed(this, value); }

        /// <summary>
        /// The maximum torque the motor can apply, usually in newton-meters.
        /// </summary>
        public readonly float maxMotorTorque { get => HingeJoint_GetMaxMotorTorque(this); set => HingeJoint_SetMaxMotorTorque(this, value); }

        /// <summary>
        /// Get the current motor torque.
        /// </summary>
        public readonly float currentMotorTorque { get => HingeJoint_GetCurrentMotorTorque(this); }

        /// <summary>
        /// Enable/Disable the joint rotation limit.
        /// </summary>
        public readonly bool enableLimit { get => HingeJoint_GetEnableLimit(this); set => HingeJoint_SetEnableLimit(this, value); }

        /// <summary>
        /// Get the lower angle limit, in degrees.
        /// </summary>
        public readonly float lowerAngleLimit { get => HingeJoint_GetLowerLimit(this); set => HingeJoint_SetLowerLimit(this, value); }

        /// <summary>
        /// Get the upper angle limit, in degrees.
        /// </summary>
        public readonly float upperAngleLimit { get => HingeJoint_GetUpperLimit(this); set => HingeJoint_SetUpperLimit(this, value); }
    }
}
