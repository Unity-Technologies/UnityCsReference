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
        private EntityId m_CachedEntityId;
        public EntityId cachedEntityId => m_CachedEntityId;

        public virtual void OnEnable()
        {
            m_CachedEntityId = GetEntityId();
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

        public virtual bool GetTileAnimationData(Vector3Int position, ITilemap tilemap, ref TileAnimationData tileAnimationData) { return false; }

        [RequiredByNativeCode]
        private void GetTileAnimationDataRef(Vector3Int position, ITilemap tilemap, ref Sprite[] tileAnimationData_AnimatedSprites, ref float tileAnimationData_AnimationSpeed, ref float tileAnimationData_AnimationStartTime, ref int tileAnimationData_Flags, ref bool hasAnimation)
        {
            TileAnimationData tileAnimationData = new TileAnimationData
            {
                animatedSprites = tileAnimationData_AnimatedSprites,
                animationSpeed = tileAnimationData_AnimationSpeed,
                animationStartTime = tileAnimationData_AnimationStartTime,
                flags = (TileAnimationFlags)tileAnimationData_Flags
            };

            hasAnimation = GetTileAnimationData(position, tilemap, ref tileAnimationData);

            tileAnimationData_AnimatedSprites = tileAnimationData.animatedSprites;
            tileAnimationData_AnimationSpeed = tileAnimationData.animationSpeed;
            tileAnimationData_AnimationStartTime = tileAnimationData.animationStartTime;
            tileAnimationData_Flags = (int)tileAnimationData.flags;
        }

        [RequiredByNativeCode]
        public virtual bool StartUp(Vector3Int position, ITilemap tilemap, GameObject go) { return false; }

        [RequiredByNativeCode]
        private void StartUpRef(Vector3Int position, ITilemap tilemap, GameObject go, ref bool startUpInvokedByUser) { startUpInvokedByUser = StartUp(position, tilemap, go); }

    }
}
