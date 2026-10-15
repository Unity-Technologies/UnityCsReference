// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using uei = UnityEngine.Internal;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Profiling;
using UnityEngine.Scripting.APIUpdating;
using Unity.Scripting.LifecycleManagement;

namespace UnityEngine.TerrainTools
{
    ///<summary>The context for a paint operation that may span multiple connected Terrain tiles.</summary>
    ///<remarks>This class is used to apply an edit operation to an area of Terrain that may span multiple Terrain tiles.
    ///              A PaintContext may be used to edit heightmap or splatmap data, and may also be used to gather normal data in read-only mode (you cannot write to normals, because they are derived from the heightmap).
    ///
    ///              
    ///
    ///              A PaintContext will calculate the relevant regions on each Terrain, and collect the original data into a single sourceRenderTarget.
    ///              Your edit operation can then read from sourcerenderTarget, and write the modified data to destinationRenderTarget.
    ///              Once you have applied your edit operation, the PaintContext can also write the modified data in destinationRenderTarget back to each Terrain, ensuring no seams between them.
    ///
    ///              
    ///
    ///              The simplest way to use a PaintContext is through the helper functions in TerrainPaintUtility:
    ///
    ///              <see cref="TerrainPaintUtility.BeginPaintHeightmap" />, <see cref="TerrainPaintUtility.EndPaintHeightmap" />, <see cref="TerrainPaintUtility.BeginPaintTexture" />, <see cref="TerrainPaintUtility.EndPaintTexture" />, <see cref="TerrainPaintUtility.CollectNormals" /> and <see cref="TerrainPaintUtility.ReleaseContextResources" />.
    ///
    ///              
    ///
    ///              You can also use PaintContext more directly through its member functions.  In general, they are used in the following order:
    ///
    ///              1) Constructor, <see cref="PaintContext.CreateFromBounds" /> - Construct a PaintContext with a target Terrain and a region to edit
    ///
    ///              2) <see cref="PaintContext.CreateRenderTargets" /> - Create the source and destination RenderTargets
    ///
    ///              3) <see cref="PaintContext.GatherHeightmap" />, <see cref="PaintContext.GatherAlphamap" />, <see cref="PaintContext.GatherNormals" /> - Read from Terrain tiles into sourceRenderTarget
    ///
    ///              4) Apply editing operations, reading from sourceRenderTarget, and writing to destinationRenderTarget
    ///
    ///              5) <see cref="PaintContext.ScatterHeightmap" />, <see cref="PaintContext.ScatterAlphamap" /> - Write from destinationRenderTarget into Terrain tiles (optional)
    ///
    ///              6) <see cref="PaintContext.Cleanup" /> - Destroy RenderTarget resources (required if you call CreateRenderTargets)
    ///
    ///              7) <see cref="PaintContext.ApplyDelayedActions" /> - Apply any delayed actions that perform expensive updates</remarks>
    ///<seealso cref="T:UnityEditor.TerrainTools.TerrainPaintTool`1" />
    [MovedFrom("UnityEngine.Experimental.TerrainAPI")]
    public partial class PaintContext
    {
        // initialized by constructor
        ///<summary>(RO) The Terrain used to build the PaintContext.</summary>
        ///<remarks>When painting across a border, the PaintContext can refer to several Terrain tiles.
        ///                  The originTerrain is used to define the terrain space (terrain-local object space) within the PaintContext.</remarks>
        public Terrain originTerrain { get; }     // the terrain that defines the coordinate system and world space position of this PaintContext
        ///<summary>(RO) The pixel rectangle that this PaintContext represents.</summary>
        ///<remarks>The pixel rectangle coordinates refer to pixels in the targetTexture on the <see cref="PaintContext.originTerrain" />.</remarks>
        ///<seealso cref="PaintContext.targetTextureWidth" />
        ///<seealso cref="PaintContext.targetTextureHeight" />
        public RectInt pixelRect { get; }         // the rectangle, in target texture pixels on the originTerrain, that this paint context represents
        ///<summary>(RO) The width of the target terrain texture.  This is the resolution for a single Terrain.</summary>
        public int targetTextureWidth { get; }    // the size of the target texture, per terrain tile
        ///<summary>(RO) The height of the target terrain texture.  This is the resolution for a single Terrain.</summary>
        public int targetTextureHeight { get; }   // the size of the target texture, per terrain tile
        ///<summary>(RO) The size of a PaintContext pixel in terrain units (as defined by <see cref="originTerrain" />.)</summary>
        public Vector2 pixelSize { get; }         // size of a paint context pixel in object/terrain/world space

        // initialized by CreateRenderTargets()
        ///<summary>(RO) Render target that stores the original data from the Terrain tiles.</summary>
        ///<remarks>This RenderTexture contains all of the data collected from all Terrain tiles that intersect the PaintContext.
        ///                  The RenderTexture is created by <see cref="PaintContext.CreateRenderTargets" />, and populated by one of the Gather functions (<see cref="PaintContext.GatherHeightmap" />, <see cref="PaintContext.GatherAlphamap" /> or <see cref="PaintContext.GatherNormals" />).</remarks>
        ///<seealso cref="PaintContext" />
        ///<seealso cref="PaintContext.destinationRenderTexture" />
        public RenderTexture sourceRenderTexture { get; private set; }       // the original data
        ///<summary>(RO) RenderTexture that an edit operation writes to modify the data.</summary>
        ///<remarks>This RenderTexture stores the modified data represented by a PaintContext.
        ///                  A terrain tool will typically read from <c>sourceRenderTexture</c>, modify the data, and write to <c>destinationRenderTexture</c>.
        ///                  The Scatter functions (<see cref="PaintContext.ScatterHeightmap" /> or <see cref="PaintContext.ScatterAlphamap" />) read from <c>destinationRenderTexture</c> to distribute the modified data back to the source Terrain tiles.
        ///                  <c>destinationRenderTexture</c> is created by <see cref="PaintContext.CreateRenderTargets" />, with size and format matching <c>sourceRenderTexture</c>.</remarks>
        ///<seealso cref="PaintContext" />
        ///<seealso cref="PaintContext.sourceRenderTexture" />
        public RenderTexture destinationRenderTexture { get; private set; }  // the modified data (you render to this)
        ///<summary>(RO) The value of RenderTexture.active at the time CreateRenderTargets is called.</summary>
        ///<remarks>
        ///  <see cref="PaintContext.Cleanup" /> uses this value to restore the active RenderTexture to its original value.
        ///                  In some cases, it may be necessary to manually restore the RenderTexture before calling Cleanup:
        ///
        ///                  <c>RenderTexture.active = PaintContext.oldRenderTexture;</c></remarks>
        ///<seealso cref="PaintContext" />
        ///<seealso cref="PaintContext.Cleanup" />
        public RenderTexture oldRenderTexture { get; private set; }          // active render texture at the time CreateRenderTargets() is called, restored on Cleanup()

