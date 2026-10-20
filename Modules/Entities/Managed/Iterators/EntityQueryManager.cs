// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.Assertions;
using Assert = UnityEngine.Assertions.AssertCore;

namespace Unity.Entities
{
    // A bundle of component safety handles implicitly referenced by an EntityQuery. This allows a query to
    // register its component dependencies when the query is used by a job.
    [NativeContainer]
    internal struct EntityQuerySafetyHandles
    {
        // All IJobChunk jobs have a EntityManager safety handle to ensure that BeforeStructuralChange throws an error if
        // jobs without any other safety handles are still running (haven't been synced).
        internal AtomicSafetyHandle m_Safety0;
        // Enableable components from query used to schedule job. To add more handles here, you must also increase
        // EntityQueryManager.MAX_ENABLEABLE_COMPONENTS_PER_QUERY.
        internal AtomicSafetyHandle m_SafetyEnableable1;
        internal AtomicSafetyHandle m_SafetyEnableable2;
        internal AtomicSafetyHandle m_SafetyEnableable3;
        internal AtomicSafetyHandle m_SafetyEnableable4;
        internal AtomicSafetyHandle m_SafetyEnableable5;
        internal AtomicSafetyHandle m_SafetyEnableable6;
        internal AtomicSafetyHandle m_SafetyEnableable7;
        internal AtomicSafetyHandle m_SafetyEnableable8;
        internal int m_SafetyReadOnlyCount;
        internal int m_SafetyReadWriteCount;

        internal unsafe EntityQuerySafetyHandles(EntityQueryImpl* queryImpl)
        {
            this = default; // workaround for CS0171 error (all fields must be fully assigned before control is returned)
            var queryData = queryImpl->_QueryData;
            m_Safety0 = queryImpl->SafetyHandles->GetEntityManagerSafetyHandle();
            m_SafetyReadOnlyCount = 1 + queryData->EnableableComponentTypeIndexCount; // +1 for EntityManager handle
            m_SafetyReadWriteCount = 0;
            fixed (AtomicSafetyHandle* pEnableableHandles = &m_SafetyEnableable1)
            {
                for (int i = 0; i < EntityQueryManager.MAX_ENABLEABLE_COMPONENTS_PER_QUERY; ++i)
                {
                    if (i < queryData->EnableableComponentTypeIndexCount)
                    {
                        pEnableableHandles[i] =
                            queryImpl->SafetyHandles->GetSafetyHandle(queryData->EnableableComponentTypeIndices[i],
                                true);
                        AtomicSafetyHandle.SetExclusiveWeak(ref pEnableableHandles[i], true);
                    }
                }
            }
        }
    }

    [GenerateTestsForBurstCompatibility]
    internal unsafe struct EntityQueryManager
    {
        [NoAutoStaticsCleanup] // DefaultWorldInitialization sets it before it creates worlds.
        internal static int EntityQueryDataChunkAllocatorBudgetInBytes = EntityComponentStore.kDefaultAllocatorBudgetInBytes;

        private ComponentDependencyManager*    m_DependencyManager;
        private BlockAllocator                 m_EntityQueryDataChunkAllocator;
        private UnsafePtrList<EntityQueryData> m_EntityQueryDatas;

        internal int EntityQueryDataAllocatorUsedBytes => m_EntityQueryDataChunkAllocator.AllocatedBytes;
        internal int EntityQueryDataAllocatorBudgetBytes => m_EntityQueryDataChunkAllocator.BudgetInBytes;
        internal const int MAX_ENABLEABLE_COMPONENTS_PER_QUERY = 8;

        private UntypedUnsafeParallelHashMap           m_EntityQueryDataCacheUntyped;
        internal int                           m_EntityQueryMasksAllocated;
        private TypeIndex m_disabledTypeIndex;
        private TypeIndex m_prefabTypeIndex;
        private TypeIndex m_systemInstanceTypeIndex;
        private TypeIndex m_chunkHeaderTypeIndex;



        public static void Create(EntityQueryManager* queryManager, ComponentDependencyManager* dependencyManager)
        {
            queryManager->m_DependencyManager = dependencyManager;
            queryManager->m_EntityQueryDataChunkAllocator = new BlockAllocator(AllocatorManager.Persistent, EntityQueryDataChunkAllocatorBudgetInBytes);
            ref var entityQueryCache = ref UnsafeUtility.As<UntypedUnsafeParallelHashMap, UnsafeParallelMultiHashMap<int, int>>(ref queryManager->m_EntityQueryDataCacheUntyped);
            entityQueryCache = new UnsafeParallelMultiHashMap<int, int>(1024, Allocator.Persistent);
            queryManager->m_EntityQueryDatas = new UnsafePtrList<EntityQueryData>(0, Allocator.Persistent);
            queryManager->m_EntityQueryMasksAllocated = 0;
            queryManager->m_disabledTypeIndex = TypeManager.GetTypeIndex<Disabled>();
            queryManager->m_prefabTypeIndex = TypeManager.GetTypeIndex<Prefab>();
            queryManager->m_systemInstanceTypeIndex = TypeManager.GetTypeIndex<SystemInstance>();
            queryManager->m_chunkHeaderTypeIndex = TypeManager.GetTypeIndex<ChunkHeader>();

        }

        public static void Destroy(EntityQueryManager* manager)
        {
            manager->Dispose();
        }

        void Dispose()
        {
            ref var entityQueryCache = ref UnsafeUtility.As<UntypedUnsafeParallelHashMap, UnsafeParallelMultiHashMap<int, int>>(ref m_EntityQueryDataCacheUntyped);
            entityQueryCache.Dispose();
            for (var g = 0; g < m_EntityQueryDatas.Length; ++g)
            {
                m_EntityQueryDatas.Ptr[g]->Dispose();
            }
            m_EntityQueryDatas.Dispose();
            //@TODO: Need to wait for all job handles to be completed..
            m_EntityQueryDataChunkAllocator.Dispose();
        }

        ArchetypeQuery* CreateArchetypeQueries(ref UnsafeScratchAllocator unsafeScratchAllocator, ComponentType* requiredTypes, int count)
        {
            var builder = new EntityQueryBuilder(Allocator.Temp);

            for (int i = 0; i != count; i++)
            {
                if (requiredTypes[i].AccessModeType == ComponentType.AccessMode.Exclude)
                {
                    var noneType = ComponentType.ReadOnly(requiredTypes[i].TypeIndex);
                    builder.WithNone(&noneType, 1);
                }
                else
                {
                    builder.WithAll(&requiredTypes[i], 1);
                }
            }

            builder.FinalizeQueryInternal();

            var result = CreateArchetypeQueries(ref unsafeScratchAllocator, builder);

            builder.Dispose();

            return result;
        }

        internal void ConstructTypeArray(ref UnsafeScratchAllocator unsafeScratchAllocator, UnsafeList<ComponentType> types, out TypeIndex* outTypes, out byte* outAccessModes, out int outLength)
        {
            if (types.Length == 0)
            {
                outTypes = null;
                outAccessModes = null;
                outLength = 0;
            }
            else
            {
                outLength = types.Length;
                outTypes = (TypeIndex*)unsafeScratchAllocator.Allocate<TypeIndex>(types.Length);
                outAccessModes = (byte*)unsafeScratchAllocator.Allocate<byte>(types.Length);

                var sortedTypes = stackalloc ComponentType[types.Length];
                for (var i = 0; i < types.Length; ++i)
                    SortingUtilities.InsertSorted(sortedTypes, i, types[i]);

                for (int i = 0; i != types.Length; i++)
                {
                    outTypes[i] = sortedTypes[i].TypeIndex;
                    outAccessModes[i] = (byte)sortedTypes[i].AccessModeType;
                }
            }
        }

#pragma warning disable 0618 // Write groups are obsolete; will be removed in Entities 7.0
        void IncludeDependentWriteGroups(ComponentType type, ref UnsafeList<ComponentType> explicitList)
        {
            if (type.AccessModeType != ComponentType.AccessMode.ReadOnly)
                return;

            var typeInfo = TypeManager.GetTypeInfo(type.TypeIndex);
            var writeGroups = TypeManager.GetWriteGroups(typeInfo);
            var writeGroupCount = typeInfo.WriteGroupCount;
            for (int i = 0; i < writeGroupCount; i++)
            {
                var excludedComponentType = GetWriteGroupReadOnlyComponentType(writeGroups, i);
                if (explicitList.Contains(excludedComponentType))
                    continue;

                explicitList.Add(excludedComponentType);
                IncludeDependentWriteGroups(excludedComponentType, ref explicitList);
            }
        }

        private static ComponentType GetWriteGroupReadOnlyComponentType(TypeIndex* writeGroupTypes, int i)
        {
            // Need to get "Clean" TypeIndex from Type. Since TypeInfo.TypeIndex is not actually the index of the
            // type. (It includes other flags.) What is stored in WriteGroups is the actual index of the type.
            ref readonly var excludedType = ref TypeManager.GetTypeInfo(writeGroupTypes[i]);
            var excludedComponentType = ComponentType.ReadOnly(excludedType.TypeIndex);
            return excludedComponentType;
        }

        void ExcludeWriteGroups(ComponentType type, ref UnsafeList<ComponentType> noneList, UnsafeList<ComponentType> explicitList)
        {
            if (type.AccessModeType == ComponentType.AccessMode.ReadOnly)
                return;

            var typeInfo = TypeManager.GetTypeInfo(type.TypeIndex);
            var writeGroups = TypeManager.GetWriteGroups(typeInfo);
            var writeGroupCount = typeInfo.WriteGroupCount;
            for (int i = 0; i < writeGroupCount; i++)
            {
                var excludedComponentType = GetWriteGroupReadOnlyComponentType(writeGroups, i);
                if (noneList.Contains(excludedComponentType))
                    continue;
                if (explicitList.Contains(excludedComponentType))
                    continue;

                noneList.Add(excludedComponentType);
            }
        }
#pragma warning restore 0618

