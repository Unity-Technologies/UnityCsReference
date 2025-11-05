// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.U2D;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using System.Collections.Generic;
using System.Collections;

namespace UnityEngine.Tilemaps
{
    [Flags]
    public enum TileFlags
    {
        None = 0,
        LockColor = 1 << 0,
        LockTransform = 1 << 1,
        InstantiateGameObjectRuntimeOnly = 1 << 2,
        KeepGameObjectRuntimeOnly = 1 << 3,
        LockAll = LockColor | LockTransform,
    }

    [Flags]
    public enum TileAnimationFlags
    {
        None = 0,
        LoopOnce = 1 << 0,
        PauseAnimation = 1 << 1,
        UpdatePhysics = 1 << 2,
        UnscaledTime = 1 << 3,
        SyncAnimation = 1 << 4,
    }

    [RequireComponent(typeof(Transform))]
    [NativeHeader("Modules/Grid/Public/GridMarshalling.h")]
    [NativeHeader("Modules/Grid/Public/Grid.h")]
    [NativeHeader("Runtime/Graphics/SpriteFrame.h")]
    [NativeHeader("Modules/Tilemap/Public/TilemapTile.h")]
    [NativeHeader("Modules/Tilemap/Public/TilemapMarshalling.h")]
    [NativeType(Header = "Modules/Tilemap/Public/Tilemap.h")]
    public sealed partial class Tilemap : GridLayout
    {
        public enum Orientation
        {
            XY = 0,
            XZ = 1,
            YX = 2,
            YZ = 3,
            ZX = 4,
            ZY = 5,
            Custom = 6,
        }

        public extern Grid layoutGrid
        {
            [NativeMethod(Name = "GetAttachedGrid")]
            get;
        }

        public Vector3 GetCellCenterLocal(Vector3Int position) { return CellToLocalInterpolated(position) + CellToLocalInterpolated(tileAnchor); }
        public Vector3 GetCellCenterWorld(Vector3Int position) { return LocalToWorld(CellToLocalInterpolated(position) + CellToLocalInterpolated(tileAnchor)); }

        public BoundsInt cellBounds
        {
            get
            {
                return new BoundsInt(origin, size);
            }
        }

        [NativeProperty("TilemapBoundsScripting")]
        public extern Bounds localBounds
        {
            get;
        }

        [NativeProperty("TilemapFrameBoundsScripting")]
        internal extern Bounds localFrameBounds
        {
            get;
        }

        public extern float animationFrameRate
        {
            get;
            set;
        }

        public extern Color color
        {
            get;
            set;
        }
        public extern Vector3Int origin
        {
            get;
            set;
        }

        public extern Vector3Int size
        {
            get;
            set;
        }

        [NativeProperty(Name = "TileAnchorScripting")]
        public extern Vector3 tileAnchor
        {
            get;
            set;
        }

        public extern Orientation orientation
        {
            get;
            set;
        }

        public extern Matrix4x4 orientationMatrix
        {
            [NativeMethod(Name = "GetTileOrientationMatrix")]
            get;
            [NativeMethod(Name = "SetOrientationMatrix")]
            set;
        }

        internal extern Object GetTileAsset(Vector3Int position);
        public TileBase GetTile(Vector3Int position) { return GetTileAsset(position) as TileBase; }
        public T GetTile<T>(Vector3Int position) where T : TileBase { return GetTileAsset(position) as T; }

        [NativeMethod(Name = "GetTileAssetEntityId", IsThreadSafe = true)]
        public extern EntityId GetTileEntityId(Vector3Int position);

        internal extern IntPtr GetTilemapHandle();

        [NativeMethod(Name = "GetTileEntityIdFromHandle", IsThreadSafe = true)]
        internal static extern EntityId GetTileEntityIdFromHandle(IntPtr tilemapHandle, Vector3Int position);

        [NativeMethod(Name = "GetTileEntityIdsFromOffsets", IsThreadSafe = true)]
        private extern void GetTileEntityIdsFromOffsets(Vector3Int position, IntPtr offsetsIntrPtr, IntPtr tilesIntPtr, int count);

        [NativeMethod(Name = "GetTileEntityIdsFromOffsetsAndHandle", IsThreadSafe = true)]
        private static extern void GetTileEntityIdsFromOffsetsAndHandle(IntPtr tilemapHandle, Vector3Int position, IntPtr offsetsIntrPtr, IntPtr tilesIntPtr, int count);

        [NativeMethod(Name = "GetTileEntityIdsFromBlockOffset", IsThreadSafe = true)]
        private extern void GetTileEntityIdsFromBlockOffset(Vector3Int position, BoundsInt blockOffset, IntPtr tilesIntPtr, int count);

        [NativeMethod(Name = "GetTileEntityIdsFromBlockOffsetAndHandle", IsThreadSafe = true)]
        private static extern void GetTileEntityIdsFromBlockOffsetAndHandle(IntPtr tilemapHandle, Vector3Int position, BoundsInt blockOffset, IntPtr tilesIntPtr, int count);