        ///<summary>(RO) The number of Terrain tiles in this PaintContext.</summary>
        public int terrainCount { get { return m_TerrainTiles.Count; } }
        ///<summary>Retrieves a Terrain from the PaintContext.</summary>
        ///<remarks>When painting across a border, the PaintContext can refer to several Terrain tiles.
        ///                  GetTerrain is used to access those Terrain tiles.
        ///                  terrainIndex must be between 0 and <see cref="PaintContext.terrainCount" /> - 1.</remarks>
        ///<param name="terrainIndex">Index of the terrain.</param>
        ///<returns>Returns the Terrain object.</returns>
        ///<seealso cref="PaintContext.GetClippedPixelRectInTerrainPixels" />
        ///<seealso cref="PaintContext.GetClippedPixelRectInRenderTexturePixels" />
        public Terrain GetTerrain(int terrainIndex)
        {
            return m_TerrainTiles[terrainIndex].terrain;
        }

        ///<summary>Retrieves the clipped pixel rectangle for a Terrain.</summary>
        ///<remarks>When painting across a border, the PaintContext can refer to several Terrain tiles.
        ///                  GetClippedPixelRectInTerrainPixels returns the <see cref="PaintContext.pixelRect" /> clipped to the specified Terrain, in the pixel coordinates of the target texture on that Terrain.
        ///                  terrainIndex must be between 0 and <see cref="PaintContext.terrainCount" /> - 1.</remarks>
        ///<param name="terrainIndex">Index of the Terrain.</param>
        ///<returns>Returns the clipped pixel rectangle.</returns>
        ///<seealso cref="PaintContext.GetTerrain" />
        ///<seealso cref="PaintContext.targetTextureWidth" />
        ///<seealso cref="PaintContext.targetTextureHeight" />
        public RectInt GetClippedPixelRectInTerrainPixels(int terrainIndex)
        {
            return m_TerrainTiles[terrainIndex].clippedTerrainPixels;
        }

        ///<summary>Retrieves the clipped pixel rectangle for a Terrain, relative to the PaintContext render textures.</summary>
        ///<remarks>When painting across a border, the PaintContext can refer to several Terrain tiles.
        ///                  GetClippedPixelRectInTerrainPixels returns the <see cref="PaintContext.pixelRect" /> clipped to the specified Terrain, in the pixel coordinates of <see cref="PaintContext.sourceRenderTexture" /> and <see cref="PaintContext.destinationRenderTexture" />.
        ///                  terrainIndex must be between 0 and <see cref="PaintContext.terrainCount" /> - 1.</remarks>
        ///<param name="terrainIndex">Index of the Terrain.</param>
        ///<returns>Returns the clipped pixel rectangle.</returns>
        ///<seealso cref="PaintContext.GetTerrain" />
        public RectInt GetClippedPixelRectInRenderTexturePixels(int terrainIndex)
        {
            return m_TerrainTiles[terrainIndex].clippedPCPixels;
        }

        // initialized by constructor
        private List<TerrainTile> m_TerrainTiles;              // all terrain tiles touched by this paint context

        private float m_HeightWorldSpaceMin;
        private float m_HeightWorldSpaceMax;

        ///<summary>The minimum height of all Terrain tiles that this PaintContext touches in world space.</summary>
        ///<remarks>Unity uses this value to transform a height value from a world space Y-coordinate to a value in the [0, 1] range.</remarks>
        public float heightWorldSpaceMin => m_HeightWorldSpaceMin;
        ///<summary>The height range (from Min to Max) of all Terrain tiles that this PaintContext touches in world space.</summary>
        ///<remarks>Unity uses this value to transform a height value from a world space Y-coordinate to a value in the [0, 1] range.</remarks>
        public float heightWorldSpaceSize => m_HeightWorldSpaceMax - m_HeightWorldSpaceMin;

        ///<summary>Interface that conveys information about a Terrain within the PaintContext area.</summary>
        public interface ITerrainInfo
        {
            ///<summary>The Terrain represented by this context. (RO)</summary>
            Terrain terrain                 { get; }            // the terrain tile
            ///<summary>
            ///  <see cref="PaintContext.pixelRect" />, clipped to this Terrain, in Terrain pixel coordinates. (RO)</summary>
            RectInt clippedTerrainPixels    { get; }            // the region modified by the PaintContext, in target texture pixels
            ///<summary>
            ///  <see cref="PaintContext.pixelRect" />, clipped to this Terrain, in PaintContext pixel coordinates. (RO)</summary>
            RectInt clippedPCPixels         { get; }            // the region modified by the PaintContext, in PaintContext.sourceRenderTexture or destinationRenderTexture pixels
            ///<summary>Use this property to fill empty regions in PaintContext. It is the same as <c>clippedTerrainPixels</c> with padding around unconnected Terrain edges. (RO)</summary>
            RectInt paddedTerrainPixels     { get; }            // a padded version of clippedTerrainPixels, used for extended-edge sampling to fill empty space
            ///<summary>Use this property to fill empty regions in PaintContext. It is the same as <c>clippedPCPixels</c> with padding around unconnected Terrain edges. (RO)</summary>
            RectInt paddedPCPixels          { get; }            // a padded version of clippedPCPixels, used for extended-edge sampling to fill empty space
            ///<summary>Controls gathering from this Terrain within the PaintContext. The default is true.</summary>
            ///<remarks>Modify this value, if required, to skip this Terrain in any Gather operations that the PaintContext performs.</remarks>
            bool gatherEnable               { get; set; }       // user tools can disable gathering of this terrain tile by setting this flag (default true)
            ///<summary>Controls scattering to this Terrain within the PaintContext. The default is true.</summary>
            ///<remarks>Modify this value, if required, to skip this Terrain in any Scatter operations that the PaintContext performs.</remarks>
            bool scatterEnable              { get; set; }       // user tools can disable scattering to this terrain tile by setting this flag (default true)
            ///<summary>Modify this value, if required, to store and retrieve values relevant to the PaintContext operation.</summary>
            ///<remarks>For example, use <c>userData</c> to cache information during a Gather operation, and then use that cached information in a Scatter operation.</remarks>
            object userData                 { get; set; }       // user tools can use this to associate data with the terrain
        }

        private class TerrainTile : ITerrainInfo
        {
            public Terrain terrain;                 // the terrain object for this tile
            public Vector2Int tileOriginPixels;     // coordinates of this terrain tile in originTerrain target texture pixels

            public RectInt clippedTerrainPixels;    // the tile pixels touched by this PaintContext (in terrain-local target texture pixels)
            public RectInt clippedPCPixels;         // the tile pixels touched by this PaintContext (in PaintContext/source/destRenderTexture pixels)

            public RectInt paddedTerrainPixels;     // a padded version of clippedTerrainPixels, used for extended-edge sampling
            public RectInt paddedPCPixels;          // a padded version of clippedPCPixels, used for extended-edge sampling

