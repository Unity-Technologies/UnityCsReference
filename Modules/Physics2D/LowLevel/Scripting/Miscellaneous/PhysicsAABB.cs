// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using static UnityEngine.LowLevelPhysics2D.PhysicsLowLevelScripting2D;

namespace UnityEngine.LowLevelPhysics2D
{
    /// <summary>
    /// Represents a 2D axis-aligned bounding-box.
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct PhysicsAABB
    {
        /// <summary>
        /// Create an axis-aligned bounding-box with the specified bounds.
        /// </summary>
        /// <param name="lowerBound">The lower-left bounding vertex. This should be equal to or lower than <paramref name="upperBound"/>.</param>
        /// <param name="upperBound">The upper-right bounding vertex. This should be equal to or above <paramref name="lowerBound"/>.</param>
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public PhysicsAABB(Vector2 lowerBound, Vector2 upperBound)
        {
            m_LowerBound = lowerBound;
            m_UpperBound = upperBound;
        }

        /// <summary>
        /// Check if the AABB is valid. To be valid, <see cref="LowLevelPhysics2D.PhysicsAABB.upperBound"/> should be equal to or above <see cref="LowLevelPhysics2D.PhysicsAABB.lowerBound"/>.
        /// </summary>
        public readonly bool isValid => PhysicsAABB_IsValid(this);

        /// <summary>
        /// The lower-left bounding vertex. This should be equal to or lower than <see cref="LowLevelPhysics2D.PhysicsAABB.upperBound"/>.
        /// </summary>
        public Vector2 lowerBound { [MethodImpl(MethodImplOptions.AggressiveInlining)] readonly get => m_LowerBound; [MethodImpl(MethodImplOptions.AggressiveInlining)] set => m_LowerBound = value; }

        /// <summary>
        /// The upper-right bounding vertex. This should be equal to or above <see cref="LowLevelPhysics2D.PhysicsAABB.lowerBound"/>.
        /// </summary>
        public Vector2 upperBound { [MethodImpl(MethodImplOptions.AggressiveInlining)] readonly get => m_UpperBound; [MethodImpl(MethodImplOptions.AggressiveInlining)] set => m_UpperBound = value; }

        /// <summary>
        /// Get a new normalized copy of the AABB ensuring that <see cref="LowLevelPhysics2D.PhysicsAABB.lowerBound"/> is lower than or equal to <see cref="LowLevelPhysics2D.PhysicsAABB.upperBound"/>.
        /// </summary>
        public readonly PhysicsAABB normalized
        {
            get
            {
                return new PhysicsAABB
                {
                    lowerBound = Vector2.Min(lowerBound, upperBound),
                    upperBound = Vector2.Max(lowerBound, upperBound)
                };
            }
        }

        /// <summary>
        /// Normalize the AABB ensuring that <see cref="LowLevelPhysics2D.PhysicsAABB.lowerBound"/> is lower than or equal to <see cref="LowLevelPhysics2D.PhysicsAABB.upperBound"/>.
        /// </summary>
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public void Normalized() => this = normalized;

        /// <summary>
        /// Check if the specified point overlaps this AABB.
        /// </summary>
        /// <param name="point">The point to check for overlap.</param>
        /// <returns>True if the point overlaps, false if not.</returns>
        public readonly bool OverlapPoint(Vector2 point) => PhysicsAABB_OverlapPoint(this, point);

        /// <summary>
        /// Perform a raycast against this AABB.
        /// Nothing will be detected if the ray starts inside the AABB.
        /// To check if the ray starts inside the AABB use <see cref="LowLevelPhysics2D.PhysicsAABB.OverlapPoint(Vector2)"/>.
        /// </summary>
        /// <param name="castRayInput">The configuration of the ray to cast.</param>
        /// <returns>The results of the intersection test.</returns>
        public readonly PhysicsQuery.CastResult CastRay(PhysicsQuery.CastRayInput castRayInput) => PhysicsAABB_CastRay(this, castRayInput);

        /// <summary>
        /// Check if the specified AABB overlaps this AABB.
        /// </summary>
        /// <param name="aabb">The AABB to check overlap with.</param>
        /// <returns>True if overlapped, false if not.</returns>
        public readonly bool Overlap(PhysicsAABB aabb) => PhysicsAABB_Overlap(this, aabb);

        /// <summary>
        /// Check if the specified point overlaps this AABB.
        /// </summary>
        /// <param name="point">The point to check overlap with.</param>
        /// <returns>True if overlapped, false if not.</returns>
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public readonly bool Overlap(Vector2 point) => OverlapPoint(point);

        /// <summary>
        /// Create a union of the the specified AABB and this AABB where resulting AABB completely encapsules both AABB.
        /// </summary>
        /// <param name="aabb">The AABB to create a union with.</param>
        /// <returns>The results of the union.</returns>
        public readonly PhysicsAABB Union(PhysicsAABB aabb) => PhysicsAABB_Union(this, aabb);

        /// <summary>
        /// Checks if the AABB contains (completely encapsulates) the specified AABB.
        /// </summary>
        /// <param name="aabb">The AABB to check being contained by this AABB.</param>
        /// <returns>True if the specified AABB is contained by this AABB. False if not.</returns>
        public readonly bool Contains(PhysicsAABB aabb) => PhysicsAABB_Contains(this, aabb);

        /// <summary>
        /// Get the center of the AABB.
        /// </summary>
        public readonly Vector2 center => PhysicsAABB_Center(this);

        /// <summary>
        /// Get the extents (half size) of the AABB.
        /// </summary>
        public readonly Vector2 extents => PhysicsAABB_Extents(this);

        /// <summary>
        /// Get the surface area (perimeter length) of the AABB.
        /// </summary>
        public readonly float perimeter => PhysicsAABB_Perimeter(this);

        /// <undoc/>
        public override readonly string ToString() => $"lowerBound={lowerBound}, upperBound={upperBound}, isValid={isValid}";

        #region Internal

        [SerializeField] Vector2 m_LowerBound;
        [SerializeField] Vector2 m_UpperBound;

        #endregion
    }
}
