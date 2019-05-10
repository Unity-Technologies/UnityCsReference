// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Experimental.TerrainAPI
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

        // returns a transform from terrain space to brush UV
        public static BrushTransform CalculateBrushTransform(
            Terrain terrain, Vector2 brushCenterTerrainUV, float brushSize, float brushRotationDegrees)
        {
            float rotationRadians = brushRotationDegrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rotationRadians);
            float sin = Mathf.Sin(rotationRadians);
            Vector2 brushU = new Vector2(cos, -sin) * brushSize;
            Vector2 brushV = new Vector2(sin, cos) * brushSize;

            // calculate brush origin
            Vector3 terrainSize = terrain.terrainData.size;
            Vector2 brushCenterTerrainSpace = brushCenterTerrainUV * new Vector2(terrainSize.x, terrainSize.z);
            Vector2 brushOrigin = brushCenterTerrainSpace - 0.5f * brushU - 0.5f * brushV;

            BrushTransform xform = new BrushTransform(brushOrigin, brushU, brushV);
            return xform;
        }

        public static void BuildTransformPaintContextUVToPaintContextUV(PaintContext src, PaintContext dst, out Vector4 scaleOffset)
        {
            // for example:
            //   src = alphaUV
            //   dst = normalUV
            // dst.uv = src.u * scales.xy + src.v * scales.zw + offset
            // terrainspace.xz = srcOrigin + src.uv * srcSize
            // terrainspace.xz = dstOrigin + dst.uv * dstSize
            // dstOrigin + dst.uv * dstSize = srcOrigin + src.uv * srcSize
            // dst.uv * dstSize = src.uv * srcSize + srcOrigin - dstOrigin
            // dst.uv = (src.uv * srcSize + srcOrigin - dstOrigin) / dstSize
            // dst.uv = (src.uv * srcSize) / dstSize + (srcOrigin - dstOrigin) / dstSize
            // scales.x = srcSize.x / dstSize.x
            // scales.yz = 0.0f;
            // scales.w = srcSize.y / dstSize.y
            // offset.xy = (srcOrigin.xy - dstOrigin.xy) / dstSize.xy

            // paint context origin in terrain space
            // (note this is the UV space origin and size, not the mesh origin & size)
            float srcOriginX = (src.pixelRect.xMin - 0.5f) * src.pixelSize.x;
            float srcOriginZ = (src.pixelRect.yMin - 0.5f) * src.pixelSize.y;
            float srcSizeX = (src.pixelRect.width) * src.pixelSize.x;
            float srcSizeZ = (src.pixelRect.height) * src.pixelSize.y;

            // paint context origin in terrain space
            // (note this is the UV space origin and size, not the mesh origin & size)
            float dstOriginX = (dst.pixelRect.xMin - 0.5f) * dst.pixelSize.x;
            float dstOriginZ = (dst.pixelRect.yMin - 0.5f) * dst.pixelSize.y;
            float dstSizeX = (dst.pixelRect.width) * dst.pixelSize.x;
            float dstSizeZ = (dst.pixelRect.height) * dst.pixelSize.y;

            scaleOffset = new Vector4(
                srcSizeX / dstSizeX,
                srcSizeZ / dstSizeZ,
                (srcOriginX - dstOriginX) / dstSizeX,
                (srcOriginZ - dstOriginZ) / dstSizeZ
            );
        }

        // this function sets up material properties used by functions provided in TerrainTool.cginc
        public static void SetupTerrainToolMaterialProperties(
            PaintContext paintContext,
            BrushTransform brushXform,     // the brush transform to terrain space (of paintContext.originTerrain)
            Material material)
        {
            // BrushUV = f(terrainSpace.xz) = f(g(pc.uv))
            //   f(ts.xy) = ts.x * brushXform.X + ts.y * brushXform.Y + brushXform.Origin
            //   g(pc.uv) = ts.xz = pcOrigin + pc.uv * pcSize
            //   f(g(pc.uv)) == (pcOrigin + pc.uv * pcSize).x * brushXform.X + (pcOrigin + pc.uv * pcSize).y * brushXform.Y + brushXform.Origin
            //   f(g(pc.uv)) == (pcOrigin.x + pc.u * pcSize.x) * brushXform.X + (pcOrigin.y + pc.v * pcSize.y) * brushXform.Y + brushXform.Origin
            //   f(g(pc.uv)) == (pcOrigin.x * brushXform.X) + (pc.u * pcSize.x) * brushXform.X + (pcOrigin.y * brushXform.Y) + (pc.v * pcSize.y) * brushXform.Y + brushXform.Origin
            //   f(g(pc.uv)) == pc.u * (pcSize.x * brushXform.X) + pc.v * (pcSize.y * brushXform.Y) + (brushXform.Origin + (pcOrigin.x * brushXform.X) + (pcOrigin.y * brushXform.Y))

            // pcOrigin =   (pc.pixelRect.xyMin - 0.5) * pc.pixelSize.xy
            // pcSize =     (pc.pixelRect.wh) * pc.pixelSize.xy

            // paint context origin in terrain space
            // (note this is the UV space origin and size, not the mesh origin & size)
            float pcOriginX = (paintContext.pixelRect.xMin - 0.5f) * paintContext.pixelSize.x;
            float pcOriginZ = (paintContext.pixelRect.yMin - 0.5f) * paintContext.pixelSize.y;
            float pcSizeX = (paintContext.pixelRect.width) * paintContext.pixelSize.x;
            float pcSizeZ = (paintContext.pixelRect.height) * paintContext.pixelSize.y;

            Vector2 scaleU = pcSizeX * brushXform.targetX;
            Vector2 scaleV = pcSizeZ * brushXform.targetY;
            Vector2 offset = brushXform.targetOrigin + pcOriginX * brushXform.targetX + pcOriginZ * brushXform.targetY;
            material.SetVector("_PCUVToBrushUVScales", new Vector4(scaleU.x, scaleU.y, scaleV.x, scaleV.y));
            material.SetVector("_PCUVToBrushUVOffset", new Vector4(offset.x, offset.y, 0.0f, 0.0f));
        }

        internal static bool paintTextureUsesCopyTexture
        {
            get
            {
                const CopyTextureSupport RT2TexAndTex2RT = CopyTextureSupport.RTToTexture | CopyTextureSupport.TextureToRT;
                return (SystemInfo.copyTextureSupport & RT2TexAndTex2RT) == RT2TexAndTex2RT;
            }
        }

        internal static PaintContext InitializePaintContext(Terrain terrain, int targetWidth, int targetHeight, RenderTextureFormat pcFormat, Rect boundsInTerrainSpace, int extraBorderPixels = 0)
        {
            PaintContext ctx = PaintContext.CreateFromBounds(terrain, boundsInTerrainSpace, targetWidth, targetHeight, extraBorderPixels);
            ctx.CreateRenderTargets(pcFormat);
            return ctx;
        }

        public static void ReleaseContextResources(PaintContext ctx)
        {
            ctx.Cleanup();
        }

        public static PaintContext BeginPaintHeightmap(Terrain terrain, Rect boundsInTerrainSpace, int extraBorderPixels = 0)
        {
            int heightmapResolution = terrain.terrainData.heightmapResolution;
            PaintContext ctx = InitializePaintContext(terrain, heightmapResolution, heightmapResolution, Terrain.heightmapRenderTextureFormat, boundsInTerrainSpace, extraBorderPixels);
            ctx.GatherHeightmap();
            return ctx;
        }

        public static void EndPaintHeightmap(PaintContext ctx, string editorUndoName)
        {
            ctx.ScatterHeightmap(editorUndoName);
            ctx.Cleanup();
        }

        public static PaintContext CollectNormals(Terrain terrain, Rect boundsInTerrainSpace, int extraBorderPixels = 0)
        {
            int heightmapResolution = terrain.terrainData.heightmapResolution;
            PaintContext ctx = InitializePaintContext(terrain, heightmapResolution, heightmapResolution, Terrain.normalmapRenderTextureFormat, boundsInTerrainSpace, extraBorderPixels);
            ctx.GatherNormals();
            return ctx;
        }

        public static PaintContext BeginPaintTexture(Terrain terrain, Rect boundsInTerrainSpace, TerrainLayer inputLayer, int extraBorderPixels = 0)
        {
            if (inputLayer == null)
                return null;

            int resolution = terrain.terrainData.alphamapResolution;
            PaintContext ctx = InitializePaintContext(terrain, resolution, resolution, RenderTextureFormat.R8, boundsInTerrainSpace, extraBorderPixels);
            ctx.GatherAlphamap(inputLayer, true);
            return ctx;
        }

        public static void EndPaintTexture(PaintContext ctx, string editorUndoName)
        {
            ctx.ScatterAlphamap(editorUndoName);
            ctx.Cleanup();
        }

        // materials
        public static Material GetBlitMaterial()
        {
            if (!s_BlitMaterial)
                s_BlitMaterial = new Material(Shader.Find("Hidden/BlitCopy"));

            return s_BlitMaterial;
        }

        public static Material GetCopyTerrainLayerMaterial()
        {
            if (!s_CopyTerrainLayerMaterial)
                s_CopyTerrainLayerMaterial = new Material(Shader.Find("Hidden/TerrainEngine/TerrainLayerUtils"));

            return s_CopyTerrainLayerMaterial;
        }

        internal static void DrawQuad(RectInt destinationPixels, RectInt sourcePixels, Texture sourceTexture)
        {
            DrawQuad2(destinationPixels, sourcePixels, sourceTexture, sourcePixels, sourceTexture);
        }

        internal static void DrawQuad2(RectInt destinationPixels, RectInt sourcePixels, Texture sourceTexture, RectInt sourcePixels2, Texture sourceTexture2)
        {
            if ((destinationPixels.width > 0) && (destinationPixels.height > 0))
            {
                Rect sourceUVs = new Rect(
                    (sourcePixels.x) / (float)sourceTexture.width,
                    (sourcePixels.y) / (float)sourceTexture.height,
                    (sourcePixels.width) / (float)sourceTexture.width,
                    (sourcePixels.height) / (float)sourceTexture.height);

                Rect sourceUVs2 = new Rect(
                    (sourcePixels2.x) / (float)sourceTexture2.width,
                    (sourcePixels2.y) / (float)sourceTexture2.height,
                    (sourcePixels2.width) / (float)sourceTexture2.width,
                    (sourcePixels2.height) / (float)sourceTexture2.height);

                GL.Begin(GL.QUADS);
                GL.Color(new Color(1.0f, 1.0f, 1.0f, 1.0f));
                GL.MultiTexCoord2(0, sourceUVs.x, sourceUVs.y);
                GL.MultiTexCoord2(1, sourceUVs2.x, sourceUVs2.y);
                GL.Vertex3(destinationPixels.x, destinationPixels.y, 0.0f);
                GL.MultiTexCoord2(0, sourceUVs.x, sourceUVs.yMax);
                GL.MultiTexCoord2(1, sourceUVs2.x, sourceUVs2.yMax);
                GL.Vertex3(destinationPixels.x, destinationPixels.yMax, 0.0f);
                GL.MultiTexCoord2(0, sourceUVs.xMax, sourceUVs.yMax);
                GL.MultiTexCoord2(1, sourceUVs2.xMax, sourceUVs2.yMax);
                GL.Vertex3(destinationPixels.xMax, destinationPixels.yMax, 0.0f);
                GL.MultiTexCoord2(0, sourceUVs.xMax, sourceUVs.y);
                GL.MultiTexCoord2(1, sourceUVs2.xMax, sourceUVs2.y);
                GL.Vertex3(destinationPixels.xMax, destinationPixels.y, 0.0f);
                GL.End();
            }
        }

        internal static RectInt CalcPixelRectFromBounds(Terrain terrain, Rect boundsInTerrainSpace, int textureWidth, int textureHeight, int extraBorderPixels)
        {
            float scaleX = (textureWidth - 1.0f) / terrain.terrainData.size.x;
            float scaleY = (textureHeight - 1.0f) / terrain.terrainData.size.z;
            int xMin = Mathf.FloorToInt(boundsInTerrainSpace.xMin * scaleX) - extraBorderPixels;
            int yMin = Mathf.FloorToInt(boundsInTerrainSpace.yMin * scaleY) - extraBorderPixels;
            int xMax = Mathf.CeilToInt(boundsInTerrainSpace.xMax * scaleX) + extraBorderPixels;
            int yMax = Mathf.CeilToInt(boundsInTerrainSpace.yMax * scaleY) + extraBorderPixels;
            return new RectInt(xMin, yMin, xMax - xMin + 1, yMax - yMin + 1);
        }

        // Alphamap utilities
        public static Texture2D GetTerrainAlphaMapChecked(Terrain terrain, int mapIndex)
        {
            if (mapIndex >= terrain.terrainData.alphamapTextureCount)
                throw new System.ArgumentException("Trying to access out-of-bounds terrain alphamap information.");

            return terrain.terrainData.GetAlphamapTexture(mapIndex);
        }

        static public int FindTerrainLayerIndex(Terrain terrain, TerrainLayer inputLayer)
        {
            var terrainLayers = terrain.terrainData.terrainLayers;
            for (int i = 0; i < terrainLayers.Length; i++)
            {
                if (terrainLayers[i] == inputLayer)
                    return i;
            }
            return -1;
        }

        internal static int AddTerrainLayer(Terrain terrain, TerrainLayer inputLayer)
        {
            var oldArray = terrain.terrainData.terrainLayers;
            int newIndex = oldArray.Length;
            var newArray = new TerrainLayer[newIndex + 1];
            Array.Copy(oldArray, 0, newArray, 0, newIndex);
            newArray[newIndex] = inputLayer;
            terrain.terrainData.terrainLayers = newArray;
            return newIndex;
        }

        //--

        static Material s_BlitMaterial = null;
        static Material s_CopyTerrainLayerMaterial = null;
    }
}
