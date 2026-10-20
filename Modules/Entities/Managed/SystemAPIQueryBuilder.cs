// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Entities.Internal;

namespace Unity.Entities
{
    /// <summary>
    /// Supports construction of queries matching one or multiple archetypes inside `ISystem` and `SystemBase` types.
    /// All queried components must be known at compile-time.
    /// </summary>
    public struct SystemAPIQueryBuilder
    {
        /// <summary>
        /// Add an absent [Chunk Component](xref:components-chunk) type to the query.
        /// </summary>
        /// <remarks>
        /// Call this method on the query builder to exclude any entities that have the specified chunk component.
        /// Chunk components are a distinct component type, which are different from excluding the same type as a
        /// standard component.
        ///
        /// To add additional excluded Chunk Components, call this method multiple times.
        /// </remarks>
        /// <typeparam name="T1">Component type to use as an absent Chunk Component</typeparam>
        /// <returns>This query builder object, to allow chaining multiple method calls.</returns>
        /// <exception cref="ThrowNotBuildException"></exception>
        public SystemAPIQueryBuilder WithAbsentChunkComponent<T1>() => throw ThrowNotBuildException();

        /// <summary>
        /// Specify all read-only component types that must NOT be present.
        /// </summary>
        /// <typeparam name="T1">Absent component</typeparam>
        /// <returns>This query builder object, to allow chaining multiple method calls.</returns>
        /// <exception cref="ThrowNotBuildException"></exception>
        public SystemAPIQueryBuilder WithAbsent<T1>() => throw ThrowNotBuildException();

        /// <summary>
        /// Specify all read-only component types that must NOT be present.
        /// </summary>
        /// <typeparam name="T1">Absent component</typeparam>
        /// <typeparam name="T2">Absent component</typeparam>
        /// <returns>This query builder object, to allow chaining multiple method calls.</returns>
        /// <exception cref="ThrowNotBuildException"></exception>
        public SystemAPIQueryBuilder WithAbsent<T1, T2>() => throw ThrowNotBuildException();

        /// <summary>
        /// Specify all read-only component types that must NOT be present.
        /// </summary>
        /// <typeparam name="T1">Absent component</typeparam>
        /// <typeparam name="T2">Absent component</typeparam>
        /// <typeparam name="T3">Absent component</typeparam>
        /// <returns>This query builder object, to allow chaining multiple method calls.</returns>
        /// <exception cref="ThrowNotBuildException"></exception>
        public SystemAPIQueryBuilder WithAbsent<T1, T2, T3>() => throw ThrowNotBuildException();

        /// <summary>
        /// Specify all read-only component types that must NOT be present.
        /// </summary>
        /// <typeparam name="T1">Absent component</typeparam>
        /// <typeparam name="T2">Absent component</typeparam>
        /// <typeparam name="T3">Absent component</typeparam>
        /// <typeparam name="T4">Absent component</typeparam>
        /// <returns>This query builder object, to allow chaining multiple method calls.</returns>
        /// <exception cref="ThrowNotBuildException"></exception>
        public SystemAPIQueryBuilder WithAbsent<T1, T2, T3, T4>() => throw ThrowNotBuildException();

        /// <summary>
        /// Specify all read-only component types that must NOT be present.
        /// </summary>
        /// <typeparam name="T1">Absent component</typeparam>
        /// <typeparam name="T2">Absent component</typeparam>
        /// <typeparam name="T3">Absent component</typeparam>
        /// <typeparam name="T4">Absent component</typeparam>
        /// <typeparam name="T5">Absent component</typeparam>
        /// <returns>This query builder object, to allow chaining multiple method calls.</returns>
        /// <exception cref="ThrowNotBuildException"></exception>
        public SystemAPIQueryBuilder WithAbsent<T1, T2, T3, T4, T5>() => throw ThrowNotBuildException();

        /// <summary>
        /// Specify all read-only component types that must NOT be present.
        /// </summary>
        /// <typeparam name="T1">Absent component</typeparam>
        /// <typeparam name="T2">Absent component</typeparam>
        /// <typeparam name="T3">Absent component</typeparam>
        /// <typeparam name="T4">Absent component</typeparam>
        /// <typeparam name="T5">Absent component</typeparam>
        /// <typeparam name="T6">Absent component</typeparam>
        /// <returns>This query builder object, to allow chaining multiple method calls.</returns>
        /// <exception cref="ThrowNotBuildException"></exception>
        public SystemAPIQueryBuilder WithAbsent<T1, T2, T3, T4, T5, T6>() => throw ThrowNotBuildException();

