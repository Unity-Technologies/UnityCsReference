// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Internal;
using static Unity.U2D.Physics.Scripting2D;

namespace Unity.U2D.Physics
{
    public readonly partial struct PhysicsWorld
    {
        [ExcludeFromDocs]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("PhysicsWorld.maximumWorldsAllocated is deprecated, please use PhysicsWorld.allocatedWorldCapacity instead. (UnityUpgradable) -> allocatedWorldCapacity", false)]
        public static int maximumWorldsAllocated => allocatedWorldCapacity;

        [ExcludeFromDocs]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("PhysicsWorld.simulationMode is obsolete. Please use PhysicsWorld.simulationType instead.", true)]
        public readonly SimulationMode2D simulationMode { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        [ExcludeFromDocs]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("PhysicsWorld.bypassLowLevel is deprecated, please use PhysicsWorld.disableSimulation instead. (UnityUpgradable) -> disableSimulation", false)]
        public static bool bypassLowLevel => disableSimulation;

        [ExcludeFromDocs]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("PhysicsWorld.useFullLayers is deprecated, please use PhysicsWorld.usePhysicsLayers instead. (UnityUpgradable) -> usePhysicsLayers", false)]
        public static bool useFullLayers => usePhysicsLayers;

        [ExcludeFromDocs]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("PhysicsWorld.drawImpulseScale is deprecated, please use PhysicsWorld.drawForceScale instead. (UnityUpgradable) -> drawForceScale", false)]
        public float drawImpulseScale { readonly get => drawForceScale; set => drawForceScale = value; }

        [ExcludeFromDocs]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("PhysicsWorld.transformTweening is deprecated, please use PhysicsWorld.transformTweenMode instead.", false)]
        public readonly bool transformTweening { get => transformTweenMode != TransformTweenMode.Off; set => transformTweenMode = value ? TransformTweenMode.Parallel : TransformTweenMode.Off; }

        [ExcludeFromDocs]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("PhysicsWorld.drawCapacity is deprecated. Draw capacity is now automatically managed.", false)]
        public readonly int drawCapacity { get => 0; set { } }

        [ExcludeFromDocs]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("PhysicsWorld.autoContactCallbacks is obsolete. There is no longer a performance benefit to disabling automatic contact callback dispatch, so contact callbacks are now always sent every simulation step. Individual shapes can still control whether they generate contact events at all via PhysicsShape.contactEvents.", true)]
        public readonly bool autoContactCallbacks { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        [ExcludeFromDocs]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("PhysicsWorld.autoTriggerCallbacks is obsolete. There is no longer a performance benefit to disabling automatic trigger callback dispatch, so trigger callbacks are now always sent every simulation step. Individual shapes can still control whether they generate trigger events at all via PhysicsShape.triggerEvents.", true)]
        public readonly bool autoTriggerCallbacks { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        [ExcludeFromDocs]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("PhysicsWorld.preSolveCallbacks is deprecated, please use PhysicsWorld.preContactCallbacks instead. (UnityUpgradable) -> preContactCallbacks", false)]
        public readonly bool preSolveCallbacks { get => preContactCallbacks; set => preContactCallbacks = value; }

        public partial record struct TransformPlaneCustom
        {
            [ExcludeFromDocs]
            [EditorBrowsable(EditorBrowsableState.Never)]
            [Obsolete("PhysicsWorld.TransformPlaneCustom(translate, rotate, scale) is deprecated, please use PhysicsWorld.TransformPlaneCustom(translate, rotate) instead. The scale is ignored: it scaled the drawing of a shape but also the geometry an authoring component built from a Transform, so a shape simulated at a different size than it appeared. Scale the authored geometry instead.", false)]
            public TransformPlaneCustom(Vector3 translate, Vector3 rotate, float scale)
                : this(translate, rotate)
            {
            }

            [ExcludeFromDocs]
            [EditorBrowsable(EditorBrowsableState.Never)]
            [Obsolete("PhysicsWorld.TransformPlaneCustom.scale is deprecated and always reads as one. It scaled the drawing of a shape but also the geometry an authoring component built from a Transform, so a shape simulated at a different size than it appeared. Scale the authored geometry instead.", false)]
            public readonly float scale => 1.0f;
        }

        public partial record struct WorldProfile
        {
            [ExcludeFromDocs]
            [EditorBrowsable(EditorBrowsableState.Never)]
            [Obsolete("PhysicsWorld.WorldProfile.prepareStages is deprecated, please use PhysicsWorld.WorldProfile.solverSetup instead. (UnityUpgradable) -> solverSetup", false)]
            public float prepareStages { readonly get => solverSetup; set => solverSetup = value; }

            [ExcludeFromDocs]
            [EditorBrowsable(EditorBrowsableState.Never)]
            [Obsolete("PhysicsWorld.WorldProfile.solveConstraints is deprecated, please use PhysicsWorld.WorldProfile.constraints instead. (UnityUpgradable) -> constraints", false)]
            public float solveConstraints { readonly get => constraints; set => constraints = value; }

            [ExcludeFromDocs]
            [EditorBrowsable(EditorBrowsableState.Never)]
            [Obsolete("PhysicsWorld.WorldProfile.applyBounciness is deprecated. Bounciness is no longer a separate solver stage so this always reads as zero.", false)]
            public float applyBounciness { readonly get => 0.0f; set {} }
        }

        public partial record struct WorldCounters
        {
            [ExcludeFromDocs]
            [EditorBrowsable(EditorBrowsableState.Never)]
            [Obsolete("PhysicsWorld.WorldCounters.memoryUsed is deprecated, please use PhysicsWorld.WorldCounters.usedMemory instead, which is a long covering the full memory range.", false)]
            public int memoryUsed { readonly get => (int)usedMemory; set => usedMemory = value; }
        }
    }

    public partial record struct PhysicsWorldDefinition
    {
        [ExcludeFromDocs]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("PhysicsWorldDefinition.simulationMode is obsolete. Please use PhysicsWorldDefinition.simulateType instead.", true)]
        public SimulationMode2D simulationMode { readonly get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        [ExcludeFromDocs]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("PhysicsWorldDefinition.simulateType is deprecated, please use PhysicsWorldDefinition.simulationType instead. (UnityUpgradable) -> simulationType", false)]
        public PhysicsWorld.SimulationType simulateType { readonly get => simulationType; set => simulationType = value; }

        [ExcludeFromDocs]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("PhysicsWorldDefinition.drawImpulseScale is deprecated, please use PhysicsWorldDefinition.drawForceScale instead. (UnityUpgradable) -> drawForceScale", false)]
        public float drawImpulseScale { readonly get => drawForceScale; set => drawForceScale = value; }

        [ExcludeFromDocs]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("PhysicsWorldDefinition.transformTweening is deprecated, please use PhysicsWorldDefinition.transformTweenMode instead.", false)]
        public bool transformTweening { readonly get => transformTweenMode != PhysicsWorld.TransformTweenMode.Off; set => transformTweenMode = value ? PhysicsWorld.TransformTweenMode.Parallel : PhysicsWorld.TransformTweenMode.Off; }

        [ExcludeFromDocs]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("PhysicsWorldDefinition.drawCapacity is deprecated. Draw capacity is now automatically managed.", false)]
        public int drawCapacity { readonly get => 0; set { } }

        [ExcludeFromDocs]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("PhysicsWorldDefinition.autoContactCallbacks is obsolete. There is no longer a performance benefit to disabling automatic contact callback dispatch, so contact callbacks are now always sent every simulation step. Individual shapes can still control whether they generate contact events at all via PhysicsShape.contactEvents.", true)]
        public bool autoContactCallbacks { readonly get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        [ExcludeFromDocs]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("PhysicsWorldDefinition.autoTriggerCallbacks is obsolete. There is no longer a performance benefit to disabling automatic trigger callback dispatch, so trigger callbacks are now always sent every simulation step. Individual shapes can still control whether they generate trigger events at all via PhysicsShape.triggerEvents.", true)]
        public bool autoTriggerCallbacks { readonly get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        [ExcludeFromDocs]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("PhysicsWorldDefinition.preSolveCallbacks is deprecated, please use PhysicsWorldDefinition.preContactCallbacks instead. (UnityUpgradable) -> preContactCallbacks", false)]
        public bool preSolveCallbacks { readonly get => preContactCallbacks; set => preContactCallbacks = value; }
    }

    public partial record struct PhysicsShapeDefinition
    {
        [ExcludeFromDocs]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("PhysicsShapeDefinition.preSolveCallbacks is deprecated, please use PhysicsShapeDefinition.preContactCallbacks instead. (UnityUpgradable) -> preContactCallbacks", false)]
        public bool preSolveCallbacks { readonly get => preContactCallbacks; set => preContactCallbacks = value; }
    }

    public readonly partial struct PhysicsBody
    {
        [ExcludeFromDocs]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("PhysicsBody.bodyType is obsolete. Please use PhysicsBody.type instead.", true)]
        public readonly RigidbodyType2D bodyType { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        [ExcludeFromDocs]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("PhysicsBody.bodyConstraints is obsolete. Please use PhysicsBody.constraints instead.", true)]
        public readonly RigidbodyConstraints2D bodyConstraints { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        [ExcludeFromDocs]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("PhysicsBody.GetPositionAndRotation3D is obsolete. Please use PhysicsBody.ReadPose instead as it does not require passing transform write details but will instead implicit user them..", true)]
        public readonly void GetPositionAndRotation3D(Transform transform, PhysicsWorld.TransformWriteMode transformWriteMode, PhysicsWorld.TransformPlane transformPlane, out Vector3 position, out Quaternion rotation) => ReadPose(transform, out position, out rotation);

        [ExcludeFromDocs]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("PhysicsBody.SetAndWriteTransform is obsolete. Please use PhysicsBody.transform and PhysicsBody.WritePose instead which offers more utility by allowing the body transform to be set separately from writing the pose.", true)]
        public readonly bool SetAndWriteTransform(PhysicsTransform transform) => throw new NotSupportedException();
    }

    public partial record struct PhysicsBodyDefinition
    {
        [ExcludeFromDocs]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("PhysicsBodyDefinition.bodyType is obsolete. Please use PhysicsBodyDefinition.type instead.", true)]
        public RigidbodyType2D bodyType { readonly get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        [ExcludeFromDocs]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("PhysicsBodyDefinition.bodyConstraints is obsolete. Please use PhysicsBodyDefinition.constraints instead.", true)]
        public RigidbodyConstraints2D bodyConstraints { readonly get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    }

    public readonly partial struct PhysicsShape
    {
        [ExcludeFromDocs]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("PhysicsShape.frictionCombine is obsolete. Please use PhysicsShape.frictionMixing instead.", true)]
        public readonly PhysicsMaterialCombine2D frictionCombine { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        [ExcludeFromDocs]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("PhysicsShape.bouncinessCombine is obsolete. Please use PhysicsShape.bouncinessMixing instead.", true)]
        public readonly PhysicsMaterialCombine2D bouncinessCombine { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        [ExcludeFromDocs]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("PhysicsShape.ApplyWind(force, drag, lift, wake) is deprecated, please use PhysicsShape.ApplyWind(PhysicsBody.WindInput) instead.", false)]
        public readonly void ApplyWind(Vector2 force, float drag, float lift, bool wake = true)
        {
            var input = new PhysicsBody.WindInput { force = force, drag = drag, lift = lift, mask = PhysicsMask.All, useTriggers = true };
            ApplyWind(input);
        }

        [ExcludeFromDocs]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("PhysicsShape.preSolveCallbacks is deprecated, please use PhysicsShape.preContactCallbacks instead. (UnityUpgradable) -> preContactCallbacks", false)]
        public readonly bool preSolveCallbacks { get => preContactCallbacks; set => preContactCallbacks = value; }

        public partial record struct SurfaceMaterial
        {
            [ExcludeFromDocs]
            [EditorBrowsable(EditorBrowsableState.Never)]
            [Obsolete("PhysicsShape.SurfaceMaterial.frictionCombine is obsolete. Please use PhysicsShape.frictionMixing instead.", true)]
            public PhysicsMaterialCombine2D frictionCombine { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

            [ExcludeFromDocs]
            [EditorBrowsable(EditorBrowsableState.Never)]
            [Obsolete("PhysicsShape.SurfaceMaterial.bouncinessCombine is obsolete. Please use PhysicsShape.bouncinessMixing instead.", true)]
            public PhysicsMaterialCombine2D bouncinessCombine { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

            [ExcludeFromDocs]
            [EditorBrowsable(EditorBrowsableState.Never)]
            [Obsolete("PhysicsShape.SurfaceMaterial.Default is deprecated. Please use PhysicsShape.SurfaceMaterial.defaultMaterial instead. (UnityUpgradable) -> defaultMaterial", false)]
            public static SurfaceMaterial Default => defaultMaterial;
        }
    }

    public readonly partial struct PhysicsChain
    {
        [ExcludeFromDocs]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("PhysicsChain.frictionCombine is obsolete. Please use PhysicsChain.frictionMixing instead.", true)]
        public readonly PhysicsMaterialCombine2D frictionCombine { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        [ExcludeFromDocs]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("PhysicsChain.bouncinessCombine is obsolete. Please use PhysicsChain.bouncinessMixing instead.", true)]
        public readonly PhysicsMaterialCombine2D bouncinessCombine { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    }

    public partial record struct SegmentGeometry
    {
        [ExcludeFromDocs]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("SegmentGeometry.ClosestPoint(PhysicsTransform, point) is deprecated, please transform the point with PhysicsTransform.TransformPoint(point) and use SegmentGeometry.ClosestPoint(point) instead.", false)]
        public readonly Vector2 ClosestPoint(PhysicsTransform transform, Vector2 point) => ClosestPoint(transform.TransformPoint(point));
    }

    public partial record struct ChainSegmentGeometry
    {
        [ExcludeFromDocs]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("ChainSegmentGeometry.ClosestPoint(PhysicsTransform, point) is deprecated, please transform the point with PhysicsTransform.TransformPoint(point) and use ChainSegmentGeometry.ClosestPoint(point) instead.", false)]
        public readonly Vector2 ClosestPoint(PhysicsTransform transform, Vector2 point) => ClosestPoint(transform.TransformPoint(point));
    }

    public partial record struct ChainGeometry
    {
        [ExcludeFromDocs]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("ChainGeometry.ClosestPoint(PhysicsTransform, point) is deprecated, please transform the point with PhysicsTransform.TransformPoint(point) and use ChainGeometry.ClosestPoint(point) instead.", false)]
        public readonly Vector2 ClosestPoint(PhysicsTransform transform, Vector2 point) => ClosestPoint(transform.TransformPoint(point));

        [ExcludeFromDocs]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("ChainGeometry.ChainGeometry(ReadOnlySpan<Vector2> vertices) is obsolete as it can lead to invalid vertices when used with managed arrays. Please use PhysicsChain.Create(PhysicsBody body, ReadOnlySpan<Vector2> vertices, PhysicsChainDefinition definition) instead.", true)]
        public ChainGeometry(ReadOnlySpan<Vector2> vertices) => throw new NotSupportedException();
    }

    public partial record struct PhysicsRotate
    {
        [ExcludeFromDocs]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("PhysicsRotate(angle) is deprecated as it is not Burst compatible. Please use PhysicsRotate.FromRadians() or PhysicsRotate.FromDegrees() instead.", false)]
        public PhysicsRotate(float angle) { this = FromRadians(angle); }

        [ExcludeFromDocs]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("PhysicsRotate.angle is deprecated. Please use PhysicsRotate.radians or PhysicsRotate.degrees instead. (UnityUpgradable) -> radians", false)]
        public readonly float angle => PhysicsRotate_GetAngle(this);
    }

    public readonly partial record struct PhysicsQuery
    {
        public readonly partial record struct CastResult
        {
            [ExcludeFromDocs]
            [EditorBrowsable(EditorBrowsableState.Never)]
            [Obsolete("CastResult.hit is deprecated, please use CastResult.isValid instead. (UnityUpgradable) -> isValid", false)]
            public readonly bool hit => isValid;
        }
    }

    public readonly partial record struct PhysicsEvents
    {
        public readonly partial record struct TransformWriteEvent
        {
            [ExcludeFromDocs]
            [EditorBrowsable(EditorBrowsableState.Never)]
            [Obsolete("PhysicsEvents.TransformWriteEvent.transfomPlaneCustom is deprecated, please use PhysicsEvents.TransformWriteEvent.transformPlaneCustom instead. (UnityUpgradable) -> transformPlaneCustom", false)]
            public readonly PhysicsWorld.TransformPlaneCustom transfomPlaneCustom => transformPlaneCustom;
        }

        public readonly partial record struct TransformTweenWriteEvent
        {
            [ExcludeFromDocs]
            [EditorBrowsable(EditorBrowsableState.Never)]
            [Obsolete("PhysicsEvents.TransformTweenWriteEvent.transfomPlaneCustom is deprecated, please use PhysicsEvents.TransformTweenWriteEvent.transformPlaneCustom instead. (UnityUpgradable) -> transformPlaneCustom", false)]
            public readonly PhysicsWorld.TransformPlaneCustom transfomPlaneCustom => transformPlaneCustom;
        }

        /// <summary>
        /// An event produced when a contact between a pair of shapes is updated, used to decide if the contact should be disabled.
        /// </summary>
        [ExcludeFromDocs]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("PhysicsEvents.PreSolveEvent is deprecated. Use PhysicsEvents.PreContactEvent with PhysicsCallbacks.IPreContactCallback, or PhysicsEvents.PreContinuousEvent with PhysicsCallbacks.IPreContinuousCallback.", false)]
        [StructLayout(LayoutKind.Sequential)]
        public readonly record struct PreSolveEvent
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

            // Built on the managed side when a target only implements the deprecated callback, mapping the new events back onto the old shape.
            internal PreSolveEvent(PhysicsWorld physicsWorld, PhysicsShape shapeA, PhysicsShape shapeB, Vector2 point, Vector2 normal)
            {
                m_PhysicsWorld = physicsWorld;
                m_ShapeA = shapeA;
                m_ShapeB = shapeB;
                m_Point = point;
                m_Normal = normal;
            }

            #region Internal

            readonly PhysicsWorld m_PhysicsWorld;
            readonly PhysicsShape m_ShapeA;
            readonly PhysicsShape m_ShapeB;
            readonly Vector2 m_Point;
            readonly Vector2 m_Normal;

            #endregion
        }
    }

    public readonly partial record struct PhysicsCallbacks
    {
        /// <summary>
        /// An interface that when implemented, is called as a target when a shape and its world both have their pre-contact callbacks enabled.
        /// </summary>
        [ExcludeFromDocs]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("PhysicsCallbacks.IPreSolveCallback is deprecated. Implement PhysicsCallbacks.IPreContactCallback to modify or cancel a contact, and PhysicsCallbacks.IPreContinuousCallback to control continuous collision stops. This interface keeps working but is only used when neither new interface is implemented on the same target.", false)]
        public interface IPreSolveCallback
        {
            /// <summary>
            /// Called when a contact between a pair of shapes is updated, allowing the contact to be disabled before it goes to the solver.
            /// Returning false disables the contact this simulation step, and returning true allows it.
            /// </summary>
            /// <param name="preSolveEvent">The event that occurred.</param>
            /// <returns>Return false to disable the contact this simulation step, or true to allow it.</returns>
            bool OnPreSolve2D(PhysicsEvents.PreSolveEvent preSolveEvent);
        }
    }

    public partial record struct PhysicsAABB
    {
        [ExcludeFromDocs]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("PhysicsAABB.Normalized() is deprecated, please use PhysicsAABB.Normalize() instead. (UnityUpgradable) -> Normalize()", false)]
        public void Normalized() => Normalize();
    }

    public readonly partial record struct PhysicsConstants
    {
        [ExcludeFromDocs]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("PhysicsConstants.MaxWorlds is deprecated. The maximum number of worlds is no longer a constant but can instead be configured with PhysicsCoreSettings2D.initialWorldCapacity.", false)]
        public const int MaxWorlds = 128;
    }

    public sealed partial class PhysicsCoreSettings2D
    {
        [ExcludeFromDocs]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("PhysicsCoreSettings2D.maximumWorlds is deprecated, please use PhysicsCoreSettings2D.initialWorldCapacity instead. (UnityUpgradable) -> initialWorldCapacity", false)]
        public int maximumWorlds { get => initialWorldCapacity; set => initialWorldCapacity = value; }
    }
}
