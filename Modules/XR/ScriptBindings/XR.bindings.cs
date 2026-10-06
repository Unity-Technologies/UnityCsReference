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
    ///<summary>Enumeration of available modes for XR rendering in the Game view or in the main window on a host PC. XR rendering only occurs when the Unity Editor is in Play Mode.</summary>
    public enum GameViewRenderMode
    {
        ///<summary>Disables rendering of any new frames from the eyes in the Game view or in the main window on a host PC.</summary>
        None = 0,
        ///<summary>Renders the left eye of the XR device in the Game View window or in main window on a host PC.</summary>
        LeftEye = 1,
        ///<summary>Renders the right eye of the XR device in the Game View window or in main window on a host PC.</summary>
        RightEye = 2,
        ///<summary>Renders both eyes of the XR device side-by-side in the Game view or in the main window on a host PC.</summary>
        BothEyes = 3,
        ///<summary>Renders both eyes of the XR device, and the occlusion mesh, side-by-side in the Game view or in the main window on a host PC.</summary>
        OcclusionMesh = 4,
        ///<summary>Renders both eyes (motion vectors) of the XR device side-by-side in the Game view or in the main window on a host PC. Only works if the motion vector texture is in-use.</summary>
        MotionVectors = 5,
    }

    ///<summary>Global XR related settings.</summary>
    [NativeHeader("Modules/XR/ScriptBindings/XR.bindings.h")]
    [NativeHeader("Runtime/Interfaces/IVRDevice.h")]
    [NativeHeader("Runtime/GfxDevice/GfxDeviceTypes.h")]
    [NativeConditional("ENABLE_VR")]
    public static partial class XRSettings
    {
        ///<summary>Indicates whether Unity loaded and initialized the XR display subsystem.</summary>
        ///<remarks>This property is read-only. Its setter is obsolete and causes a compilation error. To start or stop XR at runtime, call Start or Stop on an <see cref="XRDisplaySubsystem" /> instance instead. In a project that uses the XR Plug-in Management package, manage the loader lifecycle through that package rather than calling these methods directly.
        ///
        ///This property reports the loaded state, not the running state. It stays true after you stop the display subsystem, and becomes false only when Unity destroys that subsystem. To test whether XR is currently running, use <see cref="XRSettings.isDeviceActive" />.</remarks>
        extern public static bool enabled
        {
            [StaticAccessor("GetIVRDeviceScripting()", StaticAccessorType.ArrowWithDefaultReturnIfNull)]
            get;

            [Obsolete("XRSettings.enabled{set;} is deprecated and should no longer be used. Instead, call Start() and Stop() on an XRDisplaySubsystem instance.", true)]
            set;
        }

        ///<summary>Sets the render mode for the XR device. The render mode controls how the view of the XR device renders in the Game view and in the main window on a host PC.</summary>
        ///<remarks>Refer to <see cref="XR.GameViewRenderMode" /> for a description of each available render mode.</remarks>
        [StaticAccessor("GetIVRDeviceScripting()", StaticAccessorType.ArrowWithDefaultReturnIfNull)]
        extern public static GameViewRenderMode gameViewRenderMode { get; set; }

        ///<summary>Read-only value that can be used to determine if the XR device is active.</summary>
        ///<remarks>When true, Unity accepts input from the device and attempts to render to the device's display or displays. Note that this returns true even if the device isn't currently rendering due to lack of user presence (refer to <see cref="CommonUsages.userPresence"/>). This can become false if a device is disconnected, wasn't initialized (refer to <see cref="SubsystemManager" /> to find a display subsystem descriptor and create the subsystem from it), or the XR display subsystem is stopped.
        ///
        ///XR output is automatically mirrored to the main display (if applicable).  This can be controlled with <see cref="XRSettings.showDeviceView" />.
        ///
        ///The main window is still controlled by <see cref="Screen" /> and related APIs.</remarks>
        [NativeName("Active")]
        [StaticAccessor("GetIVRDeviceScripting()", StaticAccessorType.ArrowWithDefaultReturnIfNull)]
        extern public static bool isDeviceActive { get; }

        ///<summary>This property has been deprecated. Use <see cref="XRSettings.gameViewRenderMode" /> instead.</summary>
        [StaticAccessor("GetIVRDeviceScripting()", StaticAccessorType.ArrowWithDefaultReturnIfNull)]
        extern public static bool showDeviceView { get; set; }

        ///<summary>Controls the actual size of eye textures as a multiplier of the device's default resolution.</summary>
        ///<remarks>A value of 1.0 will use the default eye texture resolution specified by the XR device. Values less than 1.0 will use lower resolution eye textures, which may improve performance at the expense of a less sharp image. Values greater than 1.0 will use higher resolution eye textures, resulting in a potentially sharper image at a cost to performance and increased memory usage.
        ///
        ///When this property is changed, eye textures are always reallocated, which can be an expensive operation. To dynamically change eye render resolution, consider using <see cref="XRSettings.renderViewportScale" /> instead.
        ///Refer to &lt;a href="../Manual/xr-graphics-resolution-scaling.html"&gt;Resolution control in XR projects&lt;/a&gt; to learn more about how to control resolution in your XR project for different render pipelines.</remarks>
        [NativeName("RenderScale")]
        [StaticAccessor("GetIVRDeviceScripting()", StaticAccessorType.ArrowWithDefaultReturnIfNull)]
        extern public static float eyeTextureResolutionScale { get; set; }

        ///<summary>The current width of an eye texture for the loaded device.</summary>
        ///<remarks>This value will be the product of the default eye texture size for the HMD and <see cref="XRSettings.eyeTextureResolutionScale" />. If XR isn't enabled this value will be zero.</remarks>
        [StaticAccessor("GetIVRDeviceScripting()", StaticAccessorType.ArrowWithDefaultReturnIfNull)]
        extern public static int eyeTextureWidth { get; }

        ///<summary>The current height of an eye texture for the loaded device.</summary>
        ///<remarks>This value will be the product of the default eye texture size for the HMD and <see cref="XRSettings.eyeTextureResolutionScale" />. If XR isn't enabled this value will be zero.</remarks>
        [StaticAccessor("GetIVRDeviceScripting()", StaticAccessorType.ArrowWithDefaultReturnIfNull)]
        extern public static int eyeTextureHeight { get; }

        ///<summary>Fetch the eye texture RenderTextureDescriptor from the active stereo device.</summary>
        ///<remarks>If XR is enabled, this returns a RenderTexureDescriptor configured by the stereo device.  This greatly simplifies the process of generating temporary render textures for stereo rendering.</remarks>
        ///<seealso cref="ScriptableRenderContext" />
        [NativeName("IntermediateEyeTextureDesc")]
        [NativeConditional("ENABLE_VR", "RenderTextureDesc()")]
        [StaticAccessor("GetIVRDeviceScripting()", StaticAccessorType.ArrowWithDefaultReturnIfNull)]
        extern public static UnityEngine.RenderTextureDescriptor eyeTextureDesc { get; }

        ///<summary>Fetch the device eye texture dimension from the active stereo device.</summary>
        ///<remarks>If XR is enabled, this returns a TextureDimension configured by the stereo device.  The device eye texture dimension represents the native stereo layout that the stereo device will use when submitting the back buffer to the stereo display.</remarks>
        [NativeName("DeviceEyeTextureDimension")]
        [StaticAccessor("GetIVRDeviceScripting()", StaticAccessorType.ArrowWithDefaultReturnIfNull)]
        extern public static TextureDimension deviceEyeTextureDimension { get; }

        ///<summary>Controls how much of the allocated eye texture should be used for rendering.</summary>
        ///<remarks>Valid range is 0.0 to 1.0. This value can be changed at runtime without reallocating eye textures. Therefore it is useful for dynamically adjusting eye render resolution. This value cannot be changed while cameras are being rendered. Attempts to change the value during rendering will be ignored and an error will be logged. Changes made during gameplay updates won't be applied until the next frame.
        ///
        ///Some XR platforms might not immediately use this value, or ignore it.
        ///To check the current applied viewport scale, use <see cref="XRSettings.appliedRenderViewportScale" />.
        ///
        ///This value does not support deferred rendering. Attempts to change the value in the presence of a camera using deferred rendering will be ignored and an error will be logged.</remarks>
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

        ///<summary>Fetches how much of the allocated display texture is applied by the active stereo device at the current frame.</summary>
        ///<remarks>The scale factor is fetched by the device and can change from frame to frame.
        ///If this value is observed during the gameplay logic it refers to the applied viewport scale of previous frame, if observed during rendering logic it is related to the current frame.
        ///<see cref="XRSettings.renderViewportScale" /> can influence the scale factor but the XR device can decide to ignore or change it.</remarks>
        [StaticAccessor("GetIVRDeviceScripting()", StaticAccessorType.ArrowWithDefaultReturnIfNull)]
        extern public static float appliedRenderViewportScale { get; }

        ///<summary>A scale applied to the standard occulsion mask for each platform.</summary>
        ///<remarks>Occlusion masks are used to increase performance by not rendering to pixels that cannot be seen through the XR headset. Some post-processing effects require data from pixels that cannot be seen through the XR headset's restricted field of vision (blur effects, for example) in order to avoid visual artifacts and other display errors. This property scales up the occlusion mask to ensure pixels outside of the XR headset's field of vision are rendered to, allowing post-processing effects to access the required texture data. Scaling up the occlusion mask will incur a performance penalty on the GPU due to the extra pixels being rendered.</remarks>
        [StaticAccessor("GetIVRDeviceScripting()", StaticAccessorType.ArrowWithDefaultReturnIfNull)]
        extern public static float occlusionMaskScale { get; set; }

        ///<summary>Specifies whether or not the occlusion mesh should be used when rendering. Enabled by default.</summary>
        ///<remarks>The occlusion mesh prevents GPU work from happening on portions of the eye texture that won't be visible in the HMD. Disabling this will lead to a decrease in GPU rendering performance. However, this may be needed to deal with certain features such as the grab pass.</remarks>
        [StaticAccessor("GetIVRDeviceScripting()", StaticAccessorType.ArrowWithDefaultReturnIfNull)]
        extern public static bool useOcclusionMesh { get; set; }

        ///<summary>Type of XR device that is currently loaded.</summary>
        ///<remarks>**Note:** Rendering to the device may not be happening even though it is loaded.  Refer to <see cref="XRSettings.enabled" />.
        ///
        ///In order to change the currently loaded device or reload the current device, use <see cref="XRSettings.LoadDeviceByName" />.</remarks>
        [NativeName("DeviceName")]
        [StaticAccessor("GetIVRDeviceScripting()", StaticAccessorType.ArrowWithDefaultReturnIfNull)]
        extern public static string loadedDeviceName { get; }

        ///<summary>Returns a list of supported XR devices that were included at build time.</summary>
        ///<remarks>You can use the <see cref="SubsystemManager" /> to load XR devices.
        ///                The list is populated based on the XR plug-in providers you enable in your project using the XR Plug-in Management system.
        ///                Refer to &lt;a href="../Manual/xr-configure-providers.html"&gt;Choose and configure XR provider plug-ins&lt;/a&gt; to learn more about how to enable XR plug-ins.</remarks>
        extern public static string[] supportedDevices { get; }

        ///<summary>Enum type signifying the different stereo rendering modes available.</summary>
        ///<remarks>To find out what the current stereo rendering mode used in your Unity project at runtime, use <see cref="XRSettings.stereoRenderingMode" />. This method returns a value of type StereoRenderingMode.</remarks>
        public enum StereoRenderingMode
        {
            ///<summary>This is the reference stereo rendering path for VR.</summary>
            ///<remarks>The scene graph is traversed twice, rendering one eye at a time.
            ///Scene culling and shadow map rendering can be shared between both eyes.
            ///If the device hardware does not support other rendering modes, Unity will fall back to this stereo rendering mode.</remarks>
            ///<seealso cref="XRSettings.stereoRenderingMode" />
            MultiPass = 0,
            ///<summary>This is a faster rendering path for VR than <see cref="XRSettings.StereoRenderingMode.MultiPass" />.</summary>
            ///<remarks>The speed boost is achieved by traversing the scene graph only once while issuing two draw calls for each render node.
            ///The main render target must be a double wide render target.
            ///Scene culling and shadow map rendering is shared between both eyes.</remarks>
            ///<seealso cref="XRSettings.stereoRenderingMode" />
            SinglePass,
            ///<summary>This is an optimized version of the <see cref="XRSettings.StereoRenderingMode.SinglePass" /> mode.</summary>
            ///<remarks>The scene graph is only traversed once and a single instanced draw call is issued for each render node, thus reducing the bandwidth required to render the scene.
            ///Scene culling and shadow map rendering is shared between both eyes.
            ///The main render target must be an array of render targets.
            ///Special hardware support is required for this mode to run.
            ///Refer to the [manual](xref:SinglePassStereoRendering) for how to get the most out of instanced rendering.</remarks>
            ///<seealso cref="XRSettings.stereoRenderingMode" />
            SinglePassInstanced,
            ///<summary>This is a OpenGL optimized version of the <see cref="XRSettings.StereoRenderingMode.SinglePassInstanced" /> mode.</summary>
            ///<remarks>The scene graph is only traversed once and a single instanced draw call is issued for each render node, thus reducing the bandwidth required to render the scene.
            ///Scene culling and shadow map rendering is shared between both eyes.
            ///The main render target must be an array of render targets.
            ///Special hardware support is required for this mode to run. Depending on their graphics capabilities, certain GPUs will run this stereo rendering mode and others will run <see cref="XRSettings.StereoRenderingMode.SinglePassInstanced" />. GPUs that support neither of those modes will fall back to <see cref="XRSettings.StereoRenderingMode.MultiPass" />.
            ///Refer to the [manual](xref:SinglePassStereoRendering) for how to get the most out of instanced rendering.</remarks>
            ///<seealso cref="XRSettings.stereoRenderingMode" />
            SinglePassMultiview
        }

        ///<summary>The stereo rendering mode that is currently in use.</summary>
        ///<remarks>The stereo rendering mode currently in use may be different from the user-specified stereo rendering mode if the underlying GPU or platform does not support the requested one.</remarks>
        [StaticAccessor("GetIVRDeviceScripting()", StaticAccessorType.ArrowWithDefaultReturnIfNull)]
        extern public static StereoRenderingMode stereoRenderingMode { get; }
    }
}
