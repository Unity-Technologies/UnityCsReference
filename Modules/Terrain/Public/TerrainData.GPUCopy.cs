// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using CopyTextureSupport = UnityEngine.Rendering.CopyTextureSupport;
using GraphicsDeviceType = UnityEngine.Rendering.GraphicsDeviceType;

namespace UnityEngine
{
    partial class TerrainData
    {
        private static bool SupportsCopyTextureBetweenRTAndTexture
        {
            get
            {
                const CopyTextureSupport kRT2TexAndTex2RT = CopyTextureSupport.RTToTexture | CopyTextureSupport.TextureToRT;
                return (SystemInfo.copyTextureSupport & kRT2TexAndTex2RT) == kRT2TexAndTex2RT;
            }
        }

        public void CopyActiveRenderTextureToHeightmap(RectInt sourceRect, Vector2Int dest, TerrainHeightmapSyncControl syncControl)
        {
            var source = RenderTexture.active;
            if (source == null)
                throw new InvalidOperationException("Active RenderTexture is null.");

            if (sourceRect.x < 0 || sourceRect.y < 0 || sourceRect.xMax > source.width || sourceRect.yMax > source.height)
                throw new ArgumentOutOfRangeException("sourceRect");
            else if (dest.x < 0 || dest.x + sourceRect.width > heightmapResolution)
                throw new ArgumentOutOfRangeException("dest.x");
            else if (dest.y < 0 || dest.y + sourceRect.height > heightmapResolution)
                throw new ArgumentOutOfRangeException("dest.y");

            Internal_CopyActiveRenderTextureToHeightmap(sourceRect, dest.x, dest.y, syncControl);
            Experimental.TerrainAPI.TerrainCallbacks.InvokeHeightmapChangedCallback(this, new RectInt(dest.x, dest.y, sourceRect.width, sourceRect.height), syncControl == TerrainHeightmapSyncControl.HeightAndLod);
        }

        public void DirtyHeightmapRegion(RectInt region, TerrainHeightmapSyncControl syncControl)
        {
            int resolution = heightmapResolution;
            if (region.x < 0 || region.x >= resolution)
                throw new ArgumentOutOfRangeException("region.x");
            else if (region.width <= 0 || region.xMax > resolution)
                throw new ArgumentOutOfRangeException("region.width");
            if (region.y < 0 || region.y >= resolution)
                throw new ArgumentOutOfRangeException("region.y");
            else if (region.height <= 0 || region.yMax > resolution)
                throw new ArgumentOutOfRangeException("region.height");

            Internal_DirtyHeightmapRegion(region.x, region.y, region.width, region.height, syncControl);
            Experimental.TerrainAPI.TerrainCallbacks.InvokeHeightmapChangedCallback(this, region, syncControl == TerrainHeightmapSyncControl.HeightAndLod);
        }

        public static string AlphamapTextureName => "alphamap";
        public static string SurfaceMaskTextureName => "surfacemask";

