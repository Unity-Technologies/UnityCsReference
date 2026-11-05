// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Bindings;

namespace UnityEngine.Tilemaps
{
    // Blittable mirror of the native Tile (Modules/Tilemap/Public/TilemapTile.h). The
    // layout is asserted on the native side in TileGridData.h; change both together.
    //
    // Every member is field or bit math with no icall, so a Burst-compiled job can read
    // cells directly.
    [StructLayout(LayoutKind.Sequential)]
    [VisibleToOtherModules]
    internal struct TileCellData
    {
        // Bits 0-29 of the flags word are TileFlags; bits 30-31 are the collider type
        // (kTileColliderFlagsStart == 30 in TilemapTile.h).
        const uint k_FlagsMask = 0x3FFFFFFFu;
        const int k_ColliderShift = 30;
        const uint k_InvalidAssetIndex = 0xFFFFFFFFu;   // Tile::kInvalidTileAssetIndex
        const uint k_IsPositionAnimated = 1u << 29;     // kTileIsPositionAnimated

        public uint tileAssetIndex;
        public uint spriteIndex;
        public ushort matrixIndex;
        public ushort colorIndex;
        public ushort objectToInstantiateIndex;
        ushort m_Padding;                               // C++ pads here before the flags word
        public uint allFlags;
        public uint animatedSpriteIndex;

        public bool isOccupied => tileAssetIndex != k_InvalidAssetIndex;

        // When set, animatedSpriteIndex addresses TileGridDataArray's animated sprite table and replaces spriteIndex.
        public bool isPositionAnimated => (allFlags & k_IsPositionAnimated) != 0;
        public TileFlags flags => (TileFlags)(allFlags & k_FlagsMask);
        public Tile.ColliderType colliderType => (Tile.ColliderType)(allFlags >> k_ColliderShift);
    }

    // Blittable mirror of the native ScriptingTilePreviewCellData (Modules/Tilemap/Public/TileGridData.h): one
    // editor preview Tile and the cell it is in. Its indices address the preview side arrays of TileGridDataArray.
    [StructLayout(LayoutKind.Sequential)]
    [VisibleToOtherModules]
    internal struct TilePreviewCellData
    {
        public Vector3Int position;
        public TileCellData cell;
    }

    // A single 16x16 storage chunk of the tile grid, viewed as blittable data.
    //
    // This is NOT a mirror of the native TileChunk, which is not blittable (it inherits
    // ConcurrentNode and holds a spin lock and an atomic). It is a view: a pointer to
    // the chunk's live Tile storage plus its coordinate.
    [StructLayout(LayoutKind.Sequential)]
    [VisibleToOtherModules]
    internal unsafe struct TileChunkData
    {
        public const int kLogSide = 4;
        public const int kSide = 1 << kLogSide;
        public const int kCells = kSide * kSide;
        const int k_Mask = kSide - 1;

        [NativeDisableUnsafePtrRestriction] internal TileCellData* m_Tiles;
        internal Vector3Int m_ChunkCoord;

        public Vector3Int chunkCoord => m_ChunkCoord;
        public int cellCount => kCells;

        // Cells are visited in row-major (y-major, x-minor) order and include empty
        // slots; filter on TileCellData.isOccupied.
        public TileCellData this[int localIndex] => m_Tiles[localIndex];

        // Mirrors TileChunk::CellPositionAt. Pure math, no icall.
        public Vector3Int CellPositionAt(int localIndex)
        {
            return new Vector3Int(
                (m_ChunkCoord.x << kLogSide) | (localIndex & k_Mask),
                (m_ChunkCoord.y << kLogSide) | ((localIndex >> kLogSide) & k_Mask),
                m_ChunkCoord.z);
        }
    }

