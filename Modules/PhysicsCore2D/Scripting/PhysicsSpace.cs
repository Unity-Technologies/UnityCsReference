// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using Unity.Collections;
using static Unity.U2D.Physics.PhysicsSpaceScripting2D;
using static Unity.U2D.Physics.Scripting2D;

namespace Unity.U2D.Physics
{
    /// <summary>
    /// Provides the ability to store and query information in a spatial database.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct PhysicsSpace : IEquatable<PhysicsSpace>
    {
        #region Id

        readonly Int32 m_Index1;
        readonly UInt16 m_Generation;

        /// <undoc/>
        public override readonly string ToString() => isValid ? $"index={m_Index1}, generation={m_Generation}" : "<INVALID>";

        #endregion

        #region Equality

        /// <undoc/>
        public override bool Equals(object obj) => obj is PhysicsSpace other && Equals(other);

        /// <undoc/>
        public bool Equals(PhysicsSpace other) { return m_Index1 == other.m_Index1 && m_Generation == other.m_Generation; }

        /// <undoc/>
        public static bool operator ==(PhysicsSpace lhs, PhysicsSpace rhs) => lhs.Equals(rhs);

        /// <undoc/>
        public static bool operator !=(PhysicsSpace lhs, PhysicsSpace rhs) => !(lhs == rhs);

        /// <undoc/>
        public override int GetHashCode() { return HashCode.Combine(m_Index1, m_Generation); }

        #endregion

        /// <summary>
        /// Common <see cref="PhysicsShape"/>-based narrowphase queries.
        /// Provided as a convenience for the typical case where space proxies have <see cref="PhysicsShape.physicsHandle"/> user handles.
        /// </summary>
        static void ValidateAllocator(Allocator allocator)
        {
            if (allocator != Allocator.Temp && allocator != Allocator.TempJob && allocator != Allocator.Persistent)
                throw new ArgumentException("Allocator must be Temp, TempJob or Persistent.", nameof(allocator));
        }

        /// <summary>
        /// Query a <see cref="PhysicsSpace"/> assuming the <see cref="PhysicsSpace.ProxyHandle"/> are all <see cref="PhysicsShape"/>.
        /// </summary>
        public static class ShapeSpace
        {
            /// <summary>
            /// Find <see cref="PhysicsShape"/> whose AABB overlap the specified AABB.
            /// The results indicate <see cref="PhysicsShape"/> AABB overlap the specified AABB, in no specific order.
            /// </summary>
            /// <param name="physicsSpace">The PhysicsSpace to query.</param>
            /// <param name="aabb">The AABB to query.</param>
            /// <param name="categories">The categories to query for.</param>
            /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
            /// <returns>The query results. This NativeArray must be disposed of after use otherwise leaks will occur. The exception to this is if the array is empty.</returns>
            /// <exception cref="ArgumentException">Thrown if the provided <see cref="PhysicsSpace"/> is not valid.</exception>
            public static NativeArray<ProxyResult> OverlapAABB(PhysicsSpace physicsSpace, PhysicsAABB aabb, PhysicsMask categories, Allocator allocator = Allocator.Temp)
            {
                // Validate.
                if (!physicsSpace.isValid)
                    throw new ArgumentException("PhysicsSpace is not valid.", nameof(physicsSpace));
                ValidateAllocator(allocator);

                // Perform the query.
                // NOTE: We use "Allocator.TempJob" here as we don't know which thread is calling.
                using var proxyResults = physicsSpace.OverlapAABB(aabb, categories, Allocator.TempJob);
                if (proxyResults.Length > 0)
                {
                    // Create a results list.
                    var results = new PhysicsList<ProxyResult>(initialCapacity: proxyResults.Length, allocator: allocator);

                    // Query the proxies.
                    foreach (var proxy in proxyResults)
                    {
                        var shape = new PhysicsShape(proxy.userHandle);
                        if (shape.isValid && shape.aabb.Overlap(aabb))
                            results.Add(proxy);
                    }

                    // Return results array.
                    return results.ToNativeArray();
                }

                return default;
            }

            /// <summary>
            /// Find <see cref="PhysicsShape"/> that overlap the specified point.
            /// The results indicate <see cref="PhysicsShape"/> overlap the specified point, in no specific order.
            /// </summary>
            /// <param name="physicsSpace">The PhysicsSpace to query.</param>
            /// <param name="point">The point used to query.</param>
            /// <param name="categories">The categories to query for.</param>
            /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
            /// <returns>The query results. This NativeArray must be disposed of after use otherwise leaks will occur. The exception to this is if the array is empty.</returns>
            /// <exception cref="ArgumentException">Thrown if the provided <see cref="PhysicsSpace"/> is not valid.</exception>
            public static NativeArray<ProxyResult> OverlapPoint(PhysicsSpace physicsSpace, Vector2 point, PhysicsMask categories, Allocator allocator = Allocator.Temp)
            {
                // Validate.
                if (!physicsSpace.isValid)
                    throw new ArgumentException("PhysicsSpace is not valid.", nameof(physicsSpace));
                ValidateAllocator(allocator);

                // Perform the query.
                // NOTE: We use "Allocator.TempJob" here as we don't know which thread is calling.
                using var proxyResults = physicsSpace.OverlapPoint(point, categories, Allocator.TempJob);
                if (proxyResults.Length > 0)
                {
                    // Create a results list.
                    var results = new PhysicsList<ProxyResult>(initialCapacity: proxyResults.Length, allocator: allocator);

                    // Query the proxies.
                    foreach (var proxy in proxyResults)
                    {
                        var shape = new PhysicsShape(proxy.userHandle);
                        if (shape.isValid && shape.OverlapPoint(point))
                            results.Add(proxy);
                    }

                    // Return results array.
                    return results.ToNativeArray();
                }

                return default;
            }

            /// <summary>
            /// Find <see cref="PhysicsShape"/> that intersect the specified ray.
            /// The results indicate <see cref="PhysicsShape"/> that intersect the specified ray, in ascending order.
            /// </summary>
            /// <param name="physicsSpace">The PhysicsSpace to query.</param>
            /// <param name="input">The configuration of the ray to cast.</param>
            /// <param name="categories">The categories to query for.</param>
            /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
            /// <returns>The query results. This NativeArray must be disposed of after use otherwise leaks will occur. The exception to this is if the array is empty.</returns>
            /// <exception cref="ArgumentException">Thrown if the provided <see cref="PhysicsSpace"/> is not valid.</exception>
            public static NativeArray<CastResult> CastRay(PhysicsSpace physicsSpace, PhysicsQuery.CastRayInput input, PhysicsMask categories, Allocator allocator = Allocator.Temp)
            {
                // Validate.
                if (!physicsSpace.isValid)
                    throw new ArgumentException("PhysicsSpace is not valid.", nameof(physicsSpace));
                ValidateAllocator(allocator);

                // Perform the query.
                // NOTE: We use "Allocator.TempJob" here as we don't know which thread is calling.
                using var proxyResults = physicsSpace.CastRay(input, categories, Allocator.TempJob);
                if (proxyResults.Length > 0)
                {
                    // Create a results list.
                    var results = new PhysicsList<CastResult>(initialCapacity: proxyResults.Length, allocator: allocator);

                    // Query the proxies.
                    foreach (var proxy in proxyResults)
                    {
                        var shape = new PhysicsShape(proxy.userHandle);
                        if (shape.isValid)
                        {
                            var castResult = shape.CastRay(input);
                            if (castResult.isValid)
                                results.Add(new CastResult(proxy, castResult));
                        }
                    }

                    // Sort results list.
                    results.Sort(new CastResult.SortAscendingOrder());

                    // Return results array.
                    return results.ToNativeArray();
                }

                return default;
            }

            /// <summary>
            /// Find <see cref="PhysicsShape"/> that intersect the specified shape.
            /// The results indicate <see cref="PhysicsShape"/> that intersect the specified cast shape, in ascending order.
            /// </summary>
            /// <param name="physicsSpace">The PhysicsSpace to query.</param>
            /// <param name="input">The configuration of the shape to cast.</param>
            /// <param name="categories">The categories to query for.</param>
            /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
            /// <returns>The query results. This NativeArray must be disposed of after use otherwise leaks will occur. The exception to this is if the array is empty.</returns>
            /// <exception cref="ArgumentException">Thrown if the provided <see cref="PhysicsSpace"/> is not valid.</exception>
            public static NativeArray<CastResult> CastShape(PhysicsSpace physicsSpace, PhysicsQuery.CastShapeInput input, PhysicsMask categories, Allocator allocator = Allocator.Temp)
            {
                // Validate.
                if (!physicsSpace.isValid)
                    throw new ArgumentException("PhysicsSpace is not valid.", nameof(physicsSpace));
                ValidateAllocator(allocator);

                // Perform the query.
                // NOTE: We use "Allocator.TempJob" here as we don't know which thread is calling.
                using var proxyResults = physicsSpace.CastShape(input, categories, Allocator.TempJob);
                if (proxyResults.Length > 0)
                {
                    // Create a results list.
                    var results = new PhysicsList<CastResult>(initialCapacity: proxyResults.Length, allocator: allocator);

                    // Query the proxies.
                    foreach (var proxy in proxyResults)
                    {
                        var shape = new PhysicsShape(proxy.userHandle);
                        if (shape.isValid)
                        {
                            var castResult = shape.CastShape(input);
                            if (castResult.isValid)
                                results.Add(new CastResult(proxy, castResult));
                        }
                    }

                    // Sort results list.
                    results.Sort(new CastResult.SortAscendingOrder());

                    // Return results array.
                    return results.ToNativeArray();
                }

                return default;
            }
        }

        /// <summary>
        /// The narrowphase cast results.
        /// </summary>
        public readonly record struct CastResult
        {
            /// <summary>
            /// Create a narrowphase result.
            /// </summary>
            /// <param name="proxyResult">The proxy result (proxy).</param>
            /// <param name="castResult">The narrowphase result (actual).</param>
            public CastResult(PhysicsSpace.ProxyResult proxyResult, PhysicsQuery.CastResult castResult)
            {
                m_ProxyResult = proxyResult;
                m_CastResult = castResult;
            }

            /// <summary>
            /// The proxy result (proxy).
            /// </summary>
            public readonly PhysicsSpace.ProxyResult proxyResult { get => m_ProxyResult; }

            /// <summary>
            /// The narrowphase result (actual).
            /// </summary>
            public readonly PhysicsQuery.CastResult castResult { get => m_CastResult; }

            /// <undoc/>
            public override readonly string ToString() => $"CastResult(proxy=({m_ProxyResult}), cast={m_CastResult})";

            /// <summary>
            /// Ascending distance sort comparer.
            /// </summary>
            public readonly record struct SortAscendingOrder : IComparer<CastResult>
            {
                /// <undoc/>
                public int Compare(CastResult x, CastResult y) => x.castResult.fraction.CompareTo(y.castResult.fraction);
            }

            #region Internal

            readonly PhysicsSpace.ProxyResult m_ProxyResult;
            readonly PhysicsQuery.CastResult m_CastResult;

            #endregion
        }

        /// <summary>
        /// A space result from <see cref="PhysicsSpace.OverlapAABB(PhysicsAABB, PhysicsMask, Allocator)"/>, <see cref="PhysicsSpace.CastRay(PhysicsQuery.CastRayInput, PhysicsMask, Allocator)"/> or <see cref="PhysicsSpace.CastShape(PhysicsQuery.CastShapeInput, PhysicsMask, Allocator)"/>.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public readonly record struct ProxyResult
        {
            /// <summary>
            /// The proxy handle.
            /// </summary>
            public readonly ProxyHandle proxyHandle { get => m_ProxyHandle; }

            /// <summary>
            /// The user handle.
            /// </summary>
            public readonly PhysicsHandle userHandle { get => m_UserHandle; }

            /// <undoc/>
            public override readonly string ToString() => $"proxyHandle=({m_ProxyHandle}), userHandle={m_UserHandle}";

            #region Internal

            readonly ProxyHandle m_ProxyHandle;
            readonly PhysicsHandle m_UserHandle;

            #endregion
        }

        /// <summary>
        /// A proxy identity added to the space.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public readonly record struct ProxyHandle
        {
            /// <summary>
            /// The Id of the proxy.
            /// </summary>
            public readonly int Id { get => m_Index1; }

            /// <summary>
            /// Whether this handle refers to a valid proxy slot.
            /// A default-constructed handle is always invalid.
            /// This does not check that the proxy still exists in any specific space, that the
            /// underlying tree slot has not been reused, or that this handle belongs to the space
            /// it is passed to. Those are checked when the handle is used.
            /// </summary>
            public readonly bool isValid => m_Index1 != 0;

            /// <undoc/>
            public override readonly string ToString() => isValid ? $"Id={m_Index1}, generation={m_Generation}, space={m_Space0}" : "<INVALID>";

            #region Internal

            readonly int m_Index1;
            readonly ushort m_Generation;
            readonly ushort m_Space0;

            #endregion
        }

        /// <summary>
        /// Create a Physics Space.
        /// </summary>
        /// <returns>The new Physics Space.</returns>
        public static PhysicsSpace Create() => PhysicsSpace_Create();

        /// <summary>
        /// Create a Physics Space bound to the specified world so its proxies represent shapes in that world.
        /// While bound, any proxy user handle must be a live shape in this world and you can refresh the proxies from their shapes.
        /// The binding lasts for the lifetime of the space and cannot be changed.
        /// </summary>
        /// <remarks>
        /// See <see cref="sourceWorld"/>, <see cref="SyncShapes"/> and <see cref="GetBatchProxyShapes"/>.
        /// </remarks>
        /// <param name="world">The world whose shapes this space's proxies will represent.</param>
        /// <returns>The new Physics Space bound to the specified world.</returns>
        public static PhysicsSpace Create(PhysicsWorld world) => PhysicsSpace_CreateWithWorld(world);

        /// <summary>
        /// Get the world this space is bound to, or a default (invalid) world if the space is not bound.
        /// A space is bound by creating it with the world overload of Create.
        /// </summary>
        /// <remarks>
        /// See <see cref="Create(PhysicsWorld)"/>.
        /// </remarks>
        public readonly PhysicsWorld sourceWorld => PhysicsSpace_GetSourceWorld(this);

        /// <summary>
        /// Destroy the Physics Space.
        /// </summary>
        /// <returns>If the space was destroyed or not.</returns>
        public readonly bool Destroy() => PhysicsSpace_Destroy(this);

        /// <summary>
        /// Destroy all active Physics Space.
        /// </summary>
        public static void DestroyAll() => PhysicsSpace_DestroyAll();

        /// <summary>
        /// Clear any existing proxies and clone all <see cref="PhysicsShape"/> found in the specified <see cref="PhysicsWorld"/>.
        /// Each proxy created will have a user-handle assigned as <see cref="PhysicsShape.physicsHandle"/>.
        /// This means you can get the referenced shape by using <see cref="PhysicsShape.PhysicsShape(PhysicsHandle)"/>.
        /// </summary>
        /// <param name="world">The world to find the <see cref="PhysicsShape"/> in. On a world-bound space this must be the world the space is bound to.</param>
        /// <param name="filter">The filter to control what proxies are created.</param>
        /// <param name="destroyExistingProxies">Controls if any existing proxies are destroyed before cloning from the specified world. If false, care should be taken that any existing proxies refer to <see cref="PhysicsShape"/> otherwise a mix of user-handles will be present.</param>
        /// <returns>How many proxies were cloned.</returns>
        /// <exception cref="System.ArgumentException">Thrown on a world-bound space when <paramref name="world"/> is not the world the space is bound to. Nothing is changed when this is thrown.</exception>
        public readonly int Clone(PhysicsWorld world, PhysicsQuery.QueryFilter filter, bool destroyExistingProxies = true)
        {
            ValidateCloneWorld(world);

            return PhysicsSpace_Clone(this, world, filter, default, false, destroyExistingProxies);
        }

        /// <summary>
        /// Clear any existing proxies and clone any <see cref="PhysicsShape"/> found in the specified <see cref="PhysicsWorld"/> overlapping the specified <see cref="PhysicsAABB"/>.
        /// Each proxy created will have a user-handle assigned as <see cref="PhysicsShape.physicsHandle"/>.
        /// This means you can get the referenced shape by using <see cref="PhysicsShape.PhysicsShape(PhysicsHandle)"/>.
        /// </summary>
        /// <param name="world">The world to find the <see cref="PhysicsShape"/> in. On a world-bound space this must be the world the space is bound to.</param>
        /// <param name="aabb">The AABB used to discover <see cref="PhysicsShape"/> in the specified world. If the AABB size is size (default) then the whole world will be discovered.</param>
        /// <param name="filter">The filter to control what proxies are created.</param>
        /// <param name="destroyExistingProxies">Controls if any existing proxies are destroyed before cloning from the specified world. If false, care should be taken that any existing proxies refer to <see cref="PhysicsShape"/> otherwise a mix of user-handles will be present.</param>
        /// <returns>How many proxies were cloned.</returns>
        /// <exception cref="System.ArgumentException">Thrown on a world-bound space when <paramref name="world"/> is not the world the space is bound to. Nothing is changed when this is thrown.</exception>
        public readonly int Clone(PhysicsWorld world, PhysicsQuery.QueryFilter filter, PhysicsAABB aabb, bool destroyExistingProxies = true)
        {
            ValidateCloneWorld(world);

            return PhysicsSpace_Clone(this, world, filter, aabb, true, destroyExistingProxies);
        }

        /// <summary>
        /// Clear any existing proxies and clone every <see cref="PhysicsShape"/> found in the world this space is bound to.
        /// Each proxy created will have a user-handle assigned as <see cref="PhysicsShape.physicsHandle"/>.
        /// This only applies to a space bound to a world.
        /// </summary>
        /// <remarks>
        /// See <see cref="Create(PhysicsWorld)"/> and <see cref="sourceWorld"/>.
        /// </remarks>
        /// <param name="filter">The filter to control what proxies are created.</param>
        /// <param name="destroyExistingProxies">Controls if any existing proxies are destroyed before cloning from the bound world. If false, care should be taken that any existing proxies refer to <see cref="PhysicsShape"/> otherwise a mix of user-handles will be present.</param>
        /// <returns>How many proxies were cloned.</returns>
        /// <exception cref="System.InvalidOperationException">Thrown when the space is not bound to a world. Create the space with the world overload of Create to bind it.</exception>
        public readonly int Clone(PhysicsQuery.QueryFilter filter, bool destroyExistingProxies = true)
        {
            var world = RequireSourceWorld();

            return PhysicsSpace_Clone(this, world, filter, default, false, destroyExistingProxies);
        }

        /// <summary>
        /// Clear any existing proxies and clone any <see cref="PhysicsShape"/> found in the world this space is bound to overlapping the specified <see cref="PhysicsAABB"/>.
        /// Each proxy created will have a user-handle assigned as <see cref="PhysicsShape.physicsHandle"/>.
        /// This only applies to a space bound to a world.
        /// </summary>
        /// <remarks>
        /// See <see cref="Create(PhysicsWorld)"/> and <see cref="sourceWorld"/>.
        /// </remarks>
        /// <param name="aabb">The AABB used to discover <see cref="PhysicsShape"/> in the bound world. If the AABB size is size (default) then the whole world will be discovered.</param>
        /// <param name="filter">The filter to control what proxies are created.</param>
        /// <param name="destroyExistingProxies">Controls if any existing proxies are destroyed before cloning from the bound world. If false, care should be taken that any existing proxies refer to <see cref="PhysicsShape"/> otherwise a mix of user-handles will be present.</param>
        /// <returns>How many proxies were cloned.</returns>
        /// <exception cref="System.InvalidOperationException">Thrown when the space is not bound to a world. Create the space with the world overload of Create to bind it.</exception>
        public readonly int Clone(PhysicsQuery.QueryFilter filter, PhysicsAABB aabb, bool destroyExistingProxies = true)
        {
            var world = RequireSourceWorld();

            return PhysicsSpace_Clone(this, world, filter, aabb, true, destroyExistingProxies);
        }

        /// <summary>
        /// Refresh every proxy from its shape, updating each proxy AABB and categories to match the live shape.
        /// This only applies to a space bound to a world.
        /// A proxy whose shape has been destroyed since it was added is skipped and reported with a single warning.
        /// </summary>
        /// <remarks>
        /// See <see cref="Create(PhysicsWorld)"/> and <see cref="sourceWorld"/>.
        /// </remarks>
        /// <returns>The number of proxies that were synced.</returns>
        /// <exception cref="System.InvalidOperationException">Thrown when the space is not bound to a world. Create the space with the world overload of Create to bind it.</exception>
        public readonly int SyncShapes()
        {
            var world = RequireSourceWorld();

            return PhysicsSpace_SyncShapes(this, world);
        }

        /// <summary>
        /// Refresh the specified proxies from their shapes, updating each proxy AABB and categories to match the live shape.
        /// This only applies to a space bound to a world.
        /// An invalid proxy handle, or a proxy whose shape has been destroyed, is skipped and reported with a single warning.
        /// </summary>
        /// <remarks>
        /// See <see cref="Create(PhysicsWorld)"/> and <see cref="sourceWorld"/>.
        /// </remarks>
        /// <param name="proxyHandles">The proxies to sync.</param>
        /// <returns>The number of proxies that were synced.</returns>
        /// <exception cref="System.InvalidOperationException">Thrown when the space is not bound to a world. Create the space with the world overload of Create to bind it.</exception>
        public readonly int SyncShapes(ReadOnlySpan<ProxyHandle> proxyHandles)
        {
            var world = RequireSourceWorld();

            return PhysicsSpace_SyncShapesProxies(this, world, proxyHandles);
        }

        /// <summary>
        /// Refresh a single proxy from its shape, updating the proxy AABB and categories to match the live shape.
        /// This only applies to a space bound to a world.
        /// The proxy is skipped if its handle is invalid or its shape has been destroyed.
        /// </summary>
        /// <remarks>
        /// See <see cref="Create(PhysicsWorld)"/> and <see cref="sourceWorld"/>.
        /// </remarks>
        /// <param name="proxyHandle">The proxy to sync.</param>
        /// <returns>The number of proxies that were synced (0 or 1).</returns>
        /// <exception cref="System.InvalidOperationException">Thrown when the space is not bound to a world. Create the space with the world overload of Create to bind it.</exception>
        public readonly int SyncShapes(ProxyHandle proxyHandle)
        {
            ReadOnlySpan<ProxyHandle> proxies = stackalloc ProxyHandle[1] { proxyHandle };
            return SyncShapes(proxies);
        }

        // When this space is bound to a live world (see sourceWorld), a proxy user handle must be a live shape in that world.
        // An unbound space, or one whose bound world has been destroyed, has no shape constraint; a bound space given a handle that is not a live shape in the bound world throws.
        // This is the deterministic single-thread check; the native create/set path re-checks under lock to close the concurrent race.
        void ValidateBoundShapeHandle(PhysicsWorld world, PhysicsHandle userHandle)
        {
            if (!world.isValid)
                return;

            var shape = new PhysicsShape(userHandle);
            if (shape.isValid && shape.world == world)
                return;

            throw new ArgumentException("The user handle must be a live shape in the world this space is bound to.", nameof(userHandle));
        }

        // When this space is bound to a live world (see sourceWorld), Clone can only target that same world.
        // An unbound space, or one whose bound world has been destroyed, may be cloned from any world; a bound space cloned from a different world throws.
        void ValidateCloneWorld(PhysicsWorld world)
        {
            var boundWorld = sourceWorld;
            if (!boundWorld.isValid || boundWorld == world)
                return;

            throw new ArgumentException("A world-bound space can only be cloned from the world it is bound to.", nameof(world));
        }

        // The shape-related operations only apply to a space bound to a live world (see sourceWorld).
        // Returns the bound world, or throws when the space is unbound or its bound world has been destroyed so a bound-only call fails loudly rather than silently doing nothing.
        PhysicsWorld RequireSourceWorld()
        {
            var world = sourceWorld;
            if (!world.isValid)
                throw new InvalidOperationException("This operation requires a space bound to a world. Create the space with the world overload of Create to bind it.");

            return world;
        }

        /// <summary>
        /// Create a space proxy.
        /// </summary>
        /// <param name="aabb">The AABB the proxy covers.</param>
        /// <param name="categories">The categories as a physics mask associated with the proxy. This can be used when querying the space. If not used, it should be <see cref="PhysicsMask.All"/>.</param>
        /// <param name="userHandle">The custom user handle associated with the proxy. On a world-bound space this must be a live shape in the bound world.</param>
        /// <returns>The created proxy handle used to refer to the proxy.</returns>
        /// <exception cref="System.ArgumentException">Thrown on a world-bound space when <paramref name="userHandle"/> is not a live shape in the bound world. No proxy is created when this is thrown.</exception>
        public readonly ProxyHandle CreateProxy(PhysicsAABB aabb, PhysicsMask categories, PhysicsHandle userHandle)
        {
            var world = sourceWorld;
            ValidateBoundShapeHandle(world, userHandle);

            return PhysicsSpace_CreateProxy(this, world, aabb, categories, userHandle);
        }

        /// <summary>
        /// Create one proxy per shape, taking each proxy AABB, categories and user handle directly from the shape.
        /// This only applies to a space bound to a world, and every shape must be a live shape in that world.
        /// </summary>
        /// <remarks>
        /// See <see cref="Create(PhysicsWorld)"/> and <see cref="sourceWorld"/>.
        /// If any shape is invalid or not in the bound world then no proxies are created and an empty array is produced.
        /// </remarks>
        /// <param name="shapes">The shapes to create proxies for.</param>
        /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
        /// <returns>The created proxy handles, one per shape in the same order. This NativeArray must be disposed of after use otherwise leaks will occur. The exception to this is if the array is empty.</returns>
        /// <exception cref="System.InvalidOperationException">Thrown when the space is not bound to a world. Create the space with the world overload of Create to bind it.</exception>
        public readonly NativeArray<ProxyHandle> CreateProxyShapes(ReadOnlySpan<PhysicsShape> shapes, Allocator allocator = Allocator.Temp)
        {
            ValidateAllocator(allocator);
            var world = RequireSourceWorld();

            // A PhysicsShape wraps a single PhysicsHandle with matching layout, so the span reinterprets to handles with no copy.
            var shapeHandles = MemoryMarshal.Cast<PhysicsShape, PhysicsHandle>(shapes);

            return PhysicsSpace_CreateBatchProxyShapes(this, world, shapeHandles, allocator).ToNativeArray<ProxyHandle>();
        }

        /// <summary>
        /// Destroy a space proxy.
        /// </summary>
        /// <param name="proxyHandle">The proxy to destroy.</param>
        /// <returns>If the proxy was destroyed. If the proxy handle is invalid, no proxy will be destroyed.</returns>
        public readonly bool DestroyProxy(ProxyHandle proxyHandle) => PhysicsSpace_DestroyProxy(this, proxyHandle);

        /// <summary>
        /// Clear all space proxies.
        /// You should no longer use any previously returned <see cref="ProxyHandle"/> as they may be invalid or direct to the wrong proxy in the future.
        /// The space will continue with a similar proxy capacity therefore if full de-allocation is required, the space should be destroyed and a new one created.
        /// </summary>
        /// <returns>If the proxies were destroyed. If the space is invalid, no proxies will be destroyed.</returns>
        public readonly bool ClearProxies() => PhysicsSpace_ClearProxies(this);

        /// <summary>
        /// Get the proxy count in the space.
        /// </summary>
        public readonly int proxyCount => PhysicsSpace_GetProxyCount(this);

        /// <summary>
        /// Get the total memory allocated for the space, in bytes.
        /// </summary>
        public readonly int memoryAllocated => PhysicsSpace_GetMemoryAllocated(this);

        /// <summary>
        /// Get the root bounds that contain all the AABB proxies.
        /// </summary>
        public readonly PhysicsAABB rootAABB => PhysicsSpace_GetRootAABB(this);

        /// <summary>
        /// Set the proxy AABB.
        /// </summary>
        /// <param name="proxyHandle">The proxy to set.</param>
        /// <param name="aabb">The AABB to set the proxy to.</param>
        /// <param name="updateAncestors">If the AABB has simply moved then this should be false however if you have changed its size then you should update the space ancestors which takes more time.</param>
        public readonly void SetProxyAABB(ProxyHandle proxyHandle, PhysicsAABB aabb, bool updateAncestors)
        {
            ReadOnlySpan<ProxyHandle> proxies = stackalloc ProxyHandle[1] { proxyHandle };
            ReadOnlySpan<PhysicsAABB> aabbs = stackalloc PhysicsAABB[1] { aabb };
            SetBatchProxyAABB(proxies, aabbs, updateAncestors);
        }

        /// <summary>
        /// Set a batch of proxy AABB, where the AABB at each index is set on the corresponding proxy at the same index.
        /// The two spans must be the same length.
        /// Any invalid proxy handle or AABB in the batch is skipped and reported with a single warning.
        /// </summary>
        /// <param name="proxyHandles">The proxies to set.</param>
        /// <param name="aabbs">The AABB to set on each corresponding proxy.</param>
        /// <param name="updateAncestors">If the AABB have simply moved then this should be false however if you have changed their size then you should update the space ancestors which takes more time. This applies to the whole batch.</param>
        public readonly void SetBatchProxyAABB(ReadOnlySpan<ProxyHandle> proxyHandles, ReadOnlySpan<PhysicsAABB> aabbs, bool updateAncestors) => PhysicsSpace_SetBatchProxyAABB(this, proxyHandles, aabbs, updateAncestors);

        /// <summary>
        /// Set the proxy physics AABB.
        /// </summary>
        /// <param name="proxyHandle">The proxy to get.</param>
        /// <returns>The proxy physics AABB.</returns>
        public readonly PhysicsAABB GetProxyAABB(ProxyHandle proxyHandle) => PhysicsSpace_GetProxyAABB(this, proxyHandle);

        /// <summary>
        /// Get a batch of proxy AABB.
        /// If any proxy handle in the batch is invalid, no results are returned and an empty array is produced.
        /// </summary>
        /// <param name="proxyHandles">The proxies to get.</param>
        /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
        /// <returns>The proxy AABB, one per proxy in the same order. This NativeArray must be disposed of after use otherwise leaks will occur. The exception to this is if the array is empty.</returns>
        public readonly NativeArray<PhysicsAABB> GetBatchProxyAABB(ReadOnlySpan<ProxyHandle> proxyHandles, Allocator allocator = Allocator.Temp)
        {
            ValidateAllocator(allocator);
            return PhysicsSpace_GetBatchProxyAABB(this, proxyHandles, allocator).ToNativeArray<PhysicsAABB>();
        }

        /// <summary>
        /// Set the proxy categories.
        /// This can be an expensive operation as all ancestors need to be recalculated.
        /// </summary>
        /// <param name="proxyHandle">The proxy to set.</param>
        /// <param name="categories">The categories as a physics mask to set.</param>
        public readonly void SetProxyCategories(ProxyHandle proxyHandle, PhysicsMask categories)
        {
            ReadOnlySpan<ProxyHandle> proxies = stackalloc ProxyHandle[1] { proxyHandle };
            ReadOnlySpan<PhysicsMask> categoryList = stackalloc PhysicsMask[1] { categories };
            SetBatchProxyCategories(proxies, categoryList);
        }

        /// <summary>
        /// Set a batch of proxy categories, where the categories at each index are set on the corresponding proxy at the same index.
        /// The two spans must be the same length.
        /// This can be an expensive operation as all ancestors need to be recalculated.
        /// Any invalid proxy handle in the batch is skipped and reported with a single warning.
        /// </summary>
        /// <param name="proxyHandles">The proxies to set.</param>
        /// <param name="categories">The categories as a physics mask to set on each corresponding proxy.</param>
        public readonly void SetBatchProxyCategories(ReadOnlySpan<ProxyHandle> proxyHandles, ReadOnlySpan<PhysicsMask> categories) => PhysicsSpace_SetBatchProxyCategories(this, proxyHandles, categories);

        /// <summary>
        /// Get the proxy categories.
        /// </summary>
        /// <param name="proxyHandle">The proxy to get.</param>
        /// <returns>The proxy categories as a physics mask.</returns>
        public readonly PhysicsMask GetProxyCategories(ProxyHandle proxyHandle) => PhysicsSpace_GetProxyCategories(this, proxyHandle);

        /// <summary>
        /// Get a batch of proxy categories.
        /// If any proxy handle in the batch is invalid, no results are returned and an empty array is produced.
        /// </summary>
        /// <param name="proxyHandles">The proxies to get.</param>
        /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
        /// <returns>The proxy categories, one per proxy in the same order. This NativeArray must be disposed of after use otherwise leaks will occur. The exception to this is if the array is empty.</returns>
        public readonly NativeArray<PhysicsMask> GetBatchProxyCategories(ReadOnlySpan<ProxyHandle> proxyHandles, Allocator allocator = Allocator.Temp)
        {
            ValidateAllocator(allocator);
            return PhysicsSpace_GetBatchProxyCategories(this, proxyHandles, allocator).ToNativeArray<PhysicsMask>();
        }

        /// <summary>
        /// Set the proxy user handle.
        /// On a world-bound space the user handle must be a live shape in the bound world.
        /// </summary>
        /// <param name="proxyHandle">The proxy to set.</param>
        /// <param name="userHandle">The user handle to set. On a world-bound space this must be a live shape in the bound world.</param>
        /// <exception cref="System.ArgumentException">Thrown on a world-bound space when <paramref name="userHandle"/> is not a live shape in the bound world. Nothing is changed when this is thrown.</exception>
        public readonly void SetProxyUserHandle(ProxyHandle proxyHandle, PhysicsHandle userHandle)
        {
            var world = sourceWorld;
            ValidateBoundShapeHandle(world, userHandle);

            PhysicsSpace_SetProxyUserHandle(this, world, proxyHandle, userHandle);
        }

        /// <summary>
        /// Get the proxy user handle.
        /// </summary>
        /// <param name="proxyHandle">The proxy to get.</param>
        /// <returns>The proxy user handle.</returns>
        public readonly PhysicsHandle GetProxyUserHandle(ProxyHandle proxyHandle) => PhysicsSpace_GetProxyUserHandle(this, proxyHandle);

        /// <summary>
        /// Get a batch of proxy user handles.
        /// If any proxy handle in the batch is invalid, no results are returned and an empty array is produced.
        /// </summary>
        /// <param name="proxyHandles">The proxies to get.</param>
        /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
        /// <returns>The proxy user handles, one per proxy in the same order. This NativeArray must be disposed of after use otherwise leaks will occur. The exception to this is if the array is empty.</returns>
        public readonly NativeArray<PhysicsHandle> GetBatchProxyUserHandle(ReadOnlySpan<ProxyHandle> proxyHandles, Allocator allocator = Allocator.Temp)
        {
            ValidateAllocator(allocator);
            return PhysicsSpace_GetBatchProxyUserHandle(this, proxyHandles, allocator).ToNativeArray<PhysicsHandle>();
        }

        /// <summary>
        /// Get a batch of proxy user handles as shapes, valid only on a space bound to a world.
        /// On a bound space every proxy user handle is a shape, so each is returned as a shape that the caller can check for validity.
        /// This only applies to a space bound to a world.
        /// </summary>
        /// <remarks>
        /// See <see cref="Create(PhysicsWorld)"/>. The returned shapes are not guaranteed to still be valid, so check <see cref="PhysicsShape.isValid"/> before use.
        /// If any proxy handle in the batch is invalid, no results are returned and an empty array is produced.
        /// </remarks>
        /// <param name="proxyHandles">The proxies to get.</param>
        /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
        /// <returns>The proxy shapes, one per proxy in the same order. This NativeArray must be disposed of after use otherwise leaks will occur. The exception to this is if the array is empty.</returns>
        /// <exception cref="System.InvalidOperationException">Thrown when the space is not bound to a world. Create the space with the world overload of Create to bind it.</exception>
        public readonly NativeArray<PhysicsShape> GetBatchProxyShapes(ReadOnlySpan<ProxyHandle> proxyHandles, Allocator allocator = Allocator.Temp)
        {
            ValidateAllocator(allocator);
            RequireSourceWorld();

            return PhysicsSpace_GetBatchProxyUserHandle(this, proxyHandles, allocator).ToNativeArray<PhysicsShape>();
        }

        /// <summary>
        /// Get all the currently active spaces.
        /// </summary>
        /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
        /// <returns>The currently active spaces. This NativeArray must be disposed of after use otherwise leaks will occur. The exception to this is if the array is empty.</returns>
        public static NativeArray<PhysicsSpace> GetSpaces(Allocator allocator = Allocator.Temp) => PhysicsSpace_GetSpaces(allocator).ToNativeArray<PhysicsSpace>();

        /// <summary>
        /// Check if a Physics Space is valid.
        /// </summary>
        public readonly bool isValid => Space_IsValid(this);

        /// <summary>
        /// Find proxies that overlap the specified AABB.
        /// The results indicate that the proxy AABB overlap the specified AABB, in no specific order.
        /// </summary>
        /// <param name="aabb">The AABB to query.</param>
        /// <param name="categories">The categories to query for.</param>
        /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
        /// <returns>The query overlap results. This NativeArray must be disposed of after use otherwise leaks will occur. The exception to this is if the array is empty.</returns>
        public readonly NativeArray<ProxyResult> OverlapAABB(PhysicsAABB aabb, PhysicsMask categories, Allocator allocator = Allocator.Temp)
        {
            ValidateAllocator(allocator);
            return PhysicsSpace_OverlapAABB(this, aabb, categories, allocator).ToNativeArray<ProxyResult>();
        }

        /// <summary>
        /// Find proxies that overlap the specified point.
        /// The results indicate that the proxy AABB overlap the specified point, in no specific order.
        /// </summary>
        /// <param name="point">The point to query.</param>
        /// <param name="categories">The categories to query for.</param>
        /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
        /// <returns>The query overlap results. This NativeArray must be disposed of after use otherwise leaks will occur. The exception to this is if the array is empty.</returns>
        public readonly NativeArray<ProxyResult> OverlapPoint(Vector2 point, PhysicsMask categories, Allocator allocator = Allocator.Temp)
        {
            ValidateAllocator(allocator);
            return PhysicsSpace_OverlapAABB(this, new PhysicsAABB(point), categories, allocator).ToNativeArray<ProxyResult>();
        }

        /// <summary>
        /// Find proxies that intersect the specified ray.
        /// The results indicate that the proxy AABB intersect the specified cast ray, in no specific order.
        /// </summary>
        /// <param name="input">The configuration of the ray to cast.</param>
        /// <param name="categories">The categories to query for.</param>
        /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
        /// <returns>The query cast results. This NativeArray must be disposed of after use otherwise leaks will occur. The exception to this is if the array is empty.</returns>
        public readonly NativeArray<ProxyResult> CastRay(PhysicsQuery.CastRayInput input, PhysicsMask categories, Allocator allocator = Allocator.Temp)
        {
            ValidateAllocator(allocator);
            return PhysicsSpace_CastRay(this, input, categories, allocator).ToNativeArray<ProxyResult>();
        }

        /// <summary>
        /// Find proxies that intersect the specified shape.
        /// The results indicate that the proxy AABB intersect the specified cast shape, in no specific order.
        /// </summary>
        /// <param name="input">The configuration of the shape to cast.</param>
        /// <param name="categories">The categories to query for.</param>
        /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
        /// <returns>The query cast results. This NativeArray must be disposed of after use otherwise leaks will occur. The exception to this is if the array is empty.</returns>
        public readonly NativeArray<ProxyResult> CastShape(PhysicsQuery.CastShapeInput input, PhysicsMask categories, Allocator allocator = Allocator.Temp)
        {
            ValidateAllocator(allocator);
            return PhysicsSpace_CastShape(this, input, categories, allocator).ToNativeArray<ProxyResult>();
        }
    }
}
