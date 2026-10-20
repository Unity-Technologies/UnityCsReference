// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Assertions;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Assert = UnityEngine.Assertions.AssertCore;

namespace Unity.Entities
{
    internal unsafe partial struct EntityComponentStore
    {
        // ----------------------------------------------------------------------------------------------------------
        // PUBLIC
        // ----------------------------------------------------------------------------------------------------------
        public Archetype* GetOrCreateArchetype(ComponentTypeInArchetype* inTypesSorted, int count)
        {
            var srcArchetype = GetExistingArchetype(inTypesSorted, count);
            if (srcArchetype != null)
                return srcArchetype;

            srcArchetype = CreateArchetype(inTypesSorted, count);

            var types = stackalloc ComponentTypeInArchetype[count + 1];

            srcArchetype->InstantiateArchetype = CreateInstanceArchetype(inTypesSorted, count, types, srcArchetype, true);
            srcArchetype->CopyArchetype = CreateInstanceArchetype(inTypesSorted, count, types, srcArchetype, false);

            if (srcArchetype->InstantiateArchetype != null)
            {
                Assert.IsTrue(srcArchetype->InstantiateArchetype->InstantiateArchetype == srcArchetype->InstantiateArchetype);
                Assert.IsTrue(srcArchetype->InstantiateArchetype->CleanupResidueArchetype == null);
            }

            if (srcArchetype->CopyArchetype != null)
            {
                Assert.IsTrue(srcArchetype->CopyArchetype->CopyArchetype == srcArchetype->CopyArchetype);
                Assert.IsTrue(srcArchetype->CopyArchetype->CleanupResidueArchetype == null);
            }


            // Setup cleanup archetype
            if (srcArchetype->CleanupNeeded)
            {
                var cleanupEntityType = new ComponentTypeInArchetype(ComponentType.FromTypeIndex(m_CleanupEntityType));
                bool cleanupAdded = false;

                types[0] = inTypesSorted[0];
                var newTypeCount = 1;

                for (var t = 1; t < srcArchetype->TypesCount; ++t)
                {
                    var type = srcArchetype->Types[t];

                    if (type.IsCleanupComponent)
                    {
                        if (!cleanupAdded && (cleanupEntityType < srcArchetype->Types[t]))
                        {
                            types[newTypeCount++] = cleanupEntityType;
                            cleanupAdded = true;
                        }

                        types[newTypeCount++] = srcArchetype->Types[t];
                    }
                }

                if (!cleanupAdded)
                {
                    types[newTypeCount++] = cleanupEntityType;
                }

                var cleanupResidueArchetype = GetOrCreateArchetype(types, newTypeCount);
                srcArchetype->CleanupResidueArchetype = cleanupResidueArchetype;

                Assert.IsTrue(cleanupResidueArchetype->CleanupResidueArchetype == cleanupResidueArchetype);
                Assert.IsTrue(cleanupResidueArchetype->InstantiateArchetype == null);
                Assert.IsTrue(cleanupResidueArchetype->CopyArchetype == null);
            }

            // Setup meta chunk archetype
            if (count > 1)
            {
                types[0] = new ComponentTypeInArchetype(m_EntityComponentType);
                int metaArchetypeTypeCount = 1;
                for (int i = 1; i < count; ++i)
                {
                    var t = inTypesSorted[i];
                    ComponentType typeToInsert;
                    if (inTypesSorted[i].IsChunkComponent)
                    {
                        typeToInsert = new ComponentType
                        {
                            TypeIndex = ChunkComponentToNormalTypeIndex(t.TypeIndex)
                        };
                        SortingUtilities.InsertSorted(types, metaArchetypeTypeCount++, typeToInsert);
                    }
                }

                if (metaArchetypeTypeCount > 1)
                {
                    SortingUtilities.InsertSorted(types, metaArchetypeTypeCount++, m_ChunkHeaderComponentType);
                    srcArchetype->MetaChunkArchetype = GetOrCreateArchetype(types, metaArchetypeTypeCount);
                }
            }

            return srcArchetype;
        }

