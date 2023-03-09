// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;

namespace UnityEngine
{
    [StructLayout(LayoutKind.Sequential)]
    [RequiredByNativeCode]
    [NativeHeader("Runtime/Export/Scripting/AsyncOperation.bindings.h")]
    [NativeHeader("Runtime/Misc/AsyncOperation.h")]
    public partial class AsyncOperation : YieldInstruction
    {
        [NativeMethod(IsThreadSafe = true)]
        [StaticAccessor("AsyncOperationBindings", StaticAccessorType.DoubleColon)]
        private static extern void InternalDestroy(IntPtr ptr);

        [NativeMethod(IsThreadSafe = true)]
        [StaticAccessor("AsyncOperationBindings", StaticAccessorType.DoubleColon)]
        private static extern void InternalSetManagedObject(IntPtr ptr, [Unmarshalled] AsyncOperation self);

        public AsyncOperation() {}

        protected AsyncOperation(IntPtr ptr)
        {
            InternalSetManagedObject(ptr, this);
            m_Ptr = ptr; 
        }

        public extern bool isDone
        {
            [NativeMethod("IsDone")]
            get;
        }

        public extern float progress
        {
            [NativeMethod("GetProgress")]
            get;
        }

        public extern int priority
        {
            [NativeMethod("GetPriority")]
            get;
            [NativeMethod("SetPriority")]
            set;
        }

        public extern bool allowSceneActivation
        {
            [NativeMethod("GetAllowSceneActivation")]
            get;
            [NativeMethod("SetAllowSceneActivation")]
            set;
        }

        internal static class BindingsMarshaller
        {
            public static AsyncOperation ConvertToManaged(IntPtr ptr) => new AsyncOperation(ptr);
            public static IntPtr ConvertToNative(AsyncOperation asyncOperation) => asyncOperation.m_Ptr;
        }
    }
}
