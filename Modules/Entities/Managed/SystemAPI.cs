// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Core;

namespace Unity.Entities
{
    /// <summary>
    /// Giving quick and consistent access to buffers, components, time, enumeration, singletons and more.
    /// This includes any <see cref="IJobEntity"/>, <see cref="SystemBase"/>, and <see cref="ISystem"/>.
    /// Suggested usage is:
    /// ```cs
    ///   using static Unity.Entities.SystemAPI;
    ///   [...]
    ///   partial struct SomeJob : IJobEntity { void Execute(ref EcsTestData e1) => e1.value += Time.deltaTime; }
    /// ```
    /// </summary>
    public static class SystemAPI
    {
        #region QueryBuilder
        /// <summary>
        /// Gives a fluent API for constructing <see cref="EntityQuery"/>s similar to the <see cref="Unity.Entities.EntityQueryBuilder"/>. This API statically constructs the query as part of the system, and creates the query in the system's OnCreate.
        /// Calling this method should allow the construction of EntityQueries with almost no run-time performance overhead."
        /// </summary>
        /// <remarks>
        /// Gives a fluent API for constructing EntityQueries similar to the EntityQueryBuilder. This API statically constructs the query as part of the system, and creates the query in the system's OnCreate.
        /// Calling this method should allow for construction of EntityQueries with almost no run-time performance overhead."
        /// </remarks>
        /// <returns>An instance of `SystemAPIQueryBuilder`, which can be used to fluently construct queries.</returns>
        /// <exception cref="Internal.InternalCompilerInterface.ThrowCodeGenException">Exception indicating that this method has been called outside of a valid context.</exception>
        public static SystemAPIQueryBuilder QueryBuilder() =>  throw Internal.InternalCompilerInterface.ThrowCodeGenException();
        #endregion

        #region Query
        /////////////////////////////////// Query Caching ///////////////////////////////////

        /// <summary>
        /// Get Enumerable for iterating through Component types from inside a system
        /// </summary>
        /// <typeparam name="T1">RefRO or RefRW parameter type</typeparam>
        /// <returns>QueryEnumerable that allows enumerating over all RefRO and RefRW of a given type.</returns>
        /// <exception cref="Internal.InternalCompilerInterface.ThrowCodeGenException">Exception indicating that this method has been called outside of a valid context.</exception>
        /// <remarks> Not working in Entities.ForEach, IJobEntity, and Utility methods</remarks>
        public static QueryEnumerable<T1> Query<T1>()
            where T1 : unmanaged, IQueryTypeParameter
            =>  throw Internal.InternalCompilerInterface.ThrowCodeGenException();

        /// <summary>
        /// Get Enumerable for iterating through Component types from inside a system
        /// </summary>
        /// <typeparam name="T1">RefRO or RefRW parameter type</typeparam>
        /// <typeparam name="T2">RefRO or RefRW parameter type</typeparam>
        /// <returns>QueryEnumerable that allows enumerating over all RefRO and RefRW of a given type.</returns>
        /// <exception cref="Internal.InternalCompilerInterface.ThrowCodeGenException">Exception indicating that this method has been called outside of a valid context.</exception>
        /// <remarks> Not working in Entities.ForEach, IJobEntity, and Utility methods</remarks>
        public static QueryEnumerable<T1, T2> Query<T1, T2>()
            where T1 : unmanaged, IQueryTypeParameter
            where T2 : unmanaged, IQueryTypeParameter
            =>  throw Internal.InternalCompilerInterface.ThrowCodeGenException();

        /// <summary>
        /// Get Enumerable for iterating through Component types from inside a system
        /// </summary>
        /// <typeparam name="T1">RefRO or RefRW parameter type</typeparam>
        /// <typeparam name="T2">RefRO or RefRW parameter type</typeparam>
        /// <typeparam name="T3">RefRO or RefRW parameter type</typeparam>
        /// <returns>QueryEnumerable that allows enumerating over all entities with a given type.</returns>
        /// <exception cref="Internal.InternalCompilerInterface.ThrowCodeGenException">Exception indicating that this method has been called outside of a valid context.</exception>
        /// <remarks> Not working in Entities.ForEach, IJobEntity, and Utility methods</remarks>
        public static QueryEnumerable<T1, T2, T3> Query<T1, T2, T3>()
            where T1 : unmanaged, IQueryTypeParameter
            where T2 : unmanaged, IQueryTypeParameter
            where T3 : unmanaged, IQueryTypeParameter
            =>  throw Internal.InternalCompilerInterface.ThrowCodeGenException();

        /// <summary>
        /// Get Enumerable for iterating through Component types from inside a system
        /// </summary>
        /// <typeparam name="T1">RefRO or RefRW parameter type</typeparam>
        /// <typeparam name="T2">RefRO or RefRW parameter type</typeparam>
        /// <typeparam name="T3">RefRO or RefRW parameter type</typeparam>
        /// <typeparam name="T4">RefRO or RefRW parameter type</typeparam>
        /// <returns>QueryEnumerable that allows enumerating over all RefRO and RefRW of a given type.</returns>
        /// <exception cref="Internal.InternalCompilerInterface.ThrowCodeGenException">Exception indicating that this method has been called outside of a valid context.</exception>
        /// <remarks> Not working in Entities.ForEach, IJobEntity, and Utility methods</remarks>
        public static QueryEnumerable<T1, T2, T3, T4> Query<T1, T2, T3, T4>()
            where T1 : unmanaged, IQueryTypeParameter
            where T2 : unmanaged, IQueryTypeParameter
            where T3 : unmanaged, IQueryTypeParameter
            where T4 : unmanaged, IQueryTypeParameter
            =>  throw Internal.InternalCompilerInterface.ThrowCodeGenException();