        internal extern Object[] GetTileAssetsBlock(Vector3Int position, Vector3Int blockDimensions);

        public TileBase[] GetTilesBlock(BoundsInt bounds)
        {
            var array = GetTileAssetsBlock(bounds.min, bounds.size);
            var tiles = new TileBase[array.Length];
            for (int i = 0; i < array.Length; ++i)
            {
                tiles[i] = (TileBase)array[i];
            }
            return tiles;
        }

        [FreeFunction(Name = "TilemapBindings::GetTileAssetsBlockNonAlloc", HasExplicitThis = true)]
        internal extern int GetTileAssetsBlockNonAlloc(Vector3Int startPosition, Vector3Int endPosition, [UnityMarshalAs(NativeType.ScriptingObjectPtr)] Object[] tiles);

        public int GetTilesBlockNonAlloc(BoundsInt bounds, TileBase[] tiles)
        {
            return GetTileAssetsBlockNonAlloc(bounds.min, bounds.size, tiles);
        }

        public extern int GetTilesRangeCount(Vector3Int startPosition, Vector3Int endPosition);

        [FreeFunction(Name = "TilemapBindings::GetTileAssetsRangeNonAlloc", HasExplicitThis = true)]
        internal extern int GetTileAssetsRangeNonAlloc(Vector3Int startPosition, Vector3Int endPosition, Vector3Int[] positions, [UnityMarshalAs(NativeType.ScriptingObjectPtr)] Object[] tiles);

        public int GetTilesRangeNonAlloc(Vector3Int startPosition, Vector3Int endPosition, Vector3Int[] positions, TileBase[] tiles)
        {
            return GetTileAssetsRangeNonAlloc(startPosition, endPosition, positions, tiles);
        }

        internal extern void SetTileAsset(Vector3Int position, Object tile);

        public void SetTile(Vector3Int position, TileBase tile) { SetTileAsset(position, tile); }

        internal extern void SetTileAssets(Vector3Int[] positionArray, Object[] tileArray);

        public void SetTiles(Vector3Int[] positionArray, TileBase[] tileArray) { SetTileAssets(positionArray, tileArray); }

        [NativeMethod(Name = "SetTileAssetsBlock")]
        private extern void INTERNAL_CALL_SetTileAssetsBlock(Vector3Int position, Vector3Int blockDimensions, Object[] tileArray);
        public void SetTilesBlock(BoundsInt position, TileBase[] tileArray) { INTERNAL_CALL_SetTileAssetsBlock(position.min, position.size, tileArray); }

        [NativeMethod(Name = "SetTileChangeData")]
        public extern void SetTile(TileChangeData tileChangeData, bool ignoreLockFlags);
        [NativeMethod(Name = "SetTileChangeDataArray")]
        public extern void SetTiles(TileChangeData[] tileChangeDataArray, bool ignoreLockFlags);

        public bool HasTile(Vector3Int position)
        {
            return GetTileAsset(position) != null;
        }

        [NativeMethod(Name = "RefreshTileAsset")]
        public extern void RefreshTile(Vector3Int position);

        [FreeFunction(Name = "TilemapBindings::RefreshTileAssetsNative", HasExplicitThis = true)]
        internal extern unsafe void RefreshTilesNative(void* positions, int count, bool needSortRemoveDup);

        [NativeMethod(Name = "RefreshAllTileAssets")]
        public extern void RefreshAllTiles();

        internal extern void SwapTileAsset(Object changeTile, Object newTile);
        public void SwapTile(TileBase changeTile, TileBase newTile) { SwapTileAsset(changeTile, newTile); }

        internal extern bool ContainsTileAsset(Object tileAsset);
        public bool ContainsTile(TileBase tileAsset) { return ContainsTileAsset(tileAsset); }

        public extern int GetUsedTilesCount();

        public extern int GetUsedSpritesCount();

        public int GetUsedTilesNonAlloc(TileBase[] usedTiles)
        {
            return Internal_GetUsedTilesNonAlloc(usedTiles);
        }

        public int GetUsedSpritesNonAlloc(Sprite[] usedSprites)
        {
            return Internal_GetUsedSpritesNonAlloc(usedSprites);
        }

        [FreeFunction(Name = "TilemapBindings::GetUsedTilesNonAlloc", HasExplicitThis = true)]
        internal extern int Internal_GetUsedTilesNonAlloc([UnityMarshalAs(NativeType.ScriptingObjectPtr)] Object[] usedTiles);

        [FreeFunction(Name = "TilemapBindings::GetUsedSpritesNonAlloc", HasExplicitThis = true)]
        internal extern int Internal_GetUsedSpritesNonAlloc([UnityMarshalAs(NativeType.ScriptingObjectPtr)] Object[] usedSprites);

        public extern Sprite GetSprite(Vector3Int position);

        public extern Matrix4x4 GetTransformMatrix(Vector3Int position);
        public extern void SetTransformMatrix(Vector3Int position, Matrix4x4 transform);

        [NativeMethod(Name = "GetTileColor")]
        public extern Color GetColor(Vector3Int position);

