// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Jobs;
using Unity.Collections;
using UnityEngine.Scripting.APIUpdating;
using static Unity.U2D.Physics.Scripting2D;

namespace Unity.U2D.Physics
{
    /// <summary>
    /// A world is a container for all other physics objects such as <see cref="PhysicsBody"/>, <see cref="PhysicsShape"/>, <see cref="PhysicsJoint"/> etc.
    /// A world can be simulated in isolation from all other worlds.
    /// The maximum number of worlds that can be created at one time is defined by <see cref="PhysicsCoreSettings2D.maximumWorlds"/>.
    /// A world is completely isolated from all other worlds.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [MovedFrom(autoUpdateAPI: ScriptUpdateConstants.AutoUpdateAPI, sourceNamespace: ScriptUpdateConstants.SourceNamespace, sourceAssembly: ScriptUpdateConstants.SourceAssembly)]
    public readonly partial struct PhysicsWorld : IEquatable<PhysicsWorld>
    {
        #region Id

        internal readonly UInt16 m_Index1;
        readonly UInt16 m_Generation;

        /// <undoc/>
        public override readonly string ToString() => isValid ? $"index={m_Index1}, generation={m_Generation}" : "<INVALID>";

        #endregion

        #region Equality

        /// <undoc/>
        public override bool Equals(object obj) { return base.Equals(obj); }

        /// <undoc/>
        public bool Equals(PhysicsWorld other) { return m_Index1 == other.m_Index1 && m_Generation == other.m_Generation; }

        /// <undoc/>
        public static bool operator ==(PhysicsWorld lhs, PhysicsWorld rhs) => lhs.Equals(rhs);

        /// <undoc/>
        public static bool operator !=(PhysicsWorld lhs, PhysicsWorld rhs) => !(lhs == rhs);

        /// <undoc/>
        public override int GetHashCode() { return HashCode.Combine(m_Index1, m_Generation); }

        #endregion

        /// <summary>
        /// Defines when the simulation will run.
        /// </summary>
        public enum SimulationType
        {
            /// <summary>
            /// The simulation will automatically run during the FixedUpdate.
            /// </summary>
            FixedUpdate = 0,

            /// <summary>
            /// The simulation will automatically run during the Update.
            /// </summary>
            Update = 1,

            /// <summary>
            /// The simulation will only run when manually called with <see cref="PhysicsWorld.Simulate(float)"/>.
            /// </summary>
            Script = 2
        }

        /// <summary>
        /// Defines when changes to <see cref="UnityEngine.Transform"/> that are registered with <see cref="PhysicsWorld.RegisterTransformChange(Transform, PhysicsCallbacks.ITransformChangedCallback)"/> are called.
        /// 
        /// NOTE: In the Unity Editor when not in Play Mode, Transform change callbacks are always and only sent at the start of the frame for authoring purposes.
        /// </summary>
        public enum TransformChangeMode
        {
            /// <summary>
            /// Transform Change callbacks are not sent in play mode.
            /// </summary>
            Off,

            /// <summary>
            /// Transform Change callbacks are sent at the start of a frame prior to the "FixedUpdate" or "Update" script callbacks.
            /// This is typically used when any changes to Transform from the previous frame need to be handled before anything else runs.
            /// </summary>
            FrameStart,

            /// <summary>
            /// Transform Change callbacks are sent after the "FixedUpdate" script callbacks but before any "FixedUpdate" simulation*s).
            /// This is typically used when any changes to Transforms occur in the "FixedUpdate" script callbacks need to be handled before any "FixedUpdate" simulation(s).
            /// </summary>
            FixedUpdate,

            /// <summary>
            /// Transform Change callbacks are sent after the "Update" script callbacks but before any "Update" simulation(s).
            /// This is typically used when any changes to Transforms during the "Update" script callbacks need to be handled before any "Update" simulation(s).
            /// </summary>
            Update
        }

        /// <summary>
        /// Defines how the 2D Transforms from each <see cref="PhysicsBody"/> are written to the 3D Transform system.
        /// </summary>
        public enum TransformWriteMode
        {
            /// <summary>
            /// Transforms are never written. This is the fastest operation.
            /// </summary>
            Off,

            /// <summary>
            /// Transforms are written but the rotation is converted to a <see cref="UnityEngine.Quaternion"/> where only a single axis is written, all others will be set to zero rotation.
            /// This is the fastest method of writing transforms however, any 3D rotations or rotations on the unused axis will be reset to zero.
            /// The rotational axis written to depends on the current <see cref="PhysicsWorld.TransformPlane"/> selected with <see cref="PhysicsWorld.transformPlane"/> where it will always be perpendicular to the transform plane.
            /// </summary>
            Fast2D,

            /// <summary>
            /// Transforms are written but the rotation is converted to a <see cref="UnityEngine.Quaternion"/> where the rotation of the body transform is merged into the existing 3D rotation.
            /// This is the slowest method of writing transforms however, all 3D rotations are preserved.
            /// The rotational axis written to depends on the current <see cref="PhysicsWorld.TransformPlane"/> selected with <see cref="PhysicsWorld.transformPlane"/> where it will always be perpendicular to the transform plane.
            /// </summary>
            Slow3D,

            /// <summary>
            /// Transforms are not written.
            /// Instead, the callback target set with <see cref="PhysicsWorld.transformWriteCallbackTarget"/> which must implement <see cref="PhysicsCallbacks.ITransformWriteCallback"/> will have <see cref="PhysicsCallbacks.ITransformWriteCallback.OnTransformWrite(PhysicsEvents.TransformWriteEvent)"/> called allowing custom transform writing.
            /// </summary>
            Custom
        }

        /// <summary>
        /// Defines if and how Transform tweens are calculated and/or written.
        /// </summary>
        public enum TransformTweenMode
        {
            /// <summary>
            /// Transform tweens are not calculated or written.
            /// </summary>
            Off,

            /// <summary>
            /// Transform tweens are calculated and written in parallel using a <see cref="UnityEngine.Jobs.TransformAccess"/>.
            /// </summary>
            Parallel,

            /// <summary>
            /// Transform tweens are calculated and written linearly on a single thread, likely the main-thread.
            /// This may be faster than using <see cref="PhysicsWorld.TransformTweenMode.Parallel"/> if the majority of the  are not split across hierarchies so that they can be written in parallel.
            /// To further clarify, if most of the <see cref="UnityEngine.Transform"/> are not interleaved across different hierarchies, this non-parallel (sequential) mode may be faster than <see cref="PhysicsWorld.TransformTweenMode.Parallel"/>,
            /// because it avoids the overhead of splitting and synchronizing work across multiple threads when there is not enough independent hierarchy work to parallelize efficiently.
            /// </summary>
            Sequential,

            /// <summary>
            /// Transform tweens are not calculated or written.
            /// Instead, the callback target set with <see cref="PhysicsWorld.transformWriteCallbackTarget"/> which must implement <see cref="PhysicsCallbacks.ITransformWriteCallback"/> will be have <see cref="PhysicsCallbacks.ITransformWriteCallback.OnTransformTweenWrite(PhysicsEvents.TransformTweenWriteEvent)"/> called allowing custom transform tween writing.
            /// </summary>
            Custom
        }

        /// <summary>
        /// Defines the reason why a <see cref="UnityEngine.Transform"/> changed.
        /// Register and unregister for transform changes with <see cref="PhysicsWorld.RegisterTransformChange(Transform, PhysicsCallbacks.ITransformChangedCallback)"/> and <see cref="PhysicsWorld.UnregisterTransformChange(Transform, PhysicsCallbacks.ITransformChangedCallback)"/>.
        /// </summary>
        [Flags]
        public enum TransformChangeReason
        {
            /// <summary>
            /// The World-space position of the transform changed.
            /// Changing a parent results in an event in children transform too.
            /// See <see cref="UnityEngine.Transform.position"/>.
            /// </summary>
            WorldPosition = 1 << 0,

            /// <summary>
            /// The World-space rotation of the transform changed.
            /// Changing a parent results in an event in children transform too.
            /// See <see cref="UnityEngine.Transform.rotation"/>.
            /// </summary>
            WorldRotation = 1 << 1,

            /// <summary>
            /// The World-space scale of the transform changed.
            /// Changing a parent results in an event in children transform too.
            /// See <see cref="UnityEngine.Transform.lossyScale"/>.
            /// </summary>
            WorldScale = 1 << 2,

            /// <summary>
            /// The local position of the transform changed.
            /// This does not propagate to children or parent transforms.
            /// See <see cref="UnityEngine.Transform.localPosition"/>.
            /// </summary>
            LocalPosition = 1 << 3,

            /// <summary>
            /// The local rotation of the transform changed.
            /// This does not propagate to children or parent transforms.
            /// See <see cref="UnityEngine.Transform.localRotation"/>.
            /// </summary>
            LocalRotation = 1 << 4,

            /// <summary>
            /// The local scale of the transform changed.
            /// This does not propagate to children or parent transforms.
            /// See <see cref="UnityEngine.Transform.localScale"/>.
            /// </summary>
            LocalScale = 1 << 5,

            /// <summary>
            /// The animation system wrote a physics-based world-space TRS change.
            /// </summary>
            Animation = 1 << 6,

            /// <summary>
            /// The parent transform hierarchy changed.
            /// Indicates that a direct or indirect parent has been added, removed or re-parented.
            /// </summary>
            ParentHierarchy = 1 << 7,

            /// <summary>
            /// The world-space position, rotation or scale of the transform changed.
            /// </summary>
            AnyWorld = WorldPosition | WorldRotation | WorldScale,

            /// <summary>
            /// The local position, rotation or scale of the transform changed.
            /// </summary>
            AnyLocal = LocalPosition | LocalRotation | LocalScale,

            /// <summary>
            /// Any transform change.
            /// </summary>
            Any = AnyWorld | AnyLocal | Animation | ParentHierarchy
        }

        /// <summary>
        /// Defines the 2D Transform plane where Transform writes will occur.
        /// This also defines the rotation axis which will automatically be perpendicular to the selected plane.
        /// See <see cref="PhysicsWorld.transformPlane"/>.
        /// </summary>
        public enum TransformPlane
        {
            /// <summary>
            /// XY plane with anti-clockwise Z rotation.
            /// </summary>
            XY = 0,

            /// <summary>
            /// XZ plane with anti-clockwise Y rotation.
            /// </summary>
            XZ = 1,

            /// <summary>
            /// ZY plane with anti-clockwise X rotation.
            /// </summary>
            ZY = 2,

            /// <summary>
            /// Use the assigned <see cref="PhysicsWorld.transformPlaneCustom"/> to allow transformation writing and reading to/from a custom 2D plane.
            /// </summary>
            Custom = 3
        }

        /// <summary>
        /// A transformation applied to the transform write if <see cref="PhysicsWorld.transformPlane"/> is set to <see cref="PhysicsWorld.TransformPlane.Custom"/>.
        /// </summary>
        [Serializable]
        public struct TransformPlaneCustom : ISerializationCallbackReceiver
        {
            /// <summary>
            /// Create a transform plane custom as identity.
            /// </summary>
            public TransformPlaneCustom()
            {
                m_Translate = default;
                m_Rotate = default;
                m_Scale = 1.0f;

                CalculatePlaneCustom();
            }

            /// <summary>
            /// Create a transform plane custom.
            /// </summary>
            /// <param name="translate">The custom translation.</param>
            /// <param name="rotate">The custom EULER rotation.</param>
            /// <param name="scale">The custom scale.</param>
            public TransformPlaneCustom(Vector3 translate, Vector3 rotate, float scale = 1.0f)
            {
                m_Translate = translate;
                m_Rotate = rotate;
                m_Scale = Mathf.Clamp(scale, 0.001f, 10f);

                CalculatePlaneCustom();
            }

            /// <summary>
            /// Get the custom translation.
            /// </summary>
            public readonly Vector3 translate => m_Translate;

            /// <summary>
            /// Get the custom rotation.
            /// </summary>
            public readonly Vector3 rotate => m_Rotate;

            /// <summary>
            /// Get the uniform scale.
            /// </summary>
            public readonly float scale => m_Scale;

            /// <summary>
            /// Get the custom matrix defining how to transform from <see cref="PhysicsWorld"/> space to the custom world-space.
            ///
            /// NOTE: This is the inverse of the <see cref="PhysicsWorld.TransformPlaneCustom.fromCustom"/> matrix.
            /// </summary>
            public readonly Matrix4x4 toCustom => m_ToCustom;

            /// <summary>
            /// Get the custom matrix defining how to transform from the custom world-space to the <see cref="PhysicsWorld"/> space.
            ///
            /// NOTE: This is the inverse of the <see cref="PhysicsWorld.TransformPlaneCustom.toCustom"/> matrix.
            /// </summary>
            public readonly Matrix4x4 fromCustom => m_FromCustom;

            /// <summary>
            /// Project the position and rotation to the custom transform plane.
            /// </summary>
            /// <param name="physicsTransform">The physics transform to project.</param>
            /// <param name="position">The 3D transformed position.</param>
            /// <param name="rotation">The 3D transformed rotation.</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal readonly void PlaneProjection(ref PhysicsTransform physicsTransform, out Vector3 position, out Quaternion rotation)
            {
                position = ToPosition(physicsTransform.position);

                var physicsRotation = PhysicsRotate.FromRadians(physicsTransform.rotation.radians * 0.5f);
                rotation = m_CustomRotation * new Quaternion(0.0f, 0.0f, physicsRotation.sin, physicsRotation.cos);
            }

            /// <summary>
            /// Transform a 2D <see cref="PhysicsWorld"/> position to a 3D custom world-space position.
            /// </summary>
            /// <param name="position">The 2D position to transform.</param>
            /// <returns>The transformed 3D position.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly Vector3 ToPosition(Vector2 position) => toCustom.MultiplyPoint(position);

            /// <summary>
            /// Transform from a 3D custom world-space position back to a 2D <see cref="PhysicsWorld"/> position.
            /// </summary>
            /// <param name="position">The 3D position to transform.</param>
            /// <returns>The transformed 2D position.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly Vector2 FromPosition(Vector3 position) => fromCustom.MultiplyPoint(position);

            // Calculate the resultant custom transform plane.
            void CalculatePlaneCustom()
            {
                // Calculate the projection rotation.
                m_CustomRotation = Quaternion.Euler(m_Rotate);

                // Calculate the matrices.
                m_ToCustom = Matrix4x4.TRS(m_Translate, m_CustomRotation, new Vector3(m_Scale, m_Scale, m_Scale));
                m_FromCustom = m_ToCustom.inverse;
            }

            // ISerializationCallbackReceiver.
            void ISerializationCallbackReceiver.OnBeforeSerialize() => CalculatePlaneCustom();
            void ISerializationCallbackReceiver.OnAfterDeserialize() => CalculatePlaneCustom();

            #region Internal

            [SerializeField] internal Vector3 m_Translate;
            [SerializeField] internal Vector3 m_Rotate;
            [SerializeField][Range(0.001f, 10f)] internal float m_Scale;
            Matrix4x4 m_ToCustom;
            Matrix4x4 m_FromCustom;
            Quaternion m_CustomRotation;

            #endregion
        }

        /// <summary>
        /// A ignore flags are a narrow selection of objects/types in the world which needs to be ignored.
        /// </summary>
        [Flags]
        public enum IgnoreFilter : int
        {
            /// <summary>
            /// No draw filtering occurs.
            /// </summary>
            None = 0,

            /// <summary>
            /// Ignore <see cref="PhysicsBody"/> of type <see cref="PhysicsBody.BodyType.Static"/>.
            /// </summary>
            IgnoreStaticBodies = 1 << 0,

            /// <summary>
            /// Ignore <see cref="PhysicsBody"/> of type <see cref="PhysicsBody.BodyType.Kinematic"/>.
            /// </summary>
            IgnoreKinematicBodies = 1 << 1,

            /// <summary>
            /// Ignore <see cref="PhysicsBody"/> of type <see cref="PhysicsBody.BodyType.Dynamic"/>.
            /// </summary>
            IgnoreDynamicBodies = 1 << 2,

            /// <summary>
            /// Ignore <see cref="PhysicsShape"/> that are configured as a trigger.
            /// See <see cref="PhysicsShape.isTrigger"/>.
            /// </summary>
            IgnoreTriggerShapes = 1 << 3,

            /// <summary>
            /// Ignore <see cref="PhysicsShape"/> that are not configured as a trigger.
            /// See <see cref="PhysicsShape.isTrigger"/>.
            /// </summary>
            IgnoreNonTriggerShapes = 1 << 4,

            /// <summary>
            /// Ignore <see cref="PhysicsShape"/> of type <see cref="PhysicsShape.ShapeType.Circle"/>.
            /// See <see cref="PhysicsShape.shapeType"/>.
            /// </summary>
            IgnoreCircleShapes = 1 << 5,

            /// <summary>
            /// Ignore <see cref="PhysicsShape"/> of type <see cref="PhysicsShape.ShapeType.Capsule"/>.
            /// See <see cref="PhysicsShape.shapeType"/>.
            /// </summary>
            IgnoreCapsuleShapes = 1 << 6,

            /// <summary>
            /// Ignore <see cref="PhysicsShape"/> of type <see cref="PhysicsShape.ShapeType.Polygon"/>.
            /// See <see cref="PhysicsShape.shapeType"/>.
            /// </summary>
            IgnorePolygonShapes = 1 << 7,

            /// <summary>
            /// Ignore <see cref="PhysicsShape"/> of type <see cref="PhysicsShape.ShapeType.Segment"/>.
            /// See <see cref="PhysicsShape.shapeType"/>.
            /// </summary>
            IgnoreSegmentShapes = 1 << 8,

            /// <summary>
            /// Ignore <see cref="PhysicsShape"/> of type <see cref="PhysicsShape.ShapeType.ChainSegment"/>.
            /// See <see cref="PhysicsShape.shapeType"/> and <see cref="PhysicsChain"/>.
            /// </summary>
            IgnoreChainSegmentShapes = 1 << 9
        }

        /// <summary>
        /// Controls drawing and rendering is allowed.
        /// 
        /// NOTE: Drawing and rendering are always available in the Unity Editor however rendering requires compute buffer support on any device it is used without which no rendering will occur.
        /// </summary>
        public enum RenderingMode
        {
            /// <summary>
            /// Drawing and rendering is only available in the Editor and not in a player build.
            /// </summary>
            EditorOnly,

            /// <summary>
            /// Drawing and rendering is available in both the Editor and a Development player build.
            /// </summary>
            DevelopmentPlayer,

            /// <summary>
            /// Drawing and rendering is available in the Editor and any player build.
            /// </summary>
            AnyPlayer,
        };

        #region Globals

        /// <summary>
        /// Get the actual allocated  maximum worlds available.
        /// This can differ at runtime from <see cref="PhysicsCoreSettings2D.maximumWorlds"/> if it was changed and the physics system has not been restarted.
        /// Another reason this can differ would be if there was not available memory to allocate the requested maximum worlds.
        /// </summary>
        public static int maximumWorldsAllocated => PhysicsGlobal_GetMaximumWorldsAllocated();

        /// <summary>
        /// Get/Set whether safety threading locks are enabled or not.
        /// Locks are enabled by default however on platforms that do not support threading, locks are not used.
        /// Disabling locks can result in a small performance boost however, please note the following EXTREME CAUTIONS.
        /// 
        /// Typically, per-world, multiple read operations can happen in parallel however only a single write operation can occur concurrently.
        /// 
        /// Read and write operations can never happen at the same time.
        /// Locking is a self-balancing reader-preferred system that tries to reduce writers "starving".
        /// Once a writer is in a queue, it registers incoming readers as waiting readers and, once active readers are handled, it starts processing a single writer.
        /// After that writer has been handled, it flips waiting readers into active readers and processes them.
        /// Whilst this system is extremely fast, it does have a very small overhead.
        /// Disabling this system can give a small performance boost but is nearly always not worth it therefore this option should be used for testing only.
        /// 
        /// EXTREME CAUTION should be taken if disabling locks on platforms that support threading!
        /// A majority of this API is thread-safe and is is due to the safety locks!
        /// Locks are used to ensure that read and write operations do not interfere with each other.
        /// Locks also ensure that no read or write operations happen during a simulation step.
        /// Overlapping read or write operations will almost certainly result in corruptions and a subsequent crash, so unless you are absolutely sure this is not the case, do not disable locks!
        /// </summary>
        public static bool safetyLocksEnabled { get => PhysicsGlobal_GetSafetyLocksEnabled(); set => PhysicsGlobal_SetSafetyLocksEnabled(value); }

        /// <summary>
        /// Get if the automatic simulation of any <see cref="PhysicsWorld"/> is temporarily disabled.
        /// When true, no automatic simulation will occur.
        /// When false, normal operation occurs with automatic simulation.
        /// This can be controlled via <see cref="PhysicsCoreSettings2D.disableSimulation"/>.
        /// </summary>
        public static bool disableSimulation => PhysicsGlobal_GetDisableSimulation();

        /// <summary>
        /// Get if worlds are always drawn independent of whether rendering is currently active or not as specified by <see cref="PhysicsWorld.renderingMode"/>.
        /// When true, world drawing is always active and a <see cref="PhysicsEvents.WorldDrawResults"/> event is produced containing the <see cref="PhysicsWorld.DrawResults"/>.
        /// When false, world drawing only occurs depending on the <see cref="PhysicsWorld.renderingMode"/> setting.
        /// This can be controlled via <see cref="PhysicsCoreSettings2D.alwaysDrawWorlds"/>.
        /// </summary>
        public static bool alwaysDrawWorlds => PhysicsGlobal_GetAlwaysDrawWorlds();

        /// <summary>
        /// Get if rendering is currently allowed.
        /// Rendering is always allowed in the Editor however it is only allowed elsewhere depending on <see cref="PhysicsCoreSettings2D.renderingMode"/>.
        /// </summary>
        public static bool isRenderingAllowed => PhysicsGlobal_IsRenderingAllowed();

        /// <summary>
        /// Get the number of created worlds.
        /// This will be a value in the range of 1 to <see cref="PhysicsCoreSettings2D.maximumWorlds"/>.
        /// </summary>
        public static int worldCount => PhysicsWorld_GetWorldCount();

        /// <summary>
        /// Gets how many simulations can be started in parallel.
        /// 
        /// Whilst running simulations in parallel can improver overall performance, workers should ideally be left free for the simulation solver otherwise it may degrade solving performance.
        /// The actual quantity of workers used will always be capped to those available on the current device.
        /// If the total number of workers available is below 4 then parallel simulation won't occur as generally this would reduce overall performance, however parallel solving of each simulation using workers will still be used.
        /// This should not be confused with the quantity of workers used when solving a simulation.
        /// </summary>
        public static int concurrentSimulations { get => PhysicsGlobal_GetConcurrentSimulations(); }

        /// <summary>
        /// Get the internal length units per meter. Changes won't take effect until exiting play mode.
        /// The physics system relates all length units on meters but you may need different units for your project.
        /// You can set this value to use different units but it should only be modified before any other calls to the physics system occur and only modified once.
        /// Changing this value after any physics object has been created can result in severe simulation instabilities.
        ///
        /// For example, if your game uses pixels for units you can use pixels for all length values sent to the physics system.
        /// There should be no extra cost however, the physics system has some internal tolerances and thresholds that have been tuned for meters.
        /// By calling this function, the physics system is better able to adjust those tolerances and thresholds to improve accuracy.
        /// A good rule of thumb is to pass the height of your player character to this function. So if your player character is 32 pixels high, then pass 32 to this function.
        /// Then you may confidently use pixels for all the length values sent to the physics system.
        /// All length values returned from the physics system will also then be in pixels because the physics system does not do any scaling internally,
        /// however, you are now on the hook for coming up with good values for gravity, density, and forces.
        ///
        /// The default value is 1.
        /// </summary>
        public static float lengthUnitsPerMeter => PhysicsGlobal_GetLengthUnitsPerMeter();

        /// <summary>
        /// Get if the option of <see cref="PhysicsCoreSettings2D.usePhysicsLayers"/> is active or not.
        /// If no <see cref="PhysicsCoreSettings2D"/> asset is assigned, this option will return false (inactive).
        /// When active, the physics 64-bit layers are used (see <see cref="PhysicsCoreSettings2D.physicsLayerNames"/>) for property drawers and <see cref="PhysicsLayers.GetLayerMask(string[])"/>.
        /// When inactive, the 32-bit layers are used (see <see cref="UnityEngine.LayerMask"/>)  for property drawers and <see cref="PhysicsLayers.GetLayerMask(string[])"/>.
        /// In all cases, the physics system itself will always use the full 64-bit layers assigned, however when using 32-bit layers, the top 32-bits will be set to zero.
        /// </summary>
        public static bool usePhysicsLayers => PhysicsGlobal_GetUsePhysicsLayers();

        /// <summary>
        /// Get the current value of <see cref="PhysicsCoreSettings2D.transformChangeMode"/>.
        /// See <see cref="PhysicsWorld.TransformChangeMode"/>.
        /// </summary>
        public static TransformChangeMode transformChangeMode => PhysicsGlobal_GetTransformChangeMode();

        /// <summary>
        /// Get the current value of <see cref="PhysicsCoreSettings2D.renderingMode"/>.
        /// 
        /// NOTE: Drawing and rendering are always available in the Unity Editor however rendering requires compute buffer support on any device it is used without which no rendering will occur.
        /// </summary>
        public static RenderingMode renderingMode => PhysicsGlobal_GetRenderingMode();

        /// <summary>
        /// Gets what physics considers a large extent in the world.
        /// Positions greater than approximately 16km will have precision problems, so 100km as a limit should be fine in all cases. This is used to detect bad values.
        /// This value is 100000.0f * <see cref="PhysicsWorld.lengthUnitsPerMeter"/>.
        /// </summary>
        public static float hugeWorldExtent => PhysicsWorld_GetHugeWorldExtent();

        /// <summary>
        /// Get the small length used as a collision and constraint tolerance, in meters. Usually it is chosen to be numerically significant, but visually insignificant.
        /// This value is 0.005f * <see cref="PhysicsWorld.lengthUnitsPerMeter"/>. Normally this is 0.5cm.
        /// </summary>
        public static float linearSlop => PhysicsWorld_GetLinearSlop();

        /// <summary>
        /// Get the distance at which speculative contacts will be calculated. This reduces jitter.
        /// This value is 4.0f * <see cref="PhysicsWorld.lengthUnitsPerMeter"/>. Normally this is 2cm.
        /// </summary>
        public static float speculativeContactDistance => PhysicsWorld_GetSpeculativeContactDistance();

        /// <summary>
        /// Get the distance used to expand AABBs in the broadphase dynamic tree, in meters. This allows broadphase proxies to move by a small amount without triggering a tree adjustment.
        /// This value is 0.05f * <see cref="PhysicsWorld.lengthUnitsPerMeter"/>. Normally this is 5cm.
        /// </summary>
        public static float aabbMargin => PhysicsWorld_GetAABBMargin();

        /// <summary>
        /// Get the maximum rotation of a body per time step, in degrees. This limit is very large and is used to prevent numerical problems.
        /// This value is approximately 45-degrees or 0.25f * <see cref="PhysicsMath.PI"/> radians.
        /// </summary>
        public static float bodyMaxRotation => PhysicsWorld_GetBodyMaxRotation();

        /// <summary>
        /// Get the time that a body must be still before it will go to sleep, in seconds.
        /// This value is 0.5 seconds.
        /// </summary>
        public static float bodyTimeToSleep => PhysicsWorld_GetBodyTimeToSleep();

        /// <summary>
        /// Get the default world created at start-up.
        /// This world cannot be destroyed as it is permanently owned by Unity itself.
        /// See <see cref="PhysicsWorld.SetOwner(UnityEngine.Object)"/> and <see cref="PhysicsWorld.isOwned"/>.
        /// </summary>
        public static PhysicsWorld defaultWorld => PhysicsGlobal_GetDefaultWorld();

        /// <summary>
        /// Set the <see cref="UnityEngine.Transform"/> position without causing a <see cref="PhysicsEvents.TransformChangeEvent"/> to be generated by default.
        /// See <see cref="PhysicsWorld.RegisterTransformChange(Transform, PhysicsCallbacks.ITransformChangedCallback)"/>.
        /// </summary>
        /// <param name="transform">The transform to change.</param>
        /// <param name="position">The global position to set the transform to.</param>
        /// <param name="transformChangedEvent">By default, no transform changed event will be produced however this behaviour can be overridden with this argument.</param>
        public static void SetTransform(Transform transform, ref Vector3 position, bool transformChangedEvent = false)
        {
            var rotation = transform.rotation;
            PhysicsWorld_SetTransform(transform, ref position, ref rotation, transformChangedEvent);
        }

        /// <summary>
        /// Set the <see cref="UnityEngine.Transform"/> position and rotation without causing a <see cref="PhysicsEvents.TransformChangeEvent"/> to be generated.
        /// See <see cref="PhysicsWorld.RegisterTransformChange(Transform, PhysicsCallbacks.ITransformChangedCallback)"/>.
        /// </summary>
        /// <param name="transform">The transform to change.</param>
        /// <param name="position">The global position to set the transform to.</param>
        /// <param name="rotation">The global rotation to set the transform to.</param>
        /// <param name="transformChangedEvent">By default, no transform changed event will be produced however this behaviour can be overridden with this argument.</param>
        public static void SetTransform(Transform transform, ref Vector3 position, ref Quaternion rotation, bool transformChangedEvent = false) => PhysicsWorld_SetTransform(transform, ref position, ref rotation, transformChangedEvent);

        /// <summary>
        /// Set the <see cref="UnityEngine.Jobs.TransformAccess"/> position without causing a <see cref="PhysicsEvents.TransformChangeEvent"/> to be generated.
        /// See <see cref="PhysicsWorld.RegisterTransformChange(Transform, PhysicsCallbacks.ITransformChangedCallback)"/>.
        /// </summary>
        /// <param name="transformAccess">The <see cref="UnityEngine.Jobs.TransformAccess"/>used to change the transform.</param>
        /// <param name="position">The global position to set the transform to.</param>
        /// <param name="transformChangedEvent">By default, no transform changed event will be produced however this behaviour can be overridden with this argument.</param>
        public static void SetTransformAccess(ref TransformAccess transformAccess, ref Vector3 position, bool transformChangedEvent = false)
        {
            var rotation = transformAccess.rotation;
            PhysicsWorld_SetTransformAccess(ref transformAccess, ref position, ref rotation, transformChangedEvent);
        }

        /// <summary>
        /// Set the <see cref="UnityEngine.Jobs.TransformAccess"/> position and rotation without causing a <see cref="PhysicsEvents.TransformChangeEvent"/> to be generated.
        /// See <see cref="PhysicsWorld.RegisterTransformChange(Transform, PhysicsCallbacks.ITransformChangedCallback)"/>.
        /// </summary>
        /// <param name="transformAccess">The <see cref="UnityEngine.Jobs.TransformAccess"/>used to change the transform.</param>
        /// <param name="position">The global position to set the transform to.</param>
        /// <param name="rotation">The global rotation to set the transform to.</param>
        /// <param name="transformChangedEvent">By default, no transform changed event will be produced however this behaviour can be overridden with this argument.</param>
        public static void SetTransformAccess(ref TransformAccess transformAccess, ref Vector3 position, ref Quaternion rotation, bool transformChangedEvent = false) => PhysicsWorld_SetTransformAccess(ref transformAccess, ref position, ref rotation, transformChangedEvent);

        /// <summary>
        /// Register a transform watcher to call the specified callback when a transform changes.
        /// See <see cref="PhysicsEvents.TransformChangeEvent"/> for the types of transform changes that are watched for.
        ///
        /// You MUST unregister this when no longer needed with <see cref="PhysicsWorld.UnregisterTransformChange(Transform, PhysicsCallbacks.ITransformChangedCallback)"/> otherwise you will receive warnings.
        /// </summary>
        /// <param name="transform">The transform to watch for changes.</param>
        /// <param name="callback">The callback to perform when a transform change is detected.</param>
        public static void RegisterTransformChange(Transform transform, PhysicsCallbacks.ITransformChangedCallback callback) => PhysicsTransformWatcher.RegisterWatcher(transform, callback);

        /// <summary>
        /// Unregister a transform watched to stop calling the specified callback when a transform changes.
        /// See <see cref="PhysicsEvents.TransformChangeEvent"/> for the types of transform changes that are watched for.
        /// </summary>
        /// <param name="transform">The transform to stop watching changes on.</param>
        /// <param name="callback">The callback to stop being called when a transform change is detected.</param>
        public static void UnregisterTransformChange(Transform transform, PhysicsCallbacks.ITransformChangedCallback callback) => PhysicsTransformWatcher.UnregisterWatcher(transform, callback);

        /// <summary>
        /// Checks for any transform changes.
        /// Anything using <see cref="PhysicsWorld.RegisterTransformChange(Transform, PhysicsCallbacks.ITransformChangedCallback)"/> will immediately be notified of any changes.
        /// This should be used sparingly otherwise it may impact performance.
        /// The preference should be not using this but instead control transform changes to be monitored with <see cref="PhysicsWorld.transformChangeMode"/>.
        /// </summary>
        /// <returns>The number of changed transforms that were detected.</returns>
        public static int CheckTransformChanges() => PhysicsGlobal_CheckTransformChanges();

        #endregion

        /// <summary>
        /// Get all the active <see cref="PhysicsWorld"/>.
        /// This includes the <see cref="PhysicsWorld.defaultWorld"/> so will always contain at least a single world.
        /// </summary>
        /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
        /// <returns>The active world results. This NativeArray must be disposed of after use otherwise leaks will occur. The exception to this is if the array is empty.</returns>
        public static NativeArray<PhysicsWorld> GetWorlds(Allocator allocator = Unity.Collections.Allocator.Temp) => PhysicsWorld_GetWorlds(allocator).ToNativeArray<PhysicsWorld>();

        /// <summary>
        /// Get all the active <see cref="PhysicsBody"/> in the specified world.
        /// </summary>
        /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
        /// <returns>The active body results. This NativeArray must be disposed of after use otherwise leaks will occur. The exception to this is if the array is empty.</returns>
        public readonly NativeArray<PhysicsBody> GetBodies(Allocator allocator = Unity.Collections.Allocator.Temp) => PhysicsWorld_GetBodies(this, allocator).ToNativeArray<PhysicsBody>();

        /// <summary>
        /// Get all the active <see cref="PhysicsJoint"/> in the specified world.
        /// </summary>
        /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
        /// <returns>The active joints results. This NativeArray must be disposed of after use otherwise leaks will occur. The exception to this is if the array is empty.</returns>
        public readonly NativeArray<PhysicsJoint> GetJoints(Allocator allocator = Unity.Collections.Allocator.Temp) => PhysicsWorld_GetJoints(this, allocator).ToNativeArray<PhysicsJoint>();

        /// <summary>
        /// Create a PhysicsWorld using the <see cref="PhysicsWorldDefinition.defaultDefinition"/>.
        /// </summary>
        /// <returns>The created world.</returns>
        public static PhysicsWorld Create() => Create(PhysicsWorldDefinition.defaultDefinition);

        /// <summary>
        /// Create a PhysicsWorld.
        /// </summary>
        /// <param name="definition">The world definition to use.</param>
        /// <returns>The created world.</returns>
        public static PhysicsWorld Create(PhysicsWorldDefinition definition) => PhysicsWorld_Create(definition);

        /// <summary>
        /// Destroy a world, destroying all objects contained within it such as all <see cref="PhysicsBody"/> and attached <see cref="PhysicsShape"/> and <see cref="PhysicsJoint"/>.
        /// If the object is owned with <see cref="PhysicsWorld.SetOwner(UnityEngine.Object)"/> then you must provide the owner key it returned. Failing to do so will return a warning and the world will not be destroyed.
        /// You cannot destroy the <see cref="PhysicsWorld.defaultWorld"/> as it is permanently owned by Unity itself.
        /// </summary>
        /// <param name="ownerKey">Optional owner key returned when using <see cref="PhysicsWorld.SetOwner(UnityEngine.Object)"/>.</param>
        /// <returns>If the world was destroyed or not.</returns>
        public readonly bool Destroy(int ownerKey = 0) => PhysicsWorld_Destroy(this, ownerKey);

        /// <summary>
        /// Get/Set a world definition by accessing all of its current properties.
        /// This is provided as convenience only and should not be used when performance is important as all the properties defined in the definition are accessed sequentially.
        /// You should try to only use the specific properties you need rather than using this feature.
        /// </summary>
        public PhysicsWorldDefinition definition { get => PhysicsWorld_ReadDefinition(this); set => PhysicsWorld_WriteDefinition(this, value, false); }

        /// <summary>
        /// Set the (optional) owner object associated with this world and return an owner key that must be specified when destroying the world with <see cref="PhysicsWorld.Destroy(int)"/>.
        /// The physics system provides access to all objects, including the ability to destroy them so this feature can be used to stop accidental destruction of objects that are owned by other objects.
        /// You can only set the owner once, multiple attempts will produce a warning.
        /// The lifetime of the specified owner object is not linked to this world i.e. this world will still be owned by the owner object, even if it is destroyed.
        /// It is also valid to not specify an owner object (NULL) to simply gain an owner key however it can be useful, if simply for debugging purposes and discovery, to know which object is the owner.
        /// </summary>
        /// <param name="owner">The object that owns this world. This can be NULL if not required.</param>
        /// <returns>An owner key that must be passed to <see cref="PhysicsWorld.Destroy(int)"/> when destroying the body.</returns>
        public readonly int SetOwner(UnityEngine.Object owner) => PhysicsWorld_SetOwner(this, owner);

        /// <summary>
        /// Get the owner object associated with this world as specified using <see cref="PhysicsWorld.SetOwner(UnityEngine.Object)"/>.
        /// </summary>
        /// <returns>The owner object associated with this world or NULL if no owner has been specified.</returns>
        public readonly UnityEngine.Object GetOwner() => PhysicsWorld_GetOwner(this);

        /// <summary>
        /// Get if the world is owned.
        /// See <see cref="PhysicsWorld.SetOwner(UnityEngine.Object)"/>.
        /// </summary>
        public readonly bool isOwned => PhysicsWorld_IsOwned(this);

        /// <summary>
        /// Get/Set <see cref="PhysicsUserData"/> that can be used for any purpose.
        /// This cannot be set on the <see cref="PhysicsWorld.defaultWorld"/> and will always be at the default.
        /// The physics system doesn't use this data, it is entirely for custom use.
        /// </summary>
        public readonly PhysicsUserData userData { get => PhysicsWorld_GetUserData(this); set => PhysicsWorld_SetUserData(this, value); }

        /// <summary>
        /// Get <see cref="PhysicsUserData"/> that can be used for any purpose, typically by the owner only.
        /// </summary>
        public readonly PhysicsUserData ownerUserData { get => PhysicsWorld_GetOwnerUserData(this); }

        /// <summary>
        /// Set <see cref="PhysicsUserData"/> that can be used for any purpose, typically by the owner only.
        /// </summary>
        /// <param name="physicsUserData">The user data to set.</param>
        /// <param name="ownerKey">Optional owner key returned when using <see cref="PhysicsWorld.SetOwner(UnityEngine.Object)"/>.</param>
        public readonly void SetOwnerUserData(PhysicsUserData physicsUserData, int ownerKey = 0) => PhysicsWorld_SetOwnerUserData(this, physicsUserData, ownerKey);

        /// <summary>
        /// Reset the world to a canonical state so that it will reproduce identical results each time. The world must be empty for this to be called otherwise a warning is produced.
        /// </summary>
        public readonly void Reset() => PhysicsWorld_Reset(this);

        /// <summary>
        /// Check if the world is valid.
        /// </summary>
        public readonly bool isValid => PhysicsWorld_IsValid(this);

        /// <summary>
        /// Check if the world is empty as defined by having no bodies, shapes or joints.
        /// </summary>
        public readonly bool isEmpty => PhysicsWorld_IsEmpty(this);

        /// <summary>
        /// Check if this is the default <see cref="PhysicsWorld"/>.
        /// The default world is automatically created at start-up.
        /// </summary>
        public readonly bool isDefaultWorld => PhysicsWorld_IsDefaultWorld(this);

        /// <summary>
        /// Get/Set if the world is paused. When paused, any simulation attempted will be ignored whether it be automatic or manual.
        /// </summary>
        public readonly bool paused { get => PhysicsWorld_GetPaused(this); set => PhysicsWorld_SetPaused(this, value); }

        /// <summary>
        /// Controls if bodies go to sleep when not moving and not interacting.
        /// Sleeping can provide a significant performance improvement when many Dynamic or Kinematic bodies are in the world.
        /// </summary>
        public readonly bool sleepingAllowed { get => PhysicsWorld_GetSleepingAllowed(this); set => PhysicsWorld_SetSleepingAllowed(this, value); }

        /// <summary>
        /// Controls if continuous collision detection will be used between Dynamic and Static bodies.
        /// Generally you should keep continuous collision enabled to prevent fast moving objects from going through Static objects.
        /// The performance gain from disabling continuous collision is minor.
        /// </summary>
        public readonly bool continuousAllowed { get => PhysicsWorld_GetContinuousAllowed(this); set => PhysicsWorld_SetContinuousAllowed(this, value); }

        /// <summary>
        /// Controls if contact filter callbacks will be called.
        /// A contact filter callback allows direct control over whether a contact will be created between a pair of shapes.
        /// This applies to both triggers and non-triggers but only with Dynamic bodies.
        /// These are relatively expensive so disabling them can provide a significant performance benefit.
        /// A contact filter callback will call the <see cref="PhysicsShape.callbackTarget"/> for both shapes involved if they implement <see cref="PhysicsCallbacks.IContactFilterCallback"/>.
        /// </summary>
        public readonly bool contactFilterCallbacks { get => PhysicsWorld_GetContactFilterCallbacks(this); set => PhysicsWorld_SetContactFilterCallbacks(this, value); }

        /// <summary>
        /// Controls if pre-solve callbacks will be called.
        /// This only applies to Dynamic bodies and is ignored for triggers.
        /// These are relatively expensive so disabling them can provide a significant performance benefit.
        /// A pre-solve callback will call the <see cref="PhysicsShape.callbackTarget"/> for both shapes involved if they implement <see cref="PhysicsCallbacks.IPreSolveCallback"/>.
        /// </summary>
        public readonly bool preSolveCallbacks { get => PhysicsWorld_GetPreSolveCallbacks(this); set => PhysicsWorld_SetPreSolveCallbacks(this, value); }

        /// <summary>
        /// Controls if body update callback targets are automatically called.
        /// See <see cref="PhysicsWorld.SendBodyUpdateCallbacks"/>
        /// </summary>
        public readonly bool autoBodyUpdateCallbacks { get => PhysicsWorld_GetAutoBodyUpdateCallbacks(this); set => PhysicsWorld_SetAutoBodyUpdateCallbacks(this, value); }

        /// <summary>
        /// Controls if shape contact callback targets are automatically called.
        /// See <see cref="PhysicsWorld.SendContactCallbacks"/>
        /// </summary>
        public readonly bool autoContactCallbacks { get => PhysicsWorld_GetAutoContactCallbacks(this); set => PhysicsWorld_SetAutoContactCallbacks(this, value); }

        /// <summary>
        /// Controls if shape trigger callback targets are automatically called.
        /// See <see cref="PhysicsWorld.SendTriggerCallbacks"/>
        /// </summary>
        public readonly bool autoTriggerCallbacks { get => PhysicsWorld_GetAutoTriggerCallbacks(this); set => PhysicsWorld_SetAutoTriggerCallbacks(this, value); }

        /// <summary>
        /// Controls if joint threshold callback targets are automatically called.
        /// See <see cref="PhysicsWorld.SendJointThresholdCallbacks"/>
        /// </summary>
        public readonly bool autoJointThresholdCallbacks { get => PhysicsWorld_GetAutoJointThresholdCallbacks(this); set => PhysicsWorld_SetAutoJointThresholdCallbacks(this, value); }

        /// <summary>
        /// Is warm-starting allowed in the world?
        /// Disabling warming-starting will severely impact stability. This is typically used for testing only!
        /// </summary>
        public readonly bool warmStartingAllowed { get => PhysicsWorld_GetWarmStartingAllowed(this); set => PhysicsWorld_SetWarmStartingAllowed(this, value); }

        /// <summary>
        /// Adjust the bounce threshold, usually in meters per second. It is recommended not to make this value very small because it will prevent bodies from sleeping.
        /// </summary>
        public readonly float bounceThreshold { get => PhysicsWorld_GetBounceThreshold(this); set => PhysicsWorld_SetBounceThreshold(this, value); }

        /// <summary>
        /// The contact hit event threshold controls the collision speed needed to generate a contact hit event, usually in meters per second.
        /// See <see cref="PhysicsEvents.ContactHitEvent"/>.
        /// </summary>
        public readonly float contactHitEventThreshold { get => PhysicsWorld_GetContactHitEventThreshold(this); set => PhysicsWorld_SetContactHitEventThreshold(this, value); }

        /// <summary>
        /// The contact stiffness, in cycles per second.
        /// </summary>
        public readonly float contactFrequency { get => PhysicsWorld_GetContactFrequency(this); set => PhysicsWorld_SetContactFrequency(this, value); }

        /// <summary>
        /// The contact bounciness with 1 being critical damping (non-dimensional).
        /// </summary>
        public readonly float contactDamping { get => PhysicsWorld_GetContactDamping(this); set => PhysicsWorld_SetContactDamping(this, value); }

        /// <summary>
        /// The contact speed used to solve overlaps, in meters per second.
        /// </summary>
        public readonly float contactSpeed { get => PhysicsWorld_GetContactSpeed(this); set => PhysicsWorld_SetContactSpeed(this, value); }

        /// <summary>
        /// Get/Set the maximum linear speed.
        /// </summary>
        public readonly float maximumLinearSpeed { get => PhysicsWorld_GetMaximumLinearSpeed(this); set => PhysicsWorld_SetMaximumLinearSpeed(this, value); }

        /// <summary>
        /// Get/Set the gravity vector applied to all bodies in the world, usually in m/s^2.
        /// </summary>
        public readonly Vector2 gravity { get => PhysicsWorld_GetGravity(this); set => PhysicsWorld_SetGravity(this, value); }

        /// <summary>
        /// Get/Set the simulation worker count for the world.
        /// The actual quantity of workers used will always be capped to those available on the current device and reading the property will return the number of workers actually being used by the device.
        /// Changing the worker count continuously is not recommend and will impact performance as it requires the task queue be recreated.
        /// See <see cref="PhysicsWorldDefinition.simulationWorkers"/>.
        /// </summary>
        public readonly int simulationWorkers { get => PhysicsWorld_GetSimulationWorkers(this); set => PhysicsWorld_SetSimulationWorkers(this, value); }

        /// <summary>
        /// Get/Set the simulation type which controls when or if the simulation will be automatically simulated.
        /// See <see cref="PhysicsWorld.SimulationType"/>.
        /// </summary>
        public readonly SimulationType simulationType { get => PhysicsWorld_GetSimulationType(this); set => PhysicsWorld_SetSimulationType(this, value); }

        /// <summary>
        /// Get/Set the simulation sub-steps to use during simulation.
        /// See <see cref="PhysicsWorld.Simulate(float)"/>.
        /// </summary>
        public readonly int simulationSubSteps { get => PhysicsWorld_GetSimulationSubSteps(this); set => PhysicsWorld_SetSimulationSubSteps(this, value); }

        /// <summary>
        /// Get the timestamp when the last simulation was run.
        /// </summary>
        public readonly double lastSimulationTimestamp => PhysicsWorld_GetLastSimulationTimestamp(this);

        /// <summary>
        /// Get the delta-time used for the last simulation run.
        /// </summary>
        public readonly float lastSimulationDeltaTime => PhysicsWorld_GetLastSimulationDeltaTime(this);

        /// <summary>
        /// Controls the transform plane that the world uses when writing transforms.
        /// See <see cref="PhysicsWorld.transformWriteMode"/>.
        /// </summary>
        public readonly TransformPlane transformPlane { get => PhysicsWorld_GetTransformPlane(this); set => PhysicsWorld_SetTransformPlane(this, value); }

        /// <summary>
        /// Controls the transformation for the <see cref="PhysicsWorld.TransformPlane.Custom"/> to allow transformation writing and reading to/from a custom 2D plane.
        /// See <see cref="PhysicsWorld.TransformPlaneCustom"/>.
        /// </summary>
        public readonly TransformPlaneCustom transformPlaneCustom { get => PhysicsWorld_GetTransformPlaneCustom(this); set => PhysicsWorld_SetTransformPlaneCustom(this, value); }

        /// <summary>
        /// Controls how transform writing is handled.
        /// Only bodies that have their <see cref="PhysicsBody.transformWriteMode"/> active and produce a <see cref="PhysicsEvents.BodyUpdateEvent"/> will write to a transform.
        /// See <see cref="PhysicsWorld.TransformWriteMode"/>.
        /// </summary>
        public readonly TransformWriteMode transformWriteMode { get => PhysicsWorld_GetTransformWriteMode(this); set => PhysicsWorld_SetTransformWriteMode(this, value); }

        /// <summary>
        /// Get/Set the custom <see cref="System.Object"/> that implements the <see cref="PhysicsCallbacks.ITransformWriteCallback"/> to which <see cref="PhysicsEvents.TransformWriteEvent"/> and <see cref="PhysicsEvents.TransformTweenWriteEvent"/> will be sent.
        /// The callback will only occur if <see cref="PhysicsWorld.transformWriteMode"/> is set to <see cref="PhysicsWorld.TransformWriteMode.Custom"/> and there are <see cref="PhysicsWorld.bodyUpdateEvents"/> available.
        /// The object assigned here will be kept alive, not allowing the GC to dispose of it.
        /// To remove the object assigned here, set the callback target to NULL.
        /// </summary>
        public readonly System.Object transformWriteCallbackTarget { get => PhysicsWorld_GetTransformWriteCallbackTarget(this); set => PhysicsWorld_SetTransformWriteCallbackTarget(this, value); }

        /// <summary>
        /// Controls if and how Transform tweens are calculated and/or written.
        /// Transform tweening is where bodies that have their <see cref="PhysicsBody.transformObject"/> set, write to the <see cref="UnityEngine.Transform"/> each frame
        /// depending on the specific body <see cref="PhysicsBody.TransformWriteMode"/> set.
        /// Regardless of this setting, Transform tweening is never used if the <see cref="PhysicsWorld.simulationType"/> is <see cref="PhysicsWorld.SimulationType.Update"/> or <see cref="PhysicsWorld.transformWriteMode"/> is <see cref="PhysicsWorld.TransformWriteMode.Off"/>.
        /// </summary>
        public readonly TransformTweenMode transformTweenMode { get => PhysicsWorld_GetTransformTweenMode(this); set => PhysicsWorld_SetTransformTweenMode(this, value); }

        /// <summary>
        /// Controls if an extra write pass prior to the script fixed-update callback is made for any interpolation tweens to ensure that transforms are synchronized to the final body pose.
        /// Because this is an extra write pass, it has an impact on overall performance so only enable if you require transforms synchronized this way.
        ///
        /// NOTE: This only affects <see cref="PhysicsBody"/> that have their <see cref="PhysicsBody.transformWriteMode"/> set to <see cref="PhysicsBody.TransformWriteMode.Interpolate"/>.
        /// </summary>
        public readonly bool syncInterpolation { get => PhysicsWorld_GetSyncInterpolation(this); set => PhysicsWorld_SetSyncInterpolation(this, value); }

        /// <summary>
        /// Clear all the existing Transform Write Tweens.
        /// See <see cref="PhysicsBody.TransformWriteTween"/> and <see cref="PhysicsBody.TransformWriteMode"/>.
        /// </summary>
        internal readonly void ClearTransformWriteTweens() => PhysicsWorld_ClearTransformWriteTweens(this);

        /// <summary>
        /// Sets all the Transform Write Tweens to be handled per-frame.
        /// See <see cref="PhysicsBody.TransformWriteTween"/> and <see cref="PhysicsBody.TransformWriteMode"/>.
        /// </summary>
        /// <param name="transformWriteTweens">The new transform write tweens to be used.</param>
        internal readonly void SetTransformWriteTweens(ReadOnlySpan<PhysicsBody.TransformWriteTween> transformWriteTweens) => PhysicsWorld_SetTransformWriteTweens(this, transformWriteTweens);

        /// <summary>
        /// Gets all the existing Transform Write Tweens that are handled per-frame.
        /// If the <see cref="PhysicsWorld.transformTweenMode"/> is <see cref="PhysicsWorld.TransformTweenMode.Sequential"/> then the tweens are sorted into ascending transform depth allowing writing to the Transform hierarchy by simply iterating the tweens .
        /// If the <see cref="PhysicsWorld.transformTweenMode"/> is <see cref="PhysicsWorld.TransformTweenMode.Sequential"/> then the tweens are unsorted as a <see cref="UnityEngine.Jobs.TransformAccessArray"/> is used to write them.
        /// See <see cref="PhysicsBody.TransformWriteTween"/> and <see cref="PhysicsBody.TransformWriteMode"/>.
        /// </summary>
        /// <returns>All the existing Transform Write Tweens that are handled per-frame.</returns>
        public readonly NativeArray<PhysicsBody.TransformWriteTween> GetTransformWriteTweens() => PhysicsWorld_GetTransformWriteTweens(this).ToNativeArray<PhysicsBody.TransformWriteTween>();

        /// <summary>
        /// Simulate the world.
        /// If <paramref name="deltaTime"/> is zero then only contact and trigger events will be updated and no velocity or position integration or constraint updates will occur.
        /// </summary>
        /// <param name="deltaTime">The amount of time to forward simulate the world.</param>
        public unsafe readonly void Simulate(float deltaTime)
        {
            var world = this;
            PhysicsWorld_Simulate(new ReadOnlySpan<PhysicsWorld>(&world, 1), deltaTime);
        }

        /// <summary>
        /// Simulate a batch of worlds.
        /// If <paramref name="deltaTime"/> is zero then only contact and trigger events will be updated and no velocity or position integration or constraint updates will occur.
        /// The worlds can be simulated concurrently depending on the setting of <see cref="PhysicsCoreSettings2D.concurrentSimulations"/>.
        /// </summary>
        /// <param name="worlds">The worlds to forward simulate.</param>
        /// <param name="deltaTime">The amount of time to forward simulate the world.</param>
        public static void Simulate(ReadOnlySpan<PhysicsWorld> worlds, float deltaTime) => PhysicsWorld_Simulate(worlds, deltaTime);

        #region Explode

        /// <summary>
        /// Used to define the parameters when using <see cref="PhysicsWorld.Explode(PhysicsWorld.ExplosionDefinition)"/>.
        /// </summary>
        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        public struct ExplosionDefinition
        {
            /// <summary>
            /// Create a default explode definition.
            /// </summary>
            public static ExplosionDefinition defaultDefinition => PhysicsWorld_GetDefaultExplosionDefinition();

            /// <summary>
            /// Create a default explode definition.
            /// </summary>
            public ExplosionDefinition() { this = PhysicsWorld_GetDefaultExplosionDefinition(); }

            /// <summary>
            /// The categories that will produce hits.
            /// </summary>
            public PhysicsMask hitCategories { readonly get => m_HitCategories; set => m_HitCategories = value; }

            /// <summary>
            /// The center of the explosion in world space.
            /// </summary>
            public Vector2 position { readonly get => m_Position; set => m_Position = value; }

            /// <summary>
            /// The radius of the explosion.
            /// </summary>
            public float radius { readonly get => m_Radius; set => m_Radius = Mathf.Max(0f, value); }

            /// <summary>
            /// The falloff distance beyond the radius. Impulse is reduced to zero at this distance.
            /// </summary>
            public float falloff { readonly get => m_Falloff; set => m_Falloff = Mathf.Max(0f, value); }

            /// <summary>
            /// Impulse per unit length. This applies an impulse according to the shape perimeter that is facing the explosion.
            /// Explosions only apply to circles, capsules, and polygons.
            /// This may be negative for implosions.
            /// </summary>
            public float impulsePerLength { readonly get => m_ImpulsePerLength; set => m_ImpulsePerLength = value; }

            #region Internal

            [SerializeField] PhysicsMask m_HitCategories;
            [SerializeField] Vector2 m_Position;
            [SerializeField] [Min(0.0f)] float m_Radius;
            [SerializeField] [Min(0.0f)] float m_Falloff;
            [SerializeField] float m_ImpulsePerLength;

            #endregion
        }

        /// <summary>
        /// Apply a radial explosion applying impulses away from the position to all bodies found within in the radius.
        /// </summary>
        /// <param name="definition">The explosion definition describing how the explosion should be handled.</param>
        public readonly void Explode(ExplosionDefinition definition) => PhysicsWorld_Explode(this, definition);

        #endregion

        #region PhysicsEvents

        /// <summary>
        /// Get all <see cref="PhysicsBody.userData"/> assigned to each <see cref="PhysicsBody"/> returned with <see cref="PhysicsWorld.bodyUpdateEvents"/>.
        /// The Native Array returned will be of the same length and be ordered the same as the <see cref="PhysicsEvents.BodyUpdateEvent"/> returned with <see cref="PhysicsWorld.bodyUpdateEvents"/>.
        /// Any <see cref="PhysicsBody"/> that are not valid will return a default <see cref="PhysicsUserData"/>.
        /// </summary>
        /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
        /// <returns>A Native Array containing all <see cref="PhysicsUserData"/> for each <see cref="PhysicsEvents.BodyUpdateEvent"/> returned with <see cref="PhysicsWorld.bodyUpdateEvents"/>.</returns>
        public NativeArray<PhysicsUserData> GetBodyUpdateUserData(Allocator allocator = Unity.Collections.Allocator.Temp) => PhysicsWorld_GetBodyUpdateUserData(this, false, allocator).ToNativeArray<PhysicsUserData>();

        /// <summary>
        /// Get all <see cref="PhysicsBody.ownerUserData"/> assigned to each <see cref="PhysicsBody"/> returned with <see cref="PhysicsWorld.bodyUpdateEvents"/>.
        /// The Native Array returned will be of the same length and be ordered the same as the <see cref="PhysicsEvents.BodyUpdateEvent"/> returned with <see cref="PhysicsWorld.bodyUpdateEvents"/>.
        /// Any <see cref="PhysicsBody"/> that are not valid will return a default <see cref="PhysicsUserData"/>.
        /// </summary>
        /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
        /// <returns>A Native Array containing all <see cref="PhysicsUserData"/> for each <see cref="PhysicsEvents.BodyUpdateEvent"/> returned with <see cref="PhysicsWorld.bodyUpdateEvents"/>.</returns>
        public NativeArray<PhysicsUserData> GetBodyUpdateOwnerUserData(Allocator allocator = Unity.Collections.Allocator.Temp) => PhysicsWorld_GetBodyUpdateUserData(this, true, allocator).ToNativeArray<PhysicsUserData>();

        /// <summary>
        /// Get the body events from the last simulation.
        /// The <see cref="PhysicsBody"/> objects returned should be checked to see if they are valid before accessing as they may have been deleted since this event was produced (see <see cref="PhysicsBody.isValid"/>).
        /// Any change to the world state can invalidate this data so referring to this data afterwards may cause an unavoidable crash!
        /// You must immediately extract what information you need and not directly reference the returned data as it will be cleared immediately after being provided.
        /// See <see cref="PhysicsEvents.BodyUpdateEvent"/>.
        /// </summary>
        public readonly ReadOnlySpan<PhysicsEvents.BodyUpdateEvent> bodyUpdateEvents => PhysicsWorld_GetBodyUpdateEvents(this).ToReadOnlySpan<PhysicsEvents.BodyUpdateEvent>();

        /// <summary>
        /// Get the trigger begin events from the last simulation.
        /// The <see cref="PhysicsShape"/> objects returned should be checked to see if they are valid before accessing as they may have been deleted since this event was produced (see <see cref="PhysicsShape.isValid"/>).
        /// Any change to the world state can invalidate this data so referring to this data afterwards may cause an unavoidable crash!
        /// You must immediately extract what information you need and not directly reference the returned data as it will be cleared immediately after being provided.
        /// See <see cref="PhysicsEvents.TriggerBeginEvent"/>.
        /// </summary>
        public readonly ReadOnlySpan<PhysicsEvents.TriggerBeginEvent> triggerBeginEvents => PhysicsWorld_GetTriggerBeginEvents(this).ToReadOnlySpan<PhysicsEvents.TriggerBeginEvent>();

        /// <summary>
        /// Get the trigger end events from the last simulation.
        /// The <see cref="PhysicsShape"/> objects returned should be checked to see if they are valid before accessing as they may have been deleted since this event was produced (see <see cref="PhysicsShape.isValid"/>).
        /// Any change to the world state can invalidate this data so referring to this data afterwards may cause an unavoidable crash!
        /// You must immediately extract what information you need and not directly reference the returned data as it will be cleared immediately after being provided.
        /// See <see cref="PhysicsEvents.TriggerEndEvent"/>.
        /// </summary>
        public readonly ReadOnlySpan<PhysicsEvents.TriggerEndEvent> triggerEndEvents => PhysicsWorld_GetTriggerEndEvents(this).ToReadOnlySpan<PhysicsEvents.TriggerEndEvent>();

        /// <summary>
        /// Get the contact begin events from the last simulation.
        /// The <see cref="PhysicsShape"/> objects returned should be checked to see if they are valid before accessing as they may have been deleted since this event was produced (see <see cref="PhysicsShape.isValid"/>).
        /// The <see cref="PhysicsShape.Contact"/> objects returned should be checked to see if they are valid before accessing as they may have been deleted since this event was produced.
        /// Any change to the world state can invalidate this data so referring to this data afterwards may cause an unavoidable crash!
        /// You must immediately extract what information you need and not directly reference the returned data as it will be cleared immediately after being provided.
        /// See <see cref="PhysicsEvents.ContactBeginEvent"/>.
        /// </summary>
        public readonly ReadOnlySpan<PhysicsEvents.ContactBeginEvent> contactBeginEvents => PhysicsWorld_GetContactBeginEvents(this).ToReadOnlySpan<PhysicsEvents.ContactBeginEvent>();

        /// <summary>
        /// Get the contact end events from the last simulation.
        /// The <see cref="PhysicsShape"/> objects returned should be checked to see if they are valid before accessing as they may have been deleted since this event was produced (see <see cref="PhysicsShape.isValid"/>).
        /// The <see cref="PhysicsShape.Contact"/> objects returned should be checked to see if they are valid before accessing as they may have been deleted since this event was produced.
        /// Any change to the world state can invalidate this data so referring to this data afterwards may cause an unavoidable crash!
        /// You must immediately extract what information you need and not directly reference the returned data as it will be cleared immediately after being provided.
        /// See <see cref="PhysicsEvents.ContactEndEvent"/>.
        /// </summary>
        public readonly ReadOnlySpan<PhysicsEvents.ContactEndEvent> contactEndEvents => PhysicsWorld_GetContactEndEvents(this).ToReadOnlySpan<PhysicsEvents.ContactEndEvent>();

        /// <summary>
        /// Get the contact hit events from the last simulation.
        /// The <see cref="PhysicsShape"/> objects returned should be checked to see if they are valid before accessing as they may have been deleted since this event was produced (see <see cref="PhysicsShape.isValid"/>).
        /// Any change to the world state can invalidate this data so referring to this data afterwards may cause an unavoidable crash!
        /// You must immediately extract what information you need and not directly reference the returned data as it will be cleared immediately after being provided.
        /// See <see cref="PhysicsEvents.ContactHitEvent"/>.
        /// </summary>
        public readonly ReadOnlySpan<PhysicsEvents.ContactHitEvent> contactHitEvents => PhysicsWorld_GetContactHitEvents(this).ToReadOnlySpan<PhysicsEvents.ContactHitEvent>();

        /// <summary>
        /// Get the joint events from the last simulation.
        /// An event is produced by a Joint which exceeds either its <see cref="PhysicsJoint.forceThreshold"/> or <see cref="PhysicsJoint.torqueThreshold"/>.
        /// The <see cref="PhysicsJoint"/> objects returned should be checked to see if they are valid before accessing as they may have been deleted since this event was produced (see <see cref="PhysicsJoint.isValid"/>).
        /// Any change to the world state can invalidate this data so referring to this data afterwards may cause an unavoidable crash!
        /// You must immediately extract what information you need and not directly reference the returned data as it will be cleared immediately after being provided.
        /// See <see cref="PhysicsEvents.JointThresholdEvent"/>.
        /// </summary>
        public readonly ReadOnlySpan<PhysicsEvents.JointThresholdEvent> jointThresholdEvents => PhysicsWorld_GetJointThresholdEvents(this).ToReadOnlySpan<PhysicsEvents.JointThresholdEvent>();

        #endregion

        #region Target Callbacks

        /// <summary>
        /// Send all callbacks to targets:
        /// 
        ///- <see cref="PhysicsWorld.SendBodyUpdateCallbacks"/>
        ///- <see cref="PhysicsWorld.SendTriggerCallbacks"/>
        ///- <see cref="PhysicsWorld.SendContactCallbacks"/>
        ///- <see cref="PhysicsWorld.SendJointThresholdCallbacks"/>
        /// </summary>
        public readonly void SendAllCallbacks()
        {
            SendBodyUpdateCallbacks();
            SendTriggerCallbacks();
            SendContactCallbacks();
            SendJointThresholdCallbacks();
        }

        /// <summary>
        /// Send all current <see cref="PhysicsWorld.bodyUpdateEvents"/> where the <see cref="PhysicsBody"/> involved are valid (see <see cref="PhysicsBody.isValid"/>) and have a callback target assigned (see <see cref="PhysicsBody.callbackTarget"/>).
        /// Only callback targets that implement <see cref="PhysicsCallbacks.IBodyUpdateCallback"/> will be called.
        /// This will be called automatically if <see cref="PhysicsWorld.autoBodyUpdateCallbacks"/> is true.
        /// This must be called on the main thread.
        /// </summary>
        public readonly void SendBodyUpdateCallbacks()
        {
            using var callbackTargets = PhysicsWorld_GetBodyUpdateCallbackTargets(this, Allocator.Temp);

            // Send threshold callbacks.
            foreach(var target in callbackTargets.bodyUpdateCallbackTargets)
            {
                target.bodyTarget?.OnBodyUpdate2D(target.bodyUpdateEvent);
            }
        }

        /// <summary>
        /// Send all current <see cref="PhysicsWorld.contactBeginEvents"/> and <see cref="PhysicsWorld.contactEndEvents"/> where either of the <see cref="PhysicsShape"/> involved are valid (see <see cref="PhysicsShape.isValid"/>) and have a callback target assigned (see <see cref="PhysicsShape.callbackTarget"/>).
        /// These events will only be created if both of the shape pairs has <see cref="PhysicsShape.contactEvents"/> set to true.
        /// Only callback targets that implement <see cref="PhysicsCallbacks.IContactCallback"/> will be called.
        /// This will be called automatically if <see cref="PhysicsWorld.autoContactCallbacks"/> is true.
        /// This must be called on the main thread.
        /// </summary>
        public readonly void SendContactCallbacks()
        {
            using var callbackTargets = PhysicsWorld_GetContactCallbackTargets(this, Allocator.Temp);

            // Send begin callbacks.
            foreach(var target in callbackTargets.BeginCallbackTargets)
            {
                target.shapeTargetA?.OnContactBegin2D(target.beginEvent);
                target.shapeTargetB?.OnContactBegin2D(target.beginEvent);
            }

            // Send end callbacks.
            foreach(var target in callbackTargets.EndCallbackTargets)
            {
                target.shapeTargetA?.OnContactEnd2D(target.endEvent);
                target.shapeTargetB?.OnContactEnd2D(target.endEvent);
            }
        }

        /// <summary>
        /// Send all current <see cref="PhysicsWorld.triggerBeginEvents"/> and <see cref="PhysicsWorld.triggerEndEvents"/> where either of the <see cref="PhysicsShape"/> involved are valid (see <see cref="PhysicsShape.isValid"/>) and have a callback target assigned (see <see cref="PhysicsShape.callbackTarget"/>).
        /// These events will only be created if one of the shape pairs has <see cref="PhysicsShape.triggerEvents"/> set to true.
        /// Only callback targets that implement <see cref="PhysicsCallbacks.ITriggerCallback"/> will be called.
        /// This will be called automatically if <see cref="PhysicsWorld.autoTriggerCallbacks"/> is true.
        /// This must be called on the main thread.
        /// </summary>
        public readonly void SendTriggerCallbacks()
        {
            using var callbackTargets = PhysicsWorld_GetTriggerCallbackTargets(this, Allocator.Temp);

            // Send begin callbacks.
            foreach(var target in callbackTargets.BeginCallbackTargets)
            {
                target.triggerShapeTarget?.OnTriggerBegin2D(target.beginEvent);
                target.visitorShapeTarget?.OnTriggerBegin2D(target.beginEvent);
            }

            // Send end callbacks.
            foreach(var target in callbackTargets.EndCallbackTargets)
            {
                target.triggerShapeTarget?.OnTriggerEnd2D(target.endEvent);
                target.visitorShapeTarget?.OnTriggerEnd2D(target.endEvent);
            }
        }

        /// <summary>
        /// Send all current <see cref="PhysicsWorld.jointThresholdEvents"/> where the <see cref="PhysicsJoint"/> involved are valid (see <see cref="PhysicsJoint.isValid"/>) and have a callback target assigned (see <see cref="PhysicsJoint.callbackTarget"/>).
        /// These events will only be created if the joint exceeds its <see cref="PhysicsJoint.forceThreshold"/> or <see cref="PhysicsJoint.torqueThreshold"/>.
        /// Only callback targets that implement <see cref="PhysicsCallbacks.IJointThresholdCallback"/> will be called.
        /// This will be called automatically if <see cref="PhysicsWorld.autoJointThresholdCallbacks"/> is true.
        /// This must be called on the main thread.
        /// </summary>
        public readonly void SendJointThresholdCallbacks()
        {
            using var callbackTargets = PhysicsWorld_GetJointThresholdCallbackTargets(this, Allocator.Temp);

            // Send threshold callbacks.
            foreach(var target in callbackTargets.jointThresholdCallbackTargets)
            {
                target.jointTarget?.OnJointThreshold2D(target.jointThresholdEvent);
            }
        }

        /// <summary>
        /// Get all current <see cref="PhysicsWorld.bodyUpdateEvents"/> where either of the <see cref="PhysicsBody"/> involved are valid (see <see cref="PhysicsBody.isValid"/>) and have a callback target assigned (see <see cref="PhysicsBody.callbackTarget"/>).
        /// </summary>
        /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
        /// <returns>The contact callback target results. This must be disposed of after use otherwise leaks will occur. The exception to this is if there are no targets returned.</returns>
        public PhysicsCallbacks.BodyUpdateCallbackTargets GetBodyUpdateCallbackTargets(Allocator allocator = Unity.Collections.Allocator.Temp) => PhysicsWorld_GetBodyUpdateCallbackTargets(this, allocator);

        /// <summary>
        /// Get all current <see cref="PhysicsWorld.triggerBeginEvents"/> and <see cref="PhysicsWorld.triggerEndEvents"/> where either of the <see cref="PhysicsShape"/> involved are valid (see <see cref="PhysicsShape.isValid"/>) and have a callback target assigned (see <see cref="PhysicsShape.callbackTarget"/>).
        /// </summary>
        /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
        /// <returns>The trigger callback target results. This must be disposed of after use otherwise leaks will occur. The exception to this is if there are no targets returned.</returns>
        public PhysicsCallbacks.TriggerCallbackTargets GetTriggerCallbackTargets(Allocator allocator = Unity.Collections.Allocator.Temp) => PhysicsWorld_GetTriggerCallbackTargets(this, allocator);

        /// <summary>
        /// Get all current <see cref="PhysicsWorld.contactBeginEvents"/> and <see cref="PhysicsWorld.contactEndEvents"/> where either of the <see cref="PhysicsShape"/> involved are valid (see <see cref="PhysicsShape.isValid"/>) and have a callback target assigned (see <see cref="PhysicsShape.callbackTarget"/>).
        /// </summary>
        /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
        /// <returns>The contact callback target results. This must be disposed of after use otherwise leaks will occur. The exception to this is if there are no targets returned.</returns>
        public PhysicsCallbacks.ContactCallbackTargets GetContactCallbackTargets(Allocator allocator = Unity.Collections.Allocator.Temp) => PhysicsWorld_GetContactCallbackTargets(this, allocator);

        /// <summary>
        /// Get all current <see cref="PhysicsWorld.jointThresholdEvents"/> where either of the <see cref="PhysicsJoint"/> involved are valid (see <see cref="PhysicsJoint.isValid"/>) and have a callback target assigned (see <see cref="PhysicsJoint.callbackTarget"/>).
        /// </summary>
        /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
        /// <returns>The joint callback target results. This must be disposed of after use otherwise leaks will occur. The exception to this is if there are no targets returned.</returns>
        public PhysicsCallbacks.JointThresholdCallbackTargets GetJointThresholdCallbackTargets(Allocator allocator = Unity.Collections.Allocator.Temp) => PhysicsWorld_GetJointThresholdCallbackTargets(this, allocator);

#endregion

        #region Queries

        /// <summary>
        /// Tests if the provided AABB potentially overlaps any shapes.
        /// The overlap is between AABB of shapes in the world therefore it may not result in an exact overlap of any shape itself.
        /// See <see cref="PhysicsAABB"/> and <see cref="PhysicsQuery.QueryFilter"/>.
        /// </summary>
        /// <param name="aabb">The AABB used to check overlap. This must be in world-space.</param>
        /// <param name="filter">The filter to control the result returned.</param>
        /// <returns>If the query overlaps anything.</returns>
        public unsafe readonly bool TestOverlapAABB(PhysicsAABB aabb, PhysicsQuery.QueryFilter filter) => TestOverlapAABB(new ReadOnlySpan<PhysicsAABB>(&aabb, 1), filter);

        /// <summary>
        /// Tests if the provided AABBs potentially overlap any shapes.
        /// The overlap is between AABB of shapes in the world therefore it may not result in an exact overlap of any shape itself.
        /// See <see cref="PhysicsAABB"/> and <see cref="PhysicsQuery.QueryFilter"/>.
        /// </summary>
        /// <param name="aabbs">The AABB used to check overlap. These must be in world-space.</param>
        /// <param name="filter">The filter to control the result returned.</param>
        /// <returns>If the query overlaps anything.</returns>
        public readonly bool TestOverlapAABB(ReadOnlySpan<PhysicsAABB> aabbs, PhysicsQuery.QueryFilter filter) => PhysicsWorld_TestOverlapAABB(this, aabbs, filter);

        /// <summary>
        /// Tests if the provided shape overlaps any shapes.
        /// This first converts the shape to a <see cref="PhysicsShape.ShapeProxy"/> and uses <see cref="PhysicsWorld.TestOverlapShapeProxy(PhysicsShape.ShapeProxy, PhysicsQuery.QueryFilter)"/>.
        /// </summary>
        /// <param name="shape">The shape used to check overlap.</param>
        /// <param name="filter">The filter to control the result returned.</param>
        /// <returns>If the query overlaps anything.</returns>
        /// <exception cref="System.ArgumentException">Thrown if an invalid shape type is used.</exception>
        public readonly bool TestOverlapShape(PhysicsShape shape, PhysicsQuery.QueryFilter filter)
        {
            return shape.shapeType switch
            {
                PhysicsShape.ShapeType.Circle => TestOverlapGeometry(shape.circleGeometry.Transform(shape.body.transform), filter),
                PhysicsShape.ShapeType.Capsule => TestOverlapGeometry(shape.capsuleGeometry.Transform(shape.body.transform), filter),
                PhysicsShape.ShapeType.Polygon => TestOverlapGeometry(shape.polygonGeometry.Transform(shape.body.transform), filter),
                PhysicsShape.ShapeType.Segment => TestOverlapGeometry(shape.segmentGeometry.Transform(shape.body.transform), filter),
                PhysicsShape.ShapeType.ChainSegment => TestOverlapGeometry(shape.chainSegmentGeometry.Transform(shape.body.transform), filter),
                _ => throw new ArgumentException("Invalid shape type used.", nameof(shape)),
            };
        }

        /// <summary>
        /// Test if the provided shape proxy overlaps any shapes.
        /// This first converts the shape to a <see cref="PhysicsShape.ShapeProxy"/> and uses <see cref="PhysicsWorld.TestOverlapShapeProxy(PhysicsShape.ShapeProxy, PhysicsQuery.QueryFilter)"/>.
        /// </summary>
        /// <param name="shapeProxy">The shape proxy to use. This must be in world-space.</param>
        /// <param name="filter">The filter to control the result returned.</param>
        /// <returns>If the query overlaps anything.</returns>
        public unsafe readonly bool TestOverlapShapeProxy(PhysicsShape.ShapeProxy shapeProxy, PhysicsQuery.QueryFilter filter) => TestOverlapShapeProxy(new ReadOnlySpan<PhysicsShape.ShapeProxy>(&shapeProxy, 1), filter);

        /// <summary>
        /// Test if the provided shape proxies overlaps any shapes.
        /// This first converts the shape to a <see cref="PhysicsShape.ShapeProxy"/> and uses <see cref="PhysicsWorld.TestOverlapShapeProxy(PhysicsShape.ShapeProxy, PhysicsQuery.QueryFilter)"/>.
        /// </summary>
        /// <param name="shapeProxies">The shape proxy to use. This must be in world-space.</param>
        /// <param name="filter">The filter to control the result returned.</param>
        /// <returns>If the query overlaps anything.</returns>
        public readonly bool TestOverlapShapeProxy(ReadOnlySpan<PhysicsShape.ShapeProxy> shapeProxies, PhysicsQuery.QueryFilter filter) => PhysicsWorld_TestOverlapShapeProxy(this, shapeProxies, filter);

        /// <summary>
        /// Tests if the provided point overlaps any shapes.
        /// See <see cref="PhysicsQuery.QueryFilter"/>.
        /// </summary>
        /// <param name="point">The point used to check overlap. This must be in world-space.</param>
        /// <param name="filter">The filter to control the result returned.</param>
        /// <returns>If the query overlaps anything.</returns>
        public unsafe readonly bool TestOverlapPoint(Vector2 point, PhysicsQuery.QueryFilter filter) => TestOverlapPoint(new ReadOnlySpan<Vector2>(&point, 1), filter);

        /// <summary>
        /// Tests if the provided point(s) overlap any shapes.
        /// See <see cref="PhysicsQuery.QueryFilter"/>.
        /// </summary>
        /// <param name="points">The points used to check overlap. These must be in world-space.</param>
        /// <param name="filter">The filter to control the result returned.</param>
        /// <returns>If the query overlaps anything.</returns>
        public readonly bool TestOverlapPoint(ReadOnlySpan<Vector2> points, PhysicsQuery.QueryFilter filter) => PhysicsWorld_TestOverlapPoint(this, points, filter);

        /// <summary>
        /// Tests if the provided Circle geometry overlaps any shapes.
        /// A circle with a radius of zero is equivalent to <see cref="PhysicsWorld.TestOverlapPoint(Vector2, PhysicsQuery.QueryFilter)"/>.
        /// See <see cref="CircleGeometry"/> and <see cref="PhysicsQuery.QueryFilter"/>.
        /// </summary>
        /// <param name="geometry">The Circle geometry used to check overlap. This must be in world-space.</param>
        /// <param name="filter">The filter to control the result returned.</param>
        /// <returns>If the query overlaps anything.</returns>
        public unsafe readonly bool TestOverlapGeometry(CircleGeometry geometry, PhysicsQuery.QueryFilter filter) => TestOverlapGeometry(new ReadOnlySpan<CircleGeometry>(&geometry, 1), filter);

        /// <summary>
        /// Tests if the provided Circle geometry overlaps any shapes.
        /// A circle with a radius of zero is equivalent to <see cref="PhysicsWorld.TestOverlapPoint(Vector2, PhysicsQuery.QueryFilter)"/>.
        /// See <see cref="CircleGeometry"/> and <see cref="PhysicsQuery.QueryFilter"/>.
        /// </summary>
        /// <param name="geometry">The Circle geometry used to check overlap. These must be in world-space.</param>
        /// <param name="filter">The filter to control the result returned.</param>
        /// <returns>If the query overlaps anything.</returns>
        public readonly bool TestOverlapGeometry(ReadOnlySpan<CircleGeometry> geometry, PhysicsQuery.QueryFilter filter) => PhysicsWorld_TestOverlapCircleGeometry(this, geometry, filter);

        /// <summary>
        /// Tests if the provided Capsule geometry overlaps any shapes.
        /// See <see cref="CapsuleGeometry"/> and <see cref="PhysicsQuery.QueryFilter"/>.
        /// </summary>
        /// <param name="geometry">The Capsule geometry used to check overlap. This must be in world-space.</param>
        /// <param name="filter">The filter to control the result returned.</param>
        /// <returns>If the query overlaps anything.</returns>
        public unsafe readonly bool TestOverlapGeometry(CapsuleGeometry geometry, PhysicsQuery.QueryFilter filter) => TestOverlapGeometry(new ReadOnlySpan<CapsuleGeometry>(&geometry, 1), filter);

        /// <summary>
        /// Tests if the provided Capsule geometry overlaps any shapes.
        /// See <see cref="CapsuleGeometry"/> and <see cref="PhysicsQuery.QueryFilter"/>.
        /// </summary>
        /// <param name="geometry">The Capsule geometry used to check overlap. These must be in world-space.</param>
        /// <param name="filter">The filter to control the result returned.</param>
        /// <returns>If the query overlaps anything.</returns>
        public readonly bool TestOverlapGeometry(ReadOnlySpan<CapsuleGeometry> geometry, PhysicsQuery.QueryFilter filter) => PhysicsWorld_TestOverlapCapsuleGeometry(this, geometry, filter);

        /// <summary>
        /// Tests if the provided Polygon geometry overlaps any shapes.
        /// See <see cref="PolygonGeometry"/> and <see cref="PhysicsQuery.QueryFilter"/>.
        /// </summary>
        /// <param name="geometry">The Polygon geometry used to check overlap. This must be in world-space.</param>
        /// <param name="filter">The filter to control the result returned.</param>
        /// <returns>If the query overlaps anything.</returns>
        public unsafe readonly bool TestOverlapGeometry(PolygonGeometry geometry, PhysicsQuery.QueryFilter filter) => TestOverlapGeometry(new ReadOnlySpan<PolygonGeometry>(&geometry, 1), filter);

        /// <summary>
        /// Tests if the provided Polygon geometry overlaps any shapes.
        /// See <see cref="PolygonGeometry"/> and <see cref="PhysicsQuery.QueryFilter"/>.
        /// </summary>
        /// <param name="geometry">The Polygon geometry used to check overlap. These must be in world-space.</param>
        /// <param name="filter">The filter to control the result returned.</param>
        /// <returns>If the query overlaps anything.</returns>
        public readonly bool TestOverlapGeometry(ReadOnlySpan<PolygonGeometry> geometry, PhysicsQuery.QueryFilter filter) => PhysicsWorld_TestOverlapPolygonGeometry(this, geometry, filter);

        /// <summary>
        /// Tests if the provided Segment geometry overlaps any shapes.
        /// See <see cref="SegmentGeometry"/> and <see cref="PhysicsQuery.QueryFilter"/>.
        /// </summary>
        /// <param name="geometry">The Segment geometry used to check overlap. This must be in world-space.</param>
        /// <param name="filter">The filter to control the result returned.</param>
        /// <returns>If the query overlaps anything.</returns>
        public unsafe readonly bool TestOverlapGeometry(SegmentGeometry geometry, PhysicsQuery.QueryFilter filter) => TestOverlapGeometry(new ReadOnlySpan<SegmentGeometry>(&geometry, 1), filter);

        /// <summary>
        /// Tests if the provided Segment geometry overlaps any shapes.
        /// See <see cref="SegmentGeometry"/> and <see cref="PhysicsQuery.QueryFilter"/>.
        /// </summary>
        /// <param name="geometry">The Segment geometry used to check overlap. These must be in world-space.</param>
        /// <param name="filter">The filter to control the result returned.</param>
        /// <returns>If the query overlaps anything.</returns>
        public readonly bool TestOverlapGeometry(ReadOnlySpan<SegmentGeometry> geometry, PhysicsQuery.QueryFilter filter) => PhysicsWorld_TestOverlapSegmentGeometry(this, geometry, filter);

        /// <summary>
        /// Tests if the provided Chain-Segment geometry overlaps any shapes.
        /// See <see cref="ChainSegmentGeometry"/> and <see cref="PhysicsQuery.QueryFilter"/>.
        /// </summary>
        /// <param name="geometry">The Chain-Segment geometry used to check overlap. This must be in world-space.</param>
        /// <param name="filter">The filter to control the result returned.</param>
        /// <returns>If the query overlaps anything.</returns>
        public unsafe readonly bool TestOverlapGeometry(ChainSegmentGeometry geometry, PhysicsQuery.QueryFilter filter) => TestOverlapGeometry(new ReadOnlySpan<ChainSegmentGeometry>(&geometry, 1), filter);

        /// <summary>
        /// Tests if the provided Chain-Segment geometry overlaps any shapes.
        /// See <see cref="ChainSegmentGeometry"/> and <see cref="PhysicsQuery.QueryFilter"/>.
        /// </summary>
        /// <param name="geometry">The Chain-Segment geometry used to check overlap. These must be in world-space.</param>
        /// <param name="filter">The filter to control the result returned.</param>
        /// <returns>If the query overlaps anything.</returns>
        public readonly bool TestOverlapGeometry(ReadOnlySpan<ChainSegmentGeometry> geometry, PhysicsQuery.QueryFilter filter) => PhysicsWorld_TestOverlapChainSegmentGeometry(this, geometry, filter);

        /// <summary>
        /// Returns all shapes that potentially overlap the provided AABB.
        /// The overlap is between AABB of shapes in the world therefore it may not result in an exact overlap of the shape itself.
        /// See <see cref="PhysicsAABB"/>, <see cref="PhysicsQuery.QueryFilter"/>, <see cref="PhysicsQuery.WorldOverlapResult"/> and <see cref="Unity.Collections.Allocator"/>.
        /// </summary>
        /// <param name="aabb">The AABB used to check overlap. This must be in world-space.</param>
        /// <param name="filter">The filter to control what results are returned.</param>
        /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
        /// <returns>The query overlap results. This NativeArray must be disposed of after use otherwise leaks will occur. The exception to this is if the array is empty.</returns>
        public unsafe readonly NativeArray<PhysicsQuery.WorldOverlapResult> OverlapAABB(PhysicsAABB aabb, PhysicsQuery.QueryFilter filter, Allocator allocator = Unity.Collections.Allocator.Temp) => OverlapAABB(new ReadOnlySpan<PhysicsAABB>(&aabb, 1), filter, allocator);

        /// <summary>
        /// Returns all shapes that potentially overlap the provided AABBs.
        /// The overlap is between AABB of shapes in the world therefore it may not result in an exact overlap of the shape itself.
        /// See <see cref="PhysicsAABB"/>, <see cref="PhysicsQuery.QueryFilter"/>, <see cref="PhysicsQuery.WorldOverlapResult"/> and <see cref="Unity.Collections.Allocator"/>.
        /// </summary>
        /// <param name="aabbs">The AABBs used to check overlap. These must be in world-space.</param>
        /// <param name="filter">The filter to control what results are returned.</param>
        /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
        /// <returns>The query overlap results. This NativeArray must be disposed of after use otherwise leaks will occur. The exception to this is if the array is empty.</returns>
        public readonly NativeArray<PhysicsQuery.WorldOverlapResult> OverlapAABB(ReadOnlySpan<PhysicsAABB> aabbs, PhysicsQuery.QueryFilter filter, Allocator allocator = Unity.Collections.Allocator.Temp) => PhysicsWorld_OverlapAABB(this, aabbs, filter, allocator).ToNativeArray<PhysicsQuery.WorldOverlapResult>();

        /// <summary>
        /// Returns all shapes that overlap the provided shape.
        /// See <see cref="PolygonGeometry"/>, <see cref="PhysicsQuery.QueryFilter"/>, see cref="Unity.U2D.Physics.PhysicsQuery.WorldOverlapResult"/> and <see cref="Unity.Collections.Allocator"/>.
        /// </summary>
        /// <param name="shape">The shape used to check overlap.</param>
        /// <param name="filter">The filter to control what results are returned.</param>
        /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
        /// <returns>The query overlap results. This NativeArray must be disposed of after use otherwise leaks will occur. The exception to this is if the array is empty.</returns>
        /// <exception cref="System.ArgumentException">Thrown if an invalid shape type is used.</exception>
        public readonly NativeArray<PhysicsQuery.WorldOverlapResult> OverlapShape(PhysicsShape shape, PhysicsQuery.QueryFilter filter, Allocator allocator = Unity.Collections.Allocator.Temp)
        {
            return shape.shapeType switch
            {
                PhysicsShape.ShapeType.Circle => OverlapGeometry(shape.circleGeometry.Transform(shape.body.transform), filter, allocator),
                PhysicsShape.ShapeType.Capsule => OverlapGeometry(shape.capsuleGeometry.Transform(shape.body.transform), filter, allocator),
                PhysicsShape.ShapeType.Polygon => OverlapGeometry(shape.polygonGeometry.Transform(shape.body.transform), filter, allocator),
                PhysicsShape.ShapeType.Segment => OverlapGeometry(shape.segmentGeometry.Transform(shape.body.transform), filter, allocator),
                PhysicsShape.ShapeType.ChainSegment => OverlapGeometry(shape.chainSegmentGeometry.segment.Transform(shape.body.transform), filter, allocator),
                _ => throw new ArgumentException("Invalid shape type used.", nameof(shape)),
            };
        }

        /// <summary>
        /// Returns all shapes that overlap the shape proxy.
        /// See <see cref="PhysicsQuery.QueryFilter"/>. <see cref="PhysicsQuery.WorldOverlapResult"/> and <see cref="Unity.Collections.Allocator"/>.
        /// </summary>
        /// <param name="shapeProxy">The shape proxy to use. This must be in world-space.</param>
        /// <param name="filter">The filter to control what results are returned.</param>
        /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
        /// <returns>The query overlap results. This NativeArray must be disposed of after use otherwise leaks will occur. The exception to this is if the array is empty.</returns>
        public unsafe readonly NativeArray<PhysicsQuery.WorldOverlapResult> OverlapShapeProxy(PhysicsShape.ShapeProxy shapeProxy, PhysicsQuery.QueryFilter filter, Allocator allocator = Unity.Collections.Allocator.Temp) => OverlapShapeProxy(new ReadOnlySpan<PhysicsShape.ShapeProxy>(&shapeProxy, 1), filter, allocator);

        /// <summary>
        /// Returns all shapes that overlap the shape proxies.
        /// See <see cref="PhysicsQuery.QueryFilter"/>. <see cref="PhysicsQuery.WorldOverlapResult"/> and <see cref="Unity.Collections.Allocator"/>.
        /// </summary>
        /// <param name="shapeProxies">The shape proxies to use. These must be in world-space.</param>
        /// <param name="filter">The filter to control what results are returned.</param>
        /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
        /// <returns>The query overlap results. This NativeArray must be disposed of after use otherwise leaks will occur. The exception to this is if the array is empty.</returns>
        public readonly NativeArray<PhysicsQuery.WorldOverlapResult> OverlapShapeProxy(ReadOnlySpan<PhysicsShape.ShapeProxy> shapeProxies, PhysicsQuery.QueryFilter filter, Allocator allocator = Unity.Collections.Allocator.Temp) => PhysicsWorld_OverlapShapeProxy(this, shapeProxies, filter, allocator).ToNativeArray<PhysicsQuery.WorldOverlapResult>();

        /// <summary>
        /// Returns all shapes that overlap the provided point.
        /// This first converts the shape to a <see cref="PhysicsShape.ShapeProxy"/> and uses <see cref="PhysicsWorld.TestOverlapShapeProxy(PhysicsShape.ShapeProxy, PhysicsQuery.QueryFilter)"/>.
        /// See <see cref="PhysicsQuery.QueryFilter"/>, <see cref="PhysicsQuery.WorldOverlapResult"/> and <see cref="Unity.Collections.Allocator"/>.
        /// </summary>
        /// <param name="point">The point used to check overlap. This must be in world-space.</param>
        /// <param name="filter">The filter to control what results are returned.</param>
        /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
        /// <returns>The query overlap results. This NativeArray must be disposed of after use otherwise leaks will occur. The exception to this is if the array is empty.</returns>
        public unsafe readonly NativeArray<PhysicsQuery.WorldOverlapResult> OverlapPoint(Vector2 point, PhysicsQuery.QueryFilter filter, Allocator allocator = Unity.Collections.Allocator.Temp) => OverlapPoint(new ReadOnlySpan<Vector2>(&point, 1), filter, allocator);

        /// <summary>
        /// Returns all shapes that overlap the provided point(s).
        /// This first converts the shape to a <see cref="PhysicsShape.ShapeProxy"/> and uses <see cref="PhysicsWorld.TestOverlapShapeProxy(PhysicsShape.ShapeProxy, PhysicsQuery.QueryFilter)"/>.
        /// See <see cref="PhysicsQuery.QueryFilter"/>, <see cref="PhysicsQuery.WorldOverlapResult"/> and <see cref="Unity.Collections.Allocator"/>.
        /// </summary>
        /// <param name="points">The points used to check overlap. These must be in world-space.</param>
        /// <param name="filter">The filter to control what results are returned.</param>
        /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
        /// <returns>The query overlap results. This NativeArray must be disposed of after use otherwise leaks will occur. The exception to this is if the array is empty.</returns>
        public readonly NativeArray<PhysicsQuery.WorldOverlapResult> OverlapPoint(ReadOnlySpan<Vector2> points, PhysicsQuery.QueryFilter filter, Allocator allocator = Unity.Collections.Allocator.Temp) => PhysicsWorld_OverlapPoint(this, points, filter, allocator).ToNativeArray<PhysicsQuery.WorldOverlapResult>();

        /// <summary>
        /// Returns all shapes that overlap the provided Circle geometry.
        /// A circle with a radius of zero is equivalent to <see cref="PhysicsWorld.OverlapPoint(Vector2, PhysicsQuery.QueryFilter, Allocator)"/>.
        /// See <see cref="CircleGeometry"/>, <see cref="PhysicsQuery.QueryFilter"/>, <see cref="PhysicsQuery.WorldOverlapResult"/> and <see cref="Unity.Collections.Allocator"/>
        /// </summary>
        /// <param name="geometry">The Circle geometry used to check overlap. This must be in world-space.</param>
        /// <param name="filter">The filter to control what results are returned.</param>
        /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
        /// <returns>The query overlap results. This NativeArray must be disposed of after use otherwise leaks will occur. The exception to this is if the array is empty.</returns>
        public unsafe readonly NativeArray<PhysicsQuery.WorldOverlapResult> OverlapGeometry(CircleGeometry geometry, PhysicsQuery.QueryFilter filter, Allocator allocator = Unity.Collections.Allocator.Temp) => OverlapGeometry(new ReadOnlySpan<CircleGeometry>(&geometry, 1), filter, allocator);

        /// <summary>
        /// Returns all shapes that overlap the provided Circle geometry.
        /// A circle with a radius of zero is equivalent to <see cref="PhysicsWorld.OverlapPoint(Vector2, PhysicsQuery.QueryFilter, Allocator)"/>.
        /// See <see cref="CircleGeometry"/>, <see cref="PhysicsQuery.QueryFilter"/>, <see cref="PhysicsQuery.WorldOverlapResult"/> and <see cref="Unity.Collections.Allocator"/>
        /// </summary>
        /// <param name="geometry">The Circle geometry used to check overlap. These must be in world-space.</param>
        /// <param name="filter">The filter to control what results are returned.</param>
        /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
        /// <returns>The query overlap results. This NativeArray must be disposed of after use otherwise leaks will occur. The exception to this is if the array is empty.</returns>
        public readonly NativeArray<PhysicsQuery.WorldOverlapResult> OverlapGeometry(ReadOnlySpan<CircleGeometry> geometry, PhysicsQuery.QueryFilter filter, Allocator allocator = Unity.Collections.Allocator.Temp) => PhysicsWorld_OverlapCircleGeometry(this, geometry, filter, allocator).ToNativeArray<PhysicsQuery.WorldOverlapResult>();

        /// <summary>
        /// Returns all shapes that overlap the provided Capsule geometry.
        /// See <see cref="CapsuleGeometry"/>, <see cref="PhysicsQuery.QueryFilter"/>, <see cref="PhysicsQuery.WorldOverlapResult"/> and  <see cref="Unity.Collections.Allocator"/>.
        /// </summary>
        /// <param name="geometry">The Capsule geometry used to check overlap. This must be in world-space.</param>
        /// <param name="filter">The filter to control what results are returned.</param>
        /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
        /// <returns>The query overlap results. This NativeArray must be disposed of after use otherwise leaks will occur. The exception to this is if the array is empty.</returns>
        public unsafe readonly NativeArray<PhysicsQuery.WorldOverlapResult> OverlapGeometry(CapsuleGeometry geometry, PhysicsQuery.QueryFilter filter, Allocator allocator = Unity.Collections.Allocator.Temp) => OverlapGeometry(new ReadOnlySpan<CapsuleGeometry>(&geometry, 1), filter, allocator);

        /// <summary>
        /// Returns all shapes that overlap the provided Capsule geometry.
        /// See <see cref="CapsuleGeometry"/>, <see cref="PhysicsQuery.QueryFilter"/>, <see cref="PhysicsQuery.WorldOverlapResult"/> and  <see cref="Unity.Collections.Allocator"/>.
        /// </summary>
        /// <param name="geometry">The Capsule geometry used to check overlap. These must be in world-space.</param>
        /// <param name="filter">The filter to control what results are returned.</param>
        /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
        /// <returns>The query overlap results. This NativeArray must be disposed of after use otherwise leaks will occur. The exception to this is if the array is empty.</returns>
        public readonly NativeArray<PhysicsQuery.WorldOverlapResult> OverlapGeometry(ReadOnlySpan<CapsuleGeometry> geometry, PhysicsQuery.QueryFilter filter, Allocator allocator = Unity.Collections.Allocator.Temp) => PhysicsWorld_OverlapCapsuleGeometry(this, geometry, filter, allocator).ToNativeArray<PhysicsQuery.WorldOverlapResult>();

        /// <summary>
        /// Returns all shapes that overlap the provided Polygon geometry.
        /// See <see cref="PolygonGeometry"/>, <see cref="PhysicsQuery.QueryFilter"/>, <see cref="PhysicsQuery.WorldOverlapResult"/> and <see cref="Unity.Collections.Allocator"/>.
        /// </summary>
        /// <param name="geometry">The Polygon geometry used to check overlap. This must be in world-space.</param>
        /// <param name="filter">The filter to control what results are returned.</param>
        /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
        /// <returns>The query overlap results. This NativeArray must be disposed of after use otherwise leaks will occur. The exception to this is if the array is empty.</returns>
        public unsafe readonly NativeArray<PhysicsQuery.WorldOverlapResult> OverlapGeometry(PolygonGeometry geometry, PhysicsQuery.QueryFilter filter, Allocator allocator = Unity.Collections.Allocator.Temp) => OverlapGeometry(new ReadOnlySpan<PolygonGeometry>(&geometry, 1), filter, allocator);

        /// <summary>
        /// Returns all shapes that overlap the provided Polygon geometry.
        /// See <see cref="PolygonGeometry"/>, <see cref="PhysicsQuery.QueryFilter"/>, <see cref="PhysicsQuery.WorldOverlapResult"/> and <see cref="Unity.Collections.Allocator"/>.
        /// </summary>
        /// <param name="geometry">The Polygon geometry used to check overlap. These must be in world-space.</param>
        /// <param name="filter">The filter to control what results are returned.</param>
        /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
        /// <returns>The query overlap results. This NativeArray must be disposed of after use otherwise leaks will occur. The exception to this is if the array is empty.</returns>
        public readonly NativeArray<PhysicsQuery.WorldOverlapResult> OverlapGeometry(ReadOnlySpan<PolygonGeometry> geometry, PhysicsQuery.QueryFilter filter, Allocator allocator = Unity.Collections.Allocator.Temp) => PhysicsWorld_OverlapPolygonGeometry(this, geometry, filter, allocator).ToNativeArray<PhysicsQuery.WorldOverlapResult>();

        /// <summary>
        /// Returns all shapes that overlap the provided Segment geometry.
        /// See <see cref="SegmentGeometry"/>, <see cref="PhysicsQuery.QueryFilter"/>, <see cref="PhysicsQuery.WorldOverlapResult"/> and <see cref="Unity.Collections.Allocator"/>.
        /// </summary>
        /// <param name="geometry">The Segment geometry used to check overlap. This must be in world-space.</param>
        /// <param name="filter">The filter to control what results are returned.</param>
        /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
        /// <returns>The query overlap results. This NativeArray must be disposed of after use otherwise leaks will occur. The exception to this is if the array is empty.</returns>
        public unsafe readonly NativeArray<PhysicsQuery.WorldOverlapResult> OverlapGeometry(SegmentGeometry geometry, PhysicsQuery.QueryFilter filter, Allocator allocator = Unity.Collections.Allocator.Temp) => OverlapGeometry(new ReadOnlySpan<SegmentGeometry>(&geometry, 1), filter, allocator);

        /// <summary>
        /// Returns all shapes that overlap the provided Segment geometry.
        /// See <see cref="SegmentGeometry"/>, <see cref="PhysicsQuery.QueryFilter"/>, <see cref="PhysicsQuery.WorldOverlapResult"/> and <see cref="Unity.Collections.Allocator"/>.
        /// </summary>
        /// <param name="geometry">The Segment geometry used to check overlap. These must be in world-space.</param>
        /// <param name="filter">The filter to control what results are returned.</param>
        /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
        /// <returns>The query overlap results. This NativeArray must be disposed of after use otherwise leaks will occur. The exception to this is if the array is empty.</returns>
        public readonly NativeArray<PhysicsQuery.WorldOverlapResult> OverlapGeometry(ReadOnlySpan<SegmentGeometry> geometry, PhysicsQuery.QueryFilter filter, Allocator allocator = Unity.Collections.Allocator.Temp) => PhysicsWorld_OverlapSegmentGeometry(this, geometry, filter, allocator).ToNativeArray<PhysicsQuery.WorldOverlapResult>();

        /// <summary>
        /// Returns all shapes that overlap the provided Chain-Segment geometry.
        /// See <see cref="ChainSegmentGeometry"/>, <see cref="PhysicsQuery.QueryFilter"/>, <see cref="PhysicsQuery.WorldOverlapResult"/> and <see cref="Unity.Collections.Allocator"/>.
        /// </summary>
        /// <param name="geometry">The Chain-Segment geometry used to check overlap. This must be in world-space.</param>
        /// <param name="filter">The filter to control what results are returned.</param>
        /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
        /// <returns>The query overlap results. This NativeArray must be disposed of after use otherwise leaks will occur. The exception to this is if the array is empty.</returns>
        public unsafe readonly NativeArray<PhysicsQuery.WorldOverlapResult> OverlapGeometry(ChainSegmentGeometry geometry, PhysicsQuery.QueryFilter filter, Allocator allocator = Unity.Collections.Allocator.Temp) => OverlapGeometry(new ReadOnlySpan<ChainSegmentGeometry>(&geometry, 1), filter, allocator);

        /// <summary>
        /// Returns all shapes that overlap the provided Chain-Segment geometry.
        /// See <see cref="ChainSegmentGeometry"/>, <see cref="PhysicsQuery.QueryFilter"/>, <see cref="PhysicsQuery.WorldOverlapResult"/> and <see cref="Unity.Collections.Allocator"/>.
        /// </summary>
        /// <param name="geometry">The Chain-Segment geometry used to check overlap. These must be in world-space.</param>
        /// <param name="filter">The filter to control what results are returned.</param>
        /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
        /// <returns>The query overlap results. This NativeArray must be disposed of after use otherwise leaks will occur. The exception to this is if the array is empty.</returns>
        public readonly NativeArray<PhysicsQuery.WorldOverlapResult> OverlapGeometry(ReadOnlySpan<ChainSegmentGeometry> geometry, PhysicsQuery.QueryFilter filter, Allocator allocator = Unity.Collections.Allocator.Temp) => PhysicsWorld_OverlapChainSegmentGeometry(this, geometry, filter, allocator).ToNativeArray<PhysicsQuery.WorldOverlapResult>();

        /// <summary>
        /// Returns the shape(s) that intersect the specified Ray.
        /// Technically this is a line-segment and not an infinite ray.
        /// See <see cref="PhysicsQuery.QueryFilter"/>, <see cref="PhysicsQuery.WorldCastMode"/>, <see cref="PhysicsQuery.WorldCastResult"/> and <see cref="Unity.Collections.Allocator"/>.
        /// </summary>
        /// <param name="input">The configuration of the ray to cast.</param>
        /// <param name="filter">The filter to control what results are returned.</param>
        /// <param name="castMode">Controls how many and in what order the results are returned.</param>
        /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
        /// <returns>The query cast results. This NativeArray must be disposed of after use otherwise leaks will occur. The exception to this is if the array is empty.</returns>
        public readonly NativeArray<PhysicsQuery.WorldCastResult> CastRay(PhysicsQuery.CastRayInput input, PhysicsQuery.QueryFilter filter, PhysicsQuery.WorldCastMode castMode = PhysicsQuery.WorldCastMode.Closest, Allocator allocator = Unity.Collections.Allocator.Temp) => PhysicsWorld_CastRay(this, input, filter, castMode, allocator).ToNativeArray<PhysicsQuery.WorldCastResult>();

        /// <summary>
        /// Returns the shape(s) that intersect the specified shape as it is cast through the world.
        /// Neither <see cref="PhysicsShape.ShapeType.Segment"/> or <see cref="PhysicsShape.ShapeType.ChainSegment"/> shape types are supported.
        /// See <see cref="PhysicsQuery.QueryFilter"/>, <see cref="PhysicsQuery.WorldCastMode"/>, <see cref="PhysicsQuery.WorldCastResult"/> and <see cref="Unity.Collections.Allocator"/>.
        /// </summary>
        /// <param name="shape">The shape used to cast through the world.</param>
        /// <param name="translation">The translation relative to the shape pose defining the direction the shape geometry will move through the world.</param>
        /// <param name="filter">The filter to control what results are returned.</param>
        /// <param name="castMode">Controls how many and in what order the results are returned.</param>
        /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
        /// <returns>The query cast results. This NativeArray must be disposed of after use otherwise leaks will occur. The exception to this is if the array is empty.</returns>
        /// <exception cref="System.ArgumentException">Thrown if an invalid shape type is used, specifically if <see cref="PhysicsShape.ShapeType.Segment"/> or <see cref="PhysicsShape.ShapeType.ChainSegment"/> shape types are used.</exception>
        public readonly NativeArray<PhysicsQuery.WorldCastResult> CastShape(PhysicsShape shape, Vector2 translation, PhysicsQuery.QueryFilter filter, PhysicsQuery.WorldCastMode castMode = PhysicsQuery.WorldCastMode.Closest, Allocator allocator = Unity.Collections.Allocator.Temp)
        {
            return shape.shapeType switch
            {
                PhysicsShape.ShapeType.Circle => CastGeometry(shape.circleGeometry.Transform(shape.body.transform), translation, filter, castMode, allocator),
                PhysicsShape.ShapeType.Capsule => CastGeometry(shape.capsuleGeometry.Transform(shape.body.transform), translation, filter, castMode, allocator),
                PhysicsShape.ShapeType.Polygon => CastGeometry(shape.polygonGeometry.Transform(shape.body.transform), translation, filter, castMode, allocator),
                _ => throw new ArgumentException("Invalid shape type used for cast.", nameof(shape)),
            };
        }

        /// <summary>
        /// Returns the shape(s) that intersect the specified Circle geometry as it is cast through the world.
        /// See <see cref="PhysicsQuery.QueryFilter"/>, <see cref="PhysicsQuery.WorldCastMode"/>, <see cref="PhysicsQuery.WorldCastResult"/> and <see cref="Unity.Collections.Allocator"/>.
        /// </summary>
        /// <param name="shapeProxy">The shape proxy to use. This must be in world-space.</param>
        /// <param name="translation">The translation relative to the shape proxy defining the direction the shape proxy will move through the world.</param>
        /// <param name="filter">The filter to control what results are returned.</param>
        /// <param name="castMode">Controls how many and in what order the results are returned.</param>
        /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
        /// <returns>The query cast results. This NativeArray must be disposed of after use otherwise leaks will occur. The exception to this is if the array is empty.</returns>
        public readonly NativeArray<PhysicsQuery.WorldCastResult> CastShapeProxy(PhysicsShape.ShapeProxy shapeProxy, Vector2 translation, PhysicsQuery.QueryFilter filter, PhysicsQuery.WorldCastMode castMode = PhysicsQuery.WorldCastMode.Closest, Allocator allocator = Unity.Collections.Allocator.Temp) => PhysicsWorld_CastShapeProxy(this, shapeProxy, translation, filter, castMode, allocator).ToNativeArray<PhysicsQuery.WorldCastResult>();

        /// <summary>
        /// Cast a "Mover" which is geometry designed to collide with the world and solve its movement.
        /// Everything is specified via the <see cref="PhysicsQuery.WorldMoverInput"/> with results returned in <see cref="PhysicsQuery.WorldMoverResult"/>.
        /// </summary>
        /// <param name="input">The configuration of the mover to cast.</param>
        /// <returns>The solved mover results.</returns>
        public readonly PhysicsQuery.WorldMoverResult CastMover(PhysicsQuery.WorldMoverInput input) => PhysicsWorld_CastMover(this, input);

        /// <summary>
        /// Returns the shape(s) that intersect the specified Circle geometry as it is cast through the world.
        /// See <see cref="PhysicsQuery.QueryFilter"/>, <see cref="PhysicsQuery.WorldCastMode"/>, <see cref="PhysicsQuery.WorldCastResult"/> and <see cref="Unity.Collections.Allocator"/>
        /// </summary>
        /// <param name="geometry">The Circle geometry used to cast through the world. This must be in world-space.</param>
        /// <param name="translation">The translation relative to the geometry defining the direction the geometry will move through the world.</param>
        /// <param name="filter">The filter to control what results are returned.</param>
        /// <param name="castMode">Controls how many and in what order the results are returned.</param>
        /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
        /// <returns>The query cast results. This NativeArray must be disposed of after use otherwise leaks will occur. The exception to this is if the array is empty.</returns>
        public readonly NativeArray<PhysicsQuery.WorldCastResult> CastGeometry(CircleGeometry geometry, Vector2 translation, PhysicsQuery.QueryFilter filter, PhysicsQuery.WorldCastMode castMode = PhysicsQuery.WorldCastMode.Closest, Allocator allocator = Unity.Collections.Allocator.Temp) => CastShapeProxy(new PhysicsShape.ShapeProxy(geometry), translation, filter, castMode, allocator);

        /// <summary>
        /// Returns the shape(s) that intersect the specified Capsule geometry as it is cast through the world.
        /// See <see cref="PhysicsQuery.QueryFilter"/>, <see cref="PhysicsQuery.WorldCastMode"/>, <see cref="PhysicsQuery.WorldCastResult"/> and <see cref="Unity.Collections.Allocator"/>.
        /// </summary>
        /// <param name="geometry">The Capsule geometry used to cast through the world. This must be in world-space.</param>
        /// <param name="translation">The translation relative to the geometry defining the direction the geometry will move through the world.</param>
        /// <param name="filter">The filter to control what results are returned.</param>
        /// <param name="castMode">Controls how many and in what order the results are returned.</param>
        /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
        /// <returns>The query cast results. This NativeArray must be disposed of after use otherwise leaks will occur. The exception to this is if the array is empty.</returns>
        public readonly NativeArray<PhysicsQuery.WorldCastResult> CastGeometry(CapsuleGeometry geometry, Vector2 translation, PhysicsQuery.QueryFilter filter, PhysicsQuery.WorldCastMode castMode = PhysicsQuery.WorldCastMode.Closest, Allocator allocator = Unity.Collections.Allocator.Temp) => CastShapeProxy(new PhysicsShape.ShapeProxy(geometry), translation, filter, castMode, allocator);

        /// <summary>
        /// Returns the shape(s) that intersect the specified Polygon geometry as it is cast through the world.
        /// See <see cref="PhysicsQuery.QueryFilter"/>, <see cref="PhysicsQuery.WorldCastMode"/>, <see cref="PhysicsQuery.WorldCastResult"/> and <see cref="Unity.Collections.Allocator"/>.
        /// </summary>
        /// <param name="geometry">The Polygon geometry used to cast through the world. This must be in world-space.</param>
        /// <param name="translation">The translation relative to the geometry defining the direction the geometry will move through the world.</param>
        /// <param name="filter">The filter to control what results are returned.</param>
        /// <param name="castMode">Controls how many and in what order the results are returned.</param>
        /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
        /// <returns>The query cast results. This NativeArray must be disposed of after use otherwise leaks will occur. The exception to this is if the array is empty.</returns>
        public readonly NativeArray<PhysicsQuery.WorldCastResult> CastGeometry(PolygonGeometry geometry, Vector2 translation, PhysicsQuery.QueryFilter filter, PhysicsQuery.WorldCastMode castMode = PhysicsQuery.WorldCastMode.Closest, Allocator allocator = Unity.Collections.Allocator.Temp) => CastShapeProxy(new PhysicsShape.ShapeProxy(geometry), translation, filter, castMode, allocator);

        #endregion

        #region Create / Destroy

        /// <summary>
        /// Create a body using the <see cref="PhysicsBodyDefinition.defaultDefinition"/> in the world.
        /// See <see cref="PhysicsBody.Create(PhysicsWorld)"/>.
        /// </summary>
        /// <returns>The created body.</returns>
        public readonly PhysicsBody CreateBody() => PhysicsBody.Create(this);

        /// <summary>
        /// Create a body in the world.
        /// See <see cref="PhysicsBody.Create(PhysicsWorld, PhysicsBodyDefinition)"/>.
        /// </summary>
        /// <param name="definition">The body definition to use.</param>
        /// <returns>The created body.</returns>
        public readonly PhysicsBody CreateBody(PhysicsBodyDefinition definition) => PhysicsBody.Create(this, definition);

        /// <summary>
        /// Create a batch of bodies in the world.
        /// </summary>
        /// <param name="definition">The body definition to use.</param>
        /// <param name="bodyCount">The number of bodies to create.</param>
        /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
        /// <returns>The created bodies. This NativeArray must be disposed of after use otherwise leaks will occur. The exception to this is if the array is empty.</returns>
        public unsafe NativeArray<PhysicsBody> CreateBodyBatch(PhysicsBodyDefinition definition, int bodyCount, Allocator allocator = Unity.Collections.Allocator.Temp) => PhysicsBody.CreateBatch(this, definition, bodyCount, allocator);

        /// <summary>
        /// Create a batch of bodies in the world.
        /// </summary>
        /// <param name="definitions">The definitions used to create the bodies. The number of bodies produced is implicitly controlled by the number of definitions in this span.</param>
        /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
        /// <returns>The created bodies. This NativeArray must be disposed of after use otherwise leaks will occur. The exception to this is if the array is empty.</returns>
        public NativeArray<PhysicsBody> CreateBodyBatch(ReadOnlySpan<PhysicsBodyDefinition> definitions, Allocator allocator = Unity.Collections.Allocator.Temp) => PhysicsBody.CreateBatch(this, definitions, allocator);

        /// <summary>
        /// Destroy a batch of bodies.
        /// Any invalid bodies will be ignored.
        /// Owned bodies will produce a warning and will not be destroyed (See <see cref="PhysicsBody.SetOwner(UnityEngine.Object)"/>).
        /// </summary>
        /// <param name="bodies">The bodies to destroy.</param>
        public static void DestroyBodyBatch(ReadOnlySpan<PhysicsBody> bodies) => PhysicsBody.DestroyBatch(bodies);

        /// <summary>
        /// Destroy a batch of shapes, destroying all <see cref="PhysicsShape.Contact"/> the shapes are involved in.
        /// Any invalid shapes will be ignored including chain segment shapes created via a <see cref="PhysicsChain"/> (the chain must be destroyed)."
        /// Owned shapes will produce a warning and will not be destroyed (<see cref="PhysicsShape.SetOwner(UnityEngine.Object)"/>).
        /// See <see cref="PhysicsBody.MassConfiguration"/>.
        /// </summary>
        /// <param name="shapes">The shapes to destroy.</param>
        /// <param name="updateBodyMass">Whether to update the body mass configuration. Not doing so is faster, especially when destroying multiple shapes.</param>
        public static void DestroyShapeBatch(ReadOnlySpan<PhysicsShape> shapes, bool updateBodyMass) => PhysicsShape.DestroyBatch(shapes, updateBodyMass);

        /// <summary>
        /// Destroy a batch of joints.
        /// Any invalid joints will be ignored.
        /// Owned joints will produce a warning and will not be destroyed (<see cref="PhysicsJoint.SetOwner(UnityEngine.Object)"/>).
        /// </summary>
        /// <param name="joints">The joints to destroy.</param>
        public static void DestroyJointBatch(ReadOnlySpan<PhysicsJoint> joints) => PhysicsJoint.DestroyBatch(joints);

        /// <summary>
        /// Create a PhysicsDistanceJoint in the world.
        /// See <see cref="PhysicsDistanceJoint.Create(PhysicsWorld, PhysicsDistanceJointDefinition)"/>.
        /// </summary>
        /// <param name="definition">The joint definition to use.</param>
        /// <returns>The created joint.</returns>
        public readonly PhysicsDistanceJoint CreateJoint(PhysicsDistanceJointDefinition definition) => PhysicsDistanceJoint.Create(this, definition);

        /// <summary>
        /// Create a PhysicsRelativeJoint in the world.
        /// See <see cref="PhysicsRelativeJoint.Create(PhysicsWorld, PhysicsRelativeJointDefinition)"/>.
        /// </summary>
        /// <param name="definition">The joint definition to use.</param>
        /// <returns>The created joint.</returns>
        public readonly PhysicsRelativeJoint CreateJoint(PhysicsRelativeJointDefinition definition) => PhysicsRelativeJoint.Create(this, definition);

        /// <summary>
        /// Create an PhysicsIgnoreJoint in the world.
        /// See <see cref="PhysicsIgnoreJoint.Create(PhysicsWorld, PhysicsIgnoreJointDefinition)"/>.
        /// </summary>
        /// <param name="definition">The joint definition to use.</param>
        /// <returns>The created joint.</returns>
        public readonly PhysicsIgnoreJoint CreateJoint(PhysicsIgnoreJointDefinition definition) => PhysicsIgnoreJoint.Create(this, definition);

        /// <summary>
        /// Create a PhysicsSliderJoint in the world.
        /// See <see cref="PhysicsSliderJoint.Create(PhysicsWorld, PhysicsSliderJointDefinition)"/>.
        /// </summary>
        /// <param name="definition">The joint definition to use.</param>
        /// <returns>The created joint.</returns>
        public readonly PhysicsSliderJoint CreateJoint(PhysicsSliderJointDefinition definition) => PhysicsSliderJoint.Create(this, definition);

        /// <summary>
        /// Create a PhysicsHingeJoint in the world.
        /// See <see cref="PhysicsHingeJoint.Create(PhysicsWorld, PhysicsHingeJointDefinition)"/>.
        /// </summary>
        /// <param name="definition">The joint definition to use.</param>
        /// <returns>The created joint.</returns>
        public readonly PhysicsHingeJoint CreateJoint(PhysicsHingeJointDefinition definition) => PhysicsHingeJoint.Create(this, definition);

        /// <summary>
        /// Create a PhysicsFixedJoint in the world.
        /// See <see cref="PhysicsFixedJoint.Create(PhysicsWorld, PhysicsFixedJointDefinition)"/>.
        /// </summary>
        /// <param name="definition">The joint definition to use.</param>
        /// <returns>The created joint.</returns>
        public readonly PhysicsFixedJoint CreateJoint(PhysicsFixedJointDefinition definition) => PhysicsFixedJoint.Create(this, definition);

        /// <summary>
        /// Create a PhysicsWheelJoint in the world.
        /// See <see cref="PhysicsWheelJoint.Create(PhysicsWorld, PhysicsWheelJointDefinition)"/>.
        /// </summary>
        /// <param name="definition">The joint definition to use.</param>
        /// <returns>The created joint.</returns>
        public readonly PhysicsWheelJoint CreateJoint(PhysicsWheelJointDefinition definition) => PhysicsWheelJoint.Create(this, definition);

        #endregion

        #region Debugging

        /// <summary>
        /// PhysicsWorld counters that give details of the world simulation size.
        /// </summary>
        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct WorldCounters
        {
            /// <summary>
            /// The number of all body types.
            /// </summary>
            public int bodyCount { readonly get => m_BodyCount; set => m_BodyCount = value; }

            /// <summary>
            /// The number of shapes.
            /// </summary>
            public int shapeCount { readonly get => m_ShapeCount; set => m_ShapeCount = value; }

            /// <summary>
            /// The number of contacts.
            /// </summary>
            public int contactCount { readonly get => m_ContactCount; set => m_ContactCount = value; }

            /// <summary>
            /// The number of joints.
            /// </summary>
            public int jointCount { readonly get => m_JointCount; set => m_JointCount = value; }

            /// <summary>
            /// The number of islands.
            /// </summary>
            public int islandCount { readonly get => m_IslandCount; set => m_IslandCount = value; }

            /// <summary>
            /// The number of bytes assigned to the Stack allocator.
            /// </summary>
            public int stackUsed { readonly get => m_StackUsed; set => m_StackUsed = value; }

            /// <summary>
            /// The total byte allocation used by the physics system.
            /// </summary>
            public int memoryUsed { readonly get => m_MemoryUsed; set => m_MemoryUsed = value; }

            /// <summary>
            /// The broadphase tree height for Static bodies.
            /// </summary>
            public int staticBroadphaseHeight { readonly get => m_StaticBroadphaseHeight; set => m_StaticBroadphaseHeight = value; }

            /// <summary>
            /// The broadphase tree height for both Dynamic and Kinematic bodies.
            /// </summary>
            public int broadphaseHeight { readonly get => m_BroadphaseHeight; set => m_BroadphaseHeight = value; }

            /// <summary>
            /// The number of multi-threaded tasks requested solving the simulation.
            /// </summary>
            public int taskCount { readonly get => m_TaskCount; set => m_TaskCount = value; }

            /// <summary>
            /// Add the specified world counters together.
            /// </summary>
            /// <param name="countersA">The first world counters to add.</param>
            /// <param name="countersB">The second world counters to add.</param>
            /// <returns>The world counters added together.</returns>
            public static WorldCounters Add(WorldCounters countersA, WorldCounters countersB)
            {
                // Add the counters.
                return new WorldCounters
                {
                    bodyCount = countersA.bodyCount + countersB.bodyCount,
                    shapeCount = countersA.shapeCount + countersB.shapeCount,
                    contactCount = countersA.contactCount + countersB.contactCount,
                    jointCount = countersA.jointCount + countersB.jointCount,
                    islandCount = countersA.islandCount + countersB.islandCount,
                    stackUsed = countersA.stackUsed + countersB.stackUsed,
                    memoryUsed = countersA.memoryUsed + countersB.memoryUsed,
                    staticBroadphaseHeight = countersA.staticBroadphaseHeight + countersB.staticBroadphaseHeight,
                    broadphaseHeight = countersA.broadphaseHeight + countersB.broadphaseHeight,
                    taskCount = countersA.taskCount + countersB.taskCount
                };
            }            

            /// <summary>
            /// Find the maximum values the specified world counters.
            /// </summary>
            /// <param name="countersA">The first world counters to find the maximum of.</param>
            /// <param name="countersB">The second world counters to find the maximum of.</param>
            /// <returns>The maximum values from both world counters.</returns>
            public static WorldCounters Maximum(WorldCounters countersA, WorldCounters countersB)
            {
                // Find the maximum of the counters.
                return new WorldCounters
                {
                    bodyCount = Mathf.Max(countersA.bodyCount, countersB.bodyCount),
                    shapeCount = Mathf.Max(countersA.shapeCount, countersB.shapeCount),
                    contactCount = Mathf.Max(countersA.contactCount, countersB.contactCount),
                    jointCount = Mathf.Max(countersA.jointCount, countersB.jointCount),
                    islandCount = Mathf.Max(countersA.islandCount, countersB.islandCount),
                    stackUsed = Mathf.Max(countersA.stackUsed, countersB.stackUsed),
                    memoryUsed = Mathf.Max(countersA.memoryUsed, countersB.memoryUsed),
                    staticBroadphaseHeight = Mathf.Max(countersA.staticBroadphaseHeight, countersB.staticBroadphaseHeight),
                    broadphaseHeight = Mathf.Max(countersA.broadphaseHeight, countersB.broadphaseHeight),
                    taskCount = Mathf.Max(countersA.taskCount, countersB.taskCount)
                };
            }

            #region Internal

            [SerializeField] int m_BodyCount;
            [SerializeField] int m_ShapeCount;
            [SerializeField] int m_ContactCount;
            [SerializeField] int m_JointCount;
            [SerializeField] int m_IslandCount;
            [SerializeField] int m_StackUsed;
            [SerializeField] int m_StaticBroadphaseHeight;
            [SerializeField] int m_BroadphaseHeight;
            [SerializeField] int m_MemoryUsed;
            [SerializeField] int m_TaskCount;
            fixed int m_ColorCounts[PhysicsConstants.SolverGraphColorCount];

            #endregion
        }

        /// <summary>
        /// PhysicsWorld profile that contains the timings of specific world simulation stages. All times are in milliseconds.
        /// </summary>
        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct WorldProfile
        {
            /// <summary>
            /// Time spent stepping the simulation forward.
            /// </summary>
	        public float simulationStep { readonly get => m_SimulationStep; set => m_SimulationStep = value; }

            /// <summary>
            /// Time spent updating collision pairs and creating contacts.
            /// </summary>
	        public float contactPairs { readonly get => m_ContactPairs; set => m_ContactPairs = value; }

            /// <summary>
            /// Time spent updating contacts.
            /// </summary>
	        public float contactUpdates { readonly get => m_ContactUpdates; set => m_ContactUpdates = value; }

            /// <summary>
            /// Time spent integrating velocities, solving velocity constraints, and integrating positions.
            /// </summary>
	        public float solving { readonly get => m_Solving; set => m_Solving = value; }

            /// <summary>
            /// Time spent preparing simulation stages.
            /// </summary>
	        public float prepareStages { readonly get => m_PrepareStages; set => m_PrepareStages = value; }

            /// <summary>
            /// Time spent solving constraints.
            /// </summary>
	        public float solveConstraints { readonly get => m_SolveConstraints; set => m_SolveConstraints = value; }

            /// <summary>
            /// Time spent preparing joint and contact constraints.
            /// </summary>
	        public float prepareConstraints { readonly get => m_PrepareConstraints; set => m_PrepareConstraints = value; }

            /// <summary>
            /// Time spent integrating velocities.
            /// </summary>
	        public float integrateVelocities { readonly get => m_IntegrateVelocities; set => m_IntegrateVelocities = value; }

            /// <summary>
            /// Time spent warm-starting.
            /// </summary>
	        public float warmStarting { readonly get => m_WarmStarting; set => m_WarmStarting = value; }

            /// <summary>
            /// Time spent solving impulses.
            /// </summary>
	        public float solveImpulses { readonly get => m_SolveImpulses; set => m_SolveImpulses = value; }

            /// <summary>
            /// Time spent integrating transforms.
            /// </summary>
	        public float integrateTransforms { readonly get => m_IntegrateTransforms; set => m_IntegrateTransforms = value; }

            /// <summary>
            /// Time spent relaxing constraint impulses.
            /// </summary>
	        public float relaxImpulses { readonly get => m_RelaxImpulses; set => m_RelaxImpulses = value; }

            /// <summary>
            /// Time spent applying bounciness.
            /// </summary>
            public float applyBounciness { readonly get => m_ApplyBounciness; set => m_ApplyBounciness = value; }

            /// <summary>
            /// Time spent storing impulses.
            /// </summary>
	        public float storeImpulses { readonly get => m_StoreImpulses; set => m_StoreImpulses = value; }

            /// <summary>
            /// Time spent splitting islands because some contacts and/or joints have been removed.
            /// </summary>
	        public float splitIslands { readonly get => m_SplitIslands; set => m_SplitIslands = value; }

            /// <summary>
            /// Time spent updating body transforms.
            /// </summary>
	        public float bodyTransforms { readonly get => m_BodyTransforms; set => m_BodyTransforms = value; }

            /// <summary>
            /// Time spent calculate fast triggers for bodies.
            /// </summary>
            public float fastTriggers { readonly get => m_FastTriggers; set => m_FastTriggers = value; }

            /// <summary>
            /// Time spent generating joint threshold events.
            /// </summary>
            public float jointEvents { readonly get => m_JointEvents; set => m_JointEvents = value; }

            /// <summary>
            /// Time spent generating contact hit events.
            /// </summary>
	        public float hitEvents { readonly get => m_HitEvents; set => m_HitEvents = value; }

            /// <summary>
            /// Time spent refitting the broadphase.
            /// </summary>
	        public float broadphaseUpdates { readonly get => m_BroadphaseUpdates; set => m_BroadphaseUpdates = value; }

            /// <summary>
            /// Time spent solving continuous collision detection.
            /// </summary>
	        public float solveContinuous { readonly get => m_SolveContinuous; set => m_SolveContinuous = value; }

            /// <summary>
            /// Time spent updating islands that need to sleep.
            /// </summary>
	        public float sleepIslands { readonly get => m_SleepIslands; set => m_SleepIslands = value; }

            /// <summary>
            /// Time spent updating triggers.
            /// </summary>
            public float updateTriggers { readonly get => m_UpdateTriggers; set => m_UpdateTriggers = value; }

            /// <summary>
            /// Time spent writing the body poses to the transform system.
            /// </summary>
            public float writeTransforms { readonly get => m_WriteTransforms; set => m_WriteTransforms = value; }

            /// <summary>
            /// Add the specified world profiles together.
            /// </summary>
            /// <param name="profileA">The first world profiles to add.</param>
            /// <param name="profileB">The second world profiles to add.</param>
            /// <returns>The world profiles added together.</returns>
            public static WorldProfile Add(WorldProfile profileA, WorldProfile profileB)
            {
                // Add the counters.
                return new WorldProfile
                {
                    simulationStep = profileA.simulationStep + profileB.simulationStep,
                    contactPairs = profileA.contactPairs + profileB.contactPairs,
                    contactUpdates = profileA.contactUpdates + profileB.contactUpdates,
                    solving = profileA.solving + profileB.solving,
                    prepareStages = profileA.prepareStages + profileB.prepareStages,
                    solveConstraints = profileA.solveConstraints + profileB.solveConstraints,
                    prepareConstraints = profileA.prepareConstraints + profileB.prepareConstraints,
                    integrateVelocities = profileA.integrateVelocities + profileB.integrateVelocities,
                    warmStarting = profileA.warmStarting + profileB.warmStarting,
                    solveImpulses = profileA.solveImpulses + profileB.solveImpulses,
                    integrateTransforms = profileA.integrateTransforms + profileB.integrateTransforms,
                    relaxImpulses = profileA.relaxImpulses + profileB.relaxImpulses,
                    applyBounciness = profileA.applyBounciness + profileB.applyBounciness,
                    storeImpulses = profileA.storeImpulses + profileB.storeImpulses,
                    splitIslands = profileA.splitIslands + profileB.splitIslands,
                    bodyTransforms = profileA.bodyTransforms + profileB.bodyTransforms,
                    fastTriggers = profileA.fastTriggers + profileB.fastTriggers,
                    jointEvents = profileA.jointEvents + profileB.jointEvents,
                    hitEvents = profileA.hitEvents + profileB.hitEvents,
                    broadphaseUpdates = profileA.broadphaseUpdates + profileB.broadphaseUpdates,
                    solveContinuous = profileA.solveContinuous + profileB.solveContinuous,
                    sleepIslands = profileA.sleepIslands + profileB.sleepIslands,
                    updateTriggers = profileA.updateTriggers + profileB.updateTriggers,
                    writeTransforms = profileA.writeTransforms + profileB.writeTransforms
                };
            }

            /// <summary>
            /// Find the maximum values the specified world profiles.
            /// </summary>
            /// <param name="profileA">The first world profile to find the maximum of.</param>
            /// <param name="profileB">The second world profile to find the maximum of.</param>
            /// <returns>The maximum values from both world profile.</returns>
            public static WorldProfile Maximum(WorldProfile profileA, WorldProfile profileB)
            {
                return new WorldProfile
                {
                    simulationStep = Mathf.Max(profileA.simulationStep, profileB.simulationStep),
                    contactPairs = Mathf.Max(profileA.contactPairs, profileB.contactPairs),
                    contactUpdates = Mathf.Max(profileA.contactUpdates, profileB.contactUpdates),
                    solving = Mathf.Max(profileA.solving, profileB.solving),
                    prepareStages = Mathf.Max(profileA.prepareStages, profileB.prepareStages),
                    solveConstraints = Mathf.Max(profileA.solveConstraints, profileB.solveConstraints),
                    prepareConstraints = Mathf.Max(profileA.prepareConstraints, profileB.prepareConstraints),
                    integrateVelocities = Mathf.Max(profileA.integrateVelocities, profileB.integrateVelocities),
                    warmStarting = Mathf.Max(profileA.warmStarting, profileB.warmStarting),
                    solveImpulses = Mathf.Max(profileA.solveImpulses, profileB.solveImpulses),
                    integrateTransforms = Mathf.Max(profileA.integrateTransforms, profileB.integrateTransforms),
                    relaxImpulses = Mathf.Max(profileA.relaxImpulses, profileB.relaxImpulses),
                    applyBounciness = Mathf.Max(profileA.applyBounciness, profileB.applyBounciness),
                    storeImpulses = Mathf.Max(profileA.storeImpulses, profileB.storeImpulses),
                    splitIslands = Mathf.Max(profileA.splitIslands, profileB.splitIslands),
                    bodyTransforms = Mathf.Max(profileA.bodyTransforms, profileB.bodyTransforms),
                    fastTriggers = Mathf.Max(profileA.fastTriggers, profileB.fastTriggers),
                    jointEvents = Mathf.Max(profileA.jointEvents, profileB.jointEvents),
                    hitEvents = Mathf.Max(profileA.hitEvents, profileB.hitEvents),
                    broadphaseUpdates = Mathf.Max(profileA.broadphaseUpdates, profileB.broadphaseUpdates),
                    solveContinuous = Mathf.Max(profileA.solveContinuous, profileB.solveContinuous),
                    sleepIslands = Mathf.Max(profileA.sleepIslands, profileB.sleepIslands),
                    updateTriggers = Mathf.Max(profileA.updateTriggers, profileB.updateTriggers),
                    writeTransforms = Mathf.Max(profileA.writeTransforms, profileB.writeTransforms)
                };
            }

            #region Internal

	        [SerializeField] float m_SimulationStep;
	        [SerializeField] float m_ContactPairs;
	        [SerializeField] float m_ContactUpdates;
	        [SerializeField] float m_Solving;
	        [SerializeField] float m_PrepareStages;
	        [SerializeField] float m_SolveConstraints;
	        [SerializeField] float m_PrepareConstraints;
	        [SerializeField] float m_IntegrateVelocities;
	        [SerializeField] float m_WarmStarting;
	        [SerializeField] float m_SolveImpulses;
	        [SerializeField] float m_IntegrateTransforms;
	        [SerializeField] float m_RelaxImpulses;
            [SerializeField] float m_ApplyBounciness;
	        [SerializeField] float m_StoreImpulses;
	        [SerializeField] float m_SplitIslands;
	        [SerializeField] float m_BodyTransforms;
            [SerializeField] float m_FastTriggers;
            [SerializeField] float m_JointEvents;
	        [SerializeField] float m_HitEvents;
	        [SerializeField] float m_BroadphaseUpdates;
	        [SerializeField] float m_SolveContinuous;
	        [SerializeField] float m_SleepIslands;
            [SerializeField] float m_UpdateTriggers;
            [SerializeField] float m_WriteTransforms;

            #endregion
        }

        /// <summary>
        /// Get the number of awake bodies in the world.
        /// </summary>
        public readonly int awakeBodyCount => PhysicsWorld_GetAwakeBodyCount(this);

        /// <summary>
        /// Get the world counters.
        /// </summary>
        public readonly WorldCounters counters => PhysicsWorld_GetCounters(this);

        /// <summary>
        /// Get the world counters, summed for all the active worlds.
        /// </summary>
        public static WorldCounters globalCounters => PhysicsWorld_GetGlobalCounters();

        /// <summary>
        /// Get the world timing profile.
        /// </summary>
        public readonly WorldProfile profile => PhysicsWorld_GetProfile(this);

        /// <summary>
        /// Get the world timing profile, summed for all the active worlds.
        /// </summary>
        public static WorldProfile globalProfile => PhysicsWorld_GetGlobalProfile();

        #endregion

        #region Drawing

        /// <summary>
        /// Draw Options limits what gets drawn to a broad selection.
        /// </summary>
        [Flags]
        public enum DrawOptions
        {
            /// <summary>
            /// No drawing.
            /// </summary>
            Off = 0,

            /// <summary>
            /// Draw the selected bodies.
            /// </summary>
            SelectedBodies = 1 << 0,

            /// <summary>
            /// Draw the selected shapes.
            /// </summary>
            SelectedShapes = 1 << 1,

            /// <summary>
            /// Draw the selected shape bounds.
            /// </summary>
            SelectedShapeBounds = 1 << 2,

            /// <summary>
            /// Draw the selected joints.
            /// </summary>
            SelectedJoints = 1 << 3,

            /// <summary>
            /// Draw all bodies in the world.
            /// </summary>
            AllBodies = 1 << 4,

            /// <summary>
            /// Draw all the shapes in the world.
            /// </summary>
            AllShapes = 1 << 5,

            /// <summary>
            /// Draw all the shape bounds in the world.
            /// </summary>
            AllShapeBounds = 1 << 6,

            /// <summary>
            /// Draw all the joints in the world.
            /// </summary>
            AllJoints = 1 << 7,

            /// <summary>
            /// Draw all the contact points in the world.
            /// </summary>
            AllContactPoints = 1 << 8,

            /// <summary>
            /// Draw all the contact normals in the world.
            /// </summary>
            AllContactNormal = 1 << 9,

            /// <summary>
            /// Draw all the contact forces in the world.
            /// </summary>
            [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            [Obsolete("Enum member DrawOptions.AllContactImpulse is deprecated. Use DrawOptions.AllContactForces (UnityUpgradable) -> AllContactForces", false)]
            AllContactImpulse = 1 << 10,

            /// <summary>
            /// Draw all the contact forces in the world.
            /// </summary>
            AllContactForces = 1 << 10,

            /// <summary>
            /// Draw all the contact friction (tangent) in the world.
            /// </summary>
            AllContactFriction = 1 << 11,

            /// <summary>
            /// Draw all the custom drawing.
            /// </summary>
            AllCustom = 1 << 12,

            /// <summary>
            /// Draw all the solver islands in the world.
            /// </summary>
            AllSolverIslands = 1 << 13,

            /// <summary>
            /// The default drawing when drawing all. Draw all the shapes, joints and custom drawing in the world.
            /// </summary>
            DefaultAll = AllShapes | AllJoints | AllCustom,

            /// <summary>
            /// The default drawing when drawing selections. Draw selected shapes, joints and custom drawing in the world.
            /// </summary>
            DefaultSelected = SelectedShapes | SelectedJoints | AllCustom
        }

        /// <summary>
        /// Controls how shape geometry is filled when drawing.
        /// </summary>
        [Flags]
        public enum DrawFillOptions : int
        {
            /// <summary>
            /// The interior of the area is drawn.
            /// </summary>
            Interior = 1 << 0,

            /// <summary>
            /// The outline of the area is drawn.
            /// </summary>
            Outline = 1 << 1,

            /// <summary>
            /// The orientation of the area is drawn (if applicable). This is only drawn if the Outline is drawn.
            /// </summary>
            Orientation = 1 << 2,

            /// <summary>
            /// A combination drawn of:
            /// 
            ///- <see cref="PhysicsWorld.DrawFillOptions.Interior"/>
            ///- <see cref="PhysicsWorld.DrawFillOptions.Outline"/>
            ///- <see cref="PhysicsWorld.DrawFillOptions.Orientation"/>
            /// </summary>
            All = Interior | Outline | Orientation,
        }

        /// <summary>
        /// The draw results retrieved from the world.
        /// You must immediately extract what information you need and not directly reference the returned data as it will be cleared immediately after being provided.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public readonly struct DrawResults
        {
            internal readonly PhysicsBuffer m_PolygonGeometryElements;
            internal readonly PhysicsBuffer m_CircleGeometryElements;
            internal readonly PhysicsBuffer m_CapsuleGeometryElements;
            internal readonly PhysicsBuffer m_LineElements;
            internal readonly PhysicsBuffer m_PointElements;

            /// <undoc/>
            public override readonly string ToString() => $"PolygonGeometry:{m_PolygonGeometryElements}, CircleGeometry:{m_CircleGeometryElements}, CapsuleGeometry:{m_CapsuleGeometryElements}, Line:{m_LineElements}, Point: {m_PointElements}";

            /// <summary>
            /// Get if the draw results are valid i.e. they contain any data at all.
            /// </summary>
            public bool isValid =>
                m_PolygonGeometryElements.isValid ||
                m_CircleGeometryElements.isValid ||
                m_CapsuleGeometryElements.isValid ||
                m_LineElements.isValid ||
                m_PointElements.isValid;

            /// <summary>
            /// A Polygon Geometry Element.
            /// </summary>
            [StructLayout(LayoutKind.Sequential)]
            public readonly struct PolygonGeometryElement
            {
                /// <summary>
                /// The transform of the polygon element.
                /// </summary>
                public readonly PhysicsTransform transform;

                /// <summary>
                /// The point #0 of the polygon element.
                /// </summary>
                public readonly Vector2 p0;

                /// <summary>
                /// The point #1 of the polygon element.
                /// </summary>
                public readonly Vector2 p1;

                /// <summary>
                /// The point #2 of the polygon element.
                /// </summary>
                public readonly Vector2 p2;

                /// <summary>
                /// The point #3 of the polygon element.
                /// </summary>
                public readonly Vector2 p3;

                /// <summary>
                /// The point #4 of the polygon element.
                /// </summary>
                public readonly Vector2 p4;

                /// <summary>
                /// The point #5 of the polygon element.
                /// </summary>
                public readonly Vector2 p5;

                /// <summary>
                /// The point #6 of the polygon element.
                /// </summary>
                public readonly Vector2 p6;

                /// <summary>
                /// The point #7 of the polygon element.
                /// </summary>
                public readonly Vector2 p7;

                /// <summary>
                /// The number of points in the polygon element.
                /// </summary>
                public readonly int count;

                /// <summary>
                /// The radius of the polygon element.
                /// </summary>
                public readonly float radius;

                /// <summary>
                /// The depth of the element.
                /// </summary>
                public readonly float elementDepth;

                /// <summary>
                /// How the geometry element is filled with the color.
                /// See <see cref="PhysicsWorld.DrawFillOptions"/>.
                /// </summary>
                public readonly DrawFillOptions drawFillOptions;

                /// <summary>
                /// The color of the polygon element.
                /// </summary>
                public readonly Color color;

                /// <summary>
                /// The data size of the polygon element.
                /// This can be useful in understanding the memory stride in a <see cref="ComputeBuffer"/> or other structure.
                /// </summary>
                /// <returns>The size in bytes.</returns>
                public static int Size()
                {
                    return sizeof(float) * 4 +      // Transform
                            sizeof(float) * 2 * 8 + // p0-p7
                            sizeof(int) +           // count
                            sizeof(float) +         // radius
                            sizeof(float) +         // element depth
                            sizeof(int) +           // drawFillOptions
                            sizeof(float) * 4;      // color
                }
            }

            /// <summary>
            /// A Circle Geometry Element.
            /// </summary>
            [StructLayout(LayoutKind.Sequential)]
            public readonly struct CircleGeometryElement
            {
                /// <summary>
                /// The transform of the circle element.
                /// </summary>
                public readonly PhysicsTransform transform;

                /// <summary>
                /// The radius of the circle element.
                /// </summary>
                public readonly float radius;

                /// <summary>
                /// The depth of the element.
                /// </summary>
                public readonly float elementDepth;

                /// <summary>
                /// How the geometry element is filled with the color.
                /// See <see cref="PhysicsWorld.DrawFillOptions"/>.
                /// </summary>
                public readonly DrawFillOptions drawFillOptions;

                /// <summary>
                /// The color of the circle element.
                /// </summary>
                public readonly Color color;

                /// <summary>
                /// The data size of the circle element.
                /// This can be useful in understanding the memory stride in a <see cref="ComputeBuffer"/> or other structure.
                /// </summary>
                /// <returns>The size in bytes.</returns>
                public static int Size()
                {
                    return sizeof(float) * 4 +  // Transform
                            sizeof(float) +     // radius
                            sizeof(float) +     // element depth
                            sizeof(int) +       // drawFillOptions
                            sizeof(float) * 4;  // color
                }
            };

            /// <summary>
            /// A Capsule Geometry Element.
            /// </summary>
            [StructLayout(LayoutKind.Sequential)]
            public readonly struct CapsuleGeometryElement
            {
                /// <summary>
                /// The transform of the capsule element.
                /// </summary>
                public readonly PhysicsTransform transform;

                /// <summary>
                /// The radius of the capsule element.
                /// </summary>
                public readonly float radius;

                /// <summary>
                /// The length of the capsule element.
                /// </summary>
                public readonly float length;

                /// <summary>
                /// The depth of the element.
                /// </summary>
                public readonly float elementDepth;

                /// <summary>
                /// How the geometry element is filled with the color.
                /// See <see cref="PhysicsWorld.DrawFillOptions"/>.
                /// </summary>
                public readonly DrawFillOptions drawFillOptions;

                /// <summary>
                /// The color of the capsule element.
                /// </summary>
                public readonly Color color;

                /// <summary>
                /// The data size of the capsule element.
                /// This can be useful in understanding the memory stride in a <see cref="ComputeBuffer"/> or other structure.
                /// </summary>
                /// <returns>The size in bytes.</returns>
                public static int Size()
                {
                    return sizeof(float) * 4 +  // Transform
                            sizeof(float) +     // radius
                            sizeof(float) +     // length
                            sizeof(float) +     // element depth
                            sizeof(int) +       // drawFillOptions
                            sizeof(float) * 4;  // color
                }
            };

            /// <summary>
            /// A Line Element.
            /// </summary>
            [StructLayout(LayoutKind.Sequential)]
            public readonly struct LineElement
            {
                /// <summary>
                /// The transform of the line element.
                /// </summary>
                public readonly PhysicsTransform transform;

                /// <summary>
                /// The length of the line element.
                /// </summary>
                public readonly float length;

                /// <summary>
                /// The depth of the element.
                /// </summary>
                public readonly float elementDepth;

                /// <summary>
                /// The color of the line element.
                /// </summary>
                public readonly Color color;

                /// <summary>
                /// The data size of the line element.
                /// This can be useful in understanding the memory stride in a <see cref="ComputeBuffer"/> or other structure.
                /// </summary>
                /// <returns>The size in bytes.</returns>
                public static int Size()
                {
                    return sizeof(float) * 4 +  // Transform
                            sizeof(float) +     // length
                            sizeof(float) +     // element depth
                            sizeof(float) * 4;  // color
                }
            };

            /// <summary>
            /// A Point Element.
            /// </summary>
            [StructLayout(LayoutKind.Sequential)]
            public readonly struct PointElement
            {
                /// <summary>
                /// The position of the point element.
                /// </summary>
                public readonly Vector2 position;

                /// <summary>
                /// The radius of the point element (in pixels).
                /// </summary>
                public readonly float radius;

                /// <summary>
                /// The depth of the element.
                /// </summary>
                public readonly float elementDepth;

                /// <summary>
                /// The color of the point element.
                /// </summary>
                public readonly Color color;

                /// <summary>
                /// The data size of the point element.
                /// This can be useful in understanding the memory stride in a <see cref="ComputeBuffer"/> or other structure.
                /// </summary>
                /// <returns>The size in bytes.</returns>
                public static int Size()
                {
                    return sizeof(float) * 2 +  // position
                            sizeof(float) +     // radius
                            sizeof(float) +     // element depth
                            sizeof(float) * 4;  // color
                }
            };

            /// <summary>
            /// Retrieve the Polygon Geometry Elements.
            /// Any new <see cref="PhysicsWorld"/> drawing will invalidate this data so referring to this data afterwards may cause an unavoidable crash!
            /// You must immediately extract what information you need and not directly reference the returned data as it will be cleared immediately after being provided.
            /// </summary>
            public readonly NativeArray<PolygonGeometryElement> polygonGeometryArray => m_PolygonGeometryElements.ToNativeArray<PolygonGeometryElement>();

            /// <summary>
            /// Retrieve the Circle Geometry Elements.
            /// Any new <see cref="PhysicsWorld"/> drawing will invalidate this data so referring to this data afterwards may cause an unavoidable crash!
            /// You must immediately extract what information you need and not directly reference the returned data as it will be cleared immediately after being provided.
            /// </summary>
            public readonly NativeArray<CircleGeometryElement> circleGeometryArray => m_CircleGeometryElements.ToNativeArray<CircleGeometryElement>();

            /// <summary>
            /// Retrieve the Capsule Geometry Element.
            /// Any new <see cref="PhysicsWorld"/> drawing will invalidate this data so referring to this data afterwards may cause an unavoidable crash!
            /// You must immediately extract what information you need and not directly reference the returned data as it will be cleared immediately after being provided.
            /// </summary>
            public readonly NativeArray<CapsuleGeometryElement> capsuleGeometryArray => m_CapsuleGeometryElements.ToNativeArray<CapsuleGeometryElement>();

            /// <summary>
            /// Retrieve the Line Elements.
            /// Any new <see cref="PhysicsWorld"/> drawing will invalidate this data so referring to this data afterwards may cause an unavoidable crash!
            /// You must immediately extract what information you need and not directly reference the returned data as it will be cleared immediately after being provided.
            /// </summary>
            public readonly NativeArray<LineElement> lineArray => m_LineElements.ToNativeArray<LineElement>();

            /// <summary>
            /// Retrieve the Point Elements.
            /// Any new <see cref="PhysicsWorld"/> drawing will invalidate this data so referring to this data afterwards may cause an unavoidable crash!
            /// You must immediately extract what information you need and not directly reference the returned data as it will be cleared immediately after being provided.
            /// </summary>
            public readonly NativeArray<PointElement> pointArray => m_PointElements.ToNativeArray<PointElement>();

            /// <summary>
            /// Retrieve the Polygon Geometry Elements.
            /// Any new <see cref="PhysicsWorld"/> drawing will invalidate this data so referring to this data afterwards may cause an unavoidable crash!
            /// You must immediately extract what information you need and not directly reference the returned data as it will be cleared immediately after being provided.
            /// </summary>
            public readonly Span<PolygonGeometryElement> polygonGeometrySpan => m_PolygonGeometryElements.ToSpan<PolygonGeometryElement>();

            /// <summary>
            /// Retrieve the Circle Geometry Elements.
            /// Any new <see cref="PhysicsWorld"/> drawing will invalidate this data so referring to this data afterwards may cause an unavoidable crash!
            /// You must immediately extract what information you need and not directly reference the returned data as it will be cleared immediately after being provided.
            /// </summary>
            public readonly Span<CircleGeometryElement> circleGeometrySpan => m_CircleGeometryElements.ToSpan<CircleGeometryElement>();

            /// <summary>
            /// Retrieve the Capsule Geometry Elements.
            /// Any new <see cref="PhysicsWorld"/> drawing will invalidate this data so referring to this data afterwards may cause an unavoidable crash!
            /// You must immediately extract what information you need and not directly reference the returned data as it will be cleared immediately after being provided.
            /// </summary>
            public readonly Span<CapsuleGeometryElement> capsuleGeometrySpan => m_CapsuleGeometryElements.ToSpan<CapsuleGeometryElement>();

            /// <summary>
            /// Retrieve the Line Elements.
            /// Any new <see cref="PhysicsWorld"/> drawing will invalidate this data so referring to this data afterwards may cause an unavoidable crash!
            /// You must immediately extract what information you need and not directly reference the returned data as it will be cleared immediately after being provided.
            /// </summary>
            public readonly Span<LineElement> lineSpan => m_LineElements.ToSpan<LineElement>();

            /// <summary>
            /// Retrieve the Point Elements.
            /// Any new <see cref="PhysicsWorld"/> drawing will invalidate this data so referring to this data afterwards may cause an unavoidable crash!
            /// You must immediately extract what information you need and not directly reference the returned data as it will be cleared immediately after being provided.
            /// </summary>
            public readonly Span<PointElement> pointSpan => m_PointElements.ToSpan<PointElement>();
        }

        /// <summary>
        /// The colors used to draw <see cref="PhysicsBody"/>, <see cref="PhysicsShape"/>, <see cref="PhysicsJoint"/> etc.
        /// </summary>
        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        public struct DrawColors
        {
            /// <summary>
            /// The X component of the Transform axis.
            /// </summary>
            public Color transformAxisX;

            /// <summary>
            /// The Y component of the Transform axis.
            /// </summary>
            public Color transformAxisY;

            /// <summary>
            /// A shape that is attached to a dynamic body with zero mass.
            /// </summary>
            public Color bodyBad;

            /// <summary>
            /// A shape that is attached to a disabled body.
            /// </summary>
            public Color bodyDisabled;

            /// <summary>
            /// A shape that is attached to an awake body.
            /// </summary>
            public Color bodyAwake;

            /// <summary>
            /// A shape that is attached to a body with a Static body type.
            /// </summary>
            public Color bodyStatic;

            /// <summary>
            /// A shape that is attached to a body with a Kinematic body type.
            /// </summary>
            public Color bodyKinematic;

            /// <summary>
            /// A shape that is attached to a body that had a time-of-impact event.
            /// </summary>
            public Color bodyTimeOfImpactEvent;

            /// <summary>
            /// A shape that is attached to a body that is awake and has fast collisions allowed.
            /// </summary>
            public Color bodyFastCollisions;

            /// <summary>
            /// A shape that is attached to a body that is currently moving fast.
            /// </summary>
            public Color bodyMovingFast;

            /// <summary>
            /// A shape that is attached to a body that is currently having its speed capped.
            /// </summary>
            public Color bodySpeedCapped;

            /// <summary>
            /// A shape that is marked as a trigger.
            /// </summary>
            public Color shapeTrigger;

            /// <summary>
            /// The default color used when no other shape state is indicated.
            /// </summary>
            public Color shapeOther;

            /// <summary>
            /// The shape bounds.
            /// </summary>
            public Color shapeBounds;

            /// <summary>
            /// A contact that is speculative.
            /// </summary>
            public Color contactSpeculative;

            /// <summary>
            /// A contact that was added during the last simulation step.
            /// </summary>
            public Color contactAdded;

            /// <summary>
            /// A contact that already existed at the start of the last simulation step.
            /// </summary>
            public Color contactPersisted;

            /// <summary>
            /// A contact normal.
            /// </summary>
            public Color contactNormal;

            /// <summary>
            /// The contact impulse being applied.
            /// </summary>
            public Color contactImpulse;

            /// <summary>
            /// The contact friction being applied.
            /// </summary>
            public Color contactFriction;

            /// <summary>
            /// A solver island region.
            /// </summary>
            public Color solverIsland;

            /// <summary>
            /// A collection of constraint graph colors.
            /// </summary>
            readonly ConstraintGraphArray m_ConstraintGraph;

            /// <undoc/>
            [StructLayout(LayoutKind.Sequential)]
            struct ConstraintGraphArray
            {
                /// <undoc/>
                public Color graphConstraint0;

                /// <undoc/>
                public Color graphConstraint1;

                /// <undoc/>
                public Color graphConstraint2;

                /// <undoc/>
                public Color graphConstraint3;

                /// <undoc/>
                public Color graphConstraint4;

                /// <undoc/>
                public Color graphConstraint5;

                /// <undoc/>
                public Color graphConstraint6;

                /// <undoc/>
                public Color graphConstraint7;

                /// <undoc/>
                public Color graphConstraint8;

                /// <undoc/>
                public Color graphConstraint9;

                /// <undoc/>
                public Color graphConstraint10;

                /// <undoc/>
                public Color graphConstraint11;

                /// <undoc/>
                public Color graphConstraint12;

                /// <undoc/>
                public Color graphConstraint13;

                /// <undoc/>
                public Color graphConstraint14;

                /// <undoc/>
                public Color graphConstraint15;

                /// <undoc/>
                public Color graphConstraint16;

                /// <undoc/>
                public Color graphConstraint17;

                /// <undoc/>
                public Color graphConstraint18;

                /// <undoc/>
                public Color graphConstraint19;

                /// <undoc/>
                public Color graphConstraint20;

                /// <undoc/>
                public Color graphConstraint21;

                /// <undoc/>
                public Color graphConstraint22;

                /// <undoc/>
                public Color graphConstraint23;

                /// <summary>
                /// Accessor for the graph array.
                /// </summary>
                /// <param name="index">The index to access.</param>
                /// <returns>The array index value.</returns>
                /// <exception cref="System.IndexOutOfRangeException">Thrown if the index is not in the range [0, <see cref="PhysicsConstants.SolverGraphColorCount"/> -1].</exception>
                public unsafe ref Color this[int index]
                {
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    get
                    {
                        if (index >= 0 && index < PhysicsConstants.SolverGraphColorCount)
                        {
                            fixed (Color* pThis = &graphConstraint0)
                            {
                                return ref pThis[index];
                            }
                        }

                        throw new IndexOutOfRangeException($"{index} must be in the range [0, {PhysicsConstants.SolverGraphColorCount - 1}]");
                    }
                }
            }
        };

        /// <summary>
        /// Limits what gets drawn to a broad selection.
        /// See <see cref="PhysicsWorld.DrawOptions"/>.
        /// </summary>
        public readonly DrawOptions drawOptions { get => PhysicsWorld_GetDrawOptions(this); set => PhysicsWorld_SetDrawOptions(this, value); }

        /// <summary>
        /// Controls how shape geometry is filled when drawing.
        /// See <see cref="PhysicsWorld.DrawFillOptions"/>.
        /// </summary>
        public readonly DrawFillOptions drawFillOptions { get => PhysicsWorld_GetDrawFillOptions(this); set => PhysicsWorld_SetDrawFillOptions(this, value); }

        /// <summary>
        /// Limits what gets drawn to a narrow selection.
        /// This only affects <see cref="PhysicsWorld.DrawOptions"/> that are drawing all bodies, shapes etc.
        /// It does not affect selected elements or custom drawing.
        /// See <see cref="PhysicsWorld.IgnoreFilter"/>.
        /// </summary>
        public readonly IgnoreFilter drawFilter { get => PhysicsWorld_GetDrawFilter(this); set => PhysicsWorld_SetDrawFilter(this, value); }

        /// <summary>
        /// Controls the draw thickness (outline and orientation).
        /// </summary>
        public readonly float drawThickness { get => PhysicsWorld_GetDrawThickness(this); set => PhysicsWorld_SetDrawThickness(this, value); }

        /// <summary>
        /// Controls the draw fill alpha. This is used to scale the interior fill alpha and is only used when <see cref="PhysicsWorld.DrawFillOptions.Outline"/> is used so that the interior color can be distinguished from the outline color by transparency.
        /// </summary>
        public readonly float drawFillAlpha { get => PhysicsWorld_GetDrawFillAlpha(this); set => PhysicsWorld_SetDrawFillAlpha(this, value); }

        /// <summary>
        /// Controls the draw point scale used when drawing points.
        /// </summary>
        public readonly float drawPointScale { get => PhysicsWorld_GetDrawPointScale(this); set => PhysicsWorld_SetDrawPointScale(this, value); }

        /// <summary>
        /// Controls the joint contact normal scale used when drawing contact normals.
        /// </summary>
        public readonly float drawNormalScale { get => PhysicsWorld_GetDrawNormalScale(this); set => PhysicsWorld_SetDrawNormalScale(this, value); }

        /// <summary>
        /// Controls the joint contact force scale used when drawing contact forces.
        /// </summary>
        public readonly float drawForceScale { get => PhysicsWorld_GetDrawForceScale(this); set => PhysicsWorld_SetDrawForceScale(this, value); }

        /// <summary>
        /// Controls what colors are used to draw <see cref="PhysicsBody"/>, <see cref="PhysicsShape"/>, <see cref="PhysicsJoint"/> etc.
        /// </summary>
        public readonly DrawColors drawColors { get => PhysicsWorld_GetDrawColors(this); set => PhysicsWorld_SetDrawColors(this, value); }

        /// <summary>
        /// Controls the element depth.
        /// 
        /// When using custom drawing of geometry or primitive shapes there is no reference to the orthogonal axis used with
        /// respect to the current <see cref="PhysicsWorld.transformPlane"/>.
        ///
        /// The element depth is in world-space and for each transform plan is defined as:
        /// 
        ///- Element depth is rendered along the Z axis when using <see cref="PhysicsWorld.TransformPlane.XY"/>.
        ///- Element depth is rendered along the Y axis when using <see cref="PhysicsWorld.TransformPlane.XZ"/>.
        ///- Element depth is rendered along the X axis when using <see cref="PhysicsWorld.TransformPlane.ZY"/>.
        ///
        /// You should set the element depth before performing any custom draw.
        ///
        /// The element depth will be reset to zero when rendering is complete.
        /// </summary>
        public readonly float elementDepth { get => PhysicsWorld_GetElementDepth(this); set => PhysicsWorld_SetElementDepth(this, value); }

        /// <summary>
        /// Set the element depth using the specified 3D position. The relevant axis will be extracted using the current <see cref="PhysicsWorld.transformPlane"/>.
        ///
        /// For more details, see <see cref="PhysicsWorld.elementDepth"/>.
        /// </summary>
        /// <param name="position">The 3D position to extract the element depth from.</param>
        public readonly void SetElementDepth3D(Vector3 position) => elementDepth = PhysicsMath.GetTranslationIgnoredAxis(position, transformPlane);

        /// <summary>
        /// Clear all the custom drawn items.
        /// </summary>
        public readonly void ClearDraw() => PhysicsWorld_ClearDraw(this, clearWorldDraw: false, clearCustomDraw: true, clearTimedDraw: true);

        /// <summary>
        /// Draw the specified Circle Geometry.
        /// </summary>
        /// <param name="geometry">The geometry to draw.</param>
        /// <param name="transform">The transform to use on the specified geometry.</param>
        /// <param name="color">The color to draw with. Here, the color alpha is used only for the interior fill color but will never be completely opaque.</param>
        /// <param name="lifetime">How long the element should be drawn for, in seconds. The default is zero indicating that it should only be drawn once. Lifetime is only used when the world is playing.</param>
        /// <param name="drawFillOptions">Controls what aspects of the primitive is drawn.</param>
        public unsafe readonly void DrawGeometry(CircleGeometry geometry, PhysicsTransform transform, Color color, float lifetime = 0.0f, DrawFillOptions drawFillOptions = DrawFillOptions.All) => PhysicsWorld_DrawCircleGeometrySpan(this, new ReadOnlySpan<CircleGeometry>(&geometry, 1), transform, color, lifetime, drawFillOptions);

        /// <summary>
        /// Draw the specified span of Circle Geometry.
        /// </summary>
        /// <param name="geometry">The geometry to draw.</param>
        /// <param name="transform">The transform to use on the specified geometry.</param>
        /// <param name="color">The color to draw with. Here, the color alpha is used only for the interior fill color but will never be completely opaque.</param>
        /// <param name="lifetime">How long the element should be drawn for, in seconds. The default is zero indicating that it should only be drawn once. Lifetime is only used when the world is playing.</param>
        /// <param name="drawFillOptions">Controls what aspects of the primitive is drawn.</param>
        public readonly void DrawGeometry(ReadOnlySpan<CircleGeometry> geometry, PhysicsTransform transform, Color color, float lifetime = 0.0f, DrawFillOptions drawFillOptions = DrawFillOptions.All) => PhysicsWorld_DrawCircleGeometrySpan(this, geometry, transform, color, lifetime, drawFillOptions);

        /// <summary>
        /// Draw the specified Capsule geometry.
        /// </summary>
        /// <param name="geometry">The geometry to draw.</param>
        /// <param name="transform">The transform to use on the specified geometry.</param>
        /// <param name="color">The color to draw with. Here, the color alpha is used only for the interior fill color but will never be completely opaque.</param>
        /// <param name="lifetime">How long the element should be drawn for, in seconds. The default is zero indicating that it should only be drawn once. Lifetime is only used when the world is playing.</param>
        /// <param name="drawFillOptions">Controls what aspects of the primitive is drawn.</param>
        public unsafe readonly void DrawGeometry(CapsuleGeometry geometry, PhysicsTransform transform, Color color, float lifetime = 0.0f, DrawFillOptions drawFillOptions = DrawFillOptions.All) => PhysicsWorld_DrawCapsuleGeometrySpan(this, new ReadOnlySpan<CapsuleGeometry>(&geometry, 1), transform, color, lifetime, drawFillOptions);

        /// <summary>
        /// Draw the specified span of Capsule geometry.
        /// </summary>
        /// <param name="geometry">The geometry to draw.</param>
        /// <param name="transform">The transform to use on the specified geometry.</param>
        /// <param name="color">The color to draw with. Here, the color alpha is used only for the interior fill color but will never be completely opaque.</param>
        /// <param name="lifetime">How long the element should be drawn for, in seconds. The default is zero indicating that it should only be drawn once. Lifetime is only used when the world is playing.</param>
        /// <param name="drawFillOptions">Controls what aspects of the primitive is drawn.</param>
        public readonly void DrawGeometry(ReadOnlySpan<CapsuleGeometry> geometry, PhysicsTransform transform, Color color, float lifetime = 0.0f, DrawFillOptions drawFillOptions = DrawFillOptions.All) => PhysicsWorld_DrawCapsuleGeometrySpan(this, geometry, transform, color, lifetime, drawFillOptions);

        /// <summary>
        /// Draw the specified Polygon geometry.
        /// </summary>
        /// <param name="geometry">The geometry to draw.</param>
        /// <param name="transform">The transform to use on the specified geometry.</param>
        /// <param name="color">The color to draw with. Here, the color alpha is used only for the interior fill color but will never be completely opaque.</param>
        /// <param name="lifetime">How long the element should be drawn for, in seconds. The default is zero indicating that it should only be drawn once. Lifetime is only used when the world is playing.</param>
        /// <param name="drawFillOptions">Controls what aspects of the primitive is drawn.</param>
        public unsafe readonly void DrawGeometry(PolygonGeometry geometry, PhysicsTransform transform, Color color, float lifetime = 0.0f, DrawFillOptions drawFillOptions = DrawFillOptions.All) => PhysicsWorld_DrawPolygonGeometrySpan(this, new ReadOnlySpan<PolygonGeometry>(&geometry, 1), transform, color, lifetime, drawFillOptions);

        /// <summary>
        /// Draw the specified span of Polygon geometry.
        /// </summary>
        /// <param name="geometry">The geometry to draw.</param>
        /// <param name="transform">The transform to use on the specified geometry.</param>
        /// <param name="color">The color to draw with. Here, the color alpha is used only for the interior fill color but will never be completely opaque.</param>
        /// <param name="lifetime">How long the element should be drawn for, in seconds. The default is zero indicating that it should only be drawn once. Lifetime is only used when the world is playing.</param>
        /// <param name="drawFillOptions">Controls what aspects of the primitive is drawn.</param>
        public readonly void DrawGeometry(ReadOnlySpan<PolygonGeometry> geometry, PhysicsTransform transform, Color color, float lifetime = 0.0f, DrawFillOptions drawFillOptions = DrawFillOptions.All) => PhysicsWorld_DrawPolygonGeometrySpan(this, geometry, transform, color, lifetime, drawFillOptions);

        /// <summary>
        /// Draw the specified Segment geometry.
        /// </summary>
        /// <param name="geometry">The geometry to draw.</param>
        /// <param name="transform">The transform to use on the specified geometry.</param>
        /// <param name="color">The color to draw with.</param>
        /// <param name="lifetime">How long the element should be drawn for, in seconds. The default is zero indicating that it should only be drawn once. Lifetime is only used when the world is playing.</param>
        public unsafe readonly void DrawGeometry(SegmentGeometry geometry, PhysicsTransform transform, Color color, float lifetime = 0.0f) => PhysicsWorld_DrawSegmentGeometrySpan(this, new ReadOnlySpan<SegmentGeometry>(&geometry, 1), transform, color, lifetime);

        /// <summary>
        /// Draw the specified span of Segment geometry.
        /// </summary>
        /// <param name="geometry">The geometry to draw.</param>
        /// <param name="transform">The transform to use on the specified geometry.</param>
        /// <param name="color">The color to draw with.</param>
        /// <param name="lifetime">How long the element should be drawn for, in seconds. The default is zero indicating that it should only be drawn once. Lifetime is only used when the world is playing.</param>
        public readonly void DrawGeometry(ReadOnlySpan<SegmentGeometry> geometry, PhysicsTransform transform, Color color, float lifetime = 0.0f) => PhysicsWorld_DrawSegmentGeometrySpan(this, geometry, transform, color, lifetime);

        /// <summary>
        /// Draw a <see cref="PhysicsShape.ShapeProxy"/>.
        /// </summary>
        /// <param name="shapeProxy">The ShapeProxy to draw.</param>
        /// <param name="transform">The transform to use on the specified points.</param>
        /// <param name="color">The color to draw with.</param>
        /// <param name="lifetime">How long the element should be drawn for, in seconds. The default is zero indicating that it should only be drawn once. Lifetime is only used when the world is playing.</param>
        /// <param name="drawFillOptions">Controls what aspects of the primitive is drawn.</param>
        public unsafe readonly void DrawShapeProxy(PhysicsShape.ShapeProxy shapeProxy, PhysicsTransform transform, Color color, float lifetime = 0.0f, DrawFillOptions drawFillOptions = DrawFillOptions.Outline) => PhysicsWorld_DrawShapeProxySpan(this, new ReadOnlySpan<PhysicsShape.ShapeProxy>(&shapeProxy, 1), transform, color, lifetime, drawFillOptions);

        /// <summary>
        /// Draw the specified span of <see cref="PhysicsShape.ShapeProxy"/>.
        /// </summary>
        /// <param name="shapeProxies">The ShapeProxies to draw.</param>
        /// <param name="transform">The transform to use on the specified points.</param>
        /// <param name="color">The color to draw with.</param>
        /// <param name="lifetime">How long the element should be drawn for, in seconds. The default is zero indicating that it should only be drawn once. Lifetime is only used when the world is playing.</param>
        /// <param name="drawFillOptions">Controls what aspects of the primitive is drawn.</param>
        public readonly void DrawShapeProxy(ReadOnlySpan<PhysicsShape.ShapeProxy> shapeProxies, PhysicsTransform transform, Color color, float lifetime = 0.0f, DrawFillOptions drawFillOptions = DrawFillOptions.Outline) => PhysicsWorld_DrawShapeProxySpan(this, shapeProxies, transform, color, lifetime, drawFillOptions);

        /// <summary>
        /// Draw an AABB.
        /// </summary>
        /// <param name="aabb">The AABB to draw.</param>
        /// <param name="color">The color to draw with.</param>
        /// <param name="lifetime">How long the element should be drawn for, in seconds. The default is zero indicating that it should only be drawn once. Lifetime is only used when the world is playing.</param>
        /// <param name="drawFillOptions">Controls what aspects of the primitive is drawn.</param>
        public readonly void DrawAABB(PhysicsAABB aabb, Color color, float lifetime = 0.0f, DrawFillOptions drawFillOptions = DrawFillOptions.Outline) => PhysicsWorld_DrawAABB(this, aabb, color, lifetime, drawFillOptions);

        /// <summary>
        /// Draw a Box.
        /// </summary>
        /// <param name="transform">The transform to use on the specified points.</param>
        /// <param name="size">The size of the box.</param>
        /// <param name="radius">The radius of the box.</param>
        /// <param name="color">The color to draw with.</param>
        /// <param name="lifetime">How long the element should be drawn for, in seconds. The default is zero indicating that it should only be drawn once. Lifetime is only used when the world is playing.</param>
        /// <param name="drawFillOptions">Controls what aspects of the primitive is drawn.</param>
        public readonly void DrawBox(PhysicsTransform transform, Vector2 size, float radius, Color color, float lifetime = 0.0f, DrawFillOptions drawFillOptions = DrawFillOptions.Outline) => PhysicsWorld_DrawBox(this, transform, size, radius, color, lifetime, drawFillOptions);

        /// <summary>
        /// Draw a Circle outline.
        /// For further information on the parameters, see <see cref="CircleGeometry"/>.
        /// </summary>
        /// <param name="center">The center of the circle in PhysicsWorld space.</param>
        /// <param name="radius">The radius of the circle.</param>
        /// <param name="color">The color to draw with.</param>
        /// <param name="lifetime">How long the element should be drawn for, in seconds. The default is zero indicating that it should only be drawn once. Lifetime is only used when the world is playing.</param>
        /// <param name="drawFillOptions">Controls what aspects of the primitive is drawn.</param>
        public readonly void DrawCircle(Vector2 center, float radius, Color color, float lifetime = 0.0f, DrawFillOptions drawFillOptions = DrawFillOptions.Outline) => PhysicsWorld_DrawCircle(this, center, radius, color, lifetime, drawFillOptions);

        /// <summary>
        /// Draw a Capsule outline.
        /// For further information on the parameters, see <see cref="CapsuleGeometry"/>.
        /// </summary>
        /// <param name="transform">The transform to use on the specified centers.</param>
        /// <param name="center1">The local center of the first semi-circle.</param>
        /// <param name="center2">The local center of the second semi-circle.</param>
        /// <param name="radius">The radius of the capsule.</param>
        /// <param name="color">The color to draw with.</param>
        /// <param name="lifetime">How long the element should be drawn for, in seconds. The default is zero indicating that it should only be drawn once. Lifetime is only used when the world is playing.</param>
        /// <param name="drawFillOptions">Controls what aspects of the primitive is drawn.</param>
        public readonly void DrawCapsule(PhysicsTransform transform, Vector2 center1, Vector2 center2, float radius, Color color, float lifetime = 0.0f, DrawFillOptions drawFillOptions = DrawFillOptions.Outline) => PhysicsWorld_DrawCapsule(this, transform, center1, center2, radius, color, lifetime, drawFillOptions);

        /// <summary>
        /// Draw a Point.
        /// A Point is similar to a filled Circle except the radius here is specified in pixels rather than world units.
        /// </summary>
        /// <param name="position">The position of the point in PhysicsWorld space.</param>
        /// <param name="radius">The radius of the point, in pixels (approximately).</param>
        /// <param name="color">The color to draw with.</param>
        /// <param name="lifetime">How long the element should be drawn for, in seconds. The default is zero indicating that it should only be drawn once. Lifetime is only used when the world is playing.</param>
        public readonly void DrawPoint(Vector2 position, float radius, Color color, float lifetime = 0.0f) => PhysicsWorld_DrawPoint(this, position, radius, color, lifetime);

        /// <summary>
        /// Draw a Line.
        /// </summary>
        /// <param name="point0">The start of the line.</param>
        /// <param name="point1">The end of the line.</param>
        /// <param name="color">The color to draw with.</param>
        /// <param name="lifetime">How long the element should be drawn for, in seconds. The default is zero indicating that it should only be drawn once. Lifetime is only used when the world is playing.</param>
        public readonly void DrawLine(Vector2 point0, Vector2 point1, Color color, float lifetime = 0.0f) => PhysicsWorld_DrawLine(this, point0, point1, color, lifetime);

        /// <summary>
        /// Draw a set of vertices as lines joined to each other.
        /// </summary>
        /// <param name="transform">The transform to use on the specified vertices.</param>
        /// <param name="vertices">The vertices defining the lines. A minimum of two vertices must be present.</param>
        /// <param name="loop">Should the first and last vertices be joined by a line?</param>
        /// <param name="color">The color to draw with.</param>
        /// <param name="lifetime">How long the element should be drawn for, in seconds. The default is zero indicating that it should only be drawn once. Lifetime is only used when the world is playing.</param>
        public readonly void DrawLineStrip(PhysicsTransform transform, ReadOnlySpan<Vector2> vertices, bool loop, Color color, float lifetime = 0.0f) => PhysicsWorld_DrawLineStrip(this, transform, vertices, loop, color, lifetime);

        /// <summary>
        /// Draw a Transform axis.
        /// </summary>
        /// <param name="transform">The Transform axis to draw.</param>
        /// <param name="scale"></param>
        /// <param name="lifetime">How long the element should be drawn for, in seconds. The default is zero indicating that it should only be drawn once. Lifetime is only used when the world is playing.</param>
        public readonly void DrawTransformAxis(PhysicsTransform transform, float scale, float lifetime = 0.0f) => PhysicsWorld_DrawTransformAxis(this, transform, scale, lifetime);

        /// <undoc/>
        internal static void DrawAllWorlds(PhysicsAABB drawAABB) => PhysicsWorld_DrawAllWorlds(drawAABB);

        #region Query Drawing

        /// <summary>
        /// Draw the <see cref="PhysicsWorld.CastRay(PhysicsQuery.CastRayInput, PhysicsQuery.QueryFilter, PhysicsQuery.WorldCastMode, Allocator)"/> query input.
        /// </summary>
        /// <param name="input">The query input to draw.</param>
        /// <param name="color">The color to draw with.</param>
        /// <param name="lifetime">How long the element should be drawn for, in seconds. The default is zero indicating that it should only be drawn once. Lifetime is only used when the world is playing.</param>
        /// <param name="drawEnd">Whether to draw the arrow at the end of the translation or not.</param>
        public unsafe readonly void DrawQueryCastRay(PhysicsQuery.CastRayInput input, Color color, float lifetime = 0.0f, bool drawEnd = false) => DrawQueryCastRay(new ReadOnlySpan<PhysicsQuery.CastRayInput>(&input, 1), color, lifetime, drawEnd);

        /// <summary>
        /// Draw the <see cref="PhysicsWorld.CastRay(PhysicsQuery.CastRayInput, PhysicsQuery.QueryFilter, PhysicsQuery.WorldCastMode, Allocator)"/> query inputs.
        /// </summary>
        /// <param name="inputs">The query inputs to draw.</param>
        /// <param name="color">The color to draw with.</param>
        /// <param name="lifetime">How long the element should be drawn for, in seconds. The default is zero indicating that it should only be drawn once. Lifetime is only used when the world is playing.</param>
        /// <param name="drawEnd">Whether to draw the arrow at the end of the translation or not.</param>
        public readonly void DrawQueryCastRay(ReadOnlySpan<PhysicsQuery.CastRayInput> inputs, Color color, float lifetime = 0.0f, bool drawEnd = false) => PhysicsWorld_DrawQueryCastRaySpan(this, inputs, color, lifetime, drawEnd);

        /// <summary>
        /// Draw the <see cref="PhysicsWorld.CastGeometry(CircleGeometry, Vector2, PhysicsQuery.QueryFilter, PhysicsQuery.WorldCastMode, Allocator)"/> query input.
        /// </summary>
        /// <param name="geometry">The Circle geometry used to cast through the world. This must be in world-space.</param>
        /// <param name="translation">The translation relative to the geometry defining the direction the geometry will move through the world.</param>
        /// <param name="color">The color to draw with.</param>
        /// <param name="lifetime">How long the element should be drawn for, in seconds. The default is zero indicating that it should only be drawn once. Lifetime is only used when the world is playing.</param>
        /// <param name="drawFillOptions">Controls what aspects of the primitive is drawn.</param>
        /// <param name="drawEnd">Whether to draw the geometry at the end of the translation or not.</param>
        public readonly void DrawQueryCastGeometry(CircleGeometry geometry, Vector2 translation, Color color, float lifetime = 0.0f, DrawFillOptions drawFillOptions = DrawFillOptions.Outline, bool drawEnd = true) => DrawQueryCastShapeProxy(geometry, translation, color, lifetime, drawFillOptions, drawEnd);

        /// <summary>
        /// Draw the <see cref="PhysicsWorld.CastGeometry(CapsuleGeometry, Vector2, PhysicsQuery.QueryFilter, PhysicsQuery.WorldCastMode, Allocator)"/> query input.
        /// </summary>
        /// <param name="geometry">The Circle geometry used to cast through the world. This must be in world-space.</param>
        /// <param name="translation">The translation relative to the geometry defining the direction the geometry will move through the world.</param>
        /// <param name="color">The color to draw with.</param>
        /// <param name="lifetime">How long the element should be drawn for, in seconds. The default is zero indicating that it should only be drawn once. Lifetime is only used when the world is playing.</param>
        /// <param name="drawFillOptions">Controls what aspects of the primitive is drawn.</param>
        /// <param name="drawEnd">Whether to draw the geometry at the end of the translation or not.</param>
        public readonly void DrawQueryCastGeometry(CapsuleGeometry geometry, Vector2 translation, Color color, float lifetime = 0.0f, DrawFillOptions drawFillOptions = DrawFillOptions.Outline, bool drawEnd = true) => DrawQueryCastShapeProxy(geometry, translation, color, lifetime, drawFillOptions, drawEnd);

        /// <summary>
        /// Draw the <see cref="PhysicsWorld.CastGeometry(PolygonGeometry, Vector2, PhysicsQuery.QueryFilter, PhysicsQuery.WorldCastMode, Allocator)"/> query input.
        /// </summary>
        /// <param name="geometry">The Circle geometry used to cast through the world. This must be in world-space.</param>
        /// <param name="translation">The translation relative to the geometry defining the direction the geometry will move through the world.</param>
        /// <param name="color">The color to draw with.</param>
        /// <param name="lifetime">How long the element should be drawn for, in seconds. The default is zero indicating that it should only be drawn once. Lifetime is only used when the world is playing.</param>
        /// <param name="drawFillOptions">Controls what aspects of the primitive is drawn.</param>
        /// <param name="drawEnd">Whether to draw the geometry at the end of the translation or not.</param>
        public readonly void DrawQueryCastGeometry(PolygonGeometry geometry, Vector2 translation, Color color, float lifetime = 0.0f, DrawFillOptions drawFillOptions = DrawFillOptions.Outline, bool drawEnd = true) => DrawQueryCastShapeProxy(geometry, translation, color, lifetime, drawFillOptions, drawEnd);

        /// <summary>
        /// Draw the <see cref="PhysicsWorld.CastShape(PhysicsShape, Vector2, PhysicsQuery.QueryFilter, PhysicsQuery.WorldCastMode, Allocator)"/> query input.
        /// </summary>
        /// <param name="shape">The shape used to cast through the world.</param>
        /// <param name="translation">The translation relative to the shape pose defining the direction the geometry will move through the world.</param>
        /// <param name="color">The color to draw with.</param>
        /// <param name="lifetime">How long the element should be drawn for, in seconds. The default is zero indicating that it should only be drawn once. Lifetime is only used when the world is playing.</param>
        /// <param name="drawFillOptions">Controls what aspects of the primitive is drawn.</param>
        /// <param name="drawEnd">Whether to draw the shape at the end of the translation or not.</param>
        /// <exception cref="System.ArgumentException">Thrown if an invalid shape type is used, specifically if <see cref="PhysicsShape.ShapeType.Segment"/> or <see cref="PhysicsShape.ShapeType.ChainSegment"/> shape types are used.</exception>
        public readonly void DrawQueryCastShape(PhysicsShape shape, Vector2 translation, Color color, float lifetime = 0.0f, DrawFillOptions drawFillOptions = DrawFillOptions.Outline, bool drawEnd = true)
        {
            switch(shape.shapeType)
            {
                case PhysicsShape.ShapeType.Circle:
                case PhysicsShape.ShapeType.Capsule:
                case PhysicsShape.ShapeType.Polygon:
                {
                    DrawQueryCastShapeProxy(shape.CreateShapeProxy(useWorldSpace: true), translation, color, lifetime, drawFillOptions, drawEnd);
                    break;
                }

                default:
                    throw new ArgumentException("Invalid shape type used for shape.", nameof(shape));
            }
        }

        /// <summary>
        /// Draw the <see cref="PhysicsWorld.CastShapeProxy(PhysicsShape.ShapeProxy, Vector2, PhysicsQuery.QueryFilter, PhysicsQuery.WorldCastMode, Allocator)"/> query input.
        /// </summary>
        /// <param name="shapeProxy">The shape proxy to use. This must be in world-space.</param>
        /// <param name="translation">The translation relative to the shape proxy defining the direction the shape proxy will move through the world.</param>
        /// <param name="color">The color to draw with.</param>
        /// <param name="lifetime">How long the element should be drawn for, in seconds. The default is zero indicating that it should only be drawn once. Lifetime is only used when the world is playing.</param>
        /// <param name="drawFillOptions">Controls what aspects of the primitive is drawn.</param>
        /// <param name="drawEnd">Whether to draw the shape proxy at the end of the translation or not.</param>
        /// <exception cref="System.ArgumentException">Thrown if an invalid shape type for the shape proxy is used, specifically if <see cref="PhysicsShape.ShapeType.Segment"/> or <see cref="PhysicsShape.ShapeType.ChainSegment"/> shape types are used.</exception>
        public readonly void DrawQueryCastShapeProxy(PhysicsShape.ShapeProxy shapeProxy, Vector2 translation, Color color, float lifetime = 0.0f, DrawFillOptions drawFillOptions = DrawFillOptions.Outline, bool drawEnd = true)
        {
            var shapeType = shapeProxy.shapeType;
            if (shapeType == PhysicsShape.ShapeType.Segment || shapeType == PhysicsShape.ShapeType.ChainSegment)
                throw new ArgumentException("Invalid shape type used for shape proxy.", nameof(shapeProxy));

            PhysicsWorld_DrawQueryCastShapeProxy(this, shapeProxy, translation, color, lifetime, drawFillOptions, drawEnd);
        }

        /// <summary>
        /// Draw the <see cref="PhysicsQuery.CastResult"/> returned from multiple queries.
        /// Only a result where <see cref="PhysicsQuery.CastResult.hit"/> is true is drawn.
        /// </summary>
        /// <param name="result">The result to use.</param>
        /// <param name="color">The color to draw with.</param>
        /// <param name="lifetime">How long the element should be drawn for, in seconds. The default is zero indicating that it should only be drawn once. Lifetime is only used when the world is playing.</param>
        /// <param name="drawPoint">Whether to draw the point in the result or not.</param>
        /// <param name="drawNormal">Whether to draw the normal in the result or not.</param>
        public unsafe readonly void DrawQueryResult(PhysicsQuery.CastResult result, Color color, float lifetime = 0.0f, bool drawPoint = true, bool drawNormal = true) => DrawQueryResult(new ReadOnlySpan<PhysicsQuery.CastResult>(&result, 1), color, lifetime, drawPoint, drawNormal);

        /// <summary>
        /// Draw the <see cref="PhysicsQuery.CastResult"/> returned from multiple queries.
        /// Only a result where <see cref="PhysicsQuery.CastResult.hit"/> is true is drawn.
        /// </summary>
        /// <param name="results">The results to use.</param>
        /// <param name="color">The color to draw with.</param>
        /// <param name="lifetime">How long the element should be drawn for, in seconds. The default is zero indicating that it should only be drawn once. Lifetime is only used when the world is playing.</param>
        /// <param name="drawPoint">Whether to draw the point in the result or not.</param>
        /// <param name="drawNormal">Whether to draw the normal in the result or not.</param>
        public readonly void DrawQueryResult(ReadOnlySpan<PhysicsQuery.CastResult> results, Color color, float lifetime = 0.0f, bool drawPoint = true, bool drawNormal = true) => PhysicsWorld_DrawQueryCastResult(this, results, color, lifetime, drawPoint, drawNormal);

        /// <summary>
        /// Draw the <see cref="PhysicsQuery.WorldCastResult"/> returned from multiple queries.
        /// </summary>
        /// <param name="result">The result to use.</param>
        /// <param name="color">The color to draw with.</param>
        /// <param name="lifetime">How long the element should be drawn for, in seconds. The default is zero indicating that it should only be drawn once. Lifetime is only used when the world is playing.</param>
        /// <param name="drawPoint">Whether to draw the point in the result or not.</param>
        /// <param name="drawNormal">Whether to draw the normal in the result or not.</param>
        public unsafe readonly void DrawQueryResult(PhysicsQuery.WorldCastResult result, Color color, float lifetime = 0.0f, bool drawPoint = true, bool drawNormal = true) => DrawQueryResult(new ReadOnlySpan<PhysicsQuery.WorldCastResult>(&result, 1), color, lifetime, drawPoint, drawNormal);

        /// <summary>
        /// Draw the <see cref="PhysicsQuery.WorldCastResult"/> returned from multiple queries.
        /// </summary>
        /// <param name="results">The results to use.</param>
        /// <param name="color">The color to draw with.</param>
        /// <param name="lifetime">How long the element should be drawn for, in seconds. The default is zero indicating that it should only be drawn once. Lifetime is only used when the world is playing.</param>
        /// <param name="drawPoint">Whether to draw the point in the result or not.</param>
        /// <param name="drawNormal">Whether to draw the normal in the result or not.</param>
        public readonly void DrawQueryResult(ReadOnlySpan<PhysicsQuery.WorldCastResult> results, Color color, float lifetime = 0.0f, bool drawPoint = true, bool drawNormal = true) => PhysicsWorld_DrawQueryWorldCastResult(this, results, color, lifetime, drawPoint, drawNormal);

        #endregion

        #endregion
    }
}
