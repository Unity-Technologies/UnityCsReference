// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;

namespace UnityEditor
{
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
    struct DrawStats
    {
        public int batches;
        public int calls;
        public UInt64 tris, trisSent;
        public UInt64 verts;
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
        public UInt64 setPassCalls;

        public int usedTextureCount;
        public Int64 usedTextureBytes;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct MemoryStats
    {
        public int screenWidth, screenHeight;
        public int screenFrontBPP, screenBackBPP, screenDepthBPP;
        public UInt64 screenBytes; // memory for backbuffer + frontbuffer
        public Int64 renderTextureBytes;
    }

    //*undocumented*
    // Undocumented, but left public. Some people want to figure out draw calls from editor scripts to do some performance checking
    // optimizations.
    [StaticAccessor("GetGfxDevice().GetFrameStats()", StaticAccessorType.Dot)]
    [NativeHeader("Runtime/GfxDevice/GfxDevice.h")]
    [NativeHeader("Modules/Audio/Public/AudioManager.h")]
    [NativeHeader("Runtime/Graphics/RenderTexture.h")]
    public sealed class UnityStats
    {
        internal extern static DrawStats drawStats { get; }
        internal extern static MemoryStats memoryStats { get; }
        internal extern static ClientStats clientStats { get; }
        internal extern static StateStats stateChanges { get; }


        // The number of batches rendered this frame.
        public static int batches => drawStats.batches;

        // The number of draw calls made this frame.
        public static int drawCalls => drawStats.calls;

        // The number of draw calls that got dynamically batched this frame.
        public static int dynamicBatchedDrawCalls => drawStats.dynamicBatchedCalls;

        // The number of draw calls that got statically batched this frame.
        public static int staticBatchedDrawCalls => drawStats.staticBatchedCalls;

        // The number of draw calls that got instanced this frame.
        public static int instancedBatchedDrawCalls => drawStats.instancedBatchedCalls;

        // The number of dynamic batches rendered this frame.
        public static int dynamicBatches => drawStats.dynamicBatches;

        // The number of static batches rendered this frame.
        public static int staticBatches => drawStats.staticBatches;

        // The number of instanced batches rendered this frame.
        public static int instancedBatches => drawStats.instancedBatches;

        // The number of calls to SetPass.
        public static int setPassCalls => (int)drawStats.setPassCalls;

        public static int triangles => (int)drawStats.tris;
        public static int vertices => (int)drawStats.verts;

        // Temporary API for game view stats window, so it can display proper numbers for >2B cases. Profiling Counters API
        // should happen in the future to solve this properly.
        internal static long trianglesLong => (long)drawStats.tris;
        internal static long verticesLong => (long)drawStats.verts;

        // The number of shadow casters rendered in this frame.
        public static int shadowCasters => clientStats.shadowCasters;

        // The number of render texture changes made this frame.
        public static int renderTextureChanges => stateChanges.renderTexture;

        [NativeName("ClientFrameTime")] public extern static float frameTime { get; }
        [NativeName("RenderFrameTime")] public extern static float renderTime { get; }

        public extern static float audioLevel { [FreeFunction("GetAudioManager().GetMasterGroupLevel")] get; }
        public extern static float audioClippingAmount { [FreeFunction("GetAudioManager().GetMasterGroupClippingAmount")] get; }
        public extern static float audioDSPLoad { [FreeFunction("GetAudioManager().GetDSPLoad")] get; }
        public extern static float audioStreamLoad { [FreeFunction("GetAudioManager().GetStreamLoad")] get; }

        public extern static int renderTextureCount { [FreeFunction("RenderTexture::GetCreatedRenderTextureCount")] get; }
        public extern static int renderTextureBytes { [FreeFunction("RenderTexture::GetCreatedRenderTextureBytes")] get; }

        public static int usedTextureMemorySize
        {
            get
            {
                return 0;
            }
        }

        public static int usedTextureCount
        {
            get
            {
                return 0;
            }
        }

        public static string screenRes
        {
            get
            {
                var stats = memoryStats;
                return $"{memoryStats.screenWidth}x{memoryStats.screenHeight}";
            }
        }

        public static int screenBytes => (int)memoryStats.screenBytes;

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
