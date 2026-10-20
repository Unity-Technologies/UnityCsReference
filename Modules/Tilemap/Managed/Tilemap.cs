// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Scripting.LifecycleManagement;

namespace UnityEngine.Tilemaps
{
    ///<summary>The Tilemap stores <see cref="Sprite" />s in a layout marked by a <see cref="Grid" /> component.</summary>
    public partial class Tilemap
    {
        // NOTE: these public events can hold user/editor handlers, but AutoStaticsCleanup codegen in this
        // player-shipped module forces the Tilemap module into stripped player builds
        // (TestStrippingDependencies). Persist as before until lifecycle registration is strip-safe.

        // Explicit accessors so native can cache whether any of the three is subscribed.
        [NoAutoStaticsCleanup] // see stripping note above
        private static Action<Tilemap, SyncTile[]> s_TilemapTileChanged;
        [NoAutoStaticsCleanup] // see stripping note above
        private static Action<Tilemap, NativeArray<Vector3Int>> s_TilemapPositionsChanged;
        [NoAutoStaticsCleanup] // see stripping note above
        private static Action<Tilemap, NativeArray<SyncTile>, TileChangeReason> s_TilemapTilesChanged;

        // Serializes each delegate update with the native cache push, so a concurrent remove cannot
        // publish a stale false after another thread's subscribe.
        [NoAutoStaticsCleanup] // see stripping note above
        private static readonly object s_SyncTileCallbackLock = new object();

        private static void UpdateHasAnySyncTileCallback()
        {
            SetHasAnySyncTileCallback(s_TilemapTileChanged != null
                || s_TilemapPositionsChanged != null
                || s_TilemapTilesChanged != null);
        }

        ///<summary>Callback when Tiles on a Tilemap have changed.</summary>
        ///<remarks>This returns the positions on the Tilemap which have been updated and the Tile Data of each position that has been updated.</remarks>
        public static event Action<Tilemap, SyncTile[]> tilemapTileChanged
        {
            add
            {
                lock (s_SyncTileCallbackLock)
                {
                    s_TilemapTileChanged += value;
                    UpdateHasAnySyncTileCallback();
                }
            }
            remove
            {
                lock (s_SyncTileCallbackLock)
                {
                    s_TilemapTileChanged -= value;
                    UpdateHasAnySyncTileCallback();
                }
            }
        }

        ///<summary>Callback when Tiles on a Tilemap have changed.</summary>
        ///<remarks>This returns the list of positions on the Tilemap which have been updated.</remarks>
        public static event Action<Tilemap, NativeArray<Vector3Int>> tilemapPositionsChanged
        {
            add
            {
                lock (s_SyncTileCallbackLock)
                {
                    s_TilemapPositionsChanged += value;
                    UpdateHasAnySyncTileCallback();
                }
            }
            remove
            {
                lock (s_SyncTileCallbackLock)
                {
                    s_TilemapPositionsChanged -= value;
                    UpdateHasAnySyncTileCallback();
                }
            }
        }

        ///<summary>Callback when Tiles on a Tilemap have changed, without allocating.</summary>
        ///<remarks>This returns the cells which have been updated and why. The cells are only valid for the duration of the callback.</remarks>
        ///<example>
        ///  <code><![CDATA[
        /// // Log the cells which have changed, ignoring changes which cannot affect collision
        ///using Unity.Collections;
        ///using UnityEngine;
        ///using UnityEngine.Tilemaps;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void OnEnable()
        ///    {
        ///        Tilemap.tilemapTilesChanged += OnTilesChanged;
        ///    }
        ///
        ///    void OnDisable()
        ///    {
        ///        Tilemap.tilemapTilesChanged -= OnTilesChanged;
        ///    }
        ///
        ///    void OnTilesChanged(Tilemap tilemap, NativeArray<Tilemap.SyncTile> entries, TileChangeReason reason)
        ///    {
        ///        if ((reason & TileChangeReason.Physics) == 0)
        ///            return;
        ///
        ///        for (var i = 0; i < entries.Length; ++i)
        ///        {
        ///            var entry = entries[i];
        ///            Debug.Log(entry.hasTile ? $"{entry.position} has a Tile" : $"{entry.position} is empty");
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static event Action<Tilemap, NativeArray<SyncTile>, TileChangeReason> tilemapTilesChanged
        {
            add
            {
                lock (s_SyncTileCallbackLock)
                {
                    s_TilemapTilesChanged += value;
                    UpdateHasAnySyncTileCallback();
                }
            }
            remove
            {
                lock (s_SyncTileCallbackLock)
                {
                    s_TilemapTilesChanged -= value;
                    UpdateHasAnySyncTileCallback();
                }
            }
        }

        ///<summary>Callback when a Tilemap itself has changed, rather than the Tiles on it.</summary>
        ///<remarks>This returns why the Tilemap changed, for map-wide changes such as the tile anchor, orientation, cell layout, origin or size.</remarks>
        ///<example>
        ///  <code><![CDATA[
        /// // Rebuild when a Tilemap changes in a way which moves the geometry of its cells
        ///using UnityEngine;
        ///using UnityEngine.Tilemaps;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void OnEnable()
        ///    {
        ///        Tilemap.tilemapChanged += OnTilemapChanged;
        ///    }
        ///
        ///    void OnDisable()
        ///    {
        ///        Tilemap.tilemapChanged -= OnTilemapChanged;
        ///    }
        ///
        ///    void OnTilemapChanged(Tilemap tilemap, TilemapChangeReason reason)
        ///    {
        ///        if ((reason & TilemapChangeReason.Geometry) != 0)
        ///            Debug.Log($"{tilemap.name} changed: {reason}");
        ///    }
        ///}
        ///]]></code>
        ///</example>
        [NoAutoStaticsCleanup] // see stripping note above
        public static event Action<Tilemap, TilemapChangeReason> tilemapChanged;