        /// <summary>
        /// Specify all read-only component types that must NOT be present.
        /// </summary>
        /// <typeparam name="T1">Absent component</typeparam>
        /// <typeparam name="T2">Absent component</typeparam>
        /// <typeparam name="T3">Absent component</typeparam>
        /// <typeparam name="T4">Absent component</typeparam>
        /// <typeparam name="T5">Absent component</typeparam>
        /// <typeparam name="T6">Absent component</typeparam>
        /// <typeparam name="T7">Absent component</typeparam>
        /// <returns>This query builder object, to allow chaining multiple method calls.</returns>
        /// <exception cref="ThrowNotBuildException"></exception>
        public SystemAPIQueryBuilder WithAbsent<T1, T2, T3, T4, T5, T6, T7>() => throw ThrowNotBuildException();

        /// <summary>
        /// Specify all read-only DISABLED component types that must be present.
        /// </summary>
        /// <typeparam name="T1">Mandatory disabled component</typeparam>
        /// <returns>This query builder object, to allow chaining multiple method calls.</returns>
        /// <exception cref="ThrowNotBuildException"></exception>
        public SystemAPIQueryBuilder WithDisabled<T1>() => throw ThrowNotBuildException();

        /// <summary>
        /// Specify all read-only DISABLED component types that must be present.
        /// </summary>
        /// <typeparam name="T1">Mandatory disabled component</typeparam>
        /// <typeparam name="T2">Mandatory disabled component</typeparam>
        /// <returns>This query builder object, to allow chaining multiple method calls.</returns>
        /// <exception cref="ThrowNotBuildException"></exception>
        public SystemAPIQueryBuilder WithDisabled<T1, T2>() => throw ThrowNotBuildException();

        /// <summary>
        /// Specify all read-only DISABLED component types that must be present.
        /// </summary>
        /// <typeparam name="T1">Mandatory disabled component</typeparam>
        /// <typeparam name="T2">Mandatory disabled component</typeparam>
        /// <typeparam name="T3">Mandatory disabled component</typeparam>
        /// <returns>This query builder object, to allow chaining multiple method calls.</returns>
        /// <exception cref="ThrowNotBuildException"></exception>
        public SystemAPIQueryBuilder WithDisabled<T1, T2, T3>() => throw ThrowNotBuildException();

        /// <summary>
        /// Specify all read-only DISABLED component types that must be present.
        /// </summary>
        /// <typeparam name="T1">Mandatory disabled component</typeparam>
        /// <typeparam name="T2">Mandatory disabled component</typeparam>
        /// <typeparam name="T3">Mandatory disabled component</typeparam>
        /// <typeparam name="T4">Mandatory disabled component</typeparam>
        /// <returns>This query builder object, to allow chaining multiple method calls.</returns>
        /// <exception cref="ThrowNotBuildException"></exception>
        public SystemAPIQueryBuilder WithDisabled<T1, T2, T3, T4>() => throw ThrowNotBuildException();

        /// <summary>
        /// Specify all read-only DISABLED component types that must be present.
        /// </summary>
        /// <typeparam name="T1">Mandatory disabled component</typeparam>
        /// <typeparam name="T2">Mandatory disabled component</typeparam>
        /// <typeparam name="T3">Mandatory disabled component</typeparam>
        /// <typeparam name="T4">Mandatory disabled component</typeparam>
        /// <typeparam name="T5">Mandatory disabled component</typeparam>
        /// <returns>This query builder object, to allow chaining multiple method calls.</returns>
        /// <exception cref="ThrowNotBuildException"></exception>
        public SystemAPIQueryBuilder WithDisabled<T1, T2, T3, T4, T5>() => throw ThrowNotBuildException();

        /// <summary>
        /// Specify all read-only DISABLED component types that must be present.
        /// </summary>
        /// <typeparam name="T1">Mandatory disabled component</typeparam>
        /// <typeparam name="T2">Mandatory disabled component</typeparam>
        /// <typeparam name="T3">Mandatory disabled component</typeparam>
        /// <typeparam name="T4">Mandatory disabled component</typeparam>
        /// <typeparam name="T5">Mandatory disabled component</typeparam>
        /// <typeparam name="T6">Mandatory disabled component</typeparam>
        /// <returns>This query builder object, to allow chaining multiple method calls.</returns>
        /// <exception cref="ThrowNotBuildException"></exception>
        public SystemAPIQueryBuilder WithDisabled<T1, T2, T3, T4, T5, T6>() => throw ThrowNotBuildException();