        /// <summary>
        /// Get Enumerable for iterating through Component types from inside a system
        /// </summary>
        /// <typeparam name="T1">RefRO or RefRW parameter type</typeparam>
        /// <typeparam name="T2">RefRO or RefRW parameter type</typeparam>
        /// <typeparam name="T3">RefRO or RefRW parameter type</typeparam>
        /// <typeparam name="T4">RefRO or RefRW parameter type</typeparam>
        /// <typeparam name="T5">RefRO or RefRW parameter type</typeparam>
        /// <returns>QueryEnumerable that allows enumerating over all RefRO and RefRW of a given type.</returns>
        /// <exception cref="Internal.InternalCompilerInterface.ThrowCodeGenException">Exception indicating that this method has been called outside of a valid context.</exception>
        /// <remarks> Not working in Entities.ForEach, IJobEntity, and Utility methods</remarks>
        public static QueryEnumerable<T1, T2, T3, T4, T5> Query<T1, T2, T3, T4, T5>()
            where T1 : unmanaged, IQueryTypeParameter
            where T2 : unmanaged, IQueryTypeParameter
            where T3 : unmanaged, IQueryTypeParameter
            where T4 : unmanaged, IQueryTypeParameter
            where T5 : unmanaged, IQueryTypeParameter
            =>  throw Internal.InternalCompilerInterface.ThrowCodeGenException();

        /// <summary>
        /// Get Enumerable for iterating through Component types from inside a system
        /// </summary>
        /// <typeparam name="T1">RefRO or RefRW parameter type</typeparam>
        /// <typeparam name="T2">RefRO or RefRW parameter type</typeparam>
        /// <typeparam name="T3">RefRO or RefRW parameter type</typeparam>
        /// <typeparam name="T4">RefRO or RefRW parameter type</typeparam>
        /// <typeparam name="T5">RefRO or RefRW parameter type</typeparam>
        /// <typeparam name="T6">RefRO or RefRW parameter type</typeparam>
        /// <returns>QueryEnumerable that allows enumerating over all RefRO and RefRW of a given type.</returns>
        /// <exception cref="Internal.InternalCompilerInterface.ThrowCodeGenException">Exception indicating that this method has been called outside of a valid context.</exception>
        /// <remarks> Not working in Entities.ForEach, IJobEntity, and Utility methods</remarks>
        public static QueryEnumerable<T1, T2, T3, T4, T5, T6> Query<T1, T2, T3, T4, T5, T6>()
            where T1 : unmanaged, IQueryTypeParameter
            where T2 : unmanaged, IQueryTypeParameter
            where T3 : unmanaged, IQueryTypeParameter
            where T4 : unmanaged, IQueryTypeParameter
            where T5 : unmanaged, IQueryTypeParameter
            where T6 : unmanaged, IQueryTypeParameter
            =>  throw Internal.InternalCompilerInterface.ThrowCodeGenException();

        /// <summary>
        /// Get Enumerable for iterating through Component types from inside a system
        /// </summary>
        /// <typeparam name="T1">RefRO or RefRW parameter type</typeparam>
        /// <typeparam name="T2">RefRO or RefRW parameter type</typeparam>
        /// <typeparam name="T3">RefRO or RefRW parameter type</typeparam>
        /// <typeparam name="T4">RefRO or RefRW parameter type</typeparam>
        /// <typeparam name="T5">RefRO or RefRW parameter type</typeparam>
        /// <typeparam name="T6">RefRO or RefRW parameter type</typeparam>
        /// <typeparam name="T7">RefRO or RefRW type</typeparam>
        /// <returns>QueryEnumerable that allows enumerating over all RefRO and RefRW of a given type.</returns>
        /// <exception cref="Internal.InternalCompilerInterface.ThrowCodeGenException">Exception indicating that this method has been called outside of a valid context.</exception>
        /// <remarks> Not working in Entities.ForEach, IJobEntity, and Utility methods</remarks>
        public static QueryEnumerable<T1, T2, T3, T4, T5, T6, T7> Query<T1, T2, T3, T4, T5, T6, T7>()
            where T1 : unmanaged, IQueryTypeParameter
            where T2 : unmanaged, IQueryTypeParameter
            where T3 : unmanaged, IQueryTypeParameter
            where T4 : unmanaged, IQueryTypeParameter
            where T5 : unmanaged, IQueryTypeParameter
            where T6 : unmanaged, IQueryTypeParameter
            where T7 : unmanaged, IQueryTypeParameter
            =>  throw Internal.InternalCompilerInterface.ThrowCodeGenException();
        #endregion

        #region Time
        /////////////////////////////////// TimeData Caching ///////////////////////////////////

        /// <summary>
        /// The current Time data for calling system's world.
        /// </summary>
        /// <remarks> Not working in IJobEntity, and Utility methods</remarks>
        public static ref readonly TimeData Time =>  throw Internal.InternalCompilerInterface.ThrowCodeGenException();
        #endregion

        #region ComponentData
        /////////////////////////////////// GetComponentLookup Caching ///////////////////////////////////

        /// <summary>
        /// Gets a dictionary-like container containing all components of type T, keyed by Entity.
        /// </summary>
        /// <param name="isReadOnly">Whether the data is only read, not written.
        /// Access data as read-only whenever possible.</param>
        /// <typeparam name="T">A struct that implements <see cref="IComponentData"/>.</typeparam>
        /// <returns>All component data of type T.</returns>
        /// <remarks>
        /// When you call this method this method gets replaced direct access to a cached <see cref="ComponentLookup{T}"/>.
        /// </remarks>
        /// <remarks> Not working in IJobEntity, and Utility methods</remarks>
        public static ComponentLookup<T> GetComponentLookup<T>(bool isReadOnly = false)
            where T : unmanaged, IComponentData =>  throw Internal.InternalCompilerInterface.ThrowCodeGenException();

