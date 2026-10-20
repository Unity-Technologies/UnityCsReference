// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Profiling;
using Unity.Properties;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.Bindings;
using ProfilerMarker = Unity.Entities.EntitiesProfilerMarker;

namespace Unity.Entities
{
    [VisibleToOtherModules("UnityEngine.EntitiesPropertiesModule")]
    internal struct UnityObjectRefMap : IDisposable
    {
        /// <summary>
        /// An uncreated map that skips building the referenced objects list during serialization.
        /// Object references are still serialized (as -1), but no output list is produced.
        /// </summary>
        public static UnityObjectRefMap None => default;

        public NativeHashMap<EntityId, int> EntityIdMap;
        public NativeList<EntityId> EntityIds;

        public bool IsCreated => EntityIds.IsCreated && EntityIdMap.IsCreated;

        public UnityObjectRefMap(Allocator allocator)
        {
            EntityIdMap = new NativeHashMap<EntityId, int>(0, allocator);
            EntityIds = new NativeList<EntityId>(0, allocator);
        }

        public void Dispose()
        {
            EntityIdMap.Dispose();
            EntityIds.Dispose();
        }

        public object[] ToObjectArray()
        {
            return CoreModuleLightShim.Hooks.EntityIdsToObjects(IsCreated ? EntityIds.AsArray() : default);
        }

        public int Add(EntityId entityId)
        {
            var index = -1;
            if (entityId != EntityId.None && IsCreated)
            {
                if (!EntityIdMap.TryGetValue(entityId, out index))
                {
                    index = EntityIds.Length;
                    EntityIdMap.Add(entityId, index);
                    EntityIds.Add(entityId);
                }
            }

            return index;
        }
    }

    [VisibleToOtherModules("UnityEngine.CoreModule")]
    internal static partial class UnityObjectRefUtility
    {

        private static unsafe void AddInstanceIDRefsFromComponent(byte* componentData, TypeManager.EntityOffsetInfo* unityObjectRefOffsets, int unityObjectRefCount, UnsafeHashSet<EntityId>* entityIdRefs)
        {
            for (int i = 0; i < unityObjectRefCount; ++i)
            {
                var unityObjectRefOffset = unityObjectRefOffsets[i].Offset;
                var unityObjectRefPtr = (UntypedUnityObjectRef*)(componentData + unityObjectRefOffset);
                entityIdRefs->Add(unityObjectRefPtr->entityId);
            }
        }

        static unsafe void AddInstanceIDRefsFromChunk(Archetype* archetype, int entityCount, byte* chunkBuffer, UnsafeHashSet<EntityId>* entityIdRefs)
        {
            var typeCount = archetype->TypesCount;

            // This loop only cares about non-zero-sized unmanaged components and buffer components.
            for(int ti=1,tiEnd=archetype->BufferComponentsEnd; ti<tiEnd; ++ti)
            {
                var type = archetype->Types[ti];
                if (type.TypeIndex == CompanionComponentStore.CompanionLinkTypeIndex || type.TypeIndex == CompanionComponentStore.CompanionLinkTransformTypeIndex)
                    continue;

                ref readonly var ct = ref TypeManager.GetTypeInfo(type.TypeIndex);
                var unityObjectRefCount = ct.UnityObjectRefOffsetCount;

                if (unityObjectRefCount == 0)
                    continue;

                var unityObjectRefOffsets = TypeManager.GetUnityObjectRefOffsets(ct);
                int subArrayOffset = archetype->Offsets[ti];
                byte* componentArrayStart = chunkBuffer + subArrayOffset;

                if (type.IsBuffer)
                {
                    BufferHeader* header = (BufferHeader*)componentArrayStart;
                    int strideSize = archetype->SizeOfs[ti];
                    var elementSize = ct.ElementSize;

                    for (int bi = 0; bi < entityCount; ++bi)
                    {
                        var bufferStart = BufferHeader.GetElementPointer(header);
                        var bufferEnd = bufferStart + header->Length * elementSize;
                        for (var componentData = bufferStart; componentData < bufferEnd; componentData += elementSize)
                        {
                            AddInstanceIDRefsFromComponent(componentData, unityObjectRefOffsets,unityObjectRefCount, entityIdRefs);
                        }

                        header = (BufferHeader*)((byte*)header + strideSize);
                    }
                }
                else
                {
                    int size = archetype->SizeOfs[ti];
                    byte* end = componentArrayStart + size * entityCount;
                    for (var componentData = componentArrayStart; componentData < end; componentData += size)
                    {
                        AddInstanceIDRefsFromComponent(componentData, unityObjectRefOffsets, unityObjectRefCount, entityIdRefs);
                    }
                }
            }
        }

        static unsafe void AddInstanceIDRefsFromAllChunks(Archetype* archetype, UnsafeHashSet<EntityId>* entityIdRefs)
        {
            for (var chunkIndex = 0; chunkIndex < archetype->Chunks.Count; chunkIndex++)
            {
                var chunk = archetype->Chunks[chunkIndex];
                var chunkPtr = chunk.GetPtr();
                var chunkBuffer = chunkPtr->Buffer;

                AddInstanceIDRefsFromChunk(archetype, chunk.Count, chunkBuffer, entityIdRefs);
            }
        }