        /// <summary>
        /// Specify all read-only DISABLED component types that must be present.
        /// </summary>
        /// <typeparam name="T1">Mandatory disabled component</typeparam>
        /// <typeparam name="T2">Mandatory disabled component</typeparam>
        /// <typeparam name="T3">Mandatory disabled component</typeparam>
        /// <typeparam name="T4">Mandatory disabled component</typeparam>
        /// <typeparam name="T5">Mandatory disabled component</typeparam>
        /// <typeparam name="T6">Mandatory disabled component</typeparam>
        /// <typeparam name="T7">Mandatory disabled component</typeparam>
        /// <returns>This query builder object, to allow chaining multiple method calls.</returns>
        /// <exception cref="ThrowNotBuildException"></exception>
        public SystemAPIQueryBuilder WithDisabled<T1, T2, T3, T4, T5, T6, T7>() => throw ThrowNotBuildException();

        /// <summary>
        /// Specify all DISABLED component types (with write access) that must be present.
        /// </summary>
        /// <typeparam name="T1">Mandatory disabled component</typeparam>
        /// <returns>This query builder object, to allow chaining multiple method calls.</returns>
        /// <exception cref="ThrowNotBuildException"></exception>
        public SystemAPIQueryBuilder WithDisabledRW<T1>() => throw ThrowNotBuildException();

        /// <summary>
        /// Specify all DISABLED component types (with write access) that must be present.
        /// </summary>
        /// <typeparam name="T1">Mandatory disabled component</typeparam>
        /// <typeparam name="T2">Mandatory disabled component</typeparam>
        /// <returns>This query builder object, to allow chaining multiple method calls.</returns>
        /// <exception cref="ThrowNotBuildException"></exception>
        public SystemAPIQueryBuilder WithDisabledRW<T1, T2>() => throw ThrowNotBuildException();

        /// <summary>
        /// Specify all read-only component types that must be present and enabled on matching entities.
        /// </summary>
        /// <typeparam name="T1">Mandatory component</typeparam>
        /// <returns>This query builder object, to allow chaining multiple method calls.</returns>
        /// <exception cref="ThrowNotBuildException"></exception>
        public SystemAPIQueryBuilder WithAll<T1>() => throw ThrowNotBuildException();

        /// <summary>
        /// Specify all read-only component types that must be present and enabled on matching entities.
        /// </summary>
        /// <typeparam name="T1">Mandatory component</typeparam>
        /// <typeparam name="T2">Mandatory component</typeparam>
        /// <returns>This query builder object, to allow chaining multiple method calls.</returns>
        /// <exception cref="ThrowNotBuildException"></exception>
        public SystemAPIQueryBuilder WithAll<T1, T2>() => throw ThrowNotBuildException();

        /// <summary>
        /// Specify all read-only component types that must be present and enabled on matching entities.
        /// </summary>
        /// <typeparam name="T1">Mandatory component</typeparam>
        /// <typeparam name="T2">Mandatory component</typeparam>
        /// <typeparam name="T3">Mandatory component</typeparam>
        /// <returns>This query builder object, to allow chaining multiple method calls.</returns>
        /// <exception cref="ThrowNotBuildException"></exception>
        public SystemAPIQueryBuilder WithAll<T1, T2, T3>() => throw ThrowNotBuildException();

        /// <summary>
        /// Specify all read-only component types that must be present and enabled on matching entities.
        /// </summary>
        /// <typeparam name="T1">Mandatory component</typeparam>
        /// <typeparam name="T2">Mandatory component</typeparam>
        /// <typeparam name="T3">Mandatory component</typeparam>
        /// <typeparam name="T4">Mandatory component</typeparam>
        /// <returns>This query builder object, to allow chaining multiple method calls.</returns>
        /// <exception cref="ThrowNotBuildException"></exception>
        public SystemAPIQueryBuilder WithAll<T1, T2, T3, T4>() => throw ThrowNotBuildException();

        /// <summary>
        /// Specify all read-only component types that must be present and enabled on matching entities.
        /// </summary>
        /// <typeparam name="T1">Mandatory component</typeparam>
        /// <typeparam name="T2">Mandatory component</typeparam>
        /// <typeparam name="T3">Mandatory component</typeparam>
        /// <typeparam name="T4">Mandatory component</typeparam>
        /// <typeparam name="T5">Mandatory component</typeparam>
        /// <returns>This query builder object, to allow chaining multiple method calls.</returns>
        /// <exception cref="ThrowNotBuildException"></exception>
        public SystemAPIQueryBuilder WithAll<T1, T2, T3, T4, T5>() => throw ThrowNotBuildException();

        /// <summary>
        /// Specify all read-only component types that must be present and enabled on matching entities.
        /// </summary>
        /// <typeparam name="T1">Mandatory component</typeparam>
        /// <typeparam name="T2">Mandatory component</typeparam>
        /// <typeparam name="T3">Mandatory component</typeparam>
        /// <typeparam name="T4">Mandatory component</typeparam>
        /// <typeparam name="T5">Mandatory component</typeparam>
        /// <typeparam name="T6">Mandatory component</typeparam>
        /// <returns>This query builder object, to allow chaining multiple method calls.</returns>
        /// <exception cref="ThrowNotBuildException"></exception>
        public SystemAPIQueryBuilder WithAll<T1, T2, T3, T4, T5, T6>() => throw ThrowNotBuildException();