        public void CopyActiveRenderTextureToTexture(string textureName, int textureIndex, RectInt sourceRect, Vector2Int dest, bool allowDelayedCPUSync)
        {
            if (String.IsNullOrEmpty(textureName))
                throw new ArgumentNullException("textureName");

            var source = RenderTexture.active;
            if (source == null)
                throw new InvalidOperationException("Active RenderTexture is null.");

            int textureWidth = 0;
            int textureHeight = 0;

            if (textureName == SurfaceMaskTextureName)
            {
                if (textureIndex != 0)
                    throw new ArgumentOutOfRangeException("textureIndex");
                else if (source == surfaceMaskTexture)
                    throw new ArgumentException("source", "Active RenderTexture cannot be surfaceMaskTexture.");
                textureWidth = textureHeight = surfaceMaskResolution;
            }
            else if (textureName == AlphamapTextureName)
            {
                if (textureIndex < 0 || textureIndex >= alphamapTextureCount)
                    throw new ArgumentOutOfRangeException("textureIndex");
                textureWidth = textureHeight = alphamapResolution;
            }
            else
            {
                // TODO: Support generic terrain textures.
                throw new ArgumentException($"Unrecognized terrain texture name: \"{textureName}\"");
            }

            if (sourceRect.x < 0 || sourceRect.y < 0 || sourceRect.xMax > source.width || sourceRect.yMax > source.height)
                throw new ArgumentOutOfRangeException("sourceRect");
            else if (dest.x < 0 || dest.x + sourceRect.width > textureWidth)
                throw new ArgumentOutOfRangeException("dest.x");
            else if (dest.y < 0 || dest.y + sourceRect.height > textureHeight)
                throw new ArgumentOutOfRangeException("dest.y");

            if (textureName == SurfaceMaskTextureName)
            {
                Internal_CopyActiveRenderTextureToSurfaceMask(sourceRect, dest.x, dest.y, allowDelayedCPUSync);
                return;
            }

            var dstTexture = GetAlphamapTexture(textureIndex);

            // Delay synching back (using ReadPixels) if CopyTexture can be used.
            // TODO: Checking the format compatibility is difficult as it varies by platforms. For instance copying between ARGB32 RT and RGBA32 Tex seems to be fine on all tested platforms...
            allowDelayedCPUSync = allowDelayedCPUSync && SupportsCopyTextureBetweenRTAndTexture;
            if (allowDelayedCPUSync)
            {
                if (dstTexture.mipmapCount > 1)
                {
                    // Composes mip0 in a RT with full mipchain.
                    var tmp = RenderTexture.GetTemporary(new RenderTextureDescriptor(dstTexture.width, dstTexture.height, source.format)
                    {
                        sRGB = false,
                        useMipMap = true,
                        autoGenerateMips = false
                    });
                    if (!tmp.IsCreated())
                        tmp.Create();

                    Graphics.CopyTexture(dstTexture, 0, 0, tmp, 0, 0);
                    Graphics.CopyTexture(source, 0, 0, sourceRect.x, sourceRect.y, sourceRect.width, sourceRect.height, tmp, 0, 0, dest.x, dest.y);

                    // Generate the mips on the GPU
                    tmp.GenerateMips();

                    // Copy the full mipchain back to the alphamap texture
                    Graphics.CopyTexture(tmp, dstTexture);

                    RenderTexture.ReleaseTemporary(tmp);
                }
                else
                {
                    Graphics.CopyTexture(source, 0, 0, sourceRect.x, sourceRect.y, sourceRect.width, sourceRect.height, dstTexture, 0, 0, dest.x, dest.y);
                }

                // TODO: Support generic terrain textures.
                Internal_MarkAlphamapDirtyRegion(textureIndex, dest.x, dest.y, sourceRect.width, sourceRect.height);
            }
            else
            {
                if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Metal || !SystemInfo.graphicsUVStartsAtTop)
                    dstTexture.ReadPixels(new Rect(sourceRect.x, sourceRect.y, sourceRect.width, sourceRect.height), dest.x, dest.y);
                else
                    dstTexture.ReadPixels(new Rect(sourceRect.x, source.height - sourceRect.yMax, sourceRect.width, sourceRect.height), dest.x, dest.y);
                dstTexture.Apply(true);

                // TODO: Check if the texture is previously marked dirty?
                // TODO: Support generic terrain textures.
                Internal_ClearAlphamapDirtyRegion(textureIndex);
            }

            Experimental.TerrainAPI.TerrainCallbacks.InvokeTextureChangedCallback(this, textureName, new RectInt(dest.x, dest.y, sourceRect.width, sourceRect.height), !allowDelayedCPUSync);
        }

        public void DirtyTextureRegion(string textureName, RectInt region, bool allowDelayedCPUSync)
        {
            if (String.IsNullOrEmpty(textureName))
                throw new ArgumentNullException("textureName");

            int resolution = 0;

            if (textureName == AlphamapTextureName)
            {
                resolution = alphamapResolution;
            }
            else if (textureName == SurfaceMaskTextureName)
            {
                resolution = surfaceMaskResolution;
            }
            else
            {
                // TODO: Support generic terrain textures.
                throw new ArgumentException($"Unrecognized terrain texture name: \"{textureName}\"");
            }

            if (region.x < 0 || region.x >= resolution)
                throw new ArgumentOutOfRangeException("region.x");
            else if (region.width <= 0 || region.xMax > resolution)
                throw new ArgumentOutOfRangeException("region.width");
            if (region.y < 0 || region.y >= resolution)
                throw new ArgumentOutOfRangeException("region.y");
            else if (region.height <= 0 || region.yMax > resolution)
                throw new ArgumentOutOfRangeException("region.height");

            if (textureName == SurfaceMaskTextureName)
            {
                Internal_DirtySurfaceMaskRegion(region.x, region.y, region.width, region.height, allowDelayedCPUSync);
                return;
            }

            // TODO: Support generic terrain textures.
            Internal_MarkAlphamapDirtyRegion(-1, region.x, region.y, region.width, region.height);

            if (!allowDelayedCPUSync)
                SyncTexture(textureName);
            else
                Experimental.TerrainAPI.TerrainCallbacks.InvokeTextureChangedCallback(this, textureName, region, false);
        }

        public void SyncTexture(string textureName)
        {
            if (String.IsNullOrEmpty(textureName))
                throw new ArgumentNullException("textureName");

            // For now the textureName should always equal to "alphamap".
            if (textureName == AlphamapTextureName)
                Internal_SyncAlphamaps();
            else if (textureName == SurfaceMaskTextureName)
            {
                if (IsSurfaceMaskTextureCompressed())
                    throw new InvalidOperationException("Surface mask texture is compressed. Compressed surface mask texture can not be read back from GPU. Use TerrainData.enableSurfaceMaskTextureCompression to disable surface mask texture compression.");

                Internal_SyncSurfaceMask();
            }
            else
            {
                // TODO: Support generic terrain textures.
                throw new ArgumentException($"Unrecognized terrain texture name: \"{textureName}\"");
            }
        }
    }
}
