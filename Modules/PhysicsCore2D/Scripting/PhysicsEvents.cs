// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.Scripting.APIUpdating;
using Unity.Collections;
using Object = System.Object;

namespace Unity.U2D.Physics
{
    /// <summary>
    /// Various events that can be retrieved during and after the simulation has completed.
    /// See <see cref="PhysicsWorld.Simulate(float)"/> and <see cref="PhysicsWorld.Simulate(ReadOnlySpan{PhysicsWorld}, float)"/>.
    /// </summary>
    [MovedFrom(autoUpdateAPI: ScriptUpdateConstants.AutoUpdateAPI, sourceNamespace: ScriptUpdateConstants.SourceNamespace, sourceAssembly: ScriptUpdateConstants.SourceAssembly)]
    public readonly partial struct PhysicsEvents
    {
        /// <summary>
        /// An event produced by a <see cref="PhysicsBody"/> that indicates the simulation changed the body in one of the following ways:
        /// 
        ///- The body transform was changed.
        ///- The body fell asleep.
        /// See <see cref="PhysicsWorld.bodyUpdateEvents"/>.
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
        /// The shapes provided may have been destroyed so they should always be validated with <see cref="PhysicsShape.isValid"/>.
        /// See <see cref="PhysicsWorld.triggerBeginEvents"/>.
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
        /// The shapes provided may have been destroyed so they should always be validated with <see cref="PhysicsShape.isValid"/>.
        /// See <see cref="PhysicsWorld.triggerEndEvents"/>.
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
        /// The shapes provided may have been destroyed so they should always be validated with <see cref="PhysicsShape.isValid"/>.
        /// See <see cref="PhysicsWorld.contactBeginEvents"/>.
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
            /// This contact is volatile and may be destroyed automatically when the world is modified or simulated therefore it should always be checked for validity with <see cref="PhysicsShape.ContactId.isValid"/>.
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
        /// The shapes provided may have been destroyed so they should always be validated with <see cref="PhysicsShape.isValid"/>.
        /// See <see cref="PhysicsWorld.contactEndEvents"/>.
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
            /// This contact is volatile and may be destroyed automatically when the world is modified or simulated therefore it should always be checked for validity with <see cref="PhysicsShape.ContactId.isValid"/>.
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
        /// An event produced when a pair of <see cref="PhysicsShape"/> come into contact at relative speed exceeding the <see cref="PhysicsWorld.contactHitEventThreshold"/>.
        ///
        /// The shapes provided may have been destroyed so they should always be validated with <see cref="PhysicsShape.isValid"/>.
        /// This may be reported for speculative contacts that have a confirmed impulse.
        /// See <see cref="PhysicsWorld.contactHitEvents"/>.
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
            /// This contact is volatile and may be destroyed automatically when the world is modified or simulated therefore it should always be checked for validity with <see cref="PhysicsShape.ContactId.isValid"/>.
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
        /// An event produced when a pair of <see cref="PhysicsShape"/> come into contact.
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
            public override readonly string ToString() => $"ContactFilterEvent: physicsWorld={physicsWorld}, shapeA={shapeA}, shapeB={shapeB}";

            #region Internal

            readonly PhysicsWorld m_PhysicsWorld;
            readonly PhysicsShape m_ShapeA;
            readonly PhysicsShape m_ShapeB;

            #endregion
        }

        /// <summary>
        /// An event produced when a contact between a pair of <see cref="PhysicsShape"/> is updated, used to provide the ability to decide if the contact should be disabled or not.
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
            public override readonly string ToString() => $"PreSolveEvent: physicsWorld={physicsWorld}, shapeA={shapeA}, shapeB={shapeB}, point={point}, normal={normal}";

            #region Internal

            readonly PhysicsWorld m_PhysicsWorld;
            readonly PhysicsShape m_ShapeA;
            readonly PhysicsShape m_ShapeB;
            readonly Vector2 m_Point;
            readonly Vector2 m_Normal;