    // A read-only snapshot of a Tilemap's tile grid, safe to hand to jobs.
    //
    // The chunk entries point at live chunk storage rather than copies. Mutating the
    // tilemap while this is alive therefore yields undefined *contents*, but never a
    // crash: chunk allocations are pool-stable for the grid's lifetime (see
    // ConcurrentTileGrid.h), so a snapshot of a garbage-collected chunk reads stale
    // data rather than freed memory.
    //
    // The tile / sprite / matrix / colour arrays ARE copies, and are index-preserving: a
    // TileCellData's tileAssetIndex, spriteIndex, matrixIndex and colorIndex address them directly.
    // The animated sprite table and the editor preview cells and arrays are copies too.
    [NativeContainer]
    [NativeContainerSupportsMinMaxWriteRestriction]
    [VisibleToOtherModules]
    internal unsafe struct TileGridDataArray : IDisposable
    {
        // The job safety system walks nested fields and rejects the IntPtr inside
        // TilemapBuffer unless the restriction is lifted here. These buffers are
        // read-only for the job's lifetime, which is what makes that sound.
        [NativeDisableUnsafePtrRestriction] internal Tilemap.TilemapBuffer m_Chunks;
        [NativeDisableUnsafePtrRestriction] internal Tilemap.TilemapBuffer m_TileIds;
        [NativeDisableUnsafePtrRestriction] internal Tilemap.TilemapBuffer m_SpriteIds;
        [NativeDisableUnsafePtrRestriction] internal Tilemap.TilemapBuffer m_Matrices;
        [NativeDisableUnsafePtrRestriction] internal Tilemap.TilemapBuffer m_Colors;
        [NativeDisableUnsafePtrRestriction] internal Tilemap.TilemapBuffer m_AnimatedSpriteIds;
        [NativeDisableUnsafePtrRestriction] internal Tilemap.TilemapBuffer m_PreviewCells;
        [NativeDisableUnsafePtrRestriction] internal Tilemap.TilemapBuffer m_PreviewTileIds;
        [NativeDisableUnsafePtrRestriction] internal Tilemap.TilemapBuffer m_PreviewSpriteIds;
        [NativeDisableUnsafePtrRestriction] internal Tilemap.TilemapBuffer m_PreviewMatrices;
        [NativeDisableUnsafePtrRestriction] internal Tilemap.TilemapBuffer m_PreviewColors;

        // Makes Dispose() idempotent, as the IDisposable contract requires. An empty
        // tilemap legitimately yields null buffers, so buffer nullness cannot stand in
        // for "already disposed".
        internal int m_IsCreated;

        internal int m_MinIndex;
        internal int m_MaxIndex;
        internal AtomicSafetyHandle m_Safety;

        // Number of live chunks in the snapshot.
        public int Length => m_Chunks.length;

        public TileChunkData this[int index]
        {
            get
            {
                CheckElementReadAccess(index);
                return ((TileChunkData*)m_Chunks.buffer)[index];
            }
        }

        public int tileCount => m_TileIds.length;
        public int spriteCount => m_SpriteIds.length;
        public int matrixCount => m_Matrices.length;
        public int colorCount => m_Colors.length;

        // Resolution helpers for the indices stored on a cell. Callable from Burst.
        public EntityId GetTileEntityId(uint tileAssetIndex)
        {
            CheckSideArrayReadAccess((int)tileAssetIndex, m_TileIds.length);
            return ((EntityId*)m_TileIds.buffer)[tileAssetIndex];
        }

        public EntityId GetSpriteEntityId(uint spriteIndex)
        {
            CheckSideArrayReadAccess((int)spriteIndex, m_SpriteIds.length);
            return ((EntityId*)m_SpriteIds.buffer)[spriteIndex];
        }

        public Matrix4x4 GetMatrix(ushort matrixIndex)
        {
            CheckSideArrayReadAccess(matrixIndex, m_Matrices.length);
            return ((Matrix4x4*)m_Matrices.buffer)[matrixIndex];
        }

        public Color GetColor(ushort colorIndex)
        {
            CheckSideArrayReadAccess(colorIndex, m_Colors.length);
            return ((Color*)m_Colors.buffer)[colorIndex];
        }

        // The sprites of animated Tiles, addressed by TileCellData.animatedSpriteIndex.
        public int animatedSpriteCount => m_AnimatedSpriteIds.length;

        public EntityId GetAnimatedSpriteEntityId(uint animatedSpriteIndex)
        {
            CheckSideArrayReadAccess((int)animatedSpriteIndex, m_AnimatedSpriteIds.length);
            return ((EntityId*)m_AnimatedSpriteIds.buffer)[animatedSpriteIndex];
        }

        // Editor preview Tiles, sorted by y, then x, then z. Always empty outside the Editor.
        public int previewCellCount => m_PreviewCells.length;
        public int previewTileCount => m_PreviewTileIds.length;
        public int previewSpriteCount => m_PreviewSpriteIds.length;
        public int previewMatrixCount => m_PreviewMatrices.length;
        public int previewColorCount => m_PreviewColors.length;

        public TilePreviewCellData GetPreviewCell(int index)
        {
            CheckSideArrayReadAccess(index, m_PreviewCells.length);
            return ((TilePreviewCellData*)m_PreviewCells.buffer)[index];
        }

        public EntityId GetPreviewTileEntityId(uint tileAssetIndex)
        {
            CheckSideArrayReadAccess((int)tileAssetIndex, m_PreviewTileIds.length);
            return ((EntityId*)m_PreviewTileIds.buffer)[tileAssetIndex];
        }

        public EntityId GetPreviewSpriteEntityId(uint spriteIndex)
        {
            CheckSideArrayReadAccess((int)spriteIndex, m_PreviewSpriteIds.length);
            return ((EntityId*)m_PreviewSpriteIds.buffer)[spriteIndex];
        }

