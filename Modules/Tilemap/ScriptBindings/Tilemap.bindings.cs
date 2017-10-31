// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;

namespace UnityEngine.Tilemaps
{
    [Flags]
    public enum TileFlags
    {
        None = 0,
        LockColor = 1 << 0,
        LockTransform = 1 << 1,
        InstantiateGameObjectRuntimeOnly = 1 << 2,
        LockAll = LockColor | LockTransform,
    }

    [RequireComponent(typeof(Transform))]
    [NativeHeader("Modules/Grid/Public/GridMarshalling.h")]
    [NativeHeader("Modules/Grid/Public/Grid.h")]
    [NativeHeader("Runtime/Graphics/SpriteFrame.h")]
    [NativeHeader("Modules/Tilemap/Public/TilemapTile.h")]
    [NativeHeader("Modules/Tilemap/Public/TilemapMarshalling.h")]
    [NativeType(Header = "Modules/Tilemap/Public/Tilemap.h")]
    public sealed partial class Tilemap : GridLayout
    {
        public enum Orientation
        {
            XY = 0,
            XZ = 1,
            YX = 2,
            YZ = 3,
            ZX = 4,
            ZY = 5,
            Custom = 6,
        }

        public extern Grid layoutGrid
        {
            [NativeMethod(Name = "GetAttachedGrid")]
            get;
        }

        public Vector3 GetCellCenterLocal(Vector3Int position) { return CellToLocalInterpolated(position + tileAnchor); }
        public Vector3 GetCellCenterWorld(Vector3Int position) { return LocalToWorld(CellToLocalInterpolated(position + tileAnchor)); }

        public BoundsInt cellBounds
        {
            get
            {
                return new BoundsInt(origin, size);
            }
        }

        [NativeProperty("TilemapBoundsScripting")]
        public extern Bounds localBounds
        {
            get;
        }

        public extern float animationFrameRate
        {
            get;
            set;
        }

        public extern Color color
        {
            get;
            set;
        }
        public extern Vector3Int origin
        {
            get;
            set;
        }

        public extern Vector3Int size
        {
            get;
            set;
        }

        [NativeProperty(Name = "TileAnchorScripting")]
        public extern Vector3 tileAnchor
        {
            get;
            set;
        }

        public extern Orientation orientation
        {
            get;
            set;
        }

        public extern Matrix4x4 orientationMatrix
        {
            [NativeMethod(Name = "GetTileOrientationMatrix")]
            get;
            [NativeMethod(Name = "SetOrientationMatrix")]
            set;
        }

        internal extern Object GetTileAsset(Vector3Int position);
        public TileBase GetTile(Vector3Int position) { return (TileBase)GetTileAsset(position); }
        public T GetTile<T>(Vector3Int position) where T : TileBase { return GetTileAsset(position) as T; }

        internal extern Object[] GetTileAssetsBlock(Vector3Int position, Vector3Int blockDimensions);

        public TileBase[] GetTilesBlock(BoundsInt bounds)
        {
            var array = GetTileAssetsBlock(bounds.min, bounds.size);
            var tiles = new TileBase[array.Length];
            for (int i = 0; i < array.Length; ++i)
            {
                tiles[i] = (TileBase)array[i];
            }
            return tiles;
        }

        internal extern void SetTileAsset(Vector3Int position, Object tile);
        public void SetTile(Vector3Int position, TileBase tile) { SetTileAsset(position, tile); }

        internal extern void SetTileAssets(Vector3Int[] positionArray, Object[] tileArray);
        public void SetTiles(Vector3Int[] positionArray, TileBase[] tileArray) { SetTileAssets(positionArray, tileArray); }

        [NativeMethod(Name = "SetTileAssetsBlock")]
        private extern void INTERNAL_CALL_SetTileAssetsBlock(Vector3Int position, Vector3Int blockDimensions, Object[] tileArray);
        public void SetTilesBlock(BoundsInt position, TileBase[] tileArray) { INTERNAL_CALL_SetTileAssetsBlock(position.min, position.size, tileArray); }

        public bool HasTile(Vector3Int position)
        {
            return GetTileAsset(position) != null;
        }

