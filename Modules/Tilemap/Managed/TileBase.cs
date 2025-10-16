// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Scripting;

namespace UnityEngine.Tilemaps
{
    [RequiredByNativeCode]
    public abstract class TileBase : ScriptableObject
    {
        private EntityId m_EntityId;
        public EntityId cachedEntityId => m_EntityId;

        public virtual void OnEnable()
        {
            m_EntityId = GetEntityId();
        }
        public virtual void OnDisable() { }

        [RequiredByNativeCode]
        public virtual void RefreshTile(Vector3Int position, ITilemap tilemap) { tilemap.RefreshTile(position); }

        [RequiredByNativeCode]
        public virtual void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData) {}
        private TileData GetTileDataNoRef(Vector3Int position, ITilemap tilemap)
        {
            TileData tileData = new TileData();
            GetTileData(position, tilemap, ref tileData);
            return tileData;
        }

        [RequiredByNativeCode]
        public virtual bool GetTileAnimationData(Vector3Int position, ITilemap tilemap, ref TileAnimationData tileAnimationData) { return false; }

        [RequiredByNativeCode]
        private void GetTileAnimationDataRef(Vector3Int position, ITilemap tilemap, ref TileAnimationData tileAnimationData, ref bool hasAnimation)
        {
            hasAnimation = GetTileAnimationData(position, tilemap, ref tileAnimationData);
        }

        [RequiredByNativeCode]
        public virtual bool StartUp(Vector3Int position, ITilemap tilemap, GameObject go) { return false; }

        [RequiredByNativeCode]
        private void StartUpRef(Vector3Int position, ITilemap tilemap, GameObject go, ref bool startUpInvokedByUser) { startUpInvokedByUser = StartUp(position, tilemap, go); }

    }
}