        // Plan to unmanaged EntityQueryManager
        // [X] Introduce EntityQueryBuilder and test it
        // [X] Change internals to take an EntityQueryBuilder as an in parameter
        // [X] Validate queryData in CreateArchetypeQueries
        // [x] Everyone calling this needs to convert managed stuff into unmanaged EQDBuilder
        // [x] Public overloads of APIs to offer an EQDBuilder option
        // [ ] Deprecate EntityQueryDesc
        ArchetypeQuery* CreateArchetypeQueries(ref UnsafeScratchAllocator unsafeScratchAllocator, in EntityQueryBuilder queryBuilder)
        {
            var types = queryBuilder._builderDataPtr->_typeData;
            var queryData = queryBuilder._builderDataPtr->_indexData;
            var outQuery = (ArchetypeQuery*)unsafeScratchAllocator.Allocate(sizeof(ArchetypeQuery) * queryData.Length, UnsafeUtility.AlignOf<ArchetypeQuery>());

            // we need to build out new lists of component types to mutate them for WriteGroups
            var allTypes = new UnsafeList<ComponentType>(32, Allocator.Temp);
            var anyTypes = new UnsafeList<ComponentType>(32, Allocator.Temp);
            var noneTypes = new UnsafeList<ComponentType>(32, Allocator.Temp);
            var disabledTypes = new UnsafeList<ComponentType>(32, Allocator.Temp);
            var absentTypes = new UnsafeList<ComponentType>(32, Allocator.Temp);
            var presentTypes = new UnsafeList<ComponentType>(32, Allocator.Temp);
            var entityTypeIndex = TypeManager.GetTypeIndex<Entity>();

            for (int q = 0; q != queryData.Length; q++)
            {
                outQuery[q].Options = queryData[q].Options;
                {
                    var typesAll = queryData[q].All;
                    for (int i = typesAll.Index; i < typesAll.Index + typesAll.Count; i++)
                    {
                        if (Hint.Unlikely(types[i].TypeIndex == entityTypeIndex))
                        {
                            throw new ArgumentException("Entity is not allowed in list of component types for EntityQuery");
                        }
                        allTypes.Add(types[i]);
                        if (types[i].TypeIndex == m_disabledTypeIndex)
                            outQuery[q].Options |= EntityQueryOptions.IncludeDisabledEntities;
                        else if (types[i].TypeIndex == m_prefabTypeIndex)
                            outQuery[q].Options |= EntityQueryOptions.IncludePrefab;
                        else if (types[i].TypeIndex == m_chunkHeaderTypeIndex)
                            outQuery[q].Options |= EntityQueryOptions.IncludeMetaChunks;
                        else if (types[i].TypeIndex == m_systemInstanceTypeIndex)
                            outQuery[q].Options |= EntityQueryOptions.IncludeSystems;
                    }
                }

                {
                    var typesAny = queryData[q].Any;
                    for (int i = typesAny.Index; i < typesAny.Index + typesAny.Count; i++)
                    {
                        if (Hint.Unlikely(types[i].TypeIndex == entityTypeIndex))
                        {
                            throw new ArgumentException("Entity is not allowed in list of component types for EntityQuery");
                        }
                        anyTypes.Add(types[i]);
                    }
                }

                {
                    var typesNone = queryData[q].None;
                    for (int i = typesNone.Index; i < typesNone.Index + typesNone.Count; i++)
                    {
                        if (Hint.Unlikely(types[i].TypeIndex == entityTypeIndex))
                        {
                            throw new ArgumentException("Entity is not allowed in list of component types for EntityQuery");
                        }
                        var type = types[i];
                        // Can not use Assert.AreEqual here because it uses the (object, object) overload which
                        // boxes the enums being compared, and that can not be burst compiled.
                        Assert.IsTrue(ComponentType.AccessMode.ReadOnly == type.AccessModeType, "EntityQueryBuilder.None must convert ComponentType.AccessMode to ReadOnly");
                        noneTypes.Add(types[i]);
                    }
                }

                {
                    var typesDisabled = queryData[q].Disabled;
                    for (int i = typesDisabled.Index; i < typesDisabled.Index + typesDisabled.Count; i++)
                    {
                        if (Hint.Unlikely(types[i].TypeIndex == entityTypeIndex))
                        {
                            throw new ArgumentException("Entity is not allowed in list of component types for EntityQuery");
                        }
                        disabledTypes.Add(types[i]);
                    }
                }

                {
                    var typesAbsent = queryData[q].Absent;
                    for (int i = typesAbsent.Index; i < typesAbsent.Index + typesAbsent.Count; i++)
                    {
                        if (Hint.Unlikely(types[i].TypeIndex == entityTypeIndex))
                        {
                            throw new ArgumentException("Entity is not allowed in list of component types for EntityQuery");
                        }
                        var type = types[i];
                        // Can not use Assert.AreEqual here because it uses the (object, object) overload which
                        // boxes the enums being compared, and that can not be burst compiled.
                        Assert.IsTrue(ComponentType.AccessMode.ReadOnly == type.AccessModeType, "EntityQueryBuilder.Absent must convert ComponentType.AccessMode to ReadOnly");
                        absentTypes.Add(types[i]);
                    }

                    {
                        var typesPresent = queryData[q].Present;
                        for (int i = typesPresent.Index; i < typesPresent.Index + typesPresent.Count; i++)
                        {
                            if (Hint.Unlikely(types[i].TypeIndex == entityTypeIndex))
                            {
                                throw new ArgumentException("Entity is not allowed in list of component types for EntityQuery");
                            }
                            presentTypes.Add(types[i]);
                        }
                    }
                }


                // Validate the queryBuilder has components declared in a consistent way
                EntityQueryBuilder.Validate(allTypes, anyTypes, noneTypes, disabledTypes, absentTypes, presentTypes);

#pragma warning disable 0618 // Write groups are obsolete; will be removed in Entities 7.0
                var isFilterWriteGroup = (queryData[q].Options & EntityQueryOptions.FilterWriteGroup) != 0;
#pragma warning restore 0618
                if (isFilterWriteGroup)
                {
                    // Each ReadOnly<type> in any or all
                    //   if has WriteGroup types,
                    //   - Recursively add to any (if not explicitly mentioned)
                    var explicitList = new UnsafeList<ComponentType>(allTypes.Length + anyTypes.Length +
                                                                     noneTypes.Length + disabledTypes.Length +
                                                                     absentTypes.Length + presentTypes.Length + 16, Allocator.Temp);
                    explicitList.AddRange(allTypes);
                    explicitList.AddRange(anyTypes);
                    explicitList.AddRange(noneTypes);
                    explicitList.AddRange(disabledTypes);
                    explicitList.AddRange(absentTypes);
                    explicitList.AddRange(presentTypes);

                    for (int i = 0; i < anyTypes.Length; i++)
                        IncludeDependentWriteGroups(anyTypes[i], ref explicitList);
                    for (int i = 0; i < allTypes.Length; i++)
                        IncludeDependentWriteGroups(allTypes[i], ref explicitList);
                    for (int i = 0; i < disabledTypes.Length; i++)
                        IncludeDependentWriteGroups(disabledTypes[i], ref explicitList);
                    for (int i = 0; i < presentTypes.Length; i++)
                        IncludeDependentWriteGroups(presentTypes[i], ref explicitList);

                    // Each ReadWrite<type> in any or all
                    //   if has WriteGroup types,
                    //     Add to none (if not exist in any or all or none)

                    for (int i = 0; i < anyTypes.Length; i++)
                        ExcludeWriteGroups(anyTypes[i], ref noneTypes, explicitList);
                    for (int i = 0; i < allTypes.Length; i++)
                        ExcludeWriteGroups(allTypes[i], ref noneTypes, explicitList);
                    for (int i = 0; i < disabledTypes.Length; i++)
                        ExcludeWriteGroups(disabledTypes[i], ref noneTypes, explicitList);
                    for (int i = 0; i < presentTypes.Length; i++)
                        ExcludeWriteGroups(presentTypes[i], ref noneTypes, explicitList);
                    explicitList.Dispose();
                }

                ConstructTypeArray(ref unsafeScratchAllocator, noneTypes, out outQuery[q].None,
                    out outQuery[q].NoneAccessMode, out outQuery[q].NoneCount);
                ConstructTypeArray(ref unsafeScratchAllocator, allTypes, out outQuery[q].All,
                    out outQuery[q].AllAccessMode, out outQuery[q].AllCount);
                ConstructTypeArray(ref unsafeScratchAllocator, anyTypes, out outQuery[q].Any,
                    out outQuery[q].AnyAccessMode, out outQuery[q].AnyCount);
                ConstructTypeArray(ref unsafeScratchAllocator, disabledTypes, out outQuery[q].Disabled,
                    out outQuery[q].DisabledAccessMode, out outQuery[q].DisabledCount);
                ConstructTypeArray(ref unsafeScratchAllocator, absentTypes, out outQuery[q].Absent,
                    out outQuery[q].AbsentAccessMode, out outQuery[q].AbsentCount);
                ConstructTypeArray(ref unsafeScratchAllocator, presentTypes, out outQuery[q].Present,
                    out outQuery[q].PresentAccessMode, out outQuery[q].PresentCount);

                ulong allBloomFilterMask = 0;
                for (int i = 0; i < outQuery[q].AllCount; ++i)
                    allBloomFilterMask |= TypeManager.GetTypeInfo(outQuery[q].All[i]).BloomFilterMask;
                outQuery[q].AllBloomFilterMask = allBloomFilterMask;

                allTypes.Clear();
                anyTypes.Clear();
                noneTypes.Clear();
                disabledTypes.Clear();
                absentTypes.Clear();
                presentTypes.Clear();
            }

            allTypes.Dispose();
            anyTypes.Dispose();
            noneTypes.Dispose();
            disabledTypes.Dispose();
            absentTypes.Dispose();
            presentTypes.Dispose();
            return outQuery;
        }

        internal static bool CompareQueryArray(in EntityQueryBuilder builder, EntityQueryBuilder.ComponentIndexArray arr, TypeIndex* typeArray, byte* accessModeArray, int typeArrayCount)
        {
            int arrCount = arr.Count;
            if (typeArrayCount != arrCount)
                return false;

            var sortedTypes = stackalloc ComponentType[arrCount];
            var types = builder._builderDataPtr->_typeData;
            for (var i = 0; i < arrCount; ++i)
            {
                SortingUtilities.InsertSorted(sortedTypes, i, types[arr.Index + i]);
            }

            for (var i = 0; i < arrCount; ++i)
            {
                if (typeArray[i] != sortedTypes[i].TypeIndex || accessModeArray[i] != (byte)sortedTypes[i].AccessModeType)
                    return false;
            }

            return true;
        }


        public static bool CompareQuery(in EntityQueryBuilder queryBuilder, EntityQueryData* queryData)
        {
            queryBuilder.FinalizeQueryInternal();
            var indexData = queryBuilder._builderDataPtr->_indexData;
            int count = indexData.Length;
            if (queryData->ArchetypeQueryCount != count)
                return false;

            for (int i = 0; i != count; i++)
            {
                ref var archetypeQuery = ref queryData->ArchetypeQueries[i];
                var q = indexData[i];

                if (!CompareQueryArray(queryBuilder, q.All, archetypeQuery.All, archetypeQuery.AllAccessMode, archetypeQuery.AllCount))
                    return false;
                if (!CompareQueryArray(queryBuilder, q.None, archetypeQuery.None, archetypeQuery.NoneAccessMode, archetypeQuery.NoneCount))
                    return false;
                if (!CompareQueryArray(queryBuilder, q.Any, archetypeQuery.Any, archetypeQuery.AnyAccessMode, archetypeQuery.AnyCount))
                    return false;
                if (!CompareQueryArray(queryBuilder, q.Disabled, archetypeQuery.Disabled, archetypeQuery.DisabledAccessMode, archetypeQuery.DisabledCount))
                    return false;
                if (!CompareQueryArray(queryBuilder, q.Absent, archetypeQuery.Absent, archetypeQuery.AbsentAccessMode, archetypeQuery.AbsentCount))
                    return false;
                if (!CompareQueryArray(queryBuilder, q.Present, archetypeQuery.Present, archetypeQuery.PresentAccessMode, archetypeQuery.PresentCount))
                    return false;
                if (q.Options != archetypeQuery.Options)
                    return false;
            }

            return true;
        }

