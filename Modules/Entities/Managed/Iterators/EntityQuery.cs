// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;

namespace Unity.Entities
{
    /// <summary>
    /// Exception type thrown if <see cref="EntityQueryDesc"/> validation fails.
    /// </summary>
    public class EntityQueryDescValidationException : Exception
    {
        /// <summary>
        /// Construct a new exception instance
        /// </summary>
        /// <param name="message">The exception message.</param>
        public EntityQueryDescValidationException(string message) : base(message)
        {
        }
    }

    /// <summary>
    /// Describes a query to find archetypes in terms of required, optional, and excluded
    /// components.
    /// </summary>
    /// <remarks>
    /// Define an EntityQueryDesc object to describe complex queries. Inside a system,
    /// pass an EntityQueryDesc object to <see cref="ComponentSystemBase.GetEntityQuery(EntityQueryDesc[])"/>
    /// to create the <see cref="EntityQuery"/>.
    ///
    /// A query description combines the component types you specify in `All`, `Any`, and `None` sets according to the
    /// following rules:
    ///
    /// * All - Includes archetypes that have every component in this set
    /// * Any - Includes archetypes that have at least one component in this set
    /// * None - Excludes archetypes that have any component in this set, but includes entities which have the component disabled.
    /// * Disabled - Includes archetypes that have every component in this set, but only matches entities where the component is disabled.
    /// * Absent - Excludes archetypes that have any component in this set.
    /// * Present - Includes archetypes that have every component in this set, whether or not the component is enabled.
    ///
    /// For example, given entities with the following components:
    ///
    /// * Player has components: ObjectPosition, ObjectRotation, Player
    /// * Enemy1 has components: ObjectPosition, ObjectRotation, Melee
    /// * Enemy2 has components: ObjectPosition, ObjectRotation, Ranger
    ///
    /// The query description below matches all of the archetypes that:
    /// have any of [Melee or Ranger], AND have none of [Player], AND have all of [ObjectPosition and ObjectRotation]
    ///
    /// <example>
    /// <code lang="csharp" source="../../DocCodeSamples.Tests/EntityQueryExamples.cs" region="query-description" title="Query Description"/>
    /// </example>
    ///
    /// In other words, the query created from this description selects the Enemy1 and Enemy2 entities, but not the Player entity.
    /// </remarks>
    public class EntityQueryDesc : IEquatable<EntityQueryDesc>
    {
        /// <summary>
        /// Include archetypes that contain at least one (but possibly more) of the
        /// component types in the Any list.
        /// </summary>
        public ComponentType[] Any = Array.Empty<ComponentType>();
        /// <summary>
        /// Include archetypes that do not contain these component types. For enableable component types, archetypes
        /// with these components will still be matched by the query, but only for entities with these components disabled.
        /// </summary>
        /// <remarks>Effectively, this list means "absent, or present (but disabled)".</remarks>
        public ComponentType[] None = Array.Empty<ComponentType>();
        /// <summary>
        /// Include archetypes that contain all of the
        /// component types in the All list.
        /// </summary>
        public ComponentType[] All = Array.Empty<ComponentType>();
        /// <summary>
        /// Include archetypes that contain these components, but only match entities where the component is disabled.
        /// </summary>
        public ComponentType[] Disabled = Array.Empty<ComponentType>();
        /// <summary>
        /// Exclude archetypes that contain these component types.
        /// </summary>
        public ComponentType[] Absent = Array.Empty<ComponentType>();
        /// <summary>
        /// Include archetypes that contain these component types, whether or not the component is enabled.
        /// </summary>
        public ComponentType[] Present = Array.Empty<ComponentType>();
        /// <summary>
        /// Specialized query options.
        /// </summary>
        /// <remarks>
        /// You should not need to set these options for most queries.
        ///
        /// Options is a bit mask; use the bitwise OR operator to combine multiple options.
        /// </remarks>
        public EntityQueryOptions Options = EntityQueryOptions.Default;

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        void AddComponentTypeIndicesToArray(ComponentType[] componentTypes, ref NativeArray<TypeIndex> allComponentTypeIds, ref int currentIndexInComponentTypeIdArray)
        {
            for (int i = 0; i < componentTypes.Length; i++)
            {
                var componentType = componentTypes[i];
                allComponentTypeIds[currentIndexInComponentTypeIdArray++] = componentType.TypeIndex;
                if (componentType.AccessModeType == ComponentType.AccessMode.Exclude)
                    throw new ArgumentException("EntityQueryDesc cannot contain Exclude Component types");
            }
        }

        /// <summary>
        /// Run consistency checks on a query description, and throw an exception if validation fails.
        /// </summary>
        /// <exception cref="EntityQueryDescValidationException">Thrown if the query fails validation. The exception
        /// message provides additional details.</exception>
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public void Validate()
        {
            // Determine the number of ComponentTypes contained in the filters
            var itemCount = None.Length + All.Length + Any.Length + Disabled.Length + Absent.Length + Present.Length;

            // Project all the ComponentType Ids of None, All, Any queryDesc filters into the same array to identify duplicated later on

            var allComponentTypeIds = new NativeArray<TypeIndex>(itemCount, Allocator.Temp);
            var curComponentTypeIndex = 0;
            AddComponentTypeIndicesToArray(None, ref allComponentTypeIds, ref curComponentTypeIndex);
            AddComponentTypeIndicesToArray(All, ref allComponentTypeIds, ref curComponentTypeIndex);
            AddComponentTypeIndicesToArray(Any, ref allComponentTypeIds, ref curComponentTypeIndex);
            AddComponentTypeIndicesToArray(Disabled, ref allComponentTypeIds, ref curComponentTypeIndex);
            AddComponentTypeIndicesToArray(Absent, ref allComponentTypeIds, ref curComponentTypeIndex);
            AddComponentTypeIndicesToArray(Present, ref allComponentTypeIds, ref curComponentTypeIndex);

            // Check for duplicate, only if necessary
            if (itemCount > 1)
            {
                // Sort the Ids to have identical value adjacent
                allComponentTypeIds.Sort();

                // Check for identical values
                var refId = allComponentTypeIds[0];
                for (int i = 1; i < allComponentTypeIds.Length; i++)
                {
                    var curId = allComponentTypeIds[i];
                    if (curId == refId)
                    {
                        var compType = TypeManager.GetType(curId);
                        throw new EntityQueryDescValidationException(
                            $"The component type {compType.Name} appears multiple times in an EntityQueryDesc. Duplicate component types are not allowed within an EntityQueryDesc.");
                    }

                    refId = curId;
                }
            }

            allComponentTypeIds.Dispose();
        }

        /// <summary>
        /// Compare to another object for equality.
        /// </summary>
        /// <param name="obj">The object to compare</param>
        /// <returns>True if <paramref name="obj"/> is an equivalent query description, or false it not.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as EntityQueryDesc);
        }

        /// <summary>
        /// Compare to another instance for equality.
        /// </summary>
        /// <param name="other">The other instance to compare.</param>
        /// <returns>True if the two instances are equal, or false if not.</returns>
        public bool Equals(EntityQueryDesc other)
        {
            if (ReferenceEquals(this, other))
                return true;
            if (ReferenceEquals(null, other))
                return false;
            if (!Options.Equals(other.Options))
                return false;
            if (!ArraysEquivalent(All, other.All))
                return false;
            if (!ArraysEquivalent(Any, other.Any))
                return false;
            if (!ArraysEquivalent(None, other.None))
                return false;
            if (!ArraysEquivalent(Disabled, other.Disabled))
                return false;
            if (!ArraysEquivalent(Absent, other.Absent))
                return false;
            if (!ArraysEquivalent(Present, other.Present))
                return false;
            return true;
        }

        /// <summary>
        /// Compare two instance for equality.
        /// </summary>
        /// <param name="lhs">The left instance to compare</param>
        /// <param name="rhs">The right instance to compare</param>
        /// <returns>True if the two instances are equal, or false if not.</returns>
        public static bool operator ==(EntityQueryDesc lhs, EntityQueryDesc rhs)
        {
            if (ReferenceEquals(lhs, null))
                return ReferenceEquals(rhs, null);

            return lhs.Equals(rhs);
        }

        /// <summary>
        /// Compare two instance for inequality.
        /// </summary>
        /// <param name="lhs">The left instance to compare</param>
        /// <param name="rhs">The right instance to compare</param>
        /// <returns>False if the two instances are equal, or true if not.</returns>
        public static bool operator !=(EntityQueryDesc lhs, EntityQueryDesc rhs)
        {
            return !(lhs == rhs);
        }

        /// <summary>
        /// Compute the hash code for this object
        /// </summary>
        /// <returns>The hash code</returns>
        public override int GetHashCode()
        {
            int result = 17;
            result = (result * 397) ^ Options.GetHashCode();
            result = (result * 397) ^ (All ?? Array.Empty<ComponentType>()).GetHashCode();
            result = (result * 397) ^ (Any ?? Array.Empty<ComponentType>()).GetHashCode();
            result = (result * 397) ^ (None ?? Array.Empty<ComponentType>()).GetHashCode();
            result = (result * 397) ^ (Disabled ?? Array.Empty<ComponentType>()).GetHashCode();
            result = (result * 397) ^ (Absent ?? Array.Empty<ComponentType>()).GetHashCode();
            result = (result * 397) ^ (Present ?? Array.Empty<ComponentType>()).GetHashCode();
            return result;
        }