        public Matrix4x4 GetPreviewMatrix(ushort matrixIndex)
        {
            CheckSideArrayReadAccess(matrixIndex, m_PreviewMatrices.length);
            return ((Matrix4x4*)m_PreviewMatrices.buffer)[matrixIndex];
        }

        public Color GetPreviewColor(ushort colorIndex)
        {
            CheckSideArrayReadAccess(colorIndex, m_PreviewColors.length);
            return ((Color*)m_PreviewColors.buffer)[colorIndex];
        }

        public void Dispose()
        {
            if (m_IsCreated == 0)
                return;
            m_IsCreated = 0;

            AtomicSafetyHandle.DisposeHandle(ref m_Safety);
            m_Chunks.Dispose();
            m_TileIds.Dispose();
            m_SpriteIds.Dispose();
            m_Matrices.Dispose();
            m_Colors.Dispose();
            m_AnimatedSpriteIds.Dispose();
            m_PreviewCells.Dispose();
            m_PreviewTileIds.Dispose();
            m_PreviewSpriteIds.Dispose();
            m_PreviewMatrices.Dispose();
            m_PreviewColors.Dispose();
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void CheckElementReadAccess(int index)
        {
            if (index < m_MinIndex || index > m_MaxIndex)
                FailOutOfRangeError(index);
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void CheckSideArrayReadAccess(int index, int length)
        {
            if (index < 0 || index >= length)
                FailSideArrayRangeError(index, length);
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
        }

        [BurstDiscard]
        void FailOutOfRangeError(int index)
        {
            if (index < Length && (m_MinIndex != 0 || m_MaxIndex != Length - 1))
                throw new IndexOutOfRangeException(
                    $"Index {index} is out of restricted IJobParallelFor range [{m_MinIndex}...{m_MaxIndex}] in {nameof(TileGridDataArray)}.\n" +
                    "You can only access the element at the job index, unless [NativeDisableParallelForRestriction] is used on the variable.");
            throw new IndexOutOfRangeException($"Index {index} is out of range of '{Length}' Length.");
        }

        [BurstDiscard]
        void FailSideArrayRangeError(int index, int length)
        {
            throw new IndexOutOfRangeException($"Index {index} is out of range of '{length}' Length.");
        }
    }

    [NativeHeader("Modules/Tilemap/ScriptBindings/TileGridData.bindings.h")]
    [VisibleToOtherModules]
    internal static class TilemapTileGridDataExtensions
    {
        // Snapshots the tilemap's tile grid for job / Burst consumption.
        //
        // Main thread only. Dispose the result when done -- it owns eleven native
        // allocations. See TileGridDataArray for the mutation contract.
        public static TileGridDataArray AcquireReadOnlyTileGridData(this Tilemap tilemap, MemoryLabel memoryLabel)
        {
            if (tilemap == null)
                throw new ArgumentNullException(nameof(tilemap));

            TileGridDataArray result = default;
            Internal_AcquireTileGridData(tilemap,
                ref result.m_Chunks, ref result.m_TileIds, ref result.m_SpriteIds, ref result.m_Matrices, ref result.m_Colors,
                ref result.m_AnimatedSpriteIds,
                ref result.m_PreviewCells, ref result.m_PreviewTileIds, ref result.m_PreviewSpriteIds, ref result.m_PreviewMatrices, ref result.m_PreviewColors,
                memoryLabel);
            result.m_IsCreated = 1;

            result.m_MinIndex = 0;
            result.m_MaxIndex = result.Length - 1;
            AtomicSafetyHandle.CreateHandle(out result.m_Safety, Allocator.TempJob);
            // Secondary version with writing disabled makes the snapshot genuinely
            // read-only from a job's point of view, as MeshDataArray does.
            AtomicSafetyHandle.SetAllowSecondaryVersionWriting(result.m_Safety, false);
            AtomicSafetyHandle.UseSecondaryVersion(ref result.m_Safety);
            return result;
        }

        [FreeFunction("TileGridDataBindings::AcquireTileGridData")]
        extern static void Internal_AcquireTileGridData([NotNull] Tilemap tilemap,
            ref Tilemap.TilemapBuffer chunks,
            ref Tilemap.TilemapBuffer tileIds,
            ref Tilemap.TilemapBuffer spriteIds,
            ref Tilemap.TilemapBuffer matrices,
            ref Tilemap.TilemapBuffer colors,
            ref Tilemap.TilemapBuffer animatedSpriteIds,
            ref Tilemap.TilemapBuffer previewCells,
            ref Tilemap.TilemapBuffer previewTileIds,
            ref Tilemap.TilemapBuffer previewSpriteIds,
            ref Tilemap.TilemapBuffer previewMatrices,
            ref Tilemap.TilemapBuffer previewColors,
            MemoryLabel memoryLabel);
    }
}
