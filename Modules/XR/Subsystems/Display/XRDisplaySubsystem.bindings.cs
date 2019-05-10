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

namespace UnityEngine.Experimental.XR
{
    [NativeType(Header = "Modules/XR/Subsystems/Display/XRDisplaySubsystem.h")]
    [UsedByNativeCode]
    [NativeHeader("Modules/XR/XRPrefix.h")]
    [NativeConditional("ENABLE_XR")]
    public class XRDisplaySubsystem : IntegratedSubsystem<XRDisplaySubsystemDescriptor>
    {
        public static event Action<bool> displayFocusChanged;

        [RequiredByNativeCode]
        private static void InvokeDisplayFocusChanged(bool focus)
        {
            if (displayFocusChanged != null)
                displayFocusChanged.Invoke(focus);
        }

        extern public bool singlePassRenderingDisabled { get; set; }
        extern public bool displayOpaque { get; }
        extern public bool contentProtectionEnabled { get; set; }


        public enum ReprojectionMode
        {
            Unspecified,
            PositionAndOrientation,
            OrientationOnly,
            None
        };

        extern public ReprojectionMode reprojectionMode { get; set; }

        extern public void SetFocusPlane(Vector3 point, Vector3 normal, Vector3 velocity);

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
    }
}
