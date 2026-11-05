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
        ///<summary>Gets an array of EntityIds at an offset from the <c>position</c> and stores them into the given array of EntityIds in the same order.</summary>
        ///<param name="position">The position on the <see cref="Tilemap" />.</param>
        ///<param name="offsets">Offsets from the <c>position</c> on the <see cref="Tilemap" />.</param>
        ///<param name="tileEntityIds">Array to hold the resulting EntityIds.</param>
        ///<example>
        ///  <code><![CDATA[using Unity.Collections;
        ///using UnityEngine;
        ///using UnityEngine.Tilemaps;
        ///
        ///public static class TilemapExample
        ///{
        ///    public static void GetTileEntityIdFromOffsetsExample(Tilemap tilemap, Vector3Int position, Tile tile)
        ///    {
        ///        int count = 5;
        ///        NativeArray<Vector3Int> offsets = new NativeArray<Vector3Int>(count, Allocator.Temp);
        ///        NativeArray<EntityId> entityIds = new NativeArray<EntityId>(count, Allocator.Temp);
        ///        for (var i = 0; i < count; i++)
        ///        {
        ///            offsets[i] = new Vector3Int(i, 0, 0);
        ///            tilemap.SetTile(position + offsets[i], tile);
        ///        }
        ///        
        ///        tilemap.GetTileEntityIdsFromOffsets(position, offsets, entityIds);
        ///
        ///        for (var i = 0; i < count; i++)
        ///        {
        ///            Debug.Log($"The ids for the Tile placed are equal ({(entityIds[i] == tile.GetEntityId()).ToString()})");    
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
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

        internal static void GetTileEntityIdsFromOffsetsAndHandle(EntityId tilemapEntityId, Vector3Int position, NativeArray<Vector3Int> offsets, NativeArray<EntityId> tileEntityIds)
        {
            if (!offsets.IsCreated || !tileEntityIds.IsCreated)
                return;

            if (offsets.Length != tileEntityIds.Length)
                return;

            unsafe
            {
                GetTileEntityIdsFromOffsetsAndHandle(tilemapEntityId, position, (IntPtr)offsets.m_Buffer, (IntPtr)tileEntityIds.m_Buffer, tileEntityIds.Length);
            }
        }

        ///<summary>Gets a block of EntityIds at an offset from the <c>position</c> and stores them into the given array of EntityIds in the same order.</summary>
        ///<param name="position">The position on the <see cref="Tilemap" />.</param>
        ///<param name="blockOffset">Bounds of the offsets from the <c>position</c> on the <see cref="Tilemap" />.</param>
        ///<param name="tileEntityIds">Array to hold the resulting EntityIds.</param>
        ///<example>
        ///  <code><![CDATA[using Unity.Collections;
        ///using UnityEngine;
        ///using UnityEngine.Tilemaps;
        ///
        ///public static class TilemapExample
        ///{
        ///    public static void GetTileEntityIdFromBlockOffsetsExample(Tilemap tilemap, Vector3Int position, Tile tile)
        ///    {
        ///        int count = 9;
        ///        NativeArray<EntityId> entityIds = new NativeArray<EntityId>(count, Allocator.Temp);
        ///        for (var y = -1; y < 1; y++)
        ///        {
        ///            for (var x = -1; x < 1; x++)
        ///            {
        ///                var offset = new Vector3Int(x, y, 0);
        ///                tilemap.SetTile(position + offset, tile);
        ///            }
        ///        }
        ///        var block = new BoundsInt(new Vector3Int(-1, -1, 0), new Vector3Int(3, 3, 1));
        ///        tilemap.GetTileEntityIdsFromBlockOffset(position, block, entityIds);
        ///
        ///        for (var i = 0; i < count; i++)
        ///        {
        ///            Debug.Log($"The ids for the Tile placed are equal ({(entityIds[i] == tile.GetEntityId()).ToString()})");
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public void GetTileEntityIdsFromBlockOffset(Vector3Int position, BoundsInt blockOffset, NativeArray<EntityId> tileEntityIds)
        {
            if (!tileEntityIds.IsCreated)
                return;

            unsafe
            {
                GetTileEntityIdsFromBlockOffset(position, blockOffset, (IntPtr)tileEntityIds.m_Buffer, tileEntityIds.Length);
            }
        }

        internal static void GetTileEntityIdsFromBlockOffsetAndHandle(EntityId tilemapEntityId, Vector3Int position, BoundsInt blockOffset, NativeArray<EntityId> tileEntityIds)
        {
            if (!tileEntityIds.IsCreated)
                return;

            unsafe
            {
                GetTileEntityIdsFromBlockOffsetAndHandle(tilemapEntityId, position, blockOffset, (IntPtr) tileEntityIds.m_Buffer, tileEntityIds.Length);
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

        internal static void GetAnyTileEntityIdsFromOffsetsAndHandle(EntityId tilemapEntityId, Vector3Int position, NativeArray<Vector3Int> offsets, NativeArray<EntityId> tileEntityIds)
        {
            if (!offsets.IsCreated || !tileEntityIds.IsCreated)
                return;

            if (offsets.Length != tileEntityIds.Length)
                return;

            unsafe
            {
                GetAnyTileEntityIdsFromOffsetsAndHandle(tilemapEntityId, position, (IntPtr)offsets.m_Buffer, (IntPtr)tileEntityIds.m_Buffer, tileEntityIds.Length);
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

        internal static void GetAnyTileEntityIdsFromBlockOffsetAndHandle(EntityId tilemapEntityId, Vector3Int position, BoundsInt blockOffset, NativeArray<EntityId> tileEntityIds)
        {
            if (!tileEntityIds.IsCreated)
                return;

            unsafe
            {
                GetAnyTileEntityIdsFromBlockOffsetAndHandle(tilemapEntityId, position, blockOffset, (IntPtr)tileEntityIds.m_Buffer, tileEntityIds.Length);
            }
        }
    }
}
