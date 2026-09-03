// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine.Scripting;
using Unity.Scripting.LifecycleManagement;

namespace UnityEngine.Tilemaps
{
    ///<summary>Class passed onto Tiles when information is queried from the Tiles.</summary>
    ///<remarks>This handles editor preview [tiles](xref:Tilemap-ScriptableTiles-TileBase) when painting on a <see cref="Tilemap" /> in Editor mode.</remarks>
    [RequiredByNativeCode]
    public class ITilemap
    {
        // NOTE: s_Instance, createITilemap and createITilemapPriority should ideally be reset on code reload
        // (createITilemap can point into reloadable editor code), but AutoStaticsCleanup codegen in this
        // player-shipped module forces the Tilemap module into stripped player builds
        // (TestStrippingDependencies). Persist as before until lifecycle registration is strip-safe.
        [NoAutoStaticsCleanup] // see stripping note above
        internal static ITilemap s_Instance;

        internal Tilemap m_Tilemap;
        internal bool m_AddToList;
        internal bool m_NeedSort;
        internal int m_RefreshCount;
        internal NativeArray<Vector3Int> m_RefreshPos;

        internal ITilemap() { }

        ///<summary>Initializes and returns an instance of <see cref="ITilemap" />.</summary>
        ///<remarks>This will wrap the <see cref="Tilemap" /> parameter and is passed onto <see cref="Tile" />s when information is queried from the <see cref="Tile" />s.</remarks>
        ///<param name="tilemap">The <see cref="Tilemap" /> to wrap.</param>
        public ITilemap(Tilemap tilemap)
        {
            if (tilemap == null)
                throw new ArgumentNullException("Argument tilemap cannot be null");
            m_Tilemap = tilemap;
        }

        ///<summary>
        ///  <see cref="Tilemap" /> can be implicitly converted to <see cref="ITilemap" />.</summary>
        ///<param name="tilemap">The <see cref="Tilemap" /> to convert to <see cref="ITilemap" />.</param>
        public static implicit operator ITilemap(Tilemap tilemap)
        {
            return CreateInstanceFromTilemap(tilemap);
        }

        internal void SetTilemapInstance(Tilemap tilemap)
        {
            m_Tilemap = tilemap;
        }

        // Tilemap
        ///<summary>The origin of the Tilemap in cell position.</summary>
        ///<remarks>Refer to <see cref="Tilemap" /> for more information.</remarks>
        public Vector3Int origin { get { return m_Tilemap.origin; } }
        ///<summary>The size of the Tilemap in cells.</summary>
        ///<remarks>Refer to <see cref="Tilemap" /> for more information.</remarks>
        public Vector3Int size { get { return m_Tilemap.size; } }
        ///<summary>Returns the boundaries of the Tilemap in local space size.</summary>
        ///<remarks>Refer to <see cref="Tilemap" /> for more information.</remarks>
        public Bounds localBounds { get { return m_Tilemap.localBounds; } }
        ///<summary>Returns the boundaries of the Tilemap in cell size.</summary>
        ///<remarks>Refer to <see cref="Tilemap" /> for more information.</remarks>
        public BoundsInt cellBounds { get { return m_Tilemap.cellBounds; } }

        // Tile
        ///<summary>Gets the Sprite used in a Tile given the XYZ coordinates of a cell in the Tilemap.</summary>
        ///<remarks>Refer to [Scriptable Tiles](xref:Tilemap-ScriptableTiles-TileBase) and [Tilemap](xref:class-Tilemap) for more information.</remarks>
        ///<param name="position">Position of the Tile on the <see cref="Tilemap" />.</param>
        ///<returns>Sprite at the XYZ coordinate.</returns>
        public virtual Sprite GetSprite(Vector3Int position)
        {
            return m_Tilemap.GetSprite(position);
        }

        ///<summary>Gets the color of a Tile given the XYZ coordinates of a cell in the Tilemap.</summary>
        ///<remarks>Refer to [Scriptable Tiles](xref:Tilemap-ScriptableTiles-TileBase) and [Tilemap](xref:class-Tilemap) for more information.</remarks>
        ///<param name="position">Position of the Tile on the <see cref="Tilemap" />.</param>
        ///<returns>Color of the <see cref="Tile" /> at the XYZ coordinate.</returns>
        public virtual Color GetColor(Vector3Int position)
        {
            return m_Tilemap.GetColor(position);
        }

        ///<summary>Gets the transform matrix of a Tile given the XYZ coordinates of a cell in the Tilemap.</summary>
        ///<remarks>Refer to [Scriptable Tiles](xref:Tilemap-ScriptableTiles-TileBase) and [Tilemap](xref:class-Tilemap) for more information.</remarks>
        ///<param name="position">Position of the Tile on the <see cref="Tilemap" />.</param>
        ///<returns>The transform matrix.</returns>
        public virtual Matrix4x4 GetTransformMatrix(Vector3Int position)
        {
            return m_Tilemap.GetTransformMatrix(position);
        }