        Archetype* CreateInstanceArchetype(ComponentTypeInArchetype* inTypesSorted, int count, ComponentTypeInArchetype* types, Archetype* srcArchetype, bool removePrefab)
        {
            UnsafeUtility.MemCpy(types, inTypesSorted, sizeof(ComponentTypeInArchetype) * count);

            var hasCleanup = false;
            var removedTypes = 0;
            var legIndex = -1;
            var omitLegFromPrefabIndex = -1;
            for (var t = 0; t < srcArchetype->TypesCount; ++t)
            {
                var type = srcArchetype->Types[t];

                hasCleanup |= type.TypeIndex == m_CleanupEntityType;

                var skip = type.IsCleanupComponent || (removePrefab && type.TypeIndex == m_PrefabType);
                if (skip)
                    ++removedTypes;
                else
                    types[t - removedTypes] = srcArchetype->Types[t];

                if (removePrefab)
                {
                    if (type.TypeIndex == m_LinkedGroupType)
                        legIndex = t - removedTypes;
                    else if (type.TypeIndex == m_OmitLinkedEntityGroupFromPrefabInstanceType)
                        omitLegFromPrefabIndex = t - removedTypes;
                }
            }

            // Entity has already been destroyed, so it shouldn't be instantiated anymore
            if (hasCleanup)
            {
                return null;
            }

            if (legIndex >= 0 && omitLegFromPrefabIndex >= 0)
            {
                for (int i = math.min(legIndex, omitLegFromPrefabIndex), end = math.max(legIndex, omitLegFromPrefabIndex) - 1; i < end; ++i)
                    types[i] = types[i + 1];
                for (int i = math.max(legIndex, omitLegFromPrefabIndex) - 1, end = count - removedTypes - 2; i < end; ++i)
                    types[i] = types[i + 2];
                removedTypes += 2;
            }

            return removedTypes > 0 ? GetOrCreateArchetype(types, count - removedTypes) : srcArchetype;
        }

        // ----------------------------------------------------------------------------------------------------------
        // INTERNAL
        // ----------------------------------------------------------------------------------------------------------

        internal struct ArchetypeChunkFilter
        {
            public Archetype* Archetype;
#pragma warning disable 649
            public fixed int SharedComponentValues[kMaxSharedComponentCount];
#pragma warning restore 649

            public ArchetypeChunkFilter(Archetype* archetype, int* sharedComponentValues)
            {
                Archetype = archetype;
                for (int i = 0; i < archetype->NumSharedComponents; i++)
                    SharedComponentValues[i] = sharedComponentValues[i];
            }

            public ArchetypeChunkFilter(Archetype* archetype, SharedComponentValues sharedComponentValues)
            {
                Archetype = archetype;
                for (int i = 0; i < archetype->NumSharedComponents; i++)
                    SharedComponentValues[i] = sharedComponentValues[i];
            }
        }

        ChunkIndex GetChunkWithEmptySlotsWithAddedComponent(Entity entity, ComponentType componentType)
        {
            if (!Exists(entity))
                return ChunkIndex.Null;

            return GetChunkWithEmptySlotsWithAddedComponent(GetChunk(entity), componentType);
        }

        ChunkIndex GetChunkWithEmptySlotsWithAddedComponent(ChunkIndex srcChunk, ComponentType componentType, int sharedComponentIndex = 0)
        {
            if (!GetArchetypeChunkFilterWithAddedComponent(srcChunk, componentType, sharedComponentIndex, out var archetypeChunkFilter))
                return ChunkIndex.Null; // chunk already has the requested component

            return GetChunkWithEmptySlots(ref archetypeChunkFilter);
        }

        ChunkIndex GetChunkWithEmptySlotsWithRemovedComponent(Entity entity, ComponentType componentType)
        {
            if (!Exists(entity))
                return ChunkIndex.Null;

            return GetChunkWithEmptySlotsWithRemovedComponent(GetChunk(entity), componentType);
        }