        static unsafe void AddInstanceIDRefsFromUnmanagedSharedComponents(EntityDataAccess* access, Archetype* archetype, UnsafeHashSet<EntityId>* entityIdRefs)
        {
            int numSharedComponents = archetype->NumSharedComponents;
            for (int iType = 0; iType < numSharedComponents; iType++)
            {
                var sharedComponents = archetype->Chunks.GetSharedComponentValueArrayForType(iType);

                for (int chunkIndex = 0; chunkIndex < archetype->Chunks.Count; chunkIndex++)
                {
                    var sharedComponentIndex = sharedComponents[chunkIndex];

                    var typeIndex = EntityComponentStore.GetComponentTypeFromSharedComponentIndex(sharedComponentIndex);
                    ref readonly var typeInfo = ref TypeManager.GetTypeInfo(typeIndex);

                    var unityObjectRefCount = typeInfo.UnityObjectRefOffsetCount;
                    if (unityObjectRefCount == 0)
                        continue;

                    var unityObjectRefOffsets = TypeManager.GetUnityObjectRefOffsets(typeInfo);
                    var dataPtr = (byte*)access->EntityComponentStore->GetSharedComponentDataAddr_Unmanaged(sharedComponentIndex, typeIndex);
                    AddInstanceIDRefsFromComponent(dataPtr, unityObjectRefOffsets, unityObjectRefCount, entityIdRefs);
                }
            }
        }


        private static readonly ProfilerMarker s_AddFromChunks = new ProfilerMarker("AddFromChunks");
        private static readonly ProfilerMarker s_AddFromUnmanagedSharedComponents = new ProfilerMarker("AddFromUnmanagaedSharedComponents");

        internal delegate void AdditionalRootsHandlerDelegate(IntPtr state);

        [AutoStaticsCleanupOnCodeReload]
        private static List<AdditionalRootsHandlerDelegate> s_AdditionalRootsHandlerDelegates = new List<AdditionalRootsHandlerDelegate>();

        // CoreModule registers this with the asset garbage collector when its code loads.
        [VisibleToOtherModules("UnityEngine.CoreModule")]
        internal static unsafe void RootsHandlerDelegate(IntPtr state)
        {
            foreach (var additionalRootsHandlerDelegate in s_AdditionalRootsHandlerDelegates)
            {
                additionalRootsHandlerDelegate(state);
            }

            using var entityIdRefs = new UnsafeHashSet<EntityId>(256, Allocator.Temp);
            foreach (var world in World.s_AllWorlds)
            {
                var access = world.EntityManager.GetCheckedEntityDataAccessExclusive();
                access->CompleteAllTrackedJobs();
                for (var i = 0; i < access->EntityComponentStore->m_Archetypes.Length; ++i)
                {
                    var archetype = access->EntityComponentStore->m_Archetypes.Ptr[i];

                    if (!archetype->HasUnityObjectRefs)
                        continue;

                    s_AddFromChunks.Begin();
                    AddInstanceIDRefsFromAllChunks(archetype, &entityIdRefs);
                    s_AddFromChunks.End();
                    s_AddFromUnmanagedSharedComponents.Begin();
                    AddInstanceIDRefsFromUnmanagedSharedComponents(access, archetype, &entityIdRefs);
                    s_AddFromUnmanagedSharedComponents.End();
                }
            }

            if (entityIdRefs.Count == 0)
                return;

            using var instanceIDs = entityIdRefs.ToNativeArray(Allocator.Temp);
            CoreModuleLightShim.Hooks.MarkInstanceIDsAsRoot((IntPtr)instanceIDs.GetUnsafePtr(), instanceIDs.Length, state);
        }

        public static void MarkInstanceIDsAsRootForEntitiesAssetGC(IntPtr instanceIDs, int count, IntPtr state)
        {
            CoreModuleLightShim.Hooks.MarkInstanceIDsAsRoot(instanceIDs, count, state);
        }

        public static void RegisterAdditionalRootsHandlerForEntitiesAssetGC(AdditionalRootsHandlerDelegate additionalRootsHandlerDelegate)
        {
            if (additionalRootsHandlerDelegate != null)
                s_AdditionalRootsHandlerDelegates.Add(additionalRootsHandlerDelegate);
        }

        internal static unsafe void MarkInstanceIDsAsRoot(NativeArray<EntityId> unityObjects, IntPtr state)
        {
            CoreModuleLightShim.Hooks.MarkInstanceIDsAsRoot((IntPtr)unityObjects.GetUnsafePtr(), unityObjects.Length, state);
        }
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    [VisibleToOtherModules("UnityEngine.CoreModule", "UnityEngine.EntitiesPropertiesModule")]
    internal struct UntypedUnityObjectRef : IEquatable<UntypedUnityObjectRef>
    {
        [SerializeField]
        [VisibleToOtherModules("UnityEngine.CoreModule", "UnityEngine.EntitiesPropertiesModule")]
        internal EntityId entityId;

        public bool Equals(UntypedUnityObjectRef other)
        {
            return entityId == other.entityId;
        }

        public override bool Equals(object obj)
        {
            return obj is UntypedUnityObjectRef other && Equals(other);
        }

        public override int GetHashCode()
        {
            return entityId.GetHashCode();
        }
    }
}
