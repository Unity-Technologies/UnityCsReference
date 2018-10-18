// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.TerrainAPI
{
    public class PaintContext
    {
        // initialized by constructor
        public Terrain originTerrain { get; }     // the terrain that defines the coordinate system and world space position of this PaintContext
        public RectInt pixelRect { get; }         // the rectangle, in target texture pixels on the originTerrain, that this paint context represents
        public int targetTextureWidth { get; }    // the size of the target texture, per terrain tile
        public int targetTextureHeight { get; }   // the size of the target texture, per terrain tile
        public Vector2 pixelSize { get; }         // size of a paint context pixel in object/terrain/world space

        // initialized by CreateRenderTargets()
        public RenderTexture sourceRenderTexture { get { return m_SourceRenderTexture; } }       // the original data
        public RenderTexture destinationRenderTexture { get { return m_DestinationRenderTexture; } }  // the modified data (you render to this)
        public RenderTexture oldRenderTexture { get { return m_OldRenderTexture; } }          // active render texture at the time CreateRenderTargets() is called, restored on Cleanup()

        public int terrainCount { get { return m_TerrainTiles.Count; } }
        public Terrain GetTerrain(int terrainIndex)
        {
            return m_TerrainTiles[terrainIndex].terrain;
        }

        public RectInt GetClippedPixelRectInTerrainPixels(int terrainIndex)
        {
            return m_TerrainTiles[terrainIndex].clippedLocal;
        }

        public RectInt GetClippedPixelRectInRenderTexturePixels(int terrainIndex)
        {
            Rect vp = m_TerrainTiles[terrainIndex].validPaintRect;
            return new RectInt(
                Mathf.RoundToInt(vp.xMin),
                Mathf.RoundToInt(vp.yMin),
                Mathf.RoundToInt(vp.width),
                Mathf.RoundToInt(vp.height));
        }

        // initialized by constructor
        private List<TerrainTile> m_TerrainTiles;              // all terrain tiles touched by this paint context

        // initialized by CreateRenderTargets()
        private RenderTexture m_SourceRenderTexture;
        private RenderTexture m_DestinationRenderTexture;
        private RenderTexture m_OldRenderTexture;

        internal class TerrainTile
        {
            public TerrainTile() {}
            public TerrainTile(Terrain newTerrain, RectInt newRegion) { rect = newRegion; terrain = newTerrain; }

            public Terrain terrain;               // the terrain object
            public RectInt rect;                  // coordinates of this terrain tile in paint context pixels (essentially originTerrain target texture pixels)
            public RectInt clippedLocal;          // pixelRect in local pixel coordinates (for target texture), clipped to the local tile
            public Rect validPaintRect;           // the area per tile where the source texture was able to read from (in paint context pixels)
            public int mapIndex;                  //
            public int channelIndex;              //

            // offsets used for gather / scatter
            public Vector2Int readOffset;         // offsets used when reading from the terrain heightmap
            public Vector2Int writeOffset;        // offsets used when copying from PaintContext clipped heightmap back to the terrain heightmap
        }

        [Flags]
        internal enum ToolAction
        {
            None = 0,
            PaintHeightmap = 1 << 0,
            PaintTexture = 1 << 1,
        }

        // TerrainPaintUtilityEditor hooks to this event to do automatic undo
        internal static event Action<PaintContext.TerrainTile, ToolAction, string /*editorUndoName*/> onTerrainTileBeforePaint;

        public PaintContext(Terrain terrain, RectInt pixelRect, int targetTextureWidth, int targetTextureHeight)
        {
            this.originTerrain = terrain;
            this.pixelRect = pixelRect;
            this.targetTextureWidth = targetTextureWidth;
            this.targetTextureHeight = targetTextureHeight;
            TerrainData terrainData = terrain.terrainData;
            this.pixelSize = new Vector2(
                terrainData.size.x / (targetTextureWidth - 1.0f),
                terrainData.size.z / (targetTextureHeight - 1.0f));

            FindTerrainTiles();
            ClipTerrainTiles();
        }

        public static PaintContext CreateFromBounds(Terrain terrain, Rect boundsInTerrainSpace, int inputTextureWidth, int inputTextureHeight, int extraBorderPixels = 0)
        {
            return new PaintContext(
                terrain,
                TerrainPaintUtility.CalcPixelRectFromBounds(terrain, boundsInTerrainSpace, inputTextureWidth, inputTextureHeight, extraBorderPixels),
                inputTextureWidth, inputTextureHeight);
        }

        internal void FindTerrainTiles()
        {
            m_TerrainTiles = new List<PaintContext.TerrainTile>();

            Terrain left = originTerrain.leftNeighbor;
            Terrain right = originTerrain.rightNeighbor;
            Terrain top = originTerrain.topNeighbor;
            Terrain bottom = originTerrain.bottomNeighbor;

            bool wantLeft = (pixelRect.x < 0);
            bool wantRight = (pixelRect.xMax > (targetTextureWidth - 1));
            bool wantTop = (pixelRect.yMax > (targetTextureHeight - 1));
            bool wantBottom = (pixelRect.y < 0);

            if (wantLeft && wantRight)
            {
                Debug.LogWarning("PaintContext pixelRect is too large!  It should touch a maximum of 2 Terrains horizontally.");
                wantRight = false;
            }

            if (wantTop && wantBottom)
            {
                Debug.LogWarning("PaintContext pixelRect is too large!  It should touch a maximum of 2 Terrains vertically.");
                wantBottom = false;
            }

            // add center tile
            TerrainTile tile = new TerrainTile(originTerrain, new RectInt(0, 0, targetTextureWidth, targetTextureHeight));
            tile.readOffset = Vector2Int.zero;
            tile.writeOffset = Vector2Int.zero;
            m_TerrainTiles.Add(tile);

            // add horizontal and vertical neighbors
            Terrain horiz = null;
            Terrain vert = null;
            Terrain cornerTerrain = null;

            int xBias = 0;
            int yBias = 0;
            int xReadBias = 0;
            int yReadBias = 0;
            int xWriteBias = 0;
            int yWriteBias = 0;

            if (wantLeft)
            {
                xBias = -1;
                xReadBias = -1;
                xWriteBias = 1;
                horiz = left;
            }
            else if (wantRight)
            {
                xBias = 1;
                xReadBias = 1;
                xWriteBias = -1;
                horiz = right;
            }

            if (wantTop)
            {
                yBias = 1;
                yReadBias = 1;
                yWriteBias = -1;
                vert = top;
            }
            else if (wantBottom)
            {
                yBias = -1;
                yReadBias = -1;
                yWriteBias = 1;
                vert = bottom;
            }

            if (horiz)
            {
                tile = new TerrainTile(horiz, new RectInt(xBias * targetTextureWidth, 0, targetTextureWidth, targetTextureHeight));
                tile.readOffset = new Vector2Int(xReadBias, 0);
                tile.writeOffset = new Vector2Int(xWriteBias, 0);
                m_TerrainTiles.Add(tile);

                // add corner, if we have a link
                if (wantTop && horiz.topNeighbor)
                    cornerTerrain = horiz.topNeighbor;
                else if (wantBottom && horiz.bottomNeighbor)
                    cornerTerrain = horiz.bottomNeighbor;
            }

            if (vert)
            {
                tile = new PaintContext.TerrainTile(vert, new RectInt(0, yBias * targetTextureHeight, targetTextureWidth, targetTextureHeight));
                tile.readOffset = new Vector2Int(0, yReadBias);
                tile.writeOffset = new Vector2Int(0, yWriteBias);
                m_TerrainTiles.Add(tile);

                // add corner, if we have a link
                if (wantLeft && vert.leftNeighbor)
                    cornerTerrain = vert.leftNeighbor;
                else if (wantRight && vert.rightNeighbor)
                    cornerTerrain = vert.rightNeighbor;
            }

            if (cornerTerrain != null)
            {
                tile = new TerrainTile(cornerTerrain, new RectInt(xBias * targetTextureWidth, yBias * targetTextureHeight, targetTextureWidth, targetTextureHeight));
                tile.readOffset = new Vector2Int(xReadBias, yReadBias);
                tile.writeOffset = new Vector2Int(xWriteBias, yWriteBias);
                m_TerrainTiles.Add(tile);
            }
        }

        internal void ClipTerrainTiles()
        {
            for (int i = 0; i < m_TerrainTiles.Count; i++)
            {
                TerrainTile tile = m_TerrainTiles[i];
                tile.clippedLocal = new RectInt();
                tile.clippedLocal.x = Mathf.Max(0, pixelRect.x - tile.rect.x);
                tile.clippedLocal.y = Mathf.Max(0, pixelRect.y - tile.rect.y);
                tile.clippedLocal.xMax = Mathf.Min(tile.rect.width, pixelRect.xMax - tile.rect.x);
                tile.clippedLocal.yMax = Mathf.Min(tile.rect.height, pixelRect.yMax - tile.rect.y);

                tile.validPaintRect = new Rect(
                    tile.clippedLocal.x + tile.rect.x - pixelRect.x,
                    tile.clippedLocal.y + tile.rect.y - pixelRect.y,
                    tile.clippedLocal.width,
                    tile.clippedLocal.height);
            }
        }

        public void CreateRenderTargets(RenderTextureFormat colorFormat)
        {
            m_SourceRenderTexture = RenderTexture.GetTemporary(pixelRect.width, pixelRect.height, 0, colorFormat, RenderTextureReadWrite.Linear);
            m_DestinationRenderTexture = RenderTexture.GetTemporary(pixelRect.width, pixelRect.height, 0, colorFormat, RenderTextureReadWrite.Linear);
            m_SourceRenderTexture.wrapMode = TextureWrapMode.Clamp;
            m_SourceRenderTexture.filterMode = FilterMode.Point;
            m_OldRenderTexture = RenderTexture.active;
        }

        public void Cleanup(bool restoreRenderTexture = true)
        {
            if (restoreRenderTexture)
            {
                RenderTexture.active = m_OldRenderTexture;
            }
            RenderTexture.ReleaseTemporary(m_SourceRenderTexture);
            RenderTexture.ReleaseTemporary(m_DestinationRenderTexture);
            m_SourceRenderTexture = null;
            m_DestinationRenderTexture = null;
            m_OldRenderTexture = null;
        }

        public void GatherHeightmap()
        {
            Material blitMaterial = TerrainPaintUtility.GetBlitMaterial();

            RenderTexture.active = sourceRenderTexture;
            GL.Clear(false, true, new Color(0.0f, 0.0f, 0.0f, 0.0f));

            for (int i = 0; i < m_TerrainTiles.Count; i++)
            {
                TerrainTile terrainTile = m_TerrainTiles[i];
                if (terrainTile.clippedLocal.width == 0 || terrainTile.clippedLocal.height == 0)
                    continue;

                Texture sourceTexture = terrainTile.terrain.terrainData.heightmapTexture;
                if ((sourceTexture.width != targetTextureWidth) || (sourceTexture.height != targetTextureHeight))
                {
                    Debug.LogWarning("PaintContext heightmap operations must use the same resolution for all Terrains - mismatched Terrains are ignored.", terrainTile.terrain);
                    continue;
                }

                Rect readRect = new Rect(
                    (terrainTile.clippedLocal.x + terrainTile.readOffset.x) / (float)targetTextureWidth,
                    (terrainTile.clippedLocal.y + terrainTile.readOffset.y) / (float)targetTextureHeight,
                    (terrainTile.clippedLocal.width) / (float)targetTextureWidth,
                    (terrainTile.clippedLocal.height) / (float)targetTextureHeight);

                FilterMode oldFilterMode = sourceTexture.filterMode;

                sourceTexture.filterMode = FilterMode.Point;

                blitMaterial.SetTexture("_MainTex", sourceTexture);
                blitMaterial.SetPass(0);

                TerrainPaintUtility.DrawQuad(pixelRect.width, pixelRect.height, readRect, terrainTile.validPaintRect);

                sourceTexture.filterMode = oldFilterMode;
            }

            RenderTexture.active = oldRenderTexture;
        }

        public void ScatterHeightmap(string editorUndoName)
        {
            Material blitMaterial = TerrainPaintUtility.GetBlitMaterial();

            for (int i = 0; i < m_TerrainTiles.Count; i++)
            {
                TerrainTile terrainTile = m_TerrainTiles[i];
                if (terrainTile.clippedLocal.width == 0 || terrainTile.clippedLocal.height == 0)
                    continue;

                RenderTexture heightmap = terrainTile.terrain.terrainData.heightmapTexture;
                if ((heightmap.width != targetTextureWidth) || (heightmap.height != targetTextureHeight))
                {
                    Debug.LogWarning("PaintContext heightmap operations must use the same resolution for all Terrains - mismatched Terrains are ignored.", terrainTile.terrain);
                    continue;
                }

                if (onTerrainTileBeforePaint != null)
                    onTerrainTileBeforePaint(terrainTile, ToolAction.PaintHeightmap, editorUndoName);

                RenderTexture.active = heightmap;

                Rect readRect = new Rect(
                    (terrainTile.clippedLocal.x + terrainTile.rect.x - pixelRect.x + terrainTile.writeOffset.x) / (float)pixelRect.width,
                    (terrainTile.clippedLocal.y + terrainTile.rect.y - pixelRect.y + terrainTile.writeOffset.y) / (float)pixelRect.height,
                    (terrainTile.clippedLocal.width) / (float)pixelRect.width,
                    (terrainTile.clippedLocal.height) / (float)pixelRect.height);

                Rect writeRect = new Rect(
                    terrainTile.clippedLocal.x,
                    terrainTile.clippedLocal.y,
                    terrainTile.clippedLocal.width,
                    terrainTile.clippedLocal.height);

                destinationRenderTexture.filterMode = FilterMode.Point;

                blitMaterial.SetTexture("_MainTex", destinationRenderTexture);
                blitMaterial.SetPass(0);

                TerrainPaintUtility.DrawQuad(heightmap.width, heightmap.height, readRect, writeRect);

                terrainTile.terrain.terrainData.UpdateDirtyRegion(terrainTile.clippedLocal.x, terrainTile.clippedLocal.y, terrainTile.clippedLocal.width, terrainTile.clippedLocal.height, !terrainTile.terrain.drawInstanced);
                OnTerrainPainted(terrainTile, ToolAction.PaintHeightmap);
            }
        }

        public void GatherNormals()
        {
            RenderTexture rt = originTerrain.normalmapTexture;

            Material blitMaterial = TerrainPaintUtility.GetBlitMaterial();

            RenderTexture.active = sourceRenderTexture;
            GL.Clear(false, true, new Color(0.5f, 0.5f, 0.5f, 0.5f));

            for (int i = 0; i < m_TerrainTiles.Count; i++)
            {
                TerrainTile terrainTile = m_TerrainTiles[i];
                if (terrainTile.clippedLocal.width == 0 || terrainTile.clippedLocal.height == 0)
                    continue;

                Texture sourceTexture = terrainTile.terrain.normalmapTexture;
                if ((sourceTexture.width != targetTextureWidth) || (sourceTexture.height != targetTextureHeight))
                {
                    Debug.LogWarning("PaintContext normalmap operations must use the same resolution for all Terrains - mismatched Terrains are ignored.", terrainTile.terrain);
                    continue;
                }

                Rect readRect = new Rect(
                    (terrainTile.clippedLocal.x + terrainTile.readOffset.x) / (float)targetTextureWidth,
                    (terrainTile.clippedLocal.y + terrainTile.readOffset.y) / (float)targetTextureHeight,
                    (terrainTile.clippedLocal.width) / (float)targetTextureWidth,
                    (terrainTile.clippedLocal.height) / (float)targetTextureHeight);

                FilterMode oldFilterMode = sourceTexture.filterMode;

                sourceTexture.filterMode = FilterMode.Point;

                blitMaterial.SetTexture("_MainTex", sourceTexture);
                blitMaterial.SetPass(0);

                TerrainPaintUtility.DrawQuad(pixelRect.width, pixelRect.height, readRect, terrainTile.validPaintRect);

                sourceTexture.filterMode = oldFilterMode;
            }

            RenderTexture.active = oldRenderTexture;
        }

        public void GatherAlphamap(TerrainLayer inputLayer, bool addLayerIfDoesntExist = true)
        {
            if (inputLayer == null)
                return;

            int terrainLayerIndex = TerrainPaintUtility.FindTerrainLayerIndex(originTerrain, inputLayer);
            if (terrainLayerIndex == -1 && addLayerIfDoesntExist)
                terrainLayerIndex = TerrainPaintUtility.AddTerrainLayer(originTerrain, inputLayer);

            RenderTexture.active = sourceRenderTexture;
            GL.Clear(false, true, new Color(0.0f, 0.0f, 0.0f, 0.0f));

            Vector4[] layerMasks = { new Vector4(1, 0, 0, 0), new Vector4(0, 1, 0, 0), new Vector4(0, 0, 1, 0), new Vector4(0, 0, 0, 1) };

            Material copyTerrainLayerMaterial = TerrainPaintUtility.GetCopyTerrainLayerMaterial();
            for (int i = 0; i < m_TerrainTiles.Count; i++)
            {
                TerrainTile terrainTile = m_TerrainTiles[i];
                if (terrainTile.clippedLocal.width == 0 || terrainTile.clippedLocal.height == 0)
                    continue;

                Rect readRect = new Rect(
                    (terrainTile.clippedLocal.x + terrainTile.readOffset.x) / (float)targetTextureWidth,
                    (terrainTile.clippedLocal.y + terrainTile.readOffset.y) / (float)targetTextureHeight,
                    (terrainTile.clippedLocal.width) / (float)targetTextureWidth,
                    (terrainTile.clippedLocal.height) / (float)targetTextureHeight);

                int tileLayerIndex = TerrainPaintUtility.FindTerrainLayerIndex(terrainTile.terrain, inputLayer);
                if (tileLayerIndex == -1)
                {
                    if (!addLayerIfDoesntExist)
                    {
                        // setting these to zero will prevent them from being used later
                        terrainTile.clippedLocal.width = 0;
                        terrainTile.clippedLocal.height = 0;
                        terrainTile.validPaintRect.width = 0;
                        terrainTile.validPaintRect.height = 0;
                        continue;
                    }
                    tileLayerIndex = TerrainPaintUtility.AddTerrainLayer(terrainTile.terrain, inputLayer);
                }

                terrainTile.mapIndex = tileLayerIndex >> 2;
                terrainTile.channelIndex = tileLayerIndex & 0x3;

                Texture sourceTexture = TerrainPaintUtility.GetTerrainAlphaMapChecked(terrainTile.terrain, terrainTile.mapIndex);
                if ((sourceTexture.width != targetTextureWidth) || (sourceTexture.height != targetTextureHeight))
                {
                    Debug.LogWarning("PaintContext alphamap operations must use the same resolution for all Terrains - mismatched Terrains are ignored.", terrainTile.terrain);
                    continue;
                }

                FilterMode oldFilterMode = sourceTexture.filterMode;
                sourceTexture.filterMode = FilterMode.Point;

                copyTerrainLayerMaterial.SetVector("_LayerMask", layerMasks[terrainTile.channelIndex]);
                copyTerrainLayerMaterial.SetTexture("_MainTex", sourceTexture);
                copyTerrainLayerMaterial.SetPass(0);

                TerrainPaintUtility.DrawQuad(pixelRect.width, pixelRect.height, readRect, terrainTile.validPaintRect);

                sourceTexture.filterMode = oldFilterMode;
            }

            RenderTexture.active = oldRenderTexture;
        }

        public void ScatterAlphamap(string editorUndoName)
        {
            Vector4[] layerMasks = { new Vector4(1, 0, 0, 0), new Vector4(0, 1, 0, 0), new Vector4(0, 0, 1, 0), new Vector4(0, 0, 0, 1) };

            Material copyTerrainLayerMaterial = TerrainPaintUtility.GetCopyTerrainLayerMaterial();

            for (int i = 0; i < m_TerrainTiles.Count; i++)
            {
                TerrainTile terrainTile = m_TerrainTiles[i];
                if (terrainTile.clippedLocal.width == 0 || terrainTile.clippedLocal.height == 0)
                    continue;

                if (onTerrainTileBeforePaint != null)
                    onTerrainTileBeforePaint(terrainTile, ToolAction.PaintTexture, editorUndoName);

                var rtdesc = new RenderTextureDescriptor(destinationRenderTexture.width, destinationRenderTexture.height, RenderTextureFormat.ARGB32);
                rtdesc.sRGB = false;
                rtdesc.useMipMap = false;
                rtdesc.autoGenerateMips = false;
                RenderTexture destTarget = RenderTexture.GetTemporary(rtdesc);
                RenderTexture.active = destTarget;

                var writeRect = new RectInt(
                    terrainTile.clippedLocal.x + terrainTile.rect.x - pixelRect.x + terrainTile.writeOffset.x,
                    terrainTile.clippedLocal.y + terrainTile.rect.y - pixelRect.y + terrainTile.writeOffset.y,
                    terrainTile.clippedLocal.width,
                    terrainTile.clippedLocal.height);

                var readRect = new Rect(
                    writeRect.x / (float)pixelRect.width,
                    writeRect.y / (float)pixelRect.height,
                    writeRect.width / (float)pixelRect.width,
                    writeRect.height / (float)pixelRect.height);

                destinationRenderTexture.filterMode = FilterMode.Point;

                for (int j = 0; j < terrainTile.terrain.terrainData.alphamapTextureCount; j++)
                {
                    Texture2D sourceTex = terrainTile.terrain.terrainData.alphamapTextures[j];
                    if ((sourceTex.width != targetTextureWidth) || (sourceTex.height != targetTextureHeight))
                    {
                        Debug.LogWarning("PaintContext alphamap operations must use the same resolution for all Terrains - mismatched Terrains are ignored.", terrainTile.terrain);
                        continue;
                    }

                    int mapIndex = terrainTile.mapIndex;
                    int channelIndex = terrainTile.channelIndex;

                    Rect combineRect = new Rect(
                        terrainTile.clippedLocal.x / (float)sourceTex.width,
                        terrainTile.clippedLocal.y / (float)sourceTex.height,
                        terrainTile.clippedLocal.width / (float)sourceTex.width,
                        terrainTile.clippedLocal.height / (float)sourceTex.height);

                    copyTerrainLayerMaterial.SetTexture("_MainTex", destinationRenderTexture);
                    copyTerrainLayerMaterial.SetTexture("_OldAlphaMapTexture", sourceRenderTexture);
                    copyTerrainLayerMaterial.SetTexture("_AlphaMapTexture", sourceTex);
                    copyTerrainLayerMaterial.SetVector("_LayerMask", j == mapIndex ? layerMasks[channelIndex] : Vector4.zero);
                    copyTerrainLayerMaterial.SetPass(1);

                    GL.PushMatrix();
                    GL.LoadOrtho();
                    GL.LoadPixelMatrix(0, destTarget.width, 0, destTarget.height);

                    GL.Begin(GL.QUADS);
                    GL.Color(new Color(1.0f, 1.0f, 1.0f, 1.0f));

                    GL.MultiTexCoord2(0, readRect.x, readRect.y);
                    GL.MultiTexCoord2(1, combineRect.x, combineRect.y);
                    GL.Vertex3(writeRect.x, writeRect.y, 0.0f);
                    GL.MultiTexCoord2(0, readRect.x, readRect.yMax);
                    GL.MultiTexCoord2(1, combineRect.x, combineRect.yMax);
                    GL.Vertex3(writeRect.x, writeRect.yMax, 0.0f);
                    GL.MultiTexCoord2(0, readRect.xMax, readRect.yMax);
                    GL.MultiTexCoord2(1, combineRect.xMax, combineRect.yMax);
                    GL.Vertex3(writeRect.xMax, writeRect.yMax, 0.0f);
                    GL.MultiTexCoord2(0, readRect.xMax, readRect.y);
                    GL.MultiTexCoord2(1, combineRect.xMax, combineRect.y);
                    GL.Vertex3(writeRect.xMax, writeRect.y, 0.0f);

                    GL.End();
                    GL.PopMatrix();

                    if (TerrainPaintUtility.paintTextureUsesCopyTexture)
                    {
                        var rtdesc2 = new RenderTextureDescriptor(sourceTex.width, sourceTex.height, RenderTextureFormat.ARGB32);
                        rtdesc2.sRGB = false;
                        rtdesc2.useMipMap = true;
                        rtdesc2.autoGenerateMips = false;
                        var mips = RenderTexture.GetTemporary(rtdesc2);
                        if (!mips.IsCreated())
                            mips.Create();

                        // Composes mip0 in a RT with full mipchain.
                        Graphics.CopyTexture(sourceTex, 0, 0, mips, 0, 0);
                        Graphics.CopyTexture(destTarget, 0, 0, writeRect.x, writeRect.y, writeRect.width, writeRect.height, mips, 0, 0, terrainTile.clippedLocal.x, terrainTile.clippedLocal.y);
                        mips.GenerateMips();

                        // Copy them into sourceTex.
                        Graphics.CopyTexture(mips, sourceTex);

                        RenderTexture.ReleaseTemporary(mips);
                    }
                    else
                    {
                        GraphicsDeviceType deviceType = SystemInfo.graphicsDeviceType;
                        if (deviceType == GraphicsDeviceType.Metal || deviceType == GraphicsDeviceType.OpenGLCore)
                            sourceTex.ReadPixels(new Rect(writeRect.x, writeRect.y, writeRect.width, writeRect.height), terrainTile.clippedLocal.x, terrainTile.clippedLocal.y);
                        else
                            sourceTex.ReadPixels(new Rect(writeRect.x, destTarget.height - writeRect.y - writeRect.height, writeRect.width, writeRect.height), terrainTile.clippedLocal.x, terrainTile.clippedLocal.y);
                        sourceTex.Apply();
                    }
                }

                RenderTexture.active = null;
                RenderTexture.ReleaseTemporary(destTarget);

                OnTerrainPainted(terrainTile, ToolAction.PaintTexture);
            }
        }

        // Collects modified terrain so that we can update some deferred operations at the mouse up event
        private class PaintedTerrain
        {
            public Terrain terrain;
            public ToolAction action;
        };
        private static List<PaintedTerrain> s_PaintedTerrain = new List<PaintedTerrain>();

        private static void OnTerrainPainted(PaintContext.TerrainTile tile, ToolAction action)
        {
            for (int i = 0; i < s_PaintedTerrain.Count; ++i)
            {
                if (tile.terrain == s_PaintedTerrain[i].terrain)
                {
                    s_PaintedTerrain[i].action |= action;
                    return;
                }
            }
            s_PaintedTerrain.Add(new PaintedTerrain { terrain = tile.terrain, action = action });
        }

        public static void ApplyDelayedActions()
        {
            for (int i = 0; i < s_PaintedTerrain.Count; ++i)
            {
                var pt = s_PaintedTerrain[i];
                if ((pt.action & ToolAction.PaintHeightmap) != 0)
                {
                    pt.terrain.ApplyDelayedHeightmapModification();
                }
                if ((pt.action & ToolAction.PaintTexture) != 0)
                {
                    var terrainData = pt.terrain.terrainData;
                    if (terrainData == null)
                        continue;
                    terrainData.SetBaseMapDirty();
                    if (TerrainPaintUtility.paintTextureUsesCopyTexture)
                    {
                        // pull the data from GPU to CPU
                        var rtdesc = new RenderTextureDescriptor(terrainData.alphamapResolution, terrainData.alphamapResolution, RenderTextureFormat.ARGB32);
                        rtdesc.sRGB = false;
                        rtdesc.useMipMap = false;
                        rtdesc.autoGenerateMips = false;
                        RenderTexture tmp = RenderTexture.GetTemporary(rtdesc);
                        for (int c = 0; c < terrainData.alphamapTextureCount; ++c)
                        {
                            Graphics.Blit(terrainData.alphamapTextures[c], tmp);
                            terrainData.alphamapTextures[c].ReadPixels(new Rect(0, 0, rtdesc.width, rtdesc.height), 0, 0, true);
                        }
                        RenderTexture.ReleaseTemporary(tmp);
                    }
                }
            }

            s_PaintedTerrain.Clear();
        }
    }
}