        /// <summary>
        /// Specify all read-only component types that must be present and enabled on matching entities.
        /// </summary>
        /// <typeparam name="T1">Mandatory component</typeparam>
        /// <typeparam name="T2">Mandatory component</typeparam>
        /// <typeparam name="T3">Mandatory component</typeparam>
        /// <typeparam name="T4">Mandatory component</typeparam>
        /// <typeparam name="T5">Mandatory component</typeparam>
        /// <typeparam name="T6">Mandatory component</typeparam>
        /// <typeparam name="T7">Mandatory component</typeparam>
        /// <returns>This query builder object, to allow chaining multiple method calls.</returns>
        /// <exception cref="ThrowNotBuildException"></exception>
        public SystemAPIQueryBuilder WithAll<T1, T2, T3, T4, T5, T6, T7>() => throw ThrowNotBuildException();

        /// <summary>
        /// Specify all read-write component types that must be present and enabled on matching entities.
        /// </summary>
        /// <typeparam name="T1">Mandatory component</typeparam>
        /// <returns>This query builder object, to allow chaining multiple method calls.</returns>
        /// <exception cref="ThrowNotBuildException"></exception>
        public SystemAPIQueryBuilder WithAllRW<T1>() => throw ThrowNotBuildException();

        /// <summary>
        /// Specify all read-write component types that must be present and enabled on matching entities.
        /// </summary>
        /// <typeparam name="T1">Mandatory component</typeparam>
        /// <typeparam name="T2">Mandatory component</typeparam>
        /// <returns>This query builder object, to allow chaining multiple method calls.</returns>
        /// <exception cref="ThrowNotBuildException"></exception>
        public SystemAPIQueryBuilder WithAllRW<T1, T2>() => throw ThrowNotBuildException();

        /// <summary>
        /// Add a required [Chunk Component](xref:components-chunk) type to the query, which must be enabled.
        /// </summary>
        /// <remarks>
        /// Call this method on the query builder to find entities that have all the specified chunk components. Chunk
        /// components are a distinct component type, which are different from adding the same type as a standard
        /// component.
        ///
        /// To add additional required Chunk Components, call this method multiple times.
        /// </remarks>
        ///
        /// <typeparam name="T">Component type to use as a required, read-only Chunk Component</typeparam>
        /// <returns>The builder object that invoked this method.</returns>
        public SystemAPIQueryBuilder WithAllChunkComponent<T>() => throw ThrowNotBuildException();

        /// <summary>
        /// Add a required [Chunk Component](xref:components-chunk) type to the query, which must be enabled.
        /// </summary>
        /// <remarks>
        /// Call this method on the query builder to find entities that have all the specified chunk components. Chunk
        /// components are a distinct component type, which are different from adding the same type as a standard
        /// component.
        ///
        /// To add additional required Chunk Components, call this method multiple times.
        /// </remarks>
        ///
        /// <typeparam name="T">Component type to use as a required, read-write Chunk Component</typeparam>
        /// <returns>The builder object that invoked this method.</returns>
        public SystemAPIQueryBuilder WithAllChunkComponentRW<T>() => throw ThrowNotBuildException();

        /// <summary>
        /// Specify optional read-only component types that must be present.
        /// </summary>
        /// <typeparam name="T1">Optional component</typeparam>
        /// <returns>This query builder object, to allow chaining multiple method calls.</returns>
        /// <exception cref="ThrowNotBuildException"></exception>
        public SystemAPIQueryBuilder WithAny<T1>() => throw ThrowNotBuildException();

        /// <summary>
        /// Specify optional read-only component types that must be present.
        /// </summary>
        /// <typeparam name="T1">Optional component</typeparam>
        /// <typeparam name="T2">Optional component</typeparam>
        /// <returns>This query builder object, to allow chaining multiple method calls.</returns>
        /// <exception cref="ThrowNotBuildException"></exception>
        public SystemAPIQueryBuilder WithAny<T1, T2>() => throw ThrowNotBuildException();

        /// <summary>
        /// Specify optional read-only component types that must be present.
        /// </summary>
        /// <typeparam name="T1">Optional component</typeparam>
        /// <typeparam name="T2">Optional component</typeparam>
        /// <typeparam name="T3">Optional component</typeparam>
        /// <returns>This query builder object, to allow chaining multiple method calls.</returns>
        /// <exception cref="ThrowNotBuildException"></exception>
        public SystemAPIQueryBuilder WithAny<T1, T2, T3>() => throw ThrowNotBuildException();

