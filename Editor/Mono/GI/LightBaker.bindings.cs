// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.LightTransport;
using UnityEngine.Scripting;

namespace UnityEditor.LightBaking
{
    [NativeHeader("Editor/Src/GI/LightBaker/LightBaker.Bindings.h")]
    [StaticAccessor("LightBakerBindings", StaticAccessorType.DoubleColon)]
    internal static partial class LightBaker
    {
        public enum ResultType : uint
        {
            Success = 0,
            Cancelled,
            JobFailed,
            OutOfMemory,
            InvalidInput,
            LowLevelAPIFailure,
            FailedCreatingJobQueue,
            IOFailed,
            ConnectedToBaker,
            Undefined
        }

        [RequiredByNativeCode]
        public struct Result
        {
            public ResultType type;
            public string message;
            public override string ToString()
            {
                if (message.Length == 0)
                    return $"Result type: '{type}'";
                return $"Result type: '{type}', message: '{message}'";
            }

            public IProbeIntegrator.Result ConvertToIProbeIntegratorResult()
            {
                IProbeIntegrator.Result result = new ()
                {
                    type = (IProbeIntegrator.ResultType)type,
                    message = message
                };
                return result;
            }
        }

        public enum Backend
        {
            CPU = 0,
            GPU = 1,
            UnityComputeGPU = 2
        }
        public enum TransmissionChannels
        {
            Red = 0,
            Alpha = 1,
            AlphaCutout = 2,
            RGB = 3,
            None = 4
        }
        public enum TransmissionType
        {
            Opacity = 0,
            Transparency = 1,
            None = 2
        }
        public enum MeshType
        {
            Terrain = 0,
            MeshRenderer = 1
        }
        public enum MixedLightingMode
        {
            IndirectOnly = 0,
            Subtractive = 1,
            Shadowmask = 2,
        };
        public enum LightmapBakeMode
        {
            NonDirectional = 0,
            CombinedDirectional = 1
        };
        [Flags]
        public enum ProbeRequestOutputType : uint
        {
            RadianceDirect = 1 << 0,
            RadianceIndirect = 1 << 1,
            Validity = 1 << 2,
            MixedLightOcclusion = 1 << 3,
            LightProbeOcclusion = 1 << 4,
            EnvironmentOcclusion = 1 << 5,
            Depth = 1 << 6,
            All = 0xFFFFFFFF
        };
        public struct ProbeRequest
        {
            public ProbeRequestOutputType outputTypeMask;
            public ulong positionOffset;
            public ulong positionLength;
            public float pushoff;
            public string outputFolderPath;

            // Environment occlusion
            public ulong integrationRadiusOffset;
            public uint environmentOcclusionSampleCount;
            public bool ignoreDirectEnvironment;
            public bool ignoreIndirectEnvironment;
        };
        [Flags]
        public enum LightmapRequestOutputType : uint
        {
            IrradianceIndirect = 1 << 0,
            IrradianceDirect = 1 << 1,
            IrradianceEnvironment = 1 << 2,
            Occupancy = 1 << 3,
            Validity = 1 << 4,
            DirectionalityIndirect = 1 << 5,
            DirectionalityDirect = 1 << 6,
            AmbientOcclusion = 1 << 7,
            Shadowmask = 1 << 8,
            Normal = 1 << 9,
            ChartIndex = 1 << 10,
            OverlapPixelIndex = 1 << 11,
            All = 0xFFFFFFFF
        };
        public enum TilingMode : byte
        {   // Assuming a 4k lightmap (16M texels), the tiling will yield the following chunk sizes:
            None = 0,                 // 4k * 4k =    16M texels
            Quarter = 1,              // 2k * 2k =     4M texels
            Sixteenth = 2,            // 1k * 1k =     1M texels
            Sixtyfourth = 3,          // 512 * 512 = 262k texels
            TwoHundredFiftySixth = 4, // 256 * 256 =  65k texels
            Max = TwoHundredFiftySixth,
            Error = 5                 // Error. We don't want to go lower (GPU occupancy will start to be a problem for smaller atlas sizes).
        };
        public struct LightmapRequest
        {
            public LightmapRequestOutputType outputTypeMask;
            public uint lightmapOffset;
            public uint lightmapCount;
            public TilingMode tilingMode;
            public string outputFolderPath;
            public float pushoff;
        };
        public struct Resolution
        {
            public Resolution(uint widthIn, uint heightIn)
            {
                width = widthIn;
                height = heightIn;
            }
            public uint width;
            public uint height;
        }

