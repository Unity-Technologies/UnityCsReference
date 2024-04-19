// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;

namespace UnityEditor.LightBaking
{
    [NativeHeader("Editor/Src/GI/InputExtraction/InputExtraction.Bindings.h")]
    [StaticAccessor("InputExtractionBindings", StaticAccessorType.DoubleColon)]
    internal static class InputExtraction
    {
        [StructLayout(LayoutKind.Sequential)]
        public class SourceMap : IDisposable
        {
            internal IntPtr m_Ptr;
            internal bool m_OwnsPtr;

            public SourceMap()
            {
                m_Ptr = Internal_Create();
                m_OwnsPtr = true;
            }
            public SourceMap(IntPtr ptr)
            {
                m_Ptr = ptr;
                m_OwnsPtr = false;
            }
            ~SourceMap()
            {
                Destroy();
            }

            public void Dispose()
            {
                Destroy();
                GC.SuppressFinalize(this);
            }

            void Destroy()
            {
                if (m_OwnsPtr && m_Ptr != IntPtr.Zero)
                {
                    Internal_Destroy(m_Ptr);
                    m_Ptr = IntPtr.Zero;
                }
            }

            public extern int GetInstanceIndex(int instanceID);
            public extern int GetInstanceInstanceID(int instanceIndex);

            public extern int GetTreeCount();
            public extern int GetTreeInstanceIndex(int treeIndex);

            public extern int GetLightIndex(int instanceID);
            public extern int GetLightInstanceID(int lightIndex);

            static extern IntPtr Internal_Create();

            [NativeMethod(IsThreadSafe = true)]
            static extern void Internal_Destroy(IntPtr ptr);

            internal static class BindingsMarshaller
            {
                public static IntPtr ConvertToNative(SourceMap sourceMap) => sourceMap.m_Ptr;
            }
        }

        public static extern bool ExtractFromScene(string outputFolderPath, LightBaker.BakeInput input, SourceMap map);

        public static extern int[] ComputeOcclusionLightIndicesFromBakeInput(LightBaker.BakeInput bakeInput, UnityEngine.Vector3[] probePositions, uint maxLightsPerProbe);

        private static string LookupGameObjectName(SourceMap map, int instanceIndex)
        {
            if (map == null)
                return "";
            int instanceID = map.GetInstanceInstanceID(instanceIndex);
            if (instanceID == 0)
                return "";
            Object obj = EditorUtility.InstanceIDToObject(instanceID);
            if (obj == null)
                return "";
            else if (obj is UnityEngine.GameObject go)
            {
                return go.name;
            }
            else if (obj is UnityEngine.Component component)
            {
                return component.gameObject.name;
            }
            return "";
        }

        public static string LogInstances(LightBaker.BakeInput bakeInput, SourceMap map)
        {
            if (bakeInput is null)
                return string.Empty;
            string message = string.Empty;
            message += $"      instance count\t: {bakeInput.instanceCount}\n";
            for (int i = 0; i < bakeInput.instanceCount; ++i)
            {
                if (map is null)
                    message += $"         Instance [{i}]:\n";
                else
                    message += $"         Instance [{i}] [{LookupGameObjectName(map, i)}]:\n";
                LightBaker.Instance instance = bakeInput.instance((uint)i);
                message += $"            mesh type\t\t: {instance.meshType}\n";
                if (instance.meshType == LightBaker.MeshType.MeshRenderer)
                    message += $"            mesh index\t: {instance.meshIndex}\n";
                else if (instance.meshType == LightBaker.MeshType.Terrain)
                    message += $"            terrain index\t: {instance.terrainIndex}\n";
                message += $"            transform\t\t: {instance.transform.GetRow(0)}\n";
                message += $"                     \t\t: {instance.transform.GetRow(1)}\n";
                message += $"                     \t\t: {instance.transform.GetRow(2)}\n";
                message += $"                     \t\t: {instance.transform.GetRow(3)}\n";
                message += $"            cast shadows\t: {instance.castShadows}\n";
                message += $"            receive shadows\t: {instance.receiveShadows}\n";
                if (instance.meshType == LightBaker.MeshType.MeshRenderer)
                {
                    message += $"            odd negative scale\t: {instance.oddNegativeScale}\n";
                    message += $"            lod group\t\t: {instance.lodGroup}\n";
                    message += $"            lod mask\t\t: {instance.lodMask}\n";
                }
                message += $"            submesh count\t: {instance.submeshMaterialIndices.Length}\n";
                string indices = string.Empty;
                for (int j = 0; j < instance.submeshMaterialIndices.Length; ++j)
                    indices += instance.submeshMaterialIndices[j] + (j < (instance.submeshMaterialIndices.Length - 1) ? ", " : "");
                message += $"            submesh mat idxs\t: {{{indices}}}\n";
            }
            return message;
        }