            public object userData;                 // user data stash
            public bool gatherEnable;                 // user controls for read/write
            public bool scatterEnable;

            Terrain ITerrainInfo.terrain                 { get { return terrain; } }
            RectInt ITerrainInfo.clippedTerrainPixels    { get { return clippedTerrainPixels; } }
            RectInt ITerrainInfo.clippedPCPixels         { get { return clippedPCPixels; } }
            RectInt ITerrainInfo.paddedTerrainPixels     { get { return paddedTerrainPixels; } }
            RectInt ITerrainInfo.paddedPCPixels          { get { return paddedPCPixels; } }
            bool ITerrainInfo.gatherEnable               { get { return gatherEnable; } set { gatherEnable = value; } }
            bool ITerrainInfo.scatterEnable              { get { return scatterEnable; } set { scatterEnable = value; } }
            object ITerrainInfo.userData                 { get { return userData; } set { userData = value; } }

            public static TerrainTile Make(Terrain terrain, int tileOriginPixelsX, int tileOriginPixelsY,
                RectInt pixelRect, int targetTextureWidth, int targetTextureHeight, int edgePad = 0)
            {
                var tile = new TerrainTile()
                {
                    terrain = terrain,
                    gatherEnable = true,
                    scatterEnable = true,
                    tileOriginPixels = new Vector2Int(tileOriginPixelsX, tileOriginPixelsY),
                    clippedTerrainPixels = new RectInt()
                    {
                        x = Mathf.Max(0, pixelRect.x - tileOriginPixelsX),
                        y = Mathf.Max(0, pixelRect.y - tileOriginPixelsY),
                        xMax = Mathf.Min(targetTextureWidth, pixelRect.xMax - tileOriginPixelsX),
                        yMax = Mathf.Min(targetTextureHeight, pixelRect.yMax - tileOriginPixelsY)
                    },
                };
                tile.clippedPCPixels = new RectInt(
                    tile.clippedTerrainPixels.x + tile.tileOriginPixels.x - pixelRect.x,
                    tile.clippedTerrainPixels.y + tile.tileOriginPixels.y - pixelRect.y,
                    tile.clippedTerrainPixels.width,
                    tile.clippedTerrainPixels.height);
                // Optimize padding by removing it on edges that have a neighbor.
                int leftPad = terrain.leftNeighbor == null ? edgePad : 0;
                int rightPad = terrain.rightNeighbor == null ? edgePad : 0;
                int bottomPad = terrain.bottomNeighbor == null ? edgePad : 0;
                int topPad = terrain.topNeighbor == null ? edgePad : 0;
                // Redo same clipping as clippedTerrainPixels, but on padded version of terrain.
                tile.paddedTerrainPixels = new RectInt()
                {
                    x = Mathf.Max(-leftPad, pixelRect.x - tileOriginPixelsX - leftPad),
                    y = Mathf.Max(-bottomPad, pixelRect.y - tileOriginPixelsY - bottomPad),
                    xMax = Mathf.Min(targetTextureWidth + rightPad, pixelRect.xMax - tileOriginPixelsX + rightPad),
                    yMax = Mathf.Min(targetTextureHeight + topPad, pixelRect.yMax - tileOriginPixelsY + topPad)
                };
                // PaddedPCPixels is equal to clippedPCPixels padded by the same amount as terrainPixels.
                tile.paddedPCPixels = new RectInt(
                    tile.clippedPCPixels.min + (tile.paddedTerrainPixels.min - tile.clippedTerrainPixels.min),
                    tile.clippedPCPixels.size + (tile.paddedTerrainPixels.size - tile.clippedTerrainPixels.size));

                if (tile.clippedTerrainPixels.width == 0 || tile.clippedTerrainPixels.height == 0)
                {
                    tile.gatherEnable = false;
                    tile.scatterEnable = false;
                    Debug.LogError("PaintContext.ClipTerrainTiles found 0 content rect");       // we really shouldn't ever have this..
                }

                return tile;
            }
        }

        private class SplatmapUserData             // splatmap operation data per Terrain tile
        {
            public TerrainLayer terrainLayer;       // the terrain layer we are concerned with
            public int terrainLayerIndex;           // the terrain layer index on this Terrain
            public int mapIndex;                    // the splatmap index on this Terrain containing the desired TerrainLayer weight
            public int channelIndex;                // the channel on the splatmap containing the desired TerrainLayer weight
        }

        [Flags]
        internal enum ToolAction
        {
            None = 0,
            PaintHeightmap = 1 << 0,
            PaintTexture = 1 << 1,
            PaintHoles = 1 << 2,
            AddTerrainLayer = 1 << 3
        }

        ///<summary>Unity uses this value internally to transform a [0, 1] height value to a texel value, which is stored in <see cref="TerrainData.heightmapTexture" />.</summary>
        public static float kNormalizedHeightScale => 32766.0f / 65535.0f;

        // TerrainPaintUtilityEditor hooks to this event to do automatic undo
        [AutoStaticsCleanupOnCodeReload]
        internal static event Action<PaintContext.ITerrainInfo, ToolAction, string /*editorUndoName*/> onTerrainTileBeforePaint;

        internal const int k_MinimumResolution = 1;
        internal const int k_MaximumResolution = 8192;
        internal static int ClampContextResolution(int resolution)
        {
            return Mathf.Clamp(resolution, k_MinimumResolution, k_MaximumResolution);
        }

        ///<summary>Creates a new PaintContext, to edit a target texture on a Terrain, in a region defined by pixelRect.</summary>
        ///<remarks>This constructor finds all Terrain tiles that touch the pixelRect, searching across adjacent connected Terrain tiles.
        ///                  It also calculates the relevant regions on each Terrain, as well as the transforms between them.</remarks>
        ///<param name="terrain">Terrain that defines terrain space for this PaintContext.</param>
        ///<param name="pixelRect">Pixel rectangle to edit in the target terrain texture.</param>
        ///<param name="targetTextureWidth">Width of the target terrain texture (per Terrain).</param>
        ///<param name="targetTextureHeight">Height of the target terrain texture (per Terrain).</param>
        ///<param name="sharedBoundaryTexel">Whether to stretch the Textures so that edge texels lie on the Terrain boundary, and are shared with connected Terrains.</param>
        ///<param name="fillOutsideTerrain">Whether to fill empty space outside of the Terrain tiles with data from the nearest tile.</param>
        ///<seealso cref="PaintContext" />
        public PaintContext(
            Terrain terrain, RectInt pixelRect, int targetTextureWidth, int targetTextureHeight,
            [uei.DefaultValue("true")] bool sharedBoundaryTexel = true,
            [uei.DefaultValue("true")] bool fillOutsideTerrain = true)
        {
            this.originTerrain = terrain;
            this.pixelRect = pixelRect;
            this.targetTextureWidth = targetTextureWidth;
            this.targetTextureHeight = targetTextureHeight;
            TerrainData terrainData = terrain.terrainData;
            this.pixelSize = new Vector2(
                terrainData.size.x / (targetTextureWidth - (sharedBoundaryTexel ? 1.0f : 0.0f)),
                terrainData.size.z / (targetTextureHeight - (sharedBoundaryTexel ? 1.0f : 0.0f)));

            FindTerrainTilesUnlimited(sharedBoundaryTexel, fillOutsideTerrain);
        }

