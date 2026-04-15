// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine;

namespace Unity.Loading.LowLevel
{
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/ContentLoad/Public/L0ResultBuffer.h")]
    internal struct ResourceHandle : IEquatable<ResourceHandle>
    {
        internal UInt64 value;

        public bool IsValid => value != 0;

        public bool Equals(ResourceHandle other) => value == other.value;
        public override bool Equals(object obj) => obj is ResourceHandle other && Equals(other);
        public override int GetHashCode() => value.GetHashCode();
        public static bool operator ==(ResourceHandle lhs, ResourceHandle rhs) => lhs.value == rhs.value;
        public static bool operator !=(ResourceHandle lhs, ResourceHandle rhs) => lhs.value != rhs.value;
    }

    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/ContentLoad/Public/L0LoadingSystem.bindings.h")]
    internal struct LoadingResponseQueue : IDisposable
    {
        internal IntPtr m_Ptr;

        public LoadingResponseQueue()
        {
            this = ResponseQueue_Create();
        }

        public bool IsCreated => m_Ptr != IntPtr.Zero;

        public unsafe int ConsumeResults(AsyncResult* outResults, int maxResults)
        {
            return ResponseQueue_ConsumeResults(this, outResults, maxResults);
        }

        public void Dispose()
        {
            if (m_Ptr != IntPtr.Zero)
            {
                ResponseQueue_Release(this);
                m_Ptr = IntPtr.Zero;
            }
        }

        [FreeFunction("ContentLoad::L0Bindings::ResponseQueue_Create", isThreadSafe:true)]
        private static extern LoadingResponseQueue ResponseQueue_Create();

        [FreeFunction("ContentLoad::L0Bindings::ResponseQueue_Release", isThreadSafe: true)]
        private static extern void ResponseQueue_Release(LoadingResponseQueue queue);

        [FreeFunction("ContentLoad::L0Bindings::ResponseQueue_ConsumeResults", isThreadSafe: true)]
        private static extern unsafe int ResponseQueue_ConsumeResults(LoadingResponseQueue queue, AsyncResult* outResults, int maxResults);
    }

    [NativeHeader("Modules/ContentLoad/Public/L0ResultBuffer.h")]
    internal enum AsyncResultType
    {
        Load = 0,
        Release = 1
    }

    [NativeHeader("Modules/ContentLoad/Public/L0ResultBuffer.h")]
    internal enum ReturnCode
    {
        Completed = 0,
        Failed = -1
    }

    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/ContentLoad/Public/L0ResultBuffer.h")]
    internal struct AsyncResult
    {
        public ResourceHandle handle;
        public AsyncResultType type;
        public ReturnCode resultCode;
        public EntityId objectId;
    }

    [NativeHeader("Modules/ContentLoad/Public/L0LoadingSystem.bindings.h")]
    [StaticAccessor("ContentLoad::L0Bindings", StaticAccessorType.DoubleColon)]
    internal sealed unsafe class NativeLoadingSystem
    {
        public static extern void LoadAsync(LoadableObjectId* loadableObjectIds, ResourceHandle* outHandles, int count, LoadingResponseQueue resultQueue);

        public static extern void ReleaseAsync(ResourceHandle* handles, int count, LoadingResponseQueue resultQueue);

        public static extern void WaitForLoadCompletion(ResourceHandle* handles, int count);

        public static extern void WaitForReleaseCompletion(ResourceHandle* handles, int count);
    }
}