        /// <summary>
        /// Specify optional read-only component types that must be present.
        /// </summary>
        /// <typeparam name="T1">Optional component</typeparam>
        /// <typeparam name="T2">Optional component</typeparam>
        /// <typeparam name="T3">Optional component</typeparam>
        /// <typeparam name="T4">Optional component</typeparam>
        /// <returns>This query builder object, to allow chaining multiple method calls.</returns>
        /// <exception cref="ThrowNotBuildException"></exception>
        public SystemAPIQueryBuilder WithAny<T1, T2, T3, T4>() => throw ThrowNotBuildException();

        /// <summary>
        /// Specify optional read-only component types that must be present.
        /// </summary>
        /// <typeparam name="T1">Optional component</typeparam>
        /// <typeparam name="T2">Optional component</typeparam>
        /// <typeparam name="T3">Optional component</typeparam>
        /// <typeparam name="T4">Optional component</typeparam>
        /// <typeparam name="T5">Optional component</typeparam>
        /// <returns>This query builder object, to allow chaining multiple method calls.</returns>
        /// <exception cref="ThrowNotBuildException"></exception>
        public SystemAPIQueryBuilder WithAny<T1, T2, T3, T4, T5>() => throw ThrowNotBuildException();

        /// <summary>
        /// Specify optional read-only component types that must be present.
        /// </summary>
        /// <typeparam name="T1">Optional component</typeparam>
        /// <typeparam name="T2">Optional component</typeparam>
        /// <typeparam name="T3">Optional component</typeparam>
        /// <typeparam name="T4">Optional component</typeparam>
        /// <typeparam name="T5">Optional component</typeparam>
        /// <typeparam name="T6">Optional component</typeparam>
        /// <returns>This query builder object, to allow chaining multiple method calls.</returns>
        /// <exception cref="ThrowNotBuildException"></exception>
        public SystemAPIQueryBuilder WithAny<T1, T2, T3, T4, T5, T6>() => throw ThrowNotBuildException();

        /// <summary>
        /// Specify optional read-only component types that must be present.
        /// </summary>
        /// <typeparam name="T1">Optional component</typeparam>
        /// <typeparam name="T2">Optional component</typeparam>
        /// <typeparam name="T3">Optional component</typeparam>
        /// <typeparam name="T4">Optional component</typeparam>
        /// <typeparam name="T5">Optional component</typeparam>
        /// <typeparam name="T6">Optional component</typeparam>
        /// <typeparam name="T7">Optional component</typeparam>
        /// <returns>This query builder object, to allow chaining multiple method calls.</returns>
        /// <exception cref="ThrowNotBuildException"></exception>
        public SystemAPIQueryBuilder WithAny<T1, T2, T3, T4, T5, T6, T7>() => throw ThrowNotBuildException();

        /// <summary>
        /// Specify optional read-write component types that must be present.
        /// </summary>
        /// <typeparam name="T1">Optional component</typeparam>
        /// <returns>This query builder object, to allow chaining multiple method calls.</returns>
        /// <exception cref="ThrowNotBuildException"></exception>
        public SystemAPIQueryBuilder WithAnyRW<T1>() => throw ThrowNotBuildException();

        /// <summary>
        /// Specify optional read-write component types that must be present.
        /// </summary>
        /// <typeparam name="T1">Optional component</typeparam>
        /// <typeparam name="T2">Optional component</typeparam>
        /// <returns>This query builder object, to allow chaining multiple method calls.</returns>
        /// <exception cref="ThrowNotBuildException"></exception>
        public SystemAPIQueryBuilder WithAnyRW<T1, T2>() => throw ThrowNotBuildException();

        /// <summary>
        /// Add an optional [Chunk Component](xref:components-chunk) type to the query.
        /// </summary>
        /// <remarks>
        /// To match the resulting query, an Entity must have at least one of the query's optional component types,
        /// specified using either <see cref="WithAny"/> or <see cref="WithAnyChunkComponent"/>. Chunk components are a distinct component
        /// type, which are different from adding the same type as a standard component.
        ///
        /// Compare this to <see cref="M:Unity.Entities.SystemAPIEntityQueryBuilder.WithAllChunkComponent``1"/>
        ///
        /// To add additional optional Chunk Components, call this method multiple times.
        ///
        /// </remarks>
        /// <typeparam name="T">Component type to use as an optional, read-only Chunk Component</typeparam>
        /// <returns>The builder object that invoked this method.</returns>
        public SystemAPIQueryBuilder WithAnyChunkComponent<T>() => throw ThrowNotBuildException();