        ///<summary>Constructs a PaintContext that you can use to edit a texture on a Terrain, in the region defined by boundsInTerrainSpace and extraBorderPixels.</summary>
        ///<remarks>This function calculates a pixelRect from <c>boundsInTerrainSpace</c> and <c>extraBorderPixels</c>,
        ///                  and then constructs a PaintContext from the pixelRect.
        ///
        ///                  This function is called internally by <see cref="TerrainPaintUtility.BeginPaintHeightmap" />, <see cref="TerrainPaintUtility.BeginPaintTexture" /> and <see cref="TerrainPaintUtility.CollectNormals" />.</remarks>
        ///<param name="terrain">Terrain that defines terrain space for this PaintContext.</param>
        ///<param name="boundsInTerrainSpace">Terrain space bounds to edit in the target terrain texture.</param>
        ///<param name="inputTextureWidth">Width of the input Terrain Texture for all connected Terrains.</param>
        ///<param name="inputTextureHeight">Height of the input Terrain Texture for all connected Terrains.</param>
        ///<param name="extraBorderPixels">Number of extra border pixels required. The default value is 0.</param>
        ///<param name="sharedBoundaryTexel">Whether to stretch the Textures so that edge texels lie on the Terrain boundary, and are shared with connected Terrains.</param>
        ///<param name="fillOutsideTerrain">Whether to fill empty space outside of the Terrain tiles with data from the nearest tile.</param>
        ///<returns>A new PaintContext for the given Terrain and region.</returns>
        ///<seealso cref="PaintContext" />
        public static PaintContext CreateFromBounds(
            Terrain terrain, Rect boundsInTerrainSpace, int inputTextureWidth, int inputTextureHeight,
            [uei.DefaultValue("0")] int extraBorderPixels = 0,
            [uei.DefaultValue("true")] bool sharedBoundaryTexel = true,
            [uei.DefaultValue("true")] bool fillOutsideTerrain = true)
        {
            return new PaintContext(
                terrain,
                TerrainPaintUtility.CalcPixelRectFromBounds(terrain, boundsInTerrainSpace, inputTextureWidth,
                    inputTextureHeight, extraBorderPixels, sharedBoundaryTexel),
                inputTextureWidth, inputTextureHeight, sharedBoundaryTexel, fillOutsideTerrain);
        }

        private void FindTerrainTilesUnlimited(bool sharedBoundaryTexel, bool fillOutsideTerrain)
        {
            // pixel rect bounds (in world space)
            float minX = originTerrain.transform.position.x + pixelSize.x * pixelRect.xMin;
            float minZ = originTerrain.transform.position.z + pixelSize.y * pixelRect.yMin;
            float maxX = originTerrain.transform.position.x + pixelSize.x * (pixelRect.xMax - 1);
            float maxZ = originTerrain.transform.position.z + pixelSize.y * (pixelRect.yMax - 1);

            m_HeightWorldSpaceMin = originTerrain.GetPosition().y;
            m_HeightWorldSpaceMax = m_HeightWorldSpaceMin + originTerrain.terrainData.size.y;

            // this filter limits the search to Terrains that overlap the pixel rect bounds
            Predicate<Terrain> filterOverlap =
                t =>
            {
                // terrain bounds (in world space)
                float tminX = t.transform.position.x;
                float tminZ = t.transform.position.z;
                float tmaxX = t.transform.position.x + t.terrainData.size.x;
                float tmaxZ = t.transform.position.z + t.terrainData.size.z;

                // test overlap
                return (tminX <= maxX) && (tmaxX >= minX)
                    && (tminZ <= maxZ) && (tmaxZ >= minZ);
            };

            // gather Terrains that pass the filter
            TerrainUtils.TerrainMap terrainMap = TerrainUtils.TerrainMap.CreateFromConnectedNeighbors(originTerrain, filterOverlap, false);

            // convert those Terrains into the TerrainTile list
            m_TerrainTiles = new List<TerrainTile>();
            if (terrainMap != null)
            {
                foreach (var cur in terrainMap.terrainTiles)
                {
                    var coord = cur.Key;
                    Terrain terrain = cur.Value;

                    int minPixelX = coord.tileX * (targetTextureWidth - (sharedBoundaryTexel ? 1 : 0));
                    int minPixelZ = coord.tileZ * (targetTextureHeight - (sharedBoundaryTexel ? 1 : 0));
                    RectInt terrainPixelRect = new RectInt(minPixelX, minPixelZ, targetTextureWidth, targetTextureHeight);
                    if (pixelRect.Overlaps(terrainPixelRect))
                    {
                        // EdgePad fills empty regions outside terrains in PaintContext.
                        int edgePad = fillOutsideTerrain ? Mathf.Max(targetTextureWidth, targetTextureHeight) : 0;
                        m_TerrainTiles.Add(
                            TerrainTile.Make(
                                terrain,
                                minPixelX,
                                minPixelZ,
                                pixelRect,
                                targetTextureWidth,
                                targetTextureHeight,
                                edgePad));
                        m_HeightWorldSpaceMin = Mathf.Min(m_HeightWorldSpaceMin, terrain.GetPosition().y);
                        m_HeightWorldSpaceMax = Mathf.Max(m_HeightWorldSpaceMax, terrain.GetPosition().y + terrain.terrainData.size.y);
                    }
                }
            }
        }

        ///<summary>Creates the <c>sourceRenderTexture</c> and <c>destinationRenderTexture</c>.</summary>
        ///<remarks>The render textures are created at a resolution matching the current <see cref="PaintContext.pixelRect" />, using the specified <see cref="RenderTextureFormat" />.
        ///
        ///                  This function is called internally by <see cref="TerrainPaintUtility.BeginPaintHeightmap" />, <see cref="TerrainPaintUtility.BeginPaintTexture" /> and <see cref="TerrainPaintUtility.CollectNormals" />.</remarks>
        ///<param name="colorFormat">Render Texture format.</param>
        ///<seealso cref="PaintContext.destinationRenderTexture" />
        ///<seealso cref="PaintContext.sourceRenderTexture" />
        public void CreateRenderTargets(RenderTextureFormat colorFormat)
        {
            // Extended edge sampling of tiles requires a depth buffer (see TerrainPaintUtility.DrawQuadPadded for more info).
            int width = ClampContextResolution(pixelRect.width);
            int height = ClampContextResolution(pixelRect.height);
            if (width != pixelRect.width || height != pixelRect.height)
            {
                Debug.LogWarning($@"
TERRAIN EDITOR INTERNAL ERROR: An attempt to create a PaintContext with dimensions of {pixelRect.width}x{pixelRect.height} was made,
whereas the maximum supported resolution is {k_MaximumResolution}. The size has been clamped to {k_MaximumResolution}."
                );
            }
            sourceRenderTexture = RenderTexture.GetTemporary(width, height, 16, colorFormat, RenderTextureReadWrite.Linear);
            destinationRenderTexture = RenderTexture.GetTemporary(width, height, 0, colorFormat, RenderTextureReadWrite.Linear);
            sourceRenderTexture.wrapMode = TextureWrapMode.Clamp;
            sourceRenderTexture.filterMode = FilterMode.Point;
            oldRenderTexture = RenderTexture.active;
        }

