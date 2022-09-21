// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using UnityEngine.Rendering;
using UnityEngine.Scripting;
using System;
using System.Collections.Generic;

//Keep this namespace to be compatible with visual effect graph package 7.0.1
//There was an unexpected useless "using UnityEngine.Experimental.VFX;" in VFXMotionVector.cs
namespace UnityEngine.Experimental.VFX
{
    internal static class VFXManager
    {
    }
}

namespace UnityEngine.VFX
{
    [RequiredByNativeCode]
    public struct VFXCameraXRSettings
    {
        public uint viewTotal;
        public uint viewCount;
        public uint viewOffset;
    }

    [RequiredByNativeCode]
    public struct VFXBatchedEffectInfo
    {
        public VisualEffectAsset vfxAsset;
        public uint activeBatchCount;
        public uint inactiveBatchCount;
        public uint activeInstanceCount;
        public uint unbatchedInstanceCount;
        public uint totalInstanceCapacity;
        public uint maxInstancePerBatchCapacity;
        public ulong totalGPUSizeInBytes;
        public ulong totalCPUSizeInBytes;
    }

    [RequiredByNativeCode]
    internal struct VFXBatchInfo
    {
        public uint capacity;
        public uint activeInstanceCount;
    }

    [RequiredByNativeCode]
    [NativeHeader("Modules/VFX/Public/VFXManager.h")]
    [NativeHeader("Modules/VFX/Public/ScriptBindings/VFXManagerBindings.h")]
    [StaticAccessor("GetVFXManager()", StaticAccessorType.Dot)]
    public static class VFXManager
    {
        extern public static VisualEffect[] GetComponents();
        extern internal static ScriptableObject runtimeResources { get; }

        extern public static float fixedTimeStep { get; set; }
        extern public static float maxDeltaTime { get; set; }

        extern internal static float maxScrubTime { get; set; }
        extern internal static string renderPipeSettingsPath { get; }

        extern internal static uint batchEmptyLifetime { get; set; }

        extern internal static void ResyncMaterials([NotNull("NullExceptionObject")] VisualEffectAsset asset);
        extern internal static bool renderInSceneView { get; set; }
        internal static bool activateVFX { get; set; }

        extern internal static void CleanupEmptyBatches(bool force = false);

        public static void FlushEmptyBatches()
        {
            CleanupEmptyBatches(true);
        }

        extern public static VFXBatchedEffectInfo GetBatchedEffectInfo([NotNull("NullExceptionObject")] VisualEffectAsset vfx);

        [FreeFunction(Name = "VFXManagerBindings::GetBatchedEffectInfos", HasExplicitThis = false)]
        extern public static void GetBatchedEffectInfos([NotNull("NullExceptionObject")] List<VFXBatchedEffectInfo> infos);

        extern internal static VFXBatchInfo GetBatchInfo(VisualEffectAsset vfx, uint batchIndex);

        private static readonly VFXCameraXRSettings kDefaultCameraXRSettings = new VFXCameraXRSettings { viewTotal = 1, viewCount = 1, viewOffset = 0 };

        [Obsolete("Use explicit PrepareCamera and ProcessCameraCommand instead")]
        public static void ProcessCamera(Camera cam)
        {
            PrepareCamera(cam, kDefaultCameraXRSettings);
            Internal_ProcessCameraCommand(cam, null, kDefaultCameraXRSettings, IntPtr.Zero);
        }

        public static void PrepareCamera(Camera cam)
        {
            PrepareCamera(cam, kDefaultCameraXRSettings);
        }

        extern public static void PrepareCamera([NotNull("NullExceptionObject")] Camera cam, VFXCameraXRSettings camXRSettings);

        [Obsolete("Use ProcessCameraCommand with CullingResults to allow culling of VFX per camera")]
        public static void ProcessCameraCommand(Camera cam, CommandBuffer cmd)
        {
            Internal_ProcessCameraCommand(cam, cmd, kDefaultCameraXRSettings, IntPtr.Zero);
        }

        [Obsolete("Use ProcessCameraCommand with CullingResults to allow culling of VFX per camera")]
        public static void ProcessCameraCommand(Camera cam, CommandBuffer cmd, VFXCameraXRSettings camXRSettings)
        {
            Internal_ProcessCameraCommand(cam, cmd, camXRSettings, IntPtr.Zero);
        }

        public static void ProcessCameraCommand(Camera cam, CommandBuffer cmd, VFXCameraXRSettings camXRSettings, Rendering.CullingResults results)
        {
            Internal_ProcessCameraCommand(cam, cmd, camXRSettings, results.ptr);
        }

        extern private static void Internal_ProcessCameraCommand([NotNull("NullExceptionObject")] Camera cam, CommandBuffer cmd, VFXCameraXRSettings camXRSettings, IntPtr cullResults);
        extern public static VFXCameraBufferTypes IsCameraBufferNeeded([NotNull("NullExceptionObject")] Camera cam);
        extern public static void SetCameraBuffer([NotNull("NullExceptionObject")] Camera cam, VFXCameraBufferTypes type, Texture buffer, int x, int y, int width, int height);

        extern public static void SetRayTracingEnabled(bool enabled);
        extern public static void RequestRtasAabbConstruction();
    }
}