        private int IntersectSortedComponentIndexArrays(ComponentType* arrayA, int arrayACount, ComponentType* arrayB, int arrayBCount, ComponentType* outArray)
        {
            var intersectionCount = 0;

            var i = 0;
            var j = 0;
            while (i < arrayACount && j < arrayBCount)
            {
                if (arrayA[i] < arrayB[j])
                    i++;
                else if (arrayB[j] < arrayA[i])
                    j++;
                else
                {
                    outArray[intersectionCount] = arrayB[j];
                    intersectionCount++;
                    i++;
                    j++;
                }
            }

            return intersectionCount;
        }

        // Calculates the intersection of "All", "Disabled", and "Present" arrays from the provided ArchetypeQuery objects.
        // "None" types are also considered, if the type is an enableable component; see DOTS-9730.
        // The intersection is returned as a new array of ComponentType objects, which must be freed by the caller.
        // The Entity component is always required (and will be returned as the first element of the output array), so the
        // return value will never be null (unless the allocator is out of memory).
        private ComponentType* CalculateRequiredComponentsFromQuery(ref UnsafeScratchAllocator allocator, ArchetypeQuery* queries, int queryCount, out int outRequiredComponentsCount)
        {
            // Populate and sort a combined array of required component types and their access modes from the first ArchetypeQuery
            var maxIntersectionCount = queries[0].AllCount + queries[0].DisabledCount + queries[0].PresentCount + queries[0].NoneCount;
            // The first required component is always Entity.
            var outRequiredComponents = (ComponentType*)allocator.Allocate<ComponentType>(maxIntersectionCount+1);
            outRequiredComponents[0] = ComponentType.ReadWrite<Entity>();
            // If Entity is the only required component at this point, we're done; no need to look at any further ArchetypeQueries.
            if (maxIntersectionCount == 0)
            {
                outRequiredComponentsCount = 1;
                return outRequiredComponents;
            }
            var intersectionComponents = outRequiredComponents + 1;
            int currentIntersectionCount = 0;
            for (int j = 0; j < queries[0].AllCount; ++j)
            {
                intersectionComponents[currentIntersectionCount++] = new ComponentType
                {
                    TypeIndex = queries[0].All[j],
                    AccessModeType = (ComponentType.AccessMode)queries[0].AllAccessMode[j],
                };
            }
            for (int j = 0; j < queries[0].DisabledCount; ++j)
            {
                intersectionComponents[currentIntersectionCount++] = new ComponentType
                {
                    TypeIndex = queries[0].Disabled[j],
                    AccessModeType = (ComponentType.AccessMode)queries[0].DisabledAccessMode[j],
                };
            }
            for (int j = 0; j < queries[0].PresentCount; ++j)
            {
                intersectionComponents[currentIntersectionCount++] = new ComponentType
                {
                    TypeIndex = queries[0].Present[j],
                    AccessModeType = (ComponentType.AccessMode)queries[0].PresentAccessMode[j],
                };
            }
            for (int j = 0; j < queries[0].NoneCount; ++j)
            {
                // If T is an enableable component used in WithNone, we conservatively add a T as a
                // required excluded (??) component. This does not mean T is required to be present in matching
                // archetypes (see the branch handling Exclude in AddArchetypeIfMatching), but it does ensure that
                // it's added as a read dependency on any systems that use this query.
                if (!TypeManager.IsEnableable(queries[0].None[j]))
                    continue;
                intersectionComponents[currentIntersectionCount++] = new ComponentType
                {
                    TypeIndex = queries[0].None[j],
                    AccessModeType = ComponentType.AccessMode.Exclude,
                };
            }
            NativeSortExtension.Sort(intersectionComponents, currentIntersectionCount);

            // For each additional ArchetypeQuery, create the same sorted array of component types, and reduce the
            // original array to the intersection of these two arrays.
            int maxQueryRequiredCount = 0;
            for (int i = 1; i < queryCount; ++i)
            {
                maxQueryRequiredCount = math.max(maxQueryRequiredCount,
                    queries[i].AllCount + queries[i].DisabledCount + queries[i].PresentCount + queries[i].NoneCount);
            }
            var queryRequiredTypes = (ComponentType*)allocator.Allocate<ComponentType>(maxQueryRequiredCount);
            for (int i = 1; i < queryCount; ++i)
            {
                int queryRequiredCount = 0;
                for (int j = 0; j < queries[i].AllCount; ++j)
                {
                    queryRequiredTypes[queryRequiredCount++] = new ComponentType
                    {
                        TypeIndex = queries[i].All[j],
                        AccessModeType = (ComponentType.AccessMode)queries[i].AllAccessMode[j],
                    };
                }
                for (int j = 0; j < queries[i].DisabledCount; ++j)
                {
                    queryRequiredTypes[queryRequiredCount++] = new ComponentType
                    {
                        TypeIndex = queries[i].Disabled[j],
                        AccessModeType = (ComponentType.AccessMode)queries[i].DisabledAccessMode[j],
                    };
                }
                for (int j = 0; j < queries[i].PresentCount; ++j)
                {
                    queryRequiredTypes[queryRequiredCount++] = new ComponentType
                    {
                        TypeIndex = queries[i].Present[j],
                        AccessModeType = (ComponentType.AccessMode)queries[i].PresentAccessMode[j],
                    };
                }
                for (int j = 0; j < queries[0].NoneCount; ++j)
                {
                    // If T is an enableable component used in WithNone, we conservatively add a T as a
                    // required excluded (??) component. This does not mean T is required to be present in matching
                    // archetypes (see the branch handling Exclude in AddArchetypeIfMatching), but it does ensure that
                    // it's added as a read dependency on any systems that use this query.
                    if (!TypeManager.IsEnableable(queries[0].None[j]))
                        continue;
                    queryRequiredTypes[queryRequiredCount++] = new ComponentType
                    {
                        TypeIndex = queries[0].None[j],
                        AccessModeType = ComponentType.AccessMode.Exclude,
                    };
                }
                NativeSortExtension.Sort(queryRequiredTypes, queryRequiredCount);
                currentIntersectionCount = IntersectSortedComponentIndexArrays(intersectionComponents, currentIntersectionCount,
                    queryRequiredTypes, queryRequiredCount, intersectionComponents);
            }
            outRequiredComponentsCount = currentIntersectionCount + 1; // again, the +1 is for the Entity type at outRequiredComponents[0]
            return outRequiredComponents;
        }

        [ExcludeFromBurstCompatTesting("Takes managed array")]
        internal static void ConvertToEntityQueryBuilder(ref EntityQueryBuilder builder, EntityQueryDesc[] queryDesc)
        {
            for (int q = 0; q != queryDesc.Length; q++)
            {
                ref var desc = ref queryDesc[q];
                fixed (ComponentType* allTypes = desc.All)
                {
                    builder.WithAll(allTypes, desc.All.Length);
                }
                fixed (ComponentType* anyTypes = desc.Any)
                {
                    builder.WithAny(anyTypes, desc.Any.Length);
                }
                fixed (ComponentType* noneTypes = desc.None)
                {
                    builder.WithNone(noneTypes, desc.None.Length);
                }
                fixed (ComponentType* disabledTypes = desc.Disabled)
                {
                    builder.WithDisabled(disabledTypes, desc.Disabled.Length);
                }
                fixed (ComponentType* absentTypes = desc.Absent)
                {
                    builder.WithAbsent(absentTypes, desc.Absent.Length);
                }
                fixed (ComponentType* presentTypes = desc.Present)
                {
                    builder.WithPresent(presentTypes, desc.Present.Length);
                }

                builder.WithOptions(desc.Options);
                builder.FinalizeQueryInternal();
            }
        }

        public EntityQuery CreateEntityQuery(EntityDataAccess* access, EntityQueryBuilder query)
        {
            query.FinalizeQueryInternal();

            var buffer = stackalloc byte[2048];
            var scratchAllocator = new UnsafeScratchAllocator(buffer, 2048);
            var archetypeQuery = CreateArchetypeQueries(ref scratchAllocator, query);

            var indexData = query._builderDataPtr->_indexData;
            var outRequiredComponents = CalculateRequiredComponentsFromQuery(ref scratchAllocator, archetypeQuery, indexData.Length, out var outRequiredComponentsCount);
            return CreateEntityQuery(access, archetypeQuery, indexData.Length, outRequiredComponents, outRequiredComponentsCount);
        }

        public EntityQuery CreateEntityQuery(EntityDataAccess* access, ComponentType* inRequiredComponents, int inRequiredComponentsCount)
        {
            var buffer = stackalloc byte[1024];
            var scratchAllocator = new UnsafeScratchAllocator(buffer, 1024);
            var archetypeQueries = CreateArchetypeQueries(ref scratchAllocator, inRequiredComponents, inRequiredComponentsCount);
            var outRequiredComponents = (ComponentType*)scratchAllocator.Allocate<ComponentType>(inRequiredComponentsCount + 1);
            outRequiredComponents[0] = ComponentType.ReadWrite<Entity>();
            for (int i = 0; i != inRequiredComponentsCount; i++)
                SortingUtilities.InsertSorted(outRequiredComponents + 1, i, inRequiredComponents[i]);
            var outRequiredComponentsCount = inRequiredComponentsCount + 1;
            return CreateEntityQuery(access, archetypeQueries, 1, outRequiredComponents, outRequiredComponentsCount);
        }

        bool Matches(EntityQueryData* queryData, ArchetypeQuery* archetypeQueries, int archetypeQueryCount,
            ComponentType* requiredComponents, int requiredComponentsCount)
        {
            if (requiredComponentsCount != queryData->RequiredComponentsCount)
                return false;
            if (archetypeQueryCount != queryData->ArchetypeQueryCount)
                return false;
            if (requiredComponentsCount > 0 && UnsafeUtility.MemCmp(requiredComponents, queryData->RequiredComponents, sizeof(ComponentType) * requiredComponentsCount) != 0)
                return false;
            for (var i = 0; i < archetypeQueryCount; ++i)
                if (!archetypeQueries[i].Equals(queryData->ArchetypeQueries[i]))
                    return false;
            return true;
        }

        void* ChunkAllocate<T>(int count = 1, void *source = null) where T : struct
        {
            var bytes = count * UnsafeUtility.SizeOf<T>();
            if (bytes == 0)
                return null;
            var pointer = m_EntityQueryDataChunkAllocator.Allocate(bytes, UnsafeUtility.AlignOf<T>());
            if (source != null)
                UnsafeUtility.MemCpy(pointer, source, bytes);
            return pointer;
        }

