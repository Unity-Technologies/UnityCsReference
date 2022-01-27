// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Scripting;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.Tilemaps
{
    [RequiredByNativeCode]
    public class ITilemap
    {
        internal static ITilemap s_Instance;

        internal Tilemap m_Tilemap;
        internal bool m_AddToList;
        internal int m_RefreshCount;
        internal NativeArray<Vector3Int> m_RefreshPos;

        internal ITilemap() { }

        internal void SetTilemapInstance(Tilemap tilemap)
        {
            m_Tilemap = tilemap;
        }

        // Tilemap
        public Vector3Int origin { get { return m_Tilemap.origin; } }
        public Vector3Int size { get { return m_Tilemap.size; } }
        public Bounds localBounds { get { return m_Tilemap.localBounds; } }
        public BoundsInt cellBounds { get { return m_Tilemap.cellBounds; } }

        // Tile
        public virtual Sprite GetSprite(Vector3Int position)
        {
            return m_Tilemap.GetSprite(position);
        }

        public virtual Color GetColor(Vector3Int position)
        {
            return m_Tilemap.GetColor(position);
        }

        public virtual Matrix4x4 GetTransformMatrix(Vector3Int position)
        {
            return m_Tilemap.GetTransformMatrix(position);
        }

        public virtual TileFlags GetTileFlags(Vector3Int position)
        {
            return m_Tilemap.GetTileFlags(position);
        }

        // Tile Assets
        public virtual TileBase GetTile(Vector3Int position)
        {
            return m_Tilemap.GetTile(position);
        }

        public virtual T GetTile<T>(Vector3Int position) where T : TileBase
        {
            return m_Tilemap.GetTile<T>(position);
        }

        public void RefreshTile(Vector3Int position)
        {
            if (m_AddToList)
            {
                if (m_RefreshCount >= m_RefreshPos.Length)
                {
                    var refreshPos = new NativeArray<Vector3Int>(Math.Max(1, m_RefreshCount * 2), Allocator.Temp);
                    NativeArray<Vector3Int>.Copy(m_RefreshPos, refreshPos, m_RefreshPos.Length);
                    m_RefreshPos.Dispose();
                    m_RefreshPos = refreshPos;
                }
                m_RefreshPos[m_RefreshCount++] = position;
            }
            else
                m_Tilemap.RefreshTile(position);
        }

        public T GetComponent<T>()
        {
            return m_Tilemap.GetComponent<T>();
        }

        [RequiredByNativeCode]
        private static ITilemap CreateInstance()
        {
            s_Instance = new ITilemap();
            return s_Instance;
        }

        [RequiredByNativeCode]
        private static unsafe void FindAllRefreshPositions(ITilemap tilemap, int count, IntPtr oldTilesIntPtr, IntPtr newTilesIntPtr, IntPtr positionsIntPtr)
        {
            tilemap.m_AddToList = true;
            if (tilemap.m_RefreshPos == null || !tilemap.m_RefreshPos.IsCreated || tilemap.m_RefreshPos.Length < count)
                tilemap.m_RefreshPos = new NativeArray<Vector3Int>(Math.Max(16, count), Allocator.Temp);
            tilemap.m_RefreshCount = 0;

            var oldTilesPtr = oldTilesIntPtr.ToPointer();
            var newTilesPtr = newTilesIntPtr.ToPointer();
            var positionsPtr = positionsIntPtr.ToPointer();

            var oldTilesIds = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<int>(oldTilesPtr, count, Allocator.Invalid);
            var newTilesIds = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<int>(newTilesPtr, count, Allocator.Invalid);
            var positions = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<Vector3Int>(positionsPtr, count, Allocator.Invalid);
            var ash1 = AtomicSafetyHandle.Create();
            var ash2 = AtomicSafetyHandle.Create();
            var ash3 = AtomicSafetyHandle.Create();
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle<int>(ref oldTilesIds, ash1);
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle<int>(ref newTilesIds, ash2);
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle<Vector3Int>(ref positions, ash3);

            for (int i = 0; i < count; ++i)
            {
                var oldTileId = oldTilesIds[i];
                var newTileId = newTilesIds[i];
                var position = positions[i];
                if (oldTileId != 0)
                {
                    var tile = (TileBase)Object.ForceLoadFromInstanceID(oldTileId);
                    tile.RefreshTile(position, tilemap);
                }
                if (newTileId != 0)
                {
                    var tile = (TileBase)Object.ForceLoadFromInstanceID(newTileId);
                    tile.RefreshTile(position, tilemap);
                }
            }

            tilemap.m_Tilemap.RefreshTilesNative(tilemap.m_RefreshPos.m_Buffer, tilemap.m_RefreshCount);
            tilemap.m_RefreshPos.Dispose();
            tilemap.m_AddToList = false;

            AtomicSafetyHandle.Release(ash1);
            AtomicSafetyHandle.Release(ash2);
            AtomicSafetyHandle.Release(ash3);
        }

        [RequiredByNativeCode]
        private unsafe static void GetAllTileData(ITilemap tilemap, int count, IntPtr tilesIntPtr, IntPtr positionsIntPtr, IntPtr outTileDataIntPtr)
        {
            void* tilesPtr = tilesIntPtr.ToPointer();
            void* positionsPtr = positionsIntPtr.ToPointer();
            void* outTileDataPtr = outTileDataIntPtr.ToPointer();

            var tiles = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<int>(tilesPtr, count, Allocator.Invalid);
            var positions = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<Vector3Int>(positionsPtr, count, Allocator.Invalid);
            var tileDataArray = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<TileData>(outTileDataPtr, count, Allocator.Invalid);

            var ash1 = AtomicSafetyHandle.Create();
            var ash2 = AtomicSafetyHandle.Create();
            var ash3 = AtomicSafetyHandle.Create();
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref tiles, ash1);
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref positions, ash2);
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref tileDataArray, ash3);

            for (int i = 0; i < count; ++i)
            {
                TileData tileData = TileData.Default;
                var tileId = tiles[i];
                if (tileId != 0)
                {
                    TileBase tile = (TileBase)Object.ForceLoadFromInstanceID(tileId);
                    tile.GetTileData(positions[i], tilemap, ref tileData);
                }
                tileDataArray[i] = tileData;
            }

            AtomicSafetyHandle.Release(ash1);
            AtomicSafetyHandle.Release(ash2);
            AtomicSafetyHandle.Release(ash3);
        }
    }
}