        [NativeMethod(Name = "RefreshTileAsset")]
        public extern void RefreshTile(Vector3Int position);
        [NativeMethod(Name = "RefreshAllTileAssets")]
        public extern void RefreshAllTiles();

        internal extern void SwapTileAsset(Object changeTile, Object newTile);
        public void SwapTile(TileBase changeTile, TileBase newTile) { SwapTileAsset(changeTile, newTile); }

        internal extern bool ContainsTileAsset(Object tileAsset);
        public bool ContainsTile(TileBase tileAsset) { return ContainsTileAsset(tileAsset); }

        public extern int GetUsedTilesCount();

        public int GetUsedTilesNonAlloc(TileBase[] usedTiles)
        {
            return Internal_GetUsedTilesNonAlloc(usedTiles);
        }

        [FreeFunction(Name = "TilemapBindings::GetUsedTilesNonAlloc", HasExplicitThis = true)]
        internal extern int Internal_GetUsedTilesNonAlloc(Object[] usedTiles);

        public extern Sprite GetSprite(Vector3Int position);

        public extern Matrix4x4 GetTransformMatrix(Vector3Int position);
        public extern void SetTransformMatrix(Vector3Int position, Matrix4x4 transform);

        [NativeMethod(Name = "GetTileColor")]
        public extern Color GetColor(Vector3Int position);

        [NativeMethod(Name = "SetTileColor")]
        public extern void SetColor(Vector3Int position, Color color);

        public extern TileFlags GetTileFlags(Vector3Int position);
        public extern void SetTileFlags(Vector3Int position, TileFlags flags);
        public extern void AddTileFlags(Vector3Int position, TileFlags flags);
        public extern void RemoveTileFlags(Vector3Int position, TileFlags flags);

        [NativeMethod(Name = "GetTileInstantiatedObject")]
        public extern GameObject GetInstantiatedObject(Vector3Int position);

        [NativeMethod(Name = "SetTileColliderType")]
        public extern void SetColliderType(Vector3Int position, Tile.ColliderType colliderType);
        [NativeMethod(Name = "GetTileColliderType")]
        public extern Tile.ColliderType GetColliderType(Vector3Int position);

        public void FloodFill(Vector3Int position, TileBase tile)
        {
            FloodFillTileAsset(position, tile);
        }

        [NativeMethod(Name = "FloodFill")]
        private extern void FloodFillTileAsset(Vector3Int position, Object tile);

        public void BoxFill(Vector3Int position, TileBase tile, int startX, int startY, int endX, int endY)
        {
            BoxFillTileAsset(position, tile, startX, startY, endX, endY);
        }

        [NativeMethod(Name = "BoxFill")]
        private extern void BoxFillTileAsset(Vector3Int position, Object tile, int startX, int startY, int endX, int endY);

        public extern void ClearAllTiles();
        public extern void ResizeBounds();
        public extern void CompressBounds();

        public extern Vector3Int editorPreviewOrigin
        {
            [NativeMethod(Name = "GetRenderOrigin")]
            get;
        }

        public extern Vector3Int editorPreviewSize
        {
            [NativeMethod(Name = "GetRenderSize")]
            get;
        }

        internal extern Object GetEditorPreviewTileAsset(Vector3Int position);
        public TileBase GetEditorPreviewTile(Vector3Int position) { return GetEditorPreviewTileAsset(position) as TileBase; }
        public T GetEditorPreviewTile<T>(Vector3Int position) where T : TileBase { return GetEditorPreviewTile(position) as T; }

        internal extern void SetEditorPreviewTileAsset(Vector3Int position, Object tile);
        public void SetEditorPreviewTile(Vector3Int position, TileBase tile) { SetEditorPreviewTileAsset(position, tile); }

        public bool HasEditorPreviewTile(Vector3Int position)
        {
            return GetEditorPreviewTileAsset(position) != null;
        }

        public extern Sprite GetEditorPreviewSprite(Vector3Int position);

        public extern Matrix4x4 GetEditorPreviewTransformMatrix(Vector3Int position);
        public extern void SetEditorPreviewTransformMatrix(Vector3Int position, Matrix4x4 transform);