        [NativeMethod(Name = "SetTileColor")]
        public extern void SetColor(Vector3Int position, Color color);

        public extern TileFlags GetTileFlags(Vector3Int position);
        public extern void SetTileFlags(Vector3Int position, TileFlags flags);
        public extern void AddTileFlags(Vector3Int position, TileFlags flags);
        public extern void RemoveTileFlags(Vector3Int position, TileFlags flags);

        [NativeMethod(Name = "GetTileInstantiatedObject")]
        public extern GameObject GetInstantiatedObject(Vector3Int position);

        [NativeMethod(Name = "GetTileObjectToInstantiate")]
        public extern GameObject GetObjectToInstantiate(Vector3Int position);

        [NativeMethod(Name = "SetTileColliderType")]
        public extern void SetColliderType(Vector3Int position, Tile.ColliderType colliderType);
        [NativeMethod(Name = "GetTileColliderType")]
        public extern Tile.ColliderType GetColliderType(Vector3Int position);

        [NativeMethod(Name = "GetTileAnimationFrameCount")]
        public extern int GetAnimationFrameCount(Vector3Int position);
        [NativeMethod(Name = "GetTileAnimationFrame")]
        public extern int GetAnimationFrame(Vector3Int position);
        [NativeMethod(Name = "SetTileAnimationFrame")]
        public extern void SetAnimationFrame(Vector3Int position, int frame);

        [NativeMethod(Name = "GetTileAnimationTime")]
        public extern float GetAnimationTime(Vector3Int position);
        [NativeMethod(Name = "SetTileAnimationTime")]
        public extern void SetAnimationTime(Vector3Int position, float time);

        public extern TileAnimationFlags GetTileAnimationFlags(Vector3Int position);
        public extern void SetTileAnimationFlags(Vector3Int position, TileAnimationFlags flags);
        public extern void AddTileAnimationFlags(Vector3Int position, TileAnimationFlags flags);
        public extern void RemoveTileAnimationFlags(Vector3Int position, TileAnimationFlags flags);

        public void FloodFill(Vector3Int position, TileBase tile)
        {
            FloodFillTileAsset(position, tile);
        }

        [NativeMethod(Name = "FloodFill")]
        private extern void FloodFillTileAsset(Vector3Int position, Object tile);

        public void BoxFill(Vector3Int position, TileBase tile, int startX, int startY, int endX, int endY)
        {
            BoxFillTileAsset(position, tile, startX, startY, endX, endY);
        }

        [NativeMethod(Name = "BoxFill")]
        private extern void BoxFillTileAsset(Vector3Int position, Object tile, int startX, int startY, int endX, int endY);

        public void InsertCells(Vector3Int position, Vector3Int insertCells)
        {
            InsertCells(position, insertCells.x, insertCells.y, insertCells.z);
        }

        public extern void InsertCells(Vector3Int position, int numColumns, int numRows, int numLayers);

        public void DeleteCells(Vector3Int position, Vector3Int deleteCells)
        {
            DeleteCells(position, deleteCells.x, deleteCells.y, deleteCells.z);
        }

        public extern void DeleteCells(Vector3Int position, int numColumns, int numRows, int numLayers);

        public extern void ClearAllTiles();
        public extern void ResizeBounds();

        [NativeMethod(Name = "CompressBounds")]
        private extern void CompressTilemapBounds(bool keepEditorPreview);

        public void CompressBounds() { CompressTilemapBounds(false); }

        internal void CompressBoundsKeepEditorPreview() { CompressTilemapBounds(true); }

        public extern Vector3Int editorPreviewOrigin
        {
            [NativeMethod(Name = "GetRenderOrigin")]
            get;
        }

        public extern Vector3Int editorPreviewSize
        {
            [NativeMethod(Name = "GetRenderSize")]
            get;
        }

        internal extern Object GetAnyTileAsset(Vector3Int position);
        internal TileBase GetAnyTile(Vector3Int position) { return GetAnyTileAsset(position) as TileBase; }
        internal T GetAnyTile<T>(Vector3Int position) where T : TileBase { return GetAnyTile(position) as T; }
        [NativeMethod(Name = "GetAnyTileAssetEntityId", IsThreadSafe = true)]
        internal extern EntityId GetAnyTileEntityId(Vector3Int position);

        [NativeMethod(Name = "GetAnyTileEntityIdFromHandle", IsThreadSafe = true)]
        internal static extern EntityId GetAnyTileEntityIdFromHandle(IntPtr tilemapHandle, Vector3Int position);

        [NativeMethod(Name = "GetAnyTileEntityIdsFromOffsets", IsThreadSafe = true)]
        private extern void GetAnyTileEntityIdsFromOffsets(Vector3Int position, IntPtr offsetsIntrPtr, IntPtr tilesIntPtr, int count);

        [NativeMethod(Name = "GetAnyTileEntityIdsFromOffsetsAndHandle", IsThreadSafe = true)]
        private static extern void GetAnyTileEntityIdsFromOffsetsAndHandle(IntPtr tilemapHandle, Vector3Int position, IntPtr offsetsIntrPtr, IntPtr tilesIntPtr, int count);

