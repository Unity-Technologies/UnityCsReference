// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
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

        // Explicit accessors so native can cache whether either of the two is subscribed.
        [NoAutoStaticsCleanup] // see stripping note above
        private static Action<Tilemap, SyncTile[]> s_TilemapTileChanged;
        [NoAutoStaticsCleanup] // see stripping note above
        private static Action<Tilemap, NativeArray<Vector3Int>> s_TilemapPositionsChanged;

        // Serializes each delegate update with the native cache push, so a concurrent remove cannot
        // publish a stale false after another thread's subscribe.
        [NoAutoStaticsCleanup] // see stripping note above
        private static readonly object s_SyncTileCallbackLock = new object();

        private static void UpdateHasAnySyncTileCallback()
        {
            SetHasAnySyncTileCallback(s_TilemapTileChanged != null
                || s_TilemapPositionsChanged != null);
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
                if (value == false && m_BufferSyncTile != value
                    && (HasSyncTileCallback() || HasPositionsChangedCallback()))
                    SendAndClearSyncTileBuffer();
                m_BufferSyncTile = value;
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