        public EntityQuery CreateEntityQuery(EntityDataAccess* access,
            ArchetypeQuery* archetypeQueries,
            int archetypeQueryCount,
            ComponentType* requiredComponents,
            int requiredComponentCount)
        {
            //@TODO: Validate that required types is subset of archetype filters all...

            int hash = (int)math.hash(requiredComponents, requiredComponentCount * sizeof(ComponentType));
            for (var i = 0; i < archetypeQueryCount; ++i)
                hash = hash * 397 ^ archetypeQueries[i].GetHashCode();
            EntityQueryData* queryData = null;
            ref var entityQueryCache = ref UnsafeUtility.As<UntypedUnsafeParallelHashMap, UnsafeParallelMultiHashMap<int, int>>(ref m_EntityQueryDataCacheUntyped);

            if (entityQueryCache.TryGetFirstValue(hash, out var entityQueryIndex, out var iterator))
            {
                do
                {
                    var possibleMatch = m_EntityQueryDatas.Ptr[entityQueryIndex];
                    if (Matches(possibleMatch, archetypeQueries, archetypeQueryCount, requiredComponents, requiredComponentCount))
                    {
                        queryData = possibleMatch;
                        break;
                    }
                }
                while (entityQueryCache.TryGetNextValue(out entityQueryIndex, ref iterator));
            }

            if (queryData == null)
            {
                // Validate input archetype queries
                bool queryIgnoresEnabledBits =
                    (archetypeQueries[0].Options & EntityQueryOptions.IgnoreComponentEnabledState) != 0;
                for (int iAQ = 1; iAQ < archetypeQueryCount; ++iAQ)
                {
                    bool hasIgnoreEnabledBitsFlag = (archetypeQueries[iAQ].Options & EntityQueryOptions.IgnoreComponentEnabledState) != 0;
                    if (hasIgnoreEnabledBitsFlag != queryIgnoresEnabledBits)
                        throw new ArgumentException(
                            $"All EntityQueryOptions passed to CreateEntityQuery() must have the same value for the IgnoreComponentEnabledState flag");
                }

                // count & identify enableable component types
                int totalComponentCount = 0;
                for (int iAQ = 0; iAQ < archetypeQueryCount; ++iAQ)
                {
                    totalComponentCount += archetypeQueries[iAQ].AllCount + archetypeQueries[iAQ].AnyCount +
                                           archetypeQueries[iAQ].NoneCount + archetypeQueries[iAQ].DisabledCount +
                                           archetypeQueries[iAQ].AbsentCount + archetypeQueries[iAQ].PresentCount;
                }

                var allEnableableTypeIndices = new NativeList<TypeIndex>(totalComponentCount, Allocator.Temp);
                var enableableTypesInAny = new NativeList<ComponentType>(totalComponentCount, Allocator.Temp);
                for (int iAQ = 0; iAQ < archetypeQueryCount; ++iAQ)
                {
                    ref ArchetypeQuery aq = ref archetypeQueries[iAQ];
                    for (int i = 0; i < aq.AllCount; ++i)
                    {
                        if (aq.All[i].IsEnableable)
                            allEnableableTypeIndices.Add(aq.All[i]);
                    }
                    for (int i = 0; i < aq.NoneCount; ++i)
                    {
                        if (aq.None[i].IsEnableable)
                            allEnableableTypeIndices.Add(aq.None[i]);
                    }
                    for (int i = 0; i < aq.AnyCount; ++i)
                    {
                        if (aq.Any[i].IsEnableable)
                        {
                            allEnableableTypeIndices.Add(aq.Any[i]);
                            // We need a list of enableable types in all the query's Any lists below.
                            // Enableable types in the Any list are not in requiredComponents (by definition
                            // they are not required to be present), but evaluating the query implicitly
                            // requires reading their enabled bits if they are present. So, the query must
                            // have a read dependency on these types to avoid safety errors.
                            enableableTypesInAny.Add(ComponentType.ReadOnly(aq.Any[i]));
                        }
                    }
                    for (int i = 0; i < aq.DisabledCount; ++i)
                    {
                        // All "Disabled" types must be enableable. We'll enforce that elsewhere.
                        if (!aq.Disabled[i].IsEnableable)
                            throw new InvalidOperationException($"Component type {aq.Disabled[i]} passed with WithDisabled() is not an enableable component");
                        allEnableableTypeIndices.Add(aq.Disabled[i]);
                    }
                    // absent components are never present in a matching archetype, so it doesn't matter if they're
                    // enableable or not.
                    // present components are always present in a matching archetype, so it doesn't matter if they're
                    // enableable or not.
                }
                // eliminate duplicate type indices
                if (allEnableableTypeIndices.Length > 0)
                {
                    allEnableableTypeIndices.Sort();
                    int lastUniqueIndex = 0;
                    for (int i = 1; i < allEnableableTypeIndices.Length; ++i)
                    {
                        if (allEnableableTypeIndices[i] != allEnableableTypeIndices[lastUniqueIndex])
                        {
                            allEnableableTypeIndices[++lastUniqueIndex] = allEnableableTypeIndices[i];
                        }
                    }
                    allEnableableTypeIndices.Length = lastUniqueIndex + 1;
                    // This limit matches the number of safety handles for enableable types we store in job structs.
                    if (allEnableableTypeIndices.Length > MAX_ENABLEABLE_COMPONENTS_PER_QUERY)
                        throw new ArgumentException(
                            "EntityQuery objects may not reference more than 8 enableable components");
                }
                if (enableableTypesInAny.Length > 0)
                {
                    enableableTypesInAny.Sort();
                    int lastUniqueIndex = 0;
                    for (int i = 1; i < enableableTypesInAny.Length; ++i) {
                        if (enableableTypesInAny[i] != enableableTypesInAny[lastUniqueIndex]) {
                            enableableTypesInAny[++lastUniqueIndex] = enableableTypesInAny[i];
                        }
                    }
                    enableableTypesInAny.Length = lastUniqueIndex + 1;
                }
                // Allocate and populate query data
                queryData = (EntityQueryData*)ChunkAllocate<EntityQueryData>();
                queryData->RequiredComponentsCount = requiredComponentCount;
                queryData->RequiredComponents = (ComponentType*)ChunkAllocate<ComponentType>(requiredComponentCount, requiredComponents);
                queryData->EnableableComponentTypeIndexCount = queryIgnoresEnabledBits ? 0 : allEnableableTypeIndices.Length;
                queryData->EnableableComponentTypeIndices = (TypeIndex*)ChunkAllocate<TypeIndex>(allEnableableTypeIndices.Length, allEnableableTypeIndices.GetUnsafeReadOnlyPtr());
                queryData->HasEnableableComponents = (allEnableableTypeIndices.Length > 0 && !queryIgnoresEnabledBits) ? (byte)1 : (byte)0;

                InitializeReaderWriter(queryData, requiredComponents, requiredComponentCount,
                    enableableTypesInAny.GetUnsafeReadOnlyPtr<ComponentType>(), enableableTypesInAny.Length);
                queryData->ArchetypeQueryCount = archetypeQueryCount;
                queryData->ArchetypeQueries = (ArchetypeQuery*)ChunkAllocate<ArchetypeQuery>(archetypeQueryCount, archetypeQueries);
                for (var i = 0; i < archetypeQueryCount; ++i)
                {
                    queryData->ArchetypeQueries[i].All = (TypeIndex*)ChunkAllocate<TypeIndex>(queryData->ArchetypeQueries[i].AllCount, archetypeQueries[i].All);
                    queryData->ArchetypeQueries[i].Any = (TypeIndex*)ChunkAllocate<TypeIndex>(queryData->ArchetypeQueries[i].AnyCount, archetypeQueries[i].Any);
                    queryData->ArchetypeQueries[i].None = (TypeIndex*)ChunkAllocate<TypeIndex>(queryData->ArchetypeQueries[i].NoneCount, archetypeQueries[i].None);
                    queryData->ArchetypeQueries[i].Disabled = (TypeIndex*)ChunkAllocate<TypeIndex>(queryData->ArchetypeQueries[i].DisabledCount, archetypeQueries[i].Disabled);
                    queryData->ArchetypeQueries[i].Absent = (TypeIndex*)ChunkAllocate<TypeIndex>(queryData->ArchetypeQueries[i].AbsentCount, archetypeQueries[i].Absent);
                    queryData->ArchetypeQueries[i].Present = (TypeIndex*)ChunkAllocate<TypeIndex>(queryData->ArchetypeQueries[i].PresentCount, archetypeQueries[i].Present);
                    queryData->ArchetypeQueries[i].AllAccessMode = (byte*)ChunkAllocate<byte>(queryData->ArchetypeQueries[i].AllCount, archetypeQueries[i].AllAccessMode);
                    queryData->ArchetypeQueries[i].AnyAccessMode = (byte*)ChunkAllocate<byte>(queryData->ArchetypeQueries[i].AnyCount, archetypeQueries[i].AnyAccessMode);
                    queryData->ArchetypeQueries[i].NoneAccessMode = (byte*)ChunkAllocate<byte>(queryData->ArchetypeQueries[i].NoneCount, archetypeQueries[i].NoneAccessMode);
                    queryData->ArchetypeQueries[i].DisabledAccessMode = (byte*)ChunkAllocate<byte>(queryData->ArchetypeQueries[i].DisabledCount, archetypeQueries[i].DisabledAccessMode);
                    queryData->ArchetypeQueries[i].AbsentAccessMode = (byte*)ChunkAllocate<byte>(queryData->ArchetypeQueries[i].AbsentCount, archetypeQueries[i].AbsentAccessMode);
                    queryData->ArchetypeQueries[i].PresentAccessMode = (byte*)ChunkAllocate<byte>(queryData->ArchetypeQueries[i].PresentCount, archetypeQueries[i].PresentAccessMode);
                }

                var ecs = access->EntityComponentStore;

                queryData->MatchingArchetypes = new UnsafeMatchingArchetypePtrList(access->EntityComponentStore);
                queryData->CreateMatchingChunkCache(access->EntityComponentStore);

                queryData->EntityQueryMask = new EntityQueryMask();

                for (var i = 0; i < ecs->m_Archetypes.Length; ++i)
                {
                    var archetype = ecs->m_Archetypes.Ptr[i];
                    AddArchetypeIfMatching(archetype, queryData);
                }

                entityQueryCache.Add(hash, m_EntityQueryDatas.Length);
                m_EntityQueryDatas.Add(queryData);
                queryData->InvalidateChunkCache();
            }

            return EntityQuery.Construct(queryData, access);
        }

