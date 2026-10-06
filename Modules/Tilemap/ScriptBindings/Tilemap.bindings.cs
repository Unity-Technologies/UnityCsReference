// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Bindings;
using UnityEngine.U2D;
using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;

namespace UnityEngine.Tilemaps
{
    ///<summary>Flags controlling behavior for the <see cref="TileBase" />.</summary>
    [Flags]
    public enum TileFlags
    {
        ///<summary>No <see cref="TileFlags" /> are set.</summary>
        None = 0,
        ///<summary>
        ///  <see cref="TileBase" /> locks any color set by brushes or the user.</summary>
        LockColor = 1 << 0,
        ///<summary>
        ///  <see cref="TileBase" /> locks any transform matrix set by brushes or the user.</summary>
        LockTransform = 1 << 1,
        ///<summary>
        ///  <see cref="TileBase" /> does not instantiate its associated GameObject in editor mode and instantiates it only during Play mode.</summary>
        InstantiateGameObjectRuntimeOnly = 1 << 2,
        ///<summary>Keeps the <see cref="TileBase" />'s associated GameObject in Play mode when replaced with another <see cref="TileBase" /> or erased</summary>
        KeepGameObjectRuntimeOnly = 1 << 3,
        ///<summary>All lock flags.</summary>
        LockAll = LockColor | LockTransform,
    }

    ///<summary>Options for the reasons why a <see cref="Tilemap" /> itself has changed.</summary>
    ///<remarks>These reasons affect every cell, and are reported by <see cref="Tilemap.tilemapChanged" />. For changes
    ///to individual tiles, see <see cref="TileChangeReason" />.
    ///A batch of changes is reported with the reasons of every change in it combined.</remarks>
    [Flags]
    public enum TilemapChangeReason
    {
        ///<summary>The origin or size of the <see cref="Tilemap" /> changed.</summary>
        ///<remarks>Existing tiles keep their positions.</remarks>
        Dimensions = 1 << 0,
        ///<summary>A property of the <see cref="Tilemap" />, such as its tile anchor, orientation or color, changed.</summary>
        Property = 1 << 1,
        ///<summary>The geometry of the <see cref="Tilemap" /> changed, including changes to its <see cref="Grid" />.</summary>
        ///<remarks>Every tile moves, so colliders built from the <see cref="Tilemap" /> are out of date.</remarks>
        Geometry = 1 << 2,
        ///<summary>All map-wide changes.</summary>
        Everything = Dimensions | Property | Geometry,
        ///<summary>The change was made to the editor preview of the <see cref="Tilemap" /> rather than to the
        ///<see cref="Tilemap" /> itself.</summary>
        ///<remarks>This accompanies the reason for the change. Ignore changes carrying it to leave a brush preview
        ///out of anything built from the <see cref="Tilemap" />.</remarks>
        EditorPreview = 1 << 3,
    }

    ///<summary>Options for the reasons why tiles of a <see cref="Tilemap" /> have changed.</summary>
    ///<remarks>These reasons are reported by <see cref="Tilemap.tilemapTilesChanged" />. For changes to the
    ///<see cref="Tilemap" /> itself, see <see cref="TilemapChangeReason" />.
    ///A batch of changes is reported with the reasons of every change in it combined, so a reason cannot be traced
    ///back to the individual tile that caused it.</remarks>
    [Flags]
    public enum TileChangeReason
    {
        ///<summary>A <see cref="Tile" /> was set, cleared or refreshed, so any of its properties may have changed.</summary>
        Replaced = 1 << 0,
        ///<summary>The animation of a <see cref="Tile" /> changed.</summary>
        SpriteAnimation = 1 << 1,
        ///<summary>The transform matrix of a <see cref="Tile" /> changed.</summary>
        Transform = 1 << 2,
        ///<summary>The color of a <see cref="Tile" /> changed.</summary>
        Color = 1 << 3,
        ///<summary>The <see cref="TileFlags" /> of a <see cref="Tile" /> changed.</summary>
        Flags = 1 << 4,
        ///<summary>The <see cref="Tile.ColliderType" /> of a <see cref="Tile" /> changed.</summary>
        ColliderType = 1 << 5,

        ///<summary>The changes which require colliders to be rebuilt for physics.</summary>
        ///<remarks>Test against this to ignore changes that cannot affect collision, such as a tile being recolored.</remarks>
        Physics = Replaced | Transform | ColliderType,
    }

    ///<summary>Options for flags controlling behavior for a Tile Animation on a <see cref="Tilemap" />.</summary>
    [Flags]
    public enum TileAnimationFlags
    {
        ///<summary>Sets no <see cref="TileAnimationFlags" /> and the Tile Animation will run normally.</summary>
        None = 0,
        ///<summary>Loops the Tile Animation once, then stops on the last <see cref="Sprite" /> of the animation.</summary>
        LoopOnce = 1 << 0,
        ///<summary>Pauses the Tile Animation, stopping it from running.</summary>
        PauseAnimation = 1 << 1,
        ///<summary>Updates the Physics Shape in the <see cref="TilemapCollider2D" /> whenever the Tile Animation switches to the next <see cref="Sprite" /> in the animation.</summary>
        UpdatePhysics = 1 << 2,
        ///<summary>When true, the unscaled time is used to run the Tile Animation. Otherwise, the scaled time is used to run the Tile Animation.</summary>
        ///<remarks>This is useful for playing the Tile Animation whilst the game is paused and [[Time.timeScale] is set to zero.</remarks>
        UnscaledTime = 1 << 3,
        ///<summary>When set, this will sync the start time of this Tile Animation with other Tile Animations that are the same.</summary>
        ///<remarks>This will not affect Tile Animations that are changed using <see cref="Tilemap.SetAnimationTime" /> or <see cref="Tilemap.SetAnimationFrame" />.</remarks>
        SyncAnimation = 1 << 4,
    }

    [RequireComponent(typeof(Transform))]
    [NativeHeader("Modules/Grid/Public/GridMarshalling.h")]
    [NativeHeader("Modules/Grid/Public/Grid.h")]
    [NativeHeader("Runtime/Graphics/SpriteFrame.h")]
    [NativeHeader("Modules/Tilemap/Public/TilemapTile.h")]
    [NativeHeader("Modules/Tilemap/Public/TilemapMarshalling.h")]
    [NativeHeader("Modules/Tilemap/Public/Tilemap.h")]
    [NativeClass("Tilemap", PersistentTypeId = 0x6DA822BD)]
    public sealed partial class Tilemap : GridLayout
    {
        internal Tilemap(global::UnityEngine.EntityId id) : base(id) {}
        public Tilemap() {}
        ///<summary>Determines the orientation of <see cref="Tile" />s in the <see cref="Tilemap" />.</summary>
        public enum Orientation
        {
            ///<summary>Orients Tiles in the XY plane.</summary>
            XY = 0,
            ///<summary>Orients Tiles in the XZ plane.</summary>
            XZ = 1,
            ///<summary>Orients Tiles in the YX plane.</summary>
            YX = 2,
            ///<summary>Orients Tiles in the YZ plane.</summary>
            YZ = 3,
            ///<summary>Orients Tiles in the ZX plane.</summary>
            ZX = 4,
            ///<summary>Orients Tiles in the ZY plane.</summary>
            ZY = 5,
            ///<summary>Use a custom orientation to all tiles in the tile map.</summary>
            ///<remarks>Set the custom orientation into <see cref="Tilemap.orientationMatrix" />.</remarks>
            Custom = 6,
        }

        ///<summary>Gets the <see cref="Grid" /> associated with this <see cref="Tilemap" />.</summary>
        public extern Grid layoutGrid
        {
            [NativeMethod(Name = "GetAttachedGrid")]
            get;
        }

        ///<summary>Gets the logical center coordinate of a <see cref="Grid" /> cell in local space. The logical center for a cell of the <see cref="Tilemap" /> is defined by the Tile Anchor of the Tilemap.</summary>
        ///<remarks>In a rectangular grid layout, a call to <see cref="GridLayout.CellToLocal" /> with <see cref="Vector3Int" /> parameter, returns a <see cref="Vector3" /> coordinate that represents the bottom-left of the cell. While mathematically correct, you may prefer the center of the cell, for example when instantiating a GameObject into the grid.</remarks>
        ///<param name="position">Grid cell position.</param>
        ///<returns>Returns the center of the cell transformed into local space coordinates.</returns>
        ///<example>
        ///  <code><![CDATA[
        /// // Snap the GameObject to parent Tilemap center of cell
        ///using UnityEngine;
        ///using UnityEngine.Tilemaps;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        Tilemap tilemap = transform.parent.GetComponent<Tilemap>();
        ///        Vector3Int cellPosition = tilemap.LocalToCell(transform.localPosition);
        ///        transform.localPosition = tilemap.GetCellCenterLocal(cellPosition);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public Vector3 GetCellCenterLocal(Vector3Int position) { return CellToLocalInterpolated(position) + CellToLocalInterpolated(tileAnchorRatio); }
        ///<summary>Gets the logical center coordinate of a <see cref="Grid" /> cell in world space. The logical center for a cell of the <see cref="Tilemap" /> is defined by the Tile Anchor of the Tilemap.</summary>
        ///<remarks>In a rectangular grid layout, a call to <see cref="GridLayout.CellToWorld" /> with <see cref="Vector3Int" /> parameter returns a <see cref="Vector3" /> coordinate that represents the lower left of the cell. This is mathematically correct, but in certain cases such as when instantiating a GameObject into the grid, you may prefer the center of the cell instead.</remarks>
        ///<param name="position">Grid cell position.</param>
        ///<returns>Returns the center of the cell transformed into world space coordinates.</returns>
        ///<example>
        ///  <code><![CDATA[
        /// // Snap the GameObject to parent Tilemap center of cell
        ///using UnityEngine;
        ///using UnityEngine.Tilemaps;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        Tilemap tilemap = transform.parent.GetComponent<Tilemap>();
        ///        Vector3Int cellPosition = tilemap.WorldToCell(transform.position);
        ///        transform.position = tilemap.GetCellCenterWorld(cellPosition);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public Vector3 GetCellCenterWorld(Vector3Int position) { return LocalToWorld(GetCellCenterLocal(position)); }

        ///<summary>Returns the boundaries of the <see cref="Tilemap" /> in cell size.</summary>
        public BoundsInt cellBounds
        {
            get
            {
                return new BoundsInt(origin, size);
            }
        }

        ///<summary>Returns the boundaries of the <see cref="Tilemap" /> in local space size.</summary>
        [NativeProperty("TilemapBoundsScripting")]
        public extern Bounds localBounds
        {
            get;
        }

        [NativeProperty("TilemapFrameBoundsScripting")]
        internal extern Bounds localFrameBounds
        {
            get;
        }

        ///<summary>The frame rate for all Tile animations in the Tilemap.</summary>
        ///<remarks>The actual frame rate for each animation is dependent on this and the animation speed of the Tile animation.</remarks>
        public extern float animationFrameRate
        {
            get;
            set;
        }

        ///<summary>The color of the <see cref="Tilemap" /> layer.</summary>
        ///<remarks>The color of the <see cref="Tilemap" /> layer is multiplied with the color of the Tiles in the layer to tint the Tiles.</remarks>
        public extern Color color
        {
            get;
            set;
        }
        ///<summary>The origin of the <see cref="Tilemap" /> in cell position.</summary>
        ///<remarks>This takes into consideration only placed Tiles in the <see cref="Tilemap" />.</remarks>
        public extern Vector3Int origin
        {
            get;
            set;
        }

        ///<summary>The size of the <see cref="Tilemap" /> in cells.</summary>
        ///<remarks>This takes into consideration only placed Tiles in the <see cref="Tilemap" />.</remarks>
        public extern Vector3Int size
        {
            get;
            set;
        }

        ///<summary>Gets the anchor point of Tiles in the <see cref="Tilemap" />.</summary>
        [NativeProperty(Name = "TileAnchorScripting")]
        public extern Vector3 tileAnchor
        {
            get;
            set;
        }

        [NativeProperty(Name = "TileAnchorRatioScripting")]
        internal extern Vector3 tileAnchorRatio
        {
            get;
        }

        ///<summary>Orientation of the Tiles in the <see cref="Tilemap" />.</summary>
        public extern Orientation orientation
        {
            get;
            set;
        }

        ///<summary>Orientation Matrix of the orientation of the Tiles in the <see cref="Tilemap" />.</summary>
        ///<remarks>This matrix can be customised if the orientation of the Tiles is set to <see cref="Tilemap.Orientation.Custom" />.</remarks>
        public extern Matrix4x4 orientationMatrix
        {
            [NativeMethod(Name = "GetTileOrientationMatrix")]
            get;
            [NativeMethod(Name = "SetOrientationMatrix")]
            set;
        }

        internal extern Object GetTileAsset(Vector3Int position);
        ///<summary>Gets the <see cref="Tile" /> at the given XYZ coordinates of a cell in the Tilemap.</summary>
        ///<remarks>Use this method to get the [Tile](xref:Tilemap-ScriptableTiles-TileBase) at the given XYZ coordinates of a cell in the [Tilemap](xref:class-Tilemap).</remarks>
        ///<param name="position">Position of the <see cref="Tile" /> on the <see cref="Tilemap" />.</param>
        ///<returns>
        ///  <see cref="Tilemaps.TileBase" /> placed at the cell.</returns>
        public TileBase GetTile(Vector3Int position) { return GetTileAsset(position) as TileBase; }
        ///<summary>Gets the <see cref="Tile" /> of type T at the given XYZ coordinates of a cell in the <see cref="Tilemap" />.</summary>
        ///<remarks>Use this method to get the [Tile of type T](xref:Tilemap-ScriptableTiles-TileBase) at the given XYZ coordinates of a cell in the [Tilemap](xref:class-Tilemap).</remarks>
        ///<param name="position">Position of the <see cref="Tile" /> on the <see cref="Tilemap" />.</param>
        ///<returns>
        ///  <see cref="Tilemaps.TileBase">Tile of type T</see> placed at the cell.</returns>
        public T GetTile<T>(Vector3Int position) where T : TileBase { return GetTileAsset(position) as T; }

        ///<summary>Gets the EntityId of the <see cref="Tile" /> at the given xyz coordinates of a cell in the Tilemap.</summary>
        ///<param name="position">The position of the <see cref="Tile" /> on the <see cref="Tilemap" />.</param>
        ///<returns>The EntityId of the <see cref="Tilemaps.TileBase" /> placed at the cell.</returns>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///using UnityEngine.Tilemaps;
        ///
        ///public static class TilemapExample
        ///{
        ///    public static void GetTileEntityIdExample(Tilemap tilemap, Vector3Int position, Tile tile)
        ///    {
        ///        tilemap.SetTile(position, tile);
        ///        var tileId = tilemap.GetTileEntityId(position);
        ///        Debug.Log($"The ids for the Tile placed are equal ({(tileId == tile.GetEntityId()).ToString()})");
        ///    }
        ///}]]></code>
        ///</example>
        [NativeMethod(Name = "GetTileAssetEntityId", IsThreadSafe = true)]
        public extern EntityId GetTileEntityId(Vector3Int position);

        internal extern IntPtr GetTilemapHandle();

        [NativeMethod(Name = "GetTileEntityIdFromHandle", IsThreadSafe = true)]
        internal static extern EntityId GetTileEntityIdFromHandle(IntPtr tilemapHandle, Vector3Int position);

        [NativeMethod(Name = "GetTileEntityIdsFromOffsets", IsThreadSafe = true)]
        private extern void GetTileEntityIdsFromOffsets(Vector3Int position, IntPtr offsetsIntrPtr, IntPtr tilesIntPtr, int count);

        [NativeMethod(Name = "GetTileEntityIdsFromOffsetsAndHandle", IsThreadSafe = true)]
        private static extern void GetTileEntityIdsFromOffsetsAndHandle(IntPtr tilemapHandle, Vector3Int position, IntPtr offsetsIntrPtr, IntPtr tilesIntPtr, int count);

        [NativeMethod(Name = "GetTileEntityIdsFromBlockOffset", IsThreadSafe = true)]
        private extern void GetTileEntityIdsFromBlockOffset(Vector3Int position, BoundsInt blockOffset, IntPtr tilesIntPtr, int count);

        [NativeMethod(Name = "GetTileEntityIdsFromBlockOffsetAndHandle", IsThreadSafe = true)]
        private static extern void GetTileEntityIdsFromBlockOffsetAndHandle(IntPtr tilemapHandle, Vector3Int position, BoundsInt blockOffset, IntPtr tilesIntPtr, int count);

        internal extern Object[] GetTileAssetsBlock(Vector3Int position, Vector3Int blockDimensions);

