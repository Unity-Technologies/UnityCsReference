// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Internal;
using static Unity.U2D.Physics.Scripting2D;

namespace Unity.U2D.Physics
{
    public readonly partial struct PhysicsWorld
    {
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
    }

    public partial struct PhysicsWorldDefinition
    {
        [ExcludeFromDocs]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("PhysicsWorldDefinition.simulationMode is obsolete. Please use PhysicsWorldDefinition.simulateType instead.", true)]
        public SimulationMode2D simulationMode { readonly get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

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
        [Obsolete("PhysicsBody.GetPositionAndRotation3D is deprecated. Please use PhysicsBody.ReadPose instead as it does not require passing transform write details but will instead implicit user them..", false)]
        public readonly void GetPositionAndRotation3D(Transform transform, PhysicsWorld.TransformWriteMode transformWriteMode, PhysicsWorld.TransformPlane transformPlane, out Vector3 position, out Quaternion rotation) => ReadPose(transform, out position, out rotation);

        [ExcludeFromDocs]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("PhysicsBody.SetAndWriteTransform is deprecated. Please use PhysicsBody.transform and PhysicsBody.WritePose instead which offers more utility by allowing the body transform to be set separately from writing the pose.", false)]
        public readonly bool SetAndWriteTransform(PhysicsTransform transform) { this.transform = transform; return WritePose(); }
    }

    public partial struct PhysicsBodyDefinition
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

        public partial struct SurfaceMaterial
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

    public partial struct SegmentGeometry
    {
        [ExcludeFromDocs]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("SegmentGeometry.ClosestPoint(PhysicsTransform, point) is deprecated, please transform the point with PhysicsTransform.TransformPoint(point) and use SegmentGeometry.ClosestPoint(point) instead.", false)]
        public readonly Vector2 ClosestPoint(PhysicsTransform transform, Vector2 point) => ClosestPoint(transform.TransformPoint(point));
    }

    public partial struct ChainSegmentGeometry
    {
        [ExcludeFromDocs]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("ChainSegmentGeometry.ClosestPoint(PhysicsTransform, point) is deprecated, please transform the point with PhysicsTransform.TransformPoint(point) and use ChainSegmentGeometry.ClosestPoint(point) instead.", false)]
        public readonly Vector2 ClosestPoint(PhysicsTransform transform, Vector2 point) => ClosestPoint(transform.TransformPoint(point));
    }

    public partial struct ChainGeometry
    {
        [ExcludeFromDocs]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("ChainGeometry.ClosestPoint(PhysicsTransform, point) is deprecated, please transform the point with PhysicsTransform.TransformPoint(point) and use ChainGeometry.ClosestPoint(point) instead.", false)]
        public readonly Vector2 ClosestPoint(PhysicsTransform transform, Vector2 point) => ClosestPoint(transform.TransformPoint(point));

        [ExcludeFromDocs]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("ChainGeometry.ChainGeometry(ReadOnlySpan<Vector2> vertices) is obsolete as it can lead to invalid vertices when used with managed arrays. Please use PhysicsChain.Create(PhysicsBody body, ReadOnlySpan<Vector2> vertices, PhysicsChainDefinition definition) instead.", false)]
        public unsafe ChainGeometry(ReadOnlySpan<Vector2> vertices)
        {
            // Validate.
            if (vertices.Length < 3)
                throw new ArgumentOutOfRangeException(nameof(vertices), "Chain Geometry must contain a minimum of 3 vertices.");

            fixed (Vector2* addr = vertices)
            {
                // Assign the buffer data.
                m_Points = new IntPtr(addr);
                m_Count = vertices.Length;
            }
        }
    }

    public partial struct PhysicsRotate
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

    public readonly partial struct PhysicsQuery
    {
        public readonly partial struct CastResult
        {
            [ExcludeFromDocs]
            [EditorBrowsable(EditorBrowsableState.Never)]
            [Obsolete("CastResult.hit is deprecated, please use CastResult.isValid instead. (UnityUpgradable) -> isValid", false)]
            public readonly bool hit => isValid;
        }
    }

    public readonly partial struct PhysicsConstants
    {
        [ExcludeFromDocs]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("PhysicsConstants.MaxWorlds is deprecated. The maximum number of worlds is no longer a constant but can instead be configured with PhysicsWorld.maximumWorlds.", false)]
        public const int MaxWorlds = 128;
    }
}