        ///<summary>Releases the allocated resources of this PaintContext.</summary>
        ///<remarks>This function releases the <c>sourceRenderTexture</c> and <c>destinationRenderTexture</c>.
        ///                  When restoreRenderTexture is true, it also restores RenderTexture.active to the value saved as <see cref="oldRenderTexture" />.
        ///                  This function is called internally by <see cref="TerrainPaintUtility.EndPaintHeightmap" />, <see cref="TerrainPaintUtility.EndPaintTexture" /> and <see cref="TerrainPaintUtility.ReleaseContextResources" />.</remarks>
        ///<param name="restoreRenderTexture">When true, indicates that this function restores RenderTexture.active</param>
        public void Cleanup(bool restoreRenderTexture = true)
        {
            if (restoreRenderTexture)
                RenderTexture.active = oldRenderTexture;
            RenderTexture.ReleaseTemporary(sourceRenderTexture);
            RenderTexture.ReleaseTemporary(destinationRenderTexture);
            sourceRenderTexture = null;
            destinationRenderTexture = null;
            oldRenderTexture = null;
        }

        private void GatherInternal(
            Func<ITerrainInfo, Texture> terrainToTexture,
            Color defaultColor,
            string operationName,
            Material blitMaterial = null,
            int blitPass = 0,
            Action<ITerrainInfo> beforeBlit = null,
            Action<ITerrainInfo> afterBlit = null)
        {
            if (blitMaterial == null)
                blitMaterial = TerrainPaintUtility.GetBlitMaterial();

            RenderTexture.active = sourceRenderTexture;
            GL.Clear(true, true, defaultColor);

            GL.PushMatrix();
            GL.LoadPixelMatrix(0, pixelRect.width, 0, pixelRect.height);
            for (int i = 0; i < m_TerrainTiles.Count; i++)
            {
                TerrainTile terrainTile = m_TerrainTiles[i];
                if (!terrainTile.gatherEnable)
                    continue;

                Texture sourceTexture = terrainToTexture(terrainTile);
                if ((sourceTexture == null) || (!terrainTile.gatherEnable))   // double check gatherEnable in case terrainToTexture modified it
                    continue;

                if ((sourceTexture.width != targetTextureWidth) || (sourceTexture.height != targetTextureHeight))
                {
                    Debug.LogWarning(operationName + " requires the same resolution texture for all Terrains - mismatched Terrains are ignored.", terrainTile.terrain);
                    continue;
                }

                beforeBlit?.Invoke(terrainTile);
                if (!terrainTile.gatherEnable) // check again, beforeBlit may have modified it
                    continue;

                FilterMode oldFilterMode = sourceTexture.filterMode;
                sourceTexture.filterMode = FilterMode.Point;

                blitMaterial.SetTexture("_MainTex", sourceTexture);
                blitMaterial.SetPass(blitPass);
                // Draw padded quads to support extended-edge sampling of each terrain tile into empty regions.
                TerrainPaintUtility.DrawQuadPadded(terrainTile.clippedPCPixels, terrainTile.paddedPCPixels,
                    terrainTile.clippedTerrainPixels, terrainTile.paddedTerrainPixels, sourceTexture);

                sourceTexture.filterMode = oldFilterMode;

                afterBlit?.Invoke(terrainTile);
            }
            GL.PopMatrix();
            RenderTexture.active = oldRenderTexture;
        }

        private void ScatterInternal(
            Func<ITerrainInfo, RenderTexture> terrainToRT,
            string operationName,
            Material blitMaterial = null,
            int blitPass = 0,
            Action<ITerrainInfo> beforeBlit = null,
            Action<ITerrainInfo> afterBlit = null)
        {
            var oldRT = RenderTexture.active;

            if (blitMaterial == null)
                blitMaterial = TerrainPaintUtility.GetBlitMaterial();

            for (int i = 0; i < m_TerrainTiles.Count; i++)
            {
                TerrainTile terrainTile = m_TerrainTiles[i];
                if (!terrainTile.scatterEnable)
                    continue;

                RenderTexture target = terrainToRT(terrainTile);
                if ((target == null) || (!terrainTile.scatterEnable)) // double check scatterEnable in case terrainToRT modified it
                    continue;

                if ((target.width != targetTextureWidth) || (target.height != targetTextureHeight))
                {
                    Debug.LogWarning(operationName + " requires the same resolution for all Terrains - mismatched Terrains are ignored.", terrainTile.terrain);
                    continue;
                }

                beforeBlit?.Invoke(terrainTile);
                if (!terrainTile.scatterEnable)   // check again, beforeBlit may have modified it
                    continue;

                RenderTexture.active = target;
                GL.PushMatrix();
                GL.LoadPixelMatrix(0, target.width, 0, target.height);
                {
                    FilterMode oldFilterMode = destinationRenderTexture.filterMode;
                    destinationRenderTexture.filterMode = FilterMode.Point;

                    blitMaterial.SetTexture("_MainTex", destinationRenderTexture);
                    blitMaterial.SetPass(blitPass);
                    TerrainPaintUtility.DrawQuad(terrainTile.clippedTerrainPixels, terrainTile.clippedPCPixels, destinationRenderTexture);

                    destinationRenderTexture.filterMode = oldFilterMode;
                }
                GL.PopMatrix();

                afterBlit?.Invoke(terrainTile);
            }

            RenderTexture.active = oldRT;
        }