        ///<summary>Retrieves an array of Tiles with the given bounds.</summary>
        ///<remarks>This is meant for more a performant way to get Tiles as a batch, when compared to calling <see cref="GetTile" /> for every single position.
        ///The bounds size must match the array size. For example bounds of 1x2x3 needs an array length of 6.</remarks>
        ///<param name="bounds">The bounds to retrieve from.</param>
        ///<returns>The array of <see cref="Tile" />s at the given bounds.</returns>
        ///<example>
        ///  <code><![CDATA[ // Retrieves all Tiles from an area on the Tilemap and prints out the Tiles to console
        ///using UnityEngine;
        ///using UnityEngine.Tilemaps;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public BoundsInt area;
        ///
        ///    void Start()
        ///    {
        ///        Tilemap tilemap = GetComponent<Tilemap>();
        ///        TileBase[] tileArray = tilemap.GetTilesBlock(area);
        ///        for (int index = 0; index < tileArray.Length; index++)
        ///        {
        ///            print(tileArray[index]);
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
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

        [FreeFunction(Name = "TilemapBindings::GetTileAssetsBlockNonAlloc", HasExplicitThis = true)]
        internal extern int GetTileAssetsBlockNonAlloc(Vector3Int startPosition, Vector3Int endPosition, [UnityMarshalAs(NativeType.ScriptingObjectPtr)] Object[] tiles);

        ///<summary>Retrieves an array of Tiles with the given bounds.</summary>
        ///<remarks>This is meant for more a performant way to get Tiles as a batch, when compared to calling <see cref="GetTile" /> for every single position.
        ///If the size of the arrays passed in as parameters are less than the number of Tiles within the range, the arrays will not be resized and the results will be limited.</remarks>
        ///<param name="bounds">The bounds to retrieve from.</param>
        ///<param name="tiles">The array of <see cref="Tile" />s to contain the Tiles at the given bounds.</param>
        ///<returns>Returns the number of Tiles retrieved, including null entries.</returns>
        ///<example>
        ///  <code><![CDATA[
        /// // Retrieves all Tiles from an area on the Tilemap and prints out the Tiles to console
        ///using UnityEngine;
        ///using UnityEngine.Tilemaps;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public BoundsInt area;
        ///
        ///    void Start()
        ///    {
        ///        Tilemap tilemap = GetComponent<Tilemap>();
        ///        TileBase[] tileArray = new TileBase[16];
        ///        int count = tilemap.GetTilesBlockNonAlloc(area, tileArray);
        ///        for (int index = 0; index < count; index++)
        ///        {
        ///            print(tileArray[index]);
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public int GetTilesBlockNonAlloc(BoundsInt bounds, TileBase[] tiles)
        {
            return GetTileAssetsBlockNonAlloc(bounds.min, bounds.size, tiles);
        }

        ///<summary>Retrieves the number of Tiles within the given range, inclusive of the Cells at both the starting position and the ending positions. This method begins at the given starting position and iterates through all available Z Positions, then iterates through the X and Y positions until it reaches the ending position.</summary>
        ///<remarks>
        ///  <para>This method begins at the given starting position, and is inclusive of the Cell at this starting position. It retrieves all Tiles at a given Cell, including Tiles at all Z Positions at that Cell position. After retrieving all Tiles available at the current Cell, the method continues iterating onto the next Cell along the same row until it reaches the rightmost bounds of the Tilemap. After reaching the end of the Tilemap along the initial row of Cells, the method then iterates along the next row of Cells above the initial row, starting from the leftmost edge of the Tilemap. The method continues iterating in this pattern until it reaches the Cell at the ending position of the given range.
        ///
        ///If the starting position's value is higher than the ending position's value, then the method begins with the Cell at the starting position but iterates in the opposite direction of the usual method, ending when it reaches the Cell at the ending position.</para>
        ///  <para>In the example above, <see cref="GetTilesRangeCount" /> will return a count of sixteen: ten Tiles from (0, 0, 0) to (10, 0, 0), as well as the six Tiles from (0, 1, 0) to (5, 1, 0).</para>
        ///</remarks>
        ///<param name="startPosition">The starting position of the range to retrieve Tiles from.</param>
        ///<param name="endPosition">The ending position of the range to retrieve Tiles from.</param>
        ///<returns>Returns the number of Tiles within the given range.</returns>
        ///<example>
        ///  <code><![CDATA[
        /// // Retrieves all tiles with a range on the tilemap and prints out the positions and tiles to console
        ///using UnityEngine;
        ///using UnityEngine.Tilemaps;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        Tilemap tilemap = GetComponent<Tilemap>();
        ///        SetTiles(tilemap);
        ///
        ///        var count = tilemap.GetTilesRangeCount(new Vector3Int(0, 0, 0), new Vector3Int(5, 1, 0));
        ///
        ///        Vector3Int[] positions = new Vector3Int[count];
        ///        TileBase[] tiles = new TileBase[count];
        ///        count = tilemap.GetTilesRangeNonAlloc(new Vector3Int(0, 0, 0), new Vector3Int(5, 1, 0), positions, tiles);
        ///        for (int index = 0; index < count; index++)
        ///        {
        ///            print(positions[index]);
        ///            print(tiles[index]);
        ///        }
        ///    }
        ///
        ///    // Sets Tiles in a 10 by 10 block
        ///    void SetTiles(Tilemap tilemap)
        ///    {
        ///        Tile tile = ScriptableObject.CreateInstance<Tile>();
        ///        TileBase[] tiles = new TileBase[10 * 10];
        ///        for (int index = 0; index < tiles.Length; index++)
        ///        {
        ///            tiles[index] = tile;
        ///        }
        ///        tilemap.SetTilesBlock(new BoundsInt(0, 0, 0, 10, 10, 1), tiles);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public extern int GetTilesRangeCount(Vector3Int startPosition, Vector3Int endPosition);

        [FreeFunction(Name = "TilemapBindings::GetTileAssetsRangeNonAlloc", HasExplicitThis = true)]
        internal extern int GetTileAssetsRangeNonAlloc(Vector3Int startPosition, Vector3Int endPosition, Vector3Int[] positions, [UnityMarshalAs(NativeType.ScriptingObjectPtr)] Object[] tiles);

        ///<summary>Retrieves an array of Tiles within the given range, inclusive of the Cells at both the starting position and the ending positions. This method begins at the given starting position and iterates through all available Z Positions, then iterates through the X and Y positions until it reaches the ending position.</summary>
        ///<remarks>
        ///  <para>This method begins at the given starting position, and is inclusive of the Cell at this starting position. It retrieves all Tiles at a given Cell, including Tiles at all Z Positions at that Cell position. After retrieving all Tiles available at the current Cell, the method continues iterating onto the next Cell along the same row until it reaches the rightmost bounds of the Tilemap. After reaching the end of the Tilemap along the initial row of Cells, the method then iterates along the next row of Cells above the initial row, starting from the leftmost edge of the Tilemap. The method continues iterating in this pattern until it reaches the Cell at the ending position of the given range.
        ///
        ///If the starting position's value is higher than the ending position's value, then the method begins with the Cell at the starting position but iterates in the opposite direction of the usual method, ending when it reaches the Cell at the ending position.
        ///
        ///If the size of the arrays passed in as parameters are less than the number of Tiles within the range, the arrays will not be resized and the results will be limited.</para>
        ///  <para>In the example above, <see cref="GetTilesRangeNonAlloc" /> with a starting position (0, 0, 0) and an ending position (5, 1, 0) will return an array of sixteen Tiles: ten Tiles from (0, 0, 0) to (10, 0, 0), as well as the six Tiles from (0, 1, 0) to (5, 1, 0).
        ///
        ///If the starting and ending positions were swapped, then <see cref="GetTilesRangeNonAlloc" /> with starting position (5, 1, 0) and ending position (0, 0, 0) will return an array of sixteen Tiles: six Tiles from (5, 1, 0) to (0, 1, 0), as well as the ten Tiles from (10, 0, 0) to (0, 0, 0).</para>
        ///</remarks>
        ///<param name="startPosition">The starting position of the range to retrieve Tiles from.</param>
        ///<param name="endPosition">The ending position of the range to retrieve Tiles from.</param>
        ///<param name="positions">The positions of Tiles within the given range.</param>
        ///<param name="tiles">The Tiles within the given range.</param>
        ///<returns>Returns the number of positions and Tiles retrieved.</returns>
        ///<example>
        ///  <code><![CDATA[
        /// // Retrieves all tiles with a range on the tilemap and prints out the positions and tiles to console
        ///using UnityEngine;
        ///using UnityEngine.Tilemaps;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        Tilemap tilemap = GetComponent<Tilemap>();
        ///        SetTiles(tilemap);
        ///
        ///        Vector3Int[] positions = new Vector3Int[16];
        ///        TileBase[] tiles = new TileBase[16];
        ///        var count = tilemap.GetTilesRangeNonAlloc(new Vector3Int(0, 0, 0), new Vector3Int(5, 1, 0), positions, tiles);
        ///        for (int index = 0; index < count; index++)
        ///        {
        ///            print(positions[index]);
        ///            print(tiles[index]);
        ///        }
        ///    }
        ///
        ///    // Sets Tiles in a 10 by 10 block
        ///    void SetTiles(Tilemap tilemap)
        ///    {
        ///        Tile tile = ScriptableObject.CreateInstance<Tile>();
        ///        TileBase[] tiles = new TileBase[10 * 10];
        ///        for (int index = 0; index < tiles.Length; index++)
        ///        {
        ///            tiles[index] = tile;
        ///        }
        ///        tilemap.SetTilesBlock(new BoundsInt(0, 0, 0, 10, 10, 1), tiles);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public int GetTilesRangeNonAlloc(Vector3Int startPosition, Vector3Int endPosition, Vector3Int[] positions, TileBase[] tiles)
        {
            return GetTileAssetsRangeNonAlloc(startPosition, endPosition, positions, tiles);
        }

        internal extern void SetTileAsset(Vector3Int position, Object tile);

        ///<summary>Sets a <see cref="Tile" /> at the given XYZ coordinates of a cell in the <see cref="Tilemap" />.</summary>
        ///<remarks>Refer to [Scriptable Tiles](xref:Tilemap-ScriptableTiles-TileBase) and [Tilemap](xref:class-Tilemap) for more information.</remarks>
        ///<param name="position">Position of the Tile on the <see cref="Tilemap" />.</param>
        ///<param name="tile">The <see cref="Tile" /> to be placed in the cell.</param>
        public void SetTile(Vector3Int position, TileBase tile) { SetTileAsset(position, tile); }

        internal extern void SetTileAssets(Vector3Int[] positionArray, Object[] tileArray);

        ///<summary>Sets an array of <see cref="Tile" />s at the given XYZ coordinates of the corresponding cells in the <see cref="Tilemap" />.</summary>
        ///<remarks>Refer to [Scriptable Tiles](xref:Tilemap-ScriptableTiles-TileBase) and [Tilemap](xref:class-Tilemap) for more information.</remarks>
        ///<param name="positionArray">An array of positions of tiles on the <see cref="Tilemap" />.</param>
        ///<param name="tileArray">An array of <see cref="Tile" />s to be placed.</param>
        ///<example>
        ///  <code><![CDATA[ // Fills Tilemap area with checkerboard pattern of tileA and tileB
        ///using UnityEngine;
        ///using UnityEngine.Tilemaps;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public TileBase tileA;
        ///    public TileBase tileB;
        ///    public Vector2Int size;
        ///
        ///    void Start()
        ///    {
        ///        Vector3Int[] positions = new Vector3Int[size.x * size.y];
        ///        TileBase[] tileArray = new TileBase[positions.Length];
        ///
        ///        for (int index = 0; index < positions.Length; index++)
        ///        {
        ///            positions[index] = new Vector3Int(index % size.x, index / size.y, 0);
        ///            tileArray[index] = index % 2 == 0 ? tileA : tileB;
        ///        }
        ///
        ///        Tilemap tilemap = GetComponent<Tilemap>();
        ///        tilemap.SetTiles(positions, tileArray);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public void SetTiles(Vector3Int[] positionArray, TileBase[] tileArray) { SetTileAssets(positionArray, tileArray); }

        ///<summary>Sets an array of <see cref="Tile" />s at the given XYZ coordinates of the corresponding cells in the <see cref="Tilemap" />.</summary>
        ///<remarks>Refer to [Scriptable Tiles](xref:Tilemap-ScriptableTiles-TileBase) and [Tilemap](xref:class-Tilemap) for more information.</remarks>
        ///<param name="positionArray">The array of positions of tiles on the <see cref="Tilemap" />.</param>
        ///<param name="tileArray">The array of <see cref="Tile" />s to place.</param>
        ///<example>
        ///  <code><![CDATA[ // Fills Tilemap area with checkerboard pattern of tileA and tileB
        ///using UnityEngine;
        ///using UnityEngine.Tilemaps;
        ///using Unity.Collections;
        ///
        ///public class ExampleClass_Native : MonoBehaviour
        ///{
        ///    public TileBase tileA;
        ///    public TileBase tileB;
        ///    public Vector2Int size;
        ///
        ///    void Start()
        ///    {
        ///        NativeArray<Vector3Int> positions = new NativeArray<Vector3Int>(size.x * size.y, Allocator.Temp);
        ///        Tilemap.TileArray tileArray = new Tilemap.TileArray(positions.Length, Allocator.Temp);
        ///
        ///        for (int index = 0; index < positions.Length; index++)
        ///        {
        ///            positions[index] = new Vector3Int(index % size.x, index / size.y, 0);
        ///            tileArray[index] = index % 2 == 0 ? tileA : tileB;
        ///        }
        ///
        ///        Tilemap tilemap = GetComponent<Tilemap>();
        ///        tilemap.SetTiles(positions, tileArray);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public void SetTiles(NativeArray<Vector3Int> positionArray, TileArray tileArray)
        {
            if (!positionArray.IsCreated
                || positionArray.Length != tileArray.Length)
                throw new ArgumentException("All NativeArrays must be created and have the same length as tileArray.");
            if (tileArray.Length == 0)
                return;
            unsafe
            {
                Internal_SetTileAssets(positionArray.m_Buffer, tileArray.buffer);
            }
        }

        [NativeMethod(Name = "SetTileAssetsBlock")]
        private extern void INTERNAL_CALL_SetTileAssetsBlock(Vector3Int position, Vector3Int blockDimensions, Object[] tileArray);
        ///<summary>Fills an area with array of <see cref="Tile" />s.</summary>
        ///<remarks>
        ///  <para>This method is a faster way to set multiple tiles in an area than calling <c>SetTile</c> for each tile.
        ///The bounds size must match the array size. For example bounds of 1x2x3 needs an array length of 6.</para>
        ///  <para>Refer to [Scriptable Tiles](xref:Tilemap-ScriptableTiles-TileBase) and [Tilemap](xref:class-Tilemap) for more information.</para>
        ///</remarks>
        ///<param name="position">The area to fill.</param>
        ///<param name="tileArray">The array of <see cref="Tile" />s to be placed.</param>
        ///<example>
        ///  <code><![CDATA[ // Fill area on Tilemap with checkerboard pattern of tileA and tileB
        ///using UnityEngine;
        ///using UnityEngine.Tilemaps;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public TileBase tileA;
        ///    public TileBase tileB;
        ///    public BoundsInt area;
        ///
        ///    void Start()
        ///    {
        ///        TileBase[] tileArray = new TileBase[area.size.x * area.size.y * area.size.z];
        ///        for (int index = 0; index < tileArray.Length; index++)
        ///        {
        ///            tileArray[index] = index % 2 == 0 ? tileA : tileB;
        ///        }
        ///        Tilemap tilemap = GetComponent<Tilemap>();
        ///        tilemap.SetTilesBlock(area, tileArray);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public void SetTilesBlock(BoundsInt position, TileBase[] tileArray) { INTERNAL_CALL_SetTileAssetsBlock(position.min, position.size, tileArray); }

        ///<summary>Fills an area with an array of <see cref="Tile" />s.</summary>
        ///<remarks>This method is a faster way to set multiple tiles in an area than calling <c>SetTile</c> for each tile.
        ///The bounds size must match the array size. For example bounds of 1x2x3 needs an array length of 6.</remarks>
        ///<param name="position">The area to fill.</param>
        ///<param name="tileArray">The array of <see cref="Tile" />s to place.</param>
        ///<example>
        ///  <code><![CDATA[ // Fill area on Tilemap with checkerboard pattern of tileA and tileB
        ///using UnityEngine;
        ///using UnityEngine.Tilemaps;
        ///using Unity.Collections;
        ///
        ///public class ExampleClass_Native : MonoBehaviour
        ///{
        ///    public TileBase tileA;
        ///    public TileBase tileB;
        ///    public BoundsInt area;
        ///
        ///    void Start()
        ///    {
        ///        Tilemap.TileArray tileArray = new Tilemap.TileArray(area.size.x * area.size.y * area.size.z, Allocator.Temp);
        ///        for (int index = 0; index < tileArray.Length; index++)
        ///        {
        ///            tileArray[index] = index % 2 == 0 ? tileA : tileB;
        ///        }
        ///        Tilemap tilemap = GetComponent<Tilemap>();
        ///        tilemap.SetTilesBlock(area, tileArray);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public void SetTilesBlock(BoundsInt position, TileArray tileArray)
        {
            if (position.size.x * position.size.y * position.size.z != tileArray.Length)
                throw new ArgumentException("tileArray length must match the size of the bounds.");
            if (tileArray.Length == 0)
                return;
            Internal_SetTileAssetsBlock(position.min, position.size, tileArray.buffer);
        }