        /// <summary>
        /// Look up the value of a component for an entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <typeparam name="T">The type of component to retrieve.</typeparam>
        /// <returns>A struct of type T containing the component value.</returns>
        /// <remarks>
        /// Use this method to look up data in another entity using its <see cref="Entity"/> object. For example, if you
        /// have a component that contains an Entity field, you can look up the component data for the referenced
        /// entity using this method.
        ///
        /// When iterating over a set of entities with an idomatic foreach query, don't use this method to access the data of the
        /// current entity in the set. It's faster to access the data by requesting it as an argument of a query.
        ///
        /// When you call this method it gets replaced with component access methods through <see cref="ComponentLookup{T}"/>.
        ///
        /// This lookup method results in a slower, indirect memory access. When possible, organize your
        /// data to minimize the need for indirect lookups.
        /// </remarks>
        /// <exception cref="ArgumentException">Thrown if the component type has no fields.</exception>
        /// <remarks> Not working in IJobEntity, and Utility methods </remarks>
        public static T GetComponent<T>(Entity entity) where T : unmanaged, IComponentData =>  throw Internal.InternalCompilerInterface.ThrowCodeGenException();

        /// <summary>
        /// Gets a reference to a component for an entity, for read-only access.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <typeparam name="T">The type of component to retrieve.</typeparam>
        /// <returns>A read/write reference to component T.</returns>
        /// <remarks>
        /// Use this method to look up data in another entity using its <see cref="Entity"/> object. For example, if you
        /// have a component that contains an Entity field, you can look up the component data for the referenced
        /// entity using this method.
        ///
        /// When iterating over a set of entities with an idomatic foreach query, don't use this method to access the data of the
        /// current entity in the set. It's faster to access the data by requesting it as an argument of a query.
        ///
        /// When you call this method it gets replaced with component access methods through <see cref="ComponentLookup{T}"/>.
        ///
        /// This lookup method results in a slower, indirect memory access. When possible, organize your
        /// data to minimize the need for indirect lookups.
        /// </remarks>
        /// <exception cref="ArgumentException">Thrown if the component type has no fields.</exception>
        /// <remarks> Not working in IJobEntity, and Utility methods </remarks>
        public static RefRO<T> GetComponentRO<T>(Entity entity) where T : unmanaged, IComponentData =>  throw Internal.InternalCompilerInterface.ThrowCodeGenException();

        /// <summary>
        /// Gets a reference to a component for an entity, for read/write access.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <typeparam name="T">The type of component to retrieve.</typeparam>
        /// <returns>A read/write reference to component T.</returns>
        /// <remarks>
        /// Use this method to look up data in another entity using its <see cref="Entity"/> object. For example, if you
        /// have a component that contains an Entity field, you can look up the component data for the referenced
        /// entity using this method.
        ///
        /// When iterating over a set of entities with an idomatic foreach query, don't use this method to access the data of the
        /// current entity in the set. It's faster to access the data by requesting it as an argument of a query.
        ///
        /// When you call this method it gets replaced with component access methods through <see cref="ComponentLookup{T}"/>.
        ///
        /// This lookup method results in a slower, indirect memory access. When possible, organize your
        /// data to minimize the need for indirect lookups.
        /// </remarks>
        /// <exception cref="ArgumentException">Thrown if the component type has no fields.</exception>
        /// <remarks> Not working in IJobEntity, and Utility methods </remarks>
        public static RefRW<T> GetComponentRW<T>(Entity entity) where T : unmanaged, IComponentData =>  throw Internal.InternalCompilerInterface.ThrowCodeGenException();

        /// <summary>
        /// Try to look up the value of a component for an entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="result">A struct of type T containing the component value, or default if the entity does not have the component.</param>
        /// <typeparam name="T">
        /// The type of component to retrieve.
        /// </typeparam>
        /// <returns>True if the entity has the component, false otherwise.</returns>
        /// <remarks>
        /// Use this method to look up data in another entity using its <see cref="Entity"/> object. For example, if you
        /// have a component that contains an Entity field, you can look up the component data for the referenced entity using this method.
        /// </remarks>
        /// <exception cref="ArgumentException">Thrown if the component type has no fields.</exception>
        /// <remarks> Not working in Entities.ForEach, IJobEntity, and Utility methods </remarks>
        public static bool TryGetComponent<T>(Entity entity, out T result) where T : unmanaged, IComponentData =>  throw Internal.InternalCompilerInterface.ThrowCodeGenException();

        /// <summary>
        /// Sets the value of a component of an entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="component">The data to set.</param>
        /// <typeparam name="T">The component type.</typeparam>
        /// <remarks>
        /// Use this method to look up and set data in another entity using its <see cref="Entity"/> object. For example, if you
        /// have a component that contains an Entity field, you can update the component data for the referenced
        /// entity using this method.
        ///
        /// When iterating over a set of entities with an idomatic foreach query, don't use this method to access the data of the
        /// current entity in the set. It's faster to access the data by requesting it as an argument of a query.
        ///
        /// When you call this method gets replaced with component access methods through <see cref="ComponentLookup{T}"/>.
        ///
        /// In both cases, this lookup method results in a slower, indirect memory access. When possible, organize your
        /// data to minimize the need for indirect lookups.
        /// </remarks>
        /// <exception cref="ArgumentException">Thrown if the component type has no fields.</exception>
        /// <remarks> Not working in IJobEntity, and Utility methods </remarks>
        public static void SetComponent<T>(Entity entity, T component) where T : unmanaged, IComponentData =>  throw Internal.InternalCompilerInterface.ThrowCodeGenException();

