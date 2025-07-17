// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace UnityEditor
{
    [Obsolete("PickingIncludeExcludeList is deprecated. Use PickingIncludeExcludeEntityIdList instead.")]
    public struct PickingIncludeExcludeList : IDisposable
    {
        NativeArray<int> m_IncludeRenderers, m_ExcludeRenderers, m_IncludeEntities, m_ExcludeEntities;
        public NativeArray<int> IncludeRenderers => m_IncludeRenderers;
        public NativeArray<int> ExcludeRenderers => m_ExcludeRenderers;
        public NativeArray<int> IncludeEntities => m_IncludeEntities;
        public NativeArray<int> ExcludeEntities => m_ExcludeEntities;

        public PickingIncludeExcludeList(List<int> includeRendererInstanceIDs, List<int> excludeRendererInstanceIDs, List<int> includeEntityIndices, List<int> excludeEntityIndices, Allocator allocator = Allocator.Persistent)
        {
            m_IncludeRenderers = ArrayFromList(includeRendererInstanceIDs, allocator);
            m_ExcludeRenderers = ArrayFromList(excludeRendererInstanceIDs, allocator);
            m_IncludeEntities = ArrayFromList(includeEntityIndices, allocator);
            m_ExcludeEntities = ArrayFromList(excludeEntityIndices, allocator);
        }

        public void Dispose()
        {
            if (IncludeRenderers.IsCreated) IncludeRenderers.Dispose();
            if (ExcludeRenderers.IsCreated) ExcludeRenderers.Dispose();
            if (IncludeEntities.IsCreated) IncludeEntities.Dispose();
            if (ExcludeEntities.IsCreated) ExcludeEntities.Dispose();
        }

        private static NativeArray<int> ArrayFromList(List<int> list, Allocator allocator)
        {
            if (list == null || list.Count == 0)
                return new NativeArray<int>();

            var array = new NativeArray<int>(list.Count, allocator, NativeArrayOptions.UninitializedMemory);
            // Use explicit copy length because the internal list might be longer than Count, which causes an error
            NativeArray<int>.Copy(NoAllocHelpers.ExtractArrayFromList(list), array, list.Count);
            return array;
        }
    }

    public struct PickingIncludeExcludeEntityIdList : IDisposable
    {
        NativeArray<EntityId> m_IncludeRenderers, m_ExcludeRenderers, m_IncludeEntities, m_ExcludeEntities;
        public NativeArray<EntityId> IncludeRenderers => m_IncludeRenderers;
        public NativeArray<EntityId> ExcludeRenderers => m_ExcludeRenderers;
        public NativeArray<EntityId> IncludeEntities => m_IncludeEntities;
        public NativeArray<EntityId> ExcludeEntities => m_ExcludeEntities;

        public PickingIncludeExcludeEntityIdList(List<EntityId> includeRendererEntityIds, List<EntityId> excludeRendererEntityIds, List<EntityId> includeEntityIndices, List<EntityId> excludeEntityIndices, Allocator allocator = Allocator.Persistent)
        {
            m_IncludeRenderers = ArrayFromList(includeRendererEntityIds, allocator);
            m_ExcludeRenderers = ArrayFromList(excludeRendererEntityIds, allocator);
            m_IncludeEntities = ArrayFromList(includeEntityIndices, allocator);
            m_ExcludeEntities = ArrayFromList(excludeEntityIndices, allocator);
        }

        public void Dispose()
        {
            if (IncludeRenderers.IsCreated) IncludeRenderers.Dispose();
            if (ExcludeRenderers.IsCreated) ExcludeRenderers.Dispose();
            if (IncludeEntities.IsCreated) IncludeEntities.Dispose();
            if (ExcludeEntities.IsCreated) ExcludeEntities.Dispose();
        }

        private static NativeArray<EntityId> ArrayFromList(List<EntityId> list, Allocator allocator)
        {
            if (list == null || list.Count == 0)
                return new NativeArray<EntityId>();

            var array = new NativeArray<EntityId>(list.Count, allocator, NativeArrayOptions.UninitializedMemory);
            // Use explicit copy length because the internal list might be longer than Count, which causes an error
            NativeArray<EntityId>.Copy(NoAllocHelpers.ExtractArrayFromList(list), array, list.Count);
            return array;
        }
    }
}
