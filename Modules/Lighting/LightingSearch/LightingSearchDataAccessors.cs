// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Lighting.LightingSearch
{
    static class LightingSearchDataAccessors
    {
        internal static SimplifiedLightType? GetLightType(GameObject go)
        {
            if (!go.TryGetComponent<Light>(out var light))
                return null;
            return ToSimplifiedLightType(light.type);
        }

        internal static SimplifiedLightType ToSimplifiedLightType(LightType type)
        {
            switch (type)
            {
                case LightType.Spot:
                case LightType.Pyramid:
                case LightType.Box:
                    return SimplifiedLightType.Spot;
                case LightType.Directional:
                    return SimplifiedLightType.Directional;
                case LightType.Point:
                    return SimplifiedLightType.Point;
                case LightType.Rectangle:
                case LightType.Disc:
                case LightType.Tube:
                    return SimplifiedLightType.Area;
                default:
                    return SimplifiedLightType.Point;
            }
        }
        
        internal static SimplifiedLightType? ToSimplifiedLightTypeFromValue(object value)
        {
            if (value is SimplifiedLightType simplified)
                return simplified;
            if (value is LightType raw)
                return ToSimplifiedLightType(raw);
            if (value is int i)
            {
                if (Enum.IsDefined(typeof(LightType), i))
                    return ToSimplifiedLightType((LightType)i);
            }
            return null;
        }

        internal static void SetLightType(GameObject go, SimplifiedLightType value)
        {
            if (!go.TryGetComponent<Light>(out var light))
                return;

            Undo.RecordObject(light, "Change light type");
            light.type = (LightType)value;
            EditorUtility.SetDirty(light);
        }

        internal static LightmapBakeType? GetLightMode(GameObject go)
        {
            if (!go.TryGetComponent<Light>(out var light))
                return null;
            return light.lightmapBakeType;
        }

        internal static void SetLightMode(GameObject go, LightmapBakeType value)
        {
            if (!go.TryGetComponent<Light>(out var light))
                return;
            if (IsAreaLight(light.type))
                return;

            Undo.RecordObject(light, "Change light mode");
            light.lightmapBakeType = value;
            EditorUtility.SetDirty(light);
        }

        internal static float? GetColorTemperature(GameObject go)
        {
            if (!go.TryGetComponent<Light>(out var light))
                return null;
            return light.colorTemperature;
        }
        
        internal static void SetColorTemperature(GameObject go, float value)
        {
            if (!go.TryGetComponent<Light>(out var light))
                return;

            Undo.RecordObject(light, "Change light temperature");
            light.colorTemperature = value;
            EditorUtility.SetDirty(light);
        }

        internal static bool IsAreaLight(LightType type)
        {
            return type == LightType.Rectangle || type == LightType.Disc || type == LightType.Tube;
        }

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

            Undo.RecordObject(meshRenderer, "Change rendering layer");
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

            Undo.RecordObject(go, "Change contribute GI");
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

            Undo.RecordObject(meshRenderer, "Change receive GI");
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

            Undo.RecordObject(meshRenderer, "Change reflection probe usage");
            meshRenderer.reflectionProbeUsage = value;
        }

        internal static ReflectionProbeMode? GetReflectionProbeMode(GameObject go)
        {
            if (!go.TryGetComponent<ReflectionProbe>(out var reflectionProbe))
                return null;

            return reflectionProbe.mode;
        }

        internal static void SetReflectionProbeMode(GameObject go, ReflectionProbeMode value)
        {
            if (!go.TryGetComponent<ReflectionProbe>(out var reflectionProbe))
                return;

            Undo.RecordObject(reflectionProbe, "Change reflection probe mode");
            reflectionProbe.mode = value;
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

            Undo.RecordObject(reflectionProbe, "Change reflection prbe resolution");
            reflectionProbe.resolution = value;
        }

        internal static MixedLightingMode GetMixedLightingMode(LightingSettings lightingSettings)
        {
            return lightingSettings.mixedBakeMode;
        }

        internal static void SetMixedLightingMode(LightingSettings lightingSettings, MixedLightingMode value)
        {
            Undo.RecordObject(lightingSettings, "Change LigthingSettings mode");
            lightingSettings.mixedBakeMode = value;
        }

        internal static LightmapCompression GetLightmapCompression(LightingSettings lightingSettings)
        {
            return lightingSettings.lightmapCompression;
        }

        internal static void SetLightmapCompression(LightingSettings lightingSettings, LightmapCompression value)
        {
            Undo.RecordObject(lightingSettings, "Change LigthingSettings lightmap compression");
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

            Undo.RecordObject(material, "Change material emission color");
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

            Undo.RecordObject(material, "Change material global illumination flags");
            material.globalIlluminationFlags = flags;
        }
    }
}
