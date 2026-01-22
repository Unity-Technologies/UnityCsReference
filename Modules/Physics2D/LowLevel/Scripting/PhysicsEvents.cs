// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;

using UnityEngine.Scripting;

namespace UnityEngine.LowLevelPhysics2D
{
    /// <summary>
    /// Various events that can be retrieved during and after the simulation has completed.
    /// See <see cref="LowLevelPhysics2D.PhysicsWorld.Simulate(float)"/> and <see cref="LowLevelPhysics2D.PhysicsWorld.Simulate(ReadOnlySpan{PhysicsWorld}, float)"/>.
    /// </summary>
    [RequiredByNativeCode(GenerateProxy = true)]
    public readonly struct PhysicsEvents
    {
        /// <summary>
        /// An event produced by a <see cref="LowLevelPhysics2D.PhysicsBody"/> that indicates the simulation changed the body in one of the following ways:
        /// 
        ///- The body transform was changed.
        ///- The body fell asleep.
        /// See <see cref="LowLevelPhysics2D.PhysicsWorld.bodyUpdateEvents"/>.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public readonly struct BodyUpdateEvent
        {
            /// <summary>
            /// The current transform of the body.
            /// </summary>
            public readonly PhysicsTransform transform => m_Transform;

            /// <summary>
            /// The body this event relates to.
            /// </summary>
            public readonly PhysicsBody body => m_Body;

            /// <summary>
            /// Whether the body fell asleep or not.
            /// </summary>
            public readonly bool fellAsleep => m_FellAsleep;

            /// <undoc/>
            public override readonly string ToString() => $"BodyEvent: transform={transform}, body={body}, fellAsleep={fellAsleep}";

            #region Internal

            readonly IntPtr m_UserData;
            readonly PhysicsTransform m_Transform;
            readonly PhysicsBody m_Body;
            readonly bool m_FellAsleep;

            #endregion
        }

        /// <summary>
        /// An event produced when a pair of Shapes, one of which was a trigger, began touching.
        /// The shapes provided may have been destroyed so they should always be validated with <see cref="LowLevelPhysics2D.PhysicsShape.isValid"/>.
        /// See <see cref="LowLevelPhysics2D.PhysicsWorld.triggerBeginEvents"/>.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public readonly struct TriggerBeginEvent
        {
            /// <summary>
            /// The trigger shape involved in the event.
            /// </summary>
            public readonly PhysicsShape triggerShape => m_TriggerShape;

            /// <summary>
            /// The shape that began touching the trigger shape.
            /// </summary>
            public readonly PhysicsShape visitorShape => m_VisitorShape;

            /// <undoc/>
            public override readonly string ToString() => $"TriggerBeginEvent: triggerShape={triggerShape}, visitorShape={visitorShape}";

            #region Internal

            readonly PhysicsShape m_TriggerShape;
            readonly PhysicsShape m_VisitorShape;

            #endregion
        }

        /// <summary>
        /// An event produced when a pair of Shapes, one of which was a trigger, stopped touching.
        /// An end event will be produced anything that destroys contacts happens, prior to the last world simulation step, which include things like setting the body transform, destroying a body or shape or changing a contact filter etc.
        /// The shapes provided may have been destroyed so they should always be validated with <see cref="LowLevelPhysics2D.PhysicsShape.isValid"/>.
        /// See <see cref="LowLevelPhysics2D.PhysicsWorld.triggerEndEvents"/>.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public readonly struct TriggerEndEvent
        {
            /// <summary>
            /// The trigger shape involved in the event.
            /// </summary>
            public readonly PhysicsShape triggerShape => m_TriggerShape;

            /// <summary>
            /// The shape that stopped touching the trigger shape.
            /// </summary>
            public readonly PhysicsShape visitorShape => m_VisitorShape;

            /// <undoc/>
            public override readonly string ToString() => $"TriggerEndEvent: triggerShape={triggerShape}, visitorShape={visitorShape}";

            #region Internal

            readonly PhysicsShape m_TriggerShape;
            readonly PhysicsShape m_VisitorShape;

            #endregion
        }

        /// <summary>
        /// An event produced by a pair of Shapes, neither of which are a trigger, began touching.
        /// The shapes provided may have been destroyed so they should always be validated with <see cref="LowLevelPhysics2D.PhysicsShape.isValid"/>.
        /// See <see cref="LowLevelPhysics2D.PhysicsWorld.contactBeginEvents"/>.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public readonly struct ContactBeginEvent
        {
            /// <summary>
            /// One of the shapes involved in the event.
            /// </summary>
            public readonly PhysicsShape shapeA => m_ShapeA;

            /// <summary>
            /// The other shape involved in the event.
            /// </summary>
            public readonly PhysicsShape shapeB => m_ShapeB;

            /// <summary>
	        /// The unique Id of the contact.
            /// This contact is volatile and may be destroyed automatically when the world is modified or simulated therefore it should always be checked for validity with <see cref="LowLevelPhysics2D.PhysicsShape.ContactId.isValid"/>.
            /// </summary>
            public readonly PhysicsShape.ContactId contactId => m_ContactId;

            /// <undoc/>
            public override readonly string ToString() => $"ContactBeginEvent: shapeA={shapeA}, shapeB={shapeB}, Id={contactId}";

            #region Internal

            readonly PhysicsShape m_ShapeA;
            readonly PhysicsShape m_ShapeB;
            readonly PhysicsShape.ContactId m_ContactId;

            #endregion
        }

        /// <summary>
        /// An event produced by a pair of Shapes, neither of which are a trigger, stopped touching.
        /// You will get an end event if you do anything that destroys contacts prior to the last world simulation step which include things like setting the body transform, destroying a body etc.
        /// The shapes provided may have been destroyed so they should always be validated with <see cref="LowLevelPhysics2D.PhysicsShape.isValid"/>.
        /// See <see cref="LowLevelPhysics2D.PhysicsWorld.contactEndEvents"/>.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public readonly struct ContactEndEvent
        {
            /// <summary>
            /// One of the shapes involved in the event.
            /// </summary>
            public readonly PhysicsShape shapeA => m_ShapeA;

            /// <summary>
            /// The other shape involved in the event.
            /// </summary>
            public readonly PhysicsShape shapeB => m_ShapeB;

            /// <summary>
	        /// The unique Id of the contact.
            /// This contact is volatile and may be destroyed automatically when the world is modified or simulated therefore it should always be checked for validity with <see cref="LowLevelPhysics2D.PhysicsShape.ContactId.isValid"/>.
            /// </summary>
            public readonly PhysicsShape.ContactId contactId => m_ContactId;

            /// <undoc/>
            public override readonly string ToString() => $"ContactEndEvent: shapeA={shapeA}, shapeB={shapeB}, Id={contactId}";

            #region Internal

            readonly PhysicsShape m_ShapeA;
            readonly PhysicsShape m_ShapeB;
            readonly PhysicsShape.ContactId m_ContactId;

            #endregion
        }

        /// <summary>
        /// An event produced when a pair of <see cref="LowLevelPhysics2D.PhysicsShape"/> come into contact at relative speed exceeding the <see cref="LowLevelPhysics2D.PhysicsWorld.contactHitEventThreshold"/>.
        ///
        /// The shapes provided may have been destroyed so they should always be validated with <see cref="LowLevelPhysics2D.PhysicsShape.isValid"/>.
        /// This may be reported for speculative contacts that have a confirmed impulse.
        /// See <see cref="LowLevelPhysics2D.PhysicsWorld.contactHitEvents"/>.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public readonly struct ContactHitEvent
        {
            /// <summary>
            /// One of the shapes involved in the event.
            /// </summary>
            public readonly PhysicsShape shapeA => m_ShapeA;

            /// <summary>
            /// The other shape involved in the event.
            /// </summary>
            public readonly PhysicsShape shapeB => m_ShapeB;

            /// <summary>
	        /// The unique Id of the contact.
            /// This contact is volatile and may be destroyed automatically when the world is modified or simulated therefore it should always be checked for validity with <see cref="LowLevelPhysics2D.PhysicsShape.ContactId.isValid"/>.
            /// </summary>
            public readonly PhysicsShape.ContactId contactId => m_ContactId;

            /// <summary>
	        /// Point where the shapes hit at the beginning of the time step.
	        /// This is a mid-point between the two surfaces.
            /// It could be at speculative point where the two shapes were not touching at the beginning of the time step.
            /// </summary>
            public readonly Vector2 point => m_Point;

            /// <summary>
            /// Normal vector that always points in the direction from shape A to shape B.
            /// </summary>
            public readonly Vector2 normal => m_Normal;

            /// <summary>
            /// The speed the shapes are approaching, typically in meters per second.
            /// This value is always positive. 
            /// </summary>
            public readonly float approachSpeed => m_ApproachSpeed;

            /// <undoc/>
            public override readonly string ToString() => $"ContactHitEvent: shapeA={shapeA}, shapeB={shapeB}, point={point}, approachSpeed={approachSpeed}";

            #region Internal

            readonly PhysicsShape m_ShapeA;
            readonly PhysicsShape m_ShapeB;
            readonly PhysicsShape.ContactId m_ContactId;
            readonly Vector2 m_Point;
            readonly Vector2 m_Normal;
            readonly float m_ApproachSpeed;

            #endregion
        }

        /// <summary>
        /// An event produced when a pair of <see cref="LowLevelPhysics2D.PhysicsShape"/> come into contact.
        /// This can be used to decide if a contact between the two shapes should be created or not.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public readonly struct ContactFilterEvent
        {
            /// <summary>
            /// The physics world both shapes are within.
            /// </summary>
            public readonly PhysicsWorld physicsWorld => m_PhysicsWorld;

            /// <summary>
            /// One of the shapes involved in the event.
            /// </summary>
            public readonly PhysicsShape shapeA => m_ShapeA;

            /// <summary>
            /// The other shape involved in the event.
            /// </summary>
            public readonly PhysicsShape shapeB => m_ShapeB;

            /// <undoc/>
            public override readonly string ToString() => $"ContactFilterEvent: physicwWorld={physicsWorld}, shapeA={shapeA}, shapeB={shapeB}";

            #region Internal

            readonly PhysicsWorld m_PhysicsWorld;
            readonly PhysicsShape m_ShapeA;
            readonly PhysicsShape m_ShapeB;

            #endregion
        }

        /// <summary>
        /// An event produced when a contact between a pair of <see cref="LowLevelPhysics2D.PhysicsShape"/> is updated, used to provide the ability to decide if the contact should be disabled or not.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public readonly struct PreSolveEvent
        {
            /// <summary>
            /// The physics world both shapes are within.
            /// </summary>
            public readonly PhysicsWorld physicsWorld => m_PhysicsWorld;

            /// <summary>
            /// One of the shapes involved in the event.
            /// </summary>
            public readonly PhysicsShape shapeA => m_ShapeA;

            /// <summary>
            /// The other shape involved in the event.
            /// </summary>
            public readonly PhysicsShape shapeB => m_ShapeB;

            /// <summary>
            /// The point of contact.
            /// </summary>
            public readonly Vector2 point => m_Point;

            /// <summary>
            /// The surface normal at the point of contact.
            /// </summary>
            public readonly Vector2 normal => m_Normal;

            /// <undoc/>
            public override readonly string ToString() => $"PreSolveEvent: physicwWorld={physicsWorld}, shapeA={shapeA}, shapeB={shapeB}, point={point}, normal={normal}";

            #region Internal

            readonly PhysicsWorld m_PhysicsWorld;
            readonly PhysicsShape m_ShapeA;
            readonly PhysicsShape m_ShapeB;
            readonly Vector2 m_Point;
            readonly Vector2 m_Normal;

            #endregion
        }

        /// <summary>
        /// An event produced by a Joint which exceeds either its <see cref="LowLevelPhysics2D.PhysicsJoint.forceThreshold"/> or <see cref="LowLevelPhysics2D.PhysicsJoint.torqueThreshold"/>.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public readonly struct JointThresholdEvent
        {
            /// <summary>
            /// The joint involved in the event.
            /// </summary>
	        public readonly PhysicsJoint joint => m_Joint;

            /// <undoc/>
            public override readonly string ToString() => $"JointEvent: joint={joint}";

            #region Internal

            readonly PhysicsJoint m_Joint;
            readonly IntPtr m_UserData;

            #endregion
        }

        #region Pre/Post Simulate Event

        /// <summary>
        /// Event handler for a pre-simulate event callback.
        /// This is called prior to the simulation running and is always called on the main-thread.
        /// See <see cref="LowLevelPhysics2D.PhysicsWorld"/> and <see cref="LowLevelPhysics2D.PhysicsEvents.PreSimulate"/>.
        /// </summary>
        /// <param name="world">The world which is about to be simulated.</param>
        /// <param name="deltaTime">The amount of time the world will be forward simulated.</param>
        public delegate void PreSimulateEventHandler(PhysicsWorld world, float deltaTime);

        /// <summary>
        /// Event handler for a post-simulate event callback.
        /// This is called after the simulation has finished running and is always called on the main-thread.
        /// See <see cref="LowLevelPhysics2D.PhysicsWorld"/> and <see cref="LowLevelPhysics2D.PhysicsEvents.PostSimulate"/>.
        /// </summary>
        /// <param name="world">The world that has just been simulated.</param>
        /// <param name="deltaTime">The amount of time the world was forward simulated.</param>
        public delegate void PostSimulateEventHandler(PhysicsWorld world, float deltaTime);

        /// <summary>
        /// Event callback for a pre-simulate event.
        /// This is called prior to the simulation running and is always called on the main-thread.
        /// See <see cref="LowLevelPhysics2D.PhysicsEvents.PreSimulateEventHandler"/>.
        /// </summary>
        public static event PreSimulateEventHandler PreSimulate { add => s_PreSimulate += value; remove => s_PreSimulate -= value; }
        static event PreSimulateEventHandler s_PreSimulate;

        /// <summary>
        /// Event callback for a post-simulate event.
        /// This is called after the simulation has finished running and is always called on the main-thread.
        /// See <see cref="LowLevelPhysics2D.PhysicsEvents.PostSimulateEventHandler"/>.
        /// </summary>
        public static event PreSimulateEventHandler PostSimulate { add => s_PostSimulate += value; remove => s_PostSimulate -= value; }
        static event PreSimulateEventHandler s_PostSimulate;

        /// <undoc/>
        [RequiredByNativeCode]
        static void InvokePreSimulate(PhysicsWorld world, float deltaTime)
        {
            try
            {
                // Invoke the event.
                s_PreSimulate?.Invoke(world, deltaTime);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
            finally
            {
            }
        }

        /// <undoc/>
        [RequiredByNativeCode]
        static void InvokePostSimulate(PhysicsWorld world, float deltaTime)
        {
            try
            {
                // Invoke the event.
                s_PostSimulate?.Invoke(world, deltaTime);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
            finally
            {
            }
        }

        #endregion

        #region Contact/PreSolve Callbacks

        /// <undoc/>
        [RequiredByNativeCode]
        static bool SendContactFilterCallback(System.Object callbackTarget, ContactFilterEvent contactFilterEvent)
        {
            if (callbackTarget is PhysicsCallbacks.IContactFilterCallback target)
                return target.OnContactFilter2D(contactFilterEvent);

            return true;
        }

        /// <undoc/>
        [RequiredByNativeCode]
        static bool SendPreSolveCallback(System.Object callbackTarget, PreSolveEvent preSolveEvent)
        {
            if (callbackTarget is PhysicsCallbacks.IPreSolveCallback target)
                return target.OnPreSolve2D(preSolveEvent);

            return true;
        }

        #endregion

        #region Auto Target Callbacks

        /// <undoc/>
        [RequiredByNativeCode]
        static void SendBodyUpdateCallbacks(PhysicsWorld world) => world.SendBodyUpdateCallbacks();

        /// <undoc/>
        [RequiredByNativeCode]
        static void SendContactCallbacks(PhysicsWorld world) => world.SendContactCallbacks();

        /// <undoc/>
        [RequiredByNativeCode]
        static void SendTriggerCallbacks(PhysicsWorld world) => world.SendTriggerCallbacks();

        /// <undoc/>
        [RequiredByNativeCode]
        static void SendJointThresholdCallbacks(PhysicsWorld world) => world.SendJointThresholdCallbacks();

        #endregion
    }
}
