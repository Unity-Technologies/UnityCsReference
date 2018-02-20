// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Rendering;

namespace UnityEngine.XR
{
    // Offsets must match UnityVRBlitMode in IUnityVR.h
    public enum GameViewRenderMode
    {
        LeftEye = 1,
        RightEye = 2,
        BothEyes = 3,
        OcclusionMesh = 4,
    }

    [NativeHeader("Runtime/VR/VRDevice.h")]
    [NativeHeader("Runtime/VR/PluginInterface/Headers/IUnityVR.h")]
    public static partial class XRSettings
    {
        extern public static GameViewRenderMode gameViewRenderMode { get; set; }
    }
}

namespace UnityEngine.Experimental.XR
{
}
