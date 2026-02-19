// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Search.Providers;
using UnityEngine;

namespace UnityEditor.Lighting.LightingSearch
{
    internal enum MaterialGlobalIlluminationDisplay
    {
        None,
        Realtime,
        Baked
    }

    internal enum SimplifiedLightType
    {
        Spot = LightType.Spot,
        Directional = LightType.Directional,
        Point = LightType.Point,
        Area = LightType.Rectangle
    }

    static class LightingSearchSelectors
    {
        internal const string k_SceneProvider = BuiltInSceneObjectsProvider.type;
        internal const string k_AssetProvider = AssetProvider.type;
        internal const string k_ContributeGIFilter = "ContributeGI";
        internal const string k_ReceiveGIFilter = "ReceiveGI";
        internal const string k_ContributeGIPath = k_MeshRendererPath + "ContributeGI";
        internal const string k_ReceiveGIPath = k_MeshRendererPath + "ReceiveGI";
        internal const string k_MeshRendererPath = "Renderer/MeshRenderer/";
        internal const string k_MaterialPath = "Material/";
        internal const string k_MaterialGlobalIlluminationPath = k_MaterialPath + "MaterialGlobalIllumination";
        internal const string k_EmissionColorPath = k_MaterialPath + "EmissionColor";
        internal const string k_LightingSettingsPath = "LightingSettings/";
        internal const string k_MixedLightingModePath = k_LightingSettingsPath + "MixedLightingMode";
        internal const string k_LightmapCompressionPath = k_LightingSettingsPath + "LightmapCompression";
        internal const string k_RenderingLayersFilter = "RenderingLayers";
        internal const string k_RenderingLayersPath = k_MeshRendererPath + "RenderingLayers";
        internal const string k_ReflectionProbeUsageFilter = "ReflectionProbeUsage";
        internal const string k_ReflectionProbeUsagePath = k_MeshRendererPath + "ReflectionProbeUsage";
        internal const string k_ReflectionProbePath = "ReflectionProbe/";
        internal const string k_ReflectionProbeResolutionPath = k_ReflectionProbePath + "Resolution";
        internal const string k_LightPath = "Light/";
        internal const string k_LightTypePath = k_LightPath + "Type";
        internal const string k_ScenePath = "Scene/";
        internal const string k_SceneLightingSettingsPath = k_ScenePath + "LightingSettings";
        internal const string k_SceneLightingGeneratedPath = k_ScenePath + "LightingGenerated";
    }
}
