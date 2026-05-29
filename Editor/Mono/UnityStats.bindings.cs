// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using Unity.Properties;
using UnityEngine.Bindings;

namespace UnityEditor
{
    [StructLayout(LayoutKind.Sequential)]
    struct KeyStats
    {
        public int drawCallsCount;
        public int instancesCount;
        public int srpBatcherDrawCalls;
        public int srpBatcherInstances;
        public int hybridBatcherDrawCalls;
        public int hybridBatcherInstances;
        public int standardInstancedDrawCalls;
        public int standardInstancedInstances;
        
        public int standardIndirectDrawCalls;
        public int hybridIndirectDrawCalls;
        public int nullGeometryDrawCalls;
        public int nullGeometryIndirectDrawCalls;

        public UInt64 trisCount;
        public UInt64 vertsCount;
        public UInt64 setPassCallsCount;
    }
    [StructLayout(LayoutKind.Sequential)]
    struct StateStats
    {
        public UInt64 vboUploadBytes;
        public UInt64 ibUploadBytes;
        public int vboUploads;
        public int ibUploads;
        public int renderTexture;
    }

    [NativeType(CodegenOptions.Force)]
    struct ClientStats
    {
        public int shadowCasters;
    }

    [NativeType(CodegenOptions.Force)]
    struct BatchStats
    {
        public int batches;
        public int dynamicBatches;
        public int dynamicBatchedCalls;
        public UInt64 dynamicBatchedTris;
        public UInt64 dynamicBatchedVerts;
        public int staticBatches;
        public int staticBatchedCalls;
        public UInt64 staticBatchedTris;
        public UInt64 staticBatchedVerts;
        public int instancedBatches;
        public int instancedBatchedCalls;
        public UInt64 instancedTris;
        public UInt64 instancedVerts;

        public int usedTextureCount;
        public Int64 usedTextureBytes;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct ScreenStats
    {
        public int screenWidth, screenHeight;
        public int screenFrontBPP, screenBackBPP, screenDepthBPP;
        public UInt64 screenBytes; // memory for backbuffer + frontbuffer
        public Int64 renderTextureBytes;
    }

    //*undocumented*
    // Undocumented, but left public. Some people want to figure out draw calls from editor scripts to do some performance checking
    // optimizations.
    [StaticAccessor("GfxDeviceStats::Get().GetBlittableStats()", StaticAccessorType.Dot)]
    [NativeHeader("Runtime/GfxDevice/GfxDeviceStats.h")]
    [NativeHeader("Runtime/GfxDevice/GfxDevice.h")]
    [NativeHeader("Modules/Audio/Public/AudioManager.h")]
    [NativeHeader("Runtime/Graphics/RenderTexture.h")]
    [NativeHeader("Runtime/Profiler/MemoryProfilerStats.h")]
    public sealed class UnityStats
    {
        internal extern static KeyStats keyStats { get; }
        internal extern static BatchStats batchStats { get; }
        internal extern static ScreenStats screenStats { get; }
        internal extern static ClientStats clientStats { get; }
        internal extern static StateStats stateChanges { get; }

        // The TOTAL number of draw calls made this frame (sum of all categories).
        public static int drawCalls =>
            keyStats.drawCallsCount +
            keyStats.srpBatcherDrawCalls +
            keyStats.hybridBatcherDrawCalls +
            keyStats.standardInstancedDrawCalls +
            keyStats.standardIndirectDrawCalls +
            keyStats.hybridIndirectDrawCalls +
            keyStats.nullGeometryDrawCalls +
            keyStats.nullGeometryIndirectDrawCalls;

        // The TOTAL number of instances in this frame (sum of all categories).
        public static int instances =>
            keyStats.instancesCount +
            keyStats.srpBatcherInstances +
            keyStats.hybridBatcherInstances +
            keyStats.standardInstancedInstances;

        // The TOTAL number of indirect draw calls made this frame.
        public static int totalIndirectDrawCalls =>
            keyStats.standardIndirectDrawCalls +
            keyStats.hybridIndirectDrawCalls +
            keyStats.nullGeometryIndirectDrawCalls;

        // The number of SRP Batcher draw calls this frame.
        public static int srpBatcherDrawCalls => keyStats.srpBatcherDrawCalls;

        // The number of SRP Batcher instances this frame.
        public static int srpBatcherInstances => keyStats.srpBatcherInstances;

        // The number of Hybrid Batcher draw calls this frame.
        public static int hybridBatcherDrawCalls => keyStats.hybridBatcherDrawCalls;

        // The number of Hybrid Batcher instances this frame.
        public static int hybridBatcherInstances => keyStats.hybridBatcherInstances;

        // The number of Non-SRP Batcher compatible standard draw calls this frame.
        public static int standardDrawCalls => keyStats.drawCallsCount;

        // The number of Non-SRP Batcher compatible standard instances this frame.
        public static int standardInstances => keyStats.instancesCount;

        // The number of standard instanced draw calls this frame.
        public static int standardInstancedDrawCalls => keyStats.standardInstancedDrawCalls;

        // The number of standard instanced instances this frame.
        public static int standardInstancedInstances => keyStats.standardInstancedInstances;

        // The number of standard indirect draw calls this frame.
        public static int standardIndirectDrawCalls => keyStats.standardIndirectDrawCalls;

        // The number of hybrid indirect draw calls this frame.
        public static int hybridIndirectDrawCalls => keyStats.hybridIndirectDrawCalls;

