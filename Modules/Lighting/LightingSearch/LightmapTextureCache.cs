// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Lighting.Utilities;

namespace UnityEditor.Lighting.LightingSearch
{
    /// <summary>
    /// Caches lightmap textures with UV overlays and exposure adjustments.
    /// Call <see cref="Clear"/> when exposure changes to regenerate cached textures.
    /// </summary>
    internal static class LightmapTextureCache
    {
        static readonly Dictionary<EntityId, Texture2D> s_CachedTextures = new();

        static LightmapTextureCache()
        {
            LightingSearchExposureSettings.ExposureChanged += Clear;
        }

        internal static Texture2D GetOrCreate(EntityId textureEntityId, Texture2D sourceTexture)
        {
            if (sourceTexture == null)
                return null;

            if (s_CachedTextures.TryGetValue(textureEntityId, out var cachedTexture) && cachedTexture != null)
                return cachedTexture;

            var generatedTexture = TextureExposure.GetTextureUVOverlay(
                sourceTexture,
                LightingSearchExposureSettings.CurrentExposure
            );

            if (generatedTexture != null)
                s_CachedTextures[textureEntityId] = generatedTexture;

            return generatedTexture;
        }

        internal static void Clear()
        {
            DisposeAllCachedTextures();
            s_CachedTextures.Clear();
        }

        static void DisposeAllCachedTextures()
        {
            foreach (var cachedTexture in s_CachedTextures.Values)
            {
                if (cachedTexture != null)
                    Object.DestroyImmediate(cachedTexture);
            }
        }
    }
}
