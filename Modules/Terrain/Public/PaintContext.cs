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
            return m_TerrainTiles[terrainIndex].clippedLocalPixels;
        }

        public RectInt GetClippedPixelRectInRenderTexturePixels(int terrainIndex)
        {
            return m_TerrainTiles[terrainIndex].clippedPCPixels;
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
            public TerrainTile(Terrain newTerrain, int tileOriginPixelsX, int tileOriginPixelsY)   { terrain = newTerrain; tileOriginPixels = new Vector2Int(tileOriginPixelsX, tileOriginPixelsY); }

            public Terrain terrain;                 // the terrain object for this tile
            public Vector2Int tileOriginPixels;     // coordinates of this terrain tile in originTerrain target texture pixels

            public RectInt clippedLocalPixels;      // the tile pixels touched by this PaintContext (in local target texture pixels)
            public RectInt clippedPCPixels;         // the tile pixels touched by this PaintContext (in PaintContext/source/destRenderTexture pixels)

            public int mapIndex;                  // for splatmap operations, the splatmap index on this Terrain containing the desired TerrainLayer weight
            public int channelIndex;              // for splatmap operations, the channel on the splatmap containing the desired TerrainLayer weight
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
            TerrainTile tile = new TerrainTile(originTerrain, 0, 0);
            m_TerrainTiles.Add(tile);

            // add horizontal and vertical neighbors
            Terrain horiz = null;
            Terrain vert = null;
            Terrain cornerTerrain = null;

            int horizTileDelta = 0;     // how many tiles to move horizontally
            int vertTileDelta = 0;      // how many tiles to move vertically

            if (wantLeft)
            {
                horizTileDelta = -1;
                horiz = left;
            }
            else if (wantRight)
            {
                horizTileDelta = 1;
                horiz = right;
            }

            if (wantTop)
            {
                vertTileDelta = 1;
                vert = top;
            }
            else if (wantBottom)
            {
                vertTileDelta = -1;
                vert = bottom;
            }

            if (horiz)
            {
                tile = new TerrainTile(horiz, horizTileDelta * (targetTextureWidth - 1), 0);
                m_TerrainTiles.Add(tile);

                // add corner, if we have a link
                if (wantTop && horiz.topNeighbor)
                    cornerTerrain = horiz.topNeighbor;
                else if (wantBottom && horiz.bottomNeighbor)
                    cornerTerrain = horiz.bottomNeighbor;
            }

            if (vert)
            {
                tile = new PaintContext.TerrainTile(vert, 0, vertTileDelta * (targetTextureHeight - 1));
                m_TerrainTiles.Add(tile);

                // add corner, if we have a link
                if (wantLeft && vert.leftNeighbor)
                    cornerTerrain = vert.leftNeighbor;
                else if (wantRight && vert.rightNeighbor)
                    cornerTerrain = vert.rightNeighbor;
            }

            if (cornerTerrain != null)
            {
                tile = new TerrainTile(cornerTerrain, horizTileDelta * (targetTextureWidth - 1), vertTileDelta * (targetTextureHeight - 1));
                m_TerrainTiles.Add(tile);
            }
        }

        internal void ClipTerrainTiles()
        {
            for (int i = 0; i < m_TerrainTiles.Count; i++)
            {
                TerrainTile tile = m_TerrainTiles[i];
                tile.clippedLocalPixels = new RectInt();
                tile.clippedLocalPixels.x = Mathf.Max(0, pixelRect.x - tile.tileOriginPixels.x);
                tile.clippedLocalPixels.y = Mathf.Max(0, pixelRect.y - tile.tileOriginPixels.y);
                tile.clippedLocalPixels.xMax = Mathf.Min(targetTextureWidth, pixelRect.xMax - tile.tileOriginPixels.x);
                tile.clippedLocalPixels.yMax = Mathf.Min(targetTextureHeight, pixelRect.yMax - tile.tileOriginPixels.y);

                tile.clippedPCPixels = new RectInt(
                    tile.clippedLocalPixels.x + tile.tileOriginPixels.x - pixelRect.x,
                    tile.clippedLocalPixels.y + tile.tileOriginPixels.y - pixelRect.y,
                    tile.clippedLocalPixels.width,
                    tile.clippedLocalPixels.height);
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

            GL.PushMatrix();
            GL.LoadPixelMatrix(0, pixelRect.width, 0, pixelRect.height);

            for (int i = 0; i < m_TerrainTiles.Count; i++)
            {
                TerrainTile terrainTile = m_TerrainTiles[i];
                if (terrainTile.clippedLocalPixels.width == 0 || terrainTile.clippedLocalPixels.height == 0)
                    continue;

                Texture sourceTexture = terrainTile.terrain.terrainData.heightmapTexture;
                if ((sourceTexture.width != targetTextureWidth) || (sourceTexture.height != targetTextureHeight))
                {
                    Debug.LogWarning("PaintContext heightmap operations must use the same resolution for all Terrains - mismatched Terrains are ignored.", terrainTile.terrain);
                    continue;
                }

                FilterMode oldFilterMode = sourceTexture.filterMode;

                sourceTexture.filterMode = FilterMode.Point;

                blitMaterial.SetTexture("_MainTex", sourceTexture);
                blitMaterial.SetPass(0);

                TerrainPaintUtility.DrawQuad(terrainTile.clippedPCPixels, terrainTile.clippedLocalPixels, sourceTexture);

                sourceTexture.filterMode = oldFilterMode;
            }

            GL.PopMatrix();

            RenderTexture.active = oldRenderTexture;
        }

        public void ScatterHeightmap(string editorUndoName)
        {
            Material blitMaterial = TerrainPaintUtility.GetBlitMaterial();

            for (int i = 0; i < m_TerrainTiles.Count; i++)
            {
                TerrainTile terrainTile = m_TerrainTiles[i];
                if (terrainTile.clippedLocalPixels.width == 0 || terrainTile.clippedLocalPixels.height == 0)
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
                GL.PushMatrix();
                GL.LoadPixelMatrix(0, heightmap.width, 0, heightmap.height);

                destinationRenderTexture.filterMode = FilterMode.Point;

                blitMaterial.SetTexture("_MainTex", destinationRenderTexture);
                blitMaterial.SetPass(0);

                TerrainPaintUtility.DrawQuad(terrainTile.clippedLocalPixels, terrainTile.clippedPCPixels, destinationRenderTexture);

                GL.PopMatrix();

                terrainTile.terrain.terrainData.UpdateDirtyRegion(terrainTile.clippedLocalPixels.x, terrainTile.clippedLocalPixels.y, terrainTile.clippedLocalPixels.width, terrainTile.clippedLocalPixels.height, !terrainTile.terrain.drawInstanced);
                OnTerrainPainted(terrainTile, ToolAction.PaintHeightmap);
            }
        }

        public void GatherNormals()
        {
            RenderTexture rt = originTerrain.normalmapTexture;

            Material blitMaterial = TerrainPaintUtility.GetBlitMaterial();

            RenderTexture.active = sourceRenderTexture;
            GL.Clear(false, true, new Color(0.5f, 0.5f, 0.5f, 0.5f));
            GL.PushMatrix();
            GL.LoadPixelMatrix(0, pixelRect.width, 0, pixelRect.height);

            for (int i = 0; i < m_TerrainTiles.Count; i++)
            {
                TerrainTile terrainTile = m_TerrainTiles[i];
                if (terrainTile.clippedLocalPixels.width == 0 || terrainTile.clippedLocalPixels.height == 0)
                    continue;

                Texture sourceTexture = terrainTile.terrain.normalmapTexture;
                if ((sourceTexture.width != targetTextureWidth) || (sourceTexture.height != targetTextureHeight))
                {
                    Debug.LogWarning("PaintContext normalmap operations must use the same resolution for all Terrains - mismatched Terrains are ignored.", terrainTile.terrain);
                    continue;
                }

                FilterMode oldFilterMode = sourceTexture.filterMode;

                sourceTexture.filterMode = FilterMode.Point;

                blitMaterial.SetTexture("_MainTex", sourceTexture);
                blitMaterial.SetPass(0);

                TerrainPaintUtility.DrawQuad(terrainTile.clippedPCPixels, terrainTile.clippedLocalPixels, sourceTexture);

                sourceTexture.filterMode = oldFilterMode;
            }

            GL.PopMatrix();

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
            GL.PushMatrix();
            GL.LoadPixelMatrix(0, pixelRect.width, 0, pixelRect.height);

            Vector4[] layerMasks = { new Vector4(1, 0, 0, 0), new Vector4(0, 1, 0, 0), new Vector4(0, 0, 1, 0), new Vector4(0, 0, 0, 1) };

            Material copyTerrainLayerMaterial = TerrainPaintUtility.GetCopyTerrainLayerMaterial();
            for (int i = 0; i < m_TerrainTiles.Count; i++)
            {
                TerrainTile terrainTile = m_TerrainTiles[i];
                if (terrainTile.clippedLocalPixels.width == 0 || terrainTile.clippedLocalPixels.height == 0)
                    continue;

                int tileLayerIndex = TerrainPaintUtility.FindTerrainLayerIndex(terrainTile.terrain, inputLayer);
                if (tileLayerIndex == -1)
                {
                    if (!addLayerIfDoesntExist)
                    {
                        // setting these to zero will prevent them from being used later
                        terrainTile.clippedLocalPixels.width = 0;
                        terrainTile.clippedLocalPixels.height = 0;
                        terrainTile.clippedPCPixels.width = 0;
                        terrainTile.clippedPCPixels.height = 0;
                        continue;
                    }
                    tileLayerIndex = TerrainPaintUtility.AddTerrainLayer(terrainTile.terrain, inputLayer);
                }

                terrainTile.mapIndex = tileLayerIndex >> 2;
                terrainTile.channelIndex = tileLayerIndex & 0x3;

                Texture sourceTexture = TerrainPaintUtility.GetTerrainAlphaMapChecked(terrainTile.terrain, terrainTile.mapIndex);
                if ((sourceTexture.width != targetTextureWidth) || (sourceTexture.height != targetTextureHeight))
                {
                    Debug.LogWarning("PaintContext alphamap operations must use the same resolution for all Terrains - mismatched Terrains are ignored. (" +
                        sourceTexture.width + " x " + sourceTexture.height + ") != (" + targetTextureWidth + " x " + targetTextureHeight + ")",
                        terrainTile.terrain);
                    continue;
                }

                FilterMode oldFilterMode = sourceTexture.filterMode;
                sourceTexture.filterMode = FilterMode.Point;

                copyTerrainLayerMaterial.SetVector("_LayerMask", layerMasks[terrainTile.channelIndex]);
                copyTerrainLayerMaterial.SetTexture("_MainTex", sourceTexture);
                copyTerrainLayerMaterial.SetPass(0);

                TerrainPaintUtility.DrawQuad(terrainTile.clippedPCPixels, terrainTile.clippedLocalPixels, sourceTexture);

                sourceTexture.filterMode = oldFilterMode;
            }

            GL.PopMatrix();

            RenderTexture.active = oldRenderTexture;
        }

        public void ScatterAlphamap(string editorUndoName)
        {
            Vector4[] layerMasks = { new Vector4(1, 0, 0, 0), new Vector4(0, 1, 0, 0), new Vector4(0, 0, 1, 0), new Vector4(0, 0, 0, 1) };

            Material copyTerrainLayerMaterial = TerrainPaintUtility.GetCopyTerrainLayerMaterial();

            for (int i = 0; i < m_TerrainTiles.Count; i++)
            {
                TerrainTile terrainTile = m_TerrainTiles[i];
                if (terrainTile.clippedLocalPixels.width == 0 || terrainTile.clippedLocalPixels.height == 0)
                    continue;

                if (onTerrainTileBeforePaint != null)
                    onTerrainTileBeforePaint(terrainTile, ToolAction.PaintTexture, editorUndoName);

                var rtdesc = new RenderTextureDescriptor(destinationRenderTexture.width, destinationRenderTexture.height, RenderTextureFormat.ARGB32);
                rtdesc.sRGB = false;
                rtdesc.useMipMap = false;
                rtdesc.autoGenerateMips = false;
                RenderTexture destTarget = RenderTexture.GetTemporary(rtdesc);
                RenderTexture.active = destTarget;

                RectInt writeRect = terrainTile.clippedPCPixels;

                Rect readRect = new Rect(
                    writeRect.x / (float)pixelRect.width,
                    writeRect.y / (float)pixelRect.height,
                    writeRect.width / (float)pixelRect.width,
                    writeRect.height / (float)pixelRect.height);

                destinationRenderTexture.filterMode = FilterMode.Point;

                int mapIndex = terrainTile.mapIndex;
                int channelIndex = terrainTile.channelIndex;
                Texture2D sourceTexTargetChannel = terrainTile.terrain.terrainData.alphamapTextures[mapIndex];

                for (int j = 0; j < terrainTile.terrain.terrainData.alphamapTextureCount; j++)
                {
                    Texture2D sourceTex = terrainTile.terrain.terrainData.alphamapTextures[j];
                    if ((sourceTex.width != targetTextureWidth) || (sourceTex.height != targetTextureHeight))
                    {
                        Debug.LogWarning("PaintContext alphamap operations must use the same resolution for all Terrains - mismatched Terrains are ignored.", terrainTile.terrain);
                        continue;
                    }

                    Rect combineRect = new Rect(
                        terrainTile.clippedLocalPixels.x / (float)sourceTex.width,
                        terrainTile.clippedLocalPixels.y / (float)sourceTex.height,
                        terrainTile.clippedLocalPixels.width / (float)sourceTex.width,
                        terrainTile.clippedLocalPixels.height / (float)sourceTex.height);

                    copyTerrainLayerMaterial.SetTexture("_MainTex", destinationRenderTexture);
                    copyTerrainLayerMaterial.SetTexture("_OldAlphaMapTexture", sourceRenderTexture);
                    copyTerrainLayerMaterial.SetTexture("_OriginalTargetAlphaMap", sourceTexTargetChannel);

                    copyTerrainLayerMaterial.SetTexture("_AlphaMapTexture", sourceTex);
                    copyTerrainLayerMaterial.SetVector("_LayerMask", j == mapIndex ? layerMasks[channelIndex] : Vector4.zero);
                    copyTerrainLayerMaterial.SetVector("_OriginalTargetAlphaMask", layerMasks[channelIndex]);
                    copyTerrainLayerMaterial.SetPass(1);

                    GL.PushMatrix();
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
                        Graphics.CopyTexture(destTarget, 0, 0, writeRect.x, writeRect.y, writeRect.width, writeRect.height, mips, 0, 0, terrainTile.clippedLocalPixels.x, terrainTile.clippedLocalPixels.y);
                        mips.GenerateMips();

                        // Copy them into sourceTex.
                        Graphics.CopyTexture(mips, sourceTex);

                        RenderTexture.ReleaseTemporary(mips);
                    }
                    else
                    {
                        GraphicsDeviceType deviceType = SystemInfo.graphicsDeviceType;
                        if (deviceType == GraphicsDeviceType.Metal || deviceType == GraphicsDeviceType.OpenGLCore)
                            sourceTex.ReadPixels(new Rect(writeRect.x, writeRect.y, writeRect.width, writeRect.height), terrainTile.clippedLocalPixels.x, terrainTile.clippedLocalPixels.y);
                        else
                            sourceTex.ReadPixels(new Rect(writeRect.x, destTarget.height - writeRect.y - writeRect.height, writeRect.width, writeRect.height), terrainTile.clippedLocalPixels.x, terrainTile.clippedLocalPixels.y);
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
