// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEngine
{
    public static class TerrainPaintUtility
    {
        public class TerrainTile
        {
            public TerrainTile() { terrain = null; }
            public TerrainTile(Terrain newTerrain, RectInt newRegion) { rect = newRegion; terrain = newTerrain; }
            public Terrain terrain;
            public RectInt rect;

            public int mapIndex;
            public int channelIndex;

            public RectInt readOffset;
            public RectInt writeOffset;
            public RectInt commitOffset;
        }

        public class PaintContext
        {
            public RectInt brushRect;
            public TerrainTile[] terrainTiles;
            public RectInt[] clippedTiles;

            public RenderTexture sourceRenderTexture;
            public RenderTexture destinationRenderTexture;

            internal RenderTexture oldRenderTarget;
        }


        static PaintContext InitializePaintContext(Terrain terrain, Vector2 uv, Vector2Int brushSize, int inputTextureWidth, int inputTextureHeight, RenderTextureFormat colorFormat)
        {
            PaintContext ctx = new PaintContext();
            ctx.brushRect = CalcBrushRect(uv, brushSize, inputTextureWidth, inputTextureHeight);
            ctx.terrainTiles = FindTerrainTiles(terrain, inputTextureWidth, inputTextureHeight, ctx.brushRect);
            ctx.clippedTiles = ClipTerrainTiles(ctx.terrainTiles, ctx.brushRect);
            ctx.sourceRenderTexture = RenderTexture.GetTemporary(ctx.brushRect.width, ctx.brushRect.height, 0, colorFormat, RenderTextureReadWrite.Linear);
            ctx.destinationRenderTexture = RenderTexture.GetTemporary(ctx.brushRect.width, ctx.brushRect.height, 0, colorFormat, RenderTextureReadWrite.Linear);
            ctx.sourceRenderTexture.wrapMode = TextureWrapMode.Clamp;
            ctx.sourceRenderTexture.filterMode = FilterMode.Point;
            ctx.oldRenderTarget = RenderTexture.active;
            return ctx;
        }

        public static void ReleaseContextResources(PaintContext ctx)
        {
            RenderTexture.active = ctx.oldRenderTarget;
            RenderTexture.ReleaseTemporary(ctx.sourceRenderTexture);
            RenderTexture.ReleaseTemporary(ctx.destinationRenderTexture);
            ctx.sourceRenderTexture = null;
            ctx.destinationRenderTexture = null;
        }

        public static Vector2Int CalculateBrushSizeInHeightmapSpace(Terrain terrain, int brushSizeInTerrainUnits)
        {
            Vector2Int brushSize = new Vector2Int(Mathf.FloorToInt((float)(brushSizeInTerrainUnits * terrain.terrainData.heightmapWidth) / terrain.terrainData.size.x),
                    Mathf.FloorToInt((float)(brushSizeInTerrainUnits * terrain.terrainData.heightmapHeight) / terrain.terrainData.size.z));
            return Vector2Int.Max(Vector2Int.one, brushSize);
        }

        public static Vector2Int CalculateBrushSizeInAlphamapSpace(Terrain terrain, int brushSizeInTerrainUnits)
        {
            Vector2Int brushSize = new Vector2Int(Mathf.FloorToInt((float)(brushSizeInTerrainUnits * terrain.terrainData.alphamapWidth) / terrain.terrainData.size.x),
                    Mathf.FloorToInt((float)(brushSizeInTerrainUnits * terrain.terrainData.alphamapHeight) / terrain.terrainData.size.z));
            return Vector2Int.Max(Vector2Int.one, brushSize);
        }

        public static PaintContext BeginPaintHeightmap(Terrain terrain, Vector2 uv, Vector2Int brushSize)
        {
            RenderTexture rt = terrain.terrainData.heightmapTexture;

            RenderTextureFormat colorFormat = rt.format;
            int heightmapWidth = rt.width;
            int heightmapHeight = rt.height;

            PaintContext ctx = InitializePaintContext(terrain, uv, brushSize, heightmapWidth, heightmapHeight, rt.format);

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
                        (ctx.clippedTiles[i].width + terrainTile.readOffset.width) / (float)heightmapWidth,
                        (ctx.clippedTiles[i].height + terrainTile.readOffset.height) / (float)heightmapHeight);

                Rect writeRect = new Rect(
                        ctx.clippedTiles[i].x + terrainTile.rect.x - ctx.brushRect.x + terrainTile.writeOffset.x,
                        ctx.clippedTiles[i].y + terrainTile.rect.y - ctx.brushRect.y + terrainTile.writeOffset.y,
                        ctx.clippedTiles[i].width + terrainTile.writeOffset.width,
                        ctx.clippedTiles[i].height + terrainTile.writeOffset.height);

                Texture sourceTexture = terrainTile.terrain.terrainData.heightmapTexture;
                FilterMode oldFilterMode = sourceTexture.filterMode;

                sourceTexture.filterMode = FilterMode.Point;

                blitMaterial.SetTexture("_MainTex", sourceTexture);
                blitMaterial.SetPass(0);

                DrawQuad(ctx.brushRect.width, ctx.brushRect.height, readRect, writeRect);

                sourceTexture.filterMode = oldFilterMode;
            }

            RenderTexture.active = ctx.oldRenderTarget;
            return ctx;
        }

        public static void EndPaintHeightmap(PaintContext ctx)
        {
            Material blitMaterial = GetBlitMaterial();

            for (int i = 0; i < ctx.terrainTiles.Length; i++)
            {
                if (ctx.clippedTiles[i].width == 0 || ctx.clippedTiles[i].height == 0)
                    continue;

                TerrainTile terrainTile = ctx.terrainTiles[i];

                RenderTexture heightmap = terrainTile.terrain.terrainData.heightmapTexture;
                RenderTexture.active = heightmap;

                Rect readRect = new Rect(
                        (ctx.clippedTiles[i].x + terrainTile.rect.x - ctx.brushRect.x + terrainTile.commitOffset.x) / (float)ctx.brushRect.width,
                        (ctx.clippedTiles[i].y + terrainTile.rect.y - ctx.brushRect.y + terrainTile.commitOffset.y) / (float)ctx.brushRect.height,
                        (ctx.clippedTiles[i].width + terrainTile.commitOffset.width) / (float)ctx.brushRect.width,
                        (ctx.clippedTiles[i].height + terrainTile.commitOffset.height) / (float)ctx.brushRect.height);

                Rect writeRect = new Rect(
                        ctx.clippedTiles[i].x,
                        ctx.clippedTiles[i].y,
                        ctx.clippedTiles[i].width,
                        ctx.clippedTiles[i].height);

                ctx.destinationRenderTexture.filterMode = FilterMode.Point;

                blitMaterial.SetTexture("_MainTex", ctx.destinationRenderTexture);
                blitMaterial.SetPass(0);

                DrawQuad(heightmap.width, heightmap.height, readRect, writeRect);

                terrainTile.terrain.terrainData.UpdateDirtyRegion(ctx.clippedTiles[i].x, ctx.clippedTiles[i].y, ctx.clippedTiles[i].width, ctx.clippedTiles[i].height);
            }

            ReleaseContextResources(ctx);
        }

        public static PaintContext BeginPaintTexture(Terrain terrain, Vector2 uv, Vector2Int brushSize, TerrainLayer inputLayer)
        {
            if (inputLayer == null)
                return null;

            int terrainLayerIndex = FindTerrainLayerIndex(terrain, inputLayer);
            if (terrainLayerIndex == -1)
                terrainLayerIndex = AddTerrainLayer(terrain, inputLayer);

            Texture2D inputTexture = GetTerrainAlphaMapChecked(terrain, terrainLayerIndex >> 2);

            int inputTextureWidth = inputTexture.width;
            int inputTextureHeight = inputTexture.height;

            PaintContext ctx = InitializePaintContext(terrain, uv, brushSize, inputTextureWidth, inputTextureHeight, RenderTextureFormat.R8);

            RenderTexture oldRT = RenderTexture.active;
            RenderTexture.active = ctx.sourceRenderTexture;

            Vector4[] layerMasks = { new Vector4(1, 0, 0, 0), new Vector4(0, 1, 0, 0), new Vector4(0, 0, 1, 0), new Vector4(0, 0, 0, 1) };

            Material copyTerrainLayerMaterial = GetCopyTerrainLayerMaterial();
            for (int i = 0; i < ctx.terrainTiles.Length; i++)
            {
                if (ctx.clippedTiles[i].width == 0 || ctx.clippedTiles[i].height == 0)
                    continue;

                TerrainTile terrainTile = ctx.terrainTiles[i];

                terrainTile.terrain.terrainData.SetBasemapDirty(false);

                Rect readRect = new Rect(
                        (ctx.clippedTiles[i].x + terrainTile.readOffset.x) / (float)inputTextureWidth,
                        (ctx.clippedTiles[i].y + terrainTile.readOffset.y) / (float)inputTextureHeight,
                        (ctx.clippedTiles[i].width + terrainTile.readOffset.width) / (float)inputTextureWidth,
                        (ctx.clippedTiles[i].height + terrainTile.readOffset.height) / (float)inputTextureHeight);

                Rect writeRect = new Rect(
                        ctx.clippedTiles[i].x + terrainTile.rect.x - ctx.brushRect.x + terrainTile.writeOffset.x,
                        ctx.clippedTiles[i].y + terrainTile.rect.y - ctx.brushRect.y + terrainTile.writeOffset.y,
                        ctx.clippedTiles[i].width + terrainTile.writeOffset.width,
                        ctx.clippedTiles[i].height + terrainTile.writeOffset.height);


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

            RenderTexture.active = oldRT;
            return ctx;
        }

        public static void EndPaintTexture(PaintContext ctx)
        {
            Vector4[] layerMasks = { new Vector4(1, 0, 0, 0), new Vector4(0, 1, 0, 0), new Vector4(0, 0, 1, 0), new Vector4(0, 0, 0, 1) };

            Material copyTerrainLayerMaterial = GetCopyTerrainLayerMaterial();

            for (int i = 0; i < ctx.terrainTiles.Length; i++)
            {
                if (ctx.clippedTiles[i].width == 0 || ctx.clippedTiles[i].height == 0)
                    continue;

                TerrainTile terrainTile = ctx.terrainTiles[i];

                RenderTexture destTarget = RenderTexture.GetTemporary(ctx.destinationRenderTexture.width, ctx.destinationRenderTexture.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
                RenderTexture.active = destTarget;

                Rect writeRect = new Rect(
                        ctx.clippedTiles[i].x + terrainTile.rect.x - ctx.brushRect.x + terrainTile.commitOffset.x,
                        ctx.clippedTiles[i].y + terrainTile.rect.y - ctx.brushRect.y + terrainTile.commitOffset.y,
                        ctx.clippedTiles[i].width + terrainTile.commitOffset.width,
                        ctx.clippedTiles[i].height + terrainTile.commitOffset.height);

                Rect readRect = new Rect(writeRect.x / (float)ctx.brushRect.width,
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

                    RenderTexture.active = destTarget;

                    GraphicsDeviceType deviceType = SystemInfo.graphicsDeviceType;
                    if (deviceType == GraphicsDeviceType.Metal || deviceType == GraphicsDeviceType.OpenGLCore)
                        sourceTex.ReadPixels(new Rect(writeRect.x, writeRect.y, writeRect.width, writeRect.height), ctx.clippedTiles[i].x, ctx.clippedTiles[i].y);
                    else
                        sourceTex.ReadPixels(new Rect(writeRect.x, destTarget.height - writeRect.y - writeRect.height, writeRect.width, writeRect.height), ctx.clippedTiles[i].x, ctx.clippedTiles[i].y);

                    sourceTex.Apply();
                }

                RenderTexture.active = null;
                RenderTexture.ReleaseTemporary(destTarget);
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

        //

        public static Vector2 CalcTopLeftOfBrushRect(Vector2 uv, Vector2Int brushSize, int heightmapWidth, int heightmapHeight)
        {
            return uv * new Vector2((float)heightmapWidth, (float)heightmapHeight) - 0.5f * Vector2.one * (Vector2)brushSize;
        }

        public static RectInt CalcBrushRect(Vector2 uv, Vector2Int brushSize, int heightmapWidth, int heightmapHeight)
        {
            Vector2 topLeft = CalcTopLeftOfBrushRect(uv, brushSize, heightmapWidth, heightmapHeight);
            return new RectInt(Mathf.FloorToInt(topLeft.x), Mathf.FloorToInt(topLeft.y), brushSize.x, brushSize.y);
        }

        public static TerrainTile[] FindTerrainTiles(Terrain terrain, int width, int height, RectInt brushRect)
        {
            TerrainTile terrainTile;
            ArrayList arr = new ArrayList();


            Terrain left = terrain.leftNeighbor;
            Terrain right = terrain.rightNeighbor;
            Terrain top = terrain.topNeighbor;
            Terrain bottom = terrain.bottomNeighbor;

            bool hasLeft = left && (brushRect.x < 0);
            bool hasRight = right && (brushRect.xMax > (width - 1));
            bool hasTop = top && (brushRect.yMax > (height - 1));
            bool hasBottom = bottom && (brushRect.y < 0);

            terrainTile = new TerrainTile(terrain, new RectInt(0, 0, width, height));
            terrainTile.readOffset = new RectInt(0, 0, 0, 0);
            terrainTile.writeOffset = new RectInt(0, 0, 0, 0);
            terrainTile.commitOffset = new RectInt(0, 0, 0, 0);
            arr.Add(terrainTile);

            if (hasLeft)
            {
                terrainTile = new TerrainTile(left, new RectInt(-width, 0, width, height));
                terrainTile.readOffset = new RectInt(0, 0, -1, 0);
                terrainTile.writeOffset = new RectInt(1, 0, -1, 0);
                terrainTile.commitOffset = new RectInt(1, 0, 0, 0);
                arr.Add(terrainTile);
            }
            else if (hasRight)
            {
                terrainTile = new TerrainTile(right, new RectInt(width, 0, width, height));
                terrainTile.readOffset = new RectInt(1, 0, -1, 0);
                terrainTile.writeOffset = new RectInt(0, 0, -1, 0);
                terrainTile.commitOffset = new RectInt(-1, 0, 0, 0);
                arr.Add(terrainTile);
            }

            if (hasTop)
            {
                terrainTile = new TerrainTile(top, new RectInt(0, height, width, height));
                terrainTile.readOffset = new RectInt(0, 1, 0, -1);
                terrainTile.writeOffset = new RectInt(0, 0, 0, -1);
                terrainTile.commitOffset = new RectInt(0, -1, 0, 0);
                arr.Add(terrainTile);
            }
            else if (hasBottom)
            {
                terrainTile = new TerrainTile(bottom, new RectInt(0, -height, width, height));
                terrainTile.readOffset = new RectInt(0, 0, 0, -1);
                terrainTile.writeOffset = new RectInt(0, 1, 0, -1);
                terrainTile.commitOffset = new RectInt(0, 1, 0, 0);
                arr.Add(terrainTile);
            }

            Terrain cornerTerrain = null;

            if (hasTop && hasLeft && top.leftNeighbor)
                cornerTerrain = top.leftNeighbor;
            else if (hasTop && hasRight && top.rightNeighbor)
                cornerTerrain = top.rightNeighbor;
            else if (hasBottom && hasLeft && bottom.leftNeighbor)
                cornerTerrain = bottom.leftNeighbor;
            else if (hasBottom && hasRight && bottom.rightNeighbor)
                cornerTerrain = bottom.rightNeighbor;

            if (cornerTerrain)
            {
                TerrainTile horiz = arr[1] as TerrainTile;
                TerrainTile vert = arr[2] as TerrainTile;

                terrainTile = new TerrainTile(cornerTerrain, new RectInt(horiz.rect.x, vert.rect.y, horiz.rect.width, vert.rect.height));
                terrainTile.readOffset = new RectInt(horiz.readOffset.x, vert.readOffset.y, horiz.readOffset.width, vert.readOffset.height);
                terrainTile.writeOffset = new RectInt(horiz.writeOffset.x, vert.writeOffset.y, horiz.writeOffset.width, vert.writeOffset.height);
                terrainTile.commitOffset = new RectInt(horiz.commitOffset.x, vert.commitOffset.y, horiz.commitOffset.width, vert.commitOffset.height);
                arr.Add(terrainTile);
            }

            return arr.ToArray(typeof(TerrainTile)) as TerrainTile[];
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