        // The number of null geometry draw calls this frame.
        public static int nullGeometryDrawCalls => keyStats.nullGeometryDrawCalls;

        // The number of null geometry indirect draw calls this frame.
        public static int nullGeometryIndirectDrawCalls => keyStats.nullGeometryIndirectDrawCalls;

        // The number of draw calls that got dynamically batched this frame.
        public static int dynamicBatchedDrawCalls => batchStats.dynamicBatchedCalls;

        // The number of draw calls that got statically batched this frame.
        public static int staticBatchedDrawCalls => batchStats.staticBatchedCalls;

        // The number of draw calls that got instanced this frame.
        public static int instancedBatchedDrawCalls => batchStats.instancedBatchedCalls;

        // The number of dynamic batches rendered this frame.
        public static int dynamicBatches => batchStats.dynamicBatches;

        // The number of static batches rendered this frame.
        public static int staticBatches => batchStats.staticBatches;

        // The number of instanced batches rendered this frame.
        public static int instancedBatches => batchStats.instancedBatches;

        // The number of calls to SetPass.
        public static int setPassCalls => (int)keyStats.setPassCallsCount;

        public static int triangles => (int)keyStats.trisCount;
        public static int vertices => (int)keyStats.vertsCount;

        // Temporary API for game view stats window, so it can display proper numbers for >2B cases. Profiling Counters API
        // should happen in the future to solve this properly.
        internal static long trianglesLong => (long)keyStats.trisCount;
        internal static long verticesLong => (long)keyStats.vertsCount;

        // The number of shadow casters rendered in this frame.
        public static int shadowCasters => clientStats.shadowCasters;

        // The number of render texture changes made this frame.
        public static int renderTextureChanges => stateChanges.renderTexture;

        [Obsolete("UnityStats.frametime is deprecated. Use FrameTimingManager.GetLatestTimings to get CPU frame times instead.", false)]
        [NativeName("MainThreadFrameTime")] public extern static float frameTime { get; }

        [Obsolete("UnityStats.renderTime is deprecated. Use FrameTimingManager.GetLatestTimings to get CPU frame times instead.", false)]
        [NativeName("RenderThreadFrameTime")] public extern static float renderTime { get; }

        public extern static float audioLevel { [FreeFunction("GetAudioManager().GetMasterGroupLevel")] get; }
        public extern static float audioClippingAmount { [FreeFunction("GetAudioManager().GetMasterGroupClippingAmount")] get; }
        public extern static float audioDSPLoad { [FreeFunction("GetAudioManager().GetDSPLoad")] get; }
        public extern static float audioStreamLoad { [FreeFunction("GetAudioManager().GetStreamLoad")] get; }
        internal extern static bool audioOutputSuspended { [FreeFunction("GetAudioManager().IsOutputSuspended")] get; }

        public extern static int renderTextureCount { [FreeFunction("GetMemoryProfilerStats().GetRenderTextureCount")] get; }
        public extern static int renderTextureBytes { [FreeFunction("GetMemoryProfilerStats().GetRenderTextureBytes")] get; }

        public static int usedTextureMemorySize
        {
            get
            {
                return (int)batchStats.usedTextureBytes;
            }
        }

        public static int usedTextureCount
        {
            get
            {
                return batchStats.usedTextureCount;
            }
        }

        public static string screenRes
        {
            get
            {
                var stats = screenStats;
                return $"{screenStats.screenWidth}x{screenStats.screenHeight}";
            }
        }

        public static int screenBytes => (int)screenStats.screenBytes;

        public extern static int vboTotal { [FreeFunction("GetGfxDevice().GetTotalBufferCount")] get; }
        public extern static int vboTotalBytes { [FreeFunction("GetGfxDevice().GetTotalBufferBytes")] get; }

        public static int vboUploads => stateChanges.vboUploads;
        public static int vboUploadBytes => (int)stateChanges.vboUploadBytes;
        public static int ibUploads => stateChanges.ibUploads;
        public static int ibUploadBytes => (int)stateChanges.ibUploadBytes;

        public extern static int visibleSkinnedMeshes
        {
            [NativeConditional("ENABLE_PROFILER")]
            [FreeFunction("SkinnedMeshRenderer::GetVisibleSkinnedMeshRendererCount")]
            [NativeHeader("Runtime/Graphics/Mesh/SkinnedMeshRenderer.h")]
            get;
        }

        public extern static int updatedOffscreenMeshes
        {
            [NativeConditional("ENABLE_PROFILER")]
            [FreeFunction("SkinnedMeshRenderer::GetOffscreenUpdatedRendererCount")]
            [NativeHeader("Runtime/Graphics/Mesh/SkinnedMeshRenderer.h")]
            get;
        }

        public extern static int animationComponentsPlaying
        {
            [NativeConditional("ENABLE_PROFILER")]
            [FreeFunction("GetAnimationManager().GetAnimationComponentsPlayingCount")]
            [NativeHeader("Modules/Animation/AnimationManager.h")]
            get;
        }

        public extern static int animatorComponentsPlaying
        {
            [NativeConditional("ENABLE_PROFILER")]
            [FreeFunction("GetAnimatorStatistics().GetAnimatorComponentsPlayingCount")]
            [NativeHeader("Modules/Animation/AnimatorStatistics.h")]
            get;
        }
    }
}