        ///<summary>Gets the Tile Flags of the Tile at the given position.</summary>
        ///<remarks>Refer to <see cref="TileFlags" /> for more information.</remarks>
        ///<param name="position">Position of the Tile on the <see cref="Tilemap" />.</param>
        ///<returns>
        ///  <see cref="TileFlags" /> from the <see cref="Tile" />.</returns>
        public virtual TileFlags GetTileFlags(Vector3Int position)
        {
            return m_Tilemap.GetTileFlags(position);
        }

        // Tile Assets
        ///<summary>Gets the <see cref="Tile" /> at the given XYZ coordinates of a cell in the <see cref="Tilemap" />.</summary>
        ///<remarks>Use this method to get the [Tile](xref:Tilemap-ScriptableTiles-TileBase) at the given XYZ coordinates of a cell in the [Tilemap](xref:class-Tilemap).</remarks>
        ///<param name="position">Position of the Tile on the <see cref="Tilemap" />.</param>
        ///<returns>
        ///  <see cref="Tile" /> placed at the cell.</returns>
        public virtual TileBase GetTile(Vector3Int position)
        {
            return m_Tilemap.GetTile(position);
        }

        ///<summary>Gets the <see cref="Tile" /> of type T at the given XYZ coordinates of a cell in the <see cref="Tilemap" />.</summary>
        ///<remarks>Use this method to get the [Tile of type T](xref:Tilemap-ScriptableTiles-TileBase) at the given XYZ coordinates of a cell in the [Tilemap](xref:class-Tilemap).</remarks>
        ///<param name="position">Position of the Tile on the <see cref="Tilemap" />.</param>
        ///<returns>
        ///  <see cref="Tile" /> of type T placed at the cell.</returns>
        public virtual T GetTile<T>(Vector3Int position) where T : TileBase
        {
            return m_Tilemap.GetTile<T>(position);
        }

        ///<summary>Gets the EntityId of the <see cref="Tile" /> at the given xyz coordinates of a cell in the Tilemap.</summary>
        ///<param name="position">The position of the <see cref="Tile" /> on the <see cref="Tilemap" />.</param>
        ///<returns>EntityId of the <see cref="Tilemaps.TileBase" /> placed at the cell.</returns>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///using UnityEngine.Tilemaps;
        ///
        ///public static class TilemapExample
        ///{
        ///    public static void GetTileEntityIdExample(ITilemap tilemap, Vector3Int position, Tile tile)
        ///    {
        ///        tilemap.GetComponent<Tilemap>().SetTile(position, tile);
        ///        var tileId = tilemap.GetTileEntityId(position);
        ///        Debug.Log($"The ids for the Tile placed are equal ({(tileId == tile.GetEntityId()).ToString()})");
        ///    }
        ///}]]></code>
        ///</example>
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

        ///<summary>Refreshes a Tile at the given XYZ coordinates of a cell in the :Tilemap.</summary>
        ///<remarks>The [Tilemap](xref:class-Tilemap) will retrieve the rendering data, animation data and other data for the [Tile](xref:Tilemap-ScriptableTiles-TileBase) and update all relevant components.</remarks>
        ///<param name="position">Position of the Tile on the <see cref="Tilemap" />.</param>
        public void RefreshTile(Vector3Int position)
        {
            if (m_AddToList)
            {
                RefreshTileList(position);
            }
            else
                m_Tilemap.RefreshTile(position);
        }

        ///<summary>Refreshes Tiles at the given xyz coordinates of cells in the <see cref="Tilemap" /> from the array.</summary>
        ///<param name="positionArray">An array of positions of Tiles on the <see cref="Tilemap" /> to refresh.</param>
        ///<example>
        ///  <code><![CDATA[using Unity.Collections;
        ///using UnityEngine;
        ///using UnityEngine.Tilemaps;
        ///
        /// // Tile that displays a Sprite when it is alone and a different Sprite when it is orthogonally adjacent to the same NeighourTile
        ///[CreateAssetMenu]
        ///public class NeighbourTile : TileBase
        ///{
        ///    public Sprite spriteA;
        ///    public Sprite spriteB;
        ///
        ///    public override void RefreshTile(Vector3Int position, ITilemap tilemap)
        ///    {
        ///        var refreshPositions = new NativeArray<Vector3Int>(5, Allocator.Temp);
        ///        var i = 0;
        ///        for (int yd = -1; yd <= 1; yd += 2)
        ///        {
        ///            Vector3Int location = new Vector3Int(position.x, position.y + yd, position.z);
        ///            refreshPositions[i++] = location;
        ///        }
        ///        for (int xd = -1; xd <= 1; xd += 2)
        ///        {
        ///            Vector3Int location = new Vector3Int(position.x + xd, position.y, position.z);
        ///            refreshPositions[i++] = location;
        ///        }
        ///        refreshPositions[i] = position;
        ///        tilemap.RefreshTiles(refreshPositions);
        ///    }
        ///
        ///    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
        ///    {
        ///        tileData.sprite = spriteA;
        ///        for (int yd = -1; yd <= 1; yd += 2)
        ///        {
        ///            Vector3Int location = new Vector3Int(position.x, position.y + yd, position.z);
        ///            if (IsNeighbour(location, tilemap))
        ///                tileData.sprite = spriteB;
        ///        }
        ///        for (int xd = -1; xd <= 1; xd += 2)
        ///        {
        ///            Vector3Int location = new Vector3Int(position.x + xd, position.y, position.z);
        ///            if (IsNeighbour(location, tilemap))
        ///                tileData.sprite = spriteB;
        ///        }
        ///    }
        ///
        ///    private bool IsNeighbour(Vector3Int position, ITilemap tilemap)
        ///    {
        ///        TileBase tile = tilemap.GetTile(position);
        ///        return (tile != null && tile == this);
        ///    }
        ///}
        ///]]></code>
        ///</example>
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

