// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine
{
    public static class TerrainPaintUtility
    {
        public enum BuiltinPaintMaterialPasses
        {
            RaiseLowerHeight = 0,
            StampHeight,
            SetHeights,
            SmoothHeights,
            PaintTexture,
        }

        private static Material s_BuiltinPaintMaterial = null;
        public static Material GetBuiltinPaintMaterial()
        {
            if (s_BuiltinPaintMaterial == null)
                s_BuiltinPaintMaterial = new Material(Shader.Find("Hidden/TerrainEngine/PaintHeight"));
            return s_BuiltinPaintMaterial;
        }

        public class TerrainTile
        {
            public TerrainTile() { terrain = null; }
            public TerrainTile(Terrain newTerrain, RectInt newRegion) { rect = newRegion; terrain = newTerrain; }
            public Terrain terrain;
            public RectInt rect;               // pixel coordinates of this terrain tile in the paint context (a locally built space relative to the active 'center' tile)

            public int mapIndex;
            public int channelIndex;

            // offsets used for gather / scatter
            public Vector2Int readOffset;         // offsets used when reading from the terrain heightmap
            public Vector2Int writeOffset;        // offsets used when copying from PaintContext clipped heightmap back to the terrain heightmap
        }

        public class PaintContext
        {
            public RectInt brushRect;                       // the rectangle represented by this paint context, in target texture pixels (for the active terrain tile)
            public TerrainTile[] terrainTiles;              // all terrain tiles touched by this paint context
            public RectInt[] clippedTiles;                  // the intersection of brushRect with each of the terrain tiles above, clipped into local tile pixels

            public RenderTexture sourceRenderTexture;
            public RenderTexture destinationRenderTexture;

            public RenderTexture oldRenderTexture;          // active render texture before PaintContext was initialized
        }

        [Flags]
        public enum ToolAction
        {
            None = 0,
            PaintHeightmap = 1 << 0,
            PaintTexture = 1 << 1,
        }

        private static bool paintTextureUsesCopyTexture
        {
            get
            {
                const CopyTextureSupport RT2TexAndTex2RT = CopyTextureSupport.RTToTexture | CopyTextureSupport.TextureToRT;
                return (SystemInfo.copyTextureSupport & RT2TexAndTex2RT) == RT2TexAndTex2RT;
            }
        }

        static PaintContext InitializePaintContext(Terrain terrain, Rect bounds, int inputTextureWidth, int inputTextureHeight, RenderTextureFormat colorFormat)
        {
            PaintContext ctx = new PaintContext();
            ctx.brushRect = CalcBrushRectInPixels(terrain, bounds, inputTextureWidth, inputTextureHeight);         // CalcBrushRect(terrain, bounds, inputTextureWidth, inputTextureHeight);
            ctx.terrainTiles = FindTerrainTiles(terrain, inputTextureWidth, inputTextureHeight, ctx.brushRect);
            ctx.clippedTiles = ClipTerrainTiles(ctx.terrainTiles, ctx.brushRect);
            ctx.sourceRenderTexture = RenderTexture.GetTemporary(ctx.brushRect.width, ctx.brushRect.height, 0, colorFormat, RenderTextureReadWrite.Linear);
            ctx.destinationRenderTexture = RenderTexture.GetTemporary(ctx.brushRect.width, ctx.brushRect.height, 0, colorFormat, RenderTextureReadWrite.Linear);
            ctx.sourceRenderTexture.wrapMode = TextureWrapMode.Clamp;
            ctx.sourceRenderTexture.filterMode = FilterMode.Point;
            ctx.oldRenderTexture = RenderTexture.active;
            return ctx;
        }

        public static void ReleaseContextResources(PaintContext ctx)
        {
            RenderTexture.active = ctx.oldRenderTexture;
            RenderTexture.ReleaseTemporary(ctx.sourceRenderTexture);
            RenderTexture.ReleaseTemporary(ctx.destinationRenderTexture);
            ctx.sourceRenderTexture = null;
            ctx.destinationRenderTexture = null;
            ctx.oldRenderTexture = null;
        }

        public static Rect CalculateBrushRectInTerrainUnits(Terrain terrain, Vector2 uv, float brushSize)
        {
            Vector3 terrainSize = terrain.terrainData.size;
            return new Rect(uv * new Vector2(terrainSize.x, terrainSize.z) - Vector2.one * brushSize * 0.5f, Vector2.one * brushSize);
        }

        // Collects modified terrain so that we can update some deferred operations at the mouse up event
        private class PaintedTerrain
        {
            public Terrain terrain;
            public ToolAction action;
        };
        private static List<PaintedTerrain> s_PaintedTerrain = new List<PaintedTerrain>();

        private static void OnTerrainPainted(TerrainTile tile, ToolAction action)
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

        public static void FlushAllPaints()
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
                    if (paintTextureUsesCopyTexture)
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

        // TerrainPaintUtilityEditor hooks to this event to do automatic undo
        internal static event Action<TerrainTile, ToolAction, string /*editorUndoName*/> onTerrainTileBeforePaint;

        public static PaintContext BeginPaintHeightmap(Terrain terrain, Rect bounds) // bounds in terrain space units
        {
            RenderTexture rt = terrain.terrainData.heightmapTexture;

            RenderTextureFormat colorFormat = rt.format;
            int heightmapWidth = rt.width;
            int heightmapHeight = rt.height;

            PaintContext ctx = InitializePaintContext(terrain, bounds, heightmapWidth, heightmapHeight, rt.format);

            Material blitMaterial = GetBlitMaterial();

            RenderTexture.active = ctx.sourceRenderTexture;

            for (int i = 0; i < ctx.terrainTiles.Length; i++)
            {
                if (ctx.clippedTiles[i].width == 0 || ctx.clippedTiles[i].height == 0)
                    continue;

                TerrainTile terrainTile = ctx.terrainTiles[i];

                Rect readRect = new Rect(
                    (ctx.clippedTiles[i].x + terrainTile.readOffset.x) / (float)heightmapWidth,
                    (ctx.clippedTiles[i].y + terrainTile.readOffset.y) / (float)heightmapHeight,
                    (ctx.clippedTiles[i].width) / (float)heightmapWidth,
                    (ctx.clippedTiles[i].height) / (float)heightmapHeight);

                Rect writeRect = new Rect(
                    ctx.clippedTiles[i].x + terrainTile.rect.x - ctx.brushRect.x,
                    ctx.clippedTiles[i].y + terrainTile.rect.y - ctx.brushRect.y,
                    ctx.clippedTiles[i].width,
                    ctx.clippedTiles[i].height);

                Texture sourceTexture = terrainTile.terrain.terrainData.heightmapTexture;
                FilterMode oldFilterMode = sourceTexture.filterMode;

                sourceTexture.filterMode = FilterMode.Point;

                blitMaterial.SetTexture("_MainTex", sourceTexture);
                blitMaterial.SetPass(0);

                DrawQuad(ctx.brushRect.width, ctx.brushRect.height, readRect, writeRect);

                sourceTexture.filterMode = oldFilterMode;
            }

            RenderTexture.active = ctx.oldRenderTexture;
            return ctx;
        }

        public static void EndPaintHeightmap(PaintContext ctx, string editorUndoName)
        {
            Material blitMaterial = GetBlitMaterial();

            for (int i = 0; i < ctx.terrainTiles.Length; i++)
            {
                if (ctx.clippedTiles[i].width == 0 || ctx.clippedTiles[i].height == 0)
                    continue;

                TerrainTile terrainTile = ctx.terrainTiles[i];

                if (onTerrainTileBeforePaint != null)
                    onTerrainTileBeforePaint(terrainTile, ToolAction.PaintHeightmap, editorUndoName);

                RenderTexture heightmap = terrainTile.terrain.terrainData.heightmapTexture;
                RenderTexture.active = heightmap;

                Rect readRect = new Rect(
                    (ctx.clippedTiles[i].x + terrainTile.rect.x - ctx.brushRect.x + terrainTile.writeOffset.x) / (float)ctx.brushRect.width,
                    (ctx.clippedTiles[i].y + terrainTile.rect.y - ctx.brushRect.y + terrainTile.writeOffset.y) / (float)ctx.brushRect.height,
                    (ctx.clippedTiles[i].width) / (float)ctx.brushRect.width,
                    (ctx.clippedTiles[i].height) / (float)ctx.brushRect.height);

                Rect writeRect = new Rect(
                    ctx.clippedTiles[i].x,
                    ctx.clippedTiles[i].y,
                    ctx.clippedTiles[i].width,
                    ctx.clippedTiles[i].height);

                ctx.destinationRenderTexture.filterMode = FilterMode.Point;

                blitMaterial.SetTexture("_MainTex", ctx.destinationRenderTexture);
                blitMaterial.SetPass(0);

                DrawQuad(heightmap.width, heightmap.height, readRect, writeRect);

                terrainTile.terrain.terrainData.UpdateDirtyRegion(ctx.clippedTiles[i].x, ctx.clippedTiles[i].y, ctx.clippedTiles[i].width, ctx.clippedTiles[i].height, !terrainTile.terrain.drawInstanced);
                OnTerrainPainted(terrainTile, ToolAction.PaintHeightmap);
            }

            ReleaseContextResources(ctx);
        }

        public static PaintContext CollectNormals(Terrain terrain, Rect bounds)
        {
            RenderTexture rt = terrain.normalmapTexture;

            RenderTextureFormat colorFormat = rt.format;
            int heightmapWidth = rt.width;
            int heightmapHeight = rt.height;

            PaintContext ctx = InitializePaintContext(terrain, bounds, heightmapWidth, heightmapHeight, rt.format);

            Material blitMaterial = GetBlitMaterial();

            RenderTexture.active = ctx.sourceRenderTexture;

            for (int i = 0; i < ctx.terrainTiles.Length; i++)
            {
                if (ctx.clippedTiles[i].width == 0 || ctx.clippedTiles[i].height == 0)
                    continue;

                TerrainTile terrainTile = ctx.terrainTiles[i];

                Rect readRect = new Rect(
                    (ctx.clippedTiles[i].x + terrainTile.readOffset.x) / (float)heightmapWidth,
                    (ctx.clippedTiles[i].y + terrainTile.readOffset.y) / (float)heightmapHeight,
                    (ctx.clippedTiles[i].width) / (float)heightmapWidth,
                    (ctx.clippedTiles[i].height) / (float)heightmapHeight);

                Rect writeRect = new Rect(
                    ctx.clippedTiles[i].x + terrainTile.rect.x - ctx.brushRect.x,
                    ctx.clippedTiles[i].y + terrainTile.rect.y - ctx.brushRect.y,
                    ctx.clippedTiles[i].width,
                    ctx.clippedTiles[i].height);

                Texture sourceTexture = terrainTile.terrain.normalmapTexture;
                FilterMode oldFilterMode = sourceTexture.filterMode;

                sourceTexture.filterMode = FilterMode.Point;

                blitMaterial.SetTexture("_MainTex", sourceTexture);
                blitMaterial.SetPass(0);

                DrawQuad(ctx.brushRect.width, ctx.brushRect.height, readRect, writeRect);

                sourceTexture.filterMode = oldFilterMode;
            }

            RenderTexture.active = ctx.oldRenderTexture;
            return ctx;
        }

        public static PaintContext BeginPaintTexture(Terrain terrain, Rect bounds, TerrainLayer inputLayer)
        {
            if (inputLayer == null)
                return null;

            int terrainLayerIndex = FindTerrainLayerIndex(terrain, inputLayer);
            if (terrainLayerIndex == -1)
                terrainLayerIndex = AddTerrainLayer(terrain, inputLayer);

            Texture2D inputTexture = GetTerrainAlphaMapChecked(terrain, terrainLayerIndex >> 2);

            int inputTextureWidth = inputTexture.width;
            int inputTextureHeight = inputTexture.height;

            PaintContext ctx = InitializePaintContext(terrain, bounds, inputTextureWidth, inputTextureHeight, RenderTextureFormat.R8);

            RenderTexture.active = ctx.sourceRenderTexture;

            Vector4[] layerMasks = { new Vector4(1, 0, 0, 0), new Vector4(0, 1, 0, 0), new Vector4(0, 0, 1, 0), new Vector4(0, 0, 0, 1) };

            Material copyTerrainLayerMaterial = GetCopyTerrainLayerMaterial();
            for (int i = 0; i < ctx.terrainTiles.Length; i++)
            {
                if (ctx.clippedTiles[i].width == 0 || ctx.clippedTiles[i].height == 0)
                    continue;

                TerrainTile terrainTile = ctx.terrainTiles[i];

                Rect readRect = new Rect(
                    (ctx.clippedTiles[i].x + terrainTile.readOffset.x) / (float)inputTextureWidth,
                    (ctx.clippedTiles[i].y + terrainTile.readOffset.y) / (float)inputTextureHeight,
                    (ctx.clippedTiles[i].width) / (float)inputTextureWidth,
                    (ctx.clippedTiles[i].height) / (float)inputTextureHeight);

                Rect writeRect = new Rect(
                    ctx.clippedTiles[i].x + terrainTile.rect.x - ctx.brushRect.x,
                    ctx.clippedTiles[i].y + terrainTile.rect.y - ctx.brushRect.y,
                    ctx.clippedTiles[i].width,
                    ctx.clippedTiles[i].height);


                int tileLayerIndex = FindTerrainLayerIndex(terrainTile.terrain, inputLayer);
                if (tileLayerIndex == -1)
                    tileLayerIndex = AddTerrainLayer(terrainTile.terrain, inputLayer);

                terrainTile.mapIndex = tileLayerIndex >> 2;
                terrainTile.channelIndex = tileLayerIndex & 0x3;

                Texture sourceTexture = GetTerrainAlphaMapChecked(terrainTile.terrain, terrainTile.mapIndex);

                FilterMode oldFilterMode = sourceTexture.filterMode;
                sourceTexture.filterMode = FilterMode.Point;

                copyTerrainLayerMaterial.SetVector("_LayerMask", layerMasks[terrainTile.channelIndex]);
                copyTerrainLayerMaterial.SetTexture("_MainTex", sourceTexture);
                copyTerrainLayerMaterial.SetPass(0);

                DrawQuad(ctx.brushRect.width, ctx.brushRect.height, readRect, writeRect);

                sourceTexture.filterMode = oldFilterMode;
            }

            RenderTexture.active = ctx.oldRenderTexture;
            return ctx;
        }

        public static void EndPaintTexture(PaintContext ctx, string editorUndoName)
        {
            Vector4[] layerMasks = { new Vector4(1, 0, 0, 0), new Vector4(0, 1, 0, 0), new Vector4(0, 0, 1, 0), new Vector4(0, 0, 0, 1) };

            Material copyTerrainLayerMaterial = GetCopyTerrainLayerMaterial();

            for (int i = 0; i < ctx.terrainTiles.Length; i++)
            {
                if (ctx.clippedTiles[i].width == 0 || ctx.clippedTiles[i].height == 0)
                    continue;

                TerrainTile terrainTile = ctx.terrainTiles[i];

                if (onTerrainTileBeforePaint != null)
                    onTerrainTileBeforePaint(terrainTile, ToolAction.PaintTexture, editorUndoName);

                var rtdesc = new RenderTextureDescriptor(ctx.destinationRenderTexture.width, ctx.destinationRenderTexture.height, RenderTextureFormat.ARGB32);
                rtdesc.sRGB = false;
                rtdesc.useMipMap = false;
                rtdesc.autoGenerateMips = false;
                RenderTexture destTarget = RenderTexture.GetTemporary(rtdesc);
                RenderTexture.active = destTarget;

                var writeRect = new RectInt(
                    ctx.clippedTiles[i].x + terrainTile.rect.x - ctx.brushRect.x + terrainTile.writeOffset.x,
                    ctx.clippedTiles[i].y + terrainTile.rect.y - ctx.brushRect.y + terrainTile.writeOffset.y,
                    ctx.clippedTiles[i].width,
                    ctx.clippedTiles[i].height);

                var readRect = new Rect(
                    writeRect.x / (float)ctx.brushRect.width,
                    writeRect.y / (float)ctx.brushRect.height,
                    writeRect.width / (float)ctx.brushRect.width,
                    writeRect.height / (float)ctx.brushRect.height);

                ctx.destinationRenderTexture.filterMode = FilterMode.Point;

                for (int j = 0; j < terrainTile.terrain.terrainData.alphamapTextureCount; j++)
                {
                    Texture2D sourceTex = terrainTile.terrain.terrainData.alphamapTextures[j];

                    int mapIndex = terrainTile.mapIndex;
                    int channelIndex = terrainTile.channelIndex;

                    Rect combineRect = new Rect(
                        ctx.clippedTiles[i].x / (float)sourceTex.width,
                        ctx.clippedTiles[i].y / (float)sourceTex.height,
                        ctx.clippedTiles[i].width / (float)sourceTex.width,
                        ctx.clippedTiles[i].height / (float)sourceTex.height);

                    copyTerrainLayerMaterial.SetTexture("_MainTex", ctx.destinationRenderTexture);
                    copyTerrainLayerMaterial.SetTexture("_OldAlphaMapTexture", ctx.sourceRenderTexture);
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

                    if (paintTextureUsesCopyTexture)
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
                        Graphics.CopyTexture(destTarget, 0, 0, writeRect.x, writeRect.y, writeRect.width, writeRect.height, mips, 0, 0, ctx.clippedTiles[i].x, ctx.clippedTiles[i].y);
                        mips.GenerateMips();

                        // Copy them into sourceTex.
                        Graphics.CopyTexture(mips, sourceTex);

                        RenderTexture.ReleaseTemporary(mips);
                    }
                    else
                    {
                        GraphicsDeviceType deviceType = SystemInfo.graphicsDeviceType;
                        if (deviceType == GraphicsDeviceType.Metal || deviceType == GraphicsDeviceType.OpenGLCore)
                            sourceTex.ReadPixels(new Rect(writeRect.x, writeRect.y, writeRect.width, writeRect.height), ctx.clippedTiles[i].x, ctx.clippedTiles[i].y);
                        else
                            sourceTex.ReadPixels(new Rect(writeRect.x, destTarget.height - writeRect.y - writeRect.height, writeRect.width, writeRect.height), ctx.clippedTiles[i].x, ctx.clippedTiles[i].y);
                        sourceTex.Apply();
                    }
                }

                RenderTexture.active = null;
                RenderTexture.ReleaseTemporary(destTarget);

                OnTerrainPainted(terrainTile, ToolAction.PaintTexture);
            }

            ReleaseContextResources(ctx);
        }

        // materials
        public static Material GetBlitMaterial()
        {
            if (!m_BlitMaterial)
                m_BlitMaterial = new Material(Shader.Find("Hidden/BlitCopy"));

            return m_BlitMaterial;
        }

        public static Material GetCopyTerrainLayerMaterial()
        {
            if (!m_CopyTerrainLayerMaterial)
                m_CopyTerrainLayerMaterial = new Material(Shader.Find("Hidden/TerrainEngine/TerrainLayerUtils"));

            return m_CopyTerrainLayerMaterial;
        }

        static void DrawQuad(int width, int height, Rect source, Rect destination)
        {
            GL.PushMatrix();
            GL.LoadOrtho();
            GL.LoadPixelMatrix(0, width, 0, height);

            GL.Begin(GL.QUADS);
            GL.Color(new Color(1.0f, 1.0f, 1.0f, 1.0f));

            GL.TexCoord2(source.x, source.y);
            GL.Vertex3(destination.x, destination.y, 0.0f);
            GL.TexCoord2(source.x, source.yMax);
            GL.Vertex3(destination.x, destination.yMax, 0.0f);
            GL.TexCoord2(source.xMax, source.yMax);
            GL.Vertex3(destination.xMax, destination.yMax, 0.0f);
            GL.TexCoord2(source.xMax, source.y);
            GL.Vertex3(destination.xMax, destination.y, 0.0f);

            GL.End();
            GL.PopMatrix();
        }

        public static RectInt CalcBrushRectInPixels(Terrain terrain, Rect brushRect, int textureWidth, int textureHeight)
        {
            int xMin = Mathf.FloorToInt(((float)textureWidth) * brushRect.xMin / terrain.terrainData.size.x);
            int yMin = Mathf.FloorToInt(((float)textureHeight) * brushRect.yMin / terrain.terrainData.size.z);
            int xMax = Mathf.CeilToInt(((float)textureWidth) * brushRect.xMax / terrain.terrainData.size.x);
            int yMax = Mathf.CeilToInt(((float)textureHeight) * brushRect.yMax / terrain.terrainData.size.z);
            return new RectInt(xMin, yMin, xMax - xMin, yMax - yMin);
        }

        public static TerrainTile[] FindTerrainTiles(Terrain terrain, int width, int height, RectInt brushRect)
        {
            List<TerrainTile> terrainTiles = new List<TerrainTile>();

            Terrain left = terrain.leftNeighbor;
            Terrain right = terrain.rightNeighbor;
            Terrain top = terrain.topNeighbor;
            Terrain bottom = terrain.bottomNeighbor;

            bool wantLeft = (brushRect.x < 0);
            bool wantRight = (brushRect.xMax > (width - 1));
            bool wantTop = (brushRect.yMax > (height - 1));
            bool wantBottom = (brushRect.y < 0);

            if (wantLeft && wantRight)
            {
                Debug.Log("FindTerrainTiles query rectangle too large!");
                wantRight = false;
            }

            if (wantTop && wantBottom)
            {
                Debug.Log("FindTerrainTiles query rectangle too large!");
                wantBottom = false;
            }

            // add center tile
            TerrainTile tile = new TerrainTile(terrain, new RectInt(0, 0, width, height));
            tile.readOffset = Vector2Int.zero;
            tile.writeOffset = Vector2Int.zero;
            terrainTiles.Add(tile);

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
                tile = new TerrainTile(horiz, new RectInt(xBias * width, 0, width, height));
                tile.readOffset = new Vector2Int(xReadBias, 0);
                tile.writeOffset = new Vector2Int(xWriteBias, 0);
                terrainTiles.Add(tile);

                // add corner, if we have a link
                if (wantTop && horiz.topNeighbor)
                    cornerTerrain = horiz.topNeighbor;
                else if (wantBottom && horiz.bottomNeighbor)
                    cornerTerrain = horiz.bottomNeighbor;
            }

            if (vert)
            {
                tile = new TerrainTile(vert, new RectInt(0, yBias * height, width, height));
                tile.readOffset = new Vector2Int(0, yReadBias);
                tile.writeOffset = new Vector2Int(0, yWriteBias);
                terrainTiles.Add(tile);

                // add corner, if we have a link
                if (wantLeft && vert.leftNeighbor)
                    cornerTerrain = vert.leftNeighbor;
                else if (wantRight && vert.rightNeighbor)
                    cornerTerrain = vert.rightNeighbor;
            }

            if (cornerTerrain != null)
            {
                tile = new TerrainTile(cornerTerrain, new RectInt(xBias * width, yBias * height, width, height));
                tile.readOffset = new Vector2Int(xReadBias, yReadBias);
                tile.writeOffset = new Vector2Int(xWriteBias, yWriteBias);
                terrainTiles.Add(tile);
            }

            return terrainTiles.ToArray();
        }

        public static RectInt[] ClipTerrainTiles(TerrainTile[] terrainTiles, RectInt brushRect)
        {
            RectInt[] clippedTiles = new RectInt[terrainTiles.Length];
            for (int i = 0; i < terrainTiles.Length; i++)
            {
                clippedTiles[i].x = Mathf.Max(0, brushRect.x - terrainTiles[i].rect.x);
                clippedTiles[i].y = Mathf.Max(0, brushRect.y - terrainTiles[i].rect.y);
                clippedTiles[i].xMax = Mathf.Min(terrainTiles[i].rect.width, brushRect.xMax - terrainTiles[i].rect.x);
                clippedTiles[i].yMax = Mathf.Min(terrainTiles[i].rect.height, brushRect.yMax - terrainTiles[i].rect.y);
            }
            return clippedTiles;
        }

        // Alphamap utilities
        public static Texture2D GetTerrainAlphaMapChecked(Terrain terrain, int mapIndex)
        {
            if (mapIndex >= terrain.terrainData.alphamapTextureCount)
                throw new System.ArgumentException("Trying to access out-of-bounds terrain alphamap information.");

            return terrain.terrainData.alphamapTextures[mapIndex];
        }

        static public int FindTerrainLayerIndex(Terrain terrain, TerrainLayer inputLayer)
        {
            for (int i = 0; i < terrain.terrainData.terrainLayers.Length; i++)
            {
                if (terrain.terrainData.terrainLayers[i] == inputLayer)
                    return i;
            }
            return -1;
        }

        static int AddTerrainLayer(Terrain terrain, TerrainLayer inputLayer)
        {
            int newIndex = terrain.terrainData.terrainLayers.Length;
            var newarray = new TerrainLayer[newIndex + 1];
            System.Array.Copy(terrain.terrainData.terrainLayers, 0, newarray, 0, newIndex);
            newarray[newIndex] = inputLayer;
            terrain.terrainData.terrainLayers = newarray;
            return newIndex;
        }

        //--

        static Material m_BlitMaterial = null;
        static Material m_CopyTerrainLayerMaterial = null;
    }
}
