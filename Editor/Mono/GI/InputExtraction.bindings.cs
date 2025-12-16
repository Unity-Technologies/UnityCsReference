// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Bindings;
using Object = System.Object;

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

            public extern int GetInstanceIndex(UnityEngine.EntityId instanceID);
            public extern UnityEngine.EntityId GetInstanceInstanceID(int instanceIndex);

            public extern int GetTreeCount();
            public extern int GetTreeInstanceIndex(int treeIndex);

            public extern int GetLightIndex(UnityEngine.EntityId instanceID);
            public extern UnityEngine.EntityId GetLightInstanceID(int lightIndex);

            static extern IntPtr Internal_Create();

            [NativeMethod(IsThreadSafe = true)]
            static extern void Internal_Destroy(IntPtr ptr);

            internal static class BindingsMarshaller
            {
                public static IntPtr ConvertToNative(SourceMap sourceMap) => sourceMap.m_Ptr;
            }
        }

        public static extern bool ExtractFromScene(string outputFolderPath, BakeInput input, LightmapRequests lightmapRequests, LightProbeRequests lightProbeRequests, SourceMap map, bool probesOnly = false);

        [NativeMethod(IsThreadSafe = true)]
        public static extern int[] ComputeOcclusionLightIndicesFromBakeInput(BakeInput bakeInput, UnityEngine.Vector3[] probePositions, uint maxLightsPerProbe);

        [NativeMethod(IsThreadSafe = true)]
        public static extern int[] GetShadowmaskChannelsFromLightIndices(BakeInput bakeInput, int[] lightIndices);

        private static string LookupGameObjectName(SourceMap map, int instanceIndex)
        {
            if (map == null)
                return "";
            EntityId entityId = map.GetInstanceInstanceID(instanceIndex);
            if (entityId == EntityId.None)
                return "";
            Object obj = EditorUtility.EntityIdToObject(entityId);
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
        private static int[] MaskToLevelList(byte lodMask)
        {
            System.Collections.Generic.List<int> levels = new System.Collections.Generic.List<int>();
            uint b = 1;
            for (int i = 0; i < 8; ++i, b <<= 1)
            {
                if ((lodMask & b) > 0)
                    levels.Add(i);
            }
            return levels.ToArray();
        }

        private static string IntegersToString(int[] levels)
        {
            string str = "";
            for (int i = 0; i < levels.Length; ++i)
            {
                str += levels[i].ToString();
                if (i < levels.Length - 1) str += ", ";
            }
            return str;
        }

        public static string LogInstances(BakeInput bakeInput, SourceMap map)
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
                Instance instance = bakeInput.instance((uint)i);
                message += $"            mesh type\t\t\t: {instance.meshType}\n";
                if (instance.meshType == MeshType.MeshRenderer)
                    message += $"            mesh index\t\t\t: {instance.meshIndex}\n";
                else if (instance.meshType == MeshType.Terrain)
                    message += $"            terrain index\t\t: {instance.terrainIndex}\n";
                message += $"            transform\t\t\t: {instance.transform.GetRow(0)}\n";
                message += $"                     \t\t\t: {instance.transform.GetRow(1)}\n";
                message += $"                     \t\t\t: {instance.transform.GetRow(2)}\n";
                message += $"                     \t\t\t: {instance.transform.GetRow(3)}\n";
                message += $"            cast shadows\t\t: {instance.castShadows}\n";
                message += $"            receive shadows\t: {instance.receiveShadows}\n";
                if (instance.meshType == MeshType.MeshRenderer)
                {
                    int[] levelList = MaskToLevelList(instance.lodMask);
                    string levels = IntegersToString(levelList);

                    message += $"            odd neg scale\t\t: {instance.oddNegativeScale}\n";
                    message += $"            lod group\t\t\t: {instance.lodGroup}\n";
                    message += $"            lod mask\t\t\t: {Convert.ToString(instance.lodMask, 2).PadLeft(8, '0')} - levels [{levels}]\n";
                    message += $"            contrib. lod level\t: {instance.contributingLodLevel}\n";
                }
                message += $"            submesh count\t\t: {instance.submeshMaterialIndices.Length}\n";
                string indices = string.Empty;
                for (int j = 0; j < instance.submeshMaterialIndices.Length; ++j)
                    indices += instance.submeshMaterialIndices[j] + (j < (instance.submeshMaterialIndices.Length - 1) ? ", " : "");
                message += $"            submesh mat idxs\t: {{{indices}}}\n";
                message += $"            albedo tex idx\t\t: {bakeInput.instanceToAlbedoIndex((uint)i)}\n";
                message += $"            emissive tex idx\t: {bakeInput.instanceToEmissiveIndex((uint)i)}\n";
                string transmissiveIndices = string.Empty;
                for (int j = 0; j < instance.submeshMaterialIndices.Length; ++j)
                    transmissiveIndices += bakeInput.instanceToTransmissiveIndex((uint)i, (uint)j) + (j < (instance.submeshMaterialIndices.Length - 1) ? ", " : "");
                message += $"            trans tex idxs\t\t: {{{transmissiveIndices}}}\n";
            }
            return message;
        }

        public static string LogSceneMaterials(BakeInput bakeInput)
        {
            if (bakeInput is null)
                return string.Empty;
            string message = string.Empty;
            message += $"      material count\t\t: {bakeInput.materialCount}\n";
            for (int i = 0; i < bakeInput.materialCount; ++i)
            {
                message += $"         Material [{i}]:\n";
                message += $"            doubleSidedGI\t\t\t: {bakeInput.GetMaterial((uint)i).doubleSidedGI}\n";
                message += $"            transmissionChannels\t: {bakeInput.GetMaterial((uint)i).transmissionChannels}\n";
                message += $"            transmissionType\t\t: {bakeInput.GetMaterial((uint)i).transmissionType}\n";
            }
            return message;
        }

        public static string LogSceneCookies(BakeInput bakeInput)
        {
            if (bakeInput is null)
                return string.Empty;
            string message = string.Empty;

            message += $"      cookie tex count\t: {bakeInput.GetCookieCount()}\n";
            for (int i = 0; i < bakeInput.GetCookieCount(); ++i)
            {
                CookieData cookie = bakeInput.GetCookieData((uint)i);
                message += $"         CookieTexture [{i}]:\n";
                message += $"            resolution\t\t: {cookie.resolution.width} x {cookie.resolution.height}\n";
                message += $"            pixelStride\t\t: {cookie.pixelStride}\n";
                message += $"            slices\t\t: {cookie.slices}\n";
                message += $"            repeat\t\t: {cookie.repeat}\n";
            }
            return message;
        }

        public static string LogSceneLights(BakeInput bakeInput)
        {
            if (bakeInput is null)
                return string.Empty;
            string message = string.Empty;
            message += $"      light count\t\t: {bakeInput.GetLightCount()}\n";
            for (int i = 0; i < bakeInput.GetLightCount(); ++i)
            {
                Light light = bakeInput.GetLight((uint)i);
                message += $"         Light [{i}]:\n";
                message += $"            color\t\t\t\t\t: {light.color}\n";
                message += $"            indirect color\t\t: {light.indirectColor}\n";
                message += $"            orientation\t\t\t: {light.orientation}\n";
                message += $"            position\t\t\t\t: {light.position}\n";
                message += $"            range\t\t\t\t\t: {light.range}\n";
                message += $"            cookie index\t\t: {light.cookieTextureIndex}\n";
                message += $"            cookie scale\t\t: {light.cookieScale}\n";
                message += $"            cone angle\t\t\t: {light.coneAngle}\n";
                message += $"            inner cone angle\t: {light.innerConeAngle}\n";
                message += $"            shape0\t\t\t\t: {light.shape0}\n";
                message += $"            shape1\t\t\t\t: {light.shape1}\n";
                message += $"            type\t\t\t\t\t: {light.type}\n";
                message += $"            mode\t\t\t\t\t: {light.mode}\n";
                message += $"            falloff\t\t\t\t: {light.falloff}\n";
                message += $"            angular falloff\t: {light.angularFalloff}\n";
                message += $"            casts shadows\t\t: {light.castsShadows}\n";
                message += $"            shadowmask chnl\t: {light.shadowMaskChannel}\n";
            }
            return message;
        }

        public static string LogSampleCounts(SampleCount sampleCount)
        {
            string message = string.Empty;
            message += $"            direct\t\t: {sampleCount.directSampleCount}\n";
            message += $"            indirect\t\t: {sampleCount.indirectSampleCount}\n";
            message += $"            environment\t: {sampleCount.environmentSampleCount}\n";
            return message;
        }

        public static string LogSceneSettings(BakeInput bakeInput)
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
            message += $"         min bounces\t\t\t\t: {lightingSettings.minBounces}\n";
            message += $"         max bounces\t\t\t\t: {lightingSettings.maxBounces}\n";
            message += $"         lightmap bake mode\t: {lightingSettings.lightmapBakeMode}\n";
            message += $"         mixed lighting mode\t: {lightingSettings.mixedLightingMode}\n";
            message += $"         ao enabled\t\t\t\t: {lightingSettings.aoEnabled}\n";
            message += $"         ao distance\t\t\t\t: {lightingSettings.aoDistance}\n";
            return message;
        }

        public static string LogScene(BakeInput bakeInput, LightmapRequests lightmapRequests, LightProbeRequests lightProbeRequests, SourceMap map)
        {
            if (bakeInput is null)
                return string.Empty;
            string message = string.Empty;
            message += LogSceneSettings(bakeInput);
            message += LogInstances(bakeInput, map);
            message += $"      mesh count\t\t\t: {bakeInput.meshCount}\n";
            message += $"      terrain count\t\t: {bakeInput.terrainCount}\n";
            message += $"      heightmap count\t: {bakeInput.heightmapCount}\n";
            message += $"      holemap count\t\t: {bakeInput.holemapCount}\n";
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
            message += $"      lightmap count\t: {lightmapRequests.lightmapCount}\n";
            for (int i = 0; i < lightmapRequests.lightmapCount; ++i)
            {
                message += $"         Lightmap [{i}]:\n";
                message += $"            resolution\t\t: {lightmapRequests.lightmapResolution((uint)i).width} x {lightmapRequests.lightmapResolution((uint)i).height}\n";
                message += $"            instance count\t: {lightmapRequests.lightmapInstanceCount((uint)i)}\n";
            }
            return message;
        }
    }
}