        ChunkIndex GetChunkWithEmptySlotsWithRemovedComponent(ChunkIndex srcChunk, ComponentType componentType)
        {
            if (!GetArchetypeChunkFilterWithRemovedComponent(srcChunk, componentType, out var archetypeChunkFilter))
                return ChunkIndex.Null;

            return GetChunkWithEmptySlots(ref archetypeChunkFilter);
        }

        internal ChunkIndex GetChunkWithEmptySlots(ref ArchetypeChunkFilter archetypeChunkFilter)
        {
            var archetype = archetypeChunkFilter.Archetype;
            fixed(int* sharedComponentValues = archetypeChunkFilter.SharedComponentValues)
            {
                var chunk = archetype->GetExistingChunkWithEmptySlots(sharedComponentValues);
                if (chunk == ChunkIndex.Null)
                    chunk = GetCleanChunk(archetype, sharedComponentValues);
                return chunk;
            }
        }

        ArchetypeChunkFilter GetArchetypeChunkFilterWithChangedArchetype(ChunkIndex srcChunk, Archetype* dstArchetype)
        {
            var srcArchetype = GetArchetype(srcChunk);

            var archetypeChunkFilter = new ArchetypeChunkFilter();
            archetypeChunkFilter.Archetype = dstArchetype;
            var srcSharedComponentValues = srcArchetype->Chunks.GetSharedComponentValues(srcChunk.ListIndex);
            BuildSharedComponentIndicesWithChangedArchetype(srcArchetype, dstArchetype, srcSharedComponentValues, archetypeChunkFilter.SharedComponentValues);
            return archetypeChunkFilter;
        }

        /// <summary>
        /// Construct an ArchetypeChunkFilter based on the provided chunk, after changing the value of a shared component.
        /// </summary>
        /// <param name="srcChunk">The source chunk</param>
        /// <param name="componentType">The shared component type to modify</param>
        /// <param name="dstSharedComponentIndex">The desired new value index for <paramref name="componentType"/>.</param>
        /// <param name="outFilter">If successful, the filter will be stored here.</param>
        /// <returns></returns>
        /// <returns>False if srcChunk already has the provided shared component value, in which case no change is necessary and the output filter will be default-initialized.</returns>
        bool GetArchetypeChunkFilterWithChangedSharedComponent(ChunkIndex srcChunk, ComponentType componentType, int dstSharedComponentIndex,
            out ArchetypeChunkFilter outFilter)
        {
            var typeIndex = componentType.TypeIndex;
            var srcArchetype = GetArchetype(srcChunk);
            var indexInTypeArray = ChunkDataUtility.GetIndexInTypeArray(srcArchetype, typeIndex);

            var srcSharedComponentValueArray = srcArchetype->Chunks.GetSharedComponentValues(srcChunk.ListIndex);
            var sharedComponentOffset = indexInTypeArray - srcArchetype->FirstSharedComponent;
            var srcSharedComponentIndex = srcSharedComponentValueArray[sharedComponentOffset];

            if (dstSharedComponentIndex == srcSharedComponentIndex)
            {
                outFilter = default;
                return false;
            }

            outFilter = new ArchetypeChunkFilter();
            outFilter.Archetype = srcArchetype;
            for (int i = 0; i < srcArchetype->NumSharedComponents; ++i)
                outFilter.SharedComponentValues[i] = srcSharedComponentValueArray[i];
            outFilter.SharedComponentValues[sharedComponentOffset] = dstSharedComponentIndex;
            return true;
        }