        public static string LogSceneMaterials(LightBaker.BakeInput bakeInput)
        {
            if (bakeInput is null)
                return string.Empty;
            string message = string.Empty;
            message += $"      material count\t: {bakeInput.materialCount}\n";
            for (int i = 0; i < bakeInput.materialCount; ++i)
            {
                message += $"         Material [{i}]:\n";
                message += $"            doubleSidedGI\t: {bakeInput.GetMaterial((uint)i).doubleSidedGI}\n";
                message += $"            transmissionChannels\t: {bakeInput.GetMaterial((uint)i).transmissionChannels}\n";
                message += $"            transmissionType\t: {bakeInput.GetMaterial((uint)i).transmissionType}\n";
            }
            return message;
        }

        public static string LogSceneCookies(LightBaker.BakeInput bakeInput)
        {
            if (bakeInput is null)
                return string.Empty;
            string message = string.Empty;

            message += $"      cookie tex count\t: {bakeInput.GetCookieCount()}\n";
            for (int i = 0; i < bakeInput.GetCookieCount(); ++i)
            {
                LightBaker.CookieData cookie = bakeInput.GetCookieData((uint)i);
                message += $"         CookieTexture [{i}]:\n";
                message += $"            resolution\t\t: {cookie.resolution.width} x {cookie.resolution.height}\n";
                message += $"            pixelStride\t\t: {cookie.pixelStride}\n";
                message += $"            slices\t\t: {cookie.slices}\n";
                message += $"            repeat\t\t: {cookie.repeat}\n";
            }
            return message;
        }

        public static string LogSceneLights(LightBaker.BakeInput bakeInput)
        {
            if (bakeInput is null)
                return string.Empty;
            string message = string.Empty;
            message += $"      light count\t\t: {bakeInput.GetLightCount()}\n";
            for (int i = 0; i < bakeInput.GetLightCount(); ++i)
            {
                LightBaker.Light light = bakeInput.GetLight((uint)i);
                message += $"         Light [{i}]:\n";
                message += $"            color\t\t: {light.color}\n";
                message += $"            indirect color\t: {light.indirectColor}\n";
                message += $"            orientation\t: {light.orientation}\n";
                message += $"            position\t\t: {light.position}\n";
                message += $"            range\t\t: {light.range}\n";
                message += $"            cookie index\t: {light.cookieTextureIndex}\n";
                message += $"            cookie scale\t: {light.cookieScale}\n";
                message += $"            cone angle\t: {light.coneAngle}\n";
                message += $"            inner cone angle\t: {light.innerConeAngle}\n";
                message += $"            shape0\t\t: {light.shape0}\n";
                message += $"            shape1\t\t: {light.shape1}\n";
                message += $"            type\t\t: {light.type}\n";
                message += $"            mode\t\t: {light.mode}\n";
                message += $"            falloff\t\t: {light.falloff}\n";
                message += $"            angular falloff\t: {light.angularFalloff}\n";
                message += $"            casts shadows\t: {light.castsShadows}\n";
                message += $"            shadowmask chnl\t: {light.shadowMaskChannel}\n";
            }
            return message;
        }