        [NativeMethod(Name = "GetAnyTileEntityIdsFromBlockOffset", IsThreadSafe = true)]
        private extern void GetAnyTileEntityIdsFromBlockOffset(Vector3Int position, BoundsInt blockOffset, IntPtr tilesIntPtr, int count);

        [NativeMethod(Name = "GetAnyTileEntityIdsFromBlockOffsetAndHandle", IsThreadSafe = true)]
        private static extern void GetAnyTileEntityIdsFromBlockOffsetAndHandle(IntPtr tilemapHandle, Vector3Int position, BoundsInt blockOffset, IntPtr tilesIntPtr, int count);

        internal extern Object GetEditorPreviewTileAsset(Vector3Int position);
        public TileBase GetEditorPreviewTile(Vector3Int position) { return GetEditorPreviewTileAsset(position) as TileBase; }
        public T GetEditorPreviewTile<T>(Vector3Int position) where T : TileBase { return GetEditorPreviewTile(position) as T; }

        [NativeMethod(Name = "GetEditorPreviewTileAssetEntityId", IsThreadSafe = true)]
        public extern EntityId GetEditorPreviewTileEntityId(Vector3Int position);

        internal extern void SetEditorPreviewTileAsset(Vector3Int position, Object tile);
        public void SetEditorPreviewTile(Vector3Int position, TileBase tile) { SetEditorPreviewTileAsset(position, tile); }

        public bool HasEditorPreviewTile(Vector3Int position)
        {
            return GetEditorPreviewTileAsset(position) != null;
        }

        public extern Sprite GetEditorPreviewSprite(Vector3Int position);

        public extern Matrix4x4 GetEditorPreviewTransformMatrix(Vector3Int position);
        public extern void SetEditorPreviewTransformMatrix(Vector3Int position, Matrix4x4 transform);

        [NativeMethod(Name = "GetEditorPreviewTileColor")]
        public extern Color GetEditorPreviewColor(Vector3Int position);
        [NativeMethod(Name = "SetEditorPreviewTileColor")]
        public extern void SetEditorPreviewColor(Vector3Int position, Color color);

        public extern TileFlags GetEditorPreviewTileFlags(Vector3Int position);

        public void EditorPreviewFloodFill(Vector3Int position, TileBase tile)
        {
            EditorPreviewFloodFillTileAsset(position, tile);
        }

        [NativeMethod(Name = "EditorPreviewFloodFill")]
        private extern void EditorPreviewFloodFillTileAsset(Vector3Int position, Object tile);

        public void EditorPreviewBoxFill(Vector3Int position, Object tile, int startX, int startY, int endX, int endY)
        {
            EditorPreviewBoxFillTileAsset(position, tile, startX, startY, endX, endY);
        }

        [NativeMethod(Name = "EditorPreviewBoxFill")]
        private extern void EditorPreviewBoxFillTileAsset(Vector3Int position, Object tile, int startX, int startY, int endX, int endY);

        [NativeMethod(Name = "ClearAllEditorPreviewTileAssets")]
        public extern void ClearAllEditorPreviewTiles();


        [RequiredByNativeCode]
        internal void GetLoopEndedForTileAnimationCallbackSettings(ref bool hasEndLoopForTileAnimationCallback)
        {
            hasEndLoopForTileAnimationCallback = HasLoopEndedForTileAnimationCallback();
        }

        [RequiredByNativeCode]
        private void DoLoopEndedForTileAnimationCallback(int count, IntPtr positionsIntPtr)
        {
            HandleLoopEndedForTileAnimationCallback(count, positionsIntPtr);
        }

        [RequiredByNativeCode]
        public struct SyncTile
        {
            internal Vector3Int m_Position;
            internal TileBase m_Tile;
            internal TileData m_TileData;

            public Vector3Int position
            {
                get { return m_Position; }
            }

            public TileBase tile
            {
                get { return m_Tile; }
            }

            public TileData tileData
            {
                get { return m_TileData; }
            }
        }

        internal struct SyncTileCallbackSettings
        {
            internal bool hasSyncTileCallback;
            internal bool hasPositionsChangedCallback;
            internal bool isBufferSyncTile;
        }

        [RequiredByNativeCode]
        internal void GetSyncTileCallbackSettings(ref SyncTileCallbackSettings settings)
        {
            settings.hasSyncTileCallback = HasSyncTileCallback();
            settings.hasPositionsChangedCallback = HasPositionsChangedCallback();
            settings.isBufferSyncTile = bufferSyncTile;
        }

        internal extern void SendAndClearSyncTileBuffer();

        [RequiredByNativeCode]
        private void DoSyncTileCallback(SyncTile[] syncTiles)
        {
            HandleSyncTileCallback(syncTiles);
        }

        [RequiredByNativeCode]
        private void DoPositionsChangedCallback(int count, IntPtr positionsIntPtr)
        {
            HandlePositionsChangedCallback(count, positionsIntPtr);
        }

        #region Non Allocating Getters