        ArchetypeChunkFilter GetArchetypeChunkFilterWithAddedComponent(Archetype* srcArchetype, int srcChunkListIndex, Archetype* dstArchetype, int indexInTypeArray, ComponentType componentType, int sharedComponentIndex)
        {
            var archetypeChunkFilter = new ArchetypeChunkFilter();
            archetypeChunkFilter.Archetype = dstArchetype;
            var srcSharedComponentValues = srcArchetype->Chunks.GetSharedComponentValues(srcChunkListIndex);
            if (componentType.IsSharedComponent)
            {
                int indexOfNewSharedComponent = indexInTypeArray - dstArchetype->FirstSharedComponent;
                BuildSharedComponentIndicesWithAddedComponent(indexOfNewSharedComponent, sharedComponentIndex, dstArchetype->NumSharedComponents, srcSharedComponentValues, archetypeChunkFilter.SharedComponentValues);
            }
            else
            {
                for (int i = 0; i < srcArchetype->NumSharedComponents; i++)
                    archetypeChunkFilter.SharedComponentValues[i] = srcSharedComponentValues[i];
            }

            return archetypeChunkFilter;
        }

        /// <summary>
        /// Construct an ArchetypeChunkFilter based on the provided chunk, after adding a new component.
        /// </summary>
        /// <param name="srcChunk">The source chunk</param>
        /// <param name="componentType">The component type to add</param>
        /// <param name="sharedComponentIndex">Value index for <paramref name="componentType"/>, if it's a shared component. Otherwise, this parameter will be ignored.</param>
        /// <param name="outFilter"></param>
        /// <returns>False if srcChunk already has the specified component/value, in which case no change is necessary and the output filter will be default-initialized.</returns>
        bool GetArchetypeChunkFilterWithAddedComponent(ChunkIndex srcChunk, ComponentType componentType, int sharedComponentIndex, out ArchetypeChunkFilter outFilter)
        {
            var srcArchetype = GetArchetype(srcChunk);
            int indexInTypeArray = 0;
            var dstArchetype = GetArchetypeWithAddedComponent(srcArchetype, componentType, &indexInTypeArray);
            if (dstArchetype == null)
            {
                Assert.IsTrue(sharedComponentIndex == 0);
                outFilter = default;
                return false;
            }

            Assert.IsTrue(dstArchetype->NumSharedComponents <= kMaxSharedComponentCount);

            outFilter = GetArchetypeChunkFilterWithAddedComponent(srcArchetype, srcChunk.ListIndex, dstArchetype, indexInTypeArray, componentType, sharedComponentIndex);
            return true;
        }

        ArchetypeChunkFilter GetArchetypeChunkFilterWithAddedComponents(ChunkIndex srcChunk, Archetype* dstArchetype)
        {
            var srcArchetype = GetArchetype(srcChunk);
            var archetypeChunkFilter = new ArchetypeChunkFilter();
            archetypeChunkFilter.Archetype = dstArchetype;
            var numSrcSharedComponents = srcArchetype->NumSharedComponents;
            var srcSharedComponentValues = srcArchetype->Chunks.GetSharedComponentValues(srcChunk.ListIndex);
            if (dstArchetype->NumSharedComponents > numSrcSharedComponents)
            {
                BuildSharedComponentIndicesWithAddedComponents(srcArchetype, dstArchetype, srcSharedComponentValues, archetypeChunkFilter.SharedComponentValues);
            }
            else
            {
                for (int i = 0; i < numSrcSharedComponents; i++)
                    archetypeChunkFilter.SharedComponentValues[i] = srcSharedComponentValues[i];
            }
            return archetypeChunkFilter;
        }

