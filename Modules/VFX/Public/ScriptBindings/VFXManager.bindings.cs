// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Rendering;
using UnityEngine.Scripting;
using UnityEngine.Experimental.VFX;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Experimental.VFX
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

        extern internal static void ProcessCamera(Camera cam); // Hook for SRP

        [RequiredByNativeCode]
        internal static void RegisterPerCameraCallback()
        {
            RenderPipeline.beginCameraRendering += ProcessCamera;
        }

        [RequiredByNativeCode]
        internal static void UnregisterPerCameraCallback()
        {
            RenderPipeline.beginCameraRendering -= ProcessCamera;
        }
    }
}
