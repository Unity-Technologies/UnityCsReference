// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Rendering;
using System.Runtime.InteropServices;
using UnityEngine.Scripting;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering
{
    namespace VirtualTexturing
    {
        [NativeHeader("Modules/VirtualTexturing/ScriptBindings/VirtualTexturing.bindings.h")]
        [StaticAccessor("VirtualTexturing::System", StaticAccessorType.DoubleColon)]
        public static class System
        {
            extern internal static bool enabled { get; }

            [NativeThrows] extern public static void Update();

            [NativeThrows] internal static void SetDebugFlag(Guid guid, bool enabled) { SetDebugFlag(guid.ToByteArray(), enabled); }
            [NativeThrows] extern private static void SetDebugFlag(byte[] guid, bool enabled);

            public const int AllMips = int.MaxValue;
        }

        [NativeHeader("Modules/VirtualTexturing/ScriptBindings/VirtualTexturing.bindings.h")]
        [StaticAccessor("VirtualTexturing::Editor", StaticAccessorType.DoubleColon)]
        [NativeConditional("UNITY_EDITOR")]
        public static class EditorHelpers
        {
            [NativeHeader("Runtime/Shaders/SharedMaterialData.h")]
            internal struct StackValidationResult
            {
                public string stackName;
                public string errorMessage;
            }

            [NativeThrows] extern internal static int tileSize { get; }

            [NativeThrows] extern public static bool ValidateTextureStack([NotNull][Unmarshalled] Texture[] textures, out string errorMessage);

            [NativeThrows] extern internal static StackValidationResult[] ValidateMaterialTextureStacks([NotNull] Material mat);

            [NativeConditional("UNITY_EDITOR")]
            [NativeThrows] extern public static GraphicsFormat[] QuerySupportedFormats();
        }

        [NativeHeader("Modules/VirtualTexturing/ScriptBindings/VirtualTexturing.bindings.h")]
        [StaticAccessor("VirtualTexturing::Debugging", StaticAccessorType.DoubleColon)]
        public static class Debugging
        {
            [NativeThrows] extern public static int GetNumHandles();
            [NativeThrows] extern public static void GrabHandleInfo([Out] out Handle debugHandle, int index);
            [NativeThrows] extern public static string GetInfoDump();

            [NativeHeader("Modules/VirtualTexturing/Public/VirtualTexturingDebugHandle.h")]
            [StructLayout(LayoutKind.Sequential)]
            [UsedByNativeCode]
            public struct Handle
            {
                public long handle; //Handle number as exposed outside of module
                public string group; //Group of this handle (currently tile set)
                public string name; //Name of this handle
                public int numLayers; //Number of layers
                public Material material; //Material to initialize with gpu data. If null this is skipped.
            }

            [NativeThrows] extern public static bool debugTilesEnabled { get; set; }
            [NativeThrows] extern public static bool resolvingEnabled { get; set; }
            [NativeThrows] extern public static bool flushEveryTickEnabled { get; set; }
            [NativeThrows] extern public static int mipPreloadedTextureCount { get; }
        }

        [NativeHeader("Modules/VirtualTexturing/Public/VirtualTextureResolver.h")]
        [StructLayout(LayoutKind.Sequential)]
        public class Resolver : IDisposable
        {
            internal IntPtr m_Ptr;

            public Resolver()
            {
                if (System.enabled == false)
                {
                    throw new InvalidOperationException("Virtual texturing is not enabled in the player settings.");
                }
                m_Ptr = InitNative();
            }

            ~Resolver()
            {
                Dispose(false);
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            protected virtual void Dispose(bool disposing)
            {
                // we don't have any managed references, so 'disposing' part of
                // standard IDisposable pattern does not apply

                // Release native resources
                if (m_Ptr != IntPtr.Zero)
                {
                    Flush_Internal();
                    ReleaseNative(m_Ptr);
                    m_Ptr = IntPtr.Zero;
                }
            }

            private static extern IntPtr InitNative();

            [NativeMethod(IsThreadSafe = true)]
            private static extern void ReleaseNative(IntPtr ptr);

            extern void Flush_Internal();
            extern void Init_Internal(int width, int height);

            public int CurrentWidth { get; private set; } = 0;
            public int CurrentHeight { get; private set; } = 0;

            public void UpdateSize(int width, int height)
            {
                if (CurrentWidth != width || CurrentHeight != height)
                {
                    if (width <= 0 || height <= 0)
                    {
                        throw new ArgumentException($"Zero sized dimensions are invalid (width: {width}, height: {height}.");
                    }

                    CurrentWidth = width;
                    CurrentHeight = height;

                    Flush_Internal();
                    Init_Internal((int)CurrentWidth, (int)CurrentHeight);
                }
            }

            public void Process(CommandBuffer cmd, RenderTargetIdentifier rt)
            {
                Process(cmd, rt, 0, CurrentWidth, 0, CurrentHeight, 0, 0);
            }

            public void Process(CommandBuffer cmd, RenderTargetIdentifier rt, int x, int width, int y, int height, int mip, int slice)
            {
                if (cmd == null)
                {
                    throw new ArgumentNullException("cmd");
                }
                cmd.ProcessVTFeedback(rt, m_Ptr, slice, x, width, y, height, mip);
            }
        }

        [NativeHeader("Modules/VirtualTexturing/Public/VirtualTexturingSettings.h")]
        [StructLayout(LayoutKind.Sequential)]
        [UsedByNativeCode]
        [Serializable]
        public struct GPUCacheSetting
        {
            public GraphicsFormat format;
            public uint sizeInMegaBytes;
        };

        [NativeHeader("Modules/VirtualTexturing/Public/VirtualTexturingFilterMode.h")]
        public enum FilterMode
        {
            Bilinear = 1,
            Trilinear = 2
        }

        [NativeHeader("Modules/VirtualTexturing/ScriptBindings/VirtualTexturing.bindings.h")]
        [StaticAccessor("VirtualTexturing::Streaming", StaticAccessorType.DoubleColon)]
        public static class Streaming
        {
            [NativeThrows]
            extern public static void RequestRegion([NotNull] Material mat, int stackNameId, Rect r, int mipMap, int numMips);
            [NativeThrows]
            extern public static void GetTextureStackSize([NotNull] Material mat, int stackNameId, out int width, out int height);

            // Set the size of the CPU cache(s). This can cause a noticeable hiccup as a lot of system memory needs to be reallocated.
            [NativeThrows]
            extern public static void SetCPUCacheSize(int sizeInMegabytes);
            [NativeThrows]
            extern public static int GetCPUCacheSize();

            // Apply settings to the streaming GPU caches. In the worst case this triggers a recreation of all streaming GPU caches which takes several frames to be fully applied.
            [NativeThrows]
            extern public static void SetGPUCacheSettings(GPUCacheSetting[] cacheSettings);
            [NativeThrows]
            extern public static GPUCacheSetting[] GetGPUCacheSettings();
            [NativeThrows]
            extern public static void EnableMipPreloading(int texturesPerFrame, int mipCount);
        }

        [NativeHeader("Modules/VirtualTexturing/ScriptBindings/VirtualTexturing.bindings.h")]
        [StaticAccessor("VirtualTexturing::Procedural", StaticAccessorType.DoubleColon)]
        [Obsolete("Procedural Virtual Texturing is experimental, not ready for production use and Unity does not currently support it. The feature might be changed or removed in the future.", false)]
        public static class Procedural
        {
            // Set the size of the CPU cache(s). All PVT Stacks must have been freed before calling this.
            [NativeThrows] extern public static void SetCPUCacheSize(int sizeInMegabytes);
            [NativeThrows] extern public static int GetCPUCacheSize();

            // Apply settings to the streaming GPU caches. All PVT Stacks must have been freed before calling this.
            [NativeThrows] extern public static void SetGPUCacheSettings(GPUCacheSetting[] cacheSettings);
            [NativeThrows] extern public static GPUCacheSetting[] GetGPUCacheSettings();

            [NativeHeader("Modules/VirtualTexturing/ScriptBindings/VirtualTexturing.bindings.h")]
            [StaticAccessor("VirtualTexturing::Procedural", StaticAccessorType.DoubleColon)]
            internal static class Binding
            {
                extern internal static ulong Create(CreationParameters p);
                extern internal static void Destroy(ulong handle);

                [NativeThrows] extern internal static int PopRequests(ulong handle, IntPtr requestHandles, int length);
                [NativeThrows][ThreadSafe] extern internal static void GetRequestParameters(IntPtr requestHandles, IntPtr requestParameters, int length);

                // These are two version instead of just one function with fenceBuffer==null so the version without CommandBuffer is burst compatible
                [NativeThrows][ThreadSafe] extern internal static void UpdateRequestState(IntPtr requestHandles, IntPtr requestUpdates, int length);
                [NativeThrows][ThreadSafe] extern internal static void UpdateRequestStateWithCommandBuffer(IntPtr requestHandles, IntPtr requestUpdates, int length, CommandBuffer fenceBuffer);

                extern internal static void BindToMaterialPropertyBlock(ulong handle, [NotNull] MaterialPropertyBlock material, string name);
                extern internal static void BindToMaterial(ulong handle, [NotNull] Material material, string name);
                extern internal static void BindGlobally(ulong handle, string name);

                [NativeThrows] extern internal static void RequestRegion(ulong handle, Rect r, int mipMap, int numMips);
                [NativeThrows] extern internal static void InvalidateRegion(ulong handle, Rect r, int mipMap, int numMips);
                [NativeThrows] extern public static void EvictRegion(ulong handle, Rect r, int mipMap, int numMips);
            }

            [StructLayout(LayoutKind.Sequential)]
            [NativeHeader("Modules/VirtualTexturing/ScriptBindings/VirtualTexturing.bindings.h")]
            public struct CreationParameters
            {
                public const int MaxNumLayers = 4;
                public const int MaxRequestsPerFrameSupported = 0x0fff;

                public int width, height;
                public int maxActiveRequests;
                public int tilesize;
                public GraphicsFormat[] layers;
                public FilterMode filterMode;
                internal int borderSize;
                internal int gpuGeneration;
                internal int flags;

                internal void Validate()
                {
                    if (width <= 0 || height <= 0 || tilesize <= 0)
                    {
                        throw new ArgumentException($"Zero sized dimensions are invalid (width: {width}, height: {height}, tilesize {tilesize}");
                    }
                    if (layers == null || layers.Length > MaxNumLayers)
                    {
                        throw new ArgumentException($"layers is either invalid or has too many layers (maxNumLayers: {MaxNumLayers})");
                    }
                    if (gpuGeneration == 1 && filterMode != FilterMode.Bilinear)
                    {
                        throw new ArgumentException("Filter mode invalid for GPU PVT; only FilterMode.Bilinear is currently supported");
                    }
                    if (gpuGeneration == 0 && (filterMode != FilterMode.Bilinear) && (filterMode != FilterMode.Trilinear))
                    {
                        throw new ArgumentException("Filter mode invalid for CPU PVT; only FilterMode.Bilinear and FilterMode.Trilinear are currently supported");
                    }
                    GraphicsFormat[] supportedFormatsCPU =
                    {
                        GraphicsFormat.R8G8B8A8_SRGB,
                        GraphicsFormat.R8G8B8A8_UNorm,
                        GraphicsFormat.R32G32B32A32_SFloat,
                        GraphicsFormat.R8G8_SRGB,
                        GraphicsFormat.R8G8_UNorm,
                        GraphicsFormat.R32_SFloat,
                        GraphicsFormat.RGBA_DXT1_SRGB,
                        GraphicsFormat.RGBA_DXT1_UNorm,
                        GraphicsFormat.RGBA_DXT5_SRGB,
                        GraphicsFormat.RGBA_DXT5_UNorm,
                        GraphicsFormat.RGBA_BC7_SRGB,
                        GraphicsFormat.RGBA_BC7_UNorm,
                        GraphicsFormat.RG_BC5_SNorm,
                        GraphicsFormat.RG_BC5_UNorm,
                        GraphicsFormat.RGB_BC6H_SFloat,
                        GraphicsFormat.RGB_BC6H_UFloat,
                        GraphicsFormat.R16_SFloat,
                        GraphicsFormat.R16_UNorm,
                        GraphicsFormat.R16G16_SFloat,
                        GraphicsFormat.R16G16_UNorm,
                        GraphicsFormat.R16G16B16A16_SFloat,
                        GraphicsFormat.R16G16B16A16_UNorm,
                    };
                    GraphicsFormat[] supportedFormatsGPU =
                    {
                        GraphicsFormat.R8G8B8A8_SRGB,
                        GraphicsFormat.R8G8B8A8_UNorm,
                        GraphicsFormat.R32G32B32A32_SFloat,
                        GraphicsFormat.R8G8_SRGB,
                        GraphicsFormat.R8G8_UNorm,
                        GraphicsFormat.R32_SFloat,
                        GraphicsFormat.A2B10G10R10_UNormPack32,
                        GraphicsFormat.R16_UNorm
                    };

                    //GPU PVT relies on Render usage to not cause fallback behaviour.
                    //To allow CPU PVT Sample has to be supported on the format.

                    var formatUsage = (gpuGeneration == 1) ? FormatUsage.Render : FormatUsage.Sample;
                    for (int i = 0; i < layers.Length; ++i)
                    {
                        if (SystemInfo.GetCompatibleFormat(layers[i], formatUsage) != layers[i])
                        {
                            throw new ArgumentException($"Requested format {layers[i]} on layer {i} is not supported on this platform");
                        }

                        bool valid = false;
                        GraphicsFormat[] supportedFormats = (gpuGeneration == 1) ? supportedFormatsGPU : supportedFormatsCPU;

                        for (int j = 0; j < supportedFormats.Length; ++j)
                        {
                            if (layers[i] == supportedFormats[j])
                            {
                                valid = true;
                                break;
                            }
                        }

                        if (valid == false)
                        {
                            string cpuGpu = (gpuGeneration == 1) ? "GPU" : "CPU";
                            throw new ArgumentException($"{cpuGpu} Procedural Virtual Texturing doesn't support GraphicsFormat {layers[i]} for stack layer {i}");
                        }
                    }
                    if (maxActiveRequests > MaxRequestsPerFrameSupported || maxActiveRequests <= 0)
                    {
                        throw new ArgumentException($"Invalid requests per frame (maxActiveRequests: ]0, {maxActiveRequests}])");
                    }
                }
            }

            [StructLayout(LayoutKind.Sequential)]
            [UsedByNativeCode]
            [NativeHeader("Modules/VirtualTexturing/ScriptBindings/VirtualTexturing.bindings.h")]
            internal struct RequestHandlePayload : IEquatable<RequestHandlePayload>
            {
                internal int id;
                internal int lifetime;
                [NativeDisableUnsafePtrRestriction] internal IntPtr callback;

                //IEquatable
                public static bool operator!=(RequestHandlePayload lhs, RequestHandlePayload rhs) { return !(lhs == rhs); }
                public override bool Equals(object obj) { return obj is RequestHandlePayload && this == (RequestHandlePayload)obj; }
                public bool Equals(RequestHandlePayload other) { return this == other; }
                public override int GetHashCode()
                {
                    var hashCode = -2128608763;
                    hashCode = hashCode * -1521134295 + id.GetHashCode();
                    hashCode = hashCode * -1521134295 + lifetime.GetHashCode();
                    hashCode = hashCode * -1521134295 + callback.GetHashCode();
                    return hashCode;
                }

                public static bool operator==(RequestHandlePayload lhs, RequestHandlePayload rhs)
                {
                    return lhs.id == rhs.id &&
                        lhs.lifetime == rhs.lifetime &&
                        lhs.callback == rhs.callback;
                }
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct TextureStackRequestHandle<T> : IEquatable<TextureStackRequestHandle<T>>
                where T : struct
            {
                internal RequestHandlePayload payload;

                //IEquatable
                public static bool operator!=(TextureStackRequestHandle<T> h1, TextureStackRequestHandle<T> h2) { return !(h1 == h2); }
                public override bool Equals(object obj) { return obj is TextureStackRequestHandle<T> && this == (TextureStackRequestHandle<T>)obj; }
                public bool Equals(TextureStackRequestHandle<T> other) { return this == other; }
                public override int GetHashCode() { return payload.GetHashCode(); }
                public static bool operator==(TextureStackRequestHandle<T> h1, TextureStackRequestHandle<T> h2) { return h1.payload == h2.payload; }

                public void CompleteRequest(RequestStatus status)
                {
                    unsafe
                    {
                        Binding.UpdateRequestState((IntPtr)UnsafeUtility.AddressOf(ref this), (IntPtr)UnsafeUtility.AddressOf(ref status), 1);
                    }
                }

                public void CompleteRequest(RequestStatus status, CommandBuffer fenceBuffer)
                {
                    unsafe
                    {
                        Binding.UpdateRequestStateWithCommandBuffer((IntPtr)UnsafeUtility.AddressOf(ref this), (IntPtr)UnsafeUtility.AddressOf(ref status), 1, fenceBuffer);
                    }
                }

                public static void CompleteRequests(NativeSlice<TextureStackRequestHandle<T>> requestHandles, NativeSlice<RequestStatus> status)
                {
                    if (System.enabled == false)
                    {
                        throw new InvalidOperationException("Virtual texturing is not enabled in the player settings.");
                    }

                    if (requestHandles != null && status != null)
                    {
                        if (requestHandles.Length != status.Length)
                        {
                            throw new ArgumentException($"Array sizes do not match ({requestHandles.Length} handles, {status.Length} requests)");
                        }
                    }

                    unsafe
                    {
                        Binding.UpdateRequestState((IntPtr)requestHandles.GetUnsafePtr(), (IntPtr)status.GetUnsafePtr(), requestHandles.Length);
                    }
                }

                public static void CompleteRequests(NativeSlice<TextureStackRequestHandle<T>> requestHandles, NativeSlice<RequestStatus> status, CommandBuffer fenceBuffer)
                {
                    if (System.enabled == false)
                    {
                        throw new InvalidOperationException("Virtual texturing is not enabled in the player settings.");
                    }

                    if (requestHandles != null && status != null)
                    {
                        if (requestHandles.Length != status.Length)
                        {
                            throw new ArgumentException($"Array sizes do not match ({requestHandles.Length} handles, {status.Length} requests)");
                        }
                    }

                    unsafe
                    {
                        Binding.UpdateRequestStateWithCommandBuffer((IntPtr)requestHandles.GetUnsafePtr(), (IntPtr)status.GetUnsafePtr(), requestHandles.Length, fenceBuffer);
                    }
                }

                public T GetRequestParameters()
                {
                    T request = new T();
                    unsafe
                    {
                        Binding.GetRequestParameters((IntPtr)UnsafeUtility.AddressOf(ref this), (IntPtr)UnsafeUtility.AddressOf(ref request), 1);
                    }
                    return request;
                }

                public static void GetRequestParameters(NativeSlice<TextureStackRequestHandle<T>> handles, NativeSlice<T> requests)
                {
                    if (System.enabled == false)
                    {
                        throw new InvalidOperationException("Virtual texturing is not enabled in the player settings.");
                    }

                    if (handles != null && requests != null)
                    {
                        if (handles.Length != requests.Length)
                        {
                            throw new ArgumentException($"Array sizes do not match ({handles.Length} handles, {requests.Length} requests)");
                        }
                    }
                    unsafe
                    {
                        Binding.GetRequestParameters((IntPtr)handles.GetUnsafePtr(), (IntPtr)requests.GetUnsafePtr(), handles.Length);
                    }
                }
            }

            [UsedByNativeCode]
            [StructLayout(LayoutKind.Sequential)]
            [NativeHeader("Modules/VirtualTexturing/ScriptBindings/VirtualTexturing.bindings.h")]
            public struct GPUTextureStackRequestLayerParameters
            {
                public int destX, destY;
                public RenderTargetIdentifier dest;

                public extern int GetWidth();
                public extern int GetHeight();
            }

            [UsedByNativeCode]
            [StructLayout(LayoutKind.Sequential)]
            [NativeHeader("Modules/VirtualTexturing/ScriptBindings/VirtualTexturing.bindings.h")]
            public struct CPUTextureStackRequestLayerParameters
            {
                internal int _scanlineSize;
                internal int dataSize;
                [NativeDisableUnsafePtrRestriction] unsafe internal void* data;

                internal int _mipScanlineSize;
                internal int mipDataSize;
                [NativeDisableUnsafePtrRestriction] unsafe internal void* mipData;

                // Accessors
                public NativeArray<T> GetData<T>() where T : struct
                {
                    unsafe { return NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(data, dataSize, Allocator.None); }
                }

                public NativeArray<T> GetMipData<T>() where T : struct
                {
                    unsafe { return NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(mipData, mipDataSize, Allocator.None); }
                }

                public int scanlineSize => _scanlineSize;
                public int mipScanlineSize => _mipScanlineSize;
                public bool requiresCachedMip => mipDataSize != 0;
            }

            [StructLayout(LayoutKind.Sequential)]
            [UsedByNativeCode]
            [NativeHeader("Modules/VirtualTexturing/ScriptBindings/VirtualTexturing.bindings.h")]
            public struct GPUTextureStackRequestParameters
            {
                public int level;
                public int x, y;
                public int width, height;
                public int numLayers;

                GPUTextureStackRequestLayerParameters layer0;
                GPUTextureStackRequestLayerParameters layer1;
                GPUTextureStackRequestLayerParameters layer2;
                GPUTextureStackRequestLayerParameters layer3;
                public GPUTextureStackRequestLayerParameters GetLayer(int index)
                {
                    switch (index)
                    {
                        case 0:
                            return layer0;
                        case 1:
                            return layer1;
                        case 2:
                            return layer2;
                        case 3:
                            return layer3;
                    }
                    throw new IndexOutOfRangeException();
                }
            }

            [StructLayout(LayoutKind.Sequential)]
            [UsedByNativeCode]
            [NativeHeader("Modules/VirtualTexturing/ScriptBindings/VirtualTexturing.bindings.h")]
            public struct CPUTextureStackRequestParameters
            {
                public int level;
                public int x, y;
                public int width, height;
                public int numLayers;

                CPUTextureStackRequestLayerParameters layer0;
                CPUTextureStackRequestLayerParameters layer1;
                CPUTextureStackRequestLayerParameters layer2;
                CPUTextureStackRequestLayerParameters layer3;
                public CPUTextureStackRequestLayerParameters GetLayer(int index)
                {
                    switch (index)
                    {
                        case 0:
                            return layer0;
                        case 1:
                            return layer1;
                        case 2:
                            return layer2;
                        case 3:
                            return layer3;
                    }
                    throw new IndexOutOfRangeException();
                }
            }

            [UsedByNativeCode]
            internal enum ProceduralTextureStackRequestStatus /// KEEP IN SYNC WITH IVirtualTexturingManager.h
            {
                StatusFree = 0xFFFF,// Anything smaller than this is considered a free slot
                StatusRequested,    // Requested but user C# code is not processing this yet
                StatusProcessing,   // Returned to C#
                StatusComplete,     // C# indicates we're done
                StatusDropped,      // C# indicates we no longer want to do this one
            }

            public enum RequestStatus
            {
                Dropped = ProceduralTextureStackRequestStatus.StatusDropped,
                Generated = ProceduralTextureStackRequestStatus.StatusComplete
            }

            public class TextureStackBase<T> : IDisposable
                where T : struct
            {
                public int PopRequests(NativeSlice<TextureStackRequestHandle<T>> requestHandles)
                {
                    if (IsValid() == false)
                    {
                        throw new InvalidOperationException($"Invalid ProceduralTextureStack {name}");
                    }

                    if (requestHandles == null)
                    {
                        throw new ArgumentNullException();
                    }
                    unsafe
                    {
                        return Binding.PopRequests(handle, (IntPtr)requestHandles.GetUnsafePtr(), requestHandles.Length);
                    }
                }

                internal ulong handle;
                public readonly static int borderSize = 8;

                public bool IsValid()
                {
                    return handle != 0;
                }

                string name;
                CreationParameters creationParams;

                public TextureStackBase(string _name, CreationParameters _creationParams, bool gpuGeneration)
                {
                    if (System.enabled == false)
                    {
                        throw new InvalidOperationException("Virtual texturing is not enabled in the player settings.");
                    }

                    name = _name;
                    creationParams = _creationParams;
                    creationParams.borderSize = borderSize;
                    creationParams.gpuGeneration = gpuGeneration ? 1 : 0;
                    creationParams.flags = 0;
                    creationParams.Validate();
                    handle = Binding.Create(creationParams);
                }

                public void Dispose()
                {
                    if (IsValid())
                    {
                        Binding.Destroy(handle);
                        handle = 0;
                    }
                }

                public void BindToMaterialPropertyBlock(MaterialPropertyBlock mpb)
                {
                    if (mpb == null)
                    {
                        throw new ArgumentNullException("mbp");
                    }
                    if (IsValid() == false)
                    {
                        throw new InvalidOperationException($"Invalid ProceduralTextureStack {name}");
                    }
                    Binding.BindToMaterialPropertyBlock(handle, mpb, name);
                }

                public void BindToMaterial(Material mat)
                {
                    if (mat == null)
                    {
                        throw new ArgumentNullException("mat");
                    }
                    if (IsValid() == false)
                    {
                        throw new InvalidOperationException($"Invalid ProceduralTextureStack {name}");
                    }
                    Binding.BindToMaterial(handle, mat, name);
                }

                public void BindGlobally()
                {
                    if (IsValid() == false)
                    {
                        throw new InvalidOperationException($"Invalid ProceduralTextureStack {name}");
                    }
                    Binding.BindGlobally(handle, name);
                }

                public const int AllMips = int.MaxValue;

                public void RequestRegion(Rect r, int mipMap, int numMips)
                {
                    if (IsValid() == false)
                    {
                        throw new InvalidOperationException($"Invalid ProceduralTextureStack {name}");
                    }
                    Binding.RequestRegion(handle, r, mipMap, numMips);
                }

                public void InvalidateRegion(Rect r, int mipMap, int numMips)
                {
                    if (IsValid() == false)
                    {
                        throw new InvalidOperationException($"Invalid ProceduralTextureStack {name}");
                    }
                    Binding.InvalidateRegion(handle, r, mipMap, numMips);
                }

                public void EvictRegion(Rect r, int mipMap, int numMips)
                {
                    if (IsValid() == false)
                    {
                        throw new InvalidOperationException($"Invalid ProceduralTextureStack {name}");
                    }
                    Binding.EvictRegion(handle, r, mipMap, numMips);
                }
            }

            public sealed class GPUTextureStack : TextureStackBase<GPUTextureStackRequestParameters>
            {
                public GPUTextureStack(string _name, CreationParameters creationParams)
                    : base(_name, creationParams, true)
                {}
            }

            public sealed class CPUTextureStack : TextureStackBase<CPUTextureStackRequestParameters>
            {
                public CPUTextureStack(string _name, CreationParameters creationParams)
                    : base(_name, creationParams, false)
                {}
            }
        }
    }
}
