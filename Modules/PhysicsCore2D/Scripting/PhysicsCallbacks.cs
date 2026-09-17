// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Scripting.APIUpdating;
using static Unity.U2D.Physics.Scripting2D;

namespace Unity.U2D.Physics
{
    /// <summary>
    /// All callback interfaces and targets.
    /// </summary>
    [MovedFrom(autoUpdateAPI: ScriptUpdateConstants.AutoUpdateAPI, sourceNamespace: ScriptUpdateConstants.SourceNamespace, sourceAssembly: ScriptUpdateConstants.SourceAssembly)]
    public readonly partial record struct PhysicsCallbacks
    {
        #region Interfaces

        /// <summary>
        /// An interface that when implemented, can be called as a target by <see cref="PhysicsWorld.SendBodyUpdateCallbacks"/>.
        /// </summary>
        public interface IBodyUpdateCallback
        {
            /// <summary>
            /// Called when a <see cref="PhysicsEvents.BodyUpdateEvent"/> for the object occurs.
            /// This will always be called on the main-thread after the simulation has finished.
            /// </summary>
            /// <param name="bodyUpdateEvent">The event that occurred.</param>
            void OnBodyUpdate2D(PhysicsEvents.BodyUpdateEvent bodyUpdateEvent);
        }

        /// <summary>
        /// An interface that when implemented, can be called as a target when a <see cref="PhysicsShape"/> has <see cref="PhysicsShape.contactFilterCallbacks"/> set to true.
        /// The <see cref="PhysicsWorld"/> the <see cref="PhysicsShape"/> is in also has to have its <see cref="PhysicsWorld.contactFilterCallbacks"/> set to true.
        /// </summary>
        public interface IContactFilterCallback
        {
            /// <summary>
            /// Called when a pair of shapes are determined to be in contact.
            /// This is called to decide if a contact will be created for these shapes, allowing contact creation to be bypassed so a contact will not go to the solver.
            /// This is only called if the <see cref="PhysicsWorld"/> has <see cref="PhysicsWorld.contactFilterCallbacks"/> set to true.
            /// An event is only produced if one of the <see cref="PhysicsShape"/> have <see cref="PhysicsShape.contactFilterCallbacks"/> set to true.
            /// This is called for both triggers and non-triggers but only with Dynamic bodies.
            ///
            /// Extreme care must be taken with this callback!!
            /// 
            /// This callback occurs during the simulation step and can be called from any thread, therefore it must be thread-safe.
            /// During this time, the simulation state is undefined for the broadphase, events etc.
            ///	For this reason, any attempt to perform a write operation will result in a deadlock as the world itself is write locked.
            ///	Performing simple read operations on <see cref="PhysicsBody"/>, <see cref="PhysicsShape"/> or <see cref="PhysicsJoint"/> is safe, such as reading velocity or getting the geometry of a shape however, more complex operations involving the world such as performing a query can result in corruption or crashes.
            ///	A recommendation is reading <see cref="PhysicsUserData"/> from any object which is a completely safe read operation therefore any required information should be encoded there if possible.
            /// </summary>
            /// <param name="contactFilterEvent">The event that occurred.</param>
            /// <returns>Return false if you do not want a contact to be created during this simulation step. Returning true allows the contact to be created.</returns>
            bool OnContactFilter2D(PhysicsEvents.ContactFilterEvent contactFilterEvent);
        }

        /// <summary>
        /// An interface that when implemented, is called as a contact between a pair of shapes is updated, before the contact is solved.
        /// The callback receives the full contact manifold and may modify it, or cancel the contact by calling Cancel on the event.
        /// </summary>
        /// <remarks>
        /// This is only called if the <see cref="PhysicsWorld"/> and one of the <see cref="PhysicsShape"/> both have their <c>preContactCallbacks</c> set to true.
        /// It takes priority over the deprecated <see cref="IPreSolveCallback"/>, which is only used when this interface is not implemented on the same target.
        /// </remarks>
        public interface IPreContactCallback
        {
            /// <summary>
            /// Called when a contact between a pair of shapes is updated, giving the opportunity to modify the contact manifold before it is solved.
            /// Any change written to the manifold is used by the solver this step, and calling Cancel on the event cancels the contact.
            /// Modifying the world-space contact points or the point identifiers has no effect, as the solver does not read them.
            /// </summary>
            /// <remarks>
            /// This is only called for Awake Dynamic bodies and is never called for triggers.
            ///
            /// Extreme care must be taken with this callback.
            ///
            /// It occurs during the simulation step and can be called from any thread, so it must be thread-safe.
            /// During this time the simulation state is undefined for the broadphase, events and so on.
            /// Any attempt to perform a write operation on the world results in a deadlock, as the world itself is write locked.
            /// Simple read operations on <see cref="PhysicsBody"/>, <see cref="PhysicsShape"/> or <see cref="PhysicsJoint"/> are safe, such as reading velocity or getting the geometry of a shape, however more complex operations involving the world such as performing a query can result in corruption or crashes.
            /// Reading <see cref="PhysicsUserData"/> from any object is a completely safe read operation, so any required information should be encoded there if possible.
            ///
            /// If the manifold is left in an invalid state, such as a value that is not a number or an out-of-range point count, the change is discarded and a warning is produced after the step.
            /// </remarks>
            /// <param name="preContactEvent">The event that occurred, passed by reference so the manifold can be modified.</param>
            void OnPreContact2D(ref PhysicsEvents.PreContactEvent preContactEvent);
        }

        /// <summary>
        /// An interface that when implemented, is called during the continuous collision stage when a fast-moving shape is about to be stopped short of a shape it would otherwise pass through.
        /// Returning false lets the shape continue rather than being stopped at the predicted impact point.
        /// </summary>
        /// <remarks>
        /// This is only called if the <see cref="PhysicsWorld"/> and one of the <see cref="PhysicsShape"/> both have their <c>preContactCallbacks</c> set to true.
        /// It takes priority over the deprecated <see cref="IPreSolveCallback"/>, which is only used when this interface is not implemented on the same target.
        /// </remarks>
        public interface IPreContinuousCallback
        {
            /// <summary>
            /// Called during the continuous collision stage when a fast-moving shape is predicted to hit another shape this step.
            /// No contact exists yet, as the shapes are not touching during the contact update, so this is the only place the pair is seen.
            /// Returning true stops the shape at the predicted impact point, and a contact then appears on the next step once the shapes are found touching.
            /// </summary>
            /// <remarks>
            /// This is only called for Awake Dynamic bodies and is never called for triggers.
            ///
            /// Extreme care must be taken with this callback.
            ///
            /// It occurs during the simulation step and can be called from any thread, so it must be thread-safe.
            /// During this time the simulation state is undefined for the broadphase, events and so on.
            /// Any attempt to perform a write operation on the world results in a deadlock, as the world itself is write locked.
            /// Simple read operations on <see cref="PhysicsBody"/>, <see cref="PhysicsShape"/> or <see cref="PhysicsJoint"/> are safe, such as reading velocity or getting the geometry of a shape, however more complex operations involving the world such as performing a query can result in corruption or crashes.
            /// Reading <see cref="PhysicsUserData"/> from any object is a completely safe read operation, so any required information should be encoded there if possible.
            /// </remarks>
            /// <param name="preContinuousEvent">The event that occurred.</param>
            /// <returns>Return false to let the shape continue through, or true to stop it at the predicted impact point.</returns>
            bool OnPreContinuous2D(PhysicsEvents.PreContinuousEvent preContinuousEvent);
        }

        /// <summary>
        /// An interface that when implemented, can be called as a target by <see cref="PhysicsWorld.SendTriggerCallbacks"/>.
        /// </summary>
        /// <remarks>
        /// A trigger shape doesn't collide with other shapes. Other shapes pass through it instead.
        /// Unity calls `OnTriggerBegin2D` and `OnTriggerEnd2D` on the main thread after the simulation finishes.
        /// For a worked example, refer to the `PhysicsShapeTriggerCallback` example in the <a href="https://github.com/Unity-Technologies/PhysicsExamples2D/tree/master">Physics Core 2D examples repository</a> on GitHub.
        /// </remarks>
        /// <example>
        /// <code lang="cs">
        /// <![CDATA[
        /// using UnityEngine;
        /// using Unity.U2D.Physics;
        ///
        /// // Add the PhysicsCallbacks.ITriggerCallback interface.
        /// public class DetectTriggerOverlap : MonoBehaviour, PhysicsCallbacks.ITriggerCallback
        /// {
        ///     void Start()
        ///     {
        ///         PhysicsWorld world = PhysicsWorld.defaultWorld;
        ///
        ///         // Create a trigger shape that reports overlaps instead of colliding.
        ///         PhysicsBody triggerBody = world.CreateBody(new PhysicsBodyDefinition
        ///         {
        ///             position = new Vector2(0.5f, 8f),
        ///             type = PhysicsBody.BodyType.Dynamic
        ///         });
        ///         PhysicsShape triggerShape = triggerBody.CreateShape(CircleGeometry.defaultGeometry, new PhysicsShapeDefinition
        ///         {
        ///             isTrigger = true,
        ///             triggerEvents = true
        ///         });
        ///         triggerShape.callbackTarget = this;
        ///
        ///         // Create a static shape for the trigger to overlap.
        ///         PhysicsBody staticBody = world.CreateBody(new PhysicsBodyDefinition
        ///         {
        ///             position = new Vector2(0f, 0f),
        ///             type = PhysicsBody.BodyType.Static
        ///         });
        ///         staticBody.CreateShape(new CircleGeometry { radius = 3f }, new PhysicsShapeDefinition { triggerEvents = true });
        ///     }
        ///
        ///     // Log when the trigger starts overlapping another shape.
        ///     public void OnTriggerBegin2D(PhysicsEvents.TriggerBeginEvent beginEvent)
        ///     {
        ///         Debug.Log("Trigger overlap started.");
        ///     }
        ///
        ///     // Log when the trigger stops overlapping another shape.
        ///     public void OnTriggerEnd2D(PhysicsEvents.TriggerEndEvent endEvent)
        ///     {
        ///         Debug.Log("Trigger overlap ended.");
        ///     }
        /// }
        /// ]]>
        /// </code>
        /// </example>
        /// <seealso cref="PhysicsWorld.SendTriggerCallbacks"/>
        /// <seealso cref="IContactCallback"/>
        public interface ITriggerCallback
        {
            /// <summary>
            /// Called when a <see cref="PhysicsEvents.TriggerBeginEvent"/> for the object occurs.
            /// This will always be called on the main-thread after the simulation has finished.
            /// </summary>
            /// <param name="beginEvent">The event that occurred.</param>
            void OnTriggerBegin2D(PhysicsEvents.TriggerBeginEvent beginEvent);

            /// <summary>
            /// Called when a <see cref="PhysicsEvents.TriggerEndEvent"/> for the object occurs.
            /// This will always be called on the main-thread after the simulation has finished.
            /// </summary>
            /// <param name="endEvent">The event that occurred.</param>
            void OnTriggerEnd2D(PhysicsEvents.TriggerEndEvent endEvent);
        }

        /// <summary>
        /// An interface that when implemented, can be called as a target by <see cref="PhysicsWorld.SendContactCallbacks"/>.
        /// </summary>
        /// <remarks>
        /// Unity calls `OnContactBegin2D` and `OnContactEnd2D` on the main thread after the simulation finishes.
        /// </remarks>
        /// <example>
        /// <code lang="cs">
        /// <![CDATA[
        /// using UnityEngine;
        /// using Unity.U2D.Physics;
        ///
        /// // Add the PhysicsCallbacks.IContactCallback interface.
        /// public class DetectCollisions : MonoBehaviour, PhysicsCallbacks.IContactCallback
        /// {
        ///     void Start()
        ///     {
        ///         PhysicsWorld world = PhysicsWorld.defaultWorld;
        ///
        ///         // Create a small falling circle.
        ///         PhysicsBody object1 = world.CreateBody(new PhysicsBodyDefinition
        ///         {
        ///             position = new Vector2(0.5f, 8f),
        ///             type = PhysicsBody.BodyType.Dynamic
        ///         });
        ///         PhysicsShape objectShape1 = object1.CreateShape(CircleGeometry.defaultGeometry);
        ///
        ///         // Create a larger static circle below.
        ///         PhysicsBody object2 = world.CreateBody(new PhysicsBodyDefinition
        ///         {
        ///             position = new Vector2(0f, 0f),
        ///             type = PhysicsBody.BodyType.Static
        ///         });
        ///         object2.CreateShape(new CircleGeometry { radius = 3f });
        ///
        ///         // Set object 1 to activate collisions.
        ///         objectShape1.contactEvents = true;
        ///         objectShape1.callbackTarget = this;
        ///     }
        ///
        ///     // Log when the small circle collides.
        ///     public void OnContactBegin2D(PhysicsEvents.ContactBeginEvent eventData)
        ///     {
        ///         var contact = eventData.contactId.contact;
        ///         Debug.Log("Collision started between shapes: " + contact.shapeA + " and " + contact.shapeB);
        ///     }
        ///
        ///     // Log when the small circle stops colliding.
        ///     public void OnContactEnd2D(PhysicsEvents.ContactEndEvent eventData)
        ///     {
        ///         var contact = eventData.contactId.contact;
        ///         Debug.Log("Collision ended between shapes: " + contact.shapeA + " and " + contact.shapeB);
        ///     }
        /// }
        /// ]]>
        /// </code>
        /// </example>
        /// <seealso cref="PhysicsWorld.SendContactCallbacks"/>
        /// <seealso cref="ITriggerCallback"/>
        public interface IContactCallback
        {
            /// <summary>
            /// Called when a <see cref="PhysicsEvents.ContactBeginEvent"/> for the object occurs.
            /// This will always be called on the main-thread after the simulation has finished.
            /// </summary>
            /// <param name="beginEvent">The event that occurred.</param>
            void OnContactBegin2D(PhysicsEvents.ContactBeginEvent beginEvent);

            /// <summary>
            /// Called when a <see cref="PhysicsEvents.ContactEndEvent"/> for the object occurs.
            /// This will always be called on the main-thread after the simulation has finished.
            /// </summary>
            /// <param name="endEvent">The event that occurred.</param>
            void OnContactEnd2D(PhysicsEvents.ContactEndEvent endEvent);
        }

        /// <summary>
        /// An interface that when implemented, can be called as a target by <see cref="PhysicsWorld.SendJointThresholdCallbacks"/>.
        /// </summary>
        public interface IJointThresholdCallback
        {
            /// <summary>
            /// Called when a <see cref="PhysicsEvents.JointThresholdEvent"/> for the object occurs.
            /// This will always be called on the main-thread after the simulation has finished.
            /// </summary>
            /// <param name="thresholdEvent">The event that occurred.</param>
            void OnJointThreshold2D(PhysicsEvents.JointThresholdEvent thresholdEvent);
        }

        /// <summary>
        /// An interface that when implemented, can be called as a target set with <see cref="PhysicsWorld.transformWriteCallbackTarget"/>.
        /// </summary>
        public interface ITransformWriteCallback
        {
            /// <summary>
            /// The callback will only occur if <see cref="PhysicsWorld.transformWriteMode"/> is set to <see cref="PhysicsWorld.TransformWriteMode.Custom"/> and there are <see cref="PhysicsWorld.bodyUpdateEvents"/> available.
            /// To aid in correctly calculating the write pose, <see cref="PhysicsBody.TransformWriteTween.GetPose(PhysicsWorld.TransformPlane, ref PhysicsWorld.TransformPlaneCustom, bool, out UnityEngine.Vector3, out UnityEngine.Quaternion)"/> can be used.
            /// The <see cref="PhysicsBody.TransformWriteTween"/> sent to this event will automatically be assigned to the world for tweening if <see cref="PhysicsWorld.transformTweenMode"/> is not <see cref="PhysicsWorld.TransformTweenMode.Off"/>.
            /// This will always be called on the main-thread after the simulation has finished.
            /// You should avoid write operations on physics objects during this callback.
            ///
            /// NOTE: When transform writing, the <see cref="PhysicsEvents.TransformWriteEvent"/> provides all the <see cref="PhysicsBody.TransformWriteTween"/> in preparation for transform writing and tweening.
            /// </summary>
            /// <param name="transformWriteEvent">The event that occurred.</param>
            void OnTransformWrite(PhysicsEvents.TransformWriteEvent transformWriteEvent);

            /// <summary>
            /// The callback will only occur if <see cref="PhysicsWorld.transformTweenMode"/> is set to <see cref="PhysicsWorld.TransformTweenMode.Custom"/> and there are transform write tweens available.
            /// This will always be called on the main-thread after the simulation has finished.
            /// You should avoid write operations on physics objects during this callback.
            ///
            /// NOTE: When transform tweening, you can calculate <see cref="PhysicsBody.TransformWriteMode.Interpolate"/> or <see cref="PhysicsBody.TransformWriteMode.Extrapolate"/> write modes by using <see cref="PhysicsBody.TransformWriteTween.GetInterpolatedPose(PhysicsWorld.TransformPlane, ref PhysicsWorld.TransformPlaneCustom, bool, float, out UnityEngine.Vector3, out UnityEngine.Quaternion)"/> and <see cref="PhysicsBody.TransformWriteTween.GetExtrapolatedPose(PhysicsWorld.TransformPlane, ref PhysicsWorld.TransformPlaneCustom, float, out UnityEngine.Vector3, out UnityEngine.Quaternion)"/> respectively.
            /// </summary>
            /// <param name="transformTweenWriteEvent">The event that occurred.</param>
            void OnTransformTweenWrite(PhysicsEvents.TransformTweenWriteEvent transformTweenWriteEvent);
        }

        /// <summary>
        /// An interface that when implemented, can be called when using <see cref="PhysicsWorld.RegisterTransformChange(UnityEngine.Transform, ITransformChangedCallback)"/>.
        /// </summary>
        public interface ITransformChangedCallback
        {
            /// <summary>
            /// Called when a <see cref="PhysicsEvents.TransformChangeEvent"/> for the object occurs.
            /// This will always be called on the main-thread.
            /// </summary>
            /// <param name="transformChangeEvent"></param>
            void OnTransformChanged(PhysicsEvents.TransformChangeEvent transformChangeEvent);
        }

        #endregion

        #region Callback Targets

        /// <summary>
        /// Contains all the body update callback targets returned from <see cref="PhysicsWorld.GetBodyUpdateCallbackTargets(Unity.Collections.Allocator)"/>.
        /// </summary>
        public readonly record struct BodyUpdateCallbackTargets : IDisposable
        {
            /// <summary>
            /// Body update event target for callbacks.
            /// </summary>
            [StructLayout(LayoutKind.Sequential)]
            public readonly record struct BodyUpdateTarget
            {
                /// <summary>
                /// The event.
                /// </summary>
                public readonly PhysicsEvents.BodyUpdateEvent bodyUpdateEvent => m_BodyUpdateEvent;

                /// <summary>
                /// The callback target (<see cref="PhysicsShape.callbackTarget"/>) associated with <see cref="PhysicsEvents.BodyUpdateEvent"/>.
                /// This returns any implemented <see cref="PhysicsCallbacks.IBodyUpdateCallback"/> or NULL if not implemented or no target.
                /// </summary>
                public readonly IBodyUpdateCallback bodyTarget
                {
                    get
                    {
                        if (m_BodyUpdateEvent.body.isValid)
                            return m_BodyUpdateEvent.body.callbackTarget as IBodyUpdateCallback;

                        return null;
                    }
                }

                #region Internal

                readonly PhysicsEvents.BodyUpdateEvent m_BodyUpdateEvent;

                #endregion
            }

            /// <summary>
            /// The body update targets.
            /// </summary>
            public ReadOnlySpan<BodyUpdateTarget> bodyUpdateCallbackTargets => m_BodyUpdateCallbackTargets.ToReadOnlySpan<BodyUpdateTarget>();

            /// <summary>
            /// Dispose of any allocated memory. This must be called if any targets are returned otherwise memory leaks will occur.
            /// </summary>
            public void Dispose()
            {
                m_BodyUpdateCallbackTargets.Dispose();
            }

            #region Internal

            readonly PhysicsBuffer m_BodyUpdateCallbackTargets;

            #endregion
        }

        /// <summary>
        /// Contains all the trigger callback targets returned from <see cref="PhysicsWorld.GetTriggerCallbackTargets(Unity.Collections.Allocator)"/>.
        /// </summary>
        public readonly record struct TriggerCallbackTargets : IDisposable
        {
            /// <summary>
            /// Trigger begin event target for callbacks.
            /// </summary>
            [StructLayout(LayoutKind.Sequential)]
            public readonly record struct TriggerBeginTarget
            {
                /// <summary>
                /// The trigger begin event.
                /// </summary>
                public readonly PhysicsEvents.TriggerBeginEvent beginEvent => m_BeginEvent;

                /// <summary>
                /// The callback target (<see cref="PhysicsShape.callbackTarget"/>) associated with <see cref="PhysicsEvents.TriggerBeginEvent.triggerShape"/>.
                /// This returns any implemented <see cref="PhysicsCallbacks.ITriggerCallback"/> or NULL if not implemented or no target.
                /// </summary>
                public readonly ITriggerCallback triggerShapeTarget
                {
                    get
                    {
                        if (m_BeginEvent.triggerShape.isValid)
                            return m_BeginEvent.triggerShape.callbackTarget as ITriggerCallback;

                        return null;
                    }
                }

                /// <summary>
                /// The callback target (<see cref="PhysicsShape.callbackTarget"/>) associated with <see cref="PhysicsEvents.TriggerBeginEvent.visitorShape"/>.
                /// This returns any implemented <see cref="PhysicsCallbacks.ITriggerCallback"/> or NULL if not implemented or no target.
                /// </summary>
                public readonly ITriggerCallback visitorShapeTarget
                {
                    get
                    {
                        if (m_BeginEvent.visitorShape.isValid)
                            return m_BeginEvent.visitorShape.callbackTarget as ITriggerCallback;

                        return null;
                    }
                }

                #region Internal

                readonly PhysicsEvents.TriggerBeginEvent m_BeginEvent;

                #endregion
            }

            /// <summary>
            /// Trigger end event target for callbacks.
            /// </summary>
            [StructLayout(LayoutKind.Sequential)]
            public readonly record struct TriggerEndTarget
            {
                /// <summary>
                /// The trigger end event.
                /// </summary>
                public readonly PhysicsEvents.TriggerEndEvent endEvent => m_EndEvent;

                /// <summary>
                /// The callback target (<see cref="PhysicsShape.callbackTarget"/>) associated with <see cref="PhysicsEvents.TriggerEndEvent.triggerShape"/>.
                /// This returns any implemented <see cref="PhysicsCallbacks.ITriggerCallback"/> or NULL if not implemented or no target.
                /// </summary>
                public readonly ITriggerCallback triggerShapeTarget
                {
                    get
                    {
                        if (m_EndEvent.triggerShape.isValid)
                            return m_EndEvent.triggerShape.callbackTarget as ITriggerCallback;

                        return null;
                    }
                }

                /// <summary>
                /// The callback target (<see cref="PhysicsShape.callbackTarget"/>) associated with <see cref="PhysicsEvents.TriggerEndEvent.visitorShape"/>.
                /// This returns any implemented <see cref="PhysicsCallbacks.ITriggerCallback"/> or NULL if not implemented or no target.
                /// </summary>
                public readonly ITriggerCallback visitorShapeTarget
                {
                    get
                    {
                        if (m_EndEvent.visitorShape.isValid)
                            return m_EndEvent.visitorShape.callbackTarget as ITriggerCallback;

                        return null;
                    }
                }

                #region Internal

                readonly PhysicsEvents.TriggerEndEvent m_EndEvent;

                #endregion
            }

            /// <summary>
            /// The begin targets.
            /// </summary>
            public ReadOnlySpan<TriggerBeginTarget> BeginCallbackTargets => m_BeginCallbackTargets.ToReadOnlySpan<TriggerBeginTarget>();

            /// <summary>
            /// The end targets.
            /// </summary>
            public ReadOnlySpan<TriggerEndTarget> EndCallbackTargets => m_EndCallbackTargets.ToReadOnlySpan<TriggerEndTarget>();

            /// <summary>
            /// Dispose of any allocated memory. This must be called if any targets are returned otherwise memory leaks will occur.
            /// </summary>
            public void Dispose()
            {
                m_BeginCallbackTargets.Dispose();
                m_EndCallbackTargets.Dispose();
            }

            #region Internal

            readonly PhysicsBuffer m_BeginCallbackTargets;
            readonly PhysicsBuffer m_EndCallbackTargets;

            #endregion
        }

        /// <summary>
        /// Contains all the contact callback targets returned from <see cref="PhysicsWorld.GetContactCallbackTargets(Unity.Collections.Allocator)"/>.
        /// </summary>
        public readonly record struct ContactCallbackTargets : IDisposable
        {
            /// <summary>
            /// Contact begin event target for callbacks.
            /// </summary>
            [StructLayout(LayoutKind.Sequential)]
            public readonly record struct ContactBeginTarget
            {
                /// <summary>
                /// The event.
                /// </summary>
                public readonly PhysicsEvents.ContactBeginEvent beginEvent => m_BeginEvent;

                /// <summary>
                /// The callback target (<see cref="PhysicsShape.callbackTarget"/>) associated with <see cref="PhysicsEvents.ContactBeginEvent.shapeA"/>.
                /// This returns any implemented <see cref="PhysicsCallbacks.IContactCallback"/> or NULL if not implemented or no target.
                /// </summary>
                public readonly IContactCallback shapeTargetA
                {
                    get
                    {
                        if (m_BeginEvent.shapeA.isValid)
                            return m_BeginEvent.shapeA.callbackTarget as IContactCallback;

                        return null;
                    }
                }

                /// <summary>
                /// The callback target (<see cref="PhysicsShape.callbackTarget"/>) associated with <see cref="PhysicsEvents.ContactBeginEvent.shapeB"/>.
                /// This returns any implemented <see cref="PhysicsCallbacks.IContactCallback"/> or NULL if not implemented or no target.
                /// </summary>
                public readonly IContactCallback shapeTargetB
                {
                    get
                    {
                        if (m_BeginEvent.shapeB.isValid)
                            return m_BeginEvent.shapeB.callbackTarget as IContactCallback;

                        return null;
                    }
                }

                #region Internal

                readonly PhysicsEvents.ContactBeginEvent m_BeginEvent;

                #endregion
            }

            /// <summary>
            /// Contact end event target for callbacks.
            /// </summary>
            [StructLayout(LayoutKind.Sequential)]
            public readonly record struct ContactEndTarget
            {
                /// <summary>
                /// The event.
                /// </summary>
                public readonly PhysicsEvents.ContactEndEvent endEvent => m_EndEvent;

                /// <summary>
                /// The callback target (<see cref="PhysicsShape.callbackTarget"/>) associated with <see cref="PhysicsEvents.ContactEndEvent.shapeA"/>.
                /// This returns any implemented <see cref="PhysicsCallbacks.IContactCallback"/> or NULL if not implemented or no target.
                /// </summary>
                public readonly IContactCallback shapeTargetA
                {
                    get
                    {
                        if (m_EndEvent.shapeA.isValid)
                            return m_EndEvent.shapeA.callbackTarget as IContactCallback;

                        return null;
                    }
                }

                /// <summary>
                /// The callback target (<see cref="PhysicsShape.callbackTarget"/>) associated with <see cref="PhysicsEvents.ContactEndEvent.shapeB"/>.
                /// This returns any implemented <see cref="PhysicsCallbacks.IContactCallback"/> or NULL if not implemented or no target.
                /// </summary>
                public readonly IContactCallback shapeTargetB
                {
                    get
                    {
                        if (m_EndEvent.shapeB.isValid)
                            return m_EndEvent.shapeB.callbackTarget as IContactCallback;

                        return null;
                    }
                }

                #region Internal

                readonly PhysicsEvents.ContactEndEvent m_EndEvent;

                #endregion
            }

            /// <summary>
            /// The begin targets.
            /// </summary>
            public ReadOnlySpan<ContactBeginTarget> BeginCallbackTargets => m_BeginCallbackTargets.ToReadOnlySpan<ContactBeginTarget>();

            /// <summary>
            /// The end targets.
            /// </summary>
            public ReadOnlySpan<ContactEndTarget> EndCallbackTargets => m_EndCallbackTargets.ToReadOnlySpan<ContactEndTarget>();

            /// <summary>
            /// Dispose of any allocated memory. This must be called if any targets are returned otherwise memory leaks will occur.
            /// </summary>
            public void Dispose()
            {
                m_BeginCallbackTargets.Dispose();
                m_EndCallbackTargets.Dispose();
            }

            #region Internal

            readonly PhysicsBuffer m_BeginCallbackTargets;
            readonly PhysicsBuffer m_EndCallbackTargets;

            #endregion
        }

        /// <summary>
        /// Contains all the joint callback targets returned from <see cref="PhysicsWorld.GetJointThresholdCallbackTargets(Unity.Collections.Allocator)"/>.
        /// </summary>
        public readonly record struct JointThresholdCallbackTargets : IDisposable
        {
            /// <summary>
            /// Joint threshold event target for callbacks.
            /// </summary>
            [StructLayout(LayoutKind.Sequential)]
            public readonly record struct JointThresholdTarget
            {
                /// <summary>
                /// The event.
                /// </summary>
                public readonly PhysicsEvents.JointThresholdEvent jointThresholdEvent => m_JointThresholdEvent;

                /// <summary>
                /// The <see cref="PhysicsShape"/> target (<see cref="PhysicsShape.callbackTarget"/>) associated with <see cref="PhysicsEvents.JointThresholdEvent.joint"/>.
                /// This returns any implemented <see cref="PhysicsCallbacks.IJointThresholdCallback"/> or NULL if not implemented or no target.
                /// </summary>
                public readonly IJointThresholdCallback jointTarget
                {
                    get
                    {
                        if (m_JointThresholdEvent.joint.isValid)
                            return m_JointThresholdEvent.joint.callbackTarget as IJointThresholdCallback;

                        return null;
                    }
                }

                #region Internal

                readonly PhysicsEvents.JointThresholdEvent m_JointThresholdEvent;

                #endregion
            }

            /// <summary>
            /// The joint threshold targets.
            /// </summary>
            public ReadOnlySpan<JointThresholdTarget> jointThresholdCallbackTargets => m_JointThresholdCallbackTargets.ToReadOnlySpan<JointThresholdTarget>();

            /// <summary>
            /// Dispose of any allocated memory. This must be called if any targets are returned otherwise memory leaks will occur.
            /// </summary>
            public void Dispose()
            {
                m_JointThresholdCallbackTargets.Dispose();
            }

            #region Internal

            readonly PhysicsBuffer m_JointThresholdCallbackTargets;

            #endregion
        }

        #endregion
    }
}