        public struct TextureData
        {
            public TextureData(Resolution resolutionIn)
            {
                resolution = resolutionIn;
                data = new Vector4[resolution.width * resolution.height];
            }
            public Resolution resolution;
            public Vector4[] data;
        }

        public struct TextureTransform
        {
            public Vector2 scale;
            public Vector2 offset;
        };

        public struct TextureProperties
        {
            public TextureWrapMode wrapModeU;
            public TextureWrapMode wrapModeV;
            public FilterMode filterMode;
            public TextureTransform textureST;
        };

        public struct CookieData
        {
            public CookieData(Resolution resolutionIn, uint pixelStrideIn, uint slicesIn, bool repeatIn)
            {
                resolution = resolutionIn;
                pixelStride = pixelStrideIn;
                slices = slicesIn;
                repeat = repeatIn;
                data = new byte[resolution.width * resolution.height * slices * pixelStride];
            }
            public Resolution resolution;
            public uint pixelStride;
            public uint slices;
            public bool repeat;
            public byte[] data;
        }

        public struct Instance
        {
            public MeshType meshType;
            public int meshIndex; // index into BakeInput::m_MeshData, -1 for Terrain
            public int terrainIndex; // index into BakeInput::m_TerrainData, -1 for MeshRenderer
            public Matrix4x4 transform;
            public bool castShadows;
            public bool receiveShadows;
            public bool oddNegativeScale;
            public int lodGroup;
            public byte lodMask;
            public int[] submeshMaterialIndices;
        }

        public struct Terrain
        {
            public uint heightMapIndex; // index into BakeInput::m_HeightmapData
            public int terrainHoleIndex; // index into BakeInput::m_TerrainHoleData -1 means no hole data
            public float outputResolution;
            public Vector3 heightmapScale;
            public Vector4 uvBounds;
        }

        public struct Material
        {
            public bool doubleSidedGI;
            public TransmissionChannels transmissionChannels;
            public TransmissionType transmissionType;
        }

        public enum LightType : byte
        {
            Directional = 0,
            Point = 1,
            Spot = 2,
            Rectangle = 3,
            Disc = 4,
            SpotPyramidShape = 5,
            SpotBoxShape = 6
        };

        public enum FalloffType : byte
        {
            InverseSquared = 0,
            InverseSquaredNoRangeAttenuation = 1,
            Linear = 2,
            Legacy = 3
        };

        public enum AngularFalloffType : byte
        {
            LUT = 0,
            AnalyticAndInnerAngle = 1
        };

        public enum LightMode : byte
        {
            Realtime = 0,
            Mixed = 1,
            Baked = 2
        };

        public struct Light
        {
            public Vector3 color;
            public Vector3 indirectColor;
            public Quaternion orientation;
            public Vector3 position;
            public float range;
            public int cookieTextureIndex;
            public float cookieScale;
            public float coneAngle;
            public float innerConeAngle;
            public float shape0;
            public float shape1;
            public LightType type;
            public LightMode mode;
            public FalloffType falloff;
            public AngularFalloffType angularFalloff;
            public bool castsShadows;
            public int shadowMaskChannel;
        }

        public struct SampleCount
        {
            public uint directSampleCount;
            public uint indirectSampleCount;
            public uint environmentSampleCount;
        };

        public struct LightingSettings
        {
            public SampleCount lightmapSampleCounts;
            public SampleCount probeSampleCounts;
            public uint minBounces;
            public uint maxBounces;
            public LightmapBakeMode lightmapBakeMode;
            public MixedLightingMode mixedLightingMode;
            public bool aoEnabled;
            public float aoDistance;
        };