        /// <summary>
        /// Checks whether an entity has a specific type of component.
        /// </summary>
        /// <param name="entity">The Entity object.</param>
        /// <typeparam name="T">The data type of the component.</typeparam>
        /// <remarks>
        /// Always returns false for an entity that has been destroyed.
        ///
        /// Use this method to check if another entity has a given type of component using its <see cref="Entity"/>
        /// object. For example, if you have a component that contains an Entity field, you can check whether the
        /// referenced entity has a specific type of component using this method. (Entities in the set always have
        /// required components, so you don’t need to check for them.)
        ///
        /// When iterating over a set of entities with an idomatic foreach query, don't use this method to access the data of the
        /// current entity in the set. It's faster to change your query to avoid using optional components.
        ///
        /// When you call this method this method gets replaced with component access methods through a cached <see cref="ComponentLookup{T}"/>.
        ///
        /// This lookup method results in a slower, indirect memory access. When possible, organize your data to minimize the need for indirect lookups.
        /// </remarks>
        /// <returns>True, if the specified entity has the component.</returns>
        /// <remarks> Not working in IJobEntity, and Utility methods </remarks>
        public static bool HasComponent<T>(Entity entity) where T : unmanaged, IComponentData =>  throw Internal.InternalCompilerInterface.ThrowCodeGenException();

        /// <summary>
        /// Checks whether the <see cref="IComponentData"/> of type T is enabled on the specified <see cref="Entity"/>.
        /// For the purposes of EntityQuery matching, an entity with a disabled component will behave as if it does not
        /// have that component. The type T must implement the <see cref="IEnableableComponent"/> interface.
        /// </summary>
        /// <exception cref="ArgumentException">The <see cref="Entity"/> does not exist.</exception>
        /// <param name="entity">The entity whose component should be checked.</param>
        /// <typeparam name="T">An unmanaged IComponentData type</typeparam>
        /// <returns>True if the specified component is enabled, or false if it is disabled.</returns>
        /// <seealso cref="SetComponentEnabled{T}(Entity, bool)"/>
        /// <remarks> Not working in Entities.ForEach, IJobEntity, and Utility methods </remarks>
        public static bool IsComponentEnabled<T>(Entity entity) where T : unmanaged, IComponentData, IEnableableComponent =>  throw Internal.InternalCompilerInterface.ThrowCodeGenException();

        /// <summary>
        /// Enable or disable the <see cref="IComponentData"/> of type T on the specified <see cref="Entity"/>. This operation
        /// does not cause a structural change (even if it occurs on a worker thread), or affect the value of the component.
        /// For the purposes of EntityQuery matching, an entity with a disabled component will behave as if it does not
        /// have that component. The type T must implement the <see cref="IEnableableComponent"/> interface.
        /// </summary>
        /// <exception cref="ArgumentException">The <see cref="Entity"/> does not exist.</exception>
        /// <param name="entity">The entity whose component should be enabled or disabled.</param>
        /// <param name="value">True if the specified component should be enabled, or false if it should be disabled.</param>
        /// /// <typeparam name="T">An unmanaged IComponentData type</typeparam>
        /// <seealso cref="IsComponentEnabled{T}(Entity)"/>
        /// <remarks> Not working in Entities.ForEach, IJobEntity, and Utility methods </remarks>
        public static void SetComponentEnabled<T>(Entity entity, bool value) where T : unmanaged, IComponentData, IEnableableComponent =>  throw Internal.InternalCompilerInterface.ThrowCodeGenException();

        /// <summary>
        /// Gets the value of a component for an entity associated with a system.
        /// </summary>
        /// <param name="systemHandle">The system handle.</param>
        /// <typeparam name="T">The type of component to retrieve.</typeparam>
        /// <returns>A struct of type T containing the component value.</returns>
        /// <remarks>
        /// Use this method to look up data in another system owned entity using its <see cref="SystemHandle"/> object.
        ///
        /// When you call this method it gets replaced with component access methods through <see cref="ComponentLookup{T}"/>.
        /// </remarks>
        /// <exception cref="ArgumentException">Thrown if the component type has no fields.</exception>
        /// <remarks> Not working in IJobEntity, and Utility methods </remarks>
        public static T GetComponent<T>(SystemHandle systemHandle) where T : unmanaged, IComponentData =>  throw Internal.InternalCompilerInterface.ThrowCodeGenException();

        /// <summary>
        /// Gets a reference to a component for an entity associated with a system, for read/write access.
        /// </summary>
        /// <param name="systemHandle">The system handle.</param>
        /// <typeparam name="T">The type of component to retrieve.</typeparam>
        /// <returns>A read/write reference to component T.</returns>
        /// <remarks>
        /// Use this method to look up data in another system owned entity using its <see cref="SystemHandle"/> object.
        ///
        /// When you call this method it gets replaced with component access methods through <see cref="ComponentLookup{T}"/>.
        ///
        /// </remarks>
        /// <exception cref="ArgumentException">Thrown if the component type has no fields.</exception>
        /// <remarks> Not working in IJobEntity, and Utility methods </remarks>
        public static RefRW<T> GetComponentRW<T>(SystemHandle systemHandle) where T : unmanaged, IComponentData =>  throw Internal.InternalCompilerInterface.ThrowCodeGenException();

        /// <summary>
        /// Sets the value of a component of an entity associated with a system.
        /// </summary>
        /// <param name="systemHandle">The system handle.</param>
        /// <param name="component">The data to set.</param>
        /// <typeparam name="T">The component type.</typeparam>
        /// <remarks>
        /// Use this method to look up and set data in another system owned entity using its <see cref="SystemHandle"/> object.
        ///
        /// When you call this method gets replaced with component access methods  through <see cref="ComponentLookup{T}"/>.
        ///
        /// </remarks>
        /// <exception cref="ArgumentException">Thrown if the component type has no fields.</exception>
        /// <remarks> Not working in IJobEntity, and Utility methods </remarks>
        public static void SetComponent<T>(SystemHandle systemHandle, T component) where T : unmanaged, IComponentData =>  throw Internal.InternalCompilerInterface.ThrowCodeGenException();