        ///<summary>Gathers user-specified Texture data into <c>sourceRenderTexture</c>.</summary>
        ///<remarks>This function collects Texture data from all Terrain tiles in the PaintContext, and merges that data into <c>sourceRenderTexture</c>.
        ///                    The <c>terrainSource</c> function specifies what data to collect from each Terrain.
        ///                    Gather assumes that the Texture data, which <c>terrainSource</c> returns, is mapped over the Terrain tile in a manner similar to the Heightmap and Alphamaps.
        ///
        ///                    
        ///
        ///                    First, the function clears <c>sourceRenderTexture</c> to <c>defaultColor</c>.
        ///
        ///                    Then, it uses the following steps to gather each Terrain in the PaintContext:
        ///
        ///                    1) Calls <c>terrainSource</c> to retrieve the Texture.
        ///
        ///                    2) Calls <c>beforeBlit</c>.
        ///
        ///                    3) Uses <c>blitMaterial</c> and <c>blitPass</c> to copy The Texture into <c>sourceRenderTexture</c>.
        ///
        ///                    4) Calls <c>afterBlit</c>.</remarks>
        ///<param name="terrainSource">A function that returns the Texture data to collect from each Terrain.</param>
        ///<param name="defaultColor">The default color for <c>sourceRenderTexture</c>.</param>
        ///<param name="blitMaterial">The material used to copy the data.  If null, the default blit material is used.</param>
        ///<param name="blitPass">The material pass used to copy the data.</param>
        ///<param name="beforeBlit">An optional action to call before copying from each Terrain. The default is null.</param>
        ///<param name="afterBlit">An optional action to call after copying from each Terrain. The default is null.</param>
        ///<seealso cref="PaintContext" />
        ///<seealso cref="PaintContext.Scatter" />
        public void Gather(Func<ITerrainInfo, Texture> terrainSource, Color defaultColor, Material blitMaterial = null, int blitPass = 0, Action<ITerrainInfo> beforeBlit = null, Action<ITerrainInfo> afterBlit = null)
        {
            if (terrainSource != null)
                GatherInternal(terrainSource, defaultColor, "PaintContext.Gather", blitMaterial, blitPass, beforeBlit, afterBlit);
        }

        ///<summary>Applies an edited PaintContext by copying modifications back to user-specified RenderTextures for the source Terrain tiles.</summary>
        ///<remarks>After the edits to a PaintContext are complete, this function applies the modified data in <c>destinationRenderTexture</c> to the data stored for each Terrain.
        ///                  Scatter performs this copy to a set of RenderTextures, which is specified by <c>terrainDest</c>.
        ///
        ///                  
        ///
        ///                  This function uses the following steps to scatter to each Terrain in the PaintContext:
        ///
        ///                  1) Calls <c>terrainDest</c> to retrieve the target RenderTexture.
        ///
        ///                  2) Calls <c>beforeBlit</c>.
        ///
        ///                  3) Uses <c>blitMaterial</c> and <c>blitPass</c> to copy the <c>destinationRenderTexture</c> into the target RenderTexture.
        ///
        ///                  4) Calls <c>afterBlit</c>.</remarks>
        ///<param name="terrainDest">Function returning the RenderTexture to be written for each Terrain.</param>
        ///<param name="blitMaterial">The material used to copy the data.  If null, the default blit material is used.</param>
        ///<param name="blitPass">The material pass used to copy the data.  Its default value is 0.</param>
        ///<param name="beforeBlit">An optional action to call before copying to each Terrain.</param>
        ///<param name="afterBlit">An optional action to call after copying to each Terrain.</param>
        ///<seealso cref="PaintContext" />
        ///<seealso cref="PaintContext.Gather" />
        public void Scatter(Func<ITerrainInfo, RenderTexture> terrainDest, Material blitMaterial = null, int blitPass = 0, Action<ITerrainInfo> beforeBlit = null, Action<ITerrainInfo> afterBlit = null)
        {
            if (terrainDest != null)
                ScatterInternal(terrainDest, "PaintContext.Scatter", blitMaterial, blitPass, beforeBlit, afterBlit);
        }

        ///<summary>Gathers the heightmap information into <c>sourceRenderTexture</c>.</summary>
        ///<remarks>This function collects the heightmap data from all Terrain tiles in the PaintContext into <c>sourceRenderTexture</c>.
        ///
        ///                  This function is called internally by <see cref="TerrainPaintUtility.BeginPaintHeightmap" />.</remarks>
        ///<seealso cref="PaintContext.ScatterHeightmap" />
        public void GatherHeightmap()
        {
            var blitMaterial = TerrainPaintUtility.GetHeightBlitMaterial();
            blitMaterial.SetFloat("_Height_Offset", 0.0f);
            blitMaterial.SetFloat("_Height_Scale", 1.0f);

            GatherInternal(
                t => t.terrain.terrainData.heightmapTexture,
                new Color(0.0f, 0.0f, 0.0f, 0.0f),
                "PaintContext.GatherHeightmap",
                blitMaterial: blitMaterial,
                beforeBlit: t =>
                {
                    blitMaterial.SetFloat("_Height_Offset", (t.terrain.GetPosition().y - heightWorldSpaceMin) / heightWorldSpaceSize * kNormalizedHeightScale);
                    blitMaterial.SetFloat("_Height_Scale", t.terrain.terrainData.size.y / heightWorldSpaceSize);
                });
        }

        ///<summary>Applies an edited heightmap PaintContext by copying modifications back to the source Terrain tiles.</summary>
        ///<remarks>Once the edits to a PaintContext are complete, the modified data in <c>destinationRenderTexture</c> must be applied to the textures stored in each Terrain.
        ///                  ScatterHeightmap will perform this copy, and mark the modified areas for normal map update next frame.
        ///                  This function will also create a delayed action to rebuild collision, physics, pixel error metrics, visibility bounding boxes, and grass, tree, and detail positions.
        ///
        ///                  This function is called internally by <see cref="TerrainPaintUtility.EndPaintHeightmap" />.</remarks>
        ///<param name="editorUndoName">Unique name used for the undo stack.</param>
        ///<seealso cref="PaintContext.GatherHeightmap" />
        ///<seealso cref="PaintContext.ApplyDelayedActions" />
        public void ScatterHeightmap(string editorUndoName)
        {
            var blitMaterial = TerrainPaintUtility.GetHeightBlitMaterial();
            blitMaterial.SetFloat("_Height_Offset", 0.0f);
            blitMaterial.SetFloat("_Height_Scale", 1.0f);

            ScatterInternal(
                t => t.terrain.terrainData.heightmapTexture,
                "PaintContext.ScatterHeightmap",
                blitMaterial: blitMaterial,
                beforeBlit: t =>
                {
                    onTerrainTileBeforePaint?.Invoke(t, ToolAction.PaintHeightmap, editorUndoName);
                    blitMaterial.SetFloat("_Height_Offset", (heightWorldSpaceMin - t.terrain.GetPosition().y) / t.terrain.terrainData.size.y * kNormalizedHeightScale);
                    blitMaterial.SetFloat("_Height_Scale", heightWorldSpaceSize / t.terrain.terrainData.size.y);
                },
                afterBlit: t =>
                {
                    var syncMethod = t.terrain.drawInstanced ?
                        TerrainHeightmapSyncControl.None :          //keep the data on the GPU while painting
                        TerrainHeightmapSyncControl.HeightAndLod;   //sync the heightmaps and the lod info each frame (is important to sync the Lod info so performance stays reasonable)
                    t.terrain.terrainData.DirtyHeightmapRegion(t.clippedTerrainPixels, syncMethod);
                    OnTerrainPainted(t, ToolAction.PaintHeightmap);
                });
        }

