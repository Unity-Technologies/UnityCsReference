// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;

using static UnityEngine.LowLevelPhysics2D.PhysicsLowLevelScripting2D;

namespace UnityEngine.LowLevelPhysics2D
{
    /// <summary>
    /// Defines a common joint interface.
    /// This is a helper implementation interface (used for commonality/consistency) and should not be used to access a joint.
    /// </summary>
    interface IPhysicsJoint
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
        int SetOwner(Object owner);

        /// <undoc/>
        Object GetOwner();

        /// <undoc/>
        bool isOwned { get; }

        /// <undoc/>
        System.Object callbackTarget { get; set; }

        /// <undoc/>
        PhysicsUserData userData { get; set; }

        /// <undoc/>
        void Draw();
    }

    /// <summary>
    /// A joint is used to constrain bodies to the world or to each other in various ways.
    /// A joint is automatically destroyed when either body it is attached to is destroyed. A joint cannot exist unattached from a body.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct PhysicsJoint : IPhysicsJoint, IEquatable<PhysicsJoint>
    {
        #region Id

        readonly Int32 index1;
        readonly UInt16 world0;
        readonly UInt16 generation;

        /// <undoc/>
        public override readonly string ToString() => isValid ? $"type={jointType}, index={index1}, world={world0}, generation={generation}" : "<INVALID>";

        #endregion

        #region Equality

        /// <undoc/>
        public override bool Equals(object obj) { return base.Equals(obj); }

        /// <undoc/>
        public bool Equals(PhysicsJoint other) { return index1 == other.index1 && world0 == other.world0 && generation == other.generation; }

        /// <undoc/>
        public static bool operator ==(PhysicsJoint lhs, PhysicsJoint rhs) => lhs.Equals(rhs);

        /// <undoc/>
        public static bool operator !=(PhysicsJoint lhs, PhysicsJoint rhs) => !(lhs == rhs);

        /// <undoc/>
        public override int GetHashCode() { return HashCode.Combine(index1, world0, generation); }

        #endregion

        #region IPhysicsJoint

        /// <summary>
        /// Destroy the joint.
        /// If the object is owned with <see cref="LowLevelPhysics2D.PhysicsJoint.SetOwner(Object)"/> then you must provide the owner key it returned. Failing to do so will return a warning and the joint will not be destroyed.
        /// </summary>
        /// <param name="ownerKey">Optional owner key returned when using <see cref="LowLevelPhysics2D.PhysicsJoint.SetOwner(Object)"/>.</param>
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
        /// See <see cref="LowLevelPhysics2D.PhysicsJoint.JointType"/>.
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
        /// Set the (optional) owner object associated with this joint and return an owner key that must be specified when destroying the joint with <see cref="LowLevelPhysics2D.PhysicsJoint.Destroy(int)"/>.   
        /// The physics system provides access to all objects, including the ability to destroy them so this feature can be used to stop accidental destruction of objects that are owned by other objects.
        /// You can only set the owner once, multiple attempts will produce a warning.
        /// The lifetime of the specified owner object is not linked to this joint i.e. this joint will still be owned by the owner object, even if it is destroyed.
        /// It is also valid to not specify an owner object (NULL) to simply gain an owner key however it can be useful, if simply for debugging purposes and discovery, to know which object is the owner.
        /// </summary>
        /// <param name="owner">The object that owns this joint. This can be NULL if not required.</param>
        /// <returns>An owner key that must be passed to <see cref="LowLevelPhysics2D.PhysicsJoint.Destroy(int)"/> when destroying the joint.</returns>
        public readonly int SetOwner(Object owner) => PhysicsJoint_SetOwner(this, owner);

        /// <summary>
        /// Get the owner object associated with this joint as specified using <see cref="LowLevelPhysics2D.PhysicsJoint.SetOwner(Object)"/>.
        /// </summary>
        /// <returns>The owner object associated with this joint or NULL if no owner has been specified.</returns>
        public readonly Object GetOwner() => PhysicsJoint_GetOwner(this);

        /// <summary>
        /// Get if the joint is owned.
        /// See <see cref="LowLevelPhysics2D.PhysicsJoint.SetOwner(Object)"/>.
        /// </summary>
        public readonly bool isOwned => PhysicsJoint_IsOwned(this);

        /// <summary>
        /// Get/Set the <see cref="System.Object"/> object that event callbacks for this joint will be sent to.
        /// Care should be taken with any <see cref="System.Object"/> assigned as a callback target that isn't a <see cref="UnityEngine.Object"/> as this assignment will not in itself keep the object alive and can be garbage collected.
        /// To avoid this, you should have at least a single reference to the object in your code.
        /// 
        /// This includes the following events:
        /// 
        ///- A <see cref="LowLevelPhysics2D.PhysicsEvents.JointThresholdEvent"/> with call <see cref="LowLevelPhysics2D.PhysicsCallbacks.IJointThresholdCallback"/>.
        /// </summary>
        public readonly System.Object callbackTarget { get => PhysicsJoint_GetCallbackTarget(this); set => PhysicsJoint_SetCallbackTarget(this, value); }

        /// <summary>
        /// Get/Set <see cref="LowLevelPhysics2D.PhysicsUserData"/> that can be used for any purpose.
        /// The physics system doesn't use this data, it is entirely for custom use.
        /// </summary>
        public readonly PhysicsUserData userData { get => PhysicsJoint_GetUserData(this); set => PhysicsJoint_SetUserData(this, value); }

        /// <summary>
        /// Draw a PhysicsJoint that visually represents its current state in the world.
        /// This is only used in the Unity Editor or in a Development Player.
        /// See <see cref="LowLevelPhysics2D.PhysicsWorld.DrawResults"/>, <see cref="LowLevelPhysics2D.PhysicsWorld.drawOptions"/> and <see cref="LowLevelPhysics2D.PhysicsWorld.drawResults"/>.
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
        /// Owned joints will produce a warning and will not be destroyed (see <see cref="LowLevelPhysics2D.PhysicsJoint.SetOwner(Object)"/>).
        /// Any invalid joints will be ignored.
        /// </summary>
        /// <param name="joints">The joints to destroy.</param>
        public static void DestroyBatch(ReadOnlySpan<PhysicsJoint> joints) => PhysicsJoint_DestroyBatch(joints);

        #endregion
    }
}