        [RequiredByNativeCode]
        [StructLayout(LayoutKind.Sequential)]
        public class ExternalProcessConnection : IDisposable
        {
            private IntPtr _ptr;
            private readonly bool _ownsPtr;

            public ExternalProcessConnection()
            {
                _ptr = Internal_Create();
                _ownsPtr = true;
            }

            public ExternalProcessConnection(IntPtr ptr)
            {
                _ptr = ptr;
                _ownsPtr = false;
            }

            public bool Connect(int bakePortNumber)
            {
                return Internal_Connect(bakePortNumber);
            }

            ~ExternalProcessConnection()
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
                if (_ownsPtr && _ptr != IntPtr.Zero)
                {
                    Internal_Destroy(_ptr);
                    _ptr = IntPtr.Zero;
                }
            }

            [NativeMethod(IsThreadSafe = true)]
            static extern void Internal_Destroy(IntPtr ptr);
            [NativeMethod(IsThreadSafe = true)]
            static extern IntPtr Internal_Create();
            extern bool Internal_Connect(int bakePortNumber);

            internal static class BindingsMarshaller
            {
                public static IntPtr ConvertToNative(ExternalProcessConnection connection) => connection._ptr;
            }
        }

        [RequiredByNativeCode]
        [StructLayout(LayoutKind.Sequential)]
        public class BakeInput : IDisposable
        {
            private IntPtr _ptr;
            private readonly bool _ownsPtr;

            public BakeInput()
            {
                _ptr = Internal_Create();
                _ownsPtr = true;
            }
            public BakeInput(IntPtr ptr)
            {
                _ptr = ptr;
                _ownsPtr = false;
            }
            ~BakeInput()
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
                if (_ownsPtr && _ptr != IntPtr.Zero)
                {
                    Internal_Destroy(_ptr);
                    _ptr = IntPtr.Zero;
                }
            }

            public extern ulong GetByteSize();

            public Texture2D GetAlbedoTexture(uint index)
            {
                if (index >= albedoTextureCount)
                    throw new ArgumentException($"index must be between 0 and {albedoTextureCount - 1}, but was {index}");
                TextureData textureData = GetAlbedoTextureData(index);
                Texture2D tex = new ((int)textureData.resolution.width, (int)textureData.resolution.height, TextureFormat.RGBAFloat, false);
                tex.SetPixelData(textureData.data, 0);
                tex.filterMode = FilterMode.Point;
                tex.Apply();
                return tex;
            }

            public Texture2D GetEmissiveTexture(uint index)
            {
                if (index >= emissiveTextureCount)
                    throw new ArgumentException($"index must be between 0 and {emissiveTextureCount - 1}, but was {index}");
                TextureData textureData = GetEmissiveTextureData(index);
                Texture2D tex = new ((int)textureData.resolution.width, (int)textureData.resolution.height, TextureFormat.RGBAFloat, false);
                tex.SetPixelData(textureData.data, 0);
                tex.filterMode = FilterMode.Point;
                tex.Apply();
                return tex;
            }

            static extern IntPtr Internal_Create();
            [NativeMethod(IsThreadSafe = true)]
            static extern void Internal_Destroy(IntPtr ptr);

            extern LightingSettings Internal_GetLightingSettings();
            public LightingSettings GetLightingSettings()
            {
                return Internal_GetLightingSettings();
            }
            extern void Internal_SetLightingSettings(LightingSettings lightingSettings);
            public void SetLightingSettings(LightingSettings lightingSettings)
            {
                Internal_SetLightingSettings(lightingSettings);
            }

            public extern uint instanceCount { get; }
            extern Instance Internal_Instance(uint index);
            public Instance instance(uint index)
            {
                if (index >= instanceCount)
                    throw new ArgumentException($"index must be between 0 and {instanceCount - 1}, but was {index}");
                Instance instance = Internal_Instance(index);
                return instance;
            }
            extern void Internal_SetInstance(uint index, Instance instance);
            public void instance(uint index, Instance instance)
            {
                if (index >= instanceCount)
                    throw new ArgumentException($"index must be between 0 and {instanceCount - 1}, but was {index}");
                Internal_SetInstance(index, instance);
            }