        ///<summary>Gathers the Terrain holes information into <c>sourceRenderTexture</c>.</summary>
        ///<remarks>This function collects the Terrain holes data from all Terrain tiles in the Paint Context, and saves the information in <c>sourceRenderTexture</c>.
        ///
        ///                  This function is called internally by <see cref="TerrainPaintUtility.BeginPaintHoles" />.</remarks>
        ///<seealso cref="PaintContext.ScatterHoles" />
        public void GatherHoles()
        {
            GatherInternal(
                t => t.terrain.terrainData.holesTexture,
                new Color(0.0f, 0.0f, 0.0f, 0.0f),
                "PaintContext.GatherHoles");
        }

        ///<summary>Applies an edited Terrain holes PaintContext by copying modifications back to the source Terrain tiles.</summary>
        ///<remarks>Once the edits to a PaintContext are complete, the modified data in <c>destinationRenderTexture</c> must be applied to the textures stored in each Terrain.
        ///                  ScatterHoles performs this copy.
        ///                  This function will also create a delayed action to rebuild collision, physics, grass, trees and details.
        ///
        ///                  This function is called internally by <see cref="TerrainPaintUtility.EndPaintHoles" />.</remarks>
        ///<param name="editorUndoName">Unique name used for the undo stack.</param>
        ///<seealso cref="PaintContext.GatherHoles" />
        ///<seealso cref="PaintContext.ApplyDelayedActions" />
        public void ScatterHoles(string editorUndoName)
        {
            ScatterInternal(
                t =>
                {
                    onTerrainTileBeforePaint?.Invoke(t, ToolAction.PaintHoles, editorUndoName);
                    t.terrain.terrainData.CopyActiveRenderTextureToTexture(TerrainData.HolesTextureName, 0, t.clippedPCPixels, t.clippedTerrainPixels.min, true);
                    OnTerrainPainted(t, ToolAction.PaintHoles);
                    return null;
                },
                "PaintContext.ScatterHoles");
        }

        ///<summary>Gathers the normal information into <c>sourceRenderTexture</c>.</summary>
        ///<remarks>This function collects the terrain mesh normalmap data from all Terrain tiles in the PaintContext into <c>sourceRenderTexture</c>.
        ///
        ///                  This function is called internally by <see cref="TerrainPaintUtility.CollectNormals" />.
        ///
        ///                  Important: There is no corresponding ScatterNormals function, because the normals are not stored, but calculated from the heightmap.</remarks>
        ///<seealso cref="PaintContext" />
        ///<seealso cref="PaintContext.GatherHeightmap" />
        public void GatherNormals()
        {
            GatherInternal(
                t => t.terrain.normalmapTexture,
                new Color(0.5f, 0.5f, 0.5f, 0.5f),
                "PaintContext.GatherNormals");
        }

        private SplatmapUserData GetTerrainLayerUserData(ITerrainInfo context, TerrainLayer terrainLayer = null, bool addLayerIfDoesntExist = false)
        {
            // look up existing user data, if any
            SplatmapUserData userData = (context.userData as SplatmapUserData);
            if (userData != null)
            {
                // check if it is appropriate, return if so
                if ((terrainLayer == null) || (terrainLayer == userData.terrainLayer))
                    return userData;
                else
                    userData = null;
            }

            // otherwise let's build it
            if (userData == null)
            {
                int tileLayerIndex = -1;
                if (terrainLayer != null)
                {
                    // look for the layer on the terrain
                    tileLayerIndex = TerrainPaintUtility.FindTerrainLayerIndex(context.terrain, terrainLayer);
                    if ((tileLayerIndex == -1) && (addLayerIfDoesntExist))
                    {
                        onTerrainTileBeforePaint?.Invoke(context, ToolAction.AddTerrainLayer, "Adding Terrain Layer");
                        tileLayerIndex = TerrainPaintUtility.AddTerrainLayer(context.terrain, terrainLayer);
                    }
                }

                // if we found the layer, build user data
                if (tileLayerIndex != -1)
                {
                    userData = new SplatmapUserData();
                    userData.terrainLayer = terrainLayer;
                    userData.terrainLayerIndex = tileLayerIndex;
                    userData.mapIndex = tileLayerIndex >> 2;
                    userData.channelIndex = tileLayerIndex & 0x3;
                }
                context.userData = userData;
            }
            return userData;
        }

        ///<summary>Gathers the alphamap information into <c>sourceRenderTexture</c>.</summary>
        ///<remarks>This function collects the alphamap data from all Terrain tiles in the PaintContext into <c>sourceRenderTexture</c>.
        ///
        ///                  This function is called internally by <see cref="TerrainPaintUtility.BeginPaintTexture" />.</remarks>
        ///<param name="inputLayer">TerrainLayer used for painting.</param>
        ///<param name="addLayerIfDoesntExist">Set to true to specify that the inputLayer is added to the terrain if it does not already exist. Set to false to specify that terrain layers are not added to the terrain.</param>
        ///<seealso cref="PaintContext.ScatterAlphamap" />
        public void GatherAlphamap(TerrainLayer inputLayer, bool addLayerIfDoesntExist = true)
        {
            if (inputLayer == null)
                return;

            Material copyTerrainLayerMaterial = TerrainPaintUtility.GetCopyTerrainLayerMaterial();
            Vector4[] layerMasks = { new Vector4(1, 0, 0, 0), new Vector4(0, 1, 0, 0), new Vector4(0, 0, 1, 0), new Vector4(0, 0, 0, 1) };

            GatherInternal(
                t =>
                {   // return the texture to be gathered from this terrain tile
                    SplatmapUserData userData = GetTerrainLayerUserData(t, inputLayer, addLayerIfDoesntExist);
                    if (userData != null)
                        return TerrainPaintUtility.GetTerrainAlphaMapChecked(t.terrain, userData.mapIndex);
                    else
                        return null;
                },

                new Color(0.0f, 0.0f, 0.0f, 0.0f),
                "PaintContext.GatherAlphamap",
                copyTerrainLayerMaterial, 0,
                t =>
                {   // before blit -- setup layer mask in the material
                    SplatmapUserData userData = GetTerrainLayerUserData(t);
                    if (userData == null)
                        return;
                    copyTerrainLayerMaterial.SetVector("_LayerMask", layerMasks[userData.channelIndex]);
                });
        }

