// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using UnityEngine.Rendering;
using UnityEngine.Scripting;

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
    [NativeHeader("Modules/VFX/Public/VFXManager.h")]
    [StaticAccessor("GetVFXManager()", StaticAccessorType.Dot)]
    public static class VFXManager
    {
        extern public static VisualEffect[] GetComponents();

        extern public static float fixedTimeStep { get; set; }
        extern public static float maxDeltaTime { get; set; }

        extern internal static string renderPipeSettingsPath { get; }

        extern internal static bool renderInSceneView { get; set; }
        internal static bool activateVFX { get; set; }

        public static void ProcessCamera(Camera cam)
        {
            PrepareCamera(cam);
            ProcessCameraCommand(cam, null);
        }

        extern public static void PrepareCamera(Camera cam);
        extern public static void ProcessCameraCommand(Camera cam, CommandBuffer cmd);
        extern public static VFXCameraBufferTypes IsCameraBufferNeeded(Camera cam);
        extern public static void SetCameraBuffer(Camera cam, VFXCameraBufferTypes type, Texture buffer, int x, int y, int width, int height);
    }
}
