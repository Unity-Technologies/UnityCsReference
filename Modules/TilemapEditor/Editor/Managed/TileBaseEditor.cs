// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Tilemaps;

namespace UnityEditor
{
    [CustomEditor(typeof(TileBase), true)]
    [CanEditMultipleObjects]
    public class TileBaseEditor : Editor
    {
        private TileBase tile
        {
            get { return (target as TileBase); }
        }

        public override Texture2D RenderStaticPreview(string assetPath, Object[] subAssets, int width, int height)
        {
            Texture2D preview = null;
            GameObject tilemapGameObject = null;
            try
            {
                TileData tileData = default;
                tilemapGameObject = new GameObject("Preview", typeof(Grid), typeof(Tilemap), typeof(TilemapRenderer));
                var tilemap = tilemapGameObject.GetComponent<Tilemap>();
                tile.GetTileData(Vector3Int.zero, tilemap, ref tileData);
                preview = SpriteUtility.RenderStaticPreview(tileData.sprite, tileData.color, width, height, tileData.transform);
            }
            finally
            {
                if (tilemapGameObject != null)
                    DestroyImmediate(tilemapGameObject);
            }
            return preview;
        }
    }
}