        void InitializeReaderWriter(EntityQueryData* queryData, ComponentType* requiredTypes, int requiredCount,
            ComponentType* implicitReadTypes, int implicitReadCount)
        {
            Assert.IsTrue(requiredCount > 0);
            Assert.IsTrue(requiredTypes[0] == ComponentType.ReadWrite<Entity>());

            queryData->ReaderTypesCount = 0;
            queryData->WriterTypesCount = 0;

            for (var i = 1; i != requiredCount; i++)
            {
                if (requiredTypes[i].IsZeroSized && !requiredTypes[i].IsEnableable)
                    continue;

                switch (requiredTypes[i].AccessModeType)
                {
                    case ComponentType.AccessMode.ReadOnly:
                    case ComponentType.AccessMode.Exclude: // we must include required excluded types as readers in case they're enableable; see DOTS-9730
                        queryData->ReaderTypesCount++;
                        break;
                    case ComponentType.AccessMode.ReadWrite:
                        queryData->WriterTypesCount++;
                        break;
                }
            }
            queryData->ReaderTypesCount += implicitReadCount;

            queryData->ReaderTypes = (TypeIndex*)m_EntityQueryDataChunkAllocator.Allocate(sizeof(TypeIndex) * queryData->ReaderTypesCount, 4);
            queryData->WriterTypes = (TypeIndex*)m_EntityQueryDataChunkAllocator.Allocate(sizeof(TypeIndex) * queryData->WriterTypesCount, 4);

            var curReader = 0;
            var curWriter = 0;
            for (var i = 1; i != requiredCount; i++)
            {
                if (requiredTypes[i].IsZeroSized && !requiredTypes[i].IsEnableable)
                    continue;

                switch (requiredTypes[i].AccessModeType)
                {
                    case ComponentType.AccessMode.ReadOnly:
                    case ComponentType.AccessMode.Exclude: // we must include required excluded types as readers in case they're enableable; see DOTS-9730
                        queryData->ReaderTypes[curReader++] = requiredTypes[i].TypeIndex;
                        break;
                    case ComponentType.AccessMode.ReadWrite:
                        queryData->WriterTypes[curWriter++] = requiredTypes[i].TypeIndex;
                        break;
                }
            }
            for (var i = 0; i < implicitReadCount; i++)
            {
                queryData->ReaderTypes[curReader++] = implicitReadTypes[i].TypeIndex;
            }
        }

        public void AddAdditionalArchetypes(UnsafePtrList<Archetype> archetypeList)
        {
            for (int i = 0; i < archetypeList.Length; i++)
            {
                for (var g = 0; g < m_EntityQueryDatas.Length; ++g)
                {
                    var grp = m_EntityQueryDatas.Ptr[g];
                    AddArchetypeIfMatching(archetypeList.Ptr[i], grp);
                }
            }
        }

        // This method is called when either a new Archetype or a new EntityQuery is created. If the archetype
        // matches any of the query's ArchetypeQuery elements, we add a new MatchingArchetype element to the query
        // corresponding to this archetype.
        void AddArchetypeIfMatching(Archetype* archetype, EntityQueryData* query)
        {
            // TODO(DOTS-8787): Problems can occur if the archetype matches more than one of the query's ArchetypeQuery array.
            int firstMatchingArchetypeQueryIndex = -1;
            for (int i = 0; i != query->ArchetypeQueryCount; i++)
            {
                if (IsMatchingArchetype(archetype, query->ArchetypeQueries + i))
                {
                    firstMatchingArchetypeQueryIndex = i;
                    break;
                }
            }
            if (firstMatchingArchetypeQueryIndex == -1)
                return;
            ref var archetypeQuery = ref query->ArchetypeQueries[firstMatchingArchetypeQueryIndex];

            var match = MatchingArchetype.Create(ref m_EntityQueryDataChunkAllocator, archetype,
                archetypeQuery, query->RequiredComponentsCount);
            var typeIndexInArchetypeArray = match->IndexInArchetype;

            match->Archetype->SetMask(query->EntityQueryMask);

            query->MatchingArchetypes.Add(match);

            // Add back pointer from archetype to query data
            archetype->MatchingQueryData.Add((IntPtr)query);

            var typeComponentIndex = 0;
            for (var component = 0; component < query->RequiredComponentsCount; ++component)
            {
                if (query->RequiredComponents[component].AccessModeType != ComponentType.AccessMode.Exclude)
                {
                    typeComponentIndex = ChunkDataUtility.GetNextIndexInTypeArray(archetype, query->RequiredComponents[component].TypeIndex, typeComponentIndex);
                    Assert.AreNotEqual(-1, typeComponentIndex);
                    typeIndexInArchetypeArray[component] = typeComponentIndex;
                }
                else
                {
                    typeIndexInArchetypeArray[component] = -1;
                }
            }

            var enableableAllCount = 0;
            typeComponentIndex = 0;
            for (var i = 0; i < archetypeQuery.AllCount; ++i)
            {
                var typeIndex = archetypeQuery.All[i];
                if (TypeManager.IsEnableable(typeIndex))
                {
                    typeComponentIndex = ChunkDataUtility.GetNextIndexInTypeArray(archetype, typeIndex, typeComponentIndex);
                    Assert.AreNotEqual(-1, typeComponentIndex);
                    match->EnableableTypeMemoryOrderInArchetype_All[enableableAllCount++] = archetype->TypeIndexInArchetypeToMemoryOrderIndex[typeComponentIndex];
                }
            }

            var enableableNoneCount = 0;
            typeComponentIndex = 0;
            for (var i = 0; i < archetypeQuery.NoneCount; ++i)
            {
                var typeIndex = archetypeQuery.None[i];
                if (TypeManager.IsEnableable(typeIndex))
                {
                    var currentTypeComponentIndex = ChunkDataUtility.GetNextIndexInTypeArray(archetype, typeIndex, typeComponentIndex);
                    if (currentTypeComponentIndex != -1) // we skip storing "None" component types for matching archetypes that do not contain the "None" type (there are no bits to check)
                    {
                        match->EnableableTypeMemoryOrderInArchetype_None[enableableNoneCount++] = archetype->TypeIndexInArchetypeToMemoryOrderIndex[currentTypeComponentIndex];
                        typeComponentIndex = currentTypeComponentIndex;
                    }
                }
            }

            // If the archetype contains any of the required non-enableable Any types, then we know every entity in this
            // archetype definitely meets this query's Any constraint; we don't need to perform any per-entity checks
            // on any enableable components in the Any list. We've already set the MatchingArchetype's Any count to zero in this
            // case, but we also need to skip storing these types into the EnableableTypeMemoryOrderInArchetype_Any array.
            if (match->EnableableComponentsCount_Any > 0)
            {
                var enableableAnyCount = 0;
                typeComponentIndex = 0;
                for (var i = 0; i < archetypeQuery.AnyCount; ++i)
                {
                    var typeIndex = archetypeQuery.Any[i];
                    if (TypeManager.IsEnableable(typeIndex))
                    {
                        var currentTypeComponentIndex =
                            ChunkDataUtility.GetNextIndexInTypeArray(archetype, typeIndex, typeComponentIndex);
                        // The archetype might not contain *all* the Any types (by definition; this is the whole point of Any).
                        // Skip storing the missing types.
                        if (currentTypeComponentIndex != -1)
                        {
                            match->EnableableTypeMemoryOrderInArchetype_Any[enableableAnyCount++] =
                                archetype->TypeIndexInArchetypeToMemoryOrderIndex[currentTypeComponentIndex];
                            typeComponentIndex = currentTypeComponentIndex;
                        }
                    }
                }
            }

            var enableableDisabledCount = 0;
            typeComponentIndex = 0;
            for (var i = 0; i < archetypeQuery.DisabledCount; ++i)
            {
                var typeIndex = archetypeQuery.Disabled[i];
                if (TypeManager.IsEnableable(typeIndex))
                {
                    typeComponentIndex = ChunkDataUtility.GetNextIndexInTypeArray(archetype, typeIndex, typeComponentIndex);
                    Assert.AreNotEqual(-1, typeComponentIndex);
                    match->EnableableTypeMemoryOrderInArchetype_Disabled[enableableDisabledCount++] = archetype->TypeIndexInArchetypeToMemoryOrderIndex[typeComponentIndex];
                }
            }

            // Absent types aren't handled because they're not present in the archetype at all, by definition.
            // Present types aren't handled because we don't care if they're enabled or not, by definition.
        }

        static bool IsMatchingArchetype(Archetype* archetype, EntityQueryData* query)
        {
            for (int i = 0; i != query->ArchetypeQueryCount; i++)
            {
                if (IsMatchingArchetype(archetype, query->ArchetypeQueries + i))
                {
                    return true;
                }
            }

            return false;
        }

        static bool IsMatchingArchetype(Archetype* archetype, ArchetypeQuery* query)
        {
            ulong archetypeBloomFilterMask = archetype->BloomFilterMask;
            // Bloom-filter-based early out, if the required components can't possibly be present in this archetype.
            if (Hint.Likely((query->AllBloomFilterMask & archetypeBloomFilterMask) != query->AllBloomFilterMask))
                return false;

            // Early-out at the archetype level
            var options = query->Options;
            var includeDisabledEntities = (options & EntityQueryOptions.IncludeDisabledEntities) != 0;
            var includePrefab = (options & EntityQueryOptions.IncludePrefab) != 0;
            var includeSystems = (options & EntityQueryOptions.IncludeSystems) != 0;
            var includeChunkHeader = (options & EntityQueryOptions.IncludeMetaChunks) != 0;
            if (archetype->Disabled && !includeDisabledEntities)
                return false;
            if (archetype->Prefab && !includePrefab)
                return false;
            if (archetype->HasSystemInstanceComponents && !includeSystems)
                return false;
            if (archetype->HasChunkHeader && !includeChunkHeader)
                return false;


            var archetypeTypes = archetype->Types;
            var archetypeTypesCount = archetype->TypesCount;
            // More early-outs: if we require more components than are in the archetype, it obviously doesn't match
            if (archetypeTypesCount < query->AllCount ||
                archetypeTypesCount < query->DisabledCount ||
                archetypeTypesCount < query->PresentCount)
            {
                return false;
            }

            return
                TestMatchingArchetypeRequiredComponent(archetypeTypes, archetypeTypesCount, query->All, query->AllCount)
                && TestMatchingArchetypeRequiredComponent(archetypeTypes, archetypeTypesCount, query->Disabled, query->DisabledCount)
                && TestMatchingArchetypeRequiredComponent(archetypeTypes, archetypeTypesCount, query->Present, query->PresentCount)
                && TestMatchingArchetypeOptional(archetypeTypes, archetypeTypesCount, query->Any, query->AnyCount)
                && TestMatchingArchetypeExcludedComponent(archetypeTypes, archetypeTypesCount, query->None, query->NoneCount,
                    ignoreEnableableTypes:true)
                && TestMatchingArchetypeExcludedComponent(archetypeTypes, archetypeTypesCount, query->Absent, query->AbsentCount,
                    ignoreEnableableTypes:false);
        }

        static bool TestMatchingArchetypeRequiredComponent(ComponentTypeInArchetype* archetypeTypes,
            int archetypeTypesCount, TypeIndex* queryTypes, int queryTypesCount)
        {
            if (queryTypesCount == 0)
                return true; // no types to search for
            int iNextQueryType = 0;
            TypeIndex nextQueryType = queryTypes[iNextQueryType];
            for (var i = 0; i < archetypeTypesCount; i++)
            {
                var componentTypeIndex = archetypeTypes[i].TypeIndex;
                if (Hint.Unlikely(componentTypeIndex == nextQueryType))
                {
                    if (++iNextQueryType == queryTypesCount)
                        return true; // Ran out of query types; all required types were found.
                    nextQueryType = queryTypes[iNextQueryType];
                }
                else if (componentTypeIndex > nextQueryType)
                    return false; // A required type was not found
            }
            // Ran out of archetype types & didn't find all required types
            return false;
        }