        public static string LogSampleCounts(LightBaker.SampleCount sampleCount)
        {
            string message = string.Empty;
            message += $"            direct\t\t: {sampleCount.directSampleCount}\n";
            message += $"            indirect\t\t: {sampleCount.indirectSampleCount}\n";
            message += $"            environment\t: {sampleCount.environmentSampleCount}\n";
            return message;
        }

        public static string LogSceneSettings(LightBaker.BakeInput bakeInput)
        {
            if (bakeInput is null)
                return string.Empty;
            var lightingSettings = bakeInput.GetLightingSettings();
            string message = string.Empty;
            message += $"      lighting settings\t:\n";
            message += $"         lightmap sample counts:\n";
            message += LogSampleCounts(lightingSettings.lightmapSampleCounts);
            message += $"         lightprobe sample counts:\n";
            message += LogSampleCounts(lightingSettings.probeSampleCounts);
            message += $"         min bounces\t: {lightingSettings.minBounces}\n";
            message += $"         max bounces\t: {lightingSettings.maxBounces}\n";
            message += $"         lightmap bake mode\t: {lightingSettings.lightmapBakeMode}\n";
            message += $"         mixed lighting mode\t: {lightingSettings.mixedLightingMode}\n";
            message += $"         ao enabled\t\t: {lightingSettings.aoEnabled}\n";
            message += $"         ao distance\t\t: {lightingSettings.aoDistance}\n";
            return message;
        }

        public static string LogScene(LightBaker.BakeInput bakeInput, SourceMap map)
        {
            if (bakeInput is null)
                return string.Empty;
            string message = string.Empty;
            message += LogSceneSettings(bakeInput);
            message += LogInstances(bakeInput, map);
            message += $"      mesh count\t\t: {bakeInput.meshCount}\n";
            message += $"      terrain count\t\t: {bakeInput.terrainCount}\n";
            message += $"      heightmap count\t: {bakeInput.heightmapCount}\n";
            message += $"      holemap count\t: {bakeInput.holemapCount}\n";
            message += LogSceneMaterials(bakeInput);
            message += $"      albedo tex count\t: {bakeInput.albedoTextureCount}\n";
            for (int i = 0; i < bakeInput.albedoTextureCount; ++i)
            {
                message += $"         AlbedoTexture [{i}]:\n";
                message += $"            resolution\t\t: {bakeInput.GetAlbedoTextureData((uint)i).resolution.width} x {bakeInput.GetAlbedoTextureData((uint)i).resolution.height}\n";
            }
            message += $"      emissive tex count\t: {bakeInput.emissiveTextureCount}\n";
            for (int i = 0; i < bakeInput.emissiveTextureCount; ++i)
            {
                message += $"         EmissiveTexture [{i}]:\n";
                message += $"            resolution\t\t: {bakeInput.GetEmissiveTextureData((uint)i).resolution.width} x {bakeInput.GetEmissiveTextureData((uint)i).resolution.height}\n";
            }
            message += $"      transmissive tex count\t: {bakeInput.transmissiveTextureCount}\n";
            for (int i = 0; i < bakeInput.transmissiveTextureCount; ++i)
            {
                message += $"         TransmissiveTexture [{i}]:\n";
                message += $"            resolution\t\t: {bakeInput.GetTransmissiveTextureData((uint)i).resolution.width} x {bakeInput.GetTransmissiveTextureData((uint)i).resolution.height}\n";
            }
            message += LogSceneCookies(bakeInput);
            message += LogSceneLights(bakeInput);
            message += $"      lightmap count\t: {bakeInput.lightmapCount}\n";
            for (int i = 0; i < bakeInput.lightmapCount; ++i)
            {
                message += $"         Lightmap [{i}]:\n";
                message += $"            resolution\t\t: {bakeInput.lightmapResolution((uint)i).width} x {bakeInput.lightmapResolution((uint)i).height}\n";
                message += $"            instance count\t: {bakeInput.lightmapInstanceCount((uint)i)}\n";
            }
            return message;
        }
    }
}