        /// <summary>
        /// Add an optional [Chunk Component](xref:components-chunk) type to the query.
        /// </summary>
        /// <remarks>
        /// To match the resulting query, an Entity must have at least one of the query's optional component types,
        /// specified using either <see cref="WithAny"/> or <see cref="WithAnyChunkComponent"/>. Chunk components are a distinct component
        /// type, which are different from adding the same type as a standard component.
        ///
        /// Compare this to <see cref="M:Unity.Entities.SystemAPIEntityQueryBuilder.WithAllChunkComponent``1"/>
        ///
        /// To add additional optional Chunk Components, call this method multiple times.
        ///
        /// </remarks>
        /// <typeparam name="T">Component type to use as an optional, read-write Chunk Component</typeparam>
        /// <returns>The builder object that invoked this method.</returns>
        public SystemAPIQueryBuilder WithAnyChunkComponentRW<T>() => throw ThrowNotBuildException();

        /// <summary>
        /// Specify component types that must NOT be present.
        /// </summary>
        /// <typeparam name="T1">Absent component</typeparam>
        /// <returns>This query builder object, to allow chaining multiple method calls.</returns>
        /// <exception cref="ThrowNotBuildException"></exception>
        public SystemAPIQueryBuilder WithNone<T1>() => throw ThrowNotBuildException();

        /// <summary>
        /// Specify component types that must NOT be present.
        /// </summary>
        /// <typeparam name="T1">Absent component</typeparam>
        /// <typeparam name="T2">Absent component</typeparam>
        /// <returns>This query builder object, to allow chaining multiple method calls.</returns>
        /// <exception cref="ThrowNotBuildException"></exception>
        public SystemAPIQueryBuilder WithNone<T1, T2>() => throw ThrowNotBuildException();

        /// <summary>
        /// Specify component types that must NOT be present.
        /// </summary>
        /// <typeparam name="T1">Absent component</typeparam>
        /// <typeparam name="T2">Absent component</typeparam>
        /// <typeparam name="T3">Absent component</typeparam>
        /// <returns>This query builder object, to allow chaining multiple method calls.</returns>
        /// <exception cref="ThrowNotBuildException"></exception>
        public SystemAPIQueryBuilder WithNone<T1, T2, T3>() => throw ThrowNotBuildException();


        /// <summary>
        /// Specify component types that must NOT be present.
        /// </summary>
        /// <typeparam name="T1">Absent component</typeparam>
        /// <typeparam name="T2">Absent component</typeparam>
        /// <typeparam name="T3">Absent component</typeparam>
        /// <typeparam name="T4">Absent component</typeparam>
        /// <returns>This query builder object, to allow chaining multiple method calls.</returns>
        /// <exception cref="ThrowNotBuildException"></exception>
        public SystemAPIQueryBuilder WithNone<T1, T2, T3, T4>() => throw ThrowNotBuildException();

        /// <summary>
        /// Specify component types that must NOT be present.
        /// </summary>
        /// <typeparam name="T1">Absent component</typeparam>
        /// <typeparam name="T2">Absent component</typeparam>
        /// <typeparam name="T3">Absent component</typeparam>
        /// <typeparam name="T4">Absent component</typeparam>
        /// <typeparam name="T5">Absent component</typeparam>
        /// <returns>This query builder object, to allow chaining multiple method calls.</returns>
        /// <exception cref="ThrowNotBuildException"></exception>
        public SystemAPIQueryBuilder WithNone<T1, T2, T3, T4, T5>() => throw ThrowNotBuildException();

        /// <summary>
        /// Specify component types that must NOT be present.
        /// </summary>
        /// <typeparam name="T1">Absent component</typeparam>
        /// <typeparam name="T2">Absent component</typeparam>
        /// <typeparam name="T3">Absent component</typeparam>
        /// <typeparam name="T4">Absent component</typeparam>
        /// <typeparam name="T5">Absent component</typeparam>
        /// <typeparam name="T6">Absent component</typeparam>
        /// <returns>This query builder object, to allow chaining multiple method calls.</returns>
        /// <exception cref="ThrowNotBuildException"></exception>
        public SystemAPIQueryBuilder WithNone<T1, T2, T3, T4, T5, T6>() => throw ThrowNotBuildException();

        /// <summary>
        /// Specify component types that must NOT be present.
        /// </summary>
        /// <typeparam name="T1">Absent component</typeparam>
        /// <typeparam name="T2">Absent component</typeparam>
        /// <typeparam name="T3">Absent component</typeparam>
        /// <typeparam name="T4">Absent component</typeparam>
        /// <typeparam name="T5">Absent component</typeparam>
        /// <typeparam name="T6">Absent component</typeparam>
        /// <typeparam name="T7">Absent component</typeparam>
        /// <returns>This query builder object, to allow chaining multiple method calls.</returns>
        /// <exception cref="ThrowNotBuildException"></exception>
        public SystemAPIQueryBuilder WithNone<T1, T2, T3, T4, T5, T6, T7>() => throw ThrowNotBuildException();