        [StructLayout(LayoutKind.Sequential)]
        internal struct TilemapBuffer : IDisposable
        {
            public readonly IntPtr buffer => m_Buffer;
            public readonly int length => m_Length;
            public readonly Allocator allocator => m_Allocator;

            public TilemapBuffer()
            {
                m_Buffer = IntPtr.Zero;
                m_Length = 0;
                m_Allocator = Allocator.None;
            }

            public unsafe readonly T AsEngineObject<T>(int index) where T : class
            {
                if (index < 0 || index >= m_Length)
                    throw new ArgumentOutOfRangeException("index");

                var entityId = UnsafeUtility.ArrayElementAsRef<EntityId>(m_Buffer.ToPointer(), index);
                return Resources.EntityIdIsValid(entityId) ? Resources.EntityIdToObject(entityId) as T : null;
            }

            public unsafe readonly T As<T>(int index) where T : struct
            {
                if (index < 0 || index >= m_Length)
                    throw new ArgumentOutOfRangeException("index");

                return UnsafeUtility.ArrayElementAsRef<T>(m_Buffer.ToPointer(), index);
            }

            public unsafe void Dispose()
            {
                if (m_Buffer == null || m_Length == 0)
                    return;

                // Free the allocation.
                UnsafeUtility.FreeTracked(m_Buffer.ToPointer(), m_Allocator);
                m_Buffer = IntPtr.Zero;
                m_Length = 0;
                m_Allocator = Allocator.None;
            }

            #region Internal

            IntPtr m_Buffer;
            int m_Length;
            Allocator m_Allocator;

