// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Bindings;

namespace Unity.U2D.Physics
{
    internal static partial class Scripting2D
    {
        // Circle Geometry.
        [NativeMethod(Name = "CircleGeometry::IsValid", IsThreadSafe = true)] extern internal static bool CircleGeometry_IsValid(CircleGeometry geometry);
        [NativeMethod(Name = "CircleGeometry::CalculateMass", IsThreadSafe = true)] extern internal static PhysicsBody.MassConfiguration CircleGeometry_CalculateMassConfiguration(CircleGeometry geometry, float density);
        [NativeMethod(Name = "CircleGeometry::CalculateAABB", IsThreadSafe = true)] extern internal static PhysicsAABB CircleGeometry_CalculateAABB(CircleGeometry geometry, PhysicsTransform transform);
        [NativeMethod(Name = "CircleGeometry::OverlapPoint", IsThreadSafe = true)] extern internal static bool CircleGeometry_OverlapPoint(CircleGeometry geometry, Vector2 point);
        [NativeMethod(Name = "CircleGeometry::ClosestPoint", IsThreadSafe = true)] extern internal static Vector2 CircleGeometry_ClosestPoint(CircleGeometry geometry, Vector2 point);
        [NativeMethod(Name = "CircleGeometry::CastRay", IsThreadSafe = true)] extern internal static PhysicsQuery.CastResult CircleGeometry_CastRay(CircleGeometry geometry, PhysicsQuery.CastRayInput input);
        [NativeMethod(Name = "CircleGeometry::CastShape", IsThreadSafe = true)] extern internal static PhysicsQuery.CastResult CircleGeometry_CastShape(CircleGeometry geometry, PhysicsQuery.CastShapeInput input);

        // Capsule Geometry.
        [NativeMethod(Name = "CapsuleGeometry::IsValid", IsThreadSafe = true)] extern internal static bool CapsuleGeometry_IsValid(CapsuleGeometry geometry);
        [NativeMethod(Name = "CapsuleGeometry::CalculateMass", IsThreadSafe = true)] extern internal static PhysicsBody.MassConfiguration CapsuleGeometry_CalculateMassConfiguration(CapsuleGeometry geometry, float density);
        [NativeMethod(Name = "CapsuleGeometry::CalculateAABB", IsThreadSafe = true)] extern internal static PhysicsAABB CapsuleGeometry_CalculateAABB(CapsuleGeometry geometry, PhysicsTransform transform);
        [NativeMethod(Name = "CapsuleGeometry::Validate", IsThreadSafe = true)] extern internal static CapsuleGeometry CapsuleGeometry_Validate(CapsuleGeometry geometry);
        [NativeMethod(Name = "CapsuleGeometry::OverlapPoint", IsThreadSafe = true)] extern internal static bool CapsuleGeometry_OverlapPoint(CapsuleGeometry geometry, Vector2 point);
        [NativeMethod(Name = "CapsuleGeometry::ClosestPoint", IsThreadSafe = true)] extern internal static Vector2 CapsuleGeometry_ClosestPoint(CapsuleGeometry geometry, Vector2 point);
        [NativeMethod(Name = "CapsuleGeometry::CastRay", IsThreadSafe = true)] extern internal static PhysicsQuery.CastResult CapsuleGeometry_CastRay(CapsuleGeometry geometry, PhysicsQuery.CastRayInput input);
        [NativeMethod(Name = "CapsuleGeometry::CastShape", IsThreadSafe = true)] extern internal static PhysicsQuery.CastResult CapsuleGeometry_CastShape(CapsuleGeometry geometry, PhysicsQuery.CastShapeInput input);

        // Polygon Geometry.
        [NativeMethod(Name = "PolygonGeometry::CreateBox", IsThreadSafe = true)] extern internal static PolygonGeometry PolygonGeometry_CreateBox(Vector2 size, float radius, PhysicsTransform transform, bool inscribe);
        [NativeMethod(Name = "PolygonGeometry::CreatePolygons", IsThreadSafe = true)] extern internal static PhysicsBuffer PolygonGeometry_CreatePolygons(ReadOnlySpan<Vector2> vertices, PhysicsTransform transform, Vector2 vertexScale, float radius, bool useDelaunay, Allocator allocator);
        [NativeMethod(Name = "PolygonGeometry::Create_WithPhysicsTransform", IsThreadSafe = true)] extern internal static PolygonGeometry PolygonGeometry_Create_WithPhysicsTransform(ReadOnlySpan<Vector2> vertices, float radius, PhysicsTransform transform);
        [NativeMethod(Name = "PolygonGeometry::Create_WithMatrix", IsThreadSafe = true)] extern internal static PolygonGeometry PolygonGeometry_Create_WithMatrix(ReadOnlySpan<Vector2> vertices, float radius, Matrix4x4 transform);
        [NativeMethod(Name = "PolygonGeometry::Transform_WithPhysicsTransform", IsThreadSafe = true)] extern internal static PolygonGeometry PolygonGeometry_Transform_WithPhysicsTransform(PolygonGeometry geometry, PhysicsTransform transform);
        [NativeMethod(Name = "PolygonGeometry::InverseTransform_WithPhysicsTransform", IsThreadSafe = true)] extern internal static PolygonGeometry PolygonGeometry_InverseTransform_WithPhysicsTransform(PolygonGeometry geometry, PhysicsTransform transform);
        [NativeMethod(Name = "PolygonGeometry::Transform_WithMatrix", IsThreadSafe = true)] extern internal static PolygonGeometry PolygonGeometry_Transform_WithMatrix(PolygonGeometry geometry, Matrix4x4 transform, bool scaleRadius);
        [NativeMethod(Name = "PolygonGeometry::InverseTransform_WithMatrix", IsThreadSafe = true)] extern internal static PolygonGeometry PolygonGeometry_InverseTransform_WithMatrix(PolygonGeometry geometry, Matrix4x4 transform, bool scaleRadius);
        [NativeMethod(Name = "PolygonGeometry::IsValid", IsThreadSafe = true)] extern internal static bool PolygonGeometry_IsValid(PolygonGeometry geometry);
        [NativeMethod(Name = "PolygonGeometry::Validate", IsThreadSafe = true)] extern internal static PolygonGeometry PolygonGeometry_Validate(PolygonGeometry geometry);
        [NativeMethod(Name = "PolygonGeometry::CalculateMass", IsThreadSafe = true)] extern internal static PhysicsBody.MassConfiguration PolygonGeometry_CalculateMassConfiguration(PolygonGeometry geometry, float density);
        [NativeMethod(Name = "PolygonGeometry::CalculateAABB", IsThreadSafe = true)] extern internal static PhysicsAABB PolygonGeometry_CalculateAABB(PolygonGeometry geometry, PhysicsTransform transform);
        [NativeMethod(Name = "PolygonGeometry::OverlapPoint", IsThreadSafe = true)] extern internal static bool PolygonGeometry_OverlapPoint(PolygonGeometry geometry, Vector2 point);
        [NativeMethod(Name = "PolygonGeometry::ClosestPoint", IsThreadSafe = true)] extern internal static Vector2 PolygonGeometry_ClosestPoint(PolygonGeometry geometry, Vector2 point);
        [NativeMethod(Name = "PolygonGeometry::CastRay", IsThreadSafe = true)] extern internal static PhysicsQuery.CastResult PolygonGeometry_CastRay(PolygonGeometry geometry, PhysicsQuery.CastRayInput input);
        [NativeMethod(Name = "PolygonGeometry::CastShape", IsThreadSafe = true)] extern internal static PhysicsQuery.CastResult PolygonGeometry_CastShape(PolygonGeometry geometry, PhysicsQuery.CastShapeInput input);