            public extern uint terrainCount { get; }
            extern Terrain Internal_GetTerrain(uint index);
            public Terrain GetTerrain(uint index)
            {
                if (index >= terrainCount)
                    throw new ArgumentException($"index must be between 0 and {terrainCount - 1}, but was {index}");
                Terrain terrain = Internal_GetTerrain(index);
                return terrain;
            }

            public extern Vector2[] GetUV1VertexData(uint meshIndex);

            public extern uint meshCount { get; }
            public extern uint heightmapCount { get; }
            public extern uint holemapCount { get; }
            public extern uint materialCount { get; }
            extern Material Internal_GetMaterial(uint index);
            extern void Internal_SetMaterial(uint index, Material material);
            public Material GetMaterial(uint index)
            {
                if (index >= materialCount)
                    throw new ArgumentException($"index must be between 0 and {materialCount - 1}, but was {index}");
                Material material = Internal_GetMaterial(index);
                return material;
            }
            public void SetMaterial(uint index, Material material)
            {
                if (index >= materialCount)
                    throw new ArgumentException($"index must be between 0 and {materialCount - 1}, but was {index}");
                Internal_SetMaterial(index, material);
            }
            public int GetMaterialIndex(uint instanceIndex, uint submeshIndex)
            {
                if (instanceIndex >= instanceCount)
                    throw new ArgumentException($"instanceIndex must be between 0 and {instanceCount - 1}, but was {instanceIndex}");
                Instance theInstance = instance(instanceIndex);
                if (theInstance.submeshMaterialIndices.Length == 0)
                    throw new ArgumentException($"instance {instanceIndex} has not materials");
                if (submeshIndex >= theInstance.submeshMaterialIndices.Length)
                    throw new ArgumentException($"submeshIndex must be between 0 and {theInstance.submeshMaterialIndices.Length - 1}, but was {submeshIndex}");
                return theInstance.submeshMaterialIndices[submeshIndex];
            }
            extern uint Internal_GetLightCount();
            public uint GetLightCount()
            {
                return Internal_GetLightCount();
            }
            extern Light Internal_GetLight(uint index);
            extern void Internal_SetLight(uint index, Light light);
            public Light GetLight(uint index)
            {
                if (index >= GetLightCount())
                    throw new ArgumentException($"index must be between 0 and {GetLightCount() - 1}, but was {index}");
                return Internal_GetLight(index);
            }
            public void SetLight(uint index, Light light)
            {
                if (index >= GetLightCount())
                    throw new ArgumentException($"index must be between 0 and {GetLightCount() - 1}, but was {0}");
                Internal_SetLight(index, light);
            }

            extern int Internal_instanceAlbedoEmissiveIndex(uint instanceIndex);
            extern int Internal_instanceTransmissiveIndex(uint instanceIndex, uint submeshIndex);
            public int instanceToAlbedoIndex(uint instanceIndex)
            {
                if (instanceIndex >= instanceCount)
                    throw new ArgumentException($"index must be between 0 and {instanceCount - 1}, but was {instanceIndex}");
                return Internal_instanceAlbedoEmissiveIndex(instanceIndex);
            }
            public int instanceToEmissiveIndex(uint instanceIndex)
            {
                if (instanceIndex >= instanceCount)
                    throw new ArgumentException($"index must be between 0 and {instanceCount - 1}, but was {instanceIndex}");
                return Internal_instanceAlbedoEmissiveIndex(instanceIndex);
            }
            public int instanceToTransmissiveIndex(uint instanceIndex, uint submeshIndex)
            {
                if (instanceIndex >= instanceCount)
                    throw new ArgumentException($"index must be between 0 and {instanceCount - 1}, but was {instanceIndex}");
                Instance inst = instance(instanceIndex);
                int submeshCount = inst.submeshMaterialIndices.Length;
                if (submeshIndex >= submeshCount)
                    throw new ArgumentException($"submeshIndex must be between 0 and {submeshCount - 1}, but was {submeshIndex}");
                int materialIndex = inst.submeshMaterialIndices[submeshIndex];
                if (materialIndex == -1)
                    throw new ArgumentException($"material for submesh {submeshIndex} did not exist.");

                return Internal_instanceTransmissiveIndex(instanceIndex, submeshIndex);
            }

