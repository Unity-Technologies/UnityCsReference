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
        public readonly struct CastResult : IEquatable<CastResult>
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

            #region Equality

            /// <undoc/>
            public override bool Equals(object obj) => obj is CastResult other && Equals(other);

            /// <undoc/>
            public bool Equals(CastResult other) { return m_ProxyResult == other.m_ProxyResult && m_CastResult == other.m_CastResult; }

            /// <undoc/>
            public static bool operator ==(CastResult lhs, CastResult rhs) => lhs.Equals(rhs);

            /// <undoc/>
            public static bool operator !=(CastResult lhs, CastResult rhs) => !(lhs == rhs);

            /// <undoc/>
            public override int GetHashCode() { return HashCode.Combine(m_ProxyResult, m_CastResult); }

            #endregion

            /// <summary>
            /// Ascending distance sort comparer.
            /// </summary>
            public readonly struct SortAscendingOrder : IComparer<CastResult>
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
        public readonly struct ProxyResult : IEquatable<ProxyResult>
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

            #region Equality

            /// <undoc/>
            public override bool Equals(object obj) => obj is ProxyResult other && Equals(other);

            /// <undoc/>
            public bool Equals(ProxyResult other) { return m_ProxyHandle == other.m_ProxyHandle && m_UserHandle == other.m_UserHandle; }

            /// <undoc/>
            public static bool operator ==(ProxyResult lhs, ProxyResult rhs) => lhs.Equals(rhs);

            /// <undoc/>
            public static bool operator !=(ProxyResult lhs, ProxyResult rhs) => !(lhs == rhs);

            /// <undoc/>
            public override int GetHashCode() { return HashCode.Combine(m_ProxyHandle, m_UserHandle); }

            #endregion

            #region Internal

            readonly ProxyHandle m_ProxyHandle;
            readonly PhysicsHandle m_UserHandle;

            #endregion
        }

        /// <summary>
        /// A proxy identity added to the space.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public readonly struct ProxyHandle : IEquatable<ProxyHandle>
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

            #region Equality

            /// <undoc/>
            public override bool Equals(object obj) => obj is ProxyHandle other && Equals(other);

            /// <undoc/>
            public bool Equals(ProxyHandle other) { return m_Index1 == other.m_Index1 && m_Generation == other.m_Generation && m_Space0 == other.m_Space0; }

            /// <undoc/>
            public static bool operator ==(ProxyHandle lhs, ProxyHandle rhs) => lhs.Equals(rhs);

            /// <undoc/>
            public static bool operator !=(ProxyHandle lhs, ProxyHandle rhs) => !(lhs == rhs);

            /// <undoc/>
            public override int GetHashCode() { return HashCode.Combine(m_Index1, m_Generation, m_Space0); }

            #endregion

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
        /// <param name="world">The world to find the <see cref="PhysicsShape"/> in.</param>
        /// <param name="filter">The filter to control what proxies are created.</param>
        /// <param name="destroyExistingProxies">Controls if any existing proxies are destroyed before cloning from the specified world. If false, care should be taken that any existing proxies refer to <see cref="PhysicsShape"/> otherwise a mix of user-handles will be present.</param>
        /// <returns>How many proxies were cloned.</returns>
        public readonly int Clone(PhysicsWorld world, PhysicsQuery.QueryFilter filter, bool destroyExistingProxies = true) => PhysicsSpace_Clone(this, world, filter, default, false, destroyExistingProxies);

        /// <summary>
        /// Clear any existing proxies and clone any <see cref="PhysicsShape"/> found in the specified <see cref="PhysicsWorld"/> overlapping the specified <see cref="PhysicsAABB"/>.
        /// Each proxy created will have a user-handle assigned as <see cref="PhysicsShape.physicsHandle"/>.
        /// This means you can get the referenced shape by using <see cref="PhysicsShape.PhysicsShape(PhysicsHandle)"/>.
        /// </summary>
        /// <param name="world">The world to find the <see cref="PhysicsShape"/> in.</param>
        /// <param name="aabb">The AABB used to discover <see cref="PhysicsShape"/> in the specified world. If the AABB size is size (default) then the whole world will be discovered.</param>
        /// <param name="filter">The filter to control what proxies are created.</param>
        /// <param name="destroyExistingProxies">Controls if any existing proxies are destroyed before cloning from the specified world. If false, care should be taken that any existing proxies refer to <see cref="PhysicsShape"/> otherwise a mix of user-handles will be present.</param>
        /// <returns>How many proxies were cloned.</returns>
        public readonly int Clone(PhysicsWorld world, PhysicsQuery.QueryFilter filter, PhysicsAABB aabb, bool destroyExistingProxies = true) => PhysicsSpace_Clone(this, world, filter, aabb, true, destroyExistingProxies);

        /// <summary>
        /// Create a space proxy.
        /// </summary>
        /// <param name="aabb">The AABB the proxy covers.</param>
        /// <param name="categories">The categories as a physics mask associated with the proxy. This can be used when querying the space. If not used, it should be <see cref="PhysicsMask.All"/>.</param>
        /// <param name="userHandle">The custom user handle associated with the proxy.</param>
        /// <returns>The created proxy handle used to refer to the proxy.</returns>
        public readonly ProxyHandle CreateProxy(PhysicsAABB aabb, PhysicsMask categories, PhysicsHandle userHandle) => PhysicsSpace_CreateProxy(this, aabb, categories, userHandle);

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
        public readonly void SetProxyAABB(ProxyHandle proxyHandle, PhysicsAABB aabb, bool updateAncestors) => PhysicsSpace_SetProxyAABB(this, proxyHandle, aabb, updateAncestors);

        /// <summary>
        /// Set the proxy physics AABB.
        /// </summary>
        /// <param name="proxyHandle">The proxy to get.</param>
        /// <returns>The proxy physics AABB.</returns>
        public readonly PhysicsAABB GetProxyAABB(ProxyHandle proxyHandle) => PhysicsSpace_GetProxyAABB(this, proxyHandle);

        /// <summary>
        /// Set the proxy categories.
        /// This can be an expensive operation as all ancestors need to be recalculated.
        /// </summary>
        /// <param name="proxyHandle">The proxy to set.</param>
        /// <param name="categories">The categories as a physics mask to set.</param>
        public readonly void SetProxyCategories(ProxyHandle proxyHandle, PhysicsMask categories) => PhysicsSpace_SetProxyCategories(this, proxyHandle, categories);

        /// <summary>
        /// Get the proxy categories.
        /// </summary>
        /// <param name="proxyHandle">The proxy to get.</param>
        /// <returns>The proxy categories as a physics mask.</returns>
        public readonly PhysicsMask GetProxyCategories(ProxyHandle proxyHandle) => PhysicsSpace_GetProxyCategories(this, proxyHandle);

        /// <summary>
        /// Set the proxy user handle.
        /// </summary>
        /// <param name="proxyHandle">The proxy to set.</param>
        /// <param name="userHandle">The user handle to set.</param>
        public readonly void SetProxyUserHandle(ProxyHandle proxyHandle, PhysicsHandle userHandle) => PhysicsSpace_SetProxyUserHandle(this, proxyHandle, userHandle);

        /// <summary>
        /// Get the proxy user handle.
        /// </summary>
        /// <param name="proxyHandle">The proxy to get.</param>
        /// <returns>The proxy user handle.</returns>
        public readonly PhysicsHandle GetProxyUserHandle(ProxyHandle proxyHandle) => PhysicsSpace_GetProxyUserHandle(this, proxyHandle);

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
