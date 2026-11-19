// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.Bindings;

namespace UnityEngine.Tilemaps
{
    // Blittable mirror of the native Tile; the layout is asserted in TileGridData.h. Members must stay icall-free for Burst.
    [StructLayout(LayoutKind.Sequential)]
    [VisibleToOtherModules]
    internal struct TileCellData
    {
        // Bits 0-27 of the flags word are TileFlags; bits 28-29 are internal animation state and bits 30-31 are
        // the collider type (kTileUserEditable and kTileColliderFlagsStart == 30 in TilemapTile.h).
        const uint k_FlagsMask = 0x0FFFFFFFu;
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

    // Blittable mirror of the native ScriptingTilePreviewCellData. Its indices address the preview arrays of TileGridDataArray.
    [StructLayout(LayoutKind.Sequential)]
    [VisibleToOtherModules]
    internal struct TilePreviewCellData
    {
        public Vector3Int position;
        public TileCellData cell;
    }

    // Blittable mirror of the native ScriptingTileChunkEntry.
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct TileChunkEntry
    {
        public TileCellData* tiles;
        public Vector3Int chunkCoord;
    }

    // View of one 16x16 chunk: its live Tile storage and coordinate. Shares the safety handle of its TileGridDataArray,
    // so a copy kept past the snapshot cannot read freed chunk memory, and invalidation completes a job holding one.
    [NativeContainer]
    [NativeContainerIsReadOnly]
    [VisibleToOtherModules]
    internal unsafe struct TileChunkData
    {
        public const int kLogSide = 4;
        public const int kSide = 1 << kLogSide;
        public const int kCells = kSide * kSide;
        const int k_Mask = kSide - 1;

        [NativeDisableUnsafePtrRestriction] internal TileCellData* m_Tiles;
        internal Vector3Int m_ChunkCoord;

        internal AtomicSafetyHandle m_Safety;

        public Vector3Int chunkCoord => m_ChunkCoord;
        public int cellCount => kCells;

        // Row-major and includes empty slots; check TileCellData.isOccupied.
        public TileCellData this[int localIndex]
        {
            get
            {
                CheckCellReadAccess(localIndex);
                return m_Tiles[localIndex];
            }
        }

        // Mirrors TileChunk::CellPositionAt.
        public Vector3Int CellPositionAt(int localIndex)
        {
            return new Vector3Int(
                (m_ChunkCoord.x << kLogSide) | (localIndex & k_Mask),
                (m_ChunkCoord.y << kLogSide) | ((localIndex >> kLogSide) & k_Mask),
                m_ChunkCoord.z);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void CheckCellReadAccess(int localIndex)
        {
            if (localIndex < 0 || localIndex >= kCells)
                FailCellIndexError(localIndex);
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
        }

        static void FailCellIndexError(int localIndex)
        {
            throw new IndexOutOfRangeException($"Index {localIndex} is out of range of '{kCells}' cells in {nameof(TileChunkData)}.");
        }
    }

    // Read-only snapshot of a Tilemap's tile grid for jobs. Chunk cells are live storage; every other array is a copy,
    // and the side tables are index-preserving. Tile edits after acquiring make cell contents undefined, but the Get*
    // resolvers stay in bounds. Dispose it, and complete its jobs, before the Tilemap is cleared, resized, compressed,
    // reloaded or destroyed: those free chunk memory. Collections checks then invalidate the snapshot, so reads throw
    // until it is disposed; without them, reading it is undefined.
    [NativeContainer]
    [NativeContainerIsReadOnly]
    [VisibleToOtherModules]
    internal unsafe struct TileGridDataArray : IDisposable
    {
        // Job safety rejects TilemapBuffer's IntPtr otherwise; the buffers are read-only while jobs run.
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

        // An empty Tilemap yields null buffers, so buffer nullness cannot mark a disposed snapshot.
        internal int m_IsCreated;

        internal AtomicSafetyHandle m_Safety;

        public int Length => m_Chunks.length;

        public TileChunkData this[int index]
        {
            get
            {
                CheckElementReadAccess(index, Length);
                var entry = ((TileChunkEntry*)m_Chunks.buffer)[index];
                TileChunkData chunk;
                chunk.m_Tiles = entry.tiles;
                chunk.m_ChunkCoord = entry.chunkCoord;
                chunk.m_Safety = m_Safety;
                return chunk;
            }
        }

        public int tileCount => m_TileIds.length;
        public int spriteCount => m_SpriteIds.length;
        public int matrixCount => m_Matrices.length;
        public int colorCount => m_Colors.length;

        // An index outside the table, such as a sentinel or one written after acquiring, gives the neutral value
        // of an unused slot: EntityId.None, identity or white.
        public EntityId GetTileEntityId(uint tileAssetIndex)
        {
            CheckReadAccess();
            return tileAssetIndex < (uint)m_TileIds.length ? ((EntityId*)m_TileIds.buffer)[tileAssetIndex] : EntityId.None;
        }