            public extern uint albedoTextureCount { get; }
            extern TextureData Internal_GetAlbedoTextureData(uint index);
            extern void Internal_SetAlbedoTextureData(uint index, TextureData textureData);

            public TextureData GetAlbedoTextureData(uint index)
            {
                if (index >= albedoTextureCount)
                    throw new ArgumentException($"index must be between 0 and {albedoTextureCount - 1}, but was {index}");
                return Internal_GetAlbedoTextureData(index);
            }
            public void SetAlbedoTextureData(uint index, TextureData textureData)
            {
                if (index >= albedoTextureCount)
                    throw new ArgumentException($"index must be between 0 and {albedoTextureCount - 1}, but was {index}");
                Internal_SetAlbedoTextureData(index, textureData);
            }

            public extern uint emissiveTextureCount { get; }
            public extern uint transmissiveTextureCount { get; }
            public extern uint transmissiveTexturePropertiesCount { get; }
            extern TextureData Internal_GetEmissiveTextureData(uint index);
            extern void Internal_SetEmissiveTextureData(uint index, TextureData textureData);
            public TextureData GetEmissiveTextureData(uint index)
            {
                if (index >= emissiveTextureCount)
                    throw new ArgumentException($"index must be between 0 and {emissiveTextureCount - 1}, but was {index}");
                return Internal_GetEmissiveTextureData(index);
            }
            public void SetEmissiveTextureData(uint index, TextureData textureData)
            {
                if (index >= emissiveTextureCount)
                    throw new ArgumentException($"index must be between 0 and {emissiveTextureCount - 1}, but was {index}");
                Internal_SetEmissiveTextureData(index, textureData);
            }

            extern TextureData Internal_GetTransmissiveTextureData(uint index);
            extern void Internal_SetTransmissiveTextureData(uint index, TextureData textureData);
            public TextureData GetTransmissiveTextureData(uint index)
            {
                if (index >= transmissiveTextureCount)
                    throw new ArgumentException($"index must be between 0 and {transmissiveTextureCount - 1}, but was {index}");
                return Internal_GetTransmissiveTextureData(index);
            }
            public void SetTransmissiveTextureData(uint index, TextureData textureData)
            {
                if (index >= emissiveTextureCount)
                    throw new ArgumentException($"index must be between 0 and {transmissiveTextureCount - 1}, but was {index}");
                Internal_SetTransmissiveTextureData(index, textureData);
            }
            extern TextureProperties Internal_GetTransmissiveTextureProperties(uint index);
            public TextureProperties GetTransmissiveTextureProperties(uint index)
            {
                if (index >= transmissiveTexturePropertiesCount)
                    throw new ArgumentException($"index must be between 0 and {transmissiveTexturePropertiesCount - 1}, but was {index}");
                return Internal_GetTransmissiveTextureProperties(index);
            }
            extern void Internal_SetTransmissiveTextureProperties(uint index, TextureProperties textureProperties);
            public void SetTransmissiveTextureProperties(uint index, TextureProperties textureProperties)
            {
                if (index >= transmissiveTexturePropertiesCount)
                    throw new ArgumentException($"index must be between 0 and {transmissiveTexturePropertiesCount - 1}, but was {index}");
                Internal_SetTransmissiveTextureProperties(index, textureProperties);
            }

            public extern uint GetCookieCount();
            extern CookieData Internal_GetCookieData(uint index);
            extern void Internal_SetCookieData(uint index, CookieData cookieData);
            public CookieData GetCookieData(uint index)
            {
                if (index >= GetCookieCount())
                    throw new ArgumentException($"index must be between 0 and {GetCookieCount() - 1}, but was {index}");
                return Internal_GetCookieData(index);
            }
            public void SetCookieData(uint index, CookieData cookieData)
            {
                if (index >= GetCookieCount())
                    throw new ArgumentException($"index must be between 0 and {GetCookieCount() - 1}, but was {index}");
                Internal_SetCookieData(index, cookieData);
            }
            public extern void SetEnvironment(Vector4 color);
            public extern void SetEnvironmentFromTextures(TextureData posX, TextureData negX, TextureData posY, TextureData negY, TextureData posZ, TextureData negZ);
            public extern TextureData GetEnvironmentCubeTexture();

