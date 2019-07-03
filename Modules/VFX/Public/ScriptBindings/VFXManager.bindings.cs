// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Rendering;
using UnityEngine.Scripting;
using UnityEngine.VFX;

//Temporary
//Adds ProcessCamera in UnityEngine.Experimental.VFX namespace for HDRP
//Remove this code when a new com.unity.render-pipelines.high-definition built-in package has been provided
using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("Unity.RenderPipelines.HighDefinition.Runtime")]
[assembly: InternalsVisibleTo("Unity.RenderPipelines.HighDefinition.Runtime-testable")]
namespace UnityEngine.Experimental.VFX
{
    internal static class VFXManager
    {
        public static void ProcessCamera(Camera cam)
        {
            UnityEngine.VFX.VFXManager.ProcessCamera(cam);
        }
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

        // Hooks for SRP
        extern public static void ProcessCamera(Camera cam);
        extern public static VFXCameraBufferTypes IsCameraBufferNeeded(Camera cam);
        extern public static void SetCameraBuffer(Camera cam, VFXCameraBufferTypes type, Texture buffer, int x, int y, int width, int height);
    }
}