        /// <summary>
        /// Checks whether an entity associated with a system has a specific type of component.
        /// </summary>
        /// <param name="systemHandle">The system handle.</param>
        /// <typeparam name="T">The data type of the component.</typeparam>
        /// <remarks>
        /// Always returns false for an system that has been destroyed.
        ///
        /// Use this method to check if another system owned entity has a given type of component using its <see cref="SystemHandle"/>
        /// object.
        ///
        /// When you call this method this method gets replaced with component access methods through a cached <see cref="ComponentLookup{T}"/>.
        /// </remarks>
        /// <returns>True, if the specified system owned entity has the component.</returns>
        /// <remarks> Not working in IJobEntity, and Utility methods </remarks>
        public static bool HasComponent<T>(SystemHandle systemHandle) where T : unmanaged, IComponentData =>  throw Internal.InternalCompilerInterface.ThrowCodeGenException();

        /// <summary>
        /// Checks whether the <see cref="IComponentData"/> of type T is enabled on the specified system using a <see cref="SystemHandle"/>.
        /// For the purposes of EntityQuery matching, a system with a disabled component will behave as if it does not
        /// have that component. The type T must implement the <see cref="IEnableableComponent"/> interface.
        /// </summary>
        /// <exception cref="ArgumentException">The <see cref="SystemHandle"/> does not exist.</exception>
        /// <param name="systemHandle">The system whose component should be checked.</param>
        /// <typeparam name="T">An unmanaged IComponentData type</typeparam>
        /// <returns>True if the specified component is enabled, or false if it is disabled.</returns>
        /// <seealso cref="SetComponentEnabled{T}(SystemHandle, bool)"/>
        /// <remarks> Not working in Entities.ForEach, IJobEntity, and Utility methods </remarks>
        public static bool IsComponentEnabled<T>(SystemHandle systemHandle) where T : unmanaged, IComponentData, IEnableableComponent =>  throw Internal.InternalCompilerInterface.ThrowCodeGenException();

        /// <summary>
        /// Enable or disable the <see cref="IComponentData"/> of type T on the specified system using a <see cref="SystemHandle"/>. This operation
        /// does not cause a structural change (even if it occurs on a worker thread), or affect the value of the component.
        /// For the purposes of EntityQuery matching, a system with a disabled component will behave as if it does not
        /// have that component. The type T must implement the <see cref="IEnableableComponent"/> interface.
        /// </summary>
        /// <exception cref="ArgumentException">The <see cref="SystemHandle"/> does not exist.</exception>
        /// <param name="systemHandle">The system whose component should be enabled or disabled.</param>
        /// <param name="value">True if the specified component should be enabled, or false if it should be disabled.</param>
        /// /// <typeparam name="T">An unmanaged IComponentData type</typeparam>
        /// <seealso cref="IsComponentEnabled{T}(Entity)"/>
        /// <remarks> Not working in Entities.ForEach, IJobEntity, and Utility methods </remarks>
        public static void SetComponentEnabled<T>(SystemHandle systemHandle, bool value) where T : unmanaged, IComponentData, IEnableableComponent =>  throw Internal.InternalCompilerInterface.ThrowCodeGenException();

        #endregion

        #region Buffer
        /////////////////////////////////// GetBufferLookup Caching ///////////////////////////////////

        /// <summary>
        /// Gets a BufferLookup&lt;T&gt; object that can access a <see cref="DynamicBuffer{T}"/>.
        /// </summary>
        /// <remarks>
        /// This method gets replaced direct with access to a cached <see cref="BufferLookup{T}"/>.
        /// </remarks>
        /// <param name="isReadOnly">Whether the buffer data is only read or is also written. Access data in
        /// a read-only fashion whenever possible.</param>
        /// <typeparam name="T">The type of <see cref="IBufferElementData"/> stored in the buffer.</typeparam>
        /// <returns>An array-like object that provides access to buffers, indexed by <see cref="Entity"/>.</returns>
        /// <seealso cref="ComponentLookup{T}"/>
        /// <remarks> Not working in IJobEntity, and Utility methods</remarks>
        public static BufferLookup<T> GetBufferLookup<T>(bool isReadOnly = false)
            where T : unmanaged, IBufferElementData =>  throw Internal.InternalCompilerInterface.ThrowCodeGenException();

        /// <summary>
        /// Gets the dynamic buffer of an entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <remarks>
        /// This method gets replaced with component access methods through a cached <see cref="BufferLookup{T}"/>.
        /// </remarks>
        /// <typeparam name="T">The type of the buffer's elements.</typeparam>
        /// <returns>The DynamicBuffer object for accessing the buffer contents.</returns>
        /// <exception cref="ArgumentException">Thrown if T is an unsupported type.</exception>
        /// <remarks> Not working in IJobEntity, and Utility methods</remarks>
        public static DynamicBuffer<T> GetBuffer<T>(Entity entity) where T : unmanaged, IBufferElementData =>  throw Internal.InternalCompilerInterface.ThrowCodeGenException();

        /// <summary>
        /// Checks whether an entity has a dynamic buffer of a specific IBufferElementData type.
        /// </summary>
        /// <param name="entity">The Entity object.</param>
        /// <typeparam name="T">The IBufferElementData type.</typeparam>
        /// <remarks>
        /// Always returns false for an entity that has been destroyed.
        ///
        /// Use this method to check if another entity has a dynamic buffer of a given IBufferElementData type using its <see cref="Entity"/>
        /// object.
        ///
        /// When iterating over a set of entities with an idomatic foreach query, don't use this method to access the data of the
        /// current entity in the set. It's faster to change your query to avoid using optional components.
        ///
        /// this method gets replaced with component access methods through <see cref="BufferLookup{T}"/>.
        ///
        /// In both cases, this lookup method results in a slower, indirect memory access. When possible, organize your
        /// data to minimize the need for indirect lookups.
        ///
        /// </remarks>
        /// <returns>True, if the specified entity has the component.</returns>
        /// <remarks> Not working in IJobEntity, and Utility methods</remarks>
        public static bool HasBuffer<T>(Entity entity) where T : unmanaged, IBufferElementData =>  throw Internal.InternalCompilerInterface.ThrowCodeGenException();