        Archetype* GetArchetypeWithAddedComponents(Archetype* srcArchetype, in ComponentTypeSet componentTypeSet)
        {
            var srcTypes = srcArchetype->Types;
            var dstTypesCount = srcArchetype->TypesCount + componentTypeSet.Length;

            ComponentTypeInArchetype* dstTypes = stackalloc ComponentTypeInArchetype[dstTypesCount];

            // zipper the two sorted arrays "type" and "componentTypeInArchetype" into "componentTypeInArchetype"
            // because this is done in-place, it must be done backwards so as not to disturb the existing contents.

            var unusedIndices = 0;
            {
                var oldThings = srcArchetype->TypesCount - 1;
                var newThings = componentTypeSet.Length - 1;
                var mixedThings = dstTypesCount;
                while (newThings >= 0) // oldThings[0] has typeIndex 0, newThings can't have anything lower than that
                {
                    var oldThing = srcTypes[oldThings];
                    var newThing = new ComponentTypeInArchetype(componentTypeSet.GetTypeIndex(newThings));
                    if (oldThing.TypeIndex > newThing.TypeIndex) // put whichever is bigger at the end of the array
                    {
                        dstTypes[--mixedThings] = oldThing;
                        --oldThings;
                    }
                    else
                    {
                        if (oldThing.TypeIndex == newThing.TypeIndex)
                            --oldThings;

                        dstTypes[--mixedThings] = newThing;
                        --newThings;
                    }
                }

                while (oldThings >= 0) // if there are remaining old things, copy them here
                {
                    dstTypes[--mixedThings] = srcTypes[oldThings--];
                }

                unusedIndices = mixedThings; // In case we ignored duplicated types, this will be > 0
            }

            if (unusedIndices == componentTypeSet.Length)
                return srcArchetype;

            return GetOrCreateArchetype(dstTypes + unusedIndices, dstTypesCount - unusedIndices);
        }

        ArchetypeChunkFilter GetArchetypeChunkFilterWithRemovedComponent(ChunkIndex srcChunk, Archetype* dstArchetype, int indexInTypeArray, ComponentType componentType)
        {
            var srcArchetype = GetArchetype(srcChunk);
            var archetypeChunkFilter = new ArchetypeChunkFilter();
            archetypeChunkFilter.Archetype = dstArchetype;
            var srcSharedComponentValues = srcArchetype->Chunks.GetSharedComponentValues(srcChunk.ListIndex);
            if (componentType.IsSharedComponent)
            {
                int indexOfRemovedSharedComponent = indexInTypeArray - srcArchetype->FirstSharedComponent;
                BuildSharedComponentIndicesWithRemovedComponent(indexOfRemovedSharedComponent, dstArchetype->NumSharedComponents, srcSharedComponentValues, archetypeChunkFilter.SharedComponentValues);
            }
            else
            {
                for (int i = 0; i < srcArchetype->NumSharedComponents; i++)
                    archetypeChunkFilter.SharedComponentValues[i] = srcSharedComponentValues[i];
            }

            return archetypeChunkFilter;
        }

        /// <summary>
        /// Construct an ArchetypeChunkFilter based on the provided chunk, after removing a component.
        /// </summary>
        /// <param name="srcChunk">The source chunk</param>
        /// <param name="componentType">The component type to remove</param>
        /// <param name="outFilter">If successful, the filter will be stored here.</param>
        /// <returns>False if removing the component does not require an archetype change. In this case, the output filter will be default-initialized.</returns>
        bool GetArchetypeChunkFilterWithRemovedComponent(ChunkIndex srcChunk, ComponentType componentType, out ArchetypeChunkFilter outFilter)
        {
            var srcArchetype = GetArchetype(srcChunk);
            int indexInTypeArray = 0;
            var dstArchetype = GetArchetypeWithRemovedComponent(srcArchetype, componentType, &indexInTypeArray);
            if (dstArchetype == srcArchetype)
            {
                outFilter = default;
                return false;
            }

            outFilter = GetArchetypeChunkFilterWithRemovedComponent(srcChunk, dstArchetype, indexInTypeArray, componentType);
            return true;
        }

