// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Lighting.LightingSearch
{
    static class LightingSearchDataAccessors
    {
        internal static uint GetRenderingLayers(GameObject go)
        {
            if (!go.TryGetComponent<MeshRenderer>(out var meshRenderer))
                return 0;

            return meshRenderer.renderingLayerMask;
        }

        internal static void SetRenderingLayers(GameObject go, uint value)
        {
            if (!go.TryGetComponent<MeshRenderer>(out var meshRenderer))
                return;

            meshRenderer.renderingLayerMask = value;
        }

        internal static bool GetContributeGI(GameObject go)
        {
            if (!go.TryGetComponent<MeshRenderer>(out var meshRenderer))
                return false;

            return GameObjectUtility.AreStaticEditorFlagsSet(go, StaticEditorFlags.ContributeGI);
        }

        internal static void SetContributeGI(GameObject go, bool value)
        {
            var flag = GameObjectUtility.GetStaticEditorFlags(go);
            if (value)
                flag |= StaticEditorFlags.ContributeGI;
            else
                flag &= ~StaticEditorFlags.ContributeGI;

            GameObjectUtility.SetStaticEditorFlags(go, flag);
        }

        internal static ReceiveGI GetReceiveGI(GameObject go)
        {
            if (!go.TryGetComponent<MeshRenderer>(out var meshRenderer))
                return ReceiveGI.Lightmaps;

            return meshRenderer.receiveGI;
        }

        internal static void SetReceiveGI(GameObject go, ReceiveGI value)
        {
            if (!go.TryGetComponent<MeshRenderer>(out var meshRenderer)) return;

            meshRenderer.receiveGI = value;
        }

        internal static ReflectionProbeUsage GetReflectionProbeUsage(GameObject go)
        {
            if (!go.TryGetComponent<MeshRenderer>(out var meshRenderer))
                return ReflectionProbeUsage.Off;

            return meshRenderer.reflectionProbeUsage;
        }

        internal static void SetReflectionProbeUsage(GameObject go, ReflectionProbeUsage value)
        {
            if (!go.TryGetComponent<MeshRenderer>(out var meshRenderer))
                return;

            meshRenderer.reflectionProbeUsage = value;
        }

        internal static int? GetReflectionProbeResolution(GameObject go)
        {
            if (!go.TryGetComponent<ReflectionProbe>(out var reflectionProbe))
                return null;

            return reflectionProbe.resolution;
        }

        internal static void SetReflectionProbeResolution(GameObject go, int value)
        {
            if (!go.TryGetComponent<ReflectionProbe>(out var reflectionProbe))
                return;

            reflectionProbe.resolution = value;
        }

        internal static MixedLightingMode GetMixedLightingMode(LightingSettings lightingSettings)
        {
            return lightingSettings.mixedBakeMode;
        }

        internal static void SetMixedLightingMode(LightingSettings lightingSettings, MixedLightingMode value)
        {
            lightingSettings.mixedBakeMode = value;
        }

        internal static LightmapCompression GetLightmapCompression(LightingSettings lightingSettings)
        {
            return lightingSettings.lightmapCompression;
        }

        internal static void SetLightmapCompression(LightingSettings lightingSettings, LightmapCompression value)
        {
            lightingSettings.lightmapCompression = value;
        }

        static readonly int k_EmissionColor = Shader.PropertyToID("_EmissionColor");
        internal static Color? GetEmissionColor(Material material)
        {
            if (material == null || !material.HasProperty(k_EmissionColor))
                return null;

            return material.GetColor(k_EmissionColor);
        }

        internal static void SetEmissionColor(Material material, Color color)
        {
            if (material == null || !material.HasProperty(k_EmissionColor))
                return;

            material.SetColor(k_EmissionColor, color);
        }

        internal static MaterialGlobalIlluminationFlags GetMaterialGlobalIlluminationFlags(Material material)
        {
            if (material == null)
                return MaterialGlobalIlluminationFlags.None;

            return material.globalIlluminationFlags;
        }

        internal static void SetMaterialGlobalIlluminationFlags(Material material, MaterialGlobalIlluminationFlags flags)
        {
            if (material == null)
                return;

            material.globalIlluminationFlags = flags;
        }
    }
}