        ///<summary>Applies an edited alphamap PaintContext by copying modifications back to the source Terrain tiles.</summary>
        ///<remarks>Once the edits to a PaintContext are complete, the modified data in <c>destinationRenderTexture</c> must be applied to the textures stored in each Terrain.
        ///                    ScatterAlphamap will perform this copy, and re-normalize the other alphamap channels to maintain a sum of 1.
        ///                    This function will also create a delayed action to rebuild the basemap LOD texture.
        ///
        ///                    This function is called internally by <see cref="TerrainPaintUtility.EndPaintTexture" />.</remarks>
        ///<param name="editorUndoName">Unique name used for the undo stack.</param>
        ///<seealso cref="PaintContext.GatherAlphamap" />
        ///<seealso cref="PaintContext.ApplyDelayedActions" />
        public void ScatterAlphamap(string editorUndoName)
        {
            Vector4[] layerMasks = { new Vector4(1, 0, 0, 0), new Vector4(0, 1, 0, 0), new Vector4(0, 0, 1, 0), new Vector4(0, 0, 0, 1) };
            Material copyTerrainLayerMaterial = TerrainPaintUtility.GetCopyTerrainLayerMaterial();

            var rtdesc = new RenderTextureDescriptor(destinationRenderTexture.width, destinationRenderTexture.height, GraphicsFormat.R8G8B8A8_UNorm, GraphicsFormat.None);
            rtdesc.sRGB = false;
            rtdesc.useMipMap = false;
            rtdesc.autoGenerateMips = false;
            RenderTexture tempTarget = RenderTexture.GetTemporary(rtdesc);

            ScatterInternal(
                t => // We're going to do ALL of the work in this terrainToRT function, as it is very custom, and we'll just return null to skip the ScatterInternal rendering
                {
                    SplatmapUserData userData = GetTerrainLayerUserData(t);
                    if (userData != null)
                    {
                        onTerrainTileBeforePaint?.Invoke(t, ToolAction.PaintTexture, editorUndoName);

                        int targetAlphamapIndex = userData.mapIndex;
                        int targetChannelIndex = userData.channelIndex;
                        Texture2D targetAlphamapTexture = t.terrain.terrainData.alphamapTextures[targetAlphamapIndex];

                        destinationRenderTexture.filterMode = FilterMode.Point;
                        sourceRenderTexture.filterMode = FilterMode.Point;

                        // iterate all alphamaps to modify them (have to modify all to renormalize)
                        for (int i = 0; i <= t.terrain.terrainData.alphamapTextureCount; i++)   // NOTE: this is a non-standard for loop
                        {
                            // modify the target index last, (skip it the first time)
                            if (i == targetAlphamapIndex)
                                continue;
                            int alphamapIndex = (i == t.terrain.terrainData.alphamapTextureCount) ? targetAlphamapIndex : i;

                            Texture2D alphamapTexture = t.terrain.terrainData.alphamapTextures[alphamapIndex];
                            if ((alphamapTexture.width != targetTextureWidth) || (alphamapTexture.height != targetTextureHeight))
                            {
                                Debug.LogWarning("PaintContext alphamap operations must use the same resolution for all Terrains - mismatched Terrains are ignored.", t.terrain);
                                continue;
                            }

                            RenderTexture.active = tempTarget;
                            GL.PushMatrix();
                            GL.LoadPixelMatrix(0, tempTarget.width, 0, tempTarget.height);
                            {
                                copyTerrainLayerMaterial.SetTexture("_MainTex", destinationRenderTexture);
                                copyTerrainLayerMaterial.SetTexture("_OldAlphaMapTexture", sourceRenderTexture);
                                copyTerrainLayerMaterial.SetTexture("_OriginalTargetAlphaMap", targetAlphamapTexture);

                                copyTerrainLayerMaterial.SetTexture("_AlphaMapTexture", alphamapTexture);
                                copyTerrainLayerMaterial.SetVector("_LayerMask", alphamapIndex == targetAlphamapIndex ? layerMasks[targetChannelIndex] : Vector4.zero);
                                copyTerrainLayerMaterial.SetVector("_OriginalTargetAlphaMask", layerMasks[targetChannelIndex]);
                                copyTerrainLayerMaterial.SetPass(1);

                                TerrainPaintUtility.DrawQuad2(t.clippedPCPixels, t.clippedPCPixels, destinationRenderTexture, t.clippedTerrainPixels, alphamapTexture);
                            }
                            GL.PopMatrix();

                            t.terrain.terrainData.CopyActiveRenderTextureToTexture(TerrainData.AlphamapTextureName, alphamapIndex, t.clippedPCPixels, t.clippedTerrainPixels.min, true);
                        }

                        RenderTexture.active = null;
                        OnTerrainPainted(t, ToolAction.PaintTexture);
                    }
                    return null;
                },
                "PaintContext.ScatterAlphamap",
                copyTerrainLayerMaterial, 0);

            RenderTexture.ReleaseTemporary(tempTarget);
        }

        // Collects modified terrain so that we can update some deferred operations at the mouse up event
        private struct PaintedTerrain
        {
            public Terrain terrain;
            public ToolAction action;
        }
        [AutoStaticsCleanupOnCodeReload]
        private static List<PaintedTerrain> s_PaintedTerrain = new List<PaintedTerrain>();

        private static void OnTerrainPainted(ITerrainInfo tile, ToolAction action)
        {
            for (int i = 0; i < s_PaintedTerrain.Count; ++i)
            {
                if (tile.terrain == s_PaintedTerrain[i].terrain)
                {
                    var pt = s_PaintedTerrain[i];       // round-about assignment here because of struct copy semantics
                    pt.action |= action;
                    s_PaintedTerrain[i] = pt;
                    return;
                }
            }
            s_PaintedTerrain.Add(new PaintedTerrain { terrain = tile.terrain, action = action });
        }

        ///<summary>Flushes the delayed actions created by PaintContext heightmap and alphamap modifications.</summary>
        ///<remarks>Expensive updates that would cause performance issues during painting and sculpting are deferred until the user finishes interacting with them.
        ///                    <see cref="PaintContext.ScatterAlphamap" /> creates a delayed action to rebuild basemap LOD textures.
        ///                    <see cref="PaintContext.ScatterHeightmap" /> creates a delayed action to rebuild collision, physics, pixel error metrics, visibility bounding boxes, and grass, tree, and detail positions.
        ///                    ApplyDelayedActions will immediately apply these delayed actions.
        ///                    ApplyDelayedActions is called automatically on mouse button up, and when the terrain inspector is closed (OnDisable).</remarks>
        public static void ApplyDelayedActions()
        {
            for (int i = 0; i < s_PaintedTerrain.Count; ++i)
            {
                var pt = s_PaintedTerrain[i];
                var terrainData = pt.terrain.terrainData;
                if (terrainData == null)
                    continue;
                if ((pt.action & ToolAction.PaintHeightmap) != 0)
                {
                    terrainData.SyncHeightmap();
                }
                if ((pt.action & ToolAction.PaintHoles) != 0)
                {
                    terrainData.SyncTexture(TerrainData.HolesTextureName);
                }
                if ((pt.action & ToolAction.PaintTexture) != 0)
                {
                    terrainData.SetBaseMapDirty();
                    terrainData.SyncTexture(TerrainData.AlphamapTextureName);
                }
                pt.terrain.editorRenderFlags = TerrainRenderFlags.All;
            }
            s_PaintedTerrain.Clear();
        }
    }
}