        ///<summary>Returns the component of type <c>T</c> if the GameObject of the tile map has one attached, null if it doesn't.</summary>
        ///<returns>The Component of type T to retrieve.</returns>
        public T GetComponent<T>()
        {
            if (typeof(T) == typeof(Tilemap))
            {
                return (T)(System.Object)m_Tilemap;
            }
            return m_Tilemap.GetComponent<T>();
        }

        // Holds a Func pointing into editor (reloadable) code; not cleaned on reload — see stripping note on s_Instance.
        [NoAutoStaticsCleanup]
        private static Func<Tilemap, ITilemap> createITilemap;
        // Paired priority gate for createITilemap; persists alongside it — see stripping note on s_Instance.
        [NoAutoStaticsCleanup]
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

            TileBase lastTile = null;
            EntityId lastEntityId = EntityId.None;
            // The Tile may not be resolvable, eg. it has been destroyed or unloaded while still
            // referenced by the Tilemap. Skip it, as the single refresh path does. Cached alongside
            // lastTile as the null test on an Object is not free.
            bool lastTileValid = false;

            for (int i = 0; i < count; i++)
            {
                var oldTileId = oldTilesIds[i];
                var newTileId = newTilesIds[i];
                var position = positions[i];
                if (oldTileId != EntityId.None)
                {
                    if (lastEntityId != oldTileId)
                    {
                        lastTile = Resources.EntityIdToObject(oldTileId) as TileBase;
                        lastEntityId = oldTileId;
                        lastTileValid = lastTile != null;
                    }
                    if (lastTileValid)
                        lastTile.RefreshTile(position, this);
                }
                if (newTileId != EntityId.None)
                {
                    if (lastEntityId != newTileId)
                    {
                        lastTile = Resources.EntityIdToObject(newTileId) as TileBase;
                        lastEntityId = newTileId;
                        lastTileValid = lastTile != null;
                    }
                    if (lastTileValid)
                        lastTile.RefreshTile(position, this);
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

            try
            {
                tilemap.HandleRefreshPositions(count, usedTileIds, oldTilesIds, newTilesIds, positions);

                tilemap.m_Tilemap.RefreshTilesNative(tilemap.m_RefreshPos.m_Buffer, tilemap.m_RefreshCount, tilemap.m_NeedSort);
            }
            finally
            {
                // A Tile refreshing itself can throw, so always restore state for the next refresh.
                tilemap.m_RefreshPos.Dispose();
                tilemap.m_AddToList = false;
                tilemap.m_NeedSort = true;
            }

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
            TileBase lastTile = null;
            EntityId lastEntityId = EntityId.None;
            bool lastTileValid = false;
            for (int i = 0; i < count; i++)
            {
                var tileId = tileIds[i];
                var position = positions[i];
                if (tileId != EntityId.None)
                {
                    ref var tileData = ref UnsafeUtility.ArrayElementAsRef<TileData>(tileDataArray.GetUnsafePtr(), i);
                    tileData = TileData.Default;
                    if (lastEntityId != tileId)
                    {
                        lastTile = Resources.EntityIdToObject(tileId) as TileBase;
                        lastEntityId = tileId;
                        lastTileValid = lastTile != null;
                    }
                    if (lastTileValid)
                        lastTile.GetTileData(position, this, ref tileData);
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
                                if (tile != null)
                                {
                                    TileAnimationData tileAnimationData = default;
                                    tile.GetTileAnimationData(position, this, ref tileAnimationData);
                                    ref var tileAnimationEntityIdData = ref UnsafeUtility.ArrayElementAsRef<TileAnimationEntityIdData>(tileAnimationDataArray.GetUnsafePtr(), i);
                                    tileAnimationEntityIdData.CopyFrom(tileAnimationData);
                                }
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

            TileBase lastTile = null;
            int lastTileIdx = -1;
            bool lastTileValid = false;
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
                                if (lastTileIdx != j)
                                {
                                    lastTileIdx = j;
                                    lastTile = Resources.EntityIdToObject(tileId) as TileBase;
                                    lastTileValid = lastTile != null;
                                }
                                if (lastTileValid)
                                    lastTile.StartUp(position, this, go);
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