        static bool TestMatchingArchetypeOptional(ComponentTypeInArchetype* archetypeTypes,
            int archetypeTypesCount, TypeIndex* queryTypes, int queryTypesCount)
        {
            if (queryTypesCount == 0)
                return true; // no types to search for
            int iNextQueryType = 0;
            TypeIndex nextQueryType = queryTypes[iNextQueryType];
            for (var i = 0; i < archetypeTypesCount; i++)
            {
                var componentTypeIndex = archetypeTypes[i].TypeIndex;
                while(componentTypeIndex > nextQueryType)
                {
                    if (++iNextQueryType == queryTypesCount)
                        return false; // Ran out of optional types and didn't find at least one of them
                    nextQueryType = queryTypes[iNextQueryType];
                }
                if (Hint.Unlikely(componentTypeIndex == nextQueryType))
                    return true; // found at least one optional type
            }
            // Ran out of archetype types & didn't find at least one optional type
            return false;
        }

        static bool TestMatchingArchetypeExcludedComponent(ComponentTypeInArchetype* archetypeTypes,
            int archetypeTypesCount, TypeIndex* queryTypes, int queryTypesCount, bool ignoreEnableableTypes)
        {
            TypeIndex* filteredQueryTypes = stackalloc TypeIndex[queryTypesCount];
            if (ignoreEnableableTypes)
            {
                int filteredQueryTypesCount = 0;
                for (int i = 0; i < queryTypesCount; ++i)
                {
                    if (!TypeManager.IsEnableable(queryTypes[i]))
                        filteredQueryTypes[filteredQueryTypesCount++] = queryTypes[i];
                }
                queryTypes = filteredQueryTypes;
                queryTypesCount = filteredQueryTypesCount;
            }
            if (queryTypesCount == 0)
                return true; // no types to search for
            int iNextQueryType = 0;
            TypeIndex nextQueryType = queryTypes[iNextQueryType];
            for (var i = 0; i < archetypeTypesCount; i++)
            {
                var componentTypeIndex = archetypeTypes[i].TypeIndex;
                while (componentTypeIndex > nextQueryType)
                {
                    if (++iNextQueryType == queryTypesCount)
                        return true; // Ran out of query types. No excluded types were found
                    nextQueryType = queryTypes[iNextQueryType];
                }
                if (Hint.Unlikely(componentTypeIndex == nextQueryType))
                    return false; // An excluded type was found in archetype
            }
            // Ran out of archetype types & didn't find any excluded types
            return true;
        }

