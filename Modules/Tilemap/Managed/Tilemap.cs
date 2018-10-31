// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.Tilemaps
{
    public partial class Tilemap
    {
        internal static event Action<Tilemap, SyncTile[]> tilemapTileChanged;

        private Dictionary<Vector3Int, SyncTile> m_SyncTileBuffer;
        private Dictionary<Vector3Int, SyncTile> syncTileBuffer
        {
            get
            {
                if (m_SyncTileBuffer == null)
                    m_SyncTileBuffer = new Dictionary<Vector3Int, SyncTile>();
                return m_SyncTileBuffer;
            }
        }

        private bool m_BufferSyncTile;
        internal bool bufferSyncTile
        {
            get { return m_BufferSyncTile; }
            set
            {
                if (value == false && m_BufferSyncTile != value && HasSyncTileCallback() && syncTileBuffer.Count > 0)
                    SendTilemapTileChangedCallback(syncTileBuffer.Values.ToArray());
                syncTileBuffer.Clear();
                m_BufferSyncTile = value;
            }
        }

        private void HandleSyncTileCallback(SyncTile[] syncTiles)
        {
            if (bufferSyncTile)
            {
                foreach (var syncTile in syncTiles)
                {
                    syncTileBuffer[syncTile.m_Position] = syncTile;
                }
                return;
            }
            SendTilemapTileChangedCallback(syncTiles);
        }

        private void SendTilemapTileChangedCallback(SyncTile[] syncTiles)
        {
            Tilemap.tilemapTileChanged(this, syncTiles);
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
