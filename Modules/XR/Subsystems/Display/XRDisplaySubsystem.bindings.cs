// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Events;
using UnityEngine.Scripting;
using UnityEngine.Rendering;
using UsedByNativeCodeAttribute = UnityEngine.Scripting.UsedByNativeCodeAttribute;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace UnityEngine.XR
{
    ///<summary>An XRDisplaySubsystem controls rendering to a head tracked display.</summary>
    ///<remarks>An XRDisplaySubsystem instance can take up to three frames to fully initialize. You should wait three frames before accessing any of the methods or properties of this class.
    ///
    ///The following example uses a coroutine to wait for three frames before it sets the XR display subsystem's foveated rendering properties:</remarks>
    ///<example>
    ///  <code><![CDATA[using UnityEngine;
    ///using UnityEngine.XR;
    ///using System.Collections;
    ///using System.Collections.Generic;
    ///
    ///public class FoveationStarter : MonoBehaviour
    ///{
    ///    List<XRDisplaySubsystem> xrDisplays = new List<XRDisplaySubsystem>();
    ///
    ///    void Start()
    ///    {
    ///        StartCoroutine(WaitForXRDisplay());
    ///    }
    ///
    ///    IEnumerator WaitForXRDisplay()
    ///    {
    ///        yield return new WaitUntil(() => Time.frameCount >= 3);
    ///
    ///        SubsystemManager.GetSubsystems(xrDisplays);
    ///        if (xrDisplays.Count == 1)
    ///        {
    ///            xrDisplays[0].foveatedRenderingLevel = .5f; // half strength
    ///
    ///            Debug.Log($"Foveated rendering set to {xrDisplays[0].foveatedRenderingLevel}.");
    ///        }
    ///        else
    ///        {
    ///            Debug.LogWarning($"Couldn't find an XRDisplaySubsystem for foveated rendering.");
    ///        }
    ///    }
    ///}
    ///]]></code>
    ///</example>
    [NativeHeader("Modules/XR/Subsystems/Display/XRDisplaySubsystem.h")]
    [UsedByNativeCode]
    [NativeHeader("Modules/XR/XRPrefix.h")]
    [NativeConditional("ENABLE_XR")]
    public partial class XRDisplaySubsystem : IntegratedSubsystem<XRDisplaySubsystemDescriptor>
    {
        ///<summary>Event sent when XR display focus changes.</summary>
        ///<remarks>This event is sent on the main thread.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using UnityEngine.XR;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public XRDisplaySubsystem display;
        ///
        ///    void OnEnable()
        ///    {
        ///        display.displayFocusChanged += HandleFocusChanged;
        ///    }
        ///
        ///    void OnDisable()
        ///    {
        ///        display.displayFocusChanged -= HandleFocusChanged;
        ///    }
        ///
        ///    void HandleFocusChanged(bool focus)
        ///    {
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public event Action<bool> displayFocusChanged;

        [RequiredByNativeCode]
        private void InvokeDisplayFocusChanged(bool focus)
        {
            if (displayFocusChanged != null)
                displayFocusChanged.Invoke(focus);
        }

        ///<summary>Returns true when single pass stereo rendering is disabled and returns false if otherwise.</summary>
        ///<remarks>Set this property to true when your Project doesn't support single pass stereo rendering. Unity will render your Scene with multiple passes. Unity also disables single pass stereo rendering when it cannot detect supported hardware or locate the required drivers.</remarks>
        ///<seealso href="xref:SinglePassStereoRendering">Single Pass Stereo Rendering</seealso>
        [System.Obsolete("singlePassRenderingDisabled{get;set;} is deprecated. Use textureLayout and supportedTextureLayouts instead.", false)]
        public bool singlePassRenderingDisabled
        {
            get { return (textureLayout & TextureLayout.Texture2DArray) == 0; }
            set
            {
                if (value)
                {
                    textureLayout = TextureLayout.SeparateTexture2Ds;
                }
                else
                {
                    if ((supportedTextureLayouts & TextureLayout.Texture2DArray) > 0)
                        textureLayout = TextureLayout.Texture2DArray;
                }
            }
        }

        ///<summary>Determines if the current attached device has an opaque display.
        ///
        ///                Most VR devices are opaque in order to increase the immersive experience, AR devices are transparent to allow for interaction with an augmentation of the current environment.</summary>
        extern public bool displayOpaque { get; }
        ///<summary>Sets or gets the state of content protection for the current active provider.
        ///
        ///                For most providers, content protection allows you to use write only textures for rendering. This stops the ability for apps to read textures from the graphics card and view/record images that may be protected in some way.</summary>
        extern public bool contentProtectionEnabled { get; set; }
        ///<summary>The portion of the allocated display texture used by the active stereo device for the current frame.</summary>
        ///<remarks>The scale factor is fetched from the device and can change from frame to frame.
        ///                    If you access this value during gameplay logic (<c>MonoBehaviour.LateUpdate</c> or earlier), the value is the applied viewport scale of previous frame. If you access this value during rendering logic (<c>MonoBehaviour.OnPreCull</c> or later), the value is for the current frame.
        ///
        ///                    The <see cref="XRDisplaySubsystem.scaleOfAllViewports" /> setting can influence the scale factor, but the XR device can decide to ignore or change it.
        ///
        ///                    This property can range between 0 and 1. For example, a value of 0.5 indicates that one quarter of the allocated texture will be used for the display (half the width and half the height). The origin of the area used depends on the graphics API. For example, Vulkan and DirectX use the top-left portion of the texture, whereas OpenGL uses the bottom-left.</remarks>
        extern public float appliedViewportScale { get; }
        ///<summary>Controls how much of the allocated display texture should be used for rendering.</summary>
        ///<remarks>Valid range is 0.0 to 1.0. This value can be changed at runtime without reallocating textures, which makes it useful for dynamically adjusting render resolution. Changes to this value take effect the next time the scene begins rendering (after LateUpdate).
        ///
        ///Some display providers might ignore this value or clamp it.
        ///To fetch the applied viewport scale currently in use, check <see cref="XRDisplaySubsystem.appliedViewportScale" />
        ///
        ///This value is not supported with the legacy deferred renderer. If you attempt to change the value in the presence of camera that uses deferred rendering, Unity will ignore it and log a warning.</remarks>
        extern public float scaleOfAllViewports { get; set; }
        ///<summary>Controls the size of the textures submitted to the display as a multiplier of the display's default resolution.</summary>
        ///<remarks>A value of 1.0 uses the default texture resolution specified by the display provider. Values less than 1.0 use lower resolution textures, which might improve performance at the expense of a less sharp image. Values greater than 1.0 use higher resolution textures, resulting in a potentially sharper image at a cost to performance and increased memory usage.
        ///
        ///When this property changes, textures are always reallocated, which can negatively impact performance. To dynamically change texture resolution on the fly, consider using <see cref="XRDisplaySubsystem.scaleOfAllViewports" />.</remarks>
        extern public float scaleOfAllRenderTargets { get; set; }
        ///<summary>The current scale factor applied to dynamically scalable eye textures when XR dynamic resolution is active.</summary>
        ///<remarks>The scale factor is determined by the device and can change from frame to frame. If hardware dynamic resolution is turned off or not supported by the device, the scale factor is 1.0.
        ///
        ///Normally, dynamic resolution is handled automatically by the render pipeline. If you are implementing a custom pipeline or custom render pass, then your implementation must handle dynamic scaling of eye textures appropriately.</remarks>
        extern public float globalDynamicScale { get; }
        ///<summary>Set DisplaySubsystem to use zNear for rendering.</summary>
        ///<remarks>zNear is the near plan in meters from the main camera. ZFar and ZNear are used for explicit XR depth buffer sharing.</remarks>
        extern public float zNear { get; set; }
        ///<summary>Set DisplaySubsystem to use zFar for rendering.</summary>
        ///<remarks>zFar is the far plane in meters from the main camera. ZFar and ZNear are used for explicit XR depth buffer sharing.</remarks>
        extern public float zFar { get; set; }
        ///<exclude />
        extern public bool  sRGB { get; set; }
        ///<summary>A scale applied to the standard occlusion mask.</summary>
        ///<remarks>This property scales up the occlusion mask to allow pixels outside of the XR headset's field of vision are rendered to, allowing effects to access the required texture data. Scaling up the occlusion mask could incur a performance penalty on the GPU due to the extra pixels being rendered.</remarks>
        extern public float occlusionMaskScale { get; set;}

        ///<summary>Optional flags to control the foveated rendering system.</summary>
        [Flags]
        public enum FoveatedRenderingFlags
        {
            ///<summary>The default behavior with no extra configuration flags.</summary>
            None = 0,
            ///<summary>Allows the platform to use eye tracking to optimize foveated rendering.</summary>
            GazeAllowed = 1 << 0
        }

        ///<summary>Controls the intensity of the foveated rendering system.</summary>
        ///<remarks>Valid range is 0.0 to 1.0. A value of 0.0 will disable foveated rendering. A value of 1.0 will apply the maximum strength allowed by the platform and will usually provide the best GPU performance improvement. Changes to this value take effect the next time the scene begins rendering.</remarks>
        extern public float foveatedRenderingLevel { get; set; }
        ///<summary>Controls optional behavior of the foveated rendering system.</summary>
        extern public FoveatedRenderingFlags foveatedRenderingFlags { get; set; }

        ///<summary>The type of node to be late latched.</summary>
        public enum LateLatchNode
        {
            ///<summary>Head node type for late latching. This represents the camera node in the pose hierarchy.</summary>
            Head = 0,
            ///<summary>Left hand node type for late latching. This represents the left hand anchor node in the pose hierarchy.</summary>
            LeftHand = 1,
            ///<summary>Right hand node type for late latching. This represents the right hand anchor node in the pose hierarchy.</summary>
            RightHand = 2,
        }
        ///<summary>This marks a given GameObject's transform to be late latched in the next frame. Once marked for late latching, the GameObject transform and its descendants will be updated with the latest VR pose updates before rendering is submitted to the GPU.</summary>
        ///<param name="transform">The transform of the GameObject to be late latched.</param>
        ///<param name="nodeType">The late latch node type to be associated with the transform.</param>
        extern public void MarkTransformLateLatched(Transform transform, LateLatchNode nodeType);

        ///<summary>Flags that designate the supported texture layouts.</summary>
        [Flags]
        public enum TextureLayout
        {
            // *MUST* be in sync with the kUnityXRTextureLayoutFlagsTexture2DArray
            ///<summary>Textures could be configured to a texture2DArray type.</summary>
            Texture2DArray = 1 << 0,
            // *MUST* be in sync with the kUnityXRTextureLayoutFlagsSingleTexture2D
            ///<summary>Textures could be configured to a texture2D that represents multiple views.</summary>
            SingleTexture2D = 1 << 1,
            // *MUST* be in sync with the kUnityXRTextureLayoutFlagsSeparateTexture2Ds
            ///<summary>Textures could be configured to multiple texture2D type.</summary>
            SeparateTexture2Ds = 1 << 2
        }
        ///<summary>Set DisplaySubsystem to use certain texture layout. Should query supported texture layout through [[XRDisplaySubsystem.supportedTextureLayouts
        ///]] first for the capabilities.</summary>
        extern public TextureLayout textureLayout { get; set; }
        ///<summary>Specifies all texture layouts supported by this display subsystem. This var is a bit field that could be combination of <see cref="XRDisplaySubsystem.TextureLayout" />.</summary>
        extern public TextureLayout supportedTextureLayouts { get; }

        ///<summary>The kind of reprojection the app requests to stabilize rendering relative to the user's head motion.</summary>
        public enum ReprojectionMode
        {
            ///<summary>Does not specify the type of reprojection mode to use.</summary>
            Unspecified,
            ///<summary>Stabilizes the image for changes to both the user's head position and orientation. This is best for world-locked content that you want to remain stationary as the user walks around.</summary>
            PositionAndOrientation,
            ///<summary>Stabilizes the image only for changes to the user's head orientation, ignores changes in position. This is best for body-locked content that you want to move with the user as they walk around, such as a 360-degree video.</summary>
            OrientationOnly,
            ///<summary>Does not stabilize the image for the user's head motion and instead fixes it in the display. Note that this is only comfortable for users when you use it sparingly, for example when the only visible content is a small cursor.</summary>
            None
        }

        ///<summary>Provides the current, scaled width of a render texture.</summary>
        ///<remarks>Render textures created with both the <see cref="RenderTextureCreationFlags.DynamicallyScalable">DynamicallyScalable</see> and <see cref="RenderTextureCreationFlags.EyeTexture">EyeTexture</see> flags are subject to scaling when dynamic resolution is active. This function returns the effective, scaled width for the current frame.
        ///
        ///If you pass in a texture that is not affected by dynamic resolution, this method returns the original width.
        ///
        ///The scaled width can be useful when you need to know the exact size of an eye texture when XR dynamic resolution is enabled.
        ///For example, you might need the size for post-processing effects in custom shaders that use screen-space texture coordinates, to program custom XR render passes, or to create custom render pipelines.</remarks>
        ///<param name="renderTexture">A scalable XR eye texture.</param>
        ///<returns>The width after dynamic scaling. If the texture was not created with both the <see cref="RenderTextureCreationFlags.DynamicallyScalable">DynamicallyScalable</see> and <see cref="RenderTextureCreationFlags.EyeTexture">EyeTexture</see>  flags, or dynamic resolution is not enabled, the original width is returned.</returns>
        extern public int ScaledTextureWidth(RenderTexture renderTexture);
        ///<summary>Provides the current, scaled height of a render texture.</summary>
        ///<remarks>Render textures created with both the <see cref="RenderTextureCreationFlags.DynamicallyScalable">DynamicallyScalable</see> and <see cref="RenderTextureCreationFlags.EyeTexture">EyeTexture</see> flags are subject to scaling when dynamic resolution is active. This function returns the effective, scaled height for the current frame.
        ///
        ///If you pass in a texture that is not affected by dynamic resolution, this method returns the original height.
        ///
        ///The scaled height can be useful when you need to know the exact size of an eye texture when XR dynamic resolution is enabled.
        ///For example, you might need the size for post-processing effects in custom shaders that use screen-space texture coordinates, to program custom XR render passes, or to create custom render pipelines.</remarks>
        ///<param name="renderTexture">A scalable XR eye texture.</param>
        ///<returns>The height after dynamic scaling. If the texture was not created with both the <see cref="RenderTextureCreationFlags.DynamicallyScalable">DynamicallyScalable</see> and <see cref="RenderTextureCreationFlags.EyeTexture">EyeTexture</see>  flags, or dynamic resolution is not enabled, the original height is returned.</returns>
        extern public int ScaledTextureHeight(RenderTexture renderTexture);

        ///<summary>The kind of reprojection the app requests to stabilize rendering relative to the user's head motion.</summary>
        extern public ReprojectionMode reprojectionMode { get; set; }

        ///<summary>Sets a point in 3D space that acts as the focal point of the Scene for this frame. This helps to improve the visual fidelity of content around this point. You must set this value every frame.
        ///
        ///                Note that specifying body-locked content in focus improves the fidelity of body-locked content at the expense of content not locked to the body. This is especially apparent when the user moves.</summary>
        ///<param name="point">The position of the focal point in the Scene, relative to the Camera.</param>
        ///<param name="normal">Surface normal of the plane being viewed at the focal point.</param>
        ///<param name="velocity">A vector that describes how the focus point moves in the Scene at this point in time. This allows the device to compensate for both your head movement and the movement of the object in the Scene.</param>
        extern public void SetFocusPlane(Vector3 point, Vector3 normal, Vector3 velocity);

        ///<summary>Set MSAA level for the DisplaySubsystem's render texture.</summary>
        ///<param name="level">The MSAA level.</param>
        extern public void SetMSAALevel(int level);

        ///<summary>Disables the legacy renderer while this <see cref="XRDisplaySubsystem" /> is active.</summary>
        ///<remarks>The scriptable render pipeline can render to XR devices by querying information on this <see cref="XRDisplaySubsystem" />.</remarks>
        extern public bool disableLegacyRenderer { get; set; }

        ///<summary>The number of <see cref="XRRenderPass" /> entries for this XR Display.</summary>
        ///<returns>Count of render passes.</returns>
        extern public int GetRenderPassCount();
        ///<summary>Gets an <see cref="XRRenderPass" /> of a specific index.</summary>
        ///<param name="renderPassIndex">The index of the render pass to get.  Must be less than <see cref="GetRenderPassCount" />.</param>
        ///<param name="renderPass">Render pass to populate.</param>
        public void GetRenderPass(int renderPassIndex, out XRRenderPass renderPass)
        {
            if (!Internal_TryGetRenderPass(renderPassIndex, out renderPass))
            {
                throw new IndexOutOfRangeException("renderPassIndex");
            }
        }

        [NativeMethod("TryGetRenderPass")]
        extern private bool Internal_TryGetRenderPass(int renderPassIndex, out XRRenderPass renderPass);

        ///<summary>This function disables late latching recording of constant buffer locations.</summary>
        ///<param name="camera">The camera where late latch end recording is to be done.</param>
        public void EndRecordingIfLateLatched(Camera camera)
        {
            if (!Internal_TryEndRecordingIfLateLatched(camera))
            {
                if (camera == null)
                {
                    throw new ArgumentNullException("camera");
                }
            }
        }

        [NativeMethod("TryEndRecordingIfLateLatched")]
        extern private bool Internal_TryEndRecordingIfLateLatched(Camera camera);
        ///<summary>This function enables late latching recording of constant buffer memory locations which are later patched with the latest pose data.</summary>
        ///<param name="camera">The camera where late latch recording is to be enabled.</param>
        public void BeginRecordingIfLateLatched(Camera camera)
        {
            if (!Internal_TryBeginRecordingIfLateLatched(camera))
            {
                if (camera == null)
                {
                    throw new ArgumentNullException("camera");
                }
            }
        }

        [NativeMethod("TryBeginRecordingIfLateLatched")]
        extern private bool Internal_TryBeginRecordingIfLateLatched(Camera camera);

        ///<summary>Gets culling parameters for a specific culling pass index.</summary>
        ///<remarks>You can obtain a culling pass index from <see cref="XR.XRDisplaySubsystem.XRRenderPass.cullingPassIndex">XRRenderPass.cullingPassIndex</see>.</remarks>
        ///<param name="camera">
        ///  <see cref="Camera" /> for the basis of the culling view and frustum.</param>
        ///<param name="cullingPassIndex">Index of the culling pass obtained from <see cref="XR.XRDisplaySubsystem.XRRenderPass.cullingPassIndex">XRRenderPass.cullingPassIndex</see>.</param>
        ///<param name="scriptableCullingParameters">Scriptable culling parameters to populate.</param>
        public void GetCullingParameters(Camera camera, int cullingPassIndex, out ScriptableCullingParameters scriptableCullingParameters)
        {
            if (!Internal_TryGetCullingParams(camera, cullingPassIndex, out scriptableCullingParameters))
            {
                if (camera == null)
                {
                    throw new ArgumentNullException("camera");
                }
                else
                {
                    throw new IndexOutOfRangeException("cullingPassIndex");
                }
            }
        }

        [NativeHeader("Runtime/Graphics/ScriptableRenderLoop/ScriptableCulling.h")]
        [NativeMethod("TryGetCullingParams")]
        extern private bool Internal_TryGetCullingParams(Camera camera, int cullingPassIndex, out ScriptableCullingParameters scriptableCullingParameters);

        ///<summary>A single viewpoint that must be rendered by the render pipeline.  Contains a target viewport and texture array slice within a corresponding <see cref="XR.XRDisplaySubsystem.XRRenderPass.renderTarget">renderTarget</see>.</summary>
        [NativeHeader("Modules/XR/Subsystems/Display/XRDisplaySubsystem.bindings.h")]
        [StructLayout(LayoutKind.Sequential)]
        public struct XRRenderParameter
        {
            ///<summary>World transform that the render pipeline should use to render to the <see cref="XR.XRDisplaySubsystem.XRRenderPass.renderTarget">renderTarget</see>.</summary>
            public Matrix4x4 view;
            ///<summary>The projection matrix that the render pipeline should use to render to the <see cref="XR.XRDisplaySubsystem.XRRenderPass.renderTarget">renderTarget</see>.</summary>
            public Matrix4x4 projection;
            ///<summary>Selects the viewport of the output texture <see cref="XR.XRDisplaySubsystem.XRRenderPass.renderTarget">renderTarget</see>.</summary>
            public Rect viewport;
            ///<summary>Represents the area in screen-space that is not visible on the XR Display.</summary>
            ///<remarks>You can use this to prevent GPU work from happening on portions of the XR textures that aren't visible in the XR display. This is an effective way to optimize the renderer.</remarks>
            public Mesh occlusionMesh;
            ///<summary>Represents the area in screen-space that is visible on the XR Display.</summary>
            ///<remarks>The visibility mesh contains vertices outlining the visible area of the XR display in screen-space coordinates. The mesh encompasses both eyes under single-pass instanced rendering and one eye under multi-pass rendering (a different eye for each pass).
            ///
            ///When available, the Unity post-processing stack uses the visibility mesh to avoid processing pixels that are outside the visible display area of the XR headset.
            ///
            ///You can use this mesh to prevent your screen-space shaders from drawing to areas that are outside the effective display area. For example, if you are performing your own post-processing step, you might be able to optimize it by specifying the screen area in which the post-processing effect should be calculated.
            ///
            ///If a device or platform does not provide a visibility mesh, then this XR render parameter field is <c>null</c>.</remarks>
            public Mesh visibleMesh;
            ///<summary>The slice of the output texture array that the render pipeline should render to.</summary>
            public int textureArraySlice;
            ///<summary>Previous frame view matrix for use in motion vector calculation. Use <see cref="XR.XRDisplaySubsystem.XRRenderParameter.isPreviousViewValid" /> to determine if previous view is valid for use. When late latching is enabled, previous view is also adjusted for late latching.</summary>
            public Matrix4x4 previousView;
            ///<summary>Determines whether <see cref="XR.XRDisplaySubsystem.XRRenderParameter.previousView" /> is valid for use in a frame.</summary>
            public bool isPreviousViewValid;
        }

        ///<summary>Contains configuration parameters about which view into the Scene the renderer should rasterize, and a render target (which can be a texture array) for the result of the rasterization.</summary>
        ///<remarks>An XRRenderPass can contain more than one <see cref="XRRenderParameter" /> (viewpoints that the render pipeline renders to the output texture as either different viewports or texture array slices). The render pipeline must query each child <see cref="XRRenderParameter" /> via <see cref="XR.XRDisplaySubsystem.XRRenderPass.GetRenderParameter">GetRenderParameter</see>. The most optimal way to implement an XRRenderPass is to cull first, and then submit draw calls once for the resulting objects. You can also use techniques such as instanced rendering to optimize XRRenderPasses that contain more than one <see cref="XRRenderParameter" />.
        ///
        ///XRRenderPass is typically consumed by a scriptable rendering pipeline.</remarks>
        [NativeHeader("Runtime/Graphics/RenderTextureDesc.h")]
        [NativeHeader("Runtime/Graphics/CommandBuffer/RenderingCommandBuffer.h")]
        [NativeHeader("Modules/XR/Subsystems/Display/XRDisplaySubsystem.bindings.h")]
        [StructLayout(LayoutKind.Sequential)]
        public struct XRRenderPass
        {
            private IntPtr displaySubsystemInstance;
            ///<summary>The index of the render pass (originally passed in to <see cref="XRDisplaySubsystem.GetRenderPass" />).</summary>
            public int renderPassIndex;

            ///<summary>The output target for the render pass.</summary>
            ///<remarks>This a resource owned directly by the XR Display and is only valid this frame.  Can be a texture array.</remarks>
            public RenderTargetIdentifier renderTarget;
            ///<summary>Descriptor that can be passed to <see cref="RenderTexture.GetTemporary" /> to create temporary textures that match the XR Display render target.</summary>
            public RenderTextureDescriptor renderTargetDesc;
            ///<summary>The current, scaled width of the <see cref="XR.XRDisplaySubsystem.XRRenderPass.renderTarget">XRRenderPass.renderTarget</see> for this <c>renderPass</c>.</summary>
            ///<remarks>If dynamic scaling is not enabled or not supported, then the scaled width is equal to  the <see cref="RenderTextureDescriptor.width">width</see> value of the texture's <see cref="XR.XRDisplaySubsystem.XRRenderPass.renderTargetDesc">descriptor</see>.</remarks>
            public int renderTargetScaledWidth;
            ///<summary>The current, scaled height of the <see cref="XR.XRDisplaySubsystem.XRRenderPass.renderTarget">XRRenderPass.renderTarget</see> for this <c>renderPass</c>.</summary>
            ///<remarks>If dynamic scaling is not enabled or not supported, then the scaled height is equal to  the <see cref="RenderTextureDescriptor.height">height</see> value of the texture's <see cref="XR.XRDisplaySubsystem.XRRenderPass.renderTargetDesc">descriptor</see>.</remarks>
            public int renderTargetScaledHeight;

            ///<summary>A boolean indicating if this render pass contains a motion-vector generation pass.</summary>
            public bool hasMotionVectorPass;
            ///<summary>The output render-texture target for the motion-vector generation render pass.</summary>
            public RenderTargetIdentifier motionVectorRenderTarget;
            ///<summary>The render texture description for the target texture for the motion-vector render pass.</summary>
            public RenderTextureDescriptor motionVectorRenderTargetDesc;

            ///<summary>When this is false an optimal renderer can avoid resolving the depth buffer.</summary>
            public bool shouldFillOutDepth;

            ///<summary>When <c>true</c>, the SpaceWarp motion vector data is in the right-handed normalized device coordinate (NDC) space. When <c>false</c>, the motion vector data is in the left-handed NDC space.</summary>
            ///<remarks>See the <see href="https://docs.unity3d.com/Packages/com.unity.xr.openxr@latest?subfolder=/manual/features/spacewarp.html">SpaceWarp</see> documentation in the OpenXR Plugin package for more information.</remarks>
            public bool spaceWarpRightHandedNDC;

            /// <summary>When <c>true</c>, SRP-based foveated rendering is always skipped during the FinalBlit and UberPostProcess passes.</summary>
            ///<remarks>See the <see href="https://docs.unity3d.com/Packages/com.unity.xr.openxr@latest?subfolder=/manual/features/foveatedrendering.html">Foveated Rendering</see> documentation in the OpenXR Plugin package for more information.</remarks>
            public bool skipFDMForFinalPasses;

            ///<summary>An index that a render pipeline can pass to <see cref="XR.XRDisplaySubsystem.GetCullingParameters" /> to obtain culling information.</summary>
            ///<remarks>Multiple <see cref="XRRenderPass">render passes</see> can share the same index. This means that the renderer only needs to cull once, and can reuse the result of the culling for all render passes that use the same index.</remarks>
            public int cullingPassIndex;

            ///<summary>A pointer to a native struct containing platform-specific data for foveated rendering.</summary>
            ///<remarks>This pointer can be passed to <see cref="CommandBuffer.ConfigureFoveatedRendering"/> by the scriptable rendering pipeline implementation.</remarks>
            public IntPtr foveatedRenderingInfo;

            ///<summary>Gets an <see cref="XRRenderParameter" /> for a specific <see cref="XRRenderPass" />.</summary>
            ///<param name="camera">
            ///  <see cref="Camera" /> for the basis of the view and projection.</param>
            ///<param name="renderParameterIndex">Index of the render parameter to get.  Must be less than <see cref="GetRenderParameterCount" />.</param>
            ///<param name="renderParameter">
            ///  <see cref="XRRenderParameter" /> to populate.</param>
            [NativeMethod(Name = "XRRenderPassScriptApi::GetRenderParameter", IsFreeFunction = true, HasExplicitThis = true, ThrowsException = true)]
            [NativeConditional("ENABLE_XR")]
            extern public void GetRenderParameter(Camera camera, int renderParameterIndex, out XRRenderParameter renderParameter);

            ///<summary>The number of <see cref="XRRenderParameter" /> entries for this <see cref="XRRenderPass" />.</summary>
            ///<returns>Count of render parameters.</returns>
            [NativeMethod(Name = "XRRenderPassScriptApi::GetRenderParameterCount", IsFreeFunction = true, HasExplicitThis = true)]
            [NativeConditional("ENABLE_XR")]
            extern public int GetRenderParameterCount();
        }

        ///<summary>Retrieves the amount of time that the GPU spent executing the compositor renderer during the last frame, as reported by the XR Plugin. Measured in seconds.</summary>
        ///<remarks>You can use this method to get more accurate timing information from the SDK, including information about GPU time spent in SDK-specific layers.
        ///
        ///                    Statistics are only available for SDKs that support this method, and they can vary based on hardware, the SDK, and the frame. You should always check the return value of this method before you use the statistic value from the out parameter.</remarks>
        ///<param name="gpuTimeLastFrameCompositor">Outputs the time spent by the GPU for the compositor during the last frame.</param>
        ///<returns>Returns true if the GPU time spent on the last frame is available. Returns false if that time is unavailable.</returns>
        [NativeMethod("TryGetCompositorGPUTimeLastFrame")]
        extern public bool TryGetCompositorGPUTimeLastFrame(out float gpuTimeLastFrameCompositor);

        ///<summary>Retrieves the refresh rate of the display as reported by the XR Plugin.</summary>
        ///<remarks>The XR plugin uses the display refresh rate as the target frame rate. This can be useful information for synchronizing fixed updates.
        ///
        ///                    Statistics are only available for SDKs that support this method, and they can vary based on hardware, the SDK, and the frame. You should always check the return value of this method before you use the statistic value from the out parameter.</remarks>
        ///<returns>Returns true if the display refresh rate is available. Returns false if that rate is unavailable.</returns>
        [NativeMethod("TryGetDisplayRefreshRate")]
        extern public bool TryGetDisplayRefreshRate(out float displayRefreshRate);

        ///<summary>Retrieves the motion-to-photon value as reported by the XR Plugin.</summary>
        ///<remarks>The motion-to-photon represents latency. This latency is the difference between the last predicted tracking information and the moment that the scan-line of the target frame lights up on the display. You can use this to determine application latency.
        ///
        ///                    Statistics are only available for SDKs that support this method, and they can vary based on hardware, the SDK, and the frame. You should always check the return value of this method before you use the statistic value from the out parameter.</remarks>
        ///<param name="motionToPhoton">Outputs the motion-to-photon value.</param>
        ///<returns>Returns true if the motion-to-photon value is available. Returns false otherwise.</returns>
        [NativeMethod("TryGetMotionToPhoton")]
        extern public bool TryGetMotionToPhoton(out float motionToPhoton);

        ///<summary>This struct  holds data for a single blit operation.</summary>
        [NativeHeader("Modules/XR/Subsystems/Display/XRDisplaySubsystem.bindings.h")]
        [NativeHeader("Runtime/Graphics/RenderTexture.h")]
        [StructLayout(LayoutKind.Sequential)]
        public struct XRBlitParams
        {
            ///<summary>Source render texture that the blit operation wants to blit from.</summary>
            public RenderTexture srcTex;
            ///<summary>Describes source texture's desired array slice. Texture2D will have array slice 1.</summary>
            public int srcTexArraySlice;
            ///<summary>Source Rect area that the blit operation wants to blit from.</summary>
            public Rect srcRect;
            ///<summary>Destination Rect area that the blit operation wants to blit to.</summary>
            public Rect destRect;
            ///<summary>A pointer to a native struct containing platform-specific data for foveated rendering.</summary>
            ///<remarks>This pointer can be passed to <see cref="CommandBuffer.ConfigureFoveatedRendering" /> by the scriptable rendering pipeline implementation.</remarks>
            public IntPtr foveatedRenderingInfo;
            ///<summary>Specifies whether the source texture is encoded for use with an HDR display and might require decoding during the blit process.</summary>
            public bool srcHdrEncoded;
            ///<summary>The <see cref="ColorGamut" /> of the source texture if <see cref="srcHdrEncoded" /> is true.</summary>
            public ColorGamut srcHdrColorGamut;
            ///<summary>The maximum luminance in nits of the encoding used for the source texture if <see cref="srcHdrEncoded" /> is true.</summary>
            public int srcHdrMaxLuminance;
        }

        ///<summary>All information in this struct describes the desired mirror view blit operation.</summary>
        ///<remarks>And XRMirrorViewBlitDesc can contain more than one <see cref="XRBlitParams" /> (describes exactly one blit operation). The render pipeline can query each child <see cref="XRBlitParams" /> via GetBlitParameter. <see cref="XRMirrorViewBlitDesc" /> is typically consumed by a scriptable rendering pipeline.</remarks>
        [NativeHeader("Modules/XR/Subsystems/Display/XRDisplaySubsystem.bindings.h")]
        [StructLayout(LayoutKind.Sequential)]
        public struct XRMirrorViewBlitDesc
        {
            private IntPtr displaySubsystemInstance;
            ///<summary>When this is true, the current display subsystem supports native blit and <see cref="AddGraphicsThreadMirrorViewBlit" /> must be called to perform native blit.</summary>
            public bool nativeBlitAvailable;
            ///<summary>When this is true, display subsystem will modifiy the graphics state.</summary>
            public bool nativeBlitInvalidStates;
            ///<summary>The number of XRBlitParams entries for this XRMirrorViewBlitDesc.</summary>
            public int  blitParamsCount;

            ///<summary>Gets an XRBlitParams for a specific XRMirrorViewBlitDesc.</summary>
            ///<param name="blitParameterIndex">Index of the blit parameter to get.</param>
            ///<param name="blitParameter">
            ///  <see cref="XRBlitParams" /> to populate.</param>
            [NativeMethod(Name = "XRMirrorViewBlitDescScriptApi::GetBlitParameter", IsFreeFunction = true, HasExplicitThis = true)]
            [NativeConditional("ENABLE_XR")]
            extern public void GetBlitParameter(int blitParameterIndex, out XRBlitParams blitParameter);
        }

        ///<summary>Given the UnityXRRenderTextureID returned by IUnityXRDisplayInterface::CreateTexture, return the managed UnityEngine.RenderTexture instance.</summary>
        ///<param name="unityXrRenderTextureId">The ID number identifying the render texture.</param>
        ///<returns>The managed UnityEngine.RenderTexture instance associated with the UnityXRRenderTextureID.</returns>
        [NativeMethod(Name = "UnityXRRenderTextureIdToRenderTexture", IsThreadSafe = false)]
        [NativeConditional("ENABLE_XR")]
        extern public RenderTexture GetRenderTexture(uint unityXrRenderTextureId);

        ///<summary>Given a render pass, return the RenderTexture instance backing that render pass. If the render pass is invalid, or if the render texture does not exist, return null.</summary>
        ///<param name="renderPass">The render pass index to get the render texture for.</param>
        ///<returns>The render texture associated with that render pass, or null if not found.</returns>
        [NativeMethod(Name = "GetTextureForRenderPass", IsThreadSafe = false)]
        [NativeConditional("ENABLE_XR")]
        extern public RenderTexture GetRenderTextureForRenderPass(int renderPass);

        ///<summary>Given a render pass, return the shared depth buffer RenderTexture instance backing that render pass. If the render pass is invalid, or if the render texture does not exist, return null.</summary>
        ///<param name="renderPass">The render pass index to get the shared depth buffer render texture for.</param>
        ///<returns>The shared depth buffer render texture associated with that render pass, or null if not found.</returns>
        [NativeMethod(Name = "GetSharedDepthTextureForRenderPass", IsThreadSafe = false)]
        [NativeConditional("ENABLE_XR")]
        extern public RenderTexture GetSharedDepthTextureForRenderPass(int renderPass);

        ///<summary>Returns the XR display's preferred mirror blit mode.</summary>
        ///<returns>Display subsystem's preferred blit mode.</returns>
        [NativeMethod(Name = "GetPreferredMirrorViewBlitMode", IsThreadSafe = false)]
        [NativeConditional("ENABLE_XR")]
        extern public int GetPreferredMirrorBlitMode();

        ///<summary>Override the XR display's preferred mirror blit mode from the script.</summary>
        ///<param name="blitMode">
        ///  <see cref="XRMirrorViewBlitMode" /> to set.</param>
        [NativeMethod(Name = "SetPreferredMirrorViewBlitMode", IsThreadSafe = false)]
        [NativeConditional("ENABLE_XR")]
        extern public void SetPreferredMirrorBlitMode(int blitMode);

        ///<summary>Get a mirror view blit operation descriptor from the current display subsystem.</summary>
        ///<param name="mirrorRt">A render texture representing mirror view's render target.</param>
        ///<param name="outDesc">Information that describes desired mirror view blit operation.</param>
        ///<returns>Return true if information is retrieved successfully, false otherwise.</returns>
        [System.Obsolete("GetMirrorViewBlitDesc(RenderTexture, out XRMirrorViewBlitDesc) is deprecated. Use GetMirrorViewBlitDesc(RenderTexture, out XRMirrorViewBlitDesc, int) instead.", false)]
        public bool GetMirrorViewBlitDesc(RenderTexture mirrorRt, out XRMirrorViewBlitDesc outDesc)
        {
            return GetMirrorViewBlitDesc(mirrorRt, out outDesc, XRMirrorViewBlitMode.LeftEye);
        }

        ///<summary>Get a mirror view blit operation descriptor from the current display subsystem.</summary>
        ///<param name="mirrorRt">A render texture representing mirror view's render target.</param>
        ///<param name="outDesc">Information that describes desired mirror view blit operation.</param>
        ///<param name="mode">The <see cref="XRMirrorViewBlitMode" /> XR display should perform.</param>
        ///<returns>Return true if information is retrieved successfully, false otherwise.</returns>
        [NativeMethod(Name = "QueryMirrorViewBlitDesc", IsThreadSafe = false)]
        [NativeConditional("ENABLE_XR")]
        extern public bool GetMirrorViewBlitDesc(RenderTexture mirrorRt, out XRMirrorViewBlitDesc outDesc, int mode);

        ///<summary>This function records the display subsystem's native blit event to the target command buffer. This function is typically called by a scriptable rendering pipeline.</summary>
        ///<param name="cmd">The target <see cref="CommandBuffer" /> that records the native blit event.</param>
        ///<param name="allowGraphicsStateInvalidate">True causes the graphics device to invalidate internal states before and after calling into the provider's native blit. This ensures the GFX internal states' consistency with the cost of some runtime performance.</param>
        ///<returns>Returns true if native blit event is successfully recorded. Returns false otherwise.</returns>
        [System.Obsolete("AddGraphicsThreadMirrorViewBlit(CommandBuffer, bool) is deprecated. Use AddGraphicsThreadMirrorViewBlit(CommandBuffer, bool, int) instead.", false)]
        public bool AddGraphicsThreadMirrorViewBlit(CommandBuffer cmd, bool allowGraphicsStateInvalidate)
        {
            return AddGraphicsThreadMirrorViewBlit(cmd, allowGraphicsStateInvalidate, XRMirrorViewBlitMode.LeftEye);
        }

        ///<summary>This function records the display subsystem's native blit event to the target command buffer. This function is typically called by a scriptable rendering pipeline.</summary>
        ///<param name="cmd">The target <see cref="CommandBuffer" /> that records the native blit event.</param>
        ///<param name="allowGraphicsStateInvalidate">True causes the graphics device to invalidate internal states before and after calling into the provider's native blit. This ensures the GFX internal states' consistency with the cost of some runtime performance.</param>
        ///<param name="mode">The <see cref="XRMirrorViewBlitMode" /> XR display should perform.</param>
        ///<returns>Returns true if native blit event is successfully recorded. Returns false otherwise.</returns>
        [NativeMethod(Name = "AddGraphicsThreadMirrorViewBlit", IsThreadSafe = false)]
        [NativeHeader("Runtime/Graphics/CommandBuffer/RenderingCommandBuffer.h")]
        [NativeConditional("ENABLE_XR")]
        extern public bool AddGraphicsThreadMirrorViewBlit(CommandBuffer cmd, bool allowGraphicsStateInvalidate, int mode);

        new internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(XRDisplaySubsystem xrDisplaySubsystem) => xrDisplaySubsystem.m_Ptr;
        }

        private HDROutputSettings m_HDROutputSettings;
        ///<summary>The <see cref="HDROutputSettings" /> for the XR Display Subsystem.</summary>
        public HDROutputSettings hdrOutputSettings { get { if (m_HDROutputSettings == null) m_HDROutputSettings = new HDROutputSettings(-1); return m_HDROutputSettings; } }

        // Virtual functions below have default implementations in XRDisplaySubsystemDefault.cs.
        ///<summary>The refresh rate of the current attached device's display.</summary>
        ///<remarks>An alternative to <see cref="TryGetDisplayRefreshRate" />.</remarks>
        public virtual float displayRefreshRate
        {
            get
            {
                if (TryGetDisplayRefreshRate(out float rate))
                {
                    return rate;
                }
                return 0.0f;
            }
        }

        ///<summary>The field of view zoom factor of the current XR projection.</summary>
        ///<remarks>Scales the viewing frustrum of the XR projection matrix. This method of scaling handles  asymmetric XR projections correctly.
        ///
        ///                The minimum value of this property is clamped to 1.0 (no zoom).</remarks>
        public virtual float fovZoomFactor
        {
            get => GetFovZoomFactorInternal();
            set => SetFovZoomFactorInternal(value);
        }

        ///<summary>Retrieves the time the GPU has spent on executing commands from the application's last frame, as reported by the XR Plugin. Measured in seconds.</summary>
        ///<remarks>You can use this method to get more accurate timing information from the SDK, including information about GPU time spent in SDK-specific layers.
        ///
        ///                    Statistics are only available for SDKs that support this method, and they can vary based on hardware, the SDK, and the frame. You should always check the return value of this method before you use the statistic value from the out parameter.</remarks>
        ///<param name="gpuTimeLastFrame">Outputs the time spent by the GPU during the last frame.</param>
        ///<returns>Returns true if the GPU time spent on the last frame is available. Returns false if that time is unavailable.</returns>
        public virtual bool TryGetAppGPUTimeLastFrame(out float gpuTimeLastFrame)
        {
            return TryGetAppGPUTimeLastFrameInternal(out gpuTimeLastFrame);
        }

        ///<summary>Retrieves the number of dropped frames reported by the XR Plugin.</summary>
        ///<remarks>Use this method for games and applications that you want to scale content or settings dynamically in order to maximise frame rates. XR applications and games must run at a consistent, high frame rate. If an application has too many draw calls or calculations, it may have to "drop" frames in order to keep a high frame rate. When the SDK reports that the application is dropping frames, the application can adjust settings, disable objects, or perform other actions to reduce overhead.
        ///
        ///                    Statistics are only available for SDKs that support this method, and they can vary based on hardware, the SDK, and the frame. You should always check the return value of this method before you use the statistic value from the out parameter.</remarks>
        ///<param name="droppedFrameCount">Outputs the number of frames dropped since the last update.</param>
        ///<returns>Returns true if the dropped frame count is available. Returns false otherwise.</returns>
        public virtual bool TryGetDroppedFrameCount(out int droppedFrameCount)
        {
            return TryGetDroppedFrameCountInternal(out droppedFrameCount);
        }

        ///<summary>Retrieves the number of times the current frame has been drawn to the device as reported by the XR Plugin.</summary>
        ///<remarks>If performance degrades, some SDKs draw the current frame multiple times. You can use the frame present count to see if the SDK has presented the same frame to the viewer multiple times.
        ///
        ///                    Statistics are only available for SDKs that support this method, and they can vary based on hardware, the SDK, and the frame. You should always check the return value of this method before you use the statistic value from the out parameter.</remarks>
        ///<param name="framePresentCount">Outputs the number of times the current frame has been presented.</param>
        ///<returns>Returns true if the current frame count is available. Returns false otherwise.</returns>
        public virtual bool TryGetFramePresentCount(out int framePresentCount)
        {
            return TryGetFramePresentCountInternal(out framePresentCount);
        } // End of virtual functions

        // Pairing extern native methods for virtual functions
        [NativeMethod(Name = "GetFOVZoomFactor")]
        [NativeConditional("ENABLE_XR")]
        extern internal float GetFovZoomFactorInternal();

        [NativeMethod(Name = "SetFOVZoomFactor")]
        [NativeConditional("ENABLE_XR")]
        extern internal void SetFovZoomFactorInternal(float value);

        [NativeMethod("TryGetAppGPUTimeLastFrame")]
        extern internal bool TryGetAppGPUTimeLastFrameInternal(out float gpuTimeLastFrame);

        [NativeMethod("TryGetDroppedFrameCount")]
        extern internal bool TryGetDroppedFrameCountInternal(out int droppedFrameCount);

        [NativeMethod("TryGetFramePresentCount")]
        extern internal bool TryGetFramePresentCountInternal(out int framePresentCount);
    }
}