            #endregion
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct TileArray : IEnumerable<TileBase>, IDisposable
        {
            internal struct TileArrayEnumerator : IEnumerator<TileBase>
            {
                TileArray m_TileArray;
                int m_Index;

                public TileArrayEnumerator(TileArray tileArray)
                {
                    m_TileArray = tileArray;
                    m_Index = -1;
                }

                TileBase IEnumerator<TileBase>.Current => m_TileArray[m_Index];

                object IEnumerator.Current => m_TileArray[m_Index];

                void IDisposable.Dispose()
                {
                    // Does not own the buffer, so nothing to dispose
                }

                bool IEnumerator.MoveNext()
                {
                    if (m_TileArray.Length == 0)
                        return false;

                    return ++m_Index < m_TileArray.Length;
                }

                void IEnumerator.Reset()
                {
                    m_Index = -1;
                }
            }

            internal TileArray(TilemapBuffer tilemapBuffer)
            {
                m_TilemapBuffer = tilemapBuffer;
            }

            public readonly int Length => m_TilemapBuffer.length;
            public readonly TileBase this[int index] => m_TilemapBuffer.AsEngineObject<TileBase>(index);

            #region Enumeration

            public readonly IEnumerator<TileBase> GetEnumerator() => new TileArrayEnumerator(this);
            readonly IEnumerator IEnumerable.GetEnumerator() => new TileArrayEnumerator(this);

            public void Dispose() => m_TilemapBuffer.Dispose();

            #endregion

            #region Internal

            TilemapBuffer m_TilemapBuffer;

            #endregion
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SpriteArray : IEnumerable<Sprite>, IDisposable
        {
            internal struct SpriteArrayEnumerator : IEnumerator<Sprite>
            {
                SpriteArray m_SpriteArray;
                int m_Index;

                public SpriteArrayEnumerator(SpriteArray spriteArray)
                {
                    m_SpriteArray = spriteArray;
                    m_Index = -1;
                }

                Sprite IEnumerator<Sprite>.Current => m_SpriteArray[m_Index];

                object IEnumerator.Current => m_SpriteArray[m_Index];

                void IDisposable.Dispose()
                {
                    // Does not own the buffer, so nothing to dispose
                }

                bool IEnumerator.MoveNext()
                {
                    if (m_SpriteArray.Length == 0)
                        return false;

                    return ++m_Index < m_SpriteArray.Length;
                }

                void IEnumerator.Reset()
                {
                    m_Index = -1;
                }
            }

            internal SpriteArray(TilemapBuffer tilemapBuffer)
            {
                m_TilemapBuffer = tilemapBuffer;
            }

            public readonly int Length => m_TilemapBuffer.length;
            public readonly Sprite this[int index] => m_TilemapBuffer.AsEngineObject<Sprite>(index);

            #region Enumeration

            public readonly IEnumerator<Sprite> GetEnumerator() => new SpriteArrayEnumerator(this);
            readonly IEnumerator IEnumerable.GetEnumerator() => new SpriteArrayEnumerator(this);

            public void Dispose() => m_TilemapBuffer.Dispose();

            #endregion

            #region Internal

            TilemapBuffer m_TilemapBuffer;

            #endregion
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PositionArray : IEnumerable<Vector3Int>, IDisposable
        {
            internal struct PositionArrayEnumerator : IEnumerator<Vector3Int>
            {
                PositionArray m_PositionArray;
                int m_Index;

                public PositionArrayEnumerator(PositionArray positionArray)
                {
                    m_PositionArray = positionArray;
                    m_Index = -1;
                }

                Vector3Int IEnumerator<Vector3Int>.Current => m_PositionArray[m_Index];

                object IEnumerator.Current => m_PositionArray[m_Index];

                void IDisposable.Dispose()
                {
                    // Does not own the buffer, so nothing to dispose
                }

                bool IEnumerator.MoveNext()
                {
                    if (m_PositionArray.Length == 0)
                        return false;

                    return ++m_Index < m_PositionArray.Length;
                }

                void IEnumerator.Reset()
                {
                    m_Index = -1;
                }
            }

            internal PositionArray(TilemapBuffer tilemapBuffer)
            {
                m_TilemapBuffer = tilemapBuffer;
            }

            public readonly int Length => m_TilemapBuffer.length;
            public readonly Vector3Int this[int index] => m_TilemapBuffer.As<Vector3Int>(index);

            #region Enumeration

            public readonly IEnumerator<Vector3Int> GetEnumerator() => new PositionArrayEnumerator(this);
            readonly IEnumerator IEnumerable.GetEnumerator() => new PositionArrayEnumerator(this);
            public void Dispose() => m_TilemapBuffer.Dispose();

            #endregion

            #region Internal

            TilemapBuffer m_TilemapBuffer;

            #endregion
        }

        private const string k_TilemapAllocationArgumentExceptionMessage = "Allocator must be 'Temp', 'Domain' or `Persistent`";
        private const string k_TilemapMemoryLabelArgumentExceptionMessage = "MemoryLabel has not been created. Use the constructor to create it.";

        private void MemoryLabelCheck(MemoryLabel memoryLabel)
        {
            if (!memoryLabel.IsCreated)
                throw new ArgumentException(k_TilemapMemoryLabelArgumentExceptionMessage);
        }

        public TileArray GetUsedTiles(Allocator allocator = Allocator.Temp)
        {
            if (allocator == Allocator.Temp || allocator == Allocator.Persistent || allocator == Allocator.Domain)
                return new(Internal_GetUsedTiles(allocator, IntPtr.Zero));

            throw new ArgumentException(k_TilemapAllocationArgumentExceptionMessage);
        }

        public TileArray GetUsedTiles(MemoryLabel memoryLabel)
        {
            MemoryLabelCheck(memoryLabel);
            // Memory Label Allocators must be Persistent or Domain
            return new(Internal_GetUsedTiles(memoryLabel.allocator, memoryLabel.pointer));
        }

        public SpriteArray GetUsedSprites(Allocator allocator = Allocator.Temp)
        {
            if (allocator == Allocator.Temp || allocator == Allocator.Persistent || allocator == Allocator.Domain)
                return new(Internal_GetUsedSprites(allocator, IntPtr.Zero));

            throw new ArgumentException(k_TilemapAllocationArgumentExceptionMessage);
        }

        public SpriteArray GetUsedSprites(MemoryLabel memoryLabel)
        {
            MemoryLabelCheck(memoryLabel);
            // Memory Label Allocators are Persistent or Domain
            return new(Internal_GetUsedSprites(memoryLabel.allocator, memoryLabel.pointer));
        }

        public TileArray GetTiles(BoundsInt bounds, Allocator allocator = Allocator.Temp)
        {
            if (allocator == Allocator.Temp || allocator == Allocator.Persistent || allocator == Allocator.Domain)
                return new(Internal_GetTiles(bounds.min, bounds.size, allocator, IntPtr.Zero));

            throw new ArgumentException(k_TilemapAllocationArgumentExceptionMessage);
        }

        public TileArray GetTiles(BoundsInt bounds, MemoryLabel memoryLabel)
        {
            MemoryLabelCheck(memoryLabel);
            // Memory Label Allocators are Persistent or Domain
            return new(Internal_GetTiles(bounds.min, bounds.size, memoryLabel.allocator, memoryLabel.pointer));
        }

        public int GetTiles(BoundsInt bounds, out PositionArray positions, out TileArray tiles, Allocator allocator = Allocator.Temp, bool withinBounds = true)
        {
            if (allocator == Allocator.Temp || allocator == Allocator.Persistent || allocator == Allocator.Domain)
            {
                var positionsBuffer = new TilemapBuffer();
                var tilesBuffer = new TilemapBuffer();
                var length = Internal_GetTilePositions(bounds.min, bounds.max, ref positionsBuffer, ref tilesBuffer, withinBounds ? 1 : 0, allocator, IntPtr.Zero);

                positions = new(positionsBuffer);
                tiles = new(tilesBuffer);

                return length;
            }

            throw new ArgumentException(k_TilemapAllocationArgumentExceptionMessage);
        }

        public int GetTiles(BoundsInt bounds, out PositionArray positions, out TileArray tiles, MemoryLabel memoryLabel, bool withinBounds = true)
        {
            MemoryLabelCheck(memoryLabel);
            var positionsBuffer = new TilemapBuffer();
            var tilesBuffer = new TilemapBuffer();

            // Memory Label Allocators are Persistent or Domain
            var length = Internal_GetTilePositions(bounds.min, bounds.max, ref positionsBuffer, ref tilesBuffer, withinBounds ? 1 : 0, memoryLabel.allocator, memoryLabel.pointer);

            positions = new(positionsBuffer);
            tiles = new(tilesBuffer);

            return length;
        }

        [FreeFunction(Name = "TilemapBindings::GetUsedTiles", HasExplicitThis = true)]
        extern TilemapBuffer Internal_GetUsedTiles(Allocator allocator, IntPtr memLabelPtr);

        [FreeFunction(Name = "TilemapBindings::GetUsedSprites", HasExplicitThis = true)]
        extern TilemapBuffer Internal_GetUsedSprites(Allocator allocator, IntPtr memLabelPtr);

        [FreeFunction(Name = "TilemapBindings::GetTiles", HasExplicitThis = true)]
        extern TilemapBuffer Internal_GetTiles(Vector3Int startPosition, Vector3Int blockDimensions, Allocator allocator, IntPtr memLabelPtr);

        [FreeFunction(Name = "TilemapBindings::GetTilePositions", HasExplicitThis = true)]
        extern int Internal_GetTilePositions(Vector3Int startPosition, Vector3Int endPosition, ref TilemapBuffer positions, ref TilemapBuffer tiles, int withinBounds, Allocator allocator, IntPtr memLabelPtr);

        #endregion
    }

    [RequireComponent(typeof(Tilemap))]
    [NativeHeader("Modules/Grid/Public/GridMarshalling.h")]
    [NativeHeader("Modules/Tilemap/TilemapRendererJobs.h")]
    [NativeHeader("Modules/Tilemap/Public/TilemapMarshalling.h")]
    [NativeType(Header = "Modules/Tilemap/Public/TilemapRenderer.h")]
    public sealed partial class TilemapRenderer : Renderer
    {
        public enum SortOrder
        {
            BottomLeft = 0,
            BottomRight = 1,
            TopLeft = 2,
            TopRight = 3,
        }

        public enum Mode
        {
            Chunk = 0,
            Individual = 1,
            SRPBatch = 2,
        }

        public enum DetectChunkCullingBounds
        {
            Auto = 0,
            Manual = 1,
        }

        public extern Vector3Int chunkSize
        {
            get;
            set;
        }

        public extern Vector3 chunkCullingBounds
        {
            [FreeFunction("TilemapRendererBindings::GetChunkCullingBounds", HasExplicitThis = true)]
            get;
            [FreeFunction("TilemapRendererBindings::SetChunkCullingBounds", HasExplicitThis = true)]
            set;
        }

        public extern int maxChunkCount
        {
            get;
            set;
        }

        public extern int maxFrameAge
        {
            get;
            set;
        }

        public extern SortOrder sortOrder
        {
            get;
            set;
        }

        [NativeProperty("RenderMode")]
        public extern Mode mode
        {
            get;
            set;
        }

        public extern DetectChunkCullingBounds detectChunkCullingBounds
        {
            get;
            set;
        }

        public extern SpriteMaskInteraction maskInteraction
        {
            get;
            set;
        }

        [RequiredByNativeCode]
        internal void RegisterSpriteAtlasRegistered()
        {
            SpriteAtlasManager.atlasRegistered += OnSpriteAtlasRegistered;
        }

        [RequiredByNativeCode]
        internal void UnregisterSpriteAtlasRegistered()
        {
            SpriteAtlasManager.atlasRegistered -= OnSpriteAtlasRegistered;
        }

        internal extern void OnSpriteAtlasRegistered(SpriteAtlas atlas);
    }

    [RequiredByNativeCode]
    [StructLayoutAttribute(LayoutKind.Sequential)]
    [NativeType(Header = "Modules/Tilemap/TilemapScripting.h")]
    public partial struct TileData
    {
        public Sprite sprite { get { return Object.ForceLoadFromInstanceID(m_Sprite) as Sprite; } set { m_Sprite = value != null ? value.GetEntityId() : 0; } }
        public EntityId spriteEntityId { get => m_Sprite; set => m_Sprite = value; }
        public Color color { get { return m_Color; } set { m_Color = value; } }
        public Matrix4x4 transform { get { return m_Transform; } set { m_Transform = value; } }
        public GameObject gameObject { get { return Object.ForceLoadFromInstanceID(m_GameObject) as GameObject; } set { m_GameObject = value != null ? value.GetEntityId() : 0; } }
        public EntityId gameObjectEntityId { get => m_GameObject; set => m_GameObject = value; }
        public TileFlags flags { get { return m_Flags; } set { m_Flags = value; } }
        public Tile.ColliderType colliderType { get { return m_ColliderType; } set { m_ColliderType = value; } }

        private EntityId m_Sprite;
        private Color m_Color;
        private Matrix4x4 m_Transform;
        private EntityId m_GameObject;
        private TileFlags m_Flags;
        private Tile.ColliderType m_ColliderType;

        internal static readonly TileData Default = CreateDefault();
        private static TileData CreateDefault()
        {
            TileData tileData = default;
            tileData.m_Sprite = EntityId.None;
            tileData.m_Color = Color.white;
            tileData.m_Transform = Matrix4x4.identity;
            tileData.m_GameObject = EntityId.None;
            tileData.m_Flags = default;
            tileData.m_ColliderType = default;
            return tileData;
        }
    }

    [Serializable]
    [RequiredByNativeCode]
    [StructLayoutAttribute(LayoutKind.Sequential)]
    [NativeType(Header = "Modules/Tilemap/TilemapScripting.h")]
    public partial struct TileChangeData
    {
        public Vector3Int position { get { return m_Position; } set { m_Position = value; } }
        public TileBase tile { get { return (TileBase)m_TileAsset; } set { m_TileAsset = value; } }
        public Color color { get { return m_Color; } set { m_Color = value; } }
        public Matrix4x4 transform { get { return m_Transform; } set { m_Transform = value; } }

        [SerializeField]
        private Vector3Int m_Position;
        [SerializeField]
        private Object m_TileAsset;
        [SerializeField]
        private Color m_Color;
        [SerializeField]
        private Matrix4x4 m_Transform;

        public TileChangeData(Vector3Int position, TileBase tile, Color color, Matrix4x4 transform)
        {
            m_Position = position;
            m_TileAsset = tile;
            m_Color = color;
            m_Transform = transform;
        }
    }

    [RequiredByNativeCode]
    [StructLayoutAttribute(LayoutKind.Sequential)]
    [NativeType(Header = "Modules/Tilemap/TilemapScripting.h")]
    public partial struct TileAnimationData
    {
        public Sprite[] animatedSprites { get { return m_AnimatedSprites; } set { m_AnimatedSprites = value; } }
        public float animationSpeed { get { return m_AnimationSpeed; } set { m_AnimationSpeed = value; } }
        public float animationStartTime { get { return m_AnimationStartTime; } set { m_AnimationStartTime = value; } }
        public TileAnimationFlags flags { get { return m_Flags; } set { m_Flags = value; } }

        private Sprite[] m_AnimatedSprites;
        private float m_AnimationSpeed;
        private float m_AnimationStartTime;
        private TileAnimationFlags m_Flags;
    }

    [RequiredByNativeCode]
    [StructLayoutAttribute(LayoutKind.Sequential)]
    [NativeType(Header = "Modules/Tilemap/TilemapScripting.h")]
    public partial struct TileAnimationEntityIdData
    {
        public NativeArray<EntityId> animatedSpritesEntityIds
        {
            set
            {
                if (!value.IsCreated)
                    return;
                unsafe
                {
                    m_AnimatedSpritesEntityIdPtr = (IntPtr)NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(value);
                    m_Count = value.Length;
                }
            }
        }

        internal IntPtr animatedSpritesEntityIdPtr { get => m_AnimatedSpritesEntityIdPtr; set => m_AnimatedSpritesEntityIdPtr = value; }
        internal int count { get => m_Count; set => m_Count = value; }
        public float animationSpeed { get { return m_AnimationSpeed; } set { m_AnimationSpeed = value; } }
        public float animationStartTime { get { return m_AnimationStartTime; } set { m_AnimationStartTime = value; } }
        public TileAnimationFlags flags { get { return m_Flags; } set { m_Flags = value; } }

        private IntPtr m_AnimatedSpritesEntityIdPtr;
        private int m_Count;
        private float m_AnimationSpeed;
        private float m_AnimationStartTime;
        private TileAnimationFlags m_Flags;

        internal void CopyFrom(TileAnimationData other)
        {
            m_AnimatedSpritesEntityIdPtr = IntPtr.Zero;
            m_Count = 0;
            if (other.animatedSprites != null && other.animatedSprites.Length > 0)
            {
                var spriteArray = new NativeArray<EntityId>(other.animatedSprites.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                for (int i = 0; i < other.animatedSprites.Length; ++i)
                {
                    var sprite = other.animatedSprites[i];
                    spriteArray[i] = sprite != null ? sprite.GetEntityId() : EntityId.None;
                }
                animatedSpritesEntityIds = spriteArray;
                m_Count = other.animatedSprites.Length;
            }
            m_AnimationSpeed = other.animationSpeed;
            m_AnimationStartTime = other.animationStartTime;
            m_Flags = other.flags;
        }
    };

    [RequireComponent(typeof(Tilemap))]
    [NativeType(Header = "Modules/Tilemap/Public/TilemapCollider2D.h")]
    public sealed partial class TilemapCollider2D : Collider2D
    {
        // Get/Set Delaunay mesh usage.
        extern public bool useDelaunayMesh { get; set; }

        public extern uint maximumTileChangeCount
        {
            get;
            set;
        }

        public extern float extrusionFactor
        {
            get;
            set;
        }

        public extern bool hasTilemapChanges
        {
            [NativeMethod("HasTilemapChanges")]
            get;
        }

        [NativeMethod(Name = "ProcessTileChangeQueue")]
        public extern void ProcessTilemapChanges();
    }
}