        ///<summary>Callback when Tiles on a Tilemap have reached the end of their loop for their Tile Animation.</summary>
        ///<remarks>This returns the list of positions on the Tilemap which have ended their loop for their Tile Animation.</remarks>
        [NoAutoStaticsCleanup] // see stripping note above
        public static event Action<Tilemap, NativeArray<Vector3Int>> loopEndedForTileAnimation;

        private bool m_BufferSyncTile;
        internal bool bufferSyncTile
        {
            get { return m_BufferSyncTile; }
            set
            {
                var sendAndClear = value == false && m_BufferSyncTile != value
                    && (HasSyncTileCallback() || HasPositionsChangedCallback() || HasTilesChangedCallback());
                // Cleared before the flush so an edit made from inside the callback notifies instead of re-buffering.
                m_BufferSyncTile = value;
                if (sendAndClear)
                    SendAndClearSyncTileBuffer();
            }
        }

        private ITilemap m_ITilemap;
        internal ITilemap iTilemap
        {
            get { return m_ITilemap; }
            set
            {
                m_ITilemap = value;
            }
        }

        internal static bool HasLoopEndedForTileAnimationCallback()
        {
            return (Tilemap.loopEndedForTileAnimation != null);
        }

        private unsafe void HandleLoopEndedForTileAnimationCallback(int count, IntPtr positionsIntPtr)
        {
            if (!HasLoopEndedForTileAnimationCallback())
                return;

            void* positionsPtr = positionsIntPtr.ToPointer();
            var positions = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<Vector3Int>(positionsPtr, count, Allocator.Invalid);
            var safety = AtomicSafetyHandle.Create();
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref positions, safety);

            SendLoopEndedForTileAnimationCallback(positions);

            AtomicSafetyHandle.CheckDeallocateAndThrow(safety);
            AtomicSafetyHandle.Release(safety);
        }

        private void SendLoopEndedForTileAnimationCallback(NativeArray<Vector3Int> positions)
        {
            try
            {
                Tilemap.loopEndedForTileAnimation(this, positions);
            }
            catch (Exception e)
            {
                // Case 1215834: Log user exception/s and ensure engine code continues to run
                Debug.LogException(e, this);
            }
        }

        internal static bool HasSyncTileCallback()
        {
            return (s_TilemapTileChanged != null);
        }

        internal static bool HasPositionsChangedCallback()
        {
            return (s_TilemapPositionsChanged != null);
        }

        private void HandleSyncTileCallback(SyncTile[] syncTiles)
        {
            if (s_TilemapTileChanged == null)
                return;

            SendTilemapTileChangedCallback(syncTiles);
        }

        private unsafe void HandlePositionsChangedCallback(int count, IntPtr positionsIntPtr)
        {
            if (!HasPositionsChangedCallback())
                return;

            void* positionsPtr = positionsIntPtr.ToPointer();
            var positions = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<Vector3Int>(positionsPtr, count, Allocator.Invalid);
            var safety = AtomicSafetyHandle.Create();
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref positions, safety);

            SendTilemapPositionsChangedCallback(positions);

            AtomicSafetyHandle.CheckDeallocateAndThrow(safety);
            AtomicSafetyHandle.Release(safety);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool HasTilesChangedCallback() => (s_TilemapTilesChanged != null);

        private unsafe void HandleTilesChangedCallback(int count, IntPtr entriesIntPtr, TileChangeReason reason)
        {
            if (!HasTilesChangedCallback())
                return;

            void* entriesPtr = entriesIntPtr.ToPointer();
            var entries = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<SyncTile>(entriesPtr, count, Allocator.Invalid);
            var safety = AtomicSafetyHandle.Create();
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref entries, safety);

            SendTilemapTilesChangedCallback(entries, reason);

            AtomicSafetyHandle.CheckDeallocateAndThrow(safety);
            AtomicSafetyHandle.Release(safety);
        }

        private void SendTilemapTilesChangedCallback(NativeArray<SyncTile> entries, TileChangeReason reason)
        {
            try
            {
                s_TilemapTilesChanged(this, entries, reason);
            }
            catch (Exception e)
            {
                // Case 1215834: Log user exception/s and ensure engine code continues to run
                Debug.LogException(e, this);
            }
        }

        private void HandleTilemapChangedCallback(TilemapChangeReason reason)
        {
            if (Tilemap.tilemapChanged == null)
                return;

            try
            {
                Tilemap.tilemapChanged(this, reason);
            }
            catch (Exception e)
            {
                // Case 1215834: Log user exception/s and ensure engine code continues to run
                Debug.LogException(e, this);
            }
        }

        private void SendTilemapTileChangedCallback(SyncTile[] syncTiles)
        {
            try
            {
                s_TilemapTileChanged(this, syncTiles);
            }
            catch (Exception e)
            {
                // Case 1215834: Log user exception/s and ensure engine code continues to run
                Debug.LogException(e, this);
            }
        }

        private void SendTilemapPositionsChangedCallback(NativeArray<Vector3Int> positions)
        {
            try
            {
                s_TilemapPositionsChanged(this, positions);
            }
            catch (Exception e)
            {
                // Case 1215834: Log user exception/s and ensure engine code continues to run
                Debug.LogException(e, this);
            }
        }

        internal static void SetSyncTileCallback(Action<Tilemap, SyncTile[]> callback)
        {
            Tilemap.tilemapTileChanged += callback;
        }

        internal static void RemoveSyncTileCallback(Action<Tilemap, SyncTile[]> callback)
        {
            Tilemap.tilemapTileChanged -= callback;
        }
    }
}
