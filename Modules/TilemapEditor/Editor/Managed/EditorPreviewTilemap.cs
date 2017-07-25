// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.Tilemaps;

namespace UnityEditor
{
    [RequiredByNativeCode]
    internal class EditorPreviewTilemap : ITilemap
    {
        private EditorPreviewTilemap()
        {
        }

        // Tile
        public override Sprite GetSprite(Vector3Int position)
        {
            var tile = m_Tilemap.GetEditorPreviewTile(position);
            return tile ? m_Tilemap.GetEditorPreviewSprite(position) : m_Tilemap.GetSprite(position);
        }

        public override Color GetColor(Vector3Int position)
        {
            var tile = m_Tilemap.GetEditorPreviewTile(position);
            return tile ? m_Tilemap.GetEditorPreviewColor(position) : m_Tilemap.GetColor(position);
        }

        public override Matrix4x4 GetTransformMatrix(Vector3Int position)
        {
            var tile = m_Tilemap.GetEditorPreviewTile(position);
            return tile ? m_Tilemap.GetEditorPreviewTransformMatrix(position) : m_Tilemap.GetTransformMatrix(position);
        }

        public override TileFlags GetTileFlags(Vector3Int position)
        {
            var tile = m_Tilemap.GetEditorPreviewTile(position);
            return tile ? m_Tilemap.GetEditorPreviewTileFlags(position) : m_Tilemap.GetTileFlags(position);
        }

        // Tile Assets
        public override TileBase GetTile(Vector3Int position)
        {
            var tile = m_Tilemap.GetEditorPreviewTile(position);
            return tile ?? m_Tilemap.GetTile(position);
        }

        public override T GetTile<T>(Vector3Int position)
        {
            var tile = m_Tilemap.GetEditorPreviewTile<T>(position);
            return tile ?? m_Tilemap.GetTile<T>(position);
        }

        private TileBase CreateInvalidTile()
        {
            Texture2D tex = Texture2D.whiteTexture;
            Sprite sprite = Sprite.Create(tex, new Rect(0.0f, 0.0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width);

            Tile tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite;
            // Try to get a pinkish look with a random color to differentiate between other invalid tiles
            tile.color = UnityEngine.Random.ColorHSV(340f / 360f, 1f, 0.3f, 0.6f, 0.7f, 1.0f);
            tile.transform = Matrix4x4.identity;
            tile.flags = TileFlags.LockAll;

            return tile;
        }

        private static ITilemap CreateInstance()
        {
            s_Instance = new EditorPreviewTilemap();
            return s_Instance;
        }
    }
}
