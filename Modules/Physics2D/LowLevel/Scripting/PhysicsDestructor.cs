// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;

using Unity.Collections;

using static UnityEngine.LowLevelPhysics2D.PhysicsLowLevelScripting2D;
using static UnityEngine.LowLevelPhysics2D.PhysicsDestructorScripting2D;

namespace UnityEngine.LowLevelPhysics2D
{
    /// <summary>
    /// Provides the ability to destruct (fragment and Slice) geometry.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct PhysicsDestructor
    {
        /// <summary>
        /// The polygon geometry used when fragmenting.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public readonly struct FragmentGeometry
        {
            /// <summary>
            /// Create fragment geometry.
            /// </summary>
            /// <param name="transform">The transform used to transform the specified Polygon geometry.</param>
            /// <param name="geometry">The Polygon geometry to use.</param>
            public FragmentGeometry(PhysicsTransform transform, ReadOnlySpan<PolygonGeometry> geometry)
            {
                m_Transform = transform;
                m_Geometry = PhysicsBuffer.FromSpan(geometry);
            }

            #region Internal

            readonly PhysicsTransform m_Transform;
            readonly PhysicsBuffer m_Geometry;

            #endregion
        }

        /// <summary>
        /// The result of a fragment operation. This must be disposed of after use otherwise leaks will occur.
        /// See <see cref="LowLevelPhysics2D.PhysicsDestructor.Fragment"/>.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public readonly struct FragmentResult : IDisposable
        {
            /// <summary>
            /// The transform used when fragmenting. All returned geometry uses this.
            /// </summary>
            public readonly PhysicsTransform transform => m_Transform;

            /// <summary>
            /// The geometry islands indicating how polygons are connected together.
            /// Each generated polygon belongs to a unique island defining a set of polygons that are connected together as they share edges.
            /// The array returned contains a series of ranges where each range is a unique connected island where the range indicates both the start index and length of the original polygon indices.
            /// The number of discovered unique islands is defined by the size of the returned array.
            ///
            /// This is only populated when fragmenting with a mask with <see cref="LowLevelPhysics2D.PhysicsDestructor.Fragment(FragmentGeometry, FragmentGeometry, ReadOnlySpan{Vector2}, Allocator)"/>; otherwise it returns an empty array.
            /// </summary>
            public NativeArray<RangeInt> unbrokenGeometryIslands => m_UnbrokenGeometryIslands.ToNativeArray<RangeInt>();

            /// <summary>
            /// The geometry that was not fragmented (unbroken).
            /// </summary>
            public NativeArray<PolygonGeometry> unbrokenGeometry => m_UnbrokenGeometry.ToNativeArray<PolygonGeometry>();

            /// <summary>
            /// The geometry that was fragmented (broken).
            /// </summary>
            public NativeArray<PolygonGeometry> brokenGeometry => m_BrokenGeometry.ToNativeArray<PolygonGeometry>();

            /// <summary>
            /// Dispose of the fragment result.
            /// </summary>
            public void Dispose()
            {
                m_UnbrokenGeometryIslands.Dispose();
                m_UnbrokenGeometry.Dispose();
                m_BrokenGeometry.Dispose();
            }

            #region Internal

            readonly PhysicsTransform m_Transform;
            readonly PhysicsBuffer m_UnbrokenGeometryIslands;
            readonly PhysicsBuffer m_UnbrokenGeometry;
            readonly PhysicsBuffer m_BrokenGeometry;

            #endregion
        }

        /// <summary>
        /// The result of a slice operation. This must be disposed of after use otherwise leaks will occur.
        /// See <see cref="LowLevelPhysics2D.PhysicsDestructor.Slice"/>.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public readonly struct SliceResult : IDisposable
        {
            /// <summary>
            /// The transform used when slicing. All returned geometry uses this.
            /// </summary>
            public readonly PhysicsTransform transform => m_Transform;

            /// <summary>
            /// The geometry that was sliced on the "left" of the line.
            /// </summary>
            public NativeArray<PolygonGeometry> leftGeometry => m_LeftGeometry.ToNativeArray<PolygonGeometry>();

            /// <summary>
            /// The geometry that was sliced on the "right" of the line.
            /// </summary>
            public NativeArray<PolygonGeometry> rightGeometry => m_RightGeometry.ToNativeArray<PolygonGeometry>();

            /// <summary>
            /// Dispose of the slice result.
            /// </summary>
            public void Dispose()
            {
                m_LeftGeometry.Dispose();
                m_RightGeometry.Dispose();
            }

            #region Internal

            readonly PhysicsTransform m_Transform;
            readonly PhysicsBuffer m_LeftGeometry;
            readonly PhysicsBuffer m_RightGeometry;

            #endregion
        }

        /// <summary>
        /// Fragment the specified target geometry using the specified fragment points.
        /// 
        /// The fragment points define areas where polygon fragments will be produced from the target geometry.
        /// 
        /// If the resulting polygon fragments have more polygon vertices than can fit into a single <see cref="LowLevelPhysics2D.PolygonGeometry"/> then the fragment will be split into
        /// multiple polygon fragments. The maximum number of vertices a single polygon fragment can have is defined by <see cref="LowLevelPhysics2D.PhysicsConstants.MaxPolygonVertices"/>.
        /// 
        /// If even a single fragment point overlaps the target geometry then all results will be returned in <see cref="LowLevelPhysics2D.PhysicsDestructor.FragmentResult.brokenGeometry"/>.
        /// If none of the fragment points overlap the target geometry then all the results will be returned in <see cref="LowLevelPhysics2D.PhysicsDestructor.FragmentResult.unbrokenGeometry"/>.
        /// See <see cref="LowLevelPhysics2D.PhysicsDestructor.FragmentResult"/>.
        /// </summary>
        /// <param name="target">The target geometry to fragment. There must be at least a single geometry element. Any target polygons with a non-zero radius will be ignored.</param>
        /// <param name="fragmentPoints">The world-space fragment points used to define fragment regions. The number of fragment points must be greater than 1.</param>
        /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
        /// <returns>The fragment results. These results must be disposed of after use otherwise leaks will occur.</returns>
        public static FragmentResult Fragment(FragmentGeometry target, ReadOnlySpan<Vector2> fragmentPoints, Allocator allocator = Allocator.Temp) => PhysicsDestructor_Fragment(target, fragmentPoints, allocator);

        /// <summary>
        /// Fragment the specified mask geometry using the specified fragment points, after the target geometry has the mask (carving) geometry removed from it.
        ///
        /// The target geometry is first clipped with the mask geometry using a <see cref="LowLevelPhysics2D.PhysicsComposer.Operation.NOT"/> operation. The resulting target geometry is returned in <see cref="LowLevelPhysics2D.PhysicsDestructor.FragmentResult.unbrokenGeometry"/>.
        /// The mask geometry is then clipped with the original target geometry using an <see cref="LowLevelPhysics2D.PhysicsComposer.Operation.AND"/> operation. If the clipped mask produces no geometry then no results are returned in <see cref="LowLevelPhysics2D.PhysicsDestructor.FragmentResult.brokenGeometry"/>.
        /// 
        /// The fragment points define areas where polygon fragments will be produced from the clipped masked geometry. The resulting polygon fragments are returned in <see cref="LowLevelPhysics2D.PhysicsDestructor.FragmentResult.brokenGeometry"/>.
        /// 
        /// If the resulting polygon fragments have more polygon vertices than can fit into a single <see cref="LowLevelPhysics2D.PolygonGeometry"/> then the fragment will be split into
        /// multiple polygon fragments. The maximum number of vertices a single polygon fragment can have is defined by <see cref="LowLevelPhysics2D.PhysicsConstants.MaxPolygonVertices"/>.
        /// See <see cref="LowLevelPhysics2D.PhysicsDestructor.FragmentResult"/>.
        /// </summary>
        /// <param name="target">The target geometry to fragment. There must be at least a single geometry element. Any target polygons with a non-zero radius will be ignored.</param>
        /// <param name="mask">The mask geometry that will be used to clip the target geometry. There must be at least a single geometry element. Any mask polygons with a non-zero radius will be ignored.</param>
        /// <param name="fragmentPoints">The world-space fragment points used to define fragment regions. The number of fragment points must be greater than 1.</param>
        /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
        /// <returns>The fragment results. The transform returned here is the one provided in the target geometry. These results must be disposed of after use otherwise leaks will occur.</returns>
        public static FragmentResult Fragment(FragmentGeometry target, FragmentGeometry mask, ReadOnlySpan<Vector2> fragmentPoints, Allocator allocator = Allocator.Temp) => PhysicsDestructor_FragmentMasked(target, mask, fragmentPoints, allocator);

        /// <summary>
        /// Slice the specified target geometry using the specified slice line.
        ///
        /// The target geometry is sliced using the specified ray as defined by <paramref name="origin"/> and <paramref name="translation"/>.
        /// The specified line segment <paramref name="origin"/> and <paramref name="translation"/> are extended to infinity and so defines a 2D intersection plane.
        /// 
        /// All valid target geometry will returned in either the <see cref="LowLevelPhysics2D.PhysicsDestructor.SliceResult.leftGeometry"/> or <see cref="LowLevelPhysics2D.PhysicsDestructor.SliceResult.rightGeometry"/> depending on its side of the line (sliced or not).
        /// Left and Right are defined as "looking" along the ray in the direction defined by <paramref name="translation"/> with Left being anything to the left of the ray and Right being anything to the right of the ray.
        /// See <see cref="LowLevelPhysics2D.PhysicsDestructor.SliceResult"/>.
        /// </summary>
        /// <param name="target">The target geometry to slice. There must be at least a single geometry element. Any target polygons with a non-zero radius will be ignored.</param>
        /// <param name="origin">The start of the ray slice line.</param>
        /// <param name="translation">The translation relative to the origin of the slice ray. This must have a non-zero magnitude.</param>
        /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
        /// <returns>The slice results. The transform returned here is the one provided in the target geometry. These results must be disposed of after use otherwise leaks will occur.</returns>
        public static SliceResult Slice(FragmentGeometry target, Vector2 origin, Vector2 translation, Allocator allocator = Allocator.Temp) => PhysicsDestructor_Slice(target, origin, translation, allocator);
    }
}