        ///<summary>Sets a <see cref="Tile" /> with additional properties at the given XYZ coordinates of a cell in the <see cref="Tilemap" />.</summary>
        ///<remarks>Refer to [Scriptable Tiles](xref:Tilemap-ScriptableTiles-TileBase) and [Tilemap](xref:class-Tilemap) for more information.</remarks>
        ///<param name="tileChangeData">The <see cref="Tile" /> with additional properties to be placed in the cell.</param>
        ///<param name="ignoreLockFlags">Whether to ignore Lock Flags set in the Tile's TileFlags when applying Color and Transform changes from <see cref="TileChangeData" />.</param>
        [NativeMethod(Name = "SetTileChangeData")]
        public extern void SetTile(TileChangeData tileChangeData, bool ignoreLockFlags);
        ///<summary>Sets an array of <see cref="Tile" />s with additonal properties at the given XYZ coordinates of the corresponding cells in the <see cref="Tilemap" />. The Color and Transform of the <see cref="TileChangeData" /> will take precedence over the values from the <see cref="Tile" />.</summary>
        ///<remarks>Refer to [Scriptable Tiles](xref:Tilemap-ScriptableTiles-TileBase) and [Tilemap](xref:class-Tilemap) for more information.</remarks>
        ///<param name="tileChangeDataArray">The array of <see cref="Tile" />s with additional properties to place.</param>
        ///<param name="ignoreLockFlags">Whether to ignore lock flags set in the tile's <c>TileFlags</c> when applying color and transform changes from <see cref="TileChangeData" />.</param>
        [NativeMethod(Name = "SetTileChangeDataArray")]
        public extern void SetTiles(TileChangeData[] tileChangeDataArray, bool ignoreLockFlags);

        ///<summary>Sets an array of <see cref="Tile" />s with additonal properties at the given XYZ coordinates of the corresponding cells in the <see cref="Tilemap" />. The color and transform of the corresponding arrays will take precedence over the values from the <see cref="Tile" />.</summary>
        ///<remarks>Refer to [Scriptable Tiles](xref:Tilemap-ScriptableTiles-TileBase) and [Tilemap](xref:class-Tilemap) for more information.</remarks>
        ///<param name="positionArray">An array of positions of tiles on the <see cref="Tilemap" />.</param>
        ///<param name="tileArray">The array of <see cref="Tile" />s to place.</param>
        ///<param name="colorArray">The array of colors for tiles on the <see cref="Tilemap" />.</param>
        ///<param name="transformArray">The array of transforms for tiles on the <see cref="Tilemap" />.</param>
        ///<param name="ignoreLockFlags">Whether to ignore lock flags set in the tile's <c>TileFlags</c> when applying color and transform changes from <see cref="TileChangeData" />.</param>
        ///<example>
        ///  <code><![CDATA[ // Fills tilemap area with checkerboard pattern of tileA and tileB
        ///using UnityEngine;
        ///using UnityEngine.Tilemaps;
        ///using Unity.Collections;
        ///
        ///public class ExampleClass_Native_Color_Transform : MonoBehaviour
        ///{
        ///    public TileBase tileA;
        ///    public TileBase tileB;
        ///    public Vector2Int size;
        ///
        ///    void Start()
        ///    {
        ///        NativeArray<Vector3Int> positions = new NativeArray<Vector3Int>(size.x * size.y, Allocator.Temp);
        ///        Tilemap.TileArray tileArray = new Tilemap.TileArray(positions.Length, Allocator.Temp);
        ///        NativeArray<Color> colorArray = new NativeArray<Color>(positions.Length, Allocator.Temp);
        ///        NativeArray<Matrix4x4> transformArray = new NativeArray<Matrix4x4>(positions.Length, Allocator.Temp);
        ///
        ///        for (int index = 0; index < positions.Length; index++)
        ///        {
        ///            positions[index] = new Vector3Int(index % size.x, index / size.y, 0);
        ///            tileArray[index] = index % 2 == 0 ? tileA : tileB;
        ///            colorArray[index] = index % 2 == 0 ? Color.white : Color.black;
        ///            transformArray[index] = Matrix4x4.identity;
        ///        }
        ///
        ///        Tilemap tilemap = GetComponent<Tilemap>();
        ///        tilemap.SetTiles(positions, tileArray, colorArray, transformArray, true);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public void SetTiles(NativeArray<Vector3Int> positionArray, TileArray tileArray, NativeArray<Color> colorArray, NativeArray<Matrix4x4> transformArray, bool ignoreLockFlags)
        {
            if (!positionArray.IsCreated
                || !colorArray.IsCreated
                || !transformArray.IsCreated
                || positionArray.Length != tileArray.Length
                || tileArray.Length != colorArray.Length
                || colorArray.Length != transformArray.Length)
            {
                throw new ArgumentException("All NativeArrays must be created and have the same length as tileArray.");
            }
            if (tileArray.Length == 0)
                return;

            unsafe
            {
                Internal_SetTileChangeDataArray(positionArray.m_Buffer
                , tileArray.buffer
                , colorArray.m_Buffer
                , transformArray.m_Buffer
                , ignoreLockFlags);
            }
        }

        ///<summary>Returns whether there is a <see cref="Tile" /> at the position.</summary>
        ///<param name="position">Position to check.</param>
        ///<returns>Returns true if there is a Tile at the position. Returns false otherwise.</returns>
        public bool HasTile(Vector3Int position)
        {
            return GetTileAsset(position) != null;
        }

        ///<summary>Refreshes a <see cref="Tile" /> at the given XYZ coordinates of a cell in the <see cref="Tilemap" />.</summary>
        ///<remarks>
        ///  <para>The [Tilemap](xref:class-Tilemap) will retrieve the rendering data, animation data and other data for the [Tile](xref:Tilemap-ScriptableTiles-TileBase) and update all relevant components.</para>
        ///  <para>Refer to [Scriptable Tiles](xref:Tilemap-ScriptableTiles-TileBase) and [Tilemap](xref:class-Tilemap) for more information.</para>
        ///</remarks>
        ///<param name="position">Position of the <see cref="Tile" /> on the <see cref="Tilemap" />.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using UnityEngine.Tilemaps;
        ///
        /// // Tile that displays a Sprite when it is alone and a different Sprite when it is orthogonally adjacent to the same NeighourTile
        ///[CreateAssetMenu]
        ///public class NeighbourTile : TileBase
        ///{
        ///    public Sprite spriteA;
        ///    public Sprite spriteB;
        ///
        ///    public override void RefreshTile(Vector3Int position, ITilemap tilemap)
        ///    {
        ///        for (int yd = -1; yd <= 1; yd++)
        ///        {
        ///            Vector3Int location = new Vector3Int(position.x, position.y + yd, position.z);
        ///            if (IsNeighbour(location, tilemap))
        ///                tilemap.RefreshTile(location);
        ///        }
        ///        for (int xd = -1; xd <= 1; xd++)
        ///        {
        ///            Vector3Int location = new Vector3Int(position.x + xd, position.y, position.z);
        ///            if (IsNeighbour(location, tilemap))
        ///                tilemap.RefreshTile(location);
        ///        }
        ///    }
        ///
        ///    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
        ///    {
        ///        tileData.sprite = spriteA;
        ///        for (int yd = -1; yd <= 1; yd += 2)
        ///        {
        ///            Vector3Int location = new Vector3Int(position.x, position.y + yd, position.z);
        ///            if (IsNeighbour(location, tilemap))
        ///                tileData.sprite = spriteB;
        ///        }
        ///        for (int xd = -1; xd <= 1; xd += 2)
        ///        {
        ///            Vector3Int location = new Vector3Int(position.x + xd, position.y, position.z);
        ///            if (IsNeighbour(location, tilemap))
        ///                tileData.sprite = spriteB;
        ///        }
        ///    }
        ///
        ///    private bool IsNeighbour(Vector3Int position, ITilemap tilemap)
        ///    {
        ///        TileBase tile = tilemap.GetTile(position);
        ///        return (tile != null && tile == this);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        [NativeMethod(Name = "RefreshTileAsset")]
        public extern void RefreshTile(Vector3Int position);

        [FreeFunction(Name = "TilemapBindings::RefreshTileAssetsNative", HasExplicitThis = true)]
        internal extern unsafe void RefreshTilesNative(void* positions, int count, bool needSortRemoveDup);

        ///<summary>Refreshes all <see cref="Tile" />s in the <see cref="Tilemap" />. The Tilemap will retrieve the rendering data, animation data and other data for all tiles and update all relevant components.</summary>
        ///<remarks>Refer to [Scriptable Tiles](xref:Tilemap-ScriptableTiles-TileBase) and [Tilemap](xref:class-Tilemap) for more information.</remarks>
        [NativeMethod(Name = "RefreshAllTileAssets")]
        public extern void RefreshAllTiles();

        internal extern void SwapTileAsset(Object changeTile, Object newTile);
        ///<summary>Swaps all existing <see cref="Tile" />s of **changeTile** to **newTile** and refreshes all the swapped <see cref="Tile" />s.</summary>
        ///<remarks>Refer to [Scriptable Tiles](xref:Tilemap-ScriptableTiles-TileBase) and [Tilemap](xref:class-Tilemap) for more information.</remarks>
        ///<param name="changeTile">Tile to swap.</param>
        ///<param name="newTile">Tile to swap to.</param>
        ///<example>
        ///  <code><![CDATA[
        /// // Change all occurences of tileA into tileB
        ///using UnityEngine;
        ///using UnityEngine.Tilemaps;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public TileBase tileA;
        ///    public TileBase tileB;
        ///
        ///    void Start()
        ///    {
        ///        Tilemap tilemap = GetComponent<Tilemap>();
        ///        tilemap.SwapTile(tileA, tileB);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public void SwapTile(TileBase changeTile, TileBase newTile) { SwapTileAsset(changeTile, newTile); }

        internal extern bool ContainsTileAsset(Object tileAsset);
        ///<summary>Returns true if the <see cref="Tilemap" /> contains the given <see cref="Tile" />. Returns false if not.</summary>
        ///<param name="tileAsset">Tile to check.</param>
        ///<returns>Whether the <see cref="Tilemap" /> contains the Tile.</returns>
        public bool ContainsTile(TileBase tileAsset) { return ContainsTileAsset(tileAsset); }

        ///<summary>Gets the total number of different <see cref="Tile" />s used in the <see cref="Tilemap" />.</summary>
        ///<returns>The total number of different <see cref="Tile" />s used in the <see cref="Tilemap" />.</returns>
        public extern int GetUsedTilesCount();

        ///<summary>Gets the total number of different <see cref="Sprite" />s used in the <see cref="Tilemap" />.</summary>
        ///<returns>The total number of different <see cref="Sprite" />s used in the <see cref="Tilemap" />.</returns>
        public extern int GetUsedSpritesCount();

        ///<summary>Fills the given array with the total number of different <see cref="Tile" />s used in the <see cref="Tilemap" /> and returns the number of <see cref="Tile" />s filled.</summary>
        ///<remarks>If the size of the given array is less than the total number of Tiles used in the Tilemap, this will try to fill the array as much as possible. Refer to [Scriptable Tiles](xref:Tilemap-ScriptableTiles-TileBase) and [Tilemap](xref:class-Tilemap) for more information.</remarks>
        ///<param name="usedTiles">The array to be filled.</param>
        ///<returns>The number of Tiles filled.</returns>
        public int GetUsedTilesNonAlloc(TileBase[] usedTiles)
        {
            return Internal_GetUsedTilesNonAlloc(usedTiles);
        }

        ///<summary>Fills the given array with the total number of different <see cref="Sprite" />s used in the <see cref="Tilemap" /> and returns the number of Sprites filled.</summary>
        ///<remarks>If the size of the given array is less than the total number of Sprites used in the Tilemap, this will try to fill the array as much as possible.</remarks>
        ///<param name="usedSprites">The array to be filled.</param>
        ///<returns>The number of Sprites filled.</returns>
        public int GetUsedSpritesNonAlloc(Sprite[] usedSprites)
        {
            return Internal_GetUsedSpritesNonAlloc(usedSprites);
        }

        [FreeFunction(Name = "TilemapBindings::GetUsedTilesNonAlloc", HasExplicitThis = true)]
        internal extern int Internal_GetUsedTilesNonAlloc([UnityMarshalAs(NativeType.ScriptingObjectPtr)] Object[] usedTiles);

        [FreeFunction(Name = "TilemapBindings::GetUsedSpritesNonAlloc", HasExplicitThis = true)]
        internal extern int Internal_GetUsedSpritesNonAlloc([UnityMarshalAs(NativeType.ScriptingObjectPtr)] Object[] usedSprites);

        ///<summary>Gets the <see cref="Sprite" /> used in a <see cref="Tile" /> given the XYZ coordinates of a cell in the Tilemap.</summary>
        ///<remarks>Refer to [Scriptable Tiles](xref:Tilemap-ScriptableTiles-TileBase) and [Tilemap](xref:class-Tilemap) for more information.</remarks>
        ///<param name="position">Position of the <see cref="Tile" /> on the <see cref="Tilemap" />.</param>
        ///<returns>
        ///  <see cref="Sprite" /> at the XYZ coordinate.</returns>
        public extern Sprite GetSprite(Vector3Int position);

        ///<summary>Gets the transform matrix of a <see cref="Tile" /> given the XYZ coordinates of a cell in the <see cref="Tilemap" />.</summary>
        ///<remarks>Refer to [Scriptable Tiles](xref:Tilemap-ScriptableTiles-TileBase) and [Tilemap](xref:class-Tilemap) for more information.</remarks>
        ///<param name="position">Position of the <see cref="Tile" /> on the <see cref="Tilemap" />.</param>
        ///<returns>The transform matrix.</returns>
        public extern Matrix4x4 GetTransformMatrix(Vector3Int position);
        ///<summary>Sets the transform matrix of a <see cref="Tile" /> given the XYZ coordinates of a cell in the <see cref="Tilemap" />.</summary>
        ///<remarks>
        ///  <para>Note that if the Tile has set <see cref="TileFlags.LockTransform" />, then this matrix has no effect.</para>
        ///  <para>Refer to [Scriptable Tiles](xref:Tilemap-ScriptableTiles-TileBase) and [Tilemap](xref:class-Tilemap) for more information.</para>
        ///</remarks>
        ///<param name="position">Position of the Tile on the <see cref="Tilemap" />.</param>
        ///<param name="transform">The transform matrix.</param>
        ///<example>
        ///  <code><![CDATA[
        /// // Rotate the tile in (0,0,0) 90 degrees
        ///using UnityEngine;
        ///using UnityEngine.Tilemaps;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        Tilemap tilemap = GetComponent<Tilemap>();
        ///        Matrix4x4 matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0f, 0f, 90f), Vector3.one);
        ///        tilemap.SetTransformMatrix(new Vector3Int(0, 0, 0), matrix);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public extern void SetTransformMatrix(Vector3Int position, Matrix4x4 transform);

        ///<summary>Gets the Color of a <see cref="Tile" /> given the XYZ coordinates of a cell in the <see cref="Tilemap" />.</summary>
        ///<remarks>Refer to [Scriptable Tiles](xref:Tilemap-ScriptableTiles-TileBase) and [Tilemap](xref:class-Tilemap) for more information.</remarks>
        ///<param name="position">Position of the Tile on the <see cref="Tilemap" />.</param>
        ///<returns>Color of the <see cref="Tile" /> at the XYZ coordinate.</returns>
        [NativeMethod(Name = "GetTileColor")]
        public extern Color GetColor(Vector3Int position);

        ///<summary>Sets the color of a <see cref="Tile" /> given the XYZ coordinates of a cell in the <see cref="Tilemap" />.</summary>
        ///<remarks>Refer to [Scriptable Tiles](xref:Tilemap-ScriptableTiles-TileBase) and [Tilemap](xref:class-Tilemap) for more information.</remarks>
        ///<param name="position">Position of the Tile on the <see cref="Tilemap" />.</param>
        ///<param name="color">Color to set the <see cref="Tile" /> to at the XYZ coordinate.</param>
        [NativeMethod(Name = "SetTileColor")]
        public extern void SetColor(Vector3Int position, Color color);

