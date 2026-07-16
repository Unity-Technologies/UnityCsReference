// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEditor.LightBaking
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct PostProcessProbeRequest
    {
        public bool dering;
        public float indirectScale;
        public string outputFolderPath;
    };

    [RequiredByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Editor/Src/GI/BakePipeline/BakePipeline.bindings.h")]
    internal class PostProcessRequests
    {
        private IntPtr _ptr;

        public PostProcessRequests(IntPtr ptr)
        {
            _ptr = ptr;
        }

        internal extern void SetProbeRequests(PostProcessProbeRequest[] requests);
        internal extern PostProcessProbeRequest[] GetProbeRequests();

        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToUnmanaged(PostProcessRequests connection) => connection._ptr;
        }
    }

    [RequiredByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Editor/Src/GI/BakePipeline/BakePipeline.bindings.h")]
    sealed class BakePipelineDriver : IDisposable
    {
        IntPtr _ptr;
        readonly bool _ownsPtr;

        BakePipelineDriver()
        {
            _ptr = Internal_Create();
            _ownsPtr = true;
        }
        BakePipelineDriver(IntPtr ptr)
        {
            _ptr = ptr;
            _ownsPtr = false;
        }
        ~BakePipelineDriver()
        {
            Destroy();
        }

        public void Dispose()
        {
            Destroy();
            GC.SuppressFinalize(this);
        }

        void Destroy()
        {
            if (_ownsPtr && _ptr != IntPtr.Zero)
            {
                Internal_Destroy(_ptr);
                _ptr = IntPtr.Zero;
            }
        }
        static extern IntPtr Internal_Create();
        [NativeMethod(IsThreadSafe = true)]
        static extern void Internal_Destroy(IntPtr ptr);

        extern void SetEnableBakedLightmaps(bool enableBakedLightmaps);
        extern void SetEnablePatching(bool enablePatching);
        extern void Update(bool isOnDemandBakeInProgress, bool isOnDemandBakeAsync, bool shouldBeRunning,
            ref float progress, ref StageName currentStage);
        extern bool RunInProgress();
        extern void ClearProgress();

        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToUnmanaged(BakePipelineDriver bakePipelineDriver) => bakePipelineDriver._ptr;
        }

        // Keep this in sync with the enum in Editor\Src\GI\BakePipeline\BakePipeline.bindings.h
        enum StageName
        {
            Invalid = -1,
            Initialized,
            Preprocess,
            PreprocessProbes,
            Bake,
            PostProcess,
            AdditionalBake,
            Done
        };
    }
}
