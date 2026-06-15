// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace UnityEngine.Tilemaps
{
    [RequiredByNativeCode]
    public class ITilemap
    {
        internal static ITilemap s_Instance;

        internal Tilemap m_Tilemap;
        internal bool m_AddToList;
        internal bool m_NeedSort;
        internal int m_RefreshCount;
        internal NativeArray<Vector3Int> m_RefreshPos;

        internal ITilemap() { }

        public ITilemap(Tilemap tilemap)
        {
            if (tilemap == null)
                throw new ArgumentNullException("Argument tilemap cannot be null");
            m_Tilemap = tilemap;
        }

        public static implicit operator ITilemap(Tilemap tilemap)
        {
            return CreateInstanceFromTilemap(tilemap);
        }

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

        public virtual EntityId GetTileEntityId(Vector3Int position)
        {
            return m_Tilemap.GetTileEntityId(position);
        }

        internal virtual void RefreshTileList(Vector3Int position)
        {
            if (m_RefreshCount >= m_RefreshPos.Length)
            {
                var refreshPos = new NativeArray<Vector3Int>(Math.Max(1, m_RefreshCount << 1), Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                NativeArray<Vector3Int>.Copy(m_RefreshPos, refreshPos, m_RefreshPos.Length);
                m_RefreshPos.Dispose();
                m_RefreshPos = refreshPos;
            }
            m_RefreshPos[m_RefreshCount++] = position;
        }

        internal virtual void RefreshTileList(NativeArray<Vector3Int> positionArray)
        {
            var newLength = positionArray.Length;
            if (m_RefreshCount + newLength >= m_RefreshPos.Length)
            {
                var refreshPos = new NativeArray<Vector3Int>(Math.Max(1, (m_RefreshCount + newLength) << 1), Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                NativeArray<Vector3Int>.Copy(m_RefreshPos, refreshPos, m_RefreshPos.Length);
                m_RefreshPos.Dispose();
                m_RefreshPos = refreshPos;
            }
            NativeArray<Vector3Int>.Copy(positionArray, 0, m_RefreshPos, m_RefreshCount, newLength);
            m_RefreshCount += newLength;
        }

        public void RefreshTile(Vector3Int position)
        {
            if (m_AddToList)
            {
                RefreshTileList(position);
            }
            else
                m_Tilemap.RefreshTile(position);
        }

        public void RefreshTiles(NativeArray<Vector3Int> positionArray)
        {
            if (m_AddToList)
            {
                RefreshTileList(positionArray);
            }
            else
            {
                foreach (var position in positionArray)
                    m_Tilemap.RefreshTile(position);
            }
        }

        public T GetComponent<T>()
        {
            if (typeof(T) == typeof(Tilemap))
            {
                return (T)(System.Object)m_Tilemap;
            }
            return m_Tilemap.GetComponent<T>();
        }

        private static Func<Tilemap, ITilemap> createITilemap;
        private static int createITilemapPriority = 0;

        internal static int createPriority => createITilemapPriority;

        [RequiredByNativeCode]
        internal static void RegisterCreateITilemapFunc(Func<Tilemap, ITilemap> func, int priority)
        {
            if (priority <= createITilemapPriority)
                return;

            createITilemap = func;
            createITilemapPriority = priority;
        }

        [RequiredByNativeCode]
        internal static ITilemap CreateInstanceFromTilemap(Tilemap tilemap)
        {
            if (tilemap.iTilemap == null)
            {
                ITilemap instance = null;
                if (createITilemap == null)
                {
                    instance = new ITilemap(tilemap);
                }
                else
                {
                    instance = createITilemap(tilemap);
                }
                tilemap.iTilemap = instance;
            }
            return tilemap.iTilemap;
        }

        [RequiredByNativeCode]
        private static ITilemap GetInstanceFromTilemap(Tilemap tilemap)
        {
            return tilemap.iTilemap;
        }

        internal virtual void HandleRefreshPositions(int count
            , NativeArray<EntityId> usedTileIds
            , NativeArray<EntityId> oldTilesIds
            , NativeArray<EntityId> newTilesIds
            , NativeArray<Vector3Int> positions)
        {
            if (m_RefreshPos == null || !m_RefreshPos.IsCreated || m_RefreshPos.Length < count)
                m_RefreshPos = new NativeArray<Vector3Int>(Math.Max(16, count), Allocator.Temp);
            m_RefreshCount = 0;
            m_NeedSort = true;

            for (int i = 0; i < count; i++)
            {
                var oldTileId = oldTilesIds[i];
                var newTileId = newTilesIds[i];
                var position = positions[i];
                if (oldTileId != EntityId.None)
                {
                    var tile = (TileBase) Resources.EntityIdToObject( oldTileId);
                    tile.RefreshTile(position, this);
                }
                if (newTileId != EntityId.None)
                {
                    var tile = (TileBase) Resources.EntityIdToObject(newTileId);
                    tile.RefreshTile(position, this);
                }
            }
        }

        [RequiredByNativeCode]
        private static unsafe void FindAllRefreshPositions(ITilemap tilemap
            , int usedTileCount
            , IntPtr usedTilesIntPtr
            , int count
            , IntPtr oldTilesIntPtr
            , IntPtr newTilesIntPtr
            , IntPtr positionsIntPtr)
        {
            tilemap.m_AddToList = true;

            var usedTilesPtr = usedTilesIntPtr.ToPointer();
            var oldTilesPtr = oldTilesIntPtr.ToPointer();
            var newTilesPtr = newTilesIntPtr.ToPointer();
            var positionsPtr = positionsIntPtr.ToPointer();

            var usedTileIds = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<EntityId>(usedTilesPtr, usedTileCount, Allocator.Invalid);
            var oldTilesIds = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<EntityId>(oldTilesPtr, count, Allocator.Invalid);
            var newTilesIds = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<EntityId>(newTilesPtr, count, Allocator.Invalid);
            var positions = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<Vector3Int>(positionsPtr, count, Allocator.Invalid);

            var ash = AtomicSafetyHandle.Create();
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref usedTileIds, ash);
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref oldTilesIds, ash);
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref newTilesIds, ash);
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref positions, ash);
            tilemap.m_NeedSort = false;

            tilemap.HandleRefreshPositions(count, usedTileIds, oldTilesIds, newTilesIds, positions);

            tilemap.m_Tilemap.RefreshTilesNative(tilemap.m_RefreshPos.m_Buffer, tilemap.m_RefreshCount, tilemap.m_NeedSort);
            tilemap.m_RefreshPos.Dispose();
            tilemap.m_AddToList = false;
            tilemap.m_NeedSort = true;

            AtomicSafetyHandle.Release(ash);
        }

        internal virtual bool PreGetAllTileData()
        {
            return false;
        }

        [RequiredByNativeCode]
        private unsafe static void HandleAllTilesOnEnable(ITilemap tilemap
            , int usedTileCount
            , IntPtr usedTilesIntPtr)
        {
            void* usedTilesPtr = usedTilesIntPtr.ToPointer();
            var usedTileIds = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<EntityId>(usedTilesPtr, usedTileCount, Allocator.Invalid);

            var ash = AtomicSafetyHandle.Create();
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref usedTileIds, ash);

            for (int i = 0; i < usedTileCount; i++)
            {
                var tileId = usedTileIds[i];
                if (tileId != EntityId.None)
                {
                    var tile = Resources.EntityIdToObject(tileId) as TileBase;
                    if (tile != null)
                    {
                        tile.OnDisable();
                        tile.OnEnable();
                    }
                }
            }

            AtomicSafetyHandle.Release(ash);
        }

        internal virtual unsafe JobHandle HandleGetAllTileData(int usedTileCount
            , NativeArray<EntityId> usedTilesIds
            , int count
            , NativeArray<EntityId> tileIds
            , NativeArray<Vector3Int> positions
            , NativeArray<TileData> tileDataArray)
        {
            for (int i = 0; i < count; i++)
            {
                var tileId = tileIds[i];
                var position = positions[i];
                if (tileId != EntityId.None)
                {
                    ref var tileData = ref UnsafeUtility.ArrayElementAsRef<TileData>(tileDataArray.GetUnsafePtr(), i);
                    tileData = TileData.Default;
                    var tile = Resources.EntityIdToObject(tileId) as TileBase;
                    tile.GetTileData(position, this, ref tileData);
                }
            }
            return default(JobHandle);
        }

        [RequiredByNativeCode]
        private unsafe static void GetAllTileData(ITilemap tilemap
            , int usedTileCount
            , IntPtr usedTilesIntPtr
            , int count
            , IntPtr tilesIntPtr
            , IntPtr positionsIntPtr
            , IntPtr outTileDataIntPtr
            , out JobHandle jobHandle)
        {
            void* usedTilesPtr = usedTilesIntPtr.ToPointer();
            void* tilesPtr = tilesIntPtr.ToPointer();
            void* positionsPtr = positionsIntPtr.ToPointer();
            void* outTileDataPtr = outTileDataIntPtr.ToPointer();

            var usedTileIds = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<EntityId>(usedTilesPtr, usedTileCount, Allocator.Invalid);
            var tileIds = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<EntityId>(tilesPtr, count, Allocator.Invalid);
            var positions = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<Vector3Int>(positionsPtr, count, Allocator.Invalid);
            var tileDataArray = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<TileData>(outTileDataPtr, count, Allocator.Invalid);

            var ash = AtomicSafetyHandle.Create();
            var ash2 = AtomicSafetyHandle.Create();
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref usedTileIds, ash);
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref tileIds, ash);
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref positions, ash);
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref tileDataArray, ash2);

            jobHandle = tilemap.HandleGetAllTileData(usedTileCount, usedTileIds, count, tileIds, positions, tileDataArray);

            AtomicSafetyHandle.Release(ash);
            AtomicSafetyHandle.Release(ash2);
        }

        internal virtual unsafe JobHandle HandleGetAllTileAnimation(int usedTileCount
            , NativeArray<EntityId> usedTilesIds
            , NativeArray<bool> usedTileHasAnimation
            , int count
            , NativeArray<EntityId> tileIds
            , NativeArray<Vector3Int> positions
            , NativeArray<TileAnimationEntityIdData> tileAnimationDataArray)
        {
            for (int i = 0; i < count; i++)
            {
                var tileId = tileIds[i];
                if (tileId != EntityId.None)
                {
                    for (int j = 0; j < usedTileCount; j++)
                    {
                        if (usedTilesIds[j] == tileId)
                        {
                            if (usedTileHasAnimation[j])
                            {
                                var position = positions[i];
                                var tile = Resources.EntityIdToObject(tileId) as TileBase;
                                TileAnimationData tileAnimationData = default;
                                tile.GetTileAnimationData(positions[i], this, ref tileAnimationData);
                                ref var tileAnimationEntityIdData = ref UnsafeUtility.ArrayElementAsRef<TileAnimationEntityIdData>(tileAnimationDataArray.GetUnsafePtr(), i);
                                tileAnimationEntityIdData.CopyFrom(tileAnimationData);
                            }
                            break;
                        }
                    }
                }
            }
            return default(JobHandle);
        }

        [RequiredByNativeCode]
        private unsafe static void GetAllTileAnimationData(ITilemap tilemap
            , int usedTileCount
            , IntPtr usedTilesIntPtr
            , IntPtr usedTileHasAnimationIntPtr
            , int count
            , IntPtr tilesIntPtr
            , IntPtr positionsIntPtr
            , IntPtr outTileAnimationDataIntPtr
            , out JobHandle jobHandle)
        {
            void* usedTilesPtr = usedTilesIntPtr.ToPointer();
            void* usedTileHasAnimationPtr = usedTileHasAnimationIntPtr.ToPointer();
            void* tilesPtr = tilesIntPtr.ToPointer();
            void* positionsPtr = positionsIntPtr.ToPointer();
            void* outTileAnimationDataPtr = outTileAnimationDataIntPtr.ToPointer();

            var usedTileIds = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<EntityId>(usedTilesPtr, usedTileCount, Allocator.Invalid);
            var usedTileHasAnimationArray = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<bool>(usedTileHasAnimationPtr, usedTileCount, Allocator.Invalid);
            var tileIds = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<EntityId>(tilesPtr, count, Allocator.Invalid);
            var positions = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<Vector3Int>(positionsPtr, count, Allocator.Invalid);
            var tileAnimationDataArray = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<TileAnimationEntityIdData>(outTileAnimationDataPtr, count, Allocator.Invalid);

            var ash1 = AtomicSafetyHandle.Create();
            var ash2 = AtomicSafetyHandle.Create();
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref usedTileIds, ash1);
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref usedTileHasAnimationArray, ash1);
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref tileIds, ash1);
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref positions, ash1);
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref tileAnimationDataArray, ash2);

            jobHandle = tilemap.HandleGetAllTileAnimation(usedTileCount, usedTileIds, usedTileHasAnimationArray, count, tileIds, positions, tileAnimationDataArray);

            AtomicSafetyHandle.Release(ash1);
            AtomicSafetyHandle.Release(ash2);
        }

        internal virtual unsafe JobHandle HandleAllTileStartUp(int usedTileCount
            , NativeArray<EntityId> usedTilesIds
            , NativeArray<bool> usedTileHasStartUp
            , int count
            , NativeArray<EntityId> tileIds
            , NativeArray<EntityId> tileGameObjectIds
            , NativeArray<Vector3Int> positions)
        {
            GameObject go = null;
            Vector3Int* positionsPtrCast = (Vector3Int*)positions.GetUnsafePtr();
            EntityId* tileIdPtrCast = (EntityId*)tileIds.GetUnsafePtr();
            EntityId* goIdPtrCast = (EntityId*)tileGameObjectIds.GetUnsafePtr();
            for (int i = 0; i < count; i++)
            {
                var tileId = *(tileIdPtrCast + i);
                if (tileId != EntityId.None)
                {
                    for (int j = 0; j < usedTileCount; j++)
                    {
                        if (usedTilesIds[j] == tileId)
                        {
                            if (usedTileHasStartUp[j])
                            {
                                var position = *(positionsPtrCast + i);
                                var goId = *(goIdPtrCast + i);
                                go = null;
                                if (goId != EntityId.None)
                                    go = Resources.EntityIdToObject(goId) as GameObject;
                                var tile = Resources.EntityIdToObject(tileId) as TileBase;
                                tile.StartUp(position, this, go);
                            }
                            break;
                        }
                    }
                }
            };
            return default(JobHandle);
        }

        [RequiredByNativeCode]
        private unsafe static void DoAllTileStartUp(ITilemap tilemap
            , int usedTileCount
            , IntPtr usedTilesIntPtr
            , IntPtr usedTileHasStartUpIntPtr
            , int count
            , IntPtr tilesIntPtr
            , IntPtr gameObjectsIntPtr
            , IntPtr positionsIntPtr)
        {
            void* usedTilesPtr = usedTilesIntPtr.ToPointer();
            void* usedTileHasStartUpPtr = usedTileHasStartUpIntPtr.ToPointer();
            void* tilesPtr = tilesIntPtr.ToPointer();
            void* gameObjectsPtr = gameObjectsIntPtr.ToPointer();
            void* positionsPtr = positionsIntPtr.ToPointer();

            var usedTileIds = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<EntityId>(usedTilesPtr, usedTileCount, Allocator.Invalid);
            var usedTileHasStartUpArray = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<bool>(usedTileHasStartUpPtr, usedTileCount, Allocator.Invalid);
            var tiles = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<EntityId>(tilesPtr, count, Allocator.Invalid);
            var tileGameObjects = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<EntityId>(gameObjectsPtr, count, Allocator.Invalid);
            var positions = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<Vector3Int>(positionsPtr, count, Allocator.Invalid);

            var ash = AtomicSafetyHandle.Create();
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref usedTileIds, ash);
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref usedTileHasStartUpArray, ash);
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref tiles, ash);
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref tileGameObjects, ash);
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref positions, ash);

            var jobHandle = tilemap.HandleAllTileStartUp(usedTileCount, usedTileIds, usedTileHasStartUpArray, count, tiles, tileGameObjects, positions);
            jobHandle.Complete();

            AtomicSafetyHandle.Release(ash);
        }
    }
}