        [NativeMethod(Name = "GetEditorPreviewTileColor")]
        public extern Color GetEditorPreviewColor(Vector3Int position);
        [NativeMethod(Name = "SetEditorPreviewTileColor")]
        public extern void SetEditorPreviewColor(Vector3Int position, Color color);

        public extern TileFlags GetEditorPreviewTileFlags(Vector3Int position);

        public void EditorPreviewFloodFill(Vector3Int position, TileBase tile)
        {
            EditorPreviewFloodFillTileAsset(position, tile);
        }

        [NativeMethod(Name = "EditorPreviewFloodFill")]
        private extern void EditorPreviewFloodFillTileAsset(Vector3Int position, Object tile);

        public void EditorPreviewBoxFill(Vector3Int position, Object tile, int startX, int startY, int endX, int endY)
        {
            EditorPreviewBoxFillTileAsset(position, tile, startX, startY, endX, endY);
        }

        [NativeMethod(Name = "EditorPreviewBoxFill")]
        private extern void EditorPreviewBoxFillTileAsset(Vector3Int position, Object tile, int startX, int startY, int endX, int endY);

        [NativeMethod(Name = "ClearAllEditorPreviewTileAssets")]
        public extern void ClearAllEditorPreviewTiles();
    }

    [RequireComponent(typeof(Tilemap))]
    [NativeHeader("Modules/Grid/Public/GridMarshalling.h")]
    [NativeHeader("Modules/Tilemap/TilemapRendererJobs.h")]
    [NativeType(Header = "Modules/Tilemap/Public/TilemapRenderer.h")]
    public sealed partial class TilemapRenderer : Renderer
    {
        public enum SortOrder
        {
            BottomLeft = 0,
            BottomRight = 1,
            TopLeft = 2,
            TopRight = 3,
        }

        public extern Vector3Int chunkSize
        {
            get;
            set;
        }

        public extern int maxChunkCount
        {
            get;
            set;
        }

        public extern int maxFrameAge
        {
            get;
            set;
        }

        public extern SortOrder sortOrder
        {
            get;
            set;
        }

        public extern SpriteMaskInteraction maskInteraction
        {
            get;
            set;
        }
    }

    [RequiredByNativeCode]
    [StructLayoutAttribute(LayoutKind.Sequential)]
    [NativeType(Header = "Modules/Tilemap/TilemapScripting.h")]
    public partial struct TileData
    {
        public Sprite sprite { get { return m_Sprite; } set { m_Sprite = value; } }
        public Color color { get { return m_Color; } set { m_Color = value; } }
        public Matrix4x4 transform { get { return m_Transform; } set { m_Transform = value; } }
        public GameObject gameObject { get { return m_GameObject; } set { m_GameObject = value; } }
        public TileFlags flags { get { return m_Flags; } set { m_Flags = value; } }
        public Tile.ColliderType colliderType { get { return m_ColliderType; } set { m_ColliderType = value; } }

        private Sprite m_Sprite;
        private Color m_Color;
        private Matrix4x4 m_Transform;
        private GameObject m_GameObject;
        private TileFlags m_Flags;
        private Tile.ColliderType m_ColliderType;
    }

    [RequiredByNativeCode]
    [StructLayoutAttribute(LayoutKind.Sequential)]
    [NativeType(Header = "Modules/Tilemap/TilemapScripting.h")]
    public partial struct TileAnimationData
    {
        public Sprite[] animatedSprites { get { return m_AnimatedSprites; } set { m_AnimatedSprites = value; } }
        public float animationSpeed { get { return m_AnimationSpeed; } set { m_AnimationSpeed = value; } }
        public float animationStartTime { get { return m_AnimationStartTime; } set { m_AnimationStartTime = value; } }

        private Sprite[] m_AnimatedSprites;
        private float m_AnimationSpeed;
        private float m_AnimationStartTime;
    }

    [RequireComponent(typeof(Tilemap))]
    [NativeType(Header = "Modules/Tilemap/Public/TilemapCollider2D.h")]
    public sealed partial class TilemapCollider2D : Collider2D
    {
    }
}