        ///<summary>Gets the <see cref="TileFlags" /> of the <see cref="Tile" /> at the given position.</summary>
        ///<param name="position">Position of the <see cref="Tile" /> on the <see cref="Tilemap" />.</param>
        ///<returns>
        ///  <see cref="TileFlags" /> from the <see cref="Tile" />.</returns>
        public extern TileFlags GetTileFlags(Vector3Int position);
        ///<summary>Sets the <see cref="TileFlags" /> onto the <see cref="Tile" /> at the given position.</summary>
        ///<remarks>Refer to [Scriptable Tiles](xref:Tilemap-ScriptableTiles-TileBase) and [Tilemap](xref:class-Tilemap) for more information.</remarks>
        ///<param name="position">Position of the Tile on the <see cref="Tilemap" />.</param>
        ///<param name="flags">
        ///  <see cref="TileFlags" /> to set onto the <see cref="Tile" />.</param>
        public extern void SetTileFlags(Vector3Int position, TileFlags flags);
        ///<summary>Adds the <see cref="TileFlags" /> onto the <see cref="Tile" /> at the given position.</summary>
        ///<param name="position">Position of the Tile on the <see cref="Tilemap" />.</param>
        ///<param name="flags">
        ///  <see cref="TileFlags" /> to add (with bitwise or) onto the flags provided by <see cref="Tile" />.<see cref="TileBase" />.</param>
        public extern void AddTileFlags(Vector3Int position, TileFlags flags);
        ///<summary>Removes the <see cref="TileFlags" /> from the <see cref="Tile" /> at the given position.</summary>
        ///<param name="position">Position of the Tile on the <see cref="Tilemap" />.</param>
        ///<param name="flags">
        ///  <see cref="TileFlags" /> to remove from the <see cref="Tile" />.</param>
        public extern void RemoveTileFlags(Vector3Int position, TileFlags flags);

        ///<summary>Gets the <see cref="GameObject" /> instantiated by a <see cref="Tile" /> given the XYZ coordinates of a cell in the <see cref="Tilemap" />.</summary>
        ///<remarks>Refer to [Scriptable Tiles](xref:Tilemap-ScriptableTiles-TileBase) and [Tilemap](xref:class-Tilemap) for more information.</remarks>
        ///<param name="position">Position of the Tile on the <see cref="Tilemap" />.</param>
        ///<returns>
        ///  <see cref="GameObject" /> instantiated by the <see cref="Tile" /> at the position.</returns>
        [NativeMethod(Name = "GetTileInstantiatedObject")]
        public extern GameObject GetInstantiatedObject(Vector3Int position);

        ///<summary>Gets the <see cref="GameObject" /> which will be instantiated by a <see cref="Tile" /> given the XYZ coordinates of a cell in the <see cref="Tilemap" />.</summary>
        ///<remarks>Refer to [Scriptable Tiles](xref:Tilemap-ScriptableTiles-TileBase) and [Tilemap](xref:class-Tilemap) for more information.</remarks>
        ///<param name="position">The position of the <see cref="Tile" /> on the <see cref="Tilemap" />.</param>
        ///<returns>Returns the <see cref="GameObject" /> to be instantiated by the <see cref="Tile" /> at the position.</returns>
        [NativeMethod(Name = "GetTileObjectToInstantiate")]
        public extern GameObject GetObjectToInstantiate(Vector3Int position);

        ///<summary>Sets the Collider type of a <see cref="Tile" /> given the XYZ coordinates of a cell in the <see cref="Tilemap" />.</summary>
        ///<remarks>Refer to [Scriptable Tiles](xref:Tilemap-ScriptableTiles-TileBase) and [Tilemap](xref:class-Tilemap) for more information.</remarks>
        ///<param name="position">Position of the Tile on the <see cref="Tilemap" />.</param>
        ///<param name="colliderType">Collider type to set the <see cref="Tile" /> to at the XYZ coordinate.</param>
        [NativeMethod(Name = "SetTileColliderType")]
        public extern void SetColliderType(Vector3Int position, Tile.ColliderType colliderType);
        ///<summary>Gets the Collider type of a <see cref="Tile" /> given the XYZ coordinates of a cell in the <see cref="Tilemap" />.</summary>
        ///<remarks>Refer to [Scriptable Tiles](xref:Tilemap-ScriptableTiles-TileBase) and [Tilemap](xref:class-Tilemap) for more information.</remarks>
        ///<param name="position">Position of the Tile on the <see cref="Tilemap" />.</param>
        ///<returns>Collider type of the <see cref="Tile" /> at the XYZ coordinate.</returns>
        [NativeMethod(Name = "GetTileColliderType")]
        public extern Tile.ColliderType GetColliderType(Vector3Int position);

        ///<summary>Retrieves the number of animation frames for a Tile at the given position.</summary>
        ///<remarks>Returns 0 if there is no animation for a Tile at the given position.</remarks>
        ///<param name="position">Grid cell position.</param>
        ///<returns>Returns the number of animation frames. Returns 0 when there is no animation for the Tile at the given position.</returns>
        [NativeMethod(Name = "GetTileAnimationFrameCount")]
        public extern int GetAnimationFrameCount(Vector3Int position);
        ///<summary>Retrieves the current animation frame for a Tile at the given position.</summary>
        ///<remarks>This method returns the current animation frame for a Tile at the given position. If there is no animation for the Tile at the given position, then this returns -1.</remarks>
        ///<param name="position">Grid cell position.</param>
        ///<returns>Returns the current animation frame. Returns -1 when there is no animation for the Tile at the given position.</returns>
        [NativeMethod(Name = "GetTileAnimationFrame")]
        public extern int GetAnimationFrame(Vector3Int position);
        ///<summary>Sets the current animation frame for a Tile at the given position.</summary>
        ///<remarks>Use this to set the running animation of a Tile at the given position to a particular frame of the animation or to synchronize the animation time between different Tiles. This will set the animation time of the Tile to the beginning of the animation frame.</remarks>
        ///<param name="position">The grid cell position.</param>
        ///<param name="frame">The animation frame to set to.</param>
        [NativeMethod(Name = "SetTileAnimationFrame")]
        public extern void SetAnimationFrame(Vector3Int position, int frame);

        ///<summary>Retrieves the current running animation time for a Tile at the given position.</summary>
        ///<param name="position">Grid cell position.</param>
        ///<returns>Returns the running animation time in seconds.</returns>
        [NativeMethod(Name = "GetTileAnimationTime")]
        public extern float GetAnimationTime(Vector3Int position);
        ///<summary>Sets the running animation time for a Tile at the given position.</summary>
        ///<remarks>Use this to set the running animation of a Tile at the given position to a particular frame of the animation or to synchronize the animation time between different Tiles.</remarks>
        ///<param name="position">The grid cell position.</param>
        ///<param name="time">The running animation time in seconds.</param>
        [NativeMethod(Name = "SetTileAnimationTime")]
        public extern void SetAnimationTime(Vector3Int position, float time);

        ///<summary>Gets the <see cref="TileAnimationFlags" /> of the <see cref="Tile" /> at the given position.</summary>
        ///<param name="position">The position of the Tile on the <see cref="Tilemap" />.</param>
        ///<returns>Returns the <see cref="TileAnimationFlags" /> of the <see cref="Tile" />.</returns>
        public extern TileAnimationFlags GetTileAnimationFlags(Vector3Int position);
        ///<summary>Sets the <see cref="TileAnimationFlags" /> onto the <see cref="Tile" /> at the given position.</summary>
        ///<remarks>Refer to [Scriptable Tiles](xref:Tilemap-ScriptableTiles-TileBase) and [Tilemap](xref:class-Tilemap) for more information.</remarks>
        ///<param name="position">The position of the Tile on the <see cref="Tilemap" />.</param>
        ///<param name="flags">The <see cref="TileAnimationFlags" /> to set onto the <see cref="Tile" />.</param>
        public extern void SetTileAnimationFlags(Vector3Int position, TileAnimationFlags flags);
        ///<summary>Adds the <see cref="TileAnimationFlags" /> onto the <see cref="Tile" /> at the given position.</summary>
        ///<param name="position">The position of the Tile on the <see cref="Tilemap" />.</param>
        ///<param name="flags">
        ///  <see cref="TileAnimationFlags" /> to add with bitwise OR (<c>|</c>) onto the flags provided by <see cref="Tile" />.<see cref="TileBase" />.</param>
        public extern void AddTileAnimationFlags(Vector3Int position, TileAnimationFlags flags);
        ///<summary>Removes the <see cref="TileAnimationFlags" /> from the <see cref="Tile" /> at the given position.</summary>
        ///<param name="position">The position of the Tile on the <see cref="Tilemap" />.</param>
        ///<param name="flags">The <see cref="TileAnimationFlags" /> to remove from the <see cref="Tile" />.</param>
        public extern void RemoveTileAnimationFlags(Vector3Int position, TileAnimationFlags flags);

        ///<summary>Does a flood fill with the given <see cref="Tile" /> to place. on the <see cref="Tilemap" /> starting from the given coordinates.</summary>
        ///<remarks>Refer to [Scriptable Tiles](xref:Tilemap-ScriptableTiles-TileBase) and [Tilemap](xref:class-Tilemap) for more information.</remarks>
        ///<param name="position">Start position of the flood fill on the <see cref="Tilemap" />.</param>
        ///<param name="tile">
        ///  <see cref="Tile" /> to place.</param>
        public void FloodFill(Vector3Int position, TileBase tile)
        {
            FloodFillTileAsset(position, tile);
        }

        [NativeMethod(Name = "FloodFill")]
        private extern void FloodFillTileAsset(Vector3Int position, Object tile);

        ///<summary>Does a box fill with the given <see cref="Tile" /> on the <see cref="Tilemap" />. Starts from given coordinates and fills the limits from start to end (inclusive).</summary>
        ///<remarks>If the limits are larger than the Tilemap bounds, the limits will be capped to the Tilemap bounds. Refer to [Scriptable Tiles](xref:Tilemap-ScriptableTiles-TileBase) and [Tilemap](xref:class-Tilemap) for more information.</remarks>
        ///<param name="position">Position of the <see cref="Tile" /> on the <see cref="Tilemap" />.</param>
        ///<param name="tile">
        ///  <see cref="Tile" /> to place.</param>
        ///<param name="startX">The minimum X coordinate limit to fill to.</param>
        ///<param name="startY">The minimum Y coordinate limit to fill to.</param>
        ///<param name="endX">The maximum X coordinate limit to fill to.</param>
        ///<param name="endY">The maximum Y coordinate limit to fill to.</param>
        public void BoxFill(Vector3Int position, TileBase tile, int startX, int startY, int endX, int endY)
        {
            BoxFillTileAsset(position, tile, startX, startY, endX, endY);
        }

        [NativeMethod(Name = "BoxFill")]
        private extern void BoxFillTileAsset(Vector3Int position, Object tile, int startX, int startY, int endX, int endY);

        ///<summary>Inserts cells into the <see cref="Tilemap" />.</summary>
        ///<param name="position">The target position to insert at.</param>
        ///<param name="insertCells">The number of columns, rows or layers of cells to insert.</param>
        public void InsertCells(Vector3Int position, Vector3Int insertCells)
        {
            InsertCells(position, insertCells.x, insertCells.y, insertCells.z);
        }

        ///<summary>Inserts cells into the <see cref="Tilemap" />.</summary>
        ///<param name="position">The target position to insert at.</param>
        ///<param name="numColumns">The number of columns to insert.</param>
        ///<param name="numRows">The number of rows to insert.</param>
        ///<param name="numLayers">The number of layers of cells to insert.</param>
        public extern void InsertCells(Vector3Int position, int numColumns, int numRows, int numLayers);

        ///<summary>Removes cells from within the <see cref="Tilemap" />'s bounds.</summary>
        ///<param name="position">The target position to remove from.</param>
        ///<param name="deleteCells">The number of columns, rows and layers of cells to remove.</param>
        public void DeleteCells(Vector3Int position, Vector3Int deleteCells)
        {
            DeleteCells(position, deleteCells.x, deleteCells.y, deleteCells.z);
        }

        ///<summary>Removes cells from within the <see cref="Tilemap" />'s bounds.</summary>
        ///<param name="position">Target position to delete from.</param>
        ///<param name="numColumns">The number of columns to remove.</param>
        ///<param name="numRows">The number of rows to remove.</param>
        ///<param name="numLayers">The number of layers of cells to remove.</param>
        public extern void DeleteCells(Vector3Int position, int numColumns, int numRows, int numLayers);

        ///<summary>Clears all <see cref="Tile" />s that are placed in the <see cref="Tilemap" />.</summary>
        ///<remarks>This will also resize the <see cref="Tilemap" /> to its default values.
        ///
        ///In the Editor, this will also clear all editor preview <see cref="Tile" />s.</remarks>
        public extern void ClearAllTiles();
        ///<summary>Resizes Tiles in the <see cref="Tilemap" /> to bounds defined by <see cref="origin" /> and <see cref="size" />.</summary>
        ///<remarks>Tiles outside of the bounds will be removed from the <see cref="Tilemap" />.</remarks>
        public extern void ResizeBounds();

        [NativeMethod(Name = "CompressBounds")]
        private extern void CompressTilemapBounds(bool keepEditorPreview);

        ///<summary>Compresses the <see cref="origin" /> and <see cref="size" /> of the <see cref="Tilemap" /> to bounds where <see cref="Tile" />s exist.</summary>
        public void CompressBounds() { CompressTilemapBounds(false); }

        internal void CompressBoundsKeepEditorPreview() { CompressTilemapBounds(true); }

        ///<summary>The origin of the <see cref="Tilemap" /> in cell position inclusive of editor preview Tiles.</summary>
        ///<remarks>This is used when rendering the <see cref="Tilemap" /> in Editor mode, taking both placed Tiles and editor preview Tiles for preview placement into account.</remarks>
        public extern Vector3Int editorPreviewOrigin
        {
            [NativeMethod(Name = "GetRenderOrigin")]
            get;
        }

        ///<summary>The size of the <see cref="Tilemap" /> in cells inclusive of editor preview Tiles.</summary>
        ///<remarks>This is used when rendering the <see cref="Tilemap" /> in Editor mode, taking both placed Tiles and editor preview Tiles for preview placement into account.</remarks>
        public extern Vector3Int editorPreviewSize
        {
            [NativeMethod(Name = "GetRenderSize")]
            get;
        }

        internal extern Object GetAnyTileAsset(Vector3Int position);
        internal TileBase GetAnyTile(Vector3Int position) { return GetAnyTileAsset(position) as TileBase; }
        internal T GetAnyTile<T>(Vector3Int position) where T : TileBase { return GetAnyTile(position) as T; }
        [NativeMethod(Name = "GetAnyTileAssetEntityId", IsThreadSafe = true)]
        internal extern EntityId GetAnyTileEntityId(Vector3Int position);

        [NativeMethod(Name = "GetAnyTileEntityIdFromHandle", IsThreadSafe = true)]
        internal static extern EntityId GetAnyTileEntityIdFromHandle(IntPtr tilemapHandle, Vector3Int position);

        [NativeMethod(Name = "GetAnyTileEntityIdsFromOffsets", IsThreadSafe = true)]
        private extern void GetAnyTileEntityIdsFromOffsets(Vector3Int position, IntPtr offsetsIntrPtr, IntPtr tilesIntPtr, int count);

        [NativeMethod(Name = "GetAnyTileEntityIdsFromOffsetsAndHandle", IsThreadSafe = true)]
        private static extern void GetAnyTileEntityIdsFromOffsetsAndHandle(IntPtr tilemapHandle, Vector3Int position, IntPtr offsetsIntrPtr, IntPtr tilesIntPtr, int count);

        [NativeMethod(Name = "GetAnyTileEntityIdsFromBlockOffset", IsThreadSafe = true)]
        private extern void GetAnyTileEntityIdsFromBlockOffset(Vector3Int position, BoundsInt blockOffset, IntPtr tilesIntPtr, int count);

        [NativeMethod(Name = "GetAnyTileEntityIdsFromBlockOffsetAndHandle", IsThreadSafe = true)]
        private static extern void GetAnyTileEntityIdsFromBlockOffsetAndHandle(IntPtr tilemapHandle, Vector3Int position, BoundsInt blockOffset, IntPtr tilesIntPtr, int count);

        internal extern Object GetEditorPreviewTileAsset(Vector3Int position);
        ///<summary>Gets the editor preview <see cref="Tile" /> at the given XYZ coordinates of a cell in the <see cref="Tilemap" />.</summary>
        ///<remarks>Refer to [Scriptable Tiles](xref:Tilemap-ScriptableTiles-TileBase) and [Tilemap](xref:class-Tilemap) for more information.</remarks>
        ///<param name="position">Position of the editor preview <see cref="Tile" /> on the <see cref="Tilemap" />.</param>
        ///<returns>The editor preview <see cref="Tile" /> placed at the cell.</returns>
        public TileBase GetEditorPreviewTile(Vector3Int position) { return GetEditorPreviewTileAsset(position) as TileBase; }
        ///<summary>Gets the editor preview <see cref="Tile" /> at the given XYZ coordinates of a cell in the <see cref="Tilemap" />.</summary>
        ///<remarks>Refer to [Scriptable Tiles](xref:Tilemap-ScriptableTiles-TileBase) and [Tilemap](xref:class-Tilemap) for more information.</remarks>
        ///<param name="position">Position of the editor preview Tile on the <see cref="Tilemap" />.</param>
        ///<returns>The editor preview <see cref="Tile" /> placed at the cell.</returns>
        public T GetEditorPreviewTile<T>(Vector3Int position) where T : TileBase { return GetEditorPreviewTile(position) as T; }