        public static int FindMatchingArchetypeIndexForArchetype(ref UnsafeMatchingArchetypePtrList matchingArchetypes,
            Archetype* archetype)
        {
            int archetypeCount = matchingArchetypes.Length;
            var ptrs = matchingArchetypes.Ptr;
            for (int i = 0; i < archetypeCount; ++i)
            {
                if (archetype == ptrs[i]->Archetype)
                    return i;
            }

            return -1;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        private static void ThrowIfEntityQueryMasksIsGreaterThanLimit(int entityQueryMasksAllocated)
        {
            if (entityQueryMasksAllocated >= 1024)
                throw new Exception("You have reached the limit of 1024 unique EntityQueryMasks, and cannot generate any more.");
        }

        public EntityQueryMask GetEntityQueryMask(EntityQueryData* query, EntityComponentStore* ecStore)
        {
            if (query->EntityQueryMask.IsCreated())
                return query->EntityQueryMask;

            ThrowIfEntityQueryMasksIsGreaterThanLimit(m_EntityQueryMasksAllocated);

            var mask = new EntityQueryMask(
                (byte)(m_EntityQueryMasksAllocated / 8),
                (byte)(1 << (m_EntityQueryMasksAllocated % 8)),
                ecStore);

            m_EntityQueryMasksAllocated++;

            int archetypeCount = query->MatchingArchetypes.Length;
            var ptrs = query->MatchingArchetypes.Ptr;
            for (var i = 0; i < archetypeCount; ++i)
            {
                ptrs[i]->Archetype->QueryMaskArray[mask.Index] |= mask.Mask;
            }

            query->EntityQueryMask = mask;

            return mask;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    unsafe struct MatchingArchetype
    {
        public Archetype* Archetype;
        public int RequiredComponentCount;
        public int EnableableComponentsCount_All;
        public int EnableableComponentsCount_None;
        public int EnableableComponentsCount_Any;
        public int EnableableComponentsCount_Disabled; // TODO(DOTS-7809): the None and Disabled lists can be combined here, as they're treated identically in all cases.
        // No need to count enableable absent components, since they're not in the archetype (by definition)
        // No need to count enableable present components, since we don't care if they're enabled or not (by definition)

        public fixed int IndexInArchetype[1];

        public int* EnableableTypeMemoryOrderInArchetype_All
        {
            get
            {
                fixed (int* basePtr = IndexInArchetype)
                {
                    return basePtr + RequiredComponentCount;
                }
            }
        }
        public int* EnableableTypeMemoryOrderInArchetype_None
        {
            get
            {
                fixed (int* basePtr = IndexInArchetype)
                {
                    return basePtr + RequiredComponentCount + EnableableComponentsCount_All;
                }
            }
        }
        public int* EnableableTypeMemoryOrderInArchetype_Any
        {
            get
            {
                fixed (int* basePtr = IndexInArchetype)
                {
                    return basePtr + RequiredComponentCount + EnableableComponentsCount_All + EnableableComponentsCount_None;
                }
            }
        }
        public int* EnableableTypeMemoryOrderInArchetype_Disabled
        {
            get
            {
                fixed (int* basePtr = IndexInArchetype)
                {
                    return basePtr + RequiredComponentCount + EnableableComponentsCount_All + EnableableComponentsCount_None + EnableableComponentsCount_Any;
                }
            }
        }
        public static int CalculateMatchingArchetypeEnableableTypeIntersectionCount(Archetype* archetype, TypeIndex* queryComponents, int queryComponentCount)
        {
            var intersectionCount = 0;
            var typeComponentIndex = 0;
            for (int i = 0; i < queryComponentCount; ++i)
            {
                var typeIndex = queryComponents[i];
                if (!TypeManager.IsEnableable(typeIndex))
                    continue;
                var currentTypeComponentIndex = ChunkDataUtility.GetNextIndexInTypeArray(archetype, typeIndex, typeComponentIndex);
                if (currentTypeComponentIndex >= 0)
                {
                    typeComponentIndex = currentTypeComponentIndex;
                    intersectionCount++;
                }
            }

            return intersectionCount;
        }

        public static MatchingArchetype* Create(ref BlockAllocator allocator, Archetype* archetype, in ArchetypeQuery archetypeQuery,
            int queryRequiredComponentCount)
        {
            // If the archetype matches any of the non-enableable Any types in this archetypeQuery, we don't need to do
            // any per-entity checks on the enableable Any types. Force the count to zero in this case.
            //
            // This is not just an optimization; it's necessary for correctness. Otherwise, an entity with components
            // T1 (non-enableable) and T2 (enableable) would fail to match a query with WithAny<T1,T2> if T2 is disabled.
            // (This was a bug for the first several versions of Entities 1.x -- see ECSB-809).
            //
            // Both Archetype.Types[] and ArchetypeQuery.Any[] are assumed to be sorted by TypeIndex, so we can
            // implement this as a single loop instead of N^2.
            bool archetypeMatchesNonEnableableAnyTypes = false;
            int iA = 0, iAQ = 0;
            while (iA < archetype->TypesCount && iAQ < archetypeQuery.AnyCount)
            {
                TypeIndex typeAQ = archetypeQuery.Any[iAQ];
                if (typeAQ.IsEnableable)
                {
                    iAQ++;
                    continue;
                }
                TypeIndex typeA = archetype->Types[iA].TypeIndex;
                if (typeAQ < typeA)
                    iAQ++;
                else if (typeA < typeAQ)
                    iA++;
                else // non-enableable typeA == typeAQ
                {
                    archetypeMatchesNonEnableableAnyTypes = true;
                    break;
                }
            }

            var enableableAllCount = CalculateMatchingArchetypeEnableableTypeIntersectionCount(archetype, archetypeQuery.All, archetypeQuery.AllCount);
            var enableableNoneCount = CalculateMatchingArchetypeEnableableTypeIntersectionCount(archetype, archetypeQuery.None, archetypeQuery.NoneCount);
            var enableableAnyCount = archetypeMatchesNonEnableableAnyTypes
                ? 0 : CalculateMatchingArchetypeEnableableTypeIntersectionCount(archetype, archetypeQuery.Any, archetypeQuery.AnyCount);
            var enableableDisabledCount = CalculateMatchingArchetypeEnableableTypeIntersectionCount(archetype, archetypeQuery.Disabled, archetypeQuery.DisabledCount);
            var totalEnableableTypeCount = enableableAllCount + enableableNoneCount + enableableAnyCount + enableableDisabledCount;
            var match = (MatchingArchetype*)allocator.Allocate(GetAllocationSize(queryRequiredComponentCount, totalEnableableTypeCount), 8);
            match->Archetype = archetype;

            match->RequiredComponentCount = queryRequiredComponentCount;
            match->EnableableComponentsCount_All = enableableAllCount;
            match->EnableableComponentsCount_None = enableableNoneCount;
            match->EnableableComponentsCount_Any = enableableAnyCount;
            match->EnableableComponentsCount_Disabled = enableableDisabledCount;

            return match;
        }

        private static int GetAllocationSize(int requiredComponentsCount, int enableableComponentCount)
        {
            return sizeof(MatchingArchetype) +
                   sizeof(int) * (requiredComponentsCount-1) +
                   sizeof(int) * enableableComponentCount;
        }

        public bool ChunkMatchesFilter(int chunkIndex, ref EntityQueryFilter filter)
        {
            var chunks = Archetype->Chunks;

            // Must match ALL shared component data
            for (int i = 0; i < filter.Shared.Count; ++i)
            {
                var indexInEntityQuery = filter.Shared.IndexInEntityQuery[i];
                var sharedComponentIndex = filter.Shared.SharedComponentIndex[i];
                var componentIndexInChunk = IndexInArchetype[indexInEntityQuery] - Archetype->FirstSharedComponent;
                var sharedComponents = chunks.GetSharedComponentValueArrayForType(componentIndexInChunk);

                // if we don't have a match, we can early out
                if (sharedComponents[chunkIndex] != sharedComponentIndex)
                    return false;
            }

            if (filter.Changed.Count == 0 && !filter.UseOrderFiltering)
                return true;

            var orderVersionFilterPassed = filter.UseOrderFiltering && ChangeVersionUtility.DidChange(chunks.GetOrderVersion(chunkIndex), filter.RequiredChangeVersion);

            // Must have AT LEAST ONE type have changed
            var changedVersionFilterPassed = false;
            for (int i = 0; i < filter.Changed.Count; ++i)
            {
                var indexInEntityQuery = filter.Changed.IndexInEntityQuery[i];
                var componentIndexInChunk = IndexInArchetype[indexInEntityQuery];
                var changeVersions = chunks.GetChangeVersionArrayForType(componentIndexInChunk);

                var requiredVersion = filter.RequiredChangeVersion;

                changedVersionFilterPassed |= ChangeVersionUtility.DidChange(changeVersions[chunkIndex], requiredVersion);
            }

            return changedVersionFilterPassed || orderVersionFilterPassed;
        }
    }

    [DebuggerTypeProxy(typeof(UnsafeMatchingArchetypePtrListDebugView))]
    unsafe struct UnsafeMatchingArchetypePtrList
    {
        [NativeDisableUnsafePtrRestriction]
        private UnsafeList<IntPtr>* ListData;

        public MatchingArchetype** Ptr { get => (MatchingArchetype**)ListData->Ptr; }
        public int Length { get => ListData->Length; }

        public void Dispose() { UnsafeList<IntPtr>.Destroy(ListData); }
        public void Add(void* t) { ListData->Add((IntPtr)t); }

        [NativeDisableUnsafePtrRestriction]
        public EntityComponentStore* entityComponentStore;

        public UnsafeMatchingArchetypePtrList(EntityComponentStore* entityComponentStore)
        {
            ListData = UnsafeList<IntPtr>.Create(0, Allocator.Persistent);
            this.entityComponentStore = entityComponentStore;
        }
    }

    [GenerateTestsForBurstCompatibility]
    unsafe struct ArchetypeQuery : IEquatable<ArchetypeQuery>
    {
        // The TypeIndex arrays in this struct are sorted by TypeIndex inside CreateArchetypeQueries().
        public TypeIndex*   Any;
        public byte*        AnyAccessMode;
        public int          AnyCount;

        public TypeIndex*   All;
        public byte*        AllAccessMode;
        public int          AllCount;
        public ulong        AllBloomFilterMask;

        public TypeIndex*   None;
        public byte*        NoneAccessMode;
        public int          NoneCount;

        public TypeIndex*   Disabled;
        public byte*        DisabledAccessMode;
        public int          DisabledCount;

        public TypeIndex*   Absent;
        public byte*        AbsentAccessMode;
        public int          AbsentCount;

        public TypeIndex*   Present;
        public byte*        PresentAccessMode;
        public int          PresentCount;

        public EntityQueryOptions  Options;

        public bool Equals(ArchetypeQuery other)
        {
            if (AnyCount != other.AnyCount)
                return false;
            if (AllCount != other.AllCount)
                return false;
            if (NoneCount != other.NoneCount)
                return false;
            if (DisabledCount != other.DisabledCount)
                return false;
            if (AbsentCount != other.AbsentCount)
                return false;
            if (PresentCount != other.PresentCount)
                return false;
            if (AnyCount > 0 && UnsafeUtility.MemCmp(Any, other.Any, sizeof(int) * AnyCount) != 0 &&
                UnsafeUtility.MemCmp(AnyAccessMode, other.AnyAccessMode, sizeof(byte) * AnyCount) != 0)
                return false;
            if (AllCount > 0 && UnsafeUtility.MemCmp(All, other.All, sizeof(int) * AllCount) != 0 &&
                UnsafeUtility.MemCmp(AllAccessMode, other.AllAccessMode, sizeof(byte) * AllCount) != 0)
                return false;
            if (NoneCount > 0 && UnsafeUtility.MemCmp(None, other.None, sizeof(int) * NoneCount) != 0 &&
                UnsafeUtility.MemCmp(NoneAccessMode, other.NoneAccessMode, sizeof(byte) * NoneCount) != 0)
                return false;
            if (DisabledCount > 0 && UnsafeUtility.MemCmp(Disabled, other.Disabled, sizeof(int) * DisabledCount) != 0 &&
                UnsafeUtility.MemCmp(DisabledAccessMode, other.DisabledAccessMode, sizeof(byte) * DisabledCount) != 0)
                return false;
            if (AbsentCount > 0 && UnsafeUtility.MemCmp(Absent, other.Absent, sizeof(int) * AbsentCount) != 0 &&
                UnsafeUtility.MemCmp(AbsentAccessMode, other.AbsentAccessMode, sizeof(byte) * AbsentCount) != 0)
                return false;
            if (PresentCount > 0 && UnsafeUtility.MemCmp(Present, other.Present, sizeof(int) * PresentCount) != 0 &&
                UnsafeUtility.MemCmp(PresentAccessMode, other.PresentAccessMode, sizeof(byte) * PresentCount) != 0)
                return false;
            if (Options != other.Options)
                return false;

            return true;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = AnyCount + 1;
                hashCode = 397 * hashCode ^ (AllCount + 1);
                hashCode = 397 * hashCode ^ (NoneCount + 1);
                hashCode = 397 * hashCode ^ (DisabledCount + 1);
                hashCode = 397 * hashCode ^ (AbsentCount + 1);
                hashCode = 397 * hashCode ^ (PresentCount + 1);
                hashCode = 397 * hashCode ^ (int)Options;
                hashCode = (int)math.hash(Any, sizeof(int) * AnyCount, (uint)hashCode);
                hashCode = (int)math.hash(All, sizeof(int) * AllCount, (uint)hashCode);
                hashCode = (int)math.hash(None, sizeof(int) * NoneCount, (uint)hashCode);
                hashCode = (int)math.hash(Disabled, sizeof(int) * DisabledCount, (uint)hashCode);
                hashCode = (int)math.hash(Absent, sizeof(int) * AbsentCount, (uint)hashCode);
                hashCode = (int)math.hash(Present, sizeof(int) * PresentCount, (uint)hashCode);
                hashCode = (int)math.hash(AnyAccessMode, sizeof(byte) * AnyCount, (uint)hashCode);
                hashCode = (int)math.hash(AllAccessMode, sizeof(byte) * AllCount, (uint)hashCode);
                hashCode = (int)math.hash(NoneAccessMode, sizeof(byte) * NoneCount, (uint)hashCode);
                hashCode = (int)math.hash(DisabledAccessMode, sizeof(byte) * DisabledCount, (uint)hashCode);
                hashCode = (int)math.hash(AbsentAccessMode, sizeof(byte) * AbsentCount, (uint)hashCode);
                hashCode = (int)math.hash(PresentAccessMode, sizeof(byte) * PresentCount, (uint)hashCode);
                return hashCode;
            }
        }
    }

    [NoAlias]
    [BurstCompile]
    unsafe struct UnsafeCachedChunkList
    {
        [NoAlias,NativeDisableUnsafePtrRestriction]
        internal UnsafeList<ChunkIndex>* MatchingChunks;

        [NoAlias,NativeDisableUnsafePtrRestriction]
        internal UnsafeList<int>* PerChunkMatchingArchetypeIndex;

        [NoAlias,NativeDisableUnsafePtrRestriction]
        internal UnsafeList<int>* ChunkIndexInArchetype;

        [NoAlias,NativeDisableUnsafePtrRestriction]
        internal EntityComponentStore* EntityComponentStore;

        internal int CacheValid; // must not be a bool, for Burst compatibility

        internal ChunkIndex* ChunkIndices { get => MatchingChunks->Ptr; }
        public int Length { get => MatchingChunks->Length; }
        public bool IsCacheValid { get => CacheValid != 0; }

        internal UnsafeCachedChunkList(EntityComponentStore* entityComponentStore)
        {
            EntityComponentStore = entityComponentStore;
            MatchingChunks = UnsafeList<ChunkIndex>.Create(0, Allocator.Persistent);
            PerChunkMatchingArchetypeIndex = UnsafeList<int>.Create(0, Allocator.Persistent);
            ChunkIndexInArchetype = UnsafeList<int>.Create(0, Allocator.Persistent);
            CacheValid = 0;
        }

        internal void Append(ChunkIndex* t, int addChunkCount, int matchingArchetypeIndex)
        {
            MatchingChunks->AddRange(new UnsafeList<ChunkIndex>(t, addChunkCount));
            for (int i = 0; i < addChunkCount; ++i)
            {
                PerChunkMatchingArchetypeIndex->Add(matchingArchetypeIndex);
                ChunkIndexInArchetype->Add(i);
            }
        }

        internal void Dispose()
        {
            if (MatchingChunks != null)
                UnsafeList<ChunkIndex>.Destroy(MatchingChunks);
            if (PerChunkMatchingArchetypeIndex != null)
                UnsafeList<int>.Destroy(PerChunkMatchingArchetypeIndex);
            if (ChunkIndexInArchetype != null)
                UnsafeList<int>.Destroy(ChunkIndexInArchetype);
        }

        internal void Invalidate()
        {
            CacheValid = 0;
        }

        [BurstCompile]
        internal static void Rebuild(ref UnsafeCachedChunkList cache, in EntityQueryData queryData)
        {
            cache.MatchingChunks->Clear();
            cache.PerChunkMatchingArchetypeIndex->Clear();
            cache.ChunkIndexInArchetype->Clear();

            int archetypeCount = queryData.MatchingArchetypes.Length;
            var ptrs = queryData.MatchingArchetypes.Ptr;
            for (int matchingArchetypeIndex = 0; matchingArchetypeIndex < archetypeCount; ++matchingArchetypeIndex)
            {
                var archetype = ptrs[matchingArchetypeIndex]->Archetype;
                if (archetype->EntityCount > 0)
                    archetype->Chunks.AddToCachedChunkList(ref cache, matchingArchetypeIndex);
            }

            cache.CacheValid = 1;
        }

        // Expensive debug validation, to make sure cached data is consistent with the associated query.
        // Throws an ArgumentException if anything's wrong, including if the cache.IsCacheValid flag is clear.
        // The forceCheckInvalidCache allows the validity flag check to skipped, e.g. by unit tests that want to
        // make sure this method actually detects the expected inconsistencies in a known invalid cache.
        internal static void AssertIsConsistent(in UnsafeCachedChunkList cache, in EntityQueryData queryData, bool forceCheckInvalidCache)
        {
            // If the input cache doesn't even think it's valid, its contents are definitely not guaranteed to be consistent.
            // Allow the caller to skip this check and check a known invalid cache any, for unit testing purposes
            if (!forceCheckInvalidCache && !cache.IsCacheValid)
            {
                throw new ArgumentException($"Can't check an invalid cache for consistency. Did you call queryData.GetMatchingChunkCache() to access this cache?");
            }
            // The cache's lists must all be the same length
            if (cache.ChunkIndexInArchetype->Length != cache.Length)
            {
                // This indicates an error in the cache update logic; every chunk in the cache must have a valid entry in the ChunkIndexInArchetype list.
                throw new ArgumentException($"query chunk cache inconsistency: ChunkIndexInArchetypeLength={cache.ChunkIndexInArchetype->Length}, cache length={cache.Length}");
            }
            if (cache.PerChunkMatchingArchetypeIndex->Length != cache.Length)
            {
                // This indicates an error in the cache update logic; every chunk in the cache must have a valid entry in the PerChunkMatchingArchetypeIndex list.
                throw new ArgumentException($"query chunk cache inconsistency: PerChunkMatchingArchetypeIndex={cache.PerChunkMatchingArchetypeIndex->Length}, cache length={cache.Length}");
            }

            // count the expected number of chunks in all matching archetypes
            int archetypeCount = queryData.MatchingArchetypes.Length;
            var matchingArchetypes = queryData.MatchingArchetypes.Ptr;
            int matchingArchetypeChunkCount = 0;
            for (int archetypeIndex = 0; archetypeIndex < archetypeCount; ++archetypeIndex)
            {
                var archetype = matchingArchetypes[archetypeIndex]->Archetype;
                matchingArchetypeChunkCount += archetype->Chunks.Count;
            }
            // If this fails, a cache that thinks it's valid definitely contains the wrong number of chunks.
            // This would indicate an error in the cache update logic.
            // However, instead of failing immediately, let's keep going and see if we can log more data about the missing/extra
            // chunk in the loop below. This same check appears at the end of the function, in case nothing else fires.
            //if (matchingArchetypeChunkCount != cache.Length)
            //{
            //    //throw new ArgumentException($"query chunk cache inconsistency: {archetypeCount} matching archetypes contain {matchingArchetypeChunkCount} chunks, but cache only contains {cache.MatchingChunks->Length} chunks");
            //}

            // Iterate over chunks in matching archetypes, and make sure they're at the correct location in the cache
            var chunkCounter = 0;
            for (int archetypeIndex = 0; archetypeIndex < archetypeCount; ++archetypeIndex)
            {
                var archetype = matchingArchetypes[archetypeIndex]->Archetype;
                int archetypeChunkCount = archetype->Chunks.Count;
                for (int chunkIndex = 0; chunkIndex < archetypeChunkCount; ++chunkIndex)
                {
                    if (chunkCounter >= cache.Length)
                    {
                        // This should *never* happen; it should have been caught in the check just above this loop.
                        // This would indicate that either the matching archetypes or the chunk cache are being modified
                        // concurrently with this consistency check.
                        throw new ArgumentException($"query chunk cache inconsistency: matching archetype chunk {chunkCounter} exceeds chunk cache length {cache.Length}");
                    }

                    var chunk = cache.MatchingChunks->Ptr[chunkCounter];
                    if (chunk != archetype->Chunks[chunkIndex])
                    {
                        // This means the chunk may actually be in the cache, but not at the index we expect it to be.
                        // This may indicate that the chunk cache update logic is iterating over matching archetypes/chunks in a different
                        // order than this consistency check.
                        throw new ArgumentException(@$"query chunk cache inconsistency: cached chunk at index {chunkCounter} should be at chunkIndex={chunkIndex} of archetype {archetypeIndex}.
Archetype has {archetypeChunkCount} chunks and {archetype->EntityCount} entities, with types {archetype->ToString()}.
Query matches {archetypeCount} archetypes.");
                    }
                    if (chunk.SequenceNumber != archetype->Chunks[chunkIndex].SequenceNumber)
                    {
                        // Chunk* comparisons are not sufficient, because chunk allocations are pooled. The chunk may have
                        // been freed and reallocated into a new archetype.
                        // This would indicate that some operation is not invalidating the appropriate query caches.
                        throw new ArgumentException(@$"query chunk cache inconsistency: cached chunk at index {chunkCounter} has a different sequence number {chunk.SequenceNumber} than chunk in matching archetype with sequence number {archetype->Chunks[chunkIndex].SequenceNumber}.
Archetype has {archetypeChunkCount} chunks and {archetype->EntityCount} entities, with types {archetype->ToString()}.
Query matches {archetypeCount} archetypes.");
                    }
                    if (cache.ChunkIndexInArchetype->Ptr[chunkCounter] != chunk.ListIndex)
                    {
                        // This chunk in the cache corresponds to the correct chunk in the archetype, but
                        // its entry in ChunkIndexInArchetype is incorrect.
                        // This would indicate that the cache's ChunkIndexInArchetype list is not being updated correctly when the
                        // cache is updated.
                        throw new ArgumentException(@$"query chunk cache inconsistency: cached chunk at index {chunkCounter} thinks it should be at index {cache.ChunkIndexInArchetype->Ptr[chunkCounter]} in archetype {archetypeIndex}'s chunk list, but it's actually at index {chunk.ListIndex}.
Archetype has {archetypeChunkCount} chunks and {archetype->EntityCount} entities, with types {archetype->ToString()}.
Query matches {archetypeCount} archetypes.");
                    }
                    if (cache.PerChunkMatchingArchetypeIndex->Ptr[chunkCounter] != archetypeIndex)
                    {
                        // This chunk in the cache corresponds to the correct chunk in the archetype, but
                        // its entry in PerChunkMatchingArchetypeIndex is incorrect.
                        // This would indicate that the cache's PerChunkMatchingArchetypeIndex list is not being updated correctly when the
                        // cache is updated.
                        throw new ArgumentException(@$"query chunk cache inconsistency: cached chunk at index {chunkCounter} thinks it should be in matching archetype {cache.PerChunkMatchingArchetypeIndex->Ptr[chunkCounter]}, but it's actually in archetype {archetypeIndex}.
Archetype has {archetypeChunkCount} chunks and {archetype->EntityCount} entities, with types {archetype->ToString()}.
Query matches {archetypeCount} archetypes.");
                    }
                    chunkCounter += 1;
                }
            }
            // Iterate over chunks in the cache, and make sure they're in the correct location in the matching archetypes.
            for (int cacheChunkIndex = 0; cacheChunkIndex < cache.Length; ++cacheChunkIndex)
            {
                var chunk = cache.MatchingChunks->Ptr[cacheChunkIndex];
                int expectedArchetypeIndex = cache.PerChunkMatchingArchetypeIndex->Ptr[cacheChunkIndex];
                int expectedChunkIndexInArchetype = cache.ChunkIndexInArchetype->Ptr[cacheChunkIndex];
                if (expectedArchetypeIndex < 0 || expectedArchetypeIndex >= archetypeCount)
                {
                    // This indicates objectively invalid data in the cache's PerChunkMatchingArchetypeIndex array.
                    // Be especially concerned about negative numbers; that would be a pretty clear sign of a memory stomp.
                    throw new ArgumentException(@$"query chunk cache inconsistency: matching archetype index {expectedArchetypeIndex} for cached chunk {cacheChunkIndex} is out of range.
Query matches {archetypeCount} archetypes.");
                }
                var archetype = matchingArchetypes[expectedArchetypeIndex]->Archetype;
                if (expectedChunkIndexInArchetype < 0 || expectedChunkIndexInArchetype >= archetype->Chunks.Count)
                {
                    // This indicates objectively invalid data in the cache's ChunkIndexInArchetype array.
                    // Be especially concerned about negative numbers; that would be a pretty clear sign of a memory stomp.
                    throw new ArgumentException(@$"query chunk cache inconsistency: chunk index in archetype {expectedChunkIndexInArchetype} for cached chunk {cacheChunkIndex} is out of range. Archetype {expectedArchetypeIndex} contains {archetype->Chunks.Count} chunks.
Archetype has {archetype->Chunks.Count} chunks and {archetype->EntityCount} entities, with types {archetype->ToString()}.
Query matches {archetypeCount} archetypes.");
                }
                if (archetype->Chunks[expectedChunkIndexInArchetype] != chunk)
                {
                    // This means there's a chunk in the cache that didn't come from the query's matching archetypes.
                    // That would either be due to an error in the cache update logic, or the matching archetypes changing
                    // without invalidating the cache.
                    throw new ArgumentException(@$"query chunk cache inconsistency: cached chunk {cacheChunkIndex} not found at expected chunk index {expectedChunkIndexInArchetype} of expected matching archetype {expectedArchetypeIndex}.
Archetype has {archetype->Chunks.Count} chunks and {archetype->EntityCount} entities, with types {archetype->ToString()}.
Query matches {archetypeCount} archetypes.");
                }
                if (archetype->Chunks[expectedChunkIndexInArchetype].SequenceNumber != chunk.SequenceNumber)
                {
                    // Chunk* comparisons are not sufficient, because chunk allocations are pooled. The chunk may have
                    // been freed and reallocated into a new archetype.
                    // This would indicate that some operation is not invalidating the appropriate query caches.
                    throw new ArgumentException(@$"query chunk cache inconsistency: cached chunk {cacheChunkIndex} has a different sequence number {chunk.SequenceNumber} than chunk in matching archetype with sequence number {archetype->Chunks[expectedChunkIndexInArchetype].SequenceNumber}.
Archetype has {archetype->Chunks.Count} chunks and {archetype->EntityCount} entities, with types {archetype->ToString()}.
Query matches {archetypeCount} archetypes.");
                }
            }

            // All chunks in cache are accounted for.
            if (chunkCounter != cache.Length)
            {
                // This should *never* happen; we've already checked twice that these counts match.
                // This would indicate that the chunk cache is being modified concurrently with this consistency check.
                throw new ArgumentException($"query chunk cache inconsistency: we checked {chunkCounter} chunks from matching archetypes, but chunk cache length is {cache.Length}.");
            }
        }
    }

    unsafe struct EntityQueryData : IDisposable
    {
        //@TODO: better name or remove entirely...
        public ComponentType*       RequiredComponents;
        public int                  RequiredComponentsCount;

        public TypeIndex*           ReaderTypes;
        public int                  ReaderTypesCount;

        public TypeIndex*           WriterTypes;
        public int                  WriterTypesCount;

        public TypeIndex*           EnableableComponentTypeIndices;
        public int                  EnableableComponentTypeIndexCount; // number of elements in EnableableComponentTypeIndices

        public ArchetypeQuery*      ArchetypeQueries;
        public int                  ArchetypeQueryCount;

        public EntityQueryMask      EntityQueryMask;

        public UnsafeMatchingArchetypePtrList MatchingArchetypes;
        private UnsafeCachedChunkList MatchingChunkCache; // Direct access to this field is not thread-safe; use the helper methods on EntityQueryData instead.

        public byte HasEnableableComponents; // 0 = no, 1 = yes

        internal void CreateMatchingChunkCache(EntityComponentStore* ecs)
        {
            MatchingChunkCache = new UnsafeCachedChunkList(ecs);
        }

        internal void RebuildMatchingChunkCache()
        {
            UnsafeCachedChunkList.Rebuild(ref MatchingChunkCache, this);
        }

        // This method should *only* be called by EntityQueryImpl.GetMatchingChunkCache(), which includes some critical
        // thread-safety and cache validity checks. Using this return value directly in any other context is not guaranteed
        // to return a valid, up-to-date cache!
        internal UnsafeCachedChunkList UnsafeGetMatchingChunkCache()
        {
            return MatchingChunkCache;
        }

        internal void CheckChunkListCacheConsistency(bool forceCheckInvalidCache)
        {
            UnsafeCachedChunkList.AssertIsConsistent(MatchingChunkCache, this, forceCheckInvalidCache);
        }

        internal void InvalidateChunkCache()
        {
            MatchingChunkCache.Invalidate();
        }

        internal bool IsChunkCacheValid()
        {
            return MatchingChunkCache.IsCacheValid;
        }

        public void Dispose()
        {
            MatchingArchetypes.Dispose();
            MatchingChunkCache.Dispose();
        }
    }
}