        /// <summary>
        /// Checks whether the <see cref="IBufferElementData"/> of type T is enabled on the specified <see cref="Entity"/>.
        /// For the purposes of EntityQuery matching, an entity with a disabled component will behave as if it does not
        /// have that component. The type T must implement the <see cref="IEnableableComponent"/> interface.
        /// </summary>
        /// <exception cref="ArgumentException">The <see cref="Entity"/> does not exist.</exception>
        /// <param name="entity">The entity whose component should be checked.</param>
        /// <typeparam name="T">An unmanaged IComponentData type</typeparam>
        /// <returns>True if the specified component is enabled, or false if it is disabled.</returns>
        /// <seealso cref="SetBufferEnabled{T}(Entity, bool)"/>
        /// <remarks> Not working in Entities.ForEach, IJobEntity, and Utility methods </remarks>
        public static bool IsBufferEnabled<T>(Entity entity) where T : unmanaged, IBufferElementData, IEnableableComponent =>  throw Internal.InternalCompilerInterface.ThrowCodeGenException();

        /// <summary>
        /// Enable or disable the <see cref="IBufferElementData"/> of type T on the specified <see cref="Entity"/>. This operation
        /// does not cause a structural change (even if it occurs on a worker thread), or affect the value of the component.
        /// For the purposes of EntityQuery matching, an entity with a disabled component will behave as if it does not
        /// have that component. The type T must implement the <see cref="IEnableableComponent"/> interface.
        /// </summary>
        /// <exception cref="ArgumentException">The <see cref="Entity"/> does not exist.</exception>
        /// <param name="entity">The entity whose component should be enabled or disabled.</param>
        /// <param name="value">True if the specified component should be enabled, or false if it should be disabled.</param>
        /// /// <typeparam name="T">An unmanaged IComponentData type</typeparam>
        /// <seealso cref="IsBufferEnabled{T}(Entity)"/>
        /// <remarks> Not working in Entities.ForEach, IJobEntity, and Utility methods </remarks>
        public static void SetBufferEnabled<T>(Entity entity, bool value) where T : unmanaged, IBufferElementData, IEnableableComponent =>  throw Internal.InternalCompilerInterface.ThrowCodeGenException();
        #endregion

        #region StorageInfo
        /////////////////////////////////// GetEntityStorageInfoLookup Caching ///////////////////////////////////

        /// <summary>
        /// Gets a EntityStorageInfoLookup object that can access a <seealso cref="EntityStorageInfo"/>.
        /// </summary>
        /// <returns>A dictionary-like object that provides access to information about how Entities are stored,
        /// indexed by <see cref="Entity"/>.</returns>
        /// <seealso cref="EntityStorageInfoLookup"/>
        /// <remarks> Not working in IJobEntity, and Utility methods</remarks>
        public static EntityStorageInfoLookup GetEntityStorageInfoLookup() =>  throw Internal.InternalCompilerInterface.ThrowCodeGenException();

        /// <summary>
        /// Checks if the entity exists inside this system's EntityManager.
        /// </summary>
        /// <remarks>
        /// This returns true for an entity that was destroyed with DestroyEntity, but still has a cleanup component.
        /// Prefer <see cref="ComponentLookup{T}.TryGetComponent"/> where applicable.
        /// Can be used inside of Entities.ForEach.
        /// </remarks>
        /// <param name="entity">The entity to check</param>
        /// <returns>True if the given entity exists or the entity has a Cleanup Component that is yet to be destroyed</returns>
        /// <seealso cref="EntityManager.Exists"/>
        /// <remarks> Not working in IJobEntity, and Utility methods</remarks>
        public static bool Exists(Entity entity) =>  throw Internal.InternalCompilerInterface.ThrowCodeGenException();
        #endregion

        #region Singleton
        /////////////////////////////////// Singleton Caching ///////////////////////////////////

        /// <summary>
        /// Gets the value of a singleton component.
        /// </summary>
        /// <typeparam name="T">The <see cref="IComponentData"/> subtype of the singleton component.
        /// This component type must not implement <see cref="IEnableableComponent"/></typeparam>
        /// <returns>The component.</returns>
        /// <seealso cref="ComponentSystemBase.GetSingletonRW{T}"/>
        /// <seealso cref="EntityQuery.GetSingleton{T}"/>
        /// <seealso cref="EntityQuery.GetSingletonRW{T}"/>
        /// <remarks> Not working in Entities.ForEach, IJobEntity, and Utility methods</remarks>
        public static T GetSingleton<T>()
            where T : unmanaged, IComponentData =>  throw Internal.InternalCompilerInterface.ThrowCodeGenException();

        /// <summary>
        /// Gets the value of a singleton component, and returns whether or not a singleton component of the specified type exists in the <see cref="World"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="IComponentData"/> subtype of the singleton component.
        /// This component type must not implement <see cref="IEnableableComponent"/></typeparam>
        /// <param name="value">The component. if an <see cref="Entity"/> with the specified type does not exist in the <see cref="World"/>, this is assigned a default value</param>
        /// <returns>True, if exactly one <see cref="Entity"/> exists in the <see cref="World"/> with the provided component type.</returns>
        /// <remarks> Not working in Entities.ForEach, IJobEntity, and Utility methods</remarks>
        public static bool TryGetSingleton<T>(out T value)
            where T : unmanaged, IComponentData =>  throw Internal.InternalCompilerInterface.ThrowCodeGenException();

