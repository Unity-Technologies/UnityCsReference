// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

using UnityEditor;

namespace UnityEngine.Tilemaps
{
    public partial class Tilemap
    {
        public void GetTileEntityIdsFromOffsets(Vector3Int position, NativeArray<Vector3Int> offsets, NativeArray<EntityId> tileEntityIds)
        {
            if (!offsets.IsCreated || !tileEntityIds.IsCreated)
                return;

            if (offsets.Length != tileEntityIds.Length)
                return;

            unsafe
            {
                GetTileEntityIdsFromOffsets(position, (IntPtr) offsets.m_Buffer, (IntPtr)tileEntityIds.m_Buffer, tileEntityIds.Length);
            }
        }

        internal static void GetTileEntityIdsFromOffsetsAndHandle(IntPtr tilemapHandle, Vector3Int position, NativeArray<Vector3Int> offsets, NativeArray<EntityId> tileEntityIds)
        {
            if (!offsets.IsCreated || !tileEntityIds.IsCreated)
                return;

            if (offsets.Length != tileEntityIds.Length)
                return;

            unsafe
            {
                GetTileEntityIdsFromOffsetsAndHandle(tilemapHandle, position, (IntPtr)offsets.m_Buffer, (IntPtr)tileEntityIds.m_Buffer, tileEntityIds.Length);
            }
        }

        public void GetTileEntityIdsFromBlockOffset(Vector3Int position, BoundsInt blockOffset, NativeArray<EntityId> tileEntityIds)
        {
            if (!tileEntityIds.IsCreated)
                return;

            unsafe
            {
                GetTileEntityIdsFromBlockOffset(position, blockOffset, (IntPtr)tileEntityIds.m_Buffer, tileEntityIds.Length);
            }
        }

        internal static void GetTileEntityIdsFromBlockOffsetAndHandle(IntPtr tilemapHandle, Vector3Int position, BoundsInt blockOffset, NativeArray<EntityId> tileEntityIds)
        {
            if (!tileEntityIds.IsCreated)
                return;

            unsafe
            {
                GetTileEntityIdsFromBlockOffsetAndHandle(tilemapHandle, position, blockOffset, (IntPtr) tileEntityIds.m_Buffer, tileEntityIds.Length);
            }
        }

        internal void GetAnyTileEntityIdsFromOffsets(Vector3Int position, NativeArray<Vector3Int> offsets, NativeArray<EntityId> tileEntityIds)
        {
            if (!offsets.IsCreated || !tileEntityIds.IsCreated)
                return;

            if (offsets.Length != tileEntityIds.Length)
                return;

            unsafe
            {
                GetAnyTileEntityIdsFromOffsets(position, (IntPtr)offsets.m_Buffer, (IntPtr)tileEntityIds.m_Buffer, tileEntityIds.Length);
            }
        }

        internal static void GetAnyTileEntityIdsFromOffsetsAndHandle(IntPtr tilemapHandle, Vector3Int position, NativeArray<Vector3Int> offsets, NativeArray<EntityId> tileEntityIds)
        {
            if (!offsets.IsCreated || !tileEntityIds.IsCreated)
                return;

            if (offsets.Length != tileEntityIds.Length)
                return;

            unsafe
            {
                GetAnyTileEntityIdsFromOffsetsAndHandle(tilemapHandle, position, (IntPtr)offsets.m_Buffer, (IntPtr)tileEntityIds.m_Buffer, tileEntityIds.Length);
            }
        }

        internal void GetAnyTileEntityIdsFromBlockOffset(Vector3Int position, BoundsInt blockOffset, NativeArray<EntityId> tileEntityIds)
        {
            if (!tileEntityIds.IsCreated)
                return;

            unsafe
            {
                GetAnyTileEntityIdsFromBlockOffset(position, blockOffset, (IntPtr)tileEntityIds.m_Buffer, tileEntityIds.Length);
            }
        }

        internal static void GetAnyTileEntityIdsFromBlockOffsetAndHandle(IntPtr tilemapHandle, Vector3Int position, BoundsInt blockOffset, NativeArray<EntityId> tileEntityIds)
        {
            if (!tileEntityIds.IsCreated)
                return;

            unsafe
            {
                GetAnyTileEntityIdsFromBlockOffsetAndHandle(tilemapHandle, position, blockOffset, (IntPtr)tileEntityIds.m_Buffer, tileEntityIds.Length);
            }
        }
    }
}
