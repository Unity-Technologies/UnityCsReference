// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Scripting;

namespace UnityEngine.Tilemaps
{
    [RequiredByNativeCode]
    public class ITilemap
    {
        internal static ITilemap s_Instance;
        internal Tilemap m_Tilemap;

        internal ITilemap()
        {
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

        public void RefreshTile(Vector3Int position)
        {
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
    }
}