        /// <summary>
        /// Gets a reference to the singleton component, for read/write access.
        /// </summary>
        /// <remarks>
        /// NOTE: this reference refers directly to the singleton's component memory.
        /// Structural changes to the chunk where the singleton resides can invalidate this reference
        /// and result in crashes or undefined behaviour if the reference is used after structural changes.
        ///
        /// This method differs from <see cref="GetSingleton{T}"/> in the following ways:
        ///
        /// The query that it generates requests read/write access to <typeparamref name="T"/>, which registers the system as a
        /// writer of that component, so systems that read that component and update later wait for this system's jobs.
        /// <see cref="GetSingleton{T}"/> generates a read-only query, which registers the system as a reader instead.
        ///
        /// It throws an exception if a job that reads or writes <typeparamref name="T"/> is still running, whereas
        /// <see cref="GetSingleton{T}"/> throws only if a job that writes <typeparamref name="T"/> is still running. Neither
        /// method completes the conflicting job for you, and both run this check only if safety checks are enabled. To complete
        /// the job, call <see cref="EntityManager.CompleteDependencyBeforeRW{T}"/> first, or restructure the data dependencies.
        ///
        /// It increments the change version of <typeparamref name="T"/>, even if you write the same value, so queries that filter
        /// on changes to that component match the singleton entity. <see cref="GetSingleton{T}"/> leaves the change version
        /// unchanged.
        /// </remarks>
        /// <typeparam name="T">The <see cref="IComponentData"/> subtype of the singleton component.
        /// This component type must not implement <see cref="IEnableableComponent"/></typeparam>
        /// <returns>The component.</returns>
        /// <exception cref="InvalidOperationException">Thrown if <typeparamref name="T"/> is a zero-sized component or
        /// implements <see cref="IEnableableComponent"/>, and if safety checks are enabled.</exception>
        /// <seealso cref="ComponentSystemBase.GetSingleton{T}"/>
        /// <seealso cref="EntityQuery.GetSingleton{T}"/>
        /// <seealso cref="EntityQuery.GetSingletonRW{T}"/>
        /// <remarks> Not working in Entities.ForEach, IJobEntity, and Utility methods</remarks>
        public static RefRW<T> GetSingletonRW<T>() where T : unmanaged, IComponentData =>   throw Internal.InternalCompilerInterface.ThrowCodeGenException();

        /// <summary>
        /// Gets the value of a singleton component, and returns whether or not a singleton component of the specified type exists in the <see cref="World"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="IComponentData"/> subtype of the singleton component.
        /// This component type must not implement <see cref="IEnableableComponent"/></typeparam>
        /// <param name="value">The reference to the component. if an <see cref="Entity"/> with the specified type does not exist in the <see cref="World"/>, this is assigned a default value</param>
        /// <returns>True, if exactly one <see cref="Entity"/> exists in the <see cref="World"/> with the provided component type.</returns>
        /// <remarks> Not working in Entities.ForEach, IJobEntity, and Utility methods</remarks>
        public static bool TryGetSingletonRW<T>(out RefRW<T> value)
            where T : unmanaged, IComponentData =>  throw Internal.InternalCompilerInterface.ThrowCodeGenException();

        /// <summary>
        /// Sets the value of a singleton component.
        /// </summary>
        /// <param name="value">A component containing the value to assign to the singleton.</param>
        /// <typeparam name="T">The <see cref="IComponentData"/> subtype of the singleton component.
        /// This component type must not implement <see cref="IEnableableComponent"/></typeparam>
        /// <seealso cref="EntityQuery.SetSingleton{T}"/>
        /// <remarks> Not working in Entities.ForEach, IJobEntity, and Utility methods</remarks>
        public static void SetSingleton<T>(T value)
            where T : unmanaged, IComponentData =>  throw Internal.InternalCompilerInterface.ThrowCodeGenException();

        /// <summary>
        /// Gets the Entity instance for a singleton.
        /// </summary>
        /// <typeparam name="T">The Type of the singleton component.
        /// This component type must not implement <see cref="IEnableableComponent"/></typeparam>
        /// <returns>The entity associated with the specified singleton component.</returns>
        /// <seealso cref="EntityQuery.GetSingletonEntity"/>
        /// <remarks> Not working in Entities.ForEach, IJobEntity, and Utility methods</remarks>
        public static Entity GetSingletonEntity<T>() where T : unmanaged =>  throw Internal.InternalCompilerInterface.ThrowCodeGenException();

        /// <summary>
        /// Gets the singleton Entity, and returns whether or not a singleton <see cref="Entity"/> of the specified type exists in the <see cref="World"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="IComponentData"/> subtype of the singleton component.
        /// This component type must not implement <see cref="IEnableableComponent"/></typeparam>
        /// <param name="value">The <see cref="Entity"/> associated with the specified singleton component.
        ///  If a singleton of the specified types does not exist in the current <see cref="World"/>, this is set to Entity.Null</param>
        /// <returns>True, if exactly one <see cref="Entity"/> exists in the <see cref="World"/> with the provided component type.</returns>
        /// <remarks> Not working in Entities.ForEach, IJobEntity, and Utility methods</remarks>
        public static bool TryGetSingletonEntity<T>(out Entity value) where T : unmanaged =>  throw Internal.InternalCompilerInterface.ThrowCodeGenException();

        /// <summary>
        /// Gets the value of a singleton buffer component.
        /// </summary>
        /// <typeparam name="T">The <see cref="IBufferElementData"/> subtype of the singleton component buffer element.
        /// This component type must not implement <see cref="IEnableableComponent"/></typeparam>
        /// <param name="isReadOnly">Whether the buffer data is only read or is also written. Access data in
        /// a read-only fashion whenever possible.</param>
        /// <returns>The buffer.</returns>
        /// <seealso cref="EntityQuery.GetSingleton{T}"/>
        /// <remarks> Not working in Entities.ForEach, IJobEntity, and Utility methods</remarks>
        public static DynamicBuffer<T> GetSingletonBuffer<T>(bool isReadOnly = false)
            where T : unmanaged, IBufferElementData =>  throw Internal.InternalCompilerInterface.ThrowCodeGenException();