        /// <summary>
        /// Add an excluded [Chunk Component](xref:components-chunk) type to the query.
        /// </summary>
        /// <remarks>
        /// Call this method on the query builder to exclude any entities that have the specified chunk component.
        /// Chunk components are a distinct component type, which are different from excluding the same type as a
        /// standard component.
        ///
        /// To add additional excluded Chunk Components, call this method multiple times.
        ///
        /// </remarks>
        /// <typeparam name="T">Component type to use as an excluded Chunk Component</typeparam>
        /// <returns>The builder object that invoked this method.</returns>
        public SystemAPIQueryBuilder WithNoneChunkComponent<T>() => throw ThrowNotBuildException();


        /// <summary>
        /// Specify all read-only component types that must be present, whether or not they are enabled on matching entities.
        /// </summary>
        /// <typeparam name="T1">Mandatory component</typeparam>
        /// <returns>This query builder object, to allow chaining multiple method calls.</returns>
        /// <exception cref="ThrowNotBuildException"></exception>
        public SystemAPIQueryBuilder WithPresent<T1>() => throw ThrowNotBuildException();

        /// <summary>
        /// Specify all read-only component types that must be present, whether or not they are enabled on matching entities.
        /// </summary>
        /// <typeparam name="T1">Mandatory component</typeparam>
        /// <typeparam name="T2">Mandatory component</typeparam>
        /// <returns>This query builder object, to allow chaining multiple method calls.</returns>
        /// <exception cref="ThrowNotBuildException"></exception>
        public SystemAPIQueryBuilder WithPresent<T1, T2>() => throw ThrowNotBuildException();

        /// <summary>
        /// Specify all read-only component types that must be present, whether or not they are enabled on matching entities.
        /// </summary>
        /// <typeparam name="T1">Mandatory component</typeparam>
        /// <typeparam name="T2">Mandatory component</typeparam>
        /// <typeparam name="T3">Mandatory component</typeparam>
        /// <returns>This query builder object, to allow chaining multiple method calls.</returns>
        /// <exception cref="ThrowNotBuildException"></exception>
        public SystemAPIQueryBuilder WithPresent<T1, T2, T3>() => throw ThrowNotBuildException();

        /// <summary>
        /// Specify all read-only component types that must be present, whether or not they are enabled on matching entities.
        /// </summary>
        /// <typeparam name="T1">Mandatory component</typeparam>
        /// <typeparam name="T2">Mandatory component</typeparam>
        /// <typeparam name="T3">Mandatory component</typeparam>
        /// <typeparam name="T4">Mandatory component</typeparam>
        /// <returns>This query builder object, to allow chaining multiple method calls.</returns>
        /// <exception cref="ThrowNotBuildException"></exception>
        public SystemAPIQueryBuilder WithPresent<T1, T2, T3, T4>() => throw ThrowNotBuildException();

        /// <summary>
        /// Specify all read-only component types that must be present, whether or not they are enabled on matching entities.
        /// </summary>
        /// <typeparam name="T1">Mandatory component</typeparam>
        /// <typeparam name="T2">Mandatory component</typeparam>
        /// <typeparam name="T3">Mandatory component</typeparam>
        /// <typeparam name="T4">Mandatory component</typeparam>
        /// <typeparam name="T5">Mandatory component</typeparam>
        /// <returns>This query builder object, to allow chaining multiple method calls.</returns>
        /// <exception cref="ThrowNotBuildException"></exception>
        public SystemAPIQueryBuilder WithPresent<T1, T2, T3, T4, T5>() => throw ThrowNotBuildException();

        /// <summary>
        /// Specify all read-only component types that must be present, whether or not they are enabled on matching entities.
        /// </summary>
        /// <typeparam name="T1">Mandatory component</typeparam>
        /// <typeparam name="T2">Mandatory component</typeparam>
        /// <typeparam name="T3">Mandatory component</typeparam>
        /// <typeparam name="T4">Mandatory component</typeparam>
        /// <typeparam name="T5">Mandatory component</typeparam>
        /// <typeparam name="T6">Mandatory component</typeparam>
        /// <returns>This query builder object, to allow chaining multiple method calls.</returns>
        /// <exception cref="ThrowNotBuildException"></exception>
        public SystemAPIQueryBuilder WithPresent<T1, T2, T3, T4, T5, T6>() => throw ThrowNotBuildException();

