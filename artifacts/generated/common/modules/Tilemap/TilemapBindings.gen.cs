// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;

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
    
    
    public extern  Grid layoutGrid
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
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
    public  Bounds localBounds
    {
        get { Bounds tmp; INTERNAL_get_localBounds(out tmp); return tmp;  }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private  void INTERNAL_get_localBounds (out Bounds value) ;


    public extern float animationFrameRate
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public Color color
    {
        get { Color tmp; INTERNAL_get_color(out tmp); return tmp;  }
        set { INTERNAL_set_color(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_color (out Color value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_set_color (ref Color value) ;

    public Vector3Int origin
    {
        get { Vector3Int tmp; INTERNAL_get_origin(out tmp); return tmp;  }
        set { INTERNAL_set_origin(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_origin (out Vector3Int value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_set_origin (ref Vector3Int value) ;

    public Vector3Int size
    {
        get { Vector3Int tmp; INTERNAL_get_size(out tmp); return tmp;  }
        set { INTERNAL_set_size(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_size (out Vector3Int value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_set_size (ref Vector3Int value) ;

    public  Vector3 tileAnchor
    {
        get { Vector3 tmp; INTERNAL_get_tileAnchor(out tmp); return tmp;  }
        set { INTERNAL_set_tileAnchor(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private  void INTERNAL_get_tileAnchor (out Vector3 value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private  void INTERNAL_set_tileAnchor (ref Vector3 value) ;

    public extern Tilemap.Orientation orientation
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public Matrix4x4 orientationMatrix
    {
        get { Matrix4x4 tmp; INTERNAL_get_orientationMatrix(out tmp); return tmp;  }
        set { INTERNAL_set_orientationMatrix(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_orientationMatrix (out Matrix4x4 value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_set_orientationMatrix (ref Matrix4x4 value) ;

    internal Object GetTileAsset (Vector3Int position) {
        return INTERNAL_CALL_GetTileAsset ( this, ref position );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static Object INTERNAL_CALL_GetTileAsset (Tilemap self, ref Vector3Int position);
    public TileBase GetTile(Vector3Int position) { return (TileBase)GetTileAsset(position); }
    public T GetTile<T>(Vector3Int position) where T : TileBase { return GetTileAsset(position) as T; }
    internal void SetTileAsset (Vector3Int position, Object tile) {
        INTERNAL_CALL_SetTileAsset ( this, ref position, tile );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetTileAsset (Tilemap self, ref Vector3Int position, Object tile);
    public void SetTile(Vector3Int position, TileBase tile) { SetTileAsset(position, tile); }
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal void SetTileAssets (Vector3Int[] positionArray, Object[] tileArray) ;

    public void SetTiles(Vector3Int[] positionArray, TileBase[] tileArray) { SetTileAssets(positionArray, tileArray); }
    internal void SetTileAssetsBlock (Vector3Int position, Vector3Int blockDimensions, Object[] tileArray) {
        INTERNAL_CALL_SetTileAssetsBlock ( this, ref position, ref blockDimensions, tileArray );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetTileAssetsBlock (Tilemap self, ref Vector3Int position, ref Vector3Int blockDimensions, Object[] tileArray);
    public void SetTilesBlock(BoundsInt position, TileBase[] tileArray) { SetTileAssetsBlock(position.min, position.size, tileArray); }
    private Object[] GetTileAssetsBlock (Vector3Int position, Vector3Int blockDimensions) {
        return INTERNAL_CALL_GetTileAssetsBlock ( this, ref position, ref blockDimensions );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static Object[] INTERNAL_CALL_GetTileAssetsBlock (Tilemap self, ref Vector3Int position, ref Vector3Int blockDimensions);
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
    
    
    public bool HasTile (Vector3Int position) {
        return INTERNAL_CALL_HasTile ( this, ref position );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_HasTile (Tilemap self, ref Vector3Int position);
    public void RefreshTile (Vector3Int position) {
        INTERNAL_CALL_RefreshTile ( this, ref position );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_RefreshTile (Tilemap self, ref Vector3Int position);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void RefreshAllTiles () ;

    internal void SwapTileAsset (Object changeTile, Object newTile) {
        INTERNAL_CALL_SwapTileAsset ( this, changeTile, newTile );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SwapTileAsset (Tilemap self, Object changeTile, Object newTile);
    public void SwapTile(TileBase changeTile, TileBase newTile) { SwapTileAsset(changeTile, newTile); }
    
    
    internal bool ContainsTileAsset (Object tileAsset) {
        return INTERNAL_CALL_ContainsTileAsset ( this, tileAsset );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_ContainsTileAsset (Tilemap self, Object tileAsset);
    public bool ContainsTile(TileBase tileAsset) { return ContainsTileAsset(tileAsset); }
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public int GetUsedTilesCount () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public int GetUsedTilesNonAlloc (TileBase[] usedTiles) ;

    public Sprite GetSprite (Vector3Int position) {
        return INTERNAL_CALL_GetSprite ( this, ref position );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static Sprite INTERNAL_CALL_GetSprite (Tilemap self, ref Vector3Int position);
    public Matrix4x4 GetTransformMatrix (Vector3Int position) {
        Matrix4x4 result;
        INTERNAL_CALL_GetTransformMatrix ( this, ref position, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetTransformMatrix (Tilemap self, ref Vector3Int position, out Matrix4x4 value);
    public void SetTransformMatrix (Vector3Int position, Matrix4x4 transform) {
        INTERNAL_CALL_SetTransformMatrix ( this, ref position, ref transform );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetTransformMatrix (Tilemap self, ref Vector3Int position, ref Matrix4x4 transform);
    internal Color GetTileColor (Vector3Int position) {
        Color result;
        INTERNAL_CALL_GetTileColor ( this, ref position, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetTileColor (Tilemap self, ref Vector3Int position, out Color value);
    public Color GetColor(Vector3Int position) { return GetTileColor(position); }
    internal void SetTileColor (Vector3Int position, Color color) {
        INTERNAL_CALL_SetTileColor ( this, ref position, ref color );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetTileColor (Tilemap self, ref Vector3Int position, ref Color color);
    public void SetColor(Vector3Int position, Color color) { SetTileColor(position, color); }
    public TileFlags GetTileFlags (Vector3Int position) {
        return INTERNAL_CALL_GetTileFlags ( this, ref position );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static TileFlags INTERNAL_CALL_GetTileFlags (Tilemap self, ref Vector3Int position);
    public void SetTileFlags (Vector3Int position, TileFlags flags) {
        INTERNAL_CALL_SetTileFlags ( this, ref position, flags );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetTileFlags (Tilemap self, ref Vector3Int position, TileFlags flags);
    public void AddTileFlags (Vector3Int position, TileFlags flags) {
        INTERNAL_CALL_AddTileFlags ( this, ref position, flags );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_AddTileFlags (Tilemap self, ref Vector3Int position, TileFlags flags);
    public void RemoveTileFlags (Vector3Int position, TileFlags flags) {
        INTERNAL_CALL_RemoveTileFlags ( this, ref position, flags );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_RemoveTileFlags (Tilemap self, ref Vector3Int position, TileFlags flags);
    public GameObject GetInstantiatedObject (Vector3Int position) {
        return INTERNAL_CALL_GetInstantiatedObject ( this, ref position );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static GameObject INTERNAL_CALL_GetInstantiatedObject (Tilemap self, ref Vector3Int position);
    public void SetColliderType (Vector3Int position, Tile.ColliderType colliderType) {
        INTERNAL_CALL_SetColliderType ( this, ref position, colliderType );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetColliderType (Tilemap self, ref Vector3Int position, Tile.ColliderType colliderType);
    public Tile.ColliderType GetColliderType (Vector3Int position) {
        return INTERNAL_CALL_GetColliderType ( this, ref position );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static Tile.ColliderType INTERNAL_CALL_GetColliderType (Tilemap self, ref Vector3Int position);
    public void FloodFill (Vector3Int position, Object tile) {
        INTERNAL_CALL_FloodFill ( this, ref position, tile );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_FloodFill (Tilemap self, ref Vector3Int position, Object tile);
    public void BoxFill (Vector3Int position, Object tile, int startX, int startY, int endX, int endY) {
        INTERNAL_CALL_BoxFill ( this, ref position, tile, startX, startY, endX, endY );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_BoxFill (Tilemap self, ref Vector3Int position, Object tile, int startX, int startY, int endX, int endY);
    public void ClearAllTiles () {
        INTERNAL_CALL_ClearAllTiles ( this );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_ClearAllTiles (Tilemap self);
    public void ResizeBounds () {
        INTERNAL_CALL_ResizeBounds ( this );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_ResizeBounds (Tilemap self);
    public void CompressBounds () {
        INTERNAL_CALL_CompressBounds ( this );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_CompressBounds (Tilemap self);
    public Vector3Int editorPreviewOrigin
    {
        get { Vector3Int tmp; INTERNAL_get_editorPreviewOrigin(out tmp); return tmp;  }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_editorPreviewOrigin (out Vector3Int value) ;


    public Vector3Int editorPreviewSize
    {
        get { Vector3Int tmp; INTERNAL_get_editorPreviewSize(out tmp); return tmp;  }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_editorPreviewSize (out Vector3Int value) ;


    internal Object GetEditorPreviewTileAsset (Vector3Int position) {
        return INTERNAL_CALL_GetEditorPreviewTileAsset ( this, ref position );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static Object INTERNAL_CALL_GetEditorPreviewTileAsset (Tilemap self, ref Vector3Int position);
    public TileBase GetEditorPreviewTile(Vector3Int position) { return GetEditorPreviewTileAsset(position) as TileBase; }
    
    
    public T GetEditorPreviewTile<T>(Vector3Int position) where T : TileBase { return GetEditorPreviewTile(position) as T; }
    
    
    internal void SetEditorPreviewTileAsset (Vector3Int position, Object tile) {
        INTERNAL_CALL_SetEditorPreviewTileAsset ( this, ref position, tile );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetEditorPreviewTileAsset (Tilemap self, ref Vector3Int position, Object tile);
    public void SetEditorPreviewTile(Vector3Int position, TileBase tile) { SetEditorPreviewTileAsset(position, tile); }
    
    
    public bool HasEditorPreviewTile (Vector3Int position) {
        return INTERNAL_CALL_HasEditorPreviewTile ( this, ref position );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_HasEditorPreviewTile (Tilemap self, ref Vector3Int position);
    public Sprite GetEditorPreviewSprite (Vector3Int position) {
        return INTERNAL_CALL_GetEditorPreviewSprite ( this, ref position );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static Sprite INTERNAL_CALL_GetEditorPreviewSprite (Tilemap self, ref Vector3Int position);
    public Matrix4x4 GetEditorPreviewTransformMatrix (Vector3Int position) {
        Matrix4x4 result;
        INTERNAL_CALL_GetEditorPreviewTransformMatrix ( this, ref position, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetEditorPreviewTransformMatrix (Tilemap self, ref Vector3Int position, out Matrix4x4 value);
    public void SetEditorPreviewTransformMatrix (Vector3Int position, Matrix4x4 transform) {
        INTERNAL_CALL_SetEditorPreviewTransformMatrix ( this, ref position, ref transform );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetEditorPreviewTransformMatrix (Tilemap self, ref Vector3Int position, ref Matrix4x4 transform);
    public Color GetEditorPreviewColor (Vector3Int position) {
        Color result;
        INTERNAL_CALL_GetEditorPreviewColor ( this, ref position, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetEditorPreviewColor (Tilemap self, ref Vector3Int position, out Color value);
    public void SetEditorPreviewColor (Vector3Int position, Color color) {
        INTERNAL_CALL_SetEditorPreviewColor ( this, ref position, ref color );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetEditorPreviewColor (Tilemap self, ref Vector3Int position, ref Color color);
    public TileFlags GetEditorPreviewTileFlags (Vector3Int position) {
        return INTERNAL_CALL_GetEditorPreviewTileFlags ( this, ref position );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static TileFlags INTERNAL_CALL_GetEditorPreviewTileFlags (Tilemap self, ref Vector3Int position);
    public void EditorPreviewFloodFill(Vector3Int position, TileBase tile)
        {
            EditorPreviewFloodFillTileAsset(position, tile);
        }
    
    
    private void EditorPreviewFloodFillTileAsset (Vector3Int position, Object tile) {
        INTERNAL_CALL_EditorPreviewFloodFillTileAsset ( this, ref position, tile );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_EditorPreviewFloodFillTileAsset (Tilemap self, ref Vector3Int position, Object tile);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void ClearAllEditorPreviewTiles () ;

}

[RequireComponent(typeof(Tilemap))]
public sealed partial class TilemapRenderer : Renderer
{
            public enum SortOrder
        {
            BottomLeft = 0,
            BottomRight = 1,
            TopLeft = 2,
            TopRight = 3,
        }
    
    
    public Vector3Int chunkSize
    {
        get { Vector3Int tmp; INTERNAL_get_chunkSize(out tmp); return tmp;  }
        set { INTERNAL_set_chunkSize(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_chunkSize (out Vector3Int value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_set_chunkSize (ref Vector3Int value) ;

    public extern int maxChunkCount
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern int maxFrameAge
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern TilemapRenderer.SortOrder sortOrder
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern SpriteMaskInteraction maskInteraction
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

}

[RequiredByNativeCode]
[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
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
[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
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
public sealed partial class TilemapCollider2D : Collider2D
{
}


}