        ///<summary>Gets the EntityId of the editor preview <see cref="Tile" /> at the given xyz coordinates of a cell in the <see cref="Tilemap" />.</summary>
        ///<param name="position">The position of the editor preview <see cref="Tile" /> on the <see cref="Tilemap" />.</param>
        ///<returns>The EntityId of the editor preview <see cref="Tile" /> placed at the cell.</returns>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///using UnityEngine.Tilemaps;
        ///
        ///public static class TilemapExample
        ///{
        ///    public static void GetEditorPreviewTileEntityIdExample(Tilemap tilemap, Vector3Int position, Tile tile)
        ///    {
        ///        tilemap.SetEditorPreviewTile(position, tile);
        ///        var tileId = tilemap.GetEditorPreviewTileEntityId(position);
        ///        Debug.Log($"The ids for the Tile placed are equal ({(tileId == tile.GetEntityId()).ToString()})");
        ///    }
        ///}]]></code>
        ///</example>
        [NativeMethod(Name = "GetEditorPreviewTileAssetEntityId", IsThreadSafe = true)]
        public extern EntityId GetEditorPreviewTileEntityId(Vector3Int position);

        internal extern void SetEditorPreviewTileAsset(Vector3Int position, Object tile);
        ///<summary>Sets an editor preview <see cref="Tile" /> given the XYZ coordinates of a cell in the <see cref="Tilemap" />.</summary>
        ///<remarks>The editor preview Tile is used for previewing possible additions to the [Tilemap](xref:class-Tilemap).</remarks>
        ///<param name="position">Position of the editor preview Tile on the <see cref="Tilemap" />.</param>
        ///<param name="tile">The editor preview <see cref="Tile" /> to be placed the cell.</param>
        public void SetEditorPreviewTile(Vector3Int position, TileBase tile) { SetEditorPreviewTileAsset(position, tile); }

        ///<summary>Returns whether there is an editor preview Tile at the position.</summary>
        ///<param name="position">Position to check.</param>
        ///<returns>Returns true if there is an Editor Preview Tile at the position. Returns false otherwise.</returns>
        public bool HasEditorPreviewTile(Vector3Int position)
        {
            return GetEditorPreviewTileAsset(position) != null;
        }

        ///<summary>Gets the Sprite used in an editor preview <see cref="Tile" /> given the XYZ coordinates of a cell in the <see cref="Tilemap" />.</summary>
        ///<remarks>Refer to [Scriptable Tiles](xref:Tilemap-ScriptableTiles-TileBase) and [Tilemap](xref:class-Tilemap) for more information.</remarks>
        ///<param name="position">Position of the editor preview <see cref="Tile" /> on the <see cref="Tilemap" />.</param>
        ///<returns>Sprite at the XYZ coordinate.</returns>
        public extern Sprite GetEditorPreviewSprite(Vector3Int position);

        ///<summary>Gets the transform matrix of an editor preview <see cref="Tile" /> given the XYZ coordinates of a cell in the <see cref="Tilemap" />.</summary>
        ///<remarks>Refer to [Scriptable Tiles](xref:Tilemap-ScriptableTiles-TileBase) and [Tilemap](xref:class-Tilemap) for more information.</remarks>
        ///<param name="position">Position of the editor preview <see cref="Tile" /> on the <see cref="Tilemap" />.</param>
        ///<returns>The transform matrix.</returns>
        public extern Matrix4x4 GetEditorPreviewTransformMatrix(Vector3Int position);
        ///<summary>Sets the transform matrix of an editor preview <see cref="Tile" /> given the XYZ coordinates of a cell in the <see cref="Tilemap" />.</summary>
        ///<remarks>The editor preview Tile is used for previewing possible additions to the [Tilemap](xref:class-Tilemap).</remarks>
        ///<param name="position">Position of the editor preview Tile on the <see cref="Tilemap" />.</param>
        ///<param name="transform">The transform matrix.</param>
        public extern void SetEditorPreviewTransformMatrix(Vector3Int position, Matrix4x4 transform);

        ///<summary>Gets the Color of an editor preview <see cref="Tile" /> given the XYZ coordinates of a cell in the  <see cref="Tilemap" />.</summary>
        ///<remarks>Refer to [Scriptable Tiles](xref:Tilemap-ScriptableTiles-TileBase) and [Tilemap](xref:class-Tilemap) for more information.</remarks>
        ///<param name="position">Position of the <see cref="Tile" /> on the <see cref="Tilemap" />.</param>
        ///<returns>Color of the editor preview <see cref="Tile" /> at the XYZ coordinate.</returns>
        [NativeMethod(Name = "GetEditorPreviewTileColor")]
        public extern Color GetEditorPreviewColor(Vector3Int position);
        ///<summary>Sets the color of an editor preview <see cref="Tile" /> given the XYZ coordinates of a cell in the <see cref="Tilemap" />.</summary>
        ///<remarks>The editor preview Tile is used for previewing possible additions to the [Tilemap](xref:class-Tilemap).</remarks>
        ///<param name="position">Position of the editor preview Tile on the <see cref="Tilemap" />.</param>
        ///<param name="color">Color to set the editor preview <see cref="Tile" /> to at the XYZ coordinate.</param>
        [NativeMethod(Name = "SetEditorPreviewTileColor")]
        public extern void SetEditorPreviewColor(Vector3Int position, Color color);

        ///<summary>Gets the <see cref="TileFlags" /> of the editor preview <see cref="Tile" /> at the given position.</summary>
        ///<remarks>Refer to [Scriptable Tiles](xref:Tilemap-ScriptableTiles-TileBase) and [Tilemap](xref:class-Tilemap) for more information.</remarks>
        ///<param name="position">Position of the <see cref="Tile" /> on the <see cref="Tilemap" />.</param>
        ///<returns>
        ///  <see cref="TileFlags" /> from the editor preview <see cref="Tile" />.</returns>
        public extern TileFlags GetEditorPreviewTileFlags(Vector3Int position);

        ///<summary>Does an editor preview of a flood fill with the given <see cref="Tile" /> to place. on the <see cref="Tilemap" /> starting from the given coordinates.</summary>
        ///<remarks>The editor preview is used for previewing possible additions to the <see cref="Tilemap" />. Refer to [Scriptable Tiles](xref:Tilemap-ScriptableTiles-TileBase) and [Tilemap](xref:class-Tilemap) for more information.</remarks>
        ///<param name="position">Start position of the flood fill on the <see cref="Tilemap" />.</param>
        ///<param name="tile">
        ///  <see cref="TileBase" /> to place.</param>
        public void EditorPreviewFloodFill(Vector3Int position, TileBase tile)
        {
            EditorPreviewFloodFillTileAsset(position, tile);
        }

        [NativeMethod(Name = "EditorPreviewFloodFill")]
        private extern void EditorPreviewFloodFillTileAsset(Vector3Int position, Object tile);

        ///<summary>Does an editor preview of a box fill with the given <see cref="Tile" /> on the <see cref="Tilemap" />. Starts from given coordinates and fills the limits from start to end (inclusive).</summary>
        ///<remarks>The editor preview is used for previewing possible additions to the <see cref="Tilemap" />. Refer to [Scriptable Tiles](xref:Tilemap-ScriptableTiles-TileBase) and [Tilemap](xref:class-Tilemap) for more information.</remarks>
        ///<param name="position">Position of the <see cref="Tile" /> on the <see cref="Tilemap" />.</param>
        ///<param name="tile">
        ///  <see cref="Tile" /> to place.</param>
        ///<param name="startX">The start X coordinate limit to fill to.</param>
        ///<param name="startY">The start Y coordinate limit to fill to.</param>
        ///<param name="endX">The ending X coordinate limit to fill to.</param>
        ///<param name="endY">The ending Y coordinate limit to fill to.</param>
        public void EditorPreviewBoxFill(Vector3Int position, Object tile, int startX, int startY, int endX, int endY)
        {
            EditorPreviewBoxFillTileAsset(position, tile, startX, startY, endX, endY);
        }

        [NativeMethod(Name = "EditorPreviewBoxFill")]
        private extern void EditorPreviewBoxFillTileAsset(Vector3Int position, Object tile, int startX, int startY, int endX, int endY);

        ///<summary>Clears all editor preview <see cref="Tile" />s that are placed in the <see cref="Tilemap" />.</summary>
        [NativeMethod(Name = "ClearAllEditorPreviewTileAssets")]
        public extern void ClearAllEditorPreviewTiles();

        [RequiredByNativeCode]
        private ITilemap GetITilemapProxy()
        {
            return ITilemap.CreateInstanceFromTilemap(this);
        }

        [RequiredByNativeCode]
        internal void GetLoopEndedForTileAnimationCallbackSettings(ref bool hasEndLoopForTileAnimationCallback)
        {
            hasEndLoopForTileAnimationCallback = HasLoopEndedForTileAnimationCallback();
        }

        [RequiredByNativeCode]
        private void DoLoopEndedForTileAnimationCallback(int count, IntPtr positionsIntPtr)
        {
            HandleLoopEndedForTileAnimationCallback(count, positionsIntPtr);
        }

        ///<summary>A Struct for containing changes to a <see cref="Tile" /> when it has been changed on a <see cref="Tilemap" />.</summary>
        [RequiredByNativeCode]
        [StructLayout(LayoutKind.Sequential)]
        [NativeHeader("Modules/Tilemap/Public/TilemapTile.h")]
        public struct SyncTile
        {
            internal Vector3Int m_Position;
            internal EntityId m_TileId;
            internal TileData m_TileData;

            ///<summary>The position of the <see cref="Tile" /> on a <see cref="Tilemap" /> which has changed.</summary>
            public Vector3Int position => m_Position;

            ///<summary>The properties of the <see cref="Tile" /> at the given position on the <see cref="Tilemap" />.</summary>
            ///<remarks>This is invalid if there is no <see cref="Tile" /> at that position on the <see cref="Tilemap" />.</remarks>
            public TileData tileData => m_TileData;

            ///<summary>Whether there is a <see cref="Tile" /> at the given position on the <see cref="Tilemap" />.</summary>
            ///<remarks>This is faster than checking <see cref="tile" /> against null, as it does not resolve the <see cref="Tile" />.</remarks>
            public bool hasTile => Resources.EntityIdIsValid(m_TileId);

            ///<summary>The <see cref="Tile" /> at the given position on the <see cref="Tilemap" />.</summary>
            ///<remarks>This is null if a <see cref="Tile" /> has been removed from the <see cref="Tilemap" />.</remarks>
            public TileBase tile => Resources.EntityIdIsValid(m_TileId) ? Resources.EntityIdToObject(m_TileId) as TileBase : null;
        }

        internal struct SyncTileCallbackSettings
        {
            internal bool hasSyncTileCallback;
            internal bool hasPositionsChangedCallback;
            internal bool hasTilesChangedCallback;
            internal bool isBufferSyncTile;
        }

        [RequiredByNativeCode]
        internal void GetSyncTileCallbackSettings(ref SyncTileCallbackSettings settings)
        {
            settings.hasSyncTileCallback = HasSyncTileCallback();
            settings.hasPositionsChangedCallback = HasPositionsChangedCallback();
            settings.hasTilesChangedCallback = HasTilesChangedCallback();
            settings.isBufferSyncTile = bufferSyncTile;
        }

        internal extern void SendAndClearSyncTileBuffer();

        [RequiredByNativeCode]
        private void DoSyncTileCallback(SyncTile[] syncTiles)
        {
            HandleSyncTileCallback(syncTiles);
        }

        [RequiredByNativeCode]
        private void DoPositionsChangedCallback(int count, IntPtr positionsIntPtr)
        {
            HandlePositionsChangedCallback(count, positionsIntPtr);
        }

        // The engine reports both kinds of reason in one word, so each conversion keeps only what its own enum names.
        private const int k_NativeTilemapReasonMask = 0x00000007;
        private const int k_NativeTileEverything = 1 << 16;
        private const int k_NativeTileSprite = 1 << 17;
        private const int k_NativeTileSpriteAnimation = 1 << 18;
        private const int k_NativeTileTransform = 1 << 19;
        private const int k_NativeTileColor = 1 << 20;
        private const int k_NativeTileFlags = 1 << 21;
        private const int k_NativeTileColliderType = 1 << 23;
        private const int k_NativeEditorPreview = 1 << 31;

        private static TilemapChangeReason ToTilemapChangeReason(int nativeReason)
        {
            var reason = (TilemapChangeReason)(nativeReason & k_NativeTilemapReasonMask);
            if ((nativeReason & k_NativeEditorPreview) != 0)
                reason |= TilemapChangeReason.EditorPreview;
            return reason;
        }

        private static TileChangeReason ToTileChangeReason(int nativeReason)
        {
            TileChangeReason reason = 0;
            // A changed sprite is folded in: on its own it says no more than that the tile is not what it was.
            if ((nativeReason & (k_NativeTileEverything | k_NativeTileSprite)) != 0)
                reason |= TileChangeReason.Replaced;
            if ((nativeReason & k_NativeTileSpriteAnimation) != 0)
                reason |= TileChangeReason.SpriteAnimation;
            if ((nativeReason & k_NativeTileTransform) != 0)
                reason |= TileChangeReason.Transform;
            if ((nativeReason & k_NativeTileColor) != 0)
                reason |= TileChangeReason.Color;
            if ((nativeReason & k_NativeTileFlags) != 0)
                reason |= TileChangeReason.Flags;
            if ((nativeReason & k_NativeTileColliderType) != 0)
                reason |= TileChangeReason.ColliderType;
            return reason;
        }

        [RequiredByNativeCode]
        private void DoTilesChangedCallback(int count, IntPtr entriesIntPtr, int reason)
        {
            HandleTilesChangedCallback(count, entriesIntPtr, ToTileChangeReason(reason));
        }

        [RequiredByNativeCode]
        private void DoTilemapChangedCallback(int reason)
        {
            HandleTilemapChangedCallback(ToTilemapChangeReason(reason));
        }

        #region Non Allocating Getters

        [StructLayout(LayoutKind.Sequential)]
        internal struct TilemapBuffer : IDisposable
        {
            public readonly IntPtr buffer => m_Buffer;
            public readonly int length => m_Length;
            public readonly Allocator allocator => m_Allocator;

            public TilemapBuffer()
            {
                m_Buffer = IntPtr.Zero;
                m_Length = 0;
                m_Allocator = Allocator.None;
            }

            public unsafe TilemapBuffer(IntPtr buffer, int length)
            {
                m_Buffer = buffer;
                m_Length = length;
                m_Allocator = Allocator.None;
            }

            public unsafe readonly T AsEngineObject<T>(int index) where T : class
            {
                if (index < 0 || index >= m_Length)
                    throw new ArgumentOutOfRangeException("index");

                var entityId = UnsafeUtility.ArrayElementAsRef<EntityId>(m_Buffer.ToPointer(), index);
                return Resources.EntityIdIsValid(entityId) ? Resources.EntityIdToObject(entityId) as T : null;
            }

            public unsafe readonly T As<T>(int index) where T : struct
            {
                if (index < 0 || index >= m_Length)
                    throw new ArgumentOutOfRangeException("index");

                return UnsafeUtility.ArrayElementAsRef<T>(m_Buffer.ToPointer(), index);
            }

            public unsafe void SetEngineObject<T>(T value, int index) where T : UnityEngine.Object
            {
                if (index < 0 || index >= m_Length)
                    throw new ArgumentOutOfRangeException("index");

                var entityId = EntityId.None;
                if (value != null)
                    entityId = value.GetEntityId();
                UnsafeUtility.ArrayElementAsRef<EntityId>(m_Buffer.ToPointer(), index) = entityId;
            }

            public unsafe void SetValue<T>(T value, int index) where T : struct
            {
                if (index < 0 || index >= m_Length)
                    throw new ArgumentOutOfRangeException("index");

                UnsafeUtility.ArrayElementAsRef<T>(m_Buffer.ToPointer(), index) = value;
            }

            public unsafe void Dispose()
            {
                if (m_Buffer == null || m_Length == 0)
                    return;

                // Free the allocation.
                if (m_Allocator != Allocator.None)
                    UnsafeUtility.FreeTracked(m_Buffer.ToPointer(), m_Allocator);

                m_Buffer = IntPtr.Zero;
                m_Length = 0;
                m_Allocator = Allocator.None;
            }

            #region Internal

            IntPtr m_Buffer;
            int m_Length;
            Allocator m_Allocator;

            #endregion
        }

        ///<summary>A read-only array containing Tile <see cref="EntityId" />s.</summary>
        ///<remarks>This can be accessed by index to return a <see cref="TileBase" />.</remarks>
        [StructLayout(LayoutKind.Sequential)]
        public struct TileArray : IEnumerable<TileBase>, IDisposable
        {
            internal struct TileArrayEnumerator : IEnumerator<TileBase>
            {
                TileArray m_TileArray;
                int m_Index;

                public TileArrayEnumerator(TileArray tileArray)
                {
                    m_TileArray = tileArray;
                    m_Index = -1;
                }

                TileBase IEnumerator<TileBase>.Current => m_TileArray[m_Index];

                object IEnumerator.Current => m_TileArray[m_Index];

                void IDisposable.Dispose()
                {
                    // Does not own the buffer, so nothing to dispose
                }

                bool IEnumerator.MoveNext()
                {
                    if (m_TileArray.Length == 0)
                        return false;

                    return ++m_Index < m_TileArray.Length;
                }