            internal static class BindingsMarshaller
            {
                public static IntPtr ConvertToNative(BakeInput bakeInput) => bakeInput._ptr;
            }
            public extern bool CheckIntegrity();
        }

        [RequiredByNativeCode]
        [StructLayout(LayoutKind.Sequential)]
        public class LightProbeRequests : IDisposable
        {
            private IntPtr _ptr;
            private readonly bool _ownsPtr;

            public LightProbeRequests()
            {
                _ptr = Internal_Create();
                _ownsPtr = true;
            }
            public LightProbeRequests(IntPtr ptr)
            {
                _ptr = ptr;
                _ownsPtr = false;
            }
            ~LightProbeRequests()
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
                if (_ownsPtr && _ptr != IntPtr.Zero)
                {
                    Internal_Destroy(_ptr);
                    _ptr = IntPtr.Zero;
                }
            }

            public extern ulong GetByteSize();
            static extern IntPtr Internal_Create();
            [NativeMethod(IsThreadSafe = true)]
            static extern void Internal_Destroy(IntPtr ptr);

            public void SetIntegrationRadii(float[] positions)
            {
                SetIntegrationRadii(positions.AsSpan());
            }
            public extern void SetIntegrationRadii(ReadOnlySpan<float> positions);
            public extern Vector3[] GetProbePositions();
            public extern float[] GetIntegrationRadii();
            public extern uint lightProbeCount { get; }
            public extern ProbeRequest[] GetProbeRequests();
            public extern void SetLightProbeRequests(ProbeRequest[] requests);

            internal static class BindingsMarshaller
            {
                public static IntPtr ConvertToNative(LightProbeRequests lightProbeRequests) => lightProbeRequests._ptr;
            }
            public extern bool CheckIntegrity();
        }

        [RequiredByNativeCode]
        [StructLayout(LayoutKind.Sequential)]
        public class LightmapRequests : IDisposable
        {
            private IntPtr _ptr;
            private readonly bool _ownsPtr;

            public LightmapRequests()
            {
                _ptr = Internal_Create();
                _ownsPtr = true;
            }
            public LightmapRequests(IntPtr ptr)
            {
                _ptr = ptr;
                _ownsPtr = false;
            }
            ~LightmapRequests()
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
                if (_ownsPtr && _ptr != IntPtr.Zero)
                {
                    Internal_Destroy(_ptr);
                    _ptr = IntPtr.Zero;
                }
            }

            public extern ulong GetByteSize();
            static extern IntPtr Internal_Create();
            [NativeMethod(IsThreadSafe = true)]
            static extern void Internal_Destroy(IntPtr ptr);

            public extern LightmapRequest[] GetLightmapRequests();
            public extern void SetLightmapRequests(LightmapRequest[] requests);

            public uint lightmapInstanceCount(uint index)
            {
                if (index >= lightmapCount)
                    throw new ArgumentException($"index must be between 0 and {lightmapCount - 1}, but was {index}");
                return Internal_InstanceCount(index);
            }

            public extern uint lightmapCount { get; }
            extern uint Internal_LightmapWidth(uint index);
            extern uint Internal_LightmapHeight(uint index);
            extern uint Internal_InstanceCount(uint lightmapIndex);
            public extern void SetLightmapResolution(Resolution resolution);
            public Resolution lightmapResolution(uint index)
            {
                if (index >= lightmapCount)
                    throw new ArgumentException($"index must be between 0 and {lightmapCount - 1}, but was {index}");
                Resolution resolution;
                resolution.width = Internal_LightmapWidth(index);
                resolution.height = Internal_LightmapHeight(index);
                return resolution;
            }

            internal static class BindingsMarshaller
            {
                public static IntPtr ConvertToNative(LightmapRequests lightmapRequests) => lightmapRequests._ptr;
            }
            public extern bool CheckIntegrity();
        }

        [RequiredByNativeCode]
        [StructLayout(LayoutKind.Sequential)]
        public class DeviceSettings : IDisposable
        {
            static extern IntPtr Internal_Create();
            [NativeMethod(IsThreadSafe = true)]

            public extern bool Initialize(Backend backend);

            [NativeMethod(IsThreadSafe = true)]
            static extern void Internal_Destroy(IntPtr ptr);

            private IntPtr _ptr;
            private bool _ownsPtr;

            public DeviceSettings()
            {
                _ptr = Internal_Create();
                _ownsPtr = true;
            }
            public DeviceSettings(IntPtr ptr)
            {
                _ptr = ptr;
                _ownsPtr = false;
            }
            ~DeviceSettings()
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
                if (_ownsPtr && _ptr != IntPtr.Zero)
                {
                    Internal_Destroy(_ptr);
                    _ptr = IntPtr.Zero;
                }
            }

            private extern Backend Internal_GetBackend();
            private extern void Internal_SetBackend(Backend backend);

            internal Backend _backend
            {
                get => Internal_GetBackend();
                set => Internal_SetBackend(value);
            }

            internal static class BindingsMarshaller
            {
                public static IntPtr ConvertToNative(DeviceSettings obj) => obj._ptr;
            }
        }

        public static Result PopulateWorld(BakeInput bakeInput, LightmapRequests lightmapRequests, LightProbeRequests lightProbeRequests, BakeProgressState progress,
            UnityEngine.LightTransport.IDeviceContext context, UnityEngine.LightTransport.IWorld world)
        {
            Result result = new ();
            if (context is RadeonRaysContext radeonRaysContext)
            {
                IntegrationContext integrationContext = new ();
                result = PopulateWorldRadeonRays(bakeInput, lightmapRequests, lightProbeRequests, progress, radeonRaysContext, integrationContext);
                Debug.Assert(world is RadeonRaysWorld);
                var rrWorld = world as RadeonRaysWorld;
                rrWorld.SetIntegrationContext(integrationContext);
            }
            else if (context is WintermuteContext wintermuteContext)
            {
                IntegrationContext integrationContext = new ();
                result = PopulateWorldWintermute(bakeInput, lightmapRequests, lightProbeRequests, progress, wintermuteContext, integrationContext);
                Debug.Assert(world is WintermuteWorld);
                var wmWorld = world as WintermuteWorld;
                wmWorld.SetIntegrationContext(integrationContext);
            }
            return result;
        }

        public static extern bool Serialize(string path, BakeInput input);
        public static extern bool SerializeLightmapRequests(string path, LightmapRequests lightmapRequests);
        public static extern bool SerializeLightProbeRequests(string path, LightProbeRequests lightProbeRequests);
        public static extern bool Deserialize(string path, BakeInput input);
        public static extern bool DeserializeLightmapRequests(string path, LightmapRequests lightmapRequests);
        public static extern bool DeserializeLightProbeRequests(string path, LightProbeRequests lightProbeRequests);

        public static extern Result Bake(BakeInput input, LightmapRequests lightmapRequest, LightProbeRequests lightProbeRequests, DeviceSettings deviceSettings);
        public static extern Result BakeOutOfProcess(BakeInput input, LightmapRequests lightmapRequest, LightProbeRequests lightProbeRequests, DeviceSettings deviceSettings);
        public static extern Result BakeInEditorWorkerProcess(BakeInput input, LightmapRequests lightmapRequest, LightProbeRequests lightProbeRequests, DeviceSettings deviceSettings);
        public static extern bool ReportResultToParentProcess(Result result, ExternalProcessConnection connection);
        [NativeMethod(IsThreadSafe = true)]
        public static extern bool ReportProgressToParentProcess(float progress, ExternalProcessConnection connection);
        public static extern ulong ConvertExpectedWorkToWorkSteps(BakeInput bakeInput, LightmapRequests lightmapRequests, LightProbeRequests lightProbeRequests, ulong sampleCountPerLightmapTexel, ulong sampleCountPerProbe);
    }
}
