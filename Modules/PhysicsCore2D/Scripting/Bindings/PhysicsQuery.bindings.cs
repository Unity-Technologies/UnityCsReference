// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace Unity.U2D.Physics
{
    static partial class Scripting2D
    {
        [NativeMethod(Name = "PhysicsQuery::ShapeAndShape", IsThreadSafe = true)] extern internal static PhysicsShape.ContactManifold PhysicsQuery_ShapeAndShape(PhysicsShape shapeA, PhysicsTransform transformA, PhysicsShape shapeB, PhysicsTransform transformB);
        [NativeMethod(Name = "PhysicsQuery::CircleAndCircle", IsThreadSafe = true)] extern internal static PhysicsShape.ContactManifold PhysicsQuery_CircleAndCircle(CircleGeometry geometryA, PhysicsTransform transformA, CircleGeometry geometryB, PhysicsTransform transformB);
        [NativeMethod(Name = "PhysicsQuery::CapsuleAndCircle", IsThreadSafe = true)] extern internal static PhysicsShape.ContactManifold PhysicsQuery_CapsuleAndCircle(CapsuleGeometry geometryA, PhysicsTransform transformA, CircleGeometry geometryB, PhysicsTransform transformB);
        [NativeMethod(Name = "PhysicsQuery::SegmentAndCircle", IsThreadSafe = true)] extern internal static PhysicsShape.ContactManifold PhysicsQuery_SegmentAndCircle(SegmentGeometry geometryA, PhysicsTransform transformA, CircleGeometry geometryB, PhysicsTransform transformB);
        [NativeMethod(Name = "PhysicsQuery::PolygonAndCircle", IsThreadSafe = true)] extern internal static PhysicsShape.ContactManifold PhysicsQuery_PolygonAndCircle(PolygonGeometry geometryA, PhysicsTransform transformA, CircleGeometry geometryB, PhysicsTransform transformB);
        [NativeMethod(Name = "PhysicsQuery::CapsuleAndCapsule", IsThreadSafe = true)] extern internal static PhysicsShape.ContactManifold PhysicsQuery_CapsuleAndCapsule(CapsuleGeometry geometryA, PhysicsTransform transformA, CapsuleGeometry geometryB, PhysicsTransform transformB);
        [NativeMethod(Name = "PhysicsQuery::SegmentAndCapsule", IsThreadSafe = true)] extern internal static PhysicsShape.ContactManifold PhysicsQuery_SegmentAndCapsule(SegmentGeometry geometryA, PhysicsTransform transformA, CapsuleGeometry geometryB, PhysicsTransform transformB);
        [NativeMethod(Name = "PhysicsQuery::PolygonAndCapsule", IsThreadSafe = true)] extern internal static PhysicsShape.ContactManifold PhysicsQuery_PolygonAndCapsule(PolygonGeometry geometryA, PhysicsTransform transformA, CapsuleGeometry geometryB, PhysicsTransform transformB);
        [NativeMethod(Name = "PhysicsQuery::PolygonAndPolygon", IsThreadSafe = true)] extern internal static PhysicsShape.ContactManifold PhysicsQuery_PolygonAndPolygon(PolygonGeometry geometryA, PhysicsTransform transformA, PolygonGeometry geometryB, PhysicsTransform transformB);
        [NativeMethod(Name = "PhysicsQuery::SegmentAndPolygon", IsThreadSafe = true)] extern internal static PhysicsShape.ContactManifold PhysicsQuery_SegmentAndPolygon(SegmentGeometry geometryA, PhysicsTransform transformA, PolygonGeometry geometryB, PhysicsTransform transformB);
        [NativeMethod(Name = "PhysicsQuery::ChainSegmentAndCircle", IsThreadSafe = true)] extern internal static PhysicsShape.ContactManifold PhysicsQuery_ChainSegmentAndCircle(ChainSegmentGeometry geometryA, PhysicsTransform transformA, CircleGeometry geometryB, PhysicsTransform transformB);
        [NativeMethod(Name = "PhysicsQuery::ChainSegmentAndCapsule", IsThreadSafe = true)] extern internal static PhysicsShape.ContactManifold PhysicsQuery_ChainSegmentAndCapsule(ChainSegmentGeometry geometryA, PhysicsTransform transformA, CapsuleGeometry geometryB, PhysicsTransform transformB);
        [NativeMethod(Name = "PhysicsQuery::ChainSegmentAndPolygon", IsThreadSafe = true)] extern internal static PhysicsShape.ContactManifold PhysicsQuery_ChainSegmentAndPolygon(ChainSegmentGeometry geometryA, PhysicsTransform transformA, PolygonGeometry geometryB, PhysicsTransform transformB);
        [NativeMethod(Name = "PhysicsQuery::CastShapes", IsThreadSafe = true)] extern internal static PhysicsQuery.CastResult PhysicsQuery_CastShapes(PhysicsQuery.CastShapePairInput input);
        [NativeMethod(Name = "PhysicsQuery::SegmentDistance", IsThreadSafe = true)] extern internal static PhysicsQuery.SegmentDistanceResult PhysicsQuery_SegmentDistance(SegmentGeometry geometryA, PhysicsTransform transformA, SegmentGeometry geometryB, PhysicsTransform transformB);
        [NativeMethod(Name = "PhysicsQuery::ShapeDistance", IsThreadSafe = true)] extern internal static PhysicsQuery.DistanceResult PhysicsQuery_ShapeDistance(PhysicsQuery.DistanceInput distanceInput);
        [NativeMethod(Name = "PhysicsQuery::ShapeTimeOfImpact", IsThreadSafe = true)] extern internal static PhysicsQuery.TimeOfImpactResult PhysicsQuery_ShapeTimeOfImpact(PhysicsQuery.TimeOfImpactInput toiInput);
    }
}
