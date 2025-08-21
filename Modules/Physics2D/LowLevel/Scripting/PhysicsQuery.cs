// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;

using static UnityEngine.LowLevelPhysics2D.PhysicsLowLevelScripting2D;

namespace UnityEngine.LowLevelPhysics2D
{
    /// <summary>
    /// Various physics queries.
    /// </summary>
    public readonly struct PhysicsQuery
    {
        /// <summary>
        /// Check the intersection between Circle and Circle geometries.
        /// </summary>
        /// <param name="geometryA">The first geometry to use.</param>
        /// <param name="transformA">The transform used to specify where the first geometry is positioned.</param>
        /// <param name="geometryB">The second geometry to use.</param>
        /// <param name="transformB">The transform used to specify where the first geometry is positioned.</param>
        /// <returns>The contact manifold fully detailing the intersection.</returns>
        public static PhysicsShape.ContactManifold CircleAndCircle(CircleGeometry geometryA, PhysicsTransform transformA, CircleGeometry geometryB, PhysicsTransform transformB) => PhysicsQuery_CircleAndCircle(geometryA, transformA, geometryB, transformB);

        /// <summary>
        /// Check the intersection between Capsule and Circle geometries.
        /// </summary>
        /// <param name="geometryA">The first geometry to use.</param>
        /// <param name="transformA">The transform used to specify where the first geometry is positioned.</param>
        /// <param name="geometryB">The second geometry to use.</param>
        /// <param name="transformB">The transform used to specify where the first geometry is positioned.</param>
        /// <returns>The contact manifold fully detailing the intersection.</returns>
        public static PhysicsShape.ContactManifold CapsuleAndCircle(CapsuleGeometry geometryA, PhysicsTransform transformA, CircleGeometry geometryB, PhysicsTransform transformB) => PhysicsQuery_CapsuleAndCircle(geometryA, transformA, geometryB, transformB);

        /// <summary>
        /// Check the intersection between Segment and Circle geometries.
        /// </summary>
        /// <param name="geometryA">The first geometry to use.</param>
        /// <param name="transformA">The transform used to specify where the first geometry is positioned.</param>
        /// <param name="geometryB">The second geometry to use.</param>
        /// <param name="transformB">The transform used to specify where the first geometry is positioned.</param>
        /// <returns>The contact manifold fully detailing the intersection.</returns>
        public static PhysicsShape.ContactManifold SegmentAndCircle(SegmentGeometry geometryA, PhysicsTransform transformA, CircleGeometry geometryB, PhysicsTransform transformB) => PhysicsQuery_SegmentAndCircle(geometryA, transformA, geometryB, transformB);

        /// <summary>
        /// Check the intersection between Polyon and Circle geometries.
        /// </summary>
        /// <param name="geometryA">The first geometry to use.</param>
        /// <param name="transformA">The transform used to specify where the first geometry is positioned.</param>
        /// <param name="geometryB">The second geometry to use.</param>
        /// <param name="transformB">The transform used to specify where the first geometry is positioned.</param>
        /// <returns>The contact manifold fully detailing the intersection.</returns>
        public static PhysicsShape.ContactManifold PolygonAndCircle(PolygonGeometry geometryA, PhysicsTransform transformA, CircleGeometry geometryB, PhysicsTransform transformB) => PhysicsQuery_PolygonAndCircle(geometryA, transformA, geometryB, transformB);

        /// <summary>
        /// Check the intersection between Capsule and Capsule geometries.
        /// </summary>
        /// <param name="geometryA">The first geometry to use.</param>
        /// <param name="transformA">The transform used to specify where the first geometry is positioned.</param>
        /// <param name="geometryB">The second geometry to use.</param>
        /// <param name="transformB">The transform used to specify where the first geometry is positioned.</param>
        /// <returns>The contact manifold fully detailing the intersection.</returns>
        public static PhysicsShape.ContactManifold CapsuleAndCapsule(CapsuleGeometry geometryA, PhysicsTransform transformA, CapsuleGeometry geometryB, PhysicsTransform transformB) => PhysicsQuery_CapsuleAndCapsule(geometryA, transformA, geometryB, transformB);

        /// <summary>
        /// Check the intersection between Segment and Capsule geometries.
        /// </summary>
        /// <param name="geometryA">The first geometry to use.</param>
        /// <param name="transformA">The transform used to specify where the first geometry is positioned.</param>
        /// <param name="geometryB">The second geometry to use.</param>
        /// <param name="transformB">The transform used to specify where the first geometry is positioned.</param>
        /// <returns>The contact manifold fully detailing the intersection.</returns>
        public static PhysicsShape.ContactManifold SegmentAndCapsule(SegmentGeometry geometryA, PhysicsTransform transformA, CapsuleGeometry geometryB, PhysicsTransform transformB) => PhysicsQuery_SegmentAndCapsule(geometryA, transformA, geometryB, transformB);

        /// <summary>
        /// Check the intersection between Polygon and Capsule geometries.
        /// </summary>
        /// <param name="geometryA">The first geometry to use.</param>
        /// <param name="transformA">The transform used to specify where the first geometry is positioned.</param>
        /// <param name="geometryB">The second geometry to use.</param>
        /// <param name="transformB">The transform used to specify where the first geometry is positioned.</param>
        /// <returns>The contact manifold fully detailing the intersection.</returns>
        public static PhysicsShape.ContactManifold PolygonAndCapsule(PolygonGeometry geometryA, PhysicsTransform transformA, CapsuleGeometry geometryB, PhysicsTransform transformB) => PhysicsQuery_PolygonAndCapsule(geometryA, transformA, geometryB, transformB);

        /// <summary>
        /// Check the intersection between Polygon and Polygon geometries.
        /// </summary>
        /// <param name="geometryA">The first geometry to use.</param>
        /// <param name="transformA">The transform used to specify where the first geometry is positioned.</param>
        /// <param name="geometryB">The second geometry to use.</param>
        /// <param name="transformB">The transform used to specify where the first geometry is positioned.</param>
        /// <returns>The contact manifold fully detailing the intersection.</returns>
        public static PhysicsShape.ContactManifold PolygonAndPolygon(PolygonGeometry geometryA, PhysicsTransform transformA, PolygonGeometry geometryB, PhysicsTransform transformB) => PhysicsQuery_PolygonAndPolygon(geometryA, transformA, geometryB, transformB);

        /// <summary>
        /// Check the intersection between Segment and Polygon geometries.
        /// </summary>
        /// <param name="geometryA">The first geometry to use.</param>
        /// <param name="transformA">The transform used to specify where the first geometry is positioned.</param>
        /// <param name="geometryB">The second geometry to use.</param>
        /// <param name="transformB">The transform used to specify where the first geometry is positioned.</param>
        /// <returns>The contact manifold fully detailing the intersection.</returns>
        public static PhysicsShape.ContactManifold SegmentAndPolygon(SegmentGeometry geometryA, PhysicsTransform transformA, PolygonGeometry geometryB, PhysicsTransform transformB) => PhysicsQuery_SegmentAndPolygon(geometryA, transformA, geometryB, transformB);

        /// <summary>
        /// Check the intersection between ChainSegment and Circle geometries.
        /// </summary>
        /// <param name="geometryA">The first geometry to use.</param>
        /// <param name="transformA">The transform used to specify where the first geometry is positioned.</param>
        /// <param name="geometryB">The second geometry to use.</param>
        /// <param name="transformB">The transform used to specify where the first geometry is positioned.</param>
        /// <returns>The contact manifold fully detailing the intersection.</returns>
        public static PhysicsShape.ContactManifold ChainSegmentAndCircle(ChainSegmentGeometry geometryA, PhysicsTransform transformA, CircleGeometry geometryB, PhysicsTransform transformB) => PhysicsQuery_ChainSegmentAndCircle(geometryA, transformA, geometryB, transformB);

        /// <summary>
        /// Check the intersection between ChainSegment and Capsule geometries.
        /// </summary>
        /// <param name="geometryA">The first geometry to use.</param>
        /// <param name="transformA">The transform used to specify where the first geometry is positioned.</param>
        /// <param name="geometryB">The second geometry to use.</param>
        /// <param name="transformB">The transform used to specify where the first geometry is positioned.</param>
        /// <returns>The contact manifold fully detailing the intersection.</returns>
        public static PhysicsShape.ContactManifold ChainSegmentAndCapsule(ChainSegmentGeometry geometryA, PhysicsTransform transformA, CapsuleGeometry geometryB, PhysicsTransform transformB) => PhysicsQuery_ChainSegmentAndCapsule(geometryA, transformA, geometryB, transformB);

        /// <summary>
        /// Check the intersection between ChainSegment and Polygon geometries.
        /// </summary>
        /// <param name="geometryA">The first geometry to use.</param>
        /// <param name="transformA">The transform used to specify where the first geometry is positioned.</param>
        /// <param name="geometryB">The second geometry to use.</param>
        /// <param name="transformB">The transform used to specify where the first geometry is positioned.</param>
        /// <returns>The contact manifold fully detailing the intersection.</returns>
        public static PhysicsShape.ContactManifold ChainSegmentAndPolygon(ChainSegmentGeometry geometryA, PhysicsTransform transformA, PolygonGeometry geometryB, PhysicsTransform transformB) => PhysicsQuery_ChainSegmentAndPolygon(geometryA, transformA, geometryB, transformB);

        /// <summary>
        /// Calculate the distance and closest points between two segments.
        /// </summary>
        /// <param name="geometryA">The first geometry to use.</param>
        /// <param name="transformA">The transform used to specify where the first geometry is positioned.</param>
        /// <param name="geometryB">The second geometry to use.</param>
        /// <param name="transformB">The transform used to specify where the first geometry is positioned.</param>
        /// <returns>The segment distance results.</returns>
        public static SegmentDistanceResult SegmentDistance(SegmentGeometry geometryA, PhysicsTransform transformA, SegmentGeometry geometryB, PhysicsTransform transformB) => PhysicsQuery_SegmentDistance(geometryA, transformA, geometryB, transformB);

        /// <summary>
        /// Cast two shape proxies against each other.
        /// Initially touching shapes are treated as a miss. You should check for overlap first if initial overlap is required.
        /// </summary>
        /// <param name="castShapePairInput">The input describing the shape proxies and how they should move.</param>
        public static void CastShapes(CastShapePairInput castShapePairInput) => PhysicsQuery_CastShapes(castShapePairInput);

        /// <summary>
        /// Calculate the distance and closest points between two shape proxies.
        /// </summary>
        /// <param name="distanceInput">The input describing the shape proxies and how they should move.</param>
        /// <returns>The distance result.</returns>
        public static DistanceResult ShapeDistance(DistanceInput distanceInput) => PhysicsQuery_ShapeDistance(distanceInput);

        /// <summary>
        /// Calculate the upper bound on time before two shape proxies penetrate i.e. the time-of-impact.
        /// Time is represented as a fraction in the range [0, maxInterval].
        /// This uses a swept separating axis and may miss some intermediate, non-tunneling collisions.
        /// </summary>
        /// <param name="toiInput">The input describing the shapes and how they should move.</param>
        /// <returns>The time of impact result.</returns>
        public static TimeOfImpactResult ShapeTimeOfImpact(TimeOfImpactInput toiInput) => PhysicsQuery_ShapeTimeOfImpact(toiInput);

        /// <summary>
        /// A query filter is used to filter query results known as "hits".
        /// For example, you may want a ray-cast representing a projectile to hit players and the static environment but not debris.
        /// </summary>            
        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        public struct QueryFilter
        {
            /// <summary>
            /// Create a default filter set as <see cref="QueryFilter.defaultFilter"/>.
            /// </summary>
            public QueryFilter() { this = defaultFilter; }

            /// <summary>
            /// Create a query filter.
            /// </summary>
            /// <param name="categories">A <see cref="LowLevelPhysics2D.PhysicsMask"/> defining the categories this query is using.</param>
            /// <param name="hitCategories">A <see cref="LowLevelPhysics2D.PhysicsMask"/> defining the categories this query will produce hits with.</param>
            public QueryFilter(PhysicsMask categories, PhysicsMask hitCategories)
            {
                m_Categories = categories;
                m_HitCategories = hitCategories;
            }

            /// <summary>
            /// The categories this query is using. Usually you would only set one bit but multiple are allowed.
            /// </summary>
            public PhysicsMask categories { readonly get => m_Categories; set => m_Categories = value; }

            /// <summary>
            /// The categories this query will produce hits with.
            /// </summary>
            public PhysicsMask hitCategories { readonly get => m_HitCategories; set => m_HitCategories = value; }

            /// <summary>
            /// The default categories used.
            /// </summary>
            public static PhysicsMask DefaultCategories = PhysicsMask.One;

            /// <summary>
            /// The default hit categories used.
            /// </summary>
            public static PhysicsMask DefaultHitCategories = PhysicsMask.All;

            /// <summary>
            /// Get a query filter that is all categories and hit everything.
            /// </summary>
            public static QueryFilter Everything = new(PhysicsMask.All, PhysicsMask.All);

            /// <summary>
            /// Get the default query filter that hits everything. This uses both <see cref="QueryFilter.DefaultCategories"/> and <see cref="QueryFilter.DefaultHitCategories"/>.
            /// </summary>
            public static QueryFilter defaultFilter = new(DefaultCategories, DefaultHitCategories);

            #region Internal

            [SerializeField] internal PhysicsMask m_Categories;
            [SerializeField] internal PhysicsMask m_HitCategories;

            #endregion
        }

        /// <summary>
        /// The results from performing any Overlap query.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public readonly struct WorldOverlapResult
        {
            /// <summary>
            /// The shape that was detected by the overlap.
            /// </summary>
            public readonly PhysicsShape shape => m_Shape;

            /// <summary>
            /// Check if the result is valid.
            /// </summary>
            public readonly bool isValid => m_Shape.isValid;

            #region Internal

            readonly PhysicsShape m_Shape;

            #endregion
        };

        /// <summary>
        /// The results from performing any Cast query against the <see cref="LowLevelPhysics2D.PhysicsWorld"/>.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public readonly struct WorldCastResult
        {
            /// <summary>
            /// The shape that was detected by the cast.
            /// </summary>
            public readonly PhysicsShape shape => m_Shape;

            /// <summary>
            /// The point of contact.
            /// </summary>
            public readonly Vector2 point => m_Point;

            /// <summary>
            /// The surface normal at the point of contact.
            /// In all non-overlapped cases, this will be a unit-normal.
            /// If there was an initial overlap, the normal will be zero (degenerate) along with the <see cref="WorldCastResult.fraction"/> being zero and <see cref="WorldCastResult.point"/> being an arbitrary point in the overlapped region.
            /// See <see cref="WorldCastResult.point"/>.
            /// </summary>
            public readonly Vector2 normal => m_Normal;

            /// <summary>
            /// The fraction of the query cast distance the shape would move to the point of detection, in the range [0, 1].
            /// </summary>
            public readonly float fraction => m_Fraction;

            /// <summary>
            /// Check if the result is valid.
            /// </summary>
            public readonly bool isValid => m_Shape.isValid;

            #region Internal

            readonly PhysicsShape m_Shape;
            readonly Vector2 m_Point;
            readonly Vector2 m_Normal;
            readonly float m_Fraction;

            #endregion
        };

        /// <summary>
        /// The world mover arguments used by the world mover.
        /// </summary>
        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        public struct WorldMoverInput
        {
            /// <summary>
            /// Create a default world mover input.
            /// </summary>
            public WorldMoverInput()
            {
                this = s_WorldMoverInput;
            }

            /// <summary>
            /// The mover geometry to use when checking for overlaps and casting.
            /// </summary>
            public CapsuleGeometry geometry { readonly get => m_Geometry; set => m_Geometry = value; }

            /// <summary>
            /// The transform used to transform the geometry i.e. the mover starting pose.
            /// </summary>
            public PhysicsTransform transform { readonly get => m_Transform; set => m_Transform = value; }

            /// <summary>
            /// The position desired for the mover to achieve.
            /// This is typically calculated using the current velocity, any gravity required and time-integrated by the simulation time-step (delta-time).
            /// </summary>
            public Vector2 targetPosition { readonly get => m_TargetPosition; set => m_TargetPosition = value; }

            /// <summary>
            /// The velocity used to calculate the <see cref="LowLevelPhysics2D.PhysicsQuery.WorldMoverInput.targetPosition"/>.
            /// This is not used for movement but it will be returned, modified by any surfaces hit.
            /// This velocity can then be used in subsequent inputs for movement.
            /// </summary>
            public Vector2 velocity { readonly get => m_Velocity; set => m_Velocity = value; }

            /// <summary>
            /// The filter to use for checking overlaps.
            /// </summary>
            public QueryFilter overlapFilter { readonly get => m_OverlapFilter; set => m_OverlapFilter = value; }

            /// <summary>
            /// The filter to use for checking casts.
            /// The advantage of a separate filter to <see cref="LowLevelPhysics2D.PhysicsQuery.WorldMoverInput.overlapFilter"/> is that you can check for overlaps in a different way to what you can hit when moving.
            /// For instance, you may or may not want to check for other movers in they existing in the world when moving but you want to always check them for overlap initially.
            /// </summary>
            public QueryFilter castFilter { readonly get => m_CastFilter; set => m_CastFilter = value; }

            /// <summary>
            /// Solving a movement is iterative and will continue until the maximum allowed iterations has been achieve, controlled by this value.
            /// The maximum allowed iterations will not always be used and solving will cease if the iteration movement falls below the square of the <see cref="LowLevelPhysics2D.PhysicsQuery.WorldMoverInput.moveTolerance"/>.
            /// </summary>
            public int maxIterations { readonly get => m_MaxIterations; set => m_MaxIterations = Mathf.Max(0, value); }

            /// <summary>
            /// Solving a movement will cease if the movement falls below the square of this value.
            /// By default, this value is extremely small.
            /// Too high a value will result in solving ceasing too quickly, too small will result in all allowed <see cref="LowLevelPhysics2D.PhysicsQuery.WorldMoverInput.maxIterations"/> being used.
            /// </summary>
            public float moveTolerance { readonly get => m_MoveTolerance; set => m_MoveTolerance = Mathf.Max(0.01f, value); }

            /// <summary>
            /// Create a default world mover input.
            /// </summary>
            public static WorldMoverInput defaultInput { get => s_WorldMoverInput; }
            static WorldMoverInput s_WorldMoverInput = new()
            {
                geometry = CapsuleGeometry.defaultGeometry,
                overlapFilter = QueryFilter.defaultFilter,
                castFilter = QueryFilter.defaultFilter,
                transform = PhysicsTransform.identity,
                velocity = Vector2.zero,
                targetPosition = Vector2.zero,
                maxIterations = 5,
                moveTolerance = 0.1f
            };

            #region Internal

            [SerializeField] CapsuleGeometry m_Geometry;
            [SerializeField] PhysicsTransform m_Transform;
            [SerializeField] Vector2 m_TargetPosition;
            [SerializeField] Vector2 m_Velocity;
            [SerializeField] QueryFilter m_OverlapFilter;
            [SerializeField] QueryFilter m_CastFilter;
            [SerializeField] int m_MaxIterations;
            [SerializeField] float m_MoveTolerance;

            #endregion
        }

        /// <summary>
        /// The world mover result used by the world mover.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public readonly struct WorldMoverResult
        {
            /// <summary>
            /// The final transform the mover finished at.
            /// The transform rotation is always the same as the <see cref="LowLevelPhysics2D.PhysicsQuery.WorldMoverInput.transform"/> provided.
            /// </summary>
            public PhysicsTransform transform => m_Transform;

            /// <summary>
            /// The final velocity the mover finished at.
            /// </summary>
            public Vector2 velocity => m_Velocity;

            #region Internal

            readonly PhysicsTransform m_Transform;
            readonly Vector2 m_Velocity;

            #endregion
        }

        /// <summary>
        /// Controls what results are returned from a cast query against the <see cref="LowLevelPhysics2D.PhysicsWorld"/>.
        /// </summary>
        public enum WorldCastMode
        {
            /// <summary>
            /// Return only the closest hit. 
            /// </summary>
            Closest = 0,

            /// <summary>
            /// Return all the hits.
            /// </summary>
            All = 1,

            /// <summary>
            /// Return all the hits but also sort by ascending distance (closest first).
            /// </summary>
            AllSorted = 2
        };

        /// <summary>
        /// Cast-Ray arguments used by CastRay queries.
        /// </summary>
        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        public struct CastRayInput
        {
            /// <summary>
            /// Create a default Cast Ray input.
            /// </summary>
            public CastRayInput()
            {
                m_Origin = default;
                m_Translation = default;
                m_MaxFraction = 1.0f;
            }

            /// <summary>
            /// Create a Cast-Ray with a default fraction of 1.
            /// </summary>
            /// <param name="origin">The origin (start) of the ray.</param>
            /// <param name="translation">The translation relative to the <see cref="CastRayInput.origin"/> of the ray.</param>
            public CastRayInput(Vector2 origin, Vector2 translation)
            {
                m_Origin = origin;
                m_Translation = translation;
                m_MaxFraction = 1.0f;
            }

            /// <summary>
            /// Calculate a Cast-Ray given two positions.
            /// </summary>
            /// <param name="from">The position the ray starts.</param>
            /// <param name="to">The position the ray ends.</param>
            /// <returns></returns>
            public static CastRayInput FromTo(Vector2 from, Vector2 to) => new CastRayInput(from, to - from);

            /// <summary>
            /// The origin (start) of the ray.
            /// </summary>
	        public Vector2 origin { readonly get => m_Origin; set => m_Origin = value; }

            /// <summary>
	        /// The translation relative to the <see cref="CastRayInput.origin"/> of the ray.
            /// </summary>
	        public Vector2 translation { readonly get => m_Translation; set => m_Translation = value; }

            /// <summary>
	        /// The maximum fraction of the translation to consider in the range (0 to 1), typically 1.
            /// </summary>
            public float maxFraction { readonly get => m_MaxFraction; set => m_MaxFraction = Mathf.Clamp01(value); }

            #region Internal

	        [SerializeField] Vector2 m_Origin;
	        [SerializeField] Vector2 m_Translation;
            [SerializeField] [Range(0.0f, 1.0f)] float m_MaxFraction;

            #endregion
        }

        /// <summary>
        /// Cast two shape proxies against each other.
        /// To use existing shape geometries, use the helper constructors that allow creation via a specific shape geometry type.
        /// </summary>
        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        public struct CastShapePairInput
        {
            /// <summary>
            /// A proxy to the shape A.
            /// </summary>
            public PhysicsShape.ShapeProxy shapeProxyA { readonly get => m_ShapeProxyA; set => m_ShapeProxyA = value; }

            /// <summary>
            /// A proxy to the shape B.
            /// </summary>
            public PhysicsShape.ShapeProxy shapeProxyB { readonly get => m_ShapeProxyB; set => m_ShapeProxyB = value; }

            /// <summary>
	        /// The world transform for shape A
            /// </summary>
            public PhysicsTransform transformA { readonly get => m_TransformA; set => m_TransformA = value; }

            /// <summary>
	        /// The world transform for shape B
            /// </summary>
            public PhysicsTransform transformB { readonly get => m_TransformB; set => m_TransformB = value; }

            /// <summary>
            /// Translation of the shape proxy B.
            /// </summary>
            public Vector2 translationB { readonly get => m_TranslationB; set => m_TranslationB = value; }

            /// <summary>
	        /// The maximum fraction of the translation to consider in the range (0 to 1), typically 1.
            /// </summary>
            public float maxFraction { readonly get => m_MaxFraction; set => m_MaxFraction = Mathf.Clamp01(value); }

            /// <summary>
	        /// Allow cast shape proxies to encroach when initially touching. This only works if the radius is greater than zero.
            /// </summary>
	        public bool canEncroach { readonly get => m_CanEncroach; set => m_CanEncroach = value; }

            #region Internal

            [SerializeField] PhysicsShape.ShapeProxy m_ShapeProxyA;
            [SerializeField] PhysicsShape.ShapeProxy m_ShapeProxyB;
            [SerializeField] PhysicsTransform m_TransformA;
            [SerializeField] PhysicsTransform m_TransformB;
            [SerializeField] Vector2 m_TranslationB;
            [SerializeField] [Range(0.0f, 1.0f)] float m_MaxFraction;
	        [SerializeField] bool m_CanEncroach;

            #endregion
        }

        /// <summary>
        /// Cast shape arguments used by CastShape queries.
        /// To use existing shape geometries, use the helper constructors that allow creation via a specific shape geometry type.
        /// </summary>
        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        public struct CastShapeInput
        {
            /// <summary>
            /// Create a default cast shape input.
            /// </summary>
            public CastShapeInput()
            {
                m_ShapeProxy = default;
                m_Translation = default;
                m_MaxFraction = 1.0f;
                m_CanEncroach = false;
            }

            /// <summary>
            /// Create a CastShapeInput the specified <see cref="LowLevelPhysics2D.CircleGeometry"/>.
            /// You may want to also translate the geometry into the space you require.
            /// </summary>
            /// <param name="circleGeometry">The geometry to use.</param>
            /// <param name="translation">The cast translation to use.</param>
            public CastShapeInput(CircleGeometry circleGeometry, Vector2 translation)
            {
                m_ShapeProxy = new PhysicsShape.ShapeProxy(circleGeometry);
                m_Translation = translation;
                m_MaxFraction = 1.0f;
                m_CanEncroach = false;
            }

            /// <summary>
            /// Create a CastShapeInput the specified <see cref="LowLevelPhysics2D.CapsuleGeometry"/>.
            /// You may want to also translate the geometry into the space you require.
            /// </summary>
            /// <param name="capsuleGeometry">The geometry to use.</param>
            /// <param name="translation">The cast translation to use.</param>
            public CastShapeInput(CapsuleGeometry capsuleGeometry, Vector2 translation)
            {
                m_ShapeProxy = new PhysicsShape.ShapeProxy(capsuleGeometry);
                m_Translation = translation;
                m_MaxFraction = 1.0f;
                m_CanEncroach = false;
            }

            /// <summary>
            /// Create a CastShapeInput the specified <see cref="LowLevelPhysics2D.SegmentGeometry"/>.
            /// You may want to also translate the geometry into the space you require.
            /// </summary>
            /// <param name="segmentGeometry">The geometry to use.</param>
            /// <param name="translation">The cast translation to use.</param>
            public CastShapeInput(SegmentGeometry segmentGeometry, Vector2 translation)
            {
                m_ShapeProxy = new PhysicsShape.ShapeProxy(segmentGeometry);
                m_Translation = translation;
                m_MaxFraction = 1.0f;
                m_CanEncroach = false;
            }

            /// <summary>
            /// Create a CastShapeInput the specified <see cref="LowLevelPhysics2D.PolygonGeometry"/>.
            /// You may want to also translate the geometry into the space you require.
            /// </summary>
            /// <param name="polygonGeometry">The geometry to use.</param>
            /// <param name="translation">The cast translation to use.</param>
            public CastShapeInput(PolygonGeometry polygonGeometry, Vector2 translation)
            {
                m_ShapeProxy = new PhysicsShape.ShapeProxy(polygonGeometry);
                m_Translation = translation;
                m_MaxFraction = 1.0f;
                m_CanEncroach = false;
            }

            /// <summary>
            /// Create a CastShapeInput the specified <see cref="LowLevelPhysics2D.ChainSegmentGeometry"/>.
            /// You may want to also translate the geometry into the space you require.
            /// </summary>
            /// <param name="chainSegmentGeometry">The geometry to use.</param>
            /// <param name="translation">The cast translation to use.</param>
            public CastShapeInput(ChainSegmentGeometry chainSegmentGeometry, Vector2 translation)
            {
                m_ShapeProxy = new PhysicsShape.ShapeProxy(chainSegmentGeometry);
                m_Translation = translation;
                m_MaxFraction = 1.0f;
                m_CanEncroach = false;
            }

            /// <summary>
            /// Create a CastShapeInput the specified world shape. The geometry will automatically be translated into world-space.
            /// </summary>
            /// <param name="shape">The shape to use.</param>
            /// <exception cref="System.ArgumentException">Thrown if the shape is not valid.</exception>
            /// <param name="translation">The cast translation to use.</param>
            public static CastShapeInput FromShape(PhysicsShape shape, Vector2 translation)
            {
                if (!shape.isValid)
                    throw new ArgumentException("PhysicsShape is not valid.");

                // Fetch the body transform.
                var transform = shape.body.transform;

                // Extract the appropriate geometry from the shape.
                return shape.shapeType switch
                {
                    PhysicsShape.ShapeType.Circle => new CastShapeInput(shape.circleGeometry.Transform(transform), translation),
                    PhysicsShape.ShapeType.Capsule => new CastShapeInput(shape.capsuleGeometry.Transform(transform), translation),
                    PhysicsShape.ShapeType.Segment => new CastShapeInput(shape.segmentGeometry.Transform(transform), translation),
                    PhysicsShape.ShapeType.Polygon => new CastShapeInput(shape.polygonGeometry.Transform(transform), translation),
                    PhysicsShape.ShapeType.ChainSegment => new CastShapeInput(shape.chainSegmentGeometry.Transform(transform), translation),
                    _ => throw new NotImplementedException()
                };
            }

            /// <summary>
            /// A proxy to the shape.
            /// </summary>
            public PhysicsShape.ShapeProxy shapeProxy { readonly get => m_ShapeProxy; set => m_ShapeProxy = value; }

            /// <summary>
            /// Translation of the cast shape.
            /// </summary>
            public Vector2 translation { readonly get => m_Translation; set => m_Translation = value; }

            /// <summary>
	        /// The maximum fraction of the translation to consider in the range (0 to 1), typically 1.
            /// </summary>
            public float maxFraction { readonly get => m_MaxFraction; set => m_MaxFraction = Mathf.Clamp01(value); }

            /// <summary>
	        /// Allow cast shape to encroach when initially touching. This only works if the radius is greater than zero.
            /// </summary>
	        public bool canEncroach { readonly get => m_CanEncroach; set => m_CanEncroach = value; }

            #region Internal

            [SerializeField] PhysicsShape.ShapeProxy m_ShapeProxy;
            [SerializeField] Vector2 m_Translation;
            [SerializeField] [Range(0.0f, 1.0f)] float m_MaxFraction;
	        [SerializeField] bool m_CanEncroach;

            #endregion
        }

        /// <summary>
        /// Cast result when performing ray-cast or shape-cast queries against geometry.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public readonly struct CastResult
        {
            /// <summary>
            /// The surface normal at the point of contact.
            /// In all non-overlapped cases, this will be a unit-normal.
            /// If there was an initial overlap, the normal will be zero (degenerate) along with the <see cref="CastResult.fraction"/> being zero.
            /// </summary>
            public readonly Vector2 normal => m_Normal;

            /// <summary>
            /// The point of contact.
            /// </summary>
            public readonly Vector2 point => m_Point;

            /// <summary>
            /// The fraction of the input translation at collision in the range (0 to 1).
            /// </summary>
            public readonly float fraction => m_Fraction;

            /// <summary>
            /// The number of iterations used in the calculation.
            /// </summary>
            public readonly int iterations => m_Iterations;

            /// <summary>
            /// Returns if the cast hit i.e. if the output is valid or not.
            /// </summary>
            public readonly bool hit => m_Hit;

            /// <summary>
            /// Implicitly convert the cast output to a bool using the value in the <see cref="CastResult.hit"/> flag.
            /// </summary>
            /// <param name="output">The CastResult to convert.</param>
            public static implicit operator bool(CastResult output) { return output.hit; }

            #region Internal

            readonly Vector2 m_Normal;
            readonly Vector2 m_Point;
            readonly float m_Fraction;
            readonly int m_Iterations;
            readonly bool m_Hit;

            #endregion

        }

        /// <summary>
        /// An input used for shape distance queries.
        /// </summary>
        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        public struct DistanceInput
        {
            /// <summary>
            /// The proxy for shape A.
            /// </summary>
            public PhysicsShape.ShapeProxy shapeProxyA { readonly get => m_ShapeProxyA; set => m_ShapeProxyA = value; }

            /// <summary>
            /// The proxy for shape B.
            /// </summary>
            public PhysicsShape.ShapeProxy shapeProxyB { readonly get => m_ShapeProxyB; set => m_ShapeProxyB = value; }

            /// <summary>
	        /// The world transform for shape A
            /// </summary>
            public PhysicsTransform transformA { readonly get => m_TransformA; set => m_TransformA = value; }

            /// <summary>
	        /// The world transform for shape B
            /// </summary>
            public PhysicsTransform transformB { readonly get => m_TransformB; set => m_TransformB = value; }

            /// <summary>
            /// Should the proxy radius be considered?
            /// </summary>                	
	        public bool useRadii { readonly get => m_UseRadii; set => m_UseRadii = value; }

            #region Internal

            [SerializeField] PhysicsShape.ShapeProxy m_ShapeProxyA;
            [SerializeField] PhysicsShape.ShapeProxy m_ShapeProxyB;
            [SerializeField] PhysicsTransform m_TransformA;
            [SerializeField] PhysicsTransform m_TransformB;
	        [SerializeField] bool m_UseRadii;

            #endregion
        }

        /// <summary>
        /// Distance result from shape distance queries.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public readonly struct DistanceResult
        {
            /// <summary>
            /// Closest point on shape A.
            /// </summary>
            public Vector2 pointA => m_PointA;

            /// <summary>
            /// Closest point on shape B.
            /// </summary>
            public Vector2 pointB => m_PointB;

            /// <summary>
            /// A Normal vector that points from A to B.
            /// This is invalid if the distance is zero.
            /// </summary>
            public Vector2 normal => m_Normal;

            /// <summary>
            /// The distance between the points, zero if overlapped.
            /// </summary>
            public float distance => m_Distance;

            /// <summary>
            /// The number of iterations used in the calculation.
            /// </summary>
            public readonly int iterations => m_Iterations;

            #region Internal

            readonly Vector2 m_PointA;
            readonly Vector2 m_PointB;
            readonly Vector2 m_Normal;
            readonly float m_Distance;
            readonly int m_Iterations;
            readonly int m_SimplexCount;

            #endregion
        }

        /// <summary>
        /// Segment distance result from segment distance queries.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public readonly struct SegmentDistanceResult
        {
            /// <summary>
            /// The closest point on the first segment
            /// </summary>
            public Vector2 closest1 => m_Closest1;

            /// <summary>
            /// The closest point on the second segment
            /// </summary>
            public Vector2 closest2 => m_Closest2;

            /// <summary>
            /// The barycentric coordinate on the first segment
            /// </summary>
            public float fraction1 => m_Fraction1;

            /// <summary>
            /// The barycentric coordinate on the second segment
            /// </summary>
            public float fraction2 => m_Fraction2;

            /// <summary>
            /// The distance between the closest points
            /// </summary>
            public float distance => m_Distance;

            #region Internal

            readonly Vector2 m_Closest1;
            readonly Vector2 m_Closest2;
            readonly float m_Fraction1;
            readonly float m_Fraction2;
            readonly float m_Distance;

            #endregion
        }

        /// <summary>
        /// Describes the motion of a shape for a time-of-impact calculation.
        /// The shape is defined with respect to the body origin.
        /// </summary>
        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        public struct ShapeSweep
        {
            /// <summary>
            /// The local center of mass.
            /// </summary>
            public Vector2 localCOM { readonly get => m_LocalCOM; set => m_LocalCOM = value; }

            /// <summary>
            /// The world center of mass start position.
            /// </summary>
            public Vector2 positionStart { readonly get => m_PositionStart; set => m_PositionStart = value; }

            /// <summary>
            /// The world center of mass end position.
            /// </summary>
            public Vector2 positionEnd { readonly get => m_PositionEnd; set => m_PositionEnd = value; }

            /// <summary>
            /// The world rotation start.
            /// </summary>
            public PhysicsRotate rotationStart { readonly get => m_RotationStart; set => m_RotationStart = value; }

            /// <summary>
            /// The world rotation end.
            /// </summary>
            public PhysicsRotate rotationEnd { readonly get => m_RotationEnd; set => m_RotationEnd = value; }

            #region Internal

            [SerializeField] Vector2 m_LocalCOM;
            [SerializeField] Vector2 m_PositionStart;
            [SerializeField] Vector2 m_PositionEnd;
            [SerializeField] PhysicsRotate m_RotationStart;
            [SerializeField] PhysicsRotate m_RotationEnd;

            #endregion
        }

        /// <summary>
        /// The input for time-of-impact query.
        /// </summary>
        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        public struct TimeOfImpactInput
        {
            /// <summary>
            /// The proxy for shape A.
            /// </summary>
            public PhysicsShape.ShapeProxy shapeProxyA { readonly get => m_ShapeProxyA; set => m_ShapeProxyA = value; }

            /// <summary>
            /// The proxy for shape B.
            /// </summary>
            public PhysicsShape.ShapeProxy shapeProxyB { readonly get => m_ShapeProxyB; set => m_ShapeProxyB = value; }

            /// <summary>
            /// The movement of shape A.
            /// </summary>
            public ShapeSweep shapeSweepA { readonly get => m_ShapeSweepA; set => m_ShapeSweepA = value; }

            /// <summary>
            /// The movement of shape B.
            /// </summary>
            public ShapeSweep shapeSweepB { readonly get => m_ShapeSweepB; set => m_ShapeSweepB = value; }

            /// <summary>
            /// The sweep interval in the range [0, maxFraction].
            /// </summary>
            public float maxFraction { readonly get => m_MaxFraction; set => m_MaxFraction = value; }

            #region Internal

            [SerializeField] PhysicsShape.ShapeProxy m_ShapeProxyA;
            [SerializeField] PhysicsShape.ShapeProxy m_ShapeProxyB;
            [SerializeField] ShapeSweep m_ShapeSweepA;
            [SerializeField] ShapeSweep m_ShapeSweepB;
            [SerializeField] float m_MaxFraction;

            #endregion
        }

        /// <summary>
        /// Time-of-impact result from time-of-impact query.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public readonly struct TimeOfImpactResult
        {
            /// <summary>
            /// Describes the time-of-impact state.
            /// </summary>
            public enum State
            {
                /// <summary>
                /// The query encountered an error and returned an unknown result.
                /// This should not happen unless a serious issue was encountered.
                /// </summary>
	            Unknown,

                /// <summary>
                /// The query failed to find a good result.
                /// The query ran out of iterations finding an impact.
                /// </summary>
	            Failed,

                /// <summary>
                /// The shapes were initially overlapped.
                /// </summary>
	            Overlapped,

                /// <summary>
                /// An impact was detected.
                /// </summary>
	            Hit,

                /// <summary>
                /// No impact was detected during the interval.
                /// </summary>
	            Separated
            }

            /// <summary>
            /// The point of contact.
            /// </summary>
            public Vector2 point => m_Point;

            /// <summary>
            /// The surface normal at the point of contact.
            /// </summary>
            public Vector2 normal => m_Normal;

            /// <summary>
            /// The impact state.
            /// </summary>
            public State impactState => m_ImpactState;

            /// <summary>
            /// The sweep time of the collision interval in the range [0, maxFraction].
            /// </summary>
            public float fraction => m_Fraction;

            #region Internal

            readonly Vector2 m_Point;
            readonly Vector2 m_Normal;
            readonly State m_ImpactState;
            readonly float m_Fraction;

            #endregion

        }
    }
}
