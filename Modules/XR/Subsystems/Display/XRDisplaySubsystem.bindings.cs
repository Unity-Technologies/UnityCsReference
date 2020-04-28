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
    [NativeType(Header = "Modules/XR/Subsystems/Display/XRDisplaySubsystem.h")]
    [UsedByNativeCode]
    [NativeHeader("Modules/XR/XRPrefix.h")]
    [NativeConditional("ENABLE_XR")]
    public class XRDisplaySubsystem : IntegratedSubsystem<XRDisplaySubsystemDescriptor>
    {
        public event Action<bool> displayFocusChanged;

        [RequiredByNativeCode]
        private void InvokeDisplayFocusChanged(bool focus)
        {
            if (displayFocusChanged != null)
                displayFocusChanged.Invoke(focus);
        }

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

        extern public bool displayOpaque { get; }
        extern public bool contentProtectionEnabled { get; set; }
        extern public float scaleOfAllViewports { get; set; }
        extern public float scaleOfAllRenderTargets { get; set; }
        extern public float zNear { get; set; }
        extern public float zFar { get; set; }
        extern public bool  sRGB { get; set; }

        [Flags]
        public enum TextureLayout
        {
            // *MUST* be in sync with the kUnityXRTextureLayoutFlagsTexture2DArray
            Texture2DArray = 1 << 0,
            // *MUST* be in sync with the kUnityXRTextureLayoutFlagsSingleTexture2D
            SingleTexture2D = 1 << 1,
            // *MUST* be in sync with the kUnityXRTextureLayoutFlagsSeparateTexture2Ds
            SeparateTexture2Ds = 1 << 2
        }
        extern public TextureLayout textureLayout { get; set; }
        extern public TextureLayout supportedTextureLayouts { get; }

        public enum ReprojectionMode
        {
            Unspecified,
            PositionAndOrientation,
            OrientationOnly,
            None
        }

        extern public ReprojectionMode reprojectionMode { get; set; }

        extern public void SetFocusPlane(Vector3 point, Vector3 normal, Vector3 velocity);

        extern public void SetMSAALevel(int level);

        extern public bool disableLegacyRenderer { get; set; }

        extern public int GetRenderPassCount();
        public void GetRenderPass(int renderPassIndex, out XRRenderPass renderPass)
        {
            if (!Internal_TryGetRenderPass(renderPassIndex, out renderPass))
            {
                throw new IndexOutOfRangeException("renderPassIndex");
            }
        }

        [NativeMethod("TryGetRenderPass")]
        extern private bool Internal_TryGetRenderPass(int renderPassIndex, out XRRenderPass renderPass);

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

        [NativeHeader("Modules/XR/Subsystems/Display/XRDisplaySubsystem.bindings.h")]
        [StructLayout(LayoutKind.Sequential)]
        public struct XRRenderParameter
        {
            public Matrix4x4 view;
            public Matrix4x4 projection;
            public Rect viewport;
            public Mesh occlusionMesh;
            public int textureArraySlice;
        }

        [NativeHeader("Runtime/Graphics/RenderTextureDesc.h")]
        [NativeHeader("Runtime/Graphics/CommandBuffer/RenderingCommandBuffer.h")]
        [NativeHeader("Modules/XR/Subsystems/Display/XRDisplaySubsystem.bindings.h")]
        [StructLayout(LayoutKind.Sequential)]
        public struct XRRenderPass
        {
            private IntPtr displaySubsystemInstance;
            public int renderPassIndex;

            public RenderTargetIdentifier renderTarget;
            public RenderTextureDescriptor renderTargetDesc;

            public bool shouldFillOutDepth;

            public int cullingPassIndex;

            [NativeMethod(Name = "XRRenderPassScriptApi::GetRenderParameter", IsFreeFunction = true, HasExplicitThis = true, ThrowsException = true)]
            [NativeConditional("ENABLE_XR")]
            extern public void GetRenderParameter(Camera camera, int renderParameterIndex, out XRRenderParameter renderParameter);

            [NativeMethod(Name = "XRRenderPassScriptApi::GetRenderParameterCount", IsFreeFunction = true, HasExplicitThis = true)]
            [NativeConditional("ENABLE_XR")]
            extern public int GetRenderParameterCount();
        }

        [NativeMethod("TryGetAppGPUTimeLastFrame")]
        extern public bool TryGetAppGPUTimeLastFrame(out float gpuTimeLastFrame);

        [NativeMethod("TryGetCompositorGPUTimeLastFrame")]
        extern public bool TryGetCompositorGPUTimeLastFrame(out float gpuTimeLastFrameCompositor);

        [NativeMethod("TryGetDroppedFrameCount")]
        extern public bool TryGetDroppedFrameCount(out int droppedFrameCount);

        [NativeMethod("TryGetFramePresentCount")]
        extern public bool TryGetFramePresentCount(out int framePresentCount);

        [NativeMethod("TryGetDisplayRefreshRate")]
        extern public bool TryGetDisplayRefreshRate(out float displayRefreshRate);

        [NativeMethod("TryGetMotionToPhoton")]
        extern public bool TryGetMotionToPhoton(out float motionToPhoton);

        [NativeHeader("Modules/XR/Subsystems/Display/XRDisplaySubsystem.bindings.h")]
        [NativeHeader("Runtime/Graphics/RenderTexture.h")]
        [StructLayout(LayoutKind.Sequential)]
        public struct XRBlitParams
        {
            public RenderTexture srcTex;
            public int srcTexArraySlice;
            public Rect srcRect;
            public Rect destRect;
        }

        [NativeHeader("Modules/XR/Subsystems/Display/XRDisplaySubsystem.bindings.h")]
        [StructLayout(LayoutKind.Sequential)]
        public struct XRMirrorViewBlitDesc
        {
            private IntPtr displaySubsystemInstance;
            public bool nativeBlitAvailable;
            public bool nativeBlitInvalidStates;
            public int  blitParamsCount;

            [NativeMethod(Name = "XRMirrorViewBlitDescScriptApi::GetBlitParameter", IsFreeFunction = true, HasExplicitThis = true)]
            [NativeConditional("ENABLE_XR")]
            extern public void GetBlitParameter(int blitParameterIndex, out XRBlitParams blitParameter);
        }

        [NativeMethod(Name = "GetTextureForRenderPass", IsThreadSafe = false)]
        [NativeConditional("ENABLE_XR")]
        extern public RenderTexture GetRenderTextureForRenderPass(int renderPass);

        [NativeMethod(Name = "GetPreferredMirrorViewBlitMode", IsThreadSafe = false)]
        [NativeConditional("ENABLE_XR")]
        extern public int GetPreferredMirrorBlitMode();

        [NativeMethod(Name = "SetPreferredMirrorViewBlitMode", IsThreadSafe = false)]
        [NativeConditional("ENABLE_XR")]
        extern public void SetPreferredMirrorBlitMode(int blitMode);

        [System.Obsolete("GetMirrorViewBlitDesc(RenderTexture, out XRMirrorViewBlitDesc) is deprecated. Use GetMirrorViewBlitDesc(RenderTexture, out XRMirrorViewBlitDesc, int) instead.", false)]
        public bool GetMirrorViewBlitDesc(RenderTexture mirrorRt, out XRMirrorViewBlitDesc outDesc)
        {
            return GetMirrorViewBlitDesc(mirrorRt, out outDesc, XRMirrorViewBlitMode.LeftEye);
        }

        [NativeMethod(Name = "QueryMirrorViewBlitDesc", IsThreadSafe = false)]
        [NativeConditional("ENABLE_XR")]
        extern public bool GetMirrorViewBlitDesc(RenderTexture mirrorRt, out XRMirrorViewBlitDesc outDesc, int mode);

        [System.Obsolete("AddGraphicsThreadMirrorViewBlit(CommandBuffer, bool) is deprecated. Use AddGraphicsThreadMirrorViewBlit(CommandBuffer, bool, int) instead.", false)]
        public bool AddGraphicsThreadMirrorViewBlit(CommandBuffer cmd, bool allowGraphicsStateInvalidate)
        {
            return AddGraphicsThreadMirrorViewBlit(cmd, allowGraphicsStateInvalidate, XRMirrorViewBlitMode.LeftEye);
        }

        [NativeMethod(Name = "AddGraphicsThreadMirrorViewBlit", IsThreadSafe = false)]
        [NativeHeader("Runtime/Graphics/CommandBuffer/RenderingCommandBuffer.h")]
        [NativeConditional("ENABLE_XR")]
        extern public bool AddGraphicsThreadMirrorViewBlit(CommandBuffer cmd, bool allowGraphicsStateInvalidate, int mode);
    }
}