        /// <summary>
        /// Specify all read-only component types that must be present, whether or not they are enabled on matching entities.
        /// </summary>
        /// <typeparam name="T1">Mandatory component</typeparam>
        /// <typeparam name="T2">Mandatory component</typeparam>
        /// <typeparam name="T3">Mandatory component</typeparam>
        /// <typeparam name="T4">Mandatory component</typeparam>
        /// <typeparam name="T5">Mandatory component</typeparam>
        /// <typeparam name="T6">Mandatory component</typeparam>
        /// <typeparam name="T7">Mandatory component</typeparam>
        /// <returns>This query builder object, to allow chaining multiple method calls.</returns>
        /// <exception cref="ThrowNotBuildException"></exception>
        public SystemAPIQueryBuilder WithPresent<T1, T2, T3, T4, T5, T6, T7>() => throw ThrowNotBuildException();

        /// <summary>
        /// Specify all read-write component types that must be present, whether or not they are enabled on matching entities.
        /// </summary>
        /// <typeparam name="T1">Mandatory component</typeparam>
        /// <returns>This query builder object, to allow chaining multiple method calls.</returns>
        /// <exception cref="ThrowNotBuildException"></exception>
        public SystemAPIQueryBuilder WithPresentRW<T1>() => throw ThrowNotBuildException();

        /// <summary>
        /// Specify all read-write component types that must be present, whether or not they are enabled on matching entities.
        /// </summary>
        /// <typeparam name="T1">Mandatory component</typeparam>
        /// <typeparam name="T2">Mandatory component</typeparam>
        /// <returns>This query builder object, to allow chaining multiple method calls.</returns>
        /// <exception cref="ThrowNotBuildException"></exception>
        public SystemAPIQueryBuilder WithPresentRW<T1, T2>() => throw ThrowNotBuildException();

        /// <summary>
        /// Add a required [Chunk Component](xref:components-chunk) type to the query, whether or not it is enabled.
        /// </summary>
        /// <remarks>
        /// Call this method on the query builder to find entities that have all the specified chunk components. Chunk
        /// components are a distinct component type, which are different from adding the same type as a standard
        /// component.
        ///
        /// To add additional required Chunk Components, call this method multiple times.
        /// </remarks>
        ///
        /// <typeparam name="T">Component type to use as a required, read-only Chunk Component</typeparam>
        /// <returns>The builder object that invoked this method.</returns>
        public SystemAPIQueryBuilder WithPresentChunkComponent<T>() => throw ThrowNotBuildException();

        /// <summary>
        /// Add a required [Chunk Component](xref:components-chunk) type to the query, whether or not it is enabled.
        /// </summary>
        /// <remarks>
        /// Call this method on the query builder to find entities that have all the specified chunk components. Chunk
        /// components are a distinct component type, which are different from adding the same type as a standard
        /// component.
        ///
        /// To add additional required Chunk Components, call this method multiple times.
        /// </remarks>
        ///
        /// <typeparam name="T">Component type to use as a required, read-write Chunk Component</typeparam>
        /// <returns>The builder object that invoked this method.</returns>
        public SystemAPIQueryBuilder WithPresentChunkComponentRW<T>() => throw ThrowNotBuildException();

        /// <summary>
        /// Specify your own `EntityQueryOptions`.
        /// </summary>
        /// <remarks>
        /// This method may not be invoked more than once for each query description.
        /// Subsequent calls will override previous options, rather than adding to them. Use the bitwise OR operator '|'
        /// to combine multiple options.
        /// </remarks>
        /// <param name="options">The query options</param>
        /// <returns>This query builder object, to allow chaining multiple method calls.</returns>
        /// <exception cref="ThrowNotBuildException"></exception>
        public SystemAPIQueryBuilder WithOptions(EntityQueryOptions options) => throw ThrowNotBuildException();

        /// <summary>
        /// Finalize the existing query description. All `.WithXXX()` invocations chained after this method will create a new query description.
        /// </summary>
        /// <returns>This query builder object, to allow chaining multiple method calls.</returns>
        /// <exception cref="ThrowNotBuildException"></exception>
        public SystemAPIQueryBuilder AddAdditionalQuery() => throw ThrowNotBuildException();

        /// <summary>
        /// Get or create an `EntityQuery` matching the query description(s).
        /// </summary>
        /// <remarks>If an `EntityQuery` in the containing system's existing cache
        /// matches the defined query, it gets retrieved. Otherwise, a new `EntityQuery` is created and then added to the containing system's cache.
        /// </remarks>
        /// <returns>The query</returns>
        /// <exception cref="ThrowNotBuildException"></exception>
        public EntityQuery Build() => throw InternalCompilerInterface.ThrowCodeGenException();

        static InvalidOperationException ThrowNotBuildException() => throw new InvalidOperationException("QueryBuilder method chains must end with a Build() invocation to generate an EntityQuery. Please use e.g. QueryBuilder().WithAll<T>().Build() to generate a fully-formed EntityQuery.");
    }
}
