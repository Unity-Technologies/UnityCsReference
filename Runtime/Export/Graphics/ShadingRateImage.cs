// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Convenience class to handle differences between regular RenderTexture and shading rate images.
    /// The resolution of shading rate images is in tiles and is platform-dependent. 
    /// Functions that needs to be aware of this will specify if conversion is handled.
    /// </summary>
    public static partial class ShadingRateImage
    {
        /// <summary>
        /// Returns the tile size based on the given pixel size.
        /// </summary>
        /// <param name="pixelSize">Pixel size</param>
        /// <returns>Tile size matching the given pixel size</returns>
        public static Vector2Int GetAllocTileSize(Vector2Int pixelSize)
        {
            return GetAllocTileSize(pixelSize.x, pixelSize.y);
        }

        /// <summary>
        /// Returns the tile size based on the given pixel size.
        /// </summary>
        /// <param name="pixelWidth">Width in pixel size</param>
        /// <param name="pixelHeight">Height in pixels</param>
        /// <returns>Tile size matching the given pixels</returns>
        public static Vector2Int GetAllocTileSize(int pixelWidth, int pixelHeight)
        {
            GetAllocSizeInternal(pixelWidth, pixelHeight, out var w, out var h);
            return new Vector2Int(w, h);
        }

        /// <summary>
        /// Creates a shading rate image. This function performs the conversion from pixel to tile before the allocation.
        /// </summary>
        /// <param name="rtDesc">Render texture descriptor where width and height are in pixels</param>
        /// <returns>The created RenderTexture representing the shading rate image.</returns>
        public static RenderTexture AllocFromPixelSize(in RenderTextureDescriptor rtDesc)
        {
            var tileSize = GetAllocTileSize(rtDesc.width, rtDesc.height);

            var newRtDesc = rtDesc;
            newRtDesc.width = tileSize.x;
            newRtDesc.height = tileSize.y;

            return new RenderTexture(newRtDesc);
        }

        /// <summary>
        /// Utility function to create a RenderTextureDescriptor compatible with a shading rate image.
        /// This function does not perform the conversion from pixel to tile.
        /// </summary>
        /// <param name="width">Width of shading rate image</param>
        /// <param name="height">Height of shading rate image</param>
        /// <param name="volumeDepth">The number of slices of the shading rate image. The default value is 1.</param>
        /// <param name="textureDimension">Dimensionality of the resulting shading rate image. The default value is TextureDimension.Tex2D.</param>
        /// <returns>The render texture descriptor compatible for shading rate image.</returns>
        public static RenderTextureDescriptor GetRenderTextureDescriptor(int width,
            int height,
            int volumeDepth = 1,
            TextureDimension textureDimension = TextureDimension.Tex2D)
        {
            if (ShadingRateInfo.featureTier >= ShadingRateFeatureTier.Tier2)
            {
                return new RenderTextureDescriptor(width, height)
                {
                    msaaSamples = 1,
                    autoGenerateMips = false,
                    volumeDepth = volumeDepth,
                    dimension = textureDimension,
                    graphicsFormat = ShadingRateInfo.graphicsFormat,
                    enableRandomWrite = true,
                    enableShadingRate = true,
                };
            }

            // shading rate image is not supported, return invalid descriptor
            return new RenderTextureDescriptor(0, 0)
            {
                msaaSamples = 0,
                autoGenerateMips = false,
                volumeDepth = 0,
                dimension = TextureDimension.None,
                graphicsFormat = Experimental.Rendering.GraphicsFormat.None,
            };
        }
    }
}
