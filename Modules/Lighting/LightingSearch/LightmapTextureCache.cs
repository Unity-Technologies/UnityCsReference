// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Lighting.Utilities;

namespace UnityEditor.Lighting.LightingSearch
{
    /// <summary>
    /// Manages cached lightmap thumbnails with UV overlays and exposure adjustments.
    /// Cache is automatically invalidated when exposure settings change.
    /// </summary>
    internal static class LightmapTextureCache
    {
        static readonly Dictionary<EntityId, Texture2D> s_CachedTextures = new();

        static LightmapTextureCache()
        {
            // Subscribe to exposure changes to invalidate cache
            LightingSearchExposureSettings.ExposureChanged += InvalidateCache;
        }

        /// <summary>
        /// Gets a cached texture or generates a new one with current exposure.
        /// </summary>
        /// <param name="textureEntityId">Entity ID of the source texture (from GetEntityId())</param>
        /// <param name="sourceTexture">Source lightmap texture</param>
        /// <returns>Texture with UV overlay and exposure applied, or null if generation fails</returns>
        public static Texture2D GetOrCreate(EntityId textureEntityId, Texture2D sourceTexture)
        {
            if (sourceTexture == null)
                return null;

            // Return cached if available
            if (s_CachedTextures.TryGetValue(textureEntityId, out var cachedTexture))
                return cachedTexture;

            // Generate new texture with current exposure
            var generatedTexture = TextureExposure.GetTextureUVOverlay(
                sourceTexture,
                LightingSearchExposureSettings.CurrentExposure
            );

            if (generatedTexture != null)
                s_CachedTextures[textureEntityId] = generatedTexture;

            return generatedTexture;
        }

        /// <summary>
        /// Clears the entire texture cache and disposes all cached textures.
        /// Called automatically when exposure changes.
        /// </summary>
        public static void InvalidateCache()
        {
            DisposeAllCachedTextures();
            s_CachedTextures.Clear();
        }

        /// <summary>
        /// Disposes a specific cached texture.
        /// </summary>
        public static void DisposeCachedTexture(EntityId textureEntityId)
        {
            if (s_CachedTextures.TryGetValue(textureEntityId, out var texture) && texture != null)
            {
                Object.DestroyImmediate(texture);
                s_CachedTextures.Remove(textureEntityId);
            }
        }

        static void DisposeAllCachedTextures()
        {
            // Only destroy textures if we're on the main thread
            // If called from a finalizer/background thread, just clear the cache and let Unity clean up
           try
            {
                foreach (var cachedTexture in s_CachedTextures.Values)
                {
                    if (cachedTexture != null)
                        Object.DestroyImmediate(cachedTexture);
                }
            }
            catch (UnityException)
            {
                // Called from background thread - can't destroy, just clear cache
            }
            s_CachedTextures.Clear();
        }
    }
}