            #endregion
        }

        /// <summary>
        /// An event produced by a Joint which exceeds either its <see cref="PhysicsJoint.forceThreshold"/> or <see cref="PhysicsJoint.torqueThreshold"/>.
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

        /// <summary>
        /// An event produced and sent to the callback target set with <see cref="PhysicsWorld.transformWriteCallbackTarget"/> which must implement <see cref="PhysicsCallbacks.ITransformWriteCallback"/> which will have <see cref="PhysicsCallbacks.ITransformWriteCallback.OnTransformWrite(PhysicsEvents.TransformWriteEvent)"/> called allowing custom transform writing.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public readonly partial struct TransformWriteEvent
        {
            internal TransformWriteEvent(
                PhysicsWorld world,
                PhysicsWorld.SimulationType simulationType,
                PhysicsWorld.TransformPlane transformPlane,
                PhysicsWorld.TransformPlaneCustom transfomPlaneCustom,
                PhysicsWorld.TransformTweenMode transformTweenMode,
                ref NativeArray<PhysicsBody.TransformWriteTween> transformWriteTweens
                )
            {
                m_World = world;
                m_SimulationType = simulationType;
                m_TransformPlane = transformPlane;
                m_TransfomPlaneCustom = transfomPlaneCustom;
                m_TransformTweenMode = transformTweenMode;
                m_TransformWriteTweens = transformWriteTweens;
            }

            /// <summary>
            /// The physics world the event was created from.
            /// </summary>
            public readonly PhysicsWorld physicsWorld => m_World;

            /// <summary>
            /// The simulation type of the physics world when the event was created.
            /// </summary>
            public readonly PhysicsWorld.SimulationType simulationType => m_SimulationType;

            /// <summary>
            /// The transform plane of the physics world when the event was created.
            /// </summary>
            public readonly PhysicsWorld.TransformPlane transformPlane => m_TransformPlane;

            /// <summary>
            /// The transform plane (custom) of the physics world when the event was created.
            /// This maybe not be relevant unless the transform plane is <see cref="PhysicsWorld.TransformPlane.Custom"/>.
            /// </summary>
            public readonly PhysicsWorld.TransformPlaneCustom transformPlaneCustom => m_TransfomPlaneCustom;

            /// <summary>
            /// The transform tween mode of the physics world when the event was created.
            /// </summary>
            public readonly PhysicsWorld.TransformTweenMode transformTweenMode => m_TransformTweenMode;

            /// <summary>
            /// The transform write tweens available to be configured.
            ///
            /// The returned <see cref="NativeArray{T}"/> aliases the per-frame internal buffer owned by the world; it does not own its memory (so disposing it does nothing).
            /// The contents are only valid until the next simulation step runs, after which the buffer may be reused or destroyed.
            /// If a longer-lived copy is required, copy the contents into a caller-owned <see cref="NativeArray{T}"/>.
            /// </summary>
            public readonly NativeArray<PhysicsBody.TransformWriteTween> tweens => m_TransformWriteTweens;

            #region Internal

            readonly PhysicsWorld m_World;
            readonly PhysicsWorld.SimulationType m_SimulationType;
            readonly PhysicsWorld.TransformPlane m_TransformPlane;
            readonly PhysicsWorld.TransformPlaneCustom m_TransfomPlaneCustom;
            readonly PhysicsWorld.TransformTweenMode m_TransformTweenMode;
            readonly NativeArray<PhysicsBody.TransformWriteTween> m_TransformWriteTweens;

            #endregion
        }

        /// <summary>
        /// An event produced and sent to the callback target set with <see cref="PhysicsWorld.transformWriteCallbackTarget"/> which must implement <see cref="PhysicsCallbacks.ITransformWriteCallback"/> which will have <see cref="PhysicsCallbacks.ITransformWriteCallback.OnTransformTweenWrite(TransformTweenWriteEvent)"/> called allowing custom transform writing.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public readonly partial struct TransformTweenWriteEvent
        {
            internal TransformTweenWriteEvent(
                PhysicsWorld world,
                float interpolationTime,
                float extrapolationTime,
                PhysicsWorld.TransformPlane transformPlane,
                PhysicsWorld.TransformPlaneCustom transfomPlaneCustom,
                ref NativeArray<PhysicsBody.TransformWriteTween> transformWriteTweens
                )
            {
                m_World = world;
                m_InterpolationTime = interpolationTime;
                m_ExtrapolationTime = extrapolationTime;
                m_TransformPlane = transformPlane;
                m_TransfomPlaneCustom = transfomPlaneCustom;
                m_TransformWriteTweens = transformWriteTweens;
            }

            /// <summary>
            /// The physics world the event was created from.
            /// </summary>
            public readonly PhysicsWorld physicsWorld => m_World;

            /// <summary>
            /// The interpolation time when the event was created, in the range [0, 1].
            /// </summary>
            public readonly float interpolationTime => m_InterpolationTime;

            /// <summary>
            /// The extrapolation time when the event was created, in the range [0, 1].
            /// </summary>
            public readonly float extrapolationTime => m_ExtrapolationTime;

            /// <summary>
            /// The transform plane of the physics world when the event was created.
            /// </summary>
            public readonly PhysicsWorld.TransformPlane transformPlane => m_TransformPlane;

            /// <summary>
            /// The transform plane (custom) of the physics world when the event was created.
            /// This maybe not be relevant unless the transform plane is <see cref="PhysicsWorld.TransformPlane.Custom"/>.
            /// </summary>
            public readonly PhysicsWorld.TransformPlaneCustom transformPlaneCustom => m_TransfomPlaneCustom;

            /// <summary>
            /// The transform write tweens available to be configured.
            ///
            /// The returned <see cref="NativeArray{T}"/> aliases the per-frame internal buffer owned by the world; it does not own its memory (so disposing it does nothing).
            /// The contents are only valid until the next simulation step runs, after which the buffer may be reused or destroyed.
            /// If a longer-lived copy is required, copy the contents into a caller-owned <see cref="NativeArray{T}"/>.
            /// </summary>
            public readonly NativeArray<PhysicsBody.TransformWriteTween> tweens => m_TransformWriteTweens;

            #region Internal

            readonly PhysicsWorld m_World;
            readonly float m_InterpolationTime;
            readonly float m_ExtrapolationTime;
            readonly PhysicsWorld.TransformPlane m_TransformPlane;
            readonly PhysicsWorld.TransformPlaneCustom m_TransfomPlaneCustom;
            readonly NativeArray<PhysicsBody.TransformWriteTween> m_TransformWriteTweens;

            #endregion
        }

        /// <summary>
        /// An event produced after registering via <see cref="PhysicsWorld.RegisterTransformChange(Transform, PhysicsCallbacks.ITransformChangedCallback)"/>.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public readonly struct TransformChangeEvent
        {
            /// <summary>
            /// The transform that changed.
            /// </summary>
            public readonly Transform transform => Resources.EntityIdToObject(m_TransformId) as Transform;

            /// <summary>
            /// The reason(s) the transform changed.
            /// </summary>
            public readonly PhysicsWorld.TransformChangeReason changeReason => m_ChangeReason;

            #region Internal

            readonly EntityId m_TransformId;
            readonly PhysicsWorld.TransformChangeReason m_ChangeReason;

            #endregion
        }

        #region Pre/Post Simulate Event

        /// <summary>
        /// Event handler for a pre-simulate event callback.
        /// This is called prior to the simulation running and is always called on the main-thread.
        /// See <see cref="PhysicsWorld"/> and <see cref="PhysicsEvents.PreSimulate"/>.
        /// </summary>
        /// <param name="world">The world which is about to be simulated.</param>
        /// <param name="deltaTime">The amount of time the world will be forward simulated.</param>
        public delegate void PreSimulateEventHandler(PhysicsWorld world, float deltaTime);

        /// <summary>
        /// Event handler for a post-simulate event callback.
        /// This is called after the simulation has finished running and is always called on the main-thread.
        /// See <see cref="PhysicsWorld"/> and <see cref="PhysicsEvents.PostSimulate"/>.
        /// </summary>
        /// <param name="world">The world that has just been simulated.</param>
        /// <param name="deltaTime">The amount of time the world was forward simulated.</param>
        public delegate void PostSimulateEventHandler(PhysicsWorld world, float deltaTime);

        /// <summary>
        /// Event callback for a pre-simulate event.
        /// This is called prior to the simulation running and is always called on the main-thread.
        /// See <see cref="PhysicsEvents.PreSimulateEventHandler"/>.
        /// </summary>
        public static event PreSimulateEventHandler PreSimulate { add => s_PreSimulate += value; remove => s_PreSimulate -= value; }
        static event PreSimulateEventHandler s_PreSimulate;

        /// <summary>
        /// Event callback for a post-simulate event.
        /// This is called after the simulation has finished running and is always called on the main-thread.
        /// See <see cref="PhysicsEvents.PostSimulateEventHandler"/>.
        /// </summary>
        public static event PostSimulateEventHandler PostSimulate { add => s_PostSimulate += value; remove => s_PostSimulate -= value; }
        static event PostSimulateEventHandler s_PostSimulate;

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
        static bool SendContactFilterCallback(Object callbackTarget, ContactFilterEvent contactFilterEvent)
        {
            if (callbackTarget is PhysicsCallbacks.IContactFilterCallback target)
                return target.OnContactFilter2D(contactFilterEvent);

            return true;
        }

        /// <undoc/>
        [RequiredByNativeCode]
        static bool SendPreSolveCallback(Object callbackTarget, PreSolveEvent preSolveEvent)
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

        #region World Render Event

        /// <summary>
        /// Event handler for a world draw results event callback.
        /// This is only called if the world is currently rendering as specified by <see cref="PhysicsWorld.renderingMode"/> or if <see cref="PhysicsCoreSettings2D.alwaysDrawWorlds"/> is true.
        /// 
        /// CAUTION: The world is READ locked during this event so ANY write operation on the world will cause an immediate deadlock.
        /// 
        /// </summary>
        /// <param name="world">The world which was drawn.</param>
        /// <param name="drawResults">The draw results for the world. These may be invalid i.e. contain no results. This can be quickly checked with <see cref="PhysicsWorld.DrawResults.isValid"/>.</param>
        public delegate void WorldDrawResultsEventHandler(PhysicsWorld world, ref PhysicsWorld.DrawResults drawResults);

        /// <summary>
        /// Event callback for a world draw results event.
        /// This is only called if the world is currently rendering as specified by <see cref="PhysicsWorld.renderingMode"/> or if <see cref="PhysicsCoreSettings2D.alwaysDrawWorlds"/> is true.
        /// 
        /// CAUTION: The world is READ locked during this event so ANY write operation on the world will cause an immediate deadlock.
        /// 
        /// See <see cref="PhysicsEvents.WorldDrawResultsEventHandler"/>.
        /// </summary>
        public static event WorldDrawResultsEventHandler WorldDrawResults { add => s_WorldDrawResults += value; remove => s_WorldDrawResults -= value; }
        static event WorldDrawResultsEventHandler s_WorldDrawResults;

        /// <undoc/>
        [RequiredByNativeCode]
        internal static void InvokeWorldDrawResultsEvent(PhysicsWorld world, ref PhysicsWorld.DrawResults drawResults)
        {
            try
            {
                // Invoke the event.
                s_WorldDrawResults?.Invoke(world, ref drawResults);
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

        #region World Definition Changed Event

        /// <summary>
        /// Event handler for a world definition change event callback.
        /// </summary>
        /// <param name="world">The world whose definition changed.</param>
        public delegate void WorldDefinitionChangeEventHandler(PhysicsWorld world);

        /// <summary>
        /// Event callback for a world definition change event.
        /// </summary>
        public static event WorldDefinitionChangeEventHandler WorldDefinitionChange { add => s_WorldDefinitionChange += value; remove => s_WorldDefinitionChange -= value; }
        static event WorldDefinitionChangeEventHandler s_WorldDefinitionChange;

        /// <undoc/>
        [RequiredByNativeCode]
        internal static void InvokeWorldDefinitionChangeEvent(PhysicsWorld world)
        {
            try
            {
                // Invoke the event.
                s_WorldDefinitionChange?.Invoke(world);
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

    }
}