        ArchetypeChunkFilter GetArchetypeChunkFilterWithRemovedComponents(ChunkIndex srcChunk, Archetype* dstArchetype)
        {
            var srcArchetype = GetArchetype(srcChunk);
            var archetypeChunkFilter = new ArchetypeChunkFilter();
            archetypeChunkFilter.Archetype = dstArchetype;
            var numSrcSharedComponents = srcArchetype->NumSharedComponents;
            var srcSharedComponentValues = srcArchetype->Chunks.GetSharedComponentValues(srcChunk.ListIndex);
            if (dstArchetype->NumSharedComponents < numSrcSharedComponents)
            {
                BuildSharedComponentIndicesWithRemovedComponents(srcArchetype, dstArchetype, srcSharedComponentValues, archetypeChunkFilter.SharedComponentValues);
            }
            else
            {
                for (int i = 0; i < numSrcSharedComponents; i++)
                    archetypeChunkFilter.SharedComponentValues[i] = srcSharedComponentValues[i];
            }
            return archetypeChunkFilter;
        }

        static void BuildSharedComponentIndicesWithAddedComponent(int indexOfNewSharedComponent, int value,
            int newCount, SharedComponentValues srcSharedComponentValues, int* dstSharedComponentValues)
        {
            Assert.IsTrue(newCount <= kMaxSharedComponentCount);

            srcSharedComponentValues.CopyTo(dstSharedComponentValues, 0, indexOfNewSharedComponent);
            dstSharedComponentValues[indexOfNewSharedComponent] = value;
            srcSharedComponentValues.CopyTo(dstSharedComponentValues + indexOfNewSharedComponent + 1,
                indexOfNewSharedComponent, newCount - indexOfNewSharedComponent - 1);
        }

        static void BuildSharedComponentIndicesWithRemovedComponent(int indexOfRemovedSharedComponent,
            int newCount, SharedComponentValues srcSharedComponentValues, int* dstSharedComponentValues)
        {
            srcSharedComponentValues.CopyTo(dstSharedComponentValues, 0, indexOfRemovedSharedComponent);
            srcSharedComponentValues.CopyTo(dstSharedComponentValues + indexOfRemovedSharedComponent,
                indexOfRemovedSharedComponent + 1, newCount - indexOfRemovedSharedComponent);
        }

        static void BuildSharedComponentIndicesWithAddedComponents(Archetype* srcArchetype,
            Archetype* dstArchetype, SharedComponentValues srcSharedComponentValues, int* dstSharedComponentValues)
        {
            int oldFirstShared = srcArchetype->FirstSharedComponent;
            int newFirstShared = dstArchetype->FirstSharedComponent;
            int oldCount = srcArchetype->NumSharedComponents;
            int newCount = dstArchetype->NumSharedComponents;

            for (int oldIndex = oldCount - 1, newIndex = newCount - 1; newIndex >= 0; --newIndex)
            {
                // oldIndex might become -1 which is ok since oldFirstShared is always at least 1. The comparison will then always be false
                if (dstArchetype->Types[newIndex + newFirstShared] == srcArchetype->Types[oldIndex + oldFirstShared])
                    dstSharedComponentValues[newIndex] = srcSharedComponentValues[oldIndex--];
                else
                    dstSharedComponentValues[newIndex] = 0;
            }
        }

        static void BuildSharedComponentIndicesWithChangedArchetype(Archetype* srcArchetype,
            Archetype* dstArchetype, SharedComponentValues srcSharedComponentValues, int* dstSharedComponentValues)
        {
            Assert.IsTrue(dstArchetype->NumSharedComponents <= kMaxSharedComponentCount);

            int oldFirstShared = srcArchetype->FirstSharedComponent;
            int newFirstShared = dstArchetype->FirstSharedComponent;
            int oldCount = srcArchetype->NumSharedComponents;
            int newCount = dstArchetype->NumSharedComponents;

            int o = 0;
            int n = 0;

            for (; n < newCount && o < oldCount;)
            {
                var srcType = srcArchetype->Types[o + oldFirstShared].TypeIndex;
                var dstType = dstArchetype->Types[n + newFirstShared].TypeIndex;
                if (srcType == dstType)
                    dstSharedComponentValues[n++] = srcSharedComponentValues[o++];
                else if (dstType > srcType)
                    o++;
                else
                    dstSharedComponentValues[n++] = 0;
            }

            for (; n < newCount; n++)
                dstSharedComponentValues[n] = 0;
        }

