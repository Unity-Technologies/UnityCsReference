// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.U2D.Physics
{
    /// <summary>
    /// Extensions that let a physics handle be written through when it is reached by something other than a plain variable.
    /// </summary>
    /// <remarks>
    /// C# refuses an assignment made through a property or indexer that returns a value type.
    /// That rules out writing a handle's properties at any such site, however the handle itself was obtained.
    /// Each extension here returns the handle from a method instead, which C# does accept as the target of a property assignment.
    /// Every handle is a read-only struct carrying nothing but the handle itself.
    /// The returned copy therefore addresses the same underlying object, and a property written through it takes effect exactly as if it had been written through a variable.
    /// </remarks>
    public static class PhysicsHandleExtensions
    {
        /// <summary>
        /// Returns this world in a form that can be written through.
        /// </summary>
        /// <remarks>
        /// See <see cref="PhysicsHandleExtensions"/> for why this is needed.
        /// Use world.Get().gravity = gravity in place of world.gravity = gravity.
        /// </remarks>
        /// <param name="physicsWorld">The world to return.</param>
        /// <returns>The same world.</returns>
        public static PhysicsWorld Get(this PhysicsWorld physicsWorld) => physicsWorld;

        /// <summary>
        /// Returns this body in a form that can be written through.
        /// </summary>
        /// <remarks>
        /// See <see cref="PhysicsHandleExtensions"/> for why this is needed.
        /// Use body.Get().linearVelocity = velocity in place of body.linearVelocity = velocity.
        /// </remarks>
        /// <param name="physicsBody">The body to return.</param>
        /// <returns>The same body.</returns>
        public static PhysicsBody Get(this PhysicsBody physicsBody) => physicsBody;

        /// <summary>
        /// Returns this shape in a form that can be written through.
        /// </summary>
        /// <remarks>
        /// See <see cref="PhysicsHandleExtensions"/> for why this is needed.
        /// Use shape.Get().property = value in place of shape.property = value.
        /// </remarks>
        /// <param name="physicsShape">The shape to return.</param>
        /// <returns>The same shape.</returns>
        public static PhysicsShape Get(this PhysicsShape physicsShape) => physicsShape;

        /// <summary>
        /// Returns this composer in a form that can be written through.
        /// </summary>
        /// <remarks>
        /// See <see cref="PhysicsHandleExtensions"/> for why this is needed.
        /// Use composer.Get().useDelaunay = useDelaunay in place of composer.useDelaunay = useDelaunay.
        /// </remarks>
        /// <param name="physicsComposer">The composer to return.</param>
        /// <returns>The same composer.</returns>
        public static PhysicsComposer Get(this PhysicsComposer physicsComposer) => physicsComposer;

        /// <summary>
        /// Returns this chain in a form that can be written through.
        /// </summary>
        /// <remarks>
        /// See <see cref="PhysicsHandleExtensions"/> for why this is needed.
        /// Use chain.Get().friction = friction in place of chain.friction = friction.
        /// </remarks>
        /// <param name="physicsChain">The chain to return.</param>
        /// <returns>The same chain.</returns>
        public static PhysicsChain Get(this PhysicsChain physicsChain) => physicsChain;

        /// <summary>
        /// Returns this joint in a form that can be written through.
        /// </summary>
        /// <remarks>
        /// See <see cref="PhysicsHandleExtensions"/> for why this is needed.
        /// Use joint.Get().property = value in place of joint.property = value.
        /// </remarks>
        /// <param name="physicsJoint">The joint to return.</param>
        /// <returns>The same joint.</returns>
        public static PhysicsJoint Get(this PhysicsJoint physicsJoint) => physicsJoint;

        /// <summary>
        /// Returns this distance joint in a form that can be written through.
        /// </summary>
        /// <remarks>
        /// See <see cref="PhysicsHandleExtensions"/> for why this is needed.
        /// Use joint.Get().property = value in place of joint.property = value.
        /// </remarks>
        /// <param name="physicsDistanceJoint">The distance joint to return.</param>
        /// <returns>The same distance joint.</returns>
        public static PhysicsDistanceJoint Get(this PhysicsDistanceJoint physicsDistanceJoint) => physicsDistanceJoint;

        /// <summary>
        /// Returns this fixed joint in a form that can be written through.
        /// </summary>
        /// <remarks>
        /// See <see cref="PhysicsHandleExtensions"/> for why this is needed.
        /// Use joint.Get().property = value in place of joint.property = value.
        /// </remarks>
        /// <param name="physicsFixedJoint">The fixed joint to return.</param>
        /// <returns>The same fixed joint.</returns>
        public static PhysicsFixedJoint Get(this PhysicsFixedJoint physicsFixedJoint) => physicsFixedJoint;

        /// <summary>
        /// Returns this hinge joint in a form that can be written through.
        /// </summary>
        /// <remarks>
        /// See <see cref="PhysicsHandleExtensions"/> for why this is needed.
        /// Use joint.Get().property = value in place of joint.property = value.
        /// </remarks>
        /// <param name="physicsHingeJoint">The hinge joint to return.</param>
        /// <returns>The same hinge joint.</returns>
        public static PhysicsHingeJoint Get(this PhysicsHingeJoint physicsHingeJoint) => physicsHingeJoint;

        /// <summary>
        /// Returns this ignore joint in a form that can be written through.
        /// </summary>
        /// <remarks>
        /// See <see cref="PhysicsHandleExtensions"/> for why this is needed.
        /// Use joint.Get().property = value in place of joint.property = value.
        /// </remarks>
        /// <param name="physicsIgnoreJoint">The ignore joint to return.</param>
        /// <returns>The same ignore joint.</returns>
        public static PhysicsIgnoreJoint Get(this PhysicsIgnoreJoint physicsIgnoreJoint) => physicsIgnoreJoint;

        /// <summary>
        /// Returns this relative joint in a form that can be written through.
        /// </summary>
        /// <remarks>
        /// See <see cref="PhysicsHandleExtensions"/> for why this is needed.
        /// Use joint.Get().property = value in place of joint.property = value.
        /// </remarks>
        /// <param name="physicsRelativeJoint">The relative joint to return.</param>
        /// <returns>The same relative joint.</returns>
        public static PhysicsRelativeJoint Get(this PhysicsRelativeJoint physicsRelativeJoint) => physicsRelativeJoint;

        /// <summary>
        /// Returns this slider joint in a form that can be written through.
        /// </summary>
        /// <remarks>
        /// See <see cref="PhysicsHandleExtensions"/> for why this is needed.
        /// Use joint.Get().property = value in place of joint.property = value.
        /// </remarks>
        /// <param name="physicsSliderJoint">The slider joint to return.</param>
        /// <returns>The same slider joint.</returns>
        public static PhysicsSliderJoint Get(this PhysicsSliderJoint physicsSliderJoint) => physicsSliderJoint;

        /// <summary>
        /// Returns this wheel joint in a form that can be written through.
        /// </summary>
        /// <remarks>
        /// See <see cref="PhysicsHandleExtensions"/> for why this is needed.
        /// Use joint.Get().property = value in place of joint.property = value.
        /// </remarks>
        /// <param name="physicsWheelJoint">The wheel joint to return.</param>
        /// <returns>The same wheel joint.</returns>
        public static PhysicsWheelJoint Get(this PhysicsWheelJoint physicsWheelJoint) => physicsWheelJoint;
    }
}
