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
    internal sealed partial class LightmapVisualization
    {
        [NativeHeader("Editor/Src/LightmapEditorSettings.h")]
        [StaticAccessor("GetLightmapEditorSettings()", StaticAccessorType.Dot)]
        [NativeName("ShowResolutionOverlay")]
        public   static extern bool  showResolution { get; set; }

        [NativeHeader("Editor/Src/Lightmapping.h")]
        [FreeFunction]
        [NativeName("GetLightmapLODLevelScale_Internal")]
        internal extern static float GetLightmapLODLevelScale(UnityEngine.Renderer renderer);
    }

    internal sealed partial class LightmapVisualizationUtility
    {
        [StaticAccessor("VisualisationManager::Get()", StaticAccessorType.Arrow)]
        internal extern static bool IsTextureTypeEnabled(GITextureType textureType);

        [StaticAccessor("VisualisationManager::Get()", StaticAccessorType.Arrow)]
        internal extern static bool IsBakedTextureType(GITextureType textureType);

        [StaticAccessor("VisualisationManager::Get()", StaticAccessorType.Arrow)]
        public   extern static VisualisationGITexture GetSelectedObjectGITexture(GITextureType textureType);

        [StaticAccessor("VisualisationManager::Get()", StaticAccessorType.Arrow)]
        public   extern static Hash128 GetSelectedObjectGITextureHash(GITextureType textureType);

        [NativeHeader("Runtime/GI/RenderOverlay.h")]
        [FreeFunction("DrawTextureWithUVOverlay")]
        public   extern static void DrawTextureWithUVOverlay(Texture2D texture, GameObject gameObject, Rect drawableArea, Rect position, GITextureType textureType);

        [StaticAccessor("VisualisationManager::Get()", StaticAccessorType.Arrow)]
        public   extern static LightmapType GetLightmapType(GITextureType textureType);

        [StaticAccessor("VisualisationManager::Get()", StaticAccessorType.Arrow)]
        [NativeName("GetLightmapST")]
        public   extern static Vector4 GetLightmapTilingOffset(LightmapType lightmapType);
    }
}
