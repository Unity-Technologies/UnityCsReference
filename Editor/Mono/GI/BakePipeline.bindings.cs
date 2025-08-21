// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEditor.LightBaking
{
    [RequiredByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Editor/Src/GI/BakePipeline/BakePipeline.bindings.h")]
    internal sealed class BakePipelineDriver : IDisposable
    {
        private IntPtr _ptr;
        private readonly bool _ownsPtr;

        public BakePipelineDriver()
        {
            _ptr = Internal_Create();
            _ownsPtr = true;
        }
        public BakePipelineDriver(IntPtr ptr)
        {
            _ptr = ptr;
            _ownsPtr = false;
        }

        public void Dispose()
        {
            Destroy();
            GC.SuppressFinalize(this);
        }

        private void Destroy()
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

        public extern void SetEnableBakedLightmaps(bool enableBakedLightmaps);
        public extern void SetEnablePatching(bool enablePatching);
        public extern void Update(bool isOnDemandBakeInProgress, bool isOnDemandBakeAsync, bool shouldBeRunning,
            ref float progress, ref int currentStage); // The 'int currentStage' will be cast to BakePipeline::Run::StageName
        public extern bool RunInProgress();

        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToUnmanaged(BakePipelineDriver bakePipelineDriver) => bakePipelineDriver._ptr;
        }
    }
}