                void IEnumerator.Reset()
                {
                    m_Index = -1;
                }
            }

            ///<summary>Creates a new array and allocates enough memory to fit the provided number of elements.</summary>
            ///<param name="length">The number of elements to allocate.</param>
            ///<param name="allocator">The <see cref="Unity.Collections.Allocator" /> to use for the data.</param>
            public TileArray(int length, Allocator allocator)
            {
                if (allocator != Allocator.Temp && allocator != Allocator.Persistent && allocator != Allocator.Domain)
                    throw new ArgumentException(k_TilemapAllocationArgumentExceptionMessage);

                if (length <= 0)
                {
                    m_TilemapBuffer = default;
                    m_Allocator = Allocator.None;
                    m_MemoryLabel = default;
                    return;
                }

                unsafe
                {
                    int size = UnsafeUtility.SizeOf(typeof(EntityId));
                    var buffer = UnsafeUtility.MallocTracked(length * size, size, allocator, 1);
                    UnsafeUtility.MemClear(buffer, length * size);
                    m_TilemapBuffer = new TilemapBuffer((IntPtr)buffer, length);
                }
                m_Allocator = allocator;
                m_MemoryLabel = default;
            }

            ///<summary>Creates a new array and allocates enough memory to fit the provided number of elements, using the specified memory label.</summary>
            ///<param name="length">The number of elements to allocate.</param>
            ///<param name="memoryLabel">The <see cref="Unity.Collections.MemoryLabel" /> to allocate under.</param>
            public TileArray(int length, MemoryLabel memoryLabel)
            {
                if (length <= 0)
                {
                    m_TilemapBuffer = default;
                    m_Allocator = Allocator.None;
                    m_MemoryLabel = default;
                    return;
                }

                unsafe
                {
                    int size = UnsafeUtility.SizeOf(typeof(EntityId));
                    var buffer = UnsafeUtility.MallocTracked(length * size, size, memoryLabel, 1);
                    UnsafeUtility.MemClear(buffer, length * size);
                    m_TilemapBuffer = new TilemapBuffer((IntPtr) buffer, length);
                }
                m_Allocator = Allocator.None;
                m_MemoryLabel = memoryLabel;
            }

            internal TileArray(TilemapBuffer tilemapBuffer)
            {
                m_TilemapBuffer = tilemapBuffer;
                m_Allocator = Allocator.None;
                m_MemoryLabel = default;
            }

            ///<summary>The number of elements in the <see cref="TileArray" />.</summary>
            public int Length => m_TilemapBuffer.length;

            ///<summary>Returns the <see cref="TileBase" /> indexed in the <see cref="TileArray" />.</summary>
            public TileBase this[int index]
            {
                get => m_TilemapBuffer.AsEngineObject<TileBase>(index);
                set
                {
                    m_TilemapBuffer.SetEngineObject<TileBase>(value, index);
                }
            }

            #region Enumeration

            ///<summary>Retrieves an iterator that allows you to iterate over all elements within the <see cref="TileArray" />.</summary>
            ///<returns>An iterator that allows you to iterate over all elements within the <see cref="TileArray" />.</returns>
            public readonly IEnumerator<TileBase> GetEnumerator() => new TileArrayEnumerator(this);
            readonly IEnumerator IEnumerable.GetEnumerator() => new TileArrayEnumerator(this);

            ///<summary>Frees allocated memory for the <see cref="TileArray" />.</summary>
            public void Dispose()
            {
                if (m_MemoryLabel.IsCreated)
                {
                    unsafe
                    {
                        UnsafeUtility.FreeTracked((void*)m_TilemapBuffer.buffer, m_MemoryLabel);
                    }
                }
                else if (m_Allocator != Allocator.None)
                {
                    unsafe
                    {
                        UnsafeUtility.FreeTracked((void*)m_TilemapBuffer.buffer, m_Allocator);
                    }
                }
                m_TilemapBuffer.Dispose();
            }

            #endregion

            #region Internal

            TilemapBuffer m_TilemapBuffer;
            Allocator m_Allocator;
            MemoryLabel m_MemoryLabel;

            internal readonly TilemapBuffer buffer => m_TilemapBuffer;

            #endregion
        }

        ///<summary>A read-only array containing Sprite <see cref="EntityId" />s.</summary>
        ///<remarks>This can be accessed by index to return a <see cref="Sprite" />.</remarks>
        [StructLayout(LayoutKind.Sequential)]
        public struct SpriteArray : IEnumerable<Sprite>, IDisposable
        {
            internal struct SpriteArrayEnumerator : IEnumerator<Sprite>
            {
                SpriteArray m_SpriteArray;
                int m_Index;

                public SpriteArrayEnumerator(SpriteArray spriteArray)
                {
                    m_SpriteArray = spriteArray;
                    m_Index = -1;
                }

                Sprite IEnumerator<Sprite>.Current => m_SpriteArray[m_Index];

                object IEnumerator.Current => m_SpriteArray[m_Index];

                void IDisposable.Dispose()
                {
                    // Does not own the buffer, so nothing to dispose
                }

                bool IEnumerator.MoveNext()
                {
                    if (m_SpriteArray.Length == 0)
                        return false;

                    return ++m_Index < m_SpriteArray.Length;
                }

                void IEnumerator.Reset()
                {
                    m_Index = -1;
                }
            }

            internal SpriteArray(TilemapBuffer tilemapBuffer)
            {
                m_TilemapBuffer = tilemapBuffer;
            }

            ///<summary>The number of elements in the <see cref="SpriteArray" />.</summary>
            public readonly int Length => m_TilemapBuffer.length;
            ///<summary>Returns the <see cref="Sprite" /> indexed in the <see cref="SpriteArray" />.</summary>
            public readonly Sprite this[int index] => m_TilemapBuffer.AsEngineObject<Sprite>(index);

            #region Enumeration

            ///<summary>Retrieves an iterator that allows you to iterate over all elements within the <see cref="SpriteArray" />.</summary>
            ///<returns>An iterator that allows you to iterate over all elements within the <see cref="SpriteArray" />.</returns>
            public readonly IEnumerator<Sprite> GetEnumerator() => new SpriteArrayEnumerator(this);
            readonly IEnumerator IEnumerable.GetEnumerator() => new SpriteArrayEnumerator(this);

            ///<summary>Frees allocated memory for the <see cref="SpriteArray" />.</summary>
            public void Dispose() => m_TilemapBuffer.Dispose();

            #endregion

            #region Internal

            TilemapBuffer m_TilemapBuffer;

            #endregion
        }

        ///<summary>A read-only array containing Tilemap positions.</summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct PositionArray : IEnumerable<Vector3Int>, IDisposable
        {
            internal struct PositionArrayEnumerator : IEnumerator<Vector3Int>
            {
                PositionArray m_PositionArray;
                int m_Index;

                public PositionArrayEnumerator(PositionArray positionArray)
                {
                    m_PositionArray = positionArray;
                    m_Index = -1;
                }

                Vector3Int IEnumerator<Vector3Int>.Current => m_PositionArray[m_Index];

                object IEnumerator.Current => m_PositionArray[m_Index];

                void IDisposable.Dispose()
                {
                    // Does not own the buffer, so nothing to dispose
                }

                bool IEnumerator.MoveNext()
                {
                    if (m_PositionArray.Length == 0)
                        return false;

                    return ++m_Index < m_PositionArray.Length;
                }

                void IEnumerator.Reset()
                {
                    m_Index = -1;
                }
            }

            internal PositionArray(TilemapBuffer tilemapBuffer)
            {
                m_TilemapBuffer = tilemapBuffer;
            }

            ///<summary>The number of elements in the <see cref="PositionArray" />.</summary>
            public readonly int Length => m_TilemapBuffer.length;
            ///<summary>Returns the <see cref="Vector3Int" /> position indexed in the <see cref="PositionArray" />.</summary>
            public readonly Vector3Int this[int index] => m_TilemapBuffer.As<Vector3Int>(index);

            #region Enumeration

            ///<summary>Retrieves an iterator that allows you to iterate over all elements within the <see cref="PositionArray" />.</summary>
            ///<returns>An iterator that allows you to iterate over all elements within the <see cref="PositionArray" />.</returns>
            public readonly IEnumerator<Vector3Int> GetEnumerator() => new PositionArrayEnumerator(this);
            readonly IEnumerator IEnumerable.GetEnumerator() => new PositionArrayEnumerator(this);
            ///<summary>Frees allocated memory for the <see cref="PositionArray" />.</summary>
            public void Dispose() => m_TilemapBuffer.Dispose();

            #endregion

            #region Internal

            TilemapBuffer m_TilemapBuffer;

            #endregion
        }

        private const string k_TilemapAllocationArgumentExceptionMessage = "Allocator must be 'Temp', 'Domain' or `Persistent`";

        ///<summary>Returns a <see cref="TileArray" /> containing the unique <see cref="Tile" /> instances used in this <see cref="Tilemap" />. The array is allocated using the given Allocator.</summary>
        ///<remarks>The <see cref="Allocator" /> must be either <see cref="Allocator.Temp" />, <see cref="Allocator.Domain" /> or <see cref="Allocator.Persistent" />.</remarks>
        ///<param name="allocator">The allocator type used to allocate the memory for the <see cref="SpriteArray" />. The default value is <see cref="Allocator.Temp" />.</param>
        ///<returns>A <see cref="TileArray" /> containing the all unique <see cref="Tile" /> instances used in the <see cref="Tilemap" />.</returns>
        ///<example>
        ///  <code><![CDATA[ // Retrieves all used Tiles from a Tilemap and prints out the Tile names to console
        ///using UnityEngine;
        ///using UnityEngine.Tilemaps;
        ///
        ///public class TilemapExample1 : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        Tilemap tilemap = GetComponent<Tilemap>();
        ///        using var usedTiles = tilemap.GetUsedTiles(); // Will call TileArray.Dispose() once it is out of scope
        ///        foreach (var tile in usedTiles)
        ///        {
        ///            print(tile.name);
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public TileArray GetUsedTiles(Allocator allocator = Allocator.Temp)
        {
            if (allocator == Allocator.Temp || allocator == Allocator.Persistent || allocator == Allocator.Domain)
                return new(Internal_GetUsedTiles(allocator, IntPtr.Zero));

            throw new ArgumentException(k_TilemapAllocationArgumentExceptionMessage);
        }

        ///<summary>Returns a <see cref="TileArray" /> allocated by the given <c>MemoryLabel</c> with the unique <see cref="Tile" />s used in the <see cref="Tilemap" />.</summary>
        ///<param name="memoryLabel">Memory label used for profiling and tracking this memory allocation in Unity.</param>
        ///<returns>
        ///  <see cref="TileArray" /> containing the unique <see cref="Tile" />s used in the <see cref="Tilemap" />.</returns>
        ///<example>
        ///  <code><![CDATA[ // Retrieves all used Tiles from a Tilemap and prints out the Tile names to console
        ///using Unity.Collections;
        ///using UnityEngine;
        ///using UnityEngine.Tilemaps;
        ///
        ///public class TilemapExample2 : MonoBehaviour
        ///{
        ///    static readonly MemoryLabel kMemoryLabel = new MemoryLabel("TilemapExample", "Get", Allocator.Domain);
        ///
        ///    void Start()
        ///    {
        ///        Tilemap tilemap = GetComponent<Tilemap>();
        ///        using var usedTiles = tilemap.GetUsedTiles(kMemoryLabel); // Will call TileArray.Dispose() once it is out of scope
        ///        foreach (var tile in usedTiles)
        ///        {
        ///            print(tile.name);
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public TileArray GetUsedTiles(MemoryLabel memoryLabel)
        {
            // Memory Label Allocators must be Persistent or Domain
            return new(Internal_GetUsedTiles_MemoryLabel(memoryLabel));
        }

        ///<summary>Returns a <see cref="SpriteArray" /> containing the unique <see cref="Sprite" /> instances used in the <see cref="Tilemap" />. The array is allocated using the given Allocator.</summary>
        ///<remarks>The Allocator must be either <see cref="Allocator.Temp" /> or <see cref="Allocator.Persistent" />.</remarks>
        ///<param name="allocator">The allocator type used to allocate the memory for the <see cref="SpriteArray" />. The default value is <see cref="Allocator.Temp" />.</param>
        ///<returns>A <see cref="SpriteArray" /> containing the all unique <see cref="Sprite" /> assets used in the <see cref="Tilemap" />.</returns>
        ///<example>
        ///  <code><![CDATA[ // Retrieves all used Sprites from a Tilemap and prints out the Sprite names to console
        ///using UnityEngine;
        ///using UnityEngine.Tilemaps;
        ///
        ///public class TilemapExample1 : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        Tilemap tilemap = GetComponent<Tilemap>();
        ///        using var usedSprites = tilemap.GetUsedSprites(); // Will call SpriteArray.Dispose() once it is out of scope
        ///        foreach (var sprite in usedSprites)
        ///        {
        ///            print(sprite.name);
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public SpriteArray GetUsedSprites(Allocator allocator = Allocator.Temp)
        {
            if (allocator == Allocator.Temp || allocator == Allocator.Persistent || allocator == Allocator.Domain)
                return new(Internal_GetUsedSprites(allocator, IntPtr.Zero));

            throw new ArgumentException(k_TilemapAllocationArgumentExceptionMessage);
        }

        ///<summary>Returns a <see cref="SpriteArray" /> containing the unique <see cref="Sprite" /> instances used in the <see cref="Tilemap" />. The array is allocated using the given Allocator.</summary>
        ///<param name="memoryLabel">Memory label used for profiling and tracking this memory allocation in Unity.</param>
        ///<returns>A <see cref="SpriteArray" /> containing the all unique <see cref="Sprite" /> assets used in the <see cref="Tilemap" />.</returns>
        ///<example>
        ///  <code><![CDATA[ // Retrieves all used Sprites from a Tilemap and prints out the Sprite names to console
        ///using Unity.Collections;
        ///using UnityEngine;
        ///using UnityEngine.Tilemaps;
        ///
        ///public class TilemapExample2 : MonoBehaviour
        ///{
        ///    static readonly MemoryLabel kMemoryLabel = new MemoryLabel("TilemapExample", "Get", Allocator.Domain);
        ///
        ///    void Start()
        ///    {
        ///        Tilemap tilemap = GetComponent<Tilemap>();
        ///        using var usedSprites = tilemap.GetUsedSprites(kMemoryLabel); // Will call SpriteArray.Dispose() once it is out of scope
        ///        foreach (var sprite in usedSprites)
        ///        {
        ///            print(sprite.name);
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public SpriteArray GetUsedSprites(MemoryLabel memoryLabel)
        {
            // Memory Label Allocators are Persistent or Domain
            return new(Internal_GetUsedSprites_MemoryLabel(memoryLabel));
        }

        ///<summary>Retrieves all tiles within the given bounds as a <see cref="TileArray" />.</summary>
        ///<remarks>Use this method to efficiently retrieve tiles as a batch, rather than calling <see cref="GetTile" /> for each position. This can significantly reduce overhead when processing large areas.</remarks>
        ///<param name="bounds">The bounds from which to retrieve the tiles.</param>
        ///<param name="allocator">The <see cref="Allocator" /> type used to allocate the memory for the <see cref="TileArray" />.  You must use <see cref="Allocator.Temp" />, <see cref="Allocator.Domain" />, or <see cref="Allocator.Persistent" />. The default value is <see cref="Allocator.Temp" />.</param>
        ///<returns>A <see cref="TileArray" /> containing all the <see cref="TileBase" /> instances in the bounds.</returns>
        ///<example>
        ///  <code><![CDATA[ // Retrieves all Tiles from an area on the Tilemap and prints out the Tiles to console
        ///using UnityEngine;
        ///using UnityEngine.Tilemaps;
        ///
        ///public class TilemapExample1 : MonoBehaviour
        ///{
        ///    public BoundsInt area;
        ///
        ///    void GetTilesExample1()
        ///    {
        ///        Tilemap tilemap = GetComponent<Tilemap>();
        ///        using var tiles = tilemap.GetTiles(area); // Will call TileArray.Dispose() once it is out of scope
        ///        foreach (var tile in tiles)
        ///        {
        ///            print(tile != null ? tile.name : "Empty"); 
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public TileArray GetTiles(BoundsInt bounds, Allocator allocator = Allocator.Temp)
        {
            if (allocator == Allocator.Temp || allocator == Allocator.Persistent || allocator == Allocator.Domain)
                return new(Internal_GetTiles(bounds.min, bounds.size, allocator, IntPtr.Zero));

            throw new ArgumentException(k_TilemapAllocationArgumentExceptionMessage);
        }