        public EntityId GetSpriteEntityId(uint spriteIndex)
        {
            CheckReadAccess();
            return spriteIndex < (uint)m_SpriteIds.length ? ((EntityId*)m_SpriteIds.buffer)[spriteIndex] : EntityId.None;
        }

        public Matrix4x4 GetMatrix(ushort matrixIndex)
        {
            CheckReadAccess();
            return matrixIndex < (uint)m_Matrices.length ? ((Matrix4x4*)m_Matrices.buffer)[matrixIndex] : Matrix4x4.identity;
        }

        public Color GetColor(ushort colorIndex)
        {
            CheckReadAccess();
            return colorIndex < (uint)m_Colors.length ? ((Color*)m_Colors.buffer)[colorIndex] : Color.white;
        }

        // The sprites of animated Tiles, addressed by TileCellData.animatedSpriteIndex.
        public int animatedSpriteCount => m_AnimatedSpriteIds.length;

        public EntityId GetAnimatedSpriteEntityId(uint animatedSpriteIndex)
        {
            CheckReadAccess();
            return animatedSpriteIndex < (uint)m_AnimatedSpriteIds.length ? ((EntityId*)m_AnimatedSpriteIds.buffer)[animatedSpriteIndex] : EntityId.None;
        }

        // Editor preview Tiles, sorted by y, then x, then z. Always empty outside the Editor.
        public int previewCellCount => m_PreviewCells.length;
        public int previewTileCount => m_PreviewTileIds.length;
        public int previewSpriteCount => m_PreviewSpriteIds.length;
        public int previewMatrixCount => m_PreviewMatrices.length;
        public int previewColorCount => m_PreviewColors.length;

        public TilePreviewCellData GetPreviewCell(int index)
        {
            CheckElementReadAccess(index, m_PreviewCells.length);
            return ((TilePreviewCellData*)m_PreviewCells.buffer)[index];
        }

        public EntityId GetPreviewTileEntityId(uint tileAssetIndex)
        {
            CheckReadAccess();
            return tileAssetIndex < (uint)m_PreviewTileIds.length ? ((EntityId*)m_PreviewTileIds.buffer)[tileAssetIndex] : EntityId.None;
        }

        public EntityId GetPreviewSpriteEntityId(uint spriteIndex)
        {
            CheckReadAccess();
            return spriteIndex < (uint)m_PreviewSpriteIds.length ? ((EntityId*)m_PreviewSpriteIds.buffer)[spriteIndex] : EntityId.None;
        }

        public Matrix4x4 GetPreviewMatrix(ushort matrixIndex)
        {
            CheckReadAccess();
            return matrixIndex < (uint)m_PreviewMatrices.length ? ((Matrix4x4*)m_PreviewMatrices.buffer)[matrixIndex] : Matrix4x4.identity;
        }

        public Color GetPreviewColor(ushort colorIndex)
        {
            CheckReadAccess();
            return colorIndex < (uint)m_PreviewColors.length ? ((Color*)m_PreviewColors.buffer)[colorIndex] : Color.white;
        }

        // Dispose only one copy. Without collections checks, disposing a second copy frees every buffer again.
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
        void CheckReadAccess()
        {
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void CheckElementReadAccess(int index, int length)
        {
            if (index < 0 || index >= length)
                FailOutOfRangeError(index, length);
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
        }

        static void FailOutOfRangeError(int index, int length)
        {
            throw new IndexOutOfRangeException($"Index {index} is out of range of '{length}' Length.");
        }
    }

    [NativeHeader("Modules/Tilemap/ScriptBindings/TileGridData.bindings.h")]
    [VisibleToOtherModules]
    internal static class TilemapTileGridDataExtensions
    {
        // Main thread only. See TileGridDataArray for the lifetime contract.
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

            // Read-only secondary version, owned by the Tilemap so it can invalidate the snapshot.
            result.m_Safety = Internal_RegisterTileGridSnapshot(tilemap);
            InitStaticSafetyId(ref result.m_Safety);
            return result;
        }

        [NoAutoStaticsCleanup] // static safety ids are stable across code reload; re-registering would leak ids
        static int s_StaticSafetyId;

        // Names TileGridDataArray in safety errors, such as the one for a snapshot its Tilemap invalidated.
        static void InitStaticSafetyId(ref AtomicSafetyHandle handle)
        {
            if (s_StaticSafetyId == 0)
                s_StaticSafetyId = AtomicSafetyHandle.NewStaticSafetyId<TileGridDataArray>();
            AtomicSafetyHandle.SetStaticSafetyId(ref handle, s_StaticSafetyId);
        }

        [FreeFunction("TileGridDataBindings::RegisterTileGridSnapshot")]
        extern static AtomicSafetyHandle Internal_RegisterTileGridSnapshot([NotNull] Tilemap tilemap);

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