        /// <summary>
        /// Gets the value of a singleton buffer component, and returns whether or not a singleton buffer component of the specified type exists in the <see cref="World"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="IBufferElementData"/> subtype of the singleton buffer component.
        /// This component type must not implement <see cref="IEnableableComponent"/></typeparam>
        /// <param name="value">The buffer. If an <see cref="Entity"/> with the specified type doesn't exist in the <see cref="World"/>, this is assigned a default value</param>
        /// <param name="isReadOnly">Whether the buffer data is read-only or not. Set to false by default.</param>
        /// <returns>True, if exactly one <see cref="Entity"/> exists in the <see cref="World"/> with the provided component type.</returns>
        /// <remarks> Not working in Entities.ForEach, IJobEntity, and Utility methods</remarks>
        public static bool TryGetSingletonBuffer<T>(out DynamicBuffer<T> value, bool isReadOnly = false)
            where T : unmanaged, IBufferElementData =>  throw Internal.InternalCompilerInterface.ThrowCodeGenException();

        /// <summary>
        /// Checks whether a singleton component of the specified type exists.
        /// </summary>
        /// <typeparam name="T">The <see cref="IComponentData"/> subtype of the singleton component.
        /// This component type must not implement <see cref="IEnableableComponent"/></typeparam>
        /// <returns>True, if a singleton of the specified type exists in the current <see cref="World"/>.</returns>
        /// <remarks>
        /// The <see cref="SystemAPI"/> singleton methods build their query with the default <see cref="EntityQueryOptions"/>
        /// flags, which exclude entities that have the <see cref="Disabled"/> component and entities that have the
        /// <see cref="Prefab"/> component. So if the only entity that has component <typeparamref name="T"/> is disabled or is
        /// an entity prefab, this method returns false, and <see cref="GetSingleton{T}"/> throws an exception.
        ///
        /// To find a singleton component on a disabled entity or on an entity prefab, create an <see cref="EntityQuery"/> with
        /// the <see cref="EntityQueryOptions.IncludeDisabledEntities"/> or <see cref="EntityQueryOptions.IncludePrefab"/> option,
        /// then call <see cref="EntityQuery.HasSingleton{T}"/> on that query.
        /// </remarks>
        /// <exception cref="InvalidOperationException">Thrown if <typeparamref name="T"/> implements
        /// <see cref="IEnableableComponent"/> and if safety checks are enabled.</exception>
        /// <exception cref="InvalidOperationException">Thrown if more than one entity has component
        /// <typeparamref name="T"/> and if safety checks are enabled.</exception>
        /// <remarks> Not working in Entities.ForEach, IJobEntity, and Utility methods</remarks>
        public static bool HasSingleton<T>() where T : unmanaged =>  throw Internal.InternalCompilerInterface.ThrowCodeGenException();
        #endregion

        #region Handles
        /////////////////////////////////// TypeHandle Caching ///////////////////////////////////

        /// <summary>
        /// Gets the run-time type information required to access the array of <see cref="Entity"/> objects in a chunk.
        /// </summary>
        /// <returns>An object representing the type information required to safely access Entity instances stored in a
        /// chunk.</returns>
        /// <remarks> Not working in Entities.ForEach, IJobEntity, and Utility methods</remarks>
        public static EntityTypeHandle GetEntityTypeHandle() =>  throw Internal.InternalCompilerInterface.ThrowCodeGenException();

        /// <summary>
        /// Gets the run-time type information required to access an array of component data in a chunk.
        /// </summary>
        /// <param name="isReadOnly">Whether the component data is only read, not written. Access components as
        /// read-only whenever possible.</param>
        /// <typeparam name="T">A struct that implements <see cref="IComponentData"/>.</typeparam>
        /// <returns>An object representing the type information required to safely access component data stored in a
        /// chunk.</returns>
        /// <remarks>Pass an <see cref="ComponentTypeHandle{T}"/> instance to a job that has access to chunk data,
        /// such as an <see cref="IJobChunk"/> job, to access that type of component inside the job.</remarks>
        /// <remarks> Not working in Entities.ForEach, IJobEntity, and Utility methods</remarks>
        public static ComponentTypeHandle<T> GetComponentTypeHandle<T>(bool isReadOnly = false) where T : unmanaged, IComponentData =>  throw Internal.InternalCompilerInterface.ThrowCodeGenException();

        /// <summary>
        /// Gets the run-time type information required to access an array of buffer components in a chunk.
        /// </summary>
        /// <param name="isReadOnly">Whether the data is only read, not written. Access data as
        /// read-only whenever possible.</param>
        /// <typeparam name="T">A struct that implements <see cref="IBufferElementData"/>.</typeparam>
        /// <returns>An object representing the type information required to safely access buffer components stored in a
        /// chunk.</returns>
        /// <remarks>Pass a BufferTypeHandle instance to a job that has access to chunk data, such as an
        /// <see cref="IJobChunk"/> job, to access that type of buffer component inside the job.</remarks>
        /// <remarks> Not working in Entities.ForEach, IJobEntity, and Utility methods</remarks>
        public static BufferTypeHandle<T> GetBufferTypeHandle<T>(bool isReadOnly = false) where T : unmanaged, IBufferElementData =>  throw Internal.InternalCompilerInterface.ThrowCodeGenException();

        /// <summary>
        /// Gets the run-time type information required to access a shared component data in a chunk.
        /// </summary>
        /// <typeparam name="T">A struct that implements <see cref="ISharedComponentData"/>.</typeparam>
        /// <returns>An object representing the type information required to safely access shared component data stored in a
        /// chunk.</returns>
        /// <remarks> Not working in Entities.ForEach, IJobEntity, and Utility methods</remarks>
        public static SharedComponentTypeHandle<T> GetSharedComponentTypeHandle<T>() where T : unmanaged, ISharedComponentData =>  throw Internal.InternalCompilerInterface.ThrowCodeGenException();
        #endregion
    }
}