        ///<summary>Retrieves a <see cref="TileArray" /> within the given bounds.</summary>
        ///<remarks>This is meant for more a performant way to get Tiles as a batch, when compared to calling <see cref="GetTile" /> for every single position.</remarks>
        ///<param name="bounds">The bounds to retrieve from.</param>
        ///<param name="memoryLabel">Memory label used for profiling and tracking this memory allocation in Unity.</param>
        ///<returns>
        ///  <see cref="TileArray" /> containing the <see cref="TileBase" />s in the bounds.</returns>
        ///<example>
        ///  <code><![CDATA[ // Retrieves all Tiles from an area on the Tilemap and prints out the Tiles to console
        ///using Unity.Collections;
        ///using UnityEngine;
        ///using UnityEngine.Tilemaps;
        ///
        ///public class TilemapExample2 : MonoBehaviour
        ///{
        ///    static readonly MemoryLabel kMemoryLabel = new MemoryLabel("TilemapExample", "Get", Allocator.Domain);
        ///
        ///    public BoundsInt area;
        ///
        ///    void GetTilesExample2()
        ///    {
        ///        Tilemap tilemap = GetComponent<Tilemap>();
        ///        using Tilemap.TileArray tiles = tilemap.GetTiles(area, kMemoryLabel); // Will call TileArray.Dispose() once it is out of scope
        ///        foreach (TileBase tile in tiles)
        ///        {
        ///            print(tile != null ? tile.name : "Empty"); 
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public TileArray GetTiles(BoundsInt bounds, MemoryLabel memoryLabel)
        {
            // Memory Label Allocators are Persistent or Domain
            return new(Internal_GetTiles_MemoryLabel(bounds.min, bounds.size, memoryLabel));
        }

        ///<summary>Retrieves all tiles within the given bounds as a <see cref="TileArray" />, with their corresponding positions in a <see cref="PositionArray" />. Only positions containing a <see cref="Tile" /> are included.</summary>
        ///<remarks>
        ///  <para>Use this method to efficiently retrieve tiles as a batch, rather than calling <see cref="GetTile" /> for each position. This can significantly reduce overhead when processing large areas.</para>
        ///  <para>If <c>withinBounds</c> is set to <c>true</c>, this returns all tiles with the positions from <c>bounds.allPositionsWithin</c>. These positions are within the bounds of (0, 0, 0) to (5, 5, 1), where <c>x</c> and <c>y</c> are between 0 and 4, inclusive.
        ///If <c>withinBounds</c> is set to <c>false</c>, this returns all tiles from the start of the bounds (0, 0, 0) to the end of the bounds (5, 5, 1) inclusive. Tiles outside the <see cref="Tilemap" />'s bounds might be included if they fall within the start and end of the given bounds. Positions such as (6, 0, 0), (-1, 1, 0), or (-4, 5, 0) are included, but positions such as (-2, 0, 0) or (6, 6, 0) are excluded because they either come before the start of the given bounds or after the end of the given bounds.</para>
        ///</remarks>
        ///<param name="bounds">The bounds from which to retrieve the tiles.</param>
        ///<param name="positions">A <see cref="PositionArray" /> containing the position of each <see cref="TileBase" /> in the bounds.</param>
        ///<param name="tiles">The <see cref="TileArray" /> containing all the <see cref="TileBase" /> instances in the bounds.</param>
        ///<param name="allocator">The <see cref="Allocator" /> type used to allocate the memory for the <see cref="TileArray" />.  You must use <see cref="Allocator.Temp" />, <see cref="Allocator.Domain" />, or <see cref="Allocator.Persistent" />. The default value is <see cref="Allocator.Temp" />.</param>
        ///<param name="withinBounds">Whether to retrieve the tiles within the given bounds.</param>
        ///<returns>The number of positions and tiles retrieved.</returns>
        ///<example>
        ///  <code><![CDATA[ // Retrieves all Tiles from an area on the Tilemap and prints out the Tiles to console
        ///using UnityEngine;
        ///using UnityEngine.Tilemaps;
        ///
        ///public class TilemapExample3 : MonoBehaviour
        ///{
        ///    public BoundsInt area = new BoundsInt(0, 0, 0, 5, 5, 1);
        ///
        ///    void GetTilesExample3()
        ///    {
        ///        Tilemap tilemap = GetComponent<Tilemap>();
        ///        int count = tilemap.GetTiles(area, out Tilemap.PositionArray positions, out Tilemap.TileArray tiles);
        ///        for (int i = 0; i < count; i++)
        ///        {
        ///            print($"Position: {positions[i]}, Tile: {tiles[i].name}"); 
        ///        }
        ///        // Manually dispose allocated arrays
        ///        positions.Dispose();
        ///        tiles.Dispose();
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public int GetTiles(BoundsInt bounds, out PositionArray positions, out TileArray tiles, Allocator allocator = Allocator.Temp, bool withinBounds = true)
        {
            if (allocator == Allocator.Temp || allocator == Allocator.Persistent || allocator == Allocator.Domain)
            {
                var positionsBuffer = new TilemapBuffer();
                var tilesBuffer = new TilemapBuffer();
                var length = Internal_GetTilePositions(bounds.min, bounds.max, ref positionsBuffer, ref tilesBuffer, withinBounds ? 1 : 0, allocator, IntPtr.Zero);

                positions = new(positionsBuffer);
                tiles = new(tilesBuffer);

                return length;
            }

            throw new ArgumentException(k_TilemapAllocationArgumentExceptionMessage);
        }

        ///<summary>Retrieves all tiles within the given bounds as a <see cref="TileArray" />, with their corresponding positions in a <see cref="PositionArray" />. Only positions containing a <see cref="Tile" /> are included.</summary>
        ///<remarks>Use this method to efficiently retrieve tiles as a batch, rather than calling <see cref="GetTile" /> for each position. This can significantly reduce overhead when processing large areas.</remarks>
        ///<param name="bounds">The bounds from which to retrieve the tiles.</param>
        ///<param name="positions">A <see cref="PositionArray" /> containing the position of each <see cref="TileBase" /> in the bounds.</param>
        ///<param name="tiles">The <see cref="TileArray" /> containing all the <see cref="TileBase" /> instances in the bounds.</param>
        ///<param name="memoryLabel">Memory label used for profiling and tracking this memory allocation in Unity.</param>
        ///<param name="withinBounds">Whether to retrieve the tiles within the given bounds. The default value is True.</param>
        ///<returns>The number of positions and tiles retrieved.</returns>
        ///<example>
        ///  <code><![CDATA[ // Retrieves all Tiles from an area on the Tilemap and prints out the Tiles to console
        ///using Unity.Collections;
        ///using UnityEngine;
        ///using UnityEngine.Tilemaps;
        ///
        ///public class TilemapExample4 : MonoBehaviour
        ///{
        ///    static readonly MemoryLabel kMemoryLabel = new MemoryLabel("TilemapExample", "Get", Allocator.Domain);
        ///
        ///    public BoundsInt area = new BoundsInt(0, 0, 0, 5, 5, 1);
        ///
        ///    void GetTilesExample4()
        ///    {
        ///        Tilemap tilemap = GetComponent<Tilemap>();
        ///        var count = tilemap.GetTiles(area, out var positions, out var tiles, kMemoryLabel);
        ///        for (var i = 0; i < count; i++)
        ///        {
        ///            print($"Position: {positions[i]}, Tile: {tiles[i].name}"); 
        ///        }
        ///        // Manually dispose allocated arrays
        ///        positions.Dispose();
        ///        tiles.Dispose();
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public int GetTiles(BoundsInt bounds, out PositionArray positions, out TileArray tiles, MemoryLabel memoryLabel, bool withinBounds = true)
        {
            var positionsBuffer = new TilemapBuffer();
            var tilesBuffer = new TilemapBuffer();

            // Memory Label Allocators are Persistent or Domain
            var length = Internal_GetTilePositions_MemoryLabel(bounds.min, bounds.max, ref positionsBuffer, ref tilesBuffer, withinBounds ? 1 : 0, memoryLabel);

            positions = new(positionsBuffer);
            tiles = new(tilesBuffer);

            return length;
        }

        [FreeFunction(Name = "TilemapBindings::GetUsedTiles", HasExplicitThis = true)]
        extern TilemapBuffer Internal_GetUsedTiles(Allocator allocator, IntPtr memLabelPtr);
        [FreeFunction(Name = "TilemapBindings::GetUsedTiles_MemoryLabel", HasExplicitThis = true)]
        extern TilemapBuffer Internal_GetUsedTiles_MemoryLabel(MemoryLabel memoryLabel);

        [FreeFunction(Name = "TilemapBindings::GetUsedSprites", HasExplicitThis = true)]
        extern TilemapBuffer Internal_GetUsedSprites(Allocator allocator, IntPtr memLabelPtr);
        [FreeFunction(Name = "TilemapBindings::GetUsedSprites_MemoryLabel", HasExplicitThis = true)]
        extern TilemapBuffer Internal_GetUsedSprites_MemoryLabel(MemoryLabel memoryLabel);

        [FreeFunction(Name = "TilemapBindings::GetTiles", HasExplicitThis = true)]
        extern TilemapBuffer Internal_GetTiles(Vector3Int startPosition, Vector3Int blockDimensions, Allocator allocator, IntPtr memLabelPtr);

        [FreeFunction(Name = "TilemapBindings::GetTiles_MemoryLabel", HasExplicitThis = true)]
        extern TilemapBuffer Internal_GetTiles_MemoryLabel(Vector3Int startPosition, Vector3Int blockDimensions, MemoryLabel memoryLabel);

        [FreeFunction(Name = "TilemapBindings::GetTilePositions", HasExplicitThis = true)]
        extern int Internal_GetTilePositions(Vector3Int startPosition, Vector3Int endPosition, ref TilemapBuffer positions, ref TilemapBuffer tiles, int withinBounds, Allocator allocator, IntPtr memLabelPtr);
        [FreeFunction(Name = "TilemapBindings::GetTilePositions_MemoryLabel", HasExplicitThis = true)]
        extern int Internal_GetTilePositions_MemoryLabel(Vector3Int startPosition, Vector3Int endPosition, ref TilemapBuffer positions, ref TilemapBuffer tiles, int withinBounds, MemoryLabel memoryLabel);

        [FreeFunction(Name = "TilemapBindings::SetTileAssets", HasExplicitThis = true)]
        extern unsafe void Internal_SetTileAssets(void* positionArray, TilemapBuffer tileArray);

        [FreeFunction(Name = "TilemapBindings::SetTileAssetsBlock", HasExplicitThis = true)]
        extern void Internal_SetTileAssetsBlock(Vector3Int position, Vector3Int size, TilemapBuffer tileArray);

        [FreeFunction(Name = "TilemapBindings::SetTileChangeDataArray", HasExplicitThis = true)]
        extern unsafe void Internal_SetTileChangeDataArray(void* positionPtr, TilemapBuffer tileArray, void* colorPtr, void* transformPtr, bool ignoreLockFlags);

        #endregion
    }

    ///<summary>The tile map renderer is used to render the tile map marked out by a [tile map](xref:class-Tilemap) component and a [grid](xref:script-GridLayoutGroup) component.</summary>
    ///<remarks>This class is a script interface for a <see cref="Tilemaps.TilemapRenderer">tile map renderer</see> component.</remarks>
    [RequireComponent(typeof(Tilemap))]
    [NativeHeader("Modules/Grid/Public/GridMarshalling.h")]
    [NativeHeader("Modules/Tilemap/TilemapRendererJobs.h")]
    [NativeHeader("Modules/Tilemap/Public/TilemapMarshalling.h")]
    [NativeHeader("Modules/Tilemap/Public/TilemapRenderer.h")]
    [NativeClass("TilemapRenderer", PersistentTypeId = 0x1CD494D8)]
    public sealed partial class TilemapRenderer : Renderer
    {
        internal TilemapRenderer(global::UnityEngine.EntityId id) : base(id) {}
        public TilemapRenderer() {}
        ///<summary>Sort order for all tiles rendered by the <see cref="TilemapRenderer" />.</summary>
        public enum SortOrder
        {
            ///<summary>Sorts tiles for rendering starting from the tile with the lowest X and the lowest Y cell positions.</summary>
            ///<remarks>The <see cref="TilemapRenderer" /> will iterate through tiles by the next higher X value first before tiles with a higher Y value.</remarks>
            BottomLeft = 0,
            ///<summary>Sorts tiles for rendering starting from the tile with the highest X and the lowest Y cell positions.</summary>
            ///<remarks>The <see cref="TilemapRenderer" /> will iterate through tiles by the next lower X value first before tiles with a higher Y value.</remarks>
            BottomRight = 1,
            ///<summary>Sorts tiles for rendering starting from the tile with the lowest X and the highest Y cell positions.</summary>
            ///<remarks>The <see cref="TilemapRenderer" /> will iterate through tiles by the next higher X value first before tiles with a lower Y value.</remarks>
            TopLeft = 2,
            ///<summary>Sorts tiles for rendering starting from the tile with the highest X and the lowest Y cell positions.</summary>
            ///<remarks>The <see cref="TilemapRenderer" /> will iterate through tiles by the next lower X value first before tiles with a lower Y value.</remarks>
            TopRight = 3,
        }

        ///<summary>Determines how the <see cref="TilemapRenderer" /> should batch the [sprites](xref:Sprites) from [tiles](xref:Tilemap-ScriptableTiles-TileBase) for rendering.</summary>
        public enum Mode
        {
            ///<summary>Batches each <see cref="Sprite" /> from the <see cref="Tilemap" /> into grouped chunks to be rendered.</summary>
            ///<remarks>Use this mode if Sprites on the Tilemap do not need to interact with any non-Tilemap Renderers and to optimize performance..</remarks>
            Chunk = 0,
            ///<summary>Sends each <see cref="Sprite" /> from the <see cref="Tilemap" /> to be rendered individually.</summary>
            ///<remarks>Use this mode if each <see cref="Sprite" /> on the <see cref="Tilemap" /> needs to interact with other Renderers, for example due to sorting.</remarks>
            Individual = 1,
            ///<summary>Sends batchable <see cref="Sprite" />s from the <see cref="Tilemap" /> in chunks to be rendered and can be batched using the Scriptable Render Pipeline (SRP) batching system.</summary>
            ///<remarks>Use this mode if you are using a Scriptable Render Pipeline (SRP) and want Sprites on the Tilemap batch with other related Renderers using the SRP batching system. If you are not using a SRP, this will fallback to the default dynamic batching pipeline.</remarks>
            SRPBatch = 2,
        }

        ///<summary>Returns whether the <see cref="TilemapRenderer" /> automatically detects the bounds to extend chunk culling by.</summary>
        public enum DetectChunkCullingBounds
        {
            ///<summary>The <see cref="TilemapRenderer" /> will automatically detect the bounds of extension by inspecting the <see cref="Sprite" />/s used in the <see cref="Tilemap" />.</summary>
            Auto = 0,
            ///<summary>The user adds in the values used for extend the bounds for culling of <see cref="Tilemap" /> chunks.</summary>
            Manual = 1,
        }

        ///<summary>Size in number of tiles of each chunk created by the <see cref="TilemapRenderer" />.</summary>
        public extern Vector3Int chunkSize
        {
            get;
            set;
        }

        ///<summary>Bounds used for culling of <see cref="Tilemap" /> chunks.</summary>
        ///<remarks>These bounds extend the boundary of Tilemap chunks and are used for culling. This helps to ensure that oversized <see cref="Sprite" />s will not be clipped during culling for the <see cref="TilemapRenderer" />.</remarks>
        public extern Vector3 chunkCullingBounds
        {
            [FreeFunction("TilemapRendererBindings::GetChunkCullingBounds", HasExplicitThis = true)]
            get;
            [FreeFunction("TilemapRendererBindings::SetChunkCullingBounds", HasExplicitThis = true)]
            set;
        }

        ///<summary>Maximum number of chunks the <see cref="TilemapRenderer" /> caches in memory.</summary>
        public extern int maxChunkCount
        {
            get;
            set;
        }

        ///<summary>Maximum number of frames the <see cref="TilemapRenderer" /> keeps unused chunks in memory.</summary>
        public extern int maxFrameAge
        {
            get;
            set;
        }

        ///<summary>Active sort order for the <see cref="TilemapRenderer" />.</summary>
        public extern SortOrder sortOrder
        {
            get;
            set;
        }

        ///<summary>The mode in which the <see cref="TilemapRenderer" /> batches the [tiles](xref:Tilemap-ScriptableTiles-TileBase) for rendering.</summary>
        [NativeProperty("RenderMode")]
        public extern Mode mode
        {
            get;
            set;
        }

        ///<summary>Returns whether the <see cref="TilemapRenderer" /> automatically detects the bounds to extend chunk culling by.</summary>
        public extern DetectChunkCullingBounds detectChunkCullingBounds
        {
            get;
            set;
        }

        ///<summary>Specifies how the <see cref="Tilemap" /> interacts with the masks.</summary>
        ///<remarks>Tilemaps by default do not interact with Sprite Masks, and remain at their original visibility even when overlapping masks. Set this value to either VisibleInsideMask or VisibleOutsideMask to have the Tilemap interact with Sprite Masks. Set this value to VisibleInsideMask to have the Tilemap visible within a mask, and set to VisibleOutsideMask to achieve the inverse effect.</remarks>
        public extern SpriteMaskInteraction maskInteraction
        {
            get;
            set;
        }

        [RequiredByNativeCode]
        internal void RegisterSpriteAtlasRegistered()
        {
            SpriteAtlasManager.atlasRegistered += OnSpriteAtlasRegistered;
        }

