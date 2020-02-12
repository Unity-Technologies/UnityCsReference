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
            extern public static void Update();

            public const int AllMips = int.MaxValue;
            extern public static void RequestRegion([NotNull] Material mat, int stackNameId, Rect r, int mipMap, int numMips);

            // Apply the virtualtexturing settings to the renderer. This may be an expensive operation so it should be done very sparingly (e.g. during a level load/startup).
            [NativeThrows]
            extern public static void ApplyVirtualTexturingSettings(VirtualTexturingSettings settings);
        }

        [NativeHeader("Modules/VirtualTexturing/ScriptBindings/VirtualTexturing.bindings.h")]
        [StaticAccessor("VirtualTexturing::Editor", StaticAccessorType.DoubleColon)]
        [NativeConditional("UNITY_EDITOR")]
        public static class EditorHelpers
        {
            extern internal static int tileSize { get; }

            [NativeThrows]
            extern public static bool ValidateTextureStack([NotNull] Texture[] textures, out string errorMessage);

            extern internal static bool GetTextureStackSize([NotNull] Material mat, int stackNameId, out int width, out int height);

            [NativeConditional("UNITY_EDITOR", "{}")]
            extern public static GraphicsFormat[] QuerySupportedFormats();
        }

        [NativeHeader("Modules/VirtualTexturing/ScriptBindings/VirtualTexturing.bindings.h")]
        [StaticAccessor("VirtualTexturing::Debugging", StaticAccessorType.DoubleColon)]
        public static class Debugging
        {
            extern public static int GetNumHandles();
            extern public static void GrabHandleInfo([Out] out Handle debugHandle, int index);
            extern public static string GetInfoDump();

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

            extern public static bool debugTilesEnabled { get; set; }
            extern public static bool resolvingEnabled { get; set; }
            extern public static bool flushEveryTickEnabled { get; set; }
        }

        [NativeHeader("Modules/VirtualTexturing/Public/VirtualTextureResolver.h")]
        [StructLayout(LayoutKind.Sequential)]
        public class Resolver : IDisposable
        {
            internal IntPtr m_Ptr;

            public Resolver()
            {
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
                // When in the editor, it is possible that we have both the game and editor view rendering at the same time
                // When they are different resolutions, we rescale the resolver twice each frame. This is obviously not good.
                // As the SRP seems to be shared between editor/game mode, just use 'worst' case resolution
                if (CurrentWidth < width || CurrentHeight < height)
                {
                    CurrentWidth = CurrentWidth < width ? width : CurrentWidth;
                    CurrentHeight = CurrentHeight < height ? height : CurrentHeight;
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
        [UsedByNativeCode]
        public enum VirtualTexturingCacheUsage
        {
            Any,
            Streaming,
            Procedural
        }

        [NativeHeader("Modules/VirtualTexturing/Public/VirtualTexturingSettings.h")]
        [StructLayout(LayoutKind.Sequential)]
        [UsedByNativeCode]
        [Serializable]
        public struct VirtualTexturingGPUCacheSizeOverride
        {
            public VirtualTexturingCacheUsage usage;
            public GraphicsFormat format;
            public uint sizeInMegaBytes;
        };

        [NativeHeader("Modules/VirtualTexturing/Public/VirtualTexturingSettings.h")]
        [StructLayout(LayoutKind.Sequential)]
        [UsedByNativeCode]
        [Serializable]
        public struct VirtualTexturingGPUCacheSettings
        {
            public uint sizeInMegaBytes;
            public VirtualTexturingGPUCacheSizeOverride[] sizeOverrides;
        }

        [NativeHeader("Modules/VirtualTexturing/Public/VirtualTexturingSettings.h")]
        [StructLayout(LayoutKind.Sequential)]
        [UsedByNativeCode]
        [Serializable]
        public struct VirtualTexturingCPUCacheSettings
        {
            public uint sizeInMegaBytes;
        }

        [StructLayout(LayoutKind.Sequential)]
        [NativeHeader("Modules/VirtualTexturing/Public/VirtualTexturingSettings.h")]
        [NativeAsStruct]
        [UsedByNativeCode]
        [Serializable]
        // This is a class on the managed side so it's easier to integrate into a UI and serialize. e.g. the HDRP settings UI/serialization.
        public class VirtualTexturingSettings
        {
            public VirtualTexturingGPUCacheSettings gpuCache;
            public VirtualTexturingCPUCacheSettings cpuCache;
        }

        namespace Procedural
        {
            [NativeHeader("Modules/VirtualTexturing/ScriptBindings/VirtualTexturing.bindings.h")]
            [StaticAccessor("VirtualTexturing::Procedural", StaticAccessorType.DoubleColon)]
            internal static class Binding
            {
                extern internal static ulong Create(CreationParameters p);
                extern internal static void Destroy(ulong handle);

                [NativeThrows] extern internal static int PopRequests(ulong handle, IntPtr requestHandles);
                [NativeThrows][ThreadSafe] extern internal static void GetRequestParameters(IntPtr requestHandles, IntPtr requestParameters, int length);
                [NativeThrows][ThreadSafe] extern internal static void UpdateRequestState(IntPtr requestHandles, IntPtr requestUpdates, int length);

                extern internal static bool BindToMaterialPropertyBlock(ulong handle, [NotNull] MaterialPropertyBlock material, string name);
                extern internal static bool BindToMaterial(ulong handle, [NotNull] Material material, string name);
                extern internal static bool BindGlobally(ulong handle, string name);

                [NativeThrows] extern public static void RequestRegion(ulong handle, Rect r, int mipMap, int numMips);
                [NativeThrows] extern public static void InvalidateRegion(ulong handle, Rect r, int mipMap, int numMips);
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
                internal int borderSize;
                internal int gpuGeneration;

                internal void Validate()
                {
                    if (width <= 0 || height <= 0 || tilesize <= 0)
                    {
                        throw new ArgumentException($"Zero sized dimensions are invalid (width: {width}, height: {height}, tilesize {tilesize}");
                    }
                    if (layers == null || layers.Length > MaxNumLayers)
                    {
                        throw new ArgumentException($"layers is either invalid or has to many layers (maxNumLayers: {MaxNumLayers})");
                    }
                    GraphicsFormat[] supportedFormats =
                    {
                        GraphicsFormat.R8G8B8A8_SRGB,
                        GraphicsFormat.R8G8B8A8_UNorm,
                        GraphicsFormat.R32G32B32A32_SFloat,
                        GraphicsFormat.R8G8_SRGB,
                        GraphicsFormat.R8G8_UNorm,
                        GraphicsFormat.R32_SFloat,
                        GraphicsFormat.A2B10G10R10_UNormPack32
                    };
                    for (int i = 0; i < layers.Length; ++i)
                    {
                        bool valid = false;
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
                            throw new ArgumentException($"Invalid textureformat on layer: {i}. Supported formats are: {supportedFormats}");
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

                public static void CompleteRequests(NativeSlice<TextureStackRequestHandle<T>> requestHandles, NativeSlice<RequestStatus> status)
                {
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
            }

            [UsedByNativeCode]
            [StructLayout(LayoutKind.Sequential)]
            [NativeHeader("Modules/VirtualTexturing/ScriptBindings/VirtualTexturing.bindings.h")]
            public struct CPUTextureStackRequestLayerParameters
            {
                public int scanlineSize;
                public int dataSize;
                [NativeDisableUnsafePtrRestriction] unsafe public void* data;
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

                public void CopyPixelDataToLayer<T>(NativeArray<T> colorData, int layerIdx) where T : struct
                {
                    var layer = GetLayer(layerIdx);

                    NativeArray<T> dstDataAsColor;
                    AtomicSafetyHandle m_Safety = AtomicSafetyHandle.GetTempMemoryHandle();
                    unsafe
                    {
                        dstDataAsColor = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(layer.data, layer.dataSize, Allocator.Temp);
                        NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref dstDataAsColor, m_Safety);
                    }
                    var dstWidth = layer.scanlineSize / UnsafeUtility.SizeOf<T>();
                    for (int i = 0; i < height; ++i)
                    {
                        NativeArray<T>.Copy(colorData, i * width, dstDataAsColor, i * dstWidth, width);
                    }
                    dstDataAsColor.Dispose();
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
                    if (requestHandles.Length < creationParams.maxActiveRequests)
                    {
                        throw new ArgumentException($"Provided slice has invalid length ({requestHandles.Length} given, {creationParams.maxActiveRequests} required).");
                    }
                    unsafe
                    {
                        return Binding.PopRequests(handle, (IntPtr)requestHandles.GetUnsafePtr());
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
                    name = _name;
                    creationParams = _creationParams;
                    creationParams.borderSize = borderSize;
                    creationParams.gpuGeneration = gpuGeneration ? 1 : 0;
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
                    bool success = Binding.BindToMaterialPropertyBlock(handle, mpb, name);
                    if (!success)
                    {
                        throw new InvalidOperationException($"ProceduralTextureStack {name} is not fully initialized on the render thread and cannot bind to a MaterialPropertyBlock. Please try again next frame.");
                    }
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
                    bool success = Binding.BindToMaterial(handle, mat, name);
                    if (!success)
                    {
                        throw new InvalidOperationException($"ProceduralTextureStack {name} is not fully initialized on the render thread and cannot bind to a Material. Please try again next frame.");
                    }
                }

                public void BindGlobally()
                {
                    if (IsValid() == false)
                    {
                        throw new InvalidOperationException($"Invalid ProceduralTextureStack {name}");
                    }
                    bool success = Binding.BindGlobally(handle, name);
                    if (!success)
                    {
                        throw new InvalidOperationException($"ProceduralTextureStack {name} is not fully initialized on the render thread and cannot bind globally. Please try again next frame.");
                    }
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

            [NativeHeader("Modules/VirtualTexturing/ScriptBindings/VirtualTexturing.bindings.h")]
            [StaticAccessor("VirtualTexturing::Procedural", StaticAccessorType.DoubleColon)]
            public static class TextureStackRequestLayerUtil
            {
                public extern static int GetWidth(this GPUTextureStackRequestLayerParameters layer);
                public extern static int GetHeight(this GPUTextureStackRequestLayerParameters layer);
            }
        }
    }
}
