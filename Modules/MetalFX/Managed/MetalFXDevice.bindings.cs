// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.Bindings;
using UnityEngine.Rendering;

namespace UnityEngine.Rendering.MetalFX
{
    // Layout MUST stay in sync with the matching struct in GfxPluginMetalFX.h.
    [StructLayout(LayoutKind.Sequential)]
    internal struct MetalFXSpatialDispatchData
    {
        public IntPtr context;      // native context handle
        public IntPtr source;
        public IntPtr destination;
        public int inputContentWidth;   // per-frame render size (= input alloc when DRS is off)
        public int inputContentHeight;
    }

    // Layout MUST stay in sync with the matching struct in GfxPluginMetalFX.h.
    [StructLayout(LayoutKind.Sequential)]
    internal struct MetalFXTemporalDispatchData
    {
        public IntPtr context;      // native context handle
        public IntPtr color;
        public IntPtr depth;
        public IntPtr motion;
        public IntPtr destination;
        public float jitterX;
        public float jitterY;
        public float motionVectorScaleX;
        public float motionVectorScaleY;
        public int reset;
        public int depthReversed;
        public int inputContentWidth;   // per-frame render size (= input alloc when DRS is off)
        public int inputContentHeight;
    }

    [NativeHeader("PlatformDependent/CommonApple/Modules/MetalFX/GfxPluginMetalFX.h")]
    [NativeConditional("PLATFORM_APPLE")]
    internal static class MetalFXDevice
    {
        // Event offsets must match MetalFXEvent in GfxPluginMetalFX.h.
        public const int k_EventDispatchSpatial = 0;
        public const int k_EventDispatchTemporal = 1;
        public const int k_EventDestroyContext = 2;

        [NoAutoStaticsCleanup]
        static IntPtr s_RenderEventFunc = IntPtr.Zero;
        [NoAutoStaticsCleanup]
        static int s_BaseEventId;
        [NoAutoStaticsCleanup]
        static bool s_Initialized;

        static void EnsureInitialized()
        {
            if (s_Initialized)
                return;
            s_RenderEventFunc = MetalFX_GetRenderEventFunc();
            s_BaseEventId = MetalFX_GetBaseEventId();
            s_Initialized = true;
        }

        public static bool IsSpatialSupported() => MetalFX_IsSpatialSupported();
        public static bool IsTemporalSupported() => MetalFX_IsTemporalSupported();
        public static float GetTemporalMaxScale() => MetalFX_GetTemporalMaxScale();
        public static float GetTemporalMinScale() => MetalFX_GetTemporalMinScale();

        public static IntPtr CreateSpatialContext(IntPtr source, IntPtr destination, int inputWidth, int inputHeight, int outputWidth, int outputHeight, bool hdr)
            => MetalFX_CreateSpatialContext(source, destination, inputWidth, inputHeight, outputWidth, outputHeight, hdr ? 1 : 0);

        public static IntPtr CreateTemporalContext(IntPtr color, IntPtr depth, IntPtr motion, IntPtr output, bool dynamicResolution, int inputWidth, int inputHeight, int outputWidth, int outputHeight)
            => MetalFX_CreateTemporalContext(color, depth, motion, output, dynamicResolution ? 1 : 0, inputWidth, inputHeight, outputWidth, outputHeight);

        /// <summary>
        /// Schedules the release of a scaler context on the render thread. Routing it through <paramref name="cmd"/> orders it
        /// after any already-queued <see cref="Dispatch"/> that still references them.
        /// </summary>
        public static void DestroyContext(CommandBuffer cmd, IntPtr context)
        {
            EnsureInitialized();
            if (s_RenderEventFunc == IntPtr.Zero || context == IntPtr.Zero)
                return;

            cmd.IssuePluginEventAndData(s_RenderEventFunc, s_BaseEventId + k_EventDestroyContext, context);
        }

        /// <summary>
        /// Schedules a dispatch on the render thread. <paramref name="src"/> is this frame's dispatch struct
        /// </summary>
        public static void Dispatch(CommandBuffer cmd, int eventOffset, in MetalFXSpatialDispatchData src)
        {
            EnsureInitialized();
            if (s_RenderEventFunc == IntPtr.Zero)
                return;

            // Dispatch data is copied into a context-owned pool slot,
            // so a later frame can't overwrite it before the render thread reads it.
            IntPtr slot = MetalFX_PrepareSpatialDispatch(in src);
            if (slot == IntPtr.Zero)
                return;

            cmd.IssuePluginEventAndData(s_RenderEventFunc, s_BaseEventId + eventOffset, slot);
        }

        /// <inheritdoc cref="Dispatch(CommandBuffer, int, in MetalFXSpatialDispatchData)"/>
        public static void Dispatch(CommandBuffer cmd, int eventOffset, in MetalFXTemporalDispatchData src)
        {
            EnsureInitialized();
            if (s_RenderEventFunc == IntPtr.Zero)
                return;

            IntPtr slot = MetalFX_PrepareTemporalDispatch(in src);
            if (slot == IntPtr.Zero)
                return;

            cmd.IssuePluginEventAndData(s_RenderEventFunc, s_BaseEventId + eventOffset, slot);
        }

        [FreeFunction("MetalFX_IsSpatialSupported")] static extern bool MetalFX_IsSpatialSupported();
        [FreeFunction("MetalFX_IsTemporalSupported")] static extern bool MetalFX_IsTemporalSupported();
        [FreeFunction("MetalFX_GetTemporalMaxScale")] static extern float MetalFX_GetTemporalMaxScale();
        [FreeFunction("MetalFX_GetTemporalMinScale")] static extern float MetalFX_GetTemporalMinScale();
        [FreeFunction("MetalFX_CreateSpatialContext")] static extern IntPtr MetalFX_CreateSpatialContext(IntPtr source, IntPtr destination, int inputWidth, int inputHeight, int outputWidth, int outputHeight, int hdr);
        [FreeFunction("MetalFX_PrepareSpatialDispatch")] static extern IntPtr MetalFX_PrepareSpatialDispatch(in MetalFXSpatialDispatchData src);
        [FreeFunction("MetalFX_PrepareTemporalDispatch")] static extern IntPtr MetalFX_PrepareTemporalDispatch(in MetalFXTemporalDispatchData src);
        [FreeFunction("MetalFX_CreateTemporalContext")] static extern IntPtr MetalFX_CreateTemporalContext(IntPtr color, IntPtr depth, IntPtr motion, IntPtr output, int dynamicResolution, int inputWidth, int inputHeight, int outputWidth, int outputHeight);
        [FreeFunction("MetalFX_GetBaseEventId")] static extern int MetalFX_GetBaseEventId();
        [FreeFunction("MetalFX_GetRenderEventFunc")] static extern IntPtr MetalFX_GetRenderEventFunc();
    }
}