        [RequiredByNativeCode]
        internal void UnregisterSpriteAtlasRegistered()
        {
            SpriteAtlasManager.atlasRegistered -= OnSpriteAtlasRegistered;
        }

        internal extern void OnSpriteAtlasRegistered(SpriteAtlas atlas);

        [FreeFunction(Name = "TilemapRendererBindings::SetShaderUserValue", HasExplicitThis = true)] extern internal void Internal_SetShaderUserValueUInt(UInt32 v);
        ///<summary>Assign a custom value to this renderer.</summary>
        ///<remarks>
        ///  <para>The <c>SetShaderUserValue</c> method assigns a custom integer value to a renderer. You can then access the value in your shaders as the <c>unity_RendererUserValue</c> variable.
        ///                    You can use this method to change how a shader draws each renderer, without using different materials or additional CPU time.
        ///
        ///                    **Note**: The value of the <c>unity_RendererUserValue</c> shader variable will always be 0 in the following situations:
        ///
        ///                    * The shader is written for the Built-In Render Pipeline.
        ///                    * When baking lightmaps or light probes.
        ///
        ///                    **Note**: The value is not serialized, so it is not saved to the asset and resets when the object is reloaded.
        ///
        ///
        ///
        ///                    The following code sample creates a <see cref="MonoBehaviour" /> to assign a specific color value on a renderer by encoding it in a <c>uint</c>.</para>
        ///  <para>The corresponding shader code unpacks the unsigned integer into an RGB float triplet in the fragment shader.</para>
        ///</remarks>
        ///<param name="v">The integer to assign to the renderer as a custom value to be used in shaders.</param>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///using UnityEngine.Rendering;
        ///using UnityEngine.Tilemaps;
        ///using UnityEngine.U2D;
        ///
        ///[ExecuteAlways]
        ///public class PerRendererColor : MonoBehaviour
        ///{
        ///    public Color color = Color.white;
        ///
        ///    private TilemapRenderer m_Renderer;
        ///
        ///    void Start()
        ///    {
        ///        m_Renderer = GetComponent<TilemapRenderer>();
        ///    }
        ///
        ///    void Update()
        ///    {
        ///        if (m_Renderer)
        ///        {
        ///            Color32 colorb = color;
        ///            uint packedColor = (uint)colorb.r | ((uint)colorb.g << 8) | ((uint)colorb.b << 16);
        ///            m_Renderer.SetShaderUserValue(packedColor);
        ///        }
        ///    }
        ///}]]></code>
        ///</example>
        ///<example nocheck="true">
        ///  <code><![CDATA[ // This shader fills the Tilemap shape with the color packed in the renderer's user value.
        ///Shader "Example/URPUnlitUserValue"
        ///{
        ///    Properties
        ///    { }
        ///
        ///    SubShader
        ///    {
        ///        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ///
        ///        Pass
        ///        {
        ///            HLSLPROGRAM
        ///            #pragma vertex vert
        ///            #pragma fragment frag
        ///
        ///            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        ///
        ///            struct Attributes
        ///            {
        ///                float4 positionOS   : POSITION;
        ///            };
        ///
        ///            struct Varyings
        ///            {
        ///                float4 positionHCS  : SV_POSITION;
        ///            };
        ///
        ///            Varyings vert(Attributes IN)
        ///            {
        ///                Varyings OUT;
        ///                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
        ///                return OUT;
        ///            }
        ///
        ///            half4 frag() : SV_Target
        ///            {
        ///                // Decoding the user value color and returning it.
        ///                uint3 c = uint3(unity_RendererUserValue & 0xFF, (unity_RendererUserValue >> 8) & 0xFF, (unity_RendererUserValue >> 16) & 0xFF);
        ///                return half4(half3(c) / 255.0f, 1.0f);
        ///            }
        ///            ENDHLSL
        ///        }
        ///    }
        ///}]]></code>
        ///</example>
        ///<seealso cref="TilemapRenderer.GetShaderUserValue" />
        public void SetShaderUserValue(UInt32 v) => Internal_SetShaderUserValueUInt(v);
        [FreeFunction(Name = "TilemapRendererBindings::GetShaderUserValue", HasExplicitThis = true)] extern internal UInt32 Internal_GetShaderUserValueUInt();
        ///<summary>Returns the custom value assigned to this renderer.</summary>
        ///<remarks>The <c>GetShaderUserValue</c> method returns the custom integer assigned to a renderer.
        ///
        ///**Note**: By default, the renderer’s value is 0 unless explicitly assigned.</remarks>
        ///<returns>The integer assigned to the renderer as a custom value.</returns>
        ///<seealso cref="TilemapRenderer.SetShaderUserValue" />
        public UInt32 GetShaderUserValue() { return Internal_GetShaderUserValueUInt(); }
    }

    ///<summary>A Struct for the required data for rendering a <see cref="Tile" />.</summary>
    [RequiredByNativeCode]
    [StructLayoutAttribute(LayoutKind.Sequential)]
    [NativeHeader("Modules/Tilemap/TilemapScripting.h")]
    public partial struct TileData
    {
        ///<summary>
        ///  <see cref="Sprite" /> to be rendered at the <see cref="Tile" />.</summary>
        public Sprite sprite { get { return Object.ForceLoadFromInstanceID(m_Sprite) as Sprite; } set { m_Sprite = value != null ? value.GetEntityId() : EntityId.None; } }
        ///<summary>EntityId of the <see cref="Sprite" /> to be rendered at the <see cref="Tile" />.</summary>
        public EntityId spriteEntityId { get => m_Sprite; set => m_Sprite = value; }
        ///<summary>
        ///  <see cref="Color" /> of the <see cref="Tile" />.</summary>
        public Color color { get { return m_Color; } set { m_Color = value; } }
        ///<summary>
        ///  <see cref="Matrix4x4">Transform matrix</see> of the <see cref="Tile" />.</summary>
        public Matrix4x4 transform { get { return m_Transform; } set { m_Transform = value; } }
        ///<summary>
        ///  <see cref="GameObject" /> of the <see cref="Tile" />.</summary>
        public GameObject gameObject { get { return Object.ForceLoadFromInstanceID(m_GameObject) as GameObject; } set { m_GameObject = value != null ? value.GetEntityId() : EntityId.None; } }
        ///<summary>EntityId of the <see cref="GameObject" /> of the <see cref="Tile" />.</summary>
        public EntityId gameObjectEntityId { get => m_GameObject; set => m_GameObject = value; }
        ///<summary>
        ///  <see cref="TileFlags" /> of the <see cref="Tile" />.</summary>
        public TileFlags flags { get { return m_Flags; } set { m_Flags = value; } }
        ///<exclude />
        public Tile.ColliderType colliderType { get { return m_ColliderType; } set { m_ColliderType = value; } }

        private EntityId m_Sprite;
        private Color m_Color;
        private Matrix4x4 m_Transform;
        private EntityId m_GameObject;
        private TileFlags m_Flags;
        private Tile.ColliderType m_ColliderType;

        internal static readonly TileData Default = CreateDefault();
        private static TileData CreateDefault()
        {
            TileData tileData = default;
            tileData.m_Sprite = EntityId.None;
            tileData.m_Color = Color.white;
            tileData.m_Transform = Matrix4x4.identity;
            tileData.m_GameObject = EntityId.None;
            tileData.m_Flags = default;
            tileData.m_ColliderType = default;
            return tileData;
        }
    }

    ///<summary>Represents the position and <see cref="Tile" /> information to change in a Tilemap.</summary>
    [Serializable]
    [RequiredByNativeCode]
    [StructLayoutAttribute(LayoutKind.Sequential)]
    [NativeHeader("Modules/Tilemap/TilemapScripting.h")]
    public partial struct TileChangeData
    {
        ///<summary>The position to change <see cref="Tile" /> properties at.</summary>
        public Vector3Int position { get { return m_Position; } set { m_Position = value; } }
        ///<summary>The <see cref="Tile" /> to set on the <see cref="Tilemap" />.</summary>
        public TileBase tile { get { return m_TileAsset as TileBase; } set { m_TileAsset = value; } }
        ///<summary>The color to set the <see cref="Tile" /> to.</summary>
        public Color color { get { return m_Color; } set { m_Color = value; } }
        ///<summary>The transform matrix to set the <see cref="Tile" /> to.</summary>
        public Matrix4x4 transform { get { return m_Transform; } set { m_Transform = value; } }

        [SerializeField]
        private Vector3Int m_Position;
        [SerializeField]
        private Object m_TileAsset;
        [SerializeField]
        private Color m_Color;
        [SerializeField]
        private Matrix4x4 m_Transform;

        ///<exclude />
        public TileChangeData(Vector3Int position, TileBase tile, Color color, Matrix4x4 transform)
        {
            m_Position = position;
            m_TileAsset = tile;
            m_Color = color;
            m_Transform = transform;
        }
    }

    ///<summary>A Struct for the required data for animating a <see cref="Tile" />.</summary>
    [RequiredByNativeCode]
    [StructLayoutAttribute(LayoutKind.Sequential)]
    [NativeHeader("Modules/Tilemap/TilemapScripting.h")]
    public partial struct TileAnimationData
    {
        ///<summary>The array of [sprites](xref:Sprites) that are ordered by appearance in the animation.</summary>
        public Sprite[] animatedSprites { get { return m_AnimatedSprites; } set { m_AnimatedSprites = value; } }
        ///<summary>The animation speed.</summary>
        public float animationSpeed { get { return m_AnimationSpeed; } set { m_AnimationSpeed = value; } }
        ///<summary>The start time of the animation. The animation will begin at this time offset.</summary>
        public float animationStartTime { get { return m_AnimationStartTime; } set { m_AnimationStartTime = value; } }
        ///<summary>
        ///  <see cref="TileAnimationFlags" /> for controlling the Tile Animation.</summary>
        public TileAnimationFlags flags { get { return m_Flags; } set { m_Flags = value; } }

        private Sprite[] m_AnimatedSprites;
        private float m_AnimationSpeed;
        private float m_AnimationStartTime;
        private TileAnimationFlags m_Flags;
    }

    ///<summary>A struct for the required data for animating a <see cref="Tile" />.</summary>
    ///<remarks>
    ///  <para>This is similar to <see cref="TileAnimationData" /> except it uses EntityIds instead of managed object references.</para>
    ///  <para>The example above shows how you can copy a <see cref="Tile" />'s animation data to be passed to a <see cref="Tilemap" />.</para>
    ///</remarks>
    ///<example>
    ///  <code><![CDATA[using Unity.Collections;
    ///using UnityEngine;
    ///using UnityEngine.Tilemaps;
    ///
    ///public static class TilemapExample
    ///{
    ///    public struct AnimatedEntityIdData
    ///    {
    ///        public TileData m_TileData;
    ///
    ///        public NativeArray<EntityId> m_AnimatedSpriteEntityIds;
    ///        public float m_Speed;
    ///        public float m_AnimationStartTime;
    ///        public TileAnimationFlags m_TileAnimationFlags;
    ///
    ///        public void Dispose()
    ///        {
    ///            if (m_AnimatedSpriteEntityIds.IsCreated)
    ///                m_AnimatedSpriteEntityIds.Dispose();
    ///        }
    ///    }
    ///
    ///    static unsafe void GetTileAnimationEntityIdDataJob(int count, Vector3Int* position, ref AnimatedEntityIdData tileAnimationData, TileAnimationEntityIdData* outTilemapAnimationEntityIdData)
    ///    {
    ///        for (var i = 0; i < count; ++i)
    ///        {
    ///            ref TileAnimationEntityIdData outTilemapAnimationData = ref *(outTilemapAnimationEntityIdData + i);
    ///            if (tileAnimationData.m_AnimatedSpriteEntityIds.IsCreated)
    ///                outTilemapAnimationData.animatedSpritesEntityIds = tileAnimationData.m_AnimatedSpriteEntityIds;
    ///            outTilemapAnimationData.animationSpeed = tileAnimationData.m_Speed;
    ///            outTilemapAnimationData.animationStartTime = tileAnimationData.m_AnimationStartTime;
    ///            outTilemapAnimationData.flags = tileAnimationData.m_TileAnimationFlags;
    ///        }
    ///    }
    ///}
    ///]]></code>
    ///</example>
    [RequiredByNativeCode]
    [StructLayoutAttribute(LayoutKind.Sequential)]
    [NativeHeader("Modules/Tilemap/TilemapScripting.h")]
    public partial struct TileAnimationEntityIdData
    {
        ///<summary>The array of  the Entityids of [sprites](xref:Sprites), ordered by appearance in the animation.</summary>
        public NativeArray<EntityId> animatedSpritesEntityIds
        {
            set
            {
                if (!value.IsCreated)
                    return;
                unsafe
                {
                    m_AnimatedSpritesEntityIdPtr = (IntPtr)NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(value);
                    m_Count = value.Length;
                }
            }
        }

        internal IntPtr animatedSpritesEntityIdPtr { get => m_AnimatedSpritesEntityIdPtr; set => m_AnimatedSpritesEntityIdPtr = value; }
        internal int count { get => m_Count; set => m_Count = value; }
        ///<summary>The animation speed.</summary>
        public float animationSpeed { get { return m_AnimationSpeed; } set { m_AnimationSpeed = value; } }
        ///<summary>The start time of the animation. The animation will begin at this time offset.</summary>
        public float animationStartTime { get { return m_AnimationStartTime; } set { m_AnimationStartTime = value; } }
        ///<summary>
        ///  <see cref="TileAnimationFlags" /> for controlling the Tile animation.</summary>
        public TileAnimationFlags flags { get { return m_Flags; } set { m_Flags = value; } }

        private IntPtr m_AnimatedSpritesEntityIdPtr;
        private int m_Count;
        private float m_AnimationSpeed;
        private float m_AnimationStartTime;
        private TileAnimationFlags m_Flags;

        internal void CopyFrom(TileAnimationData other)
        {
            m_AnimatedSpritesEntityIdPtr = IntPtr.Zero;
            m_Count = 0;
            if (other.animatedSprites != null && other.animatedSprites.Length > 0)
            {
                var spriteArray = new NativeArray<EntityId>(other.animatedSprites.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                for (int i = 0; i < other.animatedSprites.Length; ++i)
                {
                    var sprite = other.animatedSprites[i];
                    spriteArray[i] = sprite != null ? sprite.GetEntityId() : EntityId.None;
                }
                animatedSpritesEntityIds = spriteArray;
                m_Count = other.animatedSprites.Length;
            }
            m_AnimationSpeed = other.animationSpeed;
            m_AnimationStartTime = other.animationStartTime;
            m_Flags = other.flags;
        }
    };

    ///<summary>Collider for 2D physics representing shapes defined by the corresponding <see cref="Tilemap" />.</summary>
    ///<seealso cref="BoxCollider2D" />
    ///<seealso cref="CircleCollider2D" />
    ///<seealso cref="EdgeCollider2D" />
    ///<seealso cref="PolygonCollider2D" />
    [RequireComponent(typeof(Tilemap))]
    [NativeHeader("Modules/Tilemap/Public/TilemapCollider2D.h")]
    [NativeClass("TilemapCollider2D", PersistentTypeId = 0x012CE73C)]
    public sealed partial class TilemapCollider2D : Collider2D
    {
        internal TilemapCollider2D(global::UnityEngine.EntityId id) : base(id) {}
        public TilemapCollider2D() {}
        // Get/Set Delaunay mesh usage.
        ///<summary>When the value is true, the Collider uses an additional Delaunay triangulation step to produce the Collider mesh. When the value is false, this additional step does not occur.</summary>
        ///<remarks>Using Delaunay triangulation can reduce the number of shapes created in the Collider mesh and reduce the number of small triangle fans produced, both of which can improve overall physics performance.</remarks>
        extern public bool useDelaunayMesh { get; set; }

        ///<summary>Maximum number of Tile Changes accumulated before doing a full collider rebuild instead of an incremental rebuild.</summary>
        ///<remarks>Change this if incremental rebuilds are slow for the number of Tile Changes accumulated.</remarks>
        public extern uint maximumTileChangeCount
        {
            get;
            set;
        }

        ///<summary>The amount of Collider shapes each Tile extrudes to facilitate compositing with neighboring Tiles. This eliminates fine gaps between Tiles when using a CompositeCollider2D. This is calculated in Unity units within world space.</summary>
        public extern float extrusionFactor
        {
            get;
            set;
        }

        ///<summary>Returns true if there are Tilemap changes that require processing for Collider updates. Returns false otherwise.</summary>
        public extern bool hasTilemapChanges
        {
            [NativeMethod("HasTilemapChanges")]
            get;
        }

        ///<summary>Processes Tilemap changes for Collider updates immediately, if there are any.</summary>
        ///<remarks>Tilemap changes for Collider updates are normally handled in the LateUpdate step. If immediate changes are required, use this to have the changes in the Tilemap reflected immediately. Calling this will not process changes if the TilemapCollider2D is not enabled.</remarks>
        [NativeMethod(Name = "ProcessTileChangeQueue")]
        public extern void ProcessTilemapChanges();
    }
}
