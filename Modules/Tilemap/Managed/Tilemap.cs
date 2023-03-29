// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.Tilemaps
{
    public partial class Tilemap
    {
        public static event Action<Tilemap, SyncTile[]> tilemapTileChanged;

        public static event Action<Tilemap, NativeArray<Vector3Int>> tilemapPositionsChanged;

        public static event Action<Tilemap, NativeArray<Vector3Int>> loopEndedForTileAnimation;

        private bool m_BufferSyncTile;
        internal bool bufferSyncTile
        {
            get { return m_BufferSyncTile; }
            set
            {
                if (value == false && m_BufferSyncTile != value && HasSyncTileCallback())
                    SendAndClearSyncTileBuffer();
                m_BufferSyncTile = value;
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
            return (Tilemap.tilemapTileChanged != null);
        }

        internal static bool HasPositionsChangedCallback()
        {
            return (Tilemap.tilemapPositionsChanged != null);
        }

        private void HandleSyncTileCallback(SyncTile[] syncTiles)
        {
            if (Tilemap.tilemapTileChanged == null)
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
                Tilemap.tilemapTileChanged(this, syncTiles);
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
                Tilemap.tilemapPositionsChanged(this, positions);
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
