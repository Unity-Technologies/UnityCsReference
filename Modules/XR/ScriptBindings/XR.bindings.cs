// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Rendering;
using System.Collections.Generic;
using UnityEngine.Scripting;

namespace UnityEngine.XR
{
    // Offsets must match UnityVRBlitMode in IUnityVR.h
    public enum GameViewRenderMode
    {
        None = 0,
        LeftEye = 1,
        RightEye = 2,
        BothEyes = 3,
        OcclusionMesh = 4,
        MotionVectors = 5,
    }

    [NativeHeader("Modules/XR/ScriptBindings/XR.bindings.h")]
    [NativeHeader("Runtime/Interfaces/IVRDevice.h")]
    [NativeHeader("Runtime/GfxDevice/GfxDeviceTypes.h")]
    [NativeConditional("ENABLE_VR")]
    public static partial class XRSettings
    {
        extern public static bool enabled
        {
            [StaticAccessor("GetIVRDeviceScripting()", StaticAccessorType.ArrowWithDefaultReturnIfNull)]
            get;

            [Obsolete("XRSettings.enabled{set;} is deprecated and should no longer be used. Instead, call Start() and Stop() on an XRDisplaySubsystem instance.", true)]
            set;
        }

        [StaticAccessor("GetIVRDeviceScripting()", StaticAccessorType.ArrowWithDefaultReturnIfNull)]
        extern public static GameViewRenderMode gameViewRenderMode { get; set; }

        [NativeName("Active")]
        [StaticAccessor("GetIVRDeviceScripting()", StaticAccessorType.ArrowWithDefaultReturnIfNull)]
        extern public static bool isDeviceActive { get; }

        [StaticAccessor("GetIVRDeviceScripting()", StaticAccessorType.ArrowWithDefaultReturnIfNull)]
        extern public static bool showDeviceView { get; set; }

        [NativeName("RenderScale")]
        [StaticAccessor("GetIVRDeviceScripting()", StaticAccessorType.ArrowWithDefaultReturnIfNull)]
        extern public static float eyeTextureResolutionScale { get; set; }

        [StaticAccessor("GetIVRDeviceScripting()", StaticAccessorType.ArrowWithDefaultReturnIfNull)]
        extern public static int eyeTextureWidth { get; }

        [StaticAccessor("GetIVRDeviceScripting()", StaticAccessorType.ArrowWithDefaultReturnIfNull)]
        extern public static int eyeTextureHeight { get; }

        [NativeName("IntermediateEyeTextureDesc")]
        [NativeConditional("ENABLE_VR", "RenderTextureDesc()")]
        [StaticAccessor("GetIVRDeviceScripting()", StaticAccessorType.ArrowWithDefaultReturnIfNull)]
        extern public static UnityEngine.RenderTextureDescriptor eyeTextureDesc { get; }

        [NativeName("DeviceEyeTextureDimension")]
        [StaticAccessor("GetIVRDeviceScripting()", StaticAccessorType.ArrowWithDefaultReturnIfNull)]
        extern public static TextureDimension deviceEyeTextureDimension { get; }

        public static float renderViewportScale
        {
            get
            {
                return renderViewportScaleInternal;
            }
            set
            {
                if (value < 0.0f || value > 1.0f)
                    throw new ArgumentOutOfRangeException("value", "Render viewport scale should be between 0 and 1.");
                renderViewportScaleInternal = value;
            }
        }

        [NativeName("RenderViewportScale")]
        [StaticAccessor("GetIVRDeviceScripting()", StaticAccessorType.ArrowWithDefaultReturnIfNull)]
        extern internal static float renderViewportScaleInternal { get; set; }

        [StaticAccessor("GetIVRDeviceScripting()", StaticAccessorType.ArrowWithDefaultReturnIfNull)]
        extern public static float appliedRenderViewportScale { get; }

        [StaticAccessor("GetIVRDeviceScripting()", StaticAccessorType.ArrowWithDefaultReturnIfNull)]
        extern public static float occlusionMaskScale { get; set; }

        [StaticAccessor("GetIVRDeviceScripting()", StaticAccessorType.ArrowWithDefaultReturnIfNull)]
        extern public static bool useOcclusionMesh { get; set; }

        [NativeName("DeviceName")]
        [StaticAccessor("GetIVRDeviceScripting()", StaticAccessorType.ArrowWithDefaultReturnIfNull)]
        extern public static string loadedDeviceName { get; }

        extern public static string[] supportedDevices { get; }

        public enum StereoRenderingMode
        {
            MultiPass = 0,
            SinglePass,
            SinglePassInstanced,
            SinglePassMultiview
        }

        [StaticAccessor("GetIVRDeviceScripting()", StaticAccessorType.ArrowWithDefaultReturnIfNull)]
        extern public static StereoRenderingMode stereoRenderingMode { get; }
    }
}
