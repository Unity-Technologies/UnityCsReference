// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;
using GITextureType = UnityEngineInternal.GITextureType;
using LightmapType = UnityEngineInternal.LightmapType;

namespace UnityEditor
{
    [NativeHeader("Editor/Src/GI/Enlighten/Visualisers/VisualisationManager.h")]
    internal enum GITextureAvailability
    {
        GITextureUnknown = 0,
        GITextureNotAvailable = 1,
        GITextureLoading = 2,
        GITextureAvailable = 3,
        GITextureAvailabilityCount = 4
    }

    [NativeHeader("Editor/Src/GI/Enlighten/Visualisers/VisualisationManager.h")]
    internal struct VisualisationGITexture
    {
        [NativeName("m_Type")]
        public GITextureType type;
        [NativeName("m_Availability")]
        public GITextureAvailability textureAvailability;
        [NativeName("m_Texture")]
        public Texture2D texture;
        [NativeName("m_Hash")]
        public Hash128 hash;
        [NativeName("m_ContentsHash")]
        public Hash128 contentHash;
    }

    internal sealed partial class LightmapVisualization
    {
        [NativeHeader("Editor/Src/EditorSettings.h")]
        [StaticAccessor("GetEditorSettings()", StaticAccessorType.Dot)]
        [NativeName("ShowLightmapResolutionOverlay")]
        public   static extern bool  showResolution { get; set; }

        [NativeHeader("Editor/Src/Lightmapping.h")]
        [FreeFunction]
        [NativeName("GetLightmapLODLevelScale_Internal")]
        internal extern static float GetLightmapLODLevelScale(UnityEngine.Renderer renderer);
    }

    internal sealed partial class LightmapVisualizationUtility
    {
        static readonly PrefColor kUVColor = new PrefColor("Lightmap Preview/UV Color", 51f / 255f, 111f / 255f, 244f / 255f, 1f);
        static readonly PrefColor kSelectedUVColor = new PrefColor("Lightmap Preview/Selected UV Color", 250f / 255f, 250f / 255f, 0f, 1f);

        [StaticAccessor("VisualisationManager::Get()", StaticAccessorType.Arrow)]
        internal extern static bool IsTextureTypeEnabled(GITextureType textureType);

        [StaticAccessor("VisualisationManager::Get()", StaticAccessorType.Arrow)]
        internal extern static bool IsBakedTextureType(GITextureType textureType);

        [StaticAccessor("VisualisationManager::Get()", StaticAccessorType.Arrow)]
        internal extern static bool IsAtlasTextureType(GITextureType textureType);

        [StaticAccessor("VisualisationManager::Get()", StaticAccessorType.Arrow)]
        public   extern static VisualisationGITexture[] GetRealtimeGITextures(GITextureType textureType);

        [StaticAccessor("VisualisationManager::Get()", StaticAccessorType.Arrow)]
        public   extern static VisualisationGITexture GetRealtimeGITexture(Hash128 inputSystemHash, GITextureType textureType);

        [StaticAccessor("VisualisationManager::Get()", StaticAccessorType.Arrow)]
        public   extern static VisualisationGITexture GetBakedGITexture(int lightmapIndex, int instanceId, GITextureType textureType);

        [StaticAccessor("VisualisationManager::Get()", StaticAccessorType.Arrow)]
        public   extern static VisualisationGITexture GetSelectedObjectGITexture(GITextureType textureType);

        [StaticAccessor("VisualisationManager::Get()", StaticAccessorType.Arrow)]
        public   extern static Hash128 GetBakedGITextureHash(int lightmapIndex, int instanceId, GITextureType textureType);

        [StaticAccessor("VisualisationManager::Get()", StaticAccessorType.Arrow)]
        public   extern static Hash128 GetRealtimeGITextureHash(Hash128 inputSystemHash, GITextureType textureType);

        [StaticAccessor("VisualisationManager::Get()", StaticAccessorType.Arrow)]
        public   extern static Hash128 GetSelectedObjectGITextureHash(GITextureType textureType);

        [StaticAccessor("VisualisationManager::Get()", StaticAccessorType.Arrow)]
        public   extern static GameObject[] GetRealtimeGITextureRenderers(Hash128 inputSystemHash);

        [StaticAccessor("VisualisationManager::Get()", StaticAccessorType.Arrow)]
        public   extern static GameObject[] GetBakedGITextureRenderers(int lightmapIndex);

        [NativeHeader("Runtime/GI/RenderOverlay.h")]
        [FreeFunction("DrawTextureWithUVOverlay")]
        public extern static void DrawTextureWithUVOverlay(Texture2D texture, GameObject selectedGameObject, GameObject[] gameObjects, Rect drawableArea, Rect position, GITextureType textureType, Color uvColor, Color selectedUVColor, float exposure = 0.0f);

        public static void DrawTextureWithUVOverlay(Texture2D texture, GameObject selectedGameObject, GameObject[] gameObjects, Rect drawableArea, Rect position, GITextureType textureType, float exposure = 0.0f)
        {
            DrawTextureWithUVOverlay(texture, selectedGameObject, gameObjects, drawableArea, position, textureType, kUVColor, kSelectedUVColor, exposure);
        }

        [StaticAccessor("VisualisationManager::Get()", StaticAccessorType.Arrow)]
        public   extern static LightmapType GetLightmapType(GITextureType textureType);

        [StaticAccessor("VisualisationManager::Get()", StaticAccessorType.Arrow)]
        [NativeName("GetLightmapST")]
        public   extern static Vector4 GetLightmapTilingOffset(LightmapType lightmapType);
    }
}
