// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using static Unity.U2D.Physics.Scripting2D;

namespace Unity.U2D.Physics
{
    /// <summary>
    /// The geometry of a chain of ChainSegment.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [MovedFrom(autoUpdateAPI: ScriptUpdateConstants.AutoUpdateAPI, sourceNamespace: ScriptUpdateConstants.SourceNamespace, sourceAssembly: ScriptUpdateConstants.SourceAssembly)]
    public partial struct ChainGeometry
    {
        /// <summary>
        /// Create the geometry of a Chain using the specified vertices.
        /// </summary>
        /// <param name="vertices">The vertices that will create the ChainSegment shapes.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown if the number of vertices is less than 3.</exception>
        public unsafe ChainGeometry(NativeArray<Vector2> vertices)
        {
            // Validate.
            if (vertices.Length < 3)
                throw new ArgumentOutOfRangeException(nameof(vertices), "Chain Geometry must contain a minimum of 3 vertices.");

            // Assign the buffer data.
            m_Points = new IntPtr(vertices.GetUnsafeReadOnlyPtr());
            m_Count = vertices.Length;
        }

        /// <summary>
        /// Check if the geometry is valid or not.
        /// </summary>
        public readonly bool isValid => ChainGeometry_IsValid(this);

        /// <summary>
        /// Get the geometry vertices.
        /// </summary>
        public readonly unsafe ReadOnlySpan<Vector2> vertices => new(m_Points.ToPointer(), m_Count);

        /// <summary>
        /// Calculate the AABB of the geometry.
        /// </summary>
        /// <param name="transform">The transform used to specify where the geometry is positioned.</param>
        /// <returns>The bounds of the geometry.</returns>
        public readonly PhysicsAABB CalculateAABB(PhysicsTransform transform) => ChainGeometry_CalculateAABB(this, transform);

        /// <summary>
        /// Calculate the closest point on this geometry to the specified point.
        /// </summary>
        /// <param name="point">The point to check.</param>
        /// <returns>The closest point on the geometry to the specified point.</returns>
        public readonly Vector2 ClosestPoint(Vector2 point) => ChainGeometry_ClosestPoint(this, point);

        /// <summary>
        /// Calculate if a world ray intersects the geometry.
        /// See <see cref="PhysicsQuery.CastResult"/>.
        /// </summary>
        /// <param name="castRayInput">The configuration of the ray to cast.</param>
        /// <param name="oneSided">Whether to treat the segment as having one-sided collision. The "left" side collision is ignored.</param>
        /// <returns>The results of the intersection test.</returns>
        public readonly PhysicsQuery.CastResult CastRay(PhysicsQuery.CastRayInput castRayInput, bool oneSided = true) => ChainGeometry_CastRay(this, castRayInput, oneSided);

        /// <summary>
        /// Calculate if a cast shape intersects the geometry.
        /// Initially touching shapes are treated as a miss. You should check for overlap first if initial overlap is required.
        /// See <see cref="PhysicsQuery.CastShapeInput"/> and <see cref="PhysicsQuery.CastResult"/>.
        /// </summary>
        /// <param name="input">The cast shape input used to check for intersection.</param>
        /// <returns>The results of the intersection test.</returns>
        public readonly PhysicsQuery.CastResult CastShape(PhysicsQuery.CastShapeInput input) => ChainGeometry_CastShape(this, input);

        #region Internal

        IntPtr m_Points;
        int m_Count;

        #endregion
    }
}