        Archetype* GetArchetypeWithAddedComponent(Archetype* archetype, ComponentType addedComponentType, int* indexInTypeArray = null)
        {
            var componentType = new ComponentTypeInArchetype(addedComponentType);
            ComponentTypeInArchetype* newTypes = stackalloc ComponentTypeInArchetype[archetype->TypesCount + 1];

            var t = 0;
            while (t < archetype->TypesCount && archetype->Types[t] < componentType)
            {
                newTypes[t] = archetype->Types[t];
                ++t;
            }

            if (indexInTypeArray != null)
                *indexInTypeArray = t;

            if (t != archetype->TypesCount && archetype->Types[t] == componentType)
            {
                // Tag component type is already there, no new archetype required.
                return null;
            }

            newTypes[t] = componentType;
            while (t < archetype->TypesCount)
            {
                newTypes[t + 1] = archetype->Types[t];
                ++t;
            }

            return GetOrCreateArchetype(newTypes, archetype->TypesCount + 1);
        }

        Archetype* GetArchetypeWithRemovedComponent(Archetype* archetype, ComponentType addedComponentType, int* indexInOldTypeArray = null)
        {
            var componentType = new ComponentTypeInArchetype(addedComponentType);
            ComponentTypeInArchetype* newTypes = stackalloc ComponentTypeInArchetype[archetype->TypesCount];

            var removedTypes = 0;
            for (var t = 0; t < archetype->TypesCount; ++t)
                if (archetype->Types[t].TypeIndex == componentType.TypeIndex)
                {
                    if (indexInOldTypeArray != null)
                        *indexInOldTypeArray = t;
                    ++removedTypes;
                }
                else
                    newTypes[t - removedTypes] = archetype->Types[t];

            return GetOrCreateArchetype(newTypes, archetype->TypesCount - removedTypes);
        }

        Archetype* GetArchetypeWithRemovedComponents(Archetype* archetype, in ComponentTypeSet typeSetToRemove)
        {
            ComponentTypeInArchetype* newTypes = stackalloc ComponentTypeInArchetype[archetype->TypesCount];

            var numRemovedTypes = 0;
            for (var t = 0; t < archetype->TypesCount; ++t)
            {
                var existingTypeIndex = archetype->Types[t].TypeIndex;

                var removed = false;
                for (int i = 0; i < typeSetToRemove.Length; i++)
                {
                    if (existingTypeIndex == typeSetToRemove.GetTypeIndex(i))
                    {
                        numRemovedTypes++;
                        removed = true;
                        break;
                    }
                }

                if (!removed)
                {
                    newTypes[t - numRemovedTypes] = archetype->Types[t];
                }
            }

            if (numRemovedTypes == 0)
            {
                return archetype;
            }
            return GetOrCreateArchetype(newTypes, archetype->TypesCount - numRemovedTypes);
        }

        static void BuildSharedComponentIndicesWithRemovedComponents(Archetype* srcArchetype,
            Archetype* dstArchetype, SharedComponentValues srcSharedComponentValues, int* dstSharedComponentValues)
        {
            int srcFirstShared = srcArchetype->FirstSharedComponent;
            int dstFirstShared = dstArchetype->FirstSharedComponent;
            int srcCount = srcArchetype->NumSharedComponents;
            int dstCount = dstArchetype->NumSharedComponents;

            for (int i = 0; i < dstCount; i++)
            {
                // find index of srcType that matches dstType
                var dstType = dstArchetype->Types[dstFirstShared + i];
                int matchingSrcIdx = 0;
                for (int j = 0; j < srcCount; j++)
                {
                    var srcType = srcArchetype->Types[srcFirstShared + j];
                    if (srcType == dstType)
                    {
                        matchingSrcIdx = j;
                        break;
                    }
                }

                dstSharedComponentValues[i] = srcSharedComponentValues[matchingSrcIdx];
            }
        }
    }
}