        static unsafe bool ArraysEquivalent(ComponentType[] a1, ComponentType[] a2)
        {
            if (ReferenceEquals(a1, a2))
                return true;

            if (a1 == null || a2 == null)
                return false;

            if (a1.Length != a2.Length)
                return false;

            var sorted1 = (ComponentType[])a1.Clone();
            var sorted2 = (ComponentType[])a2.Clone();
            fixed (ComponentType* p1 = sorted1)
            fixed (ComponentType* p2 = sorted2)
            {
                NativeSortExtension.Sort(p1, sorted1.Length);
                NativeSortExtension.Sort(p2, sorted2.Length);
            }
            return sorted1.AsSpan().SequenceEqual(sorted2);
        }
    }

    /// <summary>
    /// The bit flags to use for the <see cref="EntityQueryDesc.Options"/> field.
    /// </summary>
    [Flags]
    public enum EntityQueryOptions
    {
        /// <summary>
        /// No options specified.
        /// </summary>
        Default = 0,
        /// <summary>
        /// The query does not exclude entities with the special <see cref="Prefab"/> component.
        /// </summary>
        IncludePrefab = 1,
        /// <summary>
        /// The query does not exclude entities with the special <see cref="Disabled"/> component.
        /// </summary>
        /// <remarks>
        /// To ignore the state of individual enableable components on an entity, use <see cref="IgnoreComponentEnabledState"/>.
        /// </remarks>
        /// <seealso cref="IgnoreComponentEnabledState"/>
        IncludeDisabledEntities = 2,
        /// <summary> Obsolete. Use <see cref="IncludeDisabledEntities"/> instead.</summary>
        [Obsolete("This enum value has been renamed to IncludeDisabledEntities. (RemovedAfter Entities 1.0) (UnityUpgradable) -> IncludeDisabledEntities", false)]
        IncludeDisabled = 2,
        /// <summary>
        /// Deprecated. The query filters selected entities based on the
        /// <see cref="WriteGroupAttribute"/> settings of the components specified in the query description.
        /// </summary>
        [Obsolete("Write groups are deprecated and will be removed in a future release. (RemovedAfter Entities 6.7)")]
        FilterWriteGroup = 4,
        /// <summary>
        /// The query will match all entities in all the query's matching archetypes, regardless of whether any
        /// enableable components on those entities are enabled or disabled.
        /// </summary>
        /// <remarks>
        /// Specifically:
        /// - Entities with all required enableable components will be matched, even if the components are disabled.
        /// - Entities with any optional enableable components will be matched, even if the components are disabled.
        /// - Entities with any excluded enableable component will be matched, even if the components are enabled.
        /// - Entities with any excluded non-enableable component will NOT be matched; their archetype is not in the potentially matching set.
        /// - Entities missing a required component will NOT be matched; their archetype is not in the potentially matching set.
        /// - Entities missing all the optional components will NOT be matched; their archetype is not in the potentially matching set.
        /// </remarks>
        IgnoreComponentEnabledState = 8,
        /// <summary>
        /// The query does not exclude the SystemInstance component, which is a component attached to Entities belonging to systems.
        /// </summary>
        IncludeSystems = 16,
        /// <summary>
        /// The query does not exclude the special <see cref="ChunkHeader"/> component, used by meta-chunks.
        /// </summary>
        IncludeMetaChunks = 32,
    }

    /// <summary>
    /// Provides an efficient test of whether a specific archetype is included in the set of archetypes matched by an
    /// EntityQuery.
    /// </summary>
    /// <remarks>
    /// Use a query mask to quickly identify whether an entity's archetype would be matched by an EntityQuery.
    ///
    /// <example>
    /// <code lang="csharp" source="../../DocCodeSamples.Tests/EntityQueryExamples.cs" region="entity-query-mask" title="Query Mask"/>
    /// </example>
    ///
    /// You can create up to 1024 unique EntityQueryMasks in an application.
    ///
    /// Note that EntityQueryMask only filters by Archetype. It ignores any filtering on the EntityQuery (by
    /// shared component, change version, or order version), or the state of any enableable components.
    ///
    /// One specific instance to be aware of is that if a query uses <see cref="EntityQueryBuilder.WithNone{T}()"/> with
    /// an enableable type T, an EntityQueryMask created from the query will effectively ignore this constraint. This
    /// is because for enableable types, WithNone matches both archetypes that don't contain T (as expected) but also those that
    /// do; entities in the latter archetypes could still match the query if their T component is disabled. Since
    /// EntityQueryMask ignores enableable component state, this means it considers an archetype to match whether or not
    /// it has T, meaning the WithNone(T) constraint has no effect. Consider using <see cref="EntityQueryBuilder.WithAbsent{T}()"/>
    /// in this case, if the intent is for the query mask to only match entities which don't have T at all.
    /// </remarks>
    /// <seealso cref="EntityManager.GetEntityQueryMask"/>
    public unsafe struct EntityQueryMask : IEquatable<EntityQueryMask>
    {
        internal byte Index;
        internal byte Mask;

        [NativeDisableUnsafePtrRestriction]
        internal readonly EntityComponentStore* EntityComponentStore;

        internal EntityQueryMask(byte index, byte mask, EntityComponentStore* entityComponentStore)
        {
            Index = index;
            Mask = mask;
            EntityComponentStore = entityComponentStore;
        }

        internal bool IsCreated()
        {
            return EntityComponentStore != null;
        }

        /// <summary>
        /// Compare two instances for equality.
        /// </summary>
        /// <param name="lhs">The left instance to compare.</param>
        /// <param name="rhs">The right instance to compare.</param>
        /// <returns>True if both masks refer to the same archetype set within the same world, or false if not.</returns>
        public static bool operator ==(EntityQueryMask lhs, EntityQueryMask rhs)
        {
            return lhs.Index == rhs.Index && lhs.Mask == rhs.Mask && lhs.EntityComponentStore == rhs.EntityComponentStore;
        }

        /// <summary>
        /// Compare two instances for inequality.
        /// </summary>
        /// <param name="lhs">The left instance to compare.</param>
        /// <param name="rhs">The right instance to compare.</param>
        /// <returns>False if both masks refer to the same archetype set within the same world, or true if not.</returns>
        public static bool operator !=(EntityQueryMask lhs, EntityQueryMask rhs)
        {
            return !(lhs == rhs);
        }

        /// <summary>
        /// Compare this instance to another <see cref="EntityQueryMask"/> for equality.
        /// </summary>
        /// <param name="other">The instance to compare against.</param>
        /// <returns>True if both masks refer to the same archetype set within the same world, or false otherwise.</returns>
        public bool Equals(EntityQueryMask other)
        {
            return this == other;
        }

        /// <summary>
        /// Compare this instance to another object for equality.
        /// </summary>
        /// <param name="obj">The object to compare against.</param>
        /// <returns>True if <paramref name="obj"/> is an <see cref="EntityQueryMask"/> that refers to the same archetype set
        /// within the same world, or false otherwise.</returns>
        public override bool Equals(object obj)
        {
            return obj is EntityQueryMask other && this == other;
        }

        /// <summary>
        /// Computes a hash code for this instance.
        /// </summary>
        /// <returns>A hash code derived from the mask's identifying fields.</returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(Index, Mask, unchecked((int)(long)EntityComponentStore));
        }

        /// <summary>
        /// Reports whether an entity's archetype is in the set of archetypes matched by this query.
        /// </summary>
        /// <remarks>
        /// This function does not consider any chunk filtering settings on the query, or whether the entity has any of
        /// the relevant components disabled.
        /// </remarks>
        /// <param name="entity">The entity to check.</param>
        /// <returns>True if the entity would be returned by the EntityQuery (ignoring any filtering or enableable
        /// components), or false if it would not.</returns>
        public bool MatchesIgnoreFilter(Entity entity)
        {
            return EntityComponentStore->Exists(entity) && EntityComponentStore->GetArchetype(entity)->CompareMask(this);
        }
        /// <summary> Obsolete. Use <see cref="MatchesIgnoreFilter(Unity.Entities.Entity)"/> instead.</summary>
        /// <param name="entity">The entity to check.</param>
        /// <returns>True if the entity would be returned by the EntityQuery (ignoring any filtering or enableable
        /// components), or false if it would not.</returns>
        [Obsolete("This method has been renamed to MatchesIgnoreFilter, for clarity. It will return false positives if the query uses chunk filtering or enableable components, and should not be used. (RemovedAfter Entities 1.0)", true)]
        public bool Matches(Entity entity)
        {
            return MatchesIgnoreFilter(entity);
        }

        /// <summary>
        /// Reports whether a chunk's archetype is in the set of archetypes matched by this query.
        /// </summary>
        /// <remarks>
        /// This function does not consider any chunk filtering settings on the query.
        /// </remarks>
        /// <param name="chunk">The chunk to check.</param>
        /// <returns>True if the chunk would be returned by the EntityQuery (ignoring any filtering), or false if it
        /// would not.</returns>
        public bool MatchesIgnoreFilter(ArchetypeChunk chunk)
        {
            return chunk.Archetype.Archetype->CompareMask(this);
        }
        /// <summary> Obsolete. Use <see cref="MatchesIgnoreFilter(ArchetypeChunk)"/> instead.</summary>
        /// <param name="chunk">The chunk to check.</param>
        /// <returns>True if the chunk would be returned by the EntityQuery (ignoring any filtering), or false if it
        /// would not.</returns>
        [Obsolete("This method has been renamed to MatchesIgnoreFilter, for clarity. It will return false positives if the query uses chunk filtering, and should not be used. (RemovedAfter Entities 1.0)", true)]
        public bool Matches(ArchetypeChunk chunk)
        {
            return MatchesIgnoreFilter(chunk);
        }


        /// <summary>
        /// Reports whether the archetype would be selected by the EntityQuery instance used to create this entity query mask.
        /// </summary>
        /// <remarks>
        /// The match does not consider any filter settings of the EntityQuery.
        /// </remarks>
        /// <param name="archetype">The archetype to check.</param>
        /// <returns>True if the entity would be returned by the EntityQuery, false if it would not.</returns>
        public bool Matches(EntityArchetype archetype)
        {
            return archetype.Archetype->CompareMask(this);
        }

        internal bool Matches(Archetype* archetype)
        {
            return archetype->CompareMask(this);
        }
    };

    [GenerateBurstMonoInterop("EntityQuery")]
    internal unsafe partial struct EntityQueryImpl
    {
        internal EntityDataAccess*              _Access;
        internal EntityQueryData* _QueryData;
        internal EntityQueryFilter          _Filter;
        internal ulong _SeqNo;

        internal byte                    _DisallowDisposing;

        internal GCHandle                _CachedState;

        internal ComponentSafetyHandles* SafetyHandles => &_Access->DependencyManager->Safety;

        internal void Construct(EntityQueryData* queryData, EntityDataAccess* access, ulong seqno)
        {
            _Access = access;
            _QueryData = queryData;
            _Filter = default(EntityQueryFilter);
            _SeqNo = seqno;
            fixed(EntityQueryImpl* self = &this)
            {
                access->AliveEntityQueries.Add((ulong)(IntPtr)self, default);
            }
        }

        public bool IsEmpty
        {
            get
            {
                var queryIncludesEnableableTypes = _QueryData->HasEnableableComponents != 0;
                if (!_Filter.RequiresMatchesFilter && !queryIncludesEnableableTypes)
                    // with all filtering disabled, the "ignore filter" result is always correct.
                    return IsEmptyIgnoreFilter;
                else if (IsEmptyIgnoreFilter)
                    // Even with filtering enabled, the "ignore filter" result is a correct early-out if it's true.
                    return true;

                SyncFilterTypes();

                return ChunkIterationUtility.IsEmpty(ref this, _Filter);
            }
        }

        /// <summary>
        /// Waits for any running jobs to complete which could affect which chunks/entities match this query.
        /// </summary>
        /// <remarks>
        /// If the query has an active change filter, this includes jobs writing to any types whose change version is being tracked.
        /// If the query includes any enableable components, this includes job writing to any of these types.
        /// It also includes a safety check to ensure that no unregistered jobs are writing to enableable types.
        /// </remarks>
        internal void SyncFilterTypes()
        {
            SyncChangeFilterTypes();
            SyncEnableableTypes();
            // This operation has an implicit read dependency on all enableable component types in the query, since it
            // will be reading their enabled bits. Make sure no running jobs are writing to these components (or their enabled bits).
            int enableableTypeCount = _QueryData->EnableableComponentTypeIndexCount;
            for (int i = 0; i < enableableTypeCount; ++i)
            {
                var safetyHandle =
                    SafetyHandles->GetSafetyHandleForComponentTypeHandle(_QueryData->EnableableComponentTypeIndices[i], true);
                AtomicSafetyHandle.CheckReadAndThrow(safetyHandle);
            }
        }

        public bool IsEmptyIgnoreFilter => GetMatchingChunkCache().Length == 0;

        [ExcludeFromBurstCompatTesting("Returns managed array")]
        internal ComponentType[] GetQueryTypes()
        {
            using var types = new NativeList<ComponentType>(128, Allocator.Temp);

            for (var i = 0; i < _QueryData->ArchetypeQueryCount; ++i)
            {
                for (var j = 0; j < _QueryData->ArchetypeQueries[i].AnyCount; ++j)
                {
                    var accessMode = (ComponentType.AccessMode)_QueryData->ArchetypeQueries[i].AnyAccessMode[j];
                    var type = TypeManager.GetType(_QueryData->ArchetypeQueries[i].Any[j]);

                    types.Add(accessMode == ComponentType.AccessMode.ReadOnly
                        ? ComponentType.ReadOnly(type)
                        : ComponentType.ReadWrite(type));
                }
                for (var j = 0; j < _QueryData->ArchetypeQueries[i].AllCount; ++j)
                {
                    var accessMode = (ComponentType.AccessMode)_QueryData->ArchetypeQueries[i].AllAccessMode[j];
                    var type = TypeManager.GetType(_QueryData->ArchetypeQueries[i].All[j]);

                    types.Add(accessMode == ComponentType.AccessMode.ReadOnly
                        ? ComponentType.ReadOnly(type)
                        : ComponentType.ReadWrite(type));
                }
                for (var j = 0; j < _QueryData->ArchetypeQueries[i].NoneCount; ++j)
                {
                    var type = TypeManager.GetType(_QueryData->ArchetypeQueries[i].None[j]);
                    types.Add(ComponentType.ReadOnly(type));
                }

                for (var j = 0; j < _QueryData->ArchetypeQueries[i].DisabledCount; ++j)
                {
                    var accessMode = (ComponentType.AccessMode)_QueryData->ArchetypeQueries[i].DisabledAccessMode[j];
                    var type = TypeManager.GetType(_QueryData->ArchetypeQueries[i].Disabled[j]);

                    types.Add(accessMode == ComponentType.AccessMode.ReadOnly
                        ? ComponentType.ReadOnly(type)
                        : ComponentType.ReadWrite(type));
                }

                for (var j = 0; j < _QueryData->ArchetypeQueries[i].AbsentCount; ++j)
                {
                    var type = TypeManager.GetType(_QueryData->ArchetypeQueries[i].Absent[j]);
                    types.Add(ComponentType.ReadOnly(type));
                }

                for (var j = 0; j < _QueryData->ArchetypeQueries[i].PresentCount; ++j)
                {
                    var accessMode = (ComponentType.AccessMode)_QueryData->ArchetypeQueries[i].PresentAccessMode[j];
                    var type = TypeManager.GetType(_QueryData->ArchetypeQueries[i].Present[j]);

                    types.Add(accessMode == ComponentType.AccessMode.ReadOnly
                        ? ComponentType.ReadOnly(type)
                        : ComponentType.ReadWrite(type));
                }
            }
            using var typesArray = types.ToArray(Allocator.Temp);
            return typesArray.ToArray();
        }

        [ExcludeFromBurstCompatTesting("Returns managed array")]
        internal ComponentType[] GetReadAndWriteTypes()
        {
            var types = new ComponentType[_QueryData->ReaderTypesCount + _QueryData->WriterTypesCount];
            var typeArrayIndex = 0;
            for (var i = 0; i < _QueryData->ReaderTypesCount; ++i)
            {
                types[typeArrayIndex++] = ComponentType.ReadOnly(TypeManager.GetType(_QueryData->ReaderTypes[i]));
            }
            for (var i = 0; i < _QueryData->WriterTypesCount; ++i)
            {
                types[typeArrayIndex++] = TypeManager.GetType(_QueryData->WriterTypes[i]);
            }

            return types;
        }

        public void Dispose()
        {
            fixed (EntityQueryImpl* self = &this)
            {
                if (_DisallowDisposing != 0)
                    throw new InvalidOperationException("EntityQuery cannot be disposed. Note that queries created with GetEntityQuery() should not be manually disposed; they are owned by the system, and will be destroyed along with the system itself.");
                _Access->AliveEntityQueries.Remove((ulong)(IntPtr)self);

                if (_CachedState.IsAllocated)
                {
                    FreeCachedState(self);
                    _CachedState = default;
                }

                if (_QueryData != null)
                    ResetFilter();

                _Access = null;
                _QueryData = null;
            }
        }

        bool GetIsReadWrite(int indexInEntityQuery)
        {
            var type = _QueryData->RequiredComponents + indexInEntityQuery;
            var isReadWrite = type->AccessModeType == ComponentType.AccessMode.ReadWrite;
            return isReadWrite;
        }

        public int CalculateEntityCount()
        {
            SyncFilterTypes();
            return ChunkIterationUtility.CalculateEntityCount(GetMatchingChunkCache(),
                ref _QueryData->MatchingArchetypes, ref _Filter, _QueryData->HasEnableableComponents);
        }

        public int CalculateEntityCountWithoutFiltering()
        {
            var dummyFilter = default(EntityQueryFilter);
            return ChunkIterationUtility.CalculateEntityCount(GetMatchingChunkCache(),
                ref _QueryData->MatchingArchetypes, ref dummyFilter, 0);
        }

        public int CalculateChunkCount()
        {
            SyncChangeFilterTypes();
            SyncEnableableTypes();
            return ChunkIterationUtility.CalculateChunkCount(GetMatchingChunkCache(),
                ref _QueryData->MatchingArchetypes, ref _Filter, _QueryData->HasEnableableComponents);
        }

        public int CalculateChunkCountWithoutFiltering()
        {
            var dummyFilter = default(EntityQueryFilter);
            return ChunkIterationUtility.CalculateChunkCount(GetMatchingChunkCache(),
                ref _QueryData->MatchingArchetypes, ref dummyFilter, 0);
        }

        public NativeArray<int> CalculateFilteredChunkIndexArray(AllocatorManager.AllocatorHandle allocator)
        {
            // Sync on jobs that affect chunk filtering
            SyncChangeFilterTypes();
            // ...but we also need to sync on jobs that write to enableable types, to detect "empty" chunks.
            SyncEnableableTypes();
            int unfilteredChunkCount = CalculateChunkCountWithoutFiltering();
            var outputArray =
                CollectionHelper.CreateNativeArray<int>(unfilteredChunkCount, allocator, NativeArrayOptions.UninitializedMemory);

            ChunkIterationUtility.CalculateFilteredChunkIndexArray(GetMatchingChunkCache(),
                _QueryData->MatchingArchetypes, ref _Filter, _QueryData->HasEnableableComponents, ref outputArray);

            return outputArray;
        }

        public NativeArray<int> CalculateFilteredChunkIndexArrayAsync(AllocatorManager.AllocatorHandle allocator, JobHandle additionalInputDep, out JobHandle outJobHandle)
        {
            if (allocator.ToAllocator == Allocator.Temp)
                throw new ArgumentException("Allocator.Temp containers cannot be used when scheduling a job, use TempJob instead.");

            int unfilteredChunkCount = CalculateChunkCountWithoutFiltering();
            var outputArray = CollectionHelper.CreateNativeArray<int>(unfilteredChunkCount, allocator);

            // The job we schedule here has the following dependencies:
            // 1. Jobs writing to any of the query's enableable components
            // 2. Jobs writing to any types included in the query's change filter
            // 3. The additionalInputDep handle provided by the caller
            int enableableComponentCount = _QueryData->EnableableComponentTypeIndexCount;
            int changeFilterComponentCount = _Filter.Changed.Count;
            int componentDependencyCount =  enableableComponentCount + changeFilterComponentCount;
            var componentDependencies = stackalloc TypeIndex[componentDependencyCount];
            int writeCount = 0;
            for (int i = 0; i < changeFilterComponentCount; ++i)
            {
                var type = _QueryData->RequiredComponents[_Filter.Changed.IndexInEntityQuery[i]];
                componentDependencies[writeCount++] = type.TypeIndex;
            }
            for (int i = 0; i < enableableComponentCount; ++i)
            {
                componentDependencies[writeCount++] = _QueryData->EnableableComponentTypeIndices[i];
            }
            var inputDep = JobHandle.CombineDependencies(additionalInputDep,
                _Access->DependencyManager->GetDependency(componentDependencies, componentDependencyCount, null, 0,
                    clearReadFencesAfterCombining:false));

            var job = new FilteredChunkIndexJob
            {
                CachedChunkList = GetMatchingChunkCache(),
                Filter = _Filter,
                MatchingArchetypes = _QueryData->MatchingArchetypes,
                OutFilteredChunkIndices = outputArray,
                QueryIncludesEnableableComponents = _QueryData->HasEnableableComponents,
            };
            outJobHandle = job.Schedule(inputDep);
            return outputArray;
        }

        public NativeArray<int> CalculateBaseEntityIndexArray(AllocatorManager.AllocatorHandle allocator)
        {
            if (allocator.ToAllocator == Allocator.Temp)
                throw new ArgumentException("Allocator.Temp containers cannot be used when scheduling a job, use TempJob instead.");

            // Sync on jobs that affect chunk filtering
            SyncChangeFilterTypes();
            // ...but we also need to sync on jobs that write to enableable types, to get accurate per-chunk entity counts
            SyncEnableableTypes();
            int chunkCount = CalculateChunkCountWithoutFiltering();
            var outputArray =
                CollectionHelper.CreateNativeArray<int>(chunkCount, allocator, NativeArrayOptions.UninitializedMemory);

            ChunkIterationUtility.CalculateBaseEntityIndexArray(GetMatchingChunkCache(),
                _QueryData->MatchingArchetypes, ref _Filter, _QueryData->HasEnableableComponents, ref outputArray);

            return outputArray;
        }

        public NativeArray<int> CalculateBaseEntityIndexArrayAsync(AllocatorManager.AllocatorHandle allocator, JobHandle additionalInputDep, out JobHandle outJobHandle)
        {
            if (allocator.ToAllocator == Allocator.Temp)
                throw new ArgumentException("Allocator.Temp containers cannot be used when scheduling a job, use TempJob instead.");

            int conservativeChunkCount = CalculateChunkCountWithoutFiltering();
            var outputArray = CollectionHelper.CreateNativeArray<int>(conservativeChunkCount, allocator);

            // The job we schedule here has the following dependencies:
            // 1. Jobs writing to any of the query's enableable components
            // 2. Jobs writing to any types included in the query's change filter
            // 3. The additionalInputDep handle provided by the caller
            int enableableComponentCount = _QueryData->EnableableComponentTypeIndexCount;
            int changeFilterComponentCount = _Filter.Changed.Count;
            int componentDependencyCount =  enableableComponentCount + changeFilterComponentCount;
            var componentDependencies = stackalloc TypeIndex[componentDependencyCount];
            int writeCount = 0;
            for (int i = 0; i < changeFilterComponentCount; ++i)
            {
                var type = _QueryData->RequiredComponents[_Filter.Changed.IndexInEntityQuery[i]];
                componentDependencies[writeCount++] = type.TypeIndex;
            }
            for (int i = 0; i < enableableComponentCount; ++i)
            {
                componentDependencies[writeCount++] = _QueryData->EnableableComponentTypeIndices[i];
            }
            var inputDep = JobHandle.CombineDependencies(additionalInputDep,
                _Access->DependencyManager->GetDependency(componentDependencies, componentDependencyCount,
                    null, 0, clearReadFencesAfterCombining:false));

            var job = new ChunkBaseEntityIndexJob
            {
                CachedChunkList = GetMatchingChunkCache(),
                Filter = _Filter,
                MatchingArchetypes = _QueryData->MatchingArchetypes,
                OutChunkBaseEntityIndices = outputArray,
                QueryIncludesEnableableComponents = _QueryData->HasEnableableComponents,
            };
            outJobHandle = job.Schedule(inputDep);
            return outputArray;
        }

        internal int GetIndexInEntityQuery(TypeIndex componentType)
        {
            var componentIndex = 0;
            while (componentIndex < _QueryData->RequiredComponentsCount && _QueryData->RequiredComponents[componentIndex].TypeIndex != componentType)
                ++componentIndex;

            if (componentIndex >= _QueryData->RequiredComponentsCount || _QueryData->RequiredComponents[componentIndex].AccessModeType == ComponentType.AccessMode.Exclude)
                throw new InvalidOperationException($"Trying to get iterator for {TypeManager.GetType(componentType)} but the required component type was not declared in the EntityQuery.");
            return componentIndex;
        }

        [Obsolete("Remove with CreateArchetypeChunkArrayAsync. (RemovedAfter Entities 1.0)")]
        public NativeArray<ArchetypeChunk> CreateArchetypeChunkArrayAsync(AllocatorManager.AllocatorHandle allocator, out JobHandle jobhandle)
        {
            JobHandle dependency = default;

            var filterCount = _Filter.Changed.Count;
            if (filterCount > 0)
            {
                var readerTypes = stackalloc TypeIndex[filterCount];
                for (int i = 0; i < filterCount; ++i)
                    readerTypes[i] = _QueryData->RequiredComponents[_Filter.Changed.IndexInEntityQuery[i]].TypeIndex;

                dependency = _Access->DependencyManager->GetDependency(readerTypes, filterCount,
                    null, 0, clearReadFencesAfterCombining:false);
            }

            return ChunkIterationUtility.CreateArchetypeChunkArrayAsync(_QueryData->MatchingArchetypes, allocator, out jobhandle, ref _Filter, dependency);
        }

        public NativeList<ArchetypeChunk> ToArchetypeChunkListAsync(AllocatorManager.AllocatorHandle allocator, JobHandle additionalInputDep, out JobHandle outJobHandle)
        {
            if (allocator.ToAllocator == Allocator.Temp)
                throw new ArgumentException("Allocator.Temp containers cannot be used when scheduling a job, use TempJob instead.");

            int unfilteredChunkCount = CalculateChunkCountWithoutFiltering();
            var outputList = new NativeList<ArchetypeChunk>(unfilteredChunkCount, allocator);

            // The job we schedule here has the following dependencies:
            // 1. Jobs writing to any of the query's enableable components
            // 2. Jobs writing to any types included in the query's change filter
            // 3. The additionalInputDep handle provided by the caller
            int enableableComponentCount = _QueryData->EnableableComponentTypeIndexCount;
            int changeFilterComponentCount = _Filter.Changed.Count;
            int componentDependencyCount =  enableableComponentCount + changeFilterComponentCount;
            var componentDependencies = stackalloc TypeIndex[componentDependencyCount];
            int writeCount = 0;
            for (int i = 0; i < changeFilterComponentCount; ++i)
            {
                var type = _QueryData->RequiredComponents[_Filter.Changed.IndexInEntityQuery[i]];
                componentDependencies[writeCount++] = type.TypeIndex;
            }
            for (int i = 0; i < enableableComponentCount; ++i)
            {
                componentDependencies[writeCount++] = _QueryData->EnableableComponentTypeIndices[i];
            }
            var inputDep = JobHandle.CombineDependencies(additionalInputDep,
                _Access->DependencyManager->GetDependency(componentDependencies, componentDependencyCount,
                    null, 0, clearReadFencesAfterCombining:false));

            var job = new GatherChunksJob
            {
                ChunkCache = GetMatchingChunkCache(),
                Filter = _Filter,
                MatchingArchetypes = _QueryData->MatchingArchetypes,
                QueryContainsEnableableComponents = _QueryData->HasEnableableComponents,
                OutFilteredChunksList = new TypelessUnsafeList{
                    Ptr = (byte*)outputList.m_ListData->Ptr,
                    Length = &(outputList.m_ListData->m_length),
                    Capacity = outputList.m_ListData->Capacity,
                    m_Safety = outputList.m_Safety,
                },
            };
            outJobHandle = job.Schedule(inputDep);
            return outputList;
        }

        public NativeArray<ArchetypeChunk> ToArchetypeChunkArray(AllocatorManager.AllocatorHandle allocator)
        {
            // Sync on jobs that affect chunk filtering
            SyncChangeFilterTypes();
            // ...but we also need to sync on jobs that write to enableable types, to detect "empty" chunks.
            SyncEnableableTypes();
            // We could safely compute the exact number of matching chunks at this point, but doing so would be nearly as
            // much work as the follow-up work to gather those chunks into an array. Instead, we allocate a conservatively-sized
            // list, populate that list directly, and convert the list to an array to return to the caller. This avoids a needless
            // memcpy, but may return an array with a larger than expected memory allocation.
            int unfilteredChunkCount = CalculateChunkCountWithoutFiltering();
            if (unfilteredChunkCount == 0)
            {
                // ConvertExistingDataToNativeArray() returns an invalid array if the input list length is zero, but the
                // caller is expecting a valid array, so...
                return CollectionHelper.CreateNativeArray<ArchetypeChunk>(0, allocator);
            }

            var outputList = new NativeList<ArchetypeChunk>(unfilteredChunkCount, allocator);
            ChunkIterationUtility.ToArchetypeChunkList(GetMatchingChunkCache(),
                _QueryData->MatchingArchetypes, ref _Filter, _QueryData->HasEnableableComponents, ref outputList);

            // A NativeList contains two memory allocations: one for the actual list contents, and one for the
            // fixed-size UnsafeList struct. This sequence frees the UnsafeList while passing ownership of the contents
            // to a new NativeArray.
            var outputArray = CollectionHelper.ConvertExistingNativeListToNativeArray(ref outputList, outputList.Length,
                outputList.m_ListData->Allocator);
            AllocatorManager.Free(allocator, outputList.m_ListData);
            return outputArray;
        }

        public void ToArchetypeChunkList(NativeList<ArchetypeChunk> archetypeChunkList)
        {
            // Sync on jobs that affect chunk filtering
            SyncChangeFilterTypes();
            // ...but we also need to sync on jobs that write to enableable types, to detect "empty" chunks.
            SyncEnableableTypes();

            archetypeChunkList.Clear();

            int unfilteredChunkCount = CalculateChunkCountWithoutFiltering();
            if (unfilteredChunkCount == 0)
                return;

            archetypeChunkList.SetCapacity(unfilteredChunkCount);
            ChunkIterationUtility.ToArchetypeChunkList(GetMatchingChunkCache(), _QueryData->MatchingArchetypes, ref _Filter, _QueryData->HasEnableableComponents, ref archetypeChunkList);
        }

        [GenerateTestsForBurstCompatibility(RequiredUnityDefine = "ENABLE_UNITY_COLLECTIONS_CHECKS", CompileTarget = GenerateTestsForBurstCompatibilityAttribute.BurstCompatibleCompileTarget.Editor)]
        [Obsolete("This method does not correctly handle enableable components, and is generally unsafe. Use ToEntityListAsync() instead. (RemovedAfter Entities 1.0)")]
        public NativeArray<Entity> ToEntityArrayAsync(AllocatorManager.AllocatorHandle allocator, out JobHandle jobhandle, EntityQuery outer)
        {
            if (_QueryData->HasEnableableComponents != 0)
            {
                throw new InvalidOperationException(
                    "ToEntityArrayAsync() can not be used on queries with enableable components. Use ToEntityListAsync() instead.");
            }

            if (allocator.ToAllocator == Allocator.Temp)
                throw new ArgumentException("Allocator.Temp containers cannot be used when scheduling a job, use TempJob instead.");

            var entityType = new EntityTypeHandle(SafetyHandles->GetSafetyHandleForEntityTypeHandle());

            return ChunkIterationUtility.CreateEntityArrayAsync(_QueryData->MatchingArchetypes, allocator, entityType,
                outer,CalculateEntityCount(), out jobhandle, GetDependency());
        }

        [GenerateTestsForBurstCompatibility(RequiredUnityDefine = "ENABLE_UNITY_COLLECTIONS_CHECKS", CompileTarget = GenerateTestsForBurstCompatibilityAttribute.BurstCompatibleCompileTarget.Editor)]
        public NativeList<Entity> ToEntityListAsync(AllocatorManager.AllocatorHandle allocator, EntityQuery outer, JobHandle additionalInputDep, out JobHandle outJobHandle)
        {
            if (allocator.ToAllocator == Allocator.Temp)
                throw new ArgumentException("Allocator.Temp containers cannot be used when scheduling a job, use TempJob instead.");

            var entityType = new EntityTypeHandle(SafetyHandles->GetSafetyHandleForEntityTypeHandle());
            // We can't use CalculateEntityCount() here, as it would need to sync on any running jobs that write to
            // the query's enableable types. To make this a safe asynchronous operation, we use the (non-blocking) no-filtering
            // code path to get an upper-bound capacity for the output list.
            int conservativeEntityCount = CalculateEntityCountWithoutFiltering();

            // The job we schedule here has the following dependencies:
            // 1. Jobs writing to any of the query's enableable components
            // 2. Jobs writing to any types included in the query's change filter
            // 3. Jobs writing to the Entity component itself (pathological, not included here)
            // 4. The additionalInputDep handle provided by the caller
            int enableableComponentCount = _QueryData->EnableableComponentTypeIndexCount;
            int changeFilterComponentCount = _Filter.Changed.Count;
            int componentDependencyCount =  enableableComponentCount + changeFilterComponentCount;
            var componentDependencies = stackalloc TypeIndex[componentDependencyCount];
            int writeCount = 0;
            for (int i = 0; i < changeFilterComponentCount; ++i)
            {
                var type = _QueryData->RequiredComponents[_Filter.Changed.IndexInEntityQuery[i]];
                componentDependencies[writeCount++] = type.TypeIndex;
            }
            for (int i = 0; i < enableableComponentCount; ++i)
            {
                componentDependencies[writeCount++] = _QueryData->EnableableComponentTypeIndices[i];
            }
            var inputDep = JobHandle.CombineDependencies(additionalInputDep,
                _Access->DependencyManager->GetDependency(componentDependencies, componentDependencyCount,
                    null, 0, clearReadFencesAfterCombining:false));

            return ChunkIterationUtility.CreateEntityListAsync(allocator, entityType, outer, conservativeEntityCount,
                inputDep, out outJobHandle);
        }

        [GenerateTestsForBurstCompatibility(RequiredUnityDefine = "ENABLE_UNITY_COLLECTIONS_CHECKS", CompileTarget = GenerateTestsForBurstCompatibilityAttribute.BurstCompatibleCompileTarget.Editor)]
        public NativeArray<Entity> ToEntityArray(AllocatorManager.AllocatorHandle allocator, EntityQuery outer)
        {
            // CalculateEntityCount() syncs any jobs that could affect the filter results for this query
            int entityCount = CalculateEntityCount();

            // No jobs should be writing to the Entity component type, but just in case...
            _Access->DependencyManager->CompleteWriteDependency(TypeManager.GetTypeIndex<Entity>());
            var entityType = new EntityTypeHandle(SafetyHandles->GetSafetyHandleForEntityTypeHandle());
            AtomicSafetyHandle.CheckReadAndThrow(entityType.m_Safety);

            return ChunkIterationUtility.CreateEntityArray(
                    _QueryData->MatchingArchetypes, allocator, entityType, outer, entityCount);
        }

        [GenerateTestsForBurstCompatibility(RequiredUnityDefine = "ENABLE_UNITY_COLLECTIONS_CHECKS", CompileTarget = GenerateTestsForBurstCompatibilityAttribute.BurstCompatibleCompileTarget.Editor)]
        internal void GatherEntitiesToArray(out Internal.InternalGatherEntitiesResult result, EntityQuery outer)
        {
            result = default;
            result.EntityArray = outer.ToEntityArray(Allocator.TempJob);
            result.EntityBuffer = (Entity*)result.EntityArray.GetUnsafeReadOnlyPtr();
            result.EntityCount = result.EntityArray.Length;
        }

        internal void ReleaseGatheredEntities(ref Internal.InternalGatherEntitiesResult result)
        {
            if (result.EntityArray.IsCreated)
            {
                result.EntityArray.Dispose();
            }
        }

        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleComponentData) },
            RequiredUnityDefine = "ENABLE_UNITY_COLLECTIONS_CHECKS",
            CompileTarget = GenerateTestsForBurstCompatibilityAttribute.BurstCompatibleCompileTarget.Editor)]
        [Obsolete(
            "This method does not correctly support enableable components. Use ToComponentDataListAsync() instead. (RemovedAfter Entities 1.0)")]
        public NativeArray<T> ToComponentDataArrayAsync<T>(AllocatorManager.AllocatorHandle allocator, out JobHandle jobhandle,
            EntityQuery outer)
            where T : unmanaged, IComponentData
        {
            if (_QueryData->HasEnableableComponents != 0)
            {
                throw new InvalidOperationException(
                    "ToComponentDataArrayAsync() can not be used on queries with enableable components. Use ToComponentDataListAsync() instead.");
            }
            if (allocator.ToAllocator == Allocator.Temp)
                throw new ArgumentException("Allocator.Temp containers cannot be used when scheduling a job, use TempJob instead.");

            var componentType = new ComponentTypeHandle<T>(SafetyHandles->GetSafetyHandleForComponentTypeHandle(TypeManager.GetTypeIndex<T>(), true), true, _Access->EntityComponentStore->GlobalSystemVersion);


            var typeIndex = TypeManager.GetTypeIndex<T>();
            int indexInEntityQuery = GetIndexInEntityQuery(typeIndex);
            if (indexInEntityQuery == -1)
                throw new InvalidOperationException($"Trying ToComponentDataArrayAsync of {TypeManager.GetType(typeIndex)} but the required component type was not declared in the EntityQuery.");
            return ChunkIterationUtility.CreateComponentDataArrayAsync(allocator, ref componentType,CalculateEntityCount(), outer, out jobhandle, GetDependency());
        }

        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleComponentData) },
            RequiredUnityDefine = "ENABLE_UNITY_COLLECTIONS_CHECKS",
            CompileTarget = GenerateTestsForBurstCompatibilityAttribute.BurstCompatibleCompileTarget.Editor)]
        public NativeList<T> ToComponentDataListAsync<T>(AllocatorManager.AllocatorHandle allocator, EntityQuery outer, JobHandle additionalInputDep, out JobHandle outJobHandle)
            where T : unmanaged, IComponentData
        {
            var typeIndex = TypeManager.GetTypeIndex<T>();
            if (allocator.ToAllocator == Allocator.Temp)
                throw new ArgumentException("Allocator.Temp containers cannot be used when scheduling a job, use TempJob instead.");
            int indexInEntityQuery = GetIndexInEntityQuery(typeIndex);
            if (indexInEntityQuery == -1)
                throw new InvalidOperationException($"Trying ToComponentDataArrayAsync of {TypeManager.GetType(typeIndex)} but the required component type was not declared in the EntityQuery.");

            var componentType = ComponentType.ReadOnly<T>();
            var dynamicTypeHandle = new DynamicComponentTypeHandle(componentType,
                SafetyHandles->GetSafetyHandleForDynamicComponentTypeHandle(componentType.TypeIndex, true),
                default, _Access->EntityComponentStore->GlobalSystemVersion);

            // We can't use CalculateEntityCount() here, as it would need to sync on any running jobs that write to
            // the query's enableable types. To make this a safe asynchronous operation, we use the (non-blocking) no-filtering
            // code path to get an upper-bound capacity for the output list.
            int conservativeEntityCount = CalculateEntityCountWithoutFiltering();

            // The job we schedule here has the following dependencies:
            // 1. Jobs writing to any of the query's enableable components
            // 2. Jobs writing to any types included in the query's change filter
            // 3. Jobs writing to the component T itself
            // 4. The additionalInputDep handle provided by the caller
            int enableableComponentCount = _QueryData->EnableableComponentTypeIndexCount;
            int changeFilterComponentCount = _Filter.Changed.Count;
            int componentDependencyCount =  enableableComponentCount + changeFilterComponentCount + 1;
            var componentDependencies = stackalloc TypeIndex[componentDependencyCount];
            int writeCount = 0;
            for (int i = 0; i < changeFilterComponentCount; ++i)
            {
                var type = _QueryData->RequiredComponents[_Filter.Changed.IndexInEntityQuery[i]];
                componentDependencies[writeCount++] = type.TypeIndex;
            }
            for (int i = 0; i < enableableComponentCount; ++i)
            {
                componentDependencies[writeCount++] = _QueryData->EnableableComponentTypeIndices[i];
            }
            componentDependencies[writeCount++] = typeIndex;
            var inputDep = JobHandle.CombineDependencies(additionalInputDep,
                _Access->DependencyManager->GetDependency(componentDependencies, componentDependencyCount,
                    null, 0, clearReadFencesAfterCombining:false));

            return ChunkIterationUtility.CreateComponentDataListAsync<T>(allocator, ref dynamicTypeHandle, conservativeEntityCount,
                outer, inputDep, out outJobHandle);
        }

        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleComponentData) }, RequiredUnityDefine = "ENABLE_UNITY_COLLECTIONS_CHECKS", CompileTarget = GenerateTestsForBurstCompatibilityAttribute.BurstCompatibleCompileTarget.Editor)]
        public NativeArray<T> ToComponentDataArray<T>(AllocatorManager.AllocatorHandle allocator, EntityQuery outer)
            where T : unmanaged, IComponentData
        {
            // CalculateEntityCount() syncs any jobs that could affect the filtering results for this query
            int entityCount = CalculateEntityCount();
            // We also need to complete any jobs writing to the component we're gathering.
            var typeIndex = TypeManager.GetTypeIndex<T>();
            _Access->DependencyManager->CompleteWriteDependency(typeIndex);

            var componentType = new ComponentTypeHandle<T>(SafetyHandles->GetSafetyHandleForComponentTypeHandle(TypeManager.GetTypeIndex<T>(), true), true, _Access->EntityComponentStore->GlobalSystemVersion);
            AtomicSafetyHandle.CheckReadAndThrow(componentType.m_Safety);

            int indexInEntityQuery = GetIndexInEntityQuery(typeIndex);
            if (indexInEntityQuery == -1)
                throw new InvalidOperationException($"Trying ToComponentDataArray of {TypeManager.GetType(typeIndex)} but the required component type was not declared in the EntityQuery.");


            return ChunkIterationUtility.CreateComponentDataArray(allocator, ref componentType, entityCount, outer);
        }


        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleComponentData) }, RequiredUnityDefine = "ENABLE_UNITY_COLLECTIONS_CHECKS", CompileTarget = GenerateTestsForBurstCompatibilityAttribute.BurstCompatibleCompileTarget.Editor)]
        public void CopyFromComponentDataArray<T>(NativeArray<T> componentDataArray, EntityQuery outer)
            where T : unmanaged, IComponentData
        {
            // CalculateEntityCount() syncs any jobs that could affect the filtering results for this query
            int entityCount = CalculateEntityCount();
            // We also need to complete any jobs reading or writing to the component we're scattering
            var typeIndex = TypeManager.GetTypeIndex<T>();
            _Access->DependencyManager->CompleteReadAndWriteDependency(typeIndex);

            if (entityCount != componentDataArray.Length)
                throw new ArgumentException($"Length of input array ({componentDataArray.Length}) does not match length of EntityQuery ({entityCount})");

            var componentType = new ComponentTypeHandle<T>(SafetyHandles->GetSafetyHandleForComponentTypeHandle(TypeManager.GetTypeIndex<T>(), false), false, _Access->EntityComponentStore->GlobalSystemVersion);
            AtomicSafetyHandle.CheckWriteAndThrow(componentType.m_Safety);

            int indexInEntityQuery = GetIndexInEntityQuery(typeIndex);
            if (indexInEntityQuery == -1)
                throw new InvalidOperationException($"Trying CopyFromComponentDataArrayAsync of {TypeManager.GetType(typeIndex)} but the required component type was not declared in the EntityQuery.");
            ChunkIterationUtility.CopyFromComponentDataArray(componentDataArray, ref componentType, outer);
        }

        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleComponentData) }, RequiredUnityDefine = "ENABLE_UNITY_COLLECTIONS_CHECKS", CompileTarget = GenerateTestsForBurstCompatibilityAttribute.BurstCompatibleCompileTarget.Editor)]
        [Obsolete("This method does not correctly support enableable components. Use CopyFromComponentDataListAsync() instead. (RemovedAfter Entities 1.0)")]
        public void CopyFromComponentDataArrayAsync<T>(NativeArray<T> componentDataArray, out JobHandle jobhandle, EntityQuery outer)
            where T : unmanaged, IComponentData
        {
            if (_QueryData->HasEnableableComponents != 0)
            {
                throw new InvalidOperationException(
                    "CopyFromComponentDataAsync() can not be used on queries with enableable components. Use CopyFromComponentDataListAsync() instead.");
            }

            if (componentDataArray.m_AllocatorLabel == Allocator.Temp)
            {
                throw new ArgumentException(
                    $"The NativeContainer is allocated with Allocator.Temp." +
                    $", use TempJob instead.",nameof (componentDataArray));
            }

            var entityCount = CalculateEntityCount();
            if (entityCount != componentDataArray.Length)
                throw new ArgumentException($"Length of input array ({componentDataArray.Length}) does not match length of EntityQuery ({entityCount})");

            var componentType = new ComponentTypeHandle<T>(SafetyHandles->GetSafetyHandleForComponentTypeHandle(TypeManager.GetTypeIndex<T>(), false), false, _Access->EntityComponentStore->GlobalSystemVersion);

            var typeIndex = TypeManager.GetTypeIndex<T>();
            int indexInEntityQuery = GetIndexInEntityQuery(typeIndex);
            if (indexInEntityQuery == -1)
                throw new InvalidOperationException($"Trying CopyFromComponentDataArrayAsync of {TypeManager.GetType(typeIndex)} but the required component type was not declared in the EntityQuery.");

            ChunkIterationUtility.CopyFromComponentDataArrayAsync(_QueryData->MatchingArchetypes, componentDataArray, ref componentType, outer, ref _Filter, out jobhandle, GetDependency());
        }

        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleComponentData) }, RequiredUnityDefine = "ENABLE_UNITY_COLLECTIONS_CHECKS", CompileTarget = GenerateTestsForBurstCompatibilityAttribute.BurstCompatibleCompileTarget.Editor)]
        public void CopyFromComponentDataListAsync<T>(NativeList<T> componentDataList, EntityQuery outer, JobHandle additionalInputDep, out JobHandle outJobHandle)
            where T : unmanaged, IComponentData
        {
            var typeIndex = TypeManager.GetTypeIndex<T>();
            if (componentDataList.m_ListData->Allocator.ToAllocator == Allocator.Temp)
            {
                throw new ArgumentException(
                    $"The NativeContainer is allocated with Allocator.Temp." +
                    $", use TempJob instead.",nameof (componentDataList));
            }
            int indexInEntityQuery = GetIndexInEntityQuery(typeIndex);
            if (indexInEntityQuery == -1)
                throw new InvalidOperationException($"Trying CopyFromComponentDataListAsync of {TypeManager.GetType(typeIndex)} but the required component type was not declared in the EntityQuery.");

            var componentType = ComponentType.ReadWrite<T>();
            var dynamicTypeHandle = new DynamicComponentTypeHandle(componentType,
                SafetyHandles->GetSafetyHandleForDynamicComponentTypeHandle(componentType.TypeIndex, false),
                default, _Access->EntityComponentStore->GlobalSystemVersion);

            // The job we schedule here has the following dependencies:
            // 1. Jobs writing to any of the query's enableable components
            // 2. Jobs writing to any types included in the query's change filter
            // 3. Jobs reading or writing to the component T itself
            // 4. The additionalInputDep handle provided by the caller
            int enableableComponentCount = _QueryData->EnableableComponentTypeIndexCount;
            int changeFilterComponentCount = _Filter.Changed.Count;
            int componentDependencyCount =  enableableComponentCount + changeFilterComponentCount;
            var componentDependencies = stackalloc TypeIndex[componentDependencyCount];
            int writeCount = 0;
            for (int i = 0; i < changeFilterComponentCount; ++i)
            {
                var type = _QueryData->RequiredComponents[_Filter.Changed.IndexInEntityQuery[i]];
                componentDependencies[writeCount++] = type.TypeIndex;
            }
            for (int i = 0; i < enableableComponentCount; ++i)
            {
                componentDependencies[writeCount++] = _QueryData->EnableableComponentTypeIndices[i];
            }
            var inputDep = JobHandle.CombineDependencies(additionalInputDep,
                _Access->DependencyManager->GetDependency(componentDependencies, componentDependencyCount,
                    &typeIndex, 1, clearReadFencesAfterCombining:false));

            ChunkIterationUtility.CopyFromComponentDataListAsync<T>(componentDataList, ref dynamicTypeHandle, outer,
                inputDep, out outJobHandle);
        }

        public Entity GetSingletonEntity()
        {
            if (_QueryData->HasEnableableComponents != 0)
                throw new InvalidOperationException($"Can't call GetSingletonEntity() on queries containing enableable component types.");
            GetSingletonChunkAndEntity(TypeManager.GetTypeIndex<Entity>(), out var indexInArchetype, out var chunk, out var entityIndexInChunk);
            var archetype = _Access->EntityComponentStore->GetArchetype(chunk);
            Entity* chunkEntities = (Entity*)ChunkIterationUtility.GetChunkComponentDataROPtr(archetype, chunk, 0);
            return UnsafeUtility.AsRef<Entity>(chunkEntities + entityIndexInChunk);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal void GetSingletonChunkAndEntity(TypeIndex typeIndex, out int outIndexInArchetype, out ChunkIndex outChunk, out int outEntityIndexInChunk)
        {
            if (!_Filter.RequiresMatchesFilter && _QueryData->HasEnableableComponents == 0 &&
                _QueryData->RequiredComponentsCount <= 2 && _QueryData->RequiredComponents[1].TypeIndex == typeIndex)
            {
                // Fast path with no filtering
                var matchingChunkCache = GetMatchingChunkCache();
                if (matchingChunkCache.Length != 1 || matchingChunkCache.ChunkIndices[0].Count != 1)
                {
                    _QueryData->CheckChunkListCacheConsistency(false);
                    var typeName = typeIndex.ToFixedString();
                    if (matchingChunkCache.Length == 0 || matchingChunkCache.ChunkIndices[0].Count == 0)
                        throw new InvalidOperationException($"GetSingleton<{typeName}>() requires that exactly one entity exists that match this query, but there are none. Are you missing a call to RequireForUpdate<T>()? You could also use TryGetSingleton<T>()");
                    else
                        throw new InvalidOperationException(@$"GetSingleton<{typeName}>() requires that exactly one entity exists that match this query, but there are {CalculateEntityCountWithoutFiltering()} entities in {matchingChunkCache.Length} chunks.
First chunk: entityCount={matchingChunkCache.ChunkIndices[0].Count}, archetype={_Access->EntityComponentStore->GetArchetype(matchingChunkCache.ChunkIndices[0])->ToString()}.");
                }
                outChunk = matchingChunkCache.ChunkIndices[0]; // only one matching chunk
                var matchIndex = matchingChunkCache.PerChunkMatchingArchetypeIndex->Ptr[0];
                var match = _QueryData->MatchingArchetypes.Ptr[matchIndex];
                outIndexInArchetype = match->IndexInArchetype[1];
                outEntityIndexInChunk = 0;
            }
            else
            {
                // Slow path with filtering, can't just use first matching archetype/chunk
                SyncFilterTypes();
                int queryEntityCount = ChunkIterationUtility.CalculateEntityCountAndSingleton(GetMatchingChunkCache(),
                    ref _QueryData->MatchingArchetypes, ref _Filter, _QueryData->HasEnableableComponents,
                    out MatchingArchetype* firstMatchArchetype, out outChunk, out outEntityIndexInChunk);
                if (queryEntityCount != 1)
                {
                    _QueryData->CheckChunkListCacheConsistency(false);
                    throw new InvalidOperationException(
                        $"GetSingleton() requires that exactly one entity exists that matches this query, but there are {queryEntityCount}.");
                }
                var indexInQuery = GetIndexInEntityQuery(typeIndex);
                outIndexInArchetype = firstMatchArchetype->IndexInArchetype[indexInQuery];
            }
        }

        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleComponentData) })]
        public RefRW<T> GetSingletonRW<T>() where T : unmanaged, IComponentData
        {
            var typeIndex = TypeManager.GetTypeIndex<T>();
            if (!GetIsReadWrite(GetIndexInEntityQuery(typeIndex)))
                throw new InvalidOperationException($"Can't call GetSingletonRW<{typeof(T)}>() on query where access to {typeof(T)} is not read-write.");
            if (TypeManager.IsZeroSized(typeIndex))
                throw new InvalidOperationException($"Can't call GetSingletonRW<{typeof(T)}>() with zero-size type {typeof(T)}.");
            if (TypeManager.IsEnableable(typeIndex))
                throw new InvalidOperationException($"Can't call GetSingletonRW<{typeof(T)}>() with enableable component type {typeof(T)}.");

            _Access->DependencyManager->Safety.CompleteReadAndWriteDependency(typeIndex);

            GetSingletonChunkAndEntity(typeIndex, out var indexInArchetype, out var chunk, out var entityIndexInChunk);

            var archetype = _Access->EntityComponentStore->GetArchetype(chunk);
            var data = ChunkDataUtility.GetComponentDataRW(chunk, archetype, entityIndexInChunk,
                indexInArchetype, _Access->EntityComponentStore->GlobalSystemVersion);

            return new RefRW<T>(data, _Access->DependencyManager->Safety.GetSafetyHandle(typeIndex, false));

        }

        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleComponentData) })]
        public T GetSingleton<T>() where T : unmanaged, IComponentData
        {
            var typeIndex = TypeManager.GetTypeIndex<T>();
            if (TypeManager.IsZeroSized(typeIndex))
            {
                var typeName = typeIndex.ToFixedString();
                throw new InvalidOperationException($"Can't call GetSingleton<{typeName}>() with zero-size type {typeName}.");
            }

            if (_QueryData->HasEnableableComponents != 0)
                if (TypeManager.IsEnableable(typeIndex))
                {
                    var typeName = typeIndex.ToFixedString();
                    throw new InvalidOperationException(
                        $"Can't call GetSingleton<{typeName}>() with enableable component type {typeName}.");
                }
            _Access->DependencyManager->Safety.CompleteWriteDependency(typeIndex);

            // NOTE: GetSingleton is the only singleton API that is used very very commonly.
            // It is the only place where we inline the chunk detection directly into GetSingleton method.
            // (All other singleton implementations simply use GetSingletonChunk which has the same early out)
            if (!_Filter.RequiresMatchesFilter && _QueryData->RequiredComponentsCount <= 2 && _QueryData->RequiredComponents[1].TypeIndex == typeIndex)
            {
                var matchingChunkCache = GetMatchingChunkCache();
                if (matchingChunkCache.Length != 1 || matchingChunkCache.ChunkIndices[0].Count != 1)
                {
                    _QueryData->CheckChunkListCacheConsistency(false);
                    var typeName = typeIndex.ToFixedString();
                    if (matchingChunkCache.Length == 0 || matchingChunkCache.ChunkIndices[0].Count == 0)
                        throw new InvalidOperationException($"GetSingleton<{typeName}>() requires that exactly one entity exists that match this query, but there are none. Are you missing a call to RequireForUpdate<T>()? You could also use TryGetSingleton<T>()");
                    else
                        throw new InvalidOperationException(@$"GetSingleton<{typeName}>() requires that exactly one entity exists that match this query, but there are {CalculateEntityCountWithoutFiltering()} entities in {matchingChunkCache.Length} chunks.
First chunk: entityCount={matchingChunkCache.ChunkIndices[0].Count}, archetype={_Access->EntityComponentStore->GetArchetype(matchingChunkCache.ChunkIndices[0])->ToString()}.");
                }
                var chunk = matchingChunkCache.ChunkIndices[0]; // only one matching chunk
                var matchIndex = matchingChunkCache.PerChunkMatchingArchetypeIndex->Ptr[0];
                var match = _QueryData->MatchingArchetypes.Ptr[matchIndex];
                var archetype = _Access->EntityComponentStore->GetArchetype(chunk);
                return UnsafeUtility.AsRef<T>(ChunkIterationUtility.GetChunkComponentDataROPtr(archetype, chunk, match->IndexInArchetype[1]));
            }
            else
            {
                GetSingletonChunkAndEntity(typeIndex, out var indexInArchetype, out var chunk, out var entityIndexInChunk);
                var archetype = _Access->EntityComponentStore->GetArchetype(chunk);
                T* chunkComponentValues = (T*)ChunkIterationUtility.GetChunkComponentDataROPtr(archetype, chunk, indexInArchetype);
                return UnsafeUtility.AsRef<T>(chunkComponentValues + entityIndexInChunk);
            }
        }

        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleBufferElement) })]
        public DynamicBuffer<T> GetSingletonBuffer<T>(bool isReadOnly = false) where T : unmanaged, IBufferElementData
        {
            var typeIndex = TypeManager.GetTypeIndex<T>();
            if (TypeManager.IsEnableable(typeIndex))
            {
                var typeName = typeIndex.ToFixedString();
                throw new InvalidOperationException(
                    $"Can't call GetSingletonBuffer<{typeName}>() with enableable component type {typeName}.");
            }
            if (isReadOnly)
                _Access->DependencyManager->CompleteWriteDependencyNoChecks(typeIndex);
            else
                _Access->DependencyManager->CompleteReadAndWriteDependencyNoChecks(typeIndex);

            GetSingletonChunkAndEntity(typeIndex, out var indexInArchetype, out var chunk, out var entityIndexInChunk);
            var archetype = _Access->EntityComponentStore->GetArchetype(chunk);
            var safetyHandles = &_Access->DependencyManager->Safety;
            var bufferAccessor = ChunkIterationUtility.GetChunkBufferAccessor<T>(archetype, chunk, !isReadOnly, indexInArchetype,
                _Access->EntityComponentStore->GlobalSystemVersion, safetyHandles->GetSafetyHandle(typeIndex, isReadOnly),
                safetyHandles->GetBufferSafetyHandle(typeIndex));
            return bufferAccessor[entityIndexInChunk];
        }

        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleComponentData) })]
        public void SetSingleton<T>(T value) where T : unmanaged, IComponentData
        {
            GetSingletonRW<T>().ValueRW = value;
        }

        public bool TryGetSingleton<T>(out T value)
            where T : unmanaged, IComponentData
        {
            var hasSingleton = HasSingleton<T>();
            value = hasSingleton ? GetSingleton<T>() : default;
            return hasSingleton;
        }

        public bool TryGetSingletonRW<T>(out RefRW<T> value)
            where T : unmanaged, IComponentData
        {
            var hasSingleton = HasSingleton<T>();
            value = hasSingleton ? GetSingletonRW<T>() : default;
            return hasSingleton;
        }

        public bool HasSingleton<T>()
        {
            var typeIndex = TypeManager.GetTypeIndex<T>();
            if (TypeManager.IsEnableable(typeIndex))
            {
                var typeName = typeIndex.ToFixedString();
                throw new InvalidOperationException(
                    $"Can't call HasSingleton<{typeName}>() with enableable component type {typeName}.");
            }
            int matchingEntityCount = CalculateEntityCount();
            if (Hint.Unlikely(matchingEntityCount > 1))
            {
                var typeName = typeIndex.ToFixedString();
                throw new InvalidOperationException(
                    $"HasSingleton<{typeName}>() found {matchingEntityCount} instances of {typeName}; there must only be either zero or one.");
            }
            return matchingEntityCount == 1;
        }

        public bool TryGetSingletonBuffer<T>(out DynamicBuffer<T> value, bool isReadOnly = false)
            where T : unmanaged, IBufferElementData
        {
            var hasSingleton = HasSingleton<T>();
            value = hasSingleton ? GetSingletonBuffer<T>(isReadOnly) : default;
            return hasSingleton;
        }

        public bool TryGetSingletonEntity<T>(out Entity value)
        {
            var hasSingleton = HasSingleton<T>();
            value = hasSingleton ? GetSingletonEntity() : Entity.Null;
            return hasSingleton;
        }

        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleEnableableComponent) })]
        public void SetEnabledBitsOnAllChunks(TypeIndex typeIndex, bool value)
        {
            int indexInEntityQuery = GetIndexInEntityQuery(typeIndex);
            if (indexInEntityQuery == -1)
                throw new InvalidOperationException($"Trying SetEnabledBitsOnAllChunks of {TypeManager.GetType(typeIndex)} but the required component type was not declared in the EntityQuery.");
            _Access->DependencyManager->CompleteReadAndWriteDependency(typeIndex);

            ChunkIterationUtility.SetEnabledBitsOnAllChunks(ref this, typeIndex, value, _Access->EntityComponentStore->GlobalSystemVersion);
        }

        public bool CompareQuery(in EntityQueryBuilder queryDesc)
        {
            return EntityQueryManager.CompareQuery(queryDesc, _QueryData);
        }

        [BurstDiscard]
        [BurstMonoInteropMethod]
        internal static void _ResetFilter(EntityQueryImpl* self)
        {
            var sharedCount = self->_Filter.Shared.Count;
            for (var i = 0; i < sharedCount; ++i)
                self->_Access->RemoveSharedComponentReference(self->_Filter.Shared.SharedComponentIndex[i]);

            self->_Filter.Changed.Count = 0;
            self->_Filter.Shared.Count = 0;
            self->_Filter.UseOrderFiltering = false;
        }

        [BurstDiscard]
        [BurstMonoInteropMethod]
        internal static void _FreeCachedState(EntityQueryImpl* self)
        {
            if (self->_CachedState.Target is IDisposable obj)
            {
                obj.Dispose();
            }
            self->_CachedState.Free();
        }

        public void SetSharedComponentFilterUnmanaged<SharedComponent1>(SharedComponent1 sharedComponent1)
            where SharedComponent1 : unmanaged, ISharedComponentData
        {
            ResetFilter();
            AddSharedComponentFilterUnmanaged(sharedComponent1);
        }

        public void ResetFilter()
        {
            fixed (EntityQueryImpl* self = &this)
            {
                ResetFilter(self);
            }
        }

        public void SetSharedComponentFilterUnmanaged<SharedComponent1, SharedComponent2>(SharedComponent1 sharedComponent1,
            SharedComponent2 sharedComponent2)
            where SharedComponent1 : unmanaged, ISharedComponentData
            where SharedComponent2 : unmanaged, ISharedComponentData
        {
            ResetFilter();
            AddSharedComponentFilterUnmanaged(sharedComponent1);
            AddSharedComponentFilterUnmanaged(sharedComponent2);
        }

        public void SetChangedVersionFilter(ComponentType componentType)
        {
            ResetFilter();
            AddChangedVersionFilter(componentType);
        }

        public void SetOrderVersionFilter()
        {
            ResetFilter();
            AddOrderVersionFilter();
        }

        internal void SetChangedFilterRequiredVersion(uint requiredVersion)
        {
            _Filter.RequiredChangeVersion = requiredVersion;
        }

        public void SetChangedVersionFilter(ComponentType[] componentType)
        {
            if (componentType.Length > EntityQueryFilter.ChangedFilter.Capacity)
                throw new ArgumentException(
                    $"EntityQuery.SetFilterChanged accepts a maximum of {EntityQueryFilter.ChangedFilter.Capacity} component array length");
            if (componentType.Length <= 0)
                throw new ArgumentException(
                    $"EntityQuery.SetFilterChanged component array length must be larger than 0");

            ResetFilter();
            for (var i = 0; i != componentType.Length; i++)
                AddChangedVersionFilter(componentType[i]);
        }

        public void AddChangedVersionFilter(ComponentType componentType)
        {
            var newFilterIndex = _Filter.Changed.Count;
            if (newFilterIndex >= EntityQueryFilter.ChangedFilter.Capacity)
                throw new ArgumentException($"EntityQuery accepts a maximum of {EntityQueryFilter.ChangedFilter.Capacity} changed filters.");

            _Filter.Changed.Count = newFilterIndex + 1;
            _Filter.Changed.IndexInEntityQuery[newFilterIndex] = GetIndexInEntityQuery(componentType.TypeIndex);

            _Filter.AssertValid();
        }

        public void AddSharedComponentFilterUnmanaged<SharedComponent>(SharedComponent sharedComponent)
            where SharedComponent : unmanaged, ISharedComponentData
        {
            var newFilterIndex = _Filter.Shared.Count;
            if (newFilterIndex >= EntityQueryFilter.SharedComponentData.Capacity)
                throw new ArgumentException($"EntityQuery accepts a maximum of {EntityQueryFilter.SharedComponentData.Capacity} shared component filters.");

            _Filter.Shared.Count = newFilterIndex + 1;
            _Filter.Shared.IndexInEntityQuery[newFilterIndex] = GetIndexInEntityQuery(TypeManager.GetTypeIndex<SharedComponent>());
            _Filter.Shared.SharedComponentIndex[newFilterIndex] = _Access->InsertSharedComponent_Unmanaged(sharedComponent);

            _Filter.AssertValid();
        }

        public void AddOrderVersionFilter()
        {
            _Filter.UseOrderFiltering = true;

            _Filter.AssertValid();
        }

        public void CompleteDependency()
        {
            _Access->DependencyManager->CompleteDependenciesNoChecks(_QueryData->ReaderTypes, _QueryData->ReaderTypesCount,
                _QueryData->WriterTypes, _QueryData->WriterTypesCount);
        }

        public JobHandle GetDependency()
        {
            return _Access->DependencyManager->GetDependency(_QueryData->ReaderTypes, _QueryData->ReaderTypesCount,
                _QueryData->WriterTypes, _QueryData->WriterTypesCount, clearReadFencesAfterCombining:false);
        }

        public JobHandle AddDependency(JobHandle job)
        {
            return _Access->DependencyManager->AddDependency(_QueryData->ReaderTypes, _QueryData->ReaderTypesCount,
                _QueryData->WriterTypes, _QueryData->WriterTypesCount, job);
        }

        public int GetCombinedComponentOrderVersion(bool includeEntityType)
        {
            var versionSum = 0;
            for (var i = includeEntityType ? 0 : 1; i < _QueryData->RequiredComponentsCount; ++i)
                versionSum += _Access->EntityComponentStore->GetComponentTypeOrderVersion(_QueryData->RequiredComponents[i].TypeIndex);
            return versionSum;
        }

        internal bool AddReaderWritersToLists(ref UnsafeList<TypeIndex> reading, ref UnsafeList<TypeIndex> writing)
        {
            bool anyAdded = false;
            for (int i = 0; i < _QueryData->ReaderTypesCount; ++i)
                anyAdded |= CalculateReaderWriterDependency.AddReaderTypeIndex(_QueryData->ReaderTypes[i], ref reading, ref writing);

            for (int i = 0; i < _QueryData->WriterTypesCount; ++i)
                anyAdded |= CalculateReaderWriterDependency.AddWriterTypeIndex(_QueryData->WriterTypes[i], ref reading, ref writing);
            return anyAdded;
        }

        void SyncChangeFilterTypes()
        {
            // Complete jobs that could affect an active ChangeFilter, and cause entire chunks to match (or not match) the query.
            for (int i = 0; i < _Filter.Changed.Count; ++i)
            {
                var type = _QueryData->RequiredComponents[_Filter.Changed.IndexInEntityQuery[i]];
                _Access->DependencyManager->CompleteWriteDependency(type.TypeIndex);
            }
        }
        void SyncEnableableTypes()
        {
            // Complete jobs writing to any enableable types referenced by the query, which could affect which
            // entities match the query.
            TypeIndex* enableableTypes = _QueryData->EnableableComponentTypeIndices;
            int enableableTypeCount = _QueryData->EnableableComponentTypeIndexCount;
            for (int i = 0; i < enableableTypeCount; ++i)
            {
                _Access->DependencyManager->CompleteWriteDependency(enableableTypes[i]);
            }
        }

        public bool HasFilter()
        {
            return _Filter.RequiresMatchesFilter;
        }

        public bool Debugger_GetData(List<Entity> entities, List<ArchetypeChunk> chunks)
        {
            const int kSanityArraySizes = 1024 * 10;
            if (_QueryData == null)
                return false;

            var matchingArchetypesLength = _QueryData->MatchingArchetypes.Length;
            if (matchingArchetypesLength > kSanityArraySizes)
                return false;

            for (int matchingArchetypeIndex = 0; matchingArchetypeIndex < _QueryData->MatchingArchetypes.Length; ++matchingArchetypeIndex)
            {
                var matchingArchetype = _QueryData->MatchingArchetypes.Ptr[matchingArchetypeIndex];
                var archetype = matchingArchetype->Archetype;
                if (archetype->EntityCount == 0)
                    continue;

                int chunkCount = archetype->Chunks.Count;
                if (chunkCount > kSanityArraySizes)
                    return false;

                for (int c = 0; c < chunkCount; c++)
                {
                    var chunk = archetype->Chunks[c];

                    if (!chunk.MatchesFilter(matchingArchetype, ref _Filter))
                        continue;

                    var chunkEntities = (Entity*)ChunkDataUtility.GetComponentDataRO(chunk, archetype, 0, 0);
                    ChunkIterationUtility.GetEnabledMask(chunk, matchingArchetype, out var enabledMask);

                    int entityCount = chunk.Count;
                    if (entityCount > TypeManager.MaximumChunkCapacity)
                        return false;
                    if (EnabledBitUtility.countbits(enabledMask) == 0)
                        continue; // no enabled entities in chunk

                    if (chunks != null)
                    {
                        var archetypeChunk = new ArchetypeChunk(chunk, _Access->EntityComponentStore);
                        chunks.Add(archetypeChunk);
                    }
                    int endIndex = 0;
                    while (EnabledBitUtility.TryGetNextRange(enabledMask, endIndex, out int beginIndex, out endIndex))
                    {
                        if (entities != null)
                        {
                            for (int e = beginIndex; e < endIndex; e++)
                                entities.Add(chunkEntities[e]);
                        }
                    }
                }
            }

            return true;
        }

        public EntityQueryMask GetEntityQueryMask()
        {
            var ecs = _Access->EntityComponentStore;
            var mask = _Access->EntityQueryManager->GetEntityQueryMask(_QueryData, ecs);
            return mask;
        }

        public bool Matches(Entity e)
        {
            // An EntityQueryMask gives us an early out if the entity isn't even in a matching archetype
            var ecs = _Access->EntityComponentStore;
            var mask = _QueryData->EntityQueryMask;
            if (Hint.Unlikely(!mask.IsCreated()))
            {
                mask = _Access->EntityQueryManager->GetEntityQueryMask(_QueryData, ecs);
            }
            if (!mask.MatchesIgnoreFilter(e))
                return false;

            bool hasFilter = HasFilter();
            bool hasEnableableComponents = _QueryData->HasEnableableComponents != 0;
            if (hasFilter || hasEnableableComponents)
            {
                var chunk = ecs->GetChunk(e);
                var archetype = ecs->GetArchetype(chunk);
                // TODO(DOTS-6802): most of this work could be amortized, if we knew Matches() was being called on entities in the same chunk.
                var matchingArchetype = _QueryData->MatchingArchetypes.Ptr[
                    EntityQueryManager.FindMatchingArchetypeIndexForArchetype(ref _QueryData->MatchingArchetypes, archetype)];
                // Is the chunk filtered out?
                if (hasFilter)
                {
                    SyncChangeFilterTypes();
                    if (!chunk.MatchesFilter(matchingArchetype, ref _Filter))
                        return false;
                }
                // Does the entity have all required components enabled?
                if (hasEnableableComponents)
                {
                    SyncEnableableTypes();
                    ChunkIterationUtility.GetEnabledMask(chunk, matchingArchetype, out var chunkEnabledMask);
                    var entityIndexInChunk = ecs->GetEntityInChunk(e).IndexInChunk;
                    if (entityIndexInChunk < 64)
                    {
                        if ((chunkEnabledMask.ULong0 & (1ul << entityIndexInChunk)) == 0)
                            return false;
                    }
                    else
                    {
                        if ((chunkEnabledMask.ULong1 & (1ul << (entityIndexInChunk-64))) == 0)
                            return false;
                    }
                }
            }

            return true;
        }

        public bool MatchesIgnoreFilter(Entity e)
        {
            var ecs = _Access->EntityComponentStore;
            var mask = _QueryData->EntityQueryMask;
            if (Hint.Unlikely(!mask.IsCreated()))
            {
                mask = _Access->EntityQueryManager->GetEntityQueryMask(_QueryData, ecs);
            }
            return mask.MatchesIgnoreFilter(e);
        }

        internal EntityQueryDesc[] GetEntityQueryDescs()
        {
            var outQueryDescs = new EntityQueryDesc[_QueryData->ArchetypeQueryCount];
            for (int iAQ = 0, archetypeQueryCount = _QueryData->ArchetypeQueryCount; iAQ < archetypeQueryCount; ++iAQ)
            {
                ref var archetypeQuery = ref _QueryData->ArchetypeQueries[iAQ];
                var allComponentTypes = new ComponentType[archetypeQuery.AllCount];
                for (var i = 0; i < archetypeQuery.AllCount; ++i)
                {
                    allComponentTypes[i] = new ComponentType
                    {
                        TypeIndex = archetypeQuery.All[i],
                        AccessModeType = (ComponentType.AccessMode)archetypeQuery.AllAccessMode[i]
                    };
                }

                var anyComponentTypes = new ComponentType[archetypeQuery.AnyCount];
                for (var i = 0; i < archetypeQuery.AnyCount; ++i)
                {
                    anyComponentTypes[i] = new ComponentType
                    {
                        TypeIndex = archetypeQuery.Any[i],
                        AccessModeType = (ComponentType.AccessMode)archetypeQuery.AnyAccessMode[i]
                    };
                }

                var noneComponentTypes = new ComponentType[archetypeQuery.NoneCount];
                for (var i = 0; i < archetypeQuery.NoneCount; ++i)
                {
                    noneComponentTypes[i] = new ComponentType
                    {
                        TypeIndex = archetypeQuery.None[i],
                        AccessModeType = (ComponentType.AccessMode)archetypeQuery.NoneAccessMode[i]
                    };
                }

                var disabledComponentTypes = new ComponentType[archetypeQuery.DisabledCount];
                for (var i = 0; i < archetypeQuery.DisabledCount; ++i)
                {
                    disabledComponentTypes[i] = new ComponentType
                    {
                        TypeIndex = archetypeQuery.Disabled[i],
                        AccessModeType = (ComponentType.AccessMode)archetypeQuery.DisabledAccessMode[i]
                    };
                }

                var absentComponentTypes = new ComponentType[archetypeQuery.AbsentCount];
                for (var i = 0; i < archetypeQuery.AbsentCount; ++i)
                {
                    absentComponentTypes[i] = new ComponentType
                    {
                        TypeIndex = archetypeQuery.Absent[i],
                        AccessModeType = (ComponentType.AccessMode)archetypeQuery.AbsentAccessMode[i]
                    };
                }

                var presentComponentTypes = new ComponentType[archetypeQuery.PresentCount];
                for (var i = 0; i < archetypeQuery.PresentCount; ++i)
                {
                    presentComponentTypes[i] = new ComponentType
                    {
                        TypeIndex = archetypeQuery.Present[i],
                        AccessModeType = (ComponentType.AccessMode)archetypeQuery.PresentAccessMode[i]
                    };
                }

                outQueryDescs[iAQ] = new EntityQueryDesc
                {
                    All = allComponentTypes,
                    Any = anyComponentTypes,
                    None = noneComponentTypes,
                    Disabled = disabledComponentTypes,
                    Absent = absentComponentTypes,
                    Present = presentComponentTypes,
                    Options = archetypeQuery.Options,
                };
            }
            return outQueryDescs;
        }

        internal EntityQueryDesc GetEntityQueryDesc()
        {
            return GetEntityQueryDescs()[0];
        }

        internal UnsafeCachedChunkList GetMatchingChunkCache()
        {
            // TODO(DOTS-8574): This debug check is currently too slow to enable by default.
            if (Hint.Unlikely(!_QueryData->IsChunkCacheValid()))
            {
                UpdateMatchingChunkCache();
            }
            return _QueryData->UnsafeGetMatchingChunkCache();
        }

        internal void UpdateMatchingChunkCache()
        {
            // This method is intended for tests of the chunk cache itself. Under normal operation,
            // GetMatchingChunkCache() will check if the cache is valid, and rebuild it if not.
            _QueryData->RebuildMatchingChunkCache();
        }

        internal static EntityQueryImpl* Allocate()
        {
            var memoryLabel = Memory.CreateLabel("Entities", "Query.EntityQuery", Allocator.Persistent);
            void* ptr = Memory.Unmanaged.Allocate(sizeof(EntityQueryImpl), 8, memoryLabel);
            UnsafeUtility.MemClear(ptr, sizeof(EntityQueryImpl));
            return (EntityQueryImpl*)ptr;
        }

        internal static void Free(EntityQueryImpl* impl)
        {
            var memoryLabel = Memory.CreateLabel("Entities", "Query.EntityQuery", Allocator.Persistent);
            Memory.Unmanaged.Free(impl, memoryLabel);
        }
    }

    /// <summary>
    /// Use an EntityQuery object to select entities with components that meet specific requirements.
    /// </summary>
    /// <remarks>
    /// An entity query defines the set of component types that an [archetype] must contain
    /// in order for its chunks and entities to be selected and specifies whether the components accessed
    /// through the query are read-only or read-write.
    ///
    /// For simple queries, you can create an EntityQuery based on an array of
    /// component types. The following example defines a EntityQuery that finds all entities
    /// with both Rotation and RotationSpeed components.
    ///
    /// <example>
    /// <code source="../../DocCodeSamples.Tests/EntityQueryExamples.cs" region="query-from-list" title="EntityQuery Example"/>
    /// </example>
    ///
    /// The query uses [ComponentType.ReadOnly] instead of the simpler `typeof` expression
    /// to designate that the system does not write to RotationSpeed. Always specify read-only
    /// when possible, since there are fewer constraints on read-only access to data, which can help
    /// the Job scheduler execute your Jobs more efficiently.
    ///
    /// For more complex queries, you can use an <see cref="EntityQueryDesc"/> object to create the entity query.
    /// A query description provides a flexible query mechanism to specify which archetypes to select
    /// based on the following sets of components:
    ///
    /// * `All` = All component types in this array must exist in the archetype, and must be enabled on matching entities.
    /// * `Any` = At least one of the component types in this array must exist in the archetype, and must be enabled on matching entities.
    /// * `None` = None of the component types in this array can exist in the archetype, or they must be present and disabled on matching entities.
    /// * `Disabled` = All component types in this array must exist in the archetype, and must be disabled on matching entities.
    /// * `Absent` = None of the component types in this array can exist in the archetype
    /// * `Present` = All of the component types in this array must exist in the archetype, whether or not they are enabled.
    ///
    /// For example, the following query includes archetypes containing Rotation and
    /// RotationSpeed components, but excludes any archetypes containing a Static component:
    ///
    /// <example>
    /// <code source="../../DocCodeSamples.Tests/EntityQueryExamples.cs" region="query-from-description" title="EntityQuery Example"/>
    /// </example>
    ///
    /// **Note:** Do not include completely optional components in the query description. To handle optional
    /// components, use <see cref="IJobChunk"/> and the [ArchetypeChunk.Has()] method to determine whether a chunk contains the
    /// optional component or not. Since all entities within the same chunk have the same components, you
    /// only need to check whether an optional component exists once per chunk -- not once per entity.
    ///
    /// Within a system class, use the [ComponentSystemBase.GetEntityQuery()] function
    /// to get a EntityQuery instance.
    ///
    /// You can filter entities based on
    /// whether they have [changed] or whether they have a specific value for a [shared component].
    /// Once you have created an EntityQuery object, you can
    /// [reset] and change the filter settings, but you cannot modify the base query.
    ///
    /// Use an EntityQuery for the following purposes:
    ///
    /// * To get a [native array] of the values for a specific <see cref="IComponentData"/> type for all entities matching the query
    /// * To get an [native array] of the <see cref="ArchetypeChunk"/> objects matching the query
    /// * To schedule an <see cref="IJobChunk"/> job
    /// * To control whether a system updates using [ComponentSystemBase.RequireForUpdate(query)]
    ///
    /// [Entities.ForEach]: xref:Unity.Entities.SystemBase.Entities
    /// [Entities.WithStoreEntityQueryInField]: xref:Unity.Entities.SystemBase.Entities
    /// [ComponentSystemBase.GetEntityQuery()]: xref:Unity.Entities.ComponentSystemBase.GetEntityQuery*
    /// [ComponentType.ReadOnly]: xref:Unity.Entities.ComponentType.ReadOnly``1
    /// [ComponentSystemBase.RequireForUpdate()]: xref:Unity.Entities.ComponentSystemBase.RequireForUpdate(Unity.Entities.EntityQuery)
    /// [ArchetypeChunk.Has()]: xref:Unity.Entities.ArchetypeChunk.Has``1(Unity.Entities.ComponentTypeHandle{``0})
    /// [archetype]: xref:Unity.Entities.EntityArchetype
    /// [changed]: xref:Unity.Entities.EntityQuery.SetChangedVersionFilter*
    /// [shared component]: xref:Unity.Entities.EntityQuery.SetSharedComponentFilter*
    /// [reset]: xref:Unity.Entities.EntityQuery.ResetFilter*
    /// [native array]: https://docs.unity3d.com/ScriptReference/Unity.Collections.NativeArray_1.html
    /// </remarks>
    [GenerateTestsForBurstCompatibility]
    [DebuggerTypeProxy(typeof(EntityQueryDebugView))]
    public unsafe struct EntityQuery : IDisposable, IEquatable<EntityQuery>
    {
        /// <summary>
        /// This method is called from the IJobEntity source-generation module whenever an IJobEntity instance is scheduled
        /// with a custom query. It merely tests whether the custom query 1) contains components that are used in the IJobEntity's
        /// Execute() method, and 2) have compatible read-write access modes.
        ///
        /// This method does not address less straightforward cases -- e.g. what if an IJobEntity type uses the
        /// `WithDisabled` attribute with Component A, but is scheduled with a query that specifies Component A as an `All` component?
        /// And how should we handle the case where one of them uses the `IgnoreComponentEnabledState` option? Etc. Trying to take all
        /// possible permutations into account quickly becomes extremely intractable.
        ///
        /// Another important consideration is that users expect to be able to schedule IJobEntity types with custom queries that
        /// have more relaxed constraints -- indeed, being able to do so lends IJobEntity more flexibility -- and we want to preserve
        /// this behaviour.
        ///
        /// One scenario where scheduling with a custom query is *always* guaranteed to fail is when said query does not
        /// have all the components with the required read-write access modes to run the Execute() method. In this case, the user will
        /// encounter the error message `NullReferenceException: Object reference not set to an instance of an object` during runtime,
        /// as well as various exceptions thrown from the dependency system. These error messages are confusing, frustrating, and
        /// time-consuming for users to debug.
        ///
        /// In the scenario described above, this method returns false, triggering a readable runtime exception clearly explaining to users
        /// that any custom query that is used for scheduling an `IJobEntity` instance must contain the components required for the instance's
        /// Execute() method to run.
        /// </summary>
        /// <param name="componentsUsedInExecuteMethod">All the component types that are used in the `Execute()` function of the IJobEntity
        /// instance which the current query is used to schedule.</param>
        /// <returns></returns>
        internal bool HasComponentsRequiredForExecuteMethodToRun(ref Span<ComponentType> componentsUsedInExecuteMethod)
        {
            var requiredComponentsInCurrentQuery = new NativeHashMap<int, ComponentType.AccessMode>(8, Allocator.Temp);

            var currentData = __impl->_QueryData;
            for (int i = 0; i < currentData->RequiredComponentsCount; i++)
            {
                var component = currentData->RequiredComponents[i];
                requiredComponentsInCurrentQuery.Add(component.TypeIndex, component.AccessModeType);
            }

            foreach (var executeComponent in componentsUsedInExecuteMethod)
            {
                if (!requiredComponentsInCurrentQuery.TryGetValue(executeComponent.TypeIndex, out ComponentType.AccessMode accessMode))
                    return false;

                // No need to address `case ComponentType.AccessMode.Excluded`, since all component parameters used in
                // `IJobEntity.Execute()` functions are required components.
                switch (executeComponent.AccessModeType)
                {
                    // If the Execute() function simply needs to read from Component A, then it does not matter whether
                    // the current query contains Component A with read-only or read-write access.
                    case ComponentType.AccessMode.ReadOnly:
                        if (accessMode == ComponentType.AccessMode.Exclude)
                            return false;
                        continue;
                }
            }
            requiredComponentsInCurrentQuery.Dispose();
            return true;
        }

        /// <summary>
        /// Compare two queries for equality.
        /// </summary>
        /// <param name="other">The other query</param>
        /// <returns>True if the two queries are equivalent, or false if not.</returns>
        public bool Equals(EntityQuery other)
        {
            return __impl == other.__impl;
        }

        /// <summary>
        /// Compare a query to another object (assumed to be a boxed EntityQuery).
        /// </summary>
        /// <param name="obj">The other query.</param>
        /// <returns>True if <paramref name="obj"/> is an equivalent query, or false if not.</returns>
        [ExcludeFromBurstCompatTesting("Takes managed object")]
        public override bool Equals(object obj)
        {
            return obj is EntityQuery other && Equals(other);
        }

        /// <summary>
        /// Compute the hash code for this query
        /// </summary>
        /// <returns>The hash code</returns>
        public override int GetHashCode()
        {
            return unchecked((int)(long)__impl);
        }

        static internal unsafe EntityQuery Construct(EntityQueryData* queryData, EntityDataAccess* access)
        {
            EntityQuery _result = default;
            var _ptr = EntityQueryImpl.Allocate();
            _result.__seqno = WorldUnmanaged.NextSequenceNumber.Data++;
            _ptr->Construct(queryData, access, _result.__seqno);
            _result.__impl = _ptr;
            // NOTE: The safety handle has to always be created and shouldn't be stripped using burst conditional code stripping,
            // so that creation of an EntityQuery always constructs the safety handle even when created from fully bursted code with safety turned off in the editor.
            // This way when other non bursted code accesses it, everything will still work as expected.
            _result.__safety = AtomicSafetyHandle.Create();
            return _result;
        }

        /// <summary>
        /// Reports whether this query would currently select zero entities.
        /// </summary>
        /// <returns>True, if this EntityQuery matches zero existing entities. False, if it matches one or more entities.</returns>
        public bool IsEmpty => _GetImpl()->IsEmpty;

        /// <summary>
        /// Reports whether this query would currently select zero entities. This will ignore any filters set on the EntityQuery.
        /// </summary>
        /// <returns>True, if this EntityQuery matches zero existing entities. False, if it matches one or more entities.</returns>
        public bool IsEmptyIgnoreFilter => _GetImpl()->IsEmptyIgnoreFilter;

        /// <summary>
        /// Gets the array of <see cref="ComponentType"/> objects included in this EntityQuery.
        /// </summary>
        /// <returns>An array of ComponentType objects</returns>
        [ExcludeFromBurstCompatTesting("Returns managed array")]
        internal ComponentType[] GetQueryTypes() => _GetImpl()->GetQueryTypes();

        /// <summary>
        ///     Packed array of this EntityQuery's ReadOnly and writable ComponentTypes.
        ///     ReadOnly ComponentTypes come before writable types in this array.
        /// </summary>
        /// <returns>Array of ComponentTypes</returns>
        [ExcludeFromBurstCompatTesting("Returns managed array")]
        internal ComponentType[] GetReadAndWriteTypes() => _GetImpl()->GetReadAndWriteTypes();

        /// <summary>
        /// Disposes this EntityQuery instance.
        /// </summary>
        /// <remarks>Do not dispose EntityQuery instances accessed using
        /// <see cref="ComponentSystemBase.GetEntityQuery(ComponentType[])"/>. Systems automatically dispose of
        /// their own entity queries.</remarks>
        /// <exception cref="InvalidOperationException">Thrown if you attempt to dispose an EntityQuery
        /// belonging to a system.</exception>
        public void Dispose()
        {
            var self = _GetImpl();
            self->Dispose();

            EntityQueryImpl.Free(self);

            AtomicSafetyHandle.Release(__safety);

            __impl = null;
        }

        [ExcludeFromBurstCompatTesting("Returning interface value boxes")]
        internal IDisposable _CachedState
        {
            [ExcludeFromBurstCompatTesting("Returning interface value boxes")]
            get
            {
                var impl = _GetImpl();
                if (!impl->_CachedState.IsAllocated)
                    return null;
                return (IDisposable)impl->_CachedState.Target;
            }
            [ExcludeFromBurstCompatTesting("Taking interface value boxes")]
            set
            {
                var impl = _GetImpl();
                if (!impl->_CachedState.IsAllocated)
                {
                    impl->_CachedState = GCHandle.Alloc(value);
                }
                else
                {
                    impl->_CachedState.Target = value;
                }
            }
        }

        /// <summary>
        /// Calculates the number of entities selected by this EntityQuery.
        /// </summary>
        /// <remarks>
        /// The EntityQuery must execute and apply any filters to calculate the entity count. If you are checking for whether the entity count equals zero, consider the more efficient IsEmpty property.
        /// </remarks>
        /// <returns>The number of entities based on the current EntityQuery properties.</returns>
        public int CalculateEntityCount() => _GetImpl()->CalculateEntityCount();
        /// <summary>
        /// Calculates the number of entities selected by this EntityQuery, ignoring any set filters.
        /// </summary>
        /// <remarks>
        /// The EntityQuery must execute to calculate the entity count. If you are checking for whether the entity count equals zero, consider the more efficient IsEmptyIgnoreFilter property.
        /// </remarks>
        /// <returns>The number of entities based on the current EntityQuery properties.</returns>
        public int CalculateEntityCountWithoutFiltering() => _GetImpl()->CalculateEntityCountWithoutFiltering();
        /// <summary>
        /// Calculates the number of chunks that match this EntityQuery, taking into account all active query filters and enabled components.
        /// </summary>
        /// <remarks>
        /// This count will not include chunks that do not pass any active chunk-level filters
        /// (e.g. <see cref="SetSharedComponentFilter{SharedComponent1}"/>), nor any chunks where zero entities have all
        /// the required components enabled.
        /// </remarks>
        /// <returns>The number of chunks based on the current EntityQuery properties.</returns>
        public int CalculateChunkCount() => _GetImpl()->CalculateChunkCount();
        /// <summary>
        /// Calculates the number of chunks that match this EntityQuery, ignoring any set filters.
        /// </summary>
        /// <remarks>
        /// The EntityQuery must execute to calculate the chunk count.
        /// </remarks>
        /// <returns>The number of chunks based on the current EntityQuery properties.</returns>
        public int CalculateChunkCountWithoutFiltering() => _GetImpl()->CalculateChunkCountWithoutFiltering();
        /// <summary>
        /// Generates an array which gives the index of each chunk relative to the set of chunks that currently match
        /// the query, after taking all active filtering into account.
        /// </summary>
        /// <param name="allocator">The allocator used to allocate the output array.</param>
        /// <returns>An array of integers, where array[N] is the index of chunk N among the list of
        /// chunks that match this query, once all chunk- and entity-level filtering has been applied.
        /// If chunk N is filtered out of the query, array[N] will be -1.
        /// The size of this array is given by <see cref="CalculateChunkCountWithoutFiltering"/>.</returns>
        /// <remarks>
        /// Note that the chunk index used to access the output array's elements should be relative to the full,
        /// unfiltered list of chunks matched by this query. Most commonly, this is the chunkIndex parameter available
        /// within <see cref="IJobChunk.Execute"/>. For queries with no chunk filtering and no enableable components,
        /// array[N] will equal N.
        ///
        /// This function will automatically block until any running jobs which could affect its output have completed.
        /// For a non-blocking implementation, use <see cref="CalculateFilteredChunkIndexArrayAsync"/>.
        /// </remarks>
        public NativeArray<int> CalculateFilteredChunkIndexArray(AllocatorManager.AllocatorHandle allocator) =>
            _GetImpl()->CalculateFilteredChunkIndexArray(allocator);
        /// <summary>
        /// Asynchronously generates an array which gives the index of each chunk relative to the set of chunks that
        /// currently match the query, after taking all active filtering into account.
        /// </summary>
        /// <param name="allocator">The allocator used to allocate the output array.</param>
        /// <param name="additionalInputDep">A job handle which the newly scheduled job will depend upon, in addition to
        /// the dependencies automatically determined by the component safety system.</param>
        /// <param name="outJobHandle">An `out` parameter assigned the handle to the internal job that populates the
        /// output array.</param>
        /// <returns>An array of integers, where array[N] is the index of chunk N among the list of
        /// chunks that match this query, once all chunk- and entity-level filtering has been applied.
        /// If chunk N is filtered out of the query, array[N] will be -1.
        /// The size of this array is given by <see cref="CalculateChunkCountWithoutFiltering"/>. This array's contents
        /// must not be accessed until <paramref name="outJobHandle"/> has been completed.</returns>
        /// <remarks>
        /// Note that the chunk index used to access the output array's elements should be relative to the full,
        /// unfiltered list of chunks matched by this query. Most commonly, this is the chunkIndex parameter available
        /// within <see cref="IJobChunk.Execute"/>. For queries with no chunk filtering and no enableable components,
        /// array[N] will equal N.
        ///
        /// This function will automatically insert dependencies any running jobs which could affect its output.
        /// For a blocking implementation, use <see cref="CalculateFilteredChunkIndexArray"/>.
        /// </remarks>
        public NativeArray<int> CalculateFilteredChunkIndexArrayAsync(AllocatorManager.AllocatorHandle allocator, JobHandle additionalInputDep, out JobHandle outJobHandle) =>
            _GetImpl()->CalculateFilteredChunkIndexArrayAsync(allocator, additionalInputDep, out outJobHandle); /// <summary>
        /// Generates an array containing the index of the first entity within each chunk, relative to the list of
        /// entities that match this query.
        /// </summary>
        /// <param name="allocator">The allocator used to allocate the output array.</param>
        /// <returns>An array of integers, where array[N] is the index of the first entity in chunk N among the list of
        /// entities that match this query. The size of this array is given by
        /// <see cref="CalculateChunkCountWithoutFiltering"/>.</returns>
        /// <remarks>
        /// Note that the chunk index used to access the output array's elements should be relative to the full,
        /// unfiltered list of chunks matched by this query. Most commonly, this is the chunkIndex parameter available
        /// within <see cref="IJobChunk.Execute"/>.
        ///
        /// This function will automatically block until any running jobs which could affect its output have completed.
        /// For a non-blocking implementation, use <see cref="CalculateBaseEntityIndexArrayAsync"/>.
        /// </remarks>
        public NativeArray<int> CalculateBaseEntityIndexArray(AllocatorManager.AllocatorHandle allocator) =>
            _GetImpl()->CalculateBaseEntityIndexArray(allocator);
        /// <summary>
        /// Asynchronously generates an array containing the index of the first entity within each chunk, relative to the
        /// list of entities that match this query.
        /// </summary>
        /// <param name="allocator">The allocator used to allocate the output array.</param>
        /// <param name="additionalInputDep">A job handle which the newly scheduled job will depend upon, in addition to
        /// the dependencies automatically determined by the component safety system.</param>
        /// <param name="outJobHandle">An `out` parameter assigned the handle to the internal job that populates the
        /// output array.</param>
        /// <returns>An array of integers, where array[N] is the index of the first entity in chunk N among the list of
        /// entities that match this query. The size of this array is given by
        /// <see cref="CalculateChunkCountWithoutFiltering"/>. This array's contents must not be accessed until
        /// <paramref name="outJobHandle"/> has been completed.</returns>
        /// <remarks>
        /// Note that the chunk index used to access the output array's elements should be relative to the full,
        /// unfiltered list of chunks matched by this query. Most commonly, this is the chunkIndex parameter available
        /// within <see cref="IJobChunk.Execute"/>.
        ///
        /// This function will automatically insert dependencies any running jobs which could affect its output.
        /// For a blocking implementation, use <see cref="CalculateBaseEntityIndexArray"/>.
        /// </remarks>
        public NativeArray<int> CalculateBaseEntityIndexArrayAsync(AllocatorManager.AllocatorHandle allocator, JobHandle additionalInputDep, out JobHandle outJobHandle) =>
            _GetImpl()->CalculateBaseEntityIndexArrayAsync(allocator, additionalInputDep, out outJobHandle);
        /// <summary>
        ///     Index of a ComponentType in this EntityQuery's RequiredComponents list.
        ///     For example, you have a EntityQuery that requires these ComponentTypes: ObjectPosition, ObjectVelocity, and Color.
        ///
        ///     These are their type indices (according to the TypeManager):
        ///         Position.TypeIndex == 3
        ///         Velocity.TypeIndex == 5
        ///            Color.TypeIndex == 17
        ///
        ///     RequiredComponents: [Position -> Velocity -> Color] (a linked list)
        ///     Given Velocity's TypeIndex (5), the return value would be 1, since Velocity is in slot 1 of RequiredComponents.
        /// </summary>
        /// <param name="componentType">Index of a ComponentType in the TypeManager</param>
        /// <returns>An index into RequiredComponents.</returns>
        internal int GetIndexInEntityQuery(TypeIndex componentType) => _GetImpl()->GetIndexInEntityQuery(componentType);
        /// <summary>
        /// Obsolete. Use <see cref="ToArchetypeChunkListAsync"/> instead.
        /// </summary>
        /// <remarks>
        /// **Obsolete.** Use <see cref="ToArchetypeChunkListAsync"/> instead.
        ///
        /// Use <paramref name="outJobHandle"/> as a dependency for jobs that use the returned chunk array.</remarks>
        /// <param name="allocator">Allocator to use for the array.</param>
        /// <param name="outJobHandle">An `out` parameter assigned the handle to the internal job
        /// that gathers the chunks matching this EntityQuery.
        /// </param>
        /// <returns>NativeArray of all the chunks containing entities matching this query.</returns>
        [Obsolete("This method is not actually asynchronous. Use ToArchetypeChunkListAsync() instead. (RemovedAfter Entities 1.0)")]
        public NativeArray<ArchetypeChunk> CreateArchetypeChunkArrayAsync(AllocatorManager.AllocatorHandle allocator, out JobHandle outJobHandle) => _GetImpl()->CreateArchetypeChunkArrayAsync(allocator, out outJobHandle);

        /// <summary>
        /// Asynchronously creates a list of the chunks containing entities matching this EntityQuery.
        /// </summary>
        /// <remarks>
        /// Use <paramref name="outJobHandle"/> as a dependency for jobs that use the returned chunk array.</remarks>
        /// <param name="allocator">Allocator to use for the list.</param>
        /// <param name="outJobHandle">An `out` parameter assigned the handle to the internal job
        /// that gathers the chunks matching this EntityQuery.
        /// </param>
        /// <returns>A list containing all the chunks selected by the query. The contents of this list (including
        /// the list's `Length` property) must not be accessed before <paramref name="outJobHandle"/> has been completed. To pass this list to a job
        /// that expects a <see cref="NativeArray{T}"/>, use <see cref="NativeList{T}.AsDeferredJobArray"/>.</returns>
        /// <seealso cref="IJobParallelForDefer"/>
        public NativeList<ArchetypeChunk> ToArchetypeChunkListAsync(AllocatorManager.AllocatorHandle allocator, out JobHandle outJobHandle) => _GetImpl()->ToArchetypeChunkListAsync(allocator, default(JobHandle), out outJobHandle);

        /// <summary>
        /// Asynchronously creates a list of the chunks containing entities matching this EntityQuery.
        /// </summary>
        /// <remarks>
        /// Use <paramref name="outJobHandle"/> as a dependency for jobs that use the returned chunk array.
        /// If the query contains enableable components, chunks that contain zero entities with all relevant
        /// components enabled will not be included in the output list.</remarks>
        /// <param name="allocator">Allocator to use for the list.</param>
        /// <param name="additionalInputDep">A job handle which the newly scheduled job will depend upon, in addition to
        /// the dependencies automatically determined by the component safety system.</param>
        /// <param name="outJobHandle">An `out` parameter assigned the handle to the internal job
        /// that gathers the chunks matching this EntityQuery.
        /// </param>
        /// <returns>A list containing all the chunks matched by the query. The contents of this list (including
        /// the list's `Length` property) must not be accessed before <paramref name="outJobHandle"/> has been completed. To pass this list to a job
        /// that expects a <see cref="NativeArray{T}"/>, use <see cref="NativeList{T}.AsDeferredJobArray"/>.</returns>
        /// <seealso cref="IJobParallelForDefer"/>
        public NativeList<ArchetypeChunk> ToArchetypeChunkListAsync(AllocatorManager.AllocatorHandle allocator, JobHandle additionalInputDep, out JobHandle outJobHandle) => _GetImpl()->ToArchetypeChunkListAsync(allocator, additionalInputDep, out outJobHandle);

        /// <summary>
        /// Synchronously creates an array of the chunks containing entities matching this EntityQuery.
        /// </summary>
        /// <remarks>This method blocks until the internal job that performs the query completes. If the query contains enableable
        /// components, chunks that contain zero entities with all relevant components enabled will not be included
        /// in the output list.</remarks>
        /// <param name="allocator">Allocator to use for the array.</param>
        /// <returns>A NativeArray of all the chunks in this matched by this query.</returns>
        public NativeArray<ArchetypeChunk> ToArchetypeChunkArray(AllocatorManager.AllocatorHandle allocator) => _GetImpl()->ToArchetypeChunkArray(allocator);
        /// <summary> Obsolete. Use <see cref="ToArchetypeChunkArray"/> instead.</summary>
        /// <param name="allocator">Allocator to use for the array.</param>
        /// <returns>A NativeArray of all the chunks in this matched by this query.</returns>
        [Obsolete("This method has been renamed to ToArchetypeChunkArray. (RemovedAfter Entities 1.0) (UnityUpgradable) -> ToArchetypeChunkArray(*)")]
        public NativeArray<ArchetypeChunk> CreateArchetypeChunkArray(AllocatorManager.AllocatorHandle allocator) => _GetImpl()->ToArchetypeChunkArray(allocator);

        /// <summary>
        /// Synchronously fills a list with the chunks containing entities matching this EntityQuery.
        /// </summary>
        /// <remarks>This method blocks until the internal job that performs the query completes. If the query contains enableable
        /// components, chunks that contain zero entities with all relevant components enabled will not be included
        /// in the output list.</remarks>
        /// <param name="archetypeChunkList">The destination list to be filled.</param>
        public void ToArchetypeChunkList(NativeList<ArchetypeChunk> archetypeChunkList) => _GetImpl()->ToArchetypeChunkList(archetypeChunkList);

        /// <summary>
        /// Obsolete. Use <see cref="ToEntityListAsync"/> instead.
        /// </summary>
        /// <remarks>**Obsolete.** Use <see cref="ToEntityListAsync"/> instead.
        ///
        /// Creates (and asynchronously populates) a NativeArray containing the selected entities.</remarks>
        /// <param name="allocator">The type of memory to allocate.</param>
        /// <param name="jobhandle">An `out` parameter assigned a handle that you can use as a dependency for a Job
        /// that uses the output data.</param>
        /// <returns>An array containing all the entities selected by the query. The contents of this array must not be
        /// accessed before <paramref name="jobhandle"/> has been completed.</returns>
        /// <exception cref="InvalidOperationException">Thrown in the query contains any enableable components.</exception>
        [Obsolete("This method does not correctly support enableable components, and is generally unsafe. Use ToEntityListAsync() instead. (RemovedAfter Entities 1.0)")]
        public NativeArray<Entity> ToEntityArrayAsync(AllocatorManager.AllocatorHandle allocator, out JobHandle jobhandle) => _GetImpl()->ToEntityArrayAsync(allocator, out jobhandle, this);

        /// <summary>
        /// Creates (and asynchronously populates) a NativeList containing the selected entities. Since the exact number of entities matching
        /// the query won't be known until the job runs, this method returns a <see cref="NativeList{T}"/>.
        /// </summary>
        /// <remarks>
        /// This function is provided to handle cases that require a single linear indexable array of entities which match a query.
        /// If your goal is to iterate over entities to perform a per-entity operation, consider using <see cref="IJobEntity"/> or
        /// [idiomatic foreach](xref:entities-systems-systemapi-query) to iterate over the entities directly in their chunks,
        /// instead of creating a temporary copy.
        ///
        /// The job scheduled by this call will automatically use the component safety system to determine its input dependencies,
        /// to avoid the most common race conditions. If additional input dependencies are required beyond what the component safety system
        /// knows about, use <see cref="ToEntityListAsync"/>.
        /// </remarks>
        /// <param name="allocator">The type of memory to allocate.</param>
        /// <param name="outJobHandle">An `out` parameter assigned a handle that you can use as a dependency for a Job
        /// that uses the output data.</param>
        /// <returns>A list containing all the entities selected by the query. The contents of this list (including
        /// the list's `Length` property) must not be accessed before <paramref name="outJobHandle"/> has been completed. To pass this list to a job
        /// that expects a <see cref="NativeArray{T}"/>, use <see cref="NativeList{T}.AsDeferredJobArray"/>.</returns>
        public NativeList<Entity> ToEntityListAsync(AllocatorManager.AllocatorHandle allocator, out JobHandle outJobHandle) => _GetImpl()->ToEntityListAsync(allocator, this, default(JobHandle), out outJobHandle);

        /// <summary>
        /// Creates (and asynchronously populates) a NativeList containing the selected entities. Since the exact number of entities matching
        /// the query won't be known until the job runs, this method returns a <see cref="NativeList{T}"/>.
        /// </summary>
        /// <remarks>
        /// This function is provided to handle cases that require a single linear indexable array of entities matches a query.
        /// If your goal is to iterate over entities to perform a per-entity operation, consider using <see cref="IJobEntity"/> or
        /// [idiomatic foreach](xref:entities-systems-systemapi-query) to iterate over the entities directly in their chunks,
        /// instead of creating a temporary copy.
        /// </remarks>
        /// <param name="allocator">The type of memory to allocate.</param>
        /// <param name="additionalInputDep">A job handle which the newly scheduled job will depend upon, in addition to
        /// the dependencies automatically determined by the component safety system.</param>
        /// <param name="outJobHandle">An `out` parameter assigned a handle that you can use as a dependency for a Job
        /// that uses the output data.</param>
        /// <returns>A list containing all the entities selected by the query. The contents of this list (including
        /// the list's `Length` property) must not be accessed before <paramref name="outJobHandle"/> has been completed. To pass this list to a job
        /// that expects a <see cref="NativeArray{T}"/>, use <see cref="NativeList{T}.AsDeferredJobArray"/>.</returns>
        public NativeList<Entity> ToEntityListAsync(AllocatorManager.AllocatorHandle allocator, JobHandle additionalInputDep, out JobHandle outJobHandle) => _GetImpl()->ToEntityListAsync(allocator, this, additionalInputDep, out outJobHandle);

        /// <summary>
        /// Creates a NativeArray containing the selected entities.
        /// </summary>
        /// <remarks>
        /// This function is provided to handle cases that require a single linear indexable array of entities which match a query.
        /// If your goal is to iterate over entities to perform a per-entity operation, consider using <see cref="IJobEntity"/> or
        /// [idiomatic foreach](xref:entities-systems-systemapi-query) to iterate over the entities directly in their chunks,
        /// instead of creating a temporary copy.
        ///
        /// This version of the function blocks on all registered jobs against the relevant query components.
        /// For a non-blocking variant, see <see cref="ToEntityListAsync"/>.
        /// </remarks>
        /// <param name="allocator">The type of memory to allocate.</param>
        /// <returns>An array containing all the entities selected by the EntityQuery.</returns>
        public NativeArray<Entity> ToEntityArray(AllocatorManager.AllocatorHandle allocator) => _GetImpl()->ToEntityArray(allocator, this);

        internal void GatherEntitiesToArray(out Internal.InternalGatherEntitiesResult result) => _GetImpl()->GatherEntitiesToArray(out result, this);
        internal void ReleaseGatheredEntities(ref Internal.InternalGatherEntitiesResult result) => _GetImpl()->ReleaseGatheredEntities(ref result);

        /// <summary>
        /// Obsolete. Use <see cref="ToComponentDataListAsync"/> instead.
        /// </summary>
        /// <remarks> **Obsolete.** Use <see cref="ToComponentDataListAsync"/> instead.
        ///
        /// Creates (and asynchronously populates) a NativeArray containing the value of component <typeparamref name="T"/>
        /// for the selected entities.</remarks>
        /// <param name="allocator">The type of memory to allocate.</param>
        /// <param name="jobhandle">An `out` parameter assigned a handle that you can use as a dependency for a Job
        /// that uses the output data.</param>
        /// <typeparam name="T">The component type.</typeparam>
        /// <returns>An array containing all the values of component type <typeparamref name="T"/> selected by the query.
        /// The contents of this array must not be accessed before <paramref name="jobhandle"/> has been completed.</returns>
        /// <exception cref="InvalidOperationException">Thrown in the query contains any enableable components.</exception>
        /// <exception cref="InvalidOperationException">Thrown if <typeparamref name="T"/> is not part of the query.</exception>
        [Obsolete("This method does not correctly support enableable components, and is generally unsafe. Use ToComponentDataListAsync() instead. (RemovedAfter Entities 1.0)")]
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleComponentData) })]
        public NativeArray<T> ToComponentDataArrayAsync<T>(AllocatorManager.AllocatorHandle allocator, out JobHandle jobhandle)            where T : unmanaged, IComponentData
            => _GetImpl()->ToComponentDataArrayAsync<T>(allocator, out jobhandle, this);

        /// <summary>
        /// Creates (and asynchronously populates) a NativeList containing the value of component <typeparamref name="T"/>
        /// for the selected entities. Since the exact number of entities matching the query won't be known until the
        /// job runs, this method returns a <see cref="NativeList{T}"/>.
        /// </summary>
        /// <param name="allocator">The type of memory to allocate.</param>
        /// <param name="outJobHandle">An `out` parameter assigned a handle that you can use as a dependency for a Job
        /// that uses the output data.</param>
        /// <typeparam name="T">The component type.</typeparam>
        /// <remarks>The job scheduled by this call will automatically use the component safety system to determine its input dependencies,
        /// to avoid the most common race conditions. If additional input dependencies are required beyond what the component safety system
        /// knows about, use <see cref="ToComponentDataListAsync{T}"/>.</remarks>
        /// <returns>A list containing all the values of component type <typeparamref name="T"/> selected by the query. The contents of this list (including
        /// the list's `Length` property) must not be accessed before <paramref name="outJobHandle"/> has been completed. To pass this list to a job
        /// that expects a <see cref="NativeArray{T}"/>, use <see cref="NativeList{T}.AsDeferredJobArray"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown if <typeparamref name="T"/> is not part of the query.</exception>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleComponentData) })]
        public NativeList<T> ToComponentDataListAsync<T>(AllocatorManager.AllocatorHandle allocator, out JobHandle outJobHandle)
            where T : unmanaged, IComponentData
            => _GetImpl()->ToComponentDataListAsync<T>(allocator, this, default(JobHandle), out outJobHandle);

        /// <summary>
        /// Creates (and asynchronously populates) a NativeList containing the value of component <typeparamref name="T"/>
        /// for the selected entities. Since the exact number of entities matching the query won't be known until the
        /// job runs, this method returns a <see cref="NativeList{T}"/>.
        /// </summary>
        /// <param name="allocator">The type of memory to allocate.</param>
        /// <param name="additionalInputDep">A job handle which the newly scheduled job will depend upon, in addition to
        /// the dependencies automatically determined by the component safety system.</param>
        /// <param name="outJobHandle">An `out` parameter assigned a handle that you can use as a dependency for a Job
        /// that uses the output data.</param>
        /// <typeparam name="T">The component type.</typeparam>
        /// <returns>A list containing all the values of component type <typeparamref name="T"/> selected by the query. The contents of this list (including
        /// the list's `Length` property) must not be accessed before <paramref name="outJobHandle"/> has been completed. To pass this list to a job
        /// that expects a <see cref="NativeArray{T}"/>, use <see cref="NativeList{T}.AsDeferredJobArray"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown if <typeparamref name="T"/> is not part of the query.</exception>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleComponentData) })]
        public NativeList<T> ToComponentDataListAsync<T>(AllocatorManager.AllocatorHandle allocator, JobHandle additionalInputDep, out JobHandle outJobHandle)
            where T : unmanaged, IComponentData
            => _GetImpl()->ToComponentDataListAsync<T>(allocator, this, additionalInputDep, out outJobHandle);

        /// <summary>
        /// Creates a NativeArray containing the components of type T for the selected entities.
        /// </summary>
        /// <param name="allocator">The type of memory to allocate.</param>
        /// <typeparam name="T">The component type.</typeparam>
        /// <returns>An array containing the specified component for all the entities selected
        /// by the EntityQuery.</returns>
        /// <exception cref="InvalidOperationException">Thrown if you request a component that is not part of
        /// the group.</exception>
        /// <remarks>This version of the function blocks on all registered jobs against the relevant query components.
        /// For a non-blocking variant, see <see cref="ToComponentDataListAsync{T}"/></remarks>
        /// <exception cref="InvalidOperationException">Thrown if <typeparamref name="T"/> is not part of the query.</exception>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleComponentData) })]
        public NativeArray<T> ToComponentDataArray<T>(AllocatorManager.AllocatorHandle allocator)            where T : unmanaged, IComponentData
            => _GetImpl()->ToComponentDataArray<T>(allocator, this);

        /// <summary>
        /// Copies the values of component type <typeparamref name="T"/> in a NativeArray into the entities matched by this query.
        /// </summary>
        /// <param name="componentDataArray">The values to copy into the matching entities.</param>
        /// <typeparam name="T">The component type.</typeparam>
        /// <remarks>This version of the function blocks on all registered jobs that access any of the query components.
        /// For a non-blocking variant, see <see cref="CopyFromComponentDataListAsync{T}(Unity.Collections.NativeList{T},out Unity.Jobs.JobHandle)"/></remarks>
        /// <exception cref="InvalidOperationException">Thrown if <typeparamref name="T"/> is not part of the query.</exception>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleComponentData) })]
        public void CopyFromComponentDataArray<T>(NativeArray<T> componentDataArray)            where T : unmanaged, IComponentData
            => _GetImpl()->CopyFromComponentDataArray<T>(componentDataArray, this);

        /// <summary>
        /// Obsolete. Use <see cref="CopyFromComponentDataListAsync"/> instead. </summary>
        /// <param name="componentDataArray">The values to copy into the matching entities.</param>
        /// <param name="jobHandle">An `out` parameter assigned a handle that you can use as a dependency for a Job
        /// that should happen after this operation completes.</param>
        /// <typeparam name="T">The component type.</typeparam>
        /// <remarks>**Obsolete.** Use <see cref="CopyFromComponentDataListAsync"/> instead.
        ///
        /// Asynchronously copies the values of component type <typeparamref name="T"/> in a NativeArray into the entities
        /// matched by this query.
        /// This method is generally used in conjunction with <see cref="ToComponentDataArrayAsync{T}"/> to extract component values,
        /// pass them into some code that expects a flat array of values, and then scatter the updated values back to entities.</remarks>
        /// <exception cref="InvalidOperationException">Thrown in the query contains any enableable components.</exception>
        /// <exception cref="InvalidOperationException">Thrown if <typeparamref name="T"/> is not part of the query.</exception>
        [Obsolete("This method does not correctly support enableable components, and is generally unsafe. Use CopyFromComponentDataListAsync() instead. (RemovedAfter Entities 1.0)")]
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleComponentData) })]
        public void CopyFromComponentDataArrayAsync<T>(NativeArray<T> componentDataArray, out JobHandle jobHandle)
            where T : unmanaged, IComponentData
            => _GetImpl()->CopyFromComponentDataArrayAsync<T>(componentDataArray, out jobHandle, this);

        /// <summary>
        /// Asynchronously copies the values of component type <typeparamref name="T"/> in a NativeList into the entities
        /// matched by this query.</summary>
        /// <param name="componentDataList">The values to copy into the matching entities.</param>
        /// <param name="outJobHandle">An `out` parameter assigned a handle that you can use as a dependency for a Job
        /// that should happen after this operation completes.</param>
        /// <typeparam name="T">The component type.</typeparam>
        /// <remarks>The job scheduled by this call will automatically use the component safety system to determine its input dependencies,
        /// to avoid the most common race conditions. If additional input dependencies are required beyond what the component safety system
        /// knows about, use <see cref="CopyFromComponentDataListAsync{T}(Unity.Collections.NativeList{T},Unity.Jobs.JobHandle,out Unity.Jobs.JobHandle)"/>.</remarks>
        /// <remarks>This method is generally used in conjunction with <see cref="ToComponentDataListAsync{T}"/> to extract component values,
        /// pass them into some code that expects a flat array of values, and then scatter the updated values back to entities.</remarks>
        /// <exception cref="InvalidOperationException">Thrown if <typeparamref name="T"/> is not part of the query.</exception>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleComponentData) })]
        public void CopyFromComponentDataListAsync<T>(NativeList<T> componentDataList, out JobHandle outJobHandle)
            where T : unmanaged, IComponentData
            => _GetImpl()->CopyFromComponentDataListAsync<T>(componentDataList, this, default(JobHandle), out outJobHandle);

        /// <summary>
        /// Asynchronously copies the values of component type <typeparamref name="T"/> in a NativeList into the entities
        /// matched by this query.</summary>
        /// <param name="componentDataList">The values to copy into the matching entities.</param>
        /// <param name="additionalInputDep">A job handle which the newly scheduled job will depend upon, in addition to
        /// the dependencies automatically determined by the component safety system.</param>
        /// <param name="outJobHandle">An `out` parameter assigned a handle that you can use as a dependency for a Job
        /// that should happen after this operation completes.</param>
        /// <typeparam name="T">The component type.</typeparam>
        /// <remarks>This method is generally used in conjunction with <see cref="ToComponentDataListAsync{T}"/> to extract component values,
        /// pass them into some code that expects a flat array of values, and then scatter the updated values back to entities.</remarks>
        /// <exception cref="InvalidOperationException">Thrown if <typeparamref name="T"/> is not part of the query.</exception>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleComponentData) })]
        public void CopyFromComponentDataListAsync<T>(NativeList<T> componentDataList, JobHandle additionalInputDep, out JobHandle outJobHandle)
            where T : unmanaged, IComponentData
            => _GetImpl()->CopyFromComponentDataListAsync<T>(componentDataList, this, additionalInputDep, out outJobHandle);

        /// <summary>
        /// Attempts to retrieve the single entity that this query matches.
        /// </summary>
        /// <returns>The only entity matching this query.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the number of entities that match this query is not exactly one.</exception>
        /// <exception cref="InvalidOperationException">Thrown the query references any enableable components.</exception>
        public Entity GetSingletonEntity() => _GetImpl()->GetSingletonEntity();

        /// <summary>
        /// Gets the value of a singleton component. Note that if querying a singleton component from a system-associated entity,
        /// the query must include either EntityQueryOptions.IncludeSystems or the SystemInstance component.
        /// </summary>
        /// <remarks>A singleton component is a component of which only one instance exists that satisfies this query.</remarks>
        /// <typeparam name="T">The component type.</typeparam>
        /// <returns>A copy of the singleton component.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the number of entities that match this query is not exactly one.</exception>
        /// <seealso cref="SetSingleton{T}(T)"/>
        /// <seealso cref="GetSingletonEntity"/>
        /// <seealso cref="GetSingletonBuffer"/>
        /// <seealso cref="ComponentSystemBase.GetSingleton{T}"/>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleComponentData) })]
        public T GetSingleton<T>() where T : unmanaged, IComponentData
            => _GetImpl()->GetSingleton<T>();

        /// <summary>
        /// Gets a reference to a singleton component, for read/write access. Note that if querying a singleton component from a
        /// system-associated entity, the query must include either EntityQueryOptions.IncludeSystems or the SystemInstance component.
        /// </summary>
        /// <remarks>
        /// A singleton component is a component of which only one instance exists that satisfies this query.
        ///
        /// The reference refers directly to the singleton's component memory. Structural changes to the chunk where the singleton
        /// resides can invalidate this reference and result in crashes or undefined behaviour if the reference is used after
        /// structural changes.
        ///
        /// If safety checks are enabled, this method throws an exception if a job that reads or writes <typeparamref name="T"/> is
        /// still running, whereas <see cref="GetSingleton{T}"/> throws only if a job that writes <typeparamref name="T"/> is still
        /// running. Neither method completes the conflicting job for you. This method also increments the change version of
        /// <typeparamref name="T"/>, even if you don't write to the returned reference, so queries that filter on changes to
        /// <typeparamref name="T"/> match the singleton entity.
        /// </remarks>
        /// <typeparam name="T">The component type.</typeparam>
        /// <returns>A reference to the singleton component.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the number of entities that match this query is not exactly one.</exception>
        /// <exception cref="InvalidOperationException">Thrown if this query doesn't have read-write access to
        /// <typeparamref name="T"/>, if <typeparamref name="T"/> is a zero-sized component, or if <typeparamref name="T"/>
        /// implements <see cref="IEnableableComponent"/>, and if safety checks are enabled.</exception>
        /// <seealso cref="GetSingleton{T}"/>
        /// <seealso cref="SetSingleton{T}(T)"/>
        /// <seealso cref="GetSingletonEntity"/>
        /// <seealso cref="GetSingletonBuffer{T}"/>
        /// <seealso cref="ComponentSystemBase.GetSingleton{T}"/>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleComponentData) })]
        public RefRW<T> GetSingletonRW<T>() where T : unmanaged, IComponentData
            => _GetImpl()->GetSingletonRW<T>();

        /// <summary>
        /// Gets the value of a singleton component, and returns whether or not a singleton component of the specified type matches inside the <see cref="EntityQuery"/>.
        /// Note that if querying a singleton component from a system-associated entity, the query must include either EntityQueryOptions.IncludeSystems or the SystemInstance component.
        /// </summary>
        /// <typeparam name="T">The <see cref="IComponentData"/> subtype of the singleton component.
        /// This component type must not implement <see cref="IEnableableComponent"/></typeparam>
        /// <param name="value">The component. if an <see cref="Entity"/> with the specified type does not exist in the <see cref="World"/>, this is assigned a default value</param>
        /// <returns>True, if exactly one <see cref="Entity"/> exists in the <see cref="World"/> with the provided component type.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the number of entities that match this query is greater than one.</exception>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleComponentData) })]
        public bool TryGetSingleton<T>(out T value)
            where T : unmanaged, IComponentData
            => _GetImpl()->TryGetSingleton(out value);

        /// <summary>
        /// Gets a reference to the value of a singleton component, and returns whether or not a singleton component of the specified type matches inside the <see cref="EntityQuery"/>.
        /// Note that if querying a singleton component from a system-associated entity, the query must include either EntityQueryOptions.IncludeSystems or the SystemInstance component.
        /// </summary>
        /// <remarks>A singleton component is a component of which only one instance exists that satisfies this query.</remarks>
        /// <typeparam name="T">The component type.</typeparam>
        /// <param name="value">The reference of the component</param>
        /// <returns>A reference to the singleton component.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the number of entities that match this query is greater than one.</exception>
        /// <seealso cref="GetSingletonRW{T}"/>
        /// <seealso cref="SetSingleton{T}(T)"/>
        /// <seealso cref="GetSingletonEntity"/>
        /// <seealso cref="GetSingletonBuffer{T}"/>
        /// <seealso cref="ComponentSystemBase.GetSingleton{T}"/>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleComponentData) })]
        public bool TryGetSingletonRW<T>(out RefRW<T> value) where T : unmanaged, IComponentData
            => _GetImpl()->TryGetSingletonRW<T>(out value);

        /// <summary>
        /// Checks whether a singleton component of the specified type exists. Note that if querying a singleton component from a system-associated entity,
        /// the query must include either EntityQueryOptions.IncludeSystems or the SystemInstance component.
        /// </summary>
        /// <remarks>
        /// This method matches only the entities that this query matches, so the query's <see cref="EntityQueryOptions"/> determine
        /// whether a singleton entity is found. Unless the query uses <see cref="EntityQueryOptions.IncludeDisabledEntities"/>
        /// or <see cref="EntityQueryOptions.IncludePrefab"/>, entities that have the <see cref="Disabled"/> component or the
        /// <see cref="Prefab"/> component are excluded, and this method returns false for a singleton on one of those entities.
        /// </remarks>
        /// <typeparam name="T">The <see cref="IComponentData"/> subtype of the singleton component.
        /// This component type must not implement <see cref="IEnableableComponent"/></typeparam>
        /// <returns>True, if a singleton is found to match exactly once with the specified type<see cref="EntityQuery"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown if <typeparamref name="T"/> implements
        /// <see cref="IEnableableComponent"/> and if safety checks are enabled.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the number of entities that match this query is greater than one.</exception>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleComponentData) })]
        public bool HasSingleton<T>()
            => _GetImpl()->HasSingleton<T>();

        /// <summary>
        /// Gets the value of a singleton buffer component, and returns whether or not a singleton buffer component of the specified type exists in the <see cref="World"/>.
        /// Note that if querying a singleton buffer component from a system-associated entity, the query must include either EntityQueryOptions.IncludeSystems or the SystemInstance component.
        /// </summary>
        /// <typeparam name="T">The <see cref="IBufferElementData"/> subtype of the singleton buffer component.
        /// This component type must not implement <see cref="IEnableableComponent"/></typeparam>
        /// <param name="value">The buffer. if an <see cref="Entity"/> with the specified type does not exist in the <see cref="World"/>, this is assigned a default value</param>
        /// <param name="isReadOnly">Whether the buffer data is read-only or not. Set to false by default.</param>
        /// <returns>True, if exactly one <see cref="Entity"/> matches the <see cref="EntityQuery"/> with the provided component type.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the number of entities that match this query is greater than one.</exception>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleBufferElement) })]
        public bool TryGetSingletonBuffer<T>(out DynamicBuffer<T> value, bool isReadOnly = false)
            where T : unmanaged, IBufferElementData
            => _GetImpl()->TryGetSingletonBuffer(out value, isReadOnly);

        /// <summary>
        /// Gets the singleton Entity, and returns whether or not a singleton <see cref="Entity"/> of the specified type exists in the <see cref="World"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="IComponentData"/> subtype of the singleton component.
        /// This component type must not implement <see cref="IEnableableComponent"/></typeparam>
        /// <param name="value">The <see cref="Entity"/> associated with the specified singleton component.
        ///  If a singleton of the specified types does not exist in the current <see cref="World"/>, this is set to Entity.Null</param>
        /// <returns>True, if exactly one <see cref="Entity"/> matches the <see cref="EntityQuery"/> with the provided component type.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the number of entities that match this query is greater than one.</exception>
        /// <exception cref="InvalidOperationException">Thrown the query references any enableable components.</exception>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleComponentData) })]
        public bool TryGetSingletonEntity<T>(out Entity value)
            => _GetImpl()->TryGetSingletonEntity<T>(out value);

        /// <summary>
        /// Gets the value of a singleton buffer component. Note that if querying a singleton buffer component from a system-associated entity,
        /// the query must include either EntityQueryOptions.IncludeSystems or the SystemInstance component.
        /// </summary>
        /// <remarks>A singleton buffer component is a component of which only one instance exists that satisfies this query.
        /// There is no SetSingletonBuffer(); to change the contents of a singleton buffer, pass isReadOnly=false to GetSingletonBuffer()
        /// and then modify the contents directly.</remarks>
        /// <typeparam name="T">The buffer element type.</typeparam>
        /// <param name="isReadOnly">If the caller does not need to modify the buffer contents, pass true here.</param>
        /// <returns>The singleton buffer.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the number of entities that match this query is not exactly one.</exception>
        /// <seealso cref="GetSingletonEntity"/>
        /// <seealso cref="ComponentSystemBase.GetSingleton{T}"/>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleBufferElement) })]
        public DynamicBuffer<T> GetSingletonBuffer <T>(bool isReadOnly = false) where T : unmanaged, IBufferElementData
            => _GetImpl()->GetSingletonBuffer<T>(isReadOnly);

        /// <summary>
        /// Sets the value of a singleton component. Note that if querying a singleton component from a system-associated entity,
        /// the query must include either EntityQueryOptions.IncludeSystems or the SystemInstance component.
        /// </summary>
        /// <remarks>
        /// For a component to be a singleton, there can be only one instance of that component
        /// that satisfies this query.
        ///
        /// **Note:** singletons are otherwise normal entities. The EntityQuery and <see cref="ComponentSystemBase"/>
        /// singleton functions add checks that you have not created two instances of a
        /// type that can be accessed by this singleton query, but other APIs do not prevent such accidental creation.
        ///
        /// To create a singleton, create an entity with the singleton component.
        ///
        /// For example, if you had a component defined as:
        ///
        /// <example>
        /// <code lang="csharp" source="../../DocCodeSamples.Tests/EntityQueryExamples.cs" region="singleton-type-example" title="Singleton"/>
        /// </example>
        ///
        /// You could create a singleton as follows:
        ///
        /// <example>
        /// <code lang="csharp" source="../../DocCodeSamples.Tests/EntityQueryExamples.cs" region="create-singleton" title="Create Singleton"/>
        /// </example>
        ///
        /// To update the singleton component after creation, you can use an EntityQuery object that
        /// selects the singleton entity and call this `SetSingleton()` function:
        ///
        /// <example>
        /// <code lang="csharp" source="../../DocCodeSamples.Tests/EntityQueryExamples.cs" region="set-singleton" title="Set Singleton"/>
        /// </example>
        ///
        /// You can set and get the singleton value from a system: see <seealso cref="ComponentSystemBase.SetSingleton{T}(T)"/>
        /// and <seealso cref="ComponentSystemBase.GetSingleton{T}"/>.
        /// </remarks>
        /// <param name="value">An instance of type T containing the values to set.</param>
        /// <typeparam name="T">The component type.</typeparam>
        /// <exception cref="InvalidOperationException">Thrown if more than one instance of this component type
        /// exists in the world or the component type appears in more than one archetype.</exception>
        /// <seealso cref="GetSingleton{T}"/>
        /// <seealso cref="GetSingletonEntity"/>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleComponentData) })]
        public void SetSingleton<T>(T value) where T : unmanaged, IComponentData
            => _GetImpl()->SetSingleton<T>(value);

        /// <summary>
        /// Obsolete. Use <see cref="EntityManager.SetComponentEnabled{T}(Unity.Entities.EntityQuery,bool)"/> instead.
        /// </summary>
        /// <typeparam name="T">The component type which should be enabled or disabled on all matching chunks. This type
        /// must be included in the query's required types, and must implement <see cref="IEnableableComponent"/>.</typeparam>
        /// <param name="value">If true, the component <typeparamref name="T"/> will be enabled on all entities in all
        /// matching chunks. Otherwise, the component will be disabled on all components in all chunks.</param>
        /// <remarks>**Obsolete.** Use <see cref="EntityManager.SetComponentEnabled{T}(Unity.Entities.EntityQuery,bool)"/> instead.
        ///
        /// Sets or clears the "is enabled" bit for the provided component on all entities in all chunks matched by the query.
        /// The current value of the bits are ignored; this function will enable disabled components on
        /// entities, even if the component being disabled would cause the entity to not match the query. If any jobs
        /// are currently running which read or write the target component, this function will block until they complete
        /// before performing the requested operation.</remarks>
        [Obsolete("This method has been deprecated. Use EntityManager.SetComponentEnabled<T>(Unity.Entities.EntityQuery, bool) instead. (RemovedAfter Entities 1.0)")]
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleEnableableComponent) })]
        public void SetEnabledBitsOnAllChunks<T>(bool value) where T : struct, IEnableableComponent
            => _GetImpl()->SetEnabledBitsOnAllChunks(TypeManager.GetTypeIndex<T>(), value);

        /// <summary>
        /// Obsolete. Use <see cref="CompareQuery"/> instead.
        /// </summary>
        /// <remarks>**Obsolete.** Use <see cref="CompareQuery"/> instead.
        ///
        /// Compares a list of component types to the types defining this EntityQuery.
        /// Only required types in the query are used as the basis for the comparison.
        /// If you include types that the query excludes or only includes as optional,
        /// the comparison returns false.</remarks>
        /// <param name="componentTypes">An array of ComponentType objects.</param>
        /// <returns>True, if the list of types, including any read/write access specifiers,
        /// matches the list of required component types of this EntityQuery.</returns>
        [Obsolete("This method is not Burst-compatible. Use CompareQuery(in EntityQueryBuilder queryDesc) instead. (RemovedAfter Entities 1.0)")]
        public bool CompareComponents(ComponentType[] componentTypes)
        {
            fixed (ComponentType* types = componentTypes)
            {
                var builder = new EntityQueryBuilder(Allocator.Temp, types, componentTypes.Length);
                builder.FinalizeQueryInternal();
                return _GetImpl()->CompareQuery(builder);
            }
        }

        /// <summary>
        /// Obsolete. Use <see cref="CompareQuery"/> instead.
        /// </summary>
        /// <remarks>**Obsolete.** Use <see cref="CompareQuery"/> instead.
        ///
        /// Compares a list of component types to the types defining this EntityQuery.
        /// Only required types in the query are used as the basis for the comparison.
        /// If you include types that the query excludes or only includes as optional,
        /// the comparison returns false. Do not include the <see cref="Entity"/> type, which
        /// is included implicitly.</remarks>
        /// <param name="componentTypes">An array of ComponentType objects.</param>
        /// <returns>True, if the list of types, including any read/write access specifiers,
        /// matches the list of required component types of this EntityQuery.</returns>
        [Obsolete("Use CompareQuery(in EntityQueryBuilder queryDesc) instead. (RemovedAfter Entities 1.0)")]
        public bool CompareComponents(NativeArray<ComponentType> componentTypes)
        {
            var builder = new EntityQueryBuilder(Allocator.Temp, (ComponentType*)componentTypes.GetUnsafeReadOnlyPtr(), componentTypes.Length);
            builder.FinalizeQueryInternal();
            return _GetImpl()->CompareQuery(builder);
        }

        /// <summary>
        /// Compares a query description to the description defining this EntityQuery.
        /// </summary>
        /// <remarks>The `All`, `Any`, and `None` components in the query description are
        /// compared to the corresponding list in this EntityQuery.</remarks>
        /// <param name="queryDesc">The query description to compare.</param>
        /// <returns>True, if the query description contains the same components with the same
        /// read/write access modifiers as this EntityQuery.</returns>
        public bool CompareQuery(in EntityQueryBuilder queryDesc) => _GetImpl()->CompareQuery(queryDesc);
        /// <summary>
        /// Resets this EntityQuery's chunk filter settings to the default (all filtering disabled).
        /// </summary>
        /// <remarks>
        /// Removes references to shared component data, if applicable.
        /// </remarks>
        public void ResetFilter() => _GetImpl()->ResetFilter();

        /// <summary>
        /// Filters this EntityQuery so that it only selects entities with shared component of type <typeparamref name="SharedComponent"/>
        /// equal to <paramref name="sharedComponent"/>.
        /// </summary>
        /// <remarks>
        /// This call disables any existing chunk filtering on this query. For additive filtering, use <see cref="AddSharedComponentFilter{SharedComponent}(SharedComponent)"/>.
        /// </remarks>
        /// <param name="sharedComponent">The shared component value to filter.</param>
        /// <typeparam name="SharedComponent">The type of unmanaged shared component. This type must also be
        /// one of the types used to create the EntityQuery.</typeparam>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleSharedComponentData) })]
        public void SetSharedComponentFilter<SharedComponent>(SharedComponent sharedComponent)
            where SharedComponent : unmanaged, ISharedComponentData
            => _GetImpl()->SetSharedComponentFilterUnmanaged<SharedComponent>(sharedComponent);

        /// <summary>
        /// Filters this EntityQuery based on the values of two separate unmanaged shared components.
        /// </summary>
        /// <remarks>
        /// The filter only selects entities which have both a shared component of type <typeparamref name="SharedComponent1"/>
        /// whose value equals <paramref name="sharedComponent1"/> and a shared component of type
        /// <typeparamref name="SharedComponent2"/> whose value equals <paramref name="sharedComponent2"/>.
        /// This call disables any existing chunk filtering on this query. For additive filtering, use <see cref="AddSharedComponentFilter{SharedComponent}(SharedComponent)"/>.
        /// </remarks>
        /// <param name="sharedComponent1">Shared component value to filter.</param>
        /// <param name="sharedComponent2">Shared component value to filter.</param>
        /// <typeparam name="SharedComponent1">The type of shared component. This type must also be
        /// one of the types used to create the EntityQuery.</typeparam>
        /// <typeparam name="SharedComponent2">The type of shared component. This type must also be
        /// one of the types used to create the EntityQuery.</typeparam>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleSharedComponentData), typeof(BurstCompatibleSharedComponentData) })]
        public void SetSharedComponentFilter<SharedComponent1, SharedComponent2>(SharedComponent1 sharedComponent1,
            SharedComponent2 sharedComponent2)
            where SharedComponent1 : unmanaged, ISharedComponentData
            where SharedComponent2 : unmanaged, ISharedComponentData
            => _GetImpl()->SetSharedComponentFilterUnmanaged<SharedComponent1, SharedComponent2>(sharedComponent1, sharedComponent2);
        /// <summary>
        /// Filters out entities in chunks for which the specified component has not changed.
        /// </summary>
        /// <remarks>
        /// Saves a given ComponentType's index in RequiredComponents in this group's Changed filter.
        /// This call disables any existing chunk filtering on this query. For additive filtering, use <see cref="AddChangedVersionFilter"/>.
        /// </remarks>
        /// <param name="componentType">ComponentType to mark as changed on this EntityQuery's filter.</param>
        public void SetChangedVersionFilter(ComponentType componentType) => _GetImpl()->SetChangedVersionFilter(componentType);
        internal void SetChangedFilterRequiredVersion(uint requiredVersion) => _GetImpl()->SetChangedFilterRequiredVersion(requiredVersion);

        /// <summary>
        /// Filters out entities in chunks for which the specified component has not changed.
        /// </summary>
        /// <remarks>
        /// Saves a given ComponentType's index in RequiredComponents in this group's Changed filter.
        /// This call disables any existing chunk filtering on this query. For additive filtering, use <see cref="AddChangedVersionFilter"/>.
        /// </remarks>
        /// <param name="componentType">ComponentTypes to mark as changed on this EntityQuery's filter.</param>
        [ExcludeFromBurstCompatTesting("Takes managed array")]
        public void SetChangedVersionFilter(ComponentType[] componentType) => _GetImpl()->SetChangedVersionFilter(componentType);

        /// <summary>
        /// Filters out entities in chunks for which the specified component has not changed. Additive with other filter functions.
        /// </summary>
        /// <remarks>
        /// Saves a given ComponentType's index in RequiredComponents in this group's Changed filter.
        /// </remarks>
        /// <param name="componentType">ComponentType to mark as changed on this EntityQuery's filter.</param>
        public void AddChangedVersionFilter(ComponentType componentType) => _GetImpl()->AddChangedVersionFilter(componentType);

        /// <summary>
        /// Filters this EntityQuery so that it only selects entities with an unmanaged shared component of type <typeparamref name="SharedComponent"/>
        /// equal to <paramref name="sharedComponent"/>. Additive with other filter functions.
        /// </summary>
        /// <param name="sharedComponent">The unmanaged shared component value to filter.</param>
        /// <typeparam name="SharedComponent">The type of shared component. This type must also be
        /// one of the types used to create the EntityQuery.</typeparam>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleSharedComponentData) })]
        public void AddSharedComponentFilter<SharedComponent>(SharedComponent sharedComponent)
            where SharedComponent : unmanaged, ISharedComponentData
            => _GetImpl()->AddSharedComponentFilterUnmanaged<SharedComponent>(sharedComponent);

        /// <summary>
        /// Filters out entities in chunks for which no structural changes have occurred.
        /// </summary>
        /// <remarks>
        /// This call disables any existing chunk filtering on this query. For additive filtering, use <see cref="AddOrderVersionFilter"/>.
        /// </remarks>
        public void SetOrderVersionFilter() => _GetImpl()->SetOrderVersionFilter();

        /// <summary>
        /// Filters out entities in chunks for which no structural changes have occurred. Additive with other filter functions.
        /// </summary>
        public void AddOrderVersionFilter() => _GetImpl()->AddOrderVersionFilter();
        /// <summary>
        /// Ensures all jobs running on this EntityQuery complete.
        /// </summary>
        /// <remarks>An entity query uses jobs internally when required to create arrays of
        /// entities and chunks. This function completes those jobs and returns when they are finished.
        /// </remarks>
        public void CompleteDependency() => _GetImpl()->CompleteDependency();
        /// <summary>
        /// Combines all dependencies in this EntityQuery into a single JobHandle.
        /// </summary>
        /// <remarks>An entity query uses jobs internally when required to create arrays of
        /// entities and chunks.</remarks>
        /// <returns>JobHandle that represents the combined dependencies of this EntityQuery</returns>
        public JobHandle GetDependency() => _GetImpl()->GetDependency();
        /// <summary>
        /// Adds another job handle to this EntityQuery's dependencies.
        /// </summary>
        /// <remarks>An entity query uses jobs internally when required to create arrays of
        /// entities and chunks. This junction adds an external job as a dependency for those
        /// internal jobs.</remarks>
        /// <param name="job">Handle for the job to add to the query's dependencies.</param>
        /// <returns>The new combined job handle for the query's dependencies.</returns>
        public JobHandle AddDependency(JobHandle job) => _GetImpl()->AddDependency(job);
        /// <summary>
        /// This method is obsolete. Use <see cref="EntityManager.GetComponentOrderVersion{T}()"/> instead.
        /// </summary>
        /// <returns>Returns the sum of the order versions of all components required by this query, including the
        /// <see cref="Entity"/> component type implicitly required by all queries.</returns>
        [Obsolete("This function is deprecated, and will be removed in a future release. Use the new variant instead, with includeEntityType set to true.")]
        public int GetCombinedComponentOrderVersion() => _GetImpl()->GetCombinedComponentOrderVersion(true);
        /// <summary>
        /// This method computes the sum of the order versions for all components required by this query.
        /// </summary>
        /// <param name="includeEntityType">If true, the implicit <see cref="Entity"/> component implicitly required by all
        /// queries is included for the purposes of this calculation. This is not generally desirable, as adding or removing
        /// entities to any archetype would affect the combined order versions of all queries.</param>
        /// <returns>Returns the sum of the order versions of all components required by this query.</returns>
        public int GetCombinedComponentOrderVersion(bool includeEntityType) => _GetImpl()->GetCombinedComponentOrderVersion(includeEntityType);
        internal bool AddReaderWritersToLists(ref UnsafeList<TypeIndex> reading, ref UnsafeList<TypeIndex> writing) => _GetImpl()->AddReaderWritersToLists(ref reading, ref writing);
        /// <summary>
        /// Reports whether this entity query has a filter applied to it.
        /// </summary>
        /// <returns>Returns true if the query has a filter, returns false if the query does not have a filter.</returns>
        public bool HasFilter() => _GetImpl()->HasFilter();
        /// <summary>
        /// Returns an <see cref="EntityQueryMask"/>, which can be used to quickly determine if an entity matches the query.
        /// </summary>
        /// <remarks>A maximum of 1024 <see cref="EntityQueryMask"/> instances can be allocated per World. Attempting to create
        /// additional masks will throw an exception.</remarks>
        /// <returns>The query mask associated with this query.</returns>
        public EntityQueryMask GetEntityQueryMask() => _GetImpl()->GetEntityQueryMask();
        /// <summary>
        /// Returns true if the entity matches the query, false if it does not.
        /// </summary>
        /// <param name="e">The entity to check for match</param>
        /// <remarks>
        /// This function will automatically block on any running jobs writing to component data that would affect the
        /// results of the check. For a non-blocking variant that ignores any query filtering, see <see cref="MatchesIgnoreFilter"/>.
        ///
        /// This function creates a <see cref="EntityQueryMask"/>, if one does not exist for this query already.
        /// A maximum of 1024 EntityQueryMasks can be allocated per World.
        /// </remarks>
        /// <returns>True if the entity is matched by the query, or false if not.</returns>
        /// <seealso cref="MatchesIgnoreFilter"/>
        public bool Matches(Entity e) => _GetImpl()->Matches(e);
        /// <summary>
        /// Returns true if the entity's archetype is matched by this query, ignoring all query
        /// filtering (including chunk filters and enableable components).
        /// </summary>
        /// <param name="e">The entity to check for match</param>
        /// <remarks>This function creates an <see cref="EntityQueryMask"/>, if one does not exist for this query already. A maximum
        /// of 1024 EntityQueryMasks can be allocated per World. This function throws an exception if the
        /// query contains enableable components.</remarks>
        /// <returns>True if the entity's archetype is matched by the query, or false if not.</returns>
        /// <seealso cref="Matches(Entity)"/>
        public bool MatchesIgnoreFilter(Entity e) => _GetImpl()->MatchesIgnoreFilter(e);
        /// <summary> Obsolete. Use <see cref="MatchesIgnoreFilter"/> instead.</summary>
        /// <param name="e">The entity to check for match</param>
        /// <returns>True if the entity's archetype is matched by the query, or false if not.</returns>
        [Obsolete("This function has been renamed to MatchesIgnoreFilter(). (RemovedAfter Entities 1.0) (UnityUpgradable) -> MatchesIgnoreFilter(*)", true)]
        public bool MatchesNoFilter(Entity e) => _GetImpl()->MatchesIgnoreFilter(e);

        /// <summary>
        /// Reconstructs the query descriptions used to create this <see cref="EntityQuery"/>.
        ///
        /// The query matches an archetype if it meets the criteria of at least one of these query descriptions.
        /// This method is mainly intended for reflection.
        /// </summary>
        /// <returns>An array of query descriptions for this query.</returns>
        [ExcludeFromBurstCompatTesting("Returns managed array of classes")]
        public EntityQueryDesc[] GetEntityQueryDescs() => _GetImpl()->GetEntityQueryDescs();
        /// <summary>
        /// Obsolete. Use <see cref="GetEntityQueryDescs"/> instead.
        /// </summary>
        /// <returns>The first element of the query's list of descriptions.</returns>
        [ExcludeFromBurstCompatTesting("Returns class")]
        [Obsolete("A query can have multiple descriptions; this method only returns the first one. Use GetEntityQueryDescs() to return them all. (RemovedAfter Entities 2.0)", false)]
        public EntityQueryDesc GetEntityQueryDesc() => _GetImpl()->GetEntityQueryDesc();

        // These methods are intended for chunk-cache self-test code.
        // Under normal operation, most code should use EntityQueryImpl.GetMatchingChunkCache() to access the cached chunk list.
        // This method will automatically update the cache if it is stale.
        internal void InvalidateCache() => _GetImpl()->_QueryData->InvalidateChunkCache();
        internal void ForceUpdateCache() => _GetImpl()->UpdateMatchingChunkCache();
        internal void CheckChunkListCacheConsistency(bool forceCheckInvalidCache = false) => _GetImpl()->_QueryData->CheckChunkListCacheConsistency(forceCheckInvalidCache);
        internal bool IsCacheValid => _GetImpl()->_QueryData->IsChunkCacheValid();

        unsafe internal EntityQueryImpl* _Debugger_GetImpl()
        {
            if (!AtomicSafetyHandle.IsHandleValid(__safety))
                return null;
            return __impl;
        }


        /// <summary>
        ///  Internal gen impl
        /// </summary>
        /// <returns></returns>
        internal EntityQueryImpl* _GetImpl()
        {
            _CheckSafetyHandle();
            return __impl;
        }

        internal EntityComponentStore* GetEntityComponentStore()
        {
            _CheckSafetyHandle();
            return __impl->_Access->EntityComponentStore;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void _CheckSafetyHandle()
        {
            AtomicSafetyHandle.CheckWriteAndThrow(__safety);
        }

        internal AtomicSafetyHandle __safety;

        internal EntityQueryImpl* __impl;
        internal ulong __seqno;

        /// <summary>
        /// Test two queries for equality
        /// </summary>
        /// <param name="lhs">The left query</param>
        /// <param name="rhs">The right query</param>
        /// <returns>True if the left and right queries are equal, or false if not.</returns>
        public static bool operator==(EntityQuery lhs, EntityQuery rhs)
        {
            return lhs.__seqno == rhs.__seqno;
        }

        /// <summary>
        /// Test two queries for inequality
        /// </summary>
        /// <param name="lhs">The left query</param>
        /// <param name="rhs">The right query</param>
        /// <returns>False if the left and right queries are equal, or true if not.</returns>
        public static bool operator!=(EntityQuery lhs, EntityQuery rhs)
        {
            return !(lhs == rhs);
        }
    }
}