        // Segment Geometry.
        [NativeMethod(Name = "SegmentGeometry::IsValid", IsThreadSafe = true)] extern internal static bool SegmentGeometry_IsValid(SegmentGeometry geometry);
        [NativeMethod(Name = "SegmentGeometry::CalculateAABB", IsThreadSafe = true)] extern internal static PhysicsAABB SegmentGeometry_CalculateAABB(SegmentGeometry geometry, PhysicsTransform transform);
        [NativeMethod(Name = "SegmentGeometry::ClosestPoint", IsThreadSafe = true)] extern internal static Vector2 SegmentGeometry_ClosestPoint(SegmentGeometry geometry, Vector2 point);
        [NativeMethod(Name = "SegmentGeometry::CastRay", IsThreadSafe = true)] extern internal static PhysicsQuery.CastResult SegmentGeometry_CastRay(SegmentGeometry geometry, PhysicsQuery.CastRayInput input, bool oneSided);
        [NativeMethod(Name = "SegmentGeometry::CastShape", IsThreadSafe = true)] extern internal static PhysicsQuery.CastResult SegmentGeometry_CastShape(SegmentGeometry geometry, PhysicsQuery.CastShapeInput input);

        // Chain-Segment Geometry.
        [NativeMethod(Name = "ChainSegmentGeometry::IsValid", IsThreadSafe = true)] extern internal static bool ChainSegmentGeometry_IsValid(ChainSegmentGeometry geometry);
        [NativeMethod(Name = "ChainSegmentGeometry::CalculateAABB", IsThreadSafe = true)] extern internal static PhysicsAABB ChainSegmentGeometry_CalculateAABB(ChainSegmentGeometry geometry, PhysicsTransform transform);
        [NativeMethod(Name = "ChainSegmentGeometry::ClosestPoint", IsThreadSafe = true)] extern internal static Vector2 ChainSegmentGeometry_ClosestPoint(ChainSegmentGeometry geometry, Vector2 point);
        [NativeMethod(Name = "ChainSegmentGeometry::CastRay", IsThreadSafe = true)] extern internal static PhysicsQuery.CastResult ChainSegmentGeometry_CastRay(ChainSegmentGeometry geometry, PhysicsQuery.CastRayInput input, bool oneSided);
        [NativeMethod(Name = "ChainSegmentGeometry::CastShape", IsThreadSafe = true)] extern internal static PhysicsQuery.CastResult ChainSegmentGeometry_CastShape(ChainSegmentGeometry geometry, PhysicsQuery.CastShapeInput input);

        // Chain Geometry.
        [NativeMethod(Name = "ChainGeometry::IsValid", IsThreadSafe = true)] extern internal static bool ChainGeometry_IsValid(ChainGeometry geometry);
        [NativeMethod(Name = "ChainGeometry::CalculateAABB", IsThreadSafe = true)] extern internal static PhysicsAABB ChainGeometry_CalculateAABB(ChainGeometry geometry, PhysicsTransform transform);
        [NativeMethod(Name = "ChainGeometry::ClosestPoint", IsThreadSafe = true)] extern internal static Vector2 ChainGeometry_ClosestPoint(ChainGeometry geometry, Vector2 point);
        [NativeMethod(Name = "ChainGeometry::CastRay", IsThreadSafe = true)] extern internal static PhysicsQuery.CastResult ChainGeometry_CastRay(ChainGeometry geometry, PhysicsQuery.CastRayInput input, bool oneSided);
        [NativeMethod(Name = "ChainGeometry::CastShape", IsThreadSafe = true)] extern internal static PhysicsQuery.CastResult ChainGeometry_CastShape(ChainGeometry geometry, PhysicsQuery.CastShapeInput input);
    }
}
