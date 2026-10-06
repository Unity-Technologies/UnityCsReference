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
        ///<summary>The virtual texturing system.</summary>
        [NativeHeader("Modules/VirtualTexturing/ScriptBindings/VirtualTexturing.bindings.h")]
        [StaticAccessor("VirtualTexturing::System", StaticAccessorType.DoubleColon)]
        public static class System
        {
            extern internal static bool enabled { get; }

            ///<summary>Update the virtual texturing system.</summary>
            ///<remarks>This should be called every frame.</remarks>
            [NativeMethod(ThrowsException = true)] extern public static void Update();

            internal static void SetDebugFlag(Guid guid, bool enabled) { SetDebugFlagInteger(guid.ToByteArray(), enabled ? 1 : 0); }
            internal static void SetDebugFlagInteger(Guid guid, long value) { SetDebugFlagInteger(guid.ToByteArray(), value); }
            internal static void SetDebugFlagDouble(Guid guid, double value) { SetDebugFlagDouble(guid.ToByteArray(), value); }
            [NativeMethod(ThrowsException = true)] extern private static void SetDebugFlagInteger(byte[] guid, long value);
            [NativeMethod(ThrowsException = true)] extern private static void SetDebugFlagDouble(byte[] guid, double value);

            ///<summary>Request all avalable mips.</summary>
            public const int AllMips = int.MaxValue;
        }

        ///<exclude />
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

            [NativeMethod(ThrowsException = true)] extern internal static int tileSize { get; }

            ///<summary>Checks if a given TextureStack is valid.</summary>
            ///<param name="textures">Textures making up the stack.</param>
            ///<param name="errorMessage">Possible error message if the stack is not valid.</param>
            ///<returns>If the given stack is valid or not.</returns>
            [NativeMethod(ThrowsException = true)] extern public static bool ValidateTextureStack([NotNull] Texture[] textures, out string errorMessage);

            [NativeMethod(ThrowsException = true)] extern internal static StackValidationResult[] ValidateMaterialTextureStacks([NotNull] Material mat);

            ///<summary>Get the formats supported by the virtual texturing system.</summary>
            ///<remarks>Note that this function returns the same values for all platfors supported by the virtual texturing system.</remarks>
            ///<returns>Array of supported formats.</returns>
            [NativeConditional("UNITY_EDITOR")]
            [NativeMethod(ThrowsException = true)] extern public static GraphicsFormat[] QuerySupportedFormats();
        }

        ///<exclude />
        [NativeHeader("Modules/VirtualTexturing/ScriptBindings/VirtualTexturing.bindings.h")]
        [StaticAccessor("VirtualTexturing::Debugging", StaticAccessorType.DoubleColon)]
        public static class Debugging
        {
            ///<exclude />
            [NativeMethod(ThrowsException = true)] extern public static int GetNumHandles();
            ///<exclude />
            [NativeMethod(ThrowsException = true)] extern public static void GrabHandleInfo([Out] out Handle debugHandle, int index);
            ///<exclude />
            [NativeMethod(ThrowsException = true)] extern public static string GetInfoDump();

            ///<exclude />
            [NativeHeader("Modules/VirtualTexturing/Public/VirtualTexturingDebugHandle.h")]
            [StructLayout(LayoutKind.Sequential)]
            [UsedByNativeCode]
            public struct Handle
            {
                ///<exclude />
                public long handle; //Handle number as exposed outside of module
                ///<exclude />
                public string group; //Group of this handle (currently tile set)
                ///<exclude />
                public string name; //Name of this handle
                ///<exclude />
                public int numLayers; //Number of layers
                ///<exclude />
                public Material material; //Material to initialize with gpu data. If null this is skipped.
            }

            ///<exclude />
            [NativeMethod(ThrowsException = true)] extern public static bool debugTilesEnabled { get; set; }
            ///<exclude />
            [NativeMethod(ThrowsException = true)] extern public static bool resolvingEnabled { get; set; }
            ///<exclude />
            [NativeMethod(ThrowsException = true)] extern public static bool flushEveryTickEnabled { get; set; }
            ///<exclude />
            [NativeMethod(ThrowsException = true)] extern public static int mipPreloadedTextureCount { get; }
        }

        ///<summary>Class responsable for virtual texturing feedback analysis.</summary>
        ///<remarks>This class is responsible for performing a GPU-&gt;CPU readback (asyncronous) and starting the feedback analysis.</remarks>
        [NativeHeader("Modules/VirtualTexturing/Public/VirtualTextureResolver.h")]
        [StructLayout(LayoutKind.Sequential)]
        public class Resolver : IDisposable
        {
            internal IntPtr m_Ptr;

            ///<summary>Create a new VirtualTextureResolver object.</summary>
            public Resolver()
            {
                if (System.enabled == false)
                {
                    throw new InvalidOperationException("Virtual texturing is not enabled in the player settings.");
                }
                m_Ptr = InitNative();
            }

            // No finalizer by design: teardown is main-thread-only (it releases GPU readback
            // buffers through the graphics device), so it can't run on the GC finalizer thread.
            ///<summary>Disposes this object.</summary>
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

            ///<summary>Width of the texture that the internal buffers can hold.</summary>
            public int CurrentWidth { get; private set; } = 0;
            ///<summary>Height of the texture that the internal buffers can hold.</summary>
            public int CurrentHeight { get; private set; } = 0;

            ///<summary>Update the internal buffers.</summary>
            ///<remarks>This function is should be called whenever the resolution of the feedback texture changes.</remarks>
            ///<param name="width">Width of the texture passed in during <see cref="Process" />.</param>
            ///<param name="height">Height of the texture passed in during <see cref="Process" />.</param>
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

            ///<summary>Process the passed in feedback texture.</summary>
            ///<param name="cmd">The commandbuffer used to schedule processing.</param>
            ///<param name="rt">Texture containing the feedback data.</param>
            public void Process(CommandBuffer cmd, RenderTargetIdentifier rt)
            {
                Process(cmd, rt, 0, CurrentWidth, 0, CurrentHeight, 0, 0);
            }

            ///<summary>Process the passed in feedback texture.</summary>
            ///<param name="cmd">The commandbuffer used to schedule processing.</param>
            ///<param name="rt">Texture containing the feedback data.</param>
            ///<param name="x">X position of the subrect that is processed.</param>
            ///<param name="width">Width of the subrect that is processed.</param>
            ///<param name="y">Y position of the subrect that is processed.</param>
            ///<param name="height">Height of the subrect that is processed.</param>
            ///<param name="mip">Miplevel of the texture to process.</param>
            ///<param name="slice">Arrayslice of the texture to process.</param>
            public void Process(CommandBuffer cmd, RenderTargetIdentifier rt, int x, int width, int y, int height, int mip, int slice)
            {
                if (cmd == null)
                {
                    throw new ArgumentNullException("cmd");
                }
                cmd.ProcessVTFeedback(rt, m_Ptr, slice, x, width, y, height, mip);
            }

            internal static class BindingsMarshaller
            {
                public static IntPtr ConvertToNative(Resolver resolver) => resolver.m_Ptr;
            }
        }

        ///<summary>Settings for a virtual texturing GPU cache.</summary>
        [NativeHeader("Modules/VirtualTexturing/Public/VirtualTexturingSettings.h")]
        [StructLayout(LayoutKind.Sequential)]
        [UsedByNativeCode]
        [Serializable]
        public struct GPUCacheSetting
        {
            ///<summary>Format of the cache these settings are applied to.</summary>
            ///<remarks>A format of GraphicsFormat.None indicates this GPUCacheSetting will be used for any cache formats not specifically mentioned in the settings.</remarks>
            public GraphicsFormat format;
            ///<summary>Size in MegaBytes of the cache created with these settings.</summary>
            public uint sizeInMegaBytes;
        };

        ///<summary>Filtering modes available in the virtual texturing system.</summary>
        [NativeHeader("Modules/VirtualTexturing/Public/VirtualTexturingFilterMode.h")]
        public enum FilterMode
        {
            ///<summary>Bilinear filtering.</summary>
            Bilinear = 1,
            ///<summary>Trilinear filtering.</summary>
            Trilinear = 2
        }

        ///<summary>Static class representing the Streaming Virtual Texturing system.</summary>
        [NativeHeader("Modules/VirtualTexturing/ScriptBindings/VirtualTexturing.bindings.h")]
        [StaticAccessor("VirtualTexturing::Streaming", StaticAccessorType.DoubleColon)]
        public static class Streaming
        {
            ///<summary>Make a rectangle in UV space resident for a given Virtual Texture Stack.</summary>
            ///<remarks>The system will do it’s best to make this rectangle resident at the requested resolution as fast as possible but due to time and memory constraints this data may take a while to become resident or even never become resident.
            ///This function should be called regularly (preferably every frame) to indicate the continued interest in this data. When this function is no longer called the requested area may be evicted from memory or only be available at a lower resolution. See <see cref="Streaming.RequestRegion" /> for an example using this function.
            ///
            ///The following example requests the 1024 x 1024 pixel mipmap level of a given Virtual Texture Stack.</remarks>
            ///<param name="mat">The Material that contains the Virtual Texture Stack. The Virtual Texture Stacks contained in a Material are declared in the Material's Shader.</param>
            ///<param name="stackNameId">The unique identifier for the name of the Virtual Texture Stack, as declared in the Shader. To find the identifier for a given Shader property name, use <see cref="Shader.PropertyToID" />.</param>
            ///<param name="r">The rectangle in 0-1 UV space to make resident.  Anything outside the [ 0...1 [ x [ 0...1 [ rectangle will be silently ignored.</param>
            ///<param name="mipMap">The mip level to make resident. Mips are numbered from 0 (= full resolution) to n (= lowest resolution) where n is the mipmap level what is a single tile in size.  Requesting invalid mips is silently ignored.</param>
            ///<param name="numMips">The number of mip levels starting from 'mipMap' to make resident. Requesting invalid mips is silently ignored.</param>
            ///<example>
            ///  <code><![CDATA[
            ///using UnityEngine;
            ///
            ///public class GetStackSizeSample : MonoBehaviour
            ///{
            ///    public Material targetMaterial;
            ///    public string stackName;
            ///    private bool m_ShouldRequestRegionForVT = false;
            ///    const float desiredMipmapLevelPixelSize = 1024f;
            ///
            ///    private void OnBecameVisible()
            ///    {
            ///        m_ShouldRequestRegionForVT = true;
            ///    }
            ///
            ///    private void OnBecameInvisible()
            ///    {
            ///        m_ShouldRequestRegionForVT = false;
            ///    }
            ///
            ///    void Update()
            ///    {
            ///        if (m_ShouldRequestRegionForVT)
            ///        {
            ///            int stackPropertyId = Shader.PropertyToID(stackName);
            ///
            ///            // Get size in pixels of the stack.
            ///            int width, height;
            ///            UnityEngine.Rendering.VirtualTexturing.Streaming.GetTextureStackSize(targetMaterial, stackPropertyId, out width, out height);
            ///
            ///            // Calculate the index of the 1024 x 1024 mipmap level.
            ///            int powerOfTwoExponent_RealSize = (int)Mathf.Max(Mathf.Log(width, 2f), Mathf.Log(height, 2f));
            ///            int powerOfTwoExponent_DesiredSize = (int)Mathf.Log(desiredMipmapLevelPixelSize, 2f);
            ///
            ///            // The difference between the real size and the desired size is the same as the mipmap level we want.
            ///            // For example, to get a 1024 x 1024 mipmap level from a 4096 x 4096 texture, use mipmap level 2.
            ///            // If the mipmap level is larger than the texture, fall back to the original texture size at mipmap level 0.
            ///            int mipmapLevel = Mathf.Max(powerOfTwoExponent_RealSize - powerOfTwoExponent_DesiredSize, 0);
            ///
            ///            // Request this mipmap level to be made resident.
            ///            UnityEngine.Rendering.VirtualTexturing.Streaming.RequestRegion(targetMaterial, stackPropertyId, new Rect(0.0f, 0.0f, 1.0f, 1.0f), mipmapLevel, UnityEngine.Rendering.VirtualTexturing.System.AllMips);
            ///        }
            ///    }
            ///}
            ///]]></code>
            ///</example>
            [NativeMethod(ThrowsException = true)]
            extern public static void RequestRegion([NotNull] Material mat, int stackNameId, Rect r, int mipMap, int numMips);
            ///<summary>Gets the width and height of a Virtual Texture Stack, in pixels.</summary>
            ///<remarks>The width and height of a Virtual Texture Stack are usually based on the width and height of the Textures assigned to the Material; however, various factors can cause the width and height of a Virtual Texture Stack to differ from the width and height of its Textures. Use this method to get the current width and height of a Virtual Texture Stack, in pixels.
            ///
            ///Use this function to perform logic based on the width and height of the Virtual Texture Stack, such as calculating a mip level.
            ///
            ///The width and height of a Virtual Texture Stack are constant for a given set of Textures. If you change the Textures assigned to the Material, the width and height of the Virtual Texture Stack might change.
            ///
            ///If you pass invalid data to this method, such as a null Material or an invalid identifier, Unity will throw an exception and the values of <c>width</c> and <c>height</c> will remain unmodified.</remarks>
            ///<param name="mat">The Material that contains the Virtual Texture Stack. The Virtual Texture Stacks contained in a Material are declared in the Material's Shader.</param>
            ///<param name="stackNameId">The unique identifier for the name of the Virtual Texture Stack, as declared in the Shader. To find the identifier for a given Shader property name, use <see cref="Shader.PropertyToID" />.</param>
            ///<param name="width">Unity populates <c>width</c> with the width of the Virtual Texture Stack, in pixels.</param>
            ///<param name="height">Unity populates <c>height</c> with the height of the Virtual Texture Stack, in pixels.</param>
            [NativeMethod(ThrowsException = true)]
            extern public static void GetTextureStackSize([NotNull] Material mat, int stackNameId, out int width, out int height);

            // Set the size of the CPU cache(s). This can cause a noticeable hiccup as a lot of system memory needs to be reallocated.
            ///<summary>Sets the CPU cache size (in MegaBytes) used by Streaming Virtual Texturing.</summary>
            [NativeMethod(ThrowsException = true)]
            extern public static void SetCPUCacheSize(int sizeInMegabytes);
            ///<summary>Gets the CPU cache size (in MegaBytes) used by Streaming Virtual Texturing.</summary>
            [NativeMethod(ThrowsException = true)]
            extern public static int GetCPUCacheSize();

            // Apply settings to the streaming GPU caches. In the worst case this triggers a recreation of all streaming GPU caches which takes several frames to be fully applied.
            ///<summary>Sets the GPU cache settings used by Streaming Virtual Texturing.</summary>
            [NativeMethod(ThrowsException = true)]
            extern public static void SetGPUCacheSettings(GPUCacheSetting[] cacheSettings);
            ///<summary>Gets the GPU cache settings used by Streaming Virtual Texturing.</summary>
            [NativeMethod(ThrowsException = true)]
            extern public static GPUCacheSetting[] GetGPUCacheSettings();
            ///<summary>Enables mipmap level preloading used by Streaming Virtual Texturing.</summary>
            ///<remarks>Use this method to avoid texture pop-in by preloading the smallest-sized mipmap levels into GPU memory. If there are many more virtual textures in materials and <c>texturesPerFrame</c> is too low, you might still see black textures pop in. For more targeted texture preload requests, refer to <see cref="Streaming.RequestRegion" />.</remarks>
            ///<param name="texturesPerFrame">Number of textures per frame to process. The range is <c>0</c> through <c>1024</c>. The default is <c>0</c>. A number of <c>0</c> disables preloading. The higher this number, the more CPU resource will be used on the render thread.</param>
            ///<param name="mipCount">The number of mipmap levels to preload. The range is <c>1</c> through <c>9</c>. The default is <c>1</c>, which preloads only the highest mipmap level with the smallest size of 128 by 128 pixels. This is the size of the Streaming Virtual Texturing tile.</param>
            [NativeMethod(ThrowsException = true)]
            extern public static void EnableMipPreloading(int texturesPerFrame, int mipCount);
        }

        ///<summary>Static class representing the Procedural Virtual Texturing system. Unity does not currently support this system.</summary>
        [NativeHeader("Modules/VirtualTexturing/ScriptBindings/VirtualTexturing.bindings.h")]
        [StaticAccessor("VirtualTexturing::Procedural", StaticAccessorType.DoubleColon)]
        [Obsolete("Procedural Virtual Texturing is experimental, not ready for production use and Unity does not currently support it. The feature might be changed or removed in the future.", false)]
        public static class Procedural
        {
            ///<exclude />
            public static void SetDebugFlagInteger(Guid guid, long value) { System.SetDebugFlagInteger(guid, value); }
            ///<exclude />
            public static void SetDebugFlagDouble(Guid guid, double value) { System.SetDebugFlagDouble(guid, value); }

            // Set the size of the CPU cache(s). All PVT Stacks must have been freed before calling this.
            ///<exclude />
            [NativeMethod(ThrowsException = true)] extern public static void SetCPUCacheSize(int sizeInMegabytes);
            ///<exclude />
            [NativeMethod(ThrowsException = true)] extern public static int GetCPUCacheSize();

            // Apply settings to the streaming GPU caches. All PVT Stacks must have been freed before calling this.
            ///<exclude />
            [NativeMethod(ThrowsException = true)] extern public static void SetGPUCacheSettings(GPUCacheSetting[] cacheSettings);
            ///<exclude />
            [NativeMethod(ThrowsException = true)] extern public static GPUCacheSetting[] GetGPUCacheSettings();

            // Set GPU cache upload staging resources area size. All PVT Stacks must have been freed before calling this.
            // Default is 128 tiles; internally 3x number of that is created to avoid stalls due to frame latency.
            // If you expect to upload much less than 128 tiles per frame, or your tiles are large then you might want
            // to decrease this setting. An editor/game restart is needed for the change to actually take effect.
            ///<exclude />
            [NativeMethod(ThrowsException = true)] extern public static void SetGPUCacheStagingAreaCapacity(uint tilesPerFrame);
            ///<exclude />
            [NativeMethod(ThrowsException = true)] extern public static uint GetGPUCacheStagingAreaCapacity();

            [NativeHeader("Modules/VirtualTexturing/ScriptBindings/VirtualTexturing.bindings.h")]
            [StaticAccessor("VirtualTexturing::Procedural", StaticAccessorType.DoubleColon)]
            internal static class Binding
            {
                extern internal static ulong Create(CreationParameters p);
                extern internal static void Destroy(ulong handle);

                [NativeMethod(ThrowsException = true)] extern internal static int PopRequests(ulong handle, IntPtr requestHandles, int length);
                [NativeMethod(ThrowsException = true, IsThreadSafe = true)] extern internal static void GetRequestParameters(IntPtr requestHandles, IntPtr requestParameters, int length);

                // These are two version instead of just one function with fenceBuffer==null so the version without CommandBuffer is burst compatible
                [NativeMethod(ThrowsException = true, IsThreadSafe = true)] extern internal static void UpdateRequestState(IntPtr requestHandles, IntPtr requestUpdates, int length);
                [NativeMethod(ThrowsException = true, IsThreadSafe = true)] extern internal static void UpdateRequestStateWithCommandBuffer(IntPtr requestHandles, IntPtr requestUpdates, int length, CommandBuffer fenceBuffer);

                extern internal static void BindToMaterialPropertyBlock(ulong handle, [NotNull] MaterialPropertyBlock material, string name);
                extern internal static void BindToMaterial(ulong handle, [NotNull] Material material, string name);
                extern internal static void BindGlobally(ulong handle, string name);

                [NativeMethod(ThrowsException = true)] extern internal static void RequestRegion(ulong handle, Rect r, int mipMap, int numMips);
                [NativeMethod(ThrowsException = true)] extern internal static void InvalidateRegion(ulong handle, Rect r, int mipMap, int numMips);
                [NativeMethod(ThrowsException = true)] extern public static void EvictRegion(ulong handle, Rect r, int mipMap, int numMips);
            }

            ///<exclude />
            [StructLayout(LayoutKind.Sequential)]
            [NativeHeader("Modules/VirtualTexturing/ScriptBindings/VirtualTexturing.bindings.h")]
            public struct CreationParameters
            {
                ///<exclude />
                public const int MaxNumLayers = 4;
                ///<exclude />
                public const int MaxRequestsPerFrameSupported = 0x0fff;

                ///<exclude />
                public int width;
                ///<exclude />
                public int height;
                ///<exclude />
                public int maxActiveRequests;
                ///<exclude />
                public int tilesize;
                ///<exclude />
                public GraphicsFormat[] layers;
                ///<exclude />
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

                    var formatUsage = (gpuGeneration == 1) ? GraphicsFormatUsage.Render : GraphicsFormatUsage.Sample;
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

            ///<exclude />
            [StructLayout(LayoutKind.Sequential)]
            public struct TextureStackRequestHandle<T> : IEquatable<TextureStackRequestHandle<T>>
                where T : struct
            {
                internal RequestHandlePayload payload;

                //IEquatable
                ///<exclude />
                public static bool operator!=(TextureStackRequestHandle<T> h1, TextureStackRequestHandle<T> h2) { return !(h1 == h2); }
                ///<exclude />
                public override bool Equals(object obj) { return obj is TextureStackRequestHandle<T> && this == (TextureStackRequestHandle<T>)obj; }
                ///<exclude />
                public bool Equals(TextureStackRequestHandle<T> other) { return this == other; }
                ///<exclude />
                public override int GetHashCode() { return payload.GetHashCode(); }
                ///<exclude />
                public static bool operator==(TextureStackRequestHandle<T> h1, TextureStackRequestHandle<T> h2) { return h1.payload == h2.payload; }

                ///<exclude />
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

                ///<exclude />
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

                ///<exclude />
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

            ///<exclude />
            [UsedByNativeCode]
            [StructLayout(LayoutKind.Sequential)]
            [NativeHeader("Modules/VirtualTexturing/ScriptBindings/VirtualTexturing.bindings.h")]
            public struct GPUTextureStackRequestLayerParameters
            {
                ///<exclude />
                public int destX;
                ///<exclude />
                public int destY;
                ///<exclude />
                public RenderTargetIdentifier dest;

                ///<exclude />
                public extern int GetWidth();
                ///<exclude />
                public extern int GetHeight();
            }

            ///<exclude />
            [UsedByNativeCode]
            [StructLayout(LayoutKind.Sequential)]
            [NativeHeader("Modules/VirtualTexturing/ScriptBindings/VirtualTexturing.bindings.h")]
            public struct CPUTextureStackRequestLayerParameters
            {
                ///<exclude />
                internal int _scanlineSize;
                internal int dataSize;
                [NativeDisableUnsafePtrRestriction] unsafe internal void* data;

                ///<exclude />
                internal int _mipScanlineSize;
                internal int mipDataSize;
                [NativeDisableUnsafePtrRestriction] unsafe internal void* mipData;

                // Accessors
                ///<exclude />
                public NativeArray<T> GetData<T>() where T : struct
                {
                    unsafe { return NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(data, dataSize, Allocator.None); }
                }

                ///<exclude />
                public NativeArray<T> GetMipData<T>() where T : struct
                {
                    unsafe { return NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(mipData, mipDataSize, Allocator.None); }
                }

                public int scanlineSize => _scanlineSize;
                public int mipScanlineSize => _mipScanlineSize;
                ///<exclude />
                public bool requiresCachedMip => mipDataSize != 0;
            }

            ///<exclude />
            [StructLayout(LayoutKind.Sequential)]
            [UsedByNativeCode]
            [NativeHeader("Modules/VirtualTexturing/ScriptBindings/VirtualTexturing.bindings.h")]
            public struct GPUTextureStackRequestParameters
            {
                ///<exclude />
                public int level;
                ///<exclude />
                public int x;
                ///<exclude />
                public int y;
                ///<exclude />
                public int width;
                ///<exclude />
                public int height;
                ///<exclude />
                public int numLayers;

                GPUTextureStackRequestLayerParameters layer0;
                GPUTextureStackRequestLayerParameters layer1;
                GPUTextureStackRequestLayerParameters layer2;
                GPUTextureStackRequestLayerParameters layer3;
                ///<exclude />
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

            ///<exclude />
            [StructLayout(LayoutKind.Sequential)]
            [UsedByNativeCode]
            [NativeHeader("Modules/VirtualTexturing/ScriptBindings/VirtualTexturing.bindings.h")]
            public struct CPUTextureStackRequestParameters
            {
                ///<exclude />
                public int level;
                ///<exclude />
                public int x;
                ///<exclude />
                public int y;
                ///<exclude />
                public int width;
                ///<exclude />
                public int height;
                ///<exclude />
                public int numLayers;

                CPUTextureStackRequestLayerParameters layer0;
                CPUTextureStackRequestLayerParameters layer1;
                CPUTextureStackRequestLayerParameters layer2;
                CPUTextureStackRequestLayerParameters layer3;
                ///<exclude />
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
            internal enum ProceduralTextureStackRequestStatus // KEEP IN SYNC WITH IVirtualTexturingManager.h
            {
                StatusFree = 0xFFFF,// Anything smaller than this is considered a free slot
                StatusRequested,    // Requested but user C# code is not processing this yet
                StatusProcessing,   // Returned to C#
                StatusComplete,     // C# indicates we're done
                StatusDropped,      // C# indicates we no longer want to do this one
            }

            ///<exclude />
            public enum RequestStatus
            {
                ///<exclude />
                Dropped = ProceduralTextureStackRequestStatus.StatusDropped,
                ///<exclude />
                Generated = ProceduralTextureStackRequestStatus.StatusComplete
            }

            ///<exclude />
            public class TextureStackBase<T> : IDisposable
                where T : struct
            {
                ///<exclude />
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
                ///<exclude />
                public readonly static int borderSize = 8;

                ///<exclude />
                public bool IsValid()
                {
                    return handle != 0;
                }

                string name;
                CreationParameters creationParams;

                ///<exclude />
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

                ///<exclude />
                public void Dispose()
                {
                    if (IsValid())
                    {
                        Binding.Destroy(handle);
                        handle = 0;
                    }
                }

                ///<exclude />
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

                ///<exclude />
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

                ///<exclude />
                public void BindGlobally()
                {
                    if (IsValid() == false)
                    {
                        throw new InvalidOperationException($"Invalid ProceduralTextureStack {name}");
                    }
                    Binding.BindGlobally(handle, name);
                }

                ///<exclude />
                public const int AllMips = int.MaxValue;

                ///<exclude />
                public void RequestRegion(Rect r, int mipMap, int numMips)
                {
                    if (IsValid() == false)
                    {
                        throw new InvalidOperationException($"Invalid ProceduralTextureStack {name}");
                    }
                    Binding.RequestRegion(handle, r, mipMap, numMips);
                }

                ///<exclude />
                public void InvalidateRegion(Rect r, int mipMap, int numMips)
                {
                    if (IsValid() == false)
                    {
                        throw new InvalidOperationException($"Invalid ProceduralTextureStack {name}");
                    }
                    Binding.InvalidateRegion(handle, r, mipMap, numMips);
                }

                ///<exclude />
                public void EvictRegion(Rect r, int mipMap, int numMips)
                {
                    if (IsValid() == false)
                    {
                        throw new InvalidOperationException($"Invalid ProceduralTextureStack {name}");
                    }
                    Binding.EvictRegion(handle, r, mipMap, numMips);
                }
            }

            ///<exclude />
            public sealed class GPUTextureStack : TextureStackBase<GPUTextureStackRequestParameters>
            {
                ///<exclude />
                public GPUTextureStack(string _name, CreationParameters creationParams)
                    : base(_name, creationParams, true)
                {}
            }

            ///<exclude />
            public sealed class CPUTextureStack : TextureStackBase<CPUTextureStackRequestParameters>
            {
                ///<exclude />
                public CPUTextureStack(string _name, CreationParameters creationParams)
                    : base(_name, creationParams, false)
                {}
            }
        }
    }
}
